// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace HelloWindowsArchitectures
{
    // One win64 project built for three executables from the same definition:
    //   - x64 (the default architecture of win64)
    //   - x64 with the APX instruction set level (needs Visual Studio 2026 or newer)
    //   - ARM64
    // The solution shows the "x64", "x64-APX" and "ARM64" solution platforms.
    [Sharpmake.Generate]
    public class HelloWindowsArchitecturesProject : Project
    {
        public HelloWindowsArchitecturesProject()
        {
            Name = "HelloWindowsArchitectures";
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase";

            // A mask is expanded in one target per value. Baseline | APX gives two x64 targets.
            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2026,
                Optimization.Debug | Optimization.Release
            )
            {
                ProcessorArchitecture = ProcessorArchitecture.x64,
                InstructionSetLevel = InstructionSetLevel.Baseline | InstructionSetLevel.APX,
            });

            // arm64 has no instruction set level for now, and APX is not valid there: use a separate call.
            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2026,
                Optimization.Debug | Optimization.Release
            )
            {
                ProcessorArchitecture = ProcessorArchitecture.arm64,
                InstructionSetLevel = InstructionSetLevel.Baseline,
            });
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            // main.cpp uses the standard library, which needs C++ exception handling (/EHsc)
            conf.Options.Add(Options.Vc.Compiler.Exceptions.Enable);
        }
    }

    [Sharpmake.Generate]
    public class HelloWindowsArchitecturesSolution : Sharpmake.Solution
    {
        public HelloWindowsArchitecturesSolution()
        {
            Name = "HelloWindowsArchitectures";

            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2026,
                Optimization.Debug | Optimization.Release
            )
            {
                ProcessorArchitecture = ProcessorArchitecture.x64,
                InstructionSetLevel = InstructionSetLevel.Baseline | InstructionSetLevel.APX,
            });

            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2026,
                Optimization.Debug | Optimization.Release
            )
            {
                ProcessorArchitecture = ProcessorArchitecture.arm64,
                InstructionSetLevel = InstructionSetLevel.Baseline,
            });
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<HelloWindowsArchitecturesProject>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            // pin the SDK so that the generated files do not depend on the machine
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2026, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_26100_0);
            arguments.Generate<HelloWindowsArchitecturesSolution>();
        }
    }
}
