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
[module: Sharpmake.Include("Sharpmake.FunctionalTests/Sharpmake.FunctionalTests.sharpmake.cs")]

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
                    DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release,
                    framework: DotNetFramework.v4_7_2
                )
            );
            return result.ToArray();
        }

        public abstract class SharpmakeBaseProject : CSharpProject
        {
            private readonly bool _generateXmlDoc;

            protected SharpmakeBaseProject(
                bool excludeSharpmakeFiles = true,
                bool generateXmlDoc = true
            )
            {
                AddTargets(GetDefaultTargets());

                _generateXmlDoc = generateXmlDoc;

                RootPath = Globals.AbsoluteRootPath;

                DependenciesCopyLocal = DependenciesCopyLocalTypes.None;

                if (excludeSharpmakeFiles)
                    SourceFilesExcludeRegex.Add(@".*\.sharpmake.cs");
            }

            [Configure]
            public virtual void ConfigureAll(Configuration conf, Target target)
            {
                conf.ProjectFileName = "[project.Name]";
                conf.ProjectPath = @"[project.RootPath]\tmp\projects\[project.Name]";
                conf.Output = Configuration.OutputType.DotNetClassLibrary;
                conf.TargetPath = @"[project.RootPath]\tmp\bin\[target.Optimization]\[project.Name]";

                conf.IntermediatePath = @"[project.RootPath]\tmp\obj\[target.Optimization]\[project.Name]";
                conf.BaseIntermediateOutputPath = conf.IntermediatePath;

                conf.ReferencesByName.Add("System");

                conf.Options.Add(Options.CSharp.LanguageVersion.CSharp7);
                conf.Options.Add(Options.CSharp.TreatWarningsAsErrors.Enabled);
                conf.Options.Add(
                    new Options.CSharp.WarningsNotAsErrors(
                        618 // W1: CS0618: A class member was marked with the Obsolete attribute, such that a warning will be issued when the class member is referenced
                    )
                );

                if (_generateXmlDoc)
                {
                    conf.XmlDocumentationFile = @"[conf.TargetPath]\[project.AssemblyName].xml";
                    conf.Options.Add(
                        new Options.CSharp.SuppressWarning(
                            1570, // W1: CS1570: XML comment on 'construct' has badly formed XML — 'reason
                            1591  // W4: CS1591: Missing XML comment for publicly visible type or member 'Type_or_Member'
                        )
                    );
                }
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
                t.IsSubclassOf(typeof(Samples.SampleProject))       ||
                t.IsSubclassOf(typeof(FunctionalTests.FunctionalTestProject)))
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
