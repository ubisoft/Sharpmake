using Sharpmake;

namespace HelloXCode
{
    [Sharpmake.Generate]
    public class StaticLib2Project : CommonProject
    {
        public StaticLib2Project()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "static_lib2";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "StaticLibs";

            conf.IncludePaths.Add(SourceRootPath);
        }
    }
}
