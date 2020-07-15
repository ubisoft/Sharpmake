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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sharpmake
{
    public class KitsRootPaths
    {
        private static Dictionary<DevEnv, Tuple<KitsRootEnum, Options.Vc.General.WindowsTargetPlatformVersion?>> s_defaultKitsRootForDevEnv = new Dictionary<DevEnv, Tuple<KitsRootEnum, Options.Vc.General.WindowsTargetPlatformVersion?>>();
        private static Dictionary<KitsRootEnum, string> s_defaultKitsRoots = new Dictionary<KitsRootEnum, string>();

        private static Dictionary<DevEnv, Tuple<KitsRootEnum, Options.Vc.General.WindowsTargetPlatformVersion?>> s_useKitsRootForDevEnv = new Dictionary<DevEnv, Tuple<KitsRootEnum, Options.Vc.General.WindowsTargetPlatformVersion?>>();
        private static Dictionary<KitsRootEnum, string> s_kitsRoots = new Dictionary<KitsRootEnum, string>();

        private static readonly ConcurrentDictionary<DotNetFramework, string> s_netFxKitsDir = new ConcurrentDictionary<DotNetFramework, string>();

        private static readonly ConcurrentDictionary<DotNetFramework, string> s_netFxToolsDir = new ConcurrentDictionary<DotNetFramework, string>();

        private static ConcurrentDictionary<Options.Vc.General.WindowsTargetPlatformVersion, bool> s_windowsTargetPlatformVersionInstalled = new ConcurrentDictionary<Options.Vc.General.WindowsTargetPlatformVersion, bool>();

        [Obsolete("WindowsTargetPlatformVersion is per DevEnv, please use " + nameof(GetWindowsTargetPlatformVersionForDevEnv) + " instead", error: true)]
        public static Options.Vc.General.WindowsTargetPlatformVersion WindowsTargetPlatformVersion { get; private set; } = Options.Vc.General.WindowsTargetPlatformVersion.v8_1;

        private static KitsRootPaths s_kitsRootsInstance = new KitsRootPaths();

        public KitsRootPaths()
        {
            string kitsRegistryKeyString = string.Format(@"SOFTWARE{0}\Microsoft\Windows Kits\Installed Roots",
                Environment.Is64BitProcess ? @"\Wow6432Node" : string.Empty);

            s_defaultKitsRoots[KitsRootEnum.KitsRoot] = Util.GetRegistryLocalMachineSubKeyValue(kitsRegistryKeyString, KitsRootEnum.KitsRoot.ToString(), @"C:\Program Files (x86)\Windows Kits\8.0\");
            s_defaultKitsRoots[KitsRootEnum.KitsRoot81] = Util.GetRegistryLocalMachineSubKeyValue(kitsRegistryKeyString, KitsRootEnum.KitsRoot81.ToString(), @"C:\Program Files (x86)\Windows Kits\8.1\");
            s_defaultKitsRoots[KitsRootEnum.KitsRoot10] = Util.GetRegistryLocalMachineSubKeyValue(kitsRegistryKeyString, KitsRootEnum.KitsRoot10.ToString(), @"C:\Program Files (x86)\Windows Kits\10\");

            s_defaultKitsRootForDevEnv[DevEnv.vs2010] = Tuple.Create<KitsRootEnum, Options.Vc.General.WindowsTargetPlatformVersion?>(KitsRootEnum.KitsRoot, null);
            s_defaultKitsRootForDevEnv[DevEnv.vs2012] = Tuple.Create<KitsRootEnum, Options.Vc.General.WindowsTargetPlatformVersion?>(KitsRootEnum.KitsRoot, null);
            s_defaultKitsRootForDevEnv[DevEnv.vs2013] = Tuple.Create<KitsRootEnum, Options.Vc.General.WindowsTargetPlatformVersion?>(KitsRootEnum.KitsRoot81, Options.Vc.General.WindowsTargetPlatformVersion.v8_1);
            s_defaultKitsRootForDevEnv[DevEnv.vs2015] = Tuple.Create<KitsRootEnum, Options.Vc.General.WindowsTargetPlatformVersion?>(KitsRootEnum.KitsRoot81, Options.Vc.General.WindowsTargetPlatformVersion.v8_1);
            s_defaultKitsRootForDevEnv[DevEnv.vs2017] = Tuple.Create<KitsRootEnum, Options.Vc.General.WindowsTargetPlatformVersion?>(KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_10586_0);
            s_defaultKitsRootForDevEnv[DevEnv.vs2019] = Tuple.Create<KitsRootEnum, Options.Vc.General.WindowsTargetPlatformVersion?>(KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_16299_0);
        }

        public static string GetRoot(KitsRootEnum kitsRoot)
        {
            if (s_kitsRootsInstance == null)
                throw new Error();

            if (s_kitsRoots.ContainsKey(kitsRoot))
                return s_kitsRoots[kitsRoot];

            return GetDefaultRoot(kitsRoot);
        }

        public static string GetDefaultRoot(KitsRootEnum kitsRoot)
        {
            if (s_kitsRootsInstance == null)
                throw new Error();

            if (s_defaultKitsRoots.ContainsKey(kitsRoot))
                return s_defaultKitsRoots[kitsRoot];

            throw new NotImplementedException("DefaultKitsRoots path was not set with " + kitsRoot);
        }

        public static void SetRoot(KitsRootEnum kitsRoot, string kitsRootPath)
        {
            s_kitsRoots[kitsRoot] = kitsRootPath;
        }

        public static bool IsKitsRootForDevEnvOverriden(DevEnv devEnv)
        {
            return s_useKitsRootForDevEnv.ContainsKey(devEnv);
        }

        public static KitsRootEnum GetUseKitsRootForDevEnv(DevEnv devEnv)
        {
            if (s_useKitsRootForDevEnv.ContainsKey(devEnv))
                return s_useKitsRootForDevEnv[devEnv].Item1;

            if (s_defaultKitsRootForDevEnv.ContainsKey(devEnv))
                return s_defaultKitsRootForDevEnv[devEnv].Item1;

            throw new NotImplementedException("No KitsRoot to use with " + devEnv);
        }

        public static bool UsesDefaultKitRoot(DevEnv devEnv)
        {
            KitsRootEnum kitsRoot = GetUseKitsRootForDevEnv(devEnv);
            return kitsRoot == s_defaultKitsRootForDevEnv[devEnv].Item1;
        }

        public static bool IsDefaultKitRootPath(DevEnv devEnv)
        {
            KitsRootEnum kitsRoot = GetUseKitsRootForDevEnv(devEnv);
            return GetDefaultRoot(kitsRoot) == GetRoot(kitsRoot);
        }

        public static void SetUseKitsRootForDevEnv(DevEnv devEnv, KitsRootEnum kitsRoot, Options.Vc.General.WindowsTargetPlatformVersion? windowsTargetPlatformVersion = null)
        {
            windowsTargetPlatformVersion = windowsTargetPlatformVersion ?? s_defaultKitsRootForDevEnv[devEnv].Item2;

            switch (kitsRoot)
            {
                case KitsRootEnum.KitsRoot:
                    if (windowsTargetPlatformVersion.HasValue)
                        throw new Error("Unsupported setting: WindowsTargetPlatformVersion is not customizable for KitsRoot 8.0.");
                    break;
                case KitsRootEnum.KitsRoot81:
                    if (windowsTargetPlatformVersion.HasValue && windowsTargetPlatformVersion.Value != Options.Vc.General.WindowsTargetPlatformVersion.v8_1)
                        throw new Error("Unsupported setting: WindowsTargetPlatformVersion is not customizable for KitsRoot 8.1.");
                    break;
                case KitsRootEnum.KitsRoot10:
                    if (!windowsTargetPlatformVersion.HasValue)
                        throw new Error("KitsRoot10 needs to be set for " + devEnv + ".");

                    if (windowsTargetPlatformVersion == Options.Vc.General.WindowsTargetPlatformVersion.v8_1)
                        throw new Error("Inconsistent values detected: KitsRoot10 set for " + devEnv + ", but windowsTargetPlatform is set to 8.1");

                    break;
            }
            s_useKitsRootForDevEnv[devEnv] = Tuple.Create(kitsRoot, windowsTargetPlatformVersion);
        }

        public static Options.Vc.General.WindowsTargetPlatformVersion GetWindowsTargetPlatformVersionForDevEnv(DevEnv devEnv)
        {
            Options.Vc.General.WindowsTargetPlatformVersion? version = null;
            if (s_useKitsRootForDevEnv.ContainsKey(devEnv))
                version = s_useKitsRootForDevEnv[devEnv].Item2;
            else if (s_defaultKitsRootForDevEnv.ContainsKey(devEnv))
                version = s_defaultKitsRootForDevEnv[devEnv].Item2;

            if (version != null)
                return version.Value;

            throw new NotImplementedException("No WindowsTargetPlatformVersion associated with " + devEnv);
        }

        [Obsolete("WindowsTargetPlatformVersion is per DevEnv, please use " + nameof(GetWindowsTargetPlatformVersionForDevEnv) + " instead", error: true)]
        public static string GetWindowsTargetPlatformVersion()
        {
            throw new Error();
        }

        public static string GetNETFXKitsDir(DotNetFramework dotNetFramework)
        {
            string netFxKitsDir;
            if (s_netFxKitsDir.TryGetValue(dotNetFramework, out netFxKitsDir))
                return netFxKitsDir;

            if (dotNetFramework >= DotNetFramework.v4_6)
            {
                var netFXSdkRegistryKeyString = string.Format(@"SOFTWARE{0}\Microsoft\Microsoft SDKs\NETFXSDK",
                    Environment.Is64BitProcess ? @"\Wow6432Node" : string.Empty);

                netFxKitsDir = Util.GetRegistryLocalMachineSubKeyValue(netFXSdkRegistryKeyString + @"\" + dotNetFramework.ToVersionString(), "KitsInstallationFolder", $@"C:\Program Files (x86)\Windows Kits\NETFXSDK\{dotNetFramework.ToVersionString()}\");

                s_netFxKitsDir.TryAdd(dotNetFramework, netFxKitsDir);

                return netFxKitsDir;
            }

            throw new NotImplementedException("No NETFXKitsDir associated with " + dotNetFramework);
        }

        public static string GetNETFXToolsDir(DotNetFramework dotNetFramework)
        {
            string netFxToolsDir;
            if (s_netFxToolsDir.TryGetValue(dotNetFramework, out netFxToolsDir))
                return netFxToolsDir;

            var microsoftSdksRegistryKeyString = string.Format(@"SOFTWARE{0}\Microsoft\Microsoft SDKs",
                Environment.Is64BitProcess ? @"\Wow6432Node" : string.Empty);

            if (dotNetFramework >= DotNetFramework.v4_6)
            {
                netFxToolsDir = Util.GetRegistryLocalMachineSubKeyValue(
                    $@"{microsoftSdksRegistryKeyString}\NETFXSDK\{dotNetFramework.ToVersionString()}\WinSDK-NetFx40Tools-x86",
                    "InstallationFolder",
                    $@"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX {dotNetFramework.ToVersionString()} Tools\");
            }
            else if (dotNetFramework >= DotNetFramework.v4_5_1) // Note: .Net 4.5.2 lacks a NETFX tools release, so we use the previous version
            {
                netFxToolsDir = Util.GetRegistryLocalMachineSubKeyValue(
                    $@"{microsoftSdksRegistryKeyString}\Windows\v8.1A\WinSDK-NetFx40Tools-x86",
                    "InstallationFolder",
                    $@"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\");
            }
            else if (dotNetFramework >= DotNetFramework.v4_0)
            {
                netFxToolsDir = Util.GetRegistryLocalMachineSubKeyValue(
                    $@"{microsoftSdksRegistryKeyString}\Windows\v8.0A\WinSDK-NetFx40Tools-x86",
                    "InstallationFolder",
                    $@"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\");
            }
            else if (dotNetFramework >= DotNetFramework.v3_5)
            {
                netFxToolsDir = Util.GetRegistryLocalMachineSubKeyValue(
                    $@"{microsoftSdksRegistryKeyString}\Windows\v8.0A\WinSDK-NetFx35Tools-x86",
                    "InstallationFolder",
                    $@"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\bin\");
            }
            else
            {
                throw new NotImplementedException("No NETFXToolsDir associated with " + dotNetFramework);
            }

            s_netFxToolsDir.TryAdd(dotNetFramework, netFxToolsDir);
            return netFxToolsDir;
        }

        public static bool IsWindowsTargetPlatformVersionInstalled(Options.Vc.General.WindowsTargetPlatformVersion version)
        {
            bool isInstalled = false;
            if (s_windowsTargetPlatformVersionInstalled.TryGetValue(version, out isInstalled))
            {
                return isInstalled;
            }

            // cache which version folders exist on the current system
            string path;
            if (version == Options.Vc.General.WindowsTargetPlatformVersion.v8_1)
            {
                path = s_defaultKitsRoots[KitsRootEnum.KitsRoot81];
            }
            else
            {
                path = Path.Combine(s_defaultKitsRoots[KitsRootEnum.KitsRoot10], "Include", version.ToVersionString());
            }

            isInstalled = Directory.Exists(path);
            s_windowsTargetPlatformVersionInstalled.TryAdd(version, isInstalled);

            return isInstalled;
        }

        public static void SetKitsRoot10ToHighestInstalledVersion(DevEnv? devEnv = null)
        {
            Options.Vc.General.WindowsTargetPlatformVersion? highestVersion = null;

            var targetVersions = EnumUtils.EnumerateValues<Options.Vc.General.WindowsTargetPlatformVersion>().Reverse();
            foreach (var version in targetVersions)
            {
                if (IsWindowsTargetPlatformVersionInstalled(version))
                {
                    highestVersion = version;
                    break;
                }
            }

            if (devEnv.HasValue)
            {
                SetUseKitsRootForDevEnv(devEnv.Value, KitsRootEnum.KitsRoot10, highestVersion);
            }
            else
            {
                foreach (var env in EnumUtils.EnumerateValues<DevEnv>())
                {
                    SetUseKitsRootForDevEnv(env, KitsRootEnum.KitsRoot10, highestVersion);
                }
            }
        }
    }
}
