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
    public static partial class NvShield
    {
        [PlatformImplementation(Platform.nvshield,
            typeof(IPlatformDescriptor),
            typeof(Project.Configuration.IConfigurationTasks),
            typeof(IPlatformVcxproj))]
        public sealed partial class NvShieldPlatform : BasePlatform, Project.Configuration.IConfigurationTasks
        {
            #region IPlatformDescriptor implementation
            public override string SimplePlatformString => "Tegra-Android";
            public override bool IsMicrosoftPlatform => false;
            public override bool IsPcPlatform => false;
            public override bool IsUsingClang => true;
            public override bool HasDotNetSupport => false;
            public override bool HasSharedLibrarySupport => false;
            #endregion

            #region Project.Configuration.IConfigurationTasks
            public string GetDefaultOutputExtension(Project.Configuration.OutputType outputType)
            {
                switch (outputType)
                {
                    case Project.Configuration.OutputType.Exe:
                        return "so";
                    case Project.Configuration.OutputType.Dll:
                        return "so";
                    default:
                        return "a";
                }
            }
            public void SetupDynamicLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
            {
                DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
            }

            public void SetupStaticLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
            {
                DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
            }

            public IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
            {
                yield break;
            }
            #endregion

            #region IPlatformVcxproj implementation
            public override string ExecutableFileExtension => "so";
            public override string SharedLibraryFileExtension => "so";
            public override string ProgramDatabaseFileExtension => "so";
            public override string StaticLibraryFileExtension => string.Empty;
            public override string StaticOutputLibraryFileExtension => string.Empty;

            public override string GetOutputFileNamePrefix(IGenerationContext context, Project.Configuration.OutputType outputType)
            {
                return "lib";
            }

            public override void SelectCompilerOptions(IGenerationContext context)
            {
                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.GenerateDebugInformation.Enable, () => { options["GenerateDebugInformation"] = "true"; cmdLineOptions["CLangGenerateDebugInformation"] = "-g"; }),
                Sharpmake.Options.Option(Options.Compiler.GenerateDebugInformation.Disable, () => { options["GenerateDebugInformation"] = "false"; cmdLineOptions["CLangGenerateDebugInformation"] = ""; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.Warnings.NormalWarnings, () => { options["Warnings"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["Warnings"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Compiler.Warnings.AllWarnings, () => { options["Warnings"] = "AllWarnings"; cmdLineOptions["Warnings"] = "-Wall"; }),
                Sharpmake.Options.Option(Options.Compiler.Warnings.Disable, () => { options["Warnings"] = "DisableAllWarnings"; cmdLineOptions["Warnings"] = "-w"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Sharpmake.Options.Vc.General.TreatWarningsAsErrors.Enable, () => { options["WarningsAsErrors"] = "true"; cmdLineOptions["WarningsAsErrors"] = "-Werror"; }),
                Sharpmake.Options.Option(Sharpmake.Options.Vc.General.TreatWarningsAsErrors.Disable, () => { options["WarningsAsErrors"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["WarningsAsErrors"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.EchoCommandLines.Enable, () => { options["EchoCommandLinesCompiler"] = "true"; }),
                Sharpmake.Options.Option(Options.Compiler.EchoCommandLines.Disable, () => { options["EchoCommandLinesCompiler"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.EchoIncludedHeaders.Enable, () => { options["EchoIncludedHeaders"] = "true"; cmdLineOptions["EchoIncludedHeaders"] = "-H"; }),
                Sharpmake.Options.Option(Options.Compiler.EchoIncludedHeaders.Disable, () => { options["EchoIncludedHeaders"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["EchoIncludedHeaders"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                Options.Compiler.ProcessorNumber processorNumber = Sharpmake.Options.GetObject<Options.Compiler.ProcessorNumber>(conf);
                if (processorNumber == null)
                    options["ProcessorNumber"] = FileGeneratorUtilities.RemoveLineTag;
                else
                    options["ProcessorNumber"] = processorNumber.Value.ToString();

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.OptimizationLevel.Disable, () => { options["OptimizationLevel"] = "O0"; cmdLineOptions["OptimizationLevel"] = "-O0"; }),
                Sharpmake.Options.Option(Options.Compiler.OptimizationLevel.Standard, () => { options["OptimizationLevel"] = "O1"; cmdLineOptions["OptimizationLevel"] = "-O1"; }),
                Sharpmake.Options.Option(Options.Compiler.OptimizationLevel.Full, () => { options["OptimizationLevel"] = "O2"; cmdLineOptions["OptimizationLevel"] = "-O2"; }),
                Sharpmake.Options.Option(Options.Compiler.OptimizationLevel.FullWithInlining, () => { options["OptimizationLevel"] = "O3"; cmdLineOptions["OptimizationLevel"] = "-O3"; }),
                Sharpmake.Options.Option(Options.Compiler.OptimizationLevel.ForSize, () => { options["OptimizationLevel"] = "Os"; cmdLineOptions["OptimizationLevel"] = "-Os"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.StrictAliasing.Enable, () => { options["StrictAliasing"] = "true"; cmdLineOptions["StrictAliasing"] = "-fstrict-aliasing"; }),
                Sharpmake.Options.Option(Options.Compiler.StrictAliasing.Disable, () => { options["StrictAliasing"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["StrictAliasing"] = "-fno-strict-aliasing"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.UnswitchLoops.Enable, () => { options["UnswitchLoops"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["UnswitchLoops"] = "-funswitch-loops"; }),
                Sharpmake.Options.Option(Options.Compiler.UnswitchLoops.Disable, () => { options["UnswitchLoops"] = "false"; cmdLineOptions["UnswitchLoops"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                Options.Compiler.InlineLimit inlineLimit = Sharpmake.Options.GetObject<Options.Compiler.InlineLimit>(conf);
                if (inlineLimit == null || inlineLimit.Value == 100)
                    options["InlineLimit"] = FileGeneratorUtilities.RemoveLineTag;
                else
                    options["InlineLimit"] = inlineLimit.Value.ToString();

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.OmitFramePointers.Enable, () => { options["OmitFramePointers"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["OmitFramePointers"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Compiler.OmitFramePointers.Disable, () => { options["OmitFramePointers"] = "false"; cmdLineOptions["OmitFramePointers"] = "-fno-omit-frame-pointer"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.FunctionSections.Enable, () => { options["FunctionSections"] = "true"; cmdLineOptions["FunctionSections"] = "-ffunction-sections"; }),
                Sharpmake.Options.Option(Options.Compiler.FunctionSections.Disable, () => { options["FunctionSections"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["FunctionSections"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.ThumbMode.Enable, () => { options["ThumbMode"] = "true"; cmdLineOptions["ThumbMode"] = "-mthumb"; }),
                Sharpmake.Options.Option(Options.Compiler.ThumbMode.Disable, () => { options["ThumbMode"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["ThumbMode"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.FloatingPointABI.Hard, () => { options["FloatAbi"] = "hard"; cmdLineOptions["FloatAbi"] = "-mfloat-abi=hard"; }),
                Sharpmake.Options.Option(Options.Compiler.FloatingPointABI.Soft, () => { options["FloatAbi"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["FloatAbi"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.GeneratePositionIndependentCode.Enable, () => { options["PositionIndependentCode"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["PositionIndependentCode"] = "-fpic"; }),
                Sharpmake.Options.Option(Options.Compiler.GeneratePositionIndependentCode.Disable, () => { options["PositionIndependentCode"] = "false"; cmdLineOptions["PositionIndependentCode"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.StackProtection.Enable, () => { options["StackProtector"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["StackProtector"] = "-fstack-protector"; }),
                Sharpmake.Options.Option(Options.Compiler.StackProtection.Disable, () => { options["StackProtector"] = "false"; cmdLineOptions["StackProtector"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.EnableAdvancedSIMD.Enable, () => { options["FpuNeon"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["FpuNeon"] = "-mfpu=neon"; }),
                Sharpmake.Options.Option(Options.Compiler.EnableAdvancedSIMD.Disable, () => { options["FpuNeon"] = "false"; cmdLineOptions["FpuNeon"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.Exceptions.Enable, () => { options["GccExceptionHandling"] = "true"; cmdLineOptions["GccExceptionHandling"] = "-fexceptions"; }),
                Sharpmake.Options.Option(Options.Compiler.Exceptions.Disable, () => { options["GccExceptionHandling"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["GccExceptionHandling"] = "-fno-exceptions"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Sharpmake.Options.Vc.Compiler.RTTI.Enable, () => { options["RuntimeTypeInfo"] = "true"; cmdLineOptions["RuntimeTypeInfo"] = "-frtti"; }),
                Sharpmake.Options.Option(Sharpmake.Options.Vc.Compiler.RTTI.Disable, () => { options["RuntimeTypeInfo"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["RuntimeTypeInfo"] = "-fno-rtti"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.ShortEnums.Enable, () => { options["ShortEnums"] = "true"; cmdLineOptions["ShortEnums"] = "-fshort-enums"; }),
                Sharpmake.Options.Option(Options.Compiler.ShortEnums.Disable, () => { options["ShortEnums"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["ShortEnums"] = "-fno-short-enums"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.DefaultCharUnsigned.Default, () => { options["SignedChar"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["SignedChar"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Compiler.DefaultCharUnsigned.Enable, () => { options["SignedChar"] = "false"; cmdLineOptions["SignedChar"] = "-funsigned-char"; }),
                Sharpmake.Options.Option(Options.Compiler.DefaultCharUnsigned.Disable, () => { options["SignedChar"] = "true"; cmdLineOptions["SignedChar"] = "-fsigned-char"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.CLanguageStandard.Default, () => { options["CLanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["CLanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Compiler.CLanguageStandard.C89, () => { options["CLanguageStandard"] = "c89"; cmdLineOptions["CLanguageStandard"] = "-std=c89"; }),
                Sharpmake.Options.Option(Options.Compiler.CLanguageStandard.C95, () => { options["CLanguageStandard"] = "iso9899:199409"; cmdLineOptions["CLanguageStandard"] = "-std=iso9899:199409"; }),
                Sharpmake.Options.Option(Options.Compiler.CLanguageStandard.C99, () => { options["CLanguageStandard"] = "c99"; cmdLineOptions["CLanguageStandard"] = "-std=c99"; }),
                Sharpmake.Options.Option(Options.Compiler.CLanguageStandard.C11, () => { options["CLanguageStandard"] = "c11"; cmdLineOptions["CLanguageStandard"] = "-std=c11"; }),
                Sharpmake.Options.Option(Options.Compiler.CLanguageStandard.GnuC89, () => { options["CLanguageStandard"] = "gnu89"; cmdLineOptions["CLanguageStandard"] = "-std=gnu89"; }),
                Sharpmake.Options.Option(Options.Compiler.CLanguageStandard.GnuC99, () => { options["CLanguageStandard"] = "gnu99"; cmdLineOptions["CLanguageStandard"] = "-std=gnu99"; }),
                Sharpmake.Options.Option(Options.Compiler.CLanguageStandard.GnuC11, () => { options["CLanguageStandard"] = "gnu11"; cmdLineOptions["CLanguageStandard"] = "-std=gnu11"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Compiler.CPPLanguageStandard.Default, () => { options["CppLanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["CppLanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; }),
                Sharpmake.Options.Option(Options.Compiler.CPPLanguageStandard.CPP98, () => { options["CppLanguageStandard"] = "c++98"; cmdLineOptions["CppLanguageStandard"] = "-std=c++98"; }),
                Sharpmake.Options.Option(Options.Compiler.CPPLanguageStandard.CPP11, () => { options["CppLanguageStandard"] = "c++11"; cmdLineOptions["CppLanguageStandard"] = "-std=c++11"; }),
                Sharpmake.Options.Option(Options.Compiler.CPPLanguageStandard.CPP1y, () => { options["CppLanguageStandard"] = "c++1y"; cmdLineOptions["CppLanguageStandard"] = "-std=c++1y"; }),
                Sharpmake.Options.Option(Options.Compiler.CPPLanguageStandard.GnuCPP98, () => { options["CppLanguageStandard"] = "gnu++98"; cmdLineOptions["CppLanguageStandard"] = "-std=gnu++98"; }),
                Sharpmake.Options.Option(Options.Compiler.CPPLanguageStandard.GnuCPP11, () => { options["CppLanguageStandard"] = "gnu++11"; cmdLineOptions["CppLanguageStandard"] = "-std=gnu++11"; }),
                Sharpmake.Options.Option(Options.Compiler.CPPLanguageStandard.GnuCPP1y, () => { options["CppLanguageStandard"] = "gnu++1y"; cmdLineOptions["CppLanguageStandard"] = "-std=gnu++1y"; })
                );
            }

            public override void SelectLinkerOptions(IGenerationContext context)
            {
                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.EchoCommandLines.Enable, () => { options["EchoCommandLinesLinker"] = "true"; }),
                Sharpmake.Options.Option(Options.Linker.EchoCommandLines.Disable, () => { options["EchoCommandLinesLinker"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                // NvShieldOptions.Linker.AndroidSystemLibraries
                Strings androidSystemLibraries = Sharpmake.Options.GetStrings<Options.Linker.AndroidSystemLibraries>(conf);
                if (androidSystemLibraries.Count > 0)
                {
                    options["AndroidSystemLibs"] = androidSystemLibraries.JoinStrings(";");
                    // TO DO implement for cmd
                    cmdLineOptions["AndroidSystemLibs"] = FileGeneratorUtilities.RemoveLineTag;
                }
                else
                {
                    options["AndroidSystemLibs"] = FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["AndroidSystemLibs"] = FileGeneratorUtilities.RemoveLineTag;
                }

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.LinkAgainstThumbVersionOfLibGcc.Enable, () => { options["LinkGccLibThumb"] = "true"; }),
                Sharpmake.Options.Option(Options.Linker.LinkAgainstThumbVersionOfLibGcc.Disable, () => { options["LinkGccLibThumb"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.ReportUndefinedSymbols.Enable, () => { options["ReportUndefinedSymbols"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["ReportUndefinedSymbols"] = "-Wl,--no-undefined"; }),
                Sharpmake.Options.Option(Options.Linker.ReportUndefinedSymbols.Disable, () => { options["ReportUndefinedSymbols"] = "false"; cmdLineOptions["ReportUndefinedSymbols"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.LinkerType.Bfd, () => { options["UseLinker"] = FileGeneratorUtilities.RemoveLineTag; cmdLineOptions["UseLinker"] = "-fuse-ld=bfd"; }),
                Sharpmake.Options.Option(Options.Linker.LinkerType.Gold, () => { options["UseLinker"] = "false"; cmdLineOptions["UseLinker"] = "-fuse-ld=gold"; })
                );

                context.SelectOption
                (
                Sharpmake.Options.Option(Options.Linker.ThinArchive.Enable, () => { options["ThinArchive"] = "true"; }),
                Sharpmake.Options.Option(Options.Linker.ThinArchive.Disable, () => { options["ThinArchive"] = FileGeneratorUtilities.RemoveLineTag; })
                );
            }

            public override void GenerateSdkVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_nvShieldSdkDeclarationTemplate);
            }

            public override void GenerateProjectCompileVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectConfigurationsCompileTemplate);
            }

            public override void GenerateProjectConfigurationGeneral(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectConfigurationsGeneral);
            }

            public override void GenerateProjectConfigurationGeneral2(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                generator.Write(_projectConfigurationsGeneral2);
            }

            public override void GenerateProjectConfigurationFastBuildMakeFile(IVcxprojGenerationContext context, IFileGenerator generator)
            {
                GenerateProjectConfigurationGeneral2(context, generator);
            }

            public override void SetupPlatformLibraryOptions(ref string platformLibExtension, ref string platformOutputLibExtension, ref string platformPrefixExtension)
            {
                base.SetupPlatformLibraryOptions(ref platformLibExtension, ref platformOutputLibExtension, ref platformPrefixExtension);
                platformLibExtension = string.Empty;
                platformOutputLibExtension = string.Empty;
            }

            protected override string GetProjectLinkSharedVcxprojTemplate()
            {
                return _projectConfigurationsLinkTemplate;
            }

            protected override string GetProjectStaticLinkVcxprojTemplate()
            {
                return _projectConfigurationsStaticLinkTemplate;
            }

            #endregion
        }
    }
}
