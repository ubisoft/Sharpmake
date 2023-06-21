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
    public class HelloKitFrameworkProject : CommonProject
    {
        public HelloKitFrameworkProject()
        {
            Name = @"HelloKit";

            AddTargets(CommonTarget.GetMacTargets());
            AddTargets(CommonTarget.GetIosTargets());
            AddTargets(CommonTarget.GetTvosTargets());
            AddTargets(CommonTarget.GetCatalystTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.AppleFramework;
            conf.AddPrivateDependency<FmtProject>(target);
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.InfoPListFile(
                    Path.Join(SourceRootPath, "Info-Framework.plist")
                )
            );
        }
    }
}
