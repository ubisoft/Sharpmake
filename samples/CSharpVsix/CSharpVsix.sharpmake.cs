// Copyright (c) 2017 Ubisoft Entertainment
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

namespace CSharpVsix
{
    [Sharpmake.Generate]
    public class CSharpVsixProject : CSharpProject
    {
        public CSharpVsixProject()
        {
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            RootPath = @"[project.SharpmakeCsPath]\projects\[project.Name]";

            ProjectTypeGuids = CSharpProjectType.Vsix;

            Name = "CSharpVsix";
            RootNamespace = "CSharpVsix";

            VsctCompileFiles.Add(@"[project.SourceRootPath]\HelloWorldCommandPackage.vsct");

            ResourcesPath = @"[project.SourceRootPath]\Resources";

            IncludeResxAsResources = false;

            AdditionalContent.Add(@"[project.SourceRootPath]\Resources\\HelloWorldCommandPackage.ico");
            AdditionalContent.Add(@"[project.SourceRootPath]\Resources\\HelloWorldCommand.png");

            AdditionalEmbeddedResource.Add(@"[project.SourceRootPath]\VSPackage.resx");

            AdditionalNone.Add(@"[project.SourceRootPath]\source.extension.vsixmanifest");
            AdditionalNone.Add(@"[project.SourceRootPath]\Key.snk");
            AdditionalNone.Add(@"[project.SourceRootPath]\packages.config");

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

            // This Path will be used to get all SourceFiles in this Folder and all subFolders

            AssemblyName = "CSharpVsix";
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.Output = Configuration.OutputType.DotNetClassLibrary;
            conf.ReferencesByName.Add(
                "EnvDTE100",
                "EnvDTE90",
                "Extensibility",
                "stdole",                               //Interops
                "Microsoft.VisualStudio.CommandBars",
                "EnvDTE",
                "EnvDTE80",
                "Microsoft.CSharp",
                "Microsoft.VisualStudio.OLE.Interop",
                "Microsoft.VisualStudio.Shell.11.0",
                "Microsoft.VisualStudio.Shell.Immutable.10.0",
                "Microsoft.VisualStudio.Shell.Immutable.11.0",
                "Microsoft.VisualStudio.Shell.Interop",
                "Microsoft.VisualStudio.Shell.Interop.10.0",
                "Microsoft.VisualStudio.Shell.Interop.11.0",
                "Microsoft.VisualStudio.Shell.Interop.8.0",
                "Microsoft.VisualStudio.Shell.Interop.9.0",
                "PresentationCore",
                "PresentationFramework",
                "System",
                "System.Core",
                "System.Drawing",
                "System.Design",
                "System.Windows.Forms",
                "System.Xaml",
                "System.Xml",
                "WindowsBase",
                "Microsoft.VisualStudio.Shell.Immutable.12.0",
                "Microsoft.VisualStudio.Shell.Interop.12.0"
            );
            conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
            conf.ProjectPath = @"[project.RootPath]";

            conf.Options.Add(new Options.CSharp.MinimumVisualStudioVersion("14.0"));
            conf.Options.Add(Options.CSharp.SignAssembly.Enabled);
            conf.Options.Add(new Options.CSharp.AssemblyOriginatorKeyFile("Key.snk"));
            conf.Options.Add(Options.CSharp.BootstrapperEnabled.Enabled);
            conf.Options.Add(Options.CSharp.CreateVsixContainer.Enabled);
            conf.Options.Add(Options.CSharp.DeployExtension.Disabled);
            conf.Options.Add(Options.CSharp.UseCodeBase.Enabled);
            conf.Options.Add(new Options.CSharp.VsToolsPath(@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)"));
        }
    }

    [Sharpmake.Generate]
    public class CSharpVsixSolution : CSharpSolution
    {
        public CSharpVsixSolution()
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
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects\";

            conf.AddProject<CSharpVsixProject>(target);
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<CSharpVsixSolution>();
        }
    }
}
