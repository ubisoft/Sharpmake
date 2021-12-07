// Copyright (c) 2021 Ubisoft Entertainment
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

namespace HelloClangCl
{
    [Sharpmake.Generate]
    public class StaticLib1Project : CommonProject
    {
        public StaticLib1Project()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "static_lib1";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "StaticLibs";

            // intentionally in a subfolder
            conf.PrecompHeader = @"src\pch.h";
            conf.PrecompSource = @"src\pch.cpp";

            conf.IncludePaths.Add(SourceRootPath);

            switch (target.Optimization)
            {
                case Optimization.Debug:
                    conf.SourceFilesBuildExclude.Add(@"src\ensure_release.cpp");
                    break;
                case Optimization.Release:
                    // use a different method to exclude ensure_debug.cpp
                    conf.SourceFilesBuildExcludeRegex.Add(Util.RegexPathCombine("src", @"ensure_d.*\.cpp$"));
                    break;
                default:
                    throw new Error("Unexpected optimization " + target.Optimization);
            }
        }
    }
}
