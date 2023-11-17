// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace JumboBuild
{
    [Sharpmake.Generate]
    public class JumboBuildProject : Project
    {
        public JumboBuildProject()
        {
            Name = "JumboBuild";

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

            conf.CustomProperties.Add("CustomOptimizationProperty", $"Custom-{target.Optimization}");

            conf.Options.Add(Options.Vc.Compiler.JumboBuild.Enable);
            conf.MaxFilesPerJumboFile = 0;
            conf.MinFilesPerJumboFile = 2;
            conf.MinJumboFiles = 1;
        }
    }

    [Sharpmake.Generate]
    public class JumboBuildSolution : Sharpmake.Solution
    {
        public JumboBuildSolution()
        {
            Name = "JumboBuild";

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
            conf.AddProject<JumboBuildProject>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2019, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);
            arguments.Generate<JumboBuildSolution>();
        }
    }
}
