// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;

namespace Sharpmake
{
    public static partial class Android
    {
        public static class Util
        {
            // will find folders named after the platform api level,
            // following this pattern: android-XX, with XX being 2 digits
            public static string FindLatestApiLevelInDirectory(string directory)
            {
                string latestDirectory = null;
                if (Directory.Exists(directory))
                {
                    var androidDirectories = Sharpmake.Util.DirectoryGetDirectories(directory);
                    int latestValue = 0;
                    foreach (var folderName in androidDirectories.Select(Path.GetFileName))
                    {
                        int current = 0;
                        if (TryParseAndroidApiValue(folderName, out current))
                        {
                            if (current > latestValue)
                            {
                                latestValue = current;
                                latestDirectory = folderName;
                            }
                        }
                    }
                }

                return latestDirectory;
            }

            public static bool TryParseAndroidApiValue(string apiString, out int apiValue)
            {
                apiValue = 0;
                if (string.IsNullOrWhiteSpace(apiString))
                    return false;

                const int devKitEditionTargetExpectedLength = 10;
                if (apiString.Length != devKitEditionTargetExpectedLength)
                    return false;

                // skip 'android-'
                string valueString = apiString.Substring(8);

                return int.TryParse(valueString, out apiValue);
            }

            private static string s_ndkVersion = string.Empty;
            public static string GetNdkVersion(string ndkPath)
            {
                if (!s_ndkVersion.Equals(string.Empty))
                    return s_ndkVersion;

                if (string.IsNullOrEmpty(ndkPath))
                    return s_ndkVersion;

                string srcPropertiesFile = Path.Combine(ndkPath, "source.properties");
                if (!File.Exists(srcPropertiesFile))
                    return string.Empty;

                using (StreamReader sr = new StreamReader(srcPropertiesFile))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.StartsWith("Pkg.Revision"))
                        {
                            int pos = line.IndexOf("=");
                            if (-1 != pos)
                            {
                                s_ndkVersion = line.Substring(pos + 1).Trim();
                            }
                            break;
                        }
                    }
                }
                return s_ndkVersion;
            }

            public static string GetPrebuildToolchainString()
            {
                return "llvm";
            }

            /// <summary>
            /// https://developer.android.com/ndk/guides/other_build_systems#overview
            /// </summary>
            /// <returns>
            /// "darwin-x86_64" on MacOS
            /// "linux-x86_64" on Linux
            /// "windows" on Windows 32-bit
            /// "windows-x86_64" on Windows 64-bit
            /// </returns>
            public static string GetHostTag()
            {
                Platform platform = Sharpmake.Util.GetExecutingPlatform();
                switch (platform)
                {
                    case Platform.mac:
                        return "darwin-x86_64";
                    case Platform.linux:
                        return "linux-x86_64";
                    case Platform.win32:
                        return "windows";
                    case Platform.win64:
                        return "windows-x86_64";
                    default:
                        throw new Error($"Sharpmake running on unknown platform : {platform}");
                }
            }

            /// <summary>
            /// https://developer.android.com/ndk/guides/other_build_systems#overview
            /// </summary>
            /// <param name="buildTarget"></param>
            /// <param name="forCompiler"></param>
            /// <param name="vendor"></param>
            /// <returns></returns>
            public static string GetTargetTriple(AndroidBuildTargets buildTarget, bool forCompiler = false, string vendor = "")
            {
                switch (buildTarget)
                {
                    case AndroidBuildTargets.arm64_v8a:
                        return Options.Clang.GetTargetTriple("aarch64", "", vendor, "linux", "android");
                    case AndroidBuildTargets.armeabi_v7a:
                        return Options.Clang.GetTargetTriple("arm", (forCompiler) ? "v7a" : "", vendor, "linux", "androidabi");
                    case AndroidBuildTargets.x86:
                        return Options.Clang.GetTargetTriple("i686", "", vendor, "linux", "android");
                    case AndroidBuildTargets.x86_64:
                        return Options.Clang.GetTargetTriple("x86_64", "", vendor, "linux", "android");
                    default:
                        throw new Error($"Unsupported Android target: {buildTarget}");
                }
            }

            public static string GetAndroidApiLevelString(Options.Android.General.AndroidAPILevel androidApiLevel)
            {
                switch (androidApiLevel)
                {
                    case Options.Android.General.AndroidAPILevel.Android16:
                        return "16";
                    case Options.Android.General.AndroidAPILevel.Android17:
                        return "17";
                    case Options.Android.General.AndroidAPILevel.Android18:
                        return "18";
                    case Options.Android.General.AndroidAPILevel.Android19:
                        return "19";
                    case Options.Android.General.AndroidAPILevel.Android20:
                        return "20";
                    case Options.Android.General.AndroidAPILevel.Android21:
                        return "21";
                    case Options.Android.General.AndroidAPILevel.Android22:
                        return "22";
                    case Options.Android.General.AndroidAPILevel.Android23:
                        return "23";
                    case Options.Android.General.AndroidAPILevel.Android24:
                        return "24";
                    case Options.Android.General.AndroidAPILevel.Android25:
                        return "25";
                    case Options.Android.General.AndroidAPILevel.Android26:
                        return "26";
                    case Options.Android.General.AndroidAPILevel.Android27:
                        return "27";
                    case Options.Android.General.AndroidAPILevel.Android28:
                        return "28";
                    case Options.Android.General.AndroidAPILevel.Android29:
                        return "29";
                    case Options.Android.General.AndroidAPILevel.Android30:
                        return "30";
                    case Options.Android.General.AndroidAPILevel.Latest:
                    case Options.Android.General.AndroidAPILevel.Default:
                        return FindLatestApiLevelStringBySdk(GlobalSettings.AndroidHome) ?? "";
                    default:
                        throw new Error($"Unsupported Android Api level: {androidApiLevel}");
                }
            }

            public static string FindLatestApiLevelStringBySdk(string lookupDirectory)
            {
                string latestApiLevel = Util.FindLatestApiLevelInDirectory(Path.Combine(lookupDirectory, "platforms"));
                string androidApiLevel = null;
                if (!string.IsNullOrEmpty(latestApiLevel))
                {
                    int pos = latestApiLevel.IndexOf("-");
                    if (pos != -1)
                    {
                        androidApiLevel = latestApiLevel.Substring(pos + 1);
                    }
                }
                return androidApiLevel;
            }

            public static AndroidBuildTargets GetAndroidBuildTarget(Project.Configuration conf, AndroidBuildTargets defaultValue = AndroidBuildTargets.arm64_v8a)
            {
                AndroidBuildTargets androidTarget;
                return (conf.Target.TryGetFragment(out androidTarget)) ? androidTarget : defaultValue;
            }

            public static string GetTargetTripleWithVersionSuffix(AndroidBuildTargets buildTarget, Options.Android.General.AndroidAPILevel androidApiLevel)
            {
                return GetTargetTripleWithVersionSuffix(buildTarget, GetAndroidApiLevelString(androidApiLevel));
            }

            public static string GetTargetTripleWithVersionSuffix(AndroidBuildTargets buildTarget, string androidMinApi)
            {
                return $"{GetTargetTriple(buildTarget)}{androidMinApi}";
            }
        }
    }
}
