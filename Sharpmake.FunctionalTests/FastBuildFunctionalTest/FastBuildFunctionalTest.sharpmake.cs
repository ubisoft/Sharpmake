// Copyright (c) 2019-2022 Ubisoft Entertainment
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
    public static class FunctionalTestArguments
    {
        public static bool EnableLinkerMultiStamp = false;

        [CommandLine.Option("enableLinkerMultiStamp", @"Allow more than one post-build stamper for one executable. Default value: false. ex: /enableLinkerMultiStamp(<true|false>)")]
        public static void CommandLineEnableLinkerMultiStamp(bool value)
        {
            EnableLinkerMultiStamp = value;
        }
    }

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

            // make a fastbuild no-blob version of the target
            targets.Add(targets.First().Clone(BuildSystem.FastBuild));

            // and a fastbuild unity version of the target
            targets.Add(targets.First().Clone(Blob.FastBuildUnitys, BuildSystem.FastBuild));

            return targets.ToArray();
        }
    }

    public class SortableBuildStepCopy : Sharpmake.Project.Configuration.BuildStepCopy
    {
        public int Order;

        public SortableBuildStepCopy(string sourcePath, string destinationPath, bool isNameSpecific = false, string copyPattern = "*", bool fileCopy = true) :
            base(sourcePath, destinationPath, isNameSpecific, copyPattern, fileCopy)
        {
        }

        public override int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            if (obj is SortableBuildStepCopy)
                return Order.CompareTo((obj as SortableBuildStepCopy).Order);

            return 0;
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
            SourceRootPath = @"[project.RootPath]\codebase\SpanMultipleSrcDirs\main_dir";
            AdditionalSourceRootPaths.Add(@"[project.RootPath]\codebase\SpanMultipleSrcDirs\additional_dir");
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
            conf.IncludePrivatePaths.Add(@"[project.RootPath]\codebase\SpanMultipleSrcDirs\dir_individual_files");

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
    public class SpanMultipleSrcDirsFBUnityIsolate : SpanMultipleSrcDirs
    {
        public SpanMultipleSrcDirsFBUnityIsolate()
        {
            AddFragmentMask(Blob.FastBuildUnitys);
        }

        public override void FastBuildUnitys(Configuration conf, Target target)
        {
            base.FastBuildUnitys(conf, target);

            // Isolating writable files works well only for Perforce
            conf.FastBuildUnityInputIsolateWritableFiles = false;

            // Provides a list of files that should be manually isolated. It can be used for Git difference of names, for example
            conf.FastBuildUnityInputIsolateListFile = @"[project.RootPath]\codebase\SpanMultipleSrcDirs\temp\isolate_list.txt";
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

            conf.Defines.Add("SOME_UTILITY_STRING=\"UTIL FUNC\"");
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

            string generatedHeaderFilename = "header_generated_by_prebuild_step.h";
            string relativeGeneratedHeaderFilePath = Path.Combine("generated", generatedHeaderFilename);
            string absoluteGeneratedHeaderPath = Path.Combine("[conf.ProjectPath]", "generated");
            string absoluteGeneratedHeaderFilePath = Path.Combine(absoluteGeneratedHeaderPath, generatedHeaderFilename);

            // Create a PreBuild step that creates a header file that is required for compilation
            var preBuildStep = new Configuration.BuildStepExecutable(
                @"[project.SourceRootPath]\execute.bat",
                @"[project.SourceRootPath]\main.cpp",
                absoluteGeneratedHeaderFilePath,
                "echo #define PREBUILD_GENERATED_DEFINE() 0 > " + relativeGeneratedHeaderFilePath);

            conf.EventCustomPrebuildExecute.Add("GenerateHeader", preBuildStep);

            conf.IncludePrivatePaths.Add(absoluteGeneratedHeaderPath);
        }
    }

    [Generate]
    public class PostBuildCopySingleFileTest : CommonExeProject
    {
        public PostBuildCopySingleFileTest()
        {
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            // Create a PostBuild step that copies the buildoutput to another subfolder.
            // Note that the target path needs to be a file path, not a folder. Otherwise
            // Sharpmake will create a CopyDir node instead of a Copy node.
            var copyFileBuildStep = new Configuration.BuildStepCopy(
                @"[conf.TargetPath]\[conf.TargetFileName].exe",
                @"[conf.TargetPath]\file_copy_destination\[conf.TargetFileName].exe");

            conf.EventCustomPostBuildExe.Add(copyFileBuildStep);
        }
    }

    [Generate]
    public class PostBuildCopyDirTest : CommonExeProject
    {
        public PostBuildCopyDirTest()
        {
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            // Create a PostBuild step that copies all .cpp & .txt files from the source dir to another folder
            // Note that this copy step will not depend on compilation output and thus FastBuild is free
            // execute the copy operation during or before compilation.
            var copyDirBuildStep = new Configuration.BuildStepCopy(
                @"[project.SourceRootPath]",
                @"[conf.TargetPath]\file_copy_destination");

            copyDirBuildStep.IsFileCopy = false;
            copyDirBuildStep.CopyPattern = "*.cpp *.txt";

            conf.EventCustomPostBuildExe.Add(copyDirBuildStep);
        }
    }

    [Generate]
    public class PostBuildCopyDirNoPatternTest : CommonExeProject
    {
        public PostBuildCopyDirNoPatternTest()
        {
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            // Create a PostBuild step that copies all .cpp & .txt files from the source dir to another folder
            // Note that this copy step will not depend on compilation output and thus FastBuild is free
            // execute the copy operation during or before compilation.
            var copyDirBuildStep = new Configuration.BuildStepCopy(
                @"[project.SourceRootPath]",
                @"[conf.TargetPath]\file_copy_destination_no_pattern");

            copyDirBuildStep.IsFileCopy = false;
            copyDirBuildStep.CopyPattern = string.Empty;

            conf.EventCustomPostBuildExe.Add(copyDirBuildStep);
        }
    }

    [Generate]
    public class ExplicitlyOrderedPostBuildTest : CommonExeProject
    {
        public ExplicitlyOrderedPostBuildTest()
        {
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            // Copy executable (build output) to another folder. This does not need explicit prebuild dependencies
            // in FastBuild, since the source path is a file node that is defined by the Executable() function (the linker step).
            // Therefore linking the executable is implicitly a prebuild dependency of this copy step.
            var copyExeFileBuildStep = new SortableBuildStepCopy(
            @"[conf.TargetPath]\[conf.TargetFileName].exe",
            @"[conf.TargetPath]\explicitly_ordered_postbuild_test\temp_copy\[conf.TargetFileName].exe");
            conf.EventCustomPostBuildExe.Add(copyExeFileBuildStep);

            // Copy the PDB to another folder. FastBuild has no knowledge about the source of this pdb file.
            // Since we add the BuildStepCopy object to the PostBuildExe list, Sharpmake should create a PreBuildDependency
            // for the linker step automatically, to make sure the copy is executed only after the linking, which creates the pdb.
            var copyPdbFileBuildStep = new SortableBuildStepCopy(
            @"[conf.TargetPath]\[conf.TargetFileName].pdb",
            @"[conf.TargetPath]\explicitly_ordered_postbuild_test\temp_copy\[conf.TargetFileName].pdb");
            conf.EventCustomPostBuildExe.Add(copyPdbFileBuildStep);

            // Copy both .exe & .pdb to another location. This needs explicit ordering to ensure that the folder is only copied
            // after the folder is filled with the two previous copy steps.
            var copyDirBuildStep = new SortableBuildStepCopy(
                @"[conf.TargetPath]\explicitly_ordered_postbuild_test\temp_copy",
                @"[conf.TargetPath]\file_copy_destination");
            copyDirBuildStep.IsFileCopy = false;
            copyDirBuildStep.CopyPattern = "*.*";
            copyDirBuildStep.Order = 1;
            conf.EventCustomPostBuildExe.Add(copyDirBuildStep);
        }
    }

    [Generate]
    public class PostBuildExecuteTest : CommonExeProject
    {
        public PostBuildExecuteTest()
        {
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            // Create an Executable build step that executes the build output.
            var execBuildStep = new Configuration.BuildStepExecutable(
                @"[conf.TargetPath]\[conf.TargetFileName].exe",
                @"",
                @"[conf.TargetPath]\postbuild_exec_sentinel.txt",
                @"");

            execBuildStep.FastBuildUseStdOutAsOutput = true;

            conf.EventCustomPostBuildExe.Add(execBuildStep);
        }
    }

    [Generate]
    public class PostBuildTestExecution : CommonExeProject
    {
        public PostBuildTestExecution()
        {
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            // Create a Test build step that executes the build output.
            // All output from the execution is written to 'test_execution_output.txt'.
            var testBuildStep = new Configuration.BuildStepTest(
                @"[conf.TargetPath]\[conf.TargetFileName].exe",
                @"",
                @"[conf.TargetPath]\test_execution_output.txt",
                @"");

            conf.EventCustomPostBuildExe.Add(testBuildStep);
        }
    }

    [Generate]
    public class PostBuildStamper : CommonExeProject
    {
        public PostBuildStamper()
        {
            SourceRootPath = @"[project.RootPath]\codebase\PostBuildStampTest";
        }
    }

    [Generate]
    public class PostBuildStampTest : CommonExeProject
    {
        public PostBuildStampTest()
        {
        }
        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.AddPublicDependency<PostBuildStamper>(target);

            if (FunctionalTestArguments.EnableLinkerMultiStamp)
            {
                conf.PostBuildStampExes = new List<Configuration.BuildStepExecutable>
                {
                    new Configuration.BuildStepExecutable(
                        @"[conf.TargetPath]\PostBuildStamper.exe",
                        @"",
                        @"[conf.TargetPath]\[conf.TargetFileName].exe",
                        @"_Stamp",
                        useStdOutAsOutput : true),

                    new Configuration.BuildStepExecutable(
                        @"[conf.TargetPath]\PostBuildStamper.exe",
                        @"",
                        @"[conf.TargetPath]\[conf.TargetFileName].exe",
                        @"_Message",
                        useStdOutAsOutput : true)
                };
            }
            else
            {
                conf.PostBuildStampExe = new Configuration.BuildStepExecutable(
                    @"[conf.TargetPath]\PostBuildStamper.exe",
                    @"",
                    @"[conf.TargetPath]\[conf.TargetFileName].exe",
                    @"_Stamp_Message",
                    useStdOutAsOutput: true);
            }
        }
    }

    [Generate]
    public class SimpleLib : CommonProject
    {
        public SimpleLib()
        {
        }
    }

    [Generate]
    public class SimpleExeWithLib : CommonExeProject
    {
        public SimpleExeWithLib()
        {
        }
        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPublicDependency<SimpleLib>(target);
        }
    }

    [Generate]
    public class AllCppWithDotCExe : CommonExeProject
    {
        public AllCppWithDotCExe()
        {
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.SourceFilesCompileAsCPPRegex.Add(".*\\.c");
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
            conf.AddProject<PostBuildCopySingleFileTest>(target);
            conf.AddProject<PostBuildCopyDirNoPatternTest>(target);
            conf.AddProject<PostBuildCopyDirTest>(target);
            conf.AddProject<PostBuildExecuteTest>(target);
            conf.AddProject<PostBuildTestExecution>(target);
            conf.AddProject<PostBuildStampTest>(target);
            conf.AddProject<ExplicitlyOrderedPostBuildTest>(target);
            conf.AddProject<SimpleExeWithLib>(target);
            conf.AddProject<AllCppWithDotCExe>(target);

            if (target.Blob == Blob.FastBuildUnitys)
            {
                conf.AddProject<SpanMultipleSrcDirsFBUnityInclude>(target);
                conf.AddProject<SpanMultipleSrcDirsFBUnityExclude>(target);
                conf.AddProject<SpanMultipleSrcDirsFBUnityIsolate>(target);
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
            CommandLine.ExecuteOnType(typeof(FunctionalTestArguments));

            FileInfo fileInfo = Util.GetCurrentSharpmakeFileInfo();
            string sharpmakeRootDirectory = Util.SimplifyPath(Path.Combine(fileInfo.DirectoryName, "..", ".."));

            FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeRootDirectory, @"tools\FastBuild\Windows-x64\FBuild.exe");
            FastBuildSettings.FastBuildWait = true;
            FastBuildSettings.WriteAllConfigsSection = true;

            // This is just to insure that we are able to generate some custom property section when referenced from a Compiler section
            FastBuildSettings.AdditionalPropertyGroups.Add("function TestCustomProperties()", new List<string> { "Print('Hello Custom Property')", "Print('Hello Custom Property2')" });
            FastBuildSettings.AdditionalCompilerPropertyGroups.Add("Compiler-x64-vs2022", "function TestCustomProperties()");
            FastBuildSettings.AdditionalCompilerSettings.Add("Compiler-x64-vs2022", new List<string> { "TestCustomProperties()" });

            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);

            Bff.UnityResolver = new Bff.FragmentUnityResolver();

            arguments.Generate<FastBuildFunctionalTestSolution>();
        }
    }
}
