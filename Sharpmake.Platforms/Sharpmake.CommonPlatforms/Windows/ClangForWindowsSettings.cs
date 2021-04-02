// Copyright (c) 2019-2021 Ubisoft Entertainment
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

        public static string GetWindowsClangLibraryPath()
        {
            return Path.Combine(Settings.LLVMInstallDir, "lib", "clang", Settings.ClangVersion, "lib", "windows");
        }

        public static string GetWindowsClangLibraryPath(DevEnv devEnv)
        {
            return Path.Combine(Settings.LLVMInstallDirVsEmbedded(devEnv), "lib", "clang", Settings.ClangVersionVsEmbedded(devEnv), "lib", "windows");
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
                        var devEnv = context.DevelopmentEnvironmentsRange.MinDevEnv;

                        var llvmInstallDir = llvmToolsets[0] == Options.Vc.General.PlatformToolset.ClangCL ? Settings.LLVMInstallDirVsEmbedded(devEnv) : Settings.LLVMInstallDir;
                        using (resolver.NewScopedParameter("custompropertyname", "LLVMInstallDir"))
                        using (resolver.NewScopedParameter("custompropertyvalue", llvmInstallDir.TrimEnd(Util._pathSeparators))) // trailing separator will be added by LLVM.Cpp.Common.props
                            return resolver.Resolve(Vcxproj.Template.Project.CustomProperty);
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
