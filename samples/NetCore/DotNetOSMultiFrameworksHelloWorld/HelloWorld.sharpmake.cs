// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sharpmake;
namespace NetCore.DotNetOSMultiFrameworksHelloWorld
{
    public static class Globals
    {
        // branch root path relative to current sharpmake file location
        public const string RelativeRootPath = @".\codebase";
        public static string RootDirectory;
        public static string TmpDirectory { get { return Path.Combine(RootDirectory, "temp"); } }
        public static string OutputDirectory { get { return Path.Combine(TmpDirectory, "bin"); } }
    }

    [DebuggerDisplay("\"{Platform}_{DevEnv}\" {Name}")]
    public class CommonTarget : Sharpmake.ITarget
    {
        public Platform Platform;
        public DevEnv DevEnv;
        public Optimization Optimization;
        public DotNetFramework DotNetFramework;
        public DotNetOS DotNetOS;

        public CommonTarget() { }

        public CommonTarget(
            Platform platform,
            DevEnv devEnv,
            Optimization optimization,
            DotNetFramework dotNetFramework,
            DotNetOS dotNetOS
        )
        {
            Platform = platform;
            DevEnv = devEnv;
            Optimization = optimization;
            DotNetFramework = dotNetFramework;
            DotNetOS = dotNetOS;
        }

        public override string Name
        {
            get
            {
                var nameParts = new List<string>
                {
                    Optimization.ToString()
                };
                return string.Join(" ", nameParts);
            }
        }

        /// <summary>
        /// returns a string usable as a directory name, to use for instance for the intermediate path
        /// </summary>
        public string DirectoryName
        {
            get
            {
                var dirNameParts = new List<string>();

                dirNameParts.Add(Platform.ToString());
                dirNameParts.Add(Optimization.ToString());

                return string.Join("_", dirNameParts);
            }
        }

        public static CommonTarget[] GetDefaultTargets(DotNetOS dotNetOS = DotNetOS.Default)
        {
            var netFrameworkTarget = new CommonTarget(
                Platform.anycpu,
                DevEnv.vs2022,
                Optimization.Debug | Optimization.Release,
                DotNetFramework.v4_7_2,
                dotNetOS: 0 // OS is not applicable for .net framework
            );

            var netCoreTarget = new CommonTarget(
                Platform.anycpu,
                DevEnv.vs2022,
                Optimization.Debug | Optimization.Release,
                DotNetFramework.net6_0,
                dotNetOS: dotNetOS
            );

            return new[] { netFrameworkTarget, netCoreTarget };
        }

        public ITarget ToDefaultDotNetOSTarget()
        {
            return ToSpecificDotNetOSTarget(DotNetOS.Default);
        }

        public ITarget ToSpecificDotNetOSTarget(DotNetOS dotNetOS)
        {
            if (DotNetOS == 0 || DotNetOS == dotNetOS)
                return this;

            return Clone(dotNetOS);
        }
    }

    public abstract class CommonCSharpProject : CSharpProject
    {
        protected CommonCSharpProject()
            : base(typeof(CommonTarget))
        {
            RootPath = Globals.RootDirectory;
        }

        [ConfigurePriority(-100)]
        [Configure]
        public virtual void ConfigureAll(Configuration conf, CommonTarget target)
        {
            conf.ProjectFileName = "[project.Name].[target.DevEnv]";
            conf.ProjectPath = @"[project.SourceRootPath]";

            conf.IntermediatePath = Path.Combine(Globals.TmpDirectory, @"obj\[target.DirectoryName]\[project.Name]");
            conf.TargetLibraryPath = Path.Combine(Globals.TmpDirectory, @"lib\[target.DirectoryName]\[project.Name]");
            conf.TargetPath = Path.Combine(Globals.OutputDirectory, "[target.DirectoryName]");

            conf.Options.Add(Options.CSharp.WarningLevel.Level5);
            conf.Options.Add(Options.CSharp.TreatWarningsAsErrors.Enabled);
        }
    }

    [Generate]
    public class HelloWorldLib : CommonCSharpProject
    {
        public HelloWorldLib()
        {
            SourceRootPath = @"[project.RootPath]\[project.Name]";
            AddTargets(CommonTarget.GetDefaultTargets());
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.DotNetClassLibrary;

            if (target.DotNetFramework.IsDotNetFramework())
                conf.ReferencesByName.Add("System");

            if (target.DotNetFramework.IsDotNetCore())
                conf.ReferencesByNuGetPackage.Add("System.Text.Encoding.CodePages", "4.5.0");
        }
    }

    [Sharpmake.Generate]
    public class HelloWorldExe : CommonCSharpProject
    {
        public HelloWorldExe()
        {
            SourceRootPath = @"[project.RootPath]\HelloWorldMultiframeworks";
            AddTargets(CommonTarget.GetDefaultTargets());
            AddTargets(new CommonTarget(
                Platform.anycpu,
                DevEnv.vs2022,
                Optimization.Debug | Optimization.Release,
                DotNetFramework.net6_0,
                dotNetOS: DotNetOS.windows
            ));
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.DotNetConsoleApp;
            conf.AddPrivateDependency<HelloWorldLib>(target.ToDefaultDotNetOSTarget());

            if (target.DotNetFramework.IsDotNetCore())
            {
                if (target.DotNetFramework.HasFlag(DotNetFramework.netcore3_1))
                {
                    conf.Options.Add(Options.CSharp.UseWpf.Enabled);

                    conf.ReferencesByNuGetPackage.Add("Microsoft.Windows.Compatibility", "3.1.0");
                }
                else
                {
                    conf.Options.Add(Options.CSharp.UseWindowsForms.Enabled);

                    conf.ReferencesByNuGetPackage.Add("Microsoft.Windows.Compatibility", "6.0.6");
                }
            }
        }
    }

    [Generate]
    public class OSMultiFrameworksHelloWorldSolution : CSharpSolution
    {
        public OSMultiFrameworksHelloWorldSolution()
            : base(typeof(CommonTarget))
        {
            AddTargets(CommonTarget.GetDefaultTargets());
        }

        [Configure]
        public void ConfigureAll(Configuration conf, CommonTarget target)
        {
            conf.SolutionFileName = Name;
            conf.SolutionPath = Path.Combine(Globals.TmpDirectory, "solutions");

            conf.AddProject<HelloWorldExe>(target.ToSpecificDotNetOSTarget(DotNetOS.windows));
        }
    }

    public static class Main
    {
        private static void ConfigureRootDirectory()
        {
            FileInfo fileInfo = Util.GetCurrentSharpmakeFileInfo();
            string rootDirectory = Path.Combine(fileInfo.DirectoryName, Globals.RelativeRootPath);
            Globals.RootDirectory = Util.SimplifyPath(rootDirectory);
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            ConfigureRootDirectory();

            arguments.Generate<OSMultiFrameworksHelloWorldSolution>();
        }
    }
}
