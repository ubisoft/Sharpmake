using Sharpmake;

namespace SharpmakeGen.Platforms
{
    [Generate]
    public class CommonPlatformsProject : PlatformProject
    {
        public CommonPlatformsProject()
        {
            Name = "Sharpmake.CommonPlatforms";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.ProjectPath = @"[project.SourceRootPath]";
        }
    }
}
