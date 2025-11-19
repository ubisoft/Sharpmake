// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Sharpmake
{
    public static class ExtensionMethods
    {
        public static bool IsPC(this Platform platform)
        {
            return platform == Platform.win32 || platform == Platform.win64;
        }

        public static bool IsMicrosoft(this Platform platform)
        {
            return PlatformRegistry.Get<IPlatformDescriptor>(platform).IsMicrosoftPlatform;
        }

        public static bool IsUsingClang(this Platform platform)
        {
            return PlatformRegistry.Get<IPlatformDescriptor>(platform).IsUsingClang;
        }

        public static bool IsLinkerInvokedViaCompiler(this Platform platform)
        {
            return PlatformRegistry.Get<IPlatformDescriptor>(platform).IsLinkerInvokedViaCompiler;
        }

        public static string GetLinkerOptionPrefix(this Platform platform)
        {
            if(IsLinkerInvokedViaCompiler(platform))
                return "-Wl,";

            return "";
        }

        public static string ToVersionString(this DotNetFramework framework)
        {
            switch (framework)
            {
                case DotNetFramework.v4_7:
                    return "4.7";
                case DotNetFramework.v4_7_1:
                    return "4.7.1";
                case DotNetFramework.v4_7_2:
                    return "4.7.2";
                case DotNetFramework.v4_8:
                    return "4.8";
                case DotNetFramework.netcore1_0:
                    return "netcoreapp1.0";
                case DotNetFramework.netcore1_1:
                    return "netcoreapp1.1";
                case DotNetFramework.netcore2_0:
                    return "netcoreapp2.0";
                case DotNetFramework.netcore2_1:
                    return "netcoreapp2.1";
                case DotNetFramework.netcore2_2:
                    return "netcoreapp2.2";
                case DotNetFramework.netcore3_0:
                    return "netcoreapp3.0";
                case DotNetFramework.netcore3_1:
                    return "netcoreapp3.1";
                case DotNetFramework.netstandard1_0:
                    return "netstandard1.0";
                case DotNetFramework.netstandard1_1:
                    return "netstandard1.1";
                case DotNetFramework.netstandard1_2:
                    return "netstandard1.2";
                case DotNetFramework.netstandard1_3:
                    return "netstandard1.3";
                case DotNetFramework.netstandard1_4:
                    return "netstandard1.4";
                case DotNetFramework.netstandard1_5:
                    return "netstandard1.5";
                case DotNetFramework.netstandard1_6:
                    return "netstandard1.6";
                case DotNetFramework.netstandard2_0:
                    return "netstandard2.0";
                case DotNetFramework.netstandard2_1:
                    return "netstandard2.1";
                case DotNetFramework.net5_0:
                    return "net5.0";
                case DotNetFramework.net6_0:
                    return "net6.0";
                case DotNetFramework.net7_0:
                    return "net7.0";
                case DotNetFramework.net8_0:
                    return "net8.0";
                case DotNetFramework.net9_0:
                    return "net9.0";
                case DotNetFramework.net10_0:
                    return "net10.0";
                case DotNetFramework.all_netframework:
                case DotNetFramework.all_netcore:
                case DotNetFramework.all_netstandard:
                default:
                    throw new ArgumentOutOfRangeException(nameof(framework));
            }
        }

        public static string ToFolderName(this DotNetFramework framework)
        {
            //https://docs.microsoft.com/en-us/dotnet/standard/frameworks
            switch (framework)
            {
                case DotNetFramework.v4_7:
                    return "net47";
                case DotNetFramework.v4_7_1:
                    return "net471";
                case DotNetFramework.v4_7_2:
                    return "net472";
                case DotNetFramework.v4_8:
                    return "net48";
                case DotNetFramework.netcore1_0:
                    return "netcoreapp1.0";
                case DotNetFramework.netcore1_1:
                    return "netcoreapp1.1";
                case DotNetFramework.netcore2_0:
                    return "netcoreapp2.0";
                case DotNetFramework.netcore2_1:
                    return "netcoreapp2.1";
                case DotNetFramework.netcore2_2:
                    return "netcoreapp2.2";
                case DotNetFramework.netcore3_0:
                    return "netcoreapp3.0";
                case DotNetFramework.netcore3_1:
                    return "netcoreapp3.1";
                case DotNetFramework.net5_0:
                    return "net5.0";
                case DotNetFramework.net6_0:
                    return "net6.0";
                case DotNetFramework.net7_0:
                    return "net7.0";
                case DotNetFramework.net8_0:
                    return "net8.0";
                case DotNetFramework.net9_0:
                    return "net9.0";
                case DotNetFramework.net10_0:
                    return "net10.0";
                case DotNetFramework.netstandard1_0:
                    return "netstandard1.0";
                case DotNetFramework.netstandard1_1:
                    return "netstandard1.1";
                case DotNetFramework.netstandard1_2:
                    return "netstandard1.2";
                case DotNetFramework.netstandard1_3:
                    return "netstandard1.3";
                case DotNetFramework.netstandard1_4:
                    return "netstandard1.4";
                case DotNetFramework.netstandard1_5:
                    return "netstandard1.5";
                case DotNetFramework.netstandard1_6:
                    return "netstandard1.6";
                case DotNetFramework.netstandard2_0:
                    return "netstandard2.0";
                case DotNetFramework.netstandard2_1:
                    return "netstandard2.1";
                default:
                    throw new ArgumentOutOfRangeException(nameof(framework), framework, nameof(DotNetFramework) + " cannot be converted to a folder name.");
            }
        }

        public static bool IsDotNetFramework(this DotNetFramework framework)
        {
            return (0 != (framework & DotNetFramework.all_netframework));
        }

        public static bool IsDotNetCore(this DotNetFramework framework)
        {
            return (0 != (framework & DotNetFramework.all_netcore));
        }

        public static bool IsDotNetStandard(this DotNetFramework framework)
        {
            return (0 != (framework & DotNetFramework.all_netstandard));
        }

        public static string GetVisualProjectToolsVersionString(this DevEnv visualVersion)
        {
            switch (visualVersion)
            {
                case DevEnv.vs2015:
                    return "14.0";
                case DevEnv.vs2017:
                    return "15.0";
                case DevEnv.vs2019:
                    return "16.0";
                case DevEnv.vs2022:
                    return "17.0";
                default:
                    throw new Error("DevEnv " + visualVersion + " not recognized!");
            }
        }

        public static int GetVisualMajorVersion(this DevEnv visualVersion)
        {
            switch (visualVersion)
            {
                case DevEnv.vs2015:
                    return 14;
                case DevEnv.vs2017:
                    return 15;
                case DevEnv.vs2019:
                    return 16;
                case DevEnv.vs2022:
                    return 17;
                default:
                    throw new NotImplementedException("DevEnv " + visualVersion + " not recognized!");
            }
        }

        public static string GetVisualVersionString(this DevEnv visualVersion)
        {
            return string.Format("{0}.0", visualVersion.GetVisualMajorVersion());
        }

        public static string GetDefaultPlatformToolset(this DevEnv visualVersion)
        {
            switch (visualVersion)
            {
                case DevEnv.vs2015:
                    return "v140";
                case DevEnv.vs2017:
                    return "v141";
                case DevEnv.vs2019:
                    return "v142";
                case DevEnv.vs2022:
                    return "v143";
                default:
                    throw new Error("DevEnv " + visualVersion + " not recognized!");
            }
        }

        public static string GetVSYear(this DevEnv visualVersion)
        {
            switch (visualVersion)
            {
                case DevEnv.vs2015:
                    return "2015";
                case DevEnv.vs2017:
                    return "2017";
                case DevEnv.vs2019:
                    return "2019";
                case DevEnv.vs2022:
                    return "2022";
                default:
                    throw new Error("DevEnv " + visualVersion + " not recognized!");
            }
        }

        internal static void ClearVisualStudioDirCaches()
        {
            s_visualStudioDirectories.Clear();
            s_visualStudioVCRootPathCache.Clear();
        }

        private static readonly ConcurrentDictionary<DevEnv, string> s_visualStudioDirOverrides = new ConcurrentDictionary<DevEnv, string>();
        public static void SetVisualStudioDirOverride(this DevEnv visualVersion, string path)
        {
            bool result = s_visualStudioDirOverrides.TryAdd(visualVersion, path);
            if (!result)
                throw new Error("Can't override a specific Visual Studio version directory more than once. Version: " + visualVersion);
        }

        public static bool OverridenVisualStudioDir(this DevEnv visualVersion)
        {
            return s_visualStudioDirOverrides.ContainsKey(visualVersion);
        }

        private static readonly ConcurrentDictionary<Tuple<DevEnv, bool>, string> s_visualStudioDirectories = new ConcurrentDictionary<Tuple<DevEnv, bool>, string>();
        public static string GetVisualStudioDir(this DevEnv visualVersion, bool ignoreVisualStudioPathOverride = false)
        {
            if (!visualVersion.IsVisualStudio())
                throw new Error($"{visualVersion} is not a visual studio version!");

            // TODO: Replace Tuple with ValueTuple once we support C# 8 because ValueTuple is
            //       allocated on the stack. That should be faster here.
            string visualStudioDirectory = s_visualStudioDirectories.GetOrAdd(Tuple.Create(visualVersion, ignoreVisualStudioPathOverride), devEnv =>
            {
                if (!ignoreVisualStudioPathOverride)
                {
                    // First check if the visual studio path is overridden from default value.
                    string pathOverride;
                    if (s_visualStudioDirOverrides.TryGetValue(visualVersion, out pathOverride))
                        return pathOverride;
                }

                string installDir = Util.GetVisualStudioInstallPathFromQuery(visualVersion);
                if (string.IsNullOrEmpty(installDir))
                {
                    // try again but this time including previews
                    installDir = Util.GetVisualStudioInstallPathFromQuery(visualVersion, allowPrereleaseVersions: true);
                    if (string.IsNullOrEmpty(installDir)) // arbitrary fallback
                    {
                        if (visualVersion > DevEnv.vs2015)
                        {
                            installDir = @"Microsoft Visual Studio\" + GetVSYear(visualVersion) + @"\Professional";
                        }
                        else
                        {
                            installDir = string.Format(@"Microsoft Visual Studio {0}", visualVersion.GetVisualVersionString());
                        }
                        string rootDir = Environment.GetFolderPath(visualVersion < DevEnv.vs2022 ? Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles);
                        installDir = Path.Combine(rootDir, installDir);
                    }
                }
                return Util.SimplifyPath(installDir);
            });

            return visualStudioDirectory;
        }

        private static string GetDefaultCompilerVersion(this DevEnv visualVersion)
        {
            switch (visualVersion)
            {
                case DevEnv.vs2017:
                    return "14.10.25017";
                case DevEnv.vs2019:
                    return "14.20.27404";
                case DevEnv.vs2022:
                    return "14.30.30423"; // from Preview3
                default:
                    throw new Error("DevEnv " + visualVersion + " not recognized for default compiler version");
            }
        }

        private static readonly ConcurrentDictionary<DevEnv, string> s_visualStudioVCRootPathCache = new ConcurrentDictionary<DevEnv, string>();
        public static string GetVisualStudioVCRootPath(this DevEnv visualVersion)
        {
            if (!visualVersion.IsVisualStudio())
                throw new Error($"{visualVersion} is not a visual studio version!");

            string visualStudioVCRootPath = s_visualStudioVCRootPathCache.GetOrAdd(visualVersion, devEnv =>
            {
                string vsDir = visualVersion.GetVisualStudioDir();
                if (visualVersion > DevEnv.vs2015)
                {
                    return Path.Combine(vsDir, "VC", "Tools", "MSVC", visualVersion.GetVisualStudioVCToolsVersion().ToString());
                }
                else
                {
                    return Path.Combine(vsDir, "VC");
                }
            });

            return visualStudioVCRootPath;
        }

        private static readonly ConcurrentDictionary<DevEnv, Version> s_visualStudioVCToolsVersionCache = new ConcurrentDictionary<DevEnv, Version>();
        public static Version GetVisualStudioVCToolsVersion(this DevEnv visualVersion)
        {
            if (!visualVersion.IsVisualStudio())
                throw new Error($"{visualVersion} is not a visual studio version!");

            Version version = s_visualStudioVCToolsVersionCache.GetOrAdd(visualVersion, devEnv =>
            {
                string versionString = visualVersion.GetDefaultCompilerVersion(); // default fallback
                try
                {
                    string toolchainFile = Path.Combine(visualVersion.GetVisualStudioDir(), "VC", "Auxiliary", "Build", "Microsoft.VCToolsVersion.default.txt");
                    if (File.Exists(toolchainFile))
                    {
                        using (StreamReader file = new StreamReader(toolchainFile))
                            versionString = file.ReadLine().Trim();
                    }
                }
                catch { }

                return new Version(versionString);
            });

            return version;
        }

        private static ConcurrentDictionary<(DevEnv, Platform), Version> s_visualStudioCompilerVersionCache = new ConcurrentDictionary<(DevEnv, Platform), Version>();

        public static Version GetVisualStudioVCToolsCompilerVersion(this DevEnv visualVersion, Platform platform)
        {
            var cacheKey = (visualVersion, platform);
            Version version = s_visualStudioCompilerVersionCache.GetOrAdd(cacheKey, ((DevEnv, Platform) key) =>
            {
                string clExeFile = Path.Combine(visualVersion.GetVisualStudioBinPath(platform), "cl.exe");
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(clExeFile);
                return new Version(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart, fileVersionInfo.FilePrivatePart);
            });
            return version;
        }

        /// <summary>
        /// Will return the name of the root directory in MSBuild under Microsoft/VC for a particular devenv,
        /// since it uses yet another versioning pattern than the toolchain
        /// </summary>
        /// <param name="visualVersion">The visual studio version to convert</param>
        /// <returns>The name of the directory for that version</returns>
        public static string GetMSBuildVCDirVersion(this DevEnv visualVersion)
        {
            switch (visualVersion)
            {
                case DevEnv.vs2017:
                    return "v150";
                case DevEnv.vs2019:
                    return "v160";
                case DevEnv.vs2022:
                    return "v170";
                default:
                    throw new Error("DevEnv " + visualVersion + " not supported!");
            }
        }

        public static string GetVCTargetsPath(this DevEnv visualVersion)
        {
            if (!visualVersion.IsVisualStudio())
                return null;

            switch (visualVersion)
            {
                case DevEnv.vs2015:
                    string versionSubfolder = visualVersion.GetDefaultPlatformToolset().ToUpperInvariant(); // this is enough for now but we could make a specific method to retrieve this value
                    return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"MSBuild\Microsoft.Cpp\v4.0", versionSubfolder));
                case DevEnv.vs2017:
                    return Path.Combine(visualVersion.GetVisualStudioDir(), @"Common7\IDE\VC\VCTargets");
                case DevEnv.vs2019:
                case DevEnv.vs2022:
                    return Path.Combine(visualVersion.GetVisualStudioDir(), @"MSBuild\Microsoft\VC\", visualVersion.GetMSBuildVCDirVersion());
                default:
                    throw new ArgumentOutOfRangeException(nameof(visualVersion), visualVersion, null);
            }
        }

        private static string GetDefaultRedistVersion(this DevEnv visualVersion)
        {
            switch (visualVersion)
            {
                case DevEnv.vs2017:
                    return "14.16.27012";
                case DevEnv.vs2019:
                    return "14.24.28127";
                case DevEnv.vs2022:
                    return "14.30.30423"; // from Preview3
                default:
                    throw new Error("DevEnv " + visualVersion + " not recognized for default compiler version");
            }
        }

        private static readonly ConcurrentDictionary<DevEnv, Version> s_visualStudioVCRedistVersionCache = new ConcurrentDictionary<DevEnv, Version>();
        public static Version GetVisualStudioVCRedistVersion(this DevEnv visualVersion)
        {
            Version version = s_visualStudioVCRedistVersionCache.GetOrAdd(visualVersion, devEnv =>
            {
                string vsDir = visualVersion.GetVisualStudioDir();
                string versionString = visualVersion.GetDefaultRedistVersion(); // default fallback
                try
                {
                    string toolchainFile = Path.Combine(vsDir, "VC", "Auxiliary", "Build", "Microsoft.VCRedistVersion.default.txt");
                    if (File.Exists(toolchainFile))
                    {
                        using (StreamReader file = new StreamReader(toolchainFile))
                            versionString = file.ReadLine().Trim();
                    }
                }
                catch { }

                return new Version(versionString);
            });

            return version;
        }

        public static string GetVisualStudioBinPath(this DevEnv visualVersion, Platform platform)
        {
            switch (visualVersion)
            {
                case DevEnv.vs2015:
                    {
                        string targetPlatform = (platform == Platform.win64) ? "amd64" : "";
                        return Path.Combine(visualVersion.GetVisualStudioVCRootPath(), "bin", targetPlatform);
                    }
                case DevEnv.vs2017:
                case DevEnv.vs2019:
                case DevEnv.vs2022:
                    {
                        string targetPlatform = (platform == Platform.win32) ? "x86" : "x64";
                        string compilerHost = Environment.Is64BitOperatingSystem ? "HostX64" : "HostX86";
                        return Path.Combine(visualVersion.GetVisualStudioVCRootPath(), "bin", compilerHost, targetPlatform);
                    }
            }
            throw new ArgumentOutOfRangeException("VS version not recognized " + visualVersion);
        }

        public static bool OverridenWindowsPath(this DevEnv visualVersion)
        {
            return !KitsRootPaths.IsDefaultKitRootPath(visualVersion);
        }

        public static string GetWindowsExecutablePath(this DevEnv visualVersion, Platform platform)
        {
            KitsRootEnum kitsRoot = KitsRootPaths.GetUseKitsRootForDevEnv(visualVersion);

            string targetPlatform = (platform == Platform.win64) ? "x64" : "x86";

            var paths = new Strings();
            paths.Add(visualVersion.GetVisualStudioBinPath(platform));

            switch (kitsRoot)
            {
                case KitsRootEnum.KitsRoot:
                    paths.Add(Path.Combine(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot), "bin", targetPlatform));
                    break;
                case KitsRootEnum.KitsRoot81:
                    paths.Add(Path.Combine(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot81), "bin", targetPlatform));
                    break;
                case KitsRootEnum.KitsRoot10:
                    {
                        Options.Vc.General.WindowsTargetPlatformVersion windowsTargetPlatformVersion = KitsRootPaths.GetWindowsTargetPlatformVersionForDevEnv(visualVersion);

                        string kitsRoot10Path = KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot10);
                        string platformVersion = windowsTargetPlatformVersion.ToVersionString();

                        if (windowsTargetPlatformVersion >= Options.Vc.General.WindowsTargetPlatformVersion.v10_0_15063_0)
                            paths.Add(Path.Combine(kitsRoot10Path, "bin", platformVersion, targetPlatform));
                        else
                            paths.Add(Path.Combine(kitsRoot10Path, "bin", targetPlatform));

                        if (windowsTargetPlatformVersion <= Options.Vc.General.WindowsTargetPlatformVersion.v10_0_10240_0)
                            paths.Add(Path.Combine(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot81), "bin", targetPlatform));
                    }
                    break;
                default:
                    throw new NotImplementedException("No GetWindowsExecutablePath associated with " + kitsRoot);
            }

            paths.Add("$(PATH)");
            return string.Join(";", paths);
        }

        public static string GetWindowsResourceCompiler(this DevEnv visualVersion, Platform platform)
        {
            KitsRootEnum kitsRoot = KitsRootPaths.GetUseKitsRootForDevEnv(visualVersion);

            string targetPlatform = (platform == Platform.win64) ? "x64" : "x86";

            switch (kitsRoot)
            {
                case KitsRootEnum.KitsRoot:
                    return Path.Combine(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot), "bin", targetPlatform, "rc.exe");
                case KitsRootEnum.KitsRoot81:
                    return Path.Combine(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot81), "bin", targetPlatform, "rc.exe");
                case KitsRootEnum.KitsRoot10:
                    {
                        Options.Vc.General.WindowsTargetPlatformVersion windowsTargetPlatformVersion = KitsRootPaths.GetWindowsTargetPlatformVersionForDevEnv(visualVersion);
                        if (windowsTargetPlatformVersion <= Options.Vc.General.WindowsTargetPlatformVersion.v10_0_10240_0)
                        {
                            string kitsRoot81Path = KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot81);
                            return Path.Combine(kitsRoot81Path, "bin", targetPlatform, "rc.exe");
                        }

                        string kitsRoot10Path = KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot10);
                        string platformVersion = windowsTargetPlatformVersion.ToVersionString();

                        // First, try WindowsSdkVerBinPath
                        string candidateWindowsSdkVerBinPath = Path.Combine(kitsRoot10Path, "bin", platformVersion, targetPlatform, "rc.exe");
                        if (File.Exists(candidateWindowsSdkVerBinPath))
                            return candidateWindowsSdkVerBinPath;

                        // If it didn't contain rc.exe, fallback to WindowsSdkBinPath
                        return Path.Combine(kitsRoot10Path, "bin", targetPlatform, "rc.exe");
                    }
                default:
                    throw new NotImplementedException("No WindowsResourceCompiler associated with " + kitsRoot);
            }
        }

        public static string GetWindowsIncludePath(this DevEnv visualVersion)
        {
            string visualStudioDir = Util.EnsureTrailingSeparator(visualVersion.GetVisualStudioVCRootPath());
            string visualStudioInclude = string.Format(@"{0}include;{0}atlmfc\include", visualStudioDir);

            KitsRootEnum useKitsRoot = KitsRootPaths.GetUseKitsRootForDevEnv(visualVersion);

            switch (useKitsRoot)
            {
                case KitsRootEnum.KitsRoot:
                    {
                        string kitsRoot = Util.EnsureTrailingSeparator(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot));
                        return string.Format(@"{0};{1}Include\shared;{1}Include\um;{1}Include\WinRT;", visualStudioInclude, kitsRoot);
                    }
                case KitsRootEnum.KitsRoot81:
                    {
                        string kitsRoot = Util.EnsureTrailingSeparator(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot81));
                        return string.Format(@"{0};{1}Include\shared;{1}Include\um;{1}Include\WinRT;", visualStudioInclude, kitsRoot);
                    }
                case KitsRootEnum.KitsRoot10:
                    {
                        string kitsRoot10 = Util.EnsureTrailingSeparator(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot10));
                        Options.Vc.General.WindowsTargetPlatformVersion windowsTargetPlatformVersion = KitsRootPaths.GetWindowsTargetPlatformVersionForDevEnv(visualVersion);
                        string platformVersion = windowsTargetPlatformVersion.ToVersionString();
                        var paths = new List<string> {
                            $@"{visualStudioInclude}",
                            $@"{kitsRoot10}Include\{platformVersion}\um",     // $(UM_IncludePath)
                            $@"{kitsRoot10}Include\{platformVersion}\shared", // $(KIT_SHARED_IncludePath)
                            $@"{kitsRoot10}Include\{platformVersion}\winrt",  // $(WinRT_IncludePath)
                            $@"{kitsRoot10}Include\{platformVersion}\ucrt",   // $(UniversalCRT_IncludePath)
                        };

                        if (windowsTargetPlatformVersion <= Options.Vc.General.WindowsTargetPlatformVersion.v10_0_10240_0)
                        {
                            //
                            // Version 10.0.10240.0 and below only contain the UCRT libraries
                            // and headers, not the usual Win32 stuff. So if we are using
                            // version 10240 or older, also include the Windows 8.1 paths so we
                            // have a complete Win32 support.
                            //

                            string kitsRoot81 = Util.EnsureTrailingSeparator(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot81));
                            paths.AddRange(new[]
                            {
                                $@"{kitsRoot81}Include\um",
                                $@"{kitsRoot81}Include\shared"
                            });
                        }

                        return string.Join(";", paths);
                    }
                default:
                    throw new NotImplementedException("No WindowsResourceCompiler associated with " + visualVersion);
            }
        }

        public static string GetWindowsLibraryPath(this DevEnv visualVersion, Platform platform, DotNetFramework? dotNetFramework = null)
        {
            string visualStudioVCDir = Util.EnsureTrailingSeparator(visualVersion.GetVisualStudioVCRootPath());
            string subDir = platform == Platform.win64 ? @"\amd64" : "";
            if (visualVersion.IsVisualStudio() && visualVersion >= DevEnv.vs2017)
                subDir = platform == Platform.win64 ? @"\x64" : @"\x86";

            string visualStudioLib = string.Format(@"{0}lib{1};{0}atlmfc\lib{1};", visualStudioVCDir, subDir);

            KitsRootEnum useKitsRoot = KitsRootPaths.GetUseKitsRootForDevEnv(visualVersion);
            string targetPlatform = platform == Platform.win64 ? "x64" : "x86";

            switch (useKitsRoot)
            {
                case KitsRootEnum.KitsRoot:
                    {
                        string kitsRoot = Util.EnsureTrailingSeparator(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot));
                        return string.Format(@"{0};{1}lib\win8\um\{2};{1}References\CommonConfiguration\Neutral;", visualStudioLib, kitsRoot, targetPlatform);
                    }
                case KitsRootEnum.KitsRoot81:
                    {
                        string kitsRoot = Util.EnsureTrailingSeparator(KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot81));
                        return string.Format(@"{0};{1}lib\winv6.3\um\{2};{1}References\CommonConfiguration\Neutral;", visualStudioLib, kitsRoot, targetPlatform);
                    }
                case KitsRootEnum.KitsRoot10:
                    {
                        string netFxPath = string.Empty;
                        if (dotNetFramework.HasValue && visualVersion >= DevEnv.vs2015)
                        {
                            string netFXKitsDir = Util.EnsureTrailingSeparator(KitsRootPaths.GetNETFXKitsDir(dotNetFramework.Value));
                            netFxPath = Path.Combine(netFXKitsDir, "Lib", "um", targetPlatform);
                        }

                        string kitsRoot10 = KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot10);
                        Options.Vc.General.WindowsTargetPlatformVersion windowsTargetPlatformVersion = KitsRootPaths.GetWindowsTargetPlatformVersionForDevEnv(visualVersion);
                        string platformVersion = windowsTargetPlatformVersion.ToVersionString();
                        var paths = new[]
                        {
                            visualStudioLib,
                            Path.Combine(kitsRoot10, "Lib", platformVersion, "ucrt", targetPlatform),   // $(UniversalCRT_LibraryPath_x86) or $(UniversalCRT_LibraryPath_x64)
                            Path.Combine(kitsRoot10, "Lib", platformVersion, "um", targetPlatform),     // $(WindowsSDK_LibraryPath_x86) or $(WindowsSDK_LibraryPath_x64)
                            netFxPath
                        }.ToList();

                        if (windowsTargetPlatformVersion <= Options.Vc.General.WindowsTargetPlatformVersion.v10_0_10240_0)
                        {
                            string kitsRoot81 = KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot81);
                            paths.AddRange(new[] {
                                Path.Combine(kitsRoot81, "lib", "winv6.3", "um", targetPlatform),
                                Path.Combine(kitsRoot81, "References", "CommonConfiguration", "Neutral")
                            });
                        }

                        return string.Join(";", paths);
                    }
                default:
                    throw new NotImplementedException("No WindowsResourceCompiler associated with " + visualVersion);
            }
        }

        public static string GetWindowsAdditionalDependencies(this DevEnv visualVersion)
        {
            return @"kernel32.lib;user32.lib;gdi32.lib;winspool.lib;comdlg32.lib;advapi32.lib;shell32.lib;ole32.lib;oleaut32.lib;uuid.lib;odbc32.lib;odbccp32.lib";
        }

        public static string GetCommonToolsPath(this DevEnv visualVersion)
        {
            return visualVersion.GetCommonToolsPath(ignoreVisualStudioPathOverride: false);
        }

        public static string GetCommonToolsPath(this DevEnv visualVersion, bool ignoreVisualStudioPathOverride)
        {
            return Path.Combine(GetVisualStudioDir(visualVersion, ignoreVisualStudioPathOverride), "Common7\\Tools");
        }

        public static string ToVersionString(this Options.Vc.General.WindowsTargetPlatformVersion windowsTargetPlatformVersion)
        {
            switch (windowsTargetPlatformVersion)
            {
                case Options.Vc.General.WindowsTargetPlatformVersion.v8_1:
                    return "8.1";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_10240_0:
                    return "10.0.10240.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_10586_0:
                    return "10.0.10586.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_14393_0:
                    return "10.0.14393.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_15063_0:
                    return "10.0.15063.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_16299_0:
                    return "10.0.16299.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_17134_0:
                    return "10.0.17134.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_17763_0:
                    return "10.0.17763.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_18362_0:
                    return "10.0.18362.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0:
                    return "10.0.19041.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_20348_0:
                    return "10.0.20348.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_22000_0:
                    return "10.0.22000.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_22621_0:
                    return "10.0.22621.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_26100_0:
                    return "10.0.26100.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.Latest:
                    return "$(LatestTargetPlatformVersion)";
                default:
                    throw new ArgumentOutOfRangeException(windowsTargetPlatformVersion.ToString());
            }
        }

        public static bool IsDefaultToolsetForDevEnv(this Options.Vc.General.PlatformToolset platformToolset, DevEnv visualVersion)
        {
            switch (platformToolset)
            {
                case Options.Vc.General.PlatformToolset.v140:
                    return visualVersion == DevEnv.vs2015;
                case Options.Vc.General.PlatformToolset.v141:
                    return visualVersion == DevEnv.vs2017;
                case Options.Vc.General.PlatformToolset.v142:
                    return visualVersion == DevEnv.vs2019;
                case Options.Vc.General.PlatformToolset.v143:
                    return visualVersion == DevEnv.vs2022;
                case Options.Vc.General.PlatformToolset.v140_xp:
                case Options.Vc.General.PlatformToolset.v141_xp:
                case Options.Vc.General.PlatformToolset.LLVM:
                case Options.Vc.General.PlatformToolset.ClangCL:
                case Options.Vc.General.PlatformToolset.Default:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(platformToolset), platformToolset, null);
            }
        }

        public static DevEnv? GetDefaultDevEnvForToolset(this Options.Vc.General.PlatformToolset platformToolset)
        {
            switch (platformToolset)
            {
                case Options.Vc.General.PlatformToolset.Default:
                    return null;
                case Options.Vc.General.PlatformToolset.v140:
                case Options.Vc.General.PlatformToolset.v140_xp:
                    return DevEnv.vs2015;
                case Options.Vc.General.PlatformToolset.v141:
                case Options.Vc.General.PlatformToolset.v141_xp:
                    return DevEnv.vs2017;
                case Options.Vc.General.PlatformToolset.v142:
                    return DevEnv.vs2019;
                case Options.Vc.General.PlatformToolset.v143:
                    return DevEnv.vs2022;
                case Options.Vc.General.PlatformToolset.LLVM:
                case Options.Vc.General.PlatformToolset.ClangCL:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(platformToolset), platformToolset, null);
            }
        }

        public static void GetVcPathKeysFromDevEnv(this DevEnv devEnv, out string vcTargetsPathKey, out string vcRootPathKey)
        {
            switch (devEnv)
            {
                case DevEnv.vs2015:
                    vcTargetsPathKey = "VCTargetsPath14";
                    vcRootPathKey = "VCInstallDir_140";
                    break;
                case DevEnv.vs2017:
                    vcTargetsPathKey = "VCTargetsPath15";
                    vcRootPathKey = "VCInstallDir_150";
                    break;
                case DevEnv.vs2019:
                    vcTargetsPathKey = "VCTargetsPath16";
                    vcRootPathKey = "VCInstallDir_160";
                    break;
                case DevEnv.vs2022:
                    vcTargetsPathKey = "VCTargetsPath17";
                    vcRootPathKey = "VCInstallDir_170";
                    break;
                default:
                    throw new NotImplementedException("Please implement redirection of toolchain for " + devEnv);
            }
        }

        public static bool IsLLVMToolchain(this Options.Vc.General.PlatformToolset platformToolset)
        {
            switch (platformToolset)
            {
                case Options.Vc.General.PlatformToolset.LLVM:
                case Options.Vc.General.PlatformToolset.ClangCL:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets whether a <see cref="DevEnv"/> is a Visual Studio version.
        /// </summary>
        /// <param name="devEnv">The <see cref="DevEnv"/> to check.</param>
        /// <returns>`true` if <paramref name="devEnv"/> is a Visual Studio version, `false` otherwise.</returns>
        public static bool IsVisualStudio(this DevEnv devEnv)
        {
            return (0 != (devEnv & DevEnv.VisualStudio));
        }

        private static bool IsAbiCompatibleWithVS2015(this DevEnv devEnv)
        {
            switch (devEnv)
            {
                case DevEnv.vs2015:
                case DevEnv.vs2017:
                case DevEnv.vs2019:
                case DevEnv.vs2022:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets whether two <see cref="DevEnv"/> values generate ABI-compatible binaries with
        /// their respective C++ compiler.
        /// </summary>
        /// <param name="devEnv">The <see cref="DevEnv"/> to check for ABI-compatibility.</param>
        /// <param name="other">The other <see cref="DevEnv"/> to check for ABI-compatibility with.</param>
        /// <returns>`true` if ABI-compatible, `false` otherwise.</returns>
        /// <exception cref="ArgumentException"><paramref name="devEnv"/> is not a Visual Studio version.</exception>
        /// <remarks>
        /// Only works for Visual Studio versions because other DevEnvs (such as Eclipse) are not
        /// shipped with a compiler version.
        /// </remarks>
        public static bool IsAbiCompatibleWith(this DevEnv devEnv, DevEnv other)
        {
            if (!devEnv.IsVisualStudio())
                throw new ArgumentException($"{devEnv} is not a Visual Studio DevEnv.");

            // a VS version is obviously compatible with itself (identity check)
            if (devEnv == other)
                return true;

            // VS2017/VS2019/VS2022 are guaranteed by Microsoft to be ABI-compatible with VS2015 for C++.
            if (devEnv.IsAbiCompatibleWithVS2015() && other.IsAbiCompatibleWithVS2015())
                return true;

            return false;
        }

        public static int IndexOf<T>(this T[] source, T value)
        {
            return IndexOf<T>(source, value, StringComparison.InvariantCultureIgnoreCase);
        }

        public static int IndexOf<T>(this T[] source, T value, StringComparison stringComparison)
        {
            if (typeof(T) == typeof(string))
                return Array.FindIndex(source, m => m.ToString().Equals(value.ToString(), stringComparison));
            else
                return Array.IndexOf(source, value);
        }
    }
}
