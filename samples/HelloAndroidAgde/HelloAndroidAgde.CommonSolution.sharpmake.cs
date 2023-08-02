// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace HelloAndroidAgde
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
            conf.SolutionFileName += "_[target.DevEnv]";
            conf.PlatformName = "[target.SolutionPlatformName]";
            // It's easy to setup Gradle when put solution file under the projects folder
            conf.SolutionPath = System.IO.Path.Combine(Globals.TmpDirectory, "projects");
        }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.agde)]
        public virtual void ConfigureAgde(Configuration conf, CommonTarget target) { }
    }
}
