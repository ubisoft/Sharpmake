using Sharpmake;

[module: Sharpmake.Include("Platforms.sharpmake.cs")]
[module: Sharpmake.Include("Samples.sharpmake.cs")]

namespace SharpmakeGen
{
    public abstract class SharpmakeBaseProject : CSharpProject
    {
        protected SharpmakeBaseProject()
        {
            AddTargets(
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2015,
                    Optimization.Debug | Optimization.Release,
                    framework: DotNetFramework.v4_5
                )
            );

            RootPath = @"[project.SharpmakeCsPath]\..\..";
            SourceRootPath = @"[project.RootPath]\[project.Name]";
        }

        [Configure]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]";
            conf.ProjectPath = @"[project.SourceRootPath]";
            conf.Output = Configuration.OutputType.DotNetClassLibrary;
            conf.TargetPath = @"[conf.ProjectPath]\bin\[target.Optimization]";

            conf.ReferencesByName.Add("System");

            conf.Options.Add(Options.CSharp.LanguageVersion.CSharp6);
            conf.Options.Add(Options.CSharp.TreatWarningsAsErrors.Enabled);
        }
    }

    [Generate]
    public class SharpmakeProject : SharpmakeBaseProject
    {
        public SharpmakeProject()
        {
            Name = "Sharpmake";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.ReferencesByNameExternal.Add("Microsoft.Build.Utilities.v4.0");

            conf.Options.Add(Options.CSharp.AllowUnsafeBlocks.Enabled);
        }
    }

    [Generate]
    public class SharpmakeGeneratorsProject : SharpmakeBaseProject
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

    [Generate]
    public class SharpmakeNuGetProject : SharpmakeBaseProject
    {
        public SharpmakeNuGetProject()
        {
            Name = "Sharpmake.NuGet";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.ReferencesByName.Add(
                "System.Core",
                "System.Data",
                "System.Data.DataSetExtensions",
                "System.Net.Http",
                "System.Runtime.Serialization",
                "System.Xml",
                "System.Xml.Linq"
            );
        }
    }

    [Generate]
    public class SharpmakeUnitTestsProject : SharpmakeBaseProject
    {
        public SharpmakeUnitTestsProject()
        {
            Name = "Sharpmake.UnitTests";

            Services.Add("{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}"); // NUnit
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<SharpmakeProject>(target);
            conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
            conf.AddPrivateDependency<Platforms.CommonPlatformsProject>(target);

            conf.ReferencesByNuGetPackage.Add("NUnit", "3.4.1");
            conf.ReferencesByNuGetPackage.Add("NUnit.Console", "3.4.1");
        }
    }

    [Generate]
    public class SharpmakeApplicationProject : SharpmakeBaseProject
    {
        public SharpmakeApplicationProject()
        {
            Name = "Sharpmake.Application";
            ApplicationManifest = "app.manifest";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.DotNetConsoleApp;

            conf.ReferencesByName.Add("System.Windows.Forms");

            conf.AddPrivateDependency<SharpmakeProject>(target);
            conf.AddPrivateDependency<SharpmakeGeneratorsProject>(target);
            conf.AddPrivateDependency<Platforms.CommonPlatformsProject>(target);
        }
    }


    [Generate]
    public class SharpmakeSolution : CSharpSolution
    {
        public SharpmakeSolution()
        {
            Name = "Sharpmake";

            AddTargets(
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2015,
                    Optimization.Debug | Optimization.Release,
                    framework: DotNetFramework.v4_5
                )
            );
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\..\..\";

            conf.AddProject<SharpmakeApplicationProject>(target);
            conf.AddProject<SharpmakeProject>(target);
            conf.AddProject<SharpmakeGeneratorsProject>(target);
            conf.AddProject<SharpmakeUnitTestsProject>(target);
            conf.AddProject<SharpmakeNuGetProject>(target);

            conf.AddProject<Platforms.CommonPlatformsProject>(target);
            conf.AddProject<Platforms.DurangoProject>(target);
            conf.AddProject<Platforms.NvShieldProject>(target);
            conf.AddProject<Platforms.X360Project>(target);

            conf.AddProject<Samples.ConfigureOrderProject>(target);
            conf.AddProject<Samples.CPPCLIProject>(target);
            conf.AddProject<Samples.CSharpHelloWorldProject>(target);
            conf.AddProject<Samples.CSharpVsixProject>(target);
            conf.AddProject<Samples.HelloWorldProject>(target);
            conf.AddProject<Samples.PackageReferencesProject>(target);
            conf.AddProject<Samples.SharpmakeGenProject>(target);
            conf.AddProject<Samples.SimpleExeLibDependencyProject>(target);
        }

        [Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            arguments.Generate<SharpmakeSolution>();
        }
    }
}
