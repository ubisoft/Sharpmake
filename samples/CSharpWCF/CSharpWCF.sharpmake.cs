// Copyright (c) 2018-2019 Ubisoft Entertainment
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

using Sharpmake;
using System;
using System.IO;

namespace CSharpWCF
{
    [Sharpmake.Generate]
    public class CSharpWCFProject : CSharpProject
    {
        public CSharpWCFProject()
        {
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            RootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";

            ProjectTypeGuids = CSharpProjectType.Default;

            Name = "CSharpWCF";
            RootNamespace = "CSharpWCF";
            AssemblyName = "CSharpWCF";

            AddTargets(
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2015,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_5
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
            RootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";

            SourceFilesExcludeRegex.Add(@".*\.vs\.*");

            ProjectTypeGuids = CSharpProjectType.Wcf;

            Name = "CSharpWCFApp";
            RootNamespace = "CSharpWCFApp";
            AssemblyName = "CSharpWCFApp";

            AddTargets(
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2015,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_5
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
            DevEnv.vs2015,
            Optimization.Debug | Optimization.Release,
            OutputType.Dll,
            Blob.NoBlob,
            BuildSystem.MSBuild,
            DotNetFramework.v4_5));
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = String.Format("{0}.{1}.{2}",
                                                  Name,
                                                  "[target.DevEnv]",
                                                  "[target.Framework]");
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\codebase\";

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
