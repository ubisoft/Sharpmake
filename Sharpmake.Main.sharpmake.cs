using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;

[module: Sharpmake.Include("Sharpmake/Sharpmake.sharpmake.cs")]
[module: Sharpmake.Include("Sharpmake.Application/Sharpmake.Application.sharpmake.cs")]
[module: Sharpmake.Include("Sharpmake.Extensions/Sharpmake.Extensions.sharpmake.cs")]
[module: Sharpmake.Include("Sharpmake.Generators/Sharpmake.Generators.sharpmake.cs")]
[module: Sharpmake.Include("Sharpmake.Platforms/Sharpmake.Platforms.sharpmake.cs")]
[module: Sharpmake.Include("Sharpmake.UnitTests/Sharpmake.UnitTests.sharpmake.cs")]
[module: Sharpmake.Include("samples/Sharpmake.Samples.sharpmake.cs")]

namespace SharpmakeGen
{
    public static class Globals
    {
        public static string AbsoluteRootPath = string.Empty;
    }

    public static class Common
    {
        public static ITarget[] GetDefaultTargets()
        {
            var result = new List<ITarget>();
            result.Add(
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2015,
                    Optimization.Debug | Optimization.Release,
                    framework: DotNetFramework.v4_5
                )
            );
            return result.ToArray();
        }

        public abstract class SharpmakeBaseProject : CSharpProject
        {
            protected SharpmakeBaseProject(bool excludeSharpmakeFiles = true)
            {
                AddTargets(GetDefaultTargets());

                RootPath = Globals.AbsoluteRootPath;
                SourceRootPath = @"[project.RootPath]\[project.Name]";

                if (excludeSharpmakeFiles)
                    SourceFilesExcludeRegex.Add(@".*\.sharpmake.cs");
            }

            [Configure]
            public virtual void ConfigureAll(Configuration conf, Target target)
            {
                conf.ProjectFileName = "[project.Name]";
                conf.ProjectPath = @"[project.SourceRootPath]";
                conf.Output = Configuration.OutputType.DotNetClassLibrary;
                conf.TargetPath = @"[project.RootPath]\bin\[target.Optimization]";

                conf.ReferencesByName.Add("System");

                conf.Options.Add(Options.CSharp.LanguageVersion.CSharp6);
                conf.Options.Add(Options.CSharp.TreatWarningsAsErrors.Enabled);
            }
        }
    }

    [Generate]
    public class SharpmakeSolution : CSharpSolution
    {
        public SharpmakeSolution()
        {
            Name = "Sharpmake";

            AddTargets(Common.GetDefaultTargets());
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\";

            conf.AddProject<SharpmakeApplicationProject>(target);
            conf.AddProject<SharpmakeUnitTestsProject>(target);

            // Platforms, Extensions and Samples
            foreach (Type projectType in Assembly.GetExecutingAssembly().GetTypes().Where(t =>
                t.IsSubclassOf(typeof(Platforms.PlatformProject))   ||
                t.IsSubclassOf(typeof(Extensions.ExtensionProject)) ||
                t.IsSubclassOf(typeof(Samples.SampleProject)))
            )
            {
                conf.AddProject(projectType, target);
            }
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            FileInfo sharpmakeFileInfo = Util.GetCurrentSharpmakeFileInfo();
            Globals.AbsoluteRootPath = Util.PathMakeStandard(sharpmakeFileInfo.DirectoryName);

            arguments.Generate<SharpmakeSolution>();
        }
    }
}
