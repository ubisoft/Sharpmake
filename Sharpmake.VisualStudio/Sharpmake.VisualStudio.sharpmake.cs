using System;
using Sharpmake;

namespace SharpmakeGen
{
    [Generate]
    public class SharpmakeVisualStudio : Common.SharpmakeBaseProject
    {
        public SharpmakeVisualStudio()
        {
            Name = "Sharpmake.VisualStudio";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            if (!target.Framework.IsDotNetCore())
            {
                conf.Defines.Add("VISUAL_STUDIO_EXTENSION_ENABLED");
                conf.ReferencesByName.Add("System.Windows.Forms");
                conf.ReferencesByNameExternal.Add("Microsoft.Build.Utilities.v4.0");
                conf.ReferencesByNuGetPackage.Add("Microsoft.VisualStudio.Setup.Configuration.Interop", "1.16.30");
            }
        }
    }
}
