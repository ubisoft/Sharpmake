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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Sharpmake.UnitTests")]

namespace Sharpmake
{
    public class Arguments
    {
        internal List<object> FragmentMasks = new List<object>();

        public Builder Builder { get; }

        public ConfigureOrder ConfigureOrder;

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
        public event OutputDelegate EventOutputProfile;

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
        private ThreadPool _tasks;
        private Assembly _projectAssembly;  // keep the instance of manually loaded assembly, it's may be need by other assembly on load ( command line )
        private Dictionary<string, string> _referenceList; // Keep track of assemblies explicitly referenced with [module: Sharpmake.Reference("...")] in compiled files

        public BuildContext.BaseBuildContext Context { get; private set; }

        private readonly BuilderExtension _builderExt;

        private ConcurrentDictionary<Type, GenerationOutput> _generationReport = new ConcurrentDictionary<Type, GenerationOutput>();
        private HashSet<Type> _buildScheduledType = new HashSet<Type>();

        public Builder(
            BuildContext.BaseBuildContext context,
            bool multithreaded,
            bool dumpDependencyGraph,
            bool cleanBlobsOnly,
            bool blobOnly,
            bool skipInvalidPath,
            bool diagnostics,
            Func<IGeneratorManager> getGeneratorsManagerCallBack)
        {
            Context = context;
            Arguments = new Arguments(this);
            _multithreaded = multithreaded;
            DumpDependencyGraph = dumpDependencyGraph;
            _cleanBlobsOnly = cleanBlobsOnly;
            BlobOnly = blobOnly;
            Diagnostics = diagnostics;
            SkipInvalidPath = skipInvalidPath;
            _getGeneratorsManagerCallBack = getGeneratorsManagerCallBack;
            _getGeneratorsManagerCallBack().InitializeBuilder(this);
            Trace.Assert(Instance == null);
            Instance = this;
            _builderExt = new BuilderExtension(this);

            if (_multithreaded)
            {
                _tasks = new ThreadPool();
                int nbThreads = Environment.ProcessorCount;
                _tasks.Start(nbThreads);
            }
        }

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

        public void LoadAssemblies(params Assembly[] assemblies)
        {
            List<MethodInfo> mainMethods = new List<MethodInfo>();

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    MethodInfo mainMethodInfo = type.GetMethod("SharpmakeMain");
                    if (mainMethodInfo != null && mainMethodInfo.IsDefined(typeof(Main), false))
                    {
                        if (!mainMethodInfo.IsStatic)
                            throw new Error("SharpmakeMain method should be static {0}", mainMethodInfo.ToString());

                        if (mainMethodInfo.GetParameters().Length != 1 ||
                            mainMethodInfo.GetParameters()[0].GetType() == typeof(Arguments))
                            throw new Error("SharpmakeMain method should have one parameters of type Sharpmake.Builder: {0} in {1}", mainMethodInfo.ToString(), type.FullName);

                        mainMethods.Add(mainMethodInfo);
                    }
                }
            }

            if (mainMethods.Count != 1)
                throw new Error("sharpmake must contain one and only one static entry point method called Main(Sharpmake.Builder) with [Sharpmake.Main] attribute. Make sure it's public.");

            try
            {
                mainMethods[0].Invoke(null, new object[] { Arguments });
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException != null)
                    throw (e.InnerException);
            }

            if (Arguments.TypesToGenerate.Count == 0)
                throw new Error("sharpmake have nothing to generate! Make sure to add builder.Generate<[your_class]>(); in '{0}'.", mainMethods[0].ToString());

            Context.ConfigureOrder = Arguments.ConfigureOrder;

            BuildProjectAndSolution();
        }

        public void BuildProjectAndSolution()
        {
            LogWriteLine("  building projects and solutions configurations{0}...", _multithreaded ? $" using {_tasks.NumTasks()} tasks" : " single-threaded");
            using (new Util.StopwatchProfiler(ms => { LogWriteLine("    build done in {0:0.0} sec", ms / 1000.0f); }))
            {
                if (!_multithreaded)
                {
                    for (int i = 0; i < Arguments.TypesToGenerate.Count; ++i)
                    {
                        Type type = Arguments.TypesToGenerate[i];

                        HashSet<Type> projectDependenciesTypes;
                        if (type.IsSubclassOf(typeof(Project)))
                        {
                            Project project = LoadProjectType(type);
                            // Add the project to the instances projects.
                            _projects.Add(type, project);
                            projectDependenciesTypes = project.GetUnresolvedDependenciesTypes();
                        }
                        else if (type.IsSubclassOf(typeof(Solution)))
                        {
                            Solution solution = LoadSolutionType(type);
                            // Add the project to the instances projects.
                            _solutions.Add(type, solution);
                            projectDependenciesTypes = solution.GetDependenciesProjectTypes();
                        }
                        else
                        {
                            throw new Error("error, class type note supported: {0}", type.FullName);
                        }

                        foreach (Type projectDependenciesType in projectDependenciesTypes)
                        {
                            if (!Arguments.TypesToGenerate.Contains(projectDependenciesType))
                                Arguments.TypesToGenerate.Add(projectDependenciesType);
                        }
                    }
                }
                else
                {
                    _buildScheduledType.UnionWith(Arguments.TypesToGenerate);

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
                throw new Error("error, class type note supported: {0}", type.FullName);
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

        public void LoadAssemblies(params string[] assembliesFiles)
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
                            extensionLoader.LoadExtension(candidatePath, false);
                    }
                }
            }

            LoadAssemblies(assemblies);
        }

        // Expect a list of existing files with their full path
        public void LoadSharpmakeFiles(params string[] sharpmakeFiles)
        {
            Assembler assembler = new Assembler();

            // Add sharpmake assembly
            Assembly sharpmake = Assembly.GetAssembly(typeof(Builder));
            assembler.Assemblies.Add(sharpmake);

            // Add generators assembly to be able to reference them from .sharpmake files.
            DirectoryInfo entryDirectoryInfo = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            string generatorsAssembly = entryDirectoryInfo.FullName + Path.DirectorySeparatorChar + "Sharpmake.Generators.dll";
            Assembly generators = Assembly.LoadFrom(generatorsAssembly);
            assembler.Assemblies.Add(generators);

            _projectAssembly = assembler.BuildAssembly(sharpmakeFiles);

            if (_projectAssembly == null)
                throw new InternalError();

            // Keep track of assemblies explicitly referenced by compiled files
            _referenceList = assembler.References.Distinct().ToDictionary(fullpath => AssemblyName.GetAssemblyName(fullpath).FullName.ToString(), fullpath => fullpath);

            // load platforms if they were passed as references
            using (var extensionLoader = new ExtensionLoader())
            {
                foreach (var referencePath in assembler.References)
                {
                    extensionLoader.LoadExtension(referencePath, false);
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            LoadAssemblies(_projectAssembly);
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Check if this is the built assembly version of .sharpmake files that is requested to be loaded
            if (_projectAssembly != null && _projectAssembly.FullName == args.Name)
                return _projectAssembly;

            // Check if this is an assembly that if referenced by [module: Sharpmake.Reference("...")], is so, explicitly load it with its fullPath
            string explicitReferencesFullPath;
            if (_referenceList.TryGetValue(args.Name, out explicitReferencesFullPath))
                return Assembly.LoadFrom(explicitReferencesFullPath);

            return null;
        }

        public Project LoadProjectType(Type type)
        {
            using (new Util.StopwatchProfiler(ms => { ProfileWriteLine("    |{0,5} ms| load project {1}", ms, type.Name); }))
            {
                if (!type.IsDefined(typeof(Generate), false) &&
                !type.IsDefined(typeof(Compile), false) &&
                !type.IsDefined(typeof(Export), false))
                    throw new Error("cannot generate project type without [Sharpmake.Generate], [Sharpmake.Compile] or [Sharpmake.Export] attribute: {0}", type.Name);

                // Create the project instance
                Project project = Project.CreateProject(type, Arguments.FragmentMasks);

                // Pre event
                EventPreProjectConfigure?.Invoke(project);

                project.PreConfigure();

                // Create and Configure all possibles configurations.
                project.InvokeConfiguration(Context);

                project.AfterConfigure();

                // Post event
                if (EventPostProjectConfigure != null)
                {
                    foreach (Project.Configuration conf in project.Configurations)
                        EventPostProjectConfigure?.Invoke(project, conf);
                }

                // Resolve [*]
                project.Resolve(this, SkipInvalidPath);

                // Would be more optimal to not generate the blobs, but simpler that way
                if (_cleanBlobsOnly)
                    project.CleanBlobs();

                return project;
            }
        }

        public Solution LoadSolutionType(Type type)
        {
            using (new Util.StopwatchProfiler(ms => { ProfileWriteLine("    |{0,5} ms| load solution {1}", ms, type.Name); }))
            {
                if (!type.IsDefined(typeof(Generate), false) &&
                !type.IsDefined(typeof(Compile), false) &&
                !type.IsDefined(typeof(Export), false))
                    throw new Error("cannot generate solution type without [Sharpmake.Generate], [Sharpmake.Compile] or [Sharpmake.Export] attribute: {0}", type.Name);

                // Create the project instance
                Solution solution = Solution.CreateProject(type, Arguments.FragmentMasks);

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
            var generationOutput = _generationReport.GetValueOrAdd(t, new GenerationOutput());
            generationOutput.Merge(output);
        }

        private void LinkProject(Project project)
        {
            using (new Util.StopwatchProfiler(ms => { ProfileWriteLine("    |{0,5} ms| link project {1}", ms, project.Name); }))
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
            using (new Util.StopwatchProfiler(ms => { ProfileWriteLine("    |{0,5} ms| link solution {1}", ms, solution.Name); }))
            {
                // Pre event
                EventPreSolutionLink?.Invoke(solution);

                solution.Link(this);

                // Post event
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
                projects.Sort((Project p0, Project p1) => { return p1.Configurations.Count.CompareTo(p0.Configurations.Count); });

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

        internal class ProjectGenerationTask
        {
            internal Project _project;
            internal Builder _builder;

            internal ProjectGenerationTask(Builder builder, Project project)
            {
                _builder = builder;
                _project = project;
            }

            internal void Generate(object parameter)
            {
                _builder.Generate(_project);
            }
        }

        internal class SolutionGenerationTask
        {
            internal Solution _solution;
            internal Builder _builder;

            internal SolutionGenerationTask(Builder builder, Solution solution)
            {
                _builder = builder;
                _solution = solution;
            }

            internal void Generate(object parameter)
            {
                _builder.Generate(_solution);
            }
        }

        public IDictionary<Type, GenerationOutput> Generate()
        {
            Link();

            if (Context.WriteLog)
                WriteLogs();

            LogWriteLine("  generating projects and solutions...");
            using (new Util.StopwatchProfiler(ms => { LogWriteLine("    generation done done in {0:0.0} sec", ms / 1000.0f); }))
            {
                var projects = new List<Project>(_projects.Values);
                var solutions = new List<Solution>(_solutions.Values);

                // Append generated projects, if any
                projects.AddRange(_generatedProjects);

                // Pre event
                EventPreGeneration?.Invoke(projects, solutions);

                // start with huge solutions to balance task with small one at the end.
                solutions.Sort((s0, s1) => s1.Configurations.Count.CompareTo(s0.Configurations.Count));
                foreach (Solution solution in solutions)
                    Generate(solution);

                // start with huge projects to balance task with small one at the end.
                projects.Sort((p0, p1) => p1.ProjectFilesMapping.Count.CompareTo(p0.ProjectFilesMapping.Count));
                foreach (Project project in projects)
                    Generate(project);

                if (_multithreaded)
                    _tasks.Wait();

                // Post events
                EventPostGeneration?.Invoke(projects, solutions);
                EventPostGenerationReport?.Invoke(projects, solutions, _generationReport);

                return _generationReport;
            }
        }

        private void GenerateSolutionFile(object arg)
        {
            KeyValuePair<string, List<Solution.Configuration>> pair = (KeyValuePair<string, List<Solution.Configuration>>)arg;
            string solutionFile = pair.Key;
            List<Solution.Configuration> configurations = pair.Value;
            Solution.Configuration firstConf = configurations.FirstOrDefault();
            Solution solution = firstConf.Solution;

            using (new Util.StopwatchProfiler(ms => { ProfileWriteLine("    |{0,5} ms| generate solution file {1}", ms, firstConf.SolutionFileName); }))
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
            KeyValuePair<string, List<Project.Configuration>> pair = (KeyValuePair<string, List<Project.Configuration>>)arg;

            string projectFile = pair.Key;
            List<Project.Configuration> configurations = pair.Value;
            Project.Configuration firstConf = configurations.FirstOrDefault();
            Project project = firstConf.Project;

            using (new Util.StopwatchProfiler(ms => { ProfileWriteLine("    |{0,5} ms| generate project file {1}", ms, firstConf.ProjectFileName); }))
            {
                GenerationOutput output = new GenerationOutput();

                try
                {
                    DevEnv devEnv = configurations[0].Target.GetFragment<DevEnv>();
                    for (int i = 0; i < configurations.Count; ++i)
                    {
                        Project.Configuration conf = pair.Value[i];
                        if (devEnv != conf.Target.GetFragment<DevEnv>())
                            throw new Error("Multiple generator cannot output to the same file:" + Environment.NewLine + "\tBoth {0} and {1} try to generate {2}",
                                devEnv,
                                conf.Target.GetFragment<DevEnv>(),
                                projectFile);
                    }

                    if (project.SourceFilesFilters == null || (project.SourceFilesFiltersCount != 0 && !project.SkipProjectWhenFiltersActive))
                    {
                        _getGeneratorsManagerCallBack().Generate(this, project, configurations, projectFile, output.Generated, output.Skipped);
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
            if (!project.GetType().IsDefined(typeof(Generate), false))
                return;

            foreach (KeyValuePair<string, List<Project.Configuration>> pair in project.ProjectFilesMapping)
            {
                if (_multithreaded)
                    _tasks.AddTask(GenerateProjectFile, pair);
                else
                    GenerateProjectFile(pair);
            }
        }

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

        public void ProfileWriteLine(string message, params object[] args)
        {
            EventOutputProfile?.Invoke(message + Environment.NewLine, args);
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
