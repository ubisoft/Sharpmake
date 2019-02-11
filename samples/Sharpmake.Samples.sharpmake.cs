using System;
using Sharpmake;

[module: Sharpmake.Include("*/Sharpmake.*.sharpmake.cs")]

namespace SharpmakeGen.Samples
{
    public abstract class SampleProject : Common.SharpmakeBaseProject
    {
        public SampleProject()
            : base(excludeSharpmakeFiles: false, generateXmlDoc: false)
        {
            SourceRootPath = @"[project.RootPath]\samples\[project.Name]";
            SourceFilesExcludeRegex.Add(
                @"\\codebase\\",
                @"\\projects\\",
                @"\\reference\\"
            );
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "Samples";
            conf.TargetPath = @"[project.RootPath]\bin\[target.Optimization]\Samples";

            conf.AddPrivateDependency<SharpmakeProject>(target);
            conf.AddPrivateDependency<SharpmakeApplicationProject>(target);
            conf.AddPrivateDependency<Platforms.CommonPlatformsProject>(target);
        }
    }

    [Generate]
    public class ConfigureOrderProject : SampleProject
    {
        public ConfigureOrderProject()
        {
            Name = "ConfigureOrder";
        }
    }

    [Generate]
    public class CPPCLIProject : SampleProject
    {
        public CPPCLIProject()
        {
            Name = "CPPCLI";
        }
    }

    [Generate]
    public class CSharpHelloWorldProject : SampleProject
    {
        public CSharpHelloWorldProject()
        {
            Name = "CSharpHelloWorld";
        }
    }

    [Generate]
    public class CSharpVsixProject : SampleProject
    {
        public CSharpVsixProject()
        {
            Name = "CSharpVsix";
        }
    }

    [Generate]
    public class CSharpWcfProject : SampleProject
    {
        public CSharpWcfProject()
        {
            Name = "CSharpWCF";
        }
    }

    [Generate]
    public class HelloWorldProject : SampleProject
    {
        public HelloWorldProject()
        {
            Name = "HelloWorld";
        }
    }

    [Generate]
    public class FastBuildSimpleExecutable : SampleProject
    {
        public FastBuildSimpleExecutable()
        {
            Name = "FastBuildSimpleExecutable";
        }
    }

    [Generate]
    public class QTFileCustomBuildProject : SampleProject
    {
        public QTFileCustomBuildProject()
        {
            Name = "QTFileCustomBuild";
        }
    }

    [Generate]
    public class PackageReferencesProject : SampleProject
    {
        public PackageReferencesProject()
        {
            Name = "PackageReferences";
        }
    }

    [Generate]
    public class SimpleExeLibDependencyProject : SampleProject
    {
        public SimpleExeLibDependencyProject()
        {
            Name = "SimpleExeLibDependency";
        }
    }
}
