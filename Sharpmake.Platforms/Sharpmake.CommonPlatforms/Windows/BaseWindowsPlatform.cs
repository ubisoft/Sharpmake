// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Sharpmake.Generators;

namespace Sharpmake
{
    public static partial class Windows
    {
        public abstract class BaseWindowsPlatform : BaseMicrosoftPlatform, IWindowsFastBuildCompilerSettings
        {
            #region IWindowsFastBuildCompilerSettings implementation
            public override bool IsPcPlatform => true;
            public IDictionary<DevEnv, string> BinPath { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<IFastBuildCompilerKey, CompilerFamily> CompilerFamily { get; set; } = new Dictionary<IFastBuildCompilerKey, CompilerFamily>();
            public IDictionary<DevEnv, string> LinkerPath { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> LinkerExe { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, bool> LinkerInvokedViaCompiler { get; set; } = new Dictionary<DevEnv, bool>();
            public IDictionary<DevEnv, string> LibrarianExe { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> ResCompiler { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, string> ResxCompiler { get; set; } = new Dictionary<DevEnv, string>();
            public IDictionary<DevEnv, Strings> ExtraFiles { get; set; } = new Dictionary<DevEnv, Strings>();
            #endregion

            #region IPlatformVcxproj implementation
            public override IEnumerable<string> GetImplicitlyDefinedSymbols(IGenerationContext context)
            {
                var defines = new List<string>(base.GetImplicitlyDefinedSymbols(context));

                switch (context.Configuration.Output)
                {
                    case Project.Configuration.OutputType.Exe:
                        context.SelectOption(
                            Options.Option(Options.Vc.Linker.SubSystem.Console, () => { defines.Add("_CONSOLE"); }),
                            Options.Option(Options.Vc.Linker.SubSystem.Windows, () => { defines.Add("_WINDOWS"); }));
                        break;
                    case Project.Configuration.OutputType.Lib:
                        defines.Add("_LIB");
                        break;
                    case Project.Configuration.OutputType.Dll:
                        context.SelectOption(
                            Options.Option(Options.Vc.Linker.DLLDefine.Regular, () => { defines.Add("_USRDLL"); }),
                            Options.Option(Options.Vc.Linker.DLLDefine.Extension, () => { defines.Add("_AFXDLL"); defines.Add("_AFXEXT"); }));
                        break;
                    case Project.Configuration.OutputType.None:
                    default:
                        break;
                }

                return defines;
            }
            public override bool HasUserAccountControlSupport => true;

            public override IEnumerable<string> GetPlatformLibraryFiles(IGenerationContext context)
            {
                yield return "kernel32.lib";
                yield return "user32.lib";
                yield return "gdi32.lib";
                yield return "winspool.lib";
                yield return "comdlg32.lib";
                yield return "advapi32.lib";
                yield return "shell32.lib";
                yield return "ole32.lib";
                yield return "oleaut32.lib";
                yield return "uuid.lib";
                yield return "odbc32.lib";
                yield return "odbccp32.lib";
            }

            public override void SetupSdkOptions(IGenerationContext context)
            {
                var conf = context.Configuration;
                var devEnv = context.DevelopmentEnvironment;

                // We need to override the executable path for vs2015 because WindowsKit UAP.props does not
                // correctly set the WindowsSDK_ExecutablePath to the bin folder of its current version.
                if (devEnv == DevEnv.vs2015 && !KitsRootPaths.UsesDefaultKitRoot(devEnv))
                {
                    context.Options["ExecutablePath"] = devEnv.GetWindowsExecutablePath(conf.Platform);
                }

                Options.Vc.General.PlatformToolset platformToolset = Options.GetObject<Options.Vc.General.PlatformToolset>(conf);
                if (Options.Vc.General.PlatformToolset.LLVM == platformToolset)
                {
                    Options.Vc.General.PlatformToolset overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
                    if (Options.WithArgOption<Options.Vc.General.PlatformToolset>.Get<Options.Clang.Compiler.LLVMVcPlatformToolset>(conf, ref overridenPlatformToolset))
                        platformToolset = overridenPlatformToolset;

                    devEnv = platformToolset.GetDefaultDevEnvForToolset() ?? devEnv;

                    context.Options["ExecutablePath"] = ClangForWindows.GetWindowsClangExecutablePath() + ";" + devEnv.GetWindowsExecutablePath(conf.Platform);
                    if (Options.GetObject<Options.Vc.LLVM.UseClangCl>(conf) == Options.Vc.LLVM.UseClangCl.Enable)
                    {
                        context.Options["IncludePath"] = ClangForWindows.GetWindowsClangIncludePath() + ";" + devEnv.GetWindowsIncludePath();
                        context.Options["LibraryPath"] = ClangForWindows.GetWindowsClangLibraryPath() + ";" + devEnv.GetWindowsLibraryPath(conf.Platform, Util.IsDotNet(conf) ? conf.Target.GetFragment<DotNetFramework>() : default(DotNetFramework?));
                    }
                }

                var systemIncludes = new OrderableStrings(conf.DependenciesIncludeSystemPaths);
                systemIncludes.AddRange(conf.IncludeSystemPaths);
                if (systemIncludes.Count > 0)
                {
                    systemIncludes.Sort();
                    string systemIncludesString = Util.PathGetRelative(context.ProjectDirectory, systemIncludes).JoinStrings(";");

                    // this option is mandatory when using /external:I with msvc, so if the user has selected it
                    // we consider that the vcxproj supports ExternalIncludePath
                    if (Options.HasOption<Options.Vc.General.ExternalWarningLevel>(conf))
                    {
                        if (context.Options["ExternalIncludePath"] == FileGeneratorUtilities.RemoveLineTag)
                            context.Options["ExternalIncludePath"] = systemIncludesString;
                        else
                            context.Options["ExternalIncludePath"] += ";" + systemIncludesString;
                    }
                    else
                    {
                        if (context.Options["IncludePath"] == FileGeneratorUtilities.RemoveLineTag)
                            context.Options["IncludePath"] = "$(VC_IncludePath);$(WindowsSDK_IncludePath);" + systemIncludesString;
                        else
                            context.Options["IncludePath"] += ";" + systemIncludesString;
                    }
                }
            }
            #endregion
        }
    }
}
