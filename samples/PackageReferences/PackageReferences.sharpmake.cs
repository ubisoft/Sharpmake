// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using Sharpmake;
using static Sharpmake.PackageReferences;

namespace PackageReference
{
    [Generate]
    public class CSharpPackageReferences : CSharpProject
    {
        public CSharpPackageReferences()
        {
            AddTargets(
                new Target(
                    Platform.win64,
                    DevEnv.vs2022 | DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_7_2
                )
            );

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

            conf.TargetPath = @"[conf.ProjectPath]\output\[target.DevEnv]\[conf.Name]";

            conf.Options.Add(Options.CSharp.TreatWarningsAsErrors.Enabled);

            // Avoid NuGet security vulnerabilities audit warnings breaks the build.
            // .NET Framework 4.x has known vulnerabilities.
            conf.Options.Add(new Options.CSharp.WarningsNotAsErrors("NU1901", "NU1902", "NU1903", "NU1904"));

            conf.ReferencesByNuGetPackage.Add("NUnit", "3.6.0");
            conf.ReferencesByNuGetPackage.Add("Newtonsoft.Json", "13.0.1");
            conf.ReferencesByNuGetPackage.Add("Mono.Cecil", "0.9.6.4", privateAssets: AssetsDependency.All);
            conf.ReferencesByNuGetPackage.Add("MySql.Data", "6.10.6", privateAssets: AssetsDependency.Build | AssetsDependency.Compile);

            conf.ReferencesByNuGetPackage.Add("Microsoft.Extensions.Configuration.Abstractions", "9.0.5", dotNetHint: null, includeAssets: AssetsDependency.All, excludeAssets: AssetsDependency.None, privateAssets: AssetsDependency.ContentFiles | AssetsDependency.Analyzers | AssetsDependency.Build);
            conf.ReferencesByNuGetPackage.Add("Microsoft.Extensions.Configuration", "9.0.5", dotNetHint: null, includeAssets: AssetsDependency.Analyzers, excludeAssets: AssetsDependency.None, privateAssets: AssetsDependency.ContentFiles | AssetsDependency.Analyzers | AssetsDependency.Build);
            conf.ReferencesByNuGetPackage.Add("Microsoft.Extensions.Configuration.Binder", "9.0.5", dotNetHint: null, includeAssets: AssetsDependency.All, excludeAssets: AssetsDependency.Compile, privateAssets: AssetsDependency.ContentFiles | AssetsDependency.Analyzers | AssetsDependency.Build);
            conf.ReferencesByNuGetPackage.Add("Microsoft.Extensions.Configuration.FileExtensions ", "9.0.5", dotNetHint: null, includeAssets: AssetsDependency.All, excludeAssets: AssetsDependency.None, privateAssets: AssetsDependency.All);
            conf.ReferencesByNuGetPackage.Add("Microsoft.Extensions.Configuration.Json", "9.0.5", dotNetHint: null, includeAssets: AssetsDependency.Analyzers, excludeAssets: AssetsDependency.Compile, privateAssets: AssetsDependency.ContentFiles | AssetsDependency.Analyzers | AssetsDependency.Build);
            conf.ReferencesByNuGetPackage.Add("Microsoft.Extensions.Configuration.EnvironmentVariables", "9.0.5", dotNetHint: null, includeAssets: AssetsDependency.All, excludeAssets: AssetsDependency.Compile, privateAssets: AssetsDependency.All);
            conf.ReferencesByNuGetPackage.Add("Microsoft.Extensions.Configuration.UserSecrets", "9.0.5", dotNetHint: null, includeAssets: AssetsDependency.Analyzers, excludeAssets: AssetsDependency.None, privateAssets: AssetsDependency.All);
            conf.ReferencesByNuGetPackage.Add("Microsoft.Extensions.Configuration.CommandLine", "9.0.5", dotNetHint: null, includeAssets: AssetsDependency.Analyzers, excludeAssets: AssetsDependency.Compile, privateAssets: AssetsDependency.All);
        }
    }

    [Generate]
    public class CPPPackageReferences : Project
    {
        public CPPPackageReferences()
        {
            AddTargets(
                new Target(
                    Platform.win64,
                    DevEnv.vs2022 | DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_7_2
                )
            );

            RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

            // This Path will be used to get all SourceFiles in this Folder and all subFolders
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
            conf.ProjectPath = @"[project.RootPath]";

            conf.TargetPath = @"[conf.ProjectPath]\output\[target.DevEnv]\[conf.Name]";

            conf.Options.Add(Options.Vc.Compiler.Exceptions.Enable);

            conf.ReferencesByNuGetPackage.Add("gtest-vc140-static-64", "1.1.0");
        }
    }

    [Generate]
    public class PackageReferenceSolution : CSharpSolution
    {
        public PackageReferenceSolution()
        {
            AddTargets(
                new Target(
                    Platform.win64,
                    DevEnv.vs2022 | DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_7_2
                )
            );
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = string.Format("{0}.{1}.{2}",
                                                  Name,
                                                  "[target.DevEnv]",
                                                  "[target.Framework]");
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

            conf.AddProject<CSharpPackageReferences>(target);
            conf.AddProject<CPPPackageReferences>(target);
        }

        [Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_22621_0);
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2019, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);
            arguments.Generate<PackageReferenceSolution>();
        }
    }
}
