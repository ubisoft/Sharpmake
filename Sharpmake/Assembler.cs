// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
#if NET5_0
using BasicReferenceAssemblies = Basic.Reference.Assemblies.Net50;
#elif NET6_0
using BasicReferenceAssemblies = Basic.Reference.Assemblies.Net60;
#else
#error unhandled framework version
#endif

namespace Sharpmake
{
    public class Assembler
    {
        public const Options.CSharp.LanguageVersion SharpmakeScriptsCSharpVersion = Options.CSharp.LanguageVersion.CSharp10;
#if NET5_0
        public const DotNetFramework SharpmakeDotNetFramework = DotNetFramework.net5_0;
#elif NET6_0
        public const DotNetFramework SharpmakeDotNetFramework = DotNetFramework.net6_0;
#else
#error unhandled framework version
#endif

        /// <summary>
        /// Extra user assembly to use while compiling
        /// </summary>
        public List<Assembly> Assemblies { get { return _assemblies; } }

        /// <summary>
        /// Extra user assembly file name to use while compiling/running
        /// </summary>
        public IReadOnlyList<string> References { get { return _references; } }

        /// <summary>
        /// Extra user assembly file name to use while compiling
        /// </summary>
        public IReadOnlyList<string> BuildReferences { get { return _buildReferences; } }

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

        [Obsolete("Default references are always used.")]
        public bool UseDefaultReferences = true;

        public static readonly string[] DefaultReferences = BasicReferenceAssemblies.ReferenceInfos.All.Select(r => r.FileName).ToArray();

        private class AssemblyInfo : IAssemblyInfo
        {
            public string Id { get; set; }
            public string DebugProjectName { get; set; }
            public Assembly Assembly { get; set; }
            public IReadOnlyCollection<string> SourceFiles => _sourceFiles;
            public IReadOnlyCollection<string> NoneFiles => _noneFiles;
            
            
            [Obsolete("Use RuntimeReference instead")]
            public IReadOnlyCollection<string> References => RuntimeReferences;
            public IReadOnlyCollection<string> RuntimeReferences => _runtimeReferences;
            public IReadOnlyCollection<string> BuildReferences => _buildReferences;
            public IReadOnlyDictionary<string, IAssemblyInfo> SourceReferences => _sourceReferences;
            public bool UseDefaultReferences { get; set; }

            public List<string> _sourceFiles = new List<string>();
            public List<string> _noneFiles = new List<string>();

            public List<string> _runtimeReferences = new List<string>();
            public List<string> _buildReferences = new List<string>();
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

            Assembler assembler = new Assembler();
            assembler.AddSharpmakeAssemblies();
            assembler.Assemblies.AddRange(assemblies);

            Assembly assembly = assembler.BuildAssembly(fileInfo.FullName);

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
            assembler.AddSharpmakeAssemblies();
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
            string sourceTmpFile = Path.Combine(Path.GetTempPath(), (Guid.NewGuid().ToString("N") + ".tmp.sharpmake.cs"));

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
                    string parametersType = (parameters[i].ParameterType == typeof(object)) ? "Object" : parameters[i].ParameterType.FullName;

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
            Assembly assembly = assembler.Build(builderContext: null, libraryFile: null, sources: sourceTmpFile).Assembly;
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

        internal void AddSharpmakeAssemblies()
        {
            // Add sharpmake assembly
            Assemblies.Add(s_sharpmakeAssembly.Value);

            // Add generators and common platforms assemblies to be able to reference them from .sharpmake.cs files
            Assemblies.Add(s_sharpmakeGeneratorAssembly.Value);
            Assemblies.Add(s_sharpmakeCommonPlatformsAssembly.Value);
        }

        #endregion

        #region Private

        private List<Assembly> _assemblies = new List<Assembly>();
        private List<string> _references = new List<string>();
        private List<string> _buildReferences = new List<string>();
        private List<ISourceAttributeParser> _attributeParsers = new List<ISourceAttributeParser>();
        private List<IParsingFlowParser> _parsingFlowParsers = new List<IParsingFlowParser>();

        private static readonly Lazy<Assembly> s_sharpmakeAssembly = new Lazy<Assembly>(() => Assembly.GetAssembly(typeof(Builder)));
        private static readonly Lazy<Assembly> s_sharpmakeGeneratorAssembly = new Lazy<Assembly>(() =>
        {
            DirectoryInfo entryDirectoryInfo = new DirectoryInfo(Path.GetDirectoryName(s_sharpmakeAssembly.Value.Location));
            string generatorsAssembly = Path.Combine(entryDirectoryInfo.FullName, "Sharpmake.Generators.dll");
            return Assembly.LoadFrom(generatorsAssembly);
        });
        private static readonly Lazy<Assembly> s_sharpmakeCommonPlatformsAssembly = new Lazy<Assembly>(() =>
        {
            DirectoryInfo entryDirectoryInfo = new DirectoryInfo(Path.GetDirectoryName(s_sharpmakeAssembly.Value.Location));
            string generatorsAssembly = Path.Combine(entryDirectoryInfo.FullName, "Sharpmake.CommonPlatforms.dll");
            return Assembly.LoadFrom(generatorsAssembly);
        });

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
                //Make sure to use clean files path
                var cleanSourceFiles = sources?.Select(s => Path.GetFullPath(s));
                _assemblyInfo._sourceFiles.AddRange(cleanSourceFiles);
                _visiting = new Strings(new FileSystemStringComparer(), cleanSourceFiles);
            }

            public void AddSourceFile(string file)
            {
                //Make sure to use clean file path
                //To avoid ambiguity for example, consider these 2 file paths
                //F:\my_workspace\git\XXX\.\XXX.sharpmake.cs
                //F:\my_workspace\git\XXX\XXX.sharpmake.cs
                var cleanFilePath = Path.GetFullPath(file);

                if (!_visiting.Contains(cleanFilePath))
                {
                    _assemblyInfo._sourceFiles.Add(cleanFilePath);
                    _visiting.Add(cleanFilePath);
                }
            }

            public void AddNoneFile(string file)
            {
                if (!_assemblyInfo._noneFiles.Contains(file))
                    _assemblyInfo._noneFiles.Add(file);
            }

            [Obsolete("Use AddRuntimeReference() instead")]
            public void AddReference(string file) => AddRuntimeReference(file);

            public void AddRuntimeReference(string file)
            {
                if (!_assemblyInfo._runtimeReferences.Contains(file))
                {
                    _assemblyInfo._runtimeReferences.Add(file);
                    var loadInfo = _builderContext.LoadExtension(file);
                    this.AddSourceAttributeParsers(loadInfo.Parsers);
                }
            }

            public void AddBuildReference(string file)
            {
                if (!_assemblyInfo._buildReferences.Contains(file))
                {
                    _assemblyInfo._buildReferences.Add(file);
                }
            }

            [Obsolete("Use AddRuntimeReference() instead")]
            public void AddReference(IAssemblyInfo info) => AddRuntimeReference(info);

            public void AddRuntimeReference(IAssemblyInfo info)
            {
                if (info.Assembly == null)
                {
                    _assemblyInfo._sourceReferences.Add(info.Id, info);
                }
                else if (!_assemblyInfo._runtimeReferences.Contains(info.Id))
                {
                    _assemblyInfo._runtimeReferences.Add(info.Assembly.Location);
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
            HashSet<string> references = GetReferencesForBuild();

            assemblyInfo.Assembly = Compile(builderContext, assemblyInfo.SourceFiles.ToArray(), libraryFile, references);
            assemblyInfo.Id = assemblyInfo.Assembly.Location;
            return assemblyInfo;
        }

        private ConcurrentDictionary<string, string> _buildReferenceFullNames = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private HashSet<string> GetReferencesForBuild()
        {
            // First find the assembly
            Parallel.ForEach(_buildReferences, (buildAssemblyfile) =>
            {
                string fullPath = AssemblyName.GetAssemblyName(buildAssemblyfile).FullName;
                _buildReferenceFullNames.TryAdd(fullPath, buildAssemblyfile);
            });

            var references = new ConcurrentDictionary<string, bool>();

            // Search if we have a more suitable build reference for each runtime reference
            Parallel.ForEach(_references, (assemblyFile) =>
            {
                var assemblyFullName = AssemblyName.GetAssemblyName(assemblyFile).FullName;
                string buildAssemblyFile = null;
                _buildReferenceFullNames.TryGetValue(assemblyFullName, out buildAssemblyFile);
                references.TryAdd(buildAssemblyFile ?? assemblyFile, true);
            });

            foreach (Assembly assembly in _assemblies)
            {
                if (!assembly.IsDynamic)
                    references.TryAdd(assembly.Location, true);
            }

            return references.Keys.ToHashSet<string>();
        }

        private SourceText ReadSourceCode(string path)
        {
            using (var stream = File.OpenRead(path))
                return SourceText.From(stream, Encoding.Default);
        }

        private Assembly Compile(IBuilderContext builderContext, string[] files, string libraryFile, HashSet<string> references)
        {
            // Parse all files
            var syntaxTrees = new ConcurrentBag<SyntaxTree>();
            var parseOptions = new CSharpParseOptions(ConvertSharpmakeOptionToLanguageVersion(SharpmakeScriptsCSharpVersion), DocumentationMode.None, preprocessorSymbols: _defines);
            Parallel.ForEach(files, f =>
            {
                var sourceText = ReadSourceCode(f);
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(sourceText, parseOptions, path: f));
            });

            return Compile(builderContext, syntaxTrees, libraryFile, references);
        }

        private Assembly Compile(IBuilderContext builderContext, IEnumerable<SyntaxTree> syntaxTrees, string libraryFile, HashSet<string> fileReferences)
        {
            // Add references
            var metadataReferences = new List<MetadataReference>();

            foreach (var reference in fileReferences.Where(r => !string.IsNullOrEmpty(r)))
            {
                // Skip references that are already provided by the runtime
                if (BasicReferenceAssemblies.References.All.Any(a => string.Equals(Path.GetFileName(reference), a.FilePath, StringComparison.OrdinalIgnoreCase)))
                    continue;
                metadataReferences.Add(MetadataReference.CreateFromFile(reference));
            }

            metadataReferences.AddRange(BasicReferenceAssemblies.References.All);

            // suppress assembly redirect warnings
            // cf. https://github.com/dotnet/roslyn/issues/19640
            var noWarn = new List<KeyValuePair<string, ReportDiagnostic>>
            {
                new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress),
                new KeyValuePair<string, ReportDiagnostic>("CS1702", ReportDiagnostic.Suppress),
            };

            // Compile
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: (builderContext == null || builderContext.DebugScripts) ? OptimizationLevel.Debug : OptimizationLevel.Release,
                warningLevel: 4,
                specificDiagnosticOptions: noWarn,
                deterministic: true
            );

            var assemblyName = libraryFile != null ? Path.GetFileNameWithoutExtension(libraryFile) : $"Sharpmake_{new Random().Next():X8}" + GetHashCode();
            var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, metadataReferences, compilationOptions);
            string pdbFilePath = libraryFile != null ? Path.ChangeExtension(libraryFile, ".pdb") : null;

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                EmitResult result = compilation.Emit(
                    dllStream,
                    pdbStream,
                    options: new EmitOptions(
                        debugInformationFormat: DebugInformationFormat.PortablePdb,
                        pdbFilePath: pdbFilePath
                    )
                );

                bool throwErrorException = builderContext == null || builderContext.CompileErrorBehavior == BuilderCompileErrorBehavior.ThrowException;
                LogCompilationResult(result, throwErrorException);

                if (result.Success)
                {
                    if (libraryFile != null)
                    {
                        dllStream.Seek(0, SeekOrigin.Begin);
                        using (var fileStream = new FileStream(libraryFile, FileMode.Create))
                            dllStream.CopyTo(fileStream);

                        pdbStream.Seek(0, SeekOrigin.Begin);
                        using (var pdbFileStream = new FileStream(pdbFilePath, FileMode.Create))
                            pdbStream.CopyTo(pdbFileStream);

                        return Assembly.LoadFrom(libraryFile);
                    }

                    return Assembly.Load(dllStream.GetBuffer(), pdbStream.GetBuffer());
                }
            }

            return null;
        }

        private void LogCompilationResult(EmitResult result, bool throwErrorException)
        {
            string errorMessage = "";

            foreach (var diagnostic in result.Diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                    EventOutputError?.Invoke("{0}" + Environment.NewLine, diagnostic.ToString());
                else // catch everything else as warning
                    EventOutputWarning?.Invoke("{0}" + Environment.NewLine, diagnostic.ToString());

                errorMessage += diagnostic + Environment.NewLine;
            }

            if (!result.Success && throwErrorException)
                throw new Error(errorMessage);
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
            };

            var context = new AssemblerContext(this, assemblyInfo, builderContext, sources);
            AnalyseSourceFiles(context);

            _references.AddRange(assemblyInfo.RuntimeReferences);
            _buildReferences.AddRange(assemblyInfo.BuildReferences);

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

        [Obsolete]
        public static IEnumerable<string> EnumeratePathToDotNetFramework()
        {
            yield break;
        }

        private static LanguageVersion ConvertSharpmakeOptionToLanguageVersion(Options.CSharp.LanguageVersion languageVersion)
        {
            switch (languageVersion)
            {
                case Options.CSharp.LanguageVersion.LatestMajorVersion:
                    return LanguageVersion.LatestMajor;
                case Options.CSharp.LanguageVersion.LatestMinorVersion:
                    return LanguageVersion.Latest;
                case Options.CSharp.LanguageVersion.Preview:
                    return LanguageVersion.Preview;
                case Options.CSharp.LanguageVersion.ISO1:
                    return LanguageVersion.CSharp1;
                case Options.CSharp.LanguageVersion.ISO2:
                    return LanguageVersion.CSharp2;
                case Options.CSharp.LanguageVersion.CSharp3:
                    return LanguageVersion.CSharp3;
                case Options.CSharp.LanguageVersion.CSharp4:
                    return LanguageVersion.CSharp4;
                case Options.CSharp.LanguageVersion.CSharp5:
                    return LanguageVersion.CSharp5;
                case Options.CSharp.LanguageVersion.CSharp6:
                    return LanguageVersion.CSharp6;
                case Options.CSharp.LanguageVersion.CSharp7:
                    return LanguageVersion.CSharp7;
                case Options.CSharp.LanguageVersion.CSharp7_1:
                    return LanguageVersion.CSharp7_1;
                case Options.CSharp.LanguageVersion.CSharp7_2:
                    return LanguageVersion.CSharp7_2;
                case Options.CSharp.LanguageVersion.CSharp7_3:
                    return LanguageVersion.CSharp7_3;
                case Options.CSharp.LanguageVersion.CSharp8:
                    return LanguageVersion.CSharp8;
                case Options.CSharp.LanguageVersion.CSharp9:
                    return LanguageVersion.CSharp9;
                case Options.CSharp.LanguageVersion.CSharp10:
                    return LanguageVersion.CSharp10;
                default:
                    throw new NotImplementedException($"Don't know how to convert sharpmake option {languageVersion} to language version");
            }
        }

        [Obsolete]
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
