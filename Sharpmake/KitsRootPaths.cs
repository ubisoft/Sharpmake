using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharpmake
{
    public class KitsRootPaths
    {
        private static Dictionary<DevEnv, KitsRootEnum> s_defaultKitsRootForDevEnv = new Dictionary<DevEnv, KitsRootEnum>();
        private static Dictionary<KitsRootEnum, string> s_defaultKitsRoots = new Dictionary<KitsRootEnum, string>();

        private static Dictionary<DevEnv, KitsRootEnum> s_useKitsRootForDevEnv = new Dictionary<DevEnv, KitsRootEnum>();
        private static Dictionary<KitsRootEnum, string> s_kitsRoots = new Dictionary<KitsRootEnum, string>();

        private static Dictionary<DotNetFramework, string> s_netFxKitsDir = new Dictionary<DotNetFramework, string>();

        public static Options.Vc.General.WindowsTargetPlatformVersion WindowsTargetPlatformVersion { get; private set; } = Options.Vc.General.WindowsTargetPlatformVersion.v8_1;

        private static KitsRootPaths s_kitsRootsInstance = new KitsRootPaths();

        public KitsRootPaths()
        {
            string kitsRegistryKeyString = string.Format(@"SOFTWARE{0}\Microsoft\Windows Kits\Installed Roots",
                Environment.Is64BitProcess ? @"\Wow6432Node" : string.Empty);

            s_defaultKitsRoots[KitsRootEnum.KitsRoot] = Util.GetRegistryLocalMachineSubKeyValue(kitsRegistryKeyString, KitsRootEnum.KitsRoot.ToString(), @"C:\Program Files (x86)\Windows Kits\8.0\");
            s_defaultKitsRoots[KitsRootEnum.KitsRoot81] = Util.GetRegistryLocalMachineSubKeyValue(kitsRegistryKeyString, KitsRootEnum.KitsRoot81.ToString(), @"C:\Program Files (x86)\Windows Kits\8.1\");
            s_defaultKitsRoots[KitsRootEnum.KitsRoot10] = Util.GetRegistryLocalMachineSubKeyValue(kitsRegistryKeyString, KitsRootEnum.KitsRoot10.ToString(), @"C:\Program Files (x86)\Windows Kits\10\");

            var netFXSdkRegistryKeyString = string.Format(@"SOFTWARE{0}\Microsoft\Microsoft SDKs\NETFXSDK",
                Environment.Is64BitProcess ? @"\Wow6432Node" : string.Empty);
            foreach (var dotNet in Enum.GetValues(typeof(DotNetFramework)).Cast<DotNetFramework>().Where(d => d >= DotNetFramework.v4_6))
            {
                s_netFxKitsDir[dotNet] = Util.GetRegistryLocalMachineSubKeyValue(netFXSdkRegistryKeyString + @"\" + dotNet.ToVersionString(), "KitsInstallationFolder", $@"C:\Program Files (x86)\Windows Kits\NETFXSDK\{dotNet.ToVersionString()}\");
            }

            s_defaultKitsRootForDevEnv[DevEnv.vs2012] = KitsRootEnum.KitsRoot;
            s_defaultKitsRootForDevEnv[DevEnv.vs2013] = KitsRootEnum.KitsRoot81;
            s_defaultKitsRootForDevEnv[DevEnv.vs2015] = KitsRootEnum.KitsRoot81;
            s_defaultKitsRootForDevEnv[DevEnv.vs2017] = KitsRootEnum.KitsRoot10;
        }

        public static string GetRoot(KitsRootEnum kitsRoot)
        {
            if (s_kitsRootsInstance == null)
                throw new Error();

            if (s_kitsRoots.ContainsKey(kitsRoot))
                return s_kitsRoots[kitsRoot];

            if (s_defaultKitsRoots.ContainsKey(kitsRoot))
                return s_defaultKitsRoots[kitsRoot];

            throw new NotImplementedException("No Root associated with " + kitsRoot.ToString());
        }

        public static string GetDefaultRoot(KitsRootEnum kitsRoot)
        {
            if (s_kitsRootsInstance == null)
                throw new Error();

            if (s_defaultKitsRoots.ContainsKey(kitsRoot))
                return s_defaultKitsRoots[kitsRoot];

            throw new NotImplementedException("No DefaultKitsRoots associated with " + kitsRoot.ToString());
        }

        public static void SetRoot(KitsRootEnum kitsRoot, string kitsRootPath)
        {
            s_kitsRoots[kitsRoot] = kitsRootPath;
        }

        public static KitsRootEnum GetUseKitsRootForDevEnv(DevEnv devEnv)
        {
            if (s_useKitsRootForDevEnv.ContainsKey(devEnv))
                return s_useKitsRootForDevEnv[devEnv];

            if (s_defaultKitsRootForDevEnv.ContainsKey(devEnv))
                return s_defaultKitsRootForDevEnv[devEnv];

            throw new NotImplementedException("No UseKitsRoot associated with " + devEnv.ToString());
        }

        public static bool IsDefaultKitRootPath(DevEnv devEnv)
        {
            KitsRootEnum kitsRoot = GetUseKitsRootForDevEnv(devEnv);
            return GetDefaultRoot(kitsRoot) == GetRoot(kitsRoot);
        }

        public static void SetUseKitsRootForDevEnv(DevEnv devEnv, KitsRootEnum kitsRoot, Options.Vc.General.WindowsTargetPlatformVersion? windowsTargetPlatformVersion = null)
        {
            s_useKitsRootForDevEnv[devEnv] = kitsRoot;
            switch (kitsRoot)
            {
                case KitsRootEnum.KitsRoot:
                    if (windowsTargetPlatformVersion.HasValue)
                        throw new Error("Unsupported setting: WindowsTargetPlatformVersion is not customizable for KitsRoot 8.0.");
                    break;
                case KitsRootEnum.KitsRoot81:
                    if (windowsTargetPlatformVersion.HasValue && windowsTargetPlatformVersion.Value != Options.Vc.General.WindowsTargetPlatformVersion.v8_1)
                        throw new Error("Unsupported setting: WindowsTargetPlatformVersion is not customizable for KitsRoot 8.1. Redundant setting will be discarded");
                    break;
                case KitsRootEnum.KitsRoot10:
                    if (!windowsTargetPlatformVersion.HasValue)
                        windowsTargetPlatformVersion = Options.Vc.General.WindowsTargetPlatformVersion.v10_0_10586_0;

                    if (windowsTargetPlatformVersion.Value == Options.Vc.General.WindowsTargetPlatformVersion.v8_1)
                        throw new Error("Inconsistent values detected: KitsRoot10 set for " + devEnv + ", but windowsTargetPlatform is set to 8.1");

                    WindowsTargetPlatformVersion = windowsTargetPlatformVersion.Value;
                    break;
            }
        }

        public static string GetWindowsTargetPlatformVersion()
        {
            switch (WindowsTargetPlatformVersion)
            {
                case Options.Vc.General.WindowsTargetPlatformVersion.v8_1: return "8.1";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_10240_0: return "10.0.10240.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_10586_0: return "10.0.10586.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_14393_0: return "10.0.14393.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_15063_0: return "10.0.15063.0";
                case Options.Vc.General.WindowsTargetPlatformVersion.v10_0_16299_0: return "10.0.16299.0";
                default:
                    throw new ArgumentOutOfRangeException("WindowsTargetPlatformVersion");
            }
        }

        public static string GetNETFXKitsDir(DotNetFramework dotNetFramework)
        {
            if (s_netFxKitsDir.ContainsKey(dotNetFramework))
                return s_netFxKitsDir[dotNetFramework];

            throw new NotImplementedException("No NETFXKitsDir associated with " + dotNetFramework.ToString());
        }
    }
}