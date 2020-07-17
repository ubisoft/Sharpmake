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
using Sharpmake.Generators.Apple;
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
                        fastBuildDefines.Add(string.Concat(platformDefineSwitch, define));
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
                        fastBuildDefines.Add(string.Concat(platformDefineSwitch, resourceDefine));
                }
                context.CommandLineOptions["ResourcePreprocessorDefinitions"] = string.Join($"'{Environment.NewLine}                                    + ' ", fastBuildDefines);
            }
            else
            {
                context.CommandLineOptions["ResourcePreprocessorDefinitions"] = FileGeneratorUtilities.RemoveLineTag;
            }
        }

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
            var project = context.Project;

            options["ExcludedSourceFileNames"] = XCodeUtil.XCodeFormatList(conf.ResolvedSourceFilesBuildExclude, 4);
            options["SpecificLibraryPaths"] = FileGeneratorUtilities.RemoveLineTag;
            options["TargetedDeviceFamily"] = "1,2";
            options["BuildDirectory"] = (conf.Output == Project.Configuration.OutputType.Lib) ? conf.TargetLibraryPath : conf.TargetPath;

            SelectPrecompiledHeaderOptions(context);

            if (conf.IsFastBuild)
            {
                options["FastBuildTarget"] = Bff.GetShortProjectName(project, conf);
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

            Options.XCode.Compiler.AssetCatalogCompilerAppIconName assetcatalogCompilerAppiconName = Options.GetObject<Options.XCode.Compiler.AssetCatalogCompilerAppIconName>(conf);
            if (assetcatalogCompilerAppiconName != null)
                options["AssetCatalogCompilerAppIconName"] = assetcatalogCompilerAppiconName.Value;
            else
                options["AssetCatalogCompilerAppIconName"] = FileGeneratorUtilities.RemoveLineTag;

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.AutomaticReferenceCounting.Disable, () => options["AutomaticReferenceCounting"] = "NO"),
                Options.Option(Options.XCode.Compiler.AutomaticReferenceCounting.Enable, () => options["AutomaticReferenceCounting"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.ClangAnalyzerLocalizabilityNonlocalized.Disable, () => options["ClangAnalyzerLocalizabilityNonlocalized"] = "NO"),
                Options.Option(Options.XCode.Compiler.ClangAnalyzerLocalizabilityNonlocalized.Enable, () => options["ClangAnalyzerLocalizabilityNonlocalized"] = "YES")
            );

            context.SelectOption(
               Options.Option(Options.XCode.Compiler.ClangEnableModules.Disable, () => options["ClangEnableModules"] = "NO"),
               Options.Option(Options.XCode.Compiler.ClangEnableModules.Enable, () => options["ClangEnableModules"] = "YES")
            );

            Options.XCode.Compiler.CodeSignEntitlements codeSignEntitlements = Options.GetObject<Options.XCode.Compiler.CodeSignEntitlements>(conf);
            if (codeSignEntitlements != null)
                options["CodeSignEntitlements"] = XCodeUtil.ResolveProjectPaths(project, codeSignEntitlements.Value);
            else
                options["CodeSignEntitlements"] = FileGeneratorUtilities.RemoveLineTag;

            Options.XCode.Compiler.CodeSigningIdentity codeSigningIdentity = Options.GetObject<Options.XCode.Compiler.CodeSigningIdentity>(conf);
            if (codeSigningIdentity != null)
                options["CodeSigningIdentity"] = codeSigningIdentity.Value;
            else if (conf.Platform == Platform.ios)
                options["CodeSigningIdentity"] = "iPhone Developer"; //Previous Default value in the template
            else
                options["CodeSigningIdentity"] = FileGeneratorUtilities.RemoveLineTag;

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.OnlyActiveArch.Disable, () => options["OnlyActiveArch"] = "NO"),
                Options.Option(Options.XCode.Compiler.OnlyActiveArch.Enable, () => options["OnlyActiveArch"] = "YES")
            );

            options["ProductBundleIdentifier"] = Options.StringOption.Get<Options.XCode.Compiler.ProductBundleIdentifier>(conf);

            context.SelectOption(
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
            else
                options["DevelopmentTeam"] = FileGeneratorUtilities.RemoveLineTag;

            Options.XCode.Compiler.ProvisioningStyle provisioningStyle = Options.GetObject<Options.XCode.Compiler.ProvisioningStyle>(conf);
            if (provisioningStyle != null)
                options["ProvisioningStyle"] = provisioningStyle.Value;
            else
                options["ProvisioningStyle"] = "Automatic";

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

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.Exceptions.Disable, () => { options["CppExceptionHandling"] = "NO"; options["ObjCExceptionHandling"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.Exceptions.Enable, () => { options["CppExceptionHandling"] = "YES"; options["ObjCExceptionHandling"] = "YES"; }),
                Options.Option(Options.XCode.Compiler.Exceptions.EnableCpp, () => { options["CppExceptionHandling"] = "YES"; options["ObjCExceptionHandling"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.Exceptions.EnableObjC, () => { options["CppExceptionHandling"] = "NO"; options["ObjCExceptionHandling"] = "YES"; })
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.GccNoCommonBlocks.Disable, () => options["GccNoCommonBlocks"] = "NO"),
                Options.Option(Options.XCode.Compiler.GccNoCommonBlocks.Enable, () => options["GccNoCommonBlocks"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.CLanguageStandard.ANSI, () => options["CStandard"] = "ansi"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.C89, () => options["CStandard"] = "c89"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.GNU89, () => options["CStandard"] = "gnu89"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.C99, () => options["CStandard"] = "c99"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.GNU99, () => options["CStandard"] = "gnu99"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.C11, () => options["CStandard"] = "c11"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.GNU11, () => options["CStandard"] = "gnu11"),
                Options.Option(Options.XCode.Compiler.CLanguageStandard.CompilerDefault, () => options["CStandard"] = FileGeneratorUtilities.RemoveLineTag)
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.ObjCWeakReferences.Disable, () => options["ObjCWeakReferences"] = "NO"),
                Options.Option(Options.XCode.Compiler.ObjCWeakReferences.Enable, () => options["ObjCWeakReferences"] = "YES")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Disable, () => { options["OptimizationLevel"] = "0"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Fast, () => { options["OptimizationLevel"] = "1"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Faster, () => { options["OptimizationLevel"] = "2"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Fastest, () => { options["OptimizationLevel"] = "3"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Smallest, () => { options["OptimizationLevel"] = "s"; }),
                Options.Option(Options.XCode.Compiler.OptimizationLevel.Aggressive, () => { options["OptimizationLevel"] = "fast"; })
            );

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
                Options.Option(Options.XCode.Compiler.DeadStrip.Disable, () => { options["DeadStripping"] = "NO"; options["PrivateInlines"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.DeadStrip.Code, () => { options["DeadStripping"] = "YES"; options["PrivateInlines"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.DeadStrip.Inline, () => { options["DeadStripping"] = "NO"; options["PrivateInlines"] = "YES"; }),
                Options.Option(Options.XCode.Compiler.DeadStrip.All, () => { options["DeadStripping"] = "YES"; options["PrivateInlines"] = "YES"; })
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
                Options.Option(Options.XCode.Compiler.RTTI.Disable, () => { options["RuntimeTypeInfo"] = "NO"; }),
                Options.Option(Options.XCode.Compiler.RTTI.Enable, () => { options["RuntimeTypeInfo"] = "YES"; })
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
                Options.Option(Options.XCode.Compiler.LibraryStandard.CppStandard, () => { options["LibraryStandard"] = "libstdc++"; cmdLineOptions["LibraryStandard"] = "-stdlib=libstdc++"; }),
                Options.Option(Options.XCode.Compiler.LibraryStandard.LibCxx, () => { options["LibraryStandard"] = "libc++"; cmdLineOptions["LibraryStandard"] = "-stdlib=libc++"; })
            );

            Strings frameworkPaths = Options.GetStrings<Options.XCode.Compiler.FrameworkPaths>(conf);
            XCodeUtil.ResolveProjectPaths(project, frameworkPaths);
            options["FrameworkPaths"] = XCodeUtil.XCodeFormatList(frameworkPaths, 4);

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.GenerateDebuggingSymbols.Disable, () => options["GenerateDebuggingSymbols"] = "NO"),
                Options.Option(Options.XCode.Compiler.GenerateDebuggingSymbols.DeadStrip, () => options["GenerateDebuggingSymbols"] = "YES"),
                Options.Option(Options.XCode.Compiler.GenerateDebuggingSymbols.Enable, () => options["GenerateDebuggingSymbols"] = "YES")
            );

            Options.XCode.Compiler.InfoPListFile infoPListFile = Options.GetObject<Options.XCode.Compiler.InfoPListFile>(conf);
            if (infoPListFile != null)
                options["InfoPListFile"] = XCodeUtil.ResolveProjectPaths(project, infoPListFile.Value);
            else
                options["InfoPListFile"] = FileGeneratorUtilities.RemoveLineTag;

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.ICloud.Disable, () => options["iCloud"] = "0"),
                Options.Option(Options.XCode.Compiler.ICloud.Enable, () => options["iCloud"] = "1")
            );

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.LibraryStandard.CppStandard, () => options["LibraryStandard"] = "libstdc++"),
                Options.Option(Options.XCode.Compiler.LibraryStandard.LibCxx, () => options["LibraryStandard"] = "libc++")
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
            else
                options["ProvisioningProfile"] = FileGeneratorUtilities.RemoveLineTag;

            Options.XCode.Compiler.SDKRoot sdkRoot = Options.GetObject<Options.XCode.Compiler.SDKRoot>(conf);
            if (sdkRoot != null)
                options["SDKRoot"] = sdkRoot.Value;

            context.SelectOption(
                Options.Option(Options.XCode.Compiler.SkipInstall.Disable, () => options["SkipInstall"] = "NO"),
                Options.Option(Options.XCode.Compiler.SkipInstall.Enable, () => options["SkipInstall"] = "YES")
            );

            Options.XCode.Compiler.TargetedDeviceFamily targetedDeviceFamily = Options.GetObject<Options.XCode.Compiler.TargetedDeviceFamily>(conf);
            if (targetedDeviceFamily != null)
                options["TargetedDeviceFamily"] = targetedDeviceFamily.Value;
            else
                options["TargetedDeviceFamily"] = FileGeneratorUtilities.RemoveLineTag;


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
                Options.Option(Options.XCode.Compiler.WarningReturnType.Disable, () => options["WarningReturnType"] = "NO"),
                Options.Option(Options.XCode.Compiler.WarningReturnType.Enable, () => options["WarningReturnType"] = "YES")
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
        }

        private void SelectPrecompiledHeaderOptions(IGenerationContext context)
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

            context.SelectOption(
                Options.Option(Options.XCode.Linker.StripLinkedProduct.Disable, () => options["StripLinkedProduct"] = "NO"),
                Options.Option(Options.XCode.Linker.StripLinkedProduct.Enable, () => options["StripLinkedProduct"] = "YES")
            );
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
