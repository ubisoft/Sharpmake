using System.IO;
using Sharpmake;

namespace CPPForcePackageReference
{
    [Generate]
    public class CPPForcePackageReference : Project
    {
        public CPPForcePackageReference()
        {
            Name = "CPPForcePackageReference";

            AddTargets(new Target(
                    Platform.win64,
                    DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release
            ));

            SourceRootPath     = @"[project.SharpmakeCsPath]\codebase";
            NuGetReferenceType = NuGetPackageMode.PackageReference; // explicitly specify PackageReference for this cpp project
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.Options.Add(Options.Vc.Compiler.Exceptions.Enable);

            // copy Directory.Build.props that forces the Nuget PackageReference feature for cpp project to be enabled
            string directoryBuildPropsName = "Directory.Build.props";
            DirectoryInfo srcPath  = new DirectoryInfo(@$"{SharpmakeCsPath}\codebase\"); // SourceRootPath + ..
            DirectoryInfo destPath = new DirectoryInfo(@$"{SharpmakeCsPath}\projects\"); // conf.ProjectPath + ..
            if (!destPath.Exists)
            {
                destPath.Create();
            }
            Util.ForceCopy(Path.Combine(srcPath.FullName, directoryBuildPropsName), Path.Combine(destPath.FullName, directoryBuildPropsName));

            // cpp source code uses nlohmann.json, we specify another nuget package that depends on nlohmann.json
            // to test if nuget could correctly restore the dependency
            conf.ReferencesByNuGetPackage.Add("SiddiqSoft.sip2json", "1.17.3");
        }
    }

    [Generate]
    public class CPPForcePackageReferenceSolution : Sharpmake.Solution
    {
        public CPPForcePackageReferenceSolution()
        {
            Name = "CPPForcePackageReference";

            AddTargets(new Target(
                    Platform.win64,
                    DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release
            ));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<CPPForcePackageReference>(target);

        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);
            arguments.Generate<CPPForcePackageReferenceSolution>();
        }
    }
}
