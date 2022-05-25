// Copyright (c) 2020, 2022 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;

namespace Sharpmake
{
    public static partial class Linux
    {
        public interface ISystemPathProvider
        {
            IEnumerable<string> GetSystemIncludePaths(Project.Configuration conf);
            IEnumerable<string> GetSystemLibraryPaths(Project.Configuration conf);
        }

        public static class GlobalSettings
        {
            /// <summary>
            /// Allows setting a custom provider for system paths
            /// </summary>
            public static ISystemPathProvider SystemPathProvider { get; set; } = null;

            /// <summary>
            /// Use llvm-objcopy instead of objcopy from binutils when stripping/extracting debug symbols
            /// from object files
            /// </summary>
            public static bool UseLlvmObjCopy = false;
        }
    }
}
