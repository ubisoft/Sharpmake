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

[module: Sharpmake.Include("LibStuff.sharpmake.cs")]

namespace SimpleExeLibDependency
{
    [Sharpmake.Generate]
    public class SimpleExeProject : Project
    {
        public SimpleExeProject()
        {
            Name = "SimpleExeProjectName";
            AddTargets(new Target(Platform.win64, DevEnv.vs2017, Optimization.Debug));
            SourceRootPath = "[project.SharpmakeCsPath]/src";

            IsFileNameToLower = false;
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]_[target.Optimization]_[target.DevEnv]";

            conf.Options.Add(Options.Vc.Linker.TreatLinkerWarningAsErrors.Enable);

            conf.Defines.Add("_HAS_EXCEPTIONS=0");

            conf.AddPublicDependency<LibStuffProject>(target);
        }
    }

    [Sharpmake.Generate]
    public class ExeLibSolution : Sharpmake.Solution
    {
        public ExeLibSolution()
        {
            Name = "ExeLibSolutionName";
            AddTargets(new Target(Platform.win64, DevEnv.vs2017, Optimization.Debug));

            IsFileNameToLower = false;
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<SimpleExeProject>(target);
        }
    }

    public static class main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2017, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_17763_0);

            arguments.Generate<ExeLibSolution>();
        }
    }
}
