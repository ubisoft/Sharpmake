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
                    framework: DotNetFramework.net6_0
                )
            );
            return result.ToArray();
        }

        public abstract class SharpmakeBaseProject : CSharpProject
        {
            public string DefaultProjectPath = @"[project.RootPath]\tmp\projects\[project.Name]";

            protected SharpmakeBaseProject(
                bool excludeSharpmakeFiles = true,
                bool generateXmlDoc = true
            )
            {
                AddTargets(GetDefaultTargets());

                GenerateDocumentationFile = generateXmlDoc;

                RootPath = Globals.AbsoluteRootPath;

                // Use the new csproj style
                ProjectSchema = CSharpProjectSchema.NetCore;

                CustomProperties.Add("Deterministic", "true");

                // Enable Globalization Invariant Mode
                // https://github.com/dotnet/runtime/blob/master/docs/design/features/globalization-invariant-mode.md
                CustomProperties.Add("InvariantGlobalization", "true");

                if (excludeSharpmakeFiles)
                    NoneExtensions.Add(".sharpmake.cs");
            }

            public override void PostResolve()
            {
                base.PostResolve();

                // retrieve the path of the csproj, could have changed from the default
                // note that we ensure that it is identical between confs
                string projectPath = Configurations.Select(conf => conf.ProjectPath).Distinct().Single();

                // we set this property to fix the nuget restore behavior which was different
                // between visual studio and command line, since this var was not initialized
                // at the same time, leading to the restore being done in different locations
                CustomProperties.Add("MSBuildProjectExtensionsPath", Util.PathGetRelative(projectPath, DefaultProjectPath));
            }

            [Configure]
            public virtual void ConfigureAll(Configuration conf, Target target)
            {
                conf.ProjectFileName = "[project.Name]";
                conf.ProjectPath = DefaultProjectPath;
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

                if (GenerateDocumentationFile)
                {
                    conf.Options.Add(
                        new Options.CSharp.SuppressWarning(
                            1570, // W1: CS1570: XML comment on 'construct' has badly formed XML - 'reason
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

            var githubFiles = Util.DirectoryGetFiles(Path.Combine(Globals.AbsoluteRootPath, ".github"));
            ExtraItems[".github"] = new Strings { githubFiles };

            var bashFiles = Util.DirectoryGetFiles(Globals.AbsoluteRootPath, "*.sh", SearchOption.TopDirectoryOnly);
            ExtraItems["BashFiles"] = new Strings { bashFiles };

            var batchFiles = Util.DirectoryGetFiles(Globals.AbsoluteRootPath, "*.bat", SearchOption.TopDirectoryOnly);
            ExtraItems["BatchFiles"] = new Strings { batchFiles };

            var pythonFiles = Util.DirectoryGetFiles(Globals.AbsoluteRootPath, "*.py", SearchOption.TopDirectoryOnly);
            ExtraItems["PythonFiles"] = new Strings { pythonFiles };
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
