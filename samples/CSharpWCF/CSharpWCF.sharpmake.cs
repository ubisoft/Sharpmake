// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using Sharpmake;

namespace CSharpWCF
{
    [Sharpmake.Generate]
    public class CSharpWCFProject : CSharpProject
    {
        public CSharpWCFProject()
        {
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

            ProjectTypeGuids = CSharpProjectType.Default;

            Name = "CSharpWCF";
            RootNamespace = "CSharpWCF";
            AssemblyName = "CSharpWCF";

            AddTargets(
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_7_2
                )
            );
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.Output = Configuration.OutputType.DotNetClassLibrary;
            conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
            conf.ProjectPath = @"[project.RootPath]";

            conf.ReferencesByName.Add(
                "System",
                "System.Core",
                "System.Runtime.Serialization",
                "System.ServiceModel",
                "System.Xml.Linq",
                "System.Data.DataSetExtensions",
                "Microsoft.CSharp",
                "System.Data",
                "System.Net.Http",
                "System.Xml");
        }
    }

    [Sharpmake.Generate]
    public class CSharpWCFAppProject : CSharpProject
    {
        public CSharpWCFAppProject()
        {
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

            SourceFilesExcludeRegex.Add(@".*\.vs\.*");

            ProjectTypeGuids = CSharpProjectType.Wcf;

            Name = "CSharpWCFApp";
            RootNamespace = "CSharpWCFApp";
            AssemblyName = "CSharpWCFApp";

            AddTargets(
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_7_2
                )
            );
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.Output = Configuration.OutputType.DotNetClassLibrary;
            conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
            conf.ProjectPath = @"[project.RootPath]";

            conf.ReferencesByName.Add(
                "System.Web.DynamicData",
                "Microsoft.CSharp",
                "Microsoft.CSharp",
                "System.Web.Entity",
                "System.Web.ApplicationServices",
                "System",
                "System.Configuration",
                "System.Core",
                "System.Data",
                "System.Drawing",
                "System.EnterpriseServices",
                "System.Runtime.Serialization",
                "System.ServiceModel",
                "System.ServiceModel.Web",
                "System.Web",
                "System.Web.Extensions",
                "System.Web.Services",
                "System.Xml",
                "System.Xml.Linq");
        }
    }

    [Sharpmake.Generate]
    public class CSharpWCFSolution : CSharpSolution
    {
        public CSharpWCFSolution()
        {
            AddTargets(new Target(
            Platform.anycpu,
            DevEnv.vs2022,
            Optimization.Debug | Optimization.Release,
            OutputType.Dll,
            Blob.NoBlob,
            BuildSystem.MSBuild,
            DotNetFramework.v4_7_2));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = string.Format("{0}.{1}.{2}",
                                                  Name,
                                                  "[target.DevEnv]",
                                                  "[target.Framework]");
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

            conf.AddProject<CSharpWCFProject>(target);
            conf.AddProject<CSharpWCFAppProject>(target);
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<CSharpWCFSolution>();
        }
    }
}
