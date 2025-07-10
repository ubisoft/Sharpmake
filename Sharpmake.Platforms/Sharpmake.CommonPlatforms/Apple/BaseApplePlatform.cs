// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sharpmake.Generators;
using Sharpmake.Generators.Apple;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public abstract partial class BaseApplePlatform
        : IPlatformDescriptor
        , Project.Configuration.IConfigurationTasks
        , IFastBuildCompilerSettings
        , IApplePlatformBff
        , IPlatformVcxproj // TODO: this is really sad, nuke it
    {
        public abstract Platform SharpmakePlatform { get; }

        #region IPlatformDescriptor
        public abstract string SimplePlatformString { get; }
        // The toolchain-specific platform string is not needed with Xcode so we just use the
        // simple platform name in case some of the base/common generation code needs it
        public string GetToolchainPlatformString(ITarget target) => SimplePlatformString;
        public bool IsMicrosoftPlatform => false;
        public bool IsUsingClang => true;
        public bool IsLinkerInvokedViaCompiler
        {
            get
            {
                bool isLinkerInvokedViaCompiler = false;
                var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(SharpmakePlatform);
                if (fastBuildSettings.LinkerInvokedViaCompiler.TryGetValue(DevEnv.xcode, out bool isLinkerInvokedViaCompilerOverride))
                    isLinkerInvokedViaCompiler = isLinkerInvokedViaCompilerOverride;
                return isLinkerInvokedViaCompiler;
            }
        }

        public bool HasDotNetSupport => false; // maybe? (.NET Core)
        public bool HasSharedLibrarySupport => true;
        public bool HasPrecompiledHeaderSupport => true;

        public bool IsPcPlatform => false; // LCTODO: ditch since it's obsolete

        public EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] variables)
        {
            return new EnvironmentVariableResolver(variables);
        }
        #endregion

        #region IFastBuildCompilerSettings implementation
        public IDictionary<DevEnv, string> BinPath { get; set; } = new Dictionary<DevEnv, string>();
        public IDictionary<IFastBuildCompilerKey, CompilerFamily> CompilerFamily { get; set; } = new Dictionary<IFastBuildCompilerKey, CompilerFamily>();
        public IDictionary<DevEnv, string> LinkerPath { get; set; } = new Dictionary<DevEnv, string>();
        public IDictionary<DevEnv, string> LinkerExe { get; set; } = new Dictionary<DevEnv, string>();
        public IDictionary<DevEnv, bool> LinkerInvokedViaCompiler { get; set; } = new Dictionary<DevEnv, bool>();
        public IDictionary<DevEnv, string> LibrarianExe { get; set; } = new Dictionary<DevEnv, string>();
        public IDictionary<DevEnv, Strings> ExtraFiles { get; set; } = new Dictionary<DevEnv, Strings>();
        #endregion


        #region IApplePlatformBff implementation

        public abstract string BffPlatformDefine { get; }
        public abstract string CConfigName(Configuration conf);
        public abstract string CppConfigName(Configuration conf);
        public abstract string SwiftConfigName(Configuration conf);

        public void SetupClangOptions(IFileGenerator generator)
        {
            generator.Write(_compilerExtraOptionsGeneral);
            generator.Write(_compilerExtraOptionsAdditional);
            generator.Write(_compilerOptimizationOptions);
        }

        public bool IsSwiftSupported()
        {
            return SwiftForApple.Settings.SwiftSupportEnabled;
        }

        public void SetupSwiftOptions(IFileGenerator generator)
        {
            if (!IsSwiftSupported())
                throw new Error("Swift is not supported");

            generator.Write(_swiftCompilerExtraOptionsGeneral);
            generator.Write(_swiftCompilerExtraOptionsAdditional);
            generator.Write(_swiftCompilerOptimizationOptions);
        }

        public virtual void SelectPreprocessorDefinitionsBff(IBffGenerationContext context)
        {
            var platformDescriptor = PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform);
            string platformDefineSwitch = platformDescriptor.IsUsingClang ? "-D" : "/D";

            // concat defines, don't add options.Defines since they are automatically added by VS
            var defines = new Strings();
            defines.AddRange(context.Options.ExplicitDefines);
            defines.AddRange(context.Configuration.Defines);

            if (defines.Count > 0)
            {
                var fastBuildDefines = new List<string>();

                foreach (string define in defines.SortedValues)
                {
                    if (!string.IsNullOrWhiteSpace(define))
                    {
                        if (Util.GetExecutingPlatform() == Platform.win64)
                            fastBuildDefines.Add(string.Format(@"{0}{1}{2}{1}", platformDefineSwitch, Util.DoubleQuotes, define.Replace(Util.DoubleQuotes, Util.EscapedDoubleQuotes)));
                        else
                            fastBuildDefines.Add(string.Concat(platformDefineSwitch, define));
                    }
                }
                context.CommandLineOptions["PreprocessorDefinitions"] = string.Join($"'{Environment.NewLine}            + ' ", fastBuildDefines);
            }
            else
            {
                context.CommandLineOptions["PreprocessorDefinitions"] = FileGeneratorUtilities.RemoveLineTag;
            }

            Strings resourceDefines = Options.GetStrings<Options.Vc.ResourceCompiler.PreprocessorDefinitions>(context.Configuration);
            if (resourceDefines.Any())
            {
                var fastBuildDefines = new List<string>();

                foreach (string resourceDefine in resourceDefines.SortedValues)
                {
                    if (!string.IsNullOrWhiteSpace(resourceDefine))
                    {
                        if (Util.GetExecutingPlatform() == Platform.win64)
                            fastBuildDefines.Add(string.Format(@"""{0}{1}""", platformDefineSwitch, resourceDefine.Replace(Util.DoubleQuotes, Util.EscapedDoubleQuotes)));
                        else
                            fastBuildDefines.Add(string.Concat(platformDefineSwitch, resourceDefine));
                    }
                }
                context.CommandLineOptions["ResourcePreprocessorDefinitions"] = string.Join($"'{Environment.NewLine}                                    + ' ", fastBuildDefines);
            }
            else
            {
                context.CommandLineOptions["ResourcePreprocessorDefinitions"] = FileGeneratorUtilities.RemoveLineTag;
            }
        }

        public virtual void SelectAdditionalCompilerOptionsBff(IBffGenerationContext context)
        {
            if (IsSwiftSupported())
            {
                Options.XCode.Compiler.SwiftAdditionalCompilerOptions additionalSwiftOptions = Options.GetObject<Options.XCode.Compiler.SwiftAdditionalCompilerOptions>(context.Configuration);
                if (additionalSwiftOptions != null && additionalSwiftOptions.Any())
                {
                    additionalSwiftOptions.Sort();
                    context.CommandLineOptions["SwiftAdditionalCompilerOptions"] = string.Join($"'{Environment.NewLine}            + ' ", additionalSwiftOptions);
                }
                else
                {
                    context.CommandLineOptions["SwiftAdditionalCompilerOptions"] = FileGeneratorUtilities.RemoveLineTag;
                }
            }
        }

        public void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration configuration, string fastBuildOutputFile)
        {
            string outputTypeArgument;
            switch (configuration.Output)
            {
                case Project.Configuration.OutputType.Dll:
                    outputTypeArgument = " -dylib";
                    break;
                case Project.Configuration.OutputType.AppleApp:
                case Project.Configuration.OutputType.Exe:
                    outputTypeArgument = IsLinkerInvokedViaCompiler ? "" : " -execute";
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

        public IEnumerable<Project.Configuration.BuildStepExecutable> GetExtraStampEvents(Project.Configuration configuration, string fastBuildOutputFile)
        {
            if (FastBuildSettings.FastBuildSupportLinkerStampList)
            {
                foreach (var step in GetStripDebugSymbolsSteps(configuration, fastBuildOutputFile, asStampSteps: true))
                    yield return step;
            }
        }

        public IEnumerable<Project.Configuration.BuildStepBase> GetExtraPostBuildEvents(Project.Configuration configuration, string fastBuildOutputFile)
        {
            if (!FastBuildSettings.FastBuildSupportLinkerStampList)
            {
                foreach (var step in GetStripDebugSymbolsSteps(configuration, fastBuildOutputFile, asStampSteps: false))
                    yield return step;
            }
        }

        private IEnumerable<Project.Configuration.BuildStepExecutable> GetStripDebugSymbolsSteps(Project.Configuration configuration, string fastBuildOutputFile, bool asStampSteps)
        {
            if (Util.GetExecutingPlatform() == Platform.mac)
            {
                // Note: We only generate dsym for applications and bundles
                if (configuration.IsFastBuild && 
                   (configuration.Output == Project.Configuration.OutputType.AppleApp ||
                   configuration.Output == Project.Configuration.OutputType.Exe ||
                   configuration.Output == Project.Configuration.OutputType.AppleBundle || 
                   configuration.Output == Project.Configuration.OutputType.Dll
                   ))
                {
                    var debugFormat = Options.GetObject<Options.XCode.Compiler.DebugInformationFormat>(configuration);
                    if (debugFormat == Options.XCode.Compiler.DebugInformationFormat.DwarfWithDSym)
                    {
                        string outputPath = Path.Combine(configuration.TargetPath, configuration.TargetFileFullNameWithExtension + ".dSYM");
                        string dsymutilSentinelFile = Path.Combine(configuration.IntermediatePath, configuration.TargetFileName + ".dsymdone");
                        yield return new Project.Configuration.BuildStepExecutable(
                            "/usr/bin/dsymutil",
                            asStampSteps ? string.Empty : fastBuildOutputFile,
                            asStampSteps ? string.Empty : dsymutilSentinelFile,
                            $"{fastBuildOutputFile} -o {outputPath}",
                            useStdOutAsOutput: true);

                        // Stripping
                        if (Options.GetObject<Options.XCode.Linker.StripLinkedProduct>(configuration) == Options.XCode.Linker.StripLinkedProduct.Enable)
                        {
                            List<string> stripOptionList = new List<string>();
                            switch (Options.GetObject<Options.XCode.Linker.StripStyle>(configuration))
                            {
                                case Options.XCode.Linker.StripStyle.AllSymbols:
                                    stripOptionList.Add("-s");
                                    break;
                                case Options.XCode.Linker.StripStyle.NonGlobalSymbols:
                                    stripOptionList.Add("-x");
                                    break;
                                case Options.XCode.Linker.StripStyle.DebuggingSymbolsOnly:
                                    stripOptionList.Add("-S");
                                    break;
                            }
                            if (Options.GetObject<Options.XCode.Linker.StripSwiftSymbols>(configuration) == Options.XCode.Linker.StripSwiftSymbols.Enable)
                                stripOptionList.Add("-T");

                            var additionalStripFlags = Options.GetObject<Options.XCode.Linker.AdditionalStripFlags>(configuration);
                            if (additionalStripFlags != null)
                                stripOptionList.Add(XCodeUtil.ResolveProjectVariable(configuration.Project, additionalStripFlags.Value));

                            string stripOptions = string.Join(" ", stripOptionList);

                            string strippedSentinelFile = Path.Combine(configuration.IntermediatePath, configuration.TargetFileName + ".stripped");
                            yield return new Project.Configuration.BuildStepExecutable(
                                "/usr/bin/strip",
                                asStampSteps ? string.Empty : dsymutilSentinelFile,
                                asStampSteps ? string.Empty : strippedSentinelFile,
                                $"{stripOptions} {fastBuildOutputFile}",
                                useStdOutAsOutput: true
                            );
                        }
                    }
                }
            }
        }

        public string GetOutputFilename(Project.Configuration.OutputType outputType, string fastBuildOutputFile) => fastBuildOutputFile;

        public enum CompilerFlavor
        {
            C, // for C and Objective-C
            Cpp, // for C++ and Objective-C++
            Swift
        }
        public void AddCompilerSettings(IDictionary<string, CompilerSettings> masterCompilerSettings, Project.Configuration conf)
        {
            var devEnv = conf.Target.GetFragment<DevEnv>();
            var platform = conf.Target.GetFragment<Platform>();
            string compilerName = $"Compiler-{Util.GetToolchainPlatformString(platform, conf.Target)}-{devEnv}";
            string CCompilerSettingsName = "C-" + compilerName;
            string CompilerSettingsName = compilerName;

            var projectRootPath = conf.Project.RootPath;
            CompilerSettings compilerSettings = GetMasterCompilerSettings(masterCompilerSettings, CompilerSettingsName, devEnv, projectRootPath, CompilerFlavor.Cpp);
            compilerSettings.PlatformFlags |= SharpmakePlatform;
            CompilerSettings CcompilerSettings = GetMasterCompilerSettings(masterCompilerSettings, CCompilerSettingsName, devEnv, projectRootPath, CompilerFlavor.C);
            CcompilerSettings.PlatformFlags |= SharpmakePlatform;

            SetConfiguration(compilerSettings, CompilerSettingsName, projectRootPath, devEnv, CompilerFlavor.Cpp);
            SetConfiguration(CcompilerSettings, CCompilerSettingsName, projectRootPath, devEnv, CompilerFlavor.C);

            if (IsSwiftSupported())
            {
                string SwiftCompilerSettingsName = "Swift-" + compilerName;
                CompilerSettings SwiftcompilerSettings = GetMasterCompilerSettings(masterCompilerSettings, SwiftCompilerSettingsName, devEnv, projectRootPath, CompilerFlavor.Swift);
                SwiftcompilerSettings.PlatformFlags |= SharpmakePlatform;
                SetConfiguration(SwiftcompilerSettings, SwiftCompilerSettingsName, projectRootPath, devEnv, CompilerFlavor.Swift);
            }
        }

        private CompilerSettings GetMasterCompilerSettings(IDictionary<string, CompilerSettings> masterCompilerSettings, string compilerName, DevEnv devEnv, string projectRootPath, CompilerFlavor compilerFlavor)
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
                switch (compilerFlavor)
                {
                    case CompilerFlavor.C:
                    case CompilerFlavor.Cpp:
                        if (fastBuildSettings.BinPath.TryGetValue(devEnv, out string binPathOverride))
                            binPath = binPathOverride;
                        else
                            binPath = ClangForApple.GetClangExecutablePath();
                        break;

                    case CompilerFlavor.Swift:
                            binPath = SwiftForApple.GetSwiftExecutablePath();
                        break;
                    default:
                        throw new NotImplementedException();
                }

                string pathToCompiler = Util.GetCapitalizedPath(Util.PathGetAbsolute(projectRootPath, binPath));

                Strings extraFiles = new Strings();

                CompilerFamily compilerFamily;

                if (compilerFlavor == CompilerFlavor.Swift)
                {
                    compilerFamily = Sharpmake.CompilerFamily.Custom;
                }
                else
                {
                    Strings userExtraFiles;
                    if (fastBuildSettings.ExtraFiles.TryGetValue(devEnv, out userExtraFiles))
                        extraFiles.AddRange(userExtraFiles);

                    var compilerFamilyKey = new FastBuildCompilerKey(devEnv);
                    if (!fastBuildSettings.CompilerFamily.TryGetValue(compilerFamilyKey, out compilerFamily))
                        compilerFamily = Sharpmake.CompilerFamily.Clang;
                }

                string exeExtension = Util.GetExecutingPlatform() == Platform.win64 ? ".exe" : "";
                string executableName;
                switch (compilerFlavor)
                {
                    case CompilerFlavor.C:
                        executableName = "clang";
                        break;
                    case CompilerFlavor.Cpp:
                        executableName = "clang++";
                        break;
                    case CompilerFlavor.Swift:
                        executableName = "swiftc";
                        break;
                    default:
                        throw new NotImplementedException();
                }

                string executable = Path.Combine(@"$ExecutableRootPath$", executableName + exeExtension);

                compilerSettings = new CompilerSettings(compilerName, compilerFamily, SharpmakePlatform, extraFiles, executable, pathToCompiler, devEnv, new Dictionary<string, CompilerSettings.Configuration>());
                masterCompilerSettings.Add(compilerName, compilerSettings);
            }

            return compilerSettings;
        }

        private void SetConfiguration(CompilerSettings compilerSettings, string compilerName, string projectRootPath, DevEnv devEnv, CompilerFlavor compilerFlavor)
        {
            string configName;
            switch (compilerFlavor)
            {
                case CompilerFlavor.C:
                    configName = CConfigName(null);
                    break;
                case CompilerFlavor.Cpp:
                    configName = CppConfigName(null);
                    break;
                case CompilerFlavor.Swift:
                    configName = SwiftConfigName(null);
                    break;
                default:
                    throw new NotImplementedException();
            }

            IDictionary<string, CompilerSettings.Configuration> configurations = compilerSettings.Configurations;
            if (!configurations.ContainsKey(configName))
            {
                string binPath = compilerSettings.RootPath;

                if (compilerFlavor == CompilerFlavor.Swift)
                {
                    // Compiler only
                    configurations.Add(configName, new CompilerSettings.Configuration(SharpmakePlatform, compiler: compilerName, binPath: binPath));
                    return;
                }

                var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(SharpmakePlatform);
                string exeExtension = (Util.GetExecutingPlatform() == Platform.win64) ? ".exe" : "";

                string linkerPath;
                if (!fastBuildSettings.LinkerPath.TryGetValue(devEnv, out linkerPath))
                    linkerPath = binPath;

                string linkerExe;
                if (!fastBuildSettings.LinkerExe.TryGetValue(devEnv, out linkerExe))
                {
                    if (IsLinkerInvokedViaCompiler)
                        linkerExe = "clang++" + exeExtension;
                    else
                        linkerExe = ClangForApple.Settings.IsAppleClang ? "ld" : "ld64.lld" + exeExtension;
                }

                string librarianExe;
                if (!fastBuildSettings.LibrarianExe.TryGetValue(devEnv, out librarianExe))
                    librarianExe = ClangForApple.Settings.IsAppleClang ? "ar" : "llvm-ar" + exeExtension;

                configurations.Add(configName,
                    new CompilerSettings.Configuration(
                        SharpmakePlatform,
                        compiler: compilerName,
                        binPath: binPath,
                        linkerPath: Util.GetCapitalizedPath(Util.PathGetAbsolute(projectRootPath, linkerPath)),
                        librarian: Path.Combine("$LinkerPath$", librarianExe),
                        linker: Path.Combine("$LinkerPath$", linkerExe),
                        fastBuildLinkerType: CompilerSettings.LinkerType.GCC // Workaround: set GCC linker type since it will only enable response files
                    )
                );
            }
        }
        #endregion


        #region IConfigurationTasks

        // The below method was replaced by GetDefaultOutputFullExtension
        // string GetDefaultOutputExtension(OutputType outputType);

        public string GetDefaultOutputFullExtension(Project.Configuration.OutputType outputType)
        {
            switch (outputType)
            {
                // Using the Unix extensions since Darwin is a Unix implementation and the
                // executables Mac users interact with are actually bundles. If this causes
                // issues, see if using .app for executables and .dylib/.framework for
                // libraries work better. iOS is Darwin/Cocoa so assuming that the same goes
                // for it.
                case Project.Configuration.OutputType.Exe:
                    return ExecutableFileFullExtension;
                case Project.Configuration.OutputType.AppleApp:
                    return ".app";
                case Project.Configuration.OutputType.AppleFramework:
                    return ".framework";
                case Project.Configuration.OutputType.AppleBundle:
                    return ".bundle";
                case Project.Configuration.OutputType.IosTestBundle:
                    return ".xctest";
                case Project.Configuration.OutputType.Lib:
                    return StaticLibraryFileFullExtension;
                case Project.Configuration.OutputType.Dll:
                    return SharedLibraryFileFullExtension;

                // .NET remains the same on all platforms. (Mono loads .exe and .dll regardless
                // of platforms, and I assume the same about .NET Core.)
                case Project.Configuration.OutputType.DotNetConsoleApp:
                case Project.Configuration.OutputType.DotNetWindowsApp:
                    return ".exe";
                case Project.Configuration.OutputType.DotNetClassLibrary:
                    return ".dll";

                case Project.Configuration.OutputType.None:
                    return string.Empty;
                default:
                    return outputType.ToString().ToLower();
            }
        }

        public string GetOutputFileNamePrefix(Project.Configuration.OutputType outputType)
        {
            switch (outputType)
            {
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.AppleApp:
                    return string.Empty;
                default:
                    return "lib";
            }
        }

        public IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
        {
            yield break;
        }

        public void SetupDynamicLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            // There's no implib on apple platforms, the dynlib does both,
            // and it's found the same way as static libs with -l to the linker
            if (dependency.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Export &&
                !(configuration.IsFastBuild && !dependency.IsFastBuild))
            {
                if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                    configuration.AddDependencyBuiltTargetLibraryPath(dependency.TargetPath, dependency.TargetLibraryPathOrderNumber);
                if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                    configuration.AddDependencyBuiltTargetLibraryFile(
                        dependency.PreferRelativePaths ?
                            dependency.TargetFileName :
                            dependency.TargetFileFullNameWithExtension,
                        dependency.TargetFileOrderNumber);
            }
            else
            {
                if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                    configuration.AddDependencyBuiltTargetLibraryFile(Path.Combine(dependency.TargetLibraryPath, dependency.TargetFileFullNameWithExtension), dependency.TargetFileOrderNumber);
            }
        }

        public void SetupStaticLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            if (dependency.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Export &&
                !(configuration.IsFastBuild && !dependency.IsFastBuild))
            {
                if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                    configuration.AddDependencyBuiltTargetLibraryPath(dependency.TargetLibraryPath, dependency.TargetLibraryPathOrderNumber);
                if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                    configuration.AddDependencyBuiltTargetLibraryFile(
                        dependency.PreferRelativePaths ?
                            dependency.TargetFileName :
                            dependency.TargetFileFullNameWithExtension,
                        dependency.TargetFileOrderNumber);
            }
            else
            {
                if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                    configuration.DependenciesOtherLibraryFiles.Add(Path.Combine(dependency.TargetLibraryPath, dependency.TargetFileFullNameWithExtension), dependency.TargetFileOrderNumber);
            }
        }

        #endregion

        #region IPlatformVcxproj implementation
        public string ExecutableFileFullExtension => string.Empty;
        public string PackageFileFullExtension => ExecutableFileFullExtension;
        public string SharedLibraryFileFullExtension => ".dylib";
        public string ProgramDatabaseFileFullExtension => string.Empty;
        public string StaticLibraryFileFullExtension => ".a";
        public string StaticOutputLibraryFileFullExtension => StaticLibraryFileFullExtension;
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
            string libStd = cmdLineOptions["StdLib"];
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
        public IEnumerable<string> GetAssemblyIncludePaths(IGenerationContext context)
        {
            return GetAssemblyIncludePathsImpl(context);
        }
        public IEnumerable<string> GetCxUsingPath(IGenerationContext context)
        {
            yield break;
        }
        public IEnumerable<VariableAssignment> GetEnvironmentVariables(IGenerationContext context)
        {
            yield break;
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
            var project = context.Project;

            options["ExcludedSourceFileNames"] = XCodeUtil.XCodeFormatList(conf.ResolvedSourceFilesBuildExclude, 4);
            options["SpecificLibraryPaths"] = FileGeneratorUtilities.RemoveLineTag;
            options["BuildDirectory"] = (conf.Output == Project.Configuration.OutputType.Lib) ? conf.TargetLibraryPath : conf.TargetPath;

            SelectPrecompiledHeaderOptions(context);

            if (conf.IsFastBuild)
            {
                options["FastBuildTarget"] = FastBuildSettings.MakeCommandGenerator.GetTargetIdentifier(conf);
            }
            else
            {
                options["FastBuildTarget"] = FileGeneratorUtilities.RemoveLineTag;
            }

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.AlwaysSearchUserPaths.Disable, () => options["AlwaysSearchUserPaths"] = "NO"),
                Options.Option(Options.XCode.Compiler.AlwaysSearchUserPaths.Enable, () => options["AlwaysSearchUserPaths"] = "YES")
            );

            Options.XCode.Compiler.Archs archs = Options.GetObject<Options.XCode.Compiler.Archs>(conf);
            if (archs != null)
                options["Archs"] = archs.Value;
            else
                options["Archs"] = "\"$(ARCHS_STANDARD_64_BIT)\"";

            var productInstallPath = Options.GetString<Options.XCode.Compiler.ProductInstallPath>(conf);
            if (string.IsNullOrEmpty(productInstallPath) || productInstallPath == FileGeneratorUtilities.RemoveLineTag)
            {
                switch (conf.Output)
                {
                    case Project.Configuration.OutputType.Dll:
                        options["ProductInstallPath"] = FileGeneratorUtilities.RemoveLineTag;
                        break;
                    case Project.Configuration.OutputType.Lib:
                        options["ProductInstallPath"] = FileGeneratorUtilities.RemoveLineTag;
                        break;
                    case Project.Configuration.OutputType.IosTestBundle:
                        options["ProductInstallPath"] = "$(HOME)/Applications";
                        break;
                    case Project.Configuration.OutputType.AppleApp:
                        options["ProductInstallPath"] = "$(USER_APPS_DIR)";
                        break;
                    case Project.Configuration.OutputType.AppleFramework:
                        options["ProductInstallPath"] = "$(LOCAL_LIBRARY_DIR)/Frameworks";
                        break;
                    case Project.Configuration.OutputType.AppleBundle:
                        options["ProductInstallPath"] = "$(LOCAL_LIBRARY_DIR)/Bundles";
                        break;
                    case Project.Configuration.OutputType.Exe:
                    case Project.Configuration.OutputType.None:
                    case Project.Configuration.OutputType.Utility:
                        options["ProductInstallPath"] = FileGeneratorUtilities.RemoveLineTag;
                        break;
                    default:
                        throw new NotSupportedException($"XCode generator doesn't handle {conf.Output}");
                }
            }
            else
            {
                options["ProductInstallPath"] = productInstallPath;
            }

            Strings ldRunPaths = Options.GetStrings<Options.XCode.Compiler.LdRunPaths>(conf);
            options["LdRunPaths"] = ldRunPaths.Count > 0 ? XCodeUtil.XCodeFormatList(ldRunPaths, 4) : FileGeneratorUtilities.RemoveLineTag;

            options["AssetCatalogCompilerAppIconName"] = Options.StringOption.Get<Options.XCode.Compiler.AssetCatalogCompilerAppIconName>(conf);
            options["AssetCatalogCompilerLaunchImageName"] = Options.StringOption.Get<Options.XCode.Compiler.AssetCatalogCompilerLaunchImageName>(conf);
            options["AssetCatalogCompilerAlternateAppIconNames"] = XCodeUtil.XCodeFormatList(Options.GetStrings<Options.XCode.Compiler.AssetCatalogCompilerAlternateAppIconNames>(conf), 4);
            options["AssetCatalogCompilerGlobalAccentColorName"] = Options.StringOption.Get<Options.XCode.Compiler.AssetCatalogCompilerGlobalAccentColorName>(conf);
            options["AssetCatalogCompilerWidgetBackgroundColorName"] = Options.StringOption.Get<Options.XCode.Compiler.AssetCatalogCompilerWidgetBackgroundColorName>(conf);
            context.SelectOptionWithFallback(
                () => options["AssetCatalogCompilerIncludeAllAppIconAssets"] = FileGeneratorUtilities.RemoveLineTag,
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerIncludeAllAppIconAssets.Enable, () => options["AssetCatalogCompilerIncludeAllAppIconAssets"] = "YES"),
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerIncludeAllAppIconAssets.Disable, () => options["AssetCatalogCompilerIncludeAllAppIconAssets"] = "NO")
            );
            context.SelectOptionWithFallback(
                () => options["AssetCatalogCompilerIncludeInfoPlistLocalizations"] = FileGeneratorUtilities.RemoveLineTag,
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerIncludeInfoPlistLocalizations.Enable, () => options["AssetCatalogCompilerIncludeInfoPlistLocalizations"] = "YES"),
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerIncludeInfoPlistLocalizations.Disable, () => options["AssetCatalogCompilerIncludeInfoPlistLocalizations"] = "NO")
            );
            context.SelectOptionWithFallback(
                () => options["AssetCatalogCompilerOptimization"] = FileGeneratorUtilities.RemoveLineTag,
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerOptimization.Time, () => options["AssetCatalogCompilerOptimization"] = "time"),
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerOptimization.Space, () => options["AssetCatalogCompilerOptimization"] = "space")
            );
            context.SelectOptionWithFallback(
                () => options["AssetCatalogCompilerStandaloneIconBehavior"] = FileGeneratorUtilities.RemoveLineTag,
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerStandaloneIconBehavior.Default, () => options["AssetCatalogCompilerStandaloneIconBehavior"] = "Default"),
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerStandaloneIconBehavior.None, () => options["AssetCatalogCompilerStandaloneIconBehavior"] = "None"),
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerStandaloneIconBehavior.All, () => options["AssetCatalogCompilerStandaloneIconBehavior"] = "All")
            );
            context.SelectOptionWithFallback(
                () => options["AssetCatalogCompilerSkipAppStoreDeployment"] = FileGeneratorUtilities.RemoveLineTag,
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerSkipAppStoreDeployment.Enable, () => options["AssetCatalogCompilerSkipAppStoreDeployment"] = "YES"),
                Options.Option(Options.XCode.Compiler.AssetCatalogCompilerSkipAppStoreDeployment.Disable, () => options["AssetCatalogCompilerSkipAppStoreDeployment"] = "NO")
            );
            context.SelectOptionWithFallback(
                () => options["AssetCatalogNotices"] = FileGeneratorUtilities.RemoveLineTag,
                Options.Option(Options.XCode.Compiler.AssetCatalogNotices.Enable, () => options["AssetCatalogNotices"] = "YES"),
                Options.Option(Options.XCode.Compiler.AssetCatalogNotices.Disable, () => options["AssetCatalogNotices"] = "NO")
            );
            context.SelectOptionWithFallback(
                () => options["AssetCatalogWarnings"] = FileGeneratorUtilities.RemoveLineTag,
                Options.Option(Options.XCode.Compiler.AssetCatalogWarnings.Enable, () => options["AssetCatalogWarnings"] = "YES"),
                Options.Option(Options.XCode.Compiler.AssetCatalogWarnings.Disable, () => options["AssetCatalogWarnings"] = "NO")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.AutomaticReferenceCounting.Disable, () => { options["AutomaticReferenceCounting"] = "NO"; cmdLineOptions["ClangEnableObjC_ARC"] = "-fno-objc-arc"; }),
                Options.Option(Options.XCode.Compiler.AutomaticReferenceCounting.Enable, () => { options["AutomaticReferenceCounting"] = "YES"; cmdLineOptions["ClangEnableObjC_ARC"] = "-fobjc-arc"; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.ClangAnalyzerLocalizabilityNonlocalized.Disable, () => options["ClangAnalyzerLocalizabilityNonlocalized"] = "NO"),
                Options.Option(Options.XCode.Compiler.ClangAnalyzerLocalizabilityNonlocalized.Enable, () => options["ClangAnalyzerLocalizabilityNonlocalized"] = "YES")
            );

            context.SelectOption(
               Options.Option(Options.XCode.Compiler.ClangEnableModules.Disable, () => options["ClangEnableModules"] = "NO"),
               Options.Option(Options.XCode.Compiler.ClangEnableModules.Enable, () => options["ClangEnableModules"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.GenerateInfoPlist.Disable, () => options["GenerateInfoPlist"] = "NO"),
                Options.Option(Options.XCode.Compiler.GenerateInfoPlist.Enable, () => options["GenerateInfoPlist"] = "YES")
            );

            Options.XCode.Compiler.CodeSignEntitlements codeSignEntitlements = Options.GetObject<Options.XCode.Compiler.CodeSignEntitlements>(conf);
            if (codeSignEntitlements != null)
                options["CodeSignEntitlements"] = XCodeUtil.ResolveProjectPaths(project, codeSignEntitlements.Value);
            else
                options["CodeSignEntitlements"] = FileGeneratorUtilities.RemoveLineTag;

            Options.XCode.Compiler.CodeSigningIdentity codeSigningIdentity = Options.GetObject<Options.XCode.Compiler.CodeSigningIdentity>(conf);
            if (codeSigningIdentity != null)
                options["CodeSigningIdentity"] = codeSigningIdentity.Value;
            else if (conf.Platform == Platform.ios || conf.Platform == Platform.maccatalyst)
                options["CodeSigningIdentity"] = "iPhone Developer"; //Previous Default value in the template
            else if (conf.Platform == Platform.tvos)
                options["CodeSigningIdentity"] = "AppleTV Developer"; //Previous Default value in the template
            else
                options["CodeSigningIdentity"] = FileGeneratorUtilities.RemoveLineTag;

            options["DyLibInstallName"] = XCodeUtil.ResolveProjectVariable(project, Options.StringOption.Get<Options.XCode.Linker.DyLibInstallName>(conf));

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.OnlyActiveArch.Disable, () => options["OnlyActiveArch"] = "NO"),
                Options.Option(Options.XCode.Compiler.OnlyActiveArch.Enable, () => options["OnlyActiveArch"] = "YES")
            );

            options["ProductBundleDisplayName"] = XCodeUtil.ResolveProjectVariable(project, Options.StringOption.Get<Options.XCode.Compiler.ProductBundleDisplayName>(conf));
            options["ProductBundleIdentifier"] = Options.StringOption.Get<Options.XCode.Compiler.ProductBundleIdentifier>(conf);
            options["ProductBundleVersion"] = Options.StringOption.Get<Options.XCode.Compiler.ProductBundleVersion>(conf);
            options["ProductBundleShortVersion"] = Options.StringOption.Get<Options.XCode.Compiler.ProductBundleShortVersion>(conf);

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP98, () => { options["CppStandard"] = "c++98"; cmdLineOptions["CppLanguageStd"] = "-std=c++98"; }),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP11, () => { options["CppStandard"] = "c++11"; cmdLineOptions["CppLanguageStd"] = "-std=c++11"; }),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP14, () => { options["CppStandard"] = "c++14"; cmdLineOptions["CppLanguageStd"] = "-std=c++14"; }),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP17, () => { options["CppStandard"] = "c++17"; cmdLineOptions["CppLanguageStd"] = "-std=c++17"; }),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.CPP20, () => { options["CppStandard"] = "c++20"; cmdLineOptions["CppLanguageStd"] = "-std=c++20"; }),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU98, () => { options["CppStandard"] = "gnu++98"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++98"; }),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU11, () => { options["CppStandard"] = "gnu++11"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++11"; }),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU14, () => { options["CppStandard"] = "gnu++14"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++14"; }),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU17, () => { options["CppStandard"] = "gnu++17"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++17"; }),
                Options.Option(Options.XCode.Compiler.CppLanguageStandard.GNU20, () => { options["CppStandard"] = "gnu++20"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++20"; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.SwiftLanguageVersion.Disable, () => { options["SwiftVersion"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["SwiftLanguageVersion"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.XCode.Compiler.SwiftLanguageVersion.SWIFT4_0, () => { options["SwiftVersion"] = "4.0"; cmdLineOptions["SwiftLanguageVersion"] = "-swift-version 4"; }),
                Options.Option(Options.XCode.Compiler.SwiftLanguageVersion.SWIFT4_2, () => { options["SwiftVersion"] = "4.2"; cmdLineOptions["SwiftLanguageVersion"] = "-swift-version 4.2"; }),
                Options.Option(Options.XCode.Compiler.SwiftLanguageVersion.SWIFT5_0, () => { options["SwiftVersion"] = "5.0"; cmdLineOptions["SwiftLanguageVersion"] = "-swift-version 5"; }),
                Options.Option(Options.XCode.Compiler.SwiftLanguageVersion.SWIFT6_0, () => { options["SwiftVersion"] = "6.0"; cmdLineOptions["SwiftLanguageVersion"] = "-swift-version 6"; })
            );

            options["DevelopmentTeam"] = Options.StringOption.Get<Options.XCode.Compiler.DevelopmentTeam>(conf);

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.ProvisioningStyle.Automatic, () => { options["ProvisioningStyle"] = "Automatic"; }),
                Options.Option(Options.XCode.Compiler.ProvisioningStyle.Manual, () => { options["ProvisioningStyle"] = "Manual"; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.DebugInformationFormat.Dwarf, () => options["DebugInformationFormat"] = "dwarf"),
                Options.Option(Options.XCode.Compiler.DebugInformationFormat.DwarfWithDSym, () => options["DebugInformationFormat"] = "\"dwarf-with-dsym\""),
                Options.Option(Options.XCode.Compiler.DebugInformationFormat.Stabs, () => options["DebugInformationFormat"] = "stabs")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.DynamicNoPic.Disable, () => options["DynamicNoPic"] = "NO"),
                Options.Option(Options.XCode.Compiler.DynamicNoPic.Enable, () => options["DynamicNoPic"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.EnableBitcode.Disable, () => { options["EnableBitcode"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.EnableBitcode.Enable, () => { options["EnableBitcode"] = "YES"; })
            );

#pragma warning disable 618  // obsolete warning
            bool hasOldExceptionOption = Sharpmake.Options.HasOption<Options.XCode.Compiler.Exceptions>(conf);
            if (hasOldExceptionOption)
            {
                // Convert options - Temporary measure
                context.SelectOption(
                    Options.Option(Options.XCode.Compiler.Exceptions.Disable, () => { conf.Options.Add(Options.XCode.Compiler.CppExceptions.Disable); conf.Options.Add(Options.XCode.Compiler.ObjCExceptions.Disable); }),
                    Options.Option(Options.XCode.Compiler.Exceptions.Enable, () => { conf.Options.Add(Options.XCode.Compiler.CppExceptions.Enable); conf.Options.Add(Options.XCode.Compiler.ObjCExceptions.Enable); }),
                    Options.Option(Options.XCode.Compiler.Exceptions.EnableCpp, () => { conf.Options.Add(Options.XCode.Compiler.CppExceptions.Enable); conf.Options.Add(Options.XCode.Compiler.ObjCExceptions.Disable); }),
                    Options.Option(Options.XCode.Compiler.Exceptions.EnableObjC, () => { conf.Options.Add(Options.XCode.Compiler.CppExceptions.Disable); conf.Options.Add(Options.XCode.Compiler.ObjCExceptions.Enable); })
                );
            }
#pragma warning restore 618

            bool anyExceptionEnabled = false;
            context.SelectOption(
                Options.Option(Options.XCode.Compiler.CppExceptions.Disable, () => { options["CppExceptionHandling"] = "NO"; cmdLineOptions["CppExceptions"] = "-fno-cxx-exceptions"; }),
                Options.Option(Options.XCode.Compiler.CppExceptions.Enable, () => { options["CppExceptionHandling"] = "YES"; cmdLineOptions["CppExceptions"] = "-fcxx-exceptions"; anyExceptionEnabled = true; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.ObjCExceptions.Disable, () => { options["ObjCExceptionHandling"] = "NO"; cmdLineOptions["ObjCExceptions"] = "-fno-objc-exceptions"; }),
                Options.Option(Options.XCode.Compiler.ObjCExceptions.Enable, () => { options["ObjCExceptionHandling"] = "YES"; cmdLineOptions["ObjCExceptions"] = "-fobjc-exceptions"; anyExceptionEnabled = true; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.ObjCARCExceptions.Disable, () => { options["ObjCARCExceptionHandling"] = "NO"; cmdLineOptions["ObjCARCExceptions"] = "-fno-objc-arc-exceptions"; }),
                Options.Option(Options.XCode.Compiler.ObjCARCExceptions.Enable, () => { options["ObjCARCExceptionHandling"] = "YES"; cmdLineOptions["ObjCARCExceptions"] = "-fobjc-arc-exceptions"; anyExceptionEnabled = true; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.AsyncExceptions.Disable, () => { cmdLineOptions["AsyncExceptionHandling"] = "-fno-objc-arc-exceptions"; }),
                Options.Option(Options.XCode.Compiler.AsyncExceptions.Enable, () => { cmdLineOptions["AsyncExceptionHandling"] = "-fobjc-arc-exceptions"; anyExceptionEnabled = true; })
            );

            cmdLineOptions["DisableExceptions"] = anyExceptionEnabled ? FileGeneratorUtilities.RemoveLineTag : "-fno-exceptions";

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.GccNoCommonBlocks.Disable, () => options["GccNoCommonBlocks"] = "NO"),
                Options.Option(Options.XCode.Compiler.GccNoCommonBlocks.Enable, () => options["GccNoCommonBlocks"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.CLanguageStandard.ANSI, () => { options["CStandard"] = "ansi"; cmdLineOptions["CLanguageStd"] = "-std=ansi"; }),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.C89, () => { options["CStandard"] = "c89"; cmdLineOptions["CLanguageStd"] = "-std=c89"; }),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.GNU89, () => { options["CStandard"] = "gnu89"; cmdLineOptions["CLanguageStd"] = "-std=gnu89"; }),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.C99, () => { options["CStandard"] = "c99"; cmdLineOptions["CLanguageStd"] = "-std=c99"; }),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.GNU99, () => { options["CStandard"] = "gnu99"; cmdLineOptions["CLanguageStd"] = "-std=gnu99"; }),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.C11, () => { options["CStandard"] = "c11"; cmdLineOptions["CLanguageStd"] = "-std=c11"; }),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.GNU11, () => { options["CStandard"] = "gnu11"; cmdLineOptions["CLanguageStd"] = "-std=gnu11"; }),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.CompilerDefault, () => options["CStandard"] = FileGeneratorUtilities.RemoveLineTag)
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.ObjCWeakReferences.Disable, () => { options["ObjCWeakReferences"] = "NO"; cmdLineOptions["ClangEnableObjC_Weak"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.XCode.Compiler.ObjCWeakReferences.Enable, () => { options["ObjCWeakReferences"] = "YES"; cmdLineOptions["ClangEnableObjC_Weak"] = "-fobjc-weak"; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Disable, () => { options["OptimizationLevel"] = "0"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Fast, () => { options["OptimizationLevel"] = "1"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Faster, () => { options["OptimizationLevel"] = "2"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Fastest, () => { options["OptimizationLevel"] = "3"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Smallest, () => { options["OptimizationLevel"] = "s"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Aggressive, () => { options["OptimizationLevel"] = "fast"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.AggressiveSize, () => { options["OptimizationLevel"] = "z"; })
            );

            context.SelectOption
            (
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Disable, () => { cmdLineOptions["OptimizationLevel"] = "-O0"; cmdLineOptions["SwiftOptimizationLevel"] = "-Onone"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Fast, () => { cmdLineOptions["OptimizationLevel"] = "-O1"; cmdLineOptions["SwiftOptimizationLevel"] = "-O"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Faster, () => { cmdLineOptions["OptimizationLevel"] = "-O2"; cmdLineOptions["SwiftOptimizationLevel"] = "-O"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Fastest, () => { cmdLineOptions["OptimizationLevel"] = "-O3"; cmdLineOptions["SwiftOptimizationLevel"] = "-O"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Smallest, () => { cmdLineOptions["OptimizationLevel"] = "-Os"; cmdLineOptions["SwiftOptimizationLevel"] = "-O"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Aggressive, () => { cmdLineOptions["OptimizationLevel"] = "-Ofast"; cmdLineOptions["SwiftOptimizationLevel"] = "-O"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.AggressiveSize, () => { cmdLineOptions["OptimizationLevel"] = "-Oz"; cmdLineOptions["SwiftOptimizationLevel"] = "-O"; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.DeadStrip.Disable, () => { options["DeadStripping"] = "NO"; options["PrivateInlines"] = "NO"; cmdLineOptions["DeadCodeStripping"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["PrivateInlines"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.XCode.Compiler.DeadStrip.Code, () => { options["DeadStripping"] = "YES"; options["PrivateInlines"] = "NO"; cmdLineOptions["DeadCodeStripping"] = "-dead_strip"; cmdLineOptions["PrivateInlines"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.XCode.Compiler.DeadStrip.Inline, () => { options["DeadStripping"] = "NO"; options["PrivateInlines"] = "YES"; cmdLineOptions["DeadCodeStripping"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["PrivateInlines"] = "-fvisibility-inlines-hidden"; }),
                Options.Option(Options.XCode.Compiler.DeadStrip.All, () => { options["DeadStripping"] = "YES"; options["PrivateInlines"] = "YES"; cmdLineOptions["DeadCodeStripping"] = "-dead_strip"; cmdLineOptions["PrivateInlines"] = "-fvisibility-inlines-hidden"; })
                );


            context.SelectOption(
                Options.Option(Options.XCode.Compiler.PreserveDeadCodeInitsAndTerms.Disable, () => { options["PreserveDeadCodeInitsAndTerms"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.PreserveDeadCodeInitsAndTerms.Enable, () => { options["PreserveDeadCodeInitsAndTerms"] = "YES"; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.PrivateSymbols.Disable, () => { options["PrivateSymbols"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.PrivateSymbols.Enable, () => { options["PrivateSymbols"] = "YES"; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.RTTI.Disable, () => { options["RuntimeTypeInfo"] = "NO"; cmdLineOptions["RuntimeTypeInfo"] = "-fno-rtti"; }),
                Options.Option(Options.XCode.Compiler.RTTI.Enable, () => { options["RuntimeTypeInfo"] = "YES"; cmdLineOptions["RuntimeTypeInfo"] = "-frtti"; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.StrictObjCMsgSend.Disable, () => options["StrictObjCMsgSend"] = "NO"),
                Options.Option(Options.XCode.Compiler.StrictObjCMsgSend.Enable, () => options["StrictObjCMsgSend"] = "YES")
            );

            context.SelectOption(
               Options.Option(Options.XCode.Compiler.Testability.Disable, () => options["Testability"] = "NO"),
               Options.Option(Options.XCode.Compiler.Testability.Enable, () => options["Testability"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.LibraryStandard.CppStandard, () => { options["StdLib"] = "libstdc++"; cmdLineOptions["StdLib"] = "-stdlib=libstdc++"; }),
                Options.Option(Options.XCode.Compiler.LibraryStandard.LibCxx, () => { options["StdLib"] = "libc++"; cmdLineOptions["StdLib"] = "-stdlib=libc++"; })
            );

            OrderableStrings frameworkPaths = new OrderableStrings(conf.XcodeSystemFrameworkPaths);
            frameworkPaths.AddRange(conf.XcodeDependenciesSystemFrameworkPaths);
            frameworkPaths.AddRange(conf.XcodeFrameworkPaths);
            frameworkPaths.AddRange(conf.XcodeDependenciesFrameworkPaths);
            XCodeUtil.ResolveProjectPaths(project, frameworkPaths);
            options["FrameworkPaths"] = XCodeUtil.XCodeFormatList(frameworkPaths, 4);

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.GenerateDebuggingSymbols.Disable, () => { options["GenerateDebuggingSymbols"] = "NO"; cmdLineOptions["GenerateDebuggingSymbols"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.XCode.Compiler.GenerateDebuggingSymbols.DeadStrip, () => { options["GenerateDebuggingSymbols"] = "YES"; cmdLineOptions["GenerateDebuggingSymbols"] = "-g"; }),
                Options.Option(Options.XCode.Compiler.GenerateDebuggingSymbols.Enable, () => { options["GenerateDebuggingSymbols"] = "YES"; cmdLineOptions["GenerateDebuggingSymbols"] = "-g"; })
            );

            Options.XCode.Compiler.InfoPListFile infoPListFile = Options.GetObject<Options.XCode.Compiler.InfoPListFile>(conf);
            if (infoPListFile != null)
                options["InfoPListFile"] = XCodeUtil.ResolveProjectPaths(project, infoPListFile.Value);
            else
                options["InfoPListFile"] = FileGeneratorUtilities.RemoveLineTag;

            Options.XCode.Compiler.UnitTestInfoPListFile unitTestInfoPListFile = Options.GetObject<Options.XCode.Compiler.UnitTestInfoPListFile>(conf);
            if (unitTestInfoPListFile != null)
                options["UnitTestInfoPListFile"] = XCodeUtil.ResolveProjectPaths(project, unitTestInfoPListFile.Value);
            else
                options["UnitTestInfoPListFile"] = FileGeneratorUtilities.RemoveLineTag;

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.ICloud.Disable, () => options["iCloud"] = "0"),
                Options.Option(Options.XCode.Compiler.ICloud.Enable, () => options["iCloud"] = "1")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.ModelTuning.None, () => options["ModelTuning"] = FileGeneratorUtilities.RemoveLineTag),
                Options.Option(Options.XCode.Compiler.ModelTuning.G3, () => options["ModelTuning"] = "G3"),
                Options.Option(Options.XCode.Compiler.ModelTuning.G4, () => options["ModelTuning"] = "G4"),
                Options.Option(Options.XCode.Compiler.ModelTuning.G5, () => options["ModelTuning"] = "G5")
            );

            options["MachOType"] = FileGeneratorUtilities.RemoveLineTag;
            switch (conf.Output)
            {
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.AppleApp:
                    options["MachOType"] = "mh_execute";
                    break;
                case Project.Configuration.OutputType.AppleFramework:
                    options["MachOType"] = "mh_dylib";
                    break;
                case Project.Configuration.OutputType.AppleBundle:
                    options["MachOType"] = "mh_bundle";
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

            options["ProvisioningProfile"] = Options.StringOption.Get<Options.XCode.Compiler.ProvisioningProfile>(conf);

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.SkipInstall.Disable, () => options["SkipInstall"] = "NO"),
                Options.Option(Options.XCode.Compiler.SkipInstall.Enable, () => options["SkipInstall"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.TargetedDeviceFamily.IosAndIpad, () => options["TargetedDeviceFamily"] = "1,2"),
                Options.Option(Options.XCode.Compiler.TargetedDeviceFamily.Ios, () => options["TargetedDeviceFamily"] = "1"),
                Options.Option(Options.XCode.Compiler.TargetedDeviceFamily.Ipad, () => options["TargetedDeviceFamily"] = "2"),
                Options.Option(Options.XCode.Compiler.TargetedDeviceFamily.Tvos, () => options["TargetedDeviceFamily"] = "3"),
                Options.Option(Options.XCode.Compiler.TargetedDeviceFamily.Watchos, () => options["TargetedDeviceFamily"] = "4")
            );

            Options.XCode.Compiler.ValidArchs validArchs = Options.GetObject<Options.XCode.Compiler.ValidArchs>(conf);
            if (validArchs != null)
                options["ValidArchs"] = validArchs.Archs;
            else
                options["ValidArchs"] = FileGeneratorUtilities.RemoveLineTag;

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.Warning64To32BitConversion.Disable, () => options["Warning64To32BitConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.Warning64To32BitConversion.Enable, () => options["Warning64To32BitConversion"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningBlockCaptureAutoReleasing.Disable, () => options["WarningBlockCaptureAutoReleasing"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningBlockCaptureAutoReleasing.Enable, () => options["WarningBlockCaptureAutoReleasing"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningBooleanConversion.Disable, () => options["WarningBooleanConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningBooleanConversion.Enable, () => options["WarningBooleanConversion"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningComma.Disable, () => options["WarningComma"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningComma.Enable, () => options["WarningComma"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningConstantConversion.Disable, () => options["WarningConstantConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningConstantConversion.Enable, () => options["WarningConstantConversion"] = "YES")
                );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningDeprecatedObjCImplementations.Disable, () => options["WarningDeprecatedObjCImplementations"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningDeprecatedObjCImplementations.Enable, () => options["WarningDeprecatedObjCImplementations"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningDuplicateMethodMatch.Disable, () => options["WarningDuplicateMethodMatch"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningDuplicateMethodMatch.Enable, () => options["WarningDuplicateMethodMatch"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningEmptyBody.Disable, () => options["WarningEmptyBody"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningEmptyBody.Enable, () => options["WarningEmptyBody"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningEnumConversion.Disable, () => options["WarningEnumConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningEnumConversion.Enable, () => options["WarningEnumConversion"] = "YES")
                );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningDirectIsaUsage.Disable, () => options["WarningDirectIsaUsage"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningDirectIsaUsage.Enable, () => options["WarningDirectIsaUsage"] = "YES"),
                Options.Option(Options.XCode.Compiler.WarningDirectIsaUsage.EnableAndError, () => options["WarningDirectIsaUsage"] = "YES_ERROR")
            );

            context.SelectOption(
               Options.Option(Options.XCode.Compiler.WarningInfiniteRecursion.Disable, () => options["WarningInfiniteRecursion"] = "NO"),
               Options.Option(Options.XCode.Compiler.WarningInfiniteRecursion.Enable, () => options["WarningInfiniteRecursion"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningIntConversion.Disable, () => options["WarningIntConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningIntConversion.Enable, () => options["WarningIntConversion"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningNonLiteralNullConversion.Disable, () => options["WarningNonLiteralNullConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningNonLiteralNullConversion.Enable, () => options["WarningNonLiteralNullConversion"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningObjCImplicitRetainSelf.Disable, () => options["WarningObjCImplicitRetainSelf"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningObjCImplicitRetainSelf.Enable, () => options["WarningObjCImplicitRetainSelf"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningObjCLiteralConversion.Disable, () => options["WarningObjCLiteralConversion"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningObjCLiteralConversion.Enable, () => options["WarningObjCLiteralConversion"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningRangeLoopAnalysis.Disable, () => options["WarningRangeLoopAnalysis"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningRangeLoopAnalysis.Enable, () => options["WarningRangeLoopAnalysis"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningReturnType.Disable, () => { options["WarningReturnType"] = "NO"; cmdLineOptions["WarningReturnType"] = "-Wno-return-type"; }),
                Options.Option(Options.XCode.Compiler.WarningReturnType.Enable, () => { options["WarningReturnType"] = "YES"; cmdLineOptions["WarningReturnType"] = "-Wreturn-type"; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningRootClass.Disable, () => options["WarningRootClass"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningRootClass.Enable, () => options["WarningRootClass"] = "YES"),
                Options.Option(Options.XCode.Compiler.WarningRootClass.EnableAndError, () => options["WarningRootClass"] = "YES_ERROR")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningStrictPrototypes.Disable, () => options["WarningStrictPrototypes"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningStrictPrototypes.Enable, () => options["WarningStrictPrototypes"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningSuspiciousMove.Disable, () => options["WarningSuspiciousMove"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningSuspiciousMove.Enable, () => options["WarningSuspiciousMove"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningUndeclaredSelector.Disable, () => options["WarningUndeclaredSelector"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUndeclaredSelector.Enable, () => options["WarningUndeclaredSelector"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningUniniatializedAutos.Disable, () => options["WarningUniniatializedAutos"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUniniatializedAutos.Enable, () => options["WarningUniniatializedAutos"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningUnreachableCode.Disable, () => options["WarningUnreachableCode"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUnreachableCode.Enable, () => options["WarningUnreachableCode"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningUnusedFunction.Disable, () => options["WarningUnusedFunction"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUnusedFunction.Enable, () => options["WarningUnusedFunction"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.WarningUnusedVariable.Disable, () => options["WarningUnusedVariable"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningUnusedVariable.Enable, () => options["WarningUnusedVariable"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.DeploymentPostProcessing.Disable, () => options["DeploymentPostProcessing"] = "NO"),
                Options.Option(Options.XCode.Compiler.DeploymentPostProcessing.Enable, () => options["DeploymentPostProcessing"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.StripDebugSymbolsDuringCopy.Disable, () => options["StripDebugSymbolsDuringCopy"] = "NO"),
                Options.Option(Options.XCode.Compiler.StripDebugSymbolsDuringCopy.Enable, () => options["StripDebugSymbolsDuringCopy"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.TreatWarningsAsErrors.Disable, () => options["TreatWarningsAsErrors"] = "NO"),
                Options.Option(Options.XCode.Compiler.TreatWarningsAsErrors.Enable, () => options["TreatWarningsAsErrors"] = "YES")
            );

            Strings specificDeviceLibraryPaths = Options.GetStrings<Options.XCode.Compiler.SpecificDeviceLibraryPaths>(conf);
            XCodeUtil.ResolveProjectPaths(project, specificDeviceLibraryPaths);
            options["SpecificDeviceLibraryPaths"] = XCodeUtil.XCodeFormatList(specificDeviceLibraryPaths, 4);

            Strings specificSimulatorLibraryPaths = Options.GetStrings<Options.XCode.Compiler.SpecificSimulatorLibraryPaths>(conf);
            XCodeUtil.ResolveProjectPaths(project, specificSimulatorLibraryPaths);
            options["SpecificSimulatorLibraryPaths"] = XCodeUtil.XCodeFormatList(specificSimulatorLibraryPaths, 4);

            options["WarningOptions"] = FileGeneratorUtilities.RemoveLineTag;

            options["SupportsMaccatalyst"] = FileGeneratorUtilities.RemoveLineTag;
            options["SupportsMacDesignedForIphoneIpad"] = FileGeneratorUtilities.RemoveLineTag;

            context.SelectOptionWithFallback(
                () => options["SwiftEmitLocStrings"] = FileGeneratorUtilities.RemoveLineTag,
                Options.Option(Options.XCode.Compiler.SwiftEmitLocStrings.Disable, () => options["SwiftEmitLocStrings"] = "NO"),
                Options.Option(Options.XCode.Compiler.SwiftEmitLocStrings.Enable, () => options["SwiftEmitLocStrings"] = "YES")
            );

            cmdLineOptions["SwiftModuleName"] = Options.StringOption.Get<Options.XCode.Compiler.SwiftModuleName>(conf);

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.MetalFastMath.Disable, () => options["MetalFastMath"] = "NO"),
                Options.Option(Options.XCode.Compiler.MetalFastMath.Enable, () => options["MetalFastMath"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.UseHeaderMap.Disable, () => options["UseHeaderMap"] = "NO"),
                Options.Option(Options.XCode.Compiler.UseHeaderMap.Enable, () => options["UseHeaderMap"] = "YES")
            );

            #region infoplist keys
            /// common keys
            options["CFBundleSpokenName"] = Options.StringOption.Get<Options.XCode.InfoPlist.CFBundleSpokenName>(conf);
            options["CFBundleDevelopmentRegion"] = Options.StringOption.Get<Options.XCode.InfoPlist.CFBundleDevelopmentRegion>(conf);
            options["CFBundleExecutable"] = Options.StringOption.Get<Options.XCode.InfoPlist.CFBundleExecutable>(conf);
            options["CFBundleLocalizations"] = XCodeUtil.XCodeFormatList(Options.GetStrings<Options.XCode.InfoPlist.CFBundleLocalizations>(conf), 4);

            context.SelectOptionWithFallback(
                () => options["CFBundleAllowMixedLocalizations"] = FileGeneratorUtilities.RemoveLineTag,
                Options.Option(Options.XCode.InfoPlist.CFBundleAllowMixedLocalizations.Disable, () => options["CFBundleAllowMixedLocalizations"] = "NO"),
                Options.Option(Options.XCode.InfoPlist.CFBundleAllowMixedLocalizations.Enable, () => options["CFBundleAllowMixedLocalizations"] = "YES")
            );

            context.SelectOptionWithFallback(
                () => options["NSHighResolutionCapable"] = FileGeneratorUtilities.RemoveLineTag,
                Options.Option(Options.XCode.InfoPlist.NSHighResolutionCapable.Disable, () => options["NSHighResolutionCapable"] = "NO"),
                Options.Option(Options.XCode.InfoPlist.NSHighResolutionCapable.Enable, () => options["NSHighResolutionCapable"] = "YES")
            );

            /// - macOS specific, set to proper value in override method
            options["NSHumanReadableCopyright"] = FileGeneratorUtilities.RemoveLineTag;
            options["NSMainStoryboardFile"] = FileGeneratorUtilities.RemoveLineTag;
            options["NSMainNibFile"] = FileGeneratorUtilities.RemoveLineTag;
            options["NSPrefPaneIconFile"] = FileGeneratorUtilities.RemoveLineTag;
            options["NSPrefPaneIconLabel"] = FileGeneratorUtilities.RemoveLineTag;
            options["NSPrincipalClass"] = FileGeneratorUtilities.RemoveLineTag;
            options["LSRequiresNativeExecution"] = FileGeneratorUtilities.RemoveLineTag;
            options["LSMultipleInstancesProhibited"] = FileGeneratorUtilities.RemoveLineTag;
            options["NSSupportsAutomaticGraphicsSwitching"] = FileGeneratorUtilities.RemoveLineTag;
            options["NSPrefersDisplaySafeAreaCompatibilityMode"] = FileGeneratorUtilities.RemoveLineTag;
            options["UISupportsTrueScreenSizeOnMac"] = FileGeneratorUtilities.RemoveLineTag;

            /// - iOS specific, set to proper value in override method
            options["LSRequiresIPhoneOS"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIRequiredDeviceCapabilities"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIMainStoryboardFile"] = FileGeneratorUtilities.RemoveLineTag;
            options["UILaunchStoryboardName"] = FileGeneratorUtilities.RemoveLineTag;
            options["CFBundleIconFile"] = FileGeneratorUtilities.RemoveLineTag;
            options["CFBundleIconFiles"] = FileGeneratorUtilities.RemoveLineTag;
            options["CFBundleIconName"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIPrerenderedIcon"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIInterfaceOrientation"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIInterfaceOrientation_iPhone"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIInterfaceOrientation_iPad"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIUserInterfaceStyle"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIWhitePointAdaptivityStyle"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIRequiresFullScreen"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIStatusBarHidden"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIViewControllerBasedStatusBarAppearance"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIStatusBarStyle"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIApplicationSupportsIndirectInputEvents"] = FileGeneratorUtilities.RemoveLineTag;
            options["UIRequiresPersistentWiFi"] = FileGeneratorUtilities.RemoveLineTag;
            options["UISupportedInterfaceOrientations"] = FileGeneratorUtilities.RemoveLineTag;
            options["UISupportedInterfaceOrientations_iPhone"] = FileGeneratorUtilities.RemoveLineTag;
            options["UISupportedInterfaceOrientations_iPad"] = FileGeneratorUtilities.RemoveLineTag;

            /// - tvOS specific, set to proper value in override method
            options["UIAppSupportsHDR"] = FileGeneratorUtilities.RemoveLineTag;
            #endregion // infoplist keys
        }

        public virtual void SelectPrecompiledHeaderOptions(IGenerationContext context)
        {
            FixupPrecompiledHeaderOptions(context);
        }

        protected void FixupPrecompiledHeaderOptions(IGenerationContext context)
        {
            var options = context.Options;
            var cmdLineOptions = context.CommandLineOptions;
            var conf = context.Configuration;

            if (!HasPrecomp(context))
            {
                options["UsePrecompiledHeader"] = "NO";
                options["PrecompiledHeader"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                options["UsePrecompiledHeader"] = "YES";

                Strings pathsToConsider = new Strings(context.ProjectSourceCapitalized);
                pathsToConsider.AddRange(context.Project.AdditionalSourceRootPaths);
                pathsToConsider.AddRange(GetIncludePaths(context));

                string pchFileSourceRelative = null;
                if (!options.TryGetValue("PrecompiledHeaderThrough", out pchFileSourceRelative))
                    pchFileSourceRelative = conf.PrecompHeader;

                string pchFileXCodeRelative = null;
                bool foundPchInclude = false;

                foreach (var includePath in pathsToConsider)
                {
                    var pchFile = Util.PathGetAbsolute(includePath, pchFileSourceRelative);
                    if (Util.FileExists(pchFile))
                    {
                        pchFileXCodeRelative = Util.PathGetRelative(context.ProjectDirectory, pchFile, true);
                        foundPchInclude = true;
                        break;
                    }
                }

                if (!foundPchInclude)
                    throw new Error($"Sharpmake couldn't locate the PCH '{pchFileSourceRelative}' in {conf}");

                options["PrecompiledHeader"] = pchFileXCodeRelative;
            }
        }

        private string GetDeploymentTargetPlatform(Platform platform)
        {
            switch (platform)
            {
                case Platform.mac:
                    return "macos";
                default:
                    return platform.ToString();
            }
        }

        public string GetDeploymentTargetPrefix(Project.Configuration conf)
        {
            var arch = Options.GetObject<Options.XCode.Compiler.Archs>(conf)?.Value ?? "arm64";
            string deploymentTarget = $"-target {arch}-apple-{GetDeploymentTargetPlatform(conf.Platform)}";

            return deploymentTarget;
        }

        public void SelectCustomSysLibRoot(IGenerationContext context, string defaultSdkRoot)
        {
            var conf = context.Configuration;
            var cmdLineOptions = context.CommandLineOptions;

            cmdLineOptions["SysLibRoot"] = (IsLinkerInvokedViaCompiler ? "-isysroot " : "-syslibroot ") + defaultSdkRoot;
        }

        public virtual void SelectLinkerOptions(IGenerationContext context)
        {
            var options = context.Options;
            var cmdLineOptions = context.CommandLineOptions;
            var conf = context.Configuration;
            var platform = context.Configuration.Platform;

            switch (conf.Output)
            {
                case Project.Configuration.OutputType.Dll:
                case Project.Configuration.OutputType.AppleApp:
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.AppleFramework:
                    if (context.Options["GenerateMapFile"] == "true")
                    {
                        string mapFileArg = context.CommandLineOptions["GenerateMapFile"];
                        string mapOption = $"{platform.GetLinkerOptionPrefix()}-Map=";
                        if (!mapFileArg.StartsWith(mapOption, StringComparison.Ordinal))
                            throw new Error("Map file argument was supposed to start with -Wl,-Map= but it changed! Please update this module!");
                        cmdLineOptions["GenerateMapFile"] = (IsLinkerInvokedViaCompiler ? "-Wl,-map," : "-map ") + cmdLineOptions["GenerateMapFile"].Substring(mapOption.Length);
                    }
                    break;
                default:
                    break;
            }

            // TODO: implement me
            cmdLineOptions["UseThinArchives"] = "";

            context.SelectOption(
                Options.Option(Options.XCode.Linker.StripLinkedProduct.Disable, () => options["StripLinkedProduct"] = "NO"),
                Options.Option(Options.XCode.Linker.StripLinkedProduct.Enable, () => options["StripLinkedProduct"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Linker.StripStyle.AllSymbols, () => options["StripStyle"] = "all"),
                Options.Option(Options.XCode.Linker.StripStyle.NonGlobalSymbols, () => options["StripStyle"] = "non-global"),
                Options.Option(Options.XCode.Linker.StripStyle.DebuggingSymbolsOnly, () => options["StripStyle"] = "debugging")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Linker.StripSwiftSymbols.Disable, () => options["StripSwiftSymbols"] = "NO"),
                Options.Option(Options.XCode.Linker.StripSwiftSymbols.Enable, () => options["StripSwiftSymbols"] = "YES")
            );

            options["AdditionalStripFlags"] = XCodeUtil.ResolveProjectVariable(context.Project, Options.StringOption.Get<Options.XCode.Linker.AdditionalStripFlags>(conf));

            context.SelectOption(
                Options.Option(Options.XCode.Linker.PerformSingleObjectPrelink.Disable, () => options["GenerateMasterObjectFile"] = "NO"),
                Options.Option(Options.XCode.Linker.PerformSingleObjectPrelink.Enable, () => options["GenerateMasterObjectFile"] = "YES")
            );

            options["PreLinkedLibraries"] = FileGeneratorUtilities.RemoveLineTag;

            var prelinkedLibrary = Options.XCode.Linker.PrelinkLibraries.Get<Options.XCode.Linker.PrelinkLibraries>(conf);
            if (!string.IsNullOrEmpty(prelinkedLibrary))
            {
                // xcode for some reason does not use arrays for this setting but space separated values
                options["PreLinkedLibraries"] = prelinkedLibrary;
            }

            var dylibInstallName = XCodeUtil.ResolveProjectVariable(context.Project, Options.StringOption.Get<Options.XCode.Linker.DyLibInstallName>(conf));
            if (!string.IsNullOrEmpty(dylibInstallName))
            {
                cmdLineOptions["DyLibInstallName"] = $"-install_name \"{dylibInstallName}\"";
            }

            OrderableStrings systemFrameworks = new OrderableStrings(conf.XcodeSystemFrameworks);
            systemFrameworks.AddRange(conf.XcodeDependenciesSystemFrameworks);
            cmdLineOptions["SystemFrameworks"] = systemFrameworks.Any() ? "-framework " + systemFrameworks.JoinStrings(" -framework ") : FileGeneratorUtilities.RemoveLineTag;

            OrderableStrings developerFrameworks = new OrderableStrings(conf.XcodeDeveloperFrameworks);
            developerFrameworks.AddRange(conf.XcodeDependenciesDeveloperFrameworks);
            cmdLineOptions["DeveloperFrameworks"] = developerFrameworks.Any() ? "-framework " + developerFrameworks.JoinStrings(" -framework ") : FileGeneratorUtilities.RemoveLineTag;

            OrderableStrings userFrameworks = new OrderableStrings(conf.XcodeUserFrameworks);
            userFrameworks.AddRange(conf.XcodeDependenciesUserFrameworks);
            cmdLineOptions["UserFrameworks"] = userFrameworks.Any() ? "-framework " + userFrameworks.JoinStrings(" -framework ") : FileGeneratorUtilities.RemoveLineTag;

            OrderableStrings embeddedFrameworks = new OrderableStrings(conf.XcodeEmbeddedFrameworks);
            embeddedFrameworks.AddRange(conf.XcodeDependenciesEmbeddedFrameworks);
            cmdLineOptions["EmbeddedFrameworks"] = embeddedFrameworks.Any() ? "-framework " + embeddedFrameworks.JoinStrings(" -framework ") : FileGeneratorUtilities.RemoveLineTag;

            OrderableStrings systemFrameworkPaths = new OrderableStrings(conf.XcodeSystemFrameworkPaths);
            systemFrameworkPaths.AddRange(conf.XcodeDependenciesSystemFrameworkPaths);
            cmdLineOptions["CompilerSystemFrameworkPaths"] = systemFrameworkPaths.Any() ? "-iframework " + systemFrameworkPaths.JoinStrings(" -iframework ") : FileGeneratorUtilities.RemoveLineTag;
            cmdLineOptions["LinkerSystemFrameworkPaths"] = systemFrameworkPaths.Any() ? "-F " + systemFrameworkPaths.JoinStrings(" -F ") : FileGeneratorUtilities.RemoveLineTag;

            OrderableStrings frameworkPaths = new OrderableStrings(conf.XcodeFrameworkPaths);
            frameworkPaths.AddRange(conf.XcodeDependenciesFrameworkPaths);
            cmdLineOptions["CompilerFrameworkPaths"] = frameworkPaths.Any() ? "-F " + frameworkPaths.JoinStrings(" -F ") : FileGeneratorUtilities.RemoveLineTag;
            cmdLineOptions["LinkerFrameworkPaths"] = frameworkPaths.Any() ? "-F " + frameworkPaths.JoinStrings(" -F ") : FileGeneratorUtilities.RemoveLineTag;
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

        public virtual void SelectPreprocessorDefinitionsVcxproj(IVcxprojGenerationContext context)
        {
            throw new NotImplementedException(SimplePlatformString + " should not be called by a Vcxproj generator");
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

        public void GenerateProjectNasmVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
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
        public void SetupPlatformLibraryOptions(out string platformLibExtension, out  string platformOutputLibExtension, out string platformPrefixExtension, out string platformLibPrefix)
        {
            platformLibExtension = ".a";
            platformOutputLibExtension = "";
            platformPrefixExtension = "-l";
            platformLibPrefix = "";
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
            var conf = context.Configuration;

            var systemIncludes = new OrderableStrings();
            systemIncludes.AddRange(conf.DependenciesIncludeSystemPaths);
            systemIncludes.AddRange(conf.IncludeSystemPaths);

            systemIncludes.Sort();

            const string cmdLineIncludePrefix = "-isystem ";
            return systemIncludes.Select(path => new IncludeWithPrefix(cmdLineIncludePrefix, path));
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

        private IEnumerable<string> GetAssemblyIncludePathsImpl(IGenerationContext context)
        {
            yield break;
        }
        #endregion // IPlatformVcxproj implementation

    }
}
