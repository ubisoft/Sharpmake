// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sharpmake
{
    public class Arguments
    {
        internal List<object> FragmentMasks = new List<object>();

        public Builder Builder { get; }

        public ConfigureOrder ConfigureOrder = ConfigureOrder.New;

        public FileInfo MainFileInfo = null;

        public void AddFragmentMask(params object[] fragmentMasks)
        {
            foreach (object fragmentMask in fragmentMasks)
            {
                Type fragmentType = fragmentMask.GetType();
                ITarget.ValidFragmentType(fragmentType);
                FragmentMasks.Add(fragmentMask);
            }
        }

        public bool HasFragmentMask(params object[] fragmentMasks)
        {
            return FragmentMasks.Any(f =>
            {
                foreach (object fragmentMask in fragmentMasks)
                {
                    Type fragmentType = fragmentMask.GetType();
                    ITarget.ValidFragmentType(fragmentType);
                    if (f.GetType() == fragmentType)
                    {
                        return Enum.Equals(f, fragmentMask);
                    }
                }
                return false;
            });
        }

        internal List<Type> TypesToGenerate = new List<Type>();

        public void Generate<T>()
        {
            Generate(typeof(T));
        }

        public void Generate(Type type)
        {
            TypesToGenerate.Add(type);
        }

        public Arguments(Builder builder)
        {
            Builder = builder;
        }
    }

    public class Builder : IDisposable
    {
        public static Builder Instance;

        #region Events
        // Output events
        public delegate void OutputDelegate(string message, params object[] args);
        public event OutputDelegate EventOutputError;
        public event OutputDelegate EventOutputWarning;
        public event OutputDelegate EventOutputMessage;
        public event OutputDelegate EventOutputDebug;

        // Configure events
        public delegate void PreProjectConfigure(Project project);
        public event PreProjectConfigure EventPreProjectConfigure;
        public delegate void PostProjectConfigure(Project project, Project.Configuration conf);
        public event PostProjectConfigure EventPostProjectConfigure;
        public delegate void PreSolutionConfigure(Solution solution);
        public event PreSolutionConfigure EventPreSolutionConfigure;
        public delegate void PostSolutionConfigure(Solution solution, Solution.Configuration conf);
        public event PostSolutionConfigure EventPostSolutionConfigure;

        // Link events
        public delegate void PreProjectLink(Project project);
        public event PreProjectLink EventPreProjectLink;
        public delegate void PostProjectLink(Project project);
        public event PostProjectLink EventPostProjectLink;
        public delegate void PreSolutionLink(Solution solution);
        public event PreSolutionLink EventPreSolutionLink;
        public delegate void PostSolutionLink(Solution solution);
        public event PostSolutionLink EventPostSolutionLink;

        // Generate events
        public delegate void PreGeneration(List<Project> projects, List<Solution> solutions);
        public event PreGeneration EventPreGeneration;
        public delegate void PostGeneration(List<Project> projects, List<Solution> solutions);
        public event PostGeneration EventPostGeneration;
        public delegate void PostGenerationReport(List<Project> projects, List<Solution> solutions, ConcurrentDictionary<Type, GenerationOutput> generationReport);
        public event PostGenerationReport EventPostGenerationReport;
        #endregion

        public Arguments Arguments = null;

        public bool SkipInvalidPath { get; set; }

        private bool _multithreaded = false;
        public bool DumpDependencyGraph { get; private set; }
        private bool _cleanBlobsOnly = false;
        public bool BlobOnly = false;
        public bool Diagnostics = false;
        private readonly bool _debugScripts = false;
        private ThreadPool _tasks;
        // Keep all instances of manually built (and loaded) assemblies, as they may be needed by other assemblies on load (command line).
        private readonly ConcurrentDictionary<string, Assembly> _builtAssemblies = new ConcurrentDictionary<string, Assembly>(); // Assembly Full Path -> Assembly
        private readonly Dictionary<string, string> _runtimeReferences = new Dictionary<string, string>(); // Keep track of runtime assemblies explicitly referenced from scripts, and needed during execution

        private class ProfilingCompleteEvent
        {
            public static readonly string pid = Process.GetCurrentProcess().Id.ToString(); // The process ID for the process that output this event
            public string name; //  The name of the event, as displayed in Trace Viewer
            public string tid; // The thread ID for the thread that output this event
            public string ts; // The tracing clock timestamp of the event. The timestamps are provided at microsecond granularity

            public string dur; // comes with the above: tracing clock duration of complete events in microseconds
            public int count = -1;

            public string ToJsonString()
            {
                if (count != -1)
                    return $"{{ \"pid\":{pid}, \"tid\":{tid}, \"ts\":{ts}, \"dur\":{dur}, \"ph\":\"X\", \"name\":\"{name}\", \"args\": {{\"count\": {count}}}}}";
                else
                    return $"{{ \"pid\":{pid}, \"tid\":{tid}, \"ts\":{ts}, \"dur\":{dur}, \"ph\":\"X\", \"name\":\"{name}\" }}";
            }
        }
        private readonly ConcurrentBag<ProfilingCompleteEvent> _profilingCompleteEvents = new ConcurrentBag<ProfilingCompleteEvent>();

        private class ProfilingInstantEvent
        {
            public static readonly string pid = Process.GetCurrentProcess().Id.ToString(); // The process ID for the process that output this event
            public string name; //  The name of the event, as displayed in Trace Viewer
            public string tid; // The thread ID for the thread that output this event
            public string ts; // The tracing clock timestamp of the event. The timestamps are provided at microsecond granularity

            public string ToJsonString()
            {
                return $"{{ \"pid\":{pid}, \"tid\":{tid}, \"ts\":{ts}, \"ph\":\"i\", \"name\":\"{name}\" }}";
            }
        }
        private readonly ConcurrentBag<ProfilingInstantEvent> _profilingInstantEvents = new ConcurrentBag<ProfilingInstantEvent>();


        public void AddProfilingCompleteEvent(string name, long startTicks, long endTicks, int countArg = -1)
        {
            var durTicks = endTicks - startTicks;
            if (durTicks > 0)
            {
                double start = 1000000.0 * (double)startTicks / Stopwatch.Frequency;
                double end = 1000000.0 * (double)endTicks / Stopwatch.Frequency;
                double dur = end - start;

                _profilingCompleteEvents.Add(new ProfilingCompleteEvent
                {
                    name = name,
                    tid = Thread.CurrentThread.ManagedThreadId.ToString(),
                    dur = dur.ToString(),
                    ts = start.ToString(),
                    count = countArg
                });
            }
        }

        public void AddProfilingInstantEvent(string name)
        {
            double start = 1000000.0 * (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;

            _profilingInstantEvents.Add(new ProfilingInstantEvent
            {
                name = name,
                tid = Thread.CurrentThread.ManagedThreadId.ToString(),
                ts = start.ToString(),
            });
        }

        private class TraceFile { }

        private string _traceFileHeaderTemplate = @"{ ""traceEvents"": [" + Environment.NewLine;
        private string _traceFileFooterTemplate = @"]}";

        public void DumpTraceFile(string filePath)
        {
            if (_profilingCompleteEvents.IsEmpty && _profilingInstantEvents.IsEmpty)
                return;

            var mStream = new MemoryStream();
            var writer = new StreamWriter(mStream);
            {
                writer.Write(_traceFileHeaderTemplate);
                var lines = new List<string>(_profilingCompleteEvents.Count + _profilingInstantEvents.Count);
                foreach (var traceEvent in _profilingCompleteEvents)
                {
                    lines.Add(traceEvent.ToJsonString());
                }
                foreach (var traceEvent in _profilingInstantEvents)
                {
                    lines.Add(traceEvent.ToJsonString());
                }
                writer.Write(string.Join("," + Environment.NewLine, lines));
                writer.Write(_traceFileFooterTemplate);
            }
            writer.Flush();

            var generationOutput = new GenerationOutput();
            var fileInfo = new FileInfo(filePath);
            if (Util.FileWriteIfDifferentInternal(fileInfo, mStream, bypassAutoCleanupDatabase: true))
                generationOutput.Generated.Add(fileInfo.FullName);
            else
                generationOutput.Skipped.Add(fileInfo.FullName);

            ReportGenerated(typeof(TraceFile), generationOutput);
        }

        public BuildContext.BaseBuildContext Context { get; private set; }

        private readonly BuilderExtension _builderExt;

        private ConcurrentDictionary<Type, GenerationOutput> _generationReport = new ConcurrentDictionary<Type, GenerationOutput>();
        private HashSet<Type> _buildScheduledType = new HashSet<Type>();

        private HashSet<Project.Configuration> _usedProjectConfigurations = null;

        public HashSet<string> Defines { get; }

        private readonly List<ISourceAttributeParser> _attributeParsers = new List<ISourceAttributeParser>();

        private static readonly Lazy<Regex> s_defineValidationRegex = new Lazy<Regex>(() => new Regex(@"^\w+$", RegexOptions.Compiled));

        public Builder(
            BuildContext.BaseBuildContext context,
            bool multithreaded,
            bool dumpDependencyGraph,
            bool cleanBlobsOnly,
            bool blobOnly,
            bool skipInvalidPath,
            bool diagnostics,
            bool debugScripts,
            Func<IGeneratorManager> getGeneratorsManagerCallBack,
            HashSet<string> defines)
        {
            Context = context;
            Arguments = new Arguments(this);
            _multithreaded = multithreaded;
            DumpDependencyGraph = dumpDependencyGraph;
            _cleanBlobsOnly = cleanBlobsOnly;
            BlobOnly = blobOnly;
            Diagnostics = diagnostics;
            _debugScripts = debugScripts;
            SkipInvalidPath = skipInvalidPath;
            _getGeneratorsManagerCallBack = getGeneratorsManagerCallBack;
            _getGeneratorsManagerCallBack().InitializeBuilder(this);
            Trace.Assert(Instance == null);
            Instance = this;
            _builderExt = new BuilderExtension(this);
            Defines = defines ?? new HashSet<string>();

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            if (Diagnostics)
                EventPostGeneration += LogUnusedProjectConfigurations;

            if (_multithreaded)
            {
                _tasks = new ThreadPool();
                int nbThreads = Environment.ProcessorCount;
                _tasks.Start(nbThreads);
            }
        }

        [Obsolete("Use the builder with the new debugScripts argument", error: false)]
        public Builder(BuildContext.BaseBuildContext context, bool multithreaded, bool dumpDependencyGraph, bool cleanBlobsOnly, bool blobOnly, bool skipInvalidPath, bool diagnostics, Func<IGeneratorManager> getGeneratorsManagerCallBack, HashSet<string> defines)
            : this(context, multithreaded, dumpDependencyGraph, cleanBlobsOnly, blobOnly, skipInvalidPath, diagnostics, true, getGeneratorsManagerCallBack, defines) { }

        public void Dispose()
        {
            if (_multithreaded)
                _tasks.Stop();
            Instance = null;
        }

        public Project GetProject(Type type)
        {
            Project project;
            return _projects.TryGetValue(type, out project) ? project : null;
        }

        public Solution GetSolution(Type type)
        {
            Solution solution;
            return _solutions.TryGetValue(type, out solution) ? solution : null;
        }

        public void ExecuteEntryPointInAssemblies<TEntryPoint>(params Assembly[] assemblies)
            where TEntryPoint : EntryPoint
        {
            MethodInfo entryPointMethodInfo = null;

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes().Where(t => t.IsVisible))
                {
                    foreach (MethodInfo methodInfo in type.GetMethods())
                    {
                        if (!methodInfo.IsDefined(typeof(TEntryPoint), false))
                            continue;

                        if (entryPointMethodInfo != null)
                            throw new Error($"Multiple entry point found, only one entry point method with [{typeof(TEntryPoint).FullName}] is expected "
                                            + $"({entryPointMethodInfo.DeclaringType?.FullName}.{entryPointMethodInfo.Name} vs. {type.FullName}.{methodInfo.Name})");

                        if (!methodInfo.IsStatic)
                            throw new Error($"Method {type.FullName}.{methodInfo} is defined as an entry point, but is not static");

                        var parameters = methodInfo.GetParameters();
                        if (parameters.Length != 1 || parameters[0].GetType() == typeof(Arguments))
                            throw new Error($"Method {type.FullName}.{methodInfo} method must have one argument of type {typeof(Arguments).FullName}");

                        entryPointMethodInfo = methodInfo;
                    }
                }
            }

            if (entryPointMethodInfo == null)
                return;


            entryPointMethodInfo.Invoke(null, new object[] { Arguments });
        }

        private bool _profilingEnabled = false;
        public void EnableProfiling()
        {
            _profilingEnabled = true;
        }

        public Util.StopwatchProfiler CreateProfilingScope(string name)
        {
            if (!_profilingEnabled)
                return null;
            return new Util.StopwatchProfiler((start, end) => { AddProfilingCompleteEvent(name, start, end); });
        }
        public Util.StopwatchProfiler CreateProfilingScope(string name, int countArg)
        {
            if (!_profilingEnabled)
                return null;
            return new Util.StopwatchProfiler((start, end) => { AddProfilingCompleteEvent(name, start, end, countArg); });
        }
        public void CreateProfilingInstant(string name)
        {
            if (!_profilingEnabled)
                return;
            AddProfilingInstantEvent(name);
        }

        public void BuildProjectAndSolution()
        {
            LogWriteLine("  building projects and solutions configurations{0}...", _multithreaded ? $" using {_tasks.NumTasks()} tasks" : " single-threaded");
            using (new Util.StopwatchProfiler(ms => { LogWriteLine("    build done in {0:0.0} sec", ms / 1000.0f); }))
            {
                _buildScheduledType.UnionWith(Arguments.TypesToGenerate);

                if (!_multithreaded)
                {
                    var typesToGenerate = new List<Type>(Arguments.TypesToGenerate);
                    for (int i = 0; i < typesToGenerate.Count; ++i)
                    {
                        Type type = typesToGenerate[i];

                        HashSet<Type> projectDependenciesTypes;
                        if (type.IsSubclassOf(typeof(Project)))
                        {
                            Project project = LoadProjectType(type);
                            _projects.Add(type, project);
                            projectDependenciesTypes = project.GetUnresolvedDependenciesTypes();
                        }
                        else if (type.IsSubclassOf(typeof(Solution)))
                        {
                            Solution solution = LoadSolutionType(type);
                            _solutions.Add(type, solution);
                            projectDependenciesTypes = solution.GetDependenciesProjectTypes();
                        }
                        else
                        {
                            throw new Error("error, class type not supported: {0}", type.FullName);
                        }

                        foreach (Type projectDependenciesType in projectDependenciesTypes)
                        {
                            if (_buildScheduledType.Add(projectDependenciesType))
                                typesToGenerate.Add(projectDependenciesType);
                        }
                    }
                }
                else
                {
                    foreach (Type type in Arguments.TypesToGenerate)
                    {
                        _tasks.AddTask(BuildProjectAndSolutionTask, type, type.BaseType == typeof(Project) ? ThreadPool.Priority.Low : ThreadPool.Priority.High);
                    }

                    _tasks.Wait();
                }
            }
        }

        private void BuildProjectAndSolutionTask(object parameter)
        {
            Type type = parameter as Type;
            HashSet<Type> projectDependenciesTypes;

            if (type.IsSubclassOf(typeof(Project)))
            {
                Project project = LoadProjectType(type);
                lock (_projects)
                    _projects.Add(type, project);
                projectDependenciesTypes = project.GetUnresolvedDependenciesTypes();
            }
            else if (type.IsSubclassOf(typeof(Solution)))
            {
                Solution solution = LoadSolutionType(type);
                lock (_solutions)
                    _solutions.Add(type, solution);
                projectDependenciesTypes = solution.GetDependenciesProjectTypes();
            }
            else
            {
                throw new Error("error, class type not supported: {0}", type.FullName);
            }

            lock (_buildScheduledType)
            {
                foreach (Type projectDependenciesType in projectDependenciesTypes)
                {
                    if (_buildScheduledType.Add(projectDependenciesType))
                        _tasks.AddTask(BuildProjectAndSolutionTask, projectDependenciesType, type.BaseType == typeof(Project) ? ThreadPool.Priority.Low : ThreadPool.Priority.High);
                }
            }
        }

        public Assembly[] LoadAssemblies(params string[] assembliesFiles)
        {
            Strings assemblyFolders = new Strings();
            Strings references = new Strings();

            Assembly[] assemblies = new Assembly[assembliesFiles.Length];
            for (int i = 0; i < assembliesFiles.Length; ++i)
            {
                var assemblyFile = assembliesFiles[i];
                try
                {
                    var assembly = Assembly.LoadFile(assemblyFile);
                    assemblies[i] = assembly;

                    assemblyFolders.Add(Path.GetDirectoryName(assemblyFile));

                    // get references from the assembly, if any
                    foreach (var module in assembly.GetModules())
                    {
                        foreach (var reference in module.GetCustomAttributes<Reference>())
                            references.Add(reference.FileName);
                    }
                }
                catch (Exception e)
                {
                    throw new Error(e, "Cannot load assembly file: {0}", assemblyFile);
                }
            }

            using (var extensionLoader = new ExtensionLoader())
            {
                foreach (var dllName in references)
                {
                    foreach (var assemblyFolder in assemblyFolders)
                    {
                        var candidatePath = Path.Combine(assemblyFolder, dllName);
                        if (File.Exists(candidatePath))
                            extensionLoader.LoadExtension(candidatePath);
                    }
                }
            }

            return assemblies;
        }

        private IAssemblyInfo BuildAndLoadAssembly(IList<string> sharpmakeFiles, BuilderCompileErrorBehavior compileErrorBehavior)
        {
            return BuildAndLoadAssembly(sharpmakeFiles, new BuilderContext(this, compileErrorBehavior));
        }

        private IAssemblyInfo BuildAndLoadAssembly(IList<string> sharpmakeFiles, IBuilderContext context, IEnumerable<ISourceAttributeParser> parsers = null, IEnumerable<IParsingFlowParser> flowParsers = null)
        {
            Assembler assembler = new Assembler(Defines);
            assembler.AddSharpmakeAssemblies();

            // Add attribute parsers
            if (parsers != null)
            {
                assembler.UseDefaultParsers = false;
                assembler.AttributeParsers.AddRange(parsers);
            }
            else
            {
                foreach (var parser in _attributeParsers)
                    assembler.AttributeParsers.Add(parser);
            }

            if (flowParsers != null)
            {
                assembler.ParsingFlowParsers.AddRange(flowParsers);
            }

            var newAssemblyInfo = assembler.BuildAssembly(context, sharpmakeFiles.ToArray());

            if (newAssemblyInfo.Assembly == null && context.CompileErrorBehavior == BuilderCompileErrorBehavior.ThrowException)
                throw new InternalError();

            // Keep track of runtime assemblies explicitly referenced by compiled files
            foreach (var fullpath in assembler.References.Distinct())
            {
                var assemblyName = AssemblyName.GetAssemblyName(fullpath).FullName;
                string assemblyPath;
                if (_runtimeReferences.TryGetValue(assemblyName, out assemblyPath) && !string.Equals(assemblyPath, fullpath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Error($"Assembly {assemblyName} present in two different locations: {fullpath} and {assemblyPath}.");
                }
                _runtimeReferences[assemblyName] = fullpath;
            }

            if (newAssemblyInfo.Assembly != null)
                _builtAssemblies[newAssemblyInfo.Assembly.FullName] = newAssemblyInfo.Assembly;
            return newAssemblyInfo;
        }

        // Expect a list of existing files with their full path
        public Assembly LoadSharpmakeFiles(params string[] sharpmakeFiles)
        {
            return LoadSharpmakeFiles(BuilderCompileErrorBehavior.ThrowException, sharpmakeFiles).Assembly;
        }

        public IAssemblyInfo LoadSharpmakeFiles(BuilderCompileErrorBehavior compileErrorBehavior, params string[] sharpmakeFiles)
        {
            return BuildAndLoadAssembly(sharpmakeFiles, compileErrorBehavior);
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Check if this is a built assembly of .sharpmake.cs files that is requested to be loaded
            Assembly builtAssembly = null;
            _builtAssemblies.TryGetValue(args.Name, out builtAssembly);
            if (builtAssembly != null)
                return builtAssembly;

            // Check if this is a runtime assembly referenced by a script, if so, explicitly load it with its fullPath
            string explicitReferencesFullPath;
            if (_runtimeReferences.TryGetValue(args.Name, out explicitReferencesFullPath))
                return Assembly.LoadFrom(explicitReferencesFullPath);

            // Default binding redirect for old versions of an assembly to the implicitly/explicitly referenced one
            var requestedAssemblyName = new AssemblyName(args.Name);
            var referencedAssemblyHighestVersion = _runtimeReferences.Keys
                .Where(assemblyFullName => assemblyFullName.StartsWith(requestedAssemblyName.Name, StringComparison.OrdinalIgnoreCase))
                .Select(assemblyFullName => new AssemblyName(assemblyFullName))
                .OrderBy(assemblyName => assemblyName.Version)
                .LastOrDefault()? // In case the assembly args.Name is referenced with multiple version, take the highest one
                .FullName;

            if (referencedAssemblyHighestVersion != null)
            {
                return Assembly.LoadFrom(_runtimeReferences[referencedAssemblyHighestVersion]);
            }

            return null;
        }

        public Project LoadProjectType(Type type)
        {
            using (CreateProfilingScope(type.ToNiceTypeName()))
            {
                Project.ProjectTypeAttribute projectTypeAttribute;
                if (type.IsDefined(typeof(Generate), false))
                    projectTypeAttribute = Project.ProjectTypeAttribute.Generate;
                else if (type.IsDefined(typeof(Export), false))
                    projectTypeAttribute = Project.ProjectTypeAttribute.Export;
                else if (type.IsDefined(typeof(Compile), false))
                    projectTypeAttribute = Project.ProjectTypeAttribute.Compile;
                else
                    throw new Error("cannot generate project type without [Sharpmake.Generate], [Sharpmake.Compile] or [Sharpmake.Export] attribute: {0}", type.Name);

                // Create the project instance
                Project project = Project.CreateProject(type, Arguments.FragmentMasks, projectTypeAttribute);

                // Pre event
                var eventPreProjectConfigure = EventPreProjectConfigure;
                if (eventPreProjectConfigure != null)
                {
                    using (CreateProfilingScope("EventPreProjectConfigure" + project.ClassName))
                        eventPreProjectConfigure.Invoke(project);
                }

                project.PreConfigure();

                // Create and Configure all possibles configurations.
                using (CreateProfilingScope("Configures" + project.ClassName))
                    project.InvokeConfiguration(Context);

                using (CreateProfilingScope("AfterConfigure" + project.ClassName))
                    project.AfterConfigure();

                // Post event
                if (EventPostProjectConfigure != null)
                {
                    using (CreateProfilingScope("EventPostProjectConfigure" + project.ClassName))
                    {
                        foreach (Project.Configuration conf in project.Configurations)
                            EventPostProjectConfigure?.Invoke(project, conf);
                    }
                }

                // Resolve [*]
                using (CreateProfilingScope("Resolve"))
                    project.Resolve(this, SkipInvalidPath);

                // Would be more optimal to not generate the blobs, but simpler that way
                if (_cleanBlobsOnly)
                    project.CleanBlobs();

                return project;
            }
        }

        public Solution LoadSolutionType(Type type)
        {
            using (CreateProfilingScope(type.ToNiceTypeName()))
            {
                if (!type.IsDefined(typeof(Generate), false) &&
                !type.IsDefined(typeof(Compile), false) &&
                !type.IsDefined(typeof(Export), false))
                    throw new Error("cannot generate solution type without [Sharpmake.Generate], [Sharpmake.Compile] or [Sharpmake.Export] attribute: {0}", type.Name);

                // Create the solution instance
                Solution solution = Solution.CreateSolution(type, Arguments.FragmentMasks);

                // Pre event
                EventPreSolutionConfigure?.Invoke(solution);

                // Create and Configure all possible configurations.
                solution.InvokeConfiguration(Context);

                // Post event
                if (EventPostSolutionConfigure != null)
                {
                    foreach (Solution.Configuration conf in solution.Configurations)
                        EventPostSolutionConfigure?.Invoke(solution, conf);
                }

                // Resolve [*]
                solution.Resolve();

                return solution;
            }
        }

        public void AddDefine(string define)
        {
            if (!s_defineValidationRegex.Value.IsMatch(define))
                throw new Error("error: invalid define '{0}', a define must be a single word", define);

            if (Defines.Add(define))
            {
                DebugWriteLine("Added define: {0}", define);
            }
        }

        internal void RegisterGeneratedProject(Project project)
        {
            lock (_generatedProjects)
                _generatedProjects.Add(project);
        }

        private static void LogObject(TextWriter writer, string prefix, object obj)
        {
            Type type = obj.GetType();

            writer.WriteLine("{0}Type: {1}", prefix, type.Name);

            MemberInfo[] memberInfos = type.GetMembers();

            foreach (MemberInfo memberInfo in memberInfos)
            {
                object value = null;

                if (memberInfo is FieldInfo)
                {
                    FieldInfo fieldInfo = memberInfo as FieldInfo;
                    if (fieldInfo.IsPublic)
                    {
                        try
                        {
                            value = fieldInfo.GetValue(obj);
                        }
                        catch (Exception) { }
                    }
                }
                else if (memberInfo is PropertyInfo)
                {
                    PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                    if (propertyInfo.CanRead)
                    {
                        try
                        {
                            value = propertyInfo.GetValue(obj, null);
                        }
                        catch (Exception) { }
                    }
                }

                if (value != null)
                {
                    if (value is ICollection)
                    {
                        ICollection collection = value as ICollection;

                        writer.WriteLine("{0}\t{1}[{2}]", prefix, memberInfo.Name, collection.Count);
                        foreach (object v in (value as ICollection))
                            writer.WriteLine("{0}\t\t{1}", prefix, v);
                    }
                    else
                    {
                        writer.WriteLine("{0}\t{1}={2}", prefix, memberInfo.Name, value);
                    }
                }
            }
        }

        private void WriteLogs()
        {
            foreach (Project project in _projects.Values)
            {
                string logFileDirectory = Path.Combine(project.SharpmakeCsPath, "log");
                if (!Directory.Exists(logFileDirectory))
                    Directory.CreateDirectory(logFileDirectory);

                string logFile = Path.Combine(logFileDirectory, project.GetType().Name);

                using (TextWriter writer = new StreamWriter(logFile + ".project.log"))
                {
                    LogObject(writer, "", project);
                }

                foreach (Project.Configuration conf in project.Configurations)
                {
                    using (TextWriter writer = new StreamWriter(logFile + "." + conf.Target + ".project.log"))
                    {
                        LogObject(writer, "", conf);
                    }
                }
            }

            foreach (Solution solution in _solutions.Values)
            {
                string logFileDirectory = Path.Combine(solution.SharpmakeCsPath, "log");
                if (!Directory.Exists(logFileDirectory))
                    Directory.CreateDirectory(logFileDirectory);

                FileInfo logFile = new FileInfo(Path.Combine(logFileDirectory, solution.GetType().Name + ".solution.log"));

                using (TextWriter writer = new StreamWriter(logFile.FullName))
                {
                    writer.WriteLine("{0}", solution.Name);
                    LogObject(writer, "", solution);
                    foreach (Solution.Configuration conf in solution.Configurations)
                    {
                        LogObject(writer, "\t", conf);
                        writer.WriteLine("\t{0,-100} {1}" + Path.DirectorySeparatorChar + "{2}.[solution_ext]", conf.Target.GetTargetString(), Util.PathGetRelative(logFile.Directory.FullName, conf.SolutionPath), conf.SolutionFileName);

                        foreach (Solution.Configuration.IncludedProjectInfo configurationProject in conf.IncludedProjectInfos)
                        {
                            Project.Configuration projectConfiguration = configurationProject.Project.GetConfiguration(configurationProject.Target);
                            if (projectConfiguration != null)
                                writer.WriteLine("\t\t\t{0,-20} {1,-80}", configurationProject.Project.Name, configurationProject.Target.GetTargetString());
                        }
                    }
                }
            }
        }

        public void ReportGenerated(Type t, GenerationOutput output)
        {
            var generationOutput = _generationReport.GetOrAdd(t, new GenerationOutput());
            generationOutput.Merge(output);
        }

        private void LinkProject(Project project)
        {
            using (CreateProfilingScope("LinkProject" + project.Name))
            {
                // Pre event
                EventPreProjectLink?.Invoke(project);

                project.Link(this);

                // Post event
                EventPostProjectLink?.Invoke(project);
            }
        }

        private void LinkSolution(Solution solution)
        {
            using (CreateProfilingScope("LinkSolution" + solution.Name))
            {
                // Pre event
                using (CreateProfilingScope("EventPreSolutionLink" + solution.Name))
                    EventPreSolutionLink?.Invoke(solution);

                using (CreateProfilingScope("Solution.Link" + solution.Name))
                    solution.Link(this);

                // Post event
                using (CreateProfilingScope("EventPostSolutionLink" + solution.Name))
                    EventPostSolutionLink?.Invoke(solution);
            }
        }

        public void Link()
        {
            if (_linked)
                return;

            LogWriteLine("  linking projects dependencies...");
            using (new Util.StopwatchProfiler(ms => { LogWriteLine("    link done in {0:0.0} sec", ms / 1000.0f); }))
            {
                // start with huge projects to balance end of tasks
                List<Project> projects = new List<Project>(_projects.Values);
                projects.Sort((Project p0, Project p1) =>
                {
                    int p0Int = p0.Configurations.Count * p0.ResolvedSourceFiles.Count;
                    int p1Int = p1.Configurations.Count * p1.ResolvedSourceFiles.Count;
                    int cmp = p1Int.CompareTo(p0Int);
                    return cmp;
                });

                LinkProjects(projects);

                // start with huge solutions to balance end of tasks
                List<Solution> solutions = new List<Solution>(_solutions.Values);
                solutions.Sort((Solution s0, Solution s1) => { return s1.Configurations.Count.CompareTo(s0.Configurations.Count); });

                LinkSolutions(solutions);

                _linked = true;
            }
        }

        internal void LinkProjects(List<Project> projects)
        {
            if (_multithreaded)
            {
                foreach (Project project in projects)
                    _tasks.AddTask((object arg) => { LinkProject((Project)arg); }, project);
                _tasks.Wait();
            }
            else
            {
                foreach (Project project in projects)
                    LinkProject(project);
            }
        }

        internal void LinkSolutions(List<Solution> solutions)
        {
            if (_multithreaded)
            {
                foreach (Solution solution in solutions)
                    _tasks.AddTask((object arg) => { LinkSolution((Solution)arg); }, solution);
                _tasks.Wait();
            }
            else
            {
                foreach (Solution solution in solutions)
                    LinkSolution(solution);
            }
        }

        private void DetermineUsedProjectConfigurations(List<Project> projects, List<Solution> solutions)
        {
            Trace.Assert(_usedProjectConfigurations == null);
            Trace.Assert(_linked, "This method can only be called *after* the link has occurred");

            // if this becomes too slow, we can move the creation of the list to the tasks per solution, and group them after
            var usedProjectConfigs = new HashSet<Project.Configuration>();
            using (new Util.StopwatchProfiler(ms => { LogWriteLine("    figuring out used project configs took {0:0.0} sec", ms / 1000.0f); }, minThresholdMs: 100))
            {
                // generate the project if it was explicitly queried by the user-code
                var projectExplicitlyQueried = new HashSet<Type>(Arguments.TypesToGenerate.Where(t => t.IsSubclassOf(typeof(Project))));
                foreach (Project p in projects.Where(p => projectExplicitlyQueried.Contains(p.GetType())))
                {
                    foreach (var projectConfig in p.Configurations)
                        usedProjectConfigs.Add(projectConfig);
                }

                foreach (Solution s in solutions)
                {
                    foreach (var pair in s.SolutionFilesMapping)
                    {
                        List<Solution.Configuration> configurations = pair.Value;
                        foreach (var solutionConfig in configurations)
                        {
                            foreach (var includedProject in solutionConfig.IncludedProjectInfos)
                                usedProjectConfigs.Add(includedProject.Configuration);
                        }
                    }
                }
            }

            _usedProjectConfigurations = usedProjectConfigs;
        }

        private class DiagHelper
        {
            public DiagHelper(FieldInfo fieldInfo)
            {
                FieldInfo = fieldInfo;
            }

            private FieldInfo FieldInfo { get; }
            private Dictionary<int, List<Project.Configuration>> Used { get; } = new Dictionary<int, List<Project.Configuration>>();
            private Dictionary<int, List<Project.Configuration>> Unused { get; } = new Dictionary<int, List<Project.Configuration>>();

            public void AddUsedConf(int fragmentValue, Project.Configuration conf)
            {
                if (!Used.ContainsKey(fragmentValue))
                    Used[fragmentValue] = new List<Project.Configuration>() { conf };
                else
                    Used[fragmentValue].Add(conf);
            }

            public void AddUnusedConf(int fragmentValue, Project.Configuration conf)
            {
                if (!Unused.ContainsKey(fragmentValue))
                    Unused[fragmentValue] = new List<Project.Configuration>() { conf };
                else
                    Unused[fragmentValue].Add(conf);
            }

            private string GetFragmentInfoString(int fragmentValue)
            {
                var typedFragment = Enum.ToObject(FieldInfo.FieldType, fragmentValue);
                if (typedFragment is Platform platformFragment)
                {
                    return Util.GetSimplePlatformString(platformFragment);
                }
                return typedFragment.ToString();
            }

            public void LogAllFragmentValuesThatAreAllUnused()
            {
                List<string> fullyUnusedValues = null;
                foreach (var kvp in Unused)
                {
                    var fragmentValue = kvp.Key;
                    if (!Used.ContainsKey(fragmentValue))
                    {
                        if (fullyUnusedValues == null)
                            fullyUnusedValues = new List<string>();

                        fullyUnusedValues.Add($"'{GetFragmentInfoString(fragmentValue)}' ({kvp.Value.Count})");
                    }
                }

                if (fullyUnusedValues != null)
                {
                    Instance.LogWarningLine("Unused values for fragment type '{0}': {1}", FieldInfo.Name, string.Join("|", fullyUnusedValues));
                    var composedString = Used.Select(kvp => $"'{GetFragmentInfoString(kvp.Key)}' ({kvp.Value.Count})").ToList();
                    Instance.LogWarningLine("Used values for fragment type '{0}': {1}", FieldInfo.Name, string.Join("|", composedString));
                }
            }
        }

        private static bool s_logUsedConfigurations = false;
        private void LogUnusedProjectConfigurations(List<Project> projects, List<Solution> solutions)
        {
            Trace.Assert(_usedProjectConfigurations != null);
            foreach (Project p in projects)
            {
                if (p.SharpmakeProjectType == Project.ProjectTypeAttribute.Export)
                    continue;

                var dic = new Dictionary<FieldInfo, DiagHelper>();

                List<ITarget> uselessTargets = null;

                var debugList = new List<string>();
                bool foundUnusedConfInProject = false;

                foreach (var conf in p.Configurations)
                {
                    bool confWasUsed = _usedProjectConfigurations.Contains(conf);

                    var target = conf.Target;
                    if (!confWasUsed)
                    {
                        foundUnusedConfInProject = true;

                        debugList.ForEach(d => LogWriteLine(d));

                        LogWriteLine(conf.Project.SharpmakeCsFileName + ":          Config not used during generation: " + conf.Owner.GetType().ToNiceTypeName() + ":" + conf.Target);
                        if (uselessTargets == null)
                            uselessTargets = new List<ITarget>(p.Configurations.Count);
                        uselessTargets.Add(target);
                    }
                    else if (s_logUsedConfigurations)
                    {
                        if (!foundUnusedConfInProject)
                            debugList.Add(conf.Project.SharpmakeCsFileName + ":          Config WAS used during generation: " + conf.Owner.GetType().ToNiceTypeName() + ":" + conf.Target);
                        else
                            LogWriteLine(conf.Project.SharpmakeCsFileName + ":          Config WAS used during generation: " + conf.Owner.GetType().ToNiceTypeName() + ":" + conf.Target);
                    }

                    FieldInfo[] fragmentFields = target.GetFragmentFieldInfo();

                    int[] fragmentValues = new int[fragmentFields.Length];

                    for (int i = 0; i < fragmentValues.Length; ++i)
                    {
                        FieldInfo fieldInfo = fragmentFields[i];
                        int fragmentValue = (int)fieldInfo.GetValue(target);

                        if (!dic.ContainsKey(fieldInfo))
                            dic[fieldInfo] = new DiagHelper(fieldInfo);

                        if (confWasUsed)
                            dic[fieldInfo].AddUsedConf(fragmentValue, conf);
                        else
                            dic[fieldInfo].AddUnusedConf(fragmentValue, conf);
                    }
                }

                if (uselessTargets != null && uselessTargets.Count > 1)
                {
                    foreach (var tuple in dic)
                    {
                        var fieldInfo = tuple.Key;
                        var diagHelper = tuple.Value;

                        diagHelper.LogAllFragmentValuesThatAreAllUnused();
                    }

                    List<ITarget> mergeCandidateTargets = uselessTargets;
                    List<ITarget> doneTargets = new List<ITarget>(uselessTargets.Count);
                    int currentDepth = 0;
                    while (mergeCandidateTargets.Count > 0)
                    {
                        ITarget previousTarget = null;
                        int[] previousFragmentValues = null;
                        int previousDiffIndex = -1;
                        List<ITarget> nextMergeCandidateTargets = new List<ITarget>(mergeCandidateTargets.Count);
                        foreach (var target in mergeCandidateTargets)
                        {
                            int[] fragmentValues = target.GetFragmentsValue();
                            if (previousTarget == null)
                            {
                                previousTarget = target;
                                previousFragmentValues = fragmentValues;
                                continue;
                            }
                            int firstDiffIndex = -1;
                            for (int i = 0; i < fragmentValues.Length; ++i)
                            {
                                if (fragmentValues[i] != previousFragmentValues[i])
                                {
                                    if (firstDiffIndex == -1)
                                        firstDiffIndex = i;
                                    else
                                    {
                                        firstDiffIndex = -1;
                                        break;  // can't accept more than one difference
                                    }
                                }
                            }
                            if (firstDiffIndex != -1)
                            {
                                ITarget mergedTarget = target.Clone();
                                previousFragmentValues[firstDiffIndex] = fragmentValues[firstDiffIndex] | previousFragmentValues[firstDiffIndex];
                                mergedTarget.GetFragmentFieldInfo()[firstDiffIndex].SetValue(mergedTarget, previousFragmentValues[firstDiffIndex]);
                                previousTarget = mergedTarget;
                                previousDiffIndex = firstDiffIndex;
                                continue;
                            }
                            if (previousDiffIndex != -1)
                            {
                                nextMergeCandidateTargets.Add(previousTarget);
                                previousDiffIndex = -1;
                            }
                            else
                            {
                                doneTargets.Add(previousTarget);
                            }
                            previousTarget = target;
                            previousFragmentValues = fragmentValues;
                        }
                        if (previousDiffIndex != -1)
                        {
                            nextMergeCandidateTargets.Add(previousTarget);
                        }
                        else
                        {
                            doneTargets.Add(previousTarget);
                        }
                        mergeCandidateTargets = nextMergeCandidateTargets;
                        ++currentDepth;
                    }

                    if (doneTargets.Count < uselessTargets.Count)
                    {
                        LogWarningLine("Previously mentioned useless Configs in compact format:");
                        foreach (ITarget target in doneTargets)
                        {
                            LogWarningLine("Target: " + target.ToString().Replace(", ", "|"));
                        }
                    }
                }
            }
        }

        public IDictionary<Type, GenerationOutput> Generate()
        {
            using (CreateProfilingScope("Link"))
                Link();

            if (Context.WriteLog)
                WriteLogs();

            LogWriteLine("  generating projects and solutions...");
            using (new Util.StopwatchProfiler(ms => { LogWriteLine("    generation done in {0:0.0} sec", ms / 1000.0f); }))
            using (CreateProfilingScope("Generation"))
            {
                _getGeneratorsManagerCallBack().BeforeGenerate();

                var projects = new List<Project>(_projects.Values);
                var solutions = new List<Solution>(_solutions.Values);

                // Append generated projects, if any
                projects.AddRange(_generatedProjects);

                // Pre event
                if (EventPreGeneration != null)
                {
                    using (new Util.StopwatchProfiler(ms => { LogWriteLine("    pre-generation steps took {0:0.0} sec", ms / 1000.0f); }, minThresholdMs: 100))
                    using (CreateProfilingScope("PreGen Steps"))
                        EventPreGeneration.Invoke(projects, solutions);
                }

                // start with huge solutions to balance task with small one at the end.
                solutions.Sort((s0, s1) => s1.Configurations.Count.CompareTo(s0.Configurations.Count));
                foreach (Solution solution in solutions)
                    Generate(solution);

                DetermineUsedProjectConfigurations(projects, solutions);

                // start with huge projects to balance task with small one at the end.
                projects.Sort((p0, p1) => p1.ProjectFilesMapping.Count.CompareTo(p0.ProjectFilesMapping.Count));
                foreach (Project project in projects)
                    Generate(project);

                if (_multithreaded)
                    _tasks.Wait();

                // Post events
                if (EventPostGeneration != null || EventPostGenerationReport != null)
                {
                    using (new Util.StopwatchProfiler(ms => { LogWriteLine("    post-generation steps took {0:0.0} sec", ms / 1000.0f); }, minThresholdMs: 100))
                    using (CreateProfilingScope("PostGen Steps"))
                    {
                        EventPostGeneration?.Invoke(projects, solutions);
                        EventPostGenerationReport?.Invoke(projects, solutions, _generationReport);
                    }
                }

                return _generationReport;
            }
        }

        private void GenerateSolutionFile(object arg)
        {
            var pair = (KeyValuePair<string, List<Solution.Configuration>>)arg;
            string solutionFile = pair.Key;
            List<Solution.Configuration> configurations = pair.Value;
            Solution.Configuration firstConf = configurations.FirstOrDefault();
            Solution solution = firstConf.Solution;

            using (CreateProfilingScope("GenSolution" + Path.GetFileName(solutionFile)))
            {
                GenerationOutput output = new GenerationOutput();

                try
                {
                    DevEnv devEnv = configurations[0].Target.GetFragment<DevEnv>();
                    for (int i = 0; i < configurations.Count; ++i)
                    {
                        Solution.Configuration conf = pair.Value[i];
                        if (devEnv != conf.Target.GetFragment<DevEnv>())
                            throw new Error("Multiple generator cannot output to the same file:" + Environment.NewLine + "\t'{0}' and '{1}' try to generate '{2}'",
                                devEnv,
                                conf.Target.GetFragment<DevEnv>(),
                                solutionFile);
                    }

                    _getGeneratorsManagerCallBack().Generate(this, solution, configurations, solutionFile, output.Generated, output.Skipped);
                }
                catch (Exception ex)
                {
                    output.Exception = ex;
                }

                GenerationOutput allOutput = _generationReport.GetOrAdd(solution.GetType(), output);
                if (allOutput != output)
                {
                    lock (allOutput)
                        allOutput.Merge(output);
                }
            }
        }

        private void Generate(Solution solution)
        {
            foreach (KeyValuePair<string, List<Solution.Configuration>> pair in solution.SolutionFilesMapping)
            {
                if (_multithreaded)
                    _tasks.AddTask(GenerateSolutionFile, pair);
                else
                    GenerateSolutionFile(pair);
            }
        }

        private void GenerateProjectFile(object arg)
        {
            var pair = (KeyValuePair<string, List<Project.Configuration>>)arg;

            string projectFile = pair.Key;
            List<Project.Configuration> configurations = pair.Value;
            Project.Configuration firstConf = configurations.FirstOrDefault();
            Project project = firstConf.Project;

            using (CreateProfilingScope("GenProject" + Path.GetFileName(projectFile)))
            {
                GenerationOutput output = new GenerationOutput();

                try
                {
                    bool generateProject = false;

                    DevEnv devEnv = configurations[0].Target.GetFragment<DevEnv>();
                    using (CreateProfilingScope("GenProject" + Path.GetFileName(projectFile) + ":confs"))
                    {
                        for (int i = 0; i < configurations.Count; ++i)
                        {
                            Project.Configuration conf = pair.Value[i];
                            if (devEnv != conf.Target.GetFragment<DevEnv>())
                            {
                                throw new Error("Multiple generator cannot output to the same file:" + Environment.NewLine + "\tBoth {0} and {1} try to generate {2}",
                                    devEnv,
                                    conf.Target.GetFragment<DevEnv>(),
                                    projectFile);
                            }

                            if (!generateProject)
                            {
                                if (_usedProjectConfigurations == null ||
                                    Arguments.TypesToGenerate.Contains(project.GetType()) || // generate the project if it was explicitly queried by the user-code
                                    _usedProjectConfigurations.Contains(conf))
                                {
                                    generateProject = true;
                                }
                            }
                        }
                    }

                    if (project.SourceFilesFilters == null || (project.SourceFilesFiltersCount != 0 && !project.SkipProjectWhenFiltersActive))
                    {
                        if (generateProject)
                        {
                            using (CreateProfilingScope("GenProject" + Path.GetFileName(projectFile) + ":Generate"))
                                _getGeneratorsManagerCallBack().Generate(this, project, configurations, projectFile, output.Generated, output.Skipped);
                        }
                    }
                }
                catch (Exception ex)
                {
                    output.Exception = ex;
                }

                GenerationOutput allOutput = _generationReport.GetOrAdd(project.GetType(), output);
                if (allOutput != output)
                {
                    lock (allOutput)
                        allOutput.Merge(output);
                }
            }
        }

        private void Generate(Project project)
        {
            if (project.SharpmakeProjectType != Project.ProjectTypeAttribute.Generate)
                return;

            foreach (KeyValuePair<string, List<Project.Configuration>> pair in project.ProjectFilesMapping)
            {
                if (_multithreaded)
                    _tasks.AddTask(GenerateProjectFile, pair);
                else
                    GenerateProjectFile(pair);
            }
        }

        public void AddAttributeParser(ISourceAttributeParser parser)
        {
            _attributeParsers.Add(parser);
        }

        #region IBuilderContext
        private class LoadInfo : ILoadInfo
        {
            public IAssemblyInfo AssemblyInfo { get; }
            public Assembly Assembly { get; }
            public IEnumerable<ISourceAttributeParser> Parsers { get; }

            public LoadInfo(IAssemblyInfo assemblyInfo)
                : this(assemblyInfo, assemblyInfo.Assembly, null)
            { }
            public LoadInfo(IAssemblyInfo assemblyInfo, IEnumerable<ISourceAttributeParser> parsers)
                : this(assemblyInfo, assemblyInfo.Assembly, parsers)
            { }
            public LoadInfo(Assembly assembly)
                : this(null, assembly, null)
            { }
            public LoadInfo(Assembly assembly, IEnumerable<ISourceAttributeParser> parsers)
                : this(null, assembly, parsers)
            { }

            private LoadInfo(IAssemblyInfo assemblyInfo, Assembly assembly, IEnumerable<ISourceAttributeParser> parsers)
            {
                AssemblyInfo = assemblyInfo;
                Assembly = assembly;
                Parsers = parsers?.ToArray() ?? Enumerable.Empty<ISourceAttributeParser>();
            }
        }

        private class BuilderContext : IBuilderContext
        {
            private readonly Builder _builder;

            public BuilderCompileErrorBehavior CompileErrorBehavior { get; }

            public bool DebugScripts { get; }

            public BuilderContext(Builder builder, BuilderCompileErrorBehavior compileErrorBehavior)
            {
                _builder = builder;
                CompileErrorBehavior = compileErrorBehavior;
                DebugScripts = builder._debugScripts;
            }

            public ILoadInfo BuildAndLoadSharpmakeFiles(IEnumerable<ISourceAttributeParser> parsers, IEnumerable<IParsingFlowParser> flowParsers, params string[] files)
            {
                var parserCount = _builder._attributeParsers.Count;
                var assemblyInfo = _builder.BuildAndLoadAssembly(files, this, parsers, flowParsers);
                if (assemblyInfo.Assembly != null)
                    _builder.ExecuteEntryPointInAssemblies<EntryPoint>(assemblyInfo.Assembly);

                return new LoadInfo(assemblyInfo, _builder._attributeParsers.Skip(parserCount));
            }

            public ILoadInfo LoadExtension(string file)
            {
                // Load extensions if they were passed as references (platforms,
                // entry point execution to add new ISourceAttributeParser...)
                using (var extensionLoader = new ExtensionLoader())
                {
                    var parserCount = _builder._attributeParsers.Count;
                    var assembly = extensionLoader.LoadExtension(file);
                    return new LoadInfo(assembly, _builder._attributeParsers.Skip(parserCount));
                }
            }

            public void AddDefine(string define)
            {
                _builder.AddDefine(define);
            }
        }

        public IBuilderContext CreateContext(BuilderCompileErrorBehavior compileErrorBehavior = BuilderCompileErrorBehavior.ThrowException)
        {
            return new BuilderContext(this, compileErrorBehavior);
        }
        #endregion

        #region Log

        public void LogWriteLine(string message, params object[] args)
        {
            EventOutputMessage?.Invoke(message + Environment.NewLine, args);
        }

        public void LogErrorLine(string message, params object[] args)
        {
            EventOutputError?.Invoke(message + Environment.NewLine, args);
        }

        public void LogWarningLine(string message, params object[] args)
        {
            EventOutputWarning?.Invoke(message + Environment.NewLine, args);
        }

        public void DebugWriteLine(string message, params object[] args)
        {
            EventOutputDebug?.Invoke(message + Environment.NewLine, args);
        }

        #endregion

        #region Private

        internal Dictionary<Type, Project> _projects = new Dictionary<Type, Project>();
        internal Dictionary<Type, Solution> _solutions = new Dictionary<Type, Solution>();

        private List<Project> _generatedProjects = new List<Project>();

        private bool _linked = false;
        private readonly Func<IGeneratorManager> _getGeneratorsManagerCallBack;

        #endregion
    }
}
