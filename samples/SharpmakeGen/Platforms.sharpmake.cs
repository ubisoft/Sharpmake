using System;
using Sharpmake;

namespace SharpmakeGen.Platforms
{
    public abstract class PlatformProject : SharpmakeBaseProject
    {
        public PlatformProject()
        {
            SourceRootPath = @"[project.RootPath]\Sharpmake.Platforms\[project.Name]";
            AssemblyName = "[project.Name]";
            RootNamespace = "Sharpmake";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.ProjectFileName = "[project.Name]";
            conf.SolutionFolder = "Platforms";

            conf.AddPrivateDependency<SharpmakeProject>(target);
            conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
        }
    }

    [Generate]
    public class CommonPlatformsProject : PlatformProject
    {
        public CommonPlatformsProject()
        {
            Name = "Sharpmake.CommonPlatforms";
        }
    }

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

