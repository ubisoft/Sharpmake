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
using System.IO;
using System.Linq;

namespace Sharpmake
{
    public static partial class Android
    {
        internal static class Util
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
        }
    }
}
