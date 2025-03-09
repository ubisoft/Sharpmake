// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Sharpmake
{
    public static partial class Windows
    {
        public class Win64EnvironmentVariableResolver : EnvironmentVariableResolver
        {
            private static string s_dxSdkDir;

            [Resolvable]
            [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
            public static string DXSDK_DIR
            {
                get
                {
                    return Util.GetEnvironmentVariable(
                        "DXSDK_DIR",
                        @"c:\Program Files (x86)\Microsoft DirectX SDK (June 2010)\",
                        ref s_dxSdkDir,
                        true);// This SDK is deprecated. Don't warn if variable is not defined.
                }
            }

            public Win64EnvironmentVariableResolver()
            {
            }

            public Win64EnvironmentVariableResolver(params VariableAssignment[] assignments)
                : base(assignments)
            {
            }
        }
    }
}
