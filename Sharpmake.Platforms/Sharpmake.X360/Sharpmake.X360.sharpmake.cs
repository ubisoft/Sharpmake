using Sharpmake;

namespace SharpmakeGen.Platforms
{
    [Generate]
    public class X360Project : PlatformProject
    {
        public X360Project()
        {
            Name = "Sharpmake.X360";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<CommonPlatformsProject>(target);
        }
    }
}
