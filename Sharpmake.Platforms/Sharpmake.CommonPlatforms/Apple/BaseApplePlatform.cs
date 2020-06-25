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
    public abstract partial class BaseApplePlatform
        : IPlatformDescriptor
        , Project.Configuration.IConfigurationTasks
        , IFastBuildCompilerSettings
        , IClangPlatformBff
        , IPlatformVcxproj // TODO: this is really sad, nuke it
    {
        protected const string XCodeDeveloperFolder = "/Applications/Xcode.app/Contents/Developer";

        public abstract Platform SharpmakePlatform { get; }

        #region IPlatformDescriptor
        public abstract string SimplePlatformString { get; }
        public bool IsMicrosoftPlatform => false;
        public bool IsUsingClang => true;
        public bool HasDotNetSupport => false; // maybe? (.NET Core)
        public bool HasSharedLibrarySupport => true;
        public bool HasPrecompiledHeaderSupport => true;

        public bool IsPcPlatform => false; // LCTODO: ditch since it's obsolete

        public EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] variables)
        {
            return new EnvironmentVariableResolver(variables);
        }

        public string GetPlatformString(ITarget target) => SimplePlatformString;
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

        public abstract string BffPlatformDefine { get; }
        public abstract string CConfigName(Configuration conf);
        public abstract string CppConfigName(Configuration conf);

        public void SetupClangOptions(IFileGenerator generator)
        {
            WriteCompilerExtraOptionsGeneral(generator);
            generator.Write(_compilerOptimizationOptions);
        }

        protected virtual void WriteCompilerExtraOptionsGeneral(IFileGenerator generator)
        {
            generator.Write(_compilerExtraOptionsGeneral);
        }

        public bool AddLibPrefix(Configuration conf) => true;

        [Obsolete("Use " + nameof(SetupExtraLinkerSettings) + " and pass the conf", error: true)]
        public void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration.OutputType outputType, string fastBuildOutputFile)
        {
        }

        public void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration configuration, string fastBuildOutputFile)
        {
            string outputTypeArgument;
            switch (configuration.Output)
            {
                case Project.Configuration.OutputType.Dll:
                    outputTypeArgument = " -dylib";
                    break;
                case Project.Configuration.OutputType.Exe:
                    outputTypeArgument = " -execute";
                    break;
                case Project.Configuration.OutputType.Lib:
                case Project.Configuration.OutputType.Utility:
                case Project.Configuration.OutputType.DotNetConsoleApp:
                case Project.Configuration.OutputType.DotNetClassLibrary:
                case Project.Configuration.OutputType.DotNetWindowsApp:
                case Project.Configuration.OutputType.None:
                    outputTypeArgument = "";
                    break;
                default:
                    throw new Error($"{configuration.Output} is not supported as an output by the linker");
            }

            using (fileGenerator.Resolver.NewScopedParameter("outputTypeArgument", outputTypeArgument))
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
            var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(SharpmakePlatform);

            var platform = conf.Target.GetFragment<Platform>();
            string compilerName = $"Compiler-{Util.GetSimplePlatformString(platform)}-{devEnv}";
            string CCompilerSettingsName = "C-" + compilerName;
            string CompilerSettingsName = compilerName;

            var projectRootPath = conf.Project.RootPath;
            CompilerSettings compilerSettings = GetMasterCompilerSettings(masterCompilerSettings, CompilerSettingsName, devEnv, projectRootPath, false);
            compilerSettings.PlatformFlags |= SharpmakePlatform;
            CompilerSettings CcompilerSettings = GetMasterCompilerSettings(masterCompilerSettings, CCompilerSettingsName, devEnv, projectRootPath, true);
            CcompilerSettings.PlatformFlags |= SharpmakePlatform;

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
                var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(SharpmakePlatform);

                string binPath;
                if (!fastBuildSettings.BinPath.TryGetValue(devEnv, out binPath))
                    binPath = $"{XCodeDeveloperFolder}/Toolchains/XcodeDefault.xctoolchain/usr/bin";

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

                compilerSettings = new CompilerSettings(compilerName, compilerFamily, SharpmakePlatform, extraFiles, executable, pathToCompiler, devEnv, new Dictionary<string, CompilerSettings.Configuration>());
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
                var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(SharpmakePlatform);
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
                        SharpmakePlatform,
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


        #region IConfigurationTasks
        public string GetDefaultOutputExtension(Project.Configuration.OutputType outputType)
        {
            switch (outputType)
            {
                // Using the Unix extensions since Darwin is a Unix implementation and the
                // executables Mac users interact with are actually bundles. If this causes
                // issues, see if using .app for executables and .dylib/.framework for
                // libraries work better. iOS is Darwin/Cocoa so assuming that the same goes
                // for it.
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.IosApp:
                case Project.Configuration.OutputType.IosTestBundle:
                    return string.Empty;
                case Project.Configuration.OutputType.Lib:
                    return "a";
                case Project.Configuration.OutputType.Dll:
                    return "dylib";

                // .NET remains the same on all platforms. (Mono loads .exe and .dll regardless
                // of platforms, and I assume the same about .NET Core.)
                case Project.Configuration.OutputType.DotNetConsoleApp:
                case Project.Configuration.OutputType.DotNetWindowsApp:
                    return "exe";
                case Project.Configuration.OutputType.DotNetClassLibrary:
                    return "dll";

                case Project.Configuration.OutputType.None:
                    return string.Empty;
                default:
                    return outputType.ToString().ToLower();
            }
        }

        public IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
        {
            yield break;
        }

        public void SetupDynamicLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            // There's no implib on apple platforms, the dynlib does both
            if (dependency.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Export &&
                !(configuration.IsFastBuild && !dependency.IsFastBuild))
            {
                if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                    configuration.AddDependencyBuiltTargetLibraryPath(dependency.TargetPath, dependency.TargetLibraryPathOrderNumber);
                if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                    configuration.AddDependencyBuiltTargetLibraryFile(dependency.TargetFileFullName, dependency.TargetFileOrderNumber);
            }
            else
            {
                if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                    configuration.DependenciesOtherLibraryPaths.Add(dependency.TargetPath, dependency.TargetLibraryPathOrderNumber);
                if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                    configuration.DependenciesOtherLibraryFiles.Add(dependency.TargetFileFullName, dependency.TargetFileOrderNumber);
            }
        }

        public void SetupStaticLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
        }

        #endregion

        #region IPlatformVcxproj implementation
        public string ExecutableFileExtension => string.Empty;
        public string PackageFileExtension => ExecutableFileExtension;
        public string SharedLibraryFileExtension => "dylib";
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
            var cmdLineOptions = context.CommandLineOptions;
            string libStd = cmdLineOptions["LibraryStandard"];
            if (!libStd.StartsWith("-stdlib=lib"))
                throw new Error("Stdlib argument doesn't match the expected format");

            yield return "-l" + libStd.Substring(11);
            yield return "-lSystem";
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

        public virtual void SelectCompilerOptions(IGenerationContext context)
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

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.LibraryStandard.CppStandard, () => { options["LibraryStandard"] = "libstdc++"; cmdLineOptions["LibraryStandard"] = "-stdlib=libstdc++"; }),
                Options.Option(Options.XCode.Compiler.LibraryStandard.LibCxx, () => { options["LibraryStandard"] = "libc++"; cmdLineOptions["LibraryStandard"] = "-stdlib=libc++"; })
            );
        }

        public void SelectLinkerOptions(IGenerationContext context)
        {
            var options = context.Options;
            var cmdLineOptions = context.CommandLineOptions;
            var conf = context.Configuration;

            if (context.Options["GenerateMapFile"] == "true")
            {
                string mapFileArg = context.CommandLineOptions["GenerateMapFile"];
                if (!mapFileArg.StartsWith("-Wl,-Map=", StringComparison.Ordinal))
                    throw new Error("Map file argument was supposed to start with -Wl,-Map= but it changed! Please update this module!");
                // since we directly invoke ld and not clang as a linker, we need to remove -Wl,-Map= and pass -map
                context.CommandLineOptions["GenerateMapFile"] = "-map " + context.CommandLineOptions["GenerateMapFile"].Substring(9);
            }

            // TODO: implement me
            cmdLineOptions["UseThinArchives"] = "";
        }

        public void SelectPlatformAdditionalDependenciesOptions(IGenerationContext context)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
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
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateMakefileConfigurationVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateProjectCompileVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateProjectLinkVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateProjectMasmVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateUserConfigurationFile(Project.Configuration conf, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateRunFromPcDeployment(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GeneratePlatformSpecificProjectDescription(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateProjectPlatformSdkDirectoryDescription(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GeneratePostDefaultPropsImport(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateProjectConfigurationGeneral(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateProjectConfigurationGeneral2(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateProjectConfigurationFastBuildMakeFile(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateProjectConfigurationCustomMakeFile(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GenerateProjectPlatformImportSheet(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GeneratePlatformResourceFileList(IVcxprojGenerationContext context, IFileGenerator generator, Strings alreadyWrittenPriFiles, IList<Vcxproj.ProjectFile> resourceFiles, IList<Vcxproj.ProjectFile> imageResourceFiles)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
        }

        public void GeneratePlatformReferences(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
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

    }
}
