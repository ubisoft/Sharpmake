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
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Sharpmake
{
    [Resolver.Resolvable]
    public partial class Solution : Configurable<Solution.Configuration>
    {
        private string _name = "[solution.ClassName]";                       // Solution Name
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private bool _isFileNameToLower = true;
        public bool IsFileNameToLower
        {
            get { return _isFileNameToLower; }
            set { SetProperty(ref _isFileNameToLower, value); }
        }

        public string ClassName { get; private set; }                       // Solution Class Name, ex: "MySolution"
        public string SharpmakeCsFileName { get; private set; }             // File name of the c# project configuration, ex: "MyProject.cs"
        public string SharpmakeCsPath { get; private set; }                 // Path of the CsFileName, ex: "c:\dev\MyProject"

        // FastBuild specific

        /// <summary>
        /// Allow to multi-thread the build of the executables when doing a Build Solution (Ctrl+Shift+B).
        /// However, it will not be possible to build each executable individually.
        /// </summary>
        public bool GenerateFastBuildAllProject = true;

        public string FastBuildAllProjectName = "[solution.Name]_All";
        public string FastBuildAllProjectFileSuffix = "_All"; // the fastbuild all project will be named after the solution, but the suffix can be custom. Warning: this cannot be empty!

        public string FastBuildAllSolutionFolder = "FastBuild"; // set to null to add to the root
        public string FastBuildMasterBffSolutionFolder = "FastBuild"; // Warning: this one cannot be null, VS doesn't accept floating files at the root of the solution!

        // Experimental! Create solution dependencies from the FastBuild projects outputting Exe to the FastBuildAll project, to fix "F5" behavior in visual studio http://www.fastbuild.org/docs/functions/vssolution.html
        public bool FastBuildAllSlnDependencyFromExe = false;

        /// <summary>
        /// In case we've generated a "FastBuildAll" project, this flag will determine if we generate it for all
        /// the configurations, or only the ones that need it
        /// </summary>
        public bool GenerateFastBuildAllOnlyForConfThatNeedIt = true;

        /// <summary>
        /// For adding additional files/folders to the solution
        /// Keys are names of the directories in the virtual solution hierarchy, values are paths
        /// </summary>
        public Dictionary<string, Strings> ExtraItems = new Dictionary<string, Strings>();

        private string _perforceRootPath = null;
        public string PerforceRootPath
        {
            get { return _perforceRootPath; }
            set { SetProperty(ref _perforceRootPath, value); }
        }

        private bool _mergePlatformConfiguration = false;
        public bool MergePlatformConfiguration
        {
            get { return _mergePlatformConfiguration; }
            set { SetProperty(ref _mergePlatformConfiguration, value); }
        }

        public delegate void GenerationCallback(string solutionPath, string solutionFile, string solutionExtension);

        private GenerationCallback _postGenerationCallback = null;
        public GenerationCallback PostGenerationCallback
        {
            get { return _postGenerationCallback; }
            set { SetProperty(ref _postGenerationCallback, value); }
        }

        public Solution(Type targetType = null, Type configurationType = null)
        {
            Initialize(targetType ?? typeof(Target), configurationType ?? typeof(Solution.Configuration));
        }

        #region Internal

        [DebuggerDisplay("{ProjectName}")]
        public class ResolvedProject
        {
            // Associated project
            public Project Project;

            public string ProjectName;

            // The solution folder to use
            public string SolutionFolder;

            // The solution folder, as reported by the solution. When set, this overrides the folder provided by the project config.
            public string SolutionFolderOverride;

            public List<Project.Configuration> Configurations = new List<Project.Configuration>();

            // Default target use when a project is excluded from build, some generator need to specify a 'dummy' target
            public ITarget TargetDefault;

            public string OriginalProjectFile;

            // Project file name
            public string ProjectFile;

            // Resolved Project dependencies
            public List<ResolvedProject> Dependencies = new List<ResolvedProject>();

            // User data, may be use by generator to attach user data
            public Dictionary<string, Object> UserData = new Dictionary<string, Object>();
        }

        public Dictionary<string, List<Solution.Configuration>> SolutionFilesMapping { get; } = new Dictionary<string, List<Configuration>>();
        internal List<Project> ProjectsDependingOnFastBuildAllForThisSolution { get; } = new List<Project>();

        internal class ResolvedProjectGuidComparer : IEqualityComparer<ResolvedProject>
        {
            public bool Equals(ResolvedProject p, ResolvedProject q)
            {
                return p.UserData["Guid"] == q.UserData["Guid"];
            }

            public int GetHashCode(ResolvedProject obj)
            {
                return obj.ProjectFile.GetHashCode();
            }
        }

        internal static Solution CreateProject(Type solutionType, List<Object> fragmentMasks)
        {
            Solution solution;
            try
            {
                solution = Activator.CreateInstance(solutionType) as Solution;
            }
            catch (Exception e)
            {
                throw new Error(e, "Cannot create instances of type: {0}, caught exception message {1}. Make sure default ctor is public", solutionType.Name, e.Message);
            }

            solution.Targets.SetGlobalFragmentMask(fragmentMasks.ToArray());
            solution.Targets.BuildTargets();
            return solution;
        }

        public IEnumerable<ResolvedProject> GetResolvedProjects(IEnumerable<Configuration> solutionConfigurations, out bool projectsWereFiltered)
        {
            if (!_dependenciesResolved)
                throw new InternalError("Solution not resolved: {0}", GetType().FullName);
            projectsWereFiltered = false;
            var result = new Dictionary<string, ResolvedProject>();

            Dictionary<Project.Configuration, ResolvedProject> configurationsToProjects = new Dictionary<Project.Configuration, ResolvedProject>();

            foreach (Configuration solutionConfiguration in solutionConfigurations)
            {
                foreach (Configuration.IncludedProjectInfo includedProjectInfo in solutionConfiguration.IncludedProjectInfos)
                {
                    if (solutionConfiguration.IncludeOnlyFilterProject && !(includedProjectInfo.Project.IsFastBuildAll) && (includedProjectInfo.Project.SourceFilesFiltersCount == 0 || includedProjectInfo.Project.SkipProjectWhenFiltersActive))
                    {
                        projectsWereFiltered = true;
                        continue;
                    }

                    ResolvedProject resolvedProject = result.GetValueOrAdd(
                        includedProjectInfo.Configuration.ProjectFullFileName,
                        new ResolvedProject
                        {
                            Project = includedProjectInfo.Project,
                            TargetDefault = includedProjectInfo.Target,
                            OriginalProjectFile = includedProjectInfo.Configuration.ProjectFullFileName,
                            ProjectFile = Util.GetCapitalizedPath(includedProjectInfo.Configuration.ProjectFullFileNameWithExtension),
                            ProjectName = includedProjectInfo.Configuration.ProjectName,
                            SolutionFolder = includedProjectInfo.SolutionFolder,
                            SolutionFolderOverride = includedProjectInfo.SolutionFolder
                        });

                    resolvedProject.Configurations.Add(includedProjectInfo.Configuration);

                    if (!configurationsToProjects.ContainsKey(includedProjectInfo.Configuration))
                        configurationsToProjects[includedProjectInfo.Configuration] = resolvedProject;
                }
            }


            foreach (ResolvedProject resolvedProject in result.Values)
            {
                foreach (Project.Configuration resolvedProjectConf in resolvedProject.Configurations)
                {
                    // If the solution provides the folder, the configuration should be ignored
                    if (string.IsNullOrEmpty(resolvedProject.SolutionFolderOverride))
                    {
                        // Folder must all be the same for all config, else will be emptied.
                        if (string.IsNullOrEmpty(resolvedProject.SolutionFolder) &&
                            !string.IsNullOrEmpty(resolvedProjectConf.SolutionFolder))
                        {
                            resolvedProject.SolutionFolder = resolvedProjectConf.SolutionFolder;
                        }
                        else if (resolvedProject.SolutionFolder != resolvedProjectConf.SolutionFolder)
                        {
                            resolvedProject.SolutionFolder = "";
                        }
                    }

                    foreach (Project.Configuration dependencyConfiguration in resolvedProjectConf.ResolvedDependencies)
                    {
                        if (configurationsToProjects.ContainsKey(dependencyConfiguration))
                        {
                            var resolvedProjectToAdd = configurationsToProjects[dependencyConfiguration];

                            if (!resolvedProject.Dependencies.Contains(resolvedProjectToAdd))
                                resolvedProject.Dependencies.Add(resolvedProjectToAdd);
                        }
                    }
                }
            }

            return result.Values;
        }

        internal HashSet<Type> GetDependenciesProjectTypes()
        {
            HashSet<Type> dependencies = new HashSet<Type>();

            foreach (Solution.Configuration solutionConfiguration in Configurations)
            {
                foreach (Solution.Configuration.IncludedProjectInfo includedProjectInfo in solutionConfiguration.IncludedProjectInfos)
                    dependencies.Add(includedProjectInfo.Type);
            }
            return dependencies;
        }

        internal void Link(Builder builder)
        {
            if (_dependenciesResolved)
                return;

            bool hasFastBuildProjectConf = false;
            var unlinkedConfigurations = new Dictionary<Solution.Configuration, List<Project.Configuration>>(); // This will hold MSBuild -> Fastbuild refs
            foreach (Solution.Configuration solutionConfiguration in Configurations)
            {
                // Build SolutionFilesMapping
                string configurationFile = Path.Combine(solutionConfiguration.SolutionPath, solutionConfiguration.SolutionFileName);

                var fileConfigurationList = SolutionFilesMapping.GetValueOrAdd(configurationFile, new List<Solution.Configuration>());
                fileConfigurationList.Add(solutionConfiguration);

                var unlinkedList = unlinkedConfigurations.GetValueOrAdd(solutionConfiguration, new List<Project.Configuration>());

                // solutionConfiguration.IncludedProjectInfos will be appended
                // while iterating, but with projects that we already have resolved,
                // so no need to parse them again
                int origCount = solutionConfiguration.IncludedProjectInfos.Count;
                for (int i = 0; i < origCount; ++i)
                {
                    Configuration.IncludedProjectInfo configurationProject = solutionConfiguration.IncludedProjectInfos[i];
                    bool projectIsInactive = configurationProject.InactiveProject;

                    Project project = builder.GetProject(configurationProject.Type);
                    Project.Configuration projectConfiguration = project.GetConfiguration(configurationProject.Target);

                    if (projectConfiguration == null)
                    {
                        var messageBuilder = new System.Text.StringBuilder();
                        messageBuilder.AppendFormat("Resolving dependencies for solution {0}, target '{1}': cannot find target '{3}' in project {2}",
                            GetType().FullName, solutionConfiguration.Target, project.GetType().FullName, configurationProject.Target);
                        messageBuilder.AppendLine();

                        if (project.Configurations.Any())
                        {
                            messageBuilder.AppendLine("Project configurations are:");
                            int confNum = 0;
                            foreach (var conf in project.Configurations)
                                messageBuilder.AppendLine(++confNum + "/" + project.Configurations.Count + " " + conf.ToString());
                        }
                        else
                        {
                            messageBuilder.AppendLine("The project does not contain any configurations!");
                        }

                        Trace.WriteLine(messageBuilder.ToString());
                        Debugger.Break();

                        throw new Error(messageBuilder.ToString());
                    }

                    if (configurationProject.Project == null)
                        configurationProject.Project = project;
                    else if (configurationProject.Project != project)
                        throw new Error("Tried to match more than one project to Project type.");

                    if (configurationProject.Configuration == null)
                        configurationProject.Configuration = projectConfiguration;
                    else if (configurationProject.Configuration != projectConfiguration)
                        throw new Error("Tried to match more than one Project Configuration to a solution configuration.");

                    hasFastBuildProjectConf |= projectConfiguration.IsFastBuild;
                    if (projectConfiguration.IsFastBuild)
                        projectConfiguration.AddMasterBff(solutionConfiguration.MasterBffFilePath);

                    bool build = !projectConfiguration.IsExcludedFromBuild && !configurationProject.InactiveProject;
                    if (build && solutionConfiguration.IncludeOnlyFilterProject && (configurationProject.Project.SourceFilesFiltersCount == 0 || configurationProject.Project.SkipProjectWhenFiltersActive))
                        build = false;

                    if (configurationProject.ToBuild != Configuration.IncludedProjectInfo.Build.YesThroughDependency)
                    {
                        if (build)
                            configurationProject.ToBuild = Configuration.IncludedProjectInfo.Build.Yes;
                        else if (configurationProject.ToBuild != Configuration.IncludedProjectInfo.Build.Yes)
                            configurationProject.ToBuild = Configuration.IncludedProjectInfo.Build.No;
                    }

                    var dependenciesConfiguration = configurationProject.Configuration.GetRecursiveDependencies();
                    // TODO: Slow LINQ? May be better to create this list as part of GetRecursiveDependencies
                    if (!configurationProject.Configuration.IsFastBuild && configurationProject.Configuration.ResolvedDependencies.Any(d => d.IsFastBuild))
                        unlinkedList.Add(configurationProject.Configuration);
                    unlinkedList.AddRange(dependenciesConfiguration.Where(c => !c.IsFastBuild && c.ResolvedDependencies.Any(d => d.IsFastBuild)));
                    foreach (Project.Configuration dependencyConfiguration in dependenciesConfiguration)
                    {
                        Project dependencyProject = dependencyConfiguration.Project;
                        if (dependencyProject.SharpmakeProjectType == Project.ProjectTypeAttribute.Export)
                            continue;

                        Type dependencyProjectType = dependencyProject.GetType();
                        ITarget dependencyProjectTarget = dependencyConfiguration.Target;

                        hasFastBuildProjectConf |= dependencyConfiguration.IsFastBuild;
                        if (dependencyConfiguration.IsFastBuild)
                            dependencyConfiguration.AddMasterBff(solutionConfiguration.MasterBffFilePath);

                        Configuration.IncludedProjectInfo configurationProjectDependency = solutionConfiguration.GetProject(dependencyProjectType);

                        // if that project was not explicitly added to the solution configuration, add it ourselves, as it is needed
                        if (configurationProjectDependency == null)
                        {
                            configurationProjectDependency = new Configuration.IncludedProjectInfo
                            {
                                Type = dependencyProjectType,
                                Project = dependencyProject,
                                Configuration = dependencyConfiguration,
                                Target = dependencyProjectTarget,
                                InactiveProject = projectIsInactive // inherit from the parent: no reason to mark dependencies for build if parent is inactive
                            };
                            solutionConfiguration.IncludedProjectInfos.Add(configurationProjectDependency);
                        }
                        else if (!projectIsInactive && configurationProjectDependency.InactiveProject)
                        {
                            // if the project we found in the solutionConfiguration is inactive, and the current is not, replace its settings
                            configurationProjectDependency.Type = dependencyProjectType;
                            configurationProjectDependency.Project = dependencyProject;
                            configurationProjectDependency.Configuration = dependencyConfiguration;
                            configurationProjectDependency.Target = dependencyProjectTarget;
                            configurationProjectDependency.InactiveProject = false;
                        }
                        else if (projectIsInactive)
                        {
                            // if the current project is inactive, ignore
                        }
                        else
                        {
                            if (!configurationProjectDependency.Target.IsEqualTo(dependencyProjectTarget))
                            {
                                throw new Error("In solution configuration (solution: {3}, config: {4}) the parent project {5} generates multiple dependency targets for the same child project {0}: {1} and {2}. Look for all AddPublicDependency() and AddPrivateDependency() calls for the child project and follow the dependency chain.",
                                    configurationProjectDependency.Project?.GetType().ToString(),
                                    configurationProjectDependency.Target,
                                    dependencyProjectTarget,
                                    solutionConfiguration.SolutionFileName,
                                    solutionConfiguration.Target,
                                    project.Name
                                );
                            }

                            if (configurationProjectDependency.Project == null)
                                configurationProjectDependency.Project = dependencyProject;
                            else if (configurationProjectDependency.Project != dependencyProject)
                                throw new Error("Tried to match more than one project to Project type.");

                            if (configurationProjectDependency.Configuration == null)
                                configurationProjectDependency.Configuration = dependencyConfiguration;
                            else if (configurationProjectDependency.Configuration != dependencyConfiguration)
                                throw new Error("Tried to match more than one Project Configuration to a solution configuration.");
                        }

                        if (configurationProjectDependency.ToBuild != Configuration.IncludedProjectInfo.Build.YesThroughDependency)
                        {
                            // If we're finding a Fastbuild dependency of an MSBuild project, we know that it'll need re-linking if the All project is generated.
                            var needsFastbuildRelink = (dependencyConfiguration.IsFastBuild && !configurationProject.Configuration.IsFastBuild && GenerateFastBuildAllProject);

                            var isExcludedSinceNoFilter = solutionConfiguration.IncludeOnlyFilterProject
                                                      && (configurationProjectDependency.Project.SourceFilesFiltersCount == 0 || configurationProjectDependency.Project.SkipProjectWhenFiltersActive);

                            var skipBuild = dependencyConfiguration.IsExcludedFromBuild
                                         || projectIsInactive
                                         || configurationProjectDependency.InactiveProject
                                         || needsFastbuildRelink
                                         || isExcludedSinceNoFilter;

                            if (!skipBuild)
                            {
                                if (projectConfiguration.Output == Project.Configuration.OutputType.Dll || projectConfiguration.Output == Project.Configuration.OutputType.Exe)
                                    configurationProjectDependency.ToBuild = Configuration.IncludedProjectInfo.Build.YesThroughDependency;
                                else
                                    configurationProjectDependency.ToBuild = Configuration.IncludedProjectInfo.Build.Yes;
                            }
                            else if (configurationProjectDependency.ToBuild != Configuration.IncludedProjectInfo.Build.Yes)
                                configurationProjectDependency.ToBuild = Configuration.IncludedProjectInfo.Build.No;
                        }
                    }
                }
            }

            if (hasFastBuildProjectConf)
                MakeFastBuildAllProjectIfNeeded(builder, unlinkedConfigurations);

            _dependenciesResolved = true;
        }

        internal void Resolve()
        {
            if (_resolved)
                return;

            Resolver resolver = new Resolver();
            resolver.SetParameter("solution", this);
            resolver.Resolve(this);

            if (PerforceRootPath != null)
                Util.ResolvePath(SharpmakeCsPath, ref _perforceRootPath);

            foreach (Solution.Configuration conf in Configurations)
                conf.Resolve(resolver);

            foreach (var extraItemKey in ExtraItems.Keys.ToList())
            {
                Strings values = new Strings(ExtraItems[extraItemKey]);
                foreach (string value in values)
                {
                    string newValue = resolver.Resolve(value);
                    values.UpdateValue(value, newValue);
                }
                ExtraItems[extraItemKey] = values;
            }

            _resolved = true;
        }

        public string ResolveString(string input, Configuration conf = null, ITarget target = null)
        {
            Resolver resolver = new Resolver();
            resolver.SetParameter("solution", this);
            if (conf != null)
                resolver.SetParameter("conf", conf);
            if (target != null)
                resolver.SetParameter("target", target);
            return resolver.Resolve(input);
        }
        #endregion

        #region Private

        private bool _resolved = false;
        private bool _dependenciesResolved = false;

        private void Initialize(Type targetType, Type configurationType)
        {
            var expectedType = typeof(Solution.Configuration);
            if (configurationType == null || (configurationType != expectedType && !configurationType.IsSubclassOf(expectedType)))
                throw new InternalError("configuration type {0} must be a subclass of {1}", targetType.FullName, expectedType.FullName);

            ConfigurationType = configurationType;

            ClassName = GetType().Name;
            Targets.Initialize(targetType);

            string file;
            if (Util.GetStackSourceFileTopMostTypeOf(GetType(), out file))
            {
                FileInfo fileInfo = new FileInfo(file);
                SharpmakeCsFileName = Util.PathMakeStandard(fileInfo.FullName);
                SharpmakeCsPath = Util.PathMakeStandard(fileInfo.DirectoryName);
            }
            else
            {
                throw new InternalError("Cannot locate cs source for type: {0}", GetType().FullName);
            }
        }

        private void MakeFastBuildAllProjectIfNeeded(Builder builder, Dictionary<Solution.Configuration, List<Project.Configuration>> unlinkedConfigurations)
        {
            if (!GenerateFastBuildAllProject)
            {
                return;
            }

            foreach (var solutionFile in SolutionFilesMapping)
            {
                var solutionConfigurations = solutionFile.Value;

                bool generateFastBuildAll = false;
                var projectsToBuildPerSolutionConfig = new List<Tuple<Solution.Configuration, List<Solution.Configuration.IncludedProjectInfo>>>();
                foreach (var solutionConfiguration in solutionConfigurations)
                {
                    var configProjects = solutionConfiguration.IncludedProjectInfos;

                    var fastBuildProjectConfsToBuild = configProjects.Where(
                        configProject => (
                            configProject.Configuration.IsFastBuild &&
                            configProject.ToBuild == Solution.Configuration.IncludedProjectInfo.Build.Yes
                        )
                    ).ToList();

                    if (fastBuildProjectConfsToBuild.Count == 0)
                        continue;

                    // if there's only one project to build, no need for the FastBuildAll
                    generateFastBuildAll |= fastBuildProjectConfsToBuild.Count > 1;
                    projectsToBuildPerSolutionConfig.Add(Tuple.Create(solutionConfiguration, fastBuildProjectConfsToBuild));
                }

                if (!generateFastBuildAll)
                    continue;

                builder.LogWriteLine("    extra FastBuildAll project added to solution " + Path.GetFileName(solutionFile.Key));

                // Use the target type from the first solution configuration, as they all should have the same anyway
                var firstSolutionConf = projectsToBuildPerSolutionConfig.First().Item1;

                Project fastBuildAllProject = null;
                foreach (var projectsToBuildInSolutionConfig in projectsToBuildPerSolutionConfig)
                {
                    var solutionConf = projectsToBuildInSolutionConfig.Item1;
                    var projectConfigsToBuild = projectsToBuildInSolutionConfig.Item2;

                    if (GenerateFastBuildAllOnlyForConfThatNeedIt && projectConfigsToBuild.Count == 1)
                        continue;

                    var solutionTarget = solutionConf.Target;
                    if (fastBuildAllProject == null)
                    {
                        var firstProject = projectConfigsToBuild.First();

                        // Use the target type from the current solution configuration, as they all should have the same anyway
                        fastBuildAllProject = new FastBuildAllProject(solutionConf.Target.GetType())
                        {
                            Name = FastBuildAllProjectName,
                            RootPath = firstProject.Project.RootPath,
                            SourceRootPath = firstProject.Project.RootPath,
                            IsFileNameToLower = firstProject.Project.IsFileNameToLower,
                            SharpmakeProjectType = Project.ProjectTypeAttribute.Generate
                        };
                    }
                    else
                    {
                        // validate the assumption made above
                        if (fastBuildAllProject.Targets.TargetType != firstSolutionConf.Target.GetType())
                            throw new Error("Target type must match between all solution configurations");
                    }

                    fastBuildAllProject.AddTargets(solutionTarget);
                }

                fastBuildAllProject.Targets.BuildTargets();
                fastBuildAllProject.InvokeConfiguration(builder.Context);

                // we need to iterate again after invoking the configure of all the projects so we can tweak their conf
                foreach (var projectsToBuildInSolutionConfig in projectsToBuildPerSolutionConfig)
                {
                    var solutionConf = projectsToBuildInSolutionConfig.Item1;
                    var projectConfigsToBuild = projectsToBuildInSolutionConfig.Item2;

                    if (GenerateFastBuildAllOnlyForConfThatNeedIt && projectConfigsToBuild.Count == 1)
                        continue;

                    var solutionTarget = solutionConf.Target;
                    var projectConf = fastBuildAllProject.GetConfiguration(solutionTarget);

                    // Re-link projects to the new All project
                    // TODO: We should do something to detect and avoid any circular references that this project can now theoretically create.
                    List<Project.Configuration> projectConfigsToRelink;
                    if (unlinkedConfigurations.TryGetValue(solutionConf, out projectConfigsToRelink))
                    {
                        foreach (Project.Configuration config in projectConfigsToRelink.Distinct())
                        {
                            ProjectsDependingOnFastBuildAllForThisSolution.Add(config.Project);
                        }
                    }

                    projectConf.IsFastBuild = true;

                    projectConf.AddMasterBff(solutionConf.MasterBffFilePath);

                    // output the project in the same folder as the solution, and the same name
                    projectConf.ProjectPath = solutionConf.SolutionPath;
                    if (string.IsNullOrWhiteSpace(FastBuildAllProjectFileSuffix))
                        throw new Error("FastBuildAllProjectFileSuffix cannot be left empty in solution " + solutionFile);
                    projectConf.ProjectFileName = solutionFile.Key + FastBuildAllProjectFileSuffix;
                    projectConf.SolutionFolder = FastBuildAllSolutionFolder;

                    // the project doesn't output anything
                    projectConf.Output = Project.Configuration.OutputType.None;

                    // get some settings that are usually global from the first project
                    // we could expose those, if we need to set them specifically for FastBuildAllProject
                    var firstProject = projectConfigsToBuild.First();
                    projectConf.FastBuildCustomArgs = firstProject.Configuration.FastBuildCustomArgs;
                    projectConf.FastBuildCustomActionsBeforeBuildCommand = firstProject.Configuration.FastBuildCustomActionsBeforeBuildCommand;

                    // add all the projects to build as private dependencies, and OnlyBuildOrder
                    foreach (Configuration.IncludedProjectInfo projectConfigToBuild in projectConfigsToBuild)
                    {
                        // update the ToBuild, as now it is built through the "FastBuildAll" dependency
                        projectConfigToBuild.ToBuild = Configuration.IncludedProjectInfo.Build.YesThroughDependency;

                        // Relink any build-order dependencies
                        projectConf.GenericBuildDependencies.AddRange(projectConfigToBuild.Configuration.GenericBuildDependencies);
                        projectConf.GenericBuildDependencies.AddRange(projectConfigToBuild.Configuration.DotNetPublicDependencies.Select(d => d.Configuration).Where(c => !c.IsFastBuild));
                        projectConf.GenericBuildDependencies.AddRange(projectConfigToBuild.Configuration.DotNetPrivateDependencies.Select(d => d.Configuration).Where(c => !c.IsFastBuild));

                        projectConf.AddPrivateDependency(projectConfigToBuild.Target, projectConfigToBuild.Project.GetType(), DependencySetting.OnlyBuildOrder);
                    }

                    // add the newly generated project to the solution config
                    solutionConf.IncludedProjectInfos.Add(
                        new Configuration.IncludedProjectInfo
                        {
                            Project = fastBuildAllProject,
                            Configuration = projectConf,
                            Target = solutionTarget,
                            Type = fastBuildAllProject.GetType(),
                            ToBuild = Configuration.IncludedProjectInfo.Build.Yes
                        }
                    );
                }

                fastBuildAllProject.Resolve(builder, false);
                fastBuildAllProject.Link(builder);

                builder.RegisterGeneratedProject(fastBuildAllProject);
            }
        }

        #endregion
    }

    public class CSharpSolution : Solution
    {
        public CSharpSolution()
            : this(typeof(Target))
        {
        }

        public CSharpSolution(Type targetType)
            : base(targetType)
        {
            IsFileNameToLower = false;
        }
    }

    public class PythonSolution : Solution
    {
        public PythonSolution()
            : base(typeof(Target))
        {
        }

        public PythonSolution(Type targetType)
            : base(targetType)
        {
        }
    }
}
