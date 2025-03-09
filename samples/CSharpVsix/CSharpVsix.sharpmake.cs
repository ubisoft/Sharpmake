// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using Sharpmake;

namespace CSharpVsix
{
    public class Common
    {
        public static Target[] GetDefaultTargets()
        {
            return new Target[]{
                new Target(
                    Platform.anycpu,
                    DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release,
                    OutputType.Dll,
                    Blob.NoBlob,
                    BuildSystem.MSBuild,
                    DotNetFramework.v4_7_2
                )
            };
        }
    }

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
            VSIXProjectVersion = 3;

            ResourcesPath = @"[project.SourceRootPath]\Resources";

            IncludeResxAsResources = false;

            AdditionalContent.Add(@"[project.SourceRootPath]\Resources\\HelloWorldCommandPackage.ico");
            AdditionalContent.Add(@"[project.SourceRootPath]\Resources\\HelloWorldCommand.png");

            AdditionalEmbeddedResource.Add(@"[project.SourceRootPath]\VSPackage.resx");

            AdditionalNone.Add(@"[project.SourceRootPath]\source.extension.vsixmanifest");
            AdditionalNone.Add(@"[project.SourceRootPath]\Key.snk");

            AssemblyName = "CSharpVsix";

            AddTargets(Common.GetDefaultTargets());
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.Output = Configuration.OutputType.DotNetClassLibrary;

            conf.ReferencesByNuGetPackage.Add(
                "Microsoft.VisualStudio.SDK", "17.5.33428.388"
            );

            conf.ReferencesByName.Add(
                "System.Design"
            );

            conf.ProjectFileName = "[project.Name].[target.DevEnv].[target.Framework]";
            conf.ProjectPath = @"[project.RootPath]";

            conf.Options.Add(Options.CSharp.AutoGenerateBindingRedirects.Enabled);
            conf.Options.Add(Options.CSharp.SignAssembly.Enabled);
            conf.Options.Add(new Options.CSharp.AssemblyOriginatorKeyFile("Key.snk"));
            conf.Options.Add(Options.CSharp.BootstrapperEnabled.Enabled);
            conf.Options.Add(Options.CSharp.CreateVsixContainer.Enabled);
            conf.Options.Add(Options.CSharp.UseCodeBase.Enabled);

            // minimum is vs2017
            conf.Options.Add(new Options.CSharp.MinimumVisualStudioVersion("17.0"));
            conf.Options.Add(new Options.CSharp.OldToolsVersion("17.0"));

            conf.Options.Add(new Options.CSharp.VsToolsPath(@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)"));

            conf.CsprojUserFile = new Configuration.CsprojUserFileSettings
            {
                StartAction = Configuration.CsprojUserFileSettings.StartActionSetting.Program,
                StartArguments = "/rootSuffix Exp",
                StartProgram = "$(DevEnvDir)devenv.exe"
            };

            // !IMPORTANT! Comment out the below line to allow nicer experience when working from VS
            // Ideally, sharpmake should support adding the condition $(BuildingInsideVisualStudio)' != 'true' on the tag
            conf.Options.Add(Options.CSharp.DeployExtension.Disabled);
        }
    }

    [Sharpmake.Generate]
    public class CSharpVsixSolution : CSharpSolution
    {
        public CSharpVsixSolution()
        {
            AddTargets(Common.GetDefaultTargets());
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = string.Format("{0}.{1}.{2}",
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
