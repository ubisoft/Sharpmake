using Sharpmake;

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
                    DevEnv.vs2012,
                    Optimization.Debug // | Optimization.Release
            ));

            SourceRootPath = @"[project.SharpmakeCsPath]\codebase";
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            // if not set, no precompile option will be used.
            conf.PrecompHeader = "stdafx.h";
            conf.PrecompSource = "stdafx.cpp";
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
                    DevEnv.vs2012,
                    Optimization.Debug // | Optimization.Release
            ));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<HelloWorldProject>(target);
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<HelloWorldSolution>();
        }
    }
}
