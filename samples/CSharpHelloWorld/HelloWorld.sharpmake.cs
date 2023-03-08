// Copyright (c) 2017, 2019, 2021-2022 Ubisoft Entertainment
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

using System;
using Sharpmake;

namespace CSharpHelloWorld
{
    public class TargetTypes
    {
        public static Target[] GetDefaultTargets()
        {
            return new Target[]
            {
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2017,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_6_1),
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    framework: DotNetFramework.net6_0)
            };
        }
    }

    [Sharpmake.Generate]
    public class HelloWorld : CSharpProject
    {
        public HelloWorld()
        {
            AddTargets(TargetTypes.GetDefaultTargets());

            RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

            // This Path will be used to get all SourceFiles in this Folder and all subFolders
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            AssemblyName = "the other name";
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
            AddTargets(TargetTypes.GetDefaultTargets());
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = string.Format("{0}.{1}.{2}",
                                                  Name,
                                                  "[target.DevEnv]",
                                                  "[target.Framework]");
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

            conf.AddProject<HelloWorld>(target);
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<HelloWorldSolution>();
        }
    }
}
