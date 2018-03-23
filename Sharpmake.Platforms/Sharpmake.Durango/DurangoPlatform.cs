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
using System.Linq;
using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public static partial class Durango
    {
        [PlatformImplementation(Platform.durango,
            typeof(IPlatformDescriptor),
            typeof(Project.Configuration.IConfigurationTasks),
            typeof(IFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IMicrosoftPlatformBff),
            typeof(IPlatformVcxproj))]
        public sealed partial class DurangoPlatform : BaseMicrosoftPlatform, IFastBuildCompilerSettings
        {
            public override IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
            {
                var dirs = new List<string>();
                string platformDirsStr = configuration.Target.GetFragment<DevEnv>().GetDurangoLibraryPath();
                dirs.AddRange(EnumerateSemiColonSeparatedString(platformDirsStr));

                return dirs;
            }

            #region IPlatformDescriptor implementation
            public override string SimplePlatformString => "Durango";
            public override bool IsPcPlatform => false;
            public override bool HasSharedLibrarySupport => true;

            public override EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] parameters)
            {
                return new EnvironmentVariableResolver(parameters);
            }
            #endregion

            #region IPlatformFastBuildCompilerSettings implementation
            public IDictionary<DevEnv, string> BinPath { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LinkerPath { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LinkerExe { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LibrarianExe { get; set; } = new Dictionary<DevEnv, string>();
            #endregion

            #region IMicrosoftPlatformBff implementation
            public override string BffPlatformDefine => "_DURANGO";
            public override string CConfigName => ".durangoConfig";
            public override bool SupportsResourceFiles => false;
            public override bool HasUserAccountControlSupport => false;

            public override bool AddLibPrefix(Configuration conf)
            {
                return PlatformRegistry.GetDefault<IPlatformBff>().AddLibPrefix(conf);
            }

            public override void AddCompilerSettings(
                IDictionary<string, CompilerSettings> masterCompilerSettings,
                string compilerName,
                string rootPath,
                DevEnv devEnv,
                string projectRootPath
            )
            {
                switch (devEnv)
                {
                    case DevEnv.vs2012:
                        {
                            CompilerSettings compilerSettings = GetMasterCompilerSettings(masterCompilerSettings, compilerName, rootPath, devEnv, projectRootPath, false);
                            compilerSettings.PlatformFlags |= Platform.durango;
                            SetConfiguration(compilerSettings.Configurations, string.Empty, projectRootPath, devEnv, false);
                        }
                        break;
                    case DevEnv.vs2015:
                    case DevEnv.vs2017:
                        {
                            var win64PlatformSettings = PlatformRegistry.Get<IPlatformBff>(Platform.win64);

                            string overrideName = "Compiler-" + Sharpmake.Util.GetSimplePlatformString(Platform.win64) + "-" + devEnv;
                            CompilerSettings compilerSettings = win64PlatformSettings.GetMasterCompilerSettings(masterCompilerSettings, overrideName, rootPath, devEnv, projectRootPath, false);
                            compilerSettings.PlatformFlags |= Platform.durango;
                            SetConfiguration(compilerSettings.Configurations, string.Empty, projectRootPath, devEnv, false);
                        }
                        break;
                    default:
                        throw new NotImplementedException("This devEnv (" + devEnv + ") is not supported!");
                }
            }

            public override CompilerSettings GetMasterCompilerSettings(IDictionary<string, CompilerSettings> masterCompilerSettings, string compilerName, string rootPath, DevEnv devEnv, string projectRootPath, bool useCCompiler)
            {
                CompilerSettings compilerSettings;

                if (masterCompilerSettings.ContainsKey(compilerName))
                {
                    compilerSettings = masterCompilerSettings[compilerName];
                }
                else
                {
                    Strings extraFiles = new Strings();
                    string executable;

                    switch (devEnv)
                    {
                        case DevEnv.vs2012:
                            {
                                extraFiles.Add(
                                    @"$RootPath$\c1.dll",
                                    @"$RootPath$\c1xx.dll",
                                    @"$RootPath$\c1xxast.dll",
                                    @"$RootPath$\c2.dll",
                                    @"$RootPath$\msobj110.dll",
                                    @"$RootPath$\mspdb110.dll",
                                    @"$RootPath$\mspdbcore.dll",
                                    @"$RootPath$\mspdbsrv.exe",
                                    @"$RootPath$\mspft110.dll",
                                    @"$RootPath$\1033\clui.dll",
                                    @"$RootPath$\msvcp110.dll",
                                    @"$RootPath$\msvcr110.dll",
                                    @"$RootPath$\vccorlib110.dll");

                                executable = @"$RootPath$\cl.exe";
                                var fastBuildCompilerSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.durango);

                                fastBuildCompilerSettings.BinPath.TryGetValue(devEnv, out rootPath);
                            }
                            break;
                        default:
                            throw new NotImplementedException("This devEnv (" + devEnv + ") is not supported!");
                    }

                    compilerSettings = new CompilerSettings(compilerName, Platform.durango, extraFiles, executable, rootPath, devEnv, new Dictionary<string, CompilerSettings.Configuration>());
                    masterCompilerSettings.Add(compilerName, compilerSettings);
                }

                return compilerSettings;

            }

            public override void SetConfiguration(IDictionary<string, CompilerSettings.Configuration> configurations, string compilerName, string projectRootPath, DevEnv devEnv, bool useCCompiler)
            {
                string configName = ".durangoConfig";

                if (!configurations.ContainsKey(configName))
                {
                    var fastBuildCompilerSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.durango);
                    string binPath;
                    if (!fastBuildCompilerSettings.BinPath.TryGetValue(devEnv, out binPath))
                        binPath = devEnv.GetDurangoBinPath();

                    string linkerPath;
                    if (!fastBuildCompilerSettings.LinkerPath.TryGetValue(devEnv, out linkerPath))
                        linkerPath = binPath;

                    string linkerExe;
                    if (!fastBuildCompilerSettings.LinkerExe.TryGetValue(devEnv, out linkerExe))
                        linkerExe = "link.exe";

                    string librarianExe;
                    if (!fastBuildCompilerSettings.LibrarianExe.TryGetValue(devEnv, out librarianExe))
                        librarianExe = "lib.exe";

                    configurations.Add(
                        configName,
                        new CompilerSettings.Configuration(
                            Platform.durango,
                            binPath: Sharpmake.Util.GetCapitalizedPath(Sharpmake.Util.PathGetAbsolute(projectRootPath, binPath)),
                            linkerPath: Sharpmake.Util.GetCapitalizedPath(Sharpmake.Util.PathGetAbsolute(projectRootPath, linkerPath)),
                            librarian: Path.Combine(@"$LinkerPath$", librarianExe),
                            linker: Path.Combine(@"$LinkerPath$", linkerExe)
                        )
                    );

                    configurations.Add(
                        ".durangoConfigMasm",
                        new CompilerSettings.Configuration(
                            Platform.durango,
                            compiler: @"$BinPath$\ml64.exe",
                            usingOtherConfiguration: configName
                        )
                    );
                }
            }
            #endregion

            #region IPlatformVcxproj implementation
            public override string ExecutableFileExtension => "exe";
            public override string SharedLibraryFileExtension => "dll";
            public override string ProgramDatabaseFileExtension => "pdb";
            public override string StaticLibraryFileExtension => "lib";
            public override bool HasEditAndContinueDebuggingSupport => true;

            public override IEnumerable<string> GetImplicitlyDefinedSymbols(IGenerationContext context)
            {
                yield return "_DURANGO";
            }

            public override IEnumerable<string> GetCxUsingPath(IGenerationContext context)
            {
                return context.DevelopmentEnvironment.GetDurangoUsingDirectories();
            }

            public override IEnumerable<VariableAssignment> GetEnvironmentVariables(IGenerationContext context)
            {
                if (GlobalSettings.OverridenDurangoXDK)
                    yield return new VariableAssignment("MSBUILDDISABLEREGISTRYFORSDKLOOKUP", "1");
            }

            public override void SetupSdkOptions(IGenerationContext context)
            {
                var options = context.Options;
                var devEnv = context.DevelopmentEnvironment;

                if (GlobalSettings.OverridenDurangoXDK && !Util.IsDurangoSideBySideXDK() && Util.IsDurangoSideBySideXDKInstalled())
                {
                    // only use the full path to includes and libs if we have a
                    // sideByside XDK installed, but we compile using an old one.
                    // That is done because msbuild concatenates an XdkEdition in the $(Console*) variables without any way of removing it...
                    options["IncludePath"] = devEnv.GetDurangoIncludePath();
                    options["ReferencePath"] = devEnv.GetDurangoLibraryPath();
                    options["LibraryPath"] = devEnv.GetDurangoLibraryPath();
                    options["LibraryWPath"] = devEnv.GetDurangoLibraryPath();
                }
                else
                {
                    options["IncludePath"] = "$(Console_SdkIncludeRoot);";
                    options["ReferencePath"] = "$(Console_SdkLibPath);$(Console_SdkWindowsMetadataPath);";
                    options["LibraryPath"] = "$(Console_SdkLibPath);";
                    options["LibraryWPath"] = "$(Console_SdkLibPath);$(Console_SdkWindowsMetadataPath);";
                }

                //Options.Vc.General.DeployMode.     
                //    Push                                  DeployMode="Push"
                //    Pull                                  DeployMode="Pull"                          
                //    External                              DeployMode="External"                          
                context.SelectOption
                (
                    Sharpmake.Options.Option(Options.General.DeployMode.Push, () => { context.Options["DeployMode"] = FileGeneratorUtilities.RemoveLineTag; }),
                    Sharpmake.Options.Option(Options.General.DeployMode.Pull, () => { context.Options["DeployMode"] = "Pull"; }),
                    Sharpmake.Options.Option(Options.General.DeployMode.External, () => { context.Options["DeployMode"] = "External"; })
                );
            }

            public override void SetupPlatformTargetOptions(IGenerationContext context)
            {
                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;

                options["TargetMachine"] = "MachineX64";
                options["RandomizedBaseAddress"] = "true";
                cmdLineOptions["TargetMachine"] = "/MACHINE:X64";
                cmdLineOptions["RandomizedBaseAddress"] = "/DYNAMICBASE";
            }

            public override void GenerateProjectCompileVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectConfigurationsCompileTemplate);
            }

            public override void GenerateUserConfigurationFile(Project.Configuration conf, IFileGenerator generator)
            {
                generator.Write(_userFileConfigurationGeneralTemplate);
            }

            public override void GenerateRunFromPcDeployment(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                var project = context.Project;
                string registerCommand = !string.IsNullOrEmpty(project.RunFromPcDeploymentRegisterCommand) ? project.RunFromPcDeploymentRegisterCommand : FileGeneratorUtilities.RemoveLineTag;
                using (generator.Declare("DurangoRunFromPCDeploymentRegisterCommand", registerCommand))
                {
                    generator.Write(_runFromPCDeployment);
                }
            }

            public override void GenerateProjectConfigurationGeneral2(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectConfigurationsGeneral2);
            }

            public override void GenerateProjectConfigurationFastBuildMakeFile(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                base.GenerateProjectConfigurationFastBuildMakeFile(context, generator);
                generator.Write(_projectConfigurationsFastBuildMakefile);
            }

            public override void GeneratePlatformReferences(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                // Write SDKReferences on durango if needed
                // TODO: add a consistency check between configurations
                Strings sdkReferences = new Strings();
                foreach (var conf in context.ProjectConfigurations)
                {
                    if (conf.Platform == Platform.durango)
                        sdkReferences.AddRange(Sharpmake.Options.GetStrings<Options.SDKReferences>(conf));
                }

                if (sdkReferences.Count > 0)
                {
                    generator.Write(_sdkReferencesBegin);
                    foreach (string sdkReference in sdkReferences)
                    {
                        using (generator.Declare("sdkReferenceInclude", sdkReference))
                        {
                            generator.Write(_sdkReference);
                        }
                    }
                    generator.Write(_sdkReferencesEnd);
                }
            }

            public override void GeneratePlatformResourceFileList(IVcxprojGenerationContext context, IFileGenerator fileGenerator, Strings alreadyWrittenPriFiles, IList<Vcxproj.ProjectFile> resourceFiles, IList<Vcxproj.ProjectFile> imageResourceFiles)
            {
                // adding the durango resw file, if they were not in PRIFiles
                var resourceResw = new List<Vcxproj.ProjectFile>();
                foreach (string file in context.Project.XResourcesResw)
                {
                    var projectFile = new Vcxproj.ProjectFile(context, file);
                    if (!alreadyWrittenPriFiles.Contains(projectFile.FileNameProjectRelative))
                        resourceResw.Add(projectFile);
                }

                if (resourceResw.Count > 0)
                {
                    fileGenerator.Write(_projectFilesBegin);
                    foreach (var projectFile in resourceResw)
                    {
                        using (fileGenerator.Declare("file", projectFile))
                        {
                            fileGenerator.Write(_projectPriResource);
                        }
                        resourceFiles.Add(projectFile);
                    }
                    fileGenerator.Write(_projectFilesEnd);
                }

                // adding the durango img file
                if (context.Project.XResourcesImg.Count > 0)
                {
                    fileGenerator.Write(_projectFilesBegin);
                    foreach (string file in context.Project.XResourcesImg)
                    {
                        var projectFile = new Vcxproj.ProjectFile(context, file);
                        using (fileGenerator.Declare("file", projectFile))
                        {
                            fileGenerator.Write(_projectImgResource);
                        }
                        imageResourceFiles.Add(projectFile);
                    }
                    fileGenerator.Write(_projectFilesEnd);
                }

                // WARNING: THIS IS A PATCH TO ADD THE XMANIFEST FILE IN THE FILE LIST
                //          This is not a clean way to do but it is the only way we found so far
                if (context.ProjectConfigurations.First().NeedsAppxManifestFile)
                {
                    fileGenerator.Write(_projectFilesBegin);
                    {
                        string file = Sharpmake.Util.GetAppxManifestFileName(context.ProjectConfigurations.First());
                        var projectFile = new Vcxproj.ProjectFile(context, file);
                        using (fileGenerator.Declare("file", projectFile))
                        {
                            fileGenerator.Write(_projectFilesXManifest);
                        }
                    }
                    fileGenerator.Write(_projectFilesEnd);
                }
            }

            public override void GeneratePlatformSpecificProjectDescription(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                if (GlobalSettings.OverridenDurangoXDK)
                {
                    string durangoXdkKitPath = FileGeneratorUtilities.RemoveLineTag;
                    string xdkEditionTarget = FileGeneratorUtilities.RemoveLineTag;
                    string targetPlatformSdkPath = FileGeneratorUtilities.RemoveLineTag;
                    string durangoXdkCompilers = FileGeneratorUtilities.RemoveLineTag;
                    string gameOSFilePath = FileGeneratorUtilities.RemoveLineTag;
                    string durangoXdkTasks = FileGeneratorUtilities.RemoveLineTag;
                    string targetPlatformIdentifier = FileGeneratorUtilities.RemoveLineTag;
                    string platformFolder = MSBuildGlobalSettings.GetCppPlatformFolder(context.DevelopmentEnvironmentsRange.MinDevEnv, Platform.durango);
                    string xdkEditionRootVS2015 = FileGeneratorUtilities.RemoveLineTag;
                    string xdkEditionRootVS2017 = FileGeneratorUtilities.RemoveLineTag;
                    string enableLegacyXdkHeaders = FileGeneratorUtilities.RemoveLineTag;

                    if (!Util.IsDurangoSideBySideXDK())
                    {
                        durangoXdkKitPath = Sharpmake.Util.EnsureTrailingSeparator(Path.Combine(GlobalSettings.DurangoXDK, "xdk"));

                        // Set only if the machine has a SideBySide XDK installed, but we don't generate for one
                        if (Util.IsDurangoSideBySideXDKInstalled())
                        {
                            if (context.ProjectConfigurations.Any(conf => !conf.IsFastBuild))
                                durangoXdkCompilers = durangoXdkKitPath;

                            gameOSFilePath = Path.Combine(GlobalSettings.DurangoXDK, "sideload", "era.xvd");

                            // Use the tasks of the system
                            durangoXdkTasks = Sharpmake.Util.EnsureTrailingSeparator(Path.Combine(Util.GetDurangoXDKInstallPath(), Util.GetLatestDurangoSideBySideXDKInstalled(), "PC", "tasks"));
                            targetPlatformIdentifier = "Xbox.xdk";
                        }
                    }
                    else
                    {
                        xdkEditionTarget = GlobalSettings.XdkEditionTarget;
                        targetPlatformSdkPath = Util.GetDurangoExtensionXDK();
                    }

                    generator.Write(Vcxproj.Template.Project.ProjectDescriptionStartPlatformConditional);
                    {
                        if (!string.IsNullOrEmpty(platformFolder))
                        {
                            using (generator.Declare("custompropertyname", "_PlatformFolder"))
                            using (generator.Declare("custompropertyvalue", platformFolder))
                                generator.Write(Vcxproj.Template.Project.CustomProperty);
                        }

                        if (DevEnv.vs2015 >= context.DevelopmentEnvironmentsRange.MinDevEnv && DevEnv.vs2015 <= context.DevelopmentEnvironmentsRange.MaxDevEnv)
                        {
                            var vs2015PlatformFolder = MSBuildGlobalSettings.GetCppPlatformFolder(DevEnv.vs2015, Platform.durango);
                            if (!string.IsNullOrEmpty(vs2015PlatformFolder))
                                xdkEditionRootVS2015 = vs2015PlatformFolder;
                        }

                        if (DevEnv.vs2017 >= context.DevelopmentEnvironmentsRange.MinDevEnv && DevEnv.vs2017 <= context.DevelopmentEnvironmentsRange.MaxDevEnv)
                        {
                            var vs2017PlatformFolder = MSBuildGlobalSettings.GetCppPlatformFolder(DevEnv.vs2017, Platform.durango);
                            if (!string.IsNullOrEmpty(vs2017PlatformFolder))
                                xdkEditionRootVS2017 = vs2017PlatformFolder;

                            int xdkEdition;
                            bool isMinFeb2018Xdk = Util.TryParseXdkEditionTarget(GlobalSettings.XdkEditionTarget, out xdkEdition) && xdkEdition > GlobalSettings._feb2018XdkEditionTarget;
                            if (GlobalSettings.EnableLegacyXdkHeaders && isMinFeb2018Xdk)
                                enableLegacyXdkHeaders = "true";
                        }

                        using (generator.Declare("durangoXdkInstallPath", GlobalSettings.DurangoXDK))
                        using (generator.Declare("sdkReferenceDirectoryRoot", GlobalSettings.XboxOneExtensionSDK))
                        using (generator.Declare("durangoXdkKitPath", durangoXdkKitPath))
                        using (generator.Declare("xdkEditionTarget", xdkEditionTarget))
                        using (generator.Declare("targetPlatformSdkPath", targetPlatformSdkPath))
                        using (generator.Declare("durangoXdkCompilers", durangoXdkCompilers))
                        using (generator.Declare("gameOSFilePath", gameOSFilePath))
                        using (generator.Declare("durangoXdkTasks", durangoXdkTasks))
                        using (generator.Declare("targetPlatformIdentifier", targetPlatformIdentifier))
                        using (generator.Declare("xdkEditionRootVS2015", xdkEditionRootVS2015))
                        using (generator.Declare("xdkEditionRootVS2017", xdkEditionRootVS2017))
                        using (generator.Declare("enableLegacyXdkHeaders", enableLegacyXdkHeaders))
                        {
                            generator.Write(_projectDescriptionPlatformSpecific);
                        }
                    }
                    generator.Write(Vcxproj.Template.Project.PropertyGroupEnd);
                }
            }

            protected override string GetProjectLinkSharedVcxprojTemplate()
            {
                return _projectConfigurationsLinkTemplate;
            }

            protected override IEnumerable<string> GetPlatformIncludePathsImpl(IGenerationContext context)
            {
                return EnumerateSemiColonSeparatedString(context.DevelopmentEnvironment.GetDurangoIncludePath());
            }

            #endregion
        }
    }
}
