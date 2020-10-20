using Sharpmake;

namespace HelloAndroid
{
    [Sharpmake.Generate]
    public class HelloAndroidSolution : Solution
    {
        public HelloAndroidSolution()
            : base(typeof(AndroidTarget))
        {
            Name = "HelloAndroid";
            AddTargets(AndroidTarget.GetDefaultTargets());
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name].[target.DevEnv]";

            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

            conf.AddProject<HelloAndroidPackage>(target);
        }
    }
}
