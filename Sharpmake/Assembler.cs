// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Build.Utilities;

namespace Sharpmake
{
    public class Assembler
    {
        /// <summary>
        /// Extra user directory to load assembly from using statement detection
        /// </summary>
        [Obsolete("AssemblyDirectory is not used anymore")]
        public List<string> AssemblyDirectory { get { return _assemblyDirectory; } }

        /// <summary>
        /// Extra user assembly to use while compiling
        /// </summary>
        public List<Assembly> Assemblies { get { return _assemblies; } }

        /// <summary>
        /// Extra user assembly file name to use while compiling
        /// </summary>
        public IReadOnlyList<string> References { get { return _references; } }

        private readonly HashSet<string> _defines;

        /// <summary>
        /// Source attribute parser to use to add configuration based on source code
        /// </summary>
        public List<ISourceAttributeParser> AttributeParsers { get { return _attributeParsers; } }

        /// <summary>
        /// Parsing flow parsers to use to add configuration based on source code
        /// </summary>
        public List<IParsingFlowParser> ParsingFlowParsers { get { return _parsingFlowParsers; } }

        public bool UseDefaultParsers = true;

        public bool UseDefaultReferences = true;

        public static readonly string[] DefaultReferences = { "System.dll", "System.Core.dll" };

        private class AssemblyInfo : IAssemblyInfo
        {
            public string Id { get; set; }
            public string DebugProjectName { get; set; }
            public Assembly Assembly { get; set; }
            public IReadOnlyCollection<string> SourceFiles => _sourceFiles;
            public IReadOnlyCollection<string> References => _references;
            public IReadOnlyDictionary<string, IAssemblyInfo> SourceReferences => _sourceReferences;
            public bool UseDefaultReferences { get; set; }

            public List<string> _sourceFiles = new List<string>();
            public List<string> _references = new List<string>();
            public Dictionary<string, IAssemblyInfo> _sourceReferences = new Dictionary<string, IAssemblyInfo>();
        }

        public Assembler()
            : this(new HashSet<string>())
        {
        }

        public Assembler(HashSet<string> defines)
        {
            _defines = defines;
        }

        public Assembly BuildAssembly(params string[] sourceFiles)
        {
            return BuildAssembly(null, sourceFiles).Assembly;
        }

        public IAssemblyInfo BuildAssembly(IBuilderContext context, params string[] sourceFiles)
        {
            // Always compile to a physic dll to be able to debug
            string tmpFile = GetNextTmpAssemblyFilePath();
            return Build(context, tmpFile, sourceFiles);
        }

        public static TDelegate BuildDelegate<TDelegate>(string sourceFilePath, string fullFunctionName, Assembly[] assemblies)
            where TDelegate : class
        {
            FileInfo fileInfo = new FileInfo(sourceFilePath);
            if (!fileInfo.Exists)
                throw new Error("source file name not found: {0}", sourceFilePath);

            Type delegateType = typeof(TDelegate);
            Error.Valid(IsDelegate(delegateType), "BuildDelegate<FUNC_TYPE>(), FUNC_TYPE is not a delegate");
            MethodInfo delegateMethodInfo = GetDelegateMethodInfo(delegateType);


            ParameterInfo[] delegateParameterInfos = delegateMethodInfo.GetParameters();
            ParameterInfo delegateReturnInfos = delegateMethodInfo.ReturnParameter;

            Assembly assembly;

            Assembler assembler = new Assembler();
            assembler.UseDefaultReferences = false;
            assembler.Assemblies.AddRange(assemblies);

            assembly = assembler.BuildAssembly(fileInfo.FullName);

            List<MethodInfo> matchMethods = new List<MethodInfo>();

            foreach (Type type in assembly.GetTypes())
            {
                MethodInfo[] methodInfos = type.GetMethods();

                foreach (MethodInfo methodInfo in methodInfos)
                {
                    string fullName = methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
                    if (fullFunctionName == fullName &&
                        methodInfo.IsStatic && methodInfo.GetParameters().Length == delegateMethodInfo.GetParameters().Length)
                    {
                        ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                        ParameterInfo returnInfos = methodInfo.ReturnParameter;

                        bool equal = (returnInfos.GetType() == delegateReturnInfos.GetType() &&
                                       parameterInfos.Length == delegateParameterInfos.Length);

                        if (equal)
                        {
                            for (int i = 0; i < parameterInfos.Length; ++i)
                            {
                                if (parameterInfos[i].GetType() != delegateParameterInfos[i].GetType())
                                {
                                    equal = false;
                                    break;
                                }
                            }
                        }
                        if (equal)
                            matchMethods.Add(methodInfo);
                    }
                }
            }

            if (matchMethods.Count != 1)
                throw new Error("Cannot find method name {0} that match {1} in {2}", fullFunctionName, delegateMethodInfo.ToString(), sourceFilePath);

            MethodInfo method = matchMethods[0];

            // bind the method
            Delegate returnDelegate;
            try
            {
                returnDelegate = method.CreateDelegate(delegateType);
                InternalError.Valid(returnDelegate != null);
            }
            catch (Exception e)
            {
                throw new InternalError(e);
            }

            TDelegate result = returnDelegate as TDelegate;
            InternalError.Valid(result != null, "Cannot cast built delegate into user delegate");

            return result;
        }

        public static TDelegate BuildDelegate<TDelegate>(string functionBody, string functionNamespace, string[] usingNamespaces, Assembly[] assemblies)
            where TDelegate : class
        {
            Assembler assembler = new Assembler();
            assembler.UseDefaultReferences = false;
            assembler.Assemblies.AddRange(assemblies);

            const string className = "AssemblerBuildFunction_Class";
            const string methodName = "AssemblerBuildFunction_Method";

            // Fix : Bug with -> Path.GetTempFileName
            // http://msdn.microsoft.com/en-ca/library/windows/desktop/aa364991(v=vs.85).aspx
            // Limit of 65535 limit on files when generating the temp file. New temp file will use
            // a new Guid as filename and Sharpmake will clean the temporary files when done by aggregating
            // the temp files and deleting them.
            // eg. "C:\\fastbuild-work\\85f7d472c25d494ca09f2ea7fe282d50"
            //string sourceTmpFile = Path.GetTempFileName();
            string sourceTmpFile = Path.Combine(Path.GetTempPath(), (Guid.NewGuid().ToString("N") + ".tmp"));

            Type delegateType = typeof(TDelegate);

            Error.Valid(IsDelegate(delegateType), "BuildDelegate<TDelegate>(), TDelegate is not a delegate");

            MethodInfo methodInfo = GetDelegateMethodInfo(delegateType);

            using (StreamWriter writer = new StreamWriter(sourceTmpFile))
            {
                // add using namespace...
                foreach (string usingNamespace in usingNamespaces)
                    writer.WriteLine("using {0};", usingNamespace);
                writer.WriteLine();

                // namespace name
                writer.WriteLine("namespace {0}", functionNamespace);
                writer.WriteLine("{");
                writer.WriteLine("    public static class {0}", className);
                writer.WriteLine("    {");

                // write method signature
                string returnTypeName = methodInfo.ReturnType == typeof(void) ? "void" : methodInfo.ReturnType.FullName;
                writer.Write("        public static {0} {1}(", returnTypeName, methodName);
                ParameterInfo[] parameters = methodInfo.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    string parametersName = parameters[i].Name;
                    string parametersType = (parameters[i].ParameterType == typeof(Object)) ? "Object" : parameters[i].ParameterType.FullName;

                    writer.Write("{0}{1} {2}", i == 0 ? "" : ", ", parametersType, parametersName);
                }
                writer.WriteLine(")");
                // write method body
                writer.WriteLine("        {");
                writer.WriteLine("            {0}" + Environment.NewLine, functionBody.Replace("\n", "\n            "));
                writer.WriteLine("        }");
                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            // build in memory
            Assembly assembly = assembler.Build(null, null, sourceTmpFile).Assembly;
            InternalError.Valid(assembly != null);

            // Try to delete tmp file to prevent pollution, but useful while debugging
            //if (!System.Diagnostics.Debugger.IsAttached)
            Util.TryDeleteFile(sourceTmpFile);

            // Scan assembly to find our tmp class
            string fullClassName = functionNamespace + "." + className;
            Type buildedType = assembly.GetType(fullClassName);

            // get out method to bind into the delegate
            MethodInfo builtMethod = buildedType.GetMethod(methodName);
            InternalError.Valid(builtMethod != null);

            // bind the method
            Delegate returnDelegate;
            try
            {
                returnDelegate = builtMethod.CreateDelegate(delegateType);
                InternalError.Valid(returnDelegate != null);
            }
            catch (Exception e)
            {
                throw new InternalError(e);
            }

            TDelegate result = returnDelegate as TDelegate;
            InternalError.Valid(result != null, "Cannot cast built delegate into user delegate");

            return result;
        }

        #region Internal

        internal delegate void OutputDelegate(string message, params object[] args);
        internal static event OutputDelegate EventOutputError;
        internal static event OutputDelegate EventOutputWarning;

        #endregion

        #region Private

        private List<string> _assemblyDirectory = new List<string>();
        private List<Assembly> _assemblies = new List<Assembly>();
        private List<string> _references = new List<string>();
        private List<ISourceAttributeParser> _attributeParsers = new List<ISourceAttributeParser>();
        private List<IParsingFlowParser> _parsingFlowParsers = new List<IParsingFlowParser>();

        private static bool IsDelegate(Type delegateType)
        {
            if (delegateType.BaseType != typeof(MulticastDelegate))
                return false;
            MethodInfo invoke = delegateType.GetMethod("Invoke");
            return (invoke != null);
        }

        private static MethodInfo GetDelegateMethodInfo(Type delegateType)
        {
            if (!IsDelegate(delegateType))
                throw new Error("not a delegate: {0}", delegateType);
            return delegateType.GetMethod("Invoke");
        }

        private class AssemblerContext : IAssemblerContext
        {
            private readonly Assembler _assembler;
            private readonly AssemblyInfo _assemblyInfo;
            public IReadOnlyList<string> SourceFiles => _assemblyInfo.SourceFiles.ToList();
            private Strings _visiting;
            public readonly List<IParsingFlowParser> AllParsingFlowParsers;
            public readonly List<ISourceAttributeParser> AllParsers;
            public List<ISourceAttributeParser> ImportedParsers = new List<ISourceAttributeParser>();
            private readonly IBuilderContext _builderContext;

            public AssemblerContext(Assembler assembler, AssemblyInfo assemblyInfo, IBuilderContext builderContext, string[] sources)
            {
                _assembler = assembler;
                _assemblyInfo = assemblyInfo;
                _builderContext = builderContext;
                AllParsers = assembler.ComputeParsers();
                AllParsingFlowParsers = assembler.ComputeParsingFlowParsers();
                _assemblyInfo._sourceFiles.AddRange(sources);
                _visiting = new Strings(new FileSystemStringComparer(), sources);
            }

            public void AddSourceFile(string file)
            {
                if (!_visiting.Contains(file))
                {
                    _assemblyInfo._sourceFiles.Add(file);
                    _visiting.Add(file);
                }
            }

            public void AddReference(string file)
            {
                if (!_assemblyInfo._references.Contains(file))
                {
                    _assemblyInfo._references.Add(file);
                    var loadInfo = _builderContext.LoadExtension(file);
                    this.AddSourceAttributeParsers(loadInfo.Parsers);
                }
            }

            public void AddReference(IAssemblyInfo info)
            {
                if (info.Assembly == null)
                {
                    _assemblyInfo._sourceReferences.Add(info.Id, info);
                }
                else if (!_assemblyInfo._references.Contains(info.Id))
                {
                    _assemblyInfo._references.Add(info.Assembly.Location);
                    _assemblyInfo._sourceReferences.Add(info.Id, info);
                }
            }

            public void AddSourceAttributeParser(ISourceAttributeParser parser)
            {
                AllParsers.Add(parser);
                ImportedParsers.Add(parser);
            }

            public IAssemblyInfo BuildAndLoadSharpmakeFiles(params string[] files)
            {
                if (_builderContext == null)
                    throw new NotSupportedException("BuildAndLoadSharpmakeFiles is not supported on builds without a IBuilderContext");

                var loadInfo = _builderContext.BuildAndLoadSharpmakeFiles(AllParsers, AllParsingFlowParsers, files);
                this.AddSourceAttributeParsers(loadInfo.Parsers);
                return loadInfo.AssemblyInfo;
            }

            public void SetDebugProjectName(string name)
            {
                _assemblyInfo.DebugProjectName = name;
            }

            public void AddDefine(string define)
            {
                _builderContext.AddDefine(define);
            }
        }

        private IAssemblyInfo Build(IBuilderContext builderContext, string libraryFile, params string[] sources)
        {
            var assemblyInfo = LoadAssemblyInfo(builderContext, sources);

            HashSet<string> references = new HashSet<string>();

            Dictionary<string, string> providerOptions = new Dictionary<string, string>();
            providerOptions.Add("CompilerVersion", "v4.0");
            CodeDomProvider provider = new Microsoft.CSharp.CSharpCodeProvider(providerOptions);

            CompilerParameters cp = new CompilerParameters();

            if (UseDefaultReferences)
            {
                foreach (string defaultReference in DefaultReferences)
                    references.Add(GetAssemblyDllPath(defaultReference));
            }

            foreach (string assemblyFile in _references)
                references.Add(assemblyFile);

            foreach (Assembly assembly in _assemblies)
            {
                if (!assembly.IsDynamic)
                    references.Add(assembly.Location);
            }

            cp.ReferencedAssemblies.AddRange(references.ToArray());

            // Generate an library
            cp.GenerateExecutable = false;

            // Set the level at which the compiler
            // should start displaying warnings.
            cp.WarningLevel = 4;

            // Set whether to treat all warnings as errors.
            cp.TreatWarningsAsErrors = false;

            // Set compiler argument to optimize output.
            // TODO : figure out why it does not work when uncommenting the following line
            // cp.CompilerOptions = "/optimize";

            // If any defines are specified, pass them to the CSC.
            if (_defines.Any())
            {
                cp.CompilerOptions = "-DEFINE:" + string.Join(",", _defines);
            }

            // Specify the assembly file name to generate
            if (libraryFile == null)
            {
                cp.GenerateInMemory = true;
                cp.IncludeDebugInformation = false;
            }
            else
            {
                cp.GenerateInMemory = false;
                cp.IncludeDebugInformation = true;
                cp.OutputAssembly = libraryFile;
            }

            // Notes:
            // Avoid getting spoiled by environment variables. 
            // C# will give compilation errors if a LIB variable contains non-existing directories.
            Environment.SetEnvironmentVariable("LIB", null);

            // Configure Temp file collection to avoid deleting its temp file. We will delete them ourselves after the compilation
            // For some reasons, this seems to add just enough delays to avoid the following first chance exception(probably caused by some handles in csc.exe)
            // System.IO.IOException: 'The process cannot access the file 'C:\Users\xxx\AppData\Local\Temp\sa205152\sa205152.out' because it is being used by another process.'            
            // That exception wasn't causing real problems but was really annoying when debugging!
            // Executed several times sharpmake and this first chance exception no longer occurs when KeepFiles is true.
            cp.TempFiles.KeepFiles = true;

            // Invoke compilation of the source file.
            CompilerResults cr = provider.CompileAssemblyFromFile(cp, assemblyInfo.SourceFiles.ToArray());

            // Manually delete the files in the temp files collection.
            cp.TempFiles.Delete();

            if (cr.Errors.HasErrors || cr.Errors.HasWarnings)
            {
                string errorMessage = "";
                foreach (CompilerError ce in cr.Errors)
                {
                    if (ce.IsWarning)
                        EventOutputWarning?.Invoke("{0}" + Environment.NewLine, ce.ToString());
                    else
                        EventOutputError?.Invoke("{0}" + Environment.NewLine, ce.ToString());

                    errorMessage += ce + Environment.NewLine;
                }

                if (cr.Errors.HasErrors)
                {
                    if (builderContext == null || builderContext.CompileErrorBehavior == BuilderCompileErrorBehavior.ThrowException)
                        throw new Error(errorMessage);
                    return assemblyInfo;
                }
            }

            assemblyInfo.Assembly = cr.CompiledAssembly;
            assemblyInfo.Id = assemblyInfo.Assembly.Location;
            return assemblyInfo;
        }

        private List<ISourceAttributeParser> ComputeParsers()
        {
            var parsers = AttributeParsers.ToList();
            if (UseDefaultParsers)
                AddDefaultParsers(parsers);
            return parsers;
        }

        private List<IParsingFlowParser> ComputeParsingFlowParsers()
        {
            List<IParsingFlowParser> parsers = ParsingFlowParsers.ToList();
            if (UseDefaultParsers)
                AddDefaultParsingFlowParsers(parsers);
            return parsers;
        }

        private AssemblyInfo LoadAssemblyInfo(IBuilderContext builderContext, string[] sources)
        {
            var assemblyInfo = new AssemblyInfo()
            {
                Id = string.Join(";", sources),
                UseDefaultReferences = UseDefaultReferences
            };

            var context = new AssemblerContext(this, assemblyInfo, builderContext, sources);
            AnalyseSourceFiles(context);

            _references.AddRange(assemblyInfo.References);

            return assemblyInfo;
        }

        internal IAssemblyInfo LoadUncompiledAssemblyInfo(IBuilderContext context, string[] sources)
        {
            return LoadAssemblyInfo(context, sources);
        }

        internal List<string> GetSourceFiles(IBuilderContext builderContext, string[] sources)
        {
            var assemblyInfo = new AssemblyInfo()
            {
                Id = string.Join(";", sources),
                UseDefaultReferences = UseDefaultReferences
            };

            var context = new AssemblerContext(this, assemblyInfo, builderContext, sources);
            AnalyseSourceFiles(context);
            return assemblyInfo.SourceFiles.ToList();
        }

        private void AddDefaultParsers(ICollection<ISourceAttributeParser> parsers)
        {
            parsers.Add(new IncludeAttributeParser());
            parsers.Add(new ReferenceAttributeParser());
            parsers.Add(new PackageAttributeParser());
        }

        private void AddDefaultParsingFlowParsers(ICollection<IParsingFlowParser> parsers)
        {
            parsers.Add(new PreprocessorConditionParser(_defines));
        }

        private void AnalyseSourceFiles(AssemblerContext context)
        {
            var newParsers = Enumerable.Empty<ISourceAttributeParser>();
            var allParsers = context.AllParsers.ToList(); // Copy, as it may be modified when parsing other files
            int partiallyParsedCount = 0;

            do
            {
                // Get all using namespace from sourceFiles
                for (int i = 0; i < context.SourceFiles.Count; ++i)
                {
                    string sourceFile = context.SourceFiles[i];
                    if (File.Exists(sourceFile))
                    {
                        AnalyseSourceFile(sourceFile, (i < partiallyParsedCount) ? newParsers : allParsers, context.AllParsingFlowParsers, context);
                    }
                    else
                    {
                        throw new Error("source file not found: " + sourceFile);
                    }
                }
                // Get parsers discovered while parsing these files
                // We need to reparse all files currently in the list (partiallyParsedCount) again with the new parsers only,
                // and all files discovered after this with all the parsers.
                newParsers = context.ImportedParsers;
                context.ImportedParsers = new List<ISourceAttributeParser>();
                allParsers.AddRange(newParsers);
                partiallyParsedCount = context.SourceFiles.Count;
            } while (newParsers.Any());
        }

        internal void ParseSourceAttributesFromLine(
            string line,
            FileInfo sourceFilePath,
            int lineNumber,
            IAssemblerContext context
        )
        {
            ParseSourceAttributesFromLine(line, sourceFilePath, lineNumber, ComputeParsers(), context);
        }

        internal void ParseSourceAttributesFromLine(
            string line,
            FileInfo sourceFilePath,
            int lineNumber,
            IEnumerable<ISourceAttributeParser> parsers,
            IAssemblerContext context
        )
        {
            foreach (var parser in parsers)
            {
                parser.ParseLine(line, sourceFilePath, lineNumber, context);
            }
        }

        private void AnalyseSourceFile(string sourceFile, IEnumerable<ISourceAttributeParser> parsers, IEnumerable<IParsingFlowParser> flowParsers, IAssemblerContext context)
        {
            using (StreamReader reader = new StreamReader(sourceFile))
            {
                FileInfo sourceFilePath = new FileInfo(sourceFile);
                List<IParsingFlowParser> flowParsersList = flowParsers.ToList();

                foreach (IParsingFlowParser parsingFlowParser in flowParsersList)
                {
                    parsingFlowParser.FileParsingBegin(sourceFile);
                }

                int lineNumber = 0;
                string line = reader.ReadLine()?.TrimStart();
                while (line != null)
                {
                    ++lineNumber;

                    if (!string.IsNullOrEmpty(line) && line.StartsWith("namespace", StringComparison.Ordinal))
                        break;

                    // First, update the parsing flow with the current line
                    foreach (IParsingFlowParser parsingFlowParser in flowParsersList)
                    {
                        parsingFlowParser.ParseLine(line, sourceFilePath, lineNumber, context);
                    }

                    // We only want to parse the lines inside valid blocks
                    if (!flowParsersList.Any() || flowParsersList.All(p => p.ShouldParseLine()))
                    {
                        ParseSourceAttributesFromLine(line, sourceFilePath, lineNumber, parsers, context);
                    }

                    line = reader.ReadLine()?.TrimStart();
                }

                foreach (IParsingFlowParser parsingFlowParser in flowParsersList)
                {
                    parsingFlowParser.FileParsingEnd(sourceFile);
                }
            }
        }

        public static IEnumerable<string> EnumeratePathToDotNetFramework()
        {
            for (int i = (int)TargetDotNetFrameworkVersion.VersionLatest; i >= 0; --i)
            {
                string frameworkDirectory = ToolLocationHelper.GetPathToDotNetFramework((TargetDotNetFrameworkVersion)i);
                if (frameworkDirectory != null)
                    yield return frameworkDirectory;
            }
        }

        public static string GetAssemblyDllPath(string fileName)
        {
            foreach (string frameworkDirectory in EnumeratePathToDotNetFramework())
            {
                string result = Path.Combine(frameworkDirectory, fileName);
                if (File.Exists(result))
                    return result;
            }
            return null;
        }

        /// <summary>
        /// Static constructor called at executable init time
        /// </summary>
        static Assembler()
        {
            CleanupTmpAssemblies();
        }

        /// <summary>
        /// This method is intended to be called at executable init time. 
        /// It let us avoid exceptions when executing sharpmake several times in loops(exception can occur in the cs compiler
        /// when it tries to create pdb files and some already exists. Maybe that previous sharpmake sometimes still has some handles to the file?).
        /// With this cleanup code active there is no exception anymore on my PC. Previously I had the exception almost 100% on the second or third iteration
        /// of a stability test(executing sharpmake in loop to insure it always generate the same thing).
        /// </summary>
        /// <remarks>
        /// Was previously having the following exception when running stability tests(on subsequents sharpmake execution runs):
        /// Unexpected error creating debug information file 'c:\Users\xxxx\AppData\Local\Temp\Sharpmake.Assembler_1.tmp.PDB' -- 'c:\Users\xxxx\AppData\Local\Temp\Sharpmake.Assembler_1.tmp.pdb: The process cannot access the file because it is being used by another process.
        /// </remarks>
        private static void CleanupTmpAssemblies()
        {
            // Erase any remaining file that has the prefix that will be used for temporary assemblies(dll, pdb, etc...)
            // This avoids exceptions occurring when executing sharpmake several times in loops(for example when running stability tests)
            string[] oldTmpFiles = Directory.GetFiles(GetTmpAssemblyBasePath(), GetTmpAssemblyFilePrefix() + "*.*", SearchOption.TopDirectoryOnly);
            foreach (string f in oldTmpFiles)
            {
                Util.TryDeleteFile(f);
            }
        }

        /// <summary>
        /// Get the base path of temporary assembly files.
        /// </summary>
        /// <returns>the base path</returns>
        private static string GetTmpAssemblyBasePath()
        {
            return Path.GetTempPath();
        }

        /// <summary>
        /// Get the assembly files common prefixes for all temporary assemblies generated in this process.
        /// </summary>
        /// <returns>the prefix</returns>
        private static string GetTmpAssemblyFilePrefix()
        {
            // Now taking into account the working directory when setting the temporary assembly prefix.
            // That is useful to be able to run several sharpmake concurrently with /sharpmakemutexsuffix otherwise they can cause harm to each others.

            // Note: Util.BuildGuid is converting the argument to a MD5.
            string md5WorkingDir = Util.BuildGuid(Environment.CurrentDirectory).ToString().ToLower();
            return $"Sharpmake_Assembly_{md5WorkingDir}_";
        }

        private static int s_nextTempFile = 0; // Index of last assembly temporary file

        /// <summary>
        /// Get the next temporary assembly file path.
        /// </summary>
        /// <returns>path of next temporary assembly</returns>
        private string GetNextTmpAssemblyFilePath()
        {
            // try to re use the same file name to not pollute tmp directory
            string tmpFileBasePath = GetTmpAssemblyBasePath();
            string tmpFileSuffix = ".tmp.dll";

            int currentTempFile = Interlocked.Increment(ref s_nextTempFile);
            string tmpFile = Path.Combine(GetTmpAssemblyBasePath(), GetTmpAssemblyFilePrefix() + currentTempFile + tmpFileSuffix);
            return tmpFile;
        }

        #endregion
    }
}
