using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;

namespace SharpmakeGen
{
    [Generate]
    public class SharpmakeProject : Common.SharpmakeBaseProject
    {
        public SharpmakeProject()
        {
            Name = "Sharpmake";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.Options.Add(Options.CSharp.AllowUnsafeBlocks.Enabled);
            conf.AddPrivateDependency<SharpmakeVisualStudio>(target);

            conf.ReferencesByNuGetPackage.Add("Microsoft.CodeAnalysis.CSharp", "3.1.0");
            conf.ReferencesByNuGetPackage.Add("Microsoft.DiaSymReader.Native", "1.7.0");
            if (target.Framework.IsDotNetCore())
            {
                conf.ReferencesByNuGetPackage.Add("Microsoft.Win32.Registry", "4.6.0-preview7.19362.9");
            }
        }
    }
}
