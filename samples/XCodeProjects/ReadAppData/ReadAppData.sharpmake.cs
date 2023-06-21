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
using System.Collections.Generic;

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

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.AppleApp;
            conf.AddPublicDependency<FmtProject>(target);

            conf.XcodeSystemFrameworks.Add("CoreFoundation");

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
