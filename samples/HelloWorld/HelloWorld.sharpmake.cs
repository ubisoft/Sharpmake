// Copyright (c) 2017 Ubisoft Entertainment
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

namespace HelloWorld
{
    [Sharpmake.Generate]
    public class HelloWorldProject : Project
    {
        public HelloWorldProject()
        {
            Name = "HelloWorld";

            AddTargets(new Target(
                    Platform.win32 | Platform.win64,
                    DevEnv.vs2017,
                    Optimization.Debug // | Optimization.Release
            ));

            SourceRootPath = @"[project.SharpmakeCsPath]\codebase";
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            // if not set, no precompile option will be used.
            conf.PrecompHeader = "stdafx.h";
            conf.PrecompSource = "stdafx.cpp";
        }
    }

    [Sharpmake.Generate]
    public class HelloWorldSolution : Sharpmake.Solution
    {
        public HelloWorldSolution()
        {
            Name = "HelloWorld";

            AddTargets(new Target(
                    Platform.win32 | Platform.win64,
                    DevEnv.vs2017,
                    Optimization.Debug // | Optimization.Release
            ));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<HelloWorldProject>(target);
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<HelloWorldSolution>();
        }
    }
}
