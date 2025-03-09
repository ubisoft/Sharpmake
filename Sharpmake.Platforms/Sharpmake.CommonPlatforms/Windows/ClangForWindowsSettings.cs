// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Sharpmake.Generators;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public static class ClangForWindows
    {
        public static string GetWindowsClangExecutablePath()
        {
            return Path.Combine(Settings.LLVMInstallDir, "bin");
        }

        public static string GetWindowsClangExecutablePath(DevEnv devEnv)
        {
            return Path.Combine(Settings.LLVMInstallDirVsEmbedded(devEnv), "bin");
        }

        public static string GetWindowsClangIncludePath()
        {
            return Path.Combine(Settings.LLVMInstallDir, "lib", "clang", Settings.ClangVersion, "include");
        }

        public static string GetWindowsClangIncludePath(DevEnv devEnv)
        {
            return Path.Combine(Settings.LLVMInstallDirVsEmbedded(devEnv), "lib", "clang", Settings.ClangVersionVsEmbedded(devEnv), "include");
        }

        public static string GetWindowsClangLibraryPath(string libFolderName = null)
        {
            return GetWindowsClangLibraryPath(Settings.LLVMInstallDir, Settings.ClangVersion, libFolderName);
        }

        public static string GetWindowsClangLibraryPath(DevEnv devEnv, string libFolderName = null)
        {
            return GetWindowsClangLibraryPath(Settings.LLVMInstallDirVsEmbedded(devEnv), Settings.ClangVersionVsEmbedded(devEnv), libFolderName);
        }

        public static string GetWindowsClangLibraryPath(string llvmInstallDir, string clangVersion, string libFolderName = null)
        {
            if (libFolderName == null)
            {
                // Starting with LLVM 16, clangVersion only contains the major version (e.g. "15.0.7", "16" ...)
                int majorVersion = int.Parse(clangVersion.Split(new char[1] { '.' }).First());

                // Starting with LLVM 15, runtime library structure changes.
                if (majorVersion >= 15)
                    libFolderName = "x86_64-pc-windows-msvc";
                else
                    libFolderName = "windows";
            }
            return Path.Combine(llvmInstallDir, "lib", "clang", clangVersion, "lib", libFolderName);
        }

        public static class Settings
        {
            public static bool OverridenLLVMInstallDir { get; private set; }

            private static string s_llvmInstallDir;
            public static string LLVMInstallDir
            {
                get
                {
                    return s_llvmInstallDir ?? (s_llvmInstallDir = Util.GetDefaultLLVMInstallDir());
                }

                set
                {
                    s_llvmInstallDir = Util.PathMakeStandard(value);
                    OverridenLLVMInstallDir = true;
                }
            }

            public static string LLVMInstallDirVsEmbedded(DevEnv devEnv)
            {
                if (OverridenLLVMInstallDir)
                    return LLVMInstallDir;

                string vsDir = devEnv.GetVisualStudioDir();
                return Path.Combine(vsDir, "VC", "Tools", "Llvm", "x64");
            }

            private static string s_clangVersion;
            public static string ClangVersion
            {
                get
                {
                    if (s_clangVersion == null)
                    {
                        if (Util.DirectoryExists(LLVMInstallDir))
                            s_clangVersion = Util.GetClangVersionFromLLVMInstallDir(LLVMInstallDir);
                        else
                            s_clangVersion = "7.0.0"; // arbitrary
                    }
                    return s_clangVersion;
                }

                set
                {
                    s_clangVersion = value;
                    if (!Util.DirectoryExists(Path.Combine(LLVMInstallDir, "lib", "clang", s_clangVersion)))
                        throw new Error($"Cannot find required files for Clang {s_clangVersion} in {LLVMInstallDir}");
                }
            }

            private static readonly ConcurrentDictionary<DevEnv, string> s_vsEmbeddedClangVersion = new ConcurrentDictionary<DevEnv, string>();
            public static string ClangVersionVsEmbedded(DevEnv devEnv)
            {
                if (OverridenLLVMInstallDir)
                    return ClangVersion;

                return s_vsEmbeddedClangVersion.GetOrAdd(devEnv, d =>
                {
                    return Util.GetClangVersionFromLLVMInstallDir(LLVMInstallDirVsEmbedded(devEnv));
                });
            }
        }

        public static void WriteLLVMOverrides(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            string llvmOverrideSection = GetLLVMOverridesSection(context, generator.Resolver);
            if (!string.IsNullOrEmpty(llvmOverrideSection))
                generator.Write(llvmOverrideSection);
        }

        public static string GetLLVMOverridesSection(IVcxprojGenerationContext context, Resolver resolver)
        {
            if (Settings.OverridenLLVMInstallDir)
            {
                var allPlatformToolsets = context.ProjectConfigurations.Select(Options.GetObject<Options.Vc.General.PlatformToolset>);
                var llvmToolsets = allPlatformToolsets.Where(t => t.IsLLVMToolchain()).Distinct().ToList();
                if (llvmToolsets.Count > 0)
                {
                    if (llvmToolsets.Count == 1)
                    {
                        if (context.DevelopmentEnvironmentsRange.MinDevEnv != context.DevelopmentEnvironmentsRange.MaxDevEnv)
                            throw new Error("Different vs versions not supported in the same vcxproj");
                        DevEnv devEnv = context.DevelopmentEnvironmentsRange.MinDevEnv;

                        string llvmProperties = string.Empty;

                        // LLVMInstallDir
                        {
                            string llvmInstallDir = llvmToolsets[0] == Options.Vc.General.PlatformToolset.ClangCL ? Settings.LLVMInstallDirVsEmbedded(devEnv) : Settings.LLVMInstallDir;
                            using (resolver.NewScopedParameter("custompropertyname", "LLVMInstallDir"))
                            using (resolver.NewScopedParameter("custompropertyvalue", llvmInstallDir.TrimEnd(Util._pathSeparators))) // trailing separator will be added by LLVM.Cpp.Common.props
                                llvmProperties += resolver.Resolve(Vcxproj.Template.Project.CustomProperty);
                        }

                        // LLVMToolsVersion is ClangCL specific
                        if (llvmToolsets[0] == Options.Vc.General.PlatformToolset.ClangCL)
                        {
                            using (resolver.NewScopedParameter("custompropertyname", "LLVMToolsVersion"))
                            using (resolver.NewScopedParameter("custompropertyvalue", Settings.ClangVersionVsEmbedded(devEnv)))
                                llvmProperties += resolver.Resolve(Vcxproj.Template.Project.CustomProperty);
                        }

                        return llvmProperties;
                    }
                    else
                    {
                        throw new Error("Varying llvm platform toolsets in the same vcxproj file! That's not supported");
                    }
                }
            }

            return null;
        }
    }
}
