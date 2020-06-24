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
    public static partial class Apple
    {
        [PlatformImplementation(SharpmakePlatform,
            typeof(IPlatformDescriptor),
            typeof(IFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IClangPlatformBff),
            typeof(IPlatformVcxproj),
            typeof(Project.Configuration.IConfigurationTasks))]
        public sealed partial class iOsPlatform : BaseApplePlatform, IFastBuildCompilerSettings, IClangPlatformBff, IPlatformVcxproj
        {
            public const string XCodeDevelopperFolder = "/Applications/Xcode.app/Contents/Developer";

            public const Platform SharpmakePlatform = Platform.ios;

            #region IPlatformDescriptor implementation.
            public override string SimplePlatformString => "iOS";
            #endregion

            #region IPlatformVcxproj implementation
            public string ExecutableFileExtension => string.Empty;
            public string PackageFileExtension => ExecutableFileExtension;
            public string SharedLibraryFileExtension => "so";
            public string ProgramDatabaseFileExtension => string.Empty;
            public string StaticLibraryFileExtension => "a";
            public string StaticOutputLibraryFileExtension => StaticLibraryFileExtension;
            public bool ExcludesPrecompiledHeadersFromBuild => false;
            public bool HasUserAccountControlSupport => false;
            public bool HasEditAndContinueDebuggingSupport => false;

            public IEnumerable<string> GetImplicitlyDefinedSymbols(IGenerationContext context)
            {
                yield break;
            }

            public IEnumerable<string> GetLibraryPaths(IGenerationContext context)
            {
                yield break;
            }

            public IEnumerable<string> GetLibraryFiles(IGenerationContext context)
            {
                yield break;
            }

            public IEnumerable<string> GetPlatformLibraryFiles(IGenerationContext context)
            {
                yield break;
            }

            // IncludePaths should contain only the project's own includes, and PlatformIncludePaths
            // are the platform's include paths.
            public IEnumerable<string> GetIncludePaths(IGenerationContext context)
            {
                return GetIncludePathsImpl(context);
            }
            public IEnumerable<string> GetPlatformIncludePaths(IGenerationContext context)
            {
                return GetPlatformIncludePathsWithPrefixImpl(context).Select(x => x.Path);
            }
            public IEnumerable<IncludeWithPrefix> GetPlatformIncludePathsWithPrefix(IGenerationContext context)
            {
                return GetPlatformIncludePathsWithPrefixImpl(context);
            }
            public IEnumerable<string> GetResourceIncludePaths(IGenerationContext context)
            {
                return GetResourceIncludePathsImpl(context);
            }
            public IEnumerable<string> GetCxUsingPath(IGenerationContext context)
            {
                yield break;
            }
            public IEnumerable<VariableAssignment> GetEnvironmentVariables(IGenerationContext context)
            {
                yield break;
            }
            public string GetOutputFileNamePrefix(IGenerationContext context, Project.Configuration.OutputType outputType)
            {
                if (outputType != Project.Configuration.OutputType.Exe)
                    return "lib";
                return string.Empty;
            }

            public void SetupDeleteExtensionsOnCleanOptions(IGenerationContext context)
            {
            }

            public void SetupSdkOptions(IGenerationContext context)
            {
                var conf = context.Configuration;
                var options = context.Options;
            }
            public void SetupPlatformToolsetOptions(IGenerationContext context)
            {
            }
            public void SetupPlatformTargetOptions(IGenerationContext context)
            {
            }
            public void SelectCompilerOptions(IGenerationContext context)
            {
                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                context.SelectOption
                (
                    Options.Option(Options.XCode.Compiler.OptimizationLevel.Disable, () => { cmdLineOptions["OptimizationLevel"] = "-O0"; }),
                    Options.Option(Options.XCode.Compiler.OptimizationLevel.Fast, () => { cmdLineOptions["OptimizationLevel"] = "-O1"; }),
                    Options.Option(Options.XCode.Compiler.OptimizationLevel.Faster, () => { cmdLineOptions["OptimizationLevel"] = "-O2"; }),
                    Options.Option(Options.XCode.Compiler.OptimizationLevel.Fastest, () => { cmdLineOptions["OptimizationLevel"] = "-O3"; }),
                    Options.Option(Options.XCode.Compiler.OptimizationLevel.Smallest, () => { cmdLineOptions["OptimizationLevel"] = "-Os"; }),
                    Options.Option(Options.XCode.Compiler.OptimizationLevel.Aggressive, () => { cmdLineOptions["OptimizationLevel"] = "-Ofast"; })
                );

                context.SelectOption
                (
                    Options.Option(Linux.Options.Linker.UseThinArchives.Enable, () => { options["UseThinArchives"] = "true"; cmdLineOptions["UseThinArchives"] = "T"; }),
                    Options.Option(Linux.Options.Linker.UseThinArchives.Disable, () => { options["UseThinArchives"] = "false"; cmdLineOptions["UseThinArchives"] = ""; })
                );

                context.SelectOption(
                    Options.Option(Options.XCode.Compiler.LibraryStandard.CppStandard, () => { options["LibraryStandard"] = "libstdc++"; cmdLineOptions["LibraryStandard"] = "-stdlib=libstdc++"; }),
                    Options.Option(Options.XCode.Compiler.LibraryStandard.LibCxx, () => { options["LibraryStandard"] = "libc++"; cmdLineOptions["LibraryStandard"] = "-stdlib=libc++"; })
                );

                // Sysroot
                options["SDKRoot"] = "iphoneos";
                cmdLineOptions["SDKRoot"] = $"-isysroot {XCodeDevelopperFolder}/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk";
                Options.XCode.Compiler.SDKRoot customSdkRoot = Options.GetObject<Options.XCode.Compiler.SDKRoot>(conf);
                if (customSdkRoot != null)
                {
                    options["SDKRoot"] = customSdkRoot.Value;
                    cmdLineOptions["SDKRoot"] = $"-isysroot {customSdkRoot.Value}";
                }

                // Target
                options["IPhoneOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                cmdLineOptions["IPhoneOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                Options.XCode.Compiler.IPhoneOSDeploymentTarget iosDeploymentTarget = Options.GetObject<Options.XCode.Compiler.IPhoneOSDeploymentTarget>(conf);
                if (iosDeploymentTarget != null)
                {
                    options["IPhoneOSDeploymentTarget"] = iosDeploymentTarget.MinimumVersion;
                    cmdLineOptions["IPhoneOSDeploymentTarget"] = $"-target arm64-apple-ios{iosDeploymentTarget.MinimumVersion}";
                }
            }
            public void SelectLinkerOptions(IGenerationContext context)
            {
                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;
            }
            public void SelectPlatformAdditionalDependenciesOptions(IGenerationContext context)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void SelectApplicationFormatOptions(IGenerationContext context)
            {
            }
            public void SelectBuildType(IGenerationContext context)
            {
            }
            public bool HasPrecomp(IGenerationContext context)
            {
                return !string.IsNullOrEmpty(context.Configuration.PrecompHeader);
            }
            public void GenerateSdkVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateMakefileConfigurationVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateProjectCompileVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateProjectLinkVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateProjectMasmVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateUserConfigurationFile(Project.Configuration conf, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateRunFromPcDeployment(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GeneratePlatformSpecificProjectDescription(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateProjectPlatformSdkDirectoryDescription(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GeneratePostDefaultPropsImport(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateProjectConfigurationGeneral(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateProjectConfigurationGeneral2(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateProjectConfigurationFastBuildMakeFile(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateProjectConfigurationCustomMakeFile(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GenerateProjectPlatformImportSheet(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GeneratePlatformResourceFileList(IVcxprojGenerationContext context, IFileGenerator generator, Strings alreadyWrittenPriFiles, IList<Vcxproj.ProjectFile> resourceFiles, IList<Vcxproj.ProjectFile> imageResourceFiles)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }
            public void GeneratePlatformReferences(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                throw new NotImplementedException("iOS Platform should not be called by a Vcxproj generator");
            }

            // type -> files
            public IEnumerable<Tuple<string, List<Vcxproj.ProjectFile>>> GetPlatformFileLists(IVcxprojGenerationContext context)
            {
                yield break;
            }

            // TODO: Refactor this.
            public void SetupPlatformLibraryOptions(ref string platformLibExtension, ref string platformOutputLibExtension, ref string platformPrefixExtension)
            {
                platformLibExtension = ".a";
                platformOutputLibExtension = ".a";
                platformPrefixExtension = string.Empty;
            }

            private IEnumerable<string> GetIncludePathsImpl(IGenerationContext context)
            {
                var conf = context.Configuration;

                var includePaths = new OrderableStrings();
                includePaths.AddRange(conf.IncludePrivatePaths);
                includePaths.AddRange(conf.IncludePaths);
                includePaths.AddRange(conf.DependenciesIncludePaths);

                includePaths.Sort();
                return includePaths;
            }
            private IEnumerable<IncludeWithPrefix> GetPlatformIncludePathsWithPrefixImpl(IGenerationContext context)
            {
                yield break;
            }
            private IEnumerable<string> GetResourceIncludePathsImpl(IGenerationContext context)
            {
                Project.Configuration conf = context.Configuration;

                var resourceIncludePaths = new OrderableStrings();
                resourceIncludePaths.AddRange(conf.ResourceIncludePrivatePaths);
                resourceIncludePaths.AddRange(conf.ResourceIncludePaths);
                resourceIncludePaths.AddRange(conf.DependenciesResourceIncludePaths);

                return resourceIncludePaths;
            }
            #endregion // IPlatformVcxproj implementation

            #region IFastBuildCompilerSettings implementation
            public IDictionary<DevEnv, string> BinPath { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<IFastBuildCompilerKey, CompilerFamily> CompilerFamily { get; set; } = new Dictionary<IFastBuildCompilerKey, CompilerFamily>();
            public IDictionary<DevEnv, string> LinkerPath { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LinkerExe { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LibrarianExe { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, Strings> ExtraFiles { get; set; } = new Dictionary<DevEnv, Strings>();
            #endregion

            #region IClangPlatformBff implementation

            public string BffPlatformDefine => "_IOS";
            public string CConfigName(Configuration conf)
            {
                return ".iosConfig";
            }
            public string CppConfigName(Configuration conf)
            {
                return ".iosppConfig";
            }

            public void SetupClangOptions(IFileGenerator generator)
            {
                generator.Write(_compilerExtraOptions);
                generator.Write(_compilerOptimizationOptions);
            }

            public bool AddLibPrefix(Configuration conf)
            {
                return true;
            }

            [Obsolete("Use " + nameof(SetupExtraLinkerSettings) + " and pass the conf")]
            public void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration.OutputType outputType, string fastBuildOutputFile)
            {
                fileGenerator.Write(_linkerOptionsTemplate);
            }

            public void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration configuration, string fastBuildOutputFile)
            {
                fileGenerator.Write(_linkerOptionsTemplate);
            }

            public IEnumerable<Project.Configuration.BuildStepBase> GetExtraPostBuildEvents(Project.Configuration configuration, string fastBuildOutputFile)
            {
                return Enumerable.Empty<Project.Configuration.BuildStepBase>();
            }

            public string GetOutputFilename(Project.Configuration.OutputType outputType, string fastBuildOutputFile) => fastBuildOutputFile;

            public void AddCompilerSettings(IDictionary<string, CompilerSettings> masterCompilerSettings, Project.Configuration conf)
            {
                var devEnv = conf.Target.GetFragment<DevEnv>();
                var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.ios);

                var platform = conf.Target.GetFragment<Platform>();
                string compilerName = $"Compiler-{Util.GetSimplePlatformString(platform)}-{devEnv}";
                string CCompilerSettingsName = "C-" + compilerName + "-" + "iOS";
                string CompilerSettingsName = compilerName + "-" + "iOS";

                var projectRootPath = conf.Project.RootPath;
                CompilerSettings compilerSettings = GetMasterCompilerSettings(masterCompilerSettings, CompilerSettingsName, devEnv, projectRootPath, false);
                compilerSettings.PlatformFlags |= Platform.ios;
                CompilerSettings CcompilerSettings = GetMasterCompilerSettings(masterCompilerSettings, CCompilerSettingsName, devEnv, projectRootPath, true);
                CcompilerSettings.PlatformFlags |= Platform.ios;

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
                    var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.ios);

                    string binPath;
                    if (!fastBuildSettings.BinPath.TryGetValue(devEnv, out binPath))
                        binPath = $"{XCodeDevelopperFolder}/Toolchains/XcodeDefault.xctoolchain/usr/bin";

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

                    string executable = useCCompiler ? @"$ExecutableRootPath$\clang" : @"$ExecutableRootPath$\clang++";

                    compilerSettings = new CompilerSettings(compilerName, compilerFamily, Platform.ios, extraFiles, executable, pathToCompiler, devEnv, new Dictionary<string, CompilerSettings.Configuration>());
                    masterCompilerSettings.Add(compilerName, compilerSettings);
                }

                return compilerSettings;
            }

            private void SetConfiguration(CompilerSettings compilerSettings, string compilerName, string projectRootPath, DevEnv devEnv, bool useCCompiler)
            {
                string configName = useCCompiler ? CConfigName(null) : CppConfigName(null);

                IDictionary<string, CompilerSettings.Configuration> configurations = compilerSettings.Configurations;
                if (!configurations.ContainsKey(configName))
                {
                    var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.ios);
                    string binPath = compilerSettings.RootPath;
                    string linkerPath;
                    if (!fastBuildSettings.LinkerPath.TryGetValue(devEnv, out linkerPath))
                        linkerPath = binPath;

                    string linkerExe;
                    if (!fastBuildSettings.LinkerExe.TryGetValue(devEnv, out linkerExe))
                        linkerExe = "ld";

                    string librarianExe;
                    if (!fastBuildSettings.LibrarianExe.TryGetValue(devEnv, out librarianExe))
                        librarianExe = "ar";

                    configurations.Add(configName,
                        new CompilerSettings.Configuration(
                            Platform.ios,
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
