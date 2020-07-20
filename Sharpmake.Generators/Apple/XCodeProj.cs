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
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators.Apple
{
    public partial class XCodeProj : IProjectGenerator
    {
        public const string ProjectExtension = ".xcodeproj";
        private const string ProjectFileName = "project.pbxproj";
        private const string ProjectSchemeExtension = ".xcscheme";

        private const int ProjectArchiveVersion = 1;
        private const int ProjectObjectVersion = 46;

        public const string RemoveLineTag = FileGeneratorUtilities.RemoveLineTag;

        public static readonly char FolderSeparator;

        private class XCodeGenerationContext : IGenerationContext
        {
            #region IGenerationContext implementation
            public Builder Builder { get; }
            public Project Project { get; }
            public Project.Configuration Configuration { get; internal set; }
            public string ProjectDirectory { get; }
            public DevEnv DevelopmentEnvironment => Configuration.Compiler;
            public Options.ExplicitOptions Options { get; set; } = new Options.ExplicitOptions();
            public IDictionary<string, string> CommandLineOptions { get; set; } = new VisualStudio.ProjectOptionsGenerator.VcxprojCmdLineOptions();

            public string ProjectDirectoryCapitalized { get; }
            public string ProjectSourceCapitalized { get; }

            public bool PlainOutput => true;

            public void SelectOption(params Options.OptionAction[] options)
            {
                Sharpmake.Options.SelectOption(Configuration, options);
            }
            public void SelectOptionWithFallback(Action fallbackAction, params Options.OptionAction[] options)
            {
                Sharpmake.Options.SelectOptionWithFallback(Configuration, fallbackAction, options);
            }
            #endregion

            public XCodeGenerationContext(Builder builder, string projectPath, Project project)
            {
                Builder = builder;

                ProjectDirectory = projectPath;
                Project = project;
                ProjectDirectoryCapitalized = Util.GetCapitalizedPath(ProjectDirectory);
                ProjectSourceCapitalized = Util.GetCapitalizedPath(project.SourceRootPath);
            }
        }

        private readonly HashSet<ProjectItem> _projectItems = new HashSet<ProjectItem>();

        //Source files that are potentially removable. Need to check if they are excluded from build in all configs.
        private HashSet<ProjectFileSystemItem> _removableItems = new HashSet<ProjectFileSystemItem>();
        private ProjectFolder _mainGroup = null;
        private ProjectFolder _productsGroup = null;
        private ProjectFolder _frameworksFolder = null;

        private Dictionary<string, ProjectTarget> _nativeOrLegacyTargets = null;
        private Dictionary<string, ProjectResourcesBuildPhase> _resourcesBuildPhases = null;
        private Dictionary<string, ProjectSourcesBuildPhase> _sourcesBuildPhases = null;
        private Dictionary<string, ProjectFrameworksBuildPhase> _frameworksBuildPhases = null;
        private Dictionary<string, UniqueList<ProjectShellScriptBuildPhase>> _shellScriptPreBuildPhases = null;
        private Dictionary<string, UniqueList<ProjectShellScriptBuildPhase>> _shellScriptPostBuildPhases = null;
        private Dictionary<string, List<ProjectTargetDependency>> _targetDependencies = null;

        private Dictionary<ProjectFolder, ProjectReference> _projectReferencesGroups = null;
        private ProjectMain _projectMain = null;

        private Dictionary<Project.Configuration, Options.ExplicitOptions> _optionMapping = null;

        // Unit Test Variables
        private string _unitTestFramework = "XCTest";

        static XCodeProj()
        {
            FolderSeparator = Util.UnixSeparator;
        }

        public void Generate(
            Builder builder,
            Project project,
            List<Project.Configuration> configurations,
            string projectFile,
            List<string> generatedFiles,
            List<string> skipFiles
        )
        {
            FileInfo fileInfo = new FileInfo(projectFile);
            string projectPath = fileInfo.Directory.FullName;
            string projectFileName = fileInfo.Name;

            var context = new XCodeGenerationContext(builder, projectPath, project);
            PrepareSections(context, configurations);

            bool updated;
            string projectFileResult = GenerateProject(context, configurations, projectFileName, out updated);
            if (updated)
                generatedFiles.Add(projectFileResult);
            else
                skipFiles.Add(projectFileResult);

            string projectFileSchemeResult = GenerateProjectScheme(context, configurations, projectFileName, out updated);
            if (updated)
                generatedFiles.Add(projectFileSchemeResult);
            else
                skipFiles.Add(projectFileSchemeResult);
        }

        private string GenerateProject(
            XCodeGenerationContext context,
            List<Project.Configuration> configurations,
            string projectFile,
            out bool updated
        )
        {
            // Create the target folder (solutions and projects are folders in XCode).
            string projectFolder = Util.GetCapitalizedPath(Path.Combine(context.ProjectDirectoryCapitalized, projectFile + ProjectExtension));
            Directory.CreateDirectory(projectFolder);

            string projectFilePath = Path.Combine(projectFolder, ProjectFileName);
            FileInfo projectFileInfo = new FileInfo(projectFilePath);

            var fileGenerator = InitProjectGenerator(configurations);

            // Write the project file
            updated = context.Builder.Context.WriteGeneratedFile(context.Project.GetType(), projectFileInfo, fileGenerator.ToMemoryStream());

            string projectFileResult = projectFileInfo.FullName;
            return projectFileResult;
        }

        private string GenerateProjectScheme(
            XCodeGenerationContext context,
            List<Project.Configuration> configurations,
            string projectFile,
            out bool updated
        )
        {
            // Create the target folder (solutions and projects are folders in XCode).
            string projectSchemeFolder = Util.GetCapitalizedPath(Path.Combine(context.ProjectDirectoryCapitalized, projectFile + ProjectExtension, "xcshareddata", "xcschemes"));
            Directory.CreateDirectory(projectSchemeFolder);

            string projectSchemeFilePath = Path.Combine(projectSchemeFolder, projectFile + ProjectSchemeExtension);
            FileInfo projectSchemeFileInfo = new FileInfo(projectSchemeFilePath);

            var fileGenerator = InitProjectSchemeGenerator(configurations, projectFile);

            // Write the scheme file
            updated = context.Builder.Context.WriteGeneratedFile(context.Project.GetType(), projectSchemeFileInfo, fileGenerator.ToMemoryStream());
            string projectFileResult = projectSchemeFileInfo.FullName;

            return projectFileResult;
        }

        private FileGenerator InitProjectGenerator(IList<Project.Configuration> configurations)
        {
            // Header.
            var fileGenerator = new FileGenerator();
            using (fileGenerator.Declare("archiveVersion", ProjectArchiveVersion))
            using (fileGenerator.Declare("objectVersion", ProjectObjectVersion))
            {
                fileGenerator.Write(Template.GlobalHeader);
            }

            WriteSection<ProjectBuildFile>(configurations[0], fileGenerator);
            WriteSection<ProjectContainerProxy>(configurations[0], fileGenerator);
            WriteSection<ProjectFile>(configurations[0], fileGenerator);
            WriteSection<ProjectFrameworksBuildPhase>(configurations[0], fileGenerator);
            WriteSection<ProjectFolder>(configurations[0], fileGenerator);
            WriteSection<ProjectNativeTarget>(configurations[0], fileGenerator);
            WriteSection<ProjectLegacyTarget>(configurations[0], fileGenerator);
            WriteSection<ProjectMain>(configurations[0], fileGenerator);
            WriteSection<ProjectReferenceProxy>(configurations[0], fileGenerator);
            WriteSection<ProjectResourcesBuildPhase>(configurations[0], fileGenerator);
            WriteSection<ProjectSourcesBuildPhase>(configurations[0], fileGenerator);
            WriteSection<ProjectVariantGroup>(configurations[0], fileGenerator);
            WriteSection<ProjectTargetDependency>(configurations[0], fileGenerator);
            WriteSection<ProjectBuildConfiguration>(configurations[0], fileGenerator);
            WriteSection<ProjectConfigurationList>(configurations[0], fileGenerator);
            WriteSection<ProjectShellScriptBuildPhase>(configurations[0], fileGenerator);

            // Footer.
            using (fileGenerator.Declare("RootObject", _projectMain))
            {
                fileGenerator.Write(Template.GlobalFooter);
            }

            // Remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();

            return fileGenerator;
        }

        private FileGenerator InitProjectSchemeGenerator(
            List<Project.Configuration> configurations,
            string projectFile
        )
        {
            // Setup resolvers
            var fileGenerator = new FileGenerator();

            // Build testable elements
            var testableTargets = _nativeOrLegacyTargets.Values.Where(target => target.OutputFile.OutputType == Project.Configuration.OutputType.IosTestBundle);
            var testableElements = new StringBuilder();
            foreach (var target in testableTargets)
            {
                using (fileGenerator.Declare("projectFile", projectFile))
                using (fileGenerator.Declare("item", target))
                {
                    testableElements.Append(fileGenerator.Resolver.Resolve(Template.SchemeTestableReference));
                }
            }

            // Write the scheme file
            var defaultTarget = _nativeOrLegacyTargets.Values.Where(target => target.OutputFile.OutputType != Project.Configuration.OutputType.IosTestBundle).FirstOrDefault();

            var options = new Options.ExplicitOptions();
            Options.SelectOption(configurations[0],
                Options.Option(Options.XCode.Compiler.EnableGpuFrameCaptureMode.AutomaticallyEnable, () => options["EnableGpuFrameCaptureMode"] = RemoveLineTag),
                Options.Option(Options.XCode.Compiler.EnableGpuFrameCaptureMode.MetalOnly, () => options["EnableGpuFrameCaptureMode"] = "1"),
                Options.Option(Options.XCode.Compiler.EnableGpuFrameCaptureMode.OpenGLOnly, () => options["EnableGpuFrameCaptureMode"] = "2"),
                Options.Option(Options.XCode.Compiler.EnableGpuFrameCaptureMode.Disable, () => options["EnableGpuFrameCaptureMode"] = "3")
            );
            using (fileGenerator.Declare("projectFile", projectFile))
            using (fileGenerator.Declare("item", defaultTarget))
            using (fileGenerator.Declare("options", options))
            using (fileGenerator.Declare("testableElements", testableElements))
            using (fileGenerator.Declare("optimization", configurations[0].Target.Name))
            {
                fileGenerator.Write(Template.SchemeFileTemplate);
            }

            // Remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();

            return fileGenerator;
        }

        private void PrepareSections(XCodeGenerationContext context, List<Project.Configuration> configurations)
        {
            Project project = context.Project;

            //TODO: add support for multiple targets with the same outputtype. Would need a mechanism to define a default configuration per target and associate it with non-default conf with different optimization.
            //At the moment it only supports target with different output type (e.g:lib, app, test bundle)
            //Note that we also separate FastBuild configurations
            Dictionary<string, List<Project.Configuration>> projectTargetsList = GetProjectConfigurationsPerTarget(configurations);

            //Directory structure
            SetRootGroup(project, configurations[0]);

            ProjectVariantGroup variantGroup = new ProjectVariantGroup();
            _projectItems.Add(variantGroup);

            Strings projectFiles = project.GetSourceFilesForConfigurations(configurations);
            string workspacePath = Directory.GetParent(configurations[0].ProjectFullFileNameWithExtension).FullName;

            //Generate options for each configuration
            _optionMapping = new Dictionary<Project.Configuration, Options.ExplicitOptions>();
            foreach (Project.Configuration configuration in configurations)
            {
                context.Configuration = configuration;
                _optionMapping[configuration] = GenerateOptions(context);

                Strings assetCatalog = Options.GetStrings<Options.XCode.Compiler.AssetCatalog>(configuration);
                XCodeUtil.ResolveProjectPaths(project, assetCatalog);
                foreach (string asset in assetCatalog)
                {
                    projectFiles.Add(asset);
                }
            }

            _projectReferencesGroups = new Dictionary<ProjectFolder, ProjectReference>();

            _nativeOrLegacyTargets = new Dictionary<string, ProjectTarget>();
            _targetDependencies = new Dictionary<string, List<ProjectTargetDependency>>();
            _sourcesBuildPhases = new Dictionary<string, ProjectSourcesBuildPhase>();
            _resourcesBuildPhases = new Dictionary<string, ProjectResourcesBuildPhase>();
            _frameworksBuildPhases = new Dictionary<string, ProjectFrameworksBuildPhase>();
            _shellScriptPreBuildPhases = new Dictionary<string, UniqueList<ProjectShellScriptBuildPhase>>();
            _shellScriptPostBuildPhases = new Dictionary<string, UniqueList<ProjectShellScriptBuildPhase>>();

            //Loop on each targets
            foreach (var projectTarget in projectTargetsList)
            {
                string xCodeTargetName = projectTarget.Key;
                var targetConfigurations = projectTarget.Value;

                var configurationsForTarget = new HashSet<ProjectBuildConfiguration>();
                var configurationListForNativeTarget = new ProjectConfigurationList(configurationsForTarget, xCodeTargetName);
                _projectItems.Add(configurationListForNativeTarget);

                var firstConf = targetConfigurations.First();

                if (!firstConf.IsFastBuild) // since we grouped all FastBuild conf together, we only need to test the first conf
                {
                    var projectSourcesBuildPhase = new ProjectSourcesBuildPhase(xCodeTargetName, 2147483647);
                    _projectItems.Add(projectSourcesBuildPhase);
                    _sourcesBuildPhases.Add(xCodeTargetName, projectSourcesBuildPhase);
                }

                var resourceBuildPhase = new ProjectResourcesBuildPhase(xCodeTargetName, 2147483647);
                _projectItems.Add(resourceBuildPhase);
                _resourcesBuildPhases.Add(xCodeTargetName, resourceBuildPhase);

                var frameworkBuildPhase = new ProjectFrameworksBuildPhase(xCodeTargetName, 2147483647);
                _projectItems.Add(frameworkBuildPhase);
                _frameworksBuildPhases.Add(xCodeTargetName, frameworkBuildPhase);

                var targetDependencies = new List<ProjectTargetDependency>();
                _targetDependencies.Add(xCodeTargetName, targetDependencies);

                string masterBffFilePath = null;

                foreach (var conf in targetConfigurations)
                {
                    if (!conf.IsFastBuild)
                        PrepareSourceFiles(xCodeTargetName, projectFiles.Where(file => { return !project.ResourceFiles.Contains(file); }), project, conf, workspacePath);
                    PrepareResourceFiles(xCodeTargetName, project.ResourceFiles, project, conf);
                    PrepareExternalResourceFiles(xCodeTargetName, project, conf);

                    RegisterScriptBuildPhase(xCodeTargetName, _shellScriptPreBuildPhases, conf.EventPreBuild.GetEnumerator());
                    RegisterScriptBuildPhase(xCodeTargetName, _shellScriptPostBuildPhases, conf.EventPostBuild.GetEnumerator());

                    Strings systemFrameworks = Options.GetStrings<Options.XCode.Compiler.SystemFrameworks>(conf);
                    foreach (string systemFramework in systemFrameworks)
                    {
                        var systemFrameworkItem = new ProjectSystemFrameworkFile(systemFramework);
                        var buildFileItem = new ProjectBuildFile(systemFrameworkItem);
                        if (!_frameworksFolder.Children.Exists(item => item.FullPath == systemFrameworkItem.FullPath))
                        {
                            _frameworksFolder.Children.Add(systemFrameworkItem);
                            _projectItems.Add(systemFrameworkItem);
                        }
                        _projectItems.Add(buildFileItem);
                        _frameworksBuildPhases[xCodeTargetName].Files.Add(buildFileItem);
                    }

                    // master bff path
                    if (conf.IsFastBuild)
                    {
                        // we only support projects in one or no master bff, but in that last case just output a warning
                        var masterBffList = conf.FastBuildMasterBffList.Distinct().ToArray();
                        if (masterBffList.Length == 0)
                        {
                            Builder.Instance.LogWarningLine("Bff {0} doesn't appear in any master bff, it won't be buildable.", conf.BffFullFileName + FastBuildSettings.FastBuildConfigFileExtension);
                        }
                        else if (masterBffList.Length > 1)
                        {
                            throw new Error("Bff {0} appears in {1} master bff, sharpmake only supports 1.", conf.BffFullFileName + FastBuildSettings.FastBuildConfigFileExtension, masterBffList.Length);
                        }
                        else
                        {
                            if (masterBffFilePath != null && masterBffFilePath != masterBffList[0])
                                throw new Error("Project {0} has a fastbuild target that has distinct master bff, sharpmake only supports 1.", conf);
                            masterBffFilePath = masterBffList[0];
                        }
                    }

                    Strings userFrameworks = Options.GetStrings<Options.XCode.Compiler.UserFrameworks>(conf);
                    foreach (string userFramework in userFrameworks)
                    {
                        var userFrameworkItem = new ProjectUserFrameworkFile(XCodeUtil.ResolveProjectPaths(project, userFramework), workspacePath);
                        var buildFileItem = new ProjectBuildFile(userFrameworkItem);
                        _frameworksFolder.Children.Add(userFrameworkItem);
                        _projectItems.Add(userFrameworkItem);
                        _projectItems.Add(buildFileItem);
                        _frameworksBuildPhases[xCodeTargetName].Files.Add(buildFileItem);
                    }

                    if (conf.Output == Project.Configuration.OutputType.IosTestBundle)
                    {
                        var testFrameworkItem = new ProjectDeveloperFrameworkFile(_unitTestFramework);
                        var buildFileItem = new ProjectBuildFile(testFrameworkItem);
                        if (_frameworksFolder != null)
                            _frameworksFolder.Children.Add(testFrameworkItem);
                        _projectItems.Add(testFrameworkItem);
                        _projectItems.Add(buildFileItem);
                        _frameworksBuildPhases[xCodeTargetName].Files.Add(buildFileItem);
                    }
                }

                // use the first conf as file, but the target name
                var targetOutputFile = new ProjectOutputFile(firstConf, xCodeTargetName);
                _productsGroup.Children.Add(targetOutputFile);

                _projectItems.Add(targetOutputFile);

                var projectOutputBuildFile = new ProjectBuildFile(targetOutputFile);
                _projectItems.Add(projectOutputBuildFile);

                ProjectTarget target;
                if (!firstConf.IsFastBuild)
                {
                    target = new ProjectNativeTarget(xCodeTargetName, targetOutputFile, configurationListForNativeTarget, _targetDependencies[xCodeTargetName]);
                }
                else
                {
                    target = new ProjectLegacyTarget(xCodeTargetName, targetOutputFile, configurationListForNativeTarget, masterBffFilePath);
                }
                target.ResourcesBuildPhase = _resourcesBuildPhases[xCodeTargetName];
                if (_sourcesBuildPhases.ContainsKey(xCodeTargetName))
                    target.SourcesBuildPhase = _sourcesBuildPhases[xCodeTargetName];

                target.FrameworksBuildPhase = _frameworksBuildPhases[xCodeTargetName];
                if (_shellScriptPreBuildPhases.ContainsKey(xCodeTargetName))
                    target.ShellScriptPreBuildPhases = _shellScriptPreBuildPhases[xCodeTargetName];

                if (_shellScriptPostBuildPhases.ContainsKey(xCodeTargetName))
                    target.ShellScriptPostBuildPhases = _shellScriptPostBuildPhases[xCodeTargetName];

                configurationListForNativeTarget.RelatedItem = target;
                _projectItems.Add(target);
                _nativeOrLegacyTargets.Add(xCodeTargetName, target);

                //Generate BuildConfigurations
                foreach (Project.Configuration targetConf in targetConfigurations)
                {
                    var options = _optionMapping[targetConf];
                    ProjectBuildConfigurationForTarget configurationForTarget = null;
                    if (targetConf.Output == Project.Configuration.OutputType.IosTestBundle)
                        configurationForTarget = new ProjectBuildConfigurationForUnitTestTarget(targetConf, target, options);
                    else if (!targetConf.IsFastBuild)
                        configurationForTarget = new ProjectBuildConfigurationForNativeTarget(targetConf, (ProjectNativeTarget)target, options);
                    else
                        configurationForTarget = new ProjectBuildConfigurationForLegacyTarget(targetConf, (ProjectLegacyTarget)target, options);

                    configurationsForTarget.Add(configurationForTarget);
                    _projectItems.Add(configurationForTarget);
                }
            }

            // Generate dependencies for unit test targets.
            var unitTestConfigs = new List<Project.Configuration>(configurations).FindAll(element => element.Output == Project.Configuration.OutputType.IosTestBundle);
            if (unitTestConfigs != null && unitTestConfigs.Count != 0)
            {
                foreach (Project.Configuration unitTestConfig in unitTestConfigs)
                {
                    Project.Configuration bundleLoadingAppConfiguration = FindBundleLoadingApp(configurations);
                    if (bundleLoadingAppConfiguration == null)
                        continue;

                    string key = GetTargetKey(bundleLoadingAppConfiguration);
                    if (!_nativeOrLegacyTargets.ContainsKey(key))
                        continue;

                    ProjectTarget target = _nativeOrLegacyTargets[key];
                    if (!(target is ProjectNativeTarget))
                        continue;

                    ProjectNativeTarget bundleLoadingAppTarget = (ProjectNativeTarget)_nativeOrLegacyTargets[key];

                    ProjectReference projectReference = new ProjectReference(ItemSection.PBXProject, bundleLoadingAppTarget.Identifier);
                    ProjectContainerProxy projectProxy = new ProjectContainerProxy(projectReference, bundleLoadingAppTarget, ProjectContainerProxy.Type.Target);

                    ProjectTargetDependency targetDependency = new ProjectTargetDependency(projectReference, projectProxy, bundleLoadingAppTarget);
                    _projectItems.Add(targetDependency);

                    ((ProjectNativeTarget)_nativeOrLegacyTargets[GetTargetKey(unitTestConfig)]).Dependencies.Add(targetDependency);
                }
            }

            foreach (ProjectFileSystemItem item in _removableItems)
            {
                // Excluded from build in all configs: remove them from the solution.
                if (IsBuildExcludedForAllConfigurations(configurations, item.Path))
                {
                    RemoveFromFileSystem(item);
                }
            }

            HashSet<ProjectBuildConfiguration> configurationsForProject = new HashSet<ProjectBuildConfiguration>();
            ProjectConfigurationList configurationListForProject = new ProjectConfigurationList(configurationsForProject, "configurationListForProject");
            _projectItems.Add(configurationListForProject);

            //This loop will find the register to the sets _projectItems  and configurationsForProject the first configuration for each optimization type that is contained in the configurations.
            //Project options can only be set according to optimization types e.g: Debug, Release, Retail.
            foreach (Project.Configuration configuration in configurations)
            {
                var options = _optionMapping[configuration];

                ProjectBuildConfigurationForProject configurationForProject = new ProjectBuildConfigurationForProject(configuration, options);
                configurationsForProject.Add(configurationForProject);
                _projectItems.Add(configurationForProject);
            }

            bool iCloudSupport = (_optionMapping[configurations[0]]["iCloud"] == "1");
            string developmentTeam = _optionMapping[configurations[0]]["DevelopmentTeam"];
            string provisioningStyle = _optionMapping[configurations[0]]["ProvisioningStyle"];
            var nativeOrLegacyTargets = new List<ProjectTarget>(_nativeOrLegacyTargets.Values);
            _projectMain = new ProjectMain(project.Name, _mainGroup, configurationListForProject, nativeOrLegacyTargets, iCloudSupport, developmentTeam, provisioningStyle);

            configurationListForProject.RelatedItem = _projectMain;
            foreach (KeyValuePair<ProjectFolder, ProjectReference> referenceGroup in _projectReferencesGroups)
            {
                _projectMain.AddProjectDependency(referenceGroup.Key, referenceGroup.Value);
            }

            _projectItems.Add(_projectMain);
        }

        //Find Project.Configuration of the bundle loading app that matches the unit test target, if it exists.
        //Should have OutputType IosApp assuming targets with different output type.
        private Project.Configuration FindBundleLoadingApp(List<Project.Configuration> configurations)
        {
            return configurations.Find(element => (element.Output == Project.Configuration.OutputType.IosApp));
        }

        private static string GetTargetKey(Project.Configuration conf)
        {
            if (conf.IsFastBuild)
                return conf.Project.Name + " FastBuild";
            return conf.Project.Name;
        }

        // Key is the name of a Target, Value is the list of configs per target
        private Dictionary<string, List<Project.Configuration>> GetProjectConfigurationsPerTarget(List<Project.Configuration> configurations)
        {
            var configsPerTarget = configurations.GroupBy(conf => GetTargetKey(conf)).ToDictionary(g => g.Key, g => g.ToList());

            return configsPerTarget;
        }

        private void RegisterScriptBuildPhase(string xCodeTargetName, Dictionary<string, UniqueList<ProjectShellScriptBuildPhase>> shellScriptPhases, IEnumerator<string> eventsInConf)
        {
            while (eventsInConf.MoveNext())
            {
                var buildEvent = eventsInConf.Current;
                var shellScriptBuildPhase = new ProjectShellScriptBuildPhase(buildEvent, 2147483647)
                {
                    script = buildEvent
                };
                _projectItems.Add(shellScriptBuildPhase);
                if (!shellScriptPhases.ContainsKey(xCodeTargetName))
                {
                    shellScriptPhases.Add(xCodeTargetName, new UniqueList<ProjectShellScriptBuildPhase>(new ProjectShellScriptBuildPhase.EqualityComparer()) { shellScriptBuildPhase });
                }
                else
                {
                    shellScriptPhases[xCodeTargetName].Add(shellScriptBuildPhase);
                }
            }
        }

        private static void FillIncludeDirectoriesOptions(IGenerationContext context, IPlatformVcxproj platformVcxproj)
        {
            var includePaths = new OrderableStrings(platformVcxproj.GetIncludePaths(context));
            context.Options["IncludePaths"] = XCodeUtil.XCodeFormatList(includePaths, 4);
        }

        private static void FillCompilerOptions(IGenerationContext context, IPlatformVcxproj platformVcxproj)
        {
            platformVcxproj.SelectCompilerOptions(context);
        }

        private static void SelectAdditionalLibraryDirectoriesOption(IGenerationContext context, IPlatformVcxproj platformVcxproj)
        {
            var conf = context.Configuration;
            var options = context.Options;

            options["LibraryPaths"] = FileGeneratorUtilities.RemoveLineTag;

            var libraryPaths = new OrderableStrings(conf.LibraryPaths);
            libraryPaths.AddRange(conf.DependenciesOtherLibraryPaths);
            libraryPaths.AddRange(conf.DependenciesBuiltTargetsLibraryPaths);
            libraryPaths.AddRange(platformVcxproj.GetLibraryPaths(context)); // LCTODO: not sure about that one

            libraryPaths.Sort();
            options["LibraryPaths"] = XCodeUtil.XCodeFormatList(libraryPaths, 4);
        }

        private bool IsBuildExcludedForAllConfigurations(List<Project.Configuration> configurations, string fullPath)
        {
            foreach (Project.Configuration conf in configurations)
            {
                if (!conf.ResolvedSourceFilesBuildExclude.Contains(fullPath))
                {
                    return false;
                }
            }
            return true;
        }

        private void PrepareSourceFiles(string xCodeTargetName, IEnumerable<string> sourceFiles, Project project, Project.Configuration configuration, string workspacePath = null)
        {
            foreach (string file in sourceFiles)
            {
                bool alreadyPresent;
                ProjectFileSystemItem item = AddInFileSystem(file, out alreadyPresent, workspacePath, true);
                if (alreadyPresent)
                    continue;

                item.Build = !configuration.ResolvedSourceFilesBuildExclude.Contains(item.FullPath);
                item.Source = project.SourceFilesCompileExtensions.Contains(item.Extension) || (String.Compare(item.Extension, ".mm", StringComparison.OrdinalIgnoreCase) == 0) || (String.Compare(item.Extension, ".m", StringComparison.OrdinalIgnoreCase) == 0);

                if (item.Source)
                {
                    if (item.Build)
                    {
                        ProjectFile fileItem = (ProjectFile)item;
                        ProjectBuildFile buildFileItem = new ProjectBuildFile(fileItem);
                        _projectItems.Add(buildFileItem);
                        _sourcesBuildPhases[xCodeTargetName].Files.Add(buildFileItem);
                    }
                    else
                    {
                        _removableItems.Add(item);
                    }
                }
                else
                {
                    if (!item.Build)
                    {
                        // Headers not matching file restrictions : remove them from the solution.
                        RemoveFromFileSystem(item);
                    }
                }
            }
        }

        private void PrepareResourceFiles(string xCodeTargetName, IEnumerable<string> resourceFiles, Project project, Project.Configuration configuration, string workspacePath = null)
        {
            foreach (string file in resourceFiles)
            {
                bool alreadyPresent;
                ProjectFileSystemItem item = AddInFileSystem(file, out alreadyPresent, workspacePath);
                if (alreadyPresent)
                    continue;

                item.Build = true;
                item.Source = true;

                ProjectFile fileItem = (ProjectFile)item;
                ProjectBuildFile buildFileItem = new ProjectBuildFile(fileItem);
                _projectItems.Add(buildFileItem);
                _resourcesBuildPhases[xCodeTargetName].Files.Add(buildFileItem);
            }
        }

        private void PrepareExternalResourceFiles(string xCodeTargetName, Project project, Project.Configuration configuration)
        {
            Strings externalResourceFiles = Options.GetStrings<Options.XCode.Compiler.ExternalResourceFiles>(configuration);
            XCodeUtil.ResolveProjectPaths(project, externalResourceFiles);

            Strings externalResourceFolders = Options.GetStrings<Options.XCode.Compiler.ExternalResourceFolders>(configuration);
            XCodeUtil.ResolveProjectPaths(project, externalResourceFolders);

            Strings externalResourcePackages = Options.GetStrings<Options.XCode.Compiler.ExternalResourcePackages>(configuration);
            XCodeUtil.ResolveProjectPaths(project, externalResourcePackages);

            foreach (string externalResourcePackage in externalResourcePackages)
            {
                Directory.CreateDirectory(externalResourcePackage);
                externalResourceFiles.Add(externalResourcePackage);
            }

            foreach (string externalResourceFolder in externalResourceFolders)
            {
                Directory.CreateDirectory(externalResourceFolder);
                AddAllFiles(externalResourceFolder, externalResourceFiles);
            }

            string workspacePath = Directory.GetParent(configuration.ProjectFullFileNameWithExtension).FullName;
            PrepareResourceFiles(xCodeTargetName, externalResourceFiles, project, configuration, workspacePath);
        }

        private void AddAllFiles(string fullPath, Strings outputFiles)
        {
            outputFiles.Add(Util.DirectoryGetFiles(fullPath));
            foreach (string folderPath in Util.DirectoryGetDirectories(fullPath))
            {
                if (FolderAsFile(folderPath))
                    outputFiles.Add(folderPath);
                else
                    AddAllFiles(folderPath, outputFiles);
            }
        }

        private void SetRootGroup(Project project, Project.Configuration configuration)
        {
            _mainGroup = new ProjectFolder(project.GetType().Name, true);

            if (Options.GetObjects<Options.XCode.Compiler.SystemFrameworks>(configuration).Any() || Options.GetObjects<Options.XCode.Compiler.UserFrameworks>(configuration).Any())
            {
                _frameworksFolder = new ProjectFolder("Frameworks", true);
                _projectItems.Add(_frameworksFolder);
                _mainGroup.Children.Add(_frameworksFolder);
            }

            _projectItems.Add(_mainGroup);

            string workspacePath = Directory.GetParent(configuration.ProjectFullFileNameWithExtension).FullName;
            string sourceRootPath = project.SourceRootPath;
            ProjectFolder rootGroup = new ProjectFolder(sourceRootPath, Util.PathGetRelative(workspacePath, sourceRootPath));
            _projectItems.Add(rootGroup);

            _productsGroup = new ProjectFolder("Products", true);
            _mainGroup.Children.Add(rootGroup);
            _mainGroup.Children.Add(_productsGroup);
            _projectItems.Add(_productsGroup);
        }

        private void Write(string value, TextWriter writer, Resolver resolver)
        {
            string resolvedValue = resolver.Resolve(value);
            StringReader reader = new StringReader(resolvedValue);
            string str = reader.ReadToEnd();
            writer.Write(str);
            writer.Flush();
        }

        private void WriteSection<ProjectItemType>(Project.Configuration configuration, IFileGenerator fileGenerator)
            where ProjectItemType : ProjectItem
        {
            IEnumerable<ProjectItem> projectItems = _projectItems.Where(item => item is ProjectItemType);
            if (projectItems.Any())
            {
                ProjectItem firstItem = projectItems.First();
                using (fileGenerator.Declare("item", firstItem))
                {
                    fileGenerator.Write(Template.SectionBegin);
                }

                Dictionary<string, string> resolverParameters = new Dictionary<string, string>();
                foreach (ProjectItemType item in projectItems)
                {
                    resolverParameters.Clear();
                    item.GetAdditionalResolverParameters(item, fileGenerator.Resolver, ref resolverParameters);
                    using (fileGenerator.Declare(resolverParameters.Select(p => new VariableAssignment(p.Key, p.Value)).ToArray()))
                    {
                        using (fileGenerator.Declare("item", item))
                        using (fileGenerator.Declare("options", configuration))
                        {
                            fileGenerator.Write(Template.Section[item.Section]);
                        }
                    }
                }

                using (fileGenerator.Declare("item", firstItem))
                {
                    fileGenerator.Write(Template.SectionEnd);
                }
            }
        }

        private ProjectFileSystemItem AddInFileSystem(string fullPath, out bool alreadyPresent, string workspacePath = null, bool applyWorkspaceOnlyToRoot = false)
        {
            // Search in existing roots.
            var fileSystemItems = _projectItems.Where(item => item is ProjectFileSystemItem);
            foreach (ProjectFileSystemItem item in fileSystemItems)
            {
                if (fullPath.StartsWith(item.FullPath, StringComparison.OrdinalIgnoreCase))
                {
                    if (fullPath.Length > item.FullPath.Length)
                        return AddInFileSystem(item, out alreadyPresent, fullPath.Substring(item.FullPath.Length + 1), applyWorkspaceOnlyToRoot ? null : workspacePath);
                }
            }

            // Not found in existing root, create a new root for this item.
            alreadyPresent = false;
            string parentDirectoryPath = Directory.GetParent(fullPath).FullName;
            //string fileName = fullPath.Substring(parentDirectoryPath.Length + 1);

            ProjectFolder folder = workspacePath != null ? new ProjectExternalFolder(parentDirectoryPath, workspacePath) : new ProjectFolder(parentDirectoryPath);
            _projectItems.Add(folder);
            _mainGroup.Children.Insert(0, folder);

            ProjectFile file = workspacePath != null ? new ProjectExternalFile(fullPath, workspacePath) : new ProjectFile(fullPath);
            _projectItems.Add(file);
            folder.Children.Add(file);

            return file;
        }

        private ProjectFileSystemItem AddInFileSystem(ProjectFileSystemItem parent, out bool alreadyPresent, string remainingPath, string workspacePath)
        {
            alreadyPresent = false;

            string[] remainingPathParts = remainingPath.Split(FolderSeparator);
            for (int i = 0; i < remainingPathParts.Length; i++)
            {
                bool found = false;
                string remainingPathPart = remainingPathParts[i];
                foreach (ProjectFileSystemItem item in parent.Children)
                {
                    if (remainingPathPart == item.Name)
                    {
                        parent = item;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    if (i == remainingPathParts.Length - 1)
                    {
                        string fullPath = parent.FullPath + FolderSeparator + remainingPathPart;
                        ProjectFile file = workspacePath != null ? new ProjectExternalFile(fullPath, workspacePath) : new ProjectFile(fullPath);
                        _projectItems.Add(file);
                        parent.Children.Add(file);
                        return file;
                    }
                    else
                    {
                        string fullPath = parent.FullPath + FolderSeparator + remainingPathPart;
                        ProjectFolder folder = workspacePath != null ? new ProjectExternalFolder(fullPath, workspacePath) : new ProjectFolder(fullPath);
                        _projectItems.Add(folder);
                        parent.Children.Add(folder);
                        parent = folder;
                    }
                }
                else if (i == remainingPathParts.Length - 1)
                {
                    alreadyPresent = true;
                }
            }
            parent.Children.Sort((f1, f2) => string.Compare(f1.Name, f2.Name, StringComparison.OrdinalIgnoreCase));
            return parent;
        }

        private void RemoveFromFileSystem(ProjectFileSystemItem fileSystemItem)
        {
            ProjectFileSystemItem itemToSearch = fileSystemItem;
            while (itemToSearch != null)
            {
                ProjectFileSystemItem parentItemToSearch = _projectItems.Where(item => item is ProjectFileSystemItem).Cast<ProjectFileSystemItem>().FirstOrDefault(item => item.Children.Contains(itemToSearch));
                if (parentItemToSearch == null)
                    break;

                parentItemToSearch.Children.Remove(itemToSearch);
                if (parentItemToSearch.Children.Count != 0)
                    break;

                itemToSearch = parentItemToSearch;
                _projectItems.Remove(itemToSearch);
            }

            _projectItems.Remove(fileSystemItem);
        }

        private bool FolderAsFile(string fullPath)
        {
            string extension = Path.GetExtension(fullPath);
            switch (extension)
            {
                case ".bundle":
                    return true;
            }

            return false;
        }

        private Options.ExplicitOptions GenerateOptions(XCodeGenerationContext context)
        {
            var project = context.Project;
            var conf = context.Configuration;
            var options = new Options.ExplicitOptions();
            context.Options = options;

            options["TargetName"] = XCodeUtil.XCodeFormatSingleItem(conf.Target.Name);

            // TODO: really not ideal, refactor and move the properties we need from it someplace else
            var platformVcxproj = PlatformRegistry.Query<VisualStudio.IPlatformVcxproj>(context.Configuration.Platform);

            FillIncludeDirectoriesOptions(context, platformVcxproj);
            FillCompilerOptions(context, platformVcxproj);
            SelectAdditionalLibraryDirectoriesOption(context, platformVcxproj);

            context.Options["GenerateMapFile"] = RemoveLineTag;
            platformVcxproj.SelectLinkerOptions(context);

            var libFiles = new OrderableStrings(conf.LibraryFiles);
            libFiles.AddRange(conf.DependenciesBuiltTargetsLibraryFiles);
            libFiles.AddRange(conf.DependenciesOtherLibraryFiles);
            libFiles.Sort();

            var linkerOptions = new Strings(conf.AdditionalLinkerOptions);

            // TODO: make this an option
            linkerOptions.Add("-ObjC");

            // TODO: fix this to use proper lib prefixing
            linkerOptions.AddRange(libFiles.Select(library => "-l" + library));

            // TODO: when the above is fixed, we won't need this anymore
            if (conf.Output == Project.Configuration.OutputType.Dll)
                options["ExecutablePrefix"] = "lib";
            else
                options["ExecutablePrefix"] = RemoveLineTag;

            if (conf.DefaultOption == Options.DefaultTarget.Debug)
                conf.Defines.Add("_DEBUG");
            else // Release
                conf.Defines.Add("NDEBUG");

            options["PreprocessorDefinitions"] = XCodeUtil.XCodeFormatList(conf.Defines, 4, forceQuotes: true);
            options["CompilerOptions"] = XCodeUtil.XCodeFormatList(conf.AdditionalCompilerOptions, 4, forceQuotes: true);
            if (conf.AdditionalLibrarianOptions.Any())
                throw new NotImplementedException(nameof(conf.AdditionalLibrarianOptions) + " not supported with XCode generator");
            options["LinkerOptions"] = XCodeUtil.XCodeFormatList(linkerOptions, 4, forceQuotes: true);
            return options;
        }

        private static class XCodeProjIdGenerator
        {
            private static System.Security.Cryptography.SHA1CryptoServiceProvider s_cryptoProvider;
            private static Dictionary<ProjectItem, string> s_hashRepository;
            private static object s_lockMyself = new object();

            static XCodeProjIdGenerator()
            {
                s_cryptoProvider = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                s_hashRepository = new Dictionary<ProjectItem, string>();
            }

            public static string GetXCodeId(ProjectItem item)
            {
                lock (s_hashRepository)
                {
                    if (s_hashRepository.ContainsKey(item))
                        return s_hashRepository[item];

                    byte[] stringbytes = Encoding.UTF8.GetBytes(item.ToString());
                    byte[] hashedBytes = null;
                    lock (s_lockMyself)
                    {
                        hashedBytes = s_cryptoProvider.ComputeHash(stringbytes);
                    }
                    Array.Resize(ref hashedBytes, 16);

                    string guidString = new Guid(hashedBytes).ToString("N").ToUpper().Substring(7, 24);
                    s_hashRepository[item] = guidString;
                    return guidString;
                }
            }
        }

        public enum ItemSection
        {
            PBXBuildFile,
            PBXContainerItemProxy,
            PBXFileReference,
            PBXFrameworksBuildPhase,
            PBXGroup,
            PBXNativeTarget,
            PBXLegacyTarget,
            PBXProject,
            PBXReferenceProxy,
            PBXResourcesBuildPhase,
            PBXSourcesBuildPhase,
            PBXVariantGroup,
            PBXTargetDependency,
            XCBuildConfiguration_NativeTarget,
            XCBuildConfiguration_LegacyTarget,
            XCBuildConfiguration_UnitTestTarget,
            XCBuildConfiguration_Project,
            XCConfigurationList,
            PBXShellScriptBuildPhase
        }

        public abstract class ProjectItem : IEquatable<ProjectItem>, IComparable<ProjectItem>
        {
            private ItemSection _section;
            private string _identifier;
            private string _internalIdentifier;
            private int _hashCode;
            private string _uid;

            public ProjectItem(ItemSection section, string identifier)
            {
                _section = section;
                _identifier = identifier;
                _internalIdentifier = section.ToString() + "/" + Identifier;
                _hashCode = _internalIdentifier.GetHashCode();
                _uid = XCodeProjIdGenerator.GetXCodeId(this);
            }

            public ItemSection Section { get { return _section; } }
            public string SectionString { get { return _section.ToString(); } }
            public string Identifier { get { return _identifier; } }
            public string Uid { get { return _uid; } }

            public virtual void GetAdditionalResolverParameters(ProjectItem item, Resolver resolver, ref Dictionary<string, string> resolverParameters)
            {
            }

            public override bool Equals(object obj)
            {
                if (!(obj is ProjectItem))
                    return false;
                return Equals((ProjectItem)obj);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override string ToString()
            {
                return _internalIdentifier;
            }

            public bool Equals(ProjectItem other)
            {
                return _hashCode == other._hashCode;
            }

            public int CompareTo(ProjectItem other)
            {
                int compare = Section.CompareTo(other.Section);
                if (compare != 0)
                    return compare;
                return CompareToInternal(other);
            }

            protected virtual int CompareToInternal(ProjectItem other)
            {
                return string.Compare(Identifier, other.Identifier, StringComparison.Ordinal);
            }

            public static bool operator ==(ProjectItem left, ProjectItem right)
            {
                return Object.Equals(left, right);
            }

            public static bool operator !=(ProjectItem left, ProjectItem right)
            {
                return !(left == right);
            }
        }

        private static class EnumExtensions
        {
            private static ConcurrentDictionary<Enum, string> s_enumToStringMapping = new ConcurrentDictionary<Enum, string>();

            public static string EnumToString(Enum enumValue)
            {
                string stringValue;

                if (s_enumToStringMapping.TryGetValue(enumValue, out stringValue))
                    return stringValue;

                stringValue = enumValue.ToString();
                MemberInfo[] memberInfo = enumValue.GetType().GetMember(stringValue);
                if (memberInfo != null && memberInfo.Length > 0)
                {
                    object[] attributes = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                    if (attributes != null && attributes.Length > 0)
                        stringValue = ((System.ComponentModel.DescriptionAttribute)attributes[0]).Description;
                }

                s_enumToStringMapping.TryAdd(enumValue, stringValue);
                return stringValue;
            }
        }

        private abstract class ProjectFileSystemItem : ProjectItem
        {
            public enum SourceTreeSetting
            {
                [Description("\"<group>\"")]
                GROUP,
                SOURCE_ROOT,
                SDKROOT,
                BUILT_PRODUCTS_DIR,
                DEVELOPER_DIR
            }

            protected ProjectFileSystemItem(ItemSection section, string fullPath)
                : base(section, fullPath)
            {
                Children = new List<ProjectFileSystemItem>();
                FullPath = fullPath;
                Name = fullPath.Substring(fullPath.LastIndexOf(FolderSeparator) + 1);
            }

            public List<ProjectFileSystemItem> Children { get; protected set; }
            public abstract bool Build { get; set; }
            public abstract bool Source { get; set; }
            public string FullPath { get; protected set; }
            public string FileName => System.IO.Path.GetFileName(FullPath);
            public virtual string Name { get; protected set; }
            public virtual string Path
            {
                get { return Name; }
                protected set { Name = value; }
            }
            public string Type => Source ? "Sources" : "Frameworks";
            public abstract string Extension { get; }
            public string SourceTree => EnumExtensions.EnumToString(SourceTreeValue);
            public abstract SourceTreeSetting SourceTreeValue { get; }
        }

        private abstract class ProjectFileBase : ProjectFileSystemItem
        {
            private bool _build = false;
            private bool _source = false;
            private string _extension;
            private string _fileType;
            private string _explicitFileType;
            private string _includeInIndex;

            protected ProjectFileBase(ItemSection section, string fullPath)
                : base(section, fullPath)
            {
                _extension = System.IO.Path.GetExtension(FullPath);

                _fileType = GetFileType();
                _explicitFileType = RemoveLineTag;
                _includeInIndex = RemoveLineTag;

                if (_fileType == "archive.ar")
                {
                    _explicitFileType = _fileType;
                    _fileType = RemoveLineTag;
                    _includeInIndex = "0";
                }
            }

            protected string GetFileType()
            {
                switch (_extension)
                {
                    case "": return "\"compiled.mach-o.executable\"";
                    case ".c": return "sourcecode.c.c";
                    case ".cpp": return "sourcecode.cpp.cpp";
                    case ".h": return "sourcecode.c.h";
                    case ".hpp": return "sourcecode.c.h";
                    case ".s": return "sourcecode.asm";
                    case ".m": return "sourcecode.c.objc";
                    case ".j": return "sourcecode.c.objc";
                    case ".mm": return "sourcecode.cpp.objcpp";

                    case ".xcodeproj": return "\"wrapper.pb-project\"";
                    case ".framework": return "wrapper.framework";
                    case ".bundle": return "\"wrapper.plug-in\"";
                    case ".nib": return "wrapper.nib";
                    case ".app": return "wrapper.application";
                    case ".xctest": return "wrapper.cfbundle";
                    case ".dylib": return "\"compiled.mach-o.dylib\"";

                    case ".txt": return "text";
                    case ".plist": return "text.plist.xml";
                    case ".ico": return "text";
                    case ".rtf": return "text.rtf";
                    case ".strings": return "text.plist.strings";
                    case ".json": return "text.json";

                    case ".a": return "archive.ar";

                    case ".png": return "image.png";
                    case ".tiff": return "image.tiff";

                    case ".ipk": return "file.ipk";
                    case ".pem": return "file.pem";
                    case ".loc8": return "file.loc8";
                    case ".metapreload": return "file.metapreload";
                    case ".gf": return "file.gf";
                    case ".xib": return "file.xib";
                }

                return "\"?\"";
            }

            public override string Extension { get { return _extension; } }
            public override bool Build { get { return _build; } set { _build = value; } }
            public override bool Source { get { return _source; } set { _source = value; } }
            public string FileType { get { return _fileType; } protected set { _fileType = value; } }
            public string ExplicitFileType { get { return _explicitFileType; } }
            public string IncludeInIndex { get { return _includeInIndex; } }
        }

        private class ProjectFile : ProjectFileBase
        {
            public ProjectFile(ItemSection section, string fullPath)
                : base(section, fullPath)
            {
            }

            public ProjectFile(string fullPath)
                : this(ItemSection.PBXFileReference, fullPath)
            {
            }

            public override string Path => FullPath;

            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.GROUP; } }
        }

        private class ProjectExternalFile : ProjectFile
        {
            private string _relativePath;

            public ProjectExternalFile(ItemSection section, string fullPath, string workspacePath)
                : base(section, fullPath)
            {
                _relativePath = Util.PathGetRelative(workspacePath, fullPath);
            }

            public ProjectExternalFile(string fullPath, string workspacePath)
                : this(ItemSection.PBXFileReference, fullPath, workspacePath)
            {
            }

            public override ProjectFileSystemItem.SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.SOURCE_ROOT; } }
            public override string Path { get { return _relativePath; } }
        }

        private class ProjectReferenceProxy : ProjectFileBase
        {
            private ProjectContainerProxy _proxy;
            private ProjectFile _outputFile;

            public ProjectReferenceProxy(ProjectReference projectReference, ProjectContainerProxy proxy, ProjectFile outputFile)
                : base(ItemSection.PBXReferenceProxy, "PROXY" + FolderSeparator + projectReference.Name)
            {
                FileType = "archive.ar";
                _outputFile = outputFile;
                _proxy = proxy;
            }

            public override string Extension { get { return ".a"; } }
            public ProjectContainerProxy Proxy { get { return _proxy; } }
            public ProjectFile OutputFile { get { return _outputFile; } }
            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.BUILT_PRODUCTS_DIR; } }
        }

        private class ProjectReference : ProjectFile
        {
            public ProjectReference(string fullPath)
                : base(fullPath)
            {
                ProjectName = Name.Substring(0, Name.LastIndexOf('.'));
            }

            public ProjectReference(ItemSection itemSection, string identifier)
                : base(ItemSection.PBXProject, identifier)
            {
                ProjectName = identifier;
            }

            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.SOURCE_ROOT; } }
            public string ProjectName { get; }
        }

        private class ProjectOutputFile : ProjectFile
        {
            private Project.Configuration _conf;

            public ProjectOutputFile(string fullPath)
                : base(fullPath)
            {
            }

            public ProjectOutputFile(Project.Configuration conf, string name = null)
                : this(((conf.Output == Project.Configuration.OutputType.Lib) ? conf.TargetLibraryPath : conf.TargetPath) + FolderSeparator + GetFilePrefix(conf.Output) + conf.TargetFileFullName + GetFileExtension(conf))
            {
                Name = name ?? conf.Project.Name + " " + conf.Name;
                BuildableName = System.IO.Path.GetFileName(FullPath);
                _conf = conf;
            }

            private static string GetFilePrefix(Project.Configuration.OutputType outputType)
            {
                return outputType.HasAnyFlag(Project.Configuration.OutputType.Lib | Project.Configuration.OutputType.Dll) ? "lib" : "";
            }

            public static string GetFileExtension(Project.Configuration conf)
            {
                switch (conf.Output)
                {
                    case Project.Configuration.OutputType.Dll:
                        return ".dylib";
                    case Project.Configuration.OutputType.Lib:
                        return ".a";
                    case Project.Configuration.OutputType.Exe:
                        return ""; // Mac executable
                    case Project.Configuration.OutputType.IosApp:
                        return ".app";
                    case Project.Configuration.OutputType.IosTestBundle:
                        return ".xctest";
                    case Project.Configuration.OutputType.None:
                        return "";
                    default:
                        throw new NotSupportedException($"XCode generator doesn't handle {conf.Output}");
                }
            }

            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.BUILT_PRODUCTS_DIR; } }

            public Project.Configuration.OutputType OutputType { get { return _conf.Output; } }

            public string BuildableName { get; }
        }

        private abstract class ProjectFrameworkFile : ProjectFile
        {
            public ProjectFrameworkFile(string fullPath)
                : base(fullPath)
            {
            }
        }

        private class ProjectSystemFrameworkFile : ProjectFrameworkFile
        {
            private static readonly string s_frameworkPath = "System" + FolderSeparator + "Library" + FolderSeparator + "Frameworks" + FolderSeparator;
            private const string FrameworkExtension = ".framework";

            public ProjectSystemFrameworkFile(string frameworkFileName)
                : base(s_frameworkPath + frameworkFileName + FrameworkExtension)
            {
            }

            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.SDKROOT; } }
            public override string Path { get { return FullPath; } }
        }

        private class ProjectDeveloperFrameworkFile : ProjectFrameworkFile
        {
            private static readonly string s_frameworkPath = ".." + FolderSeparator + ".." + FolderSeparator
                + "Library" + FolderSeparator
                + "Frameworks" + FolderSeparator;
            private const string FrameworkExtension = ".framework";

            public ProjectDeveloperFrameworkFile(string frameworkFileName)
                : base(s_frameworkPath + frameworkFileName + FrameworkExtension)
            {
            }

            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.SDKROOT; } }
            public override string Path { get { return FullPath; } }
        }

        private class ProjectUserFrameworkFile : ProjectFrameworkFile
        {
            private string _relativePath;

            public ProjectUserFrameworkFile(string frameworkFullPath, string workspacePath)
                : base(frameworkFullPath)
            {
                _relativePath = Util.PathGetRelative(workspacePath, frameworkFullPath);
            }

            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.SOURCE_ROOT; } }
            public override string Path { get { return _relativePath; } }
        }

        private class ProjectFolder : ProjectFileSystemItem
        {
            public ProjectFolder(string fullPath, bool removePathLine = false)
                : base(ItemSection.PBXGroup, fullPath)
            {
                Path = removePathLine ? RemoveLineTag : Name;
            }

            public ProjectFolder(string identifier, string fullPath)
                : base(ItemSection.PBXGroup, identifier)
            {
                Path = fullPath;
            }

            public override bool Build { get { return false; } set { throw new NotSupportedException(); } }
            public override bool Source { get { return false; } set { throw new NotSupportedException(); } }
            public override string Extension { get { throw new NotSupportedException(); } }
            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.GROUP; } }
            public override string Path { get; protected set; }

            public override void GetAdditionalResolverParameters(ProjectItem item, Resolver resolver, ref Dictionary<string, string> resolverParameters)
            {
                ProjectFolder folderItem = (ProjectFolder)item;
                string childrenList = "";
                foreach (ProjectFileSystemItem childItem in folderItem.Children)
                {
                    using (resolver.NewScopedParameter("item", childItem))
                    {
                        childrenList += resolver.Resolve(Template.SectionSubItem);
                    }
                }

                resolverParameters.Add("itemChildren", childrenList);
            }
        }

        private class ProjectExternalFolder : ProjectFolder
        {
            public ProjectExternalFolder(string fullPath, string workspacePath)
                : base(fullPath)
            {
                Path = Util.PathGetRelative(workspacePath, fullPath);
            }

            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.SOURCE_ROOT; } }
        }

        private class ProjectProductsFolder : ProjectFolder
        {
            public ProjectProductsFolder(string fullPath)
                : base("PRODUCTS" + FolderSeparator + fullPath)
            {
            }

            public override string Name { get { return "Products"; } }
            public override string Path { get { return RemoveLineTag; } }
        }

        private class ProjectBuildFile : ProjectItem
        {
            public ProjectBuildFile(ProjectFileBase file) : base(ItemSection.PBXBuildFile, file.Name)
            {
                File = file;
            }

            public ProjectFileBase File { get; }
        }

        private abstract class ProjectBuildPhase : ProjectItem
        {
            public ProjectBuildPhase(ItemSection section, string phaseName, uint buildActionMask)
                : base(section, phaseName)
            {
                Files = new List<ProjectBuildFile>();
                BuildActionMask = buildActionMask;
                RunOnlyForDeploymentPostprocessing = 0;
            }

            public override void GetAdditionalResolverParameters(ProjectItem item, Resolver resolver, ref Dictionary<string, string> resolverParameters)
            {
                ProjectBuildPhase folderItem = (ProjectBuildPhase)item;
                string childrenList = "";
                foreach (ProjectBuildFile childItem in folderItem.Files)
                {
                    using (resolver.NewScopedParameter("item", childItem))
                    {
                        childrenList += resolver.Resolve(Template.SectionSubItem);
                    }
                }

                resolverParameters.Add("itemChildren", childrenList);
            }

            public List<ProjectBuildFile> Files { get; }
            public uint BuildActionMask { get; } = 0;
            public int RunOnlyForDeploymentPostprocessing { get; }
        }

        private class ProjectResourcesBuildPhase : ProjectBuildPhase
        {
            public ProjectResourcesBuildPhase(uint buildActionMask)
                : base(ItemSection.PBXResourcesBuildPhase, "Resources", buildActionMask)
            {
            }

            public ProjectResourcesBuildPhase(string name, uint buildActionMask)
                : base(ItemSection.PBXResourcesBuildPhase, name, buildActionMask)
            {
            }
        }

        private class ProjectSourcesBuildPhase : ProjectBuildPhase
        {
            public ProjectSourcesBuildPhase(uint buildActionMask)
                : base(ItemSection.PBXSourcesBuildPhase, "Sources", buildActionMask)
            {
            }

            public ProjectSourcesBuildPhase(string name, uint buildActionMask)
                : base(ItemSection.PBXSourcesBuildPhase, name, buildActionMask)
            {
            }
        }

        private class ProjectFrameworksBuildPhase : ProjectBuildPhase
        {
            public ProjectFrameworksBuildPhase(uint buildActionMask)
                : base(ItemSection.PBXFrameworksBuildPhase, "Frameworks", buildActionMask)
            {
            }

            public ProjectFrameworksBuildPhase(string name, uint buildActionMask)
                : base(ItemSection.PBXFrameworksBuildPhase, name, buildActionMask)
            {
            }
        }

        private class ProjectShellScriptBuildPhase : ProjectBuildPhase
        {
            public class EqualityComparer : IEqualityComparer<ProjectShellScriptBuildPhase>
            {
                public bool Equals(ProjectShellScriptBuildPhase x, ProjectShellScriptBuildPhase y)
                {
                    return x.script == y.script;
                }

                public int GetHashCode(ProjectShellScriptBuildPhase obj)
                {
                    return obj.script.GetHashCode();
                }
            }

            public String script;

            public ProjectShellScriptBuildPhase(uint buildActionMask)
                : base(ItemSection.PBXShellScriptBuildPhase, "ShellScrips", buildActionMask)
            {
            }

            public ProjectShellScriptBuildPhase(string name, uint buildActionMask)
                : base(ItemSection.PBXShellScriptBuildPhase, name, buildActionMask)
            {
            }
        }

        private class ProjectVariantGroup : ProjectItem
        {
            public ProjectVariantGroup() : base(ItemSection.PBXVariantGroup, "") { }
        }

        private class ProjectContainerProxy : ProjectItem
        {
            public enum Type
            {
                Target = 1,
                Archive = 2,
            }

            private Type _proxyType;
            private ProjectItem _proxyItem;
            private ProjectReference _projectReference;

            public ProjectContainerProxy(ProjectReference projectReference, ProjectItem proxyItem, Type proxyType)
                : base(ItemSection.PBXContainerItemProxy, "PBXContainerItemProxy for " + projectReference.Name + " - " + proxyType.ToString())
            {
                _proxyType = proxyType;
                _proxyItem = proxyItem;
                _projectReference = projectReference;
            }

            public ProjectItem ProxyItem { get { return _proxyItem; } }
            public int ProxyType { get { return (int)_proxyType; } }
            public ProjectReference ProjectReference { get { return _projectReference; } }
        }

        private class ProjectTargetDependency : ProjectItem
        {
            private ProjectContainerProxy _proxy;
            private ProjectReference _projectReference;
            private ProjectNativeTarget _target;

            public ProjectTargetDependency(ProjectReference projectReference, ProjectContainerProxy proxy)
                : base(ItemSection.PBXTargetDependency, projectReference.Name)
            {
                _proxy = proxy;
                _projectReference = projectReference;
                _target = null;
            }

            public ProjectTargetDependency(ProjectReference projectReference, ProjectContainerProxy proxy, ProjectNativeTarget target)
                : base(ItemSection.PBXTargetDependency, projectReference.Name)
            {
                _proxy = proxy;
                _projectReference = projectReference;
                _target = target;
            }

            public ProjectNativeTarget NativeTarget { get { return _target; } }
            public ProjectContainerProxy Proxy { get { return _proxy; } }
            public ProjectReference ProjectReference { get { return _projectReference; } }
            public String TargetIdentifier
            {
                get
                {
                    if (_target != null)
                        return _target.Uid;
                    else
                        return RemoveLineTag;
                }
            }
        }

        private abstract class ProjectTarget : ProjectItem
        {
            public ProjectTarget(ItemSection section, string identifier)
                : base(section, identifier)
            {
                // Only for Uid computation.
                OutputFile = null;
            }

            public ProjectTarget(ItemSection section, Project project)
                : base(section, project.Name)
            {
                // Only for Uid computation.
                OutputFile = null;
            }

            public ProjectTarget(ItemSection section, string identifier, ProjectOutputFile outputFile, ProjectConfigurationList configurationList)
                : base(section, identifier)
            {
                ConfigurationList = configurationList;
                OutputFile = outputFile;
                switch (OutputFile.OutputType)
                {
                    case Project.Configuration.OutputType.Dll:
                        ProductType = "com.apple.product-type.library.dynamic";
                        ProductInstallPath = RemoveLineTag;
                        break;
                    case Project.Configuration.OutputType.Lib:
                        ProductType = "com.apple.product-type.library.static";
                        ProductInstallPath = RemoveLineTag;
                        break;
                    case Project.Configuration.OutputType.IosTestBundle:
                        ProductType = "com.apple.product-type.bundle.unit-test";
                        ProductInstallPath = "$(HOME)/Applications";
                        break;
                    case Project.Configuration.OutputType.IosApp:
                        ProductType = "com.apple.product-type.application";
                        ProductInstallPath = "$(HOME)/Applications";
                        break;
                    case Project.Configuration.OutputType.Exe:
                    case Project.Configuration.OutputType.None:
                    case Project.Configuration.OutputType.Utility:
                        ProductType = "com.apple.product-type.tool";
                        ProductInstallPath = RemoveLineTag;
                        break;
                    default:
                        throw new NotSupportedException($"XCode generator doesn't handle {OutputFile.OutputType}");
                }
            }

            public ProjectResourcesBuildPhase ResourcesBuildPhase { get; set; }
            public ProjectSourcesBuildPhase SourcesBuildPhase { get; set; }
            public String SourceBuildPhaseUID { get { return SourcesBuildPhase?.Uid ?? RemoveLineTag; } }
            public ProjectFrameworksBuildPhase FrameworksBuildPhase { get; set; }
            public UniqueList<ProjectShellScriptBuildPhase> ShellScriptPreBuildPhases { get; set; }
            public UniqueList<ProjectShellScriptBuildPhase> ShellScriptPostBuildPhases { get; set; }
            public String ShellScriptPreBuildPhaseUIDs
            {
                get
                {
                    if (ShellScriptPreBuildPhases != null && ShellScriptPreBuildPhases.Any())
                        return string.Join(",", ShellScriptPreBuildPhases.Select(buildEvent => buildEvent.Uid));

                    return RemoveLineTag;
                }
            }
            public String ShellScriptPostBuildPhaseUIDs
            {
                get
                {
                    if (ShellScriptPostBuildPhases != null && ShellScriptPostBuildPhases.Any())
                        return string.Join(",", ShellScriptPostBuildPhases.Select(buildEvent => buildEvent.Uid));

                    return RemoveLineTag;
                }
            }
            public ProjectOutputFile OutputFile { get; }
            public string ProductType { get; }
            public ProjectConfigurationList ConfigurationList { get; }
            public string ProductInstallPath { get; }
        }

        private class ProjectNativeTarget : ProjectTarget
        {
            public ProjectNativeTarget(string identifier)
                : base(ItemSection.PBXNativeTarget, identifier)
            { }

            public ProjectNativeTarget(Project project)
                : base(ItemSection.PBXNativeTarget, project)
            { }

            public ProjectNativeTarget(string identifier, ProjectOutputFile outputFile, ProjectConfigurationList configurationList, List<ProjectTargetDependency> dependencies)
                : base(ItemSection.PBXNativeTarget, identifier, outputFile, configurationList)
            {
                Dependencies = dependencies;
            }

            public override void GetAdditionalResolverParameters(ProjectItem item, Resolver resolver, ref Dictionary<string, string> resolverParameters)
            {
                if (null == OutputFile)
                    throw new Error("Trying to compute dependencies on incomplete native target. ");

                ProjectNativeTarget folderItem = (ProjectNativeTarget)item;
                string childrenList = "";
                foreach (ProjectTargetDependency childItem in folderItem.Dependencies)
                {
                    using (resolver.NewScopedParameter("item", childItem))
                    {
                        childrenList += resolver.Resolve(Template.SectionSubItem);
                    }
                }

                resolverParameters.Add("itemChildren", childrenList);
            }

            public List<ProjectTargetDependency> Dependencies { get; }
        }

        private class ProjectLegacyTarget : ProjectTarget
        {
            private string _masterBffFilePath;

            public ProjectLegacyTarget(string identifier, ProjectOutputFile outputFile, ProjectConfigurationList configurationList, string masterBffFilePath)
                : base(ItemSection.PBXLegacyTarget, identifier, outputFile, configurationList)
            {
                _masterBffFilePath = masterBffFilePath;
            }

            public string BuildArgumentsString
            {
                get
                {
                    var fastBuildCommandLineOptions = new List<string>();

                    fastBuildCommandLineOptions.Add("$(FASTBUILD_TARGET)"); // special envvar hardcoded in the template

                    if (FastBuildSettings.FastBuildUseIDE)
                        fastBuildCommandLineOptions.Add("-ide");

                    if (FastBuildSettings.FastBuildReport)
                        fastBuildCommandLineOptions.Add("-report");

                    if (FastBuildSettings.FastBuildNoSummaryOnError)
                        fastBuildCommandLineOptions.Add("-nosummaryonerror");

                    if (FastBuildSettings.FastBuildSummary)
                        fastBuildCommandLineOptions.Add("-summary");

                    if (FastBuildSettings.FastBuildVerbose)
                        fastBuildCommandLineOptions.Add("-verbose");

                    if (FastBuildSettings.FastBuildMonitor)
                        fastBuildCommandLineOptions.Add("-monitor");

                    if (FastBuildSettings.FastBuildWait)
                        fastBuildCommandLineOptions.Add("-wait");

                    if (FastBuildSettings.FastBuildNoStopOnError)
                        fastBuildCommandLineOptions.Add("-nostoponerror");

                    if (FastBuildSettings.FastBuildFastCancel)
                        fastBuildCommandLineOptions.Add("-fastcancel");

                    if (FastBuildSettings.FastBuildNoUnity)
                        fastBuildCommandLineOptions.Add("-nounity");

                    fastBuildCommandLineOptions.Add("-config " + Path.GetFileName(_masterBffFilePath));

                    return string.Join(" ", fastBuildCommandLineOptions);
                }
            }

            public string BuildToolPath
            {
                get
                {
                    return XCodeUtil.XCodeFormatSingleItem(Util.SimplifyPath(FastBuildSettings.FastBuildMakeCommand));
                }
            }

            public string BuildWorkingDirectory
            {
                get
                {
                    return XCodeUtil.XCodeFormatSingleItem(Path.GetDirectoryName(_masterBffFilePath));
                }
            }
        }

        private class ProjectBuildConfiguration : ProjectItem
        {
            public ProjectBuildConfiguration(ItemSection section, string configurationName, Project.Configuration configuration, Options.ExplicitOptions options)
                : base(section, configurationName)
            {
                Configuration = configuration;
                Options = options;
            }

            public Options.ExplicitOptions Options { get; }
            public Project.Configuration Configuration { get; }
            public string Optimization { get { return Configuration.Target.Name; } }
        }

        private class ProjectBuildConfigurationForTarget : ProjectBuildConfiguration
        {
            public ProjectBuildConfigurationForTarget(ItemSection section, Project.Configuration configuration, ProjectTarget target, Options.ExplicitOptions options)
                : base(section, configuration.Target.Name, configuration, options)
            {
                Target = target;
            }

            public ProjectTarget Target { get; }
        }

        private class ProjectBuildConfigurationForNativeTarget : ProjectBuildConfigurationForTarget
        {
            public ProjectBuildConfigurationForNativeTarget(Project.Configuration configuration, ProjectNativeTarget nativeTarget, Options.ExplicitOptions options)
                : base(ItemSection.XCBuildConfiguration_NativeTarget, configuration, nativeTarget, options)
            { }
        }

        private class ProjectBuildConfigurationForLegacyTarget : ProjectBuildConfigurationForTarget
        {
            public ProjectBuildConfigurationForLegacyTarget(Project.Configuration configuration, ProjectLegacyTarget legacyTarget, Options.ExplicitOptions options)
                : base(ItemSection.XCBuildConfiguration_LegacyTarget, configuration, legacyTarget, options)
            { }
        }

        private class ProjectBuildConfigurationForUnitTestTarget : ProjectBuildConfigurationForTarget
        {
            public ProjectBuildConfigurationForUnitTestTarget(Project.Configuration configuration, ProjectTarget target, Options.ExplicitOptions options)
                : base(ItemSection.XCBuildConfiguration_UnitTestTarget, configuration, target, options)
            { }

            public override void GetAdditionalResolverParameters(ProjectItem item, Resolver resolver, ref Dictionary<string, string> resolverParameters)
            {
                string testHostParam = RemoveLineTag;

                var nativeTarget = Target as ProjectNativeTarget;
                if (nativeTarget == null)
                    return;

                // Lookup for the app in the unit test dependencies.
                ProjectTargetDependency testHostTargetDependency =
                    nativeTarget.Dependencies.Find(dependency => dependency.NativeTarget != null && dependency.NativeTarget.OutputFile.OutputType == Project.Configuration.OutputType.IosApp);

                if (testHostTargetDependency != null)
                {
                    ProjectNativeTarget testHostTarget = testHostTargetDependency.NativeTarget;

                    // Each ProjectNativeTarget have a list of ProjectBuildConfiguration that wrap a Project.Configuration. 
                    // Here we look for the Project.Configuration in the ProjectBuildConfiguration list of the test host target (app) 
                    // that match the unit tests bundle ProjectBuildConfiguration.
                    Project.Configuration testConfig = testHostTarget.ConfigurationList.Configurations.First(config => config.Configuration.Name == this.Configuration.Name).Configuration;

                    testHostParam = String.Format("$(BUILT_PRODUCTS_DIR)/{0}{1}{2}/{0}{1}", testHostTarget.Identifier, testConfig.TargetFileSuffix, ProjectOutputFile.GetFileExtension(testConfig));
                }

                resolverParameters.Add("testHost", testHostParam);
            }
        }

        private class ProjectBuildConfigurationForProject : ProjectBuildConfiguration
        {
            public ProjectBuildConfigurationForProject(Project.Configuration configuration, Options.ExplicitOptions options)
                : base(ItemSection.XCBuildConfiguration_Project, configuration.Target.Name, configuration, options)
            { }
        }

        private class ProjectConfigurationList : ProjectItem
        {
            private HashSet<ProjectBuildConfiguration> _configurations;
            private ProjectItem _relatedItem;

            public ProjectConfigurationList(HashSet<ProjectBuildConfiguration> configurations, string configurationListName)
                : base(ItemSection.XCConfigurationList, configurationListName)
            {
                _configurations = configurations;
            }

            public override void GetAdditionalResolverParameters(ProjectItem item, Resolver resolver, ref Dictionary<string, string> resolverParameters)
            {
                ProjectConfigurationList configurationList = (ProjectConfigurationList)item;
                string childrenList = "";
                foreach (ProjectBuildConfiguration childItem in configurationList.Configurations)
                {
                    using (resolver.NewScopedParameter("item", childItem))
                    {
                        childrenList += resolver.Resolve(Template.SectionSubItem);
                    }
                }

                resolverParameters.Add("itemChildren", childrenList);
            }

            public HashSet<ProjectBuildConfiguration> Configurations { get { return _configurations; } }

            public ProjectBuildConfiguration DefaultConfiguration
            {
                get
                {
                    if (_configurations.Count != 0)
                    {
                        return _configurations.First();
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public string ConfigurationType
            {
                get
                {
                    if (_configurations.Count != 0)
                    {
                        return _configurations.First().SectionString;
                    }
                    else
                    {
                        return "";
                    }
                }
            }

            public ProjectItem RelatedItem { get { return _relatedItem; } set { _relatedItem = value; } }
        }

        private class ProjectMain : ProjectItem
        {
            private ProjectFolder _mainGroup;
            private ProjectTarget _target;
            private string _developmentTeam;
            private string _provisioningStyle;
            private ProjectConfigurationList _configurationList;
            private string _compatibilityVersion;
            private List<ProjectTarget> _targets;
            private Dictionary<ProjectFolder, ProjectReference> _projectReferences;
            private bool _iCloudSupport;

            public ProjectMain(string projectName, ProjectFolder mainGroup, ProjectConfigurationList configurationList, List<ProjectTarget> targets, bool iCloudSupport, string developmentTeam, string provisioningStyle)
                : base(ItemSection.PBXProject, projectName)
            {
                _target = null;
                _mainGroup = mainGroup;
                _developmentTeam = developmentTeam;
                _provisioningStyle = provisioningStyle;
                _configurationList = configurationList;
                _compatibilityVersion = "Xcode 3.2";
                _targets = targets;
                _projectReferences = new Dictionary<ProjectFolder, ProjectReference>();
                _iCloudSupport = iCloudSupport;
            }

            public ProjectMain(ProjectTarget target, ProjectFolder mainGroup, ProjectConfigurationList configurationList, bool iCloudSupport, string developmentTeam, string provisioningStyle)
                : base(ItemSection.PBXProject, target.Identifier)
            {
                _target = target;
                _mainGroup = mainGroup;
                _developmentTeam = developmentTeam;
                _provisioningStyle = provisioningStyle;
                _configurationList = configurationList;
                _compatibilityVersion = "Xcode 3.2";
                _targets = new List<ProjectTarget> { target };
                _projectReferences = new Dictionary<ProjectFolder, ProjectReference>();
                _iCloudSupport = iCloudSupport;
            }

            public void AddProjectDependency(ProjectFolder projectGroup, ProjectReference projectFile)
            {
                _projectReferences.Add(projectGroup, projectFile);
            }

            public void AddTarget(ProjectNativeTarget additionalTarget)
            {
                _targets.Add(additionalTarget);
            }

            public override void GetAdditionalResolverParameters(ProjectItem item, Resolver resolver, ref Dictionary<string, string> resolverParameters)
            {
                //ProjectMain mainItem = (ProjectMain)item;
                string targetList = "";
                foreach (ProjectTarget target in _targets)
                {
                    using (resolver.NewScopedParameter("item", target))
                    {
                        targetList += resolver.Resolve(Template.SectionSubItem);
                    }
                }
                resolverParameters.Add("itemTargets", targetList);

                string dependenciesList = "";
                foreach (KeyValuePair<ProjectFolder, ProjectReference> projectReference in _projectReferences)
                {
                    using (resolver.NewScopedParameter("group", projectReference.Key))
                    using (resolver.NewScopedParameter("project", projectReference.Value))
                    {
                        dependenciesList += resolver.Resolve(Template.ProjectReferenceSubItem);
                    }
                }
                resolverParameters.Add("itemProjectReferences", dependenciesList);

                string targetAttributes = "";
                foreach (ProjectTarget target in _targets)
                {
                    using (resolver.NewScopedParameter("item", target))
                    using (resolver.NewScopedParameter("project", this))
                    {
                        targetAttributes += resolver.Resolve(Template.ProjectTargetAttribute);
                    }
                }
                resolverParameters.Add("itemTargetAttributes", targetAttributes);
            }

            public ProjectTarget Target { get { return _target; } }
            public ProjectFolder MainGroup { get { return _mainGroup; } }
            public string DevelopmentTeam { get { return _developmentTeam; } }
            public string ProvisioningStyle { get { return _provisioningStyle; } }
            public ProjectConfigurationList ConfigurationList { get { return _configurationList; } }
            public string CompatibilityVersion { get { return _compatibilityVersion; } }
            public string ICloudSupport { get { return _iCloudSupport ? "1" : "0"; } }
        }
    }
}
