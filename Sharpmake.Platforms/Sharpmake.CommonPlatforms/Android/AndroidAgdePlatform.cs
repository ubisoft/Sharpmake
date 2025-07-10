// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public static partial class Android
    {
        [PlatformImplementation(Platform.agde,
            typeof(IPlatformDescriptor),
            typeof(IFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IClangPlatformBff),
            typeof(IPlatformVcxproj),
            typeof(Project.Configuration.IConfigurationTasks))]
        public sealed partial class AndroidAgdePlatform : BasePlatform, Project.Configuration.IConfigurationTasks, IFastBuildCompilerSettings, IClangPlatformBff
        {
            #region IPlatformDescriptor implementation
            public override string SimplePlatformString => "Agde";

            public override string GetToolchainPlatformString(ITarget target)
            {
                ArgumentNullException.ThrowIfNull(target);

                var buildTarget = target.GetFragment<AndroidBuildTargets>();
                switch (buildTarget)
                {
                    case AndroidBuildTargets.armeabi_v7a:
                        return "Android-armeabi-v7a";
                    case AndroidBuildTargets.arm64_v8a:
                        return "Android-arm64-v8a";
                    case AndroidBuildTargets.x86:
                        return "Android-x86";
                    case AndroidBuildTargets.x86_64:
                        return "Android-x86_64";
                    default:
                        throw new Exception(string.Format("Unsupported Android AGDE architecture: {0}", buildTarget));
                }
            }

            public override bool IsMicrosoftPlatform => false;
            public override bool IsPcPlatform => false;
            public override bool IsUsingClang => true;
            public override bool IsLinkerInvokedViaCompiler { get; set; } = true;
            public override bool HasDotNetSupport => false;
            public override bool HasSharedLibrarySupport => true;
            public override bool HasPrecompiledHeaderSupport => true;
            #endregion

            #region IFastBuildCompilerSettings implementation
            public IDictionary<DevEnv, string> BinPath { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LinkerPath { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LinkerExe { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LibrarianExe { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, Strings> ExtraFiles { get; set; } = new Dictionary<DevEnv, Strings>();
            public IDictionary<IFastBuildCompilerKey, CompilerFamily> CompilerFamily { get; set; } = new Dictionary<IFastBuildCompilerKey, CompilerFamily>();
            IDictionary<DevEnv, bool> IFastBuildCompilerSettings.LinkerInvokedViaCompiler { get; set; } = new Dictionary<DevEnv, bool>();
            #endregion

            #region Project.Configuration.IConfigurationTasks implementation
            public void SetupDynamicLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
            {
                // Not tested. We may need to root the path like we do in SetupStaticLibraryPaths.
                DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
            }

            public void SetupStaticLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
            {
                DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
            }

            // The below method was replaced by GetDefaultOutputFullExtension
            // string GetDefaultOutputExtension(OutputType outputType);

            public string GetDefaultOutputFullExtension(Project.Configuration.OutputType outputType)
            {
                switch (outputType)
                {
                    case Project.Configuration.OutputType.Exe:
                        return ExecutableFileFullExtension;
                    case Project.Configuration.OutputType.Dll:
                        return SharedLibraryFileFullExtension;
                    default:
                        return StaticLibraryFileFullExtension;
                }
            }

            public string GetOutputFileNamePrefix(Project.Configuration.OutputType outputType)
            {
                switch (outputType)
                {
                    case Project.Configuration.OutputType.Exe:
                    case Project.Configuration.OutputType.Dll:
                    case Project.Configuration.OutputType.Lib:
                        return "lib";
                    default:
                        return string.Empty;
                }
            }

            public IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
            {
                yield break;
            }
            #endregion

            #region IPlatformVcxproj implementation
            public override string ProgramDatabaseFileFullExtension => string.Empty;
            public override string SharedLibraryFileFullExtension => ".so";
            public override string StaticLibraryFileFullExtension => ".a";
            public override string ExecutableFileFullExtension => ".so";

            public override void GeneratePlatformSpecificProjectDescription(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectStartPlatformConditional);

                string msBuildPathOverrides = string.Empty;

                // MSBuild override when mixing devenvs in the same vcxproj is not supported,
                // but before throwing an exception check if we have some override
                for (DevEnv devEnv = context.DevelopmentEnvironmentsRange.MinDevEnv; devEnv <= context.DevelopmentEnvironmentsRange.MaxDevEnv; devEnv = (DevEnv)((int)devEnv << 1))
                {
                    switch (devEnv)
                    {
                        case DevEnv.vs2017:
                        case DevEnv.vs2019:
                        case DevEnv.vs2022:
                            {
                                // _PlatformFolder override is not enough for android, we need to know the AdditionalVCTargetsPath
                                // Note that AdditionalVCTargetsPath is not officially supported by vs2017, but we use the variable anyway for convenience and consistency
                                if (!string.IsNullOrEmpty(MSBuildGlobalSettings.GetCppPlatformFolder(devEnv, Platform.agde)))
                                    throw new Error($"SetCppPlatformFolder is not supported by {devEnv}: use of MSBuildGlobalSettings.SetCppPlatformFolder should be replaced by use of MSBuildGlobalSettings.SetAdditionalVCTargetsPath.");

                                string additionalVCTargetsPath = MSBuildGlobalSettings.GetAdditionalVCTargetsPath(devEnv, Platform.agde);
                                if (!string.IsNullOrEmpty(additionalVCTargetsPath))
                                {
                                    using (generator.Declare("additionalVCTargetsPath", Sharpmake.Util.EnsureTrailingSeparator(additionalVCTargetsPath)))
                                        msBuildPathOverrides += generator.Resolver.Resolve(Vcxproj.Template.Project.AdditionalVCTargetsPath);
                                }
                            }
                            break;
                    }
                }

                var agdeConfOptions = context.ProjectConfigurationOptions.Where(d => d.Key.Platform == Platform.agde).Select(d => d.Value);

                using (generator.Declare("androidHome", Options.GetOptionValue("androidHome", agdeConfOptions)))
                using (generator.Declare("javaHome", Options.GetOptionValue("javaHome", agdeConfOptions)))
                using (generator.Declare("androidNdkVersion", Options.GetOptionValue("androidNdkVersion", agdeConfOptions)))
                using (generator.Declare("androidMinSdkVersion", Options.GetOptionValue("androidMinSdkVersion", agdeConfOptions)))
                using (generator.Declare("ndkRoot", Options.GetOptionValue("ndkRoot", agdeConfOptions)))
                {
                    generator.Write(_projectDescriptionPlatformSpecific);
                }

                if (!string.IsNullOrEmpty(msBuildPathOverrides))
                {
                    if (context.DevelopmentEnvironmentsRange.MinDevEnv != context.DevelopmentEnvironmentsRange.MaxDevEnv)
                        throw new Error("Different vs versions not supported in the same vcxproj");

                    generator.WriteVerbatim(msBuildPathOverrides);
                }

                generator.Write(Vcxproj.Template.Project.ProjectDescriptionEnd);
            }

            public override void GenerateProjectCompileVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectConfigurationsCompileTemplate);
            }

            public override void GenerateProjectConfigurationGeneral(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectConfigurationsGeneralTemplate);
            }

            public override void GenerateProjectConfigurationGeneral2(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectConfigurationsGeneral2Template);
            }

            public override void GenerateProjectConfigurationFastBuildMakeFile(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                base.GenerateProjectConfigurationFastBuildMakeFile(context, generator);
                generator.Write(_projectConfigurationsFastBuildMakefile);
            }

            protected override string GetProjectLinkSharedVcxprojTemplate()
            {
                return _projectConfigurationsSharedLinkTemplate;
            }

            protected override string GetProjectStaticLinkVcxprojTemplate()
            {
                return _projectConfigurationsStaticLinkTemplate;
            }

            public override void SetupSdkOptions(IGenerationContext context)
            {
                base.SetupSdkOptions(context);
                var conf = context.Configuration;
                var options = context.Options;

                options["androidHome"] = Options.PathOption.Get<Options.Android.General.AndroidHome>(conf, GlobalSettings.AndroidHome ?? RemoveLineTag, context.ProjectDirectoryCapitalized);
                options["ndkRoot"] = Options.PathOption.Get<Options.Android.General.NdkRoot>(conf, GlobalSettings.NdkRoot ?? RemoveLineTag, context.ProjectDirectoryCapitalized);
                options["javaHome"] = Options.PathOption.Get<Options.Android.General.JavaHome>(conf, GlobalSettings.JavaHome ?? RemoveLineTag, context.ProjectDirectoryCapitalized);

                string ndkRoot = options["ndkRoot"].Equals(RemoveLineTag) ? null : options["ndkRoot"];
                string ndkVer = Util.GetNdkVersion(ndkRoot);
                options["androidNdkVersion"] = ndkVer.Equals(string.Empty) ? RemoveLineTag : ndkVer;

                var sdkIncludePaths = GetSdkIncludePaths(context);
                options["IncludePath"] = sdkIncludePaths.JoinStrings(";");
            }

            public override void SelectCompilerOptions(IGenerationContext context)
            {
                base.SelectCompilerOptions(context);

                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                if (conf.Output.Equals(Project.Configuration.OutputType.Exe))
                {
                    options["AndroidEnablePackaging"] = "true";

                    context.SelectOption
                    (
                        Options.Option(Options.Agde.General.AndroidGradlePackaging.Enable, () => { options["SkipAndroidPackaging"] = "false"; }),
                        Options.Option(Options.Agde.General.AndroidGradlePackaging.Disable, () => { options["SkipAndroidPackaging"] = "true"; })
                    );

                    string option = Options.StringOption.Get<Options.Agde.General.AndroidApplicationModule>(conf);
                    options["AndroidApplicationModule"] = option != RemoveLineTag ? option : context.Project.Name.ToLowerInvariant();

                    options["AndroidGradleBuildDir"] = Options.PathOption.Get<Options.Agde.General.AndroidGradleBuildDir>(conf, @"$(SolutionDir)");
                    options["AndroidGradleBuildIntermediateDir"] = Options.PathOption.Get<Options.Agde.General.AndroidGradleBuildIntermediateDir>(conf);
                    options["AndroidExtraGradleArgs"] = Options.StringOption.Get<Options.Agde.General.AndroidExtraGradleArgs>(conf);

                    option = Options.StringOption.Get<Options.Agde.General.AndroidApkName>(conf);
                    options["AndroidApkName"] = option != RemoveLineTag ? option : @"$(RootNamespace)-$(PlatformTarget).apk";

                    option = Options.StringOption.Get<Options.Agde.General.AndroidGradlePackageOutputName>(conf);
                    options["AndroidGradlePackageOutputName"] = option != RemoveLineTag ? option : @"$(AndroidApkName)";

                    option = Options.GetObject<Options.Agde.General.AndroidApkLocation>(conf)?.Path ?? RemoveLineTag;
                    options["AndroidApkLocation"] = option;

                    option = Options.GetObject<Options.Agde.General.AndroidPostApkInstallCommands>(conf)?.Value ?? RemoveLineTag;
                    options["AndroidPostApkInstallCommands"] = option;

                    option = Options.GetObject<Options.Agde.General.AndroidPreApkInstallCommands>(conf)?.Value ?? RemoveLineTag;
                    options["AndroidPreApkInstallCommands"] = option;
                }
                else
                {
                    options["AndroidEnablePackaging"] = RemoveLineTag;
                    options["SkipAndroidPackaging"] = RemoveLineTag;
                    options["AndroidApplicationModule"] = RemoveLineTag;
                    options["AndroidGradleBuildDir"] = RemoveLineTag;
                    options["AndroidGradleBuildIntermediateDir"] = RemoveLineTag;
                    options["AndroidExtraGradleArgs"] = RemoveLineTag;
                    options["AndroidApkName"] = RemoveLineTag;
                    options["AndroidGradlePackageOutputName"] = RemoveLineTag;
                    options["AndroidApkLocation"] = RemoveLineTag;
                    options["AndroidPostApkInstallCommands"] = RemoveLineTag;
                    options["AndroidPreApkInstallCommands"] = RemoveLineTag;
                }

                context.SelectOption
                (
                Options.Option(Options.Android.General.AndroidAPILevel.Latest, () =>
                {
                    string lookupDirectory;
                    lookupDirectory = options["androidHome"] ?? options["ndkRoot"];

                    string androidApiLevel = RemoveLineTag;
                    if (lookupDirectory != RemoveLineTag)
                    {
                        androidApiLevel = Util.FindLatestApiLevelStringBySdk(lookupDirectory) ?? RemoveLineTag;
                    }
                    options["androidMinSdkVersion"] = androidApiLevel;
                }),
                Options.Option(Options.Android.General.AndroidAPILevel.Default, () => { options["androidMinSdkVersion"] = RemoveLineTag; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android16, () => { options["androidMinSdkVersion"] = "16"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android17, () => { options["androidMinSdkVersion"] = "17"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android18, () => { options["androidMinSdkVersion"] = "18"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android19, () => { options["androidMinSdkVersion"] = "19"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android20, () => { options["androidMinSdkVersion"] = "20"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android21, () => { options["androidMinSdkVersion"] = "21"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android22, () => { options["androidMinSdkVersion"] = "22"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android23, () => { options["androidMinSdkVersion"] = "23"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android24, () => { options["androidMinSdkVersion"] = "24"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android25, () => { options["androidMinSdkVersion"] = "25"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android26, () => { options["androidMinSdkVersion"] = "26"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android27, () => { options["androidMinSdkVersion"] = "27"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android28, () => { options["androidMinSdkVersion"] = "28"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android29, () => { options["androidMinSdkVersion"] = "29"; }),
                Options.Option(Options.Android.General.AndroidAPILevel.Android30, () => { options["androidMinSdkVersion"] = "30"; })
                );

                string androidApiNum = options["androidMinSdkVersion"];
                if (!androidApiNum.Equals(RemoveLineTag))
                {
                    if (int.TryParse(androidApiNum, out int apiValue))
                    {
                        androidApiNum = apiValue.ToString();
                    }
                    else
                    {
                        throw new Error("androidMinSdkVersion might be in wrong format!");
                    }

                    AndroidBuildTargets androidBuildtarget = Android.Util.GetAndroidBuildTarget(conf);
                    cmdLineOptions["ClangCompilerTarget"] = $"-target {Android.Util.GetTargetTripleWithVersionSuffix(androidBuildtarget, androidApiNum)}";
                }
                else
                {
                    cmdLineOptions["ClangCompilerTarget"] = RemoveLineTag;
                }

                context.SelectOptionWithFallback
                (
                () => throw new Error("Android AGDE doesn't support the current Options.Android.General.PlatformToolset"),
                Options.Option(Options.Android.General.PlatformToolset.Default, () => { options["PlatformToolset"] = RemoveLineTag; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.General.UseOfStl.GnuStl_Static, () => { options["UseOfStl"] = "gnustl_static"; cmdLineOptions["UseOfStl"] = RemoveLineTag; }),
                Options.Option(Options.Agde.General.UseOfStl.GnuStl_Shared, () => { options["UseOfStl"] = "gnustl_shared"; cmdLineOptions["UseOfStl"] = RemoveLineTag; }),
                Options.Option(Options.Agde.General.UseOfStl.LibCpp_Static, () => { options["UseOfStl"] = "cpp_static"; cmdLineOptions["UseOfStl"] = "-static-libstdc++"; }),
                Options.Option(Options.Agde.General.UseOfStl.LibCpp_Shared, () => { options["UseOfStl"] = "cpp_shared"; cmdLineOptions["UseOfStl"] = RemoveLineTag; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.General.LinkTimeOptimization.None, () => { options["LinkTimeOptimization"] = "None"; cmdLineOptions["LinkTimeOptimization"] = RemoveLineTag; }),
                Options.Option(Options.Agde.General.LinkTimeOptimization.LinkTimeOptimization, () => { options["LinkTimeOptimization"] = "LinkTimeOptimization"; cmdLineOptions["LinkTimeOptimization"] = "-flto"; }),
                Options.Option(Options.Agde.General.LinkTimeOptimization.ThinLinkTimeOptimization, () => { options["LinkTimeOptimization"] = "ThinLinkTimeOptimization"; cmdLineOptions["LinkTimeOptimization"] = "-flto=thin"; })
                );

                //Bff.Template.cs required this
                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Agde.Linker.UseThinArchives.Enable, () => { cmdLineOptions["UseThinArchives"] = "T"; }),
                Sharpmake.Options.Option(Options.Agde.Linker.UseThinArchives.Disable, () => { cmdLineOptions["UseThinArchives"] = ""; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.General.ClangLinkType.DeferToNdk, () => { options["ClangLinkType"] = "DeferToNdk"; cmdLineOptions["ClangLinkType"] = RemoveLineTag; }),
                Options.Option(Options.Agde.General.ClangLinkType.gold, () => { options["ClangLinkType"] = "gold"; cmdLineOptions["ClangLinkType"] = "-fuse-ld=gold"; }),
                Options.Option(Options.Agde.General.ClangLinkType.lld, () => { options["ClangLinkType"] = "lld"; cmdLineOptions["ClangLinkType"] = "-fuse-ld=lld"; }),
                Options.Option(Options.Agde.General.ClangLinkType.bfd, () => { options["ClangLinkType"] = "bfd"; cmdLineOptions["ClangLinkType"] = "-fuse-ld=bfd"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.General.ThumbMode.Thumb, () => { options["ThumbMode"] = "Thumb"; cmdLineOptions["ThumbMode"] = "-mthumb"; }),
                Options.Option(Options.Agde.General.ThumbMode.ARM, () => { options["ThumbMode"] = "ARM"; cmdLineOptions["ThumbMode"] = "-marm"; }),
                Options.Option(Options.Agde.General.ThumbMode.Disabled, () => { options["ThumbMode"] = "Disabled"; cmdLineOptions["ThumbMode"] = RemoveLineTag; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.General.WarningLevel.Default, () => { options["WarningLevel"] = "Default"; cmdLineOptions["WarningLevel"] = RemoveLineTag; }),
                Options.Option(Options.Agde.General.WarningLevel.TurnOffAllWarnings, () => { options["WarningLevel"] = "TurnOffAllWarnings"; cmdLineOptions["WarningLevel"] = "-w"; }),
                Options.Option(Options.Agde.General.WarningLevel.EnableFormatWarnings, () => { options["WarningLevel"] = "EnableFormatWarnings"; cmdLineOptions["WarningLevel"] = "-Wformat"; }),
                Options.Option(Options.Agde.General.WarningLevel.EnableFormatAndSecurityWarnings, () => { options["WarningLevel"] = "EnableFormatAndSecurityWarnings"; cmdLineOptions["WarningLevel"] = "-Wformat -Wsecurity"; }),
                Options.Option(Options.Agde.General.WarningLevel.EnableWarnings, () => { options["WarningLevel"] = "EnableWarnings"; cmdLineOptions["WarningLevel"] = "-Wall"; }),
                Options.Option(Options.Agde.General.WarningLevel.EnableExtraWarnings, () => { options["WarningLevel"] = "EnableExtraWarnings"; cmdLineOptions["WarningLevel"] = "-Wextra"; }),
                Options.Option(Options.Agde.General.WarningLevel.EnableAllWarnings, () => { options["WarningLevel"] = "EnableAllWarnings"; cmdLineOptions["WarningLevel"] = "-Weverything"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.CLanguageStandard.Default, () => { options["CLanguageStandard"] = "Default"; cmdLineOptions["CLanguageStd"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Compiler.CLanguageStandard.C89, () => { options["CLanguageStandard"] = "c89"; cmdLineOptions["CLanguageStd"] = "-std=c89"; }),
                Options.Option(Options.Agde.Compiler.CLanguageStandard.C99, () => { options["CLanguageStandard"] = "c99"; cmdLineOptions["CLanguageStd"] = "-std=c99"; }),
                Options.Option(Options.Agde.Compiler.CLanguageStandard.C11, () => { options["CLanguageStandard"] = "c11"; cmdLineOptions["CLanguageStd"] = "-std=c11"; }),
                Options.Option(Options.Agde.Compiler.CLanguageStandard.C17, () => { options["CLanguageStandard"] = "c17"; cmdLineOptions["CLanguageStd"] = "-std=c17"; }),
                Options.Option(Options.Agde.Compiler.CLanguageStandard.Gnu99, () => { options["CLanguageStandard"] = "gnu99"; cmdLineOptions["CLanguageStd"] = "-std=gnu99"; }),
                Options.Option(Options.Agde.Compiler.CLanguageStandard.Gnu11, () => { options["CLanguageStandard"] = "gnu11"; cmdLineOptions["CLanguageStd"] = "-std=gnu11"; }),
                Options.Option(Options.Agde.Compiler.CLanguageStandard.Gnu17, () => { options["CLanguageStandard"] = "gnu17"; cmdLineOptions["CLanguageStd"] = "-std=gnu17"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Default, () => { options["CppLanguageStandard"] = "Default"; cmdLineOptions["CppLanguageStd"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Cpp98, () => { options["CppLanguageStandard"] = "cpp98"; cmdLineOptions["CppLanguageStd"] = "-std=c++98"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Cpp03, () => { options["CppLanguageStandard"] = "cpp03"; cmdLineOptions["CppLanguageStd"] = "-std=c++03"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Cpp11, () => { options["CppLanguageStandard"] = "cpp11"; cmdLineOptions["CppLanguageStd"] = "-std=c++11"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Cpp14, () => { options["CppLanguageStandard"] = "cpp14"; cmdLineOptions["CppLanguageStd"] = "-std=c++14"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Cpp1z, () => { options["CppLanguageStandard"] = "cpp1z"; cmdLineOptions["CppLanguageStd"] = "-std=c++1z"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Cpp17, () => { options["CppLanguageStandard"] = "cpp17"; cmdLineOptions["CppLanguageStd"] = "-std=c++17"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Cpp20, () => { options["CppLanguageStandard"] = "cpp20"; cmdLineOptions["CppLanguageStd"] = "-std=c++20"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Gnupp98, () => { options["CppLanguageStandard"] = "gnupp98"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++98"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Gnupp03, () => { options["CppLanguageStandard"] = "gnupp03"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++03"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Gnupp11, () => { options["CppLanguageStandard"] = "gnupp11"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++11"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Gnupp14, () => { options["CppLanguageStandard"] = "gnupp14"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++14"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Gnupp1z, () => { options["CppLanguageStandard"] = "gnupp1z"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++1z"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Gnupp17, () => { options["CppLanguageStandard"] = "gnupp17"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++17"; }),
                Options.Option(Options.Agde.Compiler.CppLanguageStandard.Gnupp20, () => { options["CppLanguageStandard"] = "gnupp20"; cmdLineOptions["CppLanguageStd"] = "-std=gnu++20"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.DataLevelLinking.Disable, () => { options["EnableDataLevelLinking"] = "false"; cmdLineOptions["EnableDataLevelLinking"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Compiler.DataLevelLinking.Enable, () => { options["EnableDataLevelLinking"] = "true"; cmdLineOptions["EnableDataLevelLinking"] = "-fdata-sections"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.General.ClangDebugInformationFormat.None, () => { options["ClangDebugInformationFormat"] = "None"; cmdLineOptions["ClangDebugInformationFormat"] = "-g0"; }),
                Options.Option(Options.Agde.General.ClangDebugInformationFormat.FullDebug, () => { options["ClangDebugInformationFormat"] = "FullDebug"; cmdLineOptions["ClangDebugInformationFormat"] = "-g"; }),
                Options.Option(Options.Agde.General.ClangDebugInformationFormat.LineNumber, () => { options["ClangDebugInformationFormat"] = "LineNumber"; cmdLineOptions["ClangDebugInformationFormat"] = "-gline-tables-only"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.General.LimitDebugInfo.Enable, () => { options["LimitDebugInfo"] = "true"; cmdLineOptions["LimitDebugInfo"] = "-flimit-debug-info"; }),
                Options.Option(Options.Agde.General.LimitDebugInfo.Disable, () => { options["LimitDebugInfo"] = RemoveLineTag; cmdLineOptions["LimitDebugInfo"] = RemoveLineTag; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.ExceptionHandling.Disable, () => { options["ExceptionHandling"] = "Disabled"; cmdLineOptions["ExceptionHandling"] = "-fno-exceptions"; }),
                Options.Option(Options.Agde.Compiler.ExceptionHandling.Enable, () => { options["ExceptionHandling"] = "Enabled"; cmdLineOptions["ExceptionHandling"] = "-fexceptions"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.FloatABI.Default, () => { options["FloatABI"] = "Default"; cmdLineOptions["FloatABI"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Compiler.FloatABI.Soft, () => { options["FloatABI"] = "soft"; cmdLineOptions["FloatABI"] = "-mfloat-abi=soft"; }),
                Options.Option(Options.Agde.Compiler.FloatABI.Softfp, () => { options["FloatABI"] = "softfp"; cmdLineOptions["FloatABI"] = "-mfloat-abi=softfp"; }),
                Options.Option(Options.Agde.Compiler.FloatABI.Hard, () => { options["FloatABI"] = "hard"; cmdLineOptions["FloatABI"] = "-mfloat-abi=hard"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.UnwindTables.Enable, () => { context.Options["UnwindTables"] = "true"; context.CommandLineOptions["UnwindTables"] = "-funwind-tables"; }),
                Options.Option(Options.Agde.Compiler.UnwindTables.Disable, () => { context.Options["UnwindTables"] = "false"; context.CommandLineOptions["UnwindTables"] = "-fno-unwind-tables"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Vc.General.TreatWarningsAsErrors.Disable, () => { options["TreatWarningAsError"] = RemoveLineTag; cmdLineOptions["TreatWarningAsError"] = RemoveLineTag; }),
                Options.Option(Options.Vc.General.TreatWarningsAsErrors.Enable, () => { options["TreatWarningAsError"] = "true"; cmdLineOptions["TreatWarningAsError"] = "-Werror"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.StackProtectionLevel.None, () => { options["StackProtectionLevel"] = "None"; cmdLineOptions["StackProtectionLevel"] = "-fno-stack-protector"; }),
                Options.Option(Options.Agde.Compiler.StackProtectionLevel.Basic, () => { options["StackProtectionLevel"] = "Basic"; cmdLineOptions["StackProtectionLevel"] = "-fstack-protector"; }),
                Options.Option(Options.Agde.Compiler.StackProtectionLevel.Strong, () => { options["StackProtectionLevel"] = "Strong"; cmdLineOptions["StackProtectionLevel"] = "-fstack-protector-strong"; }),
                Options.Option(Options.Agde.Compiler.StackProtectionLevel.All, () => { options["StackProtectionLevel"] = "All"; cmdLineOptions["StackProtectionLevel"] = "-fstack-protector-all"; }),
                Options.Option(Options.Agde.Compiler.StackProtectionLevel.Default, () => { options["StackProtectionLevel"] = "Default"; cmdLineOptions["StackProtectionLevel"] = RemoveLineTag; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.FunctionLevelLinking.Disable, () => { options["EnableFunctionLevelLinking"] = "false"; cmdLineOptions["EnableFunctionLevelLinking"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Compiler.FunctionLevelLinking.Enable, () => { options["EnableFunctionLevelLinking"] = "true"; cmdLineOptions["EnableFunctionLevelLinking"] = "-ffunction-sections"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.OmitFramePointers.Disable, () => { options["OmitFramePointers"] = "false"; cmdLineOptions["OmitFramePointers"] = "-fno-omit-frame-pointer"; }),
                Options.Option(Options.Agde.Compiler.OmitFramePointers.Enable, () => { options["OmitFramePointers"] = "true"; cmdLineOptions["OmitFramePointers"] = "-fomit-frame-pointer"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.Optimization.Custom, () => { options["Optimization"] = "Custom"; cmdLineOptions["Optimization"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Compiler.Optimization.Disabled, () => { options["Optimization"] = "Disabled"; cmdLineOptions["Optimization"] = "-O0"; }),
                Options.Option(Options.Agde.Compiler.Optimization.MinSize, () => { options["Optimization"] = "MinSize"; cmdLineOptions["Optimization"] = "-Os"; }),
                Options.Option(Options.Agde.Compiler.Optimization.MaxSpeed, () => { options["Optimization"] = "MaxSpeed"; cmdLineOptions["Optimization"] = "-O2"; }),
                Options.Option(Options.Agde.Compiler.Optimization.Full, () => { options["Optimization"] = "Full"; cmdLineOptions["Optimization"] = "-O3"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.NativeBuildBackend.MultiToolTaskMSBuild, () => { options["NativeBuildBackend"] = "MultiToolTaskMSBuild"; }),
                Options.Option(Options.Agde.Compiler.NativeBuildBackend.OriginalMSBuild, () => { options["NativeBuildBackend"] = "OriginalMSBuild"; }),
                Options.Option(Options.Agde.Compiler.NativeBuildBackend.Ninja, () => { options["NativeBuildBackend"] = "Ninja"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Vc.Compiler.RTTI.Disable, () => { options["RuntimeTypeInfo"] = "false"; cmdLineOptions["RuntimeTypeInfo"] = "-fno-rtti"; }),
                Options.Option(Options.Vc.Compiler.RTTI.Enable, () => { options["RuntimeTypeInfo"] = "true"; cmdLineOptions["RuntimeTypeInfo"] = "-frtti"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.AddressSignificanceTable.Enable, () => { options["AddressSignificanceTable"] = "true"; cmdLineOptions["AddressSignificanceTable"] = "-faddrsig"; }),
                Options.Option(Options.Agde.Compiler.AddressSignificanceTable.Disable, () => { options["AddressSignificanceTable"] = "false"; cmdLineOptions["AddressSignificanceTable"] = "-fno-addrsig"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.ClangDiagnosticsFormat.Default, () => { options["ClangDiagnosticsFormat"] = "Default"; cmdLineOptions["ClangDiagnosticsFormat"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Compiler.ClangDiagnosticsFormat.MSVC, () => { options["ClangDiagnosticsFormat"] = "MSVC"; cmdLineOptions["ClangDiagnosticsFormat"] = "-fdiagnostics-format=msvc"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Compiler.PositionIndependentCode.Disable, () => { options["PositionIndependentCode"] = "false"; cmdLineOptions["PositionIndependentCode"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Compiler.PositionIndependentCode.Enable, () => { options["PositionIndependentCode"] = "true"; cmdLineOptions["PositionIndependentCode"] = "-fpic"; })
                );
            }

            public override void SelectPrecompiledHeaderOptions(IGenerationContext context)
            {
                base.SelectPrecompiledHeaderOptions(context);

                var options = context.Options;
                if (options["UsePrecompiledHeader"] == "NotUsing")
                {
                    options["UsePrecompiledHeader"] = FileGeneratorUtilities.RemoveLineTag;
                }
                else
                {
                    options["PrecompiledHeaderThrough"] = Path.GetFileName(options["PrecompiledHeaderThrough"]);
                    options["PrecompiledHeaderOutputFileDirectory"] = Sharpmake.Util.EnsureTrailingSeparator(context.Configuration.IntermediatePath);
                }
            }

            public override void SelectLinkerOptions(IGenerationContext context)
            {
                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                cmdLineOptions["GenerateSharedObject"] = (conf.Output == Project.Configuration.OutputType.Exe) ? "-shared" : RemoveLineTag;

                string linkerOptionPrefix = conf.Platform.GetLinkerOptionPrefix();

                context.SelectOption
                (
                Options.Option(Options.Agde.Linker.DebuggerSymbolInformation.IncludeAll, () => { options["DebuggerSymbolInformation"] = "true"; cmdLineOptions["DebuggerSymbolInformation"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Linker.DebuggerSymbolInformation.OmitUnneededSymbolInformation, () => { options["DebuggerSymbolInformation"] = "OmitUnneededSymbolInformation"; cmdLineOptions["DebuggerSymbolInformation"] = $"{linkerOptionPrefix}--strip-unneeded"; }),
                Options.Option(Options.Agde.Linker.DebuggerSymbolInformation.OmitDebuggerSymbolInformation, () => { options["DebuggerSymbolInformation"] = "OmitDebuggerSymbolInformation"; cmdLineOptions["DebuggerSymbolInformation"] = $"{linkerOptionPrefix}--strip-debug"; }),
                Options.Option(Options.Agde.Linker.DebuggerSymbolInformation.OmitAllSymbolInformation, () => { options["DebuggerSymbolInformation"] = "OmitAllSymbolInformation"; cmdLineOptions["DebuggerSymbolInformation"] = $"{linkerOptionPrefix}--strip-all"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Linker.EnableImmediateFunctionBinding.No, () => { options["FunctionBinding"] = "false"; cmdLineOptions["FunctionBinding"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Linker.EnableImmediateFunctionBinding.Yes, () => { options["FunctionBinding"] = "true"; cmdLineOptions["FunctionBinding"] = $"{linkerOptionPrefix}-z,now"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Linker.ExecutableStackRequired.No, () => { options["NoExecStackRequired"] = "true"; cmdLineOptions["NoExecStackRequired"] = $"{linkerOptionPrefix}-z,noexecstack"; }),
                Options.Option(Options.Agde.Linker.ExecutableStackRequired.Yes, () => { options["NoExecStackRequired"] = "false"; cmdLineOptions["NoExecStackRequired"] = RemoveLineTag; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Linker.ReportUnresolvedSymbolReference.No, () => { options["UnresolvedSymbolReferences"] = "false"; cmdLineOptions["UnresolvedSymbolReferences"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Linker.ReportUnresolvedSymbolReference.Yes, () => { options["UnresolvedSymbolReferences"] = "true"; cmdLineOptions["UnresolvedSymbolReferences"] = $"{linkerOptionPrefix}--no-undefined"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Linker.VariableReadOnlyAfterRelocation.No, () => { options["Relocation"] = "false"; cmdLineOptions["Relocation"] = $"{linkerOptionPrefix}-z,norelro"; }),
                Options.Option(Options.Agde.Linker.VariableReadOnlyAfterRelocation.Yes, () => { options["Relocation"] = "true"; cmdLineOptions["Relocation"] = $"{linkerOptionPrefix}-z,relro"; })
                );

                context.SelectOption
                (
                Options.Option(Options.Agde.Linker.Incremental.Disable, () => { options["IncrementalLink"] = "false"; cmdLineOptions["LinkIncremental"] = RemoveLineTag; }),
                Options.Option(Options.Agde.Linker.Incremental.Enable, () => { options["IncrementalLink"] = "true"; cmdLineOptions["LinkIncremental"] = $"{linkerOptionPrefix}--incremental"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Agde.Linker.BuildId.None, () => { options["BuildId"] = "none"; cmdLineOptions["BuildId"] = RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Agde.Linker.BuildId.Fast, () => { options["BuildId"] = "fast"; cmdLineOptions["BuildId"] = $"{linkerOptionPrefix}--build-id=fast"; }),
                Sharpmake.Options.Option(Options.Agde.Linker.BuildId.Md5, () => { options["BuildId"] = "md5"; cmdLineOptions["BuildId"] = $"{linkerOptionPrefix}--build-id=md5"; }),
                Sharpmake.Options.Option(Options.Agde.Linker.BuildId.Sha1, () => { options["BuildId"] = "sha1"; cmdLineOptions["BuildId"] = $"{linkerOptionPrefix}--build-id=sha1"; }),
                Sharpmake.Options.Option(Options.Agde.Linker.BuildId.Uuid, () => { options["BuildId"] = "uuid"; cmdLineOptions["BuildId"] = $"{linkerOptionPrefix}--build-id=uuid"; })
                );
            }

            public override void SelectPlatformAdditionalDependenciesOptions(IGenerationContext context)
            {
                // the static libs must be prefixed with -l: in the additional dependencies field in VS
                // the dynamic libs must not use the -l: prefix, otherwise AGDE fails to look up and copy it (to the package sources)
                var additionalDependencies = context.Options["AdditionalDependencies"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                context.Options["AdditionalDependencies"] = string.Join(";", additionalDependencies.Select(name =>
                {
                    if (name.EndsWith(SharedLibraryFileFullExtension, StringComparison.InvariantCultureIgnoreCase))
                    {
                        string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
                        if (nameWithoutExtension.StartsWith("lib"))
                            nameWithoutExtension = nameWithoutExtension.Remove(0, 3);
                        return "-l" + nameWithoutExtension;
                    }
                    return "-l:" + name;
                }
                ));
            }

            public override void SetupPlatformLibraryOptions(out string platformLibExtension, out string platformOutputLibExtension, out string platformPrefixExtension, out string platformLibPrefix)
            {
                platformLibExtension = ".a";
                platformOutputLibExtension = StaticLibraryFileFullExtension;
                platformPrefixExtension = "-l:";
                platformLibPrefix = "lib";
            }

            protected override IEnumerable<string> GetIncludePathsImpl(IGenerationContext context)
            {
                var includePaths = new OrderableStrings();
                includePaths.AddRange(context.Configuration.IncludePrivatePaths);
                includePaths.AddRange(context.Configuration.IncludePaths);
                includePaths.AddRange(context.Configuration.DependenciesIncludePaths);

                includePaths.Sort();
                return includePaths;
            }

            protected override IEnumerable<IncludeWithPrefix> GetPlatformIncludePathsWithPrefixImpl(IGenerationContext context)
            {
                var systemIncludes = new OrderableStrings();
                systemIncludes.AddRange(context.Configuration.DependenciesIncludeSystemPaths);
                systemIncludes.AddRange(context.Configuration.IncludeSystemPaths);

                systemIncludes.Sort();

                const string cmdLineIncludePrefix = "-isystem";
                return systemIncludes.Select(path => new IncludeWithPrefix(cmdLineIncludePrefix, path));
            }

            public override IEnumerable<string> GetLibraryPaths(IGenerationContext context)
            {
                var dirs = new List<string>();
                if (!context.Configuration.IsFastBuild)
                {
                    dirs.Add(@"$(StlLibraryPath)");
                }
                dirs.AddRange(base.GetLibraryPaths(context));

                return dirs;
            }

            public override void SelectPreprocessorDefinitionsVcxproj(IVcxprojGenerationContext context)
            {
                // concat defines, don't add options.Defines since they are automatically added by VS
                var defines = new Strings();
                defines.AddRange(context.Options.ExplicitDefines);
                defines.AddRange(context.Configuration.Defines);

                context.Options["PreprocessorDefinitions"] = defines.JoinStrings(";");
            }

            public override bool HasPrecomp(IGenerationContext context)
            {
                return !string.IsNullOrEmpty(context.Configuration.PrecompHeader);
            }

            private Strings GetSdkIncludePaths(IGenerationContext context)
            {
                return new Strings(@"$(AndroidNdkDirectory)\sources\android");
            }

            #endregion // IPlatformVcxproj implementation

            #region IClangPlatformBff implementation

            public override string CConfigName(Configuration conf)
            {
                var buildTarget = conf.Target.GetFragment<AndroidBuildTargets>();
                switch (buildTarget)
                {
                    case AndroidBuildTargets.armeabi_v7a:
                        return ".androidArmCConfig";
                    case AndroidBuildTargets.arm64_v8a:
                        return ".androidArm64CConfig";
                    case AndroidBuildTargets.x86:
                        return ".androidX86CConfig";
                    case AndroidBuildTargets.x86_64:
                        return ".androidX64CConfig";
                    default:
                        throw new Error(string.Format("Unsupported Android architecture: {0}", buildTarget));
                }
            }

            public override string CppConfigName(Configuration conf)
            {
                var buildTarget = conf.Target.GetFragment<AndroidBuildTargets>();
                switch (buildTarget)
                {
                    case AndroidBuildTargets.armeabi_v7a:
                        return ".androidArmCppConfig";
                    case AndroidBuildTargets.arm64_v8a:
                        return ".androidArm64CppConfig";
                    case AndroidBuildTargets.x86:
                        return ".androidX86CppConfig";
                    case AndroidBuildTargets.x86_64:
                        return ".androidX64CppConfig";
                    default:
                        throw new Error(string.Format("Unsupported Android architecture: {0}", buildTarget));
                }
            }

            public override EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] parameters)
            {
                return new EnvironmentVariableResolver(parameters);
            }

            public void SetupClangOptions(IFileGenerator generator)
            {
                generator.Write(_compilerExtraOptionsTemplate);
                generator.Write(_compilerOptimizationOptionsTemplate);
            }

            public override void AddCompilerSettings(IDictionary<string, CompilerSettings> masterCompilerSettings, Project.Configuration conf)
            {
                var projectRootPath = conf.Project.RootPath;
                var target = conf.Target;
                var devEnv = target.GetFragment<DevEnv>();

                string compilerName = string.Join("-", "Compiler", GetToolchainPlatformString(target), devEnv, SimplePlatformString);
                string CompilerSettingsName = compilerName;
                string CCompilerSettingsName = $"C-{compilerName}";

                // For CPP
                CompilerSettings compilerSettings = GetMasterCompilerSettings(masterCompilerSettings, conf, CompilerSettingsName, devEnv, false);
                SetConfiguration(compilerSettings.Configurations, conf, false);
                // For C
                CompilerSettings CcompilerSettings = GetMasterCompilerSettings(masterCompilerSettings, conf, CCompilerSettingsName, devEnv, true);
                SetConfiguration(CcompilerSettings.Configurations, conf, true);
            }

            public override string BffPlatformDefine => "_ANDROID";

            public override void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration outputType, string fastBuildOutputFile)
            {
                fileGenerator.Write(_linkerOptionsTemplate);
            }

            private CompilerSettings GetMasterCompilerSettings(IDictionary<string, CompilerSettings> masterCompilerSettings, Project.Configuration conf, string compilerName, DevEnv devEnv, bool useCCompiler)
            {
                CompilerSettings compilerSettings;

                if (masterCompilerSettings.ContainsKey(compilerName))
                {
                    compilerSettings = masterCompilerSettings[compilerName];
                }
                else
                {
                    var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.agde);

                    Strings extraFiles = new Strings();
                    string executable = useCCompiler ? Path.Combine("$ExecutableRootPath$", "clang.exe") : Path.Combine("$ExecutableRootPath$", "clang++.exe");
                    string ndkRoot = Options.PathOption.Get<Options.Android.General.NdkRoot>(conf, GlobalSettings.NdkRoot);
                    string rootPath = Path.Combine(ndkRoot, "toolchains", Android.Util.GetPrebuildToolchainString(), "prebuilt", Android.Util.GetHostTag(), "bin");

                    var compilerFamily = Sharpmake.CompilerFamily.Clang;
                    var compilerFamilyKey = new FastBuildCompilerKey(devEnv);
                    if (!fastBuildSettings.CompilerFamily.TryGetValue(compilerFamilyKey, out compilerFamily))
                        compilerFamily = Sharpmake.CompilerFamily.Clang;

                    extraFiles.Add(
                            @"$ExecutableRootPath$\libwinpthread-1.dll"
                        );

                    compilerSettings = new CompilerSettings(compilerName, compilerFamily, Platform.android, extraFiles, executable, rootPath, devEnv, new Dictionary<string, CompilerSettings.Configuration>());
                    masterCompilerSettings.Add(compilerName, compilerSettings);
                }

                return compilerSettings;
            }

            private void SetConfiguration(IDictionary<string, CompilerSettings.Configuration> configurations, Project.Configuration conf, bool useCCompiler = false)
            {
                string configName = useCCompiler ? CConfigName(conf) : CppConfigName(conf);

                if (!configurations.ContainsKey(configName))
                {
                    AndroidBuildTargets androidBuildtarget = Android.Util.GetAndroidBuildTarget(conf);

                    string linkerExecutable = useCCompiler ? "clang.exe" : "clang++.exe";

                    string ndkPath = Options.PathOption.Get<Options.Android.General.NdkRoot>(conf, GlobalSettings.NdkRoot);
                    string ndkVersionString = Util.GetNdkVersion(ndkPath);
                    // GNU Binutils remains available up to and including r22. All binutils tools with the exception of the assembler (GAS) were removed in r23. GAS was removed in r24.
                    // Above, we need to use LLVM utils, located <NDK>/toolchains/llvm/prebuilt/<host-tag>/bin/llvm-<tool>
                    // cf. https://android.googlesource.com/platform/ndk/+/master/docs/BuildSystemMaintainers.md#binutils
                    string librarian = Path.Combine("bin", "llvm-ar.exe");
                    if (int.TryParse(ndkVersionString, out int ndkVersion) && ndkVersion <= 22)
                        librarian = Path.Combine(Util.GetTargetTriple(androidBuildtarget), "bin", "ar.exe");

                    configurations.Add(configName,
                        new CompilerSettings.Configuration(
                            Platform.android,
                            binPath: Path.Combine(ndkPath, "toolchains", Android.Util.GetPrebuildToolchainString(), "prebuilt", Android.Util.GetHostTag()),
                            librarian: Path.Combine("$BinPath$", librarian),
                            linker: Path.Combine("$BinPath$", "bin", linkerExecutable),
                            // Using clang-orbis to get a correctly escaped response file when the command-line is too long.
                            fastBuildLinkerType: CompilerSettings.LinkerType.ClangOrbis
                        )
                        {
                        }
                    );
                }
            }
            #endregion
        }
    }
}
