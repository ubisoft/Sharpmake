using System;
using Sharpmake;

[module: Sharpmake.Include("*/Sharpmake.*.sharpmake.cs")]

namespace SharpmakeGen.Platforms
{
    public abstract class PlatformProject : Common.SharpmakeBaseProject
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
}
