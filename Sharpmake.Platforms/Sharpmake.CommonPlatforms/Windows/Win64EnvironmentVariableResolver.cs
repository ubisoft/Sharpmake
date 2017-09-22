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
