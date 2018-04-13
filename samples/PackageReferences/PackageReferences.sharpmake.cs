using Sharpmake;
using System;

namespace CSharpPackageReference
{
    [Generate]
    public class PackageReferences : CSharpProject
    {
        public PackageReferences()
        {
            AddTargets(new Target(
            Platform.anycpu,
            DevEnv.vs2015 | DevEnv.vs2017,
            Optimization.Debug | Optimization.Release,
            OutputType.Dll,
            Blob.NoBlob,
            BuildSystem.MSBuild,
            DotNetFramework.v4_5 | DotNetFramework.v4_6_2));

            RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

            // This Path will be used to get all SourceFiles in this Folder and all subFolders
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            AssemblyName = "PackageReference";
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
            conf.ProjectPath = @"[project.RootPath]";

            conf.Options.Add(Options.CSharp.TreatWarningsAsErrors.Enabled);

            conf.ReferencesByNuGetPackage.Add("NUnit", "3.6.0");
            conf.ReferencesByNuGetPackage.Add("Newtonsoft.Json", "9.0.1");
            conf.ReferencesByNuGetPackage.Add("Mono.Cecil", "0.9.6.4", privateAssets: Sharpmake.PackageReferences.AssetsDependency.All);
            conf.ReferencesByNuGetPackage.Add("MySql.Data", "6.10.6", privateAssets: Sharpmake.PackageReferences.AssetsDependency.Build | Sharpmake.PackageReferences.AssetsDependency.Compile);
        }
    }

    [Generate]
    public class PackageReferenceSolution : CSharpSolution
    {
        public PackageReferenceSolution()
        {
            AddTargets(new Target(
            Platform.anycpu,
            DevEnv.vs2015 | DevEnv.vs2017,
            Optimization.Debug | Optimization.Release,
            OutputType.Dll,
            Blob.NoBlob,
            BuildSystem.MSBuild,
            DotNetFramework.v4_5 | DotNetFramework.v4_6_2));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = String.Format("{0}.{1}.{2}",
                                                  Name,
                                                  "[target.DevEnv]",
                                                  "[target.Framework]");
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

            conf.AddProject<PackageReferences>(target);
        }

        [Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            arguments.Generate<PackageReferenceSolution>();
        }
    }
}
