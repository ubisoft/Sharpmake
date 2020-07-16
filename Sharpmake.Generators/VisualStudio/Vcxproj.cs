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

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Vcxproj : IProjectGenerator
    {
        // sharpmake dev options for now, use with care!
        // this will disable visual studio registry lookups
        private const bool _enableRegistryUse = true;
        private const bool _enableInstalledVCTargetsUse = false;

        public enum BuildStep
        {
            PreBuild = 0x01,
            PreBuildCustomAction = 0x02,
            PostBuild = 0x03,
            PostBuildCustomAction = 0x04,
        }

        private class GenerationContext : IVcxprojGenerationContext
        {
            private Dictionary<Project.Configuration, Options.ExplicitOptions> _projectConfigurationOptions;
            private IDictionary<string, string> _cmdLineOptions;
            private Project.Configuration _configuration;
            private Resolver _envVarResolver;

            public Builder Builder { get; }
            public string ProjectPath { get; }
            public string ProjectDirectory { get; }
            public string ProjectFileName { get; }
            public string ProjectDirectoryCapitalized { get; }
            public string ProjectSourceCapitalized { get; }
            public bool PlainOutput { get { return true; } }
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

            public IReadOnlyDictionary<Project.Configuration, Options.ExplicitOptions> ProjectConfigurationOptions => _projectConfigurationOptions;

            public void SetProjectConfigurationOptions(Dictionary<Project.Configuration, Options.ExplicitOptions> projectConfigurationOptions)
            {
                _projectConfigurationOptions = projectConfigurationOptions;
            }

            public DevEnv DevelopmentEnvironment => Configuration.Target.GetFragment<DevEnv>();
            public DevEnvRange DevelopmentEnvironmentsRange { get; }
            public Options.ExplicitOptions Options
            {
                get
                {
                    Debug.Assert(_projectConfigurationOptions.ContainsKey(Configuration));
                    return _projectConfigurationOptions[Configuration];
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

            public FastBuildMakeCommandGenerator FastBuildMakeCommandGenerator { get; }

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

                ProjectConfigurations = VsUtil.SortConfigurations(projectConfigurations, Path.Combine(ProjectDirectoryCapitalized, ProjectFileName + ProjectExtension)).ToArray();
                DevelopmentEnvironmentsRange = new DevEnvRange(ProjectConfigurations);

                PresentPlatforms = ProjectConfigurations.Select(conf => conf.Platform).Distinct().ToDictionary(p => p, p => PlatformRegistry.Get<IPlatformVcxproj>(p));

                FastBuildMakeCommandGenerator = FastBuildSettings.MakeCommandGenerator ?? new Bff.FastBuildDefaultNMakeCommandGenerator();
            }

            public void Reset()
            {
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
        }

        public void Generate(Builder builder, Project project, List<Project.Configuration> configurations, string projectFile, List<string> generatedFiles, List<string> skipFiles)
        {
            var context = new GenerationContext(builder, projectFile, project, configurations);
            GenerateImpl(context, generatedFiles, skipFiles);
        }

        public static string FastBuildCustomArguments = "";
        public const string ProjectExtension = ".vcxproj";
        private const string ProjectFilterExtension = ".filters";
        private const string CopyDependenciesExtension = "_runtimedependencies.txt";
        public const string EventSeparator = "&#x0D;&#x0A;";

        // Vcxproj only allows one file command per input file, so we collapse
        // the commands into a single command per file.
        public class CombinedCustomFileBuildStep
        {
            public string Commands = "";
            public string Description = "";
            public string Outputs = "";
            public string AdditionalInputs = "";
        };

        public static Dictionary<string, CombinedCustomFileBuildStep> CombineCustomFileBuildSteps(string referencePath, Resolver resolver, IEnumerable<Project.Configuration.CustomFileBuildStep> buildSteps)
        {
            // Map from relative input file to command to run on that file, for this configuration.
            var steps = new Dictionary<string, CombinedCustomFileBuildStep>(StringComparer.OrdinalIgnoreCase);

            foreach (var customBuildStep in buildSteps)
            {
                var relativeBuildStep = customBuildStep.MakePathRelative(resolver, (path, commandRelative) => Util.SimplifyPath(Util.PathGetRelative(referencePath, path)));
                relativeBuildStep.AdditionalInputs.Add(relativeBuildStep.Executable);
                // Build the command.
                string command = string.Format(
                    "{0} {1}",
                    relativeBuildStep.Executable,
                    relativeBuildStep.ExecutableArguments
                );

                command = Util.EscapeXml(command) + EventSeparator;
                CombinedCustomFileBuildStep combinedCustomBuildStep;
                // This needs to be project relative to work.
                string FileKey = Util.SimplifyPath(Util.PathGetRelative(referencePath, customBuildStep.KeyInput));
                if (!steps.TryGetValue(FileKey, out combinedCustomBuildStep))
                {
                    combinedCustomBuildStep = new CombinedCustomFileBuildStep();
                    steps.Add(FileKey, combinedCustomBuildStep);
                }
                else
                {
                    // Add separators.
                    combinedCustomBuildStep.Description += ";";
                    combinedCustomBuildStep.Outputs += ";";
                    combinedCustomBuildStep.AdditionalInputs += ";";
                }
                combinedCustomBuildStep.Commands += command;
                combinedCustomBuildStep.Description += relativeBuildStep.Description;
                combinedCustomBuildStep.Outputs = Util.EscapeXml(relativeBuildStep.Output);
                combinedCustomBuildStep.AdditionalInputs = Util.EscapeXml(relativeBuildStep.AdditionalInputs.JoinStrings(";"));
            }

            return steps;
        }

        private static string GetVCTargetsPathOverride(DevEnv devEnv)
        {
            switch (devEnv)
            {
                case DevEnv.vs2017:
                    return Path.Combine(devEnv.GetVisualStudioDir(), @"Common7\IDE\VC\VCTargets\");
                case DevEnv.vs2019:
                    return Path.Combine(devEnv.GetVisualStudioDir(), @"MSBuild\Microsoft\VC\v160\");
                default:
                    throw new NotImplementedException("VCTargetsPath redirection for " + devEnv);
            }
        }

        private static string GetMSBuildExtensionsPathOverride(DevEnv devEnv)
        {
            switch (devEnv)
            {
                case DevEnv.vs2017:
                case DevEnv.vs2019:
                    return Path.Combine(devEnv.GetVisualStudioDir(), @"MSBuild\");
                default:
                    throw new NotImplementedException("MSBuildExtensionsPath redirection for " + devEnv);
            }
        }

        private bool WriteVcOverrides(GenerationContext context, FileGenerator fileGenerator)
        {
            bool overridesActive = false;

            bool registrySettingWritten = false;
            bool disableInstalledVcTargetsUseWritten = false;
            bool msBuildExtensionsPathWritten = false;

            var platformToolsets = context.ProjectConfigurations.Select(Options.GetObject<Options.Vc.General.PlatformToolset>);
            var devEnvForToolsets = platformToolsets.Select(pts => pts.GetDefaultDevEnvForToolset()).Where(pts => pts != null).Select(d => d.Value);
            var regularRange = EnumUtils.EnumerateValues<DevEnv>().Where(d => d >= context.DevelopmentEnvironmentsRange.MinDevEnv && d <= context.DevelopmentEnvironmentsRange.MaxDevEnv);

            bool? overrideCheck = null;
            var allDevEnvs = devEnvForToolsets.Union(regularRange).Distinct().OrderBy(d => d).ToArray();
            foreach (DevEnv devEnv in allDevEnvs)
            {
                bool vsDirOverriden = devEnv.OverridenVisualStudioDir();
                if (overrideCheck.HasValue)
                {
                    if (vsDirOverriden != overrideCheck)
                        throw new Error($"Some DevEnv are overridden and some are not in the vcxproj '{context.ProjectFileName}'. Please override all or none.");
                }
                else
                {
                    overrideCheck = vsDirOverriden;
                }

                if (!vsDirOverriden)
                    continue;

                if (!devEnv.IsVisualStudio())
                    throw new Error(devEnv + " is not recognized as being visual studio");

                overridesActive = true;

                if (!_enableRegistryUse && !registrySettingWritten)
                {
                    fileGenerator.Write(Template.Project.DisableRegistryUse);
                    registrySettingWritten = true;
                }

                if (!_enableInstalledVCTargetsUse && !disableInstalledVcTargetsUseWritten)
                {
                    fileGenerator.Write(Template.Project.DisableInstalledVcTargetsUse);
                    disableInstalledVcTargetsUseWritten = true;
                }

                string vcRootPathKey;
                string vcTargetsPathKey;
                devEnv.GetVcPathKeysFromDevEnv(out vcTargetsPathKey, out vcRootPathKey);

                using (fileGenerator.Declare("vcInstallDirKey", vcRootPathKey))
                using (fileGenerator.Declare("vcInstallDirValue", Util.EnsureTrailingSeparator(Path.Combine(devEnv.GetVisualStudioDir(), @"VC\"))))
                using (fileGenerator.Declare("msBuildExtensionsPath", msBuildExtensionsPathWritten ? FileGeneratorUtilities.RemoveLineTag : Util.EnsureTrailingSeparator(GetMSBuildExtensionsPathOverride(devEnv))))
                using (fileGenerator.Declare("vsVersion", devEnv.GetVisualProjectToolsVersionString()))
                using (fileGenerator.Declare("vcTargetsPathKey", vcTargetsPathKey))
                using (fileGenerator.Declare("vcTargetsPath", Util.EnsureTrailingSeparator(GetVCTargetsPathOverride(devEnv))))
                {
                    msBuildExtensionsPathWritten = true;

                    fileGenerator.Write(Template.Project.VCOverridesProperties);
                    fileGenerator.Write(Template.Project.VCTargetsPathOverride);
                }
            }

            return overridesActive;
        }

        private void GenerateConfOptions(GenerationContext context)
        {
            // generate all configuration options once...
            var projectOptionsGen = new ProjectOptionsGenerator();
            var projectConfigurationOptions = new Dictionary<Project.Configuration, Options.ExplicitOptions>();
            context.SetProjectConfigurationOptions(projectConfigurationOptions);
            foreach (Project.Configuration conf in context.ProjectConfigurations)
            {
                context.Configuration = conf;

                // set generator information
                var platformVcxproj = context.PresentPlatforms[conf.Platform];
                var configurationTasks = PlatformRegistry.Get<Project.Configuration.IConfigurationTasks>(conf.Platform);
                conf.GeneratorSetGeneratedInformation(
                    platformVcxproj.ExecutableFileExtension,
                    platformVcxproj.PackageFileExtension,
                    configurationTasks.GetDefaultOutputExtension(Project.Configuration.OutputType.Dll),
                    platformVcxproj.ProgramDatabaseFileExtension);

                projectConfigurationOptions.Add(conf, new Options.ExplicitOptions());
                context.CommandLineOptions = new ProjectOptionsGenerator.VcxprojCmdLineOptions();

                projectOptionsGen.GenerateOptions(context);
                platformVcxproj.SelectPreprocessorDefinitionsVcxproj(context);
                FillIncludeDirectoriesOptions(context);
                FillLibrariesOptions(context);

                context.Reset(); // just a safety, not necessary to clean up
            }
        }

        private void GenerateImpl(GenerationContext context, IList<string> generatedFiles, IList<string> skipFiles)
        {
            FileName = context.ProjectPath;

            GenerateConfOptions(context);

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

            var fileGenerator = new XmlFileGenerator();

            // xml begin header
            using (fileGenerator.Declare("toolsVersion", context.DevelopmentEnvironmentsRange.MinDevEnv.GetVisualProjectToolsVersionString()))
            {
                fileGenerator.Write(Template.Project.ProjectBegin);
            }

            VsProjCommon.WriteCustomProperties(context.Project.CustomProperties, fileGenerator);

            foreach (var platformVcxproj in context.PresentPlatforms.Values)
                platformVcxproj.GenerateSdkVcxproj(context, fileGenerator);

            VsProjCommon.WriteProjectConfigurationsDescription(context.ProjectConfigurations, fileGenerator);

            bool hasFastBuildConfig = false;
            bool hasNonFastBuildConfig = false;
            foreach (var conf in context.ProjectConfigurations)
            {
                if (conf.IsFastBuild)
                    hasFastBuildConfig = true;
                else
                    hasNonFastBuildConfig = true;
            }

            var firstConf = context.ProjectConfigurations.First();

            //checking only the first one, having one with CLR support and others without would be an error
            bool clrSupport = Util.IsDotNet(firstConf);

            string projectKeyword = FileGeneratorUtilities.RemoveLineTag;
            string targetFrameworkString = FileGeneratorUtilities.RemoveLineTag;

            if (clrSupport)
            {
                projectKeyword = "ManagedCProj";
                targetFrameworkString = Util.GetDotNetTargetString(firstConf.Target.GetFragment<DotNetFramework>());
            }

            using (fileGenerator.Declare("projectName", firstConf.ProjectName))
            using (fileGenerator.Declare("guid", firstConf.ProjectGuid))
            using (fileGenerator.Declare("sccProjectName", sccProjectName))
            using (fileGenerator.Declare("sccLocalPath", sccLocalPath))
            using (fileGenerator.Declare("sccProvider", sccProvider))
            using (fileGenerator.Declare("targetFramework", targetFrameworkString))
            using (fileGenerator.Declare("projectKeyword", projectKeyword))
            {
                fileGenerator.Write(Template.Project.ProjectDescription);
            }

            string vcTargetsPath = "$(VCTargetsPath)";
            if (WriteVcOverrides(context, fileGenerator))
            {
                // Disabling this, since it prevents opening old projects in recent visual studio versions
                // TODO: find a way to make it work
                //string vcRootPathKey;
                //string vcTargetsPathKey;
                //// we use the targets path from the most recent devenv supported in this vcxproj,
                //// since it will know how to redirect to older toolsets
                //context.DevelopmentEnvironmentsRange.MaxDevEnv.GetVcPathKeysFromDevEnv(out vcTargetsPathKey, out vcRootPathKey);
                //vcTargetsPath = $"$({vcTargetsPathKey})";
            }

            var vcTargetsPathScopeVar = fileGenerator.Declare("vcTargetsPath", vcTargetsPath);

            fileGenerator.Write(Template.Project.PropertyGroupEnd);
            // xml end header

            foreach (var platform in context.PresentPlatforms.Values)
                platform.GeneratePlatformSpecificProjectDescription(context, fileGenerator);

            foreach (var platform in context.PresentPlatforms.Values)
                platform.GenerateProjectPlatformSdkDirectoryDescription(context, fileGenerator);

            fileGenerator.Write(Template.Project.ImportCppDefaultProps);

            foreach (var platform in context.PresentPlatforms.Values)
                platform.GeneratePostDefaultPropsImport(context, fileGenerator);

            // user file
            string projectFilePath = FileName + ProjectExtension;
            UserFile uf = new UserFile(projectFilePath);
            uf.GenerateUserFile(context.Builder, context.Project, context.ProjectConfigurations, generatedFiles, skipFiles);

            // configuration general
            foreach (Project.Configuration conf in context.ProjectConfigurations)
            {
                context.Configuration = conf;

                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
                using (fileGenerator.Declare("conf", conf))
                using (fileGenerator.Declare("options", context.ProjectConfigurationOptions[conf]))
                using (fileGenerator.Declare("clrSupport", (conf.IsFastBuild || !clrSupport) ? FileGeneratorUtilities.RemoveLineTag : clrSupport.ToString().ToLower()))
                {
                    var platformVcxproj = context.PresentPlatforms[conf.Platform];
                    platformVcxproj.GenerateProjectConfigurationGeneral(context, fileGenerator);
                }
            }

            // .props files
            fileGenerator.Write(Template.Project.ProjectAfterConfigurationsGeneral);
            if (context.Project.ContainsASM)
                fileGenerator.Write(Template.Project.ProjectImportedMasmProps);

            VsProjCommon.WriteProjectCustomPropsFiles(context.Project.CustomPropsFiles, context.ProjectDirectoryCapitalized, fileGenerator);
            VsProjCommon.WriteConfigurationsCustomPropsFiles(context.ProjectConfigurations, context.ProjectDirectoryCapitalized, fileGenerator);

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
                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
                using (fileGenerator.Declare("conf", conf))
                using (fileGenerator.Declare("options", context.ProjectConfigurationOptions[conf]))
                using (fileGenerator.Declare("target", conf.Target))
                {
                    var platformVcxproj = context.PresentPlatforms[conf.Platform];

                    if (conf.IsFastBuild)
                    {
                        var fastBuildCommandLineOptions = new List<string>();

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

                        // Configuring cache mode if that configuration is allowed to use caching
                        if (conf.FastBuildCacheAllowed)
                        {
                            // Setting the appropriate cache type commandline for that target.
                            switch (FastBuildSettings.CacheType)
                            {
                                case FastBuildSettings.CacheTypes.CacheRead:
                                    fastBuildCommandLineOptions.Add("-cacheread");
                                    break;
                                case FastBuildSettings.CacheTypes.CacheWrite:
                                    fastBuildCommandLineOptions.Add("-cachewrite");
                                    break;
                                case FastBuildSettings.CacheTypes.CacheReadWrite:
                                    fastBuildCommandLineOptions.Add("-cache");
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (FastBuildSettings.FastBuildDistribution && conf.FastBuildDistribution)
                            fastBuildCommandLineOptions.Add("-dist");

                        if (FastBuildSettings.FastBuildWait)
                            fastBuildCommandLineOptions.Add("-wait");

                        if (FastBuildSettings.FastBuildNoStopOnError)
                            fastBuildCommandLineOptions.Add("-nostoponerror");

                        if (FastBuildSettings.FastBuildFastCancel)
                            fastBuildCommandLineOptions.Add("-fastcancel");

                        if (FastBuildSettings.FastBuildNoUnity)
                            fastBuildCommandLineOptions.Add("-nounity");

                        if (!string.IsNullOrEmpty(conf.FastBuildCustomArgs))
                            fastBuildCommandLineOptions.Add(conf.FastBuildCustomArgs);

                        if (!string.IsNullOrEmpty(FastBuildCustomArguments))
                            fastBuildCommandLineOptions.Add(FastBuildCustomArguments);

                        string commandLine = string.Join(" ", fastBuildCommandLineOptions);

                        // Make the commandline written in the bff available, except the master bff -config
                        Bff.SetCommandLineArguments(conf, commandLine);

                        commandLine += " -config $(SolutionName)" + FastBuildSettings.FastBuildConfigFileExtension;

                        using (fileGenerator.Declare("relativeMasterBffPath", "$(SolutionDir)"))
                        using (fileGenerator.Declare("fastBuildMakeCommandBuild", context.FastBuildMakeCommandGenerator.GetCommand(FastBuildMakeCommandGenerator.BuildType.Build, conf, commandLine)))
                        using (fileGenerator.Declare("fastBuildMakeCommandRebuild", context.FastBuildMakeCommandGenerator.GetCommand(FastBuildMakeCommandGenerator.BuildType.Rebuild, conf, commandLine)))
                        {
                            platformVcxproj.GenerateProjectConfigurationFastBuildMakeFile(context, fileGenerator);
                        }
                    }
                    else if (conf.CustomBuildSettings != null)
                    {
                        platformVcxproj.GenerateProjectConfigurationCustomMakeFile(context, fileGenerator);
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
                    using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
                    using (fileGenerator.Declare("conf", conf))
                    using (fileGenerator.Declare("project", conf.Project))
                    using (fileGenerator.Declare("target", conf.Target))
                    using (fileGenerator.Declare("options", context.ProjectConfigurationOptions[conf]))
                    using (fileGenerator.Declare("clrSupport", !clrSupport ? FileGeneratorUtilities.RemoveLineTag : clrSupport.ToString().ToLower()))
                    {
                        fileGenerator.Write(Template.Project.ProjectConfigurationBeginItemDefinition);

                        var platformVcxproj = context.PresentPlatforms[conf.Platform];
                        platformVcxproj.GenerateProjectCompileVcxproj(context, fileGenerator);
                        platformVcxproj.GenerateProjectLinkVcxproj(context, fileGenerator);

                        if (conf.Project.ContainsASM)
                            platformVcxproj.GenerateProjectMasmVcxproj(context, fileGenerator);

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

                        if (conf.AdditionalManifestFiles.Count != 0 || (Options.GetObjects<Options.Vc.ManifestTool.EnableDpiAwareness>(conf).Any()) && (conf.Platform.IsPC() && conf.Platform.IsMicrosoft()))
                            fileGenerator.Write(Template.Project.ProjectConfigurationsManifestTool);

                        fileGenerator.Write(Template.Project.ProjectConfigurationEndItemDefinition);
                    }
                }
            }

            // For all projects configurations that are fastbuild only, do not add the cpp
            // source file requires to be remove from the projects, so that not 2 same cpp file be in 2 different project.
            // TODO: make a better check
            if (hasNonFastBuildConfig || !context.Project.StripFastBuildSourceFiles || context.ProjectConfigurations.Any(conf => !conf.StripFastBuildSourceFiles))
                GenerateFilesSection(context, fileGenerator, generatedFiles, skipFiles);
            else if (hasFastBuildConfig)
                GenerateBffFilesSection(context, fileGenerator);

            // Import platform makefiles.
            foreach (var platform in context.PresentPlatforms.Values)
                platform.GenerateMakefileConfigurationVcxproj(context, fileGenerator);

            // .targets files
            fileGenerator.Write(Template.Project.ProjectTargetsBegin);
            if (context.Project.ContainsASM)
            {
                fileGenerator.Write(Template.Project.ProjectMasmTargetsItem);
            }
            foreach (string targetsFiles in context.Project.CustomTargetsFiles)
            {
                string capitalizedFile = Project.GetCapitalizedFile(targetsFiles) ?? targetsFiles;

                string relativeFile = Util.PathGetRelative(context.ProjectDirectoryCapitalized, capitalizedFile);
                using (fileGenerator.Declare("importedTargetsFile", relativeFile))
                {
                    fileGenerator.Write(Template.Project.ProjectTargetsItem);
                }
            }

            // configuration .targets files
            foreach (Project.Configuration conf in context.ProjectConfigurations)
            {
                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
                using (fileGenerator.Declare("conf", conf))
                {
                    foreach (string targetsFile in conf.CustomTargetsFiles)
                    {
                        string capitalizedFile = Project.GetCapitalizedFile(targetsFile) ?? targetsFile;

                        string relativeFile = Util.PathGetRelative(context.ProjectDirectoryCapitalized, capitalizedFile);
                        using (fileGenerator.Declare("importedTargetsFile", relativeFile))
                        {
                            fileGenerator.Write(Template.Project.ProjectConfigurationImportedTargets);
                        }
                    }
                }
            }
            fileGenerator.Write(Template.Project.ProjectTargetsEnd);

            // in case we are using fast build we do not want to write most dependencies
            // in the vcxproj because they are handled internally in the bff.
            // Nevertheless, non-fastbuild dependencies (such as C# projects) must be written.
            GenerateProjectReferences(context, fileGenerator, hasFastBuildConfig);

            // Environment variables
            var environmentVariables = context.ProjectConfigurations.Select(conf => conf.Platform).Distinct().SelectMany(platform => context.PresentPlatforms[platform].GetEnvironmentVariables(context));
            VsProjCommon.WriteEnvironmentVariables(environmentVariables, fileGenerator);

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

            vcTargetsPathScopeVar.Dispose();

            FileInfo projectFileInfo = new FileInfo(context.ProjectPath + ProjectExtension);
            if (context.Builder.Context.WriteGeneratedFile(context.Project.GetType(), projectFileInfo, cleanMemoryStream))
                generatedFiles.Add(projectFileInfo.FullName);
            else
                skipFiles.Add(projectFileInfo.FullName);
        }

        private static void FillIncludeDirectoriesOptions(GenerationContext context)
        {
            IPlatformVcxproj platformVcxproj = context.PresentPlatforms[context.Configuration.Platform];

            // Fill include dirs
            var includePaths = platformVcxproj.GetIncludePaths(context);
            context.Options["AdditionalIncludeDirectories"] = includePaths.Any() ? Util.PathGetRelative(context.ProjectDirectory, includePaths).JoinStrings(";") : FileGeneratorUtilities.RemoveLineTag;

            // Fill resource include dirs
            var resourceIncludePaths = platformVcxproj.GetResourceIncludePaths(context);
            context.Options["AdditionalResourceIncludeDirectories"] = resourceIncludePaths.Any() ? Util.PathGetRelative(context.ProjectDirectory, resourceIncludePaths).JoinStrings(";") : FileGeneratorUtilities.RemoveLineTag;

            // Fill using dirs
            Strings additionalUsingDirectories = Options.GetStrings<Options.Vc.Compiler.AdditionalUsingDirectories>(context.Configuration);
            additionalUsingDirectories.AddRange(context.Configuration.AdditionalUsingDirectories);

            if (additionalUsingDirectories.Count > 0)
            {
                string additionalUsing = string.Join(";", additionalUsingDirectories.Select(s => Util.PathGetRelative(context.ProjectDirectory, s)));
                if (context.Options["AdditionalUsingDirectories"] != FileGeneratorUtilities.RemoveLineTag)
                    additionalUsing = additionalUsing + ";" + context.Options["AdditionalUsingDirectories"];
                context.Options["AdditionalUsingDirectories"] = additionalUsing;
            }
        }

        private static void FillLibrariesOptions(GenerationContext context)
        {
            IPlatformVcxproj platformVcxproj = context.PresentPlatforms[context.Configuration.Platform];

            Strings ignoreSpecificLibraryNames = Options.GetStrings<Options.Vc.Linker.IgnoreSpecificLibraryNames>(context.Configuration);
            ignoreSpecificLibraryNames.ToLower();
            ignoreSpecificLibraryNames.InsertSuffix("." + platformVcxproj.StaticLibraryFileExtension, true);

            context.Options["AdditionalDependencies"] = FileGeneratorUtilities.RemoveLineTag;
            context.Options["AdditionalLibraryDirectories"] = FileGeneratorUtilities.RemoveLineTag;

            if (!(context.Configuration.Output == Project.Configuration.OutputType.None || context.Configuration.Output == Project.Configuration.OutputType.Lib && !context.Configuration.ExportAdditionalLibrariesEvenForStaticLib))
            {
                //AdditionalLibraryDirectories
                //                                            AdditionalLibraryDirectories="dir1;dir2"    /LIBPATH:"dir1" /LIBPATH:"dir2"
                SelectAdditionalLibraryDirectoriesOption(context);

                //AdditionalDependencies
                //                                            AdditionalDependencies="lib1;lib2"      "lib1;lib2" 
                SelectAdditionalDependenciesOption(context, ignoreSpecificLibraryNames);
            }

            ////IgnoreSpecificLibraryNames
            ////                                            IgnoreDefaultLibraryNames=[lib]         /NODEFAULTLIB:[lib]
            context.Options["IgnoreDefaultLibraryNames"] = ignoreSpecificLibraryNames.JoinStrings(";");
        }

        private static void SelectAdditionalLibraryDirectoriesOption(GenerationContext context)
        {
            IPlatformVcxproj platformVcxproj = context.PresentPlatforms[context.Configuration.Platform];

            var libDirs = new OrderableStrings(context.Configuration.LibraryPaths);
            libDirs.AddRange(context.Configuration.DependenciesOtherLibraryPaths);
            libDirs.AddRange(context.Configuration.DependenciesBuiltTargetsLibraryPaths);
            libDirs.AddRange(platformVcxproj.GetLibraryPaths(context));

            if (libDirs.Any())
            {
                libDirs.Sort();

                var relativeAdditionalLibraryDirectories = Util.PathGetRelative(context.ProjectDirectory, libDirs);
                context.Options["AdditionalLibraryDirectories"] = string.Join(";", relativeAdditionalLibraryDirectories);
            }
            else
            {
                context.Options["AdditionalLibraryDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            }
        }

        private static void SelectAdditionalDependenciesOption(
            GenerationContext context,
            Strings ignoreSpecificLibraryNames
        )
        {
            IPlatformVcxproj platformVcxproj = context.PresentPlatforms[context.Configuration.Platform];

            var otherLibraryFiles = new OrderableStrings(context.Configuration.LibraryFiles);
            otherLibraryFiles.AddRange(context.Configuration.DependenciesOtherLibraryFiles);
            otherLibraryFiles.AddRange(platformVcxproj.GetLibraryFiles(context));
            otherLibraryFiles.Sort();

            // put the built library files before any other
            var libraryFiles = new OrderableStrings(context.Configuration.DependenciesBuiltTargetsLibraryFiles);
            libraryFiles.Sort();
            libraryFiles.AddRange(otherLibraryFiles);

            // convert all root paths to be relative to the project folder
            for (int i = 0; i < libraryFiles.Count; ++i)
            {
                string libraryFile = libraryFiles[i];
                if (Path.IsPathRooted(libraryFile))
                    libraryFiles[i] = Util.GetConvertedRelativePath(context.ProjectDirectory, libraryFile, context.ProjectDirectory, true, context.Project.RootPath);
            }

            string libPrefix = platformVcxproj.GetOutputFileNamePrefix(context, Project.Configuration.OutputType.Lib);

            var additionalDependencies = new Strings();
            foreach (string libraryFile in libraryFiles)
            {
                // We've got two kinds of way of listing a library:
                // - With a filename without extension we must add the potential prefix and potential extension.
                //      Ex:  On clang we add -l (supposedly because the exact file is named lib<library>.a)
                // - With a filename with a static or shared lib extension (eg. .a/.lib/.so), we shouldn't touch it as it's already set by the script.
                string decoratedName = libraryFile;
                string extension = Path.GetExtension(libraryFile).ToLower();
                if (extension.StartsWith(".", StringComparison.Ordinal))
                    extension = extension.Substring(1);

                if (extension != platformVcxproj.StaticLibraryFileExtension && extension != platformVcxproj.SharedLibraryFileExtension)
                {
                    decoratedName = libPrefix + libraryFile;
                    if (!string.IsNullOrEmpty(platformVcxproj.StaticLibraryFileExtension))
                        decoratedName += "." + platformVcxproj.StaticLibraryFileExtension;
                }

                if (!ignoreSpecificLibraryNames.Contains(decoratedName))
                    additionalDependencies.Add(decoratedName);
                else
                    ignoreSpecificLibraryNames.Remove(decoratedName);
            }

            context.Options["AdditionalDependencies"] = string.Join(";", additionalDependencies);

            platformVcxproj.SelectPlatformAdditionalDependenciesOptions(context);
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
            bool fastbuildOnly)
        {
            var firstConf = context.ProjectConfigurations.First();

            if (!fastbuildOnly)
            {
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
            }

            var projectFilesWriter = new FileGenerator(fileGenerator.Resolver);

            if (!fastbuildOnly)
            {
                string externalReferencesCopyLocal = (firstConf.Project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.ExternalReferences)
                                           ? "true"
                                           : FileGeneratorUtilities.RemoveLineTag);

                foreach (var reference in firstConf.ReferencesByPath)
                {
                    string nameWithExtension = reference.Split(Util.WindowsSeparator).Last();
                    string name = nameWithExtension.Substring(0, nameWithExtension.LastIndexOf('.'));

                    using (projectFilesWriter.Declare("include", name))
                    using (projectFilesWriter.Declare("hintPath", reference))
                    using (projectFilesWriter.Declare("private", externalReferencesCopyLocal))
                    {
                        projectFilesWriter.Write(Template.Project.ReferenceByPath);
                    }
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
                        // Don't add any Fastbuild deps to fastbuild projects, that's already handled
                        if (fastbuildOnly && dependency.IsFastBuild)
                            continue;

                        if (dependency.Project.SharpmakeProjectType == Project.ProjectTypeAttribute.Export)
                            continue; // Can't generate a project dependency for export projects(the project doesn't exist!!).

                        string include = Util.PathGetRelative(firstConf.ProjectPath, dependency.ProjectFullFileNameWithExtension);

                        // If dependency project is marked as [Compile], read the GUID from the project file
                        if (string.IsNullOrEmpty(dependency.ProjectGuid) || dependency.ProjectGuid == Guid.Empty.ToString())
                        {
                            if (dependency.Project.SharpmakeProjectType == Project.ProjectTypeAttribute.Compile)
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

                        using (projectFilesWriter.Declare("include", include))
                        using (projectFilesWriter.Declare("projectGUID", dependency.ProjectGuid ?? FileGeneratorUtilities.RemoveLineTag))
                        using (projectFilesWriter.Declare("projectRefName", dependency.ProjectName))
                        using (projectFilesWriter.Declare("private", projectDependenciesCopyLocal))
                        using (projectFilesWriter.Declare("options", options))
                        {
                            projectFilesWriter.Write(Template.Project.ProjectReference);
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

                        using (projectFilesWriter.Declare("include", relativeToProjectFile))
                        using (projectFilesWriter.Declare("projectGUID", projectGuid))
                        using (projectFilesWriter.Declare("projectRefName", FileGeneratorUtilities.RemoveLineTag))
                        using (projectFilesWriter.Declare("private", FileGeneratorUtilities.RemoveLineTag))
                        using (projectFilesWriter.Declare("options", options))
                        {
                            projectFilesWriter.Write(Template.Project.ProjectReference);
                        }
                    }
                }
            }

            if (context.Builder.Diagnostics
                && context.Project.AllowInconsistentDependencies == false
                && context.ProjectConfigurations.Any(c => ConfigurationNeedReferences(c)))
            {
                CheckReferenceDependenciesConsistency(context);
            }

            bool addDependencies = context.Project.AllowInconsistentDependencies
                ? context.ProjectConfigurations.Any(c => ConfigurationNeedReferences(c))
                : ConfigurationNeedReferences(firstConf);

            if (addDependencies)
            {
                var dependencies = new UniqueList<ProjectDependencyInfo>();
                foreach (var configuration in context.ProjectConfigurations)
                {
                    var configDeps = new UniqueList<Project.Configuration>();
                    configDeps.AddRange(configuration.ConfigurationDependencies);
                    configDeps.AddRange(configuration.BuildOrderDependencies);
                    foreach (var configurationDependency in configDeps)
                    {
                        // Ignore projects marked as Export
                        if (configurationDependency.Project.SharpmakeProjectType == Project.ProjectTypeAttribute.Export)
                            continue;

                        // Ignore exe and utility outputs
                        if (configurationDependency.Output == Project.Configuration.OutputType.Exe ||
                            configurationDependency.Output == Project.Configuration.OutputType.Utility)
                            continue;

                        // Ignore FastBuild projects if this is already a FastBuild project.
                        if (configurationDependency.IsFastBuild && fastbuildOnly)
                            continue;

                        ProjectDependencyInfo depInfo;
                        depInfo.ProjectFullFileNameWithExtension = configurationDependency.ProjectFullFileNameWithExtension;

                        // If dependency project is marked as [Compile], read the GUID from the project file
                        depInfo.ProjectGuid = configurationDependency.Project.SharpmakeProjectType == Project.ProjectTypeAttribute.Compile ? ReadGuidFromProjectFile(configurationDependency) : configurationDependency.ProjectGuid;

                        depInfo.ContainsASM = configurationDependency.Project.ContainsASM;

                        dependencies.Add(depInfo);
                    }
                }

                Options.ExplicitOptions options = context.ProjectConfigurationOptions[firstConf];
                foreach (var dependencyInfo in dependencies)
                {
                    string include = Util.PathGetRelative(context.ProjectDirectory, dependencyInfo.ProjectFullFileNameWithExtension);

                    string backupUseLibraryDependencyInputs = options["UseLibraryDependencyInputs"];
                    if (dependencyInfo.ContainsASM)
                    {
                        // Work around ms-build bug 
                        // Obj files generated in referenced projects by MASM are not linked automatically when "Use Library Dependency Inputs" is set to true
                        // https://connect.microsoft.com/VisualStudio/feedback/details/679267/obj-files-generated-in-referenced-projects-by-masm-are-not-linked-automatically-when-use-library-dependency-inputs-is-set-to-true
                        options["UseLibraryDependencyInputs"] = "false";
                    }

                    using (projectFilesWriter.Declare("include", include))
                    using (projectFilesWriter.Declare("projectGUID", dependencyInfo.ProjectGuid))
                    using (projectFilesWriter.Declare("projectRefName", FileGeneratorUtilities.RemoveLineTag)) // not needed it seems
                    using (projectFilesWriter.Declare("private", FileGeneratorUtilities.RemoveLineTag)) // TODO: check the conditions for a reference to be private
                    using (projectFilesWriter.Declare("options", options))
                    {
                        projectFilesWriter.Write(Template.Project.ProjectReference);
                    }

                    options["UseLibraryDependencyInputs"] = backupUseLibraryDependencyInputs;
                }
            }

            var projectFilesText = projectFilesWriter.ToString();
            if (!string.IsNullOrWhiteSpace(projectFilesText))
            {
                fileGenerator.Write(Template.Project.ProjectFilesBegin);
                fileGenerator.Write(projectFilesText);
                fileGenerator.Write(Template.Project.ProjectFilesEnd);
            }

            foreach (var platforms in context.PresentPlatforms.Values)
                platforms.GeneratePlatformReferences(context, fileGenerator);
        }

        private static bool ConfigurationNeedReferences(Project.Configuration conf)
        {
            return conf.Output == Project.Configuration.OutputType.Exe
                || conf.Output == Project.Configuration.OutputType.Dll
                || (conf.Output == Project.Configuration.OutputType.Lib && conf.ExportAdditionalLibrariesEvenForStaticLib)
                || conf.Output == Project.Configuration.OutputType.DotNetConsoleApp
                || conf.Output == Project.Configuration.OutputType.DotNetClassLibrary
                || conf.Output == Project.Configuration.OutputType.DotNetWindowsApp;
        }

        private void CheckReferenceDependenciesConsistency(IVcxprojGenerationContext context)
        {
            bool inconsistencyDetected = false;
            System.Text.StringBuilder inconsistencyReports = new System.Text.StringBuilder("");
            for (int i = 0; i < context.ProjectConfigurations.Count; ++i)
            {
                var iDeps = context.ProjectConfigurations.ElementAt(i).ConfigurationDependencies.Where(d => d.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Export).Select(x => x.ProjectFullFileNameWithExtension);
                for (int j = 0; j < context.ProjectConfigurations.Count; ++j)
                {
                    if (i == j)
                        continue;

                    var jDeps = context.ProjectConfigurations.ElementAt(j).ConfigurationDependencies.Where(d => d.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Export).Select(x => x.ProjectFullFileNameWithExtension);

                    var ex = iDeps.Except(jDeps);
                    if (ex.Count() != 0)
                    {
                        inconsistencyDetected = true;
                        inconsistencyReports.Append($"Config1: {context.ProjectConfigurations.ElementAt(i)}\n");
                        inconsistencyReports.Append($"Config2: {context.ProjectConfigurations.ElementAt(j)}\n");
                        inconsistencyReports.Append("Config1 depends on the following projects but not Config2:\n=> ");
                        inconsistencyReports.Append(String.Join(Environment.NewLine + "=> ", ex.ToList()) + Environment.NewLine);
                        inconsistencyReports.Append(new string('-', 70) + Environment.NewLine);
                    }
                }
            }

            if (inconsistencyDetected)
                Builder.Instance.LogErrorLine($"{context.Project.SharpmakeCsFileName}: Error: Dependencies in {FileName}{ProjectExtension} are different between configurations:\n{inconsistencyReports.ToString()}");
        }

        private void GenerateBffFilesSection(IVcxprojGenerationContext context, IFileGenerator fileGenerator)
        {
            // Add FastBuild bff file to Project
            if (FastBuildSettings.IncludeBFFInProjects)
            {
                string fastBuildFile = Bff.GetBffFileName(".", context.Configuration.BffFileName);
                fastBuildFile = Util.SimplifyPath(fastBuildFile);

                fileGenerator.Write(Template.Project.ProjectFilesBegin);
                {
                    using (fileGenerator.Declare("fastBuildFile", fastBuildFile))
                        fileGenerator.Write(Template.Project.ProjectFilesFastBuildFile);
                }
                fileGenerator.Write(Template.Project.ProjectFilesEnd);
            }
        }

        private void GenerateFiltersFile(
            IVcxprojGenerationContext context,
            string filtersFileName,
            IList<Tuple<string, List<Vcxproj.ProjectFile>>> allFileLists,
            string relativeCopyDependenciesFileName,
            Resolver resolver,
            IList<string> generatedFiles,
            IList<string> skipFiles
        )
        {
            // write [].vcxproj.filters
            var fileGenerator = new XmlFileGenerator(resolver);
            using (fileGenerator.Declare("toolsVersion", context.DevelopmentEnvironmentsRange.MinDevEnv.GetVisualProjectToolsVersionString()))
            {
                fileGenerator.Write(Vcxproj.Template.Project.Filters.Begin);
            }

            HashSet<string> allFilters = new HashSet<string>();
            foreach (var entry in allFileLists)
            {
                string type = entry.Item1;
                var files = entry.Item2;
                if (files.Count != 0)
                {
                    using (fileGenerator.Declare("type", type))
                    {
                        // write include...
                        fileGenerator.Write(Vcxproj.Template.Project.ItemGroupBegin);
                        foreach (var file in files)
                        {
                            using (fileGenerator.Declare("file", file))
                            {
                                if (file.FilterPath.Length == 0)
                                {
                                    fileGenerator.Write(Vcxproj.Template.Project.Filters.FileNoFilter);
                                }
                                else
                                {
                                    fileGenerator.Write(Vcxproj.Template.Project.Filters.FileWithFilter);
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
                    fileGenerator.Write(Vcxproj.Template.Project.Filters.FileWithDependencyFilter);
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
                        fileGenerator.Write(Vcxproj.Template.Project.Filters.Filter);
                }
                fileGenerator.Write(Vcxproj.Template.Project.ItemGroupEnd);
            }

            fileGenerator.Write(Vcxproj.Template.Project.Filters.ProjectFiltersEnd);

            // Write the project file
            FileInfo projectFiltersFileInfo = new FileInfo(filtersFileName);

            if (context.Builder.Context.WriteGeneratedFile(context.Project.GetType(), projectFiltersFileInfo, fileGenerator.ToMemoryStream()))
                generatedFiles.Add(projectFiltersFileInfo.FullName);
            else
                skipFiles.Add(projectFiltersFileInfo.FullName);
        }

        private void GenerateFilesSection(
            GenerationContext context,
            IFileGenerator fileGenerator,
            IList<string> generatedFiles,
            IList<string> skipFiles
        )
        {
            string filtersFileName = context.ProjectPath + ProjectExtension + ProjectFilterExtension;
            string copyDependenciesFileName = context.ProjectPath + CopyDependenciesExtension;
            string relativeCopyDependenciesFileName = Util.PathGetRelative(context.ProjectDirectory, copyDependenciesFileName, false);

            Strings projectFiles = context.Project.GetSourceFilesForConfigurations(context.ProjectConfigurations);

            // Add source files
            var allFiles = new List<ProjectFile>();
            var includeFiles = new List<ProjectFile>();
            var sourceFiles = new List<ProjectFile>();
            var NatvisFiles = new List<ProjectFile>();
            var PRIFiles = new List<ProjectFile>();
            var NoneFiles = new List<ProjectFile>();
            var XResourcesReswFiles = new List<ProjectFile>();
            var XResourcesImgFiles = new List<ProjectFile>();
            var customBuildFiles = new List<ProjectFile>();

            foreach (string file in context.Project.NatvisFiles)
            {
                var natvisFile = new ProjectFile(context, file);
                NatvisFiles.Add(natvisFile);
            }

            foreach (string file in context.Project.NoneFiles)
            {
                var priFile = new ProjectFile(context, file);
                NoneFiles.Add(priFile);
            }

            foreach (string file in projectFiles)
            {
                var projectFile = new ProjectFile(context, file);
                allFiles.Add(projectFile);
            }

            allFiles.Sort((l, r) => { return string.Compare(l.FileNameProjectRelative, r.FileNameProjectRelative, StringComparison.InvariantCultureIgnoreCase); });

            // Gather files with custom build steps.
            var configurationCustomFileBuildSteps = new Dictionary<Project.Configuration, Dictionary<string, CombinedCustomFileBuildStep>>();
            Strings configurationCustomBuildFiles = new Strings();
            foreach (Project.Configuration config in context.ProjectConfigurations)
            {
                using (fileGenerator.Resolver.NewScopedParameter("project", context.Project))
                using (fileGenerator.Resolver.NewScopedParameter("config", config))
                using (fileGenerator.Resolver.NewScopedParameter("target", config.Target))
                {
                    var customFileBuildSteps = CombineCustomFileBuildSteps(context.ProjectDirectory, fileGenerator.Resolver, config.CustomFileBuildSteps.Where(step => step.Filter != Project.Configuration.CustomFileBuildStep.ProjectFilter.BFFOnly));
                    configurationCustomFileBuildSteps.Add(config, customFileBuildSteps);
                    foreach (var customBuildSetup in customFileBuildSteps)
                    {
                        configurationCustomBuildFiles.Add(customBuildSetup.Key);
                    }
                }
            }

            // type -> files
            var customSourceFiles = new Dictionary<string, List<ProjectFile>>();
            foreach (var projectFile in allFiles)
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
                else if (configurationCustomBuildFiles.Contains(projectFile.FileNameProjectRelative))
                {
                    customBuildFiles.Add(projectFile);
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
                foreach (var file in includeFiles)
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
                            using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
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
                foreach (var file in includeFiles)
                {
                    using (fileGenerator.Declare("file", file))
                        fileGenerator.Write(Template.Project.ProjectFilesHeader);
                }
            }
            fileGenerator.Write(Template.Project.ProjectFilesEnd);

            if (customBuildFiles.Count > 0)
            {
                // Write custom build steps
                fileGenerator.Write(Template.Project.ProjectFilesBegin);

                foreach (var file in customBuildFiles)
                {
                    using (fileGenerator.Declare("file", file.FileNameProjectRelative))
                    using (fileGenerator.Declare("filetype", FileGeneratorUtilities.RemoveLineTag))
                    {
                        fileGenerator.Write(Template.Project.ProjectFilesCustomBuildBegin);

                        foreach (Project.Configuration conf in context.ProjectConfigurations)
                        {
                            CombinedCustomFileBuildStep buildStep;
                            if (configurationCustomFileBuildSteps[conf].TryGetValue(file.FileNameProjectRelative, out buildStep))
                            {
                                using (fileGenerator.Declare("conf", conf))
                                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
                                using (fileGenerator.Declare("description", buildStep.Description))
                                using (fileGenerator.Declare("command", buildStep.Commands))
                                using (fileGenerator.Declare("inputs", buildStep.AdditionalInputs))
                                using (fileGenerator.Declare("outputs", buildStep.Outputs))
                                {
                                    fileGenerator.Write(Template.Project.ProjectFilesCustomBuildDescription);
                                    fileGenerator.Write(Template.Project.ProjectFilesCustomBuildCommand);
                                    fileGenerator.Write(Template.Project.ProjectFilesCustomBuildInputs);
                                    fileGenerator.Write(Template.Project.ProjectFilesCustomBuildOutputs);
                                }
                            }
                        }

                        fileGenerator.Write(Template.Project.ProjectFilesCustomBuildEnd);
                    }
                }

                fileGenerator.Write(Template.Project.ProjectFilesEnd);
            }

            // Write natvis files
            if (context.Project.NatvisFiles.Count > 0 && context.ProjectConfigurations.Any(conf => conf.Target.HaveFragment<DevEnv>() && conf.Target.GetFragment<DevEnv>() >= DevEnv.vs2015))
            {
                fileGenerator.Write(Template.Project.ProjectFilesBegin);
                foreach (var file in NatvisFiles)
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
                foreach (string file in context.Project.PRIFiles.SortedValues)
                {
                    var priFile = new ProjectFile(context, file);
                    PRIFiles.Add(priFile);
                    writtenPRIFiles.Add(priFile.FileNameProjectRelative);
                    using (fileGenerator.Declare("file", priFile))
                        fileGenerator.Write(Template.Project.ProjectFilesPRIResources);
                }
                fileGenerator.Write(Template.Project.ProjectFilesEnd);
            }

            // Write None files
            if (context.Project.NoneFiles.Count > 0)
            {
                fileGenerator.Write(Template.Project.ProjectFilesBegin);
                foreach (string file in context.Project.NoneFiles)
                {
                    var projectFile = new ProjectFile(context, file);
                    using (fileGenerator.Declare("file", projectFile))
                        fileGenerator.Write(Template.Project.ProjectFilesNone);
                }
                fileGenerator.Write(Template.Project.ProjectFilesEnd);
            }

            foreach (var platform in context.PresentPlatforms.Values)
            {
                platform.GeneratePlatformResourceFileList(context, fileGenerator, writtenPRIFiles, XResourcesReswFiles, XResourcesImgFiles);

                var customPlatformFiles = platform.GetPlatformFileLists(context);
                foreach (var tuple in customPlatformFiles)
                {
                    string type = tuple.Item1;
                    var files = tuple.Item2;
                    customSourceFiles.GetValueOrAdd(type, new List<ProjectFile>()).AddRange(files);
                }
            }

            fileGenerator.Write(Template.Project.ProjectFilesBegin);

            // Validation map
            var configurationCompiledFiles = new List<List<ProjectFile>>();
            foreach (Project.Configuration conf in context.ProjectConfigurations)
                configurationCompiledFiles.Add(new List<ProjectFile>());

            bool hasCustomBuildForAllSources = context.ProjectConfigurations.First().CustomBuildForAllSources != null;
            if (hasCustomBuildForAllSources)
            {
                foreach (var file in sourceFiles)
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
                            using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
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
                foreach (var file in sourceFiles)
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
                            context.Configuration = conf;
                            var platformVcxproj = context.PresentPlatforms[conf.Platform];

                            var compiledFiles = configurationCompiledFiles[i];

                            bool hasPrecomp = platformVcxproj.HasPrecomp(context);
                            bool isPrecompSource = !string.IsNullOrEmpty(conf.PrecompSource) && file.FileName.EndsWith(conf.PrecompSource, StringComparison.OrdinalIgnoreCase);
                            bool isDontUsePrecomp = conf.PrecompSourceExclude.Contains(file.FileName) ||
                                                    conf.PrecompSourceExcludeFolders.Any(folder => file.FileName.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) ||
                                                    conf.PrecompSourceExcludeExtension.Contains(file.FileExtension);

                            bool isExcludeFromBuild = conf.ResolvedSourceFilesBuildExclude.Contains(file.FileName);
                            bool consumeWinRTExtensions = conf.ConsumeWinRTExtensions.Contains(file.FileName) || conf.ResolvedSourceFilesWithCompileAsWinRTOption.Contains(file.FileName);
                            bool excludeWinRTExtensions = conf.ExcludeWinRTExtensions.Contains(file.FileName) || conf.ResolvedSourceFilesWithExcludeAsWinRTOption.Contains(file.FileName);

                            bool isBlobFileDefine = conf.BlobFileDefine != String.Empty && file.FileName.EndsWith(Project.BlobExtension, StringComparison.OrdinalIgnoreCase);
                            bool isResourceFileDefine = conf.ResourceFileDefine != String.Empty && file.FileName.EndsWith(".rc", StringComparison.OrdinalIgnoreCase);
                            bool isCompileAsCFile = conf.ResolvedSourceFilesWithCompileAsCOption.Contains(file.FileName);
                            bool isCompileAsCPPFile = conf.ResolvedSourceFilesWithCompileAsCPPOption.Contains(file.FileName);
                            bool isCompileAsCLRFile = conf.ResolvedSourceFilesWithCompileAsCLROption.Contains(file.FileName);
                            bool isCompileAsNonCLRFile = conf.ResolvedSourceFilesWithCompileAsNonCLROption.Contains(file.FileName);
                            bool objsInSubdirectories = conf.ObjectFileName != null && !isResource;
                            bool isExcludeFromGenerateXmlDocumentation = conf.ResolvedSourceFilesGenerateXmlDocumentationExclude.Contains(file.FileName);

                            if (isPrecompSource && platformVcxproj.ExcludesPrecompiledHeadersFromBuild)
                                isExcludeFromBuild = true;
                            if (!isExcludeFromBuild && !isResource)
                                compiledFiles.Add(file);

                            if (isCompileAsCLRFile || consumeWinRTExtensions || excludeWinRTExtensions)
                                isDontUsePrecomp = true;
                            if (string.Compare(file.FileExtension, ".c", StringComparison.OrdinalIgnoreCase) == 0)
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
                                              (isDontUsePrecomp && hasPrecomp) ||
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
                                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
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
                                        else if (isDontUsePrecomp && hasPrecomp)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesSourcePrecompNotUsing);

                                            // in case we are using the LLVM toolchain, the PCH was added
                                            // as force include globally for the conf, so we need
                                            // to use the forced include vanilla list that we prepared
                                            var optionsForConf = context.ProjectConfigurationOptions[conf];
                                            if (optionsForConf.ContainsKey("ForcedIncludeFilesVanilla"))
                                            {
                                                // Note: faster to test that the options array has the
                                                // vanilla list, as we only add it in case we use LLVM,
                                                // but we could also have tested
                                                // Options.GetObject<Options.Vc.General.PlatformToolset>(conf).IsLLVMToolchain()
                                                using (fileGenerator.Declare("options", optionsForConf))
                                                    fileGenerator.Write(Template.Project.ProjectFilesForcedIncludeVanilla);
                                            }
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

                                        if (isExcludeFromGenerateXmlDocumentation)
                                        {
                                            fileGenerator.Write(Template.Project.ProjectFilesSourceExcludeGenerateXmlDocumentation);
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
                    var files = customSourceFiles[typeName];
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
                                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
                                {
                                    var compiledFiles = configurationCompiledFiles[i];
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

            var copyDependenciesBuildStepDictionary = new Dictionary<Project.Configuration, Project.Configuration.FileCustomBuild>();
            foreach (var conf in context.ProjectConfigurations)
            {
                if (conf.IsFastBuild) // copies handled in bff
                    continue;

                if (conf.Output != Project.Configuration.OutputType.Exe && !conf.ExecuteTargetCopy)
                    continue;

                var copies = ProjectOptionsGenerator.ConvertPostBuildCopiesToRelative(conf, context.ProjectDirectory);
                if (!copies.Any())
                    continue;

                var copyDependenciesBuildStep = copyDependenciesBuildStepDictionary.GetValueOrAdd(conf, new Project.Configuration.FileCustomBuild("Copy files to output paths..."));
                if (conf.CopyDependenciesBuildStep != null)
                    copyDependenciesBuildStep = conf.CopyDependenciesBuildStep;

                foreach (var copy in copies)
                {
                    var sourceFile = copy.Key;
                    var destinationFolder = copy.Value;

                    copyDependenciesBuildStep.CommandLines.Add(conf.CreateTargetCopyCommand(sourceFile, destinationFolder, context.ProjectDirectory));
                    copyDependenciesBuildStep.Inputs.Add(sourceFile);
                    copyDependenciesBuildStep.Outputs.Add(Path.Combine(destinationFolder, Path.GetFileName(sourceFile)));
                }
            }

            // Write the "copy dependencies" build step (as a custom build tool on a dummy file, to make sure the copy is always done when needed)
            bool hasDependenciesToCopy = copyDependenciesBuildStepDictionary.Any();
            var dependenciesFileGenerator = new XmlFileGenerator(fileGenerator.Resolver); // borrowing resolver
            if (hasDependenciesToCopy)
            {
                fileGenerator.Write(Template.Project.ProjectFilesEnd);
                fileGenerator.Write(Template.Project.ProjectFilesBegin);

                using (fileGenerator.Declare("file", relativeCopyDependenciesFileName))
                using (fileGenerator.Declare("filetype", "Document"))
                {
                    fileGenerator.Write(Template.Project.ProjectFilesCustomBuildBegin);

                    foreach (var pair in copyDependenciesBuildStepDictionary)
                    {
                        var conf = pair.Key;
                        Project.Configuration.FileCustomBuild copyDependencies = pair.Value;

                        using (fileGenerator.Declare("conf", conf))
                        using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
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
                var compiledFiles = configurationCompiledFiles[i];

                compiledFiles.Sort((l, r) => { return String.Compare(l.FileNameWithoutExtension, r.FileNameWithoutExtension, StringComparison.OrdinalIgnoreCase); });

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
            if (context.ProjectConfigurations.Any(x => x.IsFastBuild))
                GenerateBffFilesSection(context, fileGenerator);

            var allFileLists = new List<Tuple<string, List<ProjectFile>>>();
            allFileLists.Add(Tuple.Create(hasCustomBuildForAllSources ? "CustomBuild" : "ClCompile", sourceFiles));
            allFileLists.Add(Tuple.Create("PRIResource", XResourcesReswFiles));
            allFileLists.Add(Tuple.Create("Image", XResourcesImgFiles));
            allFileLists.Add(Tuple.Create(hasCustomBuildForAllIncludes ? "CustomBuild" : "ClInclude", includeFiles));
            allFileLists.Add(Tuple.Create("CustomBuild", customBuildFiles));
            if (NatvisFiles.Count > 0)
                allFileLists.Add(Tuple.Create("Natvis", NatvisFiles));
            if (PRIFiles.Count > 0)
                allFileLists.Add(Tuple.Create("PRIResource", PRIFiles));
            if (NoneFiles.Count > 0)
                allFileLists.Add(Tuple.Create("None", NoneFiles));
            foreach (var entry in customSourceFiles)
            {
                allFileLists.Add(Tuple.Create(entry.Key, entry.Value));
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
                FileName = Project.GetCapitalizedFile(fileName) ?? fileName;

                FileNameProjectRelative = Util.PathGetRelative(context.ProjectDirectoryCapitalized, FileName, true);
                FileNameSourceRelative = Util.PathGetRelative(context.ProjectSourceCapitalized, FileName, true);

                FileExtension = Path.GetExtension(FileName);
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);

                int lastPathSeparator = FileNameSourceRelative.LastIndexOf(Util.WindowsSeparator);
                string dirSourceRelative = lastPathSeparator == -1 ? "" : FileNameSourceRelative.Substring(0, lastPathSeparator);

                string customFilterPath;
                if (context.Project.ResolveFilterPathForFile(FileNameSourceRelative, out customFilterPath) ||
                    context.Project.CustomFilterMapping.TryGetValue(dirSourceRelative, out customFilterPath) ||
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
                overwriteFile = conf.VcxprojUserFile?.OverwriteExistingFile ?? true;
                return conf.VcxprojUserFile != null;
            }
        }
    }
}
