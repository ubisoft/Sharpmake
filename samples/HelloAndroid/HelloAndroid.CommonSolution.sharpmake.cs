// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace HelloAndroid
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
            conf.SolutionFileName = "[solution.Name]_[target.Platform]";
            if (target.DevEnv != DevEnv.xcode)
                conf.SolutionFileName += "_[target.DevEnv]";
            conf.PlatformName = "[target.SolutionPlatformName]";
            conf.SolutionPath = System.IO.Path.Combine(Globals.TmpDirectory, "solutions");
        }
    }
}
