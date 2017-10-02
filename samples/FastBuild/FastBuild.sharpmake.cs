using Sharpmake;

namespace Fastbuild
{
    public static class Common
    {
        public static ITarget[] GetTargets()
        {
            return new ITarget[] {
                new Target(
                    Platform.win64,
                    DevEnv.vs2015,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Lib,
                    Blob.NoBlob,
                    BuildSystem.MSBuild),
                new Target(
                    Platform.win64,
                    DevEnv.vs2015,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Lib,
                    Blob.FastBuildUnitys,
                    BuildSystem.FastBuild)
            };
        }

        [Main]
        public static void SharpmakeMain(Arguments args)
        {
            FastBuildSettings.FastBuildMakeCommand = @"FBuild.exe";
            args.Generate<FastBuildSolution>();
        }
    }

    [Generate]
    public class FastBuildProject : Project
    {
        public FastBuildProject()
        {
            Name = "FastBuild";
            SourceRootPath = @"[project.SharpmakeCsPath]/codebase";
            AddTargets(Common.GetTargets());
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.Name = @"[target.BuildSystem]_[target.Optimization]";
            conf.Output = Configuration.OutputType.Exe;
            conf.ProjectPath = @"[project.SharpmakeCsPath]/projects";
            conf.PrecompHeader = "precomp.h";
            conf.PrecompSource = "precomp.cpp";
            conf.Options.Add(Options.Vc.Compiler.Exceptions.Enable);
        }

        [Configure(BuildSystem.FastBuild)]
        public void ConfigureFastBuild(Configuration conf, Target target)
        {
            conf.IsMainProject = true;
            conf.IsFastBuild = true;
            conf.BlobPath = @"[project.SharpmakeCsPath]/projects.blob";
            conf.Options.Add(Options.Vc.Linker.EmbedManifest.No);
            conf.FastBuildUnityPath = @"[project.SharpmakeCsPath]/projects/unity";
            conf.FastBuildBlobbed = true;
            conf.FastBuildUnityCount = 3;
            conf.AdditionalCompilerOptions.Add("/FS");
        }
    }

    [Generate]
    public class FastBuildSolution : Solution
    {
        public FastBuildSolution()
        {
            Name = "FastBuild";
            AddTargets(Common.GetTargets());
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = @"[solution.Name]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]/projects";
            conf.Name = @"[target.BuildSystem]_[target.Optimization]";
            conf.AddProject<FastBuildProject>(target);
        }
    }
}
