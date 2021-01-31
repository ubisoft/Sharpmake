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

namespace PreBuildStepDependency
{
    [Generate]
    public class ToolProject : Project
    {
        public ToolProject()
        {
            Name = "Tool";

            AddTargets(new Target(
                    Platform.win64,
                    DevEnv.vs2019,
                    Optimization.Debug // | Optimization.Release
            ));

            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\tool";
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.IntermediatePath = @"[project.SharpmakeCsPath]\projects\intermediate\[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.Output = Configuration.OutputType.Exe;
        }
    }

    [Generate]
    public class AppProject : Project
    {
        public AppProject()
        {
            Name = "App";


            AddTargets(new Target(
                    Platform.win64,
                    DevEnv.vs2019,
                    Optimization.Debug // | Optimization.Release
            ));

            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\app";
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.IntermediatePath = @"[project.SharpmakeCsPath]\projects\intermediate\[project.Name]_[target.DevEnv]_[target.Platform]";

            conf.AddPrivateDependency<LibProject>(target, DependencySetting.DefaultWithoutBuildSteps);

            Configuration.BuildStepExecutable tool = new Configuration.BuildStepExecutable(@"[project.SharpmakeCsPath]\projects\output\win64\debug\tool.exe", "", "", "-Flag1 -Flag2 -DoStuff=256");
            conf.EventPreBuildExe.Add(tool);

        }
    }

    [Generate]
    public class LibProject : Project
    {
        public LibProject()
        {
            Name = "Lib";

            AddTargets(new Target(
                    Platform.win64,
                    DevEnv.vs2019,
                    Optimization.Debug // | Optimization.Release
            ));

            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\lib";
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.IntermediatePath = @"[project.SharpmakeCsPath]\projects\intermediate\[project.Name]_[target.DevEnv]_[target.Platform]";

            conf.Output = Configuration.OutputType.Lib;
            conf.IncludePaths.Add(conf.Project.SourceRootPath);

            // Depend on our 'tool' to be build first 
            conf.AddPrivateDependency<ToolProject>(target, DependencySetting.OnlyBuildOrder);

            Configuration.BuildStepExecutable tool = new Configuration.BuildStepExecutable(@"[project.SharpmakeCsPath]\projects\output\win64\debug\tool.exe", "", "", "-Flag1 -Flag2 -DoStuff=123");
            conf.EventPreBuildExe.Add(tool);
        }
    }


    [Sharpmake.Generate]
    public class PreBuildStepDependencySolution : Sharpmake.Solution
    {
        public PreBuildStepDependencySolution()
        {
            Name = "PreBuildStepDependency";

            AddTargets(new Target(
                    Platform.win64,
                    DevEnv.vs2019,
                    Optimization.Debug // | Optimization.Release
            ));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<LibProject>(target);
            conf.AddProject<AppProject>(target);
            conf.AddProject<ToolProject>(target);
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<PreBuildStepDependencySolution>();
        }
    }
}
