using System;
using Sharpmake;

//[module: Sharpmake.Include("*/*FunctionalTest.sharpmake.cs")]

namespace SharpmakeGen
{
    namespace FunctionalTests
    {
        [Generate]
        public abstract class TestProject : Common.SharpmakeBaseProject
        {
            public TestProject()
                : base(excludeSharpmakeFiles: false, generateXmlDoc: false)
            {
                SourceRootPath = @"[project.RootPath]\Sharpmake.FunctionalTests\[project.Name]";

                AddTargets(Common.GetDefaultTargets());
            }

            public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "FunctionalTests";

            conf.AddPrivateDependency<SharpmakeProject>(target);
            conf.AddPrivateDependency<SharpmakeApplicationProject>(target);
            conf.AddPrivateDependency<Platforms.CommonPlatformsProject>(target);
        }
        }

        [Generate]
        public class FastBuildFunctionalTest : TestProject
        {
            public FastBuildFunctionalTest()
            {
                Name = "FastBuildFunctionalTest";
            }
        }
    }
}
