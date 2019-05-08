// Copyright (c) 2017 Ubisoft Entertainment
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;
using Sharpmake.Generators.FastBuild;

namespace SharpmakeGen.FunctionalTests
{
    [DebuggerDisplay("\"{Platform}_{DevEnv}\" {Name}")]
    public class Target : Sharpmake.ITarget
    {
        public Platform Platform;
        public DevEnv DevEnv;
        public Optimization Optimization;
        public Blob Blob;
        public BuildSystem BuildSystem;

        public Target() { }

        public Target(
            Platform platform,
            DevEnv devEnv,
            Optimization optimization,
            Blob blob,
            BuildSystem buildSystem
        )
        {
            Platform = platform;
            DevEnv = devEnv;
            Optimization = optimization;
            Blob = blob;
            BuildSystem = buildSystem;
        }

        public override string Name
        {
            get
            {
                var nameParts = new List<string>();

                nameParts.Add(Optimization.ToString());

                nameParts.Add(BuildSystem.ToString());

                if ((BuildSystem == BuildSystem.FastBuild && Blob == Blob.NoBlob) || Blob == Blob.Blob)
                    nameParts.Add(Blob.ToString());

                nameParts.Add(DevEnv.ToString());

                return string.Join("_", nameParts);
            }
        }

        public string NameForSolution
        {
            get
            {
                return Optimization.ToString();
            }
        }

        public string SolutionPlatformName
        {
            get
            {
                var nameParts = new List<string>();

                nameParts.Add(BuildSystem.ToString());

                if (BuildSystem == BuildSystem.FastBuild && Blob == Blob.NoBlob)
                    nameParts.Add(Blob.ToString());

                return string.Join("_", nameParts);
            }
        }

        public static ITarget[] GetDefaultTargets()
        {
            var targets = new List<ITarget> {
                new Target(
                    Platform.win64,
                    DevEnv.vs2017,
                    Optimization.Debug | Optimization.Release,
                    Blob.NoBlob,
                    BuildSystem.MSBuild
                )
            };

            // make a fastbuild no-blob version of the target
            targets.Add(targets.First().Clone(BuildSystem.FastBuild));

            // and a fastbuild unity version of the target
            targets.Add(targets.First().Clone(Blob.FastBuildUnitys, BuildSystem.FastBuild));

            return targets.ToArray();
        }
    }

    public abstract class CommonProject : Project
    {
        public CommonProject()
            : base(typeof(Target))
        {
            RootPath = @"[project.SharpmakeCsPath]\codebase";
            SourceRootPath = @"[project.RootPath]\[project.Name]";

            AddTargets(Target.GetDefaultTargets());
        }

        [Configure]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.Output = Configuration.OutputType.Lib;

            conf.IntermediatePath = @"[conf.ProjectPath]\build\[conf.Name]\[project.Name]";
            conf.TargetPath = @"[conf.ProjectPath]\output\[conf.Name]";

            // .lib files must be with the .obj files when running in fastbuild distributed mode or we'll have missing symbols due to merging of the .pdb
            conf.TargetLibraryPath = "[conf.IntermediatePath]";
        }

        [Configure(BuildSystem.FastBuild)]
        public virtual void ConfigureFastBuild(Configuration conf, Target target)
        {
            conf.IsFastBuild = true;
            conf.FastBuildBlobbed = target.Blob == Blob.FastBuildUnitys;

            // Force writing to pdb from different cl.exe process to go through the pdb server
            conf.AdditionalCompilerOptions.Add("/FS");
        }

        [Configure(Blob.FastBuildUnitys)]
        public virtual void FastBuildUnitys(Configuration conf, Target target)
        {
            conf.BlobPath = @"[conf.ProjectPath]\unity\[project.Name]";
            conf.FastBuildBlobbingStrategy = Configuration.InputFileStrategy.Exclude;
            conf.FastBuildNoBlobStrategy = Configuration.InputFileStrategy.Include;
        }

        [Configure(Blob.NoBlob)]
        public virtual void BlobNoBlob(Configuration conf, Target target)
        {
        }

        [Configure(Platform.win64)]
        public virtual void ConfigureWin64(Configuration conf, Target target)
        {
        }
    }

    public abstract class CommonExeProject : CommonProject
    {
        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.Exe;
        }

        public override void ConfigureWin64(Configuration conf, Target target)
        {
            base.ConfigureWin64(conf, target);

            // workaround necessity of rc.exe
            conf.Options.Add(Options.Vc.Linker.EmbedManifest.No);
        }
    }

    [Generate]
    public class MixCppAndCExe : CommonExeProject
    {
        public MixCppAndCExe() { }
    }

    public abstract class SpanMultipleSrcDirs : CommonExeProject
    {
        public SpanMultipleSrcDirs()
        {
            SourceRootPath = @"[project.RootPath]\SpanMultipleSrcDirs\main_dir";
            AdditionalSourceRootPaths.Add(@"[project.RootPath]\SpanMultipleSrcDirs\additional_dir");
            SourceFiles.Add(
                @"..\dir_individual_files\floating_class.cpp",
                @"..\dir_individual_files\floating_class.h",
                @"..\dir_individual_files\floating_file.cpp"
            );
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            // needed to allow the files from the main SourceRootPath to include things from that dir
            conf.IncludePrivatePaths.Add(@"[project.RootPath]\SpanMultipleSrcDirs\dir_individual_files");

            // needed to allow the files from the main SourceRootPath to include things in AdditionalSourceRootPaths
            conf.IncludePrivatePaths.AddRange(AdditionalSourceRootPaths);
        }
    }

    [Generate]
    public class SpanMultipleSrcDirsFBUnityInclude : SpanMultipleSrcDirs
    {
        public SpanMultipleSrcDirsFBUnityInclude()
        {
            AddFragmentMask(Blob.FastBuildUnitys);
        }

        public override void FastBuildUnitys(Configuration conf, Target target)
        {
            base.FastBuildUnitys(conf, target);
            conf.FastBuildBlobbingStrategy = Configuration.InputFileStrategy.Include;
        }
    }

    [Generate]
    public class SpanMultipleSrcDirsFBUnityExclude : SpanMultipleSrcDirs
    {
        public SpanMultipleSrcDirsFBUnityExclude()
        {
            AddFragmentMask(Blob.FastBuildUnitys);
        }

        public override void FastBuildUnitys(Configuration conf, Target target)
        {
            base.FastBuildUnitys(conf, target);
            conf.FastBuildBlobbingStrategy = Configuration.InputFileStrategy.Exclude;
        }
    }

    [Generate]
    public class SpanMultipleSrcDirsFBNoBlobInclude : SpanMultipleSrcDirs
    {
        public SpanMultipleSrcDirsFBNoBlobInclude()
        {
            AddFragmentMask(Blob.NoBlob);
        }

        public override void BlobNoBlob(Configuration conf, Target target)
        {
            base.BlobNoBlob(conf, target);
            conf.FastBuildNoBlobStrategy = Configuration.InputFileStrategy.Include;
        }
    }

    [Generate]
    public class SpanMultipleSrcDirsFBNoBlobExclude : SpanMultipleSrcDirs
    {
        public SpanMultipleSrcDirsFBNoBlobExclude()
        {
            AddFragmentMask(Blob.NoBlob);
        }

        public override void BlobNoBlob(Configuration conf, Target target)
        {
            base.BlobNoBlob(conf, target);
            conf.FastBuildNoBlobStrategy = Configuration.InputFileStrategy.Exclude;
        }
    }

    [Generate]
    public class UsePrecompExe : CommonExeProject
    {
        public UsePrecompExe()
        {
            SourceFilesExtensions.Add(
                ".ceecee",
                ".ceepeepee"
            );
            SourceFilesCompileExtensions.Add(
                ".ceecee",
                ".ceepeepee"
            );
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.PrecompHeader = "precomp.h";
            conf.PrecompSource = "precomp.cpp";

            // FIXME: the following line exposes a bug, since the filename ends with the precomp name...
            //conf.PrecompSourceExclude.Add("util_noprecomp.cpp");

            conf.PrecompSourceExclude.Add("noprecomp_util.cpp");
            conf.PrecompSourceExcludeExtension.Add(".ceepeepee");
        }
    }

    [Generate]
    public class RequirePreBuildStep : CommonExeProject
    {
        public RequirePreBuildStep()
        {
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            string tempGeneratedPath = @"[project.SharpmakeCsPath]\projects\generated";
            string generatedHeaderFile = Path.Combine(tempGeneratedPath, "header_generated_by_prebuild_step.h");

            // Create a PreBuild step that creates a header file that is required for compilation
            var preBuildStep = new Configuration.BuildStepExecutable(
                @"[project.SourceRootPath]\execute.bat",
                @"[project.SourceRootPath]\main.cpp",
                generatedHeaderFile,
                "echo #define PREBUILD_GENERATED_DEFINE() 0 > " + generatedHeaderFile);

            conf.EventCustomPrebuildExecute.Add("GenerateHeader", preBuildStep);

            conf.IncludePrivatePaths.Add(tempGeneratedPath);
        }
    }

    [Sharpmake.Generate]
    public class FastBuildFunctionalTestSolution : Sharpmake.Solution
    {
        public FastBuildFunctionalTestSolution()
            : base(typeof(Target))
        {
            Name = "FastBuildFunctionalTest";
            AddTargets(Target.GetDefaultTargets());
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";

            conf.Name = "[target.NameForSolution]";
            conf.PlatformName = "[target.SolutionPlatformName]";

            conf.AddProject<MixCppAndCExe>(target);
            conf.AddProject<UsePrecompExe>(target);
            conf.AddProject<RequirePreBuildStep>(target);

            if (target.Blob == Blob.FastBuildUnitys)
            {
                conf.AddProject<SpanMultipleSrcDirsFBUnityInclude>(target);
                conf.AddProject<SpanMultipleSrcDirsFBUnityExclude>(target);
            }
            else if (target.Blob == Blob.NoBlob)
            {
                if (target.BuildSystem == BuildSystem.FastBuild)
                {
                    conf.AddProject<SpanMultipleSrcDirsFBNoBlobInclude>(target);
                    conf.AddProject<SpanMultipleSrcDirsFBNoBlobExclude>(target);
                }
            }
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            FileInfo fileInfo = Util.GetCurrentSharpmakeFileInfo();
            string sharpmakeRootDirectory = Util.SimplifyPath(Path.Combine(fileInfo.DirectoryName, "..", ".."));

            FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeRootDirectory, @"tools\FastBuild\FBuild.exe");
            FastBuildSettings.FastBuildWait = true;
            FastBuildSettings.WriteAllConfigsSection = true;

            Bff.UnityResolver = new Bff.FragmentUnityResolver();

            arguments.Generate<FastBuildFunctionalTestSolution>();
        }
    }
}
