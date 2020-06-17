using Sharpmake;

namespace HelloXCode
{
    [Sharpmake.Generate]
    public class HelloXCodeSolution : CommonSolution
    {
        public HelloXCodeSolution()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "HelloXCode";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.AddProject<ExeProject>(target);
            conf.AddProject<Dll1Project>(target);
            conf.AddProject<StaticLib1Project>(target);
            conf.AddProject<StaticLib2Project>(target);
        }
    }
}
