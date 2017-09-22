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
            AddTargets(new Target(Platform.win64, DevEnv.vs2013, Optimization.Debug));
            SourceRootPath = "[project.SharpmakeCsPath]/src";

            IsFileNameToLower = false;
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.AddPublicDependency<LibStuffProject>(target);
        }
    }

    [Sharpmake.Generate]
    public class ExeLibSolution : Sharpmake.Solution
    {
        public ExeLibSolution()
        {
            Name = "ExeLibSolutionName";
            AddTargets(new Target(Platform.win64, DevEnv.vs2013, Optimization.Debug));

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

    internal static class main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<ExeLibSolution>();
        }
    }
}
