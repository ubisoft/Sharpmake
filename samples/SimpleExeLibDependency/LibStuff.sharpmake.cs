using Sharpmake;

namespace SimpleExeLibDependency
{
    [Sharpmake.Generate]
    public class LibStuffProject : Project
    {
        public string BasePath = @"[project.SharpmakeCsPath]/libstuff";

        public LibStuffProject()
        {
            Name = "LibStuffProject_ProjectName";

            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2013,
                Optimization.Debug,
                OutputType.Lib
            ));

            SourceRootPath = "[project.BasePath]";

            IsFileNameToLower = false;
        }

        [Configure()]
        public void Configure(Configuration conf, Target target)
        {
            conf.Output = Project.Configuration.OutputType.Lib;
            conf.IncludePaths.Add("[project.BasePath]");
            conf.TargetLibraryPath = "[project.BasePath]/lib";
            conf.ProjectPath = "[project.SharpmakeCsPath]/projects";
        }
    }
}
