// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
            AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug));
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
            AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug));

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
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_22621_0);

            arguments.Generate<ExeLibSolution>();
        }
    }
}
