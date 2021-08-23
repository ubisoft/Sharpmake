using System.IO;
using Sharpmake;

namespace RiderJson
{
    public class BaseProject : Project
    {
        public BaseProject()
        {
            AddTargets(new Target(
                           Platform.win64,
                           DevEnv.vs2019,
                           Optimization.Debug | Optimization.Release,
                           OutputType.Lib,
                           Blob.FastBuildUnitys,
                           BuildSystem.FastBuild | BuildSystem.MSBuild));
        }

        [Configure]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.Name = "[target.Optimization] [target.BuildSystem]";
            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]_[target.Optimization]_[target.DevEnv]";
            conf.IsFastBuild = target.BuildSystem == BuildSystem.FastBuild;
            conf.IsBlobbed = target.Blob == Blob.Blob;
            conf.FastBuildBlobbed = target.Blob == Blob.FastBuildUnitys;
            conf.AdditionalCompilerOptions.Add("/FS");
            conf.Options.Add(Options.Vc.Compiler.CppLanguageStandard.CPP17);
        }
    }

    public class LibraryBaseProject : BaseProject
    {
        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
                        
            conf.IncludePaths.Add(@"[project.SourceRootPath]\public");
            conf.IncludePrivatePaths.Add(@"[project.SourceRootPath]\private");
            conf.PrecompHeader = "precomp.hpp";
            conf.PrecompSource = "precomp.cpp";
            conf.Defines.Add("LIBRARY_COMPILE");

            conf.Output = Configuration.OutputType.Lib;
        }
    }
    
    [Generate]
    public class LibraryProject1 : LibraryBaseProject
    {
        public LibraryProject1()
        {
            Name = "Lib1_Proj";
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\library1";
        }
    }
    
    [Generate]
    public class LibraryProject2 : LibraryBaseProject
    {
        public LibraryProject2()
        {
            Name = "Lib2_Proj";
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\library2";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPublicDependency<LibraryProject1>(target);
        }
    }

    [Generate]
    public class ExecutableProject1 : BaseProject
    {
        public ExecutableProject1()
        {
            Name = "Exe1_Proj";
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\executable1";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<LibraryProject1>(target);
        }
    }
    
    [Generate]
    public class ExecutableProject2 : BaseProject
    {
        public ExecutableProject2()
        {
            Name = "Exe2_Proj";
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\executable2";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.AddPrivateDependency<LibraryProject2>(target);
        }
    }

    public class BaseSolution : Solution
    {
        public BaseSolution()
        {
            AddTargets(new Target(
                           Platform.win64,
                           DevEnv.vs2019,
                           Optimization.Debug | Optimization.Release,
                           OutputType.Lib,
                           Blob.FastBuildUnitys,
                           BuildSystem.FastBuild | BuildSystem.MSBuild));
        }

        [Configure]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.Name = @"[solution.Name] [target.Optimization] [target.BuildSystem]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
        }
    }
    
    [Generate]
    public class FirstSolution : BaseSolution
    {
        public FirstSolution()
        {
            Name = "Sol1";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.AddProject<LibraryProject1>(target);
            conf.AddProject<LibraryProject2>(target);
            conf.AddProject<ExecutableProject2>(target);
        }
    }

    [Generate]
    public class SecondSolution : BaseSolution
    {
        public SecondSolution()
        {
            Name = "Sol2";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.AddProject<ExecutableProject1>(target);
            conf.AddProject<ExecutableProject2>(target);
            conf.AddProject<LibraryProject1>(target);
            conf.AddProject<LibraryProject2>(target);
        }
    }
    
    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments args)
        {
            string relativeRootPath = @".\codebase";
            FileInfo fileInfo = Util.GetCurrentSharpmakeFileInfo();
            string rootDirectory = Path.Combine(fileInfo.DirectoryName, relativeRootPath);
            var rootDir = Util.SimplifyPath(rootDirectory);

            FastBuildSettings.SetPathToResourceCompilerInEnvironment = true;

            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2019, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);
            string sharpmakeFastBuildDir = Util.PathGetAbsolute(rootDir, @"..\..\..\tools\FastBuild");
            FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeFastBuildDir, "Windows-x64", "FBuild.exe");

            args.Builder.EventPostGeneration += Sharpmake.Generators.Generic.RiderJson.PostGenerationCallback;
            Sharpmake.Generators.Generic.RiderJson.SolutionName = "Sol1";
            Sharpmake.Generators.Generic.RiderJson.RiderFolderPath = "projects";
            Sharpmake.Generators.Generic.RiderJson.SolutionFilePath = "projects";

            args.Generate<FirstSolution>();
            args.Generate<SecondSolution>();
        }
    }
}
