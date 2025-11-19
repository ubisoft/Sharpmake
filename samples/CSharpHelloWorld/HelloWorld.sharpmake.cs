// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using Sharpmake;

namespace CSharpHelloWorld
{
    public class TargetTypes
    {
        public static Target[] GetDefaultTargets()
        {
            return new Target[]
            {
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2017,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_7_2),
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    framework: DotNetFramework.net6_0)
            };
        }
    }

    [Sharpmake.Generate]
    public class HelloWorldReference : CSharpProject
    {
        public HelloWorldReference()
        {
            AddTargets(TargetTypes.GetDefaultTargets());
            RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
            conf.ProjectPath = @"[project.RootPath]";
            conf.Options.Add(Sharpmake.Options.CSharp.TreatWarningsAsErrors.Enabled);
            conf.Output = Configuration.OutputType.DotNetClassLibrary;
        }
    }

    [Sharpmake.Generate]
    public class HelloWorld : CSharpProject
    {
        public HelloWorld()
        {
            AddTargets(TargetTypes.GetDefaultTargets());

            RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

            // This Path will be used to get all SourceFiles in this Folder and all subFolders
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            AssemblyName = "the other name";
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
            conf.ProjectPath = @"[project.RootPath]";

            conf.CustomProperties.Add("CustomOptimizationProperty", $"Custom-{target.Optimization}");

            conf.Options.Add(Sharpmake.Options.CSharp.TreatWarningsAsErrors.Enabled);
            conf.AddPublicDependency<HelloWorldReference>(target, DependencySetting.DefaultWithoutCopy);
        }
    }

    [Sharpmake.Generate]
    public class HelloWorldSolution : CSharpSolution
    {
        public HelloWorldSolution()
        {
            AddTargets(TargetTypes.GetDefaultTargets());
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = string.Format("{0}.{1}.{2}",
                                                  Name,
                                                  "[target.DevEnv]",
                                                  "[target.Framework]");
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

            conf.AddProject<HelloWorld>(target);
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<HelloWorldSolution>();
        }
    }
}
