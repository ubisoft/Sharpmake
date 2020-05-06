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
using System.Linq;
using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public static partial class Linux
    {
        [PlatformImplementation(Platform.linux,
            typeof(IPlatformDescriptor),
            typeof(IFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IClangPlatformBff),
            typeof(IPlatformVcxproj),
            typeof(Project.Configuration.IConfigurationTasks))]
        public sealed partial class LinuxPlatform : BasePlatform, Project.Configuration.IConfigurationTasks, IFastBuildCompilerSettings, IClangPlatformBff
        {
            #region IPlatformDescriptor implementation
            public override string SimplePlatformString => "x64";
            public override bool IsMicrosoftPlatform => false; // No way!
            public override bool IsPcPlatform => true;
            public override bool IsUsingClang => true; // Maybe now? Traditionally GCC but only the GNU project is backing it now.
            //Used for bff generation in Bff.cs. We might need to use False and implement linux bff generation.
            public override bool HasDotNetSupport => false; // Technically false with .NET Core and Mono.
            public override bool HasSharedLibrarySupport => true;

            public override EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] parameters)
            {
                return new EnvironmentVariableResolver(parameters);
            }
            #endregion

            #region Project.Configuration.IConfigurationTasks implementation

            public string GetDefaultOutputExtension(Project.Configuration.OutputType outputType)
            {
                switch (outputType)
                {
                    case Project.Configuration.OutputType.Exe:
                        return string.Empty;
                    case Project.Configuration.OutputType.Dll:
                        return "so";
                    default:
                        return "a";
                }
            }

            public IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration conf)
            {
                if (GlobalSettings.SystemPathProvider != null)
                    return GlobalSettings.SystemPathProvider.GetSystemLibraryPaths(conf);

                return Enumerable.Empty<string>();
            }

            public void SetupDynamicLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
            {
                DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
            }

            public void SetupStaticLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
            {
                DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
            }

            #endregion

            #region IPlatformVcxproj implementation
            public override string ProgramDatabaseFileExtension => string.Empty;
            public override string StaticLibraryFileExtension => "a";
            public override string SharedLibraryFileExtension => "so";
            public override string StaticOutputLibraryFileExtension => string.Empty;
            public override string ExecutableFileExtension => string.Empty;

            // Ideally the object files should be suffixed .o when compiling with FastBuild, using the CompilerOutputExtension property in ObjectLists

            public override string GetOutputFileNamePrefix(IGenerationContext context, Project.Configuration.OutputType outputType)
            {
                if (outputType != Project.Configuration.OutputType.Exe)
                    return "lib";
                return string.Empty;
            }

            public override void SetupPlatformToolsetOptions(IGenerationContext context)
            {
                context.Options["PlatformToolset"] = "Remote_GCC_1_0";
            }

            public override void SetupPlatformTargetOptions(IGenerationContext context)
            {
                context.Options["TargetMachine"] = "MachineX64";
                context.Options["RandomizedBaseAddress"] = "true";
                context.CommandLineOptions["TargetMachine"] = "/MACHINE:X64";
                context.CommandLineOptions["RandomizedBaseAddress"] = "/DYNAMICBASE";
            }

            public override void SetupSdkOptions(IGenerationContext context)
            {
                context.SelectOption
                (
                    Sharpmake.Options.Option(Options.General.CopySources.Enable, () => { context.Options["CopySources"] = "true"; }),
                    Sharpmake.Options.Option(Options.General.CopySources.Disable, () => { context.Options["CopySources"] = "false"; })
                );

                context.SelectOption
                (
                    Sharpmake.Options.Option(Options.General.PlatformRemoteTool.Gpp, () => { context.Options["RemoteCppCompileToolExe"] = "g++"; }),
                    Sharpmake.Options.Option(Options.General.PlatformRemoteTool.Clang38, () => { context.Options["RemoteCppCompileToolExe"] = "clang++-3.8"; })
                );
                context.SelectOption
                (
                    Sharpmake.Options.Option(Options.General.PlatformRemoteTool.Gpp, () => { context.Options["RemoteCCompileToolExe"] = "g++"; }),
                    Sharpmake.Options.Option(Options.General.PlatformRemoteTool.Clang38, () => { context.Options["RemoteCCompileToolExe"] = "clang-3.8"; })
                );
                context.SelectOption
                (
                    Sharpmake.Options.Option(Options.General.PlatformRemoteTool.Gpp, () => { context.Options["RemoteLdToolExe"] = "g++"; }),
                    Sharpmake.Options.Option(Options.General.PlatformRemoteTool.Clang38, () => { context.Options["RemoteLdToolExe"] = "clang-3.8"; })
                );
            }

            public override void SelectCompilerOptions(IGenerationContext context)
            {
                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.GenerateDebugInformation.Enable, () => { options["GenerateDebugInformation"] = "true"; cmdLineOptions["CLangGenerateDebugInformation"] = "-g"; }),
                Sharpmake.Options.Option(Options.Compiler.GenerateDebugInformation.Disable, () => { options["GenerateDebugInformation"] = "false"; cmdLineOptions["CLangGenerateDebugInformation"] = ""; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.Warnings.NormalWarnings, () => { options["Warnings"] = "NormalWarnings"; cmdLineOptions["Warnings"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Compiler.Warnings.MoreWarnings, () => { options["Warnings"] = "MoreWarnings"; cmdLineOptions["Warnings"] = "-Wall"; }),
                Sharpmake.Options.Option(Options.Compiler.Warnings.Disable, () => { options["Warnings"] = "WarningsOff"; cmdLineOptions["Warnings"] = "-w"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.ExtraWarnings.Enable, () => { options["ExtraWarnings"] = "true"; cmdLineOptions["ExtraWarnings"] = "-Wextra"; }),
                Sharpmake.Options.Option(Options.Compiler.ExtraWarnings.Disable, () => { options["ExtraWarnings"] = "false"; cmdLineOptions["ExtraWarnings"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Sharpmake.Options.Vc.General.TreatWarningsAsErrors.Enable, () => { options["WarningsAsErrors"] = "true"; cmdLineOptions["WarningsAsErrors"] = "-Werror"; }),
                Sharpmake.Options.Option(Sharpmake.Options.Vc.General.TreatWarningsAsErrors.Disable, () => { options["WarningsAsErrors"] = "false"; cmdLineOptions["WarningsAsErrors"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.InlineFunctionDebugInformation.Enable, () => { options["InlineFunctionDebugInformation"] = "true"; if (conf.IsFastBuild) throw new NotImplementedException("FIXME!"); }),
                Sharpmake.Options.Option(Options.Compiler.InlineFunctionDebugInformation.Disable, () => { options["InlineFunctionDebugInformation"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                Options.Compiler.ProcessorNumber processorNumber = Sharpmake.Options.GetObject<Options.Compiler.ProcessorNumber>(conf);
                if (processorNumber == null)
                    options["ProcessorNumber"] = FileGeneratorUtilities.RemoveLineTag;
                else
                    options["ProcessorNumber"] = processorNumber.Value.ToString();

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.Distributable.Enable, () => { options["Distributable"] = "true"; }),
                Sharpmake.Options.Option(Options.Compiler.Distributable.Disable, () => { options["Distributable"] = "false"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.OptimizationLevel.Disable, () => { options["OptimizationLevel"] = "Level0"; cmdLineOptions["OptimizationLevel"] = "-O0"; }),
                Sharpmake.Options.Option(Options.Compiler.OptimizationLevel.Standard, () => { options["OptimizationLevel"] = "Level1"; cmdLineOptions["OptimizationLevel"] = "-O1"; }),
                Sharpmake.Options.Option(Options.Compiler.OptimizationLevel.Full, () => { options["OptimizationLevel"] = "Level2"; cmdLineOptions["OptimizationLevel"] = "-O2"; }),
                Sharpmake.Options.Option(Options.Compiler.OptimizationLevel.FullWithInlining, () => { options["OptimizationLevel"] = "Level3"; cmdLineOptions["OptimizationLevel"] = "-O3"; }),
                Sharpmake.Options.Option(Options.Compiler.OptimizationLevel.ForSize, () => { options["OptimizationLevel"] = "Levels"; cmdLineOptions["OptimizationLevel"] = "-Os"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.PositionIndependentCode.Disable, () => { options["PositionIndependentCode"] = "false"; cmdLineOptions["PositionIndependentCode"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Compiler.PositionIndependentCode.Enable, () => { options["PositionIndependentCode"] = "true"; cmdLineOptions["PositionIndependentCode"] = "-fPIC"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.FastMath.Enable, () => { options["FastMath"] = "true"; cmdLineOptions["FastMath"] = "-ffast-math"; }),
                Sharpmake.Options.Option(Options.Compiler.FastMath.Disable, () => { options["FastMath"] = "false"; cmdLineOptions["FastMath"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.NoStrictAliasing.Enable, () => { options["NoStrictAliasing"] = "true"; cmdLineOptions["NoStrictAliasing"] = "-fno-strict-aliasing"; }),
                Sharpmake.Options.Option(Options.Compiler.NoStrictAliasing.Disable, () => { options["NoStrictAliasing"] = "false"; cmdLineOptions["NoStrictAliasing"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.UnrollLoops.Enable, () => { options["UnrollLoops"] = "true"; cmdLineOptions["UnrollLoops"] = "-funroll-loops"; }),
                Sharpmake.Options.Option(Options.Compiler.UnrollLoops.Disable, () => { options["UnrollLoops"] = "false"; cmdLineOptions["UnrollLoops"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.LinkTimeOptimization.Enable, () => { options["LinkTimeOptimization"] = "true"; cmdLineOptions["LinkTimeOptimization"] = "-flto"; }),
                Sharpmake.Options.Option(Options.Compiler.LinkTimeOptimization.Disable, () => { options["LinkTimeOptimization"] = "false"; cmdLineOptions["LinkTimeOptimization"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.CheckAnsiCompliance.Enable, () => { options["AnsiCompliance"] = "true"; cmdLineOptions["AnsiCompliance"] = "-ansi"; }),
                Sharpmake.Options.Option(Options.Compiler.CheckAnsiCompliance.Disable, () => { options["AnsiCompliance"] = "false"; cmdLineOptions["AnsiCompliance"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.DefaultCharUnsigned.Enable, () => { options["CharUnsigned"] = "true"; cmdLineOptions["CharUnsigned"] = "-funsigned-char"; }),
                Sharpmake.Options.Option(Options.Compiler.DefaultCharUnsigned.Disable, () => { options["CharUnsigned"] = "false"; cmdLineOptions["CharUnsigned"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.MsExtensions.Enable, () => { options["MsExtensions"] = "true"; cmdLineOptions["MsExtensions"] = "-fms-extensions"; }),
                Sharpmake.Options.Option(Options.Compiler.MsExtensions.Disable, () => { options["MsExtensions"] = "false"; cmdLineOptions["MsExtensions"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Sharpmake.Options.Vc.Compiler.RTTI.Enable, () => { options["RuntimeTypeInfo"] = "true"; cmdLineOptions["RuntimeTypeInfo"] = "-frtti"; }),
                Sharpmake.Options.Option(Sharpmake.Options.Vc.Compiler.RTTI.Disable, () => { options["RuntimeTypeInfo"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["RuntimeTypeInfo"] = "-fno-rtti"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.EditAndContinue.Enable, () => { options["EditAndContinue"] = "true"; cmdLineOptions["EditAndContinue"] = "-Wl --enc"; }),
                Sharpmake.Options.Option(Options.Linker.EditAndContinue.Disable, () => { options["EditAndContinue"] = "false"; cmdLineOptions["EditAndContinue"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.InfoStripping.None, () => { options["InfoStripping"] = "None"; cmdLineOptions["InfoStripping"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Linker.InfoStripping.StripDebug, () => { options["InfoStripping"] = "StripDebug"; cmdLineOptions["InfoStripping"] = "-Wl -S"; }),
                Sharpmake.Options.Option(Options.Linker.InfoStripping.StripSymsAndDebug, () => { options["InfoStripping"] = "StripSymsAndDebug"; cmdLineOptions["InfoStripping"] = "-Wl -s"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.DataStripping.None, () => { options["DataStripping"] = "None"; cmdLineOptions["DataStripping"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Linker.DataStripping.StripFuncs, () => { options["DataStripping"] = "StripFuncs"; cmdLineOptions["DataStripping"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Linker.DataStripping.StripFuncsAndData, () => { options["DataStripping"] = "StripFuncsAndData"; cmdLineOptions["DataStripping"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.DuplicateStripping.Enable, () => { options["DuplicateStripping"] = "true"; cmdLineOptions["DuplicateStripping"] = "-strip-duplicates"; }),
                Sharpmake.Options.Option(Options.Linker.DuplicateStripping.Disable, () => { options["DuplicateStripping"] = "false"; cmdLineOptions["DuplicateStripping"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.Addressing.ASLR, () => { options["Addressing"] = "ASLR"; cmdLineOptions["Addressing"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Linker.Addressing.NonASLR, () => { options["Addressing"] = "NonASLR"; cmdLineOptions["Addressing"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.UseThinArchives.Enable, () => { options["UseThinArchives"] = "true"; cmdLineOptions["UseThinArchives"] = "T"; }),
                Sharpmake.Options.Option(Options.Linker.UseThinArchives.Disable, () => { options["UseThinArchives"] = "false"; cmdLineOptions["UseThinArchives"] = ""; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.WholeArchive.Enable, () => { options["WholeArchive"] = "true"; cmdLineOptions["WholeArchiveBegin"] = "--whole-archive"; cmdLineOptions["WholeArchiveEnd"] = "--no-whole-archive"; }),
                Sharpmake.Options.Option(Options.Linker.WholeArchive.Disable, () => { options["WholeArchive"] = "false"; cmdLineOptions["WholeArchiveBegin"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["WholeArchiveEnd"] = FileGeneratorUtilities.RemoveLineTag; })
                );
            }

            public override void SelectPlatformAdditionalDependenciesOptions(IGenerationContext context)
            {
                // the libs must be prefixed with -l: in the additional dependencies field in VS
                var additionalDependencies = context.Options["AdditionalDependencies"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                context.Options["AdditionalDependencies"] = string.Join(";", additionalDependencies.Select(d => "-l:" + d));
            }

            public override void GenerateProjectCompileVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectConfigurationsCompileTemplate);
            }

            public override void GeneratePlatformSpecificProjectDescription(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                using (generator.Declare("platformName", SimplePlatformString))
                {
                    generator.Write(Vcxproj.Template.Project.ProjectDescriptionStartPlatformConditional);
                    {
                        string applicationTypeRevision;
                        bool hasFastBuildConfig = context.ProjectConfigurations.Any(conf => conf.IsFastBuild);
                        bool hasNonFastBuildConfig = context.ProjectConfigurations.Any(conf => !conf.IsFastBuild);
                        if (hasFastBuildConfig && hasNonFastBuildConfig)
                        {
                            applicationTypeRevision = "1.0";
                        }
                        else
                        {
                            applicationTypeRevision = hasFastBuildConfig ? FileGeneratorUtilities.RemoveLineTag : "1.0";
                        }

                        using (generator.Declare("applicationType", "Linux"))
                        using (generator.Declare("applicationTypeRevision", applicationTypeRevision))
                        using (generator.Declare("targetLinuxPlatform", "Generic"))
                        {
                            generator.Write(_projectDescriptionPlatformSpecific);
                        }
                    }
                    generator.Write(Vcxproj.Template.Project.PropertyGroupEnd);
                }
            }

            public override void GenerateUserConfigurationFile(Project.Configuration conf, IFileGenerator generator)
            {
                generator.Write(_userFileConfigurationGeneralTemplate);
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

            public override void SetupPlatformLibraryOptions(ref string platformLibExtension, ref string platformOutputLibExtension, ref string platformPrefixExtension)
            {
                platformLibExtension = ".a";
                platformOutputLibExtension = "";
                platformPrefixExtension = "-l:";
            }

            protected override string GetProjectLinkSharedVcxprojTemplate()
            {
                return _projectConfigurationsLinkTemplate;
            }

            protected override string GetProjectStaticLinkVcxprojTemplate()
            {
                return _projectConfigurationsStaticLinkTemplate;
            }

            protected override IEnumerable<IncludeWithPrefix> GetPlatformIncludePathsWithPrefixImpl(IGenerationContext context)
            {
                if (!context.Configuration.IsFastBuild || GlobalSettings.SystemPathProvider == null)
                    yield break;

                foreach (string systemIncludePath in GlobalSettings.SystemPathProvider.GetSystemIncludePaths(context.Configuration))
                    yield return new IncludeWithPrefix("-isystem", systemIncludePath);
            }

            #endregion

            #region IFastBuildCompilerSettings implementation
            public IDictionary<DevEnv, string> BinPath { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<IFastBuildCompilerKey, CompilerFamily> CompilerFamily { get; set; } = new Dictionary<IFastBuildCompilerKey, CompilerFamily>();
            public IDictionary<DevEnv, string> LinkerPath { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LinkerExe { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LibrarianExe { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, Strings> ExtraFiles { get; set; } = new Dictionary<DevEnv, Strings>();
            #endregion

            #region IClangPlatformBff implementation

            public override string BffPlatformDefine => "_LINUX";
            public override string CConfigName(Configuration conf)
            {
                return ".linuxConfig";
            }
            public override string CppConfigName(Configuration conf)
            {
                return ".linuxppConfig";
            }

            public void SetupClangOptions(IFileGenerator generator)
            {
                generator.Write(_compilerExtraOptions);
                generator.Write(_compilerOptimizationOptions);
            }

            public override bool AddLibPrefix(Configuration conf)
            {
                return true;
            }

            public override void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration configuration, string fastBuildOutputFile)
            {
                string sharedOption = string.Empty;

                if (configuration.Output == Project.Configuration.OutputType.Dll)
                    sharedOption = " -shared";

                using (fileGenerator.Resolver.NewScopedParameter("sharedOption", sharedOption))
                {
                    fileGenerator.Write(_linkerOptionsTemplate);
                }
            }

            public override void AddCompilerSettings(IDictionary<string, CompilerSettings> masterCompilerSettings, Project.Configuration conf)
            {
                var devEnv = conf.Target.GetFragment<DevEnv>();
                var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.linux);

                var platform = conf.Target.GetFragment<Platform>();
                string compilerName = $"Compiler-{Util.GetSimplePlatformString(platform)}-{devEnv}";
                string CCompilerSettingsName = "C-" + compilerName + "-" + "Linux";
                string CompilerSettingsName = compilerName + "-" + "Linux";

                var projectRootPath = conf.Project.RootPath;
                CompilerSettings compilerSettings = GetMasterCompilerSettings(masterCompilerSettings, CompilerSettingsName, devEnv, projectRootPath, false);
                compilerSettings.PlatformFlags |= Platform.linux;
                CompilerSettings CcompilerSettings = GetMasterCompilerSettings(masterCompilerSettings, CCompilerSettingsName, devEnv, projectRootPath, true);
                CcompilerSettings.PlatformFlags |= Platform.linux;

                SetConfiguration(compilerSettings, CompilerSettingsName, projectRootPath, devEnv, false);
                SetConfiguration(CcompilerSettings, CCompilerSettingsName, projectRootPath, devEnv, true);
            }

            private CompilerSettings GetMasterCompilerSettings(IDictionary<string, CompilerSettings> masterCompilerSettings, string compilerName, DevEnv devEnv, string projectRootPath, bool useCCompiler)
            {
                CompilerSettings compilerSettings;

                if (masterCompilerSettings.ContainsKey(compilerName))
                {
                    compilerSettings = masterCompilerSettings[compilerName];
                }
                else
                {
                    var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.linux);

                    string binPath;
                    if (!fastBuildSettings.BinPath.TryGetValue(devEnv, out binPath))
                        binPath = ClangForWindows.GetWindowsClangExecutablePath();

                    string pathToCompiler = Util.GetCapitalizedPath(Util.PathGetAbsolute(projectRootPath, binPath));

                    Strings extraFiles = new Strings();
                    {
                        Strings userExtraFiles;
                        if (fastBuildSettings.ExtraFiles.TryGetValue(devEnv, out userExtraFiles))
                            extraFiles.AddRange(userExtraFiles);
                    }

                    var compilerFamily = Sharpmake.CompilerFamily.Clang;
                    var compilerFamilyKey = new FastBuildCompilerKey(devEnv);
                    if (!fastBuildSettings.CompilerFamily.TryGetValue(compilerFamilyKey, out compilerFamily))
                        compilerFamily = Sharpmake.CompilerFamily.Clang;

                    string executable = useCCompiler ? @"$ExecutableRootPath$\clang.exe" : @"$ExecutableRootPath$\clang++.exe";

                    compilerSettings = new CompilerSettings(compilerName, compilerFamily, Platform.linux, extraFiles, executable, pathToCompiler, devEnv, new Dictionary<string, CompilerSettings.Configuration>());
                    masterCompilerSettings.Add(compilerName, compilerSettings);
                }

                return compilerSettings;
            }

            private void SetConfiguration(CompilerSettings compilerSettings, string compilerName, string projectRootPath, DevEnv devEnv, bool useCCompiler)
            {
                string configName = useCCompiler ? ".linuxConfig" : ".linuxppConfig";

                IDictionary<string, CompilerSettings.Configuration> configurations = compilerSettings.Configurations;
                if (!configurations.ContainsKey(configName))
                {
                    var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.linux);
                    string binPath = compilerSettings.RootPath;
                    string linkerPath;
                    if (!fastBuildSettings.LinkerPath.TryGetValue(devEnv, out linkerPath))
                        linkerPath = binPath;

                    string linkerExe;
                    if (!fastBuildSettings.LinkerExe.TryGetValue(devEnv, out linkerExe))
                        linkerExe = "ld.lld.exe";

                    string librarianExe;
                    if (!fastBuildSettings.LibrarianExe.TryGetValue(devEnv, out librarianExe))
                        librarianExe = "llvm-ar.exe";

                    configurations.Add(configName,
                        new CompilerSettings.Configuration(
                            Platform.linux,
                            compiler: compilerName,
                            binPath: binPath,
                            linkerPath: Util.GetCapitalizedPath(Util.PathGetAbsolute(projectRootPath, linkerPath)),
                            librarian: @"$LinkerPath$\" + librarianExe,
                            linker: @"$LinkerPath$\" + linkerExe,
                            fastBuildLinkerType: CompilerSettings.LinkerType.GCC // Workaround: set GCC linker type since it will only enable response files
                        )
                    );
                }
            }
            #endregion
        }
    }
}
