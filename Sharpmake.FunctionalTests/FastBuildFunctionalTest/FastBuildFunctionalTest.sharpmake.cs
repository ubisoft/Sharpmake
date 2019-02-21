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

using System.IO;
using System.Linq;
using Sharpmake;
using System;

namespace SharpmakeGen
{
    namespace FunctionalTests
    {
        [Generate]
        public class FastBuildFunctionalTest : Project
        {
            public FastBuildFunctionalTest()
            {
                Name = "FastBuildFunctionalTest";

                AddTargets(new Target(
                        Platform.win64,
                        DevEnv.vs2017,
                        Optimization.Debug | Optimization.Release,
                        OutputType.Lib,
                        Blob.NoBlob,
                        BuildSystem.FastBuild
                ));

                RootPath = @"[project.SharpmakeCsPath]\codebase";
                SourceRootPath = @"[project.SharpmakeCsPath]\codebase";
            }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
                conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            }

            [Configure(BuildSystem.FastBuild)]
            public void ConfigureFastBuild(Configuration conf, Target target)
            {
                conf.IsFastBuild = true;
                conf.FastBuildBlobbed = target.Blob == Blob.FastBuildUnitys;
            }

            [Configure(Platform.win64)]
            public void ConfigureWin64(Configuration conf, Target target)
            {
                // workaround necessity of rc.exe
                conf.Options.Add(Options.Vc.Linker.EmbedManifest.No);
            }
        }

        [Sharpmake.Generate]
        public class FastBuildFunctionalTestSolution : Sharpmake.Solution
        {
            public FastBuildFunctionalTestSolution()
            {
                Name = "FastBuildFunctionalTest";

                AddTargets(new Target(
                            Platform.win64,
                            DevEnv.vs2017,
                            Optimization.Debug | Optimization.Release,
                            OutputType.Lib,
                            Blob.NoBlob,
                            BuildSystem.FastBuild
                ));
            }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.SolutionFileName = "[solution.Name]";
                conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";

                conf.AddProject<FastBuildFunctionalTest>(target);
            }

            [Sharpmake.Main]
            public static void SharpmakeMain(Sharpmake.Arguments arguments)
            {
                FastBuildSettings.FastBuildMakeCommand = @"tools\FastBuild\FBuild.exe";
                FastBuildSettings.WriteAllConfigsSection = true;

                arguments.Generate<FastBuildFunctionalTestSolution>();
            }
        }
    }
}
