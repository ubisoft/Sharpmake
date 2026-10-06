// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace SlnxHelloWorld
{
    [Sharpmake.Generate]
    public class SlnxExeProject : Project
    {
        public SlnxExeProject()
        {
            Name = "SlnxExe";
            SourceRootPath = @"[project.SharpmakeCsPath]";
            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2026,
                Optimization.Debug | Optimization.Release));
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.Output = Configuration.OutputType.Exe;
        }
    }

    [Sharpmake.Generate]
    public class SlnxSolution : Solution
    {
        public SlnxSolution()
        {
            Name = "SlnxHelloWorld";
            Format = SolutionFormat.Xml; // opt into .slnx
            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2026,
                Optimization.Debug | Optimization.Release));
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<SlnxExeProject>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            arguments.Generate<SlnxExeProject>();
            arguments.Generate<SlnxSolution>();
        }
    }
}
