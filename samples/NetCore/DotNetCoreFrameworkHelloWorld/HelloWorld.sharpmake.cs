// Copyright (c) 2020-2021 Ubisoft Entertainment
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
using System;
namespace NetCore
{
    namespace DotNetCoreFrameworkHelloWorld
    {
        [Sharpmake.Generate]
        public class HelloWorld : CSharpProject
        {
            internal static ITarget[] SampleTargets = new ITarget[]
            {
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2017,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.netcore2_1),
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.netcore3_1)
            };

            public HelloWorld()
            {
                GeneratedAssemblyConfig.GenerateAssemblyInfo = true;
                ClearTargets();
                AddTargets(SampleTargets);

                RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

                // This Path will be used to get all SourceFiles in this Folder and all subFolders
                SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
                AssemblyName = "the other name";
                EnableDefaultItems = true;
            }

            [Configure()]
            public virtual void ConfigureAll(Configuration conf, Target target)
            {
                conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
                conf.ProjectPath = @"[project.RootPath]";

                conf.Options.Add(Sharpmake.Options.CSharp.TreatWarningsAsErrors.Enabled);
            }
        }

        [Sharpmake.Generate]
        public class HelloWorldSolution : CSharpSolution
        {
            public HelloWorldSolution()
            {
                AddTargets(HelloWorld.SampleTargets);
            }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.SolutionFileName = String.Format("{0}.{1}.{2}",
                                                      Name,
                                                      "[target.DevEnv]",
                                                      "[target.Framework]");
                conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

                conf.AddProject<HelloWorld>(target);
            }
        }

        [Sharpmake.Generate]
        public class HelloWorldMultiFramework : CSharpProject
        {
            internal static ITarget[] SampleTargets = new ITarget[]
            {
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.netcore2_1 | DotNetFramework.netcore3_1
                )
            };

            public HelloWorldMultiFramework()
            {
                ClearTargets();
                AddTargets(SampleTargets);

                RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

                // This Path will be used to get all SourceFiles in this Folder and all subFolders
                SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
                AssemblyName = "the other name";
            }

            [Configure()]
            public virtual void ConfigureAll(Configuration conf, Target target)
            {
                conf.ProjectFileName = "[project.Name].[target.DevEnv]";
                conf.ProjectPath = @"[project.RootPath]";

                conf.Options.Add(Sharpmake.Options.CSharp.TreatWarningsAsErrors.Enabled);
            }
        }

        [Sharpmake.Generate]
        public class HelloWorldMultiFrameworkSolution : CSharpSolution
        {
            public HelloWorldMultiFrameworkSolution()
            {
                AddTargets(HelloWorldMultiFramework.SampleTargets);
            }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.SolutionFileName = String.Format("{0}.{1}",
                                                      Name,
                                                      "[target.DevEnv]");
                conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

                conf.AddProject<HelloWorldMultiFramework>(target);
            }
        }

        public static class Main
        {
            [Sharpmake.Main]
            public static void SharpmakeMain(Sharpmake.Arguments arguments)
            {
                arguments.Generate<HelloWorldSolution>();
                arguments.Generate<HelloWorldMultiFrameworkSolution>();
            }
        }
    }
}
