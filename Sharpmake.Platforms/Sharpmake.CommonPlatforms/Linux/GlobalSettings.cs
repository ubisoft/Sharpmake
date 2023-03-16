// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
