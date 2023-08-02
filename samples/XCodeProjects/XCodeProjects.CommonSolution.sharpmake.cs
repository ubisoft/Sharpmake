// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

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
