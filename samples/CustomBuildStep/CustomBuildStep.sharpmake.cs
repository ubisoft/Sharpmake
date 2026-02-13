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
            SourceFiles.Add(@"[project.SourceRootPath]\secondaryfile.cpp");

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
                    KeyInput = "generatemain.bat",
                    Output = "main.cpp",
                    Description = $"Generate main.cpp",
                    Executable = "generatemain.bat"
                });
            // Demonstrates a custom file build step that has two inputs and one output
            conf.CustomFileBuildSteps.Add(
                new Configuration.CustomFileBuildStep
                {
                    KeyInput = "generatesecondaryfile.bat",
                    Output = "secondaryfile.cpp",
                    Description = $"Generate secondaryfile.cpp",
                    Executable = "generatesecondaryfile.bat",
                    ExecutableArguments = "../codebase/concatenate_file1.in ../codebase/concatenate_file2.in",
                    AdditionalInputs = { "[project.SourceRootPath]\\concatenate_file1.in", "[project.SourceRootPath]\\concatenate_file2.in" }
				
                });

            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.Options.Add(Sharpmake.Options.Vc.General.DebugInformation.C7Compatible);
        }

        [Configure(BuildSystem.FastBuild)]
        public void ConfigureFastBuild(Configuration conf, Target target)
        {
            conf.Name = "FastBuild " + conf.Name;
            conf.IsFastBuild = true;
            conf.FastBuildBlobbed = target.Blob == Blob.FastBuildUnitys;
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
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_26100_0);

            arguments.Generate<CustomBuildStepSolution>();
        }
    }
}
