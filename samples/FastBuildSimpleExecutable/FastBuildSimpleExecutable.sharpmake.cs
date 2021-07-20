// Copyright (c) 2019, 2021 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
                        DevEnv.vs2019,
                        Optimization.Debug | Optimization.Release,
                        OutputType.Lib,
                        Blob.NoBlob,
                        BuildSystem.FastBuild
            ));

            SourceRootPath = @"[project.SharpmakeCsPath]\codebase";
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
        }

        [Configure(BuildSystem.FastBuild)]
        public void ConfigureFastBuild(Configuration conf, Target target)
        {
            conf.IsFastBuild = true;
            conf.FastBuildBlobbed = target.Blob == Blob.FastBuildUnitys;
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
                        DevEnv.vs2019,
                        Optimization.Debug | Optimization.Release,
                        OutputType.Lib,
                        Blob.NoBlob,
                        BuildSystem.FastBuild
            ));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
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

            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2019, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);

            arguments.Generate<FastBuildSolution>();
        }
    }
}
