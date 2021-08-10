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

        // this holds the path where sharpmake binaries are expected to output
        // note that it will contain an subdirectory per optimization
        public const string OutputRootPath = @"[project.RootPath]\tmp\bin";
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
                    framework: Assembler.SharpmakeDotNetFramework
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

                // Use the new csproj style
                ProjectSchema = CSharpProjectSchema.NetCore;

                // prevents output dir to have a framework subfolder
                CustomProperties.Add("AppendTargetFrameworkToOutputPath", "false");

                // we need to disable determinism while because we are using wildcards in assembly versions
                // error CS8357: The specified version string contains wildcards, which are not compatible with determinism
                CustomProperties.Add("Deterministic", "false");

                if (excludeSharpmakeFiles)
                    SourceFilesExcludeRegex.Add(@".*\.sharpmake.cs");
            }

            [Configure]
            public virtual void ConfigureAll(Configuration conf, Target target)
            {
                conf.ProjectFileName = "[project.Name]";
                conf.ProjectPath = @"[project.RootPath]\tmp\projects\[project.Name]";
                conf.Output = Configuration.OutputType.DotNetClassLibrary;
                conf.TargetPath = Path.Combine(Globals.OutputRootPath, "[lower:target.Optimization]");

                conf.IntermediatePath = @"[project.RootPath]\tmp\obj\[target.Optimization]\[project.Name]";
                conf.BaseIntermediateOutputPath = conf.IntermediatePath;

                conf.ReferencesByName.Add("System");

                conf.Options.Add(Assembler.SharpmakeScriptsCSharpVersion);
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

            ExtraItems[".github"] = new Strings {
                @".github\workflows\actions.yml"
            };

            ExtraItems["BatchFiles"] = new Strings {
                "bootstrap.bat",
                "CompileSharpmake.bat",
                "GenerateMdbFiles.bat",
                "UpdateSamplesOutput.bat",
                "visualstudio.sharpmake.bat"
            };
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
