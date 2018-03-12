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
using System.Linq;

namespace Sharpmake
{
    /// <summary>
    /// This class contains some global msbuild settings
    /// </summary>
    public class MSBuildGlobalSettings
    {
        private static readonly ConcurrentDictionary<Tuple<DevEnv, Platform>, string> s_cppPlatformFolders = new ConcurrentDictionary<Tuple<DevEnv, Platform>, string>();

        /// <summary>
        /// Allows overwriting the MSBuild platform folder used for a given platform and Visual Studio version. 
        /// This is typically used if you want to put VS files in source control such as Perforce or nuget.
        /// </summary>
        /// <param name="devEnv">Visual studio version affected</param>
        /// <param name="platform">Platform affected</param>
        /// <param name="value">The location of the MSBuild platform folder. Warning: this *must* end with a trailing separator</param>
        /// <returns></returns>
        public static void SetCppPlatformFolder(DevEnv devEnv, Platform platform, string value)
        {
            Tuple<DevEnv, Platform> key = Tuple.Create(devEnv, platform);
            if (!s_cppPlatformFolders.TryAdd(key, value))
                throw new Error("You can't register more than once a platform folder for a specific combinaison. Key already registered: " + key);
        }

        /// <summary>
        /// Get the overwritten MSBuild platform folder used for a given platform and Visual studio version.
        /// This is typically used if you want to put your VS files in source control such as Perforce or nuget.
        /// </summary>
        /// <param name="devEnv">Visual studio version affected</param>
        /// <param name="platform">Platform affected</param>
        /// <returns>the registered msbuild foldervalue for the requested pair. null if not found</returns>
        public static string GetCppPlatformFolder(DevEnv devEnv, Platform platform)
        {
            Tuple<DevEnv, Platform> key = Tuple.Create(devEnv, platform);
            string value;
            if (s_cppPlatformFolders.TryGetValue(key, out value))
                return value;
            return null; // No override found
        }
    }
}
