// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Generate]
    public class ReadAppDataProject : CommonProject
    {
        public ReadAppDataProject()
        {
            Name = @"ReadAppData";

            AddTargets(CommonTarget.GetMacTargets());
            AddTargets(CommonTarget.GetIosTargets());
            AddTargets(CommonTarget.GetTvosTargets());
            AddTargets(CommonTarget.GetCatalystTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;
        }

        private bool _fileCopyAddedForFastbuild = false;
        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.AppleApp;
            conf.AddPublicDependency<FmtProject>(target);

            conf.XcodeSystemFrameworks.Add("CoreFoundation");

            if (_fileCopyAddedForFastbuild && conf.IsFastBuild)
                return;
            if (conf.IsFastBuild)
                _fileCopyAddedForFastbuild = true;

            conf.TargetCopyFilesPath = Path.Join(@"./");
            conf.TargetCopyFiles.Add("foobar.dat");
            conf.TargetCopyFilesToSubDirectory.Add(
                new KeyValuePair<string, string>(Path.Join("huba", "hoge.dat"), @"huba")
            );
            conf.EventPostBuildCopies.Add(
                new KeyValuePair<string, string>(Path.Join("huba", "fuga.dat"), @"huba")
            );

            conf.EventPreBuildExe.Add(
                new Configuration.BuildStepCopy(
                    Path.Join(SourceRootPath, "huba", "fuga.dat"),
                    @"hogepre"
                )
            );
            conf.EventPostBuildExe.Add(
                new Configuration.BuildStepCopy(
                    Path.Join(SourceRootPath, "huba", "fuga.dat"),
                    @"hogepost"
                )
            );
            conf.EventCustomPreBuildExe.Add(
                new Configuration.BuildStepCopy(
                    Path.Join(SourceRootPath, "huba", "fuga.dat"),
                    @"hogeprecustom"
                )
            );
            conf.EventCustomPostBuildExe.Add(
                new Configuration.BuildStepCopy(
                    Path.Join(SourceRootPath, "huba", "fuga.dat"),
                    @"hogepostcustom"
                )
            );
        }
    }
}
