// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Export]
    public class FmtProject : CommonProject
    {
        public FmtProject()
        {
            Name = @"fmt";

            AddTargets(CommonTarget.GetMacTargets());
            AddTargets(CommonTarget.GetIosTargets());
            AddTargets(CommonTarget.GetTvosTargets());
            AddTargets(CommonTarget.GetCatalystTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);
            conf.IncludePaths.Add(Path.Join(SourceRootPath, @"include"));
            conf.Defines.Add("FMT_HEADER_ONLY=1");
            conf.ExportDefines.Add("FMT_HEADER_ONLY=1");

            conf.Output = Configuration.OutputType.None;
        }
    }
}
