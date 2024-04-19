// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Generate]
    public class ReadExeDataProject : CommonProject
    {
        public ReadExeDataProject()
        {
            Name = @"ReadExeData";

            AddTargets(CommonTarget.GetDefaultTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.Exe;
            conf.PrecompHeader = "stdafx.h";
            conf.PrecompSource = "stdafx.cpp";
            conf.TargetCopyFiles.Add("foobar.dat");
        }
    }
}
