using Sharpmake;

[module: Sharpmake.Include("projects.sharpmake.cs")]

namespace Android_SharpmakeTest
{
    public static class Common
    {
        // Splitting 2008 for Framework v4.0 since it is not supported
        public static Target[] CommonTarget = {
            new Target(
                Platform.android,
                DevEnv.vs2017,
                Optimization.Debug | Optimization.Release),

            new Target(
                Platform.android64,
                DevEnv.vs2017,
                Optimization.Debug | Optimization.Release),
            };
    }

    [Sharpmake.Generate]
    public class TheSolution : CSharpSolution
    {
        public TheSolution()
        {
            Name = "Android";
            AddTargets(Common.CommonTarget);
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "Android.[target.DevEnv]";

            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

            conf.AddProject<AndroidPackage>(target);
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

