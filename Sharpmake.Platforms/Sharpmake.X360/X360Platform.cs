// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System.Collections.Generic;
using Sharpmake.Generators;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public static partial class X360
    {
        [PlatformImplementation(Platform.x360,
            typeof(IPlatformDescriptor),
            typeof(IPlatformVcxproj))]
        public sealed partial class X360Platform : BaseMicrosoftPlatform
        {
            #region IPlatformDescriptor
            public override string SimplePlatformString => "Xbox 360";
            public override bool HasDotNetSupport => false;
            #endregion

            #region IPlatformVcxproj implementation
            public override string PackageFileExtension => "xex";
            public override bool IsPcPlatform => false;

            public override IEnumerable<string> GetImplicitlyDefinedSymbols(IGenerationContext context)
            {
                yield return "_XBOX";
            }

            public override IEnumerable<string> GetLibraryPaths(IGenerationContext context)
            {
                yield return @"$(Xbox360InstallDir)lib\xbox";
            }

            public override void SetupPlatformToolsetOptions(IGenerationContext context)
            {
                context.Options["PlatformToolset"] = FileGeneratorUtilities.RemoveLineTag;
            }

            public override void SelectCompilerOptions(IGenerationContext context)
            {
                var options = context.Options;
                var conf = context.Configuration;

                //X360Options.Compiler.CallAttributedProfiling
                //    disable                               CallAttributedProfiling="0"
                //    fastcap                               CallAttributedProfiling="1"                           /fastcap
                //    callcap                               CallAttributedProfiling="2"                           /callcap
                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.CallAttributedProfiling.Disable, () => { options["X360CallAttributedProfiling"] = "Disabled"; }),
                Sharpmake.Options.Option(Options.Compiler.CallAttributedProfiling.Fastcap, () => { options["X360CallAttributedProfiling"] = "Fastcap"; }),
                Sharpmake.Options.Option(Options.Compiler.CallAttributedProfiling.Callcap, () => { options["X360CallAttributedProfiling"] = "Callcap"; })
                );

                //X360Options.Compiler.PreschedulingOptimization
                //    disable                               Prescheduling="false"
                //    enable                                Prescheduling="true"                /Ou
                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.PreschedulingOptimization.Disable, () => { options["X360Prescheduling"] = "false"; }),
                Sharpmake.Options.Option(Options.Compiler.PreschedulingOptimization.Enable, () => { options["X360Prescheduling"] = "true"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.ImageConversion.PAL50Incompatible.Disable, () => { options["X360Pal50Incompatible"] = "false"; }),
                Sharpmake.Options.Option(Options.ImageConversion.PAL50Incompatible.Enable, () => { options["X360Pal50Incompatible"] = "true"; })
                );

                Options.Linker.ProjectDefaults projectDefault = Sharpmake.Options.GetObject<Options.Linker.ProjectDefaults>(conf);
                if (projectDefault == null)
                    options["X360ProjectDefaults"] = FileGeneratorUtilities.RemoveLineTag;
                else
                    options["X360ProjectDefaults"] = projectDefault.Value;

                Options.ImageConversion.AdditionalSections additionalSections = Sharpmake.Options.GetObject<Options.ImageConversion.AdditionalSections>(conf);
                if (additionalSections == null)
                    options["X360AdditionalSections"] = FileGeneratorUtilities.RemoveLineTag;
                else
                    options["X360AdditionalSections"] = additionalSections.Value;

                // X360Options.Linker.SetChecksum
                //    enable                                "true"
                //    disable                               "false"
                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.SetChecksum.Enable, () => { options["X360SetChecksum"] = "true"; }),
                Sharpmake.Options.Option(Options.Linker.SetChecksum.Disable, () => { options["X360SetChecksum"] = "false"; })
                );
            }

            public override void SelectLinkerOptions(IGenerationContext context)
            {
                base.SelectLinkerOptions(context);

                var outputType = context.Configuration.Output;

                if (
                    outputType != Project.Configuration.OutputType.Dll &&
                    outputType != Project.Configuration.OutputType.DotNetClassLibrary &&
                    outputType != Project.Configuration.OutputType.Exe &&
                    outputType != Project.Configuration.OutputType.DotNetConsoleApp &&
                    outputType != Project.Configuration.OutputType.DotNetWindowsApp)
                {
                    return;
                }

                context.Options["ImageXexOutput"] = "$(OutDir)$(TargetName).xex";

                Options.Linker.RemotePath remotePath = Sharpmake.Options.GetObject<Options.Linker.RemotePath>(context.Configuration);
                if (remotePath != null)
                    context.Options["X360RemotePath"] = remotePath.Value;

                Options.Linker.AdditionalDeploymentFolders additionalDeploymentFolders = Sharpmake.Options.GetObject<Options.Linker.AdditionalDeploymentFolders>(context.Configuration);
                if (additionalDeploymentFolders != null)
                    context.Options["AdditionalDeploymentFolders"] = additionalDeploymentFolders.Value;

                Options.Linker.LayoutFile layoutFilepath = Sharpmake.Options.GetObject<Options.Linker.LayoutFile>(context.Configuration);
                if (layoutFilepath != null)
                    context.Options["X360LayoutFile"] = layoutFilepath.Value;
                else
                    context.Options["X360LayoutFile"] = FileGeneratorUtilities.RemoveLineTag;
            }

            public override void GenerateProjectCompileVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectConfigurationsCompileTemplate);
            }

            protected override string GetProjectStaticLinkVcxprojTemplate()
            {
                return _projectConfigurationsStaticLinkTemplate;
            }

            protected override string GetProjectLinkSharedVcxprojTemplate()
            {
                return _projectConfigurationsLinkSharedTemplate;
            }

            protected override IEnumerable<string> GetIncludePathsImpl(IGenerationContext context)
            {
                var dirs = new List<string>();
                dirs.Add(@"$(Xbox360InstallDir)include\xbox");
                dirs.AddRange(base.GetIncludePathsImpl(context));

                return dirs;
            }

            #endregion
        }
    }
}
