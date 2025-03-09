// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;

namespace Sharpmake
{
    public static class ClangForApple
    {
        public static string GetClangExecutablePath()
        {
            return Path.Combine(Settings.LLVMInstallDir, "bin");
        }

        public static string GetClangIncludePath()
        {
            return Path.Combine(Settings.LLVMInstallDir, "lib", "clang", Settings.ClangVersion, "include");
        }

        public static string GetClangLibraryPath()
        {
            return Path.Combine(Settings.LLVMInstallDir, "lib", "clang", Settings.ClangVersion, "lib", "darwin");
        }

        public static class Settings
        {
            private static bool s_isAppleClang = true;
            public static bool IsAppleClang
            {
                get
                {
                    if (Util.GetExecutingPlatform() == Platform.win64)
                        return false;
                    return s_isAppleClang;
                }
                set
                {
                    if (value && Util.GetExecutingPlatform() == Platform.win64)
                        throw new Error("Apple clang doesn't work on Windows");
                    s_isAppleClang = value;
                }
            }

            private static string s_llvmInstallDir;
            public static string LLVMInstallDir
            {
                get
                {
                    if (!string.IsNullOrEmpty(s_llvmInstallDir))
                        return s_llvmInstallDir;

                    if (Util.GetExecutingPlatform() == Platform.win64)
                        return ClangForWindows.Settings.LLVMInstallDir;

                    return $"{ApplePlatform.Settings.XCodeDeveloperPath}/Toolchains/XcodeDefault.xctoolchain/usr";
                }

                set
                {
                    s_llvmInstallDir = Util.PathMakeStandard(value);
                }
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
                            throw new Error($"ClangForApple.Settings.LLVMInstallDir is pointing to {LLVMInstallDir}, which doesn't exists.");
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
        }
    }
}


