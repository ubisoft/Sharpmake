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

namespace Sharpmake.Generators.Apple
{
    public partial class XCodeProj : IProjectGenerator
    {
        private Builder _builder;
        public const string ProjectExtension = ".xcodeproj";
        private const string ProjectFileName = "project.pbxproj";
        private const string ProjectSchemeExtension = ".xcscheme";

        private const int ProjectArchiveVersion = 1;
        private const int ProjectObjectVersion = 46;

        public const string RemoveLineTag = FileGeneratorUtilities.RemoveLineTag;

        public static readonly char FolderSeparator;

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
        private Dictionary<string, List<ProjectShellScriptBuildPhase>> _shellScriptPreBuildPhases = null;
        private Dictionary<string, List<ProjectShellScriptBuildPhase>> _shellScriptPostBuildPhases = null;
        private Dictionary<string, List<ProjectTargetDependency>> _targetDependencies = null;

        private Dictionary<ProjectFolder, ProjectReference> _projectReferencesGroups = null;
        private ProjectMain _projectMain = null;

        private Dictionary<Project.Configuration, XCodeOptions> _optionMapping = null;

        // Unit Test Variables
        private string _unitTestFramework = "XCTest";

        static XCodeProj()
        {
            FolderSeparator = Util.UnixSeparator;
        }

        public void Generate(Builder builder, Project project, List<Project.Configuration> configurations, string projectFile, List<string> generatedFiles, List<string> skipFiles)
        {
            _builder = builder;

            PrepareSections(project, configurations);

            FileInfo fileInfo = new FileInfo(projectFile);
            string projectPath = fileInfo.Directory.FullName;
            string projectFileName = fileInfo.Name;

            bool updated;
            string projectFileResult = GenerateProject(project, configurations, projectPath, projectFileName, out updated);
            if (updated)
                generatedFiles.Add(projectFileResult);
            else
                skipFiles.Add(projectFileResult);

            string projectFileSchemeResult = GenerateProjectScheme(project, configurations, projectPath, projectFileName, out updated);
            if (updated)
                generatedFiles.Add(projectFileSchemeResult);
            else
                skipFiles.Add(projectFileSchemeResult);

            _builder = null;
        }

        private string GenerateProject(Project project, List<Project.Configuration> configurations, string projectPath, string projectFile, out bool updated)
        {
            // Create the target folder (solutions and projects are folders in XCode).
            string projectFolder = Util.GetCapitalizedPath(Path.Combine(projectPath, projectFile + ProjectExtension));
            Directory.CreateDirectory(projectFolder);

            string projectFilePath = Path.Combine(projectFolder, ProjectFileName);
            FileInfo projectFileInfo = new FileInfo(projectFilePath);

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

            // Write the solution file
            updated = _builder.Context.WriteGeneratedFile(project.GetType(), projectFileInfo, fileGenerator.ToMemoryStream());

            return projectFileInfo.FullName;
        }

        private string GenerateProjectScheme(Project project, List<Project.Configuration> configurations, string projectPath, string projectFile, out bool updated)
        {
            // Create the target folder (solutions and projects are folders in XCode).
            string projectSchemeFolder = Util.GetCapitalizedPath(Path.Combine(projectPath, projectFile + ProjectExtension, "xcshareddata", "xcschemes"));
            Directory.CreateDirectory(projectSchemeFolder);

            string projectSchemeFilePath = Path.Combine(projectSchemeFolder, projectFile + ProjectSchemeExtension);
            FileInfo projectSchemeFileInfo = new FileInfo(projectSchemeFilePath);

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
            
            XCodeOptions options = new XCodeOptions();
            Options.SelectOption(configurations[0],
                Options.Option(Options.XCode.Compiler.EnableGpuFrameCaptureMode.AutomaticallyEnable, () => options["EnableGpuFrameCaptureMode"] = RemoveLineTag),
                Options.Option(Options.XCode.Compiler.EnableGpuFrameCaptureMode.MetalOnly, () => options["EnableGpuFrameCaptureMode"] = "1"),
                Options.Option(Options.XCode.Compiler.EnableGpuFrameCaptureMode.OpenGLOnly, () => options["EnableGpuFrameCaptureMode"] = "2"),
                Options.Option(Options.XCode.Compiler.EnableGpuFrameCaptureMode.Disable, () => options["EnableGpuFrameCaptureMode"] = "3")
            );
            using (fileGenerator.Declare("projectFile", projectFile))
            using (fileGenerator.Declare("item", defaultTarget))
            using (fileGenerator.Declare("testableElements", testableElements))
            using (fileGenerator.Declare("optimization", configurations[0].Target.Name))
            {
                fileGenerator.Write(Template.SchemeFileTemplate);
            }

            // Remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();

            // Write the solution file
            updated = _builder.Context.WriteGeneratedFile(project.GetType(), projectSchemeFileInfo, fileGenerator.ToMemoryStream());

            return projectSchemeFileInfo.FullName;
        }

        private void PrepareSections(Project project, List<Project.Configuration> configurations)
        {
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
            _optionMapping = new Dictionary<Project.Configuration, XCodeOptions>();
            foreach (Project.Configuration configuration in configurations)
            {
                _optionMapping[configuration] = GenerateOptions(project, configuration);
                
                Strings assetCatalog = Options.GetStrings<Options.XCode.Compiler.AssetCatalog>(configuration);
                XCodeOptions.ResolveProjectPaths(project, assetCatalog);
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
            _shellScriptPreBuildPhases = new Dictionary<string, List<ProjectShellScriptBuildPhase>>();
            _shellScriptPostBuildPhases = new Dictionary<string, List<ProjectShellScriptBuildPhase>>();

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
                        PrepareSourceFiles(xCodeTargetName, projectFiles, project, conf, workspacePath);
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
                        var userFrameworkItem = new ProjectUserFrameworkFile(XCodeOptions.ResolveProjectPaths(project, userFramework), workspacePath);
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
                    XCodeOptions options = _optionMapping[targetConf];
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
                XCodeOptions options = _optionMapping[configuration];

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

        public static string XCodeFormatSingleItem(string item)
        {
            if (item.Contains(' '))
                return $"{Util.DoubleQuotes}{Util.EscapedDoubleQuotes}{item}{Util.EscapedDoubleQuotes}{Util.DoubleQuotes}";
            return $"{item}";
        }

        public static string XCodeFormatList(IEnumerable<string> items, int nbIndent)
        {
            int nbItems = items.Count();
            if (nbItems == 0)
                return FileGeneratorUtilities.RemoveLineTag;

            if (nbItems == 1)
                return XCodeFormatSingleItem(items.First());

            // Write all selected items.
            var strBuilder = new StringBuilder(1024 * 16);

            string indent = new string('\t', nbIndent);

            strBuilder.Append("(");
            strBuilder.AppendLine();

            foreach (string item in items)
            {
                strBuilder.AppendFormat("{0}\t{1},{2}", indent, XCodeFormatSingleItem(item), Environment.NewLine);
            }
            strBuilder.AppendFormat("{0})", indent);

            return strBuilder.ToString();
        }

        // Key is the name of a Target, Value is the list of configs per target
        private Dictionary<string, List<Project.Configuration>> GetProjectConfigurationsPerTarget(List<Project.Configuration> configurations)
        {
            var configsPerTarget = configurations.GroupBy(conf => GetTargetKey(conf)).ToDictionary(g => g.Key, g => g.ToList());

            return configsPerTarget;
        }

        private void RegisterScriptBuildPhase(string xCodeTargetName, Dictionary<string, List<ProjectShellScriptBuildPhase>> shellScriptPhases, IEnumerator<string> eventsInConf)
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
                    shellScriptPhases.Add(xCodeTargetName, new List<ProjectShellScriptBuildPhase>() { shellScriptBuildPhase });
                }
                else
                {
                    shellScriptPhases[xCodeTargetName].Add(shellScriptBuildPhase);
                }
            }
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

        private void PrepareSourceFiles(string xCodeTargetName, Strings sourceFiles, Project project, Project.Configuration configuration, string workspacePath = null)
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

        private void PrepareResourceFiles(string xCodeTargetName, Strings sourceFiles, Project project, Project.Configuration configuration, string workspacePath = null)
        {
            foreach (string file in sourceFiles)
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
            XCodeOptions.ResolveProjectPaths(project, externalResourceFiles);

            Strings externalResourceFolders = Options.GetStrings<Options.XCode.Compiler.ExternalResourceFolders>(configuration);
            XCodeOptions.ResolveProjectPaths(project, externalResourceFolders);

            Strings externalResourcePackages = Options.GetStrings<Options.XCode.Compiler.ExternalResourcePackages>(configuration);
            XCodeOptions.ResolveProjectPaths(project, externalResourcePackages);

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

        private XCodeOptions GenerateOptions(Project project, Project.Configuration conf)
        {
            XCodeOptions options = new XCodeOptions();

            options["Archs"] = "\"$(ARCHS_STANDARD_64_BIT)\"";
            options["CodeSignEntitlements"] = RemoveLineTag;
            options["DevelopmentTeam"] = RemoveLineTag;
            options["ExcludedSourceFileNames"] = XCodeFormatList(conf.ResolvedSourceFilesBuildExclude, 4);
            options["InfoPListFile"] = RemoveLineTag;
            options["IPhoneOSDeploymentTarget"] = RemoveLineTag;
            options["MacOSDeploymentTarget"] = RemoveLineTag;
            options["ProvisioningProfile"] = RemoveLineTag;
            options["ProvisioningStyle"] = "Automatic";
            options["RemoveLibraryPaths"] = "";
            options["RemoveSpecificDeviceLibraryPaths"] = "";
            options["RemoveSpecificSimulatorLibraryPaths"] = "";
            options["SDKRoot"] = conf.Platform == Platform.ios ? "iphoneos" : RemoveLineTag;
            options["SpecificLibraryPaths"] = RemoveLineTag;
            options["TargetedDeviceFamily"] = "1,2";
            options["UsePrecompiledHeader"] = "NO";
            options["PrecompiledHeader"] = RemoveLineTag;
            options["ValidArchs"] = RemoveLineTag;
            options["BuildDirectory"] = (conf.Output == Project.Configuration.OutputType.Lib) ? conf.TargetLibraryPath : conf.TargetPath;

            if (conf.IsFastBuild)
            {
                options["FastBuildTarget"] = FastBuild.Bff.GetShortProjectName(project, conf);
            }
            else
            {
                options["FastBuildTarget"] = RemoveLineTag;
            }

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.AlwaysSearchUserPaths.Disable, () => options["AlwaysSearchUserPaths"] = "NO"),
                Options.Option(Options.XCode.Compiler.AlwaysSearchUserPaths.Enable, () => options["AlwaysSearchUserPaths"] = "YES")
            );

            Options.XCode.Compiler.Archs archs = Options.GetObject<Options.XCode.Compiler.Archs>(conf);
            if (archs != null)
                options["Archs"] = archs.Value;

            Options.XCode.Compiler.AssetCatalogCompilerAppIconName assetcatalogCompilerAppiconName = Options.GetObject<Options.XCode.Compiler.AssetCatalogCompilerAppIconName>(conf);
            if (assetcatalogCompilerAppiconName != null)
                options["AssetCatalogCompilerAppIconName"] = assetcatalogCompilerAppiconName.Value;
            else
                options["AssetCatalogCompilerAppIconName"] = RemoveLineTag;

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.AutomaticReferenceCounting.Disable, () => options["AutomaticReferenceCounting"] = "NO"),
                Options.Option(Options.XCode.Compiler.AutomaticReferenceCounting.Enable, () => options["AutomaticReferenceCounting"] = "YES")
                );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.ClangAnalyzerLocalizabilityNonlocalized.Disable, () => options["ClangAnalyzerLocalizabilityNonlocalized"] = "NO"),
                Options.Option(Options.XCode.Compiler.ClangAnalyzerLocalizabilityNonlocalized.Enable, () => options["ClangAnalyzerLocalizabilityNonlocalized"] = "YES")
                );

            Options.SelectOption(conf,
               Options.Option(Options.XCode.Compiler.ClangEnableModules.Disable, () => options["ClangEnableModules"] = "NO"),
               Options.Option(Options.XCode.Compiler.ClangEnableModules.Enable, () => options["ClangEnableModules"] = "YES")
            );

            Options.XCode.Compiler.CodeSignEntitlements codeSignEntitlements = Options.GetObject<Options.XCode.Compiler.CodeSignEntitlements>(conf);
            if (codeSignEntitlements != null)
                options["CodeSignEntitlements"] = XCodeOptions.ResolveProjectPaths(project, codeSignEntitlements.Value);

            Options.XCode.Compiler.CodeSigningIdentity codeSigningIdentity = Options.GetObject<Options.XCode.Compiler.CodeSigningIdentity>(conf);
            if (codeSigningIdentity != null)
            {
                options["CodeSigningIdentity"] = codeSigningIdentity.Value;
            }
            else if (conf.Platform == Platform.ios)
                options["CodeSigningIdentity"] = "iPhone Developer"; //Previous Default value in the template
            else
                options["CodeSigningIdentity"] = RemoveLineTag;

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.OnlyActiveArch.Disable, () => options["OnlyActiveArch"] = "NO"),
                Options.Option(Options.XCode.Compiler.OnlyActiveArch.Enable, () => options["OnlyActiveArch"] = "YES")
                );

            options["ProductBundleIdentifier"] = Options.StringOption.Get<Options.XCode.Compiler.ProductBundleIdentifier>(conf);

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP98, () => options["CppStandard"] = "c++98"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP11, () => options["CppStandard"] = "c++11"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP14, () => options["CppStandard"] = "c++14"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP17, () => options["CppStandard"] = "c++17"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU98, () => options["CppStandard"] = "gnu++98"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU11, () => options["CppStandard"] = "gnu++11"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU14, () => options["CppStandard"] = "gnu++14"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU17, () => options["CppStandard"] = "gnu++17")
                );

            Options.XCode.Compiler.DevelopmentTeam developmentTeam = Options.GetObject<Options.XCode.Compiler.DevelopmentTeam>(conf);
            if (developmentTeam != null)
                options["DevelopmentTeam"] = developmentTeam.Value;

            Options.XCode.Compiler.ProvisioningStyle provisioningStyle = Options.GetObject<Options.XCode.Compiler.ProvisioningStyle>(conf);
            if (provisioningStyle != null)
                options["ProvisioningStyle"] = provisioningStyle.Value;

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.DebugInformationFormat.Dwarf, () => options["DebugInformationFormat"] = "dwarf"),
                Options.Option(Options.XCode.Compiler.DebugInformationFormat.DwarfWithDSym, () => options["DebugInformationFormat"] = "\"dwarf-with-dsym\""),
                Options.Option(Options.XCode.Compiler.DebugInformationFormat.Stabs, () => options["DebugInformationFormat"] = "stabs")
                );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.DynamicNoPic.Disable, () => options["DynamicNoPic"] = "NO"),
                Options.Option(Options.XCode.Compiler.DynamicNoPic.Enable, () => options["DynamicNoPic"] = "YES")
                );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.EnableBitcode.Disable, () => { options["EnableBitcode"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.EnableBitcode.Enable, () => { options["EnableBitcode"] = "YES"; })
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.Exceptions.Disable, () => { options["CppExceptionHandling"] = "NO"; options["ObjCExceptionHandling"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.Exceptions.Enable, () => { options["CppExceptionHandling"] = "YES"; options["ObjCExceptionHandling"] = "YES"; }),
                Options.Option(Options.XCode.Compiler.Exceptions.EnableCpp, () => { options["CppExceptionHandling"] = "YES"; options["ObjCExceptionHandling"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.Exceptions.EnableObjC, () => { options["CppExceptionHandling"] = "NO"; options["ObjCExceptionHandling"] = "YES"; })
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.GccNoCommonBlocks.Disable, () => options["GccNoCommonBlocks"] = "NO"),
                Options.Option(Options.XCode.Compiler.GccNoCommonBlocks.Enable, () => options["GccNoCommonBlocks"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.CLanguageStandard.ANSI, () => options["CStandard"] = "ansi"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.C89, () => options["CStandard"] = "c89"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.GNU89, () => options["CStandard"] = "gnu89"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.C99, () => options["CStandard"] = "c99"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.GNU99, () => options["CStandard"] = "gnu99"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.C11, () => options["CStandard"] = "c11"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.GNU11, () => options["CStandard"] = "gnu11"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.CompilerDefault, () => options["CStandard"] = RemoveLineTag)
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.ObjCWeakReferences.Disable, () => options["ObjCWeakReferences"] = "NO"),
                Options.Option(Options.XCode.Compiler.ObjCWeakReferences.Enable, () => options["ObjCWeakReferences"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Disable, () => { options["OptimizationLevel"] = "0"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Fast, () => { options["OptimizationLevel"] = "1"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Faster, () => { options["OptimizationLevel"] = "2"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Fastest, () => { options["OptimizationLevel"] = "3"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Smallest, () => { options["OptimizationLevel"] = "s"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Aggressive, () => { options["OptimizationLevel"] = "fast"; })
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.DeadStrip.Disable, () => { options["DeadStripping"] = "NO"; options["PrivateInlines"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.DeadStrip.Code, () => { options["DeadStripping"] = "YES"; options["PrivateInlines"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.DeadStrip.Inline, () => { options["DeadStripping"] = "NO"; options["PrivateInlines"] = "YES"; }),
                Options.Option(Options.XCode.Compiler.DeadStrip.All, () => { options["DeadStripping"] = "YES"; options["PrivateInlines"] = "YES"; })
                );


            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.PreserveDeadCodeInitsAndTerms.Disable, () => { options["PreserveDeadCodeInitsAndTerms"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.PreserveDeadCodeInitsAndTerms.Enable, () => { options["PreserveDeadCodeInitsAndTerms"] = "YES"; })
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.PrivateSymbols.Disable, () => { options["PrivateSymbols"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.PrivateSymbols.Enable, () => { options["PrivateSymbols"] = "YES"; })
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.RTTI.Disable, () => { options["RuntimeTypeInfo"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.RTTI.Enable, () => { options["RuntimeTypeInfo"] = "YES"; })
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.StrictObjCMsgSend.Disable, () => options["StrictObjCMsgSend"] = "NO"),
                Options.Option(Options.XCode.Compiler.StrictObjCMsgSend.Enable, () => options["StrictObjCMsgSend"] = "YES")
            );

            Options.SelectOption(conf,
               Options.Option(Options.XCode.Compiler.Testability.Disable, () => options["Testability"] = "NO"),
               Options.Option(Options.XCode.Compiler.Testability.Enable, () => options["Testability"] = "YES")
            );

            Strings frameworkPaths = Options.GetStrings<Options.XCode.Compiler.FrameworkPaths>(conf);
            options["FrameworkPaths"] = XCodeOptions.ResolveProjectPaths(project, frameworkPaths.JoinStrings(",\n", "\t\t\t\t\t\"", "\"").TrimEnd('\n'));

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.GenerateDebuggingSymbols.Disable, () => options["GenerateDebuggingSymbols"] = "NO"),
                Options.Option(Options.XCode.Compiler.GenerateDebuggingSymbols.DeadStrip, () => options["GenerateDebuggingSymbols"] = "YES"),
                Options.Option(Options.XCode.Compiler.GenerateDebuggingSymbols.Enable, () => options["GenerateDebuggingSymbols"] = "YES")
                );

            Options.XCode.Compiler.InfoPListFile infoPListFile = Options.GetObject<Options.XCode.Compiler.InfoPListFile>(conf);
            if (infoPListFile != null)
                options["InfoPListFile"] = XCodeOptions.ResolveProjectPaths(project, infoPListFile.Value);

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.ICloud.Disable, () => options["iCloud"] = "0"),
                Options.Option(Options.XCode.Compiler.ICloud.Enable, () => options["iCloud"] = "1")
                );

            Options.XCode.Compiler.IPhoneOSDeploymentTarget iosDeploymentTarget = Options.GetObject<Options.XCode.Compiler.IPhoneOSDeploymentTarget>(conf);
            if (iosDeploymentTarget != null)
                options["IPhoneOSDeploymentTarget"] = iosDeploymentTarget.MinimumVersion;

            Options.XCode.Compiler.MacOSDeploymentTarget macDeploymentTarget = Options.GetObject<Options.XCode.Compiler.MacOSDeploymentTarget>(conf);
            if (macDeploymentTarget != null)
                options["MacOSDeploymentTarget"] = macDeploymentTarget.MinimumVersion;

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.LibraryStandard.CppStandard, () => options["LibraryStandard"] = "libstdc++"),
                Options.Option(Options.XCode.Compiler.LibraryStandard.LibCxx, () => options["LibraryStandard"] = "libc++")
                );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.ModelTuning.None, () => options["ModelTuning"] = RemoveLineTag),
                Options.Option(Options.XCode.Compiler.ModelTuning.G3, () => options["ModelTuning"] = "G3"),
                Options.Option(Options.XCode.Compiler.ModelTuning.G4, () => options["ModelTuning"] = "G4"),
                Options.Option(Options.XCode.Compiler.ModelTuning.G5, () => options["ModelTuning"] = "G5")
                );

            options["MachOType"] = RemoveLineTag;
            switch (conf.Output)
            {
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.IosApp:
                    options["MachOType"] = "mh_execute";
                    break;
                case Project.Configuration.OutputType.Lib:
                    options["MachOType"] = "staticlib";
                    break;
                case Project.Configuration.OutputType.Dll:
                    options["MachOType"] = "mh_dylib";
                    break;
                case Project.Configuration.OutputType.None:
                    // do nothing
                    break;
                default:
                    throw new NotSupportedException($"XCode generator doesn't handle {conf.Output}");
            }

            Options.XCode.Compiler.ProvisioningProfile provisioningProfile = Options.GetObject<Options.XCode.Compiler.ProvisioningProfile>(conf);
            if (provisioningProfile != null)
                options["ProvisioningProfile"] = provisioningProfile.ProfileName;

            Options.XCode.Compiler.SDKRoot sdkRoot = Options.GetObject<Options.XCode.Compiler.SDKRoot>(conf);
            if (sdkRoot != null)
                options["SDKRoot"] = sdkRoot.Value;

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.SkipInstall.Disable, () => options["SkipInstall"] = "NO"),
                Options.Option(Options.XCode.Compiler.SkipInstall.Enable, () => options["SkipInstall"] = "YES")
                );

            Options.XCode.Compiler.TargetedDeviceFamily targetedDeviceFamily = Options.GetObject<Options.XCode.Compiler.TargetedDeviceFamily>(conf);
            if (targetedDeviceFamily != null)
                options["TargetedDeviceFamily"] = targetedDeviceFamily.Value;
            else
                options["TargetedDeviceFamily"] = RemoveLineTag;

            options["TargetName"] = XCodeFormatSingleItem(conf.Target.Name);

            Options.XCode.Compiler.ValidArchs validArchs = Options.GetObject<Options.XCode.Compiler.ValidArchs>(conf);
            if (validArchs != null)
                options["ValidArchs"] = validArchs.Archs;

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.Warning64To32BitConversion.Disable, () => options["Warning64To32BitConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.Warning64To32BitConversion.Enable, () => options["Warning64To32BitConversion"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningBlockCaptureAutoReleasing.Disable, () => options["WarningBlockCaptureAutoReleasing"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningBlockCaptureAutoReleasing.Enable, () => options["WarningBlockCaptureAutoReleasing"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningBooleanConversion.Disable, () => options["WarningBooleanConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningBooleanConversion.Enable, () => options["WarningBooleanConversion"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningComma.Disable, () => options["WarningComma"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningComma.Enable, () => options["WarningComma"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningConstantConversion.Disable, () => options["WarningConstantConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningConstantConversion.Enable, () => options["WarningConstantConversion"] = "YES")
                );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningDeprecatedObjCImplementations.Disable, () => options["WarningDeprecatedObjCImplementations"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningDeprecatedObjCImplementations.Enable, () => options["WarningDeprecatedObjCImplementations"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningDuplicateMethodMatch.Disable, () => options["WarningDuplicateMethodMatch"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningDuplicateMethodMatch.Enable, () => options["WarningDuplicateMethodMatch"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningEmptyBody.Disable, () => options["WarningEmptyBody"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningEmptyBody.Enable, () => options["WarningEmptyBody"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningEnumConversion.Disable, () => options["WarningEnumConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningEnumConversion.Enable, () => options["WarningEnumConversion"] = "YES")
                );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningDirectIsaUsage.Disable, () => options["WarningDirectIsaUsage"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningDirectIsaUsage.Enable, () => options["WarningDirectIsaUsage"] = "YES"),
                Options.Option(Options.XCode.Compiler.WarningDirectIsaUsage.EnableAndError, () => options["WarningDirectIsaUsage"] = "YES_ERROR")
            );

            Options.SelectOption(conf,
               Options.Option(Options.XCode.Compiler.WarningInfiniteRecursion.Disable, () => options["WarningInfiniteRecursion"] = "NO"),
               Options.Option(Options.XCode.Compiler.WarningInfiniteRecursion.Enable, () => options["WarningInfiniteRecursion"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningIntConversion.Disable, () => options["WarningIntConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningIntConversion.Enable, () => options["WarningIntConversion"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningNonLiteralNullConversion.Disable, () => options["WarningNonLiteralNullConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningNonLiteralNullConversion.Enable, () => options["WarningNonLiteralNullConversion"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningObjCImplicitRetainSelf.Disable, () => options["WarningObjCImplicitRetainSelf"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningObjCImplicitRetainSelf.Enable, () => options["WarningObjCImplicitRetainSelf"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningObjCLiteralConversion.Disable, () => options["WarningObjCLiteralConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningObjCLiteralConversion.Enable, () => options["WarningObjCLiteralConversion"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningRangeLoopAnalysis.Disable, () => options["WarningRangeLoopAnalysis"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningRangeLoopAnalysis.Enable, () => options["WarningRangeLoopAnalysis"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningReturnType.Disable, () => options["WarningReturnType"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningReturnType.Enable, () => options["WarningReturnType"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningRootClass.Disable, () => options["WarningRootClass"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningRootClass.Enable, () => options["WarningRootClass"] = "YES"),
                Options.Option(Options.XCode.Compiler.WarningRootClass.EnableAndError, () => options["WarningRootClass"] = "YES_ERROR")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningStrictPrototypes.Disable, () => options["WarningStrictPrototypes"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningStrictPrototypes.Enable, () => options["WarningStrictPrototypes"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningSuspiciousMove.Disable, () => options["WarningSuspiciousMove"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningSuspiciousMove.Enable, () => options["WarningSuspiciousMove"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningUndeclaredSelector.Disable, () => options["WarningUndeclaredSelector"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUndeclaredSelector.Enable, () => options["WarningUndeclaredSelector"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningUniniatializedAutos.Disable, () => options["WarningUniniatializedAutos"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUniniatializedAutos.Enable, () => options["WarningUniniatializedAutos"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningUnreachableCode.Disable, () => options["WarningUnreachableCode"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUnreachableCode.Enable, () => options["WarningUnreachableCode"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningUnusedFunction.Disable, () => options["WarningUnusedFunction"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUnusedFunction.Enable, () => options["WarningUnusedFunction"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningUnusedVariable.Disable, () => options["WarningUnusedVariable"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUnusedVariable.Enable, () => options["WarningUnusedVariable"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.DeploymentPostProcessing.Disable, () => options["DeploymentPostProcessing"] = "NO"),
                Options.Option(Options.XCode.Compiler.DeploymentPostProcessing.Enable, () => options["DeploymentPostProcessing"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.StripDebugSymbolsDuringCopy.Disable, () => options["StripDebugSymbolsDuringCopy"] = "NO"),
                Options.Option(Options.XCode.Compiler.StripDebugSymbolsDuringCopy.Enable, () => options["StripDebugSymbolsDuringCopy"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.TreatWarningsAsErrors.Disable, () => options["TreatWarningsAsErrors"] = "NO"),
                Options.Option(Options.XCode.Compiler.TreatWarningsAsErrors.Enable, () => options["TreatWarningsAsErrors"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Linker.StripLinkedProduct.Disable, () => options["StripLinkedProduct"] = "NO"),
                Options.Option(Options.XCode.Linker.StripLinkedProduct.Enable, () => options["StripLinkedProduct"] = "YES")
            );

            if (conf.PrecompHeader != null)
            {
                options["UsePrecompiledHeader"] = "YES";

                string workspacePath = Util.GetCapitalizedPath(Directory.GetParent(conf.ProjectFullFileNameWithExtension).FullName);
                string precompiledHeaderFullPath = Util.GetCapitalizedPath(project.SourceRootPath + FolderSeparator + conf.PrecompHeader);
                options["PrecompiledHeader"] = Util.PathGetRelative(workspacePath, precompiledHeaderFullPath);
            }

            OrderableStrings includePaths = conf.IncludePaths;
            includePaths.AddRange(conf.IncludePrivatePaths);
            includePaths.AddRange(conf.DependenciesIncludePaths);
            options["IncludePaths"] = XCodeFormatList(includePaths, 4);

            var libraryPaths = new OrderableStrings(conf.LibraryPaths);
            libraryPaths.AddRange(conf.DependenciesOtherLibraryPaths);
            libraryPaths.AddRange(conf.DependenciesBuiltTargetsLibraryPaths);

            if (libraryPaths.Count == 0)
            {
                options["LibraryPaths"] = RemoveLineTag;
                options["RemoveLibraryPaths"] = RemoveLineTag;
            }
            else
            {
                libraryPaths.Sort();
                options["LibraryPaths"] = libraryPaths.JoinStrings(",\n", "\t\t\t\t\t\"", "\"").TrimEnd('\n');
            }

            Strings specificDeviceLibraryPaths = Options.GetStrings<Options.XCode.Compiler.SpecificDeviceLibraryPaths>(conf);
            if (specificDeviceLibraryPaths.Count == 0)
            {
                options["SpecificDeviceLibraryPaths"] = RemoveLineTag;
                options["RemoveSpecificDeviceLibraryPaths"] = RemoveLineTag;
            }
            else
            {
                options["SpecificDeviceLibraryPaths"] = XCodeOptions.ResolveProjectPaths(project, specificDeviceLibraryPaths.JoinStrings(",\n", "\t\t\t\t\t\"", "\"").TrimEnd('\n'));
            }

            Strings specificSimulatorLibraryPaths = Options.GetStrings<Options.XCode.Compiler.SpecificSimulatorLibraryPaths>(conf);
            if (specificSimulatorLibraryPaths.Count == 0)
            {
                options["SpecificSimulatorLibraryPaths"] = RemoveLineTag;
                options["RemoveSpecificSimulatorLibraryPaths"] = RemoveLineTag;
            }
            else
            {
                options["SpecificSimulatorLibraryPaths"] = XCodeOptions.ResolveProjectPaths(project, specificSimulatorLibraryPaths.JoinStrings(",\n", "\t\t\t\t\t\"", "\"").TrimEnd('\n'));
            }

            options["PreprocessorDefinitions"] = RemoveLineTag;
            options["CompilerOptions"] = RemoveLineTag;
            options["LinkerOptions"] = RemoveLineTag;
            options["WarningOptions"] = RemoveLineTag;

            var libFiles = new OrderableStrings(conf.LibraryFiles);
            libFiles.AddRange(conf.DependenciesBuiltTargetsLibraryFiles);
            libFiles.AddRange(conf.DependenciesOtherLibraryFiles);
            libFiles.Sort();

            Strings linkerOptions = new Strings(conf.AdditionalLinkerOptions);

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

            if (conf.Defines.Any())
                options["PreprocessorDefinitions"] = conf.Defines.Select(item => "\t\t\t\t\t\"" + item.Replace("\"", "") + "\"").Aggregate((first, next) => first + ",\n" + next).TrimEnd('\n', '\t');
            if (conf.AdditionalCompilerOptions.Any())
                options["CompilerOptions"] = conf.AdditionalCompilerOptions.Select(item => "\t\t\t\t\t\"" + item.Replace("\"", "") + "\"").Aggregate((first, next) => first + ",\n" + next).TrimEnd('\n', '\t');
            if (conf.AdditionalLibrarianOptions.Any())
                throw new NotImplementedException(nameof(conf.AdditionalLibrarianOptions) + " not supported with XCode generator");
            if (linkerOptions.Any())
                options["LinkerOptions"] = linkerOptions.Select(item => "\t\t\t\t\t\"" + item.Replace("\"", "") + "\"").Aggregate((first, next) => first + ",\n" + next).TrimEnd('\n', '\t');
            return options;
        }

        private class XCodeOptions : Dictionary<string, string>
        {
            public static string ResolveProjectPaths(Project project, string stringToResolve)
            {
                Resolver resolver = new Resolver();
                using (resolver.NewScopedParameter("project", project))
                {
                    string resolvedString = resolver.Resolve(stringToResolve);
                    return Util.SimplifyPath(resolvedString);
                }
            }

            public static void ResolveProjectPaths(Project project, Strings stringsToResolve)
            {
                foreach (string value in stringsToResolve.Values)
                {
                    string newValue = ResolveProjectPaths(project, value);
                    stringsToResolve.UpdateValue(value, newValue);
                }
            }
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
            public List<ProjectShellScriptBuildPhase> ShellScriptPreBuildPhases { get; set; }
            public List<ProjectShellScriptBuildPhase> ShellScriptPostBuildPhases { get; set; }
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
                    return XCodeFormatSingleItem(Util.SimplifyPath(FastBuildSettings.FastBuildMakeCommand));
                }
            }

            public string BuildWorkingDirectory
            {
                get
                {
                    return XCodeFormatSingleItem(Path.GetDirectoryName(_masterBffFilePath));
                }
            }
        }

        private class ProjectBuildConfiguration : ProjectItem
        {
            public ProjectBuildConfiguration(ItemSection section, string configurationName, Project.Configuration configuration, XCodeOptions options)
                : base(section, configurationName)
            {
                Configuration = configuration;
                Options = options;
            }

            public XCodeOptions Options { get; }
            public Project.Configuration Configuration { get; }
            public string Optimization { get { return Configuration.Target.Name; } }
        }

        private class ProjectBuildConfigurationForTarget : ProjectBuildConfiguration
        {
            public ProjectBuildConfigurationForTarget(ItemSection section, Project.Configuration configuration, ProjectTarget target, XCodeOptions options)
                : base(section, configuration.Target.Name, configuration, options)
            {
                Target = target;
            }

            public ProjectTarget Target { get; }
        }

        private class ProjectBuildConfigurationForNativeTarget : ProjectBuildConfigurationForTarget
        {
            public ProjectBuildConfigurationForNativeTarget(Project.Configuration configuration, ProjectNativeTarget nativeTarget, XCodeOptions options)
                : base(ItemSection.XCBuildConfiguration_NativeTarget, configuration, nativeTarget, options)
            { }
        }

        private class ProjectBuildConfigurationForLegacyTarget : ProjectBuildConfigurationForTarget
        {
            public ProjectBuildConfigurationForLegacyTarget(Project.Configuration configuration, ProjectLegacyTarget legacyTarget, XCodeOptions options)
                : base(ItemSection.XCBuildConfiguration_LegacyTarget, configuration, legacyTarget, options)
            { }
        }

        private class ProjectBuildConfigurationForUnitTestTarget : ProjectBuildConfigurationForTarget
        {
            public ProjectBuildConfigurationForUnitTestTarget(Project.Configuration configuration, ProjectTarget target, XCodeOptions options)
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
            public ProjectBuildConfigurationForProject(Project.Configuration configuration, XCodeOptions options)
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
