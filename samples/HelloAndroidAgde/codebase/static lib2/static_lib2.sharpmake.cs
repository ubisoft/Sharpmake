// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace HelloAndroidAgde
{
    [Sharpmake.Generate]
    public class StaticLib2Project : CommonProject
    {
        public StaticLib2Project()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "static lib2";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "StaticLibs";

            conf.IncludePaths.Add(SourceRootPath);
        }
    }
}
