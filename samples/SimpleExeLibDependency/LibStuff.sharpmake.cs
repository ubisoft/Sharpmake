// Copyright (c) 2017, 2019-2021 Ubisoft Entertainment
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

namespace SimpleExeLibDependency
{
    [Sharpmake.Generate]
    public class LibStuffProject : Project
    {
        public string BasePath = @"[project.SharpmakeCsPath]/libstuff";

        public LibStuffProject()
        {
            Name = "LibStuffProject_ProjectName";

            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2017,
                Optimization.Debug,
                OutputType.Lib
            ));

            SourceRootPath = "[project.BasePath]";

            IsFileNameToLower = false;
        }

        [Configure()]
        public void Configure(Configuration conf, Target target)
        {
            conf.Output = Configuration.OutputType.Lib;
            conf.ProjectPath = "[project.SharpmakeCsPath]/projects";
            conf.TargetLibraryPath = "[project.BasePath]/lib";
            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]_[target.Optimization]_[target.DevEnv]";

            conf.Defines.Add("_HAS_EXCEPTIONS=0");

            conf.Options.Add(Options.Vc.Librarian.TreatLibWarningAsErrors.Enable);

            conf.IncludePaths.Add("[project.BasePath]");
        }
    }
}
