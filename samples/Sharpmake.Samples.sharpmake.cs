using System;
using Sharpmake;

[module: Sharpmake.Include("*/Sharpmake.*.sharpmake.cs")]

namespace SharpmakeGen.Samples
{
    public abstract class SampleProject : Common.SharpmakeBaseProject
    {
        public string SharpmakeMainFile = "[project.Name].sharpmake.cs";

        protected SampleProject()
            : base(excludeSharpmakeFiles: false, generateXmlDoc: false)
        {
            // samples are special, all the classes are here instead of in the subfolders
            SourceRootPath = @"[project.SharpmakeCsPath]\[project.Name]";
            SourceFilesExcludeRegex.Add(
                @"\\codebase\\",
                @"\\projects\\",
                @"\\reference\\"
            );

            DependenciesCopyLocal = DependenciesCopyLocalTypes.None;
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "Samples";
            conf.TargetPath = @"[project.RootPath]\tmp\samples\[target.Optimization]\[project.Name]";

            conf.AddPrivateDependency<SharpmakeProject>(target);
            conf.AddPrivateDependency<SharpmakeApplicationProject>(target);
            conf.AddPrivateDependency<Platforms.CommonPlatformsProject>(target);

            conf.CsprojUserFile = new Project.Configuration.CsprojUserFileSettings
            {
                StartAction = Project.Configuration.CsprojUserFileSettings.StartActionSetting.Program,
                StartProgram = @"[project.RootPath]\tmp\bin\[conf.Target.Optimization]\Sharpmake.Application.exe",
                StartArguments = "/sources(@'[project.SharpmakeMainFile]')",
                WorkingDirectory = "[project.SourceRootPath]"
            };
        }
    }

    [Generate]
    public class CompileCommandDatabaseProject : SampleProject
    {
        public CompileCommandDatabaseProject()
        {
            Name = "CompileCommandDatabase";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
        }
    }

    [Generate]
    public class ConfigureOrderProject : SampleProject
    {
        public ConfigureOrderProject()
        {
            Name = "ConfigureOrder";
            SharpmakeMainFile = "main.sharpmake.cs";
        }
    }

    [Generate]
    public class CPPCLIProject : SampleProject
    {
        public CPPCLIProject()
        {
            Name = "CPPCLI";
            SharpmakeMainFile = "CLRTest.sharpmake.cs";
        }
    }

    [Generate]
    public class CSharpHelloWorldProject : SampleProject
    {
        public CSharpHelloWorldProject()
        {
            Name = "CSharpHelloWorld";
            SharpmakeMainFile = "HelloWorld.sharpmake.cs";
        }
    }

    [Generate]
    public class CSharpImportsProject : SampleProject
    {
        public CSharpImportsProject()
        {
            Name = "CSharpImports";
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
    public class DotNetCoreFrameworkHelloWorldProject : SampleProject
    {
        public DotNetCoreFrameworkHelloWorldProject()
        {
            Name = "DotNetCoreFrameworkHelloWorld";
            SharpmakeMainFile = "HelloWorld.sharpmake.cs";
            SourceRootPath = @"[project.SharpmakeCsPath]\NetCore\[project.Name]";
        }
    }

    [Generate]
    public class DotNetFrameworkHelloWorldProject : SampleProject
    {
        public DotNetFrameworkHelloWorldProject()
        {
            Name = "DotNetFrameworkHelloWorld";
            SharpmakeMainFile = "HelloWorld.sharpmake.cs";
            SourceRootPath = @"[project.SharpmakeCsPath]\NetCore\[project.Name]";
        }
    }

    [Generate]
    public class DotNetMultiFrameworksHelloWorldProject : SampleProject
    {
        public DotNetMultiFrameworksHelloWorldProject()
        {
            Name = "DotNetMultiFrameworksHelloWorld";
            SharpmakeMainFile = "HelloWorld.sharpmake.cs";
            SourceRootPath = @"[project.SharpmakeCsPath]\NetCore\[project.Name]";
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
    public class HelloClangClProject : SampleProject
    {
        public HelloClangClProject()
        {
            Name = "HelloClangCl";
            SharpmakeMainFile = "HelloClangCl.Main.sharpmake.cs";

            // This one is special, we have .sharpmake.cs files in the codebase
            SourceFilesExcludeRegex.Remove(@"\\codebase\\");
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
        }
    }

    [Generate]
    public class HelloLinuxProject : SampleProject
    {
        public HelloLinuxProject()
        {
            Name = "HelloLinux";
            SharpmakeMainFile = "HelloLinux.Main.sharpmake.cs";

            // This one is special, we have .sharpmake.cs files in the codebase
            SourceFilesExcludeRegex.Remove(@"\\codebase\\");
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
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
    public class HelloXCodeProject : SampleProject
    {
        public HelloXCodeProject()
        {
            Name = "HelloXCode";
            SharpmakeMainFile = "HelloXCode.Main.sharpmake.cs";

            // This one is special, we have .sharpmake.cs files in the codebase
            SourceFilesExcludeRegex.Remove(@"\\codebase\\");
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
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
    public class QTFileCustomBuildProject : SampleProject
    {
        public QTFileCustomBuildProject()
        {
            Name = "QTFileCustomBuild";
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

    [Generate]
    public class VcpkgProject : SampleProject
    {
        public VcpkgProject()
        {
            Name = "vcpkg";
            SharpmakeMainFile = @"sharpmake\main.sharpmake.cs";
        }
    }
}
