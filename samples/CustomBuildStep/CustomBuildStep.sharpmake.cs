// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace FastBuild
{
    [Sharpmake.Generate]
    public class CustomBuildStepProject : Project
    {
        public CustomBuildStepProject()
        {
            Name = "CustomBuildStep";
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase";
            SourceFilesExtensions.Add(".bat");

            // need to add generated files explicitly since they don't exist yet
            SourceFiles.Add(@"[project.SourceRootPath]\main.cpp");
            SourceFiles.Add(@"[project.SourceRootPath]\concatenated.cpp");

            AddTargets(
                new Target(
                    Platform.win64,
                    DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Lib,
                    Blob.NoBlob,
                    BuildSystem.MSBuild | BuildSystem.FastBuild
                )
            );
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            // A simple custom build step that generates main.cpp via a .bat file
            conf.CustomFileBuildSteps.Add(
                new Configuration.CustomFileBuildStep
                {
                    KeyInput = "filegeneration.bat",
                    Output = "main.cpp",
                    Description = $"Generate main.cpp",
                    Executable = "filegeneration.bat"
                }
            );

            // Demonstrates a custom file build step that has two inputs and one output
            conf.CustomFileBuildSteps.Add(
                new Configuration.CustomFileBuildStep
                {
                    KeyInput = "concatenatefiles.bat",
                    Output = "concatenated.cpp",
                    Description = $"Generate concatenated.cpp",
                    Executable = "concatenatefiles.bat",
                    ExecutableArguments = "../codebase/concatenate_file1.in ../codebase/concatenate_file2.in",
                    AdditionalInputs = { "[project.SourceRootPath]\\concatenate_file1.in", "[project.SourceRootPath]\\concatenate_file2.in" }
                }
            );

            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
        }

        [Configure(BuildSystem.FastBuild)]
        public void ConfigureFastBuild(Configuration conf, Target target)
        {
            conf.Name = "FastBuild " + conf.Name;
            conf.IsFastBuild = true;
            conf.FastBuildBlobbed = target.Blob == Blob.FastBuildUnitys;

            // Force writing to pdb from different cl.exe process to go through the pdb server
            conf.AdditionalCompilerOptions.Add("/FS");
        }
    }

    [Sharpmake.Generate]
    public class CustomBuildStepSolution : Sharpmake.Solution
    {
        public CustomBuildStepSolution()
        {
            Name = "CustomBuildStepSolution";

            AddTargets(
                new Target(
                    Platform.win64,
                    DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Lib,
                    Blob.NoBlob,
                    BuildSystem.MSBuild | BuildSystem.FastBuild
                )
            );
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";

            conf.AddProject<CustomBuildStepProject>(target);
        }

        [Configure(BuildSystem.FastBuild)]
        public void ConfigureFastBuild(Configuration conf, Target target)
        {
            conf.Name = "FastBuild " + conf.Name;
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            FastBuildSettings.FastBuildMakeCommand = @"..\..\..\tools\FastBuild\Windows-x64\FBuild.exe";
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_22621_0);

            arguments.Generate<CustomBuildStepSolution>();
        }
    }
}
