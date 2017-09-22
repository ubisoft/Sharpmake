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

        private string _perforceRootPath = null;
        public string PerforceRootPath
        {
            get { return _perforceRootPath; }
            set { SetProperty(ref _perforceRootPath, value); }
        }

        // TODO: currently broken
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

        public Solution()
        {
            Initialize(typeof(Target));
        }

        public Solution(Type targetType)
        {
            Initialize(targetType);
        }

        #region Internal

        public class ResolvedProject
        {
            // Associated project
            public Project Project;

            public string ProjectName;

            public string SolutionFolder;

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

            solution.Targets.AddFragmentMask(fragmentMasks.ToArray());
            solution.Targets.BuildTargets();
            return solution;
        }

        public List<ResolvedProject> GetResolvedProjects(List<Configuration> solutionConfigurations)
        {
            if (!_dependenciesResolved)
                throw new InternalError("Solution not resolved: {0}", GetType().FullName);

            List<ResolvedProject> result = new List<ResolvedProject>();

            foreach (Configuration solutionConfiguration in solutionConfigurations)
            {
                foreach (Configuration.IncludedProjectInfo includedProjectInfo in solutionConfiguration.IncludedProjectInfos)
                {
                    if (solutionConfiguration.IncludeOnlyFilterProject && (includedProjectInfo.Project.SourceFilesFiltersCount == 0 || includedProjectInfo.Project.SkipProjectWhenFiltersActive))
                        continue;

                    ResolvedProject resolvedProject = result.Find(p => p.OriginalProjectFile == includedProjectInfo.Configuration.ProjectFullFileName);
                    if (resolvedProject == null)
                    {
                        resolvedProject = new ResolvedProject();
                        resolvedProject.Project = includedProjectInfo.Project;


                        resolvedProject.TargetDefault = includedProjectInfo.Target;
                        resolvedProject.OriginalProjectFile = includedProjectInfo.Configuration.ProjectFullFileName;
                        resolvedProject.ProjectFile = Util.GetCapitalizedPath(includedProjectInfo.Configuration.ProjectFullFileNameWithExtension);
                        resolvedProject.ProjectName = includedProjectInfo.Configuration.ProjectName;
                        result.Add(resolvedProject);
                    }

                    resolvedProject.Configurations.Add(includedProjectInfo.Configuration);
                }
            }


            foreach (ResolvedProject resolvedProject in result)
            {
                // Folder must all be the same for all config, else will be emptied.
                foreach (Project.Configuration projectConfiguration in resolvedProject.Configurations)
                {
                    if (resolvedProject.SolutionFolder == null)
                        resolvedProject.SolutionFolder = projectConfiguration.SolutionFolder;
                    else if (resolvedProject.SolutionFolder != projectConfiguration.SolutionFolder)
                        resolvedProject.SolutionFolder = "";
                }

                foreach (Project.Configuration resolvedProjectConf in resolvedProject.Configurations)
                {
                    foreach (Project.Configuration dependencyConfiguration in resolvedProjectConf.ResolvedDependencies)
                    {
                        foreach (ResolvedProject resolvedProjectToAdd in result)
                        {
                            if (resolvedProjectToAdd.Configurations.Contains(dependencyConfiguration))
                            {
                                if (!resolvedProject.Dependencies.Contains(resolvedProjectToAdd))
                                    resolvedProject.Dependencies.Add(resolvedProjectToAdd);
                            }
                        }
                    }
                }
            }

            return result;
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

            foreach (Solution.Configuration solutionConfiguration in Configurations)
            {
                // Build SolutionFilesMapping
                string configurationFile = Path.Combine(solutionConfiguration.SolutionPath, solutionConfiguration.SolutionFileName);

                var fileConfigurationList = SolutionFilesMapping.GetValueOrAdd(configurationFile, new List<Solution.Configuration>());
                fileConfigurationList.Add(solutionConfiguration);

                // solutionConfiguration.IncludedProjectInfos will be appended
                // while iterating, but with projects that we already have resolved,
                // so no need to parse them again
                int origCount = solutionConfiguration.IncludedProjectInfos.Count;
                for (int i = 0; i < origCount; ++i)
                {
                    Configuration.IncludedProjectInfo configurationProject = solutionConfiguration.IncludedProjectInfos[i];

                    Project project = builder.GetProject(configurationProject.Type);
                    Project.Configuration projectConfiguration = project.GetConfiguration(configurationProject.Target);

                    if (projectConfiguration == null)
                    {
                        throw new Error(
                            "Solution {0} for target '{1}' contains project {2} with invalid target '{3}'",
                            GetType().FullName, solutionConfiguration.Target, project.GetType().FullName, configurationProject.Target
                        );
                    }

                    configurationProject.Project = project;
                    configurationProject.Configuration = projectConfiguration;

                    bool build = !projectConfiguration.IsExcludedFromBuild && !configurationProject.InactiveProject;
                    if (configurationProject.ToBuild != Configuration.IncludedProjectInfo.Build.YesThroughDependency)
                    {
                        if (build)
                            configurationProject.ToBuild = Configuration.IncludedProjectInfo.Build.Yes;
                        else if(configurationProject.ToBuild != Configuration.IncludedProjectInfo.Build.Yes)
                            configurationProject.ToBuild = Configuration.IncludedProjectInfo.Build.No;
                    }

                    var dependenciesConfiguration = configurationProject.Configuration.GetRecursiveDependencies();
                    foreach (Project.Configuration dependencyConfiguration in dependenciesConfiguration)
                    {
                        Type dependencyProjectType = dependencyConfiguration.Project.GetType();

                        if (dependencyProjectType.IsDefined(typeof(Export), false))
                            continue;

                        ITarget dependencyProjectTarget = dependencyConfiguration.Target;

                        Configuration.IncludedProjectInfo configurationProjectDependency = solutionConfiguration.GetProject(dependencyProjectType);

                        if (configurationProjectDependency == null)
                        {
                            var dependencyProject = builder.GetProject(dependencyProjectType);
                            configurationProjectDependency = new Configuration.IncludedProjectInfo
                            {
                                Type = dependencyProjectType,
                                Project = dependencyProject,
                                Configuration = dependencyProject.GetConfiguration(dependencyProjectTarget),
                                Target = dependencyProjectTarget,
                                InactiveProject = configurationProject.InactiveProject // inherit from the parent: no reason to mark dependencies for build if parent is inactive
                            };
                            solutionConfiguration.IncludedProjectInfos.Add(configurationProjectDependency);
                        }
                        else
                        {
                            if (!configurationProjectDependency.Target.IsEqualTo(dependencyProjectTarget))
                                throw new Error("In solution configuration (solution: {3}, config: {4}) the parent project {5} generates multiple dependency targets for the same child project {0}: {1} and {2}. Look for all AddPublicDependency() and AddPrivateDependency() calls for the child project and follow the dependency chain.",
                                                configurationProjectDependency.Project.GetType().ToString(),
                                                configurationProjectDependency.Target,
                                                dependencyProjectTarget,
                                                solutionConfiguration.SolutionFileName,
                                                solutionConfiguration.Target,
                                                project.Name);
                        }

                        bool depBuild = !dependencyConfiguration.IsExcludedFromBuild && !configurationProjectDependency.InactiveProject;
                        if (configurationProjectDependency.ToBuild != Configuration.IncludedProjectInfo.Build.YesThroughDependency)
                        {
                            if (depBuild)
                                configurationProjectDependency.ToBuild = Configuration.IncludedProjectInfo.Build.YesThroughDependency;
                            else if(configurationProjectDependency.ToBuild != Configuration.IncludedProjectInfo.Build.Yes)
                                configurationProjectDependency.ToBuild = Configuration.IncludedProjectInfo.Build.No;
                        }
                    }
                }
            }
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

        private void Initialize(Type targetType)
        {
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
                throw new InternalError("Cannot locate cs source for type: {}", GetType().FullName);
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
