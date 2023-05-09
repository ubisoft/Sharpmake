// Copyright (c) 2022-2023 Ubisoft Entertainment
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
using System.IO;

namespace XCodeProjects
{
    public class CommonSolution : Sharpmake.Solution
    {
        public CommonSolution()
            : base(typeof(CommonTarget))
        {
            IsFileNameToLower = false;
        }

        [ConfigurePriority(ConfigurePriorities.All)]
        [Configure]
        public virtual void ConfigureAll(Configuration conf, CommonTarget target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.Platform]_[target.DevEnv]";
            conf.PlatformName = "[target.SolutionPlatformName]";
            conf.SolutionPath = Path.Combine(Globals.TmpDirectory, "solutions");
        }

#region Platfoms
        [ConfigurePriority(ConfigurePriorities.Platform - 1)]
        [Configure(
            Platform.mac | Platform.ios | Platform.tvos | Platform.watchos | Platform.maccatalyst
        )]
        public virtual void ConfigureApple(Configuration conf, CommonTarget target) { }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.mac)]
        public virtual void ConfigureMacOS(Configuration conf, CommonTarget target) { }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.ios)]
        public virtual void ConfigureIOS(Configuration conf, CommonTarget target) { }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.tvos)]
        public virtual void ConfigureTVOS(Configuration conf, CommonTarget target) { }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.watchos)]
        public virtual void ConfigureWatchOS(Configuration conf, CommonTarget target) { }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.maccatalyst)]
        public virtual void ConfigureCatalyst(Configuration conf, CommonTarget target) { }
#endregion
    }
}
