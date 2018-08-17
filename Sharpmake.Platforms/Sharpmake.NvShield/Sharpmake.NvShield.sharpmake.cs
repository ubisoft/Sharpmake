using Sharpmake;

namespace SharpmakeGen.Platforms
{
    [Generate]
    public class NvShieldProject : PlatformProject
    {
        public NvShieldProject()
        {
            Name = "Sharpmake.NvShield";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<CommonPlatformsProject>(target);
        }
    }
}
