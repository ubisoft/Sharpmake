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
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Sharpmake
{
    public static partial class Durango
    {
        public static class Util
        {
            ////////////////////////////////////////////////////////////////////////
            /// Those methods apply to what's *installed* on the machine
            /// Warning: those are not necessarily the ones used by your projects
            private static string s_durangoXDKInstallPath = null;
            public static string GetDurangoXDKInstallPath()
            {
                if (s_durangoXDKInstallPath == null)
                {
                    string registryKeyString = string.Format(@"SOFTWARE{0}\Microsoft\Durango XDK",
                        Environment.Is64BitProcess ? @"\Wow6432Node" : string.Empty);
                    using (RegistryKey localMachineKey = Registry.LocalMachine.OpenSubKey(registryKeyString))
                    {
                        if (localMachineKey != null)
                            s_durangoXDKInstallPath = (string)localMachineKey.GetValue("InstallPath") ?? string.Empty;
                        else
                            s_durangoXDKInstallPath = string.Empty;
                    }
                }
                return s_durangoXDKInstallPath;
            }

            private static string s_latestDurangoSideBySideXDKInstalled = null;
            public static string GetLatestDurangoSideBySideXDKInstalled()
            {
                if (s_latestDurangoSideBySideXDKInstalled == null)
                {
                    // this key appeared with November 2015 XDK, the first to allow Side by Side
                    string registryKeyString = string.Format(@"SOFTWARE{0}\Microsoft\Durango XDK",
                        Environment.Is64BitProcess ? @"\Wow6432Node" : string.Empty);
                    using (RegistryKey localMachineKey = Registry.LocalMachine.OpenSubKey(registryKeyString))
                    {
                        if (localMachineKey != null)
                            s_latestDurangoSideBySideXDKInstalled = (string)localMachineKey.GetValue("Latest") ?? string.Empty;
                        else
                            s_latestDurangoSideBySideXDKInstalled = string.Empty;
                    }
                }
                return s_latestDurangoSideBySideXDKInstalled;
            }
            public static bool IsDurangoSideBySideXDKInstalled()
            {
                return !string.IsNullOrEmpty(GetLatestDurangoSideBySideXDKInstalled());
            }

            ////////////////////////////////////////////////////////////////////////
            /// Those methods will check the XDK in use
            private static string s_durangoSideBySideXDKLatest = null;
            public static string GetLatestDurangoSideBySideXDK()
            {
                if (s_durangoSideBySideXDKLatest == null)
                {
                    if (Directory.Exists(GlobalSettings.DurangoXDK))
                    {
                        var xdkFolders = Sharpmake.Util.DirectoryGetDirectories(GlobalSettings.DurangoXDK);
                        // if the XDK is a Side by Side (November 2015 or more recent),
                        // we will find folders named after the edition number,
                        // following this pattern: YYMMQQ: 6 digit numbers divided into 3 two digit parts.
                        // The first two digits represent the calendar year in which the XDK was released,
                        // the second two digits represent the month, and the final two digits are a release, or QFE, number.
                        int latestValue = 0;
                        foreach (var folder in xdkFolders.Select(x => Path.GetFileName(x)))
                        {
                            int current = 0;
                            if (folder.Length >= 6 && int.TryParse(folder.Substring(0, 6), out current))
                            {
                                if (current > latestValue)
                                {
                                    latestValue = current;
                                    s_durangoSideBySideXDKLatest = folder;
                                }
                            }
                        }
                    }

                    if (s_durangoSideBySideXDKLatest == null)
                        s_durangoSideBySideXDKLatest = string.Empty;
                }
                return s_durangoSideBySideXDKLatest;
            }
            public static bool IsDurangoSideBySideXDK()
            {
                return !string.IsNullOrEmpty(GetLatestDurangoSideBySideXDK());
            }

            private static string s_durangoExtensionXDK = null;
            public static string GetDurangoExtensionXDK()
            {
                if (s_durangoExtensionXDK == null)
                {
                    if (IsDurangoSideBySideXDK())
                        s_durangoExtensionXDK = Sharpmake.Util.EnsureTrailingSeparator(Path.Combine(GlobalSettings.XboxOneExtensionSDK, "Durango." + GlobalSettings.XdkEditionTarget, "v8.0"));
                    else
                        s_durangoExtensionXDK = Sharpmake.Util.EnsureTrailingSeparator(Path.Combine(GlobalSettings.DurangoXDK, "xdk"));
                }
                return s_durangoExtensionXDK;
            }

            public static void AddDurangoSDKReferences(Project.Configuration conf, string[] xboxExtensions, bool forceReferenceByPath = false)
            {
                if (conf.IsFastBuild || forceReferenceByPath)
                {
                    // With Nov2015 XDK, the Durango Extensions folder changed, but the variable will point to the correct folder
                    // the folder was renamed from Extensions, to ExtensionSDKs
                    string extensionsFolderName = GetDurangoExtensionXDK();
                    string extensionsSubFolderName = IsDurangoSideBySideXDK() ? "ExtensionSDKs" : "Extensions";

                    string winmdReferencesFormatPath = Path.Combine(extensionsFolderName, extensionsSubFolderName, @"Xbox {0} API\8.0\References\CommonConfiguration\neutral\Microsoft.Xbox.{0}.winmd");
                    string winmdExtensionsFormatPath = Path.Combine(extensionsFolderName, extensionsSubFolderName, @"Xbox {0} API\8.0\Redist\CommonConfiguration\neutral");

                    foreach (string xboxExtension in xboxExtensions)
                    {
                        conf.ReferencesByPath.Add(string.Format(winmdReferencesFormatPath, xboxExtension));

                        try
                        {
                            // Copy Microsoft.Xbox.*.dll/.pdb files
                            string[] xboxServicesFiles = Sharpmake.Util.DirectoryGetFiles(string.Format(winmdExtensionsFormatPath, xboxExtension));
                            conf.TargetCopyFiles.Add(xboxServicesFiles);
                        }
                        catch { }
                    }
                }
                else
                {
                    foreach (string xboxExtension in xboxExtensions)
                    {
                        conf.Options.Add(new Options.SDKReferences(string.Format("Xbox {0} API, Version=8.0", xboxExtension)));
                    }
                }
            }
        }
    }
}