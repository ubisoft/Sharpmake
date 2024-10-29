// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;

namespace Sharpmake
{
    public static class ApplePlatform
    {
        public static class Settings
        {
            private static readonly string DefaultXcodeDeveloperPath = "/Applications/Xcode.app/Contents/Developer";

            private static string s_xcodeDevPath;
            public static string XCodeDeveloperPath
            {
                get
                {
                    if (s_xcodeDevPath == null && Util.GetExecutingPlatform() != Platform.mac)
                        throw new Error("ApplePlatform.Settings.XCodeDeveloperPath must be defined when not running on macOS");
                    
                    return s_xcodeDevPath ?? (s_xcodeDevPath = DefaultXcodeDeveloperPath);
                }

                set
                {
                    s_xcodeDevPath = Util.PathMakeStandard(value);
                }
            }

            public static readonly string DefaultMacOSSDKPath = ComputeSdkPath(DefaultXcodeDeveloperPath, "MacOSX");

            private static string s_macOSSDKPath;
            public static string MacOSSDKPath
            {
                get
                {
                    return s_macOSSDKPath ?? (s_macOSSDKPath = ComputeSdkPath(XCodeDeveloperPath, "MacOSX"));
                }

                set
                {
                    s_macOSSDKPath = Util.PathMakeStandard(value);
                }
            }

            public static readonly string DefaultIPhoneOSSDKPath = ComputeSdkPath(DefaultXcodeDeveloperPath, "iPhoneOS");
            private static string s_iphoneOSSDKPath;
            public static string IPhoneOSSDKPath
            {
                get
                {
                    return s_iphoneOSSDKPath ?? (s_iphoneOSSDKPath = ComputeSdkPath(XCodeDeveloperPath, "iPhoneOS"));
                }

                set
                {
                    s_iphoneOSSDKPath = Util.PathMakeStandard(value);
                }
            }


            public static readonly string DefaultMacCatalystSDKPath = ComputeSdkPath(DefaultXcodeDeveloperPath, "iPhoneOS");
            private static string s_macCatalystSDKPath;
            public static string MacCatalystSDKPath
            {
                get
                {
                    return s_macCatalystSDKPath ?? (s_macCatalystSDKPath = ComputeSdkPath(XCodeDeveloperPath, "iPhoneOS"));
                }

                set
                {
                    s_macCatalystSDKPath = Util.PathMakeStandard(value);
                }
            }

            public static readonly string DefaultTVOSSDKPath = ComputeSdkPath(DefaultXcodeDeveloperPath, "AppleTVOS");
            private static string s_tvOSSDKPath;
            public static string TVOSSDKPath
            {
                get
                {
                    return s_tvOSSDKPath ?? (s_tvOSSDKPath = ComputeSdkPath(XCodeDeveloperPath, "AppleTVOS"));
                }

                set
                {
                    s_tvOSSDKPath = Util.PathMakeStandard(value);
                }
            }

            public static readonly string DefaultWatchOSSDKPath = ComputeSdkPath(DefaultXcodeDeveloperPath, "watchOS");
            private static string s_watchOSSDKPath;
            public static string WatchOSSDKPath
            {
                get
                {
                    return s_watchOSSDKPath ?? (s_watchOSSDKPath = ComputeSdkPath(XCodeDeveloperPath, "watchOS"));
                }

                set
                {
                    s_watchOSSDKPath = Util.PathMakeStandard(value);
                }
            }

            private static string ComputeSdkPath(string xcodeDeveloperPath, string sdkBaseName)
            {
                return Path.Combine(xcodeDeveloperPath, "Platforms", $"{sdkBaseName}.platform", "Developer", "SDKs", $"{sdkBaseName}.sdk");
            }
        }
    }
}
