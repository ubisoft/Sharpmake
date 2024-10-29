// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;

namespace Sharpmake
{
    public static class SwiftForApple
    {
        public static string GetSwiftExecutablePath()
        {
            return Path.Combine(Settings.SwiftInstallDir, "bin");
        }

        public static class Settings
        {
            public static bool SwiftSupportEnabled = Util.GetExecutingPlatform() == Platform.mac;

            private static string s_swiftInstallDir;
            public static string SwiftInstallDir
            {
                get
                {
                    ValidateSwiftSupport();
                    if (!string.IsNullOrEmpty(s_swiftInstallDir))
                        return s_swiftInstallDir;

                    if (Util.GetExecutingPlatform() == Platform.win64)
                        throw new Error($"There is no default swift installation path on Windows, {nameof(SwiftForApple.Settings.SwiftInstallDir)} must be set explicitly.");

                    return $"{ApplePlatform.Settings.XCodeDeveloperPath}/Toolchains/XcodeDefault.xctoolchain/usr";
                }

                set
                {
                    s_swiftInstallDir = Util.PathMakeStandard(value);
                }
            }

            private static string s_swiftClangVersion;
            public static string SwiftClangVersion
            {
                get
                {
                    ValidateSwiftSupport();
                    if (s_swiftClangVersion == null)
                    {
                        if (Util.DirectoryExists(SwiftInstallDir))
                            s_swiftClangVersion = Util.GetClangVersionFromLLVMInstallDir(SwiftInstallDir);
                        else
                            throw new Error($"ClangForApple.Settings.LLVMInstallDir is pointing to {SwiftInstallDir}, which doesn't exists.");
                    }
                    return s_swiftClangVersion;
                }

                set
                {
                    s_swiftClangVersion = value;
                    if (!Util.DirectoryExists(Path.Combine(SwiftInstallDir, "lib", "clang", s_swiftClangVersion)))
                        throw new Error($"Cannot find required files for Clang {s_swiftClangVersion} in {SwiftInstallDir}");
                }
            }
            private static void ValidateSwiftSupport()
            {
                if (!SwiftSupportEnabled)
                    throw new Error("Swift support was not enabled");
            }
        }
    }
}


