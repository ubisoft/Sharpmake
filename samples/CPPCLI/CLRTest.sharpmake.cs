// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sharpmake;

[module: Sharpmake.Include("projects.sharpmake.cs")]

namespace CLR_SharpmakeTest
{
    public static class Common
    {
        public static Target[] CommonTarget = {
            new Target(
                Platform.win32,
                DevEnv.vs2019,
                Optimization.Debug | Optimization.Release,
                OutputType.Dll,
                Blob.NoBlob,
                BuildSystem.MSBuild,
                DotNetFramework.v4_7_2
            ),
            new Target(
                Platform.win32,
                DevEnv.vs2022,
                Optimization.Debug | Optimization.Release,
                OutputType.Dll,
                Blob.NoBlob,
                BuildSystem.MSBuild,
                DotNetFramework.v4_8
            )
        };
    }

    [Sharpmake.Generate]
    public class TheSolution : CSharpSolution
    {
        public TheSolution()
        {
            Name = "CPPCLI";
            AddTargets(Common.CommonTarget);
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "CPPCLI.[target.DevEnv].[target.Framework]";

            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

            conf.AddProject<CLR_CPP_Proj>(target);
            conf.AddProject<OtherCSharpProj>(target);
            conf.AddProject<TestCSharpConsole>(target);
        }
    }

    public static class StartupClass
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2019, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_22621_0);

            arguments.Generate<TheSolution>();
        }
    }
}

