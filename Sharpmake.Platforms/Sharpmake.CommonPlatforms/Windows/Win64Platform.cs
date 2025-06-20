﻿// Copyright (c) Ubisoft. All Rights Reserved.
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
    public static partial class Windows
    {
        [PlatformImplementation(Platform.win64,
            typeof(IPlatformDescriptor),
            typeof(Project.Configuration.IConfigurationTasks),
            typeof(IFastBuildCompilerSettings),
            typeof(IWindowsFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IMicrosoftPlatformBff),
            typeof(IPlatformVcxproj))]
        public sealed class Win64Platform : BaseWindowsPlatform
        {
            #region IPlatformDescriptor implementation
            public override string SimplePlatformString => "Win64";
            public override string GetToolchainPlatformString(ITarget target) => "x64";
            #endregion

            #region IMicrosoftPlatformBff implementation
            public override string BffPlatformDefine => "WIN64";
            public override bool SupportsResourceFiles => true;

            public override string CConfigName(Configuration conf)
            {
                var platformToolset = Options.GetObject<Options.Vc.General.PlatformToolset>(conf);
                string platformToolsetString = string.Empty;
                if (platformToolset != Options.Vc.General.PlatformToolset.Default && !platformToolset.IsDefaultToolsetForDevEnv(conf.Target.GetFragment<DevEnv>()))
                    platformToolsetString = $"_{platformToolset}";

                string lldString = string.Empty;
                var useLldLink = Options.GetObject<Options.Vc.LLVM.UseLldLink>(conf);
                if (useLldLink == Options.Vc.LLVM.UseLldLink.Enable ||
                   (useLldLink == Options.Vc.LLVM.UseLldLink.Default && platformToolset.IsLLVMToolchain()))
                {
                    lldString = "_LLD";
                }

                if (platformToolset.IsLLVMToolchain())
                {
                    Options.Vc.General.PlatformToolset overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
                    if (Options.WithArgOption<Options.Vc.General.PlatformToolset>.Get<Options.Clang.Compiler.LLVMVcPlatformToolset>(conf, ref overridenPlatformToolset))
                        platformToolsetString += $"_{overridenPlatformToolset}";
                }

                return $".win64{platformToolsetString}{lldString}Config";
            }

            public override string CppConfigName(Configuration conf)
            {
                return CConfigName(conf);
            }

            public override void AddCompilerSettings(
                IDictionary<string, CompilerSettings> masterCompilerSettings,
                Project.Configuration conf
            )
            {
                var projectRootPath = conf.Project.RootPath;
                var devEnv = conf.Target.GetFragment<DevEnv>();
                var platform = conf.Target.GetPlatform();

                string compilerName = "Compiler-" + Util.GetToolchainPlatformString(platform, conf.Target);

                var platformToolset = Options.GetObject<Options.Vc.General.PlatformToolset>(conf);
                if (platformToolset.IsLLVMToolchain())
                {
                    var useClangCl = Options.GetObject<Options.Vc.LLVM.UseClangCl>(conf);

                    // Use default platformToolset to get MS compiler instead of Clang, when ClangCl is disabled
                    if (useClangCl == Options.Vc.LLVM.UseClangCl.Disable)
                    {
                        Options.Vc.General.PlatformToolset overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
                        if (Options.WithArgOption<Options.Vc.General.PlatformToolset>.Get<Options.Clang.Compiler.LLVMVcPlatformToolset>(conf, ref overridenPlatformToolset))
                            platformToolset = overridenPlatformToolset;
                        else
                            platformToolset = Options.Vc.General.PlatformToolset.Default;
                    }
                }

                if (platformToolset != Options.Vc.General.PlatformToolset.Default && !platformToolset.IsDefaultToolsetForDevEnv(devEnv))
                    compilerName += "-" + platformToolset;
                else
                    compilerName += "-" + devEnv;

                CompilerSettings compilerSettings = GetMasterCompilerSettings(masterCompilerSettings, compilerName, devEnv, projectRootPath, platformToolset, false);
                compilerSettings.PlatformFlags |= Platform.win64;
                SetConfiguration(conf, compilerSettings.Configurations, CppConfigName(conf), projectRootPath, devEnv, false);
            }

            public CompilerSettings GetMasterCompilerSettings(
                IDictionary<string, CompilerSettings> masterCompilerSettings,
                string compilerName,
                DevEnv devEnv,
                string projectRootPath,
                Options.Vc.General.PlatformToolset platformToolset,
                bool useCCompiler
            )
            {
                CompilerSettings compilerSettings;

                if (masterCompilerSettings.ContainsKey(compilerName))
                {
                    compilerSettings = masterCompilerSettings[compilerName];
                }
                else
                {
                    DevEnv? compilerDevEnv = null;
                    string platformToolSetPath = null;
                    string pathToCompiler = null;
                    string compilerExeName = null;
                    var compilerFamily = Sharpmake.CompilerFamily.Auto;
                    var fastBuildSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.win64);

                    switch (platformToolset)
                    {
                        case Options.Vc.General.PlatformToolset.Default:
                            compilerDevEnv = devEnv;
                            break;
                        case Options.Vc.General.PlatformToolset.LLVM:
                        case Options.Vc.General.PlatformToolset.ClangCL:

                            platformToolSetPath = platformToolset == Options.Vc.General.PlatformToolset.ClangCL ? ClangForWindows.Settings.LLVMInstallDirVsEmbedded(devEnv) : ClangForWindows.Settings.LLVMInstallDir;
                            pathToCompiler = Path.Combine(platformToolSetPath, "bin");
                            compilerExeName = "clang-cl.exe";

                            var compilerFamilyKey = new FastBuildWindowsCompilerFamilyKey(devEnv, platformToolset);
                            if (!fastBuildSettings.CompilerFamily.TryGetValue(compilerFamilyKey, out compilerFamily))
                                compilerFamily = Sharpmake.CompilerFamily.ClangCl;

                            break;
                        default:
                            compilerDevEnv = platformToolset.GetDefaultDevEnvForToolset();
                            break;
                    }

                    if (compilerDevEnv.HasValue)
                    {
                        platformToolSetPath = Path.Combine(compilerDevEnv.Value.GetVisualStudioDir(), "VC");
                        pathToCompiler = compilerDevEnv.Value.GetVisualStudioBinPath(Platform.win64);
                        compilerExeName = "cl.exe";

                        var compilerFamilyKey = new FastBuildWindowsCompilerFamilyKey(devEnv, platformToolset);
                        if (!fastBuildSettings.CompilerFamily.TryGetValue(compilerFamilyKey, out compilerFamily))
                            compilerFamily = Sharpmake.CompilerFamily.MSVC;
                    }

                    Strings extraFiles = new Strings();
                    {
                        Strings userExtraFiles;
                        if (fastBuildSettings.ExtraFiles.TryGetValue(devEnv, out userExtraFiles))
                            extraFiles.AddRange(userExtraFiles);
                    }

                    if (compilerDevEnv.HasValue)
                    {
                        extraFiles.Add(
                            @"$ExecutableRootPath$\c1.dll",
                            @"$ExecutableRootPath$\c1xx.dll",
                            @"$ExecutableRootPath$\c2.dll",
                            @"$ExecutableRootPath$\mspdbcore.dll",
                            @"$ExecutableRootPath$\mspdbsrv.exe",
                            @"$ExecutableRootPath$\1033\clui.dll"
                        );

                        if (compilerDevEnv.Value.IsVisualStudio())
                        {
                            string systemDllPath = FastBuildSettings.SystemDllRoot;
                            if (systemDllPath == null)
                            {
                                var windowsTargetPlatformVersion = KitsRootPaths.GetWindowsTargetPlatformVersionForDevEnv(compilerDevEnv.Value);
                                string redistDirectory;
                                if (windowsTargetPlatformVersion <= Options.Vc.General.WindowsTargetPlatformVersion.v10_0_17134_0)
                                    redistDirectory = @"Redist\ucrt\DLLs\x64\";
                                else
                                    redistDirectory = $@"Redist\{windowsTargetPlatformVersion.ToVersionString()}\ucrt\DLLs\x64\";

                                systemDllPath = Path.Combine(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot10), redistDirectory);
                            }

                            if (!Path.IsPathRooted(systemDllPath))
                                systemDllPath = Util.SimplifyPath(Path.Combine(projectRootPath, systemDllPath));

                            extraFiles.Add(
                                @"$ExecutableRootPath$\msobj140.dll",
                                @"$ExecutableRootPath$\mspft140.dll",
                                @"$ExecutableRootPath$\mspdb140.dll"
                            );

                            if (compilerDevEnv.Value == DevEnv.vs2015)
                            {
                                extraFiles.Add(
                                    @"$ExecutableRootPath$\vcvars64.bat",
                                    Path.Combine(platformToolSetPath, @"redist\x64\Microsoft.VC140.CRT\concrt140.dll"),
                                    Path.Combine(platformToolSetPath, @"redist\x64\Microsoft.VC140.CRT\msvcp140.dll"),
                                    Path.Combine(platformToolSetPath, @"redist\x64\Microsoft.VC140.CRT\vccorlib140.dll"),
                                    Path.Combine(platformToolSetPath, @"redist\x64\Microsoft.VC140.CRT\vcruntime140.dll"),
                                    Path.Combine(systemDllPath, "ucrtbase.dll")
                                );
                            }
                            else
                            {
                                extraFiles.Add(
                                    @"$ExecutableRootPath$\mspdbcore.dll",
                                    @"$ExecutableRootPath$\msvcdis140.dll",
                                    @"$ExecutableRootPath$\msvcp140.dll",
                                    @"$ExecutableRootPath$\pgodb140.dll",
                                    @"$ExecutableRootPath$\vcruntime140.dll",
                                    Path.Combine(platformToolSetPath, @"Auxiliary\Build\vcvars64.bat")
                                );
                            }

                            if (compilerDevEnv.Value >= DevEnv.vs2019)
                            {
                                Version toolsVersion = compilerDevEnv.Value.GetVisualStudioVCToolsVersion();

                                if (toolsVersion >= new Version("14.22.27905")) // 16.3.2
                                    extraFiles.Add(@"$ExecutableRootPath$\tbbmalloc.dll");

                                if (toolsVersion >= new Version("14.25.28610")) // 16.5
                                    extraFiles.Add(@"$ExecutableRootPath$\vcruntime140_1.dll");

                                if (toolsVersion >= new Version("14.28.29333")) // 16.8
                                    extraFiles.Add(@"$ExecutableRootPath$\msvcp140_atomic_wait.dll");
                            }

                            try
                            {
                                foreach (string p in Util.DirectoryGetFiles(systemDllPath, "api-ms-win-*.dll"))
                                    extraFiles.Add(p);
                            }
                            catch { }
                        }
                        else
                        {
                            throw new NotImplementedException("This devEnv (" + compilerDevEnv.Value + ") is not supported!");
                        }
                    }

                    string executable = Path.Combine("$ExecutableRootPath$", compilerExeName);

                    compilerSettings = new CompilerSettings(compilerName, compilerFamily, Platform.win64, extraFiles, executable, pathToCompiler, devEnv, new Dictionary<string, CompilerSettings.Configuration>());
                    masterCompilerSettings.Add(compilerName, compilerSettings);
                }

                return compilerSettings;
            }

            private void SetConfiguration(
                Project.Configuration conf,
                IDictionary<string, CompilerSettings.Configuration> configurations,
                string configName,
                string projectRootPath,
                DevEnv devEnv,
                bool useCCompiler)
            {
                if (configurations.ContainsKey(configName))
                    return;

                string linkerPathOverride = null;
                string linkerExeOverride = null;
                string librarianExeOverride = null;
                GetLinkerExecutableInfo(conf, out linkerPathOverride, out linkerExeOverride, out librarianExeOverride);

                var fastBuildCompilerSettings = PlatformRegistry.Get<IWindowsFastBuildCompilerSettings>(Platform.win64);
                string binPath;
                if (!fastBuildCompilerSettings.BinPath.TryGetValue(devEnv, out binPath))
                    binPath = devEnv.GetVisualStudioBinPath(Platform.win64);

                string linkerPath;
                if (!string.IsNullOrEmpty(linkerPathOverride))
                    linkerPath = linkerPathOverride;
                else if (!fastBuildCompilerSettings.LinkerPath.TryGetValue(devEnv, out linkerPath))
                    linkerPath = binPath;

                string linkerExe;
                if (!string.IsNullOrEmpty(linkerExeOverride))
                    linkerExe = linkerExeOverride;
                else if (!fastBuildCompilerSettings.LinkerExe.TryGetValue(devEnv, out linkerExe))
                    linkerExe = "link.exe";

                string librarianExe;
                if (!string.IsNullOrEmpty(librarianExeOverride))
                    librarianExe = librarianExeOverride;
                else if (!fastBuildCompilerSettings.LibrarianExe.TryGetValue(devEnv, out librarianExe))
                    librarianExe = "lib.exe";

                string resCompiler;
                if (!fastBuildCompilerSettings.ResCompiler.TryGetValue(devEnv, out resCompiler))
                    resCompiler = devEnv.GetWindowsResourceCompiler(Platform.win64);

                string capitalizedBinPath = Util.GetCapitalizedPath(Util.PathGetAbsolute(projectRootPath, binPath));
                configurations.Add(
                    configName,
                    new CompilerSettings.Configuration(
                        Platform.win64,
                        binPath: capitalizedBinPath,
                        linkerPath: Util.GetCapitalizedPath(Util.PathGetAbsolute(projectRootPath, linkerPath)),
                        resourceCompiler: Util.GetCapitalizedPath(Util.PathGetAbsolute(projectRootPath, resCompiler)),
                        librarian: Path.Combine(@"$LinkerPath$", librarianExe),
                        linker: Path.Combine(@"$LinkerPath$", linkerExe)
                    )
                );

                string masmConfigurationName = configName + "Masm";
                var masmConfiguration = new CompilerSettings.Configuration(
                    Platform.win64,
                    compiler: "ML" + masmConfigurationName,
                    usingOtherConfiguration: configName
                );
                masmConfiguration.Masm = Path.Combine(capitalizedBinPath, "ml64.exe");

                configurations.Add(
                    masmConfigurationName,
                    masmConfiguration
                );

                string nasmConfigurationName = configName + "Nasm";
                var nasmConfiguration = new CompilerSettings.Configuration(
                    Platform.win64,
                    compiler: "Nasm" + nasmConfigurationName,
                    usingOtherConfiguration: configName
                );
                nasmConfiguration.Nasm = conf.Project.NasmExePath;

                configurations.Add(
                    nasmConfigurationName,
                    nasmConfiguration
                );
            }
            #endregion

            #region IPlatformVcxproj implementation
            public override bool HasEditAndContinueDebuggingSupport => true;
            public override IEnumerable<string> GetImplicitlyDefinedSymbols(IGenerationContext context)
            {
                var defines = new List<string>();
                defines.AddRange(base.GetImplicitlyDefinedSymbols(context));
                defines.Add("WIN64");

                return defines;
            }

            public override void SetupPlatformTargetOptions(IGenerationContext context)
            {
                context.Options["TargetMachine"] = "MachineX64";
                context.CommandLineOptions["TargetMachine"] = "/MACHINE:X64";
                context.CommandLineOptions["NasmCompilerFormat"] = "-fwin64";
            }

            public override void SelectPlatformAdditionalDependenciesOptions(IGenerationContext context)
            {
                base.SelectPlatformAdditionalDependenciesOptions(context);
                context.Options["AdditionalDependencies"] += ";%(AdditionalDependencies)";
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
                var includes = new List<IncludeWithPrefix>();
                string includePrefix = "/I";

                var platformToolset = Options.GetObject<Options.Vc.General.PlatformToolset>(context.Configuration);
                DevEnv devEnv = platformToolset.GetDefaultDevEnvForToolset() ?? context.DevelopmentEnvironment;

                if (platformToolset.IsLLVMToolchain() && Options.GetObject<Options.Vc.LLVM.UseClangCl>(context.Configuration) == Options.Vc.LLVM.UseClangCl.Enable)
                {
                    // when using clang-cl, mark MSVC includes, so they are properly recognized
                    includePrefix = "/clang:-isystem";

                    Options.Vc.General.PlatformToolset overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
                    if (Options.WithArgOption<Options.Vc.General.PlatformToolset>.Get<Options.Clang.Compiler.LLVMVcPlatformToolset>(context.Configuration, ref overridenPlatformToolset))
                    {
                        platformToolset = overridenPlatformToolset;
                        devEnv = platformToolset.GetDefaultDevEnvForToolset() ?? context.DevelopmentEnvironment;
                    }

                    string clangIncludePath = platformToolset == Options.Vc.General.PlatformToolset.ClangCL ? ClangForWindows.GetWindowsClangIncludePath(devEnv) : ClangForWindows.GetWindowsClangIncludePath();
                    includes.Add(new IncludeWithPrefix(includePrefix, clangIncludePath));
                }
                else
                {
                    // this option is mandatory when using /external:I with msvc, so if the user has selected it
                    // we consider that the vcxproj supports ExternalIncludePath
                    if (Options.HasOption<Options.Vc.General.ExternalWarningLevel>(context.Configuration))
                        includePrefix = "/external:I";
                }

                IEnumerable<string> msvcIncludePaths = EnumerateSemiColonSeparatedString(devEnv.GetWindowsIncludePath());
                includes.AddRange(msvcIncludePaths.Select(path => new IncludeWithPrefix(includePrefix, path)));

                // Additional system includes
                OrderableStrings SystemIncludes = new OrderableStrings(context.Configuration.DependenciesIncludeSystemPaths);
                SystemIncludes.AddRange(context.Configuration.IncludeSystemPaths);
                if (SystemIncludes.Count > 0)
                {
                    SystemIncludes.Sort();
                    includes.AddRange(SystemIncludes.Select(path => new IncludeWithPrefix(includePrefix, path)));
                }
                return includes;
            }

            public override void GeneratePlatformSpecificProjectDescription(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                string propertyGroups = string.Empty;

                // MSBuild override when mixing devenvs in the same vcxproj is not supported,
                // but before throwing an exception check if we have some override
                for (DevEnv devEnv = context.DevelopmentEnvironmentsRange.MinDevEnv; devEnv <= context.DevelopmentEnvironmentsRange.MaxDevEnv; devEnv = (DevEnv)((int)devEnv << 1))
                {
                    switch (devEnv)
                    {
                        case DevEnv.vs2015:
                        case DevEnv.vs2017:
                            {
                                string platformFolder = MSBuildGlobalSettings.GetCppPlatformFolder(devEnv, Platform.win64);
                                if (!string.IsNullOrEmpty(platformFolder))
                                {
                                    using (generator.Declare("platformFolder", Util.EnsureTrailingSeparator(platformFolder))) // _PlatformFolder require the path to end with a "\"
                                        propertyGroups += generator.Resolver.Resolve(Vcxproj.Template.Project.PlatformFolderOverride);
                                }
                            }
                            break;
                        case DevEnv.vs2019:
                        case DevEnv.vs2022:
                            {
                                // Note1: _PlatformFolder override is deprecated starting with vs2019, so we write AdditionalVCTargetsPath instead
                                // Note2: MSBuildGlobalSettings.SetCppPlatformFolder for vs2019 and above is no more the valid way to handle it.

                                if (!string.IsNullOrEmpty(MSBuildGlobalSettings.GetCppPlatformFolder(devEnv, Platform.win64)))
                                    throw new Error($"SetCppPlatformFolder is not supported by {devEnv}: use of MSBuildGlobalSettings.SetCppPlatformFolder should be replaced by use of MSBuildGlobalSettings.SetAdditionalVCTargetsPath.");

                                // vs2019 and up use AdditionalVCTargetsPath
                                string additionalVCTargetsPath = MSBuildGlobalSettings.GetAdditionalVCTargetsPath(devEnv, Platform.win64);
                                if (!string.IsNullOrEmpty(additionalVCTargetsPath))
                                {
                                    using (generator.Declare("additionalVCTargetsPath", Util.EnsureTrailingSeparator(additionalVCTargetsPath))) // the path shall end with a "\"
                                        propertyGroups += generator.Resolver.Resolve(Vcxproj.Template.Project.AdditionalVCTargetsPath);
                                }
                            }
                            break;
                    }
                }

                string llvmOverrideSection = ClangForWindows.GetLLVMOverridesSection(context, generator.Resolver);
                if (!string.IsNullOrEmpty(llvmOverrideSection))
                    propertyGroups += llvmOverrideSection;

                if (!string.IsNullOrEmpty(propertyGroups))
                {
                    if (context.DevelopmentEnvironmentsRange.MinDevEnv != context.DevelopmentEnvironmentsRange.MaxDevEnv)
                        throw new Error("Different vs versions not supported in the same vcxproj");

                    using (generator.Declare("platformName", GetToolchainPlatformString(null)))
                    {
                        generator.Write(Vcxproj.Template.Project.ProjectDescriptionStartPlatformConditional);
                        generator.WriteVerbatim(propertyGroups);
                        generator.Write(Vcxproj.Template.Project.PropertyGroupEnd);
                    }
                }
            }
            #endregion
        }
    }
}
