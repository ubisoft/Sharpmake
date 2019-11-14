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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

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

        static readonly string[] _defaultReferences =
        {
            // Minimum required assemblies
            typeof(object).Assembly.Location, // mscorelib.dll for .NET, System.Private.CoreLib.dll for .NET Core
            "System.dll",
            "System.Core.dll",
            "System.Linq.dll",
            "System.Runtime.dll",
            "System.Collections.dll",
            "System.IO.FileSystem.dll",

            // Common utilitie assemblies that were commonly included in old c# compiler
            typeof(System.Text.RegularExpressions.Regex).Assembly.Location,
            typeof(Console).Assembly.Location,
            typeof(StreamReader).Assembly.Location,
        };
        
        // Get the super set of assemblies I currently have loaded and explicit default references
        public static readonly string[] DefaultReferences = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.Location)
            .Where(f => f.IndexOf("Sharpmake", StringComparison.OrdinalIgnoreCase) == -1)
            .Concat(_defaultReferences.Select(GetAssemblyDllPath).Where(f => !string.IsNullOrEmpty(f)).ToArray())
            .Distinct()
            .ToArray();

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
            // Alway compile to a physic dll to be able to debug
            string tmpFile = GetTmpAssemblyFile();
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

            // Try to delete tmp file to prevent polution, but usefull while debugging
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
        }

        private IAssemblyInfo Build(IBuilderContext builderContext, string libraryFile, params string[] sources)
        {
            var assemblyInfo = LoadAssemblyInfo(builderContext, sources);

            // Parse all input files
            var parseOptions = new CSharpParseOptions(preprocessorSymbols: _defines);

            var syntaxTrees = assemblyInfo.SourceFiles
                .AsParallel()
                .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), options: parseOptions, path: file, encoding: System.Text.Encoding.UTF8))
                .ToList();

            // Build references list
            HashSet<string> referenceFiles = new HashSet<string>();

            if (UseDefaultReferences)
            {
                foreach (string defaultReference in DefaultReferences)
                    referenceFiles.Add(defaultReference);
            }

            foreach (string assemblyFile in _references)
                referenceFiles.Add(assemblyFile);

            foreach (Assembly assembly in _assemblies)
            {
                if (!assembly.IsDynamic)
                    referenceFiles.Add(assembly.Location);
            }

            var references = referenceFiles
                .AsParallel()
                .Where(f => !string.IsNullOrEmpty(f))
                .Select(file => MetadataReference.CreateFromFile(file))
                .ToList();

            // Compiler Options
            var options = new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,

                generalDiagnosticOption: ReportDiagnostic.Error,

                // Set the level at which the compiler
                // should start displaying warnings.
                warningLevel: 4,
                
                optimizationLevel: libraryFile == null ? OptimizationLevel.Release : OptimizationLevel.Debug
            );

            // Create Compiler
            var compiler = CSharpCompilation.Create("Sharpmake_Generated", syntaxTrees, references, options);

            EmitResult compileResult;

            // Specify the assembly file name to generate
            if (libraryFile == null)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    compileResult = compiler.Emit(stream);
                    if (compileResult.Success)
                        assemblyInfo.Assembly = Assembly.Load(stream.GetBuffer());
                }
            }
            else
            {
                var pdbFile = Path.ChangeExtension(libraryFile, "pdb");
                using (var assemblyStream = File.Open(libraryFile, FileMode.Create, FileAccess.ReadWrite))
                using (var pdbStream = File.Open(pdbFile, FileMode.Create, FileAccess.ReadWrite))
                {
                    var emitOptions = new EmitOptions(pdbFilePath: pdbFile);
                    if (Util.IsRunningOnUnix() || Util.IsRunningInMono())
                        emitOptions = emitOptions.WithDebugInformationFormat(DebugInformationFormat.PortablePdb);

                    compileResult = compiler.Emit(assemblyStream, pdbStream, options:emitOptions);
                }

                if (compileResult.Success)
                    assemblyInfo.Assembly = Assembly.LoadFile(libraryFile);
            }

            if (compileResult.Diagnostics.Any())
            {
                string errorMessage = "";
                foreach (var diag in compileResult.Diagnostics)
                {
                    if (diag.Severity == DiagnosticSeverity.Warning)
                        EventOutputWarning?.Invoke(diag.ToString() + Environment.NewLine);
                    else if (diag.Severity == DiagnosticSeverity.Error)
                        EventOutputError?.Invoke(diag.ToString() + Environment.NewLine);

                    errorMessage += diag.ToString() + Environment.NewLine;
                }

                if (!compileResult.Success)
                {
                    if (builderContext == null || builderContext.CompileErrorBehavior == BuilderCompileErrorBehavior.ThrowException)
                        throw new Error(errorMessage);
                    return assemblyInfo;
                }
            }

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

                    if (!string.IsNullOrEmpty(line) && line.StartsWith("namespace", StringComparison.Ordinal))
                        break;
                }

                foreach (IParsingFlowParser parsingFlowParser in flowParsersList)
                {
                    parsingFlowParser.FileParsingEnd(sourceFile);
                }
            }
        }

        public static string GetAssemblyDllPath(string fileName)
        {
            if (File.Exists(fileName))
                return fileName;

            return VisualStudioExtension.EnumeratePathToDotNetFramework()
                .Select(path => Path.Combine(path, fileName))
                .FirstOrDefault(File.Exists);
        }

        private static int s_nextTempFile = 0;

        [System.Diagnostics.DebuggerNonUserCode]
        private string GetTmpAssemblyFile()
        {
            // try to re use the same file name to not pollute tmp directory
            string tmpFilePrefix = GetType().FullName + "_";
            string tmpFileSuffix = ".tmp.dll";

            while (s_nextTempFile < int.MaxValue)
            {
                int currentTempFile = Interlocked.Increment(ref s_nextTempFile);
                string tmpFile = Path.Combine(Path.GetTempPath(), tmpFilePrefix + currentTempFile + tmpFileSuffix);
                if (!File.Exists(tmpFile) || Util.TryDeleteFile(tmpFile))
                {
                    return tmpFile;
                }
            }
            return null;
        }

        #endregion
    }
}
