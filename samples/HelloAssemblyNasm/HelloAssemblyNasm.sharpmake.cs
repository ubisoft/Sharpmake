// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.
using System.IO;
using Sharpmake;

namespace HelloAssemblyNasm
{
    public static class Globals
    {
        // branch root path relative to current sharpmake file location
        public const string RelativeRootPath = @".\codebase";
        public static string RootDirectory;
        public static string NasmInstallPath = "";
    }


    [Sharpmake.Generate]
    public class HelloAssemblyNasmProject : Project
    {
        public HelloAssemblyNasmProject()
        {
            Name = "HelloAssemblyNasmExecutable";
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase";

            // The utils/precomp files are supposed to be only included, not built separately
            SourceFilesBuildExclude.Add(@"[project.SharpmakeCsPath]\codebase\sub folder\utils.nasm");
            SourceFilesBuildExclude.Add(@"[project.SharpmakeCsPath]\codebase\sub folder\precomp.nasm");

            // Remove asm from compilation, we are testing nasm only.
            SourceFilesCompileExtensions.Remove(".asm");
            SourceFilesExtensions.Remove(".asm");

            NasmExePath = Globals.NasmInstallPath;

            // FastBuild
            StripFastBuildSourceFiles = false;
            AddTargets(new Target(
                        Platform.win64,
                        DevEnv.vs2022,
                        Optimization.Debug | Optimization.Release,
                        OutputType.Lib,
                        Blob.NoBlob,
                        BuildSystem.FastBuild | BuildSystem.MSBuild
            ));
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]_[target.BuildSystem]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.Defines.Add("_HAS_EXCEPTIONS=0");

            conf.AssemblyIncludePaths.Add(@"[project.SharpmakeCsPath]\codebase\sub folder");
            conf.Project.NasmPreIncludedFiles.Add("precomp.nasm");
        }

        [Configure(BuildSystem.FastBuild)]
        public void ConfigureFastBuild(Configuration conf, Target target)
        {
            conf.IsFastBuild = true;
            conf.FastBuildBlobbed = target.Blob == Blob.FastBuildUnitys;

            // Force writing to pdb from different cl.exe process to go through the pdb server
            conf.AdditionalCompilerOptions.Add("/FS");
        }

        [Configure(BuildSystem.MSBuild)]
        public void ConfigureMSBuild(Configuration conf, Target target)
        {
            conf.Project.NasmTargetsFile = @"[project.SharpmakeCsPath]\custom\nasm.targets";
            conf.Project.NasmPropsFile = @"[project.SharpmakeCsPath]\custom\nasm.props";
        }
    }

    [Sharpmake.Generate]
    public class HelloAssemblyNasmSolution : Sharpmake.Solution
    {
        public HelloAssemblyNasmSolution()
        {
            Name = "HelloAssemblyNasm";
            AddTargets(new Target(
                        Platform.win64,
                        DevEnv.vs2022,
                        Optimization.Debug | Optimization.Release,
                        OutputType.Lib,
                        Blob.NoBlob,
                        BuildSystem.FastBuild | BuildSystem.MSBuild
            ));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]_[target.BuildSystem]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<HelloAssemblyNasmProject>(target);
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
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            ConfigureRootDirectory();

            // Hardcoded executable for simplicity, no nuget dependency.
            string nasmExePath = Util.PathGetAbsolute(Globals.RootDirectory, @"..\tools\");
            Globals.NasmInstallPath = Path.Combine(nasmExePath, "nasm.exe");

            // for the purpose of this sample, we'll reuse the FastBuild executable that live in the sharpmake source repo
            string sharpmakeFastBuildDir = Util.PathGetAbsolute(Globals.RootDirectory, @"..\..\..\tools\FastBuild");
            FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeFastBuildDir, "Windows-x64", "FBuild.exe");

            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);
            arguments.Generate<HelloAssemblyNasmSolution>();
        }
    }
}
