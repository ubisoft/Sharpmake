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

using System.IO;

namespace Sharpmake
{
    public static class ClangForWindows
    {
        public static string GetWindowsClangExecutablePath()
        {
            return Path.Combine(Settings.LLVMInstallDir, "bin");
        }

        public static string GetWindowsClangIncludePath()
        {
            return Path.Combine(Settings.LLVMInstallDir, "lib", "clang", Settings.ClangVersion, "include");
        }

        public static string GetWindowsClangLibraryPath()
        {
            return Path.Combine(Settings.LLVMInstallDir, "lib", "clang", Settings.ClangVersion, "lib", "windows");
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
                    s_llvmInstallDir = value;
                    OverridenLLVMInstallDir = true;
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
        }
    }
}
