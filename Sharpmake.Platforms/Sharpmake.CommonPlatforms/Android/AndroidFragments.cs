// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;

namespace Sharpmake
{
    public static partial class Android
    {
        [Fragment, Flags]
        public enum AndroidBuildTargets
        {
            armeabi_v7a = 1 << 0,
            arm64_v8a = 1 << 1,
            x86 = 1 << 2,
            x86_64 = 1 << 3,
        }

        [Fragment, Flags]
        public enum AndroidBuildType
        {
            Ant = 1 << 0,
            Gradle = 1 << 1
        }
    }
}
