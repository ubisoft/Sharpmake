// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

namespace FastBuild
{
    public static class Globals
    {
        // branch root path relative to current sharpmake file location
        public const string RelativeRootPath = @".\codebase";
        public static string RootDirectory;
    }

    [Sharpmake.Generate]
    public class FastBuildSimpleExecutable : Project
    {
        public FastBuildSimpleExecutable()
        {
            Name = "FastBuildSimpleExecutable";

            StripFastBuildSourceFiles = false;

            AddTargets(new Target(
                        Platform.win64,
                        DevEnv.vs2019 | DevEnv.vs2022,
                        Optimization.Debug | Optimization.Release,
                        OutputType.Lib,
                        Blob.NoBlob,
                        BuildSystem.FastBuild | BuildSystem.MSBuild
            ));

            SourceRootPath = @"[project.SharpmakeCsPath]\codebase";
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]_[target.BuildSystem]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.Options.Add(Options.Vc.Compiler.Exceptions.Enable);
        }

        [Configure(BuildSystem.FastBuild)]
        public void ConfigureFastBuild(Configuration conf, Target target)
        {
            conf.IsFastBuild = true;
            conf.FastBuildBlobbed = target.Blob == Blob.FastBuildUnitys;

            // Force writing to pdb from different cl.exe process to go through the pdb server
            conf.AdditionalCompilerOptions.Add("/FS");
            
            conf.IntellisenseAdditionalDefines.Add("MY_INTELLISENSE_DEFINE", "MY_INTELLISENSE_DEFINE2");
            conf.IntellisenseAdditionalCommandLineOptions.Add("/MY_INTELLISENSE_OPTION", "/MY_INTELLISENSE_OPTION2"); // Dummy options just to validate the output
            conf.FastBuildLinkConcurrencyGroup = "Test1";
        }

        [Configure(Optimization.Release)]
        public virtual void ConfigureRelease(Configuration conf, Target target)
        {
            // Testing generation of what is needed for working fastbuild deoptimization when using non-exposed compiler optimization options.
            conf.AdditionalCompilerOptimizeOptions.Add("/O2"); // This switch is known but for the purpose of this test we will put in in this field.
            conf.AdditionalCompilerOptimizeOptions.Add("/Os"); // This switch is known but for the purpose of this test we will put in in this field.
            conf.AdditionalCompilerOptions.Add("/bigobj");
            conf.FastBuildDeoptimization = Configuration.DeoptimizationWritableFiles.DeoptimizeWritableFiles;
        }
    }

    [Sharpmake.Generate]
    public class FastBuildSolution : Sharpmake.Solution
    {
        public FastBuildSolution()
        {
            Name = "FastBuildSample";

            AddTargets(new Target(
                        Platform.win64,
                        DevEnv.vs2019 | DevEnv.vs2022,
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

            conf.AddProject<FastBuildSimpleExecutable>(target);
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

            // for the purpose of this sample, we'll reuse the FastBuild executable that live in the sharpmake source repo
            string sharpmakeFastBuildDir = Util.PathGetAbsolute(Globals.RootDirectory, @"..\..\..\tools\FastBuild");
            FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeFastBuildDir, "Windows-x64", "FBuild.exe");

            // This is necessary since there is no rc.exe in the same directory than link.exe
            FastBuildSettings.SetPathToResourceCompilerInEnvironment = true;

            // Add an additional environment variable for fastbuild for testing
            FastBuildSettings.AdditionalGlobalEnvironmentVariables.Add("KEY", "VALUE");

            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2019, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);

            // Defining several groups to insure that the generation is correct but only the first one is truly used.
            // Compiling debug + release at same time while -monitor is specified on commandline will show that a single compilation is done at a time(see %TEMP%\FastBuildLog.Log).
            FastBuildSettings.EnableConcurrencyGroups = true;
            FastBuildSettings.AddConcurrencyGroup("Test1", new FastBuildSettings.ConcurrencyGroup { ConcurrencyLimit = 1 });
            FastBuildSettings.AddConcurrencyGroup("Test2", new FastBuildSettings.ConcurrencyGroup { ConcurrencyPerJobMiB = 5000 });
            FastBuildSettings.AddConcurrencyGroup("Test3", new FastBuildSettings.ConcurrencyGroup { ConcurrencyLimit = 42, ConcurrencyPerJobMiB = 5000 });

            arguments.Generate<FastBuildSolution>();
        }
    }
}
