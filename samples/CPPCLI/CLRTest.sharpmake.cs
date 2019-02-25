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
                DevEnv.vs2015 | DevEnv.vs2017,
                Optimization.Debug | Optimization.Release,
                OutputType.Dll,
                Blob.NoBlob,
                BuildSystem.MSBuild,
                DotNetFramework.v4_5 | DotNetFramework.v4_6_2)};
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
            arguments.Generate<TheSolution>();
        }
    }
}

