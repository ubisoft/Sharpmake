// Copyright (c) 2022 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using Sharpmake;
using System;
using System.IO;

namespace HelloRust
{
    public static class Globals
    {
        public const string RelativeRootPath = @".\codebase";
        public static string RootDir;
        public static string TmpDir { get { return Path.Combine(RootDir, "tmp"); } }
        public static string ObjectDir { get { return Path.Combine(TmpDir, "obj"); } }
        public static string OutputDir { get { return Path.Combine(TmpDir, "bin"); } }

        internal static void ConfigureRootDirectory()
        {
            FileInfo fileInfo = Util.GetCurrentSharpmakeFileInfo();
            string rootDir = Path.Combine(fileInfo.DirectoryName, RelativeRootPath);
            RootDir = Util.SimplifyPath(rootDir);
        }
    }

    [Sharpmake.Generate]
    public class Cpp : Project
    {
        public Cpp()
        {
            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2019,
                Optimization.Debug | Optimization.Release
            ));

            RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

            // This Path will be used to get all SourceFiles in this Folder and all subFolders
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.IntermediatePath = Path.Combine(Globals.ObjectDir, @"[project.Name]\[target.Optimization]");
            conf.TargetPath = Path.Combine(Globals.OutputDir, @"[target.Optimization]");

            conf.AddPrivateDependency<HelloRust>(target);
        }
    }

    [Sharpmake.Generate]
    public class HelloRust : RustProject
    {
        public HelloRust()
            : base(typeof(Target), @"[project.SourceRootPath]\Cargo.toml")
        {
            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2019,
                Optimization.Debug | Optimization.Release
            ));

            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
        }

        [Sharpmake.Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.IntermediatePath = Path.Combine(Globals.ObjectDir, @"[project.Name]\[target.Optimization]");
            conf.TargetPath = Path.Combine(Globals.OutputDir, @"[target.Optimization]");

            AddRustBuildStep(conf, target);
        }
    }

    [Sharpmake.Generate]
    public class HelloRustSolution : Solution
    {
        public HelloRustSolution()
        {
            Name = "HelloRust";

            AddTargets(new Target(
                    Platform.win64,
                    DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release
            ));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<Cpp>(target);
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            Globals.ConfigureRootDirectory();

            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2017, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_17763_0);
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2019, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);

            arguments.Generate<HelloRustSolution>();
        }
    }
}
