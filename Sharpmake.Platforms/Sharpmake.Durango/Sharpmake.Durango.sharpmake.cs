using Sharpmake;

namespace SharpmakeGen.Platforms
{
    [Generate]
    public class DurangoProject : PlatformProject
    {
        public DurangoProject()
        {
            Name = "Sharpmake.Durango";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<CommonPlatformsProject>(target);
        }
    }
}
