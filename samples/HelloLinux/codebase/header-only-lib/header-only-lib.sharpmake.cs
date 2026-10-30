// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

namespace HelloLinux
{
    [Sharpmake.Generate]
    public class HeaderOnlyLibProject : CommonProject
    {
        public HeaderOnlyLibProject()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "header-only-lib";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.Utility;
            conf.IncludePaths.Add(SourceRootPath);

            // Headers in this folder are included with -isystem, so warnings in them are not reported.
            conf.IncludeSystemPaths.Add(Path.Combine(SourceRootPath, "system"));
        }
    }
}
