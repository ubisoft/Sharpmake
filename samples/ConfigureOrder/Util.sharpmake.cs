// Copyright (c) 2017, 2019, 2021 Ubisoft Entertainment
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

using Sharpmake;

namespace ConfigureOrdering
{
    public class Util
    {
        private static Target s_defaultTarget;
        public static Target DefaultTarget
        {
            get
            {
                if (s_defaultTarget == null)
                {
                    s_defaultTarget = new Target(
                        Platform.win32,
                        DevEnv.vs2017,
                        Optimization.Release,
                        OutputType.Lib,
                        Blob.NoBlob,
                        BuildSystem.MSBuild,
                        DotNetFramework.v4_6_2
                    );
                }
                return s_defaultTarget;
            }
        }
    }
}
