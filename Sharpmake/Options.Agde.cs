// Copyright (c) 2022 Ubisoft Entertainment
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

namespace Sharpmake
{
    public static partial class Options
    {
        public static class Agde
        {
            public static class General
            {
                /// <summary>
                /// Name of the gradle app folder/module
                /// </summary>
                public class AndroidApplicationModule : StringOption
                {
                    public AndroidApplicationModule(string androidApplicationModule) : base(androidApplicationModule) { }
                }

                /// <summary>
                /// Intermediate directory of the gradle build process
                /// </summary>
                public class AndroidGradleBuildIntermediateDir : PathOption
                {
                    public AndroidGradleBuildIntermediateDir(string androidGradleBuildIntermediateDir)
                       : base(androidGradleBuildIntermediateDir) { }
                }

                /// <summary>
                /// Output Extra Gradle Arguments for AGDE project which can be set per configuration.
                /// </summary>
                public class AndroidExtraGradleArgs : StringOption
                {
                    public AndroidExtraGradleArgs(string androidExtraGradleArgs)
                       : base(androidExtraGradleArgs) { }
                }
            }
        }
    }
}
