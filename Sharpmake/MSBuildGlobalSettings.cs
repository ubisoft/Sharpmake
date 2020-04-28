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

namespace Sharpmake
{
    /// <summary>
    /// This class contains some global msbuild settings
    /// </summary>
    public static class MSBuildGlobalSettings
    {
        // cppPlatformFolders (pre-vs2019)
        private static readonly ConcurrentDictionary<Tuple<DevEnv, string>, string> s_cppPlatformFolders = new ConcurrentDictionary<Tuple<DevEnv, string>, string>();

        /// <summary>
        /// Allows overwriting the MSBuild platform folder used for a known sharpmake platform and Visual Studio version.
        /// This is typically used if you want to put VS files in source control such as Perforce or nuget.
        /// </summary>
        /// <param name="devEnv">Visual studio version affected</param>
        /// <param name="platform">Platform affected</param>
        /// <param name="value">The location of the MSBuild platform folder. Warning: this *must* end with a trailing separator</param>
        /// <returns></returns>
        public static void SetCppPlatformFolder(DevEnv devEnv, Platform platform, string value)
        {
            SetCppPlatformFolder(devEnv, platform.ToString(), value);
        }

        /// <summary>
        /// Allows overwriting the MSBuild platform folder used for a custom platform passed as a string and Visual Studio version.
        /// This is typically used if you want to put VS files in source control such as Perforce or nuget.
        /// </summary>
        /// <param name="devEnv">Visual studio version affected</param>
        /// <param name="platform">Platform affected</param>
        /// <param name="value">The location of the MSBuild platform folder. Warning: this *must* end with a trailing separator</param>
        /// <returns></returns>
        public static void SetCppPlatformFolder(DevEnv devEnv, string platform, string value)
        {
            var key = Tuple.Create(devEnv, platform);
            if (!string.Equals(s_cppPlatformFolders.GetOrAdd(key, value), value))
                throw new Error("You can't register more than once a platform folder for a specific combinaison. Key already registered: " + key);
        }

        /// <summary>
        /// Use to reset the override of a platform
        /// </summary>
        /// <param name="devEnv">Visual studio version</param>
        /// <param name="platform">Platform</param>
        public static void ResetCppPlatformFolder(DevEnv devEnv, string platform)
        {
            var key = Tuple.Create(devEnv, platform);
            string value;
            s_cppPlatformFolders.TryRemove(key, out value);
        }

        /// <summary>
        /// Get the overwritten MSBuild platform folder used for a known sharpmake platform and Visual studio version.
        /// This is typically used if you want to put your VS files in source control such as Perforce or nuget.
        /// </summary>
        /// <param name="devEnv">Visual studio version affected</param>
        /// <param name="platform">Platform affected</param>
        /// <returns>the registered msbuild foldervalue for the requested pair. null if not found</returns>
        public static string GetCppPlatformFolder(DevEnv devEnv, Platform platform)
        {
            return GetCppPlatformFolder(devEnv, platform.ToString());
        }

        /// <summary>
        /// Get the overwritten MSBuild platform folder used for a custom platform passed as a string and Visual studio version.
        /// This is typically used if you want to put your VS files in source control such as Perforce or nuget.
        /// </summary>
        /// <param name="devEnv">Visual studio version affected</param>
        /// <param name="platform">Platform affected</param>
        /// <returns>the registered msbuild foldervalue for the requested pair. null if not found</returns>
        public static string GetCppPlatformFolder(DevEnv devEnv, string platform)
        {
            var key = Tuple.Create(devEnv, platform);
            string value;
            if (s_cppPlatformFolders.TryGetValue(key, out value))
                return value;
            return null; // No override found
        }


        // additionalVCTargetsPath (vs2019)
        private static readonly ConcurrentDictionary<Tuple<DevEnv, string>, string> s_additionalVCTargetsPath = new ConcurrentDictionary<Tuple<DevEnv, string>, string>();

        /// <summary>
        /// Allows setting MSBuild vc targets path used for a known sharpmake platform and Visual Studio version.
        /// This is typically used if you want to add platform specific files since vs2019 as the older _PlatformFolder way of doing it is deprecated.
        /// </summary>
        /// <param name="devEnv">Visual studio version affected</param>
        /// <param name="platform">Platform affected</param>
        /// <param name="value">The location of the MSBuild additional VC target path. Warning: this *must* end with a trailing separator</param>
        /// <returns></returns>
        public static void SetAdditionalVCTargetsPath(DevEnv devEnv, Platform platform, string value)
        {
            SetAdditionalVCTargetsPath(devEnv, platform.ToString(), value);
        }

        /// <summary>
        /// Allows setting MSBuild vc target path used for a custom platform passed as a string and Visual Studio version.
        /// This is typically used if you want to add platform specific files with vs2019 as the older way of doing it through _PlatformFolder is deprecated.
        /// </summary>
        /// <param name="devEnv">Visual studio version affected</param>
        /// <param name="platform">Platform affected</param>
        /// <param name="value">The location of the MSBuild additional VC target path. Warning: this *must* end with a trailing separator</param>
        /// <returns></returns>
        public static void SetAdditionalVCTargetsPath(DevEnv devEnv, string platform, string value)
        {
            var key = Tuple.Create(devEnv, platform);
            if (!string.Equals(s_additionalVCTargetsPath.GetOrAdd(key, value), value))
                throw new Error("You can't register more than once an additional VC target path for a specific combinaison. Key already registered: " + key);
        }

        /// <summary>
        /// Use to reset the set of AdditionalVCTargetsPath
        /// </summary>
        /// <param name="devEnv">Visual studio version</param>
        /// <param name="platform">Platform</param>
        public static void ResetAdditionalVCTargetsPath(DevEnv devEnv, string platform)
        {
            var key = Tuple.Create(devEnv, platform);
            string value;
            s_additionalVCTargetsPath.TryRemove(key, out value);
        }

        /// <summary>
        /// Get the MSBuild Additional VC targets path used for a known sharpmake platform and Visual studio version.
        /// This is typically used if you want to add platform specific files with vs2019 as the older way of doing it through _PlatformFolder is deprecated.
        /// </summary>
        /// <param name="devEnv">Visual studio version affected</param>
        /// <param name="platform">Platform affected</param>
        /// <returns>the registered msbuild additional VC targets path for the requested pair. null if not found</returns>
        public static string GetAdditionalVCTargetsPath(DevEnv devEnv, Platform platform)
        {
            return GetAdditionalVCTargetsPath(devEnv, platform.ToString());
        }

        /// <summary>
        /// Get the MSBuild Additional VC targets path used for a custom platform passed as a string and Visual studio version.
        /// This is typically used if you want to add platform specific files with vs2019 as the older way of doing it through _PlatformFolder is deprecated.
        /// </summary>
        /// <param name="devEnv">Visual studio version affected</param>
        /// <param name="platform">Platform affected</param>
        /// <returns>the registered msbuild additional VC targets for the requested pair. null if not found</returns>
        public static string GetAdditionalVCTargetsPath(DevEnv devEnv, string platform)
        {
            var key = Tuple.Create(devEnv, platform);
            string value;
            if (s_additionalVCTargetsPath.TryGetValue(key, out value))
                return value;
            return null; // No override found
        }
    }
}
