// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace NetCore.EnableDefaultItemsFinegrained
{
    // Sample demonstrating per-type SDK default-item suppression.
    // EnableDefaultItems = true with EnableDefaultNoneItems = false and EnableDefaultPageItems = false:
    // - .cs and .resx are discovered by SDK globs (no explicit lists)
    // - Page items (.xaml) are suppressed from the SDK glob; Sharpmake emits explicit <Page> entries
    // - None items (.txt) are suppressed from the SDK glob; Sharpmake emits explicit <None> entries
    [Sharpmake.Generate]
    public class HelloWorldDefaultItemsFinegrained : CSharpProject
    {
        internal static ITarget[] SampleTargets = new ITarget[]
        {
            new Target(
                Platform.anycpu,
                DevEnv.vs2022,
                Optimization.Debug | Optimization.Release)
            {
                OutputType = OutputType.Dll,
                Framework = DotNetFramework.net8_0
            }
        };

        public HelloWorldDefaultItemsFinegrained()
        {
            ClearTargets();
            AddTargets(SampleTargets);

            // Place the project file alongside source files so SDK can discover them without Link entries.
            RootPath = @"[project.SharpmakeCsPath]\codebase";
            SourceRootPath = @"[project.RootPath]\[project.Name]";

            EnableDefaultItems = true;
            AppDesignerFolder = "";
            BaseIntermediateOutputPath = @"[project.SourceRootPath]\obj";

            // SDK Page glob suppressed — Sharpmake emits explicit <Page> entries for .xaml.
            EnableDefaultPageItems = false;

            // SDK None glob suppressed — Sharpmake emits explicit <None> entries for .txt.
            EnableDefaultNoneItems = false;
            NoneExtensions.Add(".txt");

            // These extensions are for WPF/WinForms resource embedding; clear them for a
            // clean SDK project that doesn't use those technologies.
            ResourceFilesExtensions.Clear();
        }

        [Configure]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
            conf.ProjectPath = @"[project.SourceRootPath]";
            conf.Output = Configuration.OutputType.DotNetConsoleApp;
            conf.IntermediatePath = null;
            conf.Options.Add(Options.CSharp.TreatWarningsAsErrors.Enabled);
            conf.Options.Add(Options.CSharp.FileAlignment.None);
        }
    }

    [Sharpmake.Generate]
    public class HelloWorldDefaultItemsFinegrainedSolution : CSharpSolution
    {
        public HelloWorldDefaultItemsFinegrainedSolution()
        {
            AddTargets(HelloWorldDefaultItemsFinegrained.SampleTargets);
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = string.Format("{0}.{1}.{2}", Name, "[target.DevEnv]", "[target.Framework]");
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\HelloWorldDefaultItemsFinegrained";
            conf.AddProject<HelloWorldDefaultItemsFinegrained>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            arguments.Generate<HelloWorldDefaultItemsFinegrainedSolution>();
        }
    }
}
