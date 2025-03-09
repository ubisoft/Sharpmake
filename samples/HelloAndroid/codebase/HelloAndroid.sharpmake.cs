// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

namespace HelloAndroid
{
    [Sharpmake.Generate]
    public class HelloAndroidSolution : CommonSolution
    {
        public string GradleRootPath = Path.Combine(Globals.TmpDirectory, @"..\..\gradle\root");

        public HelloAndroidSolution()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "HelloAndroid";

            ExePackaging.DirectoryCopyResourceFiles(GradleRootPath, ExePackaging.AndroidPackageProjectsPath + @"\exepackaging");
        }


        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.AddProject<ExePackaging>(target);
            conf.SetStartupProject<ExePackaging>();
        }
    }
}
