using System;
using Sharpmake;

[module: Sharpmake.Include("*/Sharpmake.*.sharpmake.cs")]

namespace SharpmakeGen.Extensions
{
    public abstract class ExtensionProject : Common.SharpmakeBaseProject
    {
        public ExtensionProject()
        {
            SourceRootPath = @"[project.RootPath]\Sharpmake.Extensions\[project.Name]";
            AssemblyName = "[project.Name]";
            RootNamespace = "Sharpmake";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.ProjectFileName = "[project.Name]";
            conf.SolutionFolder = "Extensions";

            conf.AddPrivateDependency<SharpmakeProject>(target);
            conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
        }
    }
}
