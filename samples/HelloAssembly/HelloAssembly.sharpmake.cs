// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace HelloAssembly
{
    [Sharpmake.Generate]
    public class HelloAssemblyProject : Project
    {
        public HelloAssemblyProject()
        {
            Name = "HelloAssembly";
            AddTargets(new Target(Platform.win64, DevEnv.vs2019, Optimization.Debug | Optimization.Release));
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase";

            // The utils file is supposed to be only included, not built separately
            SourceFilesBuildExclude.Add(@"[project.SharpmakeCsPath]\codebase\sub folder\utils.asm");
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.Defines.Add("_HAS_EXCEPTIONS=0");

            conf.AssemblyIncludePaths.Add(@"[project.SharpmakeCsPath]\codebase\sub folder");
        }
    }

    [Sharpmake.Generate]
    public class HelloAssemblySolution : Sharpmake.Solution
    {
        public HelloAssemblySolution()
        {
            Name = "HelloAssembly";
            AddTargets(new Target(Platform.win64, DevEnv.vs2019, Optimization.Debug | Optimization.Release));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<HelloAssemblyProject>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2019, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);
            arguments.Generate<HelloAssemblySolution>();
        }
    }
}
