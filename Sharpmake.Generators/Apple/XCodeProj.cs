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

        public const string RemoveLineTag = "REMOVE_LINE_TAG";

        public static readonly char FolderSeparator;

        private readonly HashSet<ProjectItem> _projectItems = new HashSet<ProjectItem>();

        //Source files that are potentially removable. Need to check if they are excluded from build in all configs.
        private HashSet<ProjectFileSystemItem> _removableItems = new HashSet<ProjectFileSystemItem>();
        private ProjectFolder _mainGroup = null;
        private ProjectFolder _productsGroup = null;
        private ProjectFolder _projectsFolder = null;
        private ProjectFolder _frameworksFolder = null;

        private Dictionary<Project.Configuration, ProjectNativeTarget> _nativeTargets = null;
        private Dictionary<Project.Configuration, ProjectResourcesBuildPhase> _resourcesBuildPhases = null;
        private Dictionary<Project.Configuration, ProjectSourcesBuildPhase> _sourcesBuildPhases = null;
        private Dictionary<Project.Configuration, ProjectFrameworksBuildPhase> _frameworksBuildPhases = null;

        private List<ProjectOutputFile> _projectOutputFiles = null;
        private Dictionary<Project.Configuration, List<ProjectTargetDependency>> _targetDependencies = null;
        private Dictionary<ProjectFolder, ProjectReference> _projectReferencesGroups = null;
        private ProjectMain _projectMain = null;

        private Dictionary<Project.Configuration, XCodeOptions> _optionMapping = null;

        // Unit Test Variables
        private string _unitTestFramework = "XCTest";

        static XCodeProj()
        {
            FolderSeparator = Path.DirectorySeparatorChar;
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
            WriteSection<ProjectMain>(configurations[0], fileGenerator);
            WriteSection<ProjectReferenceProxy>(configurations[0], fileGenerator);
            WriteSection<ProjectResourcesBuildPhase>(configurations[0], fileGenerator);
            WriteSection<ProjectSourcesBuildPhase>(configurations[0], fileGenerator);
            WriteSection<ProjectVariantGroup>(configurations[0], fileGenerator);
            WriteSection<ProjectTargetDependency>(configurations[0], fileGenerator);
            WriteSection<ProjectBuildConfiguration>(configurations[0], fileGenerator);
            WriteSection<ProjectConfigurationList>(configurations[0], fileGenerator);

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
            var testableTargets = _nativeTargets.Values.Where(target => target.OutputFile.OutputType == Project.Configuration.OutputType.IosTestBundle);
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
            var defaultTarget = _nativeTargets.Values.Where(target => target.OutputFile.OutputType != Project.Configuration.OutputType.IosTestBundle).FirstOrDefault();
            using (fileGenerator.Declare("projectFile", projectFile))
            using (fileGenerator.Declare("item", defaultTarget))
            using (fileGenerator.Declare("testableElements", testableElements))
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
            Dictionary<Project.Configuration, List<Project.Configuration>> configsList = GetProjectConfigurationsPerNativeTarget(configurations);

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
            }

            _projectReferencesGroups = new Dictionary<ProjectFolder, ProjectReference>();
            _projectOutputFiles = new List<ProjectOutputFile>();

            _nativeTargets = new Dictionary<Project.Configuration, ProjectNativeTarget>();
            _targetDependencies = new Dictionary<Project.Configuration, List<ProjectTargetDependency>>();
            _sourcesBuildPhases = new Dictionary<Project.Configuration, ProjectSourcesBuildPhase>();
            _resourcesBuildPhases = new Dictionary<Project.Configuration, ProjectResourcesBuildPhase>();
            _frameworksBuildPhases = new Dictionary<Project.Configuration, ProjectFrameworksBuildPhase>();

            //Loop on default configs for each target
            foreach (Project.Configuration conf in configsList.Keys)
            {
                HashSet<ProjectBuildConfiguration> configurationsForNativeTarget = new HashSet<ProjectBuildConfiguration>();
                ProjectConfigurationList configurationListForNativeTarget = new ProjectConfigurationList(configurationsForNativeTarget, conf.TargetFileName);
                _projectItems.Add(configurationListForNativeTarget);

                ProjectOutputFile projectOutputFile = new ProjectOutputFile(conf);
                _projectItems.Add(projectOutputFile);
                _productsGroup.Children.Add(projectOutputFile);

                ProjectBuildFile projectOutputBuildFile = new ProjectBuildFile(projectOutputFile);
                _projectItems.Add(projectOutputBuildFile);
                _projectOutputFiles.Add(projectOutputFile);

                ProjectSourcesBuildPhase sourcesBuildPhase = new ProjectSourcesBuildPhase(conf.TargetFileName, 2147483647);
                _projectItems.Add(sourcesBuildPhase);
                _sourcesBuildPhases.Add(conf, sourcesBuildPhase);
                PrepareSourceFiles(projectFiles, project, conf, workspacePath);

                ProjectResourcesBuildPhase resourceBuildPhase = new ProjectResourcesBuildPhase(conf.TargetFileName, 2147483647);
                _projectItems.Add(resourceBuildPhase);
                _resourcesBuildPhases.Add(conf, resourceBuildPhase);
                PrepareResourceFiles(project.ResourceFiles, project, conf);
                PrepareExternalResourceFiles(project, conf);

                ProjectFrameworksBuildPhase frameworkBuildPhase = new ProjectFrameworksBuildPhase(conf.TargetFileName, 2147483647);
                _projectItems.Add(frameworkBuildPhase);
                _frameworksBuildPhases.Add(conf, frameworkBuildPhase);

                List<ProjectTargetDependency> targetDependencies = new List<ProjectTargetDependency>();
                _targetDependencies.Add(conf, targetDependencies);

                if (conf.Output == Project.Configuration.OutputType.Exe || conf.Output == Project.Configuration.OutputType.IosTestBundle || conf.Output == Project.Configuration.OutputType.IosApp)
                {
                    foreach (Project.Configuration dependentConfiguration in conf.ResolvedDependencies)
                    {
                        if (dependentConfiguration.Output != Project.Configuration.OutputType.None)
                        {
                            ProjectReference projectReference = new ProjectReference(dependentConfiguration.ProjectFullFileNameWithExtension);
                            _projectItems.Add(projectReference);
                            if (!_projectsFolder.Children.Contains(projectReference))
                                _projectsFolder.Children.Add(projectReference);

                            ProjectOutputFile outputFileProxy = new ProjectOutputFile(dependentConfiguration);
                            ProjectNativeTarget nativeTargetProxy = new ProjectNativeTarget(dependentConfiguration.TargetFileFullName);
                            ProjectContainerProxy projectProxy = new ProjectContainerProxy(projectReference, nativeTargetProxy, ProjectContainerProxy.Type.Target);
                            _projectItems.Add(projectProxy);

                            ProjectTargetDependency targetDependency = new ProjectTargetDependency(projectReference, projectProxy);
                            _projectItems.Add(targetDependency);
                            _targetDependencies[conf].Add(targetDependency);

                            projectProxy = new ProjectContainerProxy(projectReference, outputFileProxy, ProjectContainerProxy.Type.Archive);
                            _projectItems.Add(projectProxy);

                            ProjectReferenceProxy referenceProxy = new ProjectReferenceProxy(projectReference, projectProxy, outputFileProxy);
                            _projectItems.Add(referenceProxy);

                            ProjectProductsFolder projectDependencyGroup = new ProjectProductsFolder(projectReference.Name);

                            if (!_projectReferencesGroups.ContainsKey(projectDependencyGroup))
                            {
                                projectDependencyGroup.Children.Add(referenceProxy);
                                _projectReferencesGroups.Add(projectDependencyGroup, projectReference);
                            }

                            _projectItems.Add(projectDependencyGroup);

                            ProjectBuildFile libraryBuildFile = new ProjectBuildFile(referenceProxy);
                            _projectItems.Add(libraryBuildFile);
                            _frameworksBuildPhases[conf].Files.Add(libraryBuildFile);
                        }
                    }
                }

                Strings systemFrameworks = Options.GetStrings<Options.XCode.Compiler.SystemFrameworks>(configurations[0]);
                foreach (string systemFramework in systemFrameworks)
                {
                    ProjectSystemFrameworkFile systemFrameworkItem = new ProjectSystemFrameworkFile(systemFramework);
                    ProjectBuildFile buildFileItem = new ProjectBuildFile(systemFrameworkItem);
                    if (!_frameworksFolder.Children.Exists(item => item.FullPath == systemFrameworkItem.FullPath))
                    {
                        _frameworksFolder.Children.Add(systemFrameworkItem);
                        _projectItems.Add(systemFrameworkItem);
                    }
                    _projectItems.Add(buildFileItem);
                    _frameworksBuildPhases[conf].Files.Add(buildFileItem);
                }

                Strings userFrameworks = Options.GetStrings<Options.XCode.Compiler.UserFrameworks>(configurations[0]);
                foreach (string userFramework in userFrameworks)
                {
                    ProjectUserFrameworkFile userFrameworkItem = new ProjectUserFrameworkFile(XCodeOptions.ResolveProjectPaths(project, userFramework), workspacePath);
                    ProjectBuildFile buildFileItem = new ProjectBuildFile(userFrameworkItem);
                    _frameworksFolder.Children.Add(userFrameworkItem);
                    _projectItems.Add(userFrameworkItem);
                    _projectItems.Add(buildFileItem);
                    _frameworksBuildPhases[conf].Files.Add(buildFileItem);
                }

                if (conf.Output == Project.Configuration.OutputType.IosTestBundle)
                {
                    ProjectDeveloperFrameworkFile testFrameworkItem = new ProjectDeveloperFrameworkFile(_unitTestFramework);
                    ProjectBuildFile buildFileItem = new ProjectBuildFile(testFrameworkItem);
                    if (_frameworksFolder != null)
                        _frameworksFolder.Children.Add(testFrameworkItem);
                    _projectItems.Add(testFrameworkItem);
                    _projectItems.Add(buildFileItem);
                    _frameworksBuildPhases[conf].Files.Add(buildFileItem);
                }

                ProjectNativeTarget target = new ProjectNativeTarget(conf.TargetFileName, projectOutputFile, configurationListForNativeTarget, _targetDependencies[conf]);
                target.ResourcesBuildPhase = _resourcesBuildPhases[conf];
                target.SourcesBuildPhase = _sourcesBuildPhases[conf];
                target.FrameworksBuildPhase = _frameworksBuildPhases[conf];
                configurationListForNativeTarget.RelatedItem = target;
                _projectItems.Add(target);
                _nativeTargets.Add(conf, target);

                //Generate BuildConfigurations
                foreach (Project.Configuration targetConf in configsList[conf])
                {
                    XCodeOptions options = _optionMapping[targetConf];

                    ProjectBuildConfigurationForTarget configurationForNativeTarget;
                    if (targetConf.Output == Project.Configuration.OutputType.IosTestBundle)
                        configurationForNativeTarget = new ProjectBuildConfigurationForUnitTestTarget(targetConf, target, options);
                    else
                        configurationForNativeTarget = new ProjectBuildConfigurationForNativeTarget(targetConf, target, options);

                    configurationsForNativeTarget.Add(configurationForNativeTarget);
                    _projectItems.Add(configurationForNativeTarget);
                }
            }

            // Generate dependencies for unit test targets.
            List<Project.Configuration> unitTestConfigs = new List<Project.Configuration>(configsList.Keys).FindAll(element => element.Output == Project.Configuration.OutputType.IosTestBundle);
            if (unitTestConfigs != null && unitTestConfigs.Count != 0)
            {
                foreach (Project.Configuration unitTestConfig in unitTestConfigs)
                {
                    Project.Configuration bundleLoadingAppConfiguration = FindBundleLoadingApp(configurations);

                    if (bundleLoadingAppConfiguration != null && _nativeTargets.ContainsKey(bundleLoadingAppConfiguration))
                    {
                        ProjectNativeTarget bundleLoadingAppTarget = _nativeTargets[bundleLoadingAppConfiguration];

                        ProjectReference projectReference = new ProjectReference(ItemSection.PBXProject, bundleLoadingAppTarget.Identifier);
                        ProjectContainerProxy projectProxy = new ProjectContainerProxy(projectReference, bundleLoadingAppTarget, ProjectContainerProxy.Type.Target);

                        ProjectTargetDependency targetDependency = new ProjectTargetDependency(projectReference, projectProxy, bundleLoadingAppTarget);
                        _projectItems.Add(targetDependency);

                        _nativeTargets[unitTestConfig].Dependencies.Add(targetDependency);
                    }
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
            List<ProjectNativeTarget> nativeTargets = new List<ProjectNativeTarget>(_nativeTargets.Values);
            _projectMain = new ProjectMain(project.Name, _mainGroup, configurationListForProject, nativeTargets, iCloudSupport, developmentTeam);

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

        //Key is the default config of a Native Target, Value is the list of configs per native target with different optimization (Debug, Release,...)
        private Dictionary<Project.Configuration, List<Project.Configuration>> GetProjectConfigurationsPerNativeTarget(List<Project.Configuration> configurations)
        {
            Dictionary<Project.Configuration, List<Project.Configuration>> configsPerNativeTarget = new Dictionary<Project.Configuration, List<Project.Configuration>>();

            var outputTypes = Enum.GetValues(typeof(Project.Configuration.OutputType));

            foreach (Project.Configuration.OutputType type in outputTypes)
            {
                List<Project.Configuration> configs = configurations.FindAll(element => element.Output == type);

                if (configs != null && configs.Count != 0)
                    configsPerNativeTarget.Add(configs.First(), configs);
            }
            return configsPerNativeTarget;
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

        private void PrepareSourceFiles(Strings sourceFiles, Project project, Project.Configuration configuration, string workspacePath = null)
        {
            foreach (string file in sourceFiles)
            {
                ProjectFileSystemItem item = AddInFileSystem(file, workspacePath, true);
                item.Build = !configuration.ResolvedSourceFilesBuildExclude.Contains(item.FullPath);

                item.Source = project.SourceFilesCompileExtensions.Contains(item.Extension);
                if (item.Source || (String.Compare(item.Extension, ".mm", StringComparison.OrdinalIgnoreCase) == 0) || (String.Compare(item.Extension, ".m", StringComparison.OrdinalIgnoreCase) == 0))
                {
                    if (item.Build)
                    {
                        ProjectFile fileItem = (ProjectFile)item;
                        ProjectBuildFile buildFileItem = new ProjectBuildFile(fileItem);
                        _projectItems.Add(buildFileItem);
                        _sourcesBuildPhases[configuration].Files.Add(buildFileItem);
                    }
                    else if (!item.Build)
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

        private void PrepareResourceFiles(Strings sourceFiles, Project project, Project.Configuration configuration, string workspacePath = null)
        {
            foreach (string file in sourceFiles)
            {
                ProjectFileSystemItem item = AddInFileSystem(file, workspacePath);

                item.Build = true;
                item.Source = true;

                ProjectFile fileItem = (ProjectFile)item;
                ProjectBuildFile buildFileItem = new ProjectBuildFile(fileItem);
                _projectItems.Add(buildFileItem);
                _resourcesBuildPhases[configuration].Files.Add(buildFileItem);
            }
        }

        private void PrepareExternalResourceFiles(Project project, Project.Configuration configuration)
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
            PrepareResourceFiles(externalResourceFiles, project, configuration, workspacePath);
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

            if (configuration.ResolvedDependencies.Any())
            {
                _projectsFolder = new ProjectFolder("Projects", true);
                _projectItems.Add(_projectsFolder);
                _mainGroup.Children.Add(_projectsFolder);
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

        private ProjectFileSystemItem AddInFileSystem(string fullPath, string workspacePath = null, bool applyWorkspaceOnlyToRoot = false)
        {
            // Search in existing roots.
            foreach (ProjectFileSystemItem item in _projectItems.Where(item => item is ProjectFileSystemItem))
            {
                if (fullPath.StartsWith(item.FullPath))
                {
                    if (fullPath.Length > item.FullPath.Length)
                        return AddInFileSystem(item, fullPath.Substring(item.FullPath.Length + 1), applyWorkspaceOnlyToRoot ? null : workspacePath);
                }
            }

            // Not found in existing root, create a new root for this item.
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

        private ProjectFileSystemItem AddInFileSystem(ProjectFileSystemItem parent, string remainingPath, string workspacePath)
        {
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
            }
            parent.Children.Sort((f1, f2) => f1.Name.CompareTo(f2.Name));
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

            options["Archs"] = "\"$(ARCHS_STANDARD_32_64_BIT)\"";
            options["CodeSignEntitlements"] = RemoveLineTag;
            options["DevelopmentTeam"] = RemoveLineTag;
            options["InfoPListFile"] = RemoveLineTag;
            options["IPhoneOSDeploymentTarget"] = RemoveLineTag;
            options["MacOSDeploymentTarget"] = RemoveLineTag;
            options["ProvisioningProfile"] = RemoveLineTag;
            options["RemoveLibraryPaths"] = "";
            options["RemoveSpecificDeviceLibraryPaths"] = "";
            options["RemoveSpecificSimulatorLibraryPaths"] = "";
            options["SDKRoot"] = "iphoneos";
            options["SpecificLibraryPaths"] = RemoveLineTag;
            options["TargetedDeviceFamily"] = "1,2";
            options["UsePrecompiledHeader"] = "NO";
            options["PrecompiledHeader"] = RemoveLineTag;
            options["ValidArchs"] = RemoveLineTag;

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.AlwaysSearchUserPaths.Disable, () => options["AlwaysSearchUserPaths"] = "NO"),
                Options.Option(Options.XCode.Compiler.AlwaysSearchUserPaths.Enable, () => options["AlwaysSearchUserPaths"] = "YES")
                );

            Options.XCode.Compiler.Archs archs = Options.GetObject<Options.XCode.Compiler.Archs>(conf);
            if (archs != null)
                options["Archs"] = archs.Value;

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.AutomaticReferenceCounting.Disable, () => options["AutomaticReferenceCounting"] = "NO"),
                Options.Option(Options.XCode.Compiler.AutomaticReferenceCounting.Enable, () => options["AutomaticReferenceCounting"] = "YES")
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
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP98, () => options["CppStandard"] = "c++98"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP11, () => options["CppStandard"] = "c++11"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP14, () => options["CppStandard"] = "c++14"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU98, () => options["CppStandard"] = "gnu++98"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU11, () => options["CppStandard"] = "gnu++11"),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU14, () => options["CppStandard"] = "gnu++14")
                );


            Options.XCode.Compiler.DevelopmentTeam developmentTeam = Options.GetObject<Options.XCode.Compiler.DevelopmentTeam>(conf);
            if (developmentTeam != null)
                options["DevelopmentTeam"] = developmentTeam.Value;

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
                Options.Option(Options.XCode.Compiler.Exceptions.Disable, () => { options["CppExceptionHandling"] = "NO"; options["ObjCExceptionHandling"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.Exceptions.Enable, () => { options["CppExceptionHandling"] = "YES"; options["ObjCExceptionHandling"] = "YES"; }),
                Options.Option(Options.XCode.Compiler.Exceptions.EnableCpp, () => { options["CppExceptionHandling"] = "YES"; options["ObjCExceptionHandling"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.Exceptions.EnableObjC, () => { options["CppExceptionHandling"] = "NO"; options["ObjCExceptionHandling"] = "YES"; })
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

            Options.XCode.Compiler.ValidArchs validArchs = Options.GetObject<Options.XCode.Compiler.ValidArchs>(conf);
            if (validArchs != null)
                options["ValidArchs"] = validArchs.Archs;

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.Warning64To32BitConversion.Disable, () => options["Warning64To32BitConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.Warning64To32BitConversion.Enable, () => options["Warning64To32BitConversion"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningBooleanConversion.Disable, () => options["WarningBooleanConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningBooleanConversion.Enable, () => options["WarningBooleanConversion"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningConstantConversion.Disable, () => options["WarningConstantConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningConstantConversion.Enable, () => options["WarningConstantConversion"] = "YES")
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
                Options.Option(Options.XCode.Compiler.WarningIntConversion.Disable, () => options["WarningIntConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningIntConversion.Enable, () => options["WarningIntConversion"] = "YES")
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
                Options.Option(Options.XCode.Compiler.WarningUndeclaredSelector.Disable, () => options["WarningUndeclaredSelector"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUndeclaredSelector.Enable, () => options["WarningUndeclaredSelector"] = "YES")
            );

            Options.SelectOption(conf,
                Options.Option(Options.XCode.Compiler.WarningUniniatializedAutos.Disable, () => options["WarningUniniatializedAutos"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUniniatializedAutos.Enable, () => options["WarningUniniatializedAutos"] = "YES")
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

            if (conf.PrecompHeader != null)
            {
                options["UsePrecompiledHeader"] = "YES";

                string workspacePath = Util.GetCapitalizedPath(Directory.GetParent(conf.ProjectFullFileNameWithExtension).FullName);
                string precompiledHeaderFullPath = Util.GetCapitalizedPath(project.SourceRootPath + FolderSeparator + conf.PrecompHeader);
                options["PrecompiledHeader"] = Util.PathGetRelative(workspacePath, precompiledHeaderFullPath);
            }

            OrderableStrings includePaths = conf.IncludePaths;
            includePaths.AddRange(conf.DependenciesIncludePaths);
            options["IncludePaths"] = includePaths.JoinStrings(",\n", "\t\t\t\t\t\"", "\"").TrimEnd('\n');
            if (conf.LibraryPaths.Count == 0)
            {
                options["LibraryPaths"] = RemoveLineTag;
                options["RemoveLibraryPaths"] = RemoveLineTag;
            }
            else
            {
                options["LibraryPaths"] = conf.LibraryPaths.JoinStrings(",\n", "\t\t\t\t\t\"", "\"").TrimEnd('\n');
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

            Strings linkerOptions = new Strings(conf.AdditionalLinkerOptions);
            linkerOptions.Add("-ObjC");
            linkerOptions.AddRange(conf.LibraryFiles.Select(library => "-l" + library));

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
            public static Resolver Resolver { get; set; } = new Resolver();

            public static string ResolveProjectPaths(Project project, string stringToResolve)
            {
                using (Resolver.NewScopedParameter("project", project))
                {
                    string resolvedString = Resolver.Resolve(stringToResolve);
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

        private class XCodeProjIdGenerator
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
            PBXProject,
            PBXReferenceProxy,
            PBXResourcesBuildPhase,
            PBXSourcesBuildPhase,
            PBXVariantGroup,
            PBXTargetDependency,
            XCBuildConfiguration_NativeTarget,
            XCBuildConfiguration_UnitTestTarget,
            XCBuildConfiguration_Project,
            XCConfigurationList,
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
                return Identifier.CompareTo(other.Identifier);
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
            private string _projectName;

            public ProjectReference(string fullPath)
                : base(fullPath)
            {
                _projectName = Name.Substring(0, Name.LastIndexOf('.'));
            }



            public ProjectReference(ItemSection itemSection, string identifier)
                : base(ItemSection.PBXProject, identifier)
            {
                _projectName = identifier;
            }

            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.SOURCE_ROOT; } }
            public string ProjectName { get { return _projectName; } }
        }

        private class ProjectOutputFile : ProjectFile
        {
            private Project.Configuration _conf;

            public ProjectOutputFile(string fullPath)
                : base(fullPath)
            {
            }

            public ProjectOutputFile(Project.Configuration conf)
                : this(conf.TargetPath + System.IO.Path.DirectorySeparatorChar + conf.TargetFilePrefix + conf.TargetFileName + GetFileExtension(conf))
            {
                _conf = conf;
            }

            static public string GetFileExtension(Project.Configuration conf)
            {
                switch (conf.Output)
                {
                    case Project.Configuration.OutputType.Lib:
                        return ".a";
                    case Project.Configuration.OutputType.Exe:
                        return ""; // Mac executable
                    case Project.Configuration.OutputType.IosApp:
                        return ".app";
                    case Project.Configuration.OutputType.IosTestBundle:
                        return ".xctest";
                    default:
                        return "";
                }
            }

            public override SourceTreeSetting SourceTreeValue { get { return SourceTreeSetting.BUILT_PRODUCTS_DIR; } }

            public Project.Configuration.OutputType OutputType { get { return _conf.Output; } }

            public string BuildableName { get { return (OutputType == Project.Configuration.OutputType.Lib ? "lib" : "") + Name; } }
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
            private static readonly string s_frameworkPath = "System" + System.IO.Path.DirectorySeparatorChar + "Library" + System.IO.Path.DirectorySeparatorChar + "Frameworks" + System.IO.Path.DirectorySeparatorChar;
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
            private static readonly string s_frameworkPath = ".." + System.IO.Path.DirectorySeparatorChar + ".." + System.IO.Path.DirectorySeparatorChar
                + "Library" + System.IO.Path.DirectorySeparatorChar
                + "Frameworks" + System.IO.Path.DirectorySeparatorChar;
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
            public ProjectFolder(string fullPath, bool removePathLine = false) : base(ItemSection.PBXGroup, fullPath)
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
            private ProjectFileBase _file;

            public ProjectBuildFile(ProjectFileBase file) : base(ItemSection.PBXBuildFile, file.Name)
            {
                _file = file;
            }

            public ProjectFileBase File { get { return _file; } }
        }

        private abstract class ProjectBuildPhase : ProjectItem
        {
            private readonly List<ProjectBuildFile> _files;
            private uint _buildActionMask = 0;
            private int _runOnlyForDeploymentPostprocessing;

            public ProjectBuildPhase(ItemSection section, string phaseName, uint buildActionMask)
                : base(section, phaseName)
            {
                _files = new List<ProjectBuildFile>();
                _buildActionMask = buildActionMask;
                _runOnlyForDeploymentPostprocessing = 0;
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

            public List<ProjectBuildFile> Files { get { return _files; } }
            public uint BuildActionMask { get { return _buildActionMask; } }
            public int RunOnlyForDeploymentPostprocessing { get { return _runOnlyForDeploymentPostprocessing; } }
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

        private class ProjectNativeTarget : ProjectItem
        {
            private ProjectConfigurationList _configurationList;
            private ProjectOutputFile _outputFile;
            private string _productType;
            private ProjectResourcesBuildPhase _resourcesBuildPhase;
            private ProjectSourcesBuildPhase _sourcesBuildPhase;
            private ProjectFrameworksBuildPhase _frameworksBuildPhase;
            private string _productInstallPath;
            private List<ProjectTargetDependency> _dependencies;

            public ProjectNativeTarget(string identifier)
                : base(ItemSection.PBXNativeTarget, identifier)
            {
                // Only for Uid computation.
                _outputFile = null;
            }

            public ProjectNativeTarget(Project project)
                : base(ItemSection.PBXNativeTarget, project.Name)
            {
                // Only for Uid computation.
                _outputFile = null;
            }

            public ProjectNativeTarget(string identifier, ProjectOutputFile outputFile, ProjectConfigurationList configurationList, List<ProjectTargetDependency> dependencies)
                : base(ItemSection.PBXNativeTarget, identifier)
            {
                _configurationList = configurationList;
                _outputFile = outputFile;
                _dependencies = dependencies;
                switch (_outputFile.OutputType)
                {
                    case Project.Configuration.OutputType.Lib:
                        _productType = "com.apple.product-type.library.static";
                        _productInstallPath = RemoveLineTag;
                        break;
                    case Project.Configuration.OutputType.IosTestBundle:
                        _productType = "com.apple.product-type.bundle.unit-test";
                        _productInstallPath = "$(HOME)/Applications";
                        break;
                    case Project.Configuration.OutputType.IosApp:
                        _productType = "com.apple.product-type.application";
                        _productInstallPath = "$(HOME)/Applications";
                        break;
                    default:
                        _productType = "com.apple.product-type.tool";
                        _productInstallPath = RemoveLineTag;
                        break;
                }
            }

            public override void GetAdditionalResolverParameters(ProjectItem item, Resolver resolver, ref Dictionary<string, string> resolverParameters)
            {
                if (null == _outputFile)
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

            public ProjectResourcesBuildPhase ResourcesBuildPhase { get { return _resourcesBuildPhase; } set { _resourcesBuildPhase = value; } }
            public ProjectSourcesBuildPhase SourcesBuildPhase { get { return _sourcesBuildPhase; } set { _sourcesBuildPhase = value; } }
            public ProjectFrameworksBuildPhase FrameworksBuildPhase { get { return _frameworksBuildPhase; } set { _frameworksBuildPhase = value; } }
            public ProjectOutputFile OutputFile { get { return _outputFile; } }
            public string ProductType { get { return _productType; } }
            public ProjectConfigurationList ConfigurationList { get { return _configurationList; } }
            public string ProductInstallPath { get { return _productInstallPath; } }
            public List<ProjectTargetDependency> Dependencies { get { return _dependencies; } }
        }

        private class ProjectBuildConfiguration : ProjectItem
        {
            private Project.Configuration _configuration;
            private XCodeOptions _options;

            public ProjectBuildConfiguration(ItemSection section, string configurationName, Project.Configuration configuration, XCodeOptions options)
                : base(section, configurationName)
            {
                _configuration = configuration;
                _options = options;
            }

            public XCodeOptions Options { get { return _options; } }
            public Project.Configuration Configuration { get { return _configuration; } }
            public string Optimization { get { return _configuration.Target.Name; } }
        }

        private class ProjectBuildConfigurationForTarget : ProjectBuildConfiguration
        {
            private ProjectNativeTarget _nativeTarget;

            public ProjectBuildConfigurationForTarget(ItemSection section, Project.Configuration configuration, ProjectNativeTarget nativeTarget, XCodeOptions options)
                : base(section, configuration.Target.Name, configuration, options)
            {
                _nativeTarget = nativeTarget;
            }

            public ProjectNativeTarget NativeTarget { get { return _nativeTarget; } }
        }

        private class ProjectBuildConfigurationForNativeTarget : ProjectBuildConfigurationForTarget
        {
            public ProjectBuildConfigurationForNativeTarget(Project.Configuration configuration, ProjectNativeTarget nativeTarget, XCodeOptions options)
                : base(ItemSection.XCBuildConfiguration_NativeTarget, configuration, nativeTarget, options)
            { }
        }

        private class ProjectBuildConfigurationForUnitTestTarget : ProjectBuildConfigurationForTarget
        {
            public ProjectBuildConfigurationForUnitTestTarget(Project.Configuration configuration, ProjectNativeTarget nativeTarget, XCodeOptions options)
                : base(ItemSection.XCBuildConfiguration_UnitTestTarget, configuration, nativeTarget, options)
            { }

            public override void GetAdditionalResolverParameters(ProjectItem item, Resolver resolver, ref Dictionary<string, string> resolverParameters)
            {
                string testHostParam = RemoveLineTag;

                // Lookup for the app in the unit test dependencies.
                ProjectTargetDependency testHostTargetDependency =
                    NativeTarget.Dependencies.Find(dependency => dependency.NativeTarget != null && dependency.NativeTarget.OutputFile.OutputType == Project.Configuration.OutputType.IosApp);

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
            private ProjectNativeTarget _nativeTarget;
            private string _developmentTeam;
            private ProjectConfigurationList _configurationList;
            private string _compatibilityVersion;
            private List<ProjectNativeTarget> _targets;
            private Dictionary<ProjectFolder, ProjectReference> _projectReferences;
            private bool _iCloudSupport;

            public ProjectMain(string projectName, ProjectFolder mainGroup, ProjectConfigurationList configurationList, List<ProjectNativeTarget> targets, bool iCloudSupport, string developmentTeam)
                : base(ItemSection.PBXProject, projectName)
            {
                _nativeTarget = null;
                _mainGroup = mainGroup;
                _developmentTeam = developmentTeam;
                _configurationList = configurationList;
                _compatibilityVersion = "Xcode 3.2";
                _targets = targets;
                _projectReferences = new Dictionary<ProjectFolder, ProjectReference>();
                _iCloudSupport = iCloudSupport;
            }

            public ProjectMain(ProjectNativeTarget nativeTarget, ProjectFolder mainGroup, ProjectConfigurationList configurationList, bool iCloudSupport, string developmentTeam)
                : base(ItemSection.PBXProject, nativeTarget.Identifier)
            {
                _nativeTarget = nativeTarget;
                _mainGroup = mainGroup;
                _developmentTeam = developmentTeam;
                _configurationList = configurationList;
                _compatibilityVersion = "Xcode 3.2";
                _targets = new List<ProjectNativeTarget> { nativeTarget };
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
                foreach (ProjectNativeTarget target in _targets)
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
                foreach (ProjectNativeTarget target in _targets)
                {
                    using (resolver.NewScopedParameter("item", target))
                    using (resolver.NewScopedParameter("project", this))
                    {
                        targetAttributes += resolver.Resolve(Template.ProjectTargetAttribute);
                    }
                }
                resolverParameters.Add("itemTargetAttributes", targetAttributes);
            }

            public ProjectNativeTarget NativeTarget { get { return _nativeTarget; } }
            public ProjectFolder MainGroup { get { return _mainGroup; } }
            public string DevelopmentTeam { get { return _developmentTeam; } }
            public ProjectConfigurationList ConfigurationList { get { return _configurationList; } }
            public string CompatibilityVersion { get { return _compatibilityVersion; } }
            public string ICloudSupport { get { return _iCloudSupport ? "1" : "0"; } }
        }
    }
}
