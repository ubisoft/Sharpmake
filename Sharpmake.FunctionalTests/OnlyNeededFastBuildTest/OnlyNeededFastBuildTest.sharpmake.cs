// Copyright (c) 2022 Ubisoft Entertainment
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
                    DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release,
                    Blob.NoBlob,
                    BuildSystem.MSBuild
                )
            };

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
            RootPath = @"[project.SharpmakeCsPath]";
            SourceRootPath = @"[project.RootPath]\codebase\[project.Name]";

            AddTargets(Target.GetDefaultTargets());
        }

        [ConfigurePriority(-100)]
        [Configure]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.IsFastBuild = target.BuildSystem == BuildSystem.FastBuild;

            conf.ProjectFileName = "[project.Name]_[target.Platform]_[target.DevEnv]";
            if (conf.IsFastBuild)
            {
                conf.ProjectName += "_FastBuild";
                conf.ProjectFileName += "_FastBuild";
                conf.SolutionFolder = "FastBuild";
            }
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.IntermediatePath = @"[conf.ProjectPath]\build\[conf.Name]\[project.Name]";
            conf.TargetPath = @"[conf.ProjectPath]\output\[conf.Name]";

            conf.Output = Configuration.OutputType.Lib; // defaults to creating static libs

            // .lib files must be with the .obj files when running in fastbuild distributed mode or we'll have missing symbols due to merging of the .pdb
            conf.TargetLibraryPath = "[conf.IntermediatePath]";
        }

        [Configure(BuildSystem.FastBuild)]
        public virtual void ConfigureFastBuild(Configuration conf, Target target)
        {
            conf.BlobPath = @"[conf.ProjectPath]\unity\[project.Name]";
            conf.FastBuildBlobbed = target.Blob == Blob.FastBuildUnitys;

            // Force writing to pdb from different cl.exe process to go through the pdb server
            conf.AdditionalCompilerOptions.Add("/FS");
        }
    }

    public abstract class CommonExeProject : CommonProject
    {
        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.Exe;

            // workaround necessity of rc.exe
            if (target.Platform == Platform.win64)
                conf.Options.Add(Options.Vc.Linker.EmbedManifest.No);
        }
    }

    [Generate]
    public class SomeLib : CommonProject
    {
        public SomeLib()
        {
        }
    }

    [Generate]
    public class SomeExeWithLib : CommonExeProject
    {
        public SomeExeWithLib()
        {
        }
        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPublicDependency<SomeLib>(target);
        }
    }

    [Generate]
    public class SomeStandaloneExe : CommonExeProject
    {
        public SomeStandaloneExe()
        {
        }
    }

    [Sharpmake.Generate]
    public class OnlyNeededFastBuildTestSolution : Sharpmake.Solution
    {
        public OnlyNeededFastBuildTestSolution()
            : base(typeof(Target))
        {
            Name = "OnlyNeededFastBuildTest";
            AddTargets(Target.GetDefaultTargets());
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";

            conf.Name = "[target.NameForSolution]";
            conf.PlatformName = "[target.SolutionPlatformName]";

            conf.IncludeOnlyNeededFastBuildProjects = true;

            conf.AddProject<SomeStandaloneExe>(target);
            conf.AddProject<SomeExeWithLib>(target);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            FileInfo fileInfo = Util.GetCurrentSharpmakeFileInfo();
            string sharpmakeRootDirectory = Util.SimplifyPath(Path.Combine(fileInfo.DirectoryName, "..", ".."));

            FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeRootDirectory, @"tools\FastBuild\Windows-x64\FBuild.exe");
            FastBuildSettings.FastBuildWait = true;
            FastBuildSettings.WriteAllConfigsSection = true;

            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.Latest);

            Bff.UnityResolver = new Bff.FragmentUnityResolver();

            arguments.Generate<OnlyNeededFastBuildTestSolution>();
        }
    }
}
