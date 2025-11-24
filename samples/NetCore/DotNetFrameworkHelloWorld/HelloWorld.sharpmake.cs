// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using Sharpmake;

namespace NetCore
{
    namespace DotNetFrameworkHelloWorld
    {
        [Sharpmake.Generate]
        public class HelloWorld : CSharpProject
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
                    DotNetFramework.v4_7_2),
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_8)
            };

            public HelloWorld()
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
                conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
                conf.ProjectPath = @"[project.RootPath]";
                conf.TargetPath = @"[conf.ProjectPath]\output\[target.DevEnv]\[conf.Name]";

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
                conf.SolutionFileName = string.Format("{0}.{1}.{2}",
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
                    DotNetFramework.v4_7_2 | DotNetFramework.v4_8)
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
                conf.SolutionFileName = string.Format("{0}.{1}",
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
