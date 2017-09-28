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
using Sharpmake.Generators.FastBuild;

#pragma warning disable 0162    // Disables "unreacheable code" warning
#pragma warning disable 0168    // Disables "variable is never used" warning
#pragma warning disable 0219    // Disables "variable assigned but it's value is never used" warning

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Vcxproj
    {
        public enum BuildStep
        {
            PreBuild = 0x01,
            PreBuildCustomAction = 0x02,
            PostBuild = 0x03,
            PostBuildCustomAction = 0x04,
        }

        class GenerationContext : IVcxprojGenerationContext
        {
            private Options.ExplicitOptions _options;
            private IDictionary<string, string> _cmdLineOptions;
            private Project.Configuration _configuration;
            private Resolver _envVarResolver;

            public Builder Builder { get; }
            public string ProjectPath { get; }
            public string ProjectDirectory { get; }
            public string ProjectFileName { get; }
            public string ProjectDirectoryCapitalized { get; }
            public string ProjectSourceCapitalized { get; }
            public Project Project { get; }
            public Project.Configuration Configuration
            {
                get
                {
                    Debug.Assert(_configuration != null);
                    return _configuration;
                }
                set
                {
                    _configuration = value;
                }
            }
            public IReadOnlyList<Project.Configuration> ProjectConfigurations { get; }
            public DevEnv DevelopmentEnvironment => Configuration.Target.GetFragment<DevEnv>();
            public DevEnvRange DevelopmentEnvironmentsRange { get; }
            public Options.ExplicitOptions Options
            {
                get
                {
                    Debug.Assert(_options != null);
                    return _options;
                }
                set
                {
                    _options = value;
                }
            }
            public IDictionary<string, string> CommandLineOptions
            {
                get
                {
                    Debug.Assert(_cmdLineOptions != null);
                    return _cmdLineOptions;
                }
                set
                {
                    _cmdLineOptions = value;
                }
            }
            public Resolver EnvironmentVariableResolver
            {
                get
                {
                    Debug.Assert(_envVarResolver != null);
                    return _envVarResolver;
                }
                set
                {
                    _envVarResolver = value;
                }
            }
            public IReadOnlyDictionary<Platform, IPlatformVcxproj> PresentPlatforms { get; }

            public GenerationContext(Builder builder, string projectPath, Project project, IEnumerable<Project.Configuration> projectConfigurations)
            {
                Builder = builder;

                FileInfo fileInfo = new FileInfo(projectPath);
                ProjectPath = fileInfo.FullName;
                ProjectDirectory = Path.GetDirectoryName(ProjectPath);
                ProjectFileName = Path.GetFileName(ProjectPath);
                Project = project;

                ProjectDirectoryCapitalized = Util.GetCapitalizedPath(ProjectDirectory);
                ProjectSourceCapitalized = Util.GetCapitalizedPath(Project.SourceRootPath);

                ProjectConfigurations = SortConfigurations(projectConfigurations).ToArray();
                DevelopmentEnvironmentsRange = new DevEnvRange(projectConfigurations);

                PresentPlatforms = projectConfigurations.Select(conf => conf.Platform).Distinct().ToDictionary(p => p, p => PlatformRegistry.Get<IPlatformVcxproj>(p));
            }

            public void Reset()
            {
                Options = null;
                CommandLineOptions = null;
                Configuration = null;
                EnvironmentVariableResolver = null;
            }

            public void SelectOption(params Options.OptionAction[] options)
            {
                Sharpmake.Options.SelectOption(Configuration, options);
            }

            public void SelectOptionWithFallback(Action fallbackAction, params Options.OptionAction[] options)
            {
                Sharpmake.Options.SelectOptionWithFallback(Configuration, fallbackAction, options);
            }

            private IEnumerable<Project.Configuration> SortConfigurations(IEnumerable<Project.Configuration> unsortedConfigurations)
            {
                // Need to sort by name and platform
                List<Project.Configuration> configurations = new List<Project.Configuration>();
                configurations.AddRange(unsortedConfigurations.OrderBy(conf => conf.Name + conf.Platform));

                // validate that 2 conf name in the same project don't have the same name
                Dictionary<string, Project.Configuration> configurationNameMapping = new Dictionary<string, Project.Configuration>();

                bool hasNvShieldConfiguration = false;
                foreach (Project.Configuration conf in configurations)
                {
                    var projectUniqueName = conf.Name + Util.GetPlatformString(conf.Platform, conf.Project) + conf.Target.GetFragment<DevEnv>();
                    configurationNameMapping[projectUniqueName] = conf;
                }

                return configurations;
            }
        }

        public void Generate(Builder builder, Project project, List<Project.Configuration> configurations, string projectFile, List<string> generatedFiles, List<string> skipFiles)
        {
            var context = new GenerationContext(builder, projectFile, project, configurations);
            FileInfo fileInfo = new FileInfo(projectFile);
            string projectPath = fileInfo.Directory.FullName;
            string projectFileName = fileInfo.Name;
            GenerateImpl(context, generatedFiles, skipFiles);
        }
        public static string FastBuildCustomArguments = "";
        public const string ProjectExtension = ".vcxproj";
        private const string ProjectFilterExtension = ".filters";
        private const string CopyDependenciesExtension = "_runtimedependencies.txt";

        /// <summary>
        /// Generate a pseudo Guid base on relative path from the Project CsPath to the generated files
        /// Need to do it that way because many vcproj may be generated from the same Project.
        /// </summary>
        private string GetProjectFileGuid(string outputProjectFile, Project project)
        {
            string reletiveToCsProjectFile = Util.PathGetRelative(project.SharpmakeCsPath, outputProjectFile);
            return Util.BuildGuid(reletiveToCsProjectFile).ToString().ToUpper();
        }

        private void GenerateImpl(GenerationContext context, IList<string> generatedFiles, IList<string> skipFiles)
        {
            FileName = context.ProjectPath;

            // set generator information
            string projectName = null;
            foreach (var conf in context.ProjectConfigurations)
            {
                // Get the name of the project by reading configurations. Make sure that all
                // configurations use the same name!
                if (projectName == null)
                    projectName = conf.ProjectName;
                else if (projectName != conf.ProjectName)
                    throw new Error("Project configurations in the same project files must be the same: {0} != {1} in {2}", projectName, conf.ProjectName, context.ProjectFileName);

                var platformVcxproj = PlatformRegistry.Get<IPlatformVcxproj>(conf.Platform);
                conf.GeneratorSetGeneratedInformation(
                    platformVcxproj.ExecutableFileExtension,
                    platformVcxproj.PackageFileExtension,
                    platformVcxproj.SharedLibraryFileExtension,
                    platformVcxproj.ProgramDatabaseFileExtension);
            }

            // source control
            string sccProjectName = FileGeneratorUtilities.RemoveLineTag;
            string sccLocalPath = FileGeneratorUtilities.RemoveLineTag;
            string sccProvider = FileGeneratorUtilities.RemoveLineTag;
            if (context.Project.PerforceRootPath != null)
            {
                sccProjectName = "Perforce Project";
                sccLocalPath = Util.PathGetRelative(context.ProjectDirectory, context.Project.PerforceRootPath);
                sccProvider = "MSSCCI:Perforce SCM";
            }

            var fileGenerator = new FileGenerator();

            var firstConf = context.ProjectConfigurations.First();

            // xml begin header
            using (fileGenerator.Declare("toolsVersion", context.DevelopmentEnvironmentsRange.MinDevEnv.GetVisualProjectToolsVersionString()))
            {
                fileGenerator.Write(Template.Project.ProjectBegin);
            }

            WriteCustomProperties(context, fileGenerator);

            foreach (var platformVcxproj in context.PresentPlatforms.Values)
                platformVcxproj.GenerateSdkVcxproj(context, fileGenerator);

            bool hasFastBuildConfig = false;
            bool hasNonFastBuildConfig = false;

            fileGenerator.Write(Template.Project.ProjectBeginConfigurationDescription);
            // xml header contain description of each target
            var platformNames = new Strings();
            var configNames = new Strings();
            foreach (var conf in context.ProjectConfigurations)
            {
                var platformName = Util.GetPlatformString(conf.Platform, conf.Project);
                platformNames.Add(platformName);
                configNames.Add(conf.Name);

                if (conf.IsFastBuild)
                    hasFastBuildConfig = true;
                else
                    hasNonFastBuildConfig = true;
            }

            // write all combinations to avoid "Incomplete Configuration" VS warning
            foreach (var configName in configNames.SortedValues)
            {
                foreach (var platformName in platformNames.SortedValues)
                {
                    using (fileGenerator.Declare("platformName", platformName))
                    using (fileGenerator.Declare("configName", configName))
                    {
                        fileGenerator.Write(Template.Project.ProjectConfigurationDescription);
                    }
                }
            }

            fileGenerator.Write(Template.Project.ProjectEndConfigurationDescription);

            //checking only the first one, having one with CLR support and others without would be an error
            bool clrSupport = Util.IsDotNet(firstConf);

            string projectKeyword = FileGeneratorUtilities.RemoveLineTag;
            string targetFrameworkString = FileGeneratorUtilities.RemoveLineTag;

            if (clrSupport)
            {
                projectKeyword = "ManagedCProj";
                targetFrameworkString = Util.GetDotNetTargetString(firstConf.Target.GetFragment<DotNetFramework>());
            }

            string windowsSdkDir10 = FileGeneratorUtilities.RemoveLineTag;
            string targetPlatformVersionString = FileGeneratorUtilities.RemoveLineTag;
            if (context.DevelopmentEnvironmentsRange.MinDevEnv >= DevEnv.vs2015)
            {
                windowsSdkDir10 = KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot10);
                targetPlatformVersionString = KitsRootPaths.GetWindowsTargetPlatformVersion();
            }

            string vc11TargetsPath = Template.Project.ProjectDescriptionVC11TargetsPath;
            if (context.DevelopmentEnvironmentsRange.MinDevEnv >= DevEnv.vs2013)
                vc11TargetsPath = FileGeneratorUtilities.RemoveLineTag;

            // xml end header

            using (fileGenerator.Declare("projectName", projectName))
            using (fileGenerator.Declare("guid", firstConf.ProjectGuid))
            using (fileGenerator.Declare("sccProjectName", sccProjectName))
            using (fileGenerator.Declare("sccLocalPath", sccLocalPath))
            using (fileGenerator.Declare("sccProvider", sccProvider))
            using (fileGenerator.Declare("targetFramework", targetFrameworkString))
            using (fileGenerator.Declare("targetPlatformVersion", targetPlatformVersionString))
            using (fileGenerator.Declare("windowsSdkDir10", windowsSdkDir10))
            using (fileGenerator.Declare("projectKeyword", projectKeyword))
            using (fileGenerator.Declare("vc11TargetsPath", vc11TargetsPath))
            {
                fileGenerator.Write(Template.Project.ProjectDescription, FileGeneratorUtilities.RemoveLineTag);
            }

            foreach (var platform in context.PresentPlatforms.Values)
                platform.GeneratePlatformSpecificProjectDescription(context, fileGenerator);

            fileGenerator.Write(Template.Project.ProjectDescriptionEnd, FileGeneratorUtilities.RemoveLineTag);

            foreach (var platform in context.PresentPlatforms.Values)
                platform.GenerateProjectPlatformSdkDirectoryDescription(context, fileGenerator);

            // generate all configuration options onces...
            Dictionary<Project.Configuration, Options.ExplicitOptions> options = new Dictionary<Project.Configuration, Options.ExplicitOptions>();
            Dictionary<Project.Configuration, ProjectOptionsGenerator.VcxprojCmdLineOptions> cmdLineOptions = new Dictionary<Project.Configuration, ProjectOptionsGenerator.VcxprojCmdLineOptions>();
            ProjectOptionsGenerator projectOptionsGen = new ProjectOptionsGenerator();
            foreach (Project.Configuration conf in context.ProjectConfigurations)
            {
                var confOptions = new Options.ExplicitOptions();
                var confCmdLineOptions = new ProjectOptionsGenerator.VcxprojCmdLineOptions();

                context.Configuration = conf;
                context.Options = confOptions;
                context.CommandLineOptions = confCmdLineOptions;
                projectOptionsGen.GenerateOptions(context);
                context.Reset(); // just a safety, not necessary to clean up

                options.Add(conf, confOptions);
                cmdLineOptions.Add(conf, confCmdLineOptions);
            }

            // user file
            string projectFilePath = FileName + ProjectExtension;
            UserFile uf = new UserFile(projectFilePath);
            uf.GenerateUserFile(context.Builder, context.Project, context.ProjectConfigurations, generatedFiles, skipFiles);

            // configuration general
            foreach (Project.Configuration conf in context.ProjectConfigurations)
            {
                context.Configuration = conf;

                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                using (fileGenerator.Declare("conf", conf))
                using (fileGenerator.Declare("options", options[conf]))
                using (fileGenerator.Declare("clrSupport", (conf.IsFastBuild || !clrSupport) ? FileGeneratorUtilities.RemoveLineTag : clrSupport.ToString().ToLower()))
                {
                    PlatformRegistry.Get<IPlatformVcxproj>(conf.Platform).GenerateProjectConfigurationGeneral(context, fileGenerator);
                }
            }

            // .props files
            fileGenerator.Write(Template.Project.ProjectAfterConfigurationsGeneral);
            foreach (string propsFile in context.Project.CustomPropsFiles)
            {
                string capitalizedFile = Project.GetCapitalizedFile(propsFile) ?? propsFile;

                string relativeFile = Util.PathGetRelative(context.ProjectDirectoryCapitalized, capitalizedFile);
                using (fileGenerator.Declare("importedPropsFile", relativeFile))
                {
                    fileGenerator.Write(Template.Project.ProjectImportedProps);
                }
            }
            fileGenerator.Write(Template.Project.ProjectImportedPropsEnd);
            fileGenerator.Write(Template.Project.ProjectAfterConfigurationsGeneralImportPropertySheets);
            foreach (var platform in context.PresentPlatforms.Values)
                platform.GenerateProjectPlatformImportSheet(context, fileGenerator);
            fileGenerator.Write(Template.Project.ProjectAfterImportedProps);

            // configuration general2
            foreach (Project.Configuration conf in context.ProjectConfigurations)
            {
                context.Configuration = conf;

                using (fileGenerator.Declare("project", context.Project))
                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                using (fileGenerator.Declare("conf", conf))
                using (fileGenerator.Declare("options", options[conf]))
                {
                    var platformVcxproj = PlatformRegistry.Get<IPlatformVcxproj>(conf.Platform);

                    if (conf.IsFastBuild)
                    {
                        string fastBuildCommandLineOptions = "-vs";

                        if (FastBuildSettings.FastBuildReport)
                            fastBuildCommandLineOptions += " -report";

                        if (FastBuildSettings.FastBuildSummary)
                            fastBuildCommandLineOptions += " -summary";

                        if (FastBuildSettings.FastBuildVerbose)
                            fastBuildCommandLineOptions += " -verbose";

                        if (FastBuildSettings.FastBuildMonitor)
                            fastBuildCommandLineOptions += " -monitor";

                        // Configuring cache mode if that configuration is allowed to use caching
                        if (conf.FastBuildCacheAllowed)
                        {
                            // Setting the appropriate cache type commandline for that target.
                            switch (FastBuildSettings.CacheType)
                            {
                                case FastBuildSettings.CacheTypes.CacheRead:
                                    fastBuildCommandLineOptions += " -cacheread";
                                    break;
                                case FastBuildSettings.CacheTypes.CacheWrite:
                                    fastBuildCommandLineOptions += " -cachewrite";
                                    break;
                                case FastBuildSettings.CacheTypes.CacheReadWrite:
                                    fastBuildCommandLineOptions += " -cache";
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (FastBuildSettings.FastBuildDistribution && conf.FastBuildDistribution)
                            fastBuildCommandLineOptions += " -dist";

                        if (FastBuildSettings.FastBuildWait)
                            fastBuildCommandLineOptions += " -wait";

                        if (FastBuildSettings.FastBuildNoStopOnError)
                            fastBuildCommandLineOptions += " -nostoponerror";
                        if (FastBuildSettings.FastBuildFastCancel)
                            fastBuildCommandLineOptions += " -fastcancel";

                        if (!string.IsNullOrEmpty(conf.FastBuildCustomArgs))
                        {
                            fastBuildCommandLineOptions += " ";
                            fastBuildCommandLineOptions += conf.FastBuildCustomArgs;
                        }
                        string masterBffPath = Bff.GetMasterBffPath(conf);
                        string masterBffFullName = Bff.GetMasterBffFileName(conf);
                        string relativeMasterBffFile = Util.PathGetRelative(masterBffPath, masterBffFullName, true);
                        string relativeMasterBffPath = Util.PathGetRelative(context.ProjectDirectory, masterBffPath, true);
                        if (relativeMasterBffFile != "fbuild.bff")
                            fastBuildCommandLineOptions += " -config " + relativeMasterBffFile;

                        fastBuildCommandLineOptions += FastBuildCustomArguments;

                        // Make the commandline written in the bff available.
                        Bff.SetCommandLineArguments(conf, fastBuildCommandLineOptions);

                        using (fileGenerator.Declare("relativeMasterBffPath", relativeMasterBffPath))
                        using (fileGenerator.Declare("fastBuildMakeCommandBuild", FastBuildSettings.MakeCommandGenerator.GetCommand(FastBuildMakeCommandGenerator.BuildType.Build, conf, fastBuildCommandLineOptions)))
                        using (fileGenerator.Declare("fastBuildMakeCommandRebuild", FastBuildSettings.MakeCommandGenerator.GetCommand(FastBuildMakeCommandGenerator.BuildType.Rebuild, conf, fastBuildCommandLineOptions)))
                        {
                            platformVcxproj.GenerateProjectConfigurationFastBuildMakeFile(context, fileGenerator);
                        }
                    }
                    else
                    {
                        platformVcxproj.GenerateProjectConfigurationGeneral2(context, fileGenerator);
                    }
                }
            }

            // configuration ItemDefinitionGroup
            foreach (Project.Configuration conf in context.ProjectConfigurations)
            {
                context.Configuration = conf;

                if (!conf.IsFastBuild)
                {
                    using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                    using (fileGenerator.Declare("conf", conf))
                    using (fileGenerator.Declare("project", conf.Project))
                    using (fileGenerator.Declare("target", conf.Target))
                    using (fileGenerator.Declare("options", options[conf]))
                    using (fileGenerator.Declare("clrSupport", !clrSupport ? FileGeneratorUtilities.RemoveLineTag : clrSupport.ToString().ToLower()))
                    {
                        fileGenerator.Write(Template.Project.ProjectConfigurationBeginItemDefinition);

                        IPlatformVcxproj platformVcxproj = PlatformRegistry.Get<IPlatformVcxproj>(conf.Platform);
                        platformVcxproj.GenerateProjectCompileVcxproj(context, fileGenerator);
                        platformVcxproj.GenerateProjectLinkVcxproj(context, fileGenerator);

                        if (conf.EventPreBuild.Count != 0)
                            fileGenerator.Write(Template.Project.ProjectConfigurationsPreBuildEvent);

                        if (conf.EventPreLink.Count != 0)
                            fileGenerator.Write(Template.Project.ProjectConfigurationsPreLinkEvent);

                        if (conf.EventPrePostLink.Count != 0)
                            fileGenerator.Write(Template.Project.ProjectConfigurationsPrePostLinkEvent);

                        if (conf.EventPostBuild.Count != 0)
                            fileGenerator.Write(Template.Project.ProjectConfigurationsPostBuildEvent);

                        if (conf.CustomBuildStep.Count != 0)
                            fileGenerator.Write(Template.Project.ProjectConfigurationsCustomBuildStep);

                        if (conf.EventCustomBuild.Count != 0)
                            fileGenerator.Write(Template.Project.ProjectConfigurationsCustomBuildEvent);

                        if (conf.Platform.IsPC())
                            fileGenerator.Write(Template.Project.ProjectConfigurationsResourceCompile);

                        if (conf.AdditionalManifestFiles.Count != 0 || (Options.GetObjects<Options.Vc.ManifestTool.EnableDpiAwareness>(conf).Count() > 0) && (conf.Platform.IsPC() && conf.Platform.IsMicrosoft()))
                            fileGenerator.Write(Template.Project.ProjectConfigurationsManifestTool);

                        fileGenerator.Write(Template.Project.ProjectConfigurationEndItemDefinition);
                    }
                }
            }

            // For all projects configurations that are fastbuild only, do not add the cpp
            // source file requires to be remove from the projects, so that not 2 same cpp file be in 2 different project.
            // TODO: make a better check
            if (hasNonFastBuildConfig)
                GenerateFilesSection(context, fileGenerator, generatedFiles, skipFiles);
            else
                GenerateBffFilesSection(context, fileGenerator, generatedFiles, skipFiles, false);

            // Import platform makefiles.
            foreach (var platform in context.PresentPlatforms.Values)
                platform.GenerateMakefileConfigurationVcxproj(context, fileGenerator);

            fileGenerator.Write(Template.Project.ProjectTargetsBegin);
            foreach (string targetsFiles in context.Project.CustomTargetsFiles)
            {
                string capitalizedFile = Project.GetCapitalizedFile(targetsFiles) ?? targetsFiles;

                string relativeFile = Util.PathGetRelative(context.ProjectDirectoryCapitalized, capitalizedFile);
                using (fileGenerator.Declare("importedTargetsFile", relativeFile))
                {
                    fileGenerator.Write(Template.Project.ProjectTargetsItem);
                }
            }
            fileGenerator.Write(Template.Project.ProjectTargetsEnd);

            // in case we are using fast build we do not want to write the dependencies
            // in the vcxproj because they are handled internally in the bff
            if (hasNonFastBuildConfig)
                GenerateProjectReferences(context, fileGenerator, options);

            // Environment variables
            var environmentVariables = context.ProjectConfigurations.Select(conf => conf.Platform).Distinct().SelectMany(platform => PlatformRegistry.Get<IPlatformVcxproj>(platform).GetEnvironmentVariables(context));
            if (environmentVariables.Any())
            {
                fileGenerator.Write(Template.Project.ItemGroupBegin);
                foreach (var environmentTuple in environmentVariables)
                {
                    using (fileGenerator.Declare("environmentVariableName", environmentTuple.Identifier))
                    using (fileGenerator.Declare("environmentVariableValue", environmentTuple.Value))
                        fileGenerator.Write(Template.Project.ProjectBuildMacroEnvironmentVariable);
                }
                fileGenerator.Write(Template.Project.ItemGroupEnd);
            }

            // Generate vcxproj configuration to run after a deployment from the PC
            if (context.Project.UseRunFromPcDeployment)
            {
                foreach (var platform in context.PresentPlatforms.Values)
                    platform.GenerateRunFromPcDeployment(context, fileGenerator);
            }

            fileGenerator.Write(Template.Project.ProjectEnd);

            // remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();
            MemoryStream cleanMemoryStream = fileGenerator.ToMemoryStream();

            FileInfo projectFileInfo = new FileInfo(context.ProjectPath + ProjectExtension);
            if (context.Builder.Context.WriteGeneratedFile(context.Project.GetType(), projectFileInfo, cleanMemoryStream))
                generatedFiles.Add(projectFileInfo.FullName);
            else
                skipFiles.Add(projectFileInfo.FullName);
        }

        private void WriteCustomProperties(IVcxprojGenerationContext context, IFileGenerator fileGenerator)
        {
            if (context.Project.CustomProperties.Keys.Count == 0)
                return;

            fileGenerator.Write(Template.Project.CustomPropertiesStart);
            foreach (var key in context.Project.CustomProperties.Keys)
            {
                using (fileGenerator.Declare("custompropertyname", key))
                using (fileGenerator.Declare("custompropertyvalue", context.Project.CustomProperties[key]))
                    fileGenerator.Write(Template.Project.CustomProperty);
            }
            fileGenerator.Write(Template.Project.CustomPropertiesEnd);
        }

        private struct ProjectDependencyInfo
        {
            public string ProjectFullFileNameWithExtension;
            public string ProjectGuid;
            public bool ContainsASM;
        }

        public string FileName { get; private set; } = string.Empty;

        private string ReadGuidFromProjectFile(Project.Configuration dependency)
        {
            var guidFromProjectFile = Sln.ReadGuidFromProjectFile(dependency.ProjectFullFileNameWithExtension);
            return (string.IsNullOrEmpty(guidFromProjectFile)) ? FileGeneratorUtilities.RemoveLineTag : guidFromProjectFile;
        }

        private void GenerateProjectReferences(
            IVcxprojGenerationContext context,
            IFileGenerator fileGenerator,
            IDictionary<Project.Configuration, Options.ExplicitOptions> optionsDictionary)
        {
            var firstConf = context.ProjectConfigurations.First();

            if (context.Builder.Diagnostics)
            {
                // check consistency
                foreach (var conf in context.ProjectConfigurations)
                {
                    if (firstConf.ReferencesByName.SortedValues.ToString() != conf.ReferencesByName.SortedValues.ToString())
                        throw new Error("ReferencesByName in " + FileName + ProjectExtension + " are different between configurations. Please fix, or split the vcxproj.");

                    if (firstConf.ReferencesByPath.SortedValues.ToString() != conf.ReferencesByPath.SortedValues.ToString())
                        throw new Error("ReferencesByPath in " + FileName + ProjectExtension + " are different between configurations. Please fix, or split the vcxproj.");
                }
            }

            if (firstConf.ReferencesByName.Count != 0)
            {
                fileGenerator.Write(Template.Project.ItemGroupBegin);
                foreach (var referenceName in firstConf.ReferencesByName)
                {
                    bool copyLocal = (firstConf.Project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.DotNetReferences));
                    using (fileGenerator.Declare("include", referenceName))
                    using (fileGenerator.Declare("private", copyLocal.ToString().ToLower())) //ToString().ToLower() as told by msdn for booleans in xml files
                    {
                        if (copyLocal)
                            fileGenerator.Write(Template.Project.ReferenceByName);
                        else
                            fileGenerator.Write(Template.Project.SingleReferenceByName);
                    }
                }
                fileGenerator.Write(Template.Project.ItemGroupEnd);
            }

            fileGenerator.Write(Template.Project.ProjectFilesBegin);

            string externalReferencesCopyLocal = (firstConf.Project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.ExternalReferences)
                                       ? "true"
                                       : FileGeneratorUtilities.RemoveLineTag);

            foreach (var reference in firstConf.ReferencesByPath)
            {
                string nameWithExtension = reference.Split(Util.WindowsSeparator).Last();
                string name = nameWithExtension.Substring(0, nameWithExtension.LastIndexOf('.'));

                using (fileGenerator.Declare("include", name))
                using (fileGenerator.Declare("hintPath", reference))
                using (fileGenerator.Declare("private", externalReferencesCopyLocal))
                {
                    fileGenerator.Write(Template.Project.ReferenceByPath);
                }
            }

            // Write dotNet dependencies references
            {
                // The behavior should be the same than for csproj...
                string projectDependenciesCopyLocal = firstConf.Project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.ProjectReferences).ToString().ToLower();

                Options.ExplicitOptions options = new Options.ExplicitOptions();
                options["ReferenceOutputAssembly"] = FileGeneratorUtilities.RemoveLineTag;
                options["CopyLocalSatelliteAssemblies"] = FileGeneratorUtilities.RemoveLineTag;
                options["LinkLibraryDependencies"] = FileGeneratorUtilities.RemoveLineTag;
                options["UseLibraryDependencyInputs"] = FileGeneratorUtilities.RemoveLineTag;

                // The check for the blobbed is so we add references to blobed projects over non blobed projects.
                var publicDotNetDependenciesConf = context.ProjectConfigurations.Where(x => x.IsBlobbed).FirstOrDefault(x => x.DotNetPublicDependencies.Count > 0) ??
                                                   context.ProjectConfigurations.FirstOrDefault(x => x.DotNetPublicDependencies.Count > 0);

                var privateDotNetDependenciesConf = context.ProjectConfigurations.Where(x => x.IsBlobbed).FirstOrDefault(x => x.DotNetPrivateDependencies.Count > 0) ??
                                                    context.ProjectConfigurations.FirstOrDefault(x => x.DotNetPrivateDependencies.Count > 0);

                var dotNetDependenciesLists = new List<IEnumerable<Project.Configuration>>();
                if (publicDotNetDependenciesConf != null)
                    dotNetDependenciesLists.Add(publicDotNetDependenciesConf.DotNetPublicDependencies.Select(x => x.Configuration));
                if (privateDotNetDependenciesConf != null)
                    dotNetDependenciesLists.Add(privateDotNetDependenciesConf.DotNetPrivateDependencies.Select(x => x.Configuration));

                foreach (var dotNetDependencies in dotNetDependenciesLists)
                {
                    foreach (var dependency in dotNetDependencies)
                    {
                        string include = Util.PathGetRelative(firstConf.ProjectPath, dependency.ProjectFullFileNameWithExtension);

                        // If dependency project is marked as [Compile], read the GUID from the project file
                        if (string.IsNullOrEmpty(dependency.ProjectGuid) || dependency.ProjectGuid == Guid.Empty.ToString())
                        {
                            if (dependency.Project.GetType().IsDefined(typeof(Compile), false))
                                dependency.ProjectGuid = ReadGuidFromProjectFile(dependency);
                        }

                        // avoid linking with .lib from a dependency that doesn't create a lib
                        if (dependency.Output == Project.Configuration.OutputType.DotNetClassLibrary &&
                           !dependency.CppCliExportsNativeLib)
                        {
                            options["LinkLibraryDependencies"] = "false";
                        }
                        else
                        {
                            options["LinkLibraryDependencies"] = FileGeneratorUtilities.RemoveLineTag;
                        }

                        using (fileGenerator.Declare("include", include))
                        using (fileGenerator.Declare("projectGUID", dependency.ProjectGuid ?? FileGeneratorUtilities.RemoveLineTag))
                        using (fileGenerator.Declare("projectRefName", dependency.ProjectName))
                        using (fileGenerator.Declare("private", projectDependenciesCopyLocal))
                        using (fileGenerator.Declare("options", options))
                        {
                            fileGenerator.Write(Template.Project.ProjectReference);
                        }
                    }
                }

                // The check for the blobbed is so we add references to blobbed projects over non blobbed projects.
                var projectReferencesByPathConfig =
                    context.ProjectConfigurations.Where(x => x.IsBlobbed).FirstOrDefault(x => x.ProjectReferencesByPath.Count > 0) ??
                    context.ProjectConfigurations.FirstOrDefault(x => x.ProjectReferencesByPath.Count > 0);

                if (projectReferencesByPathConfig != null)
                {
                    foreach (var projectFileName in projectReferencesByPathConfig.ProjectReferencesByPath)
                    {
                        string projectFullFileNameWithExtension = Util.GetCapitalizedPath(projectFileName);
                        string relativeToProjectFile = Util.PathGetRelative(context.ProjectDirectoryCapitalized, projectFullFileNameWithExtension);
                        string projectGuid = Sln.ReadGuidFromProjectFile(projectFileName);

                        using (fileGenerator.Declare("include", relativeToProjectFile))
                        using (fileGenerator.Declare("projectGUID", projectGuid))
                        using (fileGenerator.Declare("projectRefName", FileGeneratorUtilities.RemoveLineTag))
                        using (fileGenerator.Declare("private", FileGeneratorUtilities.RemoveLineTag))
                        using (fileGenerator.Declare("options", options))
                        {
                            fileGenerator.Write(Template.Project.ProjectReference);
                        }
                    }
                }
            }

            bool addDependencies = false;
            if (context.Project.AllowInconsistentDependencies)
            {
                foreach (var configuration in context.ProjectConfigurations)
                {
                    if (configuration.Output == Project.Configuration.OutputType.Exe || configuration.Output == Project.Configuration.OutputType.Dll ||
                        configuration.Output == Project.Configuration.OutputType.DotNetConsoleApp ||
                        configuration.Output == Project.Configuration.OutputType.DotNetClassLibrary ||
                        configuration.Output == Project.Configuration.OutputType.DotNetWindowsApp)
                    {
                        addDependencies = true;
                        break;
                    }
                }
            }
            else
            {
                if (firstConf.Output == Project.Configuration.OutputType.Exe || firstConf.Output == Project.Configuration.OutputType.Dll ||
                    (firstConf.Output == Project.Configuration.OutputType.Lib && firstConf.ExportAdditionalLibrariesEvenForStaticLib) ||
                    firstConf.Output == Project.Configuration.OutputType.DotNetConsoleApp ||
                    firstConf.Output == Project.Configuration.OutputType.DotNetClassLibrary ||
                    firstConf.Output == Project.Configuration.OutputType.DotNetWindowsApp)
                {
                    addDependencies = true;
                }
            }

            if (addDependencies)
            {
                if (context.Builder.Diagnostics)
                {
                    bool inconsistencyDetected = false;
                    string inconsistencyReports = "";
                    for (int i = 0; i < context.ProjectConfigurations.Count; ++i)
                    {
                        var iDeps = context.ProjectConfigurations.ElementAt(i).ConfigurationDependencies.Where(d => !d.Project.GetType().IsDefined(typeof(Export), false)).Select(x => x.ProjectFullFileNameWithExtension);
                        for (int j = 0; j < context.ProjectConfigurations.Count; ++j)
                        {
                            if (i == j)
                                continue;

                            var jDeps = context.ProjectConfigurations.ElementAt(j).ConfigurationDependencies.Where(d => !d.Project.GetType().IsDefined(typeof(Export), false)).Select(x => x.ProjectFullFileNameWithExtension);

                            var ex = iDeps.Except(jDeps);
                            if (ex.Count() != 0)
                            {
                                inconsistencyDetected = true;
                                var inconsistency = "Config1: " + context.ProjectConfigurations.ElementAt(i) + Environment.NewLine +
                                    "Config2: " + context.ProjectConfigurations.ElementAt(j) + Environment.NewLine + "=> " +
                                    String.Join(Environment.NewLine + "=> ", ex.ToList());
                                inconsistencyReports += inconsistency + Environment.NewLine;
                            }
                        }
                    }

                    if (inconsistencyDetected && context.Project.AllowInconsistentDependencies == false)
                        Builder.Instance.LogErrorLine($"{context.Project.SharpmakeCsFileName}: Error: Dependencies in {FileName}{ProjectExtension} are different between configurations:\n{inconsistencyReports}");
                }

                var dependencies = new UniqueList<ProjectDependencyInfo>();
                foreach (var configuration in context.ProjectConfigurations)
                {
                    foreach (var configurationDependency in configuration.ConfigurationDependencies)
                    {
                        // Ignore projects marked as Export
                        if (configurationDependency.Project.GetType().IsDefined(typeof(Export), false))
                            continue;

                        // Ignore exe and utility outputs
                        if (configurationDependency.Output == Project.Configuration.OutputType.Exe ||
                            configurationDependency.Output == Project.Configuration.OutputType.Utility)
                            continue;

                        ProjectDependencyInfo depInfo;
                        depInfo.ProjectFullFileNameWithExtension = configurationDependency.ProjectFullFileNameWithExtension;

                        // If dependency project is marked as [Compile], read the GUID from the project file
                        depInfo.ProjectGuid = configurationDependency.Project.GetType().IsDefined(typeof(Compile), false) ? ReadGuidFromProjectFile(configurationDependency) : configurationDependency.ProjectGuid;

                        depInfo.ContainsASM = configurationDependency.Project.ContainsASM;

                        dependencies.Add(depInfo);
                    }
                }

                Options.ExplicitOptions options = optionsDictionary[firstConf];
                foreach (var dependencyInfo in dependencies)
                {
                    string include = Util.PathGetRelative(firstConf.ProjectPath, dependencyInfo.ProjectFullFileNameWithExtension);

                    string backupUseLibraryDependencyInputs = options["UseLibraryDependencyInputs"];
                    if (dependencyInfo.ContainsASM)
                    {
                        // Work around ms-build bug 
                        // Obj files generated in referenced projects by MASM are not linked automatically when "Use Library Dependency Inputs" is set to true
                        // https://connect.microsoft.com/VisualStudio/feedback/details/679267/obj-files-generated-in-referenced-projects-by-masm-are-not-linked-automatically-when-use-library-dependency-inputs-is-set-to-true
                        options["UseLibraryDependencyInputs"] = "false";
                    }

                    using (fileGenerator.Declare("include", include))
                    using (fileGenerator.Declare("projectGUID", dependencyInfo.ProjectGuid))
                    using (fileGenerator.Declare("projectRefName", FileGeneratorUtilities.RemoveLineTag)) // not needed it seems
                    using (fileGenerator.Declare("private", FileGeneratorUtilities.RemoveLineTag)) // TODO: check the conditions for a reference to be private
                    using (fileGenerator.Declare("options", options))
                    {
                        fileGenerator.Write(Template.Project.ProjectReference);
                    }

                    options["UseLibraryDependencyInputs"] = backupUseLibraryDependencyInputs;
                }
            }

            fileGenerator.Write(Template.Project.ProjectFilesEnd);

            foreach (var platforms in context.PresentPlatforms.Values)
                platforms.GeneratePlatformReferences(context, fileGenerator);
        }

        private void GenerateBffFilesSection(IVcxprojGenerationContext context, IFileGenerator fileGenerator, IList<string> generatedFiles, IList<string> skipFiles, bool lookIfHasAnyFastBuild)
        {
            // Add FastBuild bff file to Project
            var firstConf = context.ProjectConfigurations.First();
            if (firstConf.IsFastBuild && FastBuildSettings.IncludeBFFInProjects)
            {
                string fastBuildFile = Bff.GetBffFileName(".", context.Configuration.BffFileName);
                fastBuildFile = Util.SimplifyPath(fastBuildFile);

                fileGenerator.Write(Template.Project.ProjectFilesBegin);
                using (fileGenerator.Declare("fastBuildFile", fastBuildFile))
                    fileGenerator.Write(Template.Project.ProjectFilesFastBuildFile);

                if (firstConf.IsMainProject) // add the master bff file to the main project of the solution
                {
                    string masterBffFileName = Bff.GetMasterBffFileName(firstConf);
                    using (fileGenerator.Declare("fastBuildFile", masterBffFileName))
                        fileGenerator.Write(Template.Project.ProjectFilesFastBuildFile);

                    using (fileGenerator.Declare("fastBuildFile", Bff.GetGlobalBffConfigFileName(masterBffFileName)))
                        fileGenerator.Write(Template.Project.ProjectFilesFastBuildFile);
                }
                fileGenerator.Write(Template.Project.ProjectFilesEnd);
            }
        }

        private void GenerateFiltersFile(
            IVcxprojGenerationContext context,
            string filtersFileName,
            IList<Tuple<string, List<ProjectFile>>> allFileLists,
            string relativeCopyDependenciesFileName,
            Resolver resolver,
            IList<string> generatedFiles,
            IList<string> skipFiles
        )
        {
            // write [].vcxproj.filters
            var fileGenerator = new FileGenerator(resolver);
            using (fileGenerator.Declare("toolsVersion", context.DevelopmentEnvironmentsRange.MinDevEnv.GetVisualProjectToolsVersionString()))
            {
                fileGenerator.Write(Vcxproj.Template.Project.Filers.Begin);
            }

            HashSet<string> allFilters = new HashSet<string>();
            foreach (var entry in allFileLists)
            {
                string type = entry.Item1;
                List<ProjectFile> files = entry.Item2;
                if (files.Count != 0)
                {
                    using (fileGenerator.Declare("type", type))
                    {
                        // write include...
                        fileGenerator.Write(Vcxproj.Template.Project.ItemGroupBegin);
                        foreach (ProjectFile file in files)
                        {
                            using (fileGenerator.Declare("file", file))
                            {
                                if (file.FilterPath.Length == 0)
                                {
                                    fileGenerator.Write(Vcxproj.Template.Project.Filers.FileNoFilter);
                                }
                                else
                                {
                                    fileGenerator.Write(Vcxproj.Template.Project.Filers.FileWithFilter);
                                    allFilters.Add(file.FilterPath);
                                }
                            }
                        }

                        fileGenerator.Write(Vcxproj.Template.Project.ItemGroupEnd);
                    }
                }
            }

            if (relativeCopyDependenciesFileName.Length > 0)
            {
                fileGenerator.Write(Vcxproj.Template.Project.ItemGroupBegin);
                using (fileGenerator.Declare("fileName", relativeCopyDependenciesFileName))
                {
                    fileGenerator.Write(Vcxproj.Template.Project.Filers.FileWithDependencyFilter);
                }
                fileGenerator.Write(Vcxproj.Template.Project.ItemGroupEnd);
            }

            // write filters...
            if (allFilters.Count != 0)
            {
                List<string> allFiltersList = new List<string>();

                // generate all possible parent filters
                allFiltersList.AddRange(allFilters);
                foreach (string filter in allFiltersList)
                {
                    string[] parts = filter.Split(Util.WindowsSeparator);
                    string current = parts[0];
                    allFilters.Add(current);
                    for (int i = 1; i < parts.Length - 1; ++i)
                    {
                        current = current + Util.WindowsSeparator + parts[i];
                        allFilters.Add(current);
                    }
                }
                allFiltersList.Clear();

                // sort filters
                allFiltersList.AddRange(allFilters);
                allFiltersList.Sort();

                fileGenerator.Write(Vcxproj.Template.Project.ItemGroupBegin);
                foreach (string filter in allFiltersList)
                {
                    string guid = Util.BuildGuid(filter).ToString();
                    using (fileGenerator.Declare("name", filter))
                    using (fileGenerator.Declare("guid", guid))
                        fileGenerator.Write(Vcxproj.Template.Project.Filers.Filter);
                }
                fileGenerator.Write(Vcxproj.Template.Project.ItemGroupEnd);
            }

            fileGenerator.Write(Vcxproj.Template.Project.Filers.ProjectFiltersEnd);

            // Write the project file
            FileInfo projectFiltersFileInfo = new FileInfo(filtersFileName);

            if (context.Builder.Context.WriteGeneratedFile(context.Project.GetType(), projectFiltersFileInfo, fileGenerator.ToMemoryStream()))
                generatedFiles.Add(projectFiltersFileInfo.FullName);
            else
                skipFiles.Add(projectFiltersFileInfo.FullName);
        }

        private void GenerateFilesSection(IVcxprojGenerationContext context, IFileGenerator fileGenerator, IList<string> generatedFiles, IList<string> skipFiles)
        {
            var platformVcxproj = PlatformRegistry.Get<IPlatformVcxproj>(context.Configuration.Platform);
            string filtersFileName = context.ProjectPath + ProjectExtension + ProjectFilterExtension;
            string copyDependenciesFileName = context.ProjectPath + CopyDependenciesExtension;
            string relativeCopyDependenciesFileName = Util.PathGetRelative(context.ProjectDirectory, copyDependenciesFileName, false);

            Strings projectFiles = context.Project.GetSourceFilesForConfigurations(context.ProjectConfigurations);

            // Add source files
            List<ProjectFile> allFiles = new List<ProjectFile>();
            List<ProjectFile> includeFiles = new List<ProjectFile>();
            List<ProjectFile> sourceFiles = new List<ProjectFile>();
            List<ProjectFile> NatvisFiles = new List<ProjectFile>();
            List<ProjectFile> PRIFiles = new List<ProjectFile>();
            List<ProjectFile> XResourcesReswFiles = new List<ProjectFile>();
            List<ProjectFile> XResourcesImgFiles = new List<ProjectFile>();

            foreach (string file in context.Project.NatvisFiles)
            {
                ProjectFile natvisFile = new ProjectFile(context, file);
                NatvisFiles.Add(natvisFile);
            }

            foreach (string file in context.Project.PRIFiles)
            {
                ProjectFile priFile = new ProjectFile(context, file);
                PRIFiles.Add(priFile);
            }

            foreach (string file in projectFiles)
            {
                ProjectFile projectFile = new ProjectFile(context, file);
                allFiles.Add(projectFile);
            }

            allFiles.Sort((ProjectFile l, ProjectFile r) => { return string.Compare(l.FileNameProjectRelative, r.FileNameProjectRelative, StringComparison.InvariantCulture); });

            // type -> files
            var customSourceFiles = new Dictionary<string, List<ProjectFile>>();
            foreach (ProjectFile projectFile in allFiles)
            {
                string type = null;
                if (context.Project.ExtensionBuildTools.TryGetValue(projectFile.FileExtension, out type))
                {
                    List<ProjectFile> files = null;
                    if (!customSourceFiles.TryGetValue(type, out files))
                    {
                        files = new List<ProjectFile>();
                        customSourceFiles[type] = files;
                    }
                    files.Add(projectFile);
                }
                else if (context.Project.SourceFilesCompileExtensions.Contains(projectFile.FileExtension) ||
                         (String.Compare(projectFile.FileExtension, ".rc", StringComparison.OrdinalIgnoreCase) == 0))
                {
                    sourceFiles.Add(projectFile);
                }
                else // if (projectFile.FileExtension == "h")
                {
                    includeFiles.Add(projectFile);
                }
            }

            // Write header files
            fileGenerator.Write(Template.Project.ProjectFilesBegin);

            bool hasCustomBuildForAllIncludes = context.ProjectConfigurations.First().CustomBuildForAllIncludes != null;

            if (hasCustomBuildForAllIncludes)
            {
                foreach (ProjectFile file in includeFiles)
                {
                    using (fileGenerator.Declare("file", file.FileNameProjectRelative))
                    using (fileGenerator.Declare("filetype", FileGeneratorUtilities.RemoveLineTag))
                    {
                        fileGenerator.Write(Template.Project.ProjectFilesCustomBuildBegin);

                        foreach (Project.Configuration conf in context.ProjectConfigurations)
                        {
                            if (conf.CustomBuildForAllIncludes == null)
                                continue;

                            using (fileGenerator.Declare("conf", conf))
                            using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                            using (fileGenerator.Declare("description", conf.CustomBuildForAllIncludes.Description))
                            using (fileGenerator.Declare("command", conf.CustomBuildForAllIncludes.CommandLines.JoinStrings(Environment.NewLine, escapeXml: true)))
                            using (fileGenerator.Declare("inputs", FileGeneratorUtilities.RemoveLineTag))
                            using (fileGenerator.Declare("outputs", conf.CustomBuildForAllIncludes.Outputs.JoinStrings(";")))
                            {
                                fileGenerator.Write(Template.Project.ProjectFilesCustomBuildDescription);
                                fileGenerator.Write(Template.Project.ProjectFilesCustomBuildCommand);
                                fileGenerator.Write(Template.Project.ProjectFilesCustomBuildInputs);
                                fileGenerator.Write(Template.Project.ProjectFilesCustomBuildOutputs);
                            }
                        }

                        fileGenerator.Write(Template.Project.ProjectFilesCustomBuildEnd);
                    }
                }
            }
            else
            {
                foreach (ProjectFile file in includeFiles)
                {
                    using (fileGenerator.Declare("file", file))
                        fileGenerator.Write(Template.Project.ProjectFilesHeader);
                }
            }
            fileGenerator.Write(Template.Project.ProjectFilesEnd);

            // Write natvis files
            if (context.Project.NatvisFiles.Count > 0 && context.ProjectConfigurations.Any(conf => conf.Target.HaveFragment<DevEnv>() && conf.Target.GetFragment<DevEnv>() >= DevEnv.vs2015))
            {
                fileGenerator.Write(Template.Project.ProjectFilesBegin);
                foreach (ProjectFile file in NatvisFiles)
                {
                    using (fileGenerator.Declare("file", file))
                        fileGenerator.Write(Template.Project.ProjectFilesNatvis);
                }
                fileGenerator.Write(Template.Project.ProjectFilesEnd);
            }

            // Write PRI files
            var writtenPRIFiles = new Strings();
            if (context.Project.PRIFiles.Count > 0)
            {
                fileGenerator.Write(Template.Project.ProjectFilesBegin);
                foreach (string file in context.Project.PRIFiles)
                {
                    ProjectFile projectFile = new ProjectFile(context, file);
                    writtenPRIFiles.Add(projectFile.FileNameProjectRelative);
                    using (fileGenerator.Declare("file", projectFile))
                        fileGenerator.Write(Template.Project.ProjectFilesPRIResources);
                }
                fileGenerator.Write(Template.Project.ProjectFilesEnd);
            }

            foreach (var platform in context.PresentPlatforms.Values)
                platform.GeneratePlatformResourceFileList(context, fileGenerator, writtenPRIFiles, XResourcesReswFiles, XResourcesImgFiles);

            fileGenerator.Write(Template.Project.ProjectFilesBegin);

            // Validation map
            List<List<ProjectFile>> configurationCompiledFiles = new List<List<ProjectFile>>();
            foreach (Project.Configuration conf in context.ProjectConfigurations)
                configurationCompiledFiles.Add(new List<ProjectFile>());

            bool hasCustomBuildForAllSources = context.ProjectConfigurations.First().CustomBuildForAllSources != null;
            if (hasCustomBuildForAllSources)
            {
                foreach (ProjectFile file in sourceFiles)
                {
                    using (fileGenerator.Declare("file", file.FileNameProjectRelative))
                    using (fileGenerator.Declare("filetype", FileGeneratorUtilities.RemoveLineTag))
                    {
                        fileGenerator.Write(Template.Project.ProjectFilesCustomBuildBegin);

                        foreach (Project.Configuration conf in context.ProjectConfigurations)
                        {
                            if (conf.CustomBuildForAllSources == null)
                                continue;

                            using (fileGenerator.Declare("conf", conf))
                            using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                            using (fileGenerator.Declare("description", conf.CustomBuildForAllSources.Description))
                            using (fileGenerator.Declare("command", conf.CustomBuildForAllSources.CommandLines.JoinStrings(Environment.NewLine, escapeXml: true)))
                            using (fileGenerator.Declare("inputs", FileGeneratorUtilities.RemoveLineTag))
                            using (fileGenerator.Declare("outputs", conf.CustomBuildForAllSources.Outputs.JoinStrings(";")))
                            {
                                fileGenerator.Write(Template.Project.ProjectFilesCustomBuildDescription);
                                fileGenerator.Write(Template.Project.ProjectFilesCustomBuildCommand);
                                fileGenerator.Write(Template.Project.ProjectFilesCustomBuildInputs);
                                fileGenerator.Write(Template.Project.ProjectFilesCustomBuildOutputs);
                            }
                        }

                        fileGenerator.Write(Template.Project.ProjectFilesCustomBuildEnd);
                    }
                }
            }
            else
            {
                // Write source files
                foreach (ProjectFile file in sourceFiles)
                {
                    using (fileGenerator.Declare("file", file))
                    using (fileGenerator.Declare("filetype", FileGeneratorUtilities.RemoveLineTag))
                    {
                        bool isResource = string.Compare(file.FileExtension, ".rc", StringComparison.OrdinalIgnoreCase) == 0;

                        if (isResource)
                            fileGenerator.Write(Template.Project.ProjectFilesResourceBegin);
                        else
                            fileGenerator.Write(Template.Project.ProjectFilesSourceBegin);

                        bool haveFileOptions = false;
                        bool closeFileSource = true;

                        for (int i = 0; i < context.ProjectConfigurations.Count; ++i)
                        {
                            Project.Configuration conf = context.ProjectConfigurations[i];
                            List<ProjectFile> compiledFiles = configurationCompiledFiles[i];

                            bool isPrecompSource = !string.IsNullOrEmpty(conf.PrecompSource) && file.FileName.EndsWith(conf.PrecompSource, StringComparison.OrdinalIgnoreCase);
                            bool isDontUsePrecomp = conf.PrecompSourceExclude.Contains(file.FileName) ||
                                                    conf.PrecompSourceExcludeFolders.Any(folder => file.FileName.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) ||
                                                    conf.PrecompSourceExcludeExtension.Contains(file.FileExtension);

                            bool isExcludeFromBuild = conf.ResolvedSourceFilesBuildExclude.Contains(file.FileName);
                            bool consumeWinRTExtensions = conf.ConsumeWinRTExtensions.Contains(file.FileName) || conf.ResolvedSourceFilesWithCompileAsWinRTOption.Contains(file.FileName);
                            bool excludeWinRTExtensions = conf.ExcludeWinRTExtensions.Contains(file.FileName) || conf.ResolvedSourceFilesWithExcludeAsWinRTOption.Contains(file.FileName);

                            bool isBlobFileDefine = conf.BlobFileDefine != String.Empty && file.FileName.EndsWith(Project.BlobExtension, StringComparison.OrdinalIgnoreCase);
                            bool isResourceFileDefine = conf.ResourceFileDefine != String.Empty && file.FileName.EndsWith(".rc");
                            bool isCompileAsCFile = conf.ResolvedSourceFilesWithCompileAsCOption.Contains(file.FileName);
                            bool isCompileAsCPPFile = conf.ResolvedSourceFilesWithCompileAsCPPOption.Contains(file.FileName);
                            bool isCompileAsCLRFile = conf.ResolvedSourceFilesWithCompileAsCLROption.Contains(file.FileName);
                            bool isCompileAsNonCLRFile = conf.ResolvedSourceFilesWithCompileAsNonCLROption.Contains(file.FileName);
                            bool objsInSubdirectories = conf.ObjectFileName != null && !isResource;

                            if (isPrecompSource && platformVcxproj.ExcludesPrecompiledHeadersFromBuild)
                                isExcludeFromBuild = true;
                            if (!isExcludeFromBuild && !isResource)
                                compiledFiles.Add(file);

                            if (isCompileAsCLRFile || consumeWinRTExtensions || excludeWinRTExtensions)
                                isDontUsePrecomp = true;
                            if (String.Compare(file.FileExtension, ".c", StringComparison.OrdinalIgnoreCase) == 0)
                                isDontUsePrecomp = true;

                            string exceptionSetting = null;
                            switch (conf.GetExceptionSettingForFile(file.FileName))
                            {
                                case Sharpmake.Options.Vc.Compiler.Exceptions.Enable:
                                    exceptionSetting = "Sync";
                                    break;
                                case Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithExternC:
                                    exceptionSetting = "SyncCThrow";
                                    break;
                                case Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithSEH:
                                    exceptionSetting = "Async";
                                    break;
                            }

                            bool hasExceptionSetting = !string.IsNullOrEmpty(exceptionSetting);

                            haveFileOptions = haveFileOptions ||
                                              isExcludeFromBuild ||
                                              isPrecompSource ||
                                              isDontUsePrecomp ||
                                              isBlobFileDefine ||
                                              isResourceFileDefine ||
                                              isCompileAsCFile ||
                                              isCompileAsCPPFile ||
                                              isCompileAsNonCLRFile ||
                                              hasExceptionSetting ||
                                              consumeWinRTExtensions ||
                                              excludeWinRTExtensions ||
                                              objsInSubdirectories;

                            if (haveFileOptions)
                            {
                                using (fileGenerator.Declare("conf", conf))
                                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                                {
                                    if (closeFileSource)
                                    {
                                        fileGenerator.Write(Template.Project.ProjectFilesSourceBeginOptions);
                                        closeFileSource = false;
                                    }

                                    if (isBlobFileDefine)
                                    {
                                        using (fileGenerator.Declare("ProjectFilesSourceDefine", conf.BlobFileDefine))
                                            fileGenerator.Write(Template.Project.ProjectFilesSourceDefine);
                                    }

                                    if (isResourceFileDefine)
                                    {
                                        using (fileGenerator.Declare("ProjectFilesSourceDefine", conf.ResourceFileDefine))
                                            fileGenerator.Write(Template.Project.ProjectFilesSourceDefine);
                                    }

                                    if (isExcludeFromBuild)
                                    {
                                        fileGenerator.Write(Template.Project.ProjectFilesSourceExcludeFromBuild);
                                    }
                                    else
                                    {
                                        if (isCompileAsCFile)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesSourceCompileAsC);
                                        }
                                        else if (isCompileAsCPPFile)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesSourceCompileAsCPP);
                                        }
                                        else if (isCompileAsCLRFile)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesSourceCompileAsCLR);
                                        }
                                        if (isCompileAsNonCLRFile)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesSourceDoNotCompileAsCLR);
                                        }

                                        if (isPrecompSource)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesSourcePrecompCreate);
                                        }
                                        else if (isDontUsePrecomp)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesSourcePrecompNotUsing);
                                        }

                                        if (consumeWinRTExtensions)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesSourceConsumeWinRTExtensions);
                                        }

                                        if (hasExceptionSetting)
                                        {
                                            using (fileGenerator.Declare("exceptionSetting", exceptionSetting))
                                            {
                                                fileGenerator.Write(Template.Project.ProjectFilesSourceEnableExceptions);
                                            }
                                        }

                                        if (excludeWinRTExtensions)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesSourceExcludeWinRTExtensions);
                                        }

                                        if (objsInSubdirectories)
                                        {
                                            using (fileGenerator.Declare("ObjectFileName", conf.ObjectFileName(file.FileNameSourceRelative)))
                                            {
                                                fileGenerator.Write(Template.Project.ProjectFilesSourceObjectFileName);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (haveFileOptions)
                        {
                            if (isResource)
                                fileGenerator.Write(Template.Project.ProjectFilesResourceEnd);
                            else
                                fileGenerator.Write(Template.Project.ProjectFilesSourceEndOptions);
                        }
                        else
                            fileGenerator.Write(Template.Project.ProjectFilesSourceEnd);
                    }
                }
            }
            // Write files built with custom tools
            var typeNames = new List<string>(customSourceFiles.Keys);
            typeNames.Sort();
            foreach (string typeName in typeNames)
            {
                fileGenerator.Write(Template.Project.ProjectFilesEnd);
                fileGenerator.Write(Template.Project.ProjectFilesBegin);
                using (fileGenerator.Declare("type", typeName))
                {
                    List<ProjectFile> files = customSourceFiles[typeName];
                    foreach (var file in files)
                    {
                        using (fileGenerator.Declare("file", file))
                        {
                            fileGenerator.Write(Template.Project.ProjectFilesCustomSourceBegin);

                            bool haveFileOptions = false;
                            for (int i = 0; i < context.ProjectConfigurations.Count; ++i)
                            {
                                Project.Configuration conf = context.ProjectConfigurations[i];
                                using (fileGenerator.Declare("conf", conf))
                                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                                {
                                    List<ProjectFile> compiledFiles = configurationCompiledFiles[i];
                                    bool isExcludeFromBuild = conf.ResolvedSourceFilesBuildExclude.Contains(file.FileName);
                                    if (isExcludeFromBuild)
                                    {
                                        if (!haveFileOptions)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesCustomSourceBeginOptions);
                                            haveFileOptions = true;
                                        }
                                        fileGenerator.Write(Template.Project.ProjectFilesSourceExcludeFromBuild);
                                    }
                                }
                            }
                            if (haveFileOptions)
                                fileGenerator.Write(Template.Project.ProjectFilesCustomSourceEndOptions);
                            else
                                fileGenerator.Write(Template.Project.ProjectFilesCustomSourceEnd);
                        }
                    }
                }
            }

            // Write the "copy dependencies" build step (as a custom build tool on a dummy file, to make sure the copy is always done when needed)
            bool hasDependenciesToCopy = context.ProjectConfigurations.Any(conf => conf.CopyDependenciesBuildStep != null);
            var dependenciesFileGenerator = new FileGenerator(fileGenerator.Resolver); // borrowing resolver
            if (hasDependenciesToCopy)
            {
                fileGenerator.Write(Template.Project.ProjectFilesEnd);
                fileGenerator.Write(Template.Project.ProjectFilesBegin);

                using (fileGenerator.Declare("file", relativeCopyDependenciesFileName))
                using (fileGenerator.Declare("filetype", "Document"))
                {
                    fileGenerator.Write(Template.Project.ProjectFilesCustomBuildBegin);

                    foreach (Project.Configuration conf in context.ProjectConfigurations)
                    {
                        Project.Configuration.FileCustomBuild copyDependencies = conf.CopyDependenciesBuildStep;

                        if (copyDependencies == null)
                            continue;

                        using (fileGenerator.Declare("conf", conf))
                        using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                        using (fileGenerator.Declare("description", copyDependencies.Description))
                        using (fileGenerator.Declare("command", copyDependencies.CommandLines.JoinStrings(Environment.NewLine, escapeXml: true)))
                        using (fileGenerator.Declare("inputs", copyDependencies.Inputs.JoinStrings(";")))
                        using (fileGenerator.Declare("outputs", copyDependencies.Outputs.JoinStrings(";")))
                        using (fileGenerator.Declare("linkobjects", copyDependencies.LinkObjects))
                        {
                            fileGenerator.Write(Template.Project.ProjectFilesCustomBuildDescription);
                            fileGenerator.Write(Template.Project.ProjectFilesCustomBuildCommand);
                            fileGenerator.Write(Template.Project.ProjectFilesCustomBuildInputs);
                            fileGenerator.Write(Template.Project.ProjectFilesCustomBuildOutputs);
                            fileGenerator.Write(Template.Project.ProjectFilesCustomBuildLinkObject);

                            // Also write the dependencies in the generated "runtimedependencies" file, for convenience
                            dependenciesFileGenerator.Write(string.Format("{0}[conf.Name]|[platformName]{0}", Environment.NewLine));
                            dependenciesFileGenerator.Write(string.Format("  {0}" + Environment.NewLine, copyDependencies.Inputs.JoinStrings(Environment.NewLine + "  ")));
                        }
                    }
                    fileGenerator.Write(Template.Project.ProjectFilesCustomBuildEnd);
                }
            }

            // Validation
            for (int i = 0; i < context.ProjectConfigurations.Count; ++i)
            {
                Project.Configuration conf = context.ProjectConfigurations[i];
                List<ProjectFile> compiledFiles = configurationCompiledFiles[i];

                compiledFiles.Sort((ProjectFile l, ProjectFile r) => { return String.Compare(l.FileNameWithoutExtension, r.FileNameWithoutExtension, StringComparison.OrdinalIgnoreCase); });

                for (int j = 0; j < compiledFiles.Count - 1; ++j)
                {
                    ProjectFile l = compiledFiles[j];
                    ProjectFile r = compiledFiles[j + 1];

                    if (String.Compare(l.FileNameWithoutExtension, r.FileNameSourceRelative, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string plausibleCause = "";

                        string message =
                            string.Format(
                                "error: {0} project configuration contains 2 files with the same file name '{1}', project compilation will fail due to same obj names"
                                + Environment.NewLine + "{2}" + Environment.NewLine + "{3}.{4}",
                                conf, l.FileNameWithoutExtension, l.FileNameProjectRelative, r.FileNameProjectRelative, plausibleCause);
                        throw new Error(message);
                    }
                }
            }

            // done!
            fileGenerator.Write(Template.Project.ProjectFilesEnd);

            // for the configuration that are fastbuild but external and requires to add the bff files
            bool lookIfHasAnyFastBuild = false;
            if (context.ProjectConfigurations.First().IsMainProject) // main project might mix fastbuild and non-fastbuild
                lookIfHasAnyFastBuild = context.ProjectConfigurations.Any(x => x.IsFastBuild);
            GenerateBffFilesSection(context, fileGenerator, generatedFiles, skipFiles, lookIfHasAnyFastBuild);

            var allFileLists = new List<Tuple<string, List<ProjectFile>>>();
            allFileLists.Add(new Tuple<string, List<ProjectFile>>(hasCustomBuildForAllSources ? "CustomBuild" : "ClCompile", sourceFiles));
            allFileLists.Add(new Tuple<string, List<ProjectFile>>("PRIResource", XResourcesReswFiles));
            allFileLists.Add(new Tuple<string, List<ProjectFile>>("Image", XResourcesImgFiles));
            allFileLists.Add(new Tuple<string, List<ProjectFile>>(hasCustomBuildForAllIncludes ? "CustomBuild" : "ClInclude", includeFiles));
            if (NatvisFiles.Count > 0)
                allFileLists.Add(new Tuple<string, List<ProjectFile>>("Natvis", NatvisFiles));
            if (PRIFiles.Count > 0)
                allFileLists.Add(new Tuple<string, List<ProjectFile>>("PRIResource", PRIFiles));
            foreach (var entry in customSourceFiles)
            {
                allFileLists.Add(new Tuple<string, List<ProjectFile>>(entry.Key, entry.Value));
            }

            bool skipFilterGeneration = context.ProjectConfigurations.Any(conf => conf.SkipFilterGeneration);
            if (!skipFilterGeneration || !File.Exists(filtersFileName))
            {
                using (fileGenerator.Declare("project", context.Project))
                    GenerateFiltersFile(context, filtersFileName, allFileLists, hasDependenciesToCopy ? relativeCopyDependenciesFileName : string.Empty, fileGenerator.Resolver, generatedFiles, skipFiles);
            }

            if (hasDependenciesToCopy)
            {
                FileInfo copyDependenciesFileInfo = new FileInfo(copyDependenciesFileName);

                if (context.Builder.Context.WriteGeneratedFile(context.Project.GetType(), copyDependenciesFileInfo, dependenciesFileGenerator.ToMemoryStream()))
                    generatedFiles.Add(copyDependenciesFileInfo.FullName);
                else
                    skipFiles.Add(copyDependenciesFileInfo.FullName);
            }
        }

        public class ProjectFile
        {
            public string FileName;
            public string FileNameSourceRelative;
            public string FileNameProjectRelative;
            public string FileNameWithoutExtension;
            public string FileExtension;
            public string FilterPath;

            public ProjectFile(IGenerationContext context, string fileName)
            {
                FileName = Project.GetCapitalizedFile(fileName);
                if (FileName == null)
                    FileName = fileName;

                FileNameProjectRelative = Util.PathGetRelative(context.ProjectDirectoryCapitalized, FileName, true);
                FileNameSourceRelative = Util.PathGetRelative(context.ProjectSourceCapitalized, FileName, true);

                FileExtension = Path.GetExtension(FileName);
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);

                int lastPathSeparator = FileNameSourceRelative.LastIndexOf(Util.WindowsSeparator);
                string dirSourceRelative = lastPathSeparator == -1 ? "" : FileNameSourceRelative.Substring(0, lastPathSeparator);

                string customFilterPath;
                if (context.Project.CustomFilterMapping.TryGetValue(dirSourceRelative, out customFilterPath) ||
                    context.Project.ResolveFilterPath(dirSourceRelative, out customFilterPath))
                {
                    FilterPath = customFilterPath;
                }
                else
                {
                    FilterPath = dirSourceRelative;
                }

                FilterPath = FilterPath.Trim('.', Util.WindowsSeparator);
            }

            public override string ToString()
            {
                return FileName;
            }
        }

        private class UserFile : UserFileBase
        {
            public UserFile(string projectFilePath) : base(projectFilePath) { }

            protected override void GenerateConfigurationContent(IFileGenerator fileGenerator, Project.Configuration conf)
            {
                PlatformRegistry.Get<IPlatformVcxproj>(conf.Platform).GenerateUserConfigurationFile(conf, fileGenerator);
            }

            protected override bool HasContentForConfiguration(Project.Configuration conf, out bool overwriteFile)
            {
                overwriteFile = conf.CsprojUserFile?.OverwriteExistingFile ?? true;
                return conf.VcxprojUserFile != null;
            }
        }
    }
}

#pragma warning restore 0219
#pragma warning restore 0168
#pragma warning restore 0162
