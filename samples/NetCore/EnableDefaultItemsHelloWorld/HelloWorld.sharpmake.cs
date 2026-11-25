// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace NetCore.EnableDefaultItemsHelloWorld
{
    // Sample demonstrating EnableDefaultItems = true.
    // Sharpmake emits no file lists; the SDK glob discovers .cs, .resx, and .xaml automatically.
    // ContentPath triggers an explicit <Content> entry for the Content/ folder since Content
    // is not part of the SDK's default item discovery.
    [Sharpmake.Generate]
    public class HelloWorld : CSharpProject
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

        public HelloWorld()
        {
            ClearTargets();
            AddTargets(SampleTargets);

            // Place the project file alongside source files so SDK can discover them without Link entries.
            RootPath = @"[project.SharpmakeCsPath]\codebase";
            SourceRootPath = @"[project.RootPath]\[project.Name]";

            EnableDefaultItems = true;
            AppDesignerFolder = "";
            BaseIntermediateOutputPath = @"[project.SourceRootPath]\obj";

            // Content/ is not covered by SDK default globs; emit an explicit glob overlay
            // so files under Content/ get the correct <Content> build action.
            Globs.Add(new GlobSetting { Include = @"Content\**", ItemType = GlobItemType.Content });

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
    public class HelloWorldSolution : CSharpSolution
    {
        public HelloWorldSolution()
        {
            AddTargets(HelloWorld.SampleTargets);
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = string.Format("{0}.{1}.{2}", Name, "[target.DevEnv]", "[target.Framework]");
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\HelloWorld";
            conf.AddProject<HelloWorld>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            arguments.Generate<HelloWorldSolution>();
        }
    }
}
