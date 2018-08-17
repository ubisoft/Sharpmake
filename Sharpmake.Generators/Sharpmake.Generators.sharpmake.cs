using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;

namespace SharpmakeGen
{
    [Generate]
    public class SharpmakeGeneratorsProject : Common.SharpmakeBaseProject
    {
        public SharpmakeGeneratorsProject()
        {
            Name = "Sharpmake.Generators";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.ReferencesByName.Add(
                "System.Xml",
                "System.Xml.Linq"
            );
            conf.AddPrivateDependency<SharpmakeProject>(target);
        }
    }
}
