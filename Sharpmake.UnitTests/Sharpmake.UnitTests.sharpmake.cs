using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;

namespace SharpmakeGen
{
    [Generate]
    public class SharpmakeUnitTestsProject : Common.SharpmakeBaseProject
    {
        public SharpmakeUnitTestsProject()
            : base(generateXmlDoc: false)
        {
            Name = "Sharpmake.UnitTests";

            Services.Add("{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}"); // NUnit

            // indicates where to find the nuget(s) we reference without needing nuget.config or global setting
            CustomProperties.Add("RestoreAdditionalProjectSources", "https://api.nuget.org/v3/index.json");
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.TargetPath = @"[project.RootPath]\tmp\unittests\[lower:target.Optimization]";

            conf.AddPrivateDependency<SharpmakeProject>(target);
            conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
            conf.AddPrivateDependency<Platforms.CommonPlatformsProject>(target);

            conf.ReferencesByNuGetPackage.Add("Microsoft.NET.Test.Sdk", "16.9.4");

            conf.ReferencesByNuGetPackage.Add("NUnit.Console", "3.12.0");
            conf.ReferencesByNuGetPackage.Add("NUnit", "3.13.1");
            conf.ReferencesByNuGetPackage.Add("NUnit3TestAdapter", "3.17.0");
        }
    }
}
