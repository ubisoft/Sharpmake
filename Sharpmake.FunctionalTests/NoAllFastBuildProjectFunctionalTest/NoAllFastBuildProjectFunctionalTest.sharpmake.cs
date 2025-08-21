// Copyright (c) 2020-2021 Ubisoft Entertainment
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
using Sharpmake.Generators.FastBuild;

namespace SharpmakeGen.FunctionalTests
{
    public static class DefaultTarget
    {
        public static Target Get()
        {
            return new Target(
                Platform.win64,
                DevEnv.vs2022,
                Optimization.Debug | Optimization.Release
            );
        }
    }

    public abstract class CommonProject : Project
    {
        public CommonProject()
            : base(typeof(Target))
        {
            RootPath = @"[project.SharpmakeCsPath]";
            SourceRootPath = @"[project.RootPath]\codebase\[project.Name]";

            AddTargets(DefaultTarget.Get());
        }

        [Configure]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.IsFastBuild = true;

            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.IntermediatePath = @"[conf.ProjectPath]\build\[conf.Name]\[project.Name]";
            conf.TargetPath = @"[conf.ProjectPath]\output\[conf.Name]";
            conf.TargetLibraryPath = "[conf.IntermediatePath]";

            conf.IncludePaths.Add("[project.SourceRootPath]");
        }
    }

    public abstract class CommonLibProject : CommonProject
    {
        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.Lib;
        }
    }

    public abstract class CommonExeProject : CommonProject
    {
        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.Exe;

            // workaround necessity of rc.exe
            conf.Options.Add(Options.Vc.Linker.EmbedManifest.No);
        }
    }

    [Generate]
    public class LibA : CommonLibProject
    {
        public LibA() { }
    }

    [Generate]
    public class LibB : CommonLibProject
    {
        public LibB() { }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<LibA>(target);
        }
    }

    [Generate]
    public class LibC : CommonLibProject
    {
        public LibC() { }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<LibA>(target);
        }
    }

    [Generate]
    public class TestsA : CommonExeProject
    {
        public TestsA() { }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<LibA>(target);
        }
    }

    [Generate]
    public class TestsB : CommonExeProject
    {
        public TestsB() { }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<LibB>(target);
        }
    }

    [Generate]
    public class TestsC : CommonExeProject
    {
        public TestsC() { }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<LibC>(target);
        }
    }

    [Generate]
    public class MainProject : CommonExeProject
    {
        public MainProject() { }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<LibB>(target);
            conf.AddPrivateDependency<LibC>(target);
        }
    }

    [Generate]
    public class NoAllFastBuildProjectFunctionalTestSolution : Solution
    {
        public NoAllFastBuildProjectFunctionalTestSolution()
            : base(typeof(Target))
        {
            Name = "NoAllFastBuildProjectFunctionalTest";
            GenerateFastBuildAllProject = false;

            AddTargets(DefaultTarget.Get());
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";

            conf.Name = "[target.ProjectConfigurationName]_FastBuild";
            conf.PlatformName = "[target.Platform]";

            conf.AddProject<MainProject>(target);
            conf.AddProject<TestsA>(target);
            conf.AddProject<TestsB>(target);
            conf.AddProject<TestsC>(target);
        }

        [Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            FileInfo fileInfo = Util.GetCurrentSharpmakeFileInfo();
            string sharpmakeRootDirectory = Util.SimplifyPath(Path.Combine(fileInfo.DirectoryName, "..", ".."));

            FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeRootDirectory, @"tools\FastBuild\Windows-x64\FBuild.exe");
            FastBuildSettings.FastBuildWait = true;
            FastBuildSettings.WriteAllConfigsSection = true;

            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);

            Bff.UnityResolver = new Bff.FragmentUnityResolver();

            arguments.Generate<NoAllFastBuildProjectFunctionalTestSolution>();
        }
    }
}
