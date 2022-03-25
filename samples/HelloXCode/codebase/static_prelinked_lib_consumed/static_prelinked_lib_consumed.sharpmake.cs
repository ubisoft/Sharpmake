// Copyright (c) 2022 Ubisoft Entertainment
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
    /// The library exposes methods and variables that will never be directly linked into EXE file,
    /// but instead, will be pre-linked in "Consumer" library.
    /// </summary>
    [Sharpmake.Generate]
    public class StaticPrelinkedLibConsumed : CommonProject
    {
        public StaticPrelinkedLibConsumed()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "static_prelinked_lib_consumed";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "StaticLibs";

            conf.IncludePaths.Add(SourceRootPath);
        }
    }
}
