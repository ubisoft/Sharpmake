// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

namespace ProjectReferencesExport
{
    public partial class BaseProject : Project
    {
        public BaseProject()
        {
            AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";

            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]\[target.Name]";

            conf.IncludePaths.Add(@"[project.SourceRootPath]\include");
            conf.IncludePrivatePaths.Add(@"[project.SourceRootPath]\src");

            conf.Options.Add(Options.Vc.Compiler.Exceptions.Enable);
        }
    }

    [Sharpmake.Generate]
    public class FooBarProject : BaseProject
    {
        [Configure]
        public void ConfigureOutput(Configuration conf, Target target)
        {
            conf.Output = Configuration.OutputType.Lib;
        }

        [Configure]
        public void ConfigurePrecompHeader(Configuration conf, Target target)
        {
            conf.PrecompHeader = "stdafx.h";
            conf.PrecompSource = "stdafx.cpp";
        }
    }

    [Sharpmake.Generate]
    public class DependantProject : BaseProject
    {
        public DependantProject()
        {
            ForceReferencesExport = true;
        }

        [Configure]
        public void ConfigureDependencies(Configuration conf, Target target)
        {
            conf.AddPrivateDependency<FooBarProject>(target);
        }

        [Configure]
        public void ConfigureOutput(Configuration conf, Target target)
        {
            conf.Output = Configuration.OutputType.Lib;
        }

        public override void PostLink()
        {
            base.PostLink();
            ConfigureSharedPrecompiledHeader();
        }

        internal void ConfigureSharedPrecompiledHeader()
        {
            foreach (Configuration conf in Configurations)
            {
                var dependency = Builder.Instance.GetProject(typeof(FooBarProject));
                var dependencyConf = dependency.GetConfiguration(conf.Target);

                conf.CompilerPdbFilePath = dependencyConf.CompilerPdbFilePath;

                string sourceRoot = Util.PathGetRelative(SourceRootPath, dependency.SourceRootPath);
                string header = Path.Combine(sourceRoot, dependencyConf.PrecompHeader);

                conf.PrecompHeader = header;
                conf.PrecompSource = "use";

                conf.ForcedIncludes.Add(header);

                conf.PrecompHeaderOutputFolder = Util.PathGetRelative(conf.ProjectPath, dependencyConf.IntermediatePath);
                conf.PrecompHeaderOutputFile = $"{dependencyConf.ProjectName}.pch";
            }
        }
    }

    [Sharpmake.Generate]
    public class MainProject : BaseProject
    {
        [Configure]
        public void ConfigureDependencies(Configuration conf, Target target)
        {
            conf.AddPrivateDependency<FooBarProject>(target);
            conf.AddPrivateDependency<DependantProject>(target);
        }
    }

    [Sharpmake.Generate]
    public class ProjectReferencesExportSolution : Sharpmake.Solution
    {
        public ProjectReferencesExportSolution()
        {
            Name = "ProjectReferencesExport";
            AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<FooBarProject>(target);
            conf.AddProject<DependantProject>(target);
            conf.AddProject<MainProject>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);

            arguments.Generate<ProjectReferencesExportSolution>();
        }
    }
}
