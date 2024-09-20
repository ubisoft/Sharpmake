// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;

namespace Sharpmake
{
    public static class ApplePlatform
    {
        public static class Settings
        {
            private static string s_xcodeDevPath;
            public static string XCodeDeveloperPath
            {
                get
                {
                    if (s_xcodeDevPath == null && Util.GetExecutingPlatform() != Platform.mac)
                        throw new Error("ApplePlatform.Settings.XCodeDeveloperPath must be defined when not running on macOS");
                    
                    return s_xcodeDevPath ?? (s_xcodeDevPath = "/Applications/Xcode.app/Contents/Developer");
                }

                set
                {
                    s_xcodeDevPath = Util.PathMakeStandard(value);
                }
            }

            private static string s_macOSSDKPath;
            public static string MacOSSDKPath
            {
                get
                {
                    return s_macOSSDKPath ?? (s_macOSSDKPath = $"{XCodeDeveloperPath}/Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk");
                }

                set
                {
                    s_macOSSDKPath = Util.PathMakeStandard(value);
                }
            }

            private static string s_iphoneOSSDKPath;
            public static string IPhoneOSSDKPath
            {
                get
                {
                    return s_iphoneOSSDKPath ?? (s_iphoneOSSDKPath = $"{XCodeDeveloperPath}/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk");
                }

                set
                {
                    s_iphoneOSSDKPath = Util.PathMakeStandard(value);
                }
            }

                        
            private static string s_macCatalystSDKPath;
            public static string MacCatalystSDKPath
            {
                get
                {
                    return s_macCatalystSDKPath ?? (s_macCatalystSDKPath = $"{XCodeDeveloperPath}/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk");
                }

                set
                {
                    s_macCatalystSDKPath = Util.PathMakeStandard(value);
                }
            }

            private static string s_tvOSSDKPath;
            public static string TVOSSDKPath
            {
                get
                {
                    return s_tvOSSDKPath ?? (s_tvOSSDKPath = $"{XCodeDeveloperPath}/Platforms/AppleTVOS.platform/Developer/SDKs/AppleTVOS.sdk");
                }

                set
                {
                    s_tvOSSDKPath = Util.PathMakeStandard(value);
                }
            }

            private static string s_watchOSSDKPath;
            public static string WatchOSSDKPath
            {
                get
                {
                    return s_watchOSSDKPath ?? (s_watchOSSDKPath = $"{XCodeDeveloperPath}/Platforms/watchOS.platform/Developer/SDKs/watchOS.sdk");
                }

                set
                {
                    s_watchOSSDKPath = Util.PathMakeStandard(value);
                }
            }

        }
    }
}
