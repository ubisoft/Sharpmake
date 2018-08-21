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
using System.Text.RegularExpressions;
using Microsoft.Build.Utilities;

namespace Sharpmake
{
    public class Assembler
    {
        /// <summary>
        /// Extra user directory to load assembly from using statement detection
        /// </summary>
        public List<string> AssemblyDirectory { get { return _assemblyDirectory; } }

        /// <summary>
        /// Extra user assembly to use while compiling
        /// </summary>
        public List<Assembly> Assemblies { get { return _assemblies; } }

        /// <summary>
        /// Extra user assembly file name to use while compiling
        /// </summary>
        public List<string> References { get { return _references; } }

        /// <summary>
        /// Source attribute parser to use to add configuration based on source code
        /// </summary>
        public List<ISourceAttributeParser> AttributeParsers { get { return _attributeParsers; } }

        public bool UseDefaultParsers = true;

        public bool UseDefaultReferences = true;

        public static readonly string[] DefaultReferences = { "System.dll", "System.Core.dll" };
        
        private class AssemblyInfo : IAssemblyInfo
        {
            public string Id { get; set; }
            public Assembly Assembly { get; set; }
            public IReadOnlyCollection<string> SourceFiles => _sourceFiles;
            public IReadOnlyCollection<string> References => _references;
            public IReadOnlyDictionary<string, IAssemblyInfo> SourceReferences => _sourceReferences;
            public bool UseDefaultReferences { get; set; }

            public List<string> _sourceFiles = new List<string>();
            public List<string> _references = new List<string>();
            public Dictionary<string, IAssemblyInfo> _sourceReferences = new Dictionary<string, IAssemblyInfo>();
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
        public List<string> _references = new List<string>();
        private Dictionary<string, IAssemblyInfo> _sourceReferences = new Dictionary<string, IAssemblyInfo>();
        private List<ISourceAttributeParser> _attributeParsers = new List<ISourceAttributeParser>();

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
            public List<string> SourceFiles;
            private Strings _visiting;
            public List<ISourceAttributeParser> AllParsers = new List<ISourceAttributeParser>();
            public List<ISourceAttributeParser> ImportedParsers = new List<ISourceAttributeParser>();
            private readonly IBuilderContext _builderContext;

            public AssemblerContext(Assembler assembler, IBuilderContext builderContext, string[] sources)
            {
                _builderContext = builderContext;
                _assembler = assembler;
                AllParsers = assembler.ComputeParsers();
                _visiting = new Strings(new FileSystemStringComparer(), sources);
                SourceFiles = new List<string>(_visiting);
            }

            public void AddSourceFile(string file)
            {
                if (!_visiting.Contains(file))
                {
                    SourceFiles.Add(file);
                    _visiting.Add(file);
                }
            }

            public void AddReference(string file)
            {
                if (!_assembler._references.Contains(file))
                    _assembler._references.Add(file);
            }

            public void AddReference(IAssemblyInfo info)
            {
                if (info.Assembly == null)
                {
                    _assembler._sourceReferences.Add(info.Id, info);
                }
                else if (!_assembler._references.Contains(info.Id))
                {
                    _assembler._references.Add(info.Assembly.Location);
                    _assembler._sourceReferences.Add(info.Id, info);
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

                var loadInfo = _builderContext.BuildAndLoadSharpmakeFiles(AllParsers, files);
                this.AddSourceAttributeParsers(loadInfo.Parsers);
                return loadInfo.AssemblyInfo;
            }

            public BuilderCompileErrorBehavior CompileErrorBehavior => _builderContext?.CompileErrorBehavior ?? BuilderCompileErrorBehavior.ThrowException;
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

            // Invoke compilation of the source file.
            CompilerResults cr = provider.CompileAssemblyFromFile(cp, assemblyInfo.SourceFiles.ToArray());

            if (cr.Errors.HasErrors || cr.Errors.HasWarnings)
            {
                string errorMessage = "";
                foreach (CompilerError ce in cr.Errors)
                {
                    if(ce.IsWarning)
                        EventOutputWarning?.Invoke(ce + Environment.NewLine);
                    else
                        EventOutputError?.Invoke(ce + Environment.NewLine);

                    errorMessage += ce + Environment.NewLine;
                }

                if (cr.Errors.HasErrors)
                {
                    if (builderContext.CompileErrorBehavior == BuilderCompileErrorBehavior.ThrowException)
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

        private AssemblyInfo LoadAssemblyInfo(IBuilderContext builderContext, string[] sources)
        {
            var context = new AssemblerContext(this, builderContext, sources);
            AnalyseSourceFiles(context);

            return new AssemblyInfo()
            {
                Id = string.Join(";", context.SourceFiles),
                UseDefaultReferences = UseDefaultReferences,
                _sourceFiles = context.SourceFiles.ToList(),
                _references = _references.ToList(),
                _sourceReferences = new Dictionary<string, IAssemblyInfo>(_sourceReferences),
            };
        }

        internal List<string> GetSourceFiles(IBuilderContext builderContext, string[] sources)
        {
            var context = new AssemblerContext(this, builderContext, sources);
            AnalyseSourceFiles(context);
            return context.SourceFiles;
        }

        private void AddDefaultParsers(ICollection<ISourceAttributeParser> parsers)
        {
            parsers.Add(new IncludeAttributeParser());
            parsers.Add(new ReferenceAttributeParser());
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
                        AnalyseSourceFile(sourceFile, (i < partiallyParsedCount) ? newParsers : allParsers, context);
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

        private void AnalyseSourceFile(string sourceFile, IEnumerable<ISourceAttributeParser> parsers, IAssemblerContext context)
        {
            using (StreamReader reader = new StreamReader(sourceFile))
            {
                FileInfo sourceFilePath = new FileInfo(sourceFile);

                int lineNumber = 0;
                string line = reader.ReadLine();
                while (line != null)
                {
                    ++lineNumber;
                    
                    ParseSourceAttributesFromLine(line, sourceFilePath, lineNumber, parsers, context);

                    line = reader.ReadLine()?.TrimStart();

                    if (!string.IsNullOrEmpty(line) && line.StartsWith("namespace", StringComparison.Ordinal))
                        break;
                }
            }
        }

        public static string GetAssemblyDllPath(string fileName)
        {
            for (int i = (int)TargetDotNetFrameworkVersion.VersionLatest; i >= 0; --i)
            {
                string frameworkDirectory = ToolLocationHelper.GetPathToDotNetFramework((TargetDotNetFrameworkVersion)i);
                if (frameworkDirectory != null)
                {
                    string result = Path.Combine(frameworkDirectory, fileName);
                    if (File.Exists(result))
                        return result;
                }
            }
            return null;
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
                int currentTempFile = s_nextTempFile;
                ++s_nextTempFile;
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
