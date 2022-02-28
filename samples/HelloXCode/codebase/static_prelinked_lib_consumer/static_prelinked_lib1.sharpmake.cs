// Copyright (c) 2020 Ubisoft Entertainment
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

using System.Collections.Generic;
using System.IO;
using Sharpmake;

namespace HelloXCode
{
    /// <summary>
    /// This project tests the XCode's Pre-Linked libraries feature.
    /// Project includes (but does not link) the sub library (consumed) that is then pre-linked by XCode
    /// the new PrelinkLibraries options. The EXE using this library will then be able to link the consumed library even though
    /// it is not in actually used in EXE.
    /// </summary>
    [Sharpmake.Generate]
    public class StaticPrelinkedLibConsumerProject : CommonProject
    {
        public StaticPrelinkedLibConsumerProject()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "static_prelinked_lib_consumer";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "StaticLibs";

            conf.IncludePaths.Add(SourceRootPath);

            //  Important! do not link the Consumed library - let it be pre-linked by XCode
            conf.AddPrivateDependency<StaticPrelinkedLibConsumed>(target, DependencySetting.DefaultWithoutLinking);

            //  Custom build step - to generate the sub library
            var platform = target.GetPlatform().HasFlag(Platform.mac) ? "mac" : "ios";
            var projPath = Path.Combine(Globals.TmpDirectory, "projects/static_prelinked_lib_consumed");
            conf.EventPreBuild.Add($"xcodebuild build -scheme static_prelinked_lib_consumed_{platform} -project {projPath}/static_prelinked_lib_consumed_{platform}.xcodeproj");

            //  Test pre-linked libraries
            var libraryToPrelink = Path.Combine(conf.TargetLibraryPath, "..", "static_prelinked_lib_consumed", "libstatic_prelinked_lib_consumed.a");

            conf.Options.Add(Options.XCode.Linker.PerformSingleObjectPrelink.Enable);
            conf.Options.Add(new Options.XCode.Linker.PrelinkLibraries(libraryToPrelink));
        }
    }
}
