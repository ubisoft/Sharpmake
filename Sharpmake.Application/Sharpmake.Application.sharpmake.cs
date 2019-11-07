using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;

namespace SharpmakeGen
{
    [Generate]
    public class SharpmakeApplicationProject : Common.SharpmakeBaseProject
    {
        public SharpmakeApplicationProject()
            : base(generateXmlDoc: false)
        {
            Name = "Sharpmake.Application";
            ApplicationManifest = "app.manifest";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.DotNetConsoleApp;
            conf.Options.Add(Options.CSharp.PublishTrimmed.Enabled);

            conf.AddPrivateDependency<SharpmakeProject>(target);
            conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
            conf.AddPrivateDependency<SharpmakeVisualStudio>(target);
            conf.AddPrivateDependency<Platforms.CommonPlatformsProject>(target);
        }
    }
}
