// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

// This module attribut control how the debug project will be named.
// It is used here only as an example as we doesn't generate debug solution/project for the samples.
[module: Sharpmake.DebugProjectName("Sharpmake.HelloWorld")]

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
                    DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release
            ));

            SourceRootPath = @"[project.SharpmakeCsPath]\codebase";
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.Defines.Add("_HAS_EXCEPTIONS=0");

            // if not set, no precompile option will be used.
            conf.PrecompHeader = "stdafx.h";
            conf.PrecompSource = "stdafx.cpp";

            conf.CustomProperties.Add("CustomOptimizationProperty", $"Custom-{target.Optimization}");
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
                    DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release
            ));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<HelloWorldProject>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2019, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);
            arguments.Generate<HelloWorldSolution>();
        }
    }
}
