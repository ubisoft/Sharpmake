// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using Sharpmake;
namespace NetCore
{
    namespace DotNetMultiFrameworksHelloWorld
    {
        [Generate]
        public class HelloWorldLib : CSharpProject
        {
            internal static ITarget[] SampleTargets = {
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_7_2 | DotNetFramework.v4_8 | DotNetFramework.netstandard2_0 | DotNetFramework.net5_0)
            };

            public HelloWorldLib()
            {
                ClearTargets();
                AddTargets(SampleTargets);

                RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

                // This Path will be used to get all SourceFiles in this Folder and all subFolders
                SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            }

            [Configure]
            public virtual void ConfigureAll(Configuration conf, ITarget target)
            {
                conf.ProjectFileName = "[project.Name].[target.DevEnv]";
                conf.ProjectPath = @"[project.RootPath]";
                conf.Output = Configuration.OutputType.DotNetClassLibrary;

                conf.Options.Add(Sharpmake.Options.CSharp.TreatWarningsAsErrors.Enabled);

                if (target.GetFragment<DotNetFramework>().IsDotNetFramework())
                {
                    conf.ReferencesByName.Add("System");
                }

                if (target.GetFragment<DotNetFramework>().IsDotNetStandard())
                {
                    conf.ReferencesByNuGetPackage.Add("System.Text.Encoding.CodePages", "4.5.0");
                }

                if (target.GetFragment<DotNetFramework>().IsDotNetCore())
                {
                    conf.DotNetOSVersion = DotNetOS.windows;
                }
            }
        }

        [Sharpmake.Generate]
        public class HelloWorldMultiFrameworks : CSharpProject
        {
            internal static ITarget[] SampleTargets = {
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2019,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_7_2 | DotNetFramework.v4_8 | DotNetFramework.netcore3_1)
            };

            public HelloWorldMultiFrameworks()
            {
                ClearTargets();
                AddTargets(SampleTargets);

                RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

                // This Path will be used to get all SourceFiles in this Folder and all subFolders
                SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            }

            [Configure]
            public virtual void ConfigureAll(Configuration conf, ITarget target)
            {
                conf.ProjectFileName = "[project.Name].[target.DevEnv]";
                conf.ProjectPath = @"[project.RootPath]";

                conf.Options.Add(Sharpmake.Options.CSharp.TreatWarningsAsErrors.Enabled);

                conf.AddPrivateDependency<HelloWorldLib>(GetLibraryTargetFromApplicationTarget(target));
            }

            private static ITarget GetLibraryTargetFromApplicationTarget(ITarget target)
            {
                if (!target.GetFragment<DotNetFramework>().IsDotNetCore())
                    return target;
                // if target is a .net core application, libraries are .net standard
                return target.Clone(DotNetFramework.netstandard2_0);
            }
        }

        [Generate]
        public class HelloWorldMultiFrameworksSolution : CSharpSolution
        {
            public HelloWorldMultiFrameworksSolution()
            {
                AddTargets(HelloWorldMultiFrameworks.SampleTargets);
            }

            [Configure]
            public void ConfigureAll(Configuration conf, ITarget target)
            {
                conf.SolutionFileName = string.Format("{0}.{1}",
                                                      Name,
                                                      "[target.DevEnv]");
                conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

                conf.AddProject<HelloWorldMultiFrameworks>(target);
            }
        }

        public static class Main
        {
            [Sharpmake.Main]
            public static void SharpmakeMain(Arguments arguments)
            {
                arguments.Generate<HelloWorldMultiFrameworksSolution>();
            }
        }
    }
}
