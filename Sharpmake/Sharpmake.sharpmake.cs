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

            // indicates where to find the nuget(s) we reference without needing nuget.config or global setting
            CustomProperties.Add("RestoreAdditionalProjectSources", "https://api.nuget.org/v3/index.json");
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.ProjectPath = @"[project.SourceRootPath]";

            conf.Options.Add(Options.CSharp.AllowUnsafeBlocks.Enabled);

            conf.ReferencesByNuGetPackage.Add("Microsoft.CodeAnalysis.CSharp", "4.0.1");
            conf.ReferencesByNuGetPackage.Add("Microsoft.VisualStudio.Setup.Configuration.Interop", "3.0.4492");

            if (target.Framework.IsDotNetFramework())
            {
                conf.ReferencesByNuGetPackage.Add("Basic.Reference.Assemblies.Net472", "1.2.4");
                conf.ReferencesByNuGetPackage.Add("System.Text.Json", "5.0.2");
            }
            else if (target.Framework.IsDotNetCore())
            {
                conf.ReferencesByNuGetPackage.Add("Basic.Reference.Assemblies.Net50", "1.2.4");
                conf.ReferencesByNuGetPackage.Add("Microsoft.Win32.Registry", "5.0.0");
            }
        }
    }
}
