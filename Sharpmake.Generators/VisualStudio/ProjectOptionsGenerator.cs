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
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using Sharpmake.Generators.FastBuild;

namespace Sharpmake.Generators.VisualStudio
{
    internal enum ProjectOptionGenerationLevel
    {
        General,
        Compiler,
        Librarian,
        Linker,
        Manifest,
        PostBuild,
        All,
    }

    public class ProjectOptionsGenerator
    {
        private class ProjectOptionsGenerationContext
        {
            private readonly Project.Configuration _projectConfiguration;

            public string OutputDirectoryRelative { get; set; }
            public string OutputLibraryDirectoryRelative { get; set; }
            public string IntermediateDirectoryRelative { get; set; }
            public string TargetName { get; set; }
            public bool HasClrSupport { get; set; }
            public EnvironmentVariableResolver Resolver { get; }
            public string PlatformLibraryExtension { get; }
            public string PlatformOutputLibraryExtension { get; }
            public string PlatformPrefixExtension { get; }
            public IPlatformDescriptor PlatformDescriptor { get; }
            public IPlatformVcxproj PlatformVcxproj { get; }

            public ProjectOptionsGenerationContext(Project.Configuration conf, params VariableAssignment[] resolverParams)
            {
                _projectConfiguration = conf;
                Resolver = PlatformRegistry.Get<IPlatformDescriptor>(conf.Platform).GetPlatformEnvironmentResolver(resolverParams);

                PlatformDescriptor = PlatformRegistry.Get<IPlatformDescriptor>(conf.Platform);
                PlatformVcxproj = PlatformRegistry.Get<IPlatformVcxproj>(conf.Platform);

                string platformLibraryExtension = ".lib";
                string platformOutputLibraryExtension = ".lib";
                string platformPrefixExtension = string.Empty;
                PlatformVcxproj.SetupPlatformLibraryOptions(ref platformLibraryExtension, ref platformOutputLibraryExtension, ref platformPrefixExtension);

                PlatformLibraryExtension = platformLibraryExtension;
                PlatformOutputLibraryExtension = platformOutputLibraryExtension;
                PlatformPrefixExtension = platformPrefixExtension;
            }
        }

        internal class VcxprojCmdLineOptions : Dictionary<string, string>
        {
        }

        private static string GetPlatformStringDefineQuot(Platform platform)
        {
            return @"&quot;";
        }

        private string GetPlatformStringResourceDefineQuote(Platform platform)
        {
            return @"\&quot;";
        }

        internal void GenerateOptions(IGenerationContext context, ProjectOptionGenerationLevel level = ProjectOptionGenerationLevel.All)
        {
            var optionsContext = new ProjectOptionsGenerationContext(context.Configuration,
                new VariableAssignment("project", context.Project),
                new VariableAssignment("target", context.Configuration.Target),
                new VariableAssignment("conf", context.Configuration));

            GenerateGeneralOptions(context, optionsContext);

            if (level >= ProjectOptionGenerationLevel.Compiler)
                GenerateCompilerOptions(context, optionsContext);

            if (level >= ProjectOptionGenerationLevel.Librarian)
                GenerateLibrarianOptions(context, optionsContext);

            if (level >= ProjectOptionGenerationLevel.Linker)
                GenerateLinkerOptions(context, optionsContext);

            if (level >= ProjectOptionGenerationLevel.Manifest)
                GenerateManifestToolOptions(context, optionsContext);

            GenerateLLVMOptions(context, optionsContext);

            if (level >= ProjectOptionGenerationLevel.PostBuild)
                GeneratePostBuildOptions(context, optionsContext);
        }

        private void GenerateGeneralOptions(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            // Default defines, includes, libraries...
            context.Options.ExplicitDefines.AddRange(optionsContext.PlatformVcxproj.GetImplicitlyDefinedSymbols(context));

            // Set whatever VS needs to delete when you run the Clean command.
            optionsContext.PlatformVcxproj.SetupDeleteExtensionsOnCleanOptions(context);

            if (context.Configuration.DefaultOption == Options.DefaultTarget.Debug)
            {
                context.SelectOption
                (
                    Options.Option(Options.Vc.Compiler.RuntimeLibrary.MultiThreaded, () => { }),
                    Options.Option(Options.Vc.Compiler.RuntimeLibrary.MultiThreadedDebug, () => context.Options.ExplicitDefines.Add("_DEBUG")),
                    Options.Option(Options.Vc.Compiler.RuntimeLibrary.MultiThreadedDLL, () => { }),
                    Options.Option(Options.Vc.Compiler.RuntimeLibrary.MultiThreadedDebugDLL, () => context.Options.ExplicitDefines.Add("_DEBUG"))
                );
            }
            else // Release
            {
                context.Options.ExplicitDefines.Add("NDEBUG");
            }

            //Output
            //    Application                             Project.ProjectConfiguration.ConfigurationType="1"
            //    Dll                                     Project.ProjectConfiguration.ConfigurationType="2"             /D "_WINDLL"                            /DLL
            //    Lib                                     Project.ProjectConfiguration.ConfigurationType="4"
            SelectConfigurationTypeOption(context);

            context.Options.ExplicitDefines.AddRange(optionsContext.PlatformVcxproj.GetImplicitlyDefinedSymbols(context));

            optionsContext.OutputDirectoryRelative = Util.PathGetRelative(context.ProjectDirectory, context.Configuration.TargetPath);
            optionsContext.OutputLibraryDirectoryRelative = Util.PathGetRelative(context.ProjectDirectory, context.Configuration.TargetLibraryPath);
            if (context.Configuration.Output == Project.Configuration.OutputType.Lib)
                context.Options["OutputDirectory"] = optionsContext.OutputLibraryDirectoryRelative;
            else if (context.Configuration.Output != Project.Configuration.OutputType.None)
                context.Options["OutputDirectory"] = optionsContext.OutputDirectoryRelative;
            else
                context.Options["OutputDirectory"] = FileGeneratorUtilities.RemoveLineTag;

            //IntermediateDirectory
            optionsContext.IntermediateDirectoryRelative = Util.PathGetRelative(context.ProjectDirectory, context.Configuration.IntermediatePath);
            context.Options["IntermediateDirectory"] = context.Configuration.Output != Project.Configuration.OutputType.None ? optionsContext.IntermediateDirectoryRelative : FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["IntermediateDirectory"] = FormatCommandLineOptionPath(context, optionsContext.IntermediateDirectoryRelative);

            optionsContext.TargetName = context.Configuration.TargetFileFullName;
            if (!string.IsNullOrEmpty(context.Configuration.LayoutDir))
                context.Options["LayoutDir"] = Util.PathGetRelative(context.ProjectDirectory, context.Configuration.LayoutDir);
            else
                context.Options["LayoutDir"] = FileGeneratorUtilities.RemoveLineTag;
            context.Options["PullMappingFile"] = !string.IsNullOrEmpty(context.Configuration.PullMappingFile) ? context.Configuration.PullMappingFile : FileGeneratorUtilities.RemoveLineTag;
            context.Options["PullTemporaryFolder"] = !string.IsNullOrEmpty(context.Configuration.PullTemporaryFolder) ? context.Configuration.PullTemporaryFolder : FileGeneratorUtilities.RemoveLineTag;

            if (!string.IsNullOrEmpty(context.Configuration.LayoutExtensionFilter))
                context.Options["LayoutExtensionFilter"] = context.Configuration.LayoutExtensionFilter;
            else
                context.Options["LayoutExtensionFilter"] = FileGeneratorUtilities.RemoveLineTag;

            // This should normally be set with the KitsRootPaths class, but this allows the coder to force a platform version.
            var winTargetPlatformVersionOptionActions = new List<Options.OptionAction>();
            foreach (Options.Vc.General.WindowsTargetPlatformVersion winVersion in Enum.GetValues(typeof(Options.Vc.General.WindowsTargetPlatformVersion)))
                winTargetPlatformVersionOptionActions.Add(Options.Option(winVersion, () => { context.Options["WindowsTargetPlatformVersion"] = winVersion.ToVersionString(); }));
            context.SelectOptionWithFallback(
                () => { context.Options["WindowsTargetPlatformVersion"] = FileGeneratorUtilities.RemoveLineTag; },
                winTargetPlatformVersionOptionActions.ToArray()
            );

            if (context.Configuration.Target.GetPlatform() == Platform.linux)
            {
                context.Options["ProjectDirectory"] = "/mnt/" + context.ProjectDirectoryCapitalized.Replace(@":", string.Empty).Replace('\\', '/');
                context.Options["OutputDirectoryRemote"] = (@"$(RemoteProjectDir)" + context.Options["OutputDirectory"]).Replace('\\', '/');
                context.Options["IntermediateDirectoryRemote"] = (@"$(RemoteProjectDir)" + context.Options["IntermediateDirectory"]).Replace('\\', '/');
            }
        }

        private static void SelectConfigurationTypeOption(IGenerationContext context)
        {
            context.CommandLineOptions["ConfigurationType"] = FileGeneratorUtilities.RemoveLineTag;
            context.Options["ConfigurationType"] = FileGeneratorUtilities.RemoveLineTag;
            switch (context.Configuration.Output)
            {
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.DotNetConsoleApp:
                case Project.Configuration.OutputType.DotNetWindowsApp:
                    {
                        context.Options["ConfigurationType"] = context.Configuration.IsFastBuild ? "Makefile" : "Application";
                    }
                    break;
                case Project.Configuration.OutputType.Dll:
                case Project.Configuration.OutputType.DotNetClassLibrary:
                    {
                        if (!PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform).HasSharedLibrarySupport)
                        {
                            throw new Error($"Current platform {context.Configuration.Platform} doesn't support shared lib output type: Project {context.Project.GetType()} conf {context.Configuration.Target}");
                        }
                        context.Options["ConfigurationType"] = context.Configuration.IsFastBuild ? "Makefile" : "DynamicLibrary";
                        context.CommandLineOptions["ConfigurationType"] = @"/D""_WINDLL""";
                    }
                    break;
                case Project.Configuration.OutputType.Lib:
                    context.Options["ConfigurationType"] = context.Configuration.IsFastBuild ? "Makefile" : "StaticLibrary";
                    break;
                case Project.Configuration.OutputType.Utility:
                    context.Options["ConfigurationType"] = "Utility";
                    break;
                case Project.Configuration.OutputType.None:
                    context.Options["ConfigurationType"] = context.Configuration.IsFastBuild || context.Configuration.CustomBuildSettings != null ? "Makefile" : FileGeneratorUtilities.RemoveLineTag;
                    break;
            }
        }

        private void GenerateCompilerOptions(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            var forcedIncludes = new Strings();

            if (!context.Configuration.IsFastBuild)
            {
                // support of PCH requires them to be set as ForceIncludes with ClangCl
                if (Options.GetObject<Options.Vc.General.PlatformToolset>(context.Configuration).IsLLVMToolchain() &&
                    Options.GetObject<Options.Vc.LLVM.UseClangCl>(context.Configuration) == Options.Vc.LLVM.UseClangCl.Enable)
                {
                    forcedIncludes.Add(context.Configuration.PrecompHeader);
                }
            }

            forcedIncludes.AddRange(context.Configuration.ForcedIncludes);

            if (forcedIncludes.Count > 0)
            {
                context.Options["ForcedIncludeFiles"] = forcedIncludes.JoinStrings(";");

                // save the vanilla value without the LLVM workaround for reuse later
                if (forcedIncludes.Count != context.Configuration.ForcedIncludes.Count)
                    context.Options["ForcedIncludeFilesVanilla"] = context.Configuration.ForcedIncludes.JoinStrings(";");

                StringBuilder result = new StringBuilder();
                foreach (var forcedInclude in forcedIncludes)
                    result.Append(@"/FI""" + forcedInclude + @""" ");
                result.Remove(result.Length - 1, 1);
                context.CommandLineOptions["ForcedIncludeFiles"] = result.ToString();
            }
            else
            {
                context.Options["ForcedIncludeFiles"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["ForcedIncludeFiles"] = FileGeneratorUtilities.RemoveLineTag;
            }

            if (optionsContext.PlatformDescriptor.IsUsingClang)
            {
                context.Options["CharacterSet"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["CharacterSet"] = FileGeneratorUtilities.RemoveLineTag;

                context.Options["LanguageStandard"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["LanguageStandard"] = FileGeneratorUtilities.RemoveLineTag;

                context.SelectOption
                (
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.Default, () => { context.Options["ClangCppLanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.Cpp98, () => { context.Options["ClangCppLanguageStandard"] = "-std=c++98"; }),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.Cpp11, () => { context.Options["ClangCppLanguageStandard"] = "-std=c++11"; }),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.Cpp14, () => { context.Options["ClangCppLanguageStandard"] = "-std=c++14"; }),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.Cpp17, () => { context.Options["ClangCppLanguageStandard"] = "-std=c++17"; }),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.GnuCpp98, () => { context.Options["ClangCppLanguageStandard"] = "-std=gnu++98"; }),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.GnuCpp11, () => { context.Options["ClangCppLanguageStandard"] = "-std=gnu++11"; }),
                Options.Option(Options.Clang.Compiler.CppLanguageStandard.GnuCpp14, () => { context.Options["ClangCppLanguageStandard"] = "-std=gnu++14"; })
                );
            }
            else
            {
                context.Options["ClangCppLanguageStandard"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["ClangCppLanguageStandard"] = FileGeneratorUtilities.RemoveLineTag;

                //Options.Vc.General.CharacterSet.
                //    NotSet                                  CharacterSet="0"
                //    UseUnicodeCharaterSet                   Project.ProjectConfiguration.CharacterSet="1"                  /D "_UNICODE" /D "UNICODE"
                //    UseMultiByteCharaterSet                 Project.ProjectConfiguration.CharacterSet="2"                  /D "_MBCS"
                context.SelectOption
                (
                Options.Option(Options.Vc.General.CharacterSet.Default, () => { context.Options["CharacterSet"] = "NotSet"; context.CommandLineOptions["CharacterSet"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.General.CharacterSet.Unicode, () => { context.Options["CharacterSet"] = "Unicode"; context.CommandLineOptions["CharacterSet"] = @"/D""_UNICODE"" /D""UNICODE"""; }),
                Options.Option(Options.Vc.General.CharacterSet.MultiByte, () => { context.Options["CharacterSet"] = "MultiByte"; context.CommandLineOptions["CharacterSet"] = @"/D""_MBCS"""; })
                );

                //Options.Vc.Compiler.CppLanguageStandard.
                //    CPP98                                   LanguageStandard=""
                //    CPP11                                   LanguageStandard=""
                //    CPP14                                   LanguageStandard="stdcpp14"                                    /std:c++14
                //    CPP17                                   LanguageStandard="stdcpp17"                                    /std:c++17
                //    GNU98                                   LanguageStandard=""
                //    GNU11                                   LanguageStandard=""
                //    GNU14                                   LanguageStandard="stdcpp14"                                    /std:c++14
                //    GNU17                                   LanguageStandard="stdcpp17"                                    /std:c++17
                //    Latest                                  LanguageStandard="stdcpplatest"                                /std:c++latest
                context.SelectOption
                (
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.CPP98, () => { context.Options["LanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["LanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.CPP11, () => { context.Options["LanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["LanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.CPP14, () => { context.Options["LanguageStandard"] = "stdcpp14"; context.CommandLineOptions["LanguageStandard"] = "/std:c++14"; }),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.CPP17, () => { context.Options["LanguageStandard"] = "stdcpp17"; context.CommandLineOptions["LanguageStandard"] = "/std:c++17"; }),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.GNU98, () => { context.Options["LanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["LanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.GNU11, () => { context.Options["LanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["LanguageStandard"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.GNU14, () => { context.Options["LanguageStandard"] = "stdcpp14"; context.CommandLineOptions["LanguageStandard"] = "/std:c++14"; }),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.GNU17, () => { context.Options["LanguageStandard"] = "stdcpp17"; context.CommandLineOptions["LanguageStandard"] = "/std:c++17"; }),
                Options.Option(Options.Vc.Compiler.CppLanguageStandard.Latest, () => { context.Options["LanguageStandard"] = "stdcpplatest"; context.CommandLineOptions["LanguageStandard"] = "/std:c++latest"; })
                );
            }

            // Compiler section

            //Options.Vc.General.CommonLanguageRuntimeSupport.
            context.SelectOption
            (
            Options.Option(Options.Vc.General.CommonLanguageRuntimeSupport.NoClrSupport, () => { context.Options["CLRSupport"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["CLRSupport"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.General.CommonLanguageRuntimeSupport.ClrSupport, () => { context.Options["CLRSupport"] = "true"; context.CommandLineOptions["CLRSupport"] = "/clr"; }),
            Options.Option(Options.Vc.General.CommonLanguageRuntimeSupport.PureMsilClrSupport, () => { context.Options["CLRSupport"] = "Pure"; context.CommandLineOptions["CLRSupport"] = "/clr:pure"; }),
            Options.Option(Options.Vc.General.CommonLanguageRuntimeSupport.SafeMsilClrSupport, () => { context.Options["CLRSupport"] = "Safe"; context.CommandLineOptions["CLRSupport"] = "/clr:safe"; }),
            Options.Option(Options.Vc.General.CommonLanguageRuntimeSupport.SafeMsilClrSupportOldSyntax, () => { context.Options["CLRSupport"] = "OldSyntax"; context.CommandLineOptions["CLRSupport"] = "/clr:oldSyntax"; })
            );

            context.SelectOption
            (
            Options.Option(Options.Vc.General.MfcSupport.UseMfcStdWin, () => { context.Options["UseOfMfc"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["UseOfMfc"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.General.MfcSupport.UseMfcStatic, () => { context.Options["UseOfMfc"] = "Static"; context.CommandLineOptions["UseOfMfc"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.General.MfcSupport.UseMfcDynamic, () => { context.Options["UseOfMfc"] = "Dynamic"; context.CommandLineOptions["UseOfMfc"] = FileGeneratorUtilities.RemoveLineTag; })
            );

            //Options.Vc.General.WholeProgramOptimization.
            //    NoWholeProgramOptimization              WholeProgramOptimization="0"
            //    UseLinkTimeCodeGeneration               WholeProgramOptimization="1"                    /GL                                 /LTCG
            //    ProfileGuidedOptimizationInstrument     WholeProgramOptimization="2"                    /GL                                 /LTCG:PGINSTRUMENT
            //    ProfileGuidedOptimizationOptimize       WholeProgramOptimization="3"                    /GL                                 /LTCG:PGOPTIMIZE /PGD:"f:\coding\helloworld\helloworld\Debug\hellochange.pgd"
            //    ProfileGuidedOptimizationUpdate         WholeProgramOptimization="3"                    /GL                                 /LTCG:PGUPDATE /PGD:"f:\coding\helloworld\helloworld\Debug\hellochange.pgd"
            context.SelectOption
            (
            Options.Option(Options.Vc.General.WholeProgramOptimization.Disable, () => { context.Options["WholeProgramOptimization"] = "false"; context.Options["CompilerWholeProgramOptimization"] = "false"; context.CommandLineOptions["CompilerWholeProgramOptimization"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.General.WholeProgramOptimization.LinkTime, () => { context.Options["WholeProgramOptimization"] = "true"; context.Options["CompilerWholeProgramOptimization"] = "true"; context.CommandLineOptions["CompilerWholeProgramOptimization"] = "/GL"; }),
            Options.Option(Options.Vc.General.WholeProgramOptimization.Instrument, () => { context.Options["WholeProgramOptimization"] = "PGInstrument"; context.Options["CompilerWholeProgramOptimization"] = "true"; context.CommandLineOptions["CompilerWholeProgramOptimization"] = "/GL"; }),
            Options.Option(Options.Vc.General.WholeProgramOptimization.Optimize, () => { context.Options["WholeProgramOptimization"] = "PGOptimize"; context.Options["CompilerWholeProgramOptimization"] = "true"; context.CommandLineOptions["CompilerWholeProgramOptimization"] = "/GL"; }),
            Options.Option(Options.Vc.General.WholeProgramOptimization.Update, () => { context.Options["WholeProgramOptimization"] = "PGUpdate"; context.Options["CompilerWholeProgramOptimization"] = "true"; context.CommandLineOptions["CompilerWholeProgramOptimization"] = "/GL"; })
            );

            optionsContext.PlatformVcxproj.SelectApplicationFormatOptions(context);
            optionsContext.PlatformVcxproj.SelectBuildType(context);

            context.Options["ExecutablePath"] = FileGeneratorUtilities.RemoveLineTag;
            context.Options["IncludePath"] = FileGeneratorUtilities.RemoveLineTag;
            context.Options["LibraryPath"] = FileGeneratorUtilities.RemoveLineTag;
            context.Options["ExcludePath"] = FileGeneratorUtilities.RemoveLineTag;
            context.Options["AdditionalUsingDirectories"] = FileGeneratorUtilities.RemoveLineTag;

            optionsContext.PlatformVcxproj.SetupSdkOptions(context);

            bool writeResourceCompileTag = optionsContext.PlatformVcxproj.GetResourceIncludePaths(context).Any();

            //Resource Compiler ShowProgress
            //    No                                      ShowProgress="false"
            //    Yes                                     ShowProgress="true"
            context.SelectOption
            (
            Options.Option(Options.Vc.ResourceCompiler.ShowProgress.No, () => { context.Options["ResourceCompilerShowProgress"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.ResourceCompiler.ShowProgress.Yes, () => { context.Options["ResourceCompilerShowProgress"] = "true"; writeResourceCompileTag = true; })
            );

            // Options.Vc.ResourceCompiler.PreprocessorDefinitions
            Strings resourcedefines = Options.GetStrings<Options.Vc.ResourceCompiler.PreprocessorDefinitions>(context.Configuration);
            if (resourcedefines.Any())
            {
                context.Options["ResourcePreprocessorDefinitions"] = resourcedefines.JoinStrings(";").Replace(@"""", GetPlatformStringResourceDefineQuote(context.Configuration.Platform));
                writeResourceCompileTag = true;
            }
            else
            {
                context.Options["ResourcePreprocessorDefinitions"] = FileGeneratorUtilities.RemoveLineTag;
            }

            context.Options["ResourceCompileTag"] = writeResourceCompileTag ? string.Empty : FileGeneratorUtilities.RemoveLineTag;

            //Options.Vc.General.DebugInformation.
            //    Disabled                                Project.ProjectConfiguration.Tool.DebugInformationFormat="0"
            //    C7Compatible                            Project.ProjectConfiguration.Tool.DebugInformationFormat="1"   /Z7
            //    ProgramDatabase                         Project.ProjectConfiguration.Tool.DebugInformationFormat="3"   /Zi
            //    ProgramDatabaseForEditAndContinue       Project.ProjectConfiguration.Tool.DebugInformationFormat="4"   /ZI

            SelectDebugInformationOption(context, optionsContext);

            //Options.Vc.General.UseDebugLibraries.
            //    Disable                                 WarnAsError="false"
            //    Enable                                  WarnAsError="true"                              /WX
            context.SelectOption
            (
            Options.Option(Options.Vc.General.UseDebugLibraries.Disabled, () => { context.Options["UseDebugLibraries"] = "false"; }),
            Options.Option(Options.Vc.General.UseDebugLibraries.Enabled, () => { context.Options["UseDebugLibraries"] = "true"; })
            );

            //Options.Vc.General.WarningLevel.
            //    Level0                                  Project.ProjectConfiguration.Tool.WarningLevel="0"             /W0
            //    Level1                                  Project.ProjectConfiguration.Tool.WarningLevel="1"             /W1
            //    Level2                                  Project.ProjectConfiguration.Tool.WarningLevel="2"             /W2
            //    Level3                                  Project.ProjectConfiguration.Tool.WarningLevel="3"             /W3
            //    Level4                                  Project.ProjectConfiguration.Tool.WarningLevel="4"             /W4
            context.SelectOption
            (
            Options.Option(Options.Vc.General.WarningLevel.Level0, () => { context.Options["WarningLevel"] = "TurnOffAllWarnings"; context.CommandLineOptions["WarningLevel"] = "/W0"; }),
            Options.Option(Options.Vc.General.WarningLevel.Level1, () => { context.Options["WarningLevel"] = "Level1"; context.CommandLineOptions["WarningLevel"] = "/W1"; }),
            Options.Option(Options.Vc.General.WarningLevel.Level2, () => { context.Options["WarningLevel"] = "Level2"; context.CommandLineOptions["WarningLevel"] = "/W2"; }),
            Options.Option(Options.Vc.General.WarningLevel.Level3, () => { context.Options["WarningLevel"] = "Level3"; context.CommandLineOptions["WarningLevel"] = "/W3"; }),
            Options.Option(Options.Vc.General.WarningLevel.Level4, () => { context.Options["WarningLevel"] = "Level4"; context.CommandLineOptions["WarningLevel"] = "/W4"; }),
            Options.Option(Options.Vc.General.WarningLevel.EnableAllWarnings, () => { context.Options["WarningLevel"] = "EnableAllWarnings"; context.CommandLineOptions["WarningLevel"] = "/Wall"; })
            );

            //Options.Vc.General.TreatWarnigAsError.
            //    Disable                                 WarnAsError="false"
            //    Enable                                  WarnAsError="true"                              /WX
            context.SelectOption
            (
            Options.Option(Options.Vc.General.TreatWarningsAsErrors.Disable, () => { context.Options["TreatWarningAsError"] = "false"; context.CommandLineOptions["TreatWarningAsError"] = "/WX-"; }),
            Options.Option(Options.Vc.General.TreatWarningsAsErrors.Enable, () => { context.Options["TreatWarningAsError"] = "true"; context.CommandLineOptions["TreatWarningAsError"] = "/WX"; })
            );

            context.SelectOption
            (
            Options.Option(Options.Vc.General.DiagnosticsFormat.Classic, () => { context.Options["DiagnosticsFormat"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["DiagnosticsFormat"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.General.DiagnosticsFormat.Caret, () => { context.Options["DiagnosticsFormat"] = "Caret"; context.CommandLineOptions["DiagnosticsFormat"] = "/diagnostics:caret"; }),
            Options.Option(Options.Vc.General.DiagnosticsFormat.ColumnInfo, () => { context.Options["DiagnosticsFormat"] = "Column"; context.CommandLineOptions["DiagnosticsFormat"] = "/diagnostics:column"; })
            );


            context.Options["TrackFileAccess"] = FileGeneratorUtilities.RemoveLineTag;

            if (context.DevelopmentEnvironment.IsVisualStudio())
            {
                SelectPreferredToolArchitecture(context);
                SelectPlatformToolsetOption(context, optionsContext);
            }


            // Compiler.SuppressStartupBanner
            context.CommandLineOptions["SuppressStartupBanner"] = "/nologo";

            //Options.Vc.Compiler.MultiProcessorCompilation.
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.MultiProcessorCompilation.Enable, () => { context.Options["MultiProcessorCompilation"] = "true"; context.CommandLineOptions["MultiProcessorCompilation"] = "/MP"; }),
            Options.Option(Options.Vc.Compiler.MultiProcessorCompilation.Disable, () => { context.Options["MultiProcessorCompilation"] = "false"; context.CommandLineOptions["MultiProcessorCompilation"] = FileGeneratorUtilities.RemoveLineTag; })
            );


            //Options.Vc.Compiler.Optimization.
            //    Disable                                 Project.ProjectConfiguration.Tool.Optimization="0"             /Od
            //    MinimizeSize                            Project.ProjectConfiguration.Tool.Optimization="1"             /O1
            //    MaximizeSpeed                           Project.ProjectConfiguration.Tool.Optimization="2"             /O2
            //    FullOptimization                        Project.ProjectConfiguration.Tool.Optimization="3"             /Ox
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.Optimization.Disable, () => { context.Options["Optimization"] = "Disabled"; context.CommandLineOptions["Optimization"] = "/Od"; }),
            Options.Option(Options.Vc.Compiler.Optimization.MinimizeSize, () => { context.Options["Optimization"] = "MinSpace"; context.CommandLineOptions["Optimization"] = "/O1"; }),
            Options.Option(Options.Vc.Compiler.Optimization.MaximizeSpeed, () => { context.Options["Optimization"] = "MaxSpeed"; context.CommandLineOptions["Optimization"] = "/O2"; }),
            Options.Option(Options.Vc.Compiler.Optimization.FullOptimization, () => { context.Options["Optimization"] = "Full"; context.CommandLineOptions["Optimization"] = "/Ox"; })
            );

            //Options.Vc.Compiler.Inline.
            //    Default                                 InlineFunctionExpansion="0"
            //    OnlyInline                              InlineFunctionExpansion="1"                     /Ob1
            //    AnySuitable                             InlineFunctionExpansion="2"                     /Ob2
            //    Disable                                 InlineFunctionExpansion="3"                     /Ob0
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.Inline.Default, () => { context.Options["InlineFunctionExpansion"] = "Default"; context.CommandLineOptions["InlineFunctionExpansion"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.Inline.OnlyInline, () => { context.Options["InlineFunctionExpansion"] = "OnlyExplicitInline"; context.CommandLineOptions["InlineFunctionExpansion"] = "/Ob1"; }),
            Options.Option(Options.Vc.Compiler.Inline.AnySuitable, () => { context.Options["InlineFunctionExpansion"] = "AnySuitable"; context.CommandLineOptions["InlineFunctionExpansion"] = "/Ob2"; }),
            Options.Option(Options.Vc.Compiler.Inline.Disable, () => { context.Options["InlineFunctionExpansion"] = "Disabled"; context.CommandLineOptions["InlineFunctionExpansion"] = "/Ob0"; })
            );

            //Options.Vc.Compiler.Intrinsic.
            //    Disable                                 EnableIntrinsicFunctions="false"
            //    Enable                                  EnableIntrinsicFunctions="true"                 /Oi
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.Intrinsic.Disable, () => { context.Options["EnableIntrinsicFunctions"] = "false"; context.CommandLineOptions["EnableIntrinsicFunctions"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.Intrinsic.Enable, () => { context.Options["EnableIntrinsicFunctions"] = "true"; context.CommandLineOptions["EnableIntrinsicFunctions"] = "/Oi"; })
            );

            //Compiler.Optimization.FavorSizeOrSpeed
            //    Neither                                 FavorSizeOrSpeed="0"
            //    FavorFastCode                           FavorSizeOrSpeed="1"                            /Ot
            //    FavorSmallCode                          FavorSizeOrSpeed="2"                            /Os
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.FavorSizeOrSpeed.Neither, () => { context.Options["FavorSizeOrSpeed"] = "Neither"; context.CommandLineOptions["FavorSizeOrSpeed"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.FavorSizeOrSpeed.FastCode, () => { context.Options["FavorSizeOrSpeed"] = "Speed"; context.CommandLineOptions["FavorSizeOrSpeed"] = "/Ot"; }),
            Options.Option(Options.Vc.Compiler.FavorSizeOrSpeed.SmallCode, () => { context.Options["FavorSizeOrSpeed"] = "Size"; context.CommandLineOptions["FavorSizeOrSpeed"] = "/Os"; })
            );

            //Compiler.Optimization.OmitFramePointers
            //    Disable                                 OmitFramePointers="false"
            //    Enable                                  OmitFramePointers="true"                        /Oy
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.OmitFramePointers.Disable, () => { context.Options["OmitFramePointers"] = "false"; context.CommandLineOptions["OmitFramePointers"] = "/Oy-"; }),
            Options.Option(Options.Vc.Compiler.OmitFramePointers.Enable, () => { context.Options["OmitFramePointers"] = "true"; context.CommandLineOptions["OmitFramePointers"] = "/Oy"; })
            );

            //Compiler.Optimization.FiberSafe
            //    Disable                                 EnableFiberSafeOptimizations="false"
            //    Enable                                  EnableFiberSafeOptimizations="true"             /GT
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.FiberSafe.Disable, () => { context.Options["EnableFiberSafeOptimizations"] = "false"; context.CommandLineOptions["EnableFiberSafeOptimizations"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.FiberSafe.Enable, () => { context.Options["EnableFiberSafeOptimizations"] = "true"; context.CommandLineOptions["EnableFiberSafeOptimizations"] = "/GT"; })
            );

            //Compiler.IgnoreStandardIncludePath.
            //    Disable                                 IgnoreStandardIncludePath="false"
            //    Enable                                  IgnoreStandardIncludePath="true"                /X
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.IgnoreStandardIncludePath.Disable, () => { context.Options["IgnoreStandardIncludePath"] = "false"; context.CommandLineOptions["IgnoreStandardIncludePath"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.IgnoreStandardIncludePath.Enable, () => { context.Options["IgnoreStandardIncludePath"] = "true"; context.CommandLineOptions["IgnoreStandardIncludePath"] = "/X"; })
            );

            //Compiler.Proprocessor.GenerateProcessorFile
            //    Disable                                 GeneratePreprocessedFile="0"
            //    WithLineNumbers                         GeneratePreprocessedFile="1"                    /P
            //    WithoutLineNumbers                      GeneratePreprocessedFile="2"                    /EP /P
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.GenerateProcessorFile.Disable, () => { context.Options["GeneratePreprocessedFile"] = "false"; context.Options["PreprocessSuppressLineNumbers"] = "false"; context.CommandLineOptions["GeneratePreprocessedFile"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.GenerateProcessorFile.WithLineNumbers, () => { context.Options["GeneratePreprocessedFile"] = "true"; context.Options["PreprocessSuppressLineNumbers"] = "false"; context.CommandLineOptions["GeneratePreprocessedFile"] = "/P"; }),
            Options.Option(Options.Vc.Compiler.GenerateProcessorFile.WithoutLineNumbers, () => { context.Options["GeneratePreprocessedFile"] = "true"; context.Options["PreprocessSuppressLineNumbers"] = "true"; context.CommandLineOptions["GeneratePreprocessedFile"] = "/EP /P"; })
            );

            //Options.Vc.Compiler.KeepComment.
            //    Disable                                 KeepComments="false"
            //    Enable                                  KeepComments="true"                             /C
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.KeepComment.Disable, () => { context.Options["KeepComments"] = "false"; context.CommandLineOptions["KeepComments"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.KeepComment.Enable, () => { context.Options["KeepComments"] = "true"; context.CommandLineOptions["KeepComments"] = "/C"; })
            );

            //Options.Vc.Compiler.StringPooling.
            //    Disable                                 StringPooling="false"
            //    Enable                                  StringPooling="true"                            /GF
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.StringPooling.Disable, () => { context.Options["StringPooling"] = "false"; context.CommandLineOptions["StringPooling"] = "/GF-"; }),
            Options.Option(Options.Vc.Compiler.StringPooling.Enable, () => { context.Options["StringPooling"] = "true"; context.CommandLineOptions["StringPooling"] = "/GF"; })
            );

            //Options.Vc.Compiler.Exceptions.
            //    Disable                                 ExceptionHandling="false"
            //    Enable                                  ExceptionHandling="Sync"                        /EHsc
            //    EnableWithExternC                       ExceptionHandling="SyncCThrow"                  /EHs
            //    EnableWithSEH                           ExceptionHandling="Async"                       /EHa
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.Exceptions.Disable, () => { context.Options["ExceptionHandling"] = "false"; context.CommandLineOptions["ExceptionHandling"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.Exceptions.Enable, () => { context.Options["ExceptionHandling"] = "Sync"; context.CommandLineOptions["ExceptionHandling"] = "/EHsc"; }),
            Options.Option(Options.Vc.Compiler.Exceptions.EnableWithExternC, () => { context.Options["ExceptionHandling"] = "SyncCThrow"; context.CommandLineOptions["ExceptionHandling"] = "/EHs"; }),
            Options.Option(Options.Vc.Compiler.Exceptions.EnableWithSEH, () => { context.Options["ExceptionHandling"] = "Async"; context.CommandLineOptions["ExceptionHandling"] = "/EHa"; })
            );

            context.Options["ForcedUsingFiles"] = FileGeneratorUtilities.RemoveLineTag;
            if (context.Configuration.ForceUsingFiles.Any() || context.Configuration.DependenciesForceUsingFiles.Any() || context.Configuration.ForceUsingDependencies.Any())
            {
                StringBuilder builder = new StringBuilder(context.Configuration.ForceUsingFiles.JoinStrings(";", true));
                if (context.Configuration.ForceUsingFiles.Any())
                    builder.Append(";");

                builder.Append(context.Configuration.DependenciesForceUsingFiles.JoinStrings(";"));
                if (context.Configuration.DependenciesForceUsingFiles.Any())
                    builder.Append(";");

                foreach (var dep in context.Configuration.ForceUsingDependencies)
                    builder.AppendFormat(@"{0}.dll;", dep.Project is CSharpProject ? dep.TargetFileName : dep.TargetFileFullName);
                string ForceUsingFiles = builder.ToString();
                context.Options["ForcedUsingFiles"] = ForceUsingFiles.Remove(ForceUsingFiles.Length - 1, 1);
            }

            //Options.Vc.Compiler.CompileAsWinRT.     
            //    Disable                                 CompileAsWinRT="false"
            //    Enable                                  CompileAsWinRT="true"
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.CompileAsWinRT.Default, () => { context.Options["CompileAsWinRT"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.CompileAsWinRT.Disable, () => { context.Options["CompileAsWinRT"] = "false"; }),
            Options.Option(Options.Vc.Compiler.CompileAsWinRT.Enable, () => { context.Options["CompileAsWinRT"] = "true"; })
            );

            //Options.Vc.Compiler.TypeChecks.
            //    Disable                                 SmallerTypeCheck="true"                         /RTCc
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.TypeChecks.Disable, () => { context.Options["SmallerTypeCheck"] = "false"; context.CommandLineOptions["SmallerTypeCheck"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.TypeChecks.Enable, () => { context.Options["SmallerTypeCheck"] = "true"; context.CommandLineOptions["SmallerTypeCheck"] = "/RTCc"; })
            );

            //Options.Vc.Compiler.RuntimeChecks.
            //    Default                                 BasicRuntimeChecks="0"
            //    StackFrames                             BasicRuntimeChecks="1"                          /RTCs
            //    UninitializedVariables                  BasicRuntimeChecks="2"                          /RTCu
            //    Both                                    BasicRuntimeChecks="3"                          /RTC1
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.RuntimeChecks.Default, () => { context.Options["BasicRuntimeChecks"] = "Default"; context.CommandLineOptions["BasicRuntimeChecks"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.RuntimeChecks.StackFrames, () => { context.Options["BasicRuntimeChecks"] = "StackFrameRuntimeCheck"; context.CommandLineOptions["BasicRuntimeChecks"] = "/RTCs"; }),
            Options.Option(Options.Vc.Compiler.RuntimeChecks.UninitializedVariables, () => { context.Options["BasicRuntimeChecks"] = "UninitializedLocalUsageCheck"; context.CommandLineOptions["BasicRuntimeChecks"] = "/RTCu"; }),
            Options.Option(Options.Vc.Compiler.RuntimeChecks.Both, () => { context.Options["BasicRuntimeChecks"] = "EnableFastChecks"; context.CommandLineOptions["BasicRuntimeChecks"] = "/RTC1"; })
            );

            if (Util.IsCpp(context.Configuration))
            {
                //Options.Vc.Compiler.RuntimeLibrary.
                //    MultiThreaded                           RuntimeLibrary="0"                              /MT
                //    MultiThreadedDebug                      RuntimeLibrary="1"                              /MTd
                //    MultiThreadedDLL                        RuntimeLibrary="2"                              /MD
                //    MultiThreadedDebugDLL                   RuntimeLibrary="3"                              /MDd
                context.SelectOption
                (
                Options.Option(Options.Vc.Compiler.RuntimeLibrary.MultiThreaded, () => { context.Options["RuntimeLibrary"] = "MultiThreaded"; context.CommandLineOptions["RuntimeLibrary"] = "/MT"; }),
                Options.Option(Options.Vc.Compiler.RuntimeLibrary.MultiThreadedDebug, () => { context.Options["RuntimeLibrary"] = "MultiThreadedDebug"; context.CommandLineOptions["RuntimeLibrary"] = "/MTd"; }),
                Options.Option(Options.Vc.Compiler.RuntimeLibrary.MultiThreadedDLL, () => { context.Options["RuntimeLibrary"] = "MultiThreadedDLL"; context.CommandLineOptions["RuntimeLibrary"] = "/MD"; }),
                Options.Option(Options.Vc.Compiler.RuntimeLibrary.MultiThreadedDebugDLL, () => { context.Options["RuntimeLibrary"] = "MultiThreadedDebugDLL"; context.CommandLineOptions["RuntimeLibrary"] = "/MDd"; })
                );
            }
            else
            {
                context.Options["RuntimeLibrary"] = FileGeneratorUtilities.RemoveLineTag;
            }

            bool clrSupport = Util.IsDotNet(context.Configuration);
            if (!clrSupport && context.DevelopmentEnvironment.IsVisualStudio() && context.DevelopmentEnvironment < DevEnv.vs2019) // Gm is deprecated starting with vs2019
            {
                //Options.Vc.Compiler.MinimalRebuild.
                //    Disable                                 MinimalRebuild="false"
                //    Enable                                  MinimalRebuild="true"                           /Gm
                context.SelectOption
                (
                Options.Option(Options.Vc.Compiler.MinimalRebuild.Disable, () => { context.Options["MinimalRebuild"] = "false"; context.CommandLineOptions["MinimalRebuild"] = "/Gm-"; }),
                Options.Option(Options.Vc.Compiler.MinimalRebuild.Enable, () => { context.Options["MinimalRebuild"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["MinimalRebuild"] = "/Gm"; })
                );
            }
            else
            {
                context.Options["MinimalRebuild"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["MinimalRebuild"] = FileGeneratorUtilities.RemoveLineTag;
            }

            if (!clrSupport)
            {
                //Options.Vc.Compiler.RTTI.
                //    Disable                                 RuntimeTypeInfo="false"                         /GR-
                //    Enable                                  RuntimeTypeInfo="true"
                context.SelectOption
                (
                Options.Option(Options.Vc.Compiler.RTTI.Disable, () => { context.Options["RuntimeTypeInfo"] = "false"; context.CommandLineOptions["RuntimeTypeInfo"] = "/GR-"; }),
                Options.Option(Options.Vc.Compiler.RTTI.Enable, () => { context.Options["RuntimeTypeInfo"] = "true"; context.CommandLineOptions["RuntimeTypeInfo"] = "/GR"; })
                );
            }
            else
            {
                context.Options["RuntimeTypeInfo"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["RuntimeTypeInfo"] = FileGeneratorUtilities.RemoveLineTag;
            }

            //Options.Vc.Compiler.StructAlignment.
            //    Default                                 StructMemberAlignment="0"
            //    Alignment1                              StructMemberAlignment="1"                       /Zp1
            //    Alignment2                              StructMemberAlignment="2"                       /Zp2
            //    Alignment4                              StructMemberAlignment="3"                       /Zp4
            //    Alignment8                              StructMemberAlignment="4"                       /Zp8
            //    Alignment16                             StructMemberAlignment="5"                       /Zp16
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.StructAlignment.Default, () => { context.Options["StructMemberAlignment"] = "Default"; context.CommandLineOptions["StructMemberAlignment"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.StructAlignment.Alignment1, () => { context.Options["StructMemberAlignment"] = "1Byte"; context.CommandLineOptions["StructMemberAlignment"] = "/Zp1"; }),
            Options.Option(Options.Vc.Compiler.StructAlignment.Alignment2, () => { context.Options["StructMemberAlignment"] = "2Bytes"; context.CommandLineOptions["StructMemberAlignment"] = "/Zp2"; }),
            Options.Option(Options.Vc.Compiler.StructAlignment.Alignment4, () => { context.Options["StructMemberAlignment"] = "4Bytes"; context.CommandLineOptions["StructMemberAlignment"] = "/Zp4"; }),
            Options.Option(Options.Vc.Compiler.StructAlignment.Alignment8, () => { context.Options["StructMemberAlignment"] = "8Bytes"; context.CommandLineOptions["StructMemberAlignment"] = "/Zp8"; }),
            Options.Option(Options.Vc.Compiler.StructAlignment.Alignment16, () => { context.Options["StructMemberAlignment"] = "16Bytes"; context.CommandLineOptions["StructMemberAlignment"] = "/Zp16"; })
            );

            //Options.Vc.Compiler.BufferSecurityCheck.
            //    Enable                                  BufferSecurityCheck="true"
            //    Disable                                 BufferSecurityCheck="false"                     /GS-
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.BufferSecurityCheck.Enable, () => { context.Options["BufferSecurityCheck"] = "true"; context.CommandLineOptions["BufferSecurityCheck"] = "/GS"; }),
            Options.Option(Options.Vc.Compiler.BufferSecurityCheck.Disable, () => { context.Options["BufferSecurityCheck"] = "false"; context.CommandLineOptions["BufferSecurityCheck"] = "/GS-"; })
            );

            //Options.Vc.Compiler.OptimizeGlobalData.
            //    Disable                                 /Gw- in AdditionalOptions
            //    Enable                                  /Gw in AdditionalOptions
            if (context.Configuration.Platform.IsMicrosoft())
            {
                context.SelectOption
                (
                Options.Option(Options.Vc.Compiler.OptimizeGlobalData.Disable, () =>
                { /* do nothing */
                }),
                Options.Option(Options.Vc.Compiler.OptimizeGlobalData.Enable, () => { context.Configuration.AdditionalCompilerOptions.Add("/Gw"); })
                );
            }

            //Options.Vc.Compiler.FunctionLevelLinking.
            //    Disable                                 EnableFunctionLevelLinking="false"
            //    Enable                                  EnableFunctionLevelLinking="true"               /Gy
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.FunctionLevelLinking.Disable, () => { context.Options["EnableFunctionLevelLinking"] = "false"; context.CommandLineOptions["EnableFunctionLevelLinking"] = "/Gy-"; }),
            Options.Option(Options.Vc.Compiler.FunctionLevelLinking.Enable, () => { context.Options["EnableFunctionLevelLinking"] = "true"; context.CommandLineOptions["EnableFunctionLevelLinking"] = "/Gy"; })
            );

            //Options.Vc.Compiler.EnhancedInstructionSet.
            //    Disable                                 EnableEnhancedInstructionSet
            //    SIMD                                    EnableEnhancedInstructionSet                /arch:SSE
            //    SIMD2                                   EnableEnhancedInstructionSet                /arch:SSE2
            //    AdvancedVectorExtensions                EnableEnhancedInstructionSet                /arch:AVX
            //    NoEnhancedInstructions                  EnableEnhancedInstructionSet                /arch:IA32
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.EnhancedInstructionSet.Disable, () => { context.Options["EnableEnhancedInstructionSet"] = "NotSet"; context.CommandLineOptions["EnableEnhancedInstructionSet"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.EnhancedInstructionSet.SIMD, () => { context.Options["EnableEnhancedInstructionSet"] = "StreamingSIMDExtensions"; context.CommandLineOptions["EnableEnhancedInstructionSet"] = "/arch:SSE"; }),
            Options.Option(Options.Vc.Compiler.EnhancedInstructionSet.SIMD2, () => { context.Options["EnableEnhancedInstructionSet"] = "StreamingSIMDExtensions2"; context.CommandLineOptions["EnableEnhancedInstructionSet"] = "/arch:SSE2"; }),
            Options.Option(Options.Vc.Compiler.EnhancedInstructionSet.AdvancedVectorExtensions, () => { context.Options["EnableEnhancedInstructionSet"] = "AdvancedVectorExtensions"; context.CommandLineOptions["EnableEnhancedInstructionSet"] = "/arch:AVX"; }),
            Options.Option(Options.Vc.Compiler.EnhancedInstructionSet.AdvancedVectorExtensions2, () => { context.Options["EnableEnhancedInstructionSet"] = "AdvancedVectorExtensions2"; context.CommandLineOptions["EnableEnhancedInstructionSet"] = "/arch:AVX2"; }),
            Options.Option(Options.Vc.Compiler.EnhancedInstructionSet.NoEnhancedInstructions, () => { context.Options["EnableEnhancedInstructionSet"] = "NoExtensions"; context.CommandLineOptions["EnableEnhancedInstructionSet"] = "/arch:IA32"; })
            );

            //Options.Vc.Compiler.FloatingPointModel.
            //    Precise                                 FloatingPointModel="0"                          /fp:precise
            //    Strict                                  FloatingPointModel="1"                          /fp:strict
            //    Fast                                    FloatingPointModel="2"                          /fp:fast
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.FloatingPointModel.Precise, () => { context.Options["FloatingPointModel"] = "Precise"; context.CommandLineOptions["FloatingPointModel"] = "/fp:precise"; }),
            Options.Option(Options.Vc.Compiler.FloatingPointModel.Strict, () => { context.Options["FloatingPointModel"] = "Strict"; context.CommandLineOptions["FloatingPointModel"] = "/fp:strict"; }),
            Options.Option(Options.Vc.Compiler.FloatingPointModel.Fast, () => { context.Options["FloatingPointModel"] = "Fast"; context.CommandLineOptions["FloatingPointModel"] = "/fp:fast"; })
            );

            //Options.Vc.Compiler.FloatingPointExceptions.
            //    Disable                                 FloatingPointExceptions="false"
            //    Enable                                  FloatingPointExceptions="true"                  /fp:except
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.FloatingPointExceptions.Disable, () => { context.Options["FloatingPointExceptions"] = "false"; context.CommandLineOptions["FloatingPointExceptions"] = "/fp:except-"; }),
            Options.Option(Options.Vc.Compiler.FloatingPointExceptions.Enable, () => { context.Options["FloatingPointExceptions"] = "true"; context.CommandLineOptions["FloatingPointExceptions"] = "/fp:except"; })
            );

            // CreateHotpatchableImage = "false"
            // CreateHotpatchableImage = "true"     /hotpatch
            context.CommandLineOptions["CreateHotpatchableImage"] = FileGeneratorUtilities.RemoveLineTag;


            //Options.Vc.Compiler.CreateHotPatchableCode.
            //    Disable                                 CreateHotPatchableCode="false"
            //    Enable                                  CreateHotPatchableCode="true"                  /hotpatch
            context.SelectOption
            (
                Options.Option(Options.Vc.Compiler.CreateHotPatchableCode.Default, () => { context.Options["CreateHotPatchableCode"] = FileGeneratorUtilities.RemoveLineTag; ; context.CommandLineOptions["CreateHotPatchableCode"] = FileGeneratorUtilities.RemoveLineTag; ; }),
                Options.Option(Options.Vc.Compiler.CreateHotPatchableCode.Disable, () => { context.Options["CreateHotPatchableCode"] = "false"; context.CommandLineOptions["CreateHotPatchableCode"] = FileGeneratorUtilities.RemoveLineTag; ; }),
                Options.Option(Options.Vc.Compiler.CreateHotPatchableCode.Enable, () => { context.Options["CreateHotPatchableCode"] = "true"; context.CommandLineOptions["CreateHotPatchableCode"] = "/hotpatch"; })
            );

            //Options.Vc.Compiler.ConformanceMode.
            //    Disable                                 ConformanceMode="false"
            //    Enable                                  ConformanceMode="true"                          /permissive-
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.ConformanceMode.Disable, () => { context.Options["ConformanceMode"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["ConformanceMode"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.ConformanceMode.Enable, () => { context.Options["ConformanceMode"] = "true"; context.CommandLineOptions["ConformanceMode"] = "/permissive-"; })
            );

            //Options.Vc.Compiler.DisableLanguageExtensions.
            //    Disable                                 DisableLanguageExtensions="false"
            //    Enable                                  DisableLanguageExtensions="true"                /Za
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.DisableLanguageExtensions.Disable, () => { context.Options["DisableLanguageExtensions"] = "false"; context.CommandLineOptions["DisableLanguageExtensions"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.DisableLanguageExtensions.Enable, () => { context.Options["DisableLanguageExtensions"] = "true"; context.CommandLineOptions["DisableLanguageExtensions"] = "/Za"; })
            );

            //Options.Vc.Compiler.BuiltInWChartType.
            //    Disable                                 TreatWChar_tAsBuiltInType="false"               /Zc:wchar_t-
            //    Enable                                  TreatWChar_tAsBuiltInType="true"                /Zc:wchar_t
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.BuiltInWChartType.Disable, () => { context.Options["TreatWChar_tAsBuiltInType"] = "false"; context.CommandLineOptions["TreatWChar_tAsBuiltInType"] = "/Zc:wchar_t-"; }),
            Options.Option(Options.Vc.Compiler.BuiltInWChartType.Enable, () => { context.Options["TreatWChar_tAsBuiltInType"] = "true"; context.CommandLineOptions["TreatWChar_tAsBuiltInType"] = "/Zc:wchar_t"; })
            );

            //    Disable                                 RemoveUnreferencedCodeData="false"
            //    Enable                                  RemoveUnreferencedCodeData="true"                /Zc:inline
            if (!context.DevelopmentEnvironment.IsVisualStudio() || context.DevelopmentEnvironment < DevEnv.vs2013)
            {
                context.Options["RemoveUnreferencedCodeData"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["RemoveUnreferencedCodeData"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.SelectOption
                (
                Options.Option(Options.Vc.Compiler.RemoveUnreferencedCodeData.Disable, () =>
                {
                    context.Options["RemoveUnreferencedCodeData"] = "false";
                    context.CommandLineOptions["RemoveUnreferencedCodeData"] = FileGeneratorUtilities.RemoveLineTag;
                }),
                Options.Option(Options.Vc.Compiler.RemoveUnreferencedCodeData.Enable, () =>
                {
                    context.Options["RemoveUnreferencedCodeData"] = FileGeneratorUtilities.RemoveLineTag;
                    context.CommandLineOptions["RemoveUnreferencedCodeData"] = "/Zc:inline";
                })
                );
            }

            //Options.Vc.Compiler.ForceLoopScope.
            //    Disable                                 ForceConformanceInForLoopScope="false"          /Zc:forScope-
            //    Enable                                  ForceConformanceInForLoopScope="true"           /Zc:forScope
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.ForceLoopScope.Disable, () => { context.Options["ForceConformanceInForLoopScope"] = "false"; context.CommandLineOptions["ForceConformanceInForLoopScope"] = "/Zc:forScope-"; }),
            Options.Option(Options.Vc.Compiler.ForceLoopScope.Enable, () => { context.Options["ForceConformanceInForLoopScope"] = "true"; context.CommandLineOptions["ForceConformanceInForLoopScope"] = "/Zc:forScope"; })
            );

            //Options.Vc.Compiler.OpenMP.   
            //    Disable                                 OpenMP="false"                                  /openmp-
            //    Enable                                  OpenMP="true"                                   /openmp
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.OpenMP.Disable, () => { context.Options["OpenMP"] = "false"; context.CommandLineOptions["OpenMP"] = "/openmp-"; }),
            Options.Option(Options.Vc.Compiler.OpenMP.Enable, () => { context.Options["OpenMP"] = "true"; context.CommandLineOptions["OpenMP"] = "/openmp"; })
            );

            //Options.Vc.Compiler.GenerateXMLDocumentation.   
            //    Disable                                 GenerateXMLDocumentation="false"                                  
            //    Enable                                  GenerateXMLDocumentation="true"                                   /openmp
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.GenerateXMLDocumentation.Disable, () => { context.Options["GenerateXMLDocumentation"] = "false"; context.CommandLineOptions["GenerateXMLDocumentation"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.GenerateXMLDocumentation.Enable, () => { context.Options["GenerateXMLDocumentation"] = "true"; context.CommandLineOptions["GenerateXMLDocumentation"] = @"/doc""[project.RootPath]"""; })
            );

            //Options.Vc.Compiler.PrecompiledHeader
            //      NotUsingPrecompiledHeaders          UsePrecompiledHeader="0"
            //      CreatePrecompiledHeader             UsePrecompiledHeader="1"                            /Yc
            //      UsePrecompiledHeader                UsePrecompiledHeader="2"                            /Yu
            SelectPrecompiledHeaderOption(context, optionsContext);

            //Options.Vc.Compiler.CallingConvention.
            //    cdecl                                   CallingConvention="0"                           /Gd
            //    fastcall                                CallingConvention="1"                           /Gr
            //    stdcall                                 CallingConvention="2"                           /Gz
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.CallingConvention.cdecl, () => { context.Options["CallingConvention"] = "Cdecl"; context.CommandLineOptions["CallingConvention"] = "/Gd"; }),
            Options.Option(Options.Vc.Compiler.CallingConvention.fastcall, () => { context.Options["CallingConvention"] = "FastCall"; context.CommandLineOptions["CallingConvention"] = "/Gr"; }),
            Options.Option(Options.Vc.Compiler.CallingConvention.stdcall, () => { context.Options["CallingConvention"] = "StdCall"; context.CommandLineOptions["CallingConvention"] = "/Gz"; })
            );

            //Options.Vc.Compiler.ShowIncludes.
            //    Disable                               ShowIncludes="false"
            //    Enable                                ShowIncludes="true"                           /showIncludes
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.ShowIncludes.Disable, () => { context.Options["ShowIncludes"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.ShowIncludes.Enable, () => { context.Options["ShowIncludes"] = "true"; })
            );

            // '/JMC' and '/clr' command-line options are incompatible
            if (!clrSupport)
            {
                //Options.Vc.Compiler.SupportJustMyCode.
                //    Yes                                   SupportJustMyCode="true"                          /JMC
                //    No
                context.SelectOption
                (
                Options.Option(Options.Vc.Compiler.SupportJustMyCode.Default, () => { context.Options["SupportJustMyCode"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["SupportJustMyCode"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.Compiler.SupportJustMyCode.No, () =>
                {
                    if (context.DevelopmentEnvironment >= DevEnv.vs2017)
                    {
                        context.Options["SupportJustMyCode"] = "false";
                        context.CommandLineOptions["SupportJustMyCode"] = "/JMC-";
                    }
                    else
                    {
                        context.Options["SupportJustMyCode"] = FileGeneratorUtilities.RemoveLineTag;
                        context.CommandLineOptions["SupportJustMyCode"] = FileGeneratorUtilities.RemoveLineTag;
                    }
                }),
                Options.Option(Options.Vc.Compiler.SupportJustMyCode.Yes, () => { context.Options["SupportJustMyCode"] = "true"; context.CommandLineOptions["SupportJustMyCode"] = "/JMC"; })
                );
            }
            else
            {
                context.Options["SupportJustMyCode"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["SupportJustMyCode"] = FileGeneratorUtilities.RemoveLineTag;
            }

            //Options.Vc.Compiler.SpectreMitigation.
            //    Default
            //    Enabled                               SpectreMitigation="Spectre"                       /Qspectre
            //    Disabled                              SpectreMitigation="false"
            context.SelectOption
            (
            Options.Option(Options.Vc.Compiler.SpectreMitigation.Default, () => { context.Options["SpectreMitigation"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["SpectreMitigation"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Compiler.SpectreMitigation.Enabled, () => { context.Options["SpectreMitigation"] = "Spectre"; context.CommandLineOptions["SpectreMitigation"] = "/Qspectre"; }),
            Options.Option(Options.Vc.Compiler.SpectreMitigation.Disabled, () => { context.Options["SpectreMitigation"] = "false"; context.CommandLineOptions["SpectreMitigation"] = FileGeneratorUtilities.RemoveLineTag; })
            );

            if (context.DevelopmentEnvironment == DevEnv.vs2017 || context.DevelopmentEnvironment == DevEnv.vs2019)
            {
                //Options.Vc.Compiler.DefineCPlusPlus. See: https://devblogs.microsoft.com/cppblog/msvc-now-correctly-reports-__cplusplus/
                //    Disable                                 /Zc:__cplusplus-
                //    Enable                                  /Zc:__cplusplus
                if (!Options.GetObject<Options.Vc.General.PlatformToolset>(context.Configuration).IsLLVMToolchain() ||
                        Options.GetObject<Options.Vc.LLVM.UseClangCl>(context.Configuration) != Options.Vc.LLVM.UseClangCl.Enable)
                {
                    if (!context.Configuration.Platform.IsUsingClang())
                    {
                        context.SelectOption
                        (
                        Options.Option(Options.Vc.Compiler.DefineCPlusPlus.Default, () => { }),
                        Options.Option(Options.Vc.Compiler.DefineCPlusPlus.Disable, () => { context.Configuration.AdditionalCompilerOptions.Add("/Zc:__cplusplus-"); }),
                        Options.Option(Options.Vc.Compiler.DefineCPlusPlus.Enable, () => { context.Configuration.AdditionalCompilerOptions.Add("/Zc:__cplusplus"); })
                        );
                    }
                }
            }

            // Options.Vc.Compiler.DisableSpecificWarnings
            Strings disableWarnings = Options.GetStrings<Options.Vc.Compiler.DisableSpecificWarnings>(context.Configuration);
            if (disableWarnings.Count > 0)
            {
                StringBuilder result = new StringBuilder();
                foreach (string disableWarning in disableWarnings.SortedValues)
                    result.Append(@"/wd""" + disableWarning + @""" ");
                result.Remove(result.Length - 1, 1);
                context.Options["DisableSpecificWarnings"] = disableWarnings.JoinStrings(";");
                context.CommandLineOptions["DisableSpecificWarnings"] = result.ToString();
            }
            else
            {
                context.Options["DisableSpecificWarnings"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["DisableSpecificWarnings"] = FileGeneratorUtilities.RemoveLineTag;
            }

            // Options.Vc.Compiler.UndefinePreprocessorDefinitions
            Strings undefinePreprocessors = Options.GetStrings<Options.Vc.Compiler.UndefinePreprocessorDefinitions>(context.Configuration);
            if (undefinePreprocessors.Count > 0)
            {
                context.Options["UndefinePreprocessorDefinitions"] = undefinePreprocessors.JoinStrings(";");

                StringBuilder result = new StringBuilder();
                foreach (string undefine in undefinePreprocessors)
                    result.Append(@"/U""" + undefine + @""" ");
                result.Remove(result.Length - 1, 1);
                context.CommandLineOptions["UndefinePreprocessorDefinitions"] = result.ToString();
            }
            else
            {
                context.Options["UndefinePreprocessorDefinitions"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["UndefinePreprocessorDefinitions"] = FileGeneratorUtilities.RemoveLineTag;
            }

            // UndefineAllPreprocessorDefinitions
            context.CommandLineOptions["UndefineAllPreprocessorDefinitions"] = FileGeneratorUtilities.RemoveLineTag;

            // Default defines...
            optionsContext.PlatformVcxproj.SelectCompilerOptions(context);

            // Options.Vc.Compiler.AdditionalOptions
            if (context.Configuration.AdditionalCompilerOptions.Any())
            {
                context.Configuration.AdditionalCompilerOptions.Sort();
                string additionalCompilerOptions = context.Configuration.AdditionalCompilerOptions.JoinStrings(" ");
                context.Options["AdditionalCompilerOptions"] = additionalCompilerOptions;
            }
            else
            {
                context.Options["AdditionalCompilerOptions"] = FileGeneratorUtilities.RemoveLineTag;
            }

            optionsContext.HasClrSupport = clrSupport;
        }

        public static List<KeyValuePair<string, string>> ConvertPostBuildCopiesToRelative(Project.Configuration conf, string relativeTo)
        {
            var relativePostBuildCopies = new List<KeyValuePair<string, string>>();
            if (!conf.ResolvedTargetCopyFiles.Any() && conf.CopyDependenciesBuildStep == null && !conf.EventPostBuildCopies.Any())
                return relativePostBuildCopies;

            relativePostBuildCopies.AddRange(conf.ResolvedTargetCopyFiles.Select(x => new KeyValuePair<string, string>(x, conf.TargetPath)));
            relativePostBuildCopies.AddRange(conf.EventPostBuildCopies);

            for (int i = 0; i < relativePostBuildCopies.Count;)
            {
                string sourceFileFullPath = relativePostBuildCopies[i].Key;
                string dstDir = relativePostBuildCopies[i].Value;

                // discard if the source is already in the destination folder
                string sourceFileDirectory = Path.GetDirectoryName(sourceFileFullPath);
                if (string.Compare(sourceFileDirectory, dstDir, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    relativePostBuildCopies.RemoveAt(i);
                    continue;
                }

                // keep the full path for the source if outside of the global root
                string sourcePath;
                if (sourceFileFullPath.StartsWith(conf.Project.RootPath, StringComparison.OrdinalIgnoreCase))
                    sourcePath = Util.PathGetRelative(relativeTo, sourceFileFullPath, true);
                else
                    sourcePath = sourceFileFullPath;

                string relativeDstDir = Util.PathGetRelative(relativeTo, dstDir);
                relativePostBuildCopies[i] = new KeyValuePair<string, string>(sourcePath, relativeDstDir);

                ++i;
            }

            return relativePostBuildCopies;
        }

        private static void SelectDebugInformationOption(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            // win64 don't support /ZI which is the default one, forward it to /Zi
            if (optionsContext.PlatformVcxproj.HasEditAndContinueDebuggingSupport)
            {
                context.SelectOption
                (
                Options.Option(Options.Vc.General.DebugInformation.Disable, () => { context.Options["DebugInformationFormat"] = "None"; context.CommandLineOptions["DebugInformationFormat"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.General.DebugInformation.C7Compatible, () => { context.Options["DebugInformationFormat"] = "OldStyle"; context.CommandLineOptions["DebugInformationFormat"] = "/Z7"; }),
                Options.Option(Options.Vc.General.DebugInformation.ProgramDatabase, () => { context.Options["DebugInformationFormat"] = "ProgramDatabase"; context.CommandLineOptions["DebugInformationFormat"] = "/Zi"; }),
                Options.Option(Options.Vc.General.DebugInformation.ProgramDatabaseEnC, () => { context.Options["DebugInformationFormat"] = "EditAndContinue"; context.CommandLineOptions["DebugInformationFormat"] = "/ZI"; })
                );
            }
            else
            {
                context.SelectOption
                (
                Options.Option(Options.Vc.General.DebugInformation.Disable, () => { context.Options["DebugInformationFormat"] = "None"; context.CommandLineOptions["DebugInformationFormat"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.General.DebugInformation.C7Compatible, () => { context.Options["DebugInformationFormat"] = "OldStyle"; context.CommandLineOptions["DebugInformationFormat"] = "/Z7"; }),
                Options.Option(Options.Vc.General.DebugInformation.ProgramDatabase, () => { context.Options["DebugInformationFormat"] = "ProgramDatabase"; context.CommandLineOptions["DebugInformationFormat"] = "/Zi"; }),
                Options.Option(Options.Vc.General.DebugInformation.ProgramDatabaseEnC, () => { context.Options["DebugInformationFormat"] = "ProgramDatabase"; context.CommandLineOptions["DebugInformationFormat"] = "/Zi"; })
                );
            }
        }

        private static void SelectPreferredToolArchitecture(IGenerationContext context)
        {
            switch (context.DevelopmentEnvironment)
            {
                case DevEnv.vs2010:
                case DevEnv.vs2012:
                    {
                        // Falling back to <_IsNativeEnvironment> that has same effect
                        context.Options["PreferredToolArchitecture"] = FileGeneratorUtilities.RemoveLineTag;
                        context.SelectOption
                        (
                        Options.Option(Options.Vc.General.PreferredToolArchitecture.Default, () => { context.Options["_IsNativeEnvironment"] = FileGeneratorUtilities.RemoveLineTag; }),
                        Options.Option(Options.Vc.General.PreferredToolArchitecture.x86, () => { context.Options["_IsNativeEnvironment"] = "false"; }),
                        Options.Option(Options.Vc.General.PreferredToolArchitecture.x64, () => { context.Options["_IsNativeEnvironment"] = "true"; })
                        );
                    }
                    break;
                case DevEnv.vs2013:
                case DevEnv.vs2015:
                case DevEnv.vs2017:
                case DevEnv.vs2019:
                    {
                        context.Options["_IsNativeEnvironment"] = FileGeneratorUtilities.RemoveLineTag;
                        context.SelectOption
                        (
                        Options.Option(Options.Vc.General.PreferredToolArchitecture.Default, () => { context.Options["PreferredToolArchitecture"] = FileGeneratorUtilities.RemoveLineTag; }),
                        Options.Option(Options.Vc.General.PreferredToolArchitecture.x86, () => { context.Options["PreferredToolArchitecture"] = "x86"; }),
                        Options.Option(Options.Vc.General.PreferredToolArchitecture.x64, () => { context.Options["PreferredToolArchitecture"] = "x64"; })
                        );
                    }
                    break;
            }
        }

        private static void SelectPlatformToolsetOption(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            context.SelectOption
            (
                Options.Option(Options.Vc.General.PlatformToolset.Default, () => { context.Options["PlatformToolset"] = context.DevelopmentEnvironment.GetDefaultPlatformToolset(); }),
                Options.Option(Options.Vc.General.PlatformToolset.v100, () => { context.Options["PlatformToolset"] = "v100"; }),
                Options.Option(Options.Vc.General.PlatformToolset.v110, () => { context.Options["PlatformToolset"] = "v110"; }),
                Options.Option(Options.Vc.General.PlatformToolset.v110_xp, () => { context.Options["PlatformToolset"] = "v110_xp"; }),
                Options.Option(Options.Vc.General.PlatformToolset.v120, () => { context.Options["PlatformToolset"] = "v120"; }),
                Options.Option(Options.Vc.General.PlatformToolset.v120_xp, () => { context.Options["PlatformToolset"] = "v120_xp"; }),
                Options.Option(Options.Vc.General.PlatformToolset.v140, () => { context.Options["PlatformToolset"] = "v140"; }),
                Options.Option(Options.Vc.General.PlatformToolset.v140_xp, () => { context.Options["PlatformToolset"] = "v140_xp"; }),
                Options.Option(Options.Vc.General.PlatformToolset.v141, () => { context.Options["PlatformToolset"] = "v141"; }),
                Options.Option(Options.Vc.General.PlatformToolset.v141_xp, () => { context.Options["PlatformToolset"] = "v141_xp"; }),
                Options.Option(Options.Vc.General.PlatformToolset.v142, () => { context.Options["PlatformToolset"] = "v142"; }),
                Options.Option(Options.Vc.General.PlatformToolset.LLVM_vs2012, () => { context.Options["PlatformToolset"] = "LLVM-vs2012"; context.Options["TrackFileAccess"] = "false"; }),
                Options.Option(Options.Vc.General.PlatformToolset.LLVM_vs2014, () => { context.Options["PlatformToolset"] = "LLVM-vs2014"; }),
                Options.Option(Options.Vc.General.PlatformToolset.LLVM, () => { context.Options["PlatformToolset"] = "llvm"; })
            );
            optionsContext.PlatformVcxproj.SetupPlatformToolsetOptions(context);
        }

        private static void SelectPrecompiledHeaderOption(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            if (!optionsContext.PlatformVcxproj.HasPrecomp(context))
            {
                context.Options["UsePrecompiledHeader"] = "NotUsing";
                context.Options["PrecompiledHeaderThrough"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PrecompiledHeaderFile"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PrecompiledHeaderOutputFileDirectory"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["PrecompiledHeaderThrough"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["PrecompiledHeaderFile"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.Options["UsePrecompiledHeader"] = "Use";
                context.Options["PrecompiledHeaderThrough"] = context.Configuration.PrecompHeader;
                string pchOutputDirectoryRelative = string.IsNullOrEmpty(context.Configuration.PrecompHeaderOutputFolder) ? optionsContext.IntermediateDirectoryRelative : Util.PathGetRelative(context.ProjectDirectory, context.Configuration.PrecompHeaderOutputFolder);
                context.Options["PrecompiledHeaderFile"] = Path.Combine(pchOutputDirectoryRelative, $"{context.Configuration.Project.Name}.pch");
                context.Options["PrecompiledHeaderOutputFileDirectory"] = pchOutputDirectoryRelative;
                context.CommandLineOptions["PrecompiledHeaderThrough"] = context.Options["PrecompiledHeaderThrough"];
                context.CommandLineOptions["PrecompiledHeaderFile"] = FormatCommandLineOptionPath(context, context.Options["PrecompiledHeaderFile"]);

                if (!optionsContext.PlatformDescriptor.HasPrecompiledHeaderSupport)
                    throw new Error("Precompiled header not supported for spu configuration: {0}", context.Configuration);
            }
        }

        private void GenerateLibrarianOptions(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            context.SelectOption
            (
            Options.Option(Options.Vc.Librarian.TreatLibWarningAsErrors.Disable, () => { context.Options["TreatLibWarningAsErrors"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["TreatLibWarningAsErrors"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Librarian.TreatLibWarningAsErrors.Enable, () => { context.Options["TreatLibWarningAsErrors"] = "true"; context.CommandLineOptions["TreatLibWarningAsErrors"] = "/WX"; })
            );
        }

        private void GenerateLinkerOptions(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            string outputExtension = context.Configuration.OutputExtension;
            if (outputExtension.Length > 0 && !outputExtension.StartsWith(".", StringComparison.Ordinal))
                outputExtension = outputExtension.Insert(0, ".");

            string outputFileName = optionsContext.PlatformVcxproj.GetOutputFileNamePrefix(context, context.Configuration.Output) + optionsContext.TargetName;

            context.Options["ImportLibrary"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["ImportLibrary"] = FileGeneratorUtilities.RemoveLineTag;
            context.Options["OutputFileName"] = outputFileName;
            context.Options["OutputFileExtension"] = outputExtension;

            context.Options["AdditionalDeploymentFolders"] = "";

            switch (context.Configuration.Output)
            {
                case Project.Configuration.OutputType.Dll:
                case Project.Configuration.OutputType.DotNetClassLibrary:
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.DotNetConsoleApp:
                case Project.Configuration.OutputType.DotNetWindowsApp:
                case Project.Configuration.OutputType.IosApp:
                case Project.Configuration.OutputType.IosTestBundle:
                    context.Options["OutputFile"] = optionsContext.OutputDirectoryRelative + Util.WindowsSeparator + outputFileName + outputExtension;
                    if (context.Configuration.Output == Project.Configuration.OutputType.Dll)
                    {
                        string importLibRelative = optionsContext.OutputLibraryDirectoryRelative + Util.WindowsSeparator + optionsContext.TargetName + ".lib";
                        context.Options["ImportLibrary"] = importLibRelative;
                        context.CommandLineOptions["ImportLibrary"] = "/IMPLIB:" + FormatCommandLineOptionPath(context, importLibRelative);
                    }
                    break;
                case Project.Configuration.OutputType.Lib:
                    context.Options["OutputFile"] = optionsContext.OutputLibraryDirectoryRelative + Util.WindowsSeparator + outputFileName + outputExtension;
                    break;
                case Project.Configuration.OutputType.Utility:
                case Project.Configuration.OutputType.None:
                    context.Options["OutputFile"] = FileGeneratorUtilities.RemoveLineTag;
                    context.Options["OutputFileExtension"] = FileGeneratorUtilities.RemoveLineTag;
                    context.Options["OutputFileName"] = FileGeneratorUtilities.RemoveLineTag;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //ShowProgress
            //    NotSet                                  ShowProgress="0"
            //    DisplayAllProgressMessages              ShowProgress="1"                            /VERBOSE
            //    DisplaysSomeProgressMessages            ShowProgress="2"                            /VERBOSE:LIB
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.ShowProgress.NotSet, () => { context.Options["ShowProgress"] = "NotSet"; context.CommandLineOptions["ShowProgress"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.ShowProgress.DisplayAllProgressMessages, () => { context.Options["ShowProgress"] = "LinkVerbose"; context.CommandLineOptions["ShowProgress"] = "/VERBOSE"; }),
            Options.Option(Options.Vc.Linker.ShowProgress.DisplaysSomeProgressMessages, () => { context.Options["ShowProgress"] = "LinkVerboseLib"; context.CommandLineOptions["ShowProgress"] = "/VERBOSE:Lib"; })
            );

            //Incremental
            //    Default                                 LinkIncremental="0"
            //    Disable                                 LinkIncremental="1"                         /INCREMENTAL:NO
            //    Enable                                  LinkIncremental="2"                         /INCREMENTAL
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.Incremental.Default, () => { context.Options["LinkIncremental"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["LinkIncremental"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.Incremental.Disable, () => { context.Options["LinkIncremental"] = "false"; context.CommandLineOptions["LinkIncremental"] = "/INCREMENTAL:NO"; }),
            Options.Option(Options.Vc.Linker.Incremental.Enable, () => { context.Options["LinkIncremental"] = "true"; context.CommandLineOptions["LinkIncremental"] = "/INCREMENTAL"; })
            );

            //EmbedManifest
            //    Yes                                 EmbedManifest="true"
            //    No                                  EmbedManifest="false"
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.EmbedManifest.Default, () => { context.Options["EmbedManifest"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.EmbedManifest.Yes, () => { context.Options["EmbedManifest"] = "true"; }),
            Options.Option(Options.Vc.Linker.EmbedManifest.No, () => { context.Options["EmbedManifest"] = "false"; })
            );

            //SuppressStartupBanner
            //    Disable                                 SuppressStartupBanner="false"
            //    Enable                                  SuppressStartupBanner="true"                /NOLOGO
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.SuppressStartupBanner.Disable, () => { context.Options["SuppressStartupBanner"] = "false"; context.CommandLineOptions["LinkerSuppressStartupBanner"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.SuppressStartupBanner.Enable, () => { context.Options["SuppressStartupBanner"] = "true"; context.CommandLineOptions["LinkerSuppressStartupBanner"] = "/NOLOGO"; })
            );

            //LinkLibraryDependencies
            //    Enable                                  LinkLibraryDependencies="true"
            //    Disable                                 LinkLibraryDependencies="false"
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.LinkLibraryDependencies.Default, () => { context.Options["LinkLibraryDependencies"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.LinkLibraryDependencies.Enable, () => { context.Options["LinkLibraryDependencies"] = "true"; }),
            Options.Option(Options.Vc.Linker.LinkLibraryDependencies.Disable, () => { context.Options["LinkLibraryDependencies"] = "false"; })
            );

            //ReferenceOutputAssembly
            //    Enable                                  ReferenceOutputAssembly="true"
            //    Disable                                 ReferenceOutputAssembly="false"
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.ReferenceOutputAssembly.Default, () => { context.Options["ReferenceOutputAssembly"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.ReferenceOutputAssembly.Enable, () => { context.Options["ReferenceOutputAssembly"] = "true"; }),
            Options.Option(Options.Vc.Linker.ReferenceOutputAssembly.Disable, () => { context.Options["ReferenceOutputAssembly"] = "false"; })
            );

            //CopyLocalSatelliteAssemblies
            //    Enable                                  CopyLocalSatelliteAssemblies="true"
            //    Disable                                 CopyLocalSatelliteAssemblies="false"
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.CopyLocalSatelliteAssemblies.Default, () => { context.Options["CopyLocalSatelliteAssemblies"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.CopyLocalSatelliteAssemblies.Enable, () => { context.Options["CopyLocalSatelliteAssemblies"] = "true"; }),
            Options.Option(Options.Vc.Linker.CopyLocalSatelliteAssemblies.Disable, () => { context.Options["CopyLocalSatelliteAssemblies"] = "false"; })
            );

            //IgnoreImportLibrary
            //    Enable                                  IgnoreImportLibrary="true"
            //    Disable                                 IgnoreImportLibrary="false"
            if (context.Configuration.Target.GetFragment<DevEnv>() == DevEnv.vs2010)
            {
                context.Options["IgnoreImportLibrary"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.SelectOption
                (
                Options.Option(Options.Vc.Linker.IgnoreImportLibrary.Enable, () => { context.Options["IgnoreImportLibrary"] = "true"; }),
                Options.Option(Options.Vc.Linker.IgnoreImportLibrary.Disable, () => { context.Options["IgnoreImportLibrary"] = "false"; })
                );
            }

            //RunCodeAnalysis
            //    Enable                                  RunCodeAnalysis="true"
            //    Disable                                 RunCodeAnalysis="false"
            if (context.Configuration.Target.GetFragment<DevEnv>() == DevEnv.vs2010)
            {
                context.Options["RunCodeAnalysis"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.SelectOption
                (
                Options.Option(Options.Vc.CodeAnalysis.RunCodeAnalysis.Enable, () => { context.Options["RunCodeAnalysis"] = "true"; }),
                Options.Option(Options.Vc.CodeAnalysis.RunCodeAnalysis.Disable, () => { context.Options["RunCodeAnalysis"] = FileGeneratorUtilities.RemoveLineTag; })
                );
            }

            //UseLibraryDependencyInputs
            //    Enable                                  UseLibraryDependencyInputs="true"
            //    Disable                                 UseLibraryDependencyInputs="false"
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.UseLibraryDependencyInputs.Default, () => { context.Options["UseLibraryDependencyInputs"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.UseLibraryDependencyInputs.Enable, () => { context.Options["UseLibraryDependencyInputs"] = "true"; }),
            Options.Option(Options.Vc.Linker.UseLibraryDependencyInputs.Disable, () => { context.Options["UseLibraryDependencyInputs"] = "false"; })
            );

            //DisableFastUpToDateCheck
            //    Enable                                  DisableFastUpToDateCheck="true"
            //    Disable                                 DisableFastUpToDateCheck="false"
            context.SelectOption
            (
            Options.Option(Options.Vc.General.DisableFastUpToDateCheck.Enable, () => { context.Options["DisableFastUpToDateCheck"] = "true"; }),
            Options.Option(Options.Vc.General.DisableFastUpToDateCheck.Disable, () => { context.Options["DisableFastUpToDateCheck"] = FileGeneratorUtilities.RemoveLineTag; })
            );

            //EnableManagedIncrementalBuild
            context.SelectOption
            (
            Options.Option(Options.Vc.General.EnableManagedIncrementalBuild.Enable, () => { context.Options["EnableManagedIncrementalBuild"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.General.EnableManagedIncrementalBuild.Disable, () => { context.Options["EnableManagedIncrementalBuild"] = "false"; })
            );

            //RandomizedBaseAddress
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.RandomizedBaseAddress.Default, () => { context.Options["RandomizedBaseAddress"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["RandomizedBaseAddress"] = "/DYNAMICBASE"; }),
            Options.Option(Options.Vc.Linker.RandomizedBaseAddress.Enable, () => { context.Options["RandomizedBaseAddress"] = "true"; context.CommandLineOptions["RandomizedBaseAddress"] = "/DYNAMICBASE"; }),
            Options.Option(Options.Vc.Linker.RandomizedBaseAddress.Disable, () => { context.Options["RandomizedBaseAddress"] = "false"; context.CommandLineOptions["RandomizedBaseAddress"] = "/DYNAMICBASE:NO"; })
            );

            // Delay Loaded DLLs
            Strings delayedDLLs = Options.GetStrings<Options.Vc.Linker.DelayLoadDLLs>(context.Configuration);
            if (delayedDLLs.Any())
            {
                context.Options["DelayLoadedDLLs"] = delayedDLLs.JoinStrings(";");

                StringBuilder result = new StringBuilder();
                foreach (string delayedDLL in delayedDLLs)
                    result.Append(@"/DELAYLOAD:""" + delayedDLL + @""" ");
                result.Remove(result.Length - 1, 1);
                context.CommandLineOptions["DelayLoadedDLLs"] = result.ToString();
            }
            else
            {
                context.Options["DelayLoadedDLLs"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["DelayLoadedDLLs"] = FileGeneratorUtilities.RemoveLineTag;
            }

            // Set module definition
            if (!string.IsNullOrEmpty(context.Configuration.ModuleDefinitionFile))
            {
                var filePath = Util.PathGetRelative(context.ProjectDirectory, context.Configuration.ModuleDefinitionFile);
                context.Options["ModuleDefinitionFile"] = filePath;
                context.CommandLineOptions["ModuleDefinitionFile"] = "/DEF:" + FormatCommandLineOptionPath(context, filePath);
            }
            else
            {
                context.Options["ModuleDefinitionFile"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["ModuleDefinitionFile"] = FileGeneratorUtilities.RemoveLineTag;
            }

            //IgnoreAllDefaultLibraries
            //    Enable                                  IgnoreAllDefaultLibraries="true"        /NODEFAULTLIB
            //    Disable                                 IgnoreAllDefaultLibraries="false"
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.IgnoreAllDefaultLibraries.Enable, () => { context.Options["IgnoreAllDefaultLibraries"] = "true"; context.CommandLineOptions["IgnoreAllDefaultLibraries"] = "/NODEFAULTLIB"; }),
            Options.Option(Options.Vc.Linker.IgnoreAllDefaultLibraries.Disable, () => { context.Options["IgnoreAllDefaultLibraries"] = "false"; context.CommandLineOptions["IgnoreAllDefaultLibraries"] = FileGeneratorUtilities.RemoveLineTag; })
            );

            //GenerateManifest
            //    Enable                                  GenerateManifest="true"                 /MANIFEST
            //    Disable                                 GenerateManifest="false"
            SelectGenerateManifestOption(context, optionsContext);

            SelectGenerateDebugInformationOption(context, optionsContext);

            // GenerateMapFile
            SelectGenerateMapFileOption(context, optionsContext);

            //MapExports
            //    Enable                                  MapExports="true"                       /MAPINFO:EXPORTS
            //    Disable                                 MapExports="false"
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.MapExports.Enable, () => { context.Options["MapExports"] = "true"; context.CommandLineOptions["MapExports"] = "/MAPINFO:EXPORTS"; }),
            Options.Option(Options.Vc.Linker.MapExports.Disable, () => { context.Options["MapExports"] = "false"; context.CommandLineOptions["MapExports"] = FileGeneratorUtilities.RemoveLineTag; })
            );

            //AssemblyDebug
            //    NoDebuggableAttributeEmitted            AssemblyDebug="0"
            //    RuntimeTrackingAndDisableOptimizations  AssemblyDebug="1"                       /ASSEMBLYDEBUG
            //    NoRuntimeTrackingAndEnableOptimizations  AssemblyDebug="2"                       /ASSEMBLYDEBUG:DISABLE
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.AssemblyDebug.NoDebuggableAttributeEmitted, () => { context.Options["AssemblyDebug"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["AssemblyDebug"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.AssemblyDebug.RuntimeTrackingAndDisableOptimizations, () => { context.Options["AssemblyDebug"] = "true"; context.CommandLineOptions["AssemblyDebug"] = "/ASSEMBLYDEBUG"; }),
            Options.Option(Options.Vc.Linker.AssemblyDebug.NoRuntimeTrackingAndEnableOptimizations, () => { context.Options["AssemblyDebug"] = "false"; context.CommandLineOptions["AssemblyDebug"] = "/ASSEMBLYDEBUG:DISABLE"; })
            );

            //SubSystem
            //    Console                                 SubSystem="1"                           /SUBSYSTEM:CONSOLE
            //    Windows                                 SubSystem="2"                           /SUBSYSTEM:WINDOWS
            //    Native                                  SubSystem="3"                           /SUBSYSTEM:NATIVE
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.SubSystem.Console, () => { context.Options["SubSystem"] = "Console"; context.CommandLineOptions["SubSystem"] = "/SUBSYSTEM:CONSOLE"; }),
            Options.Option(Options.Vc.Linker.SubSystem.Application, () => { context.Options["SubSystem"] = "Windows"; context.CommandLineOptions["SubSystem"] = "/SUBSYSTEM:WINDOWS"; }),
            Options.Option(Options.Vc.Linker.SubSystem.Native, () => { context.Options["SubSystem"] = "Native"; context.CommandLineOptions["SubSystem"] = "/SUBSYSTEM:NATIVE"; })
            );


            //HeapSize
            //HeapReserveSize
            //                                            HeapReserveSize="0"                     /HEAP:reserve
            //HeapCommitSize
            //                                            HeapCommitSize="0"                      /HEAP:reserve,commit
            Options.Vc.Linker.HeapSize heap = Options.GetObject<Options.Vc.Linker.HeapSize>(context.Configuration);
            if (heap == null)
            {
                context.Options["HeapReserveSize"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["HeapCommitSize"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["HeapReserveSize"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["HeapCommitSize"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.Options["HeapReserveSize"] = heap.ReserveSize.ToString();
                context.Options["HeapCommitSize"] = heap.CommintSize.ToString();
                context.CommandLineOptions["HeapReserveSize"] = "/HEAP:reserve";
                context.CommandLineOptions["HeapCommitSize"] = "/HEAP:reserve,commit";
            }

            //StackSize
            //StackReserveSize
            //                                            StackReserveSize="0"                    /STACK:reserve
            //StackCommitSize
            //                                            StackCommitSize="0"                     /STACK:reserve,commit
            Options.Vc.Linker.StackSize stack = Options.GetObject<Options.Vc.Linker.StackSize>(context.Configuration);
            if (stack == null)
            {
                context.Options["StackReserveSize"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["StackCommitSize"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["StackReserveSize"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["StackCommitSize"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.Options["StackReserveSize"] = stack.ReserveSize.ToString();
                context.Options["StackCommitSize"] = stack.CommintSize.ToString();
                if (stack.CommintSize > 0)
                {
                    context.CommandLineOptions["StackReserveSize"] = FileGeneratorUtilities.RemoveLineTag;
                    context.CommandLineOptions["StackCommitSize"] = "/STACK:" + stack.ReserveSize + "," + stack.CommintSize;
                }
                else
                {
                    context.CommandLineOptions["StackReserveSize"] = "/STACK:" + stack.ReserveSize;
                    context.CommandLineOptions["StackCommitSize"] = FileGeneratorUtilities.RemoveLineTag;
                }
            }

            //AllowIsolation
            //    Disabled                                AllowIsolation=false                    /ALLOWISOLATION:NO
            //    Enable                                  AllowIsolation=true                     /ALLOWISOLATION
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.AllowIsolation.Enabled, () => { context.Options["AllowIsolation"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["AllowIsolation"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.AllowIsolation.Disabled, () => { context.Options["AllowIsolation"] = "false"; context.CommandLineOptions["AllowIsolation"] = "/ALLOWISOLATION:NO"; })
            );

            //LargeAddress
            //    Default                                 LargeAddressAware="0"
            //    NotSupportLargerThan2Gb                 LargeAddressAware="1"                   /LARGEADDRESSAWARE:NO
            //    SupportLargerThan2Gb                    LargeAddressAware="2"                   /LARGEADDRESSAWARE
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.LargeAddress.Default, () => { context.Options["LargeAddressAware"] = "true"; context.CommandLineOptions["LargeAddressAware"] = "/LARGEADDRESSAWARE"; }),
            Options.Option(Options.Vc.Linker.LargeAddress.NotSupportLargerThan2Gb, () => { context.Options["LargeAddressAware"] = "false"; context.CommandLineOptions["LargeAddressAware"] = "/LARGEADDRESSAWARE:NO"; }),
            Options.Option(Options.Vc.Linker.LargeAddress.SupportLargerThan2Gb, () => { context.Options["LargeAddressAware"] = "true"; context.CommandLineOptions["LargeAddressAware"] = "/LARGEADDRESSAWARE"; })
            );

            Options.Vc.Linker.BaseAddress baseAddress = Options.GetObject<Options.Vc.Linker.BaseAddress>(context.Configuration);
            if (baseAddress != null && baseAddress.Value.Length > 0)
            {
                context.Options["BaseAddress"] = baseAddress.Value;
                context.CommandLineOptions["BaseAddress"] = @"/BASE:""" + (baseAddress.Value) + @"""";
            }
            else
            {
                context.Options["BaseAddress"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["BaseAddress"] = FileGeneratorUtilities.RemoveLineTag;
            }

            //Reference
            //    Default                                 OptimizeReferences="0"
            //    KeepUnreferencedData                    OptimizeReferences="1"                  /OPT:NOREF
            //    EliminateUnreferencedData               OptimizeReferences="2"                  /OPT:REF
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.Reference.KeepUnreferencedData, () => { context.Options["OptimizeReferences"] = "false"; context.CommandLineOptions["OptimizeReference"] = "/OPT:NOREF"; }),
            Options.Option(Options.Vc.Linker.Reference.EliminateUnreferencedData, () => { context.Options["OptimizeReferences"] = "true"; context.CommandLineOptions["OptimizeReference"] = "/OPT:REF"; })
            );

            //EnableCOMDATFolding
            //    Default                                 EnableCOMDATFolding="0"
            //    DoNotRemoveRedundantCOMDATs             EnableCOMDATFolding="1"                 /OPT:NOICF
            //    RemoveRedundantCOMDATs                  EnableCOMDATFolding="2"                 /OPT:ICF
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.EnableCOMDATFolding.DoNotRemoveRedundantCOMDATs, () => { context.Options["EnableCOMDATFolding"] = "false"; context.CommandLineOptions["EnableCOMDATFolding"] = "/OPT:NOICF"; }),
            Options.Option(Options.Vc.Linker.EnableCOMDATFolding.RemoveRedundantCOMDATs, () => { context.Options["EnableCOMDATFolding"] = "true"; context.CommandLineOptions["EnableCOMDATFolding"] = "/OPT:ICF"; })
            );

            //FixedBaseAddress
            //    Default                                 FixedBaseAddress="0"
            //    Enable                                  FixedBaseAddress="1"                  /FIXED
            //    Disable                                 FixedBaseAddress="2"                  /FIXED:NO
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.FixedBaseAddress.Default, () => { context.Options["FixedBaseAddress"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["FixedBaseAddress"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.FixedBaseAddress.Enable, () => { context.Options["FixedBaseAddress"] = "true"; context.CommandLineOptions["FixedBaseAddress"] = "/FIXED"; }),
            Options.Option(Options.Vc.Linker.FixedBaseAddress.Disable, () => { context.Options["FixedBaseAddress"] = "false"; context.CommandLineOptions["FixedBaseAddress"] = "/FIXED:NO"; })
            );

            //GenerateWindowsMetadata
            //    Default                                 GenerateWindowsMetadata="0"
            //    Enable                                  GenerateWindowsMetadata="1"                  /WINMD
            //    Disable                                 GenerateWindowsMetadata="2"                  /WINMD:NO
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.GenerateWindowsMetadata.Default, () =>
            {
                context.Options["GenerateWindowsMetadata"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["WindowsMetadataFile"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["GenerateWindowsMetadata"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["WindowsMetadataFile"] = FileGeneratorUtilities.RemoveLineTag;
            }),
            Options.Option(Options.Vc.Linker.GenerateWindowsMetadata.Enable, () =>
            {
                context.Options["GenerateWindowsMetadata"] = "true";
                string windowsMetadataFile = @"$(OutDir)\$(RootNamespace).winmd";
                context.Options["WindowsMetadataFile"] = windowsMetadataFile;
                context.CommandLineOptions["GenerateWindowsMetadata"] = "/WINMD";
                context.CommandLineOptions["WindowsMetadataFile"] = @"/WINMDFILE:""" + windowsMetadataFile + @"""";
            }),
            Options.Option(Options.Vc.Linker.GenerateWindowsMetadata.Disable, () =>
            {
                context.Options["GenerateWindowsMetadata"] = "false";
                context.Options["WindowsMetadataFile"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["GenerateWindowsMetadata"] = "/WINMD:NO";
                context.CommandLineOptions["WindowsMetadataFile"] = FileGeneratorUtilities.RemoveLineTag;
            })
            );

            //LinkTimeCodeGeneration
            //    Default                                 LinkTimeCodeGeneration="0"
            //    UseLinkTimeCodeGeneration               LinkTimeCodeGeneration="1"              /ltcg
            //    ProfileGuidedOptimizationInstrument     LinkTimeCodeGeneration="2"              /ltcg:pginstrument
            //    ProfileGuidedOptimizationOptimize       LinkTimeCodeGeneration="3"              /ltcg:pgoptimize
            //    ProfileGuidedOptimizationUpdate         LinkTimeCodeGeneration="4"              /ltcg:pgupdate
            bool profileGuideOptimization = false;

            if (context.Configuration.Output == Project.Configuration.OutputType.Lib)
            {
                context.SelectOption
                (
                Options.Option(Options.Vc.Linker.LinkTimeCodeGeneration.Default, () => { context.Options["LinkTimeCodeGeneration"] = "false"; context.CommandLineOptions["LinkTimeCodeGeneration"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.Linker.LinkTimeCodeGeneration.UseLinkTimeCodeGeneration, () => { context.Options["LinkTimeCodeGeneration"] = "true"; context.CommandLineOptions["LinkTimeCodeGeneration"] = "/LTCG"; }),
                Options.Option(Options.Vc.Linker.LinkTimeCodeGeneration.ProfileGuidedOptimizationInstrument, () => { context.Options["LinkTimeCodeGeneration"] = "true"; context.CommandLineOptions["LinkTimeCodeGeneration"] = "/LTCG"; }),
                Options.Option(Options.Vc.Linker.LinkTimeCodeGeneration.ProfileGuidedOptimizationOptimize, () => { context.Options["LinkTimeCodeGeneration"] = "true"; context.CommandLineOptions["LinkTimeCodeGeneration"] = "/LTCG"; }),
                Options.Option(Options.Vc.Linker.LinkTimeCodeGeneration.ProfileGuidedOptimizationUpdate, () => { context.Options["LinkTimeCodeGeneration"] = "true"; context.CommandLineOptions["LinkTimeCodeGeneration"] = "/LTCG"; })
                );
            }
            else
            {
                context.SelectOption
                (
                Options.Option(Options.Vc.Linker.LinkTimeCodeGeneration.Default, () => { context.Options["LinkTimeCodeGeneration"] = "Default"; context.CommandLineOptions["LinkTimeCodeGeneration"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.Linker.LinkTimeCodeGeneration.UseLinkTimeCodeGeneration, () => { context.Options["LinkTimeCodeGeneration"] = "UseLinkTimeCodeGeneration"; context.CommandLineOptions["LinkTimeCodeGeneration"] = "/LTCG"; }),
                Options.Option(Options.Vc.Linker.LinkTimeCodeGeneration.ProfileGuidedOptimizationInstrument, () => { context.Options["LinkTimeCodeGeneration"] = "PGInstrument"; profileGuideOptimization = true; context.CommandLineOptions["LinkTimeCodeGeneration"] = "/LTCG:PGInstrument"; }),
                Options.Option(Options.Vc.Linker.LinkTimeCodeGeneration.ProfileGuidedOptimizationOptimize, () => { context.Options["LinkTimeCodeGeneration"] = "PGOptimization"; profileGuideOptimization = true; context.CommandLineOptions["LinkTimeCodeGeneration"] = "/LTCG:PGOptimize"; }),
                Options.Option(Options.Vc.Linker.LinkTimeCodeGeneration.ProfileGuidedOptimizationUpdate, () => { context.Options["LinkTimeCodeGeneration"] = "PGUpdate"; profileGuideOptimization = true; context.CommandLineOptions["LinkTimeCodeGeneration"] = "/LTCG:PGUpdate"; })
                );
            }


            if (profileGuideOptimization)
            {
                string profileGuidedDatabase = optionsContext.OutputDirectoryRelative + Util.WindowsSeparator + optionsContext.TargetName + ".pgd";
                context.Options["ProfileGuidedDatabase"] = profileGuidedDatabase;
                context.CommandLineOptions["ProfileGuidedDatabase"] = @"/PGD:""" + profileGuidedDatabase + @"""";
            }
            else
            {
                context.Options["ProfileGuidedDatabase"] = "";
                context.CommandLineOptions["ProfileGuidedDatabase"] = FileGeneratorUtilities.RemoveLineTag;
            }

            // FunctionOrder
            // FunctionOrder="@..\path_to\order.txt"             /ORDER:"@..\path_to\order.txt"
            Options.Vc.Linker.FunctionOrder fctOrder = Options.GetObject<Options.Vc.Linker.FunctionOrder>(context.Configuration);
            context.Options["FunctionOrder"] = (fctOrder != null) ? fctOrder.Order : FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["FunctionOrder"] = (fctOrder != null) ? @"/ORDER:@""" + fctOrder.Order + @"""" : FileGeneratorUtilities.RemoveLineTag;

            // ForceFileOutput
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.ForceFileOutput.Default, () => { context.Options["ForceFileOutput"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["ForceFileOutput"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.ForceFileOutput.MultiplyDefinedSymbolOnly, () => { context.Options["ForceFileOutput"] = "MultiplyDefinedSymbolOnly"; context.CommandLineOptions["ForceFileOutput"] = "/FORCE:MULTIPLE"; })
            );

            //CreateHotPatchableImage
            //    Disable
            //    Enable                                  CreateHotPatchableImage="Enabled"         /FUNCTIONPADMIN
            //    X86Image                                CreateHotPatchableImage="X86Image"        /FUNCTIONPADMIN:5
            //    X64Image                                CreateHotPatchableImage="X64Image"        /FUNCTIONPADMIN:6
            //    ItaniumImage                            CreateHotPatchableImage="ItaniumImage"    /FUNCTIONPADMIN:16
            context.SelectOption
            (
                Options.Option(Options.Vc.Linker.CreateHotPatchableImage.Disable, () => { context.Options["CreateHotPatchableImage"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["CreateHotPatchableImage"] = FileGeneratorUtilities.RemoveLineTag; }),
                Options.Option(Options.Vc.Linker.CreateHotPatchableImage.Enable, () => { context.Options["CreateHotPatchableImage"] = "Enabled"; context.CommandLineOptions["CreateHotPatchableImage"] = "/FUNCTIONPADMIN"; }),
                Options.Option(Options.Vc.Linker.CreateHotPatchableImage.X86Image, () => { context.Options["CreateHotPatchableImage"] = "X86Image"; context.CommandLineOptions["CreateHotPatchableImage"] = "/FUNCTIONPADMIN:5"; }),
                Options.Option(Options.Vc.Linker.CreateHotPatchableImage.X64Image, () => { context.Options["CreateHotPatchableImage"] = "X64Image"; context.CommandLineOptions["CreateHotPatchableImage"] = "/FUNCTIONPADMIN:6"; }),
                Options.Option(Options.Vc.Linker.CreateHotPatchableImage.ItaniumImage, () => { context.Options["CreateHotPatchableImage"] = "ItaniumImage"; context.CommandLineOptions["CreateHotPatchableImage"] = "/FUNCTIONPADMIN:16"; })
            );

            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.TreatLinkerWarningAsErrors.Disable, () => { context.Options["TreatLinkerWarningAsErrors"] = FileGeneratorUtilities.RemoveLineTag; context.CommandLineOptions["TreatLinkerWarningAsErrors"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.Linker.TreatLinkerWarningAsErrors.Enable, () => { context.Options["TreatLinkerWarningAsErrors"] = "true"; context.CommandLineOptions["TreatLinkerWarningAsErrors"] = "/WX"; })
            );

            // Target Machine
            optionsContext.PlatformVcxproj.SetupPlatformTargetOptions(context);
            optionsContext.PlatformVcxproj.SelectLinkerOptions(context);

            // Options.Vc.Librarian.AdditionalOptions
            context.Configuration.AdditionalLibrarianOptions.Sort();
            string additionalLibrarianOptions = context.Configuration.AdditionalLibrarianOptions.JoinStrings(" ").Trim();

            // Options.Vc.Linker.AdditionalOptions
            context.Configuration.AdditionalLinkerOptions.Sort();
            string linkerAdditionalOptions = context.Configuration.AdditionalLinkerOptions.JoinStrings(" ").Trim();

            Func<Strings, string> formatIgnoredWarnings = disabledWarnings =>
            {
                if (disabledWarnings.Count > 0)
                    return "/ignore:" + disabledWarnings.JoinStrings(",");
                return string.Empty;
            };

            // Treat Options.Vc.Librarian/Linker.DisableSpecificWarnings here because
            // they do not have a specific line in the vcxproj
            string ignoredLibWarnings = formatIgnoredWarnings(Options.GetStrings<Options.Vc.Librarian.DisableSpecificWarnings>(context.Configuration));
            if (!string.IsNullOrEmpty(ignoredLibWarnings))
            {
                if (additionalLibrarianOptions.Length > 0)
                    additionalLibrarianOptions += " ";
                additionalLibrarianOptions += ignoredLibWarnings;
            }

            string ignoredLinkerWarnings = formatIgnoredWarnings(Options.GetStrings<Options.Vc.Linker.DisableSpecificWarnings>(context.Configuration));
            if (!string.IsNullOrEmpty(ignoredLinkerWarnings))
            {
                if (linkerAdditionalOptions.Length > 0)
                    linkerAdditionalOptions += " ";
                linkerAdditionalOptions += ignoredLinkerWarnings;
            }

            context.Options["AdditionalLibrarianOptions"] = additionalLibrarianOptions.Length > 0 ? additionalLibrarianOptions : FileGeneratorUtilities.RemoveLineTag;
            context.Options["AdditionalLinkerOptions"] = linkerAdditionalOptions.Length > 0 ? linkerAdditionalOptions : FileGeneratorUtilities.RemoveLineTag;
        }

        private void GenerateManifestToolOptions(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            if (!context.DevelopmentEnvironment.IsVisualStudio()) // TODO: ideally this option generator should be split between VS / non-VS
                return;

            Strings manifestInputs = new Strings();

            string vsManifestFilesPath = Util.SimplifyPath(Path.Combine(context.DevelopmentEnvironment.GetVisualStudioVCRootPath(), "Include", "Manifest"));

            //EnableDpiAwareness
            context.SelectOption
            (
            Options.Option(Options.Vc.ManifestTool.EnableDpiAwareness.Default, () => { context.Options["EnableDpiAwareness"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.ManifestTool.EnableDpiAwareness.Yes, () => { context.Options["EnableDpiAwareness"] = "true"; manifestInputs.Add(Path.Combine(vsManifestFilesPath, "dpiaware.manifest")); }),
            Options.Option(Options.Vc.ManifestTool.EnableDpiAwareness.PerMonitor, () => { context.Options["EnableDpiAwareness"] = "PerMonitorHighDPIAware"; manifestInputs.Add(Path.Combine(vsManifestFilesPath, "PerMonitorHighDPIAware.manifest")); }),
            Options.Option(Options.Vc.ManifestTool.EnableDpiAwareness.No, () => { context.Options["EnableDpiAwareness"] = "false"; })
            );

            if (context.Configuration.AdditionalManifestFiles.Count > 0)
            {
                context.Options["AdditionalManifestFiles"] = string.Join(";", Util.PathGetRelative(context.ProjectDirectory, context.Configuration.AdditionalManifestFiles));
                manifestInputs.AddRange(context.Configuration.AdditionalManifestFiles);
            }
            else
                context.Options["AdditionalManifestFiles"] = FileGeneratorUtilities.RemoveLineTag;

            if (manifestInputs.Count > 0)
            {
                Options.Vc.Linker.EmbedManifest embedManifest = Options.GetObject<Options.Vc.Linker.EmbedManifest>(context.Configuration);
                if (embedManifest == Options.Vc.Linker.EmbedManifest.No)
                    throw new NotImplementedException("Sharpmake does not support manifestinputs without embedding the manifest!");

                var cmdManifests = manifestInputs.Select(p => Bff.CmdLineConvertIncludePathsFunc(context, optionsContext.Resolver, p, "/manifestinput:"));

                context.CommandLineOptions["ManifestInputs"] = string.Join($"'{Environment.NewLine}                            + ' ", cmdManifests);
            }
            else
            {
                context.CommandLineOptions["ManifestInputs"] = FileGeneratorUtilities.RemoveLineTag;
            }
        }

        private void GeneratePostBuildOptions(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            string eventSeparator = Vcxproj.EventSeparator;

            if (context.Configuration.EventPreBuild.Count == 0)
            {
                context.Options["PreBuildEvent"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PreBuildEventDescription"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PreBuildEventEnable"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.Options["PreBuildEvent"] = (context.Configuration.EventPreBuild.JoinStrings(eventSeparator) + eventSeparator).Replace(@"""", @"&quot;");
                context.Options["PreBuildEventDescription"] = context.Configuration.EventPreBuildDescription != string.Empty ? context.Configuration.EventPreBuildDescription : FileGeneratorUtilities.RemoveLineTag;
                context.Options["PreBuildEventEnable"] = context.Configuration.EventPreBuildExcludedFromBuild ? "false" : "true";
            }

            if (context.Configuration.EventPreLink.Count == 0)
            {
                context.Options["PreLinkEvent"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PreLinkEventDescription"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PreLinkEventEnable"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.Options["PreLinkEvent"] = (context.Configuration.EventPreLink.JoinStrings(eventSeparator) + eventSeparator).Replace(@"""", @"&quot;");
                context.Options["PreLinkEventDescription"] = context.Configuration.EventPreLinkDescription != string.Empty ? context.Configuration.EventPreLinkDescription : FileGeneratorUtilities.RemoveLineTag;
                context.Options["PreLinkEventEnable"] = context.Configuration.EventPreLinkExcludedFromBuild ? "false" : "true";
            }

            if (context.Configuration.EventPrePostLink.Count == 0)
            {
                context.Options["PrePostLinkEvent"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PrePostLinkEventDescription"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PrePostLinkEventEnable"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.Options["PrePostLinkEvent"] = (context.Configuration.EventPrePostLink.JoinStrings(eventSeparator) + eventSeparator).Replace(@"""", @"&quot;");
                context.Options["PrePostLinkEventDescription"] = context.Configuration.EventPrePostLinkDescription != string.Empty ? context.Configuration.EventPrePostLinkDescription : FileGeneratorUtilities.RemoveLineTag;
                context.Options["PrePostLinkEventEnable"] = context.Configuration.EventPrePostLinkExcludedFromBuild ? "false" : "true";
            }

            if (!context.Configuration.IsFastBuild)
            {
                if (context.Configuration.Output == Project.Configuration.OutputType.Exe || context.Configuration.ExecuteTargetCopy)
                {
                    foreach (var customEvent in context.Configuration.ResolvedEventPreBuildExe)
                    {
                        if (customEvent is Project.Configuration.BuildStepExecutable)
                        {
                            var execEvent = (Project.Configuration.BuildStepExecutable)customEvent;

                            string relativeExecutableFile = Util.PathGetRelative(context.ProjectDirectory, execEvent.ExecutableFile);
                            string eventString = string.Format(
                                "{0} {1}",
                                Util.SimplifyPath(relativeExecutableFile),
                                execEvent.ExecutableOtherArguments
                            );

                            context.Configuration.EventPreBuild.Add(eventString);
                        }
                        else if (customEvent is Project.Configuration.BuildStepCopy)
                        {
                            var copyEvent = (Project.Configuration.BuildStepCopy)customEvent;
                            context.Configuration.EventPreBuild.Add(copyEvent.GetCopyCommand(context.ProjectDirectory, optionsContext.Resolver));
                        }
                        else
                        {
                            throw new Error("Unsupported type of build event found in Prebuild steps: " + customEvent.GetType().Name);
                        }
                    }

                    foreach (var customEvent in context.Configuration.ResolvedEventPostBuildExe)
                    {
                        if (customEvent is Project.Configuration.BuildStepExecutable)
                        {
                            var execEvent = (Project.Configuration.BuildStepExecutable)customEvent;

                            string relativeExecutableFile = Util.PathGetRelative(context.ProjectDirectory, execEvent.ExecutableFile);
                            string eventString = string.Format(
                                "{0} {1}",
                                Util.SimplifyPath(relativeExecutableFile),
                                execEvent.ExecutableOtherArguments
                            );

                            if (!context.Configuration.EventPostBuild.Contains(eventString))
                                context.Configuration.EventPostBuild.Add(eventString);
                        }
                        else if (customEvent is Project.Configuration.BuildStepCopy)
                        {
                            var copyEvent = (Project.Configuration.BuildStepCopy)customEvent;
                            string eventString = copyEvent.GetCopyCommand(context.ProjectDirectory, optionsContext.Resolver);

                            if (!context.Configuration.EventPostBuild.Contains(eventString))
                                context.Configuration.EventPostBuild.Add(eventString);
                        }
                        else
                        {
                            throw new Error("Unsupported type of build event found in PostBuild steps: " + customEvent.GetType().Name);
                        }
                    }
                }

                if (context.Configuration.Output == Project.Configuration.OutputType.Exe || context.Configuration.Output == Project.Configuration.OutputType.Dll)
                {
                    if (context.Configuration.PostBuildStepTest != null)
                    {
                        // First, execute tests
                        context.Configuration.EventPostBuild.Insert(0,
                            string.Format(
                                "{0} {1}",
                                Util.SimplifyPath(Util.PathGetRelative(context.ProjectDirectory, context.Configuration.PostBuildStepTest.TestExecutable)),
                                context.Configuration.PostBuildStepTest.TestArguments
                            )
                        );
                    }
                    if (context.Configuration.PostBuildStampExe != null)
                    {
                        // NO, first, execute stamp !
                        context.Configuration.EventPostBuild.Insert(0,
                            string.Format(
                                "{0} {1} {2} {3}",
                                Util.SimplifyPath(Util.PathGetRelative(context.ProjectDirectory, context.Configuration.PostBuildStampExe.ExecutableFile)),
                                context.Configuration.PostBuildStampExe.ExecutableInputFileArgumentOption,
                                context.Configuration.PostBuildStampExe.ExecutableOutputFileArgumentOption,
                                context.Configuration.PostBuildStampExe.ExecutableOtherArguments
                            )
                        );
                    }
                }
            }

            if (context.Configuration.EventPreBuild.Count == 0)
            {
                context.Options["PreBuildEvent"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PreBuildEventDescription"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PreBuildEventEnable"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.Options["PreBuildEvent"] = context.Configuration.EventPreBuild.JoinStrings(eventSeparator, escapeXml: true) + eventSeparator;
                context.Options["PreBuildEventDescription"] = context.Configuration.EventPreBuildDescription != string.Empty ? context.Configuration.EventPreBuildDescription : FileGeneratorUtilities.RemoveLineTag;
                context.Options["PreBuildEventEnable"] = context.Configuration.EventPreBuildExcludedFromBuild ? "false" : "true";
            }

            if (context.Configuration.EventPostBuild.Count == 0)
            {
                context.Options["PostBuildEvent"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PostBuildEventDescription"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["PostBuildEventEnable"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.Options["PostBuildEvent"] = Util.JoinStrings(context.Configuration.EventPostBuild, eventSeparator, escapeXml: true) + eventSeparator;
                context.Options["PostBuildEventDescription"] = context.Configuration.EventPostBuildDescription != string.Empty ? context.Configuration.EventPostBuildDescription : FileGeneratorUtilities.RemoveLineTag;
                context.Options["PostBuildEventEnable"] = context.Configuration.EventPostBuildExcludedFromBuild ? "false" : "true";
            }

            if (context.Configuration.EventCustomBuild.Count == 0)
            {
                context.Options["CustomBuildEvent"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildEventDescription"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildEventOutputs"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.Options["CustomBuildEvent"] = (context.Configuration.EventCustomBuild.JoinStrings(eventSeparator, escapeXml: true) + eventSeparator);
                context.Options["CustomBuildEventDescription"] = context.Configuration.EventCustomBuildDescription != string.Empty ? context.Configuration.EventCustomBuildDescription : FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildEventOutputs"] = context.Configuration.EventCustomBuildOutputs != string.Empty ? context.Configuration.EventCustomBuildOutputs : FileGeneratorUtilities.RemoveLineTag;
            }

            if (context.Configuration.CustomBuildStep.Count == 0)
            {
                context.Options["CustomBuildStep"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildStepDescription"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildStepOutputs"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildStepInputs"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildStepBeforeTargets"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildStepAfterTargets"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildStepTreatOutputAsContent"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                context.Options["CustomBuildStep"] = (Util.JoinStrings(context.Configuration.CustomBuildStep, eventSeparator, escapeXml: true) + eventSeparator);
                context.Options["CustomBuildStepDescription"] = context.Configuration.CustomBuildStepDescription != string.Empty ? context.Configuration.CustomBuildStepDescription : FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildStepOutputs"] = context.Configuration.CustomBuildStepOutputs.Count == 0 ? FileGeneratorUtilities.RemoveLineTag : (context.Configuration.CustomBuildStepOutputs.JoinStrings(";", escapeXml: true));
                context.Options["CustomBuildStepInputs"] = context.Configuration.CustomBuildStepInputs.Count == 0 ? FileGeneratorUtilities.RemoveLineTag : (context.Configuration.CustomBuildStepInputs.JoinStrings(";", escapeXml: true));
                context.Options["CustomBuildStepBeforeTargets"] = context.Configuration.CustomBuildStepBeforeTargets != string.Empty ? context.Configuration.CustomBuildStepBeforeTargets : FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildStepAfterTargets"] = context.Configuration.CustomBuildStepAfterTargets != string.Empty ? context.Configuration.CustomBuildStepAfterTargets : FileGeneratorUtilities.RemoveLineTag;
                context.Options["CustomBuildStepTreatOutputAsContent"] = context.Configuration.CustomBuildStepTreatOutputAsContent != string.Empty ? context.Configuration.CustomBuildStepTreatOutputAsContent : FileGeneratorUtilities.RemoveLineTag;
            }
        }

        private void GenerateLLVMOptions(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            context.SelectOption
            (
            Options.Option(Options.Vc.LLVM.UseClangCl.Enable, () => { context.Options["UseClangCl"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.LLVM.UseClangCl.Disable, () => { context.Options["UseClangCl"] = "false"; })
            );

            context.SelectOption
            (
            Options.Option(Options.Vc.LLVM.UseLldLink.Default, () => { context.Options["UseLldLink"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Vc.LLVM.UseLldLink.Enable, () => { context.Options["UseLldLink"] = "true"; }),
            Options.Option(Options.Vc.LLVM.UseLldLink.Disable, () => { context.Options["UseLldLink"] = "false"; })
            );

            if (Options.GetObject<Options.Vc.General.PlatformToolset>(context.Configuration).IsLLVMToolchain() &&
                Options.GetObject<Options.Vc.LLVM.UseClangCl>(context.Configuration) == Options.Vc.LLVM.UseClangCl.Enable)
            {
                // This prevents clang-cl from auto-detecting the locally installed MSVC toolchain. Only paths on the command line will be considered.
                context.Configuration.AdditionalCompilerOptions.Add("-nostdinc");
            }
        }

        public static string MakeBuildStepName(Project.Configuration conf, Project.Configuration.BuildStepBase eventBuildStep, Vcxproj.BuildStep buildStep)
        {
            if (!eventBuildStep.IsResolved)
                throw new Error("Event hasn't been resolved!");

            Func<string, string> extractName = (name) => name.Substring(name.LastIndexOf(@"\", StringComparison.Ordinal) + 1).Replace('.', '_');

            bool isPostBuildCustomActionWithSpecificName = buildStep == Vcxproj.BuildStep.PostBuild || buildStep == Vcxproj.BuildStep.PostBuildCustomAction || eventBuildStep.IsNameSpecific;

            if (eventBuildStep is Project.Configuration.BuildStepExecutable)
            {
                var cEvent = eventBuildStep as Project.Configuration.BuildStepExecutable;
                string execName;

                if (isPostBuildCustomActionWithSpecificName)
                {
                    execName = @"Exec_" + extractName(cEvent.ExecutableFile) + "_" + (conf.TargetPath + conf.TargetFileFullName + cEvent.ExecutableOtherArguments).GetHashCode().ToString("X8");
                }
                else
                {
                    execName = @"Exec_" + extractName(cEvent.ExecutableFile);
                    execName += "_" + (execName).GetHashCode().ToString("X8");
                }

                return execName;
            }
            else if (eventBuildStep is Project.Configuration.BuildStepCopy)
            {
                var cEvent = eventBuildStep as Project.Configuration.BuildStepCopy;
                string copyName;

                if (isPostBuildCustomActionWithSpecificName)
                {
                    copyName = "Copy_" + (conf.TargetFileFullName + cEvent.SourcePath + cEvent.DestinationPath).GetHashCode().ToString("X8");
                }
                else
                {
                    copyName = "Copy_" + (cEvent.SourcePath + cEvent.DestinationPath).GetHashCode().ToString("X8");
                }

                return copyName;
            }
            else if (eventBuildStep is Project.Configuration.BuildStepTest)
            {
                var tEvent = eventBuildStep as Project.Configuration.BuildStepTest;
                string testName;

                if (isPostBuildCustomActionWithSpecificName)
                {
                    testName = "Test_" + extractName(tEvent.TestExecutable) + "_" + (conf.TargetPath + conf.TargetFileFullName).GetHashCode().ToString("X8");
                }
                else
                {
                    testName = "Test_" + extractName(tEvent.TestExecutable);
                    testName += "_" + (testName).GetHashCode().ToString("X8");
                }

                return testName;
            }
            else
            {
                throw new Error("error, BuildStep not supported: {0}", eventBuildStep.GetType().FullName);
            }
        }

        private static void SelectGenerateManifestOption(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.GenerateManifest.Enable, () =>
            {
                context.Options["GenerateManifest"] = "true";

                if (optionsContext.PlatformVcxproj.HasUserAccountControlSupport)
                {
                    context.CommandLineOptions["GenerateManifest"] = string.Format(@"/MANIFEST /MANIFESTUAC:""level=^'{0}^' uiAccess=^'false^'""", context.Configuration.ApplicationPermissions);

                    switch (context.Configuration.ApplicationPermissions)
                    {
                        case Project.Configuration.UACExecutionLevel.asInvoker:
                            context.Options["UACExecutionLevel"] = FileGeneratorUtilities.RemoveLineTag;
                            break;
                        case Project.Configuration.UACExecutionLevel.highestAvailable:
                        case Project.Configuration.UACExecutionLevel.requireAdministrator:
                            context.Options["UACExecutionLevel"] = context.Configuration.ApplicationPermissions.ToString();
                            break;
                    }
                }
                else
                {
                    context.CommandLineOptions["GenerateManifest"] = @"/MANIFEST /MANIFESTUAC:NO";
                    context.Options["UACExecutionLevel"] = FileGeneratorUtilities.RemoveLineTag;
                }

                if (context.Options["EmbedManifest"] == "false")
                {
                    string manifestFile = optionsContext.IntermediateDirectoryRelative + Util.WindowsSeparator + optionsContext.TargetName + context.Configuration.ManifestFileSuffix;
                    context.Options["ManifestFile"] = manifestFile;
                    context.CommandLineOptions["ManifestFile"] = @"/ManifestFile:""" + FormatCommandLineOptionPath(context, manifestFile) + @"""";
                }
                else
                {
                    context.Options["ManifestFile"] = FileGeneratorUtilities.RemoveLineTag;
                    context.CommandLineOptions["ManifestFile"] = "/MANIFEST:EMBED";
                }
            }),
            Options.Option(Options.Vc.Linker.GenerateManifest.Disable, () =>
            {
                context.Options["GenerateManifest"] = "false";
                context.Options["ManifestFile"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["GenerateManifest"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["ManifestFile"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["UACExecutionLevel"] = FileGeneratorUtilities.RemoveLineTag;
            })
            );
        }

        private static void SelectGenerateDebugInformationOption(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            //GenerateDebugInformation="false"
            //    VS2012-VS2013
            //    GenerateDebugInformation.Enable         GenerateDebugInformation="true"           /DEBUG
            //    GenerateDebugInformation.Disable        GenerateDebugInformation="false"
            //    (GenerateFullProgramDatabaseFile is ignored, there can only be full pdb files)
            //
            //    VS2015
            //    GenerateDebugInformation.Enable         GenerateDebugInformation="true"           /DEBUG
            //    GenerateDebugInformation.EnableFastLink GenerateDebugInformation="DebugFastLink"  /DEBUG:FASTLINK
            //    Disable                                 GenerateDebugInformation="No"
            //
            //    VS2017-VS2019
            //    Enable                                  GenerateDebugInformation="true"           /DEBUG
            //    EnableFastLink                          GenerateDebugInformation="DebugFastLink"  /DEBUG:FASTLINK
            //    Disable                                 GenerateDebugInformation="No"

            Action<bool> enableDebugInformation = (isFastLink) =>
            {
                bool forceFullPDB = false;
                context.SelectOption
                (
                Options.Option(Options.Vc.Linker.GenerateFullProgramDatabaseFile.Enable, () => { context.Options["FullProgramDatabaseFile"] = "true"; forceFullPDB = true; }),
                Options.Option(Options.Vc.Linker.GenerateFullProgramDatabaseFile.Disable, () => { context.Options["FullProgramDatabaseFile"] = "false"; }),
                Options.Option(Options.Vc.Linker.GenerateFullProgramDatabaseFile.Default, () => { context.Options["FullProgramDatabaseFile"] = FileGeneratorUtilities.RemoveLineTag; })
                );

                if (isFastLink && forceFullPDB)
                    throw new Error("Cannot set both EnableFastLink and GenerateFullProgramDatabaseFile.Enable in conf " + context.Configuration);

                bool isMicrosoftPlatform = context.Configuration.Platform.IsMicrosoft();
                if (isFastLink)
                {
                    if (!isMicrosoftPlatform)
                        throw new Error("Cannot set EnableFastLink on non-microsoft platform " + context.Configuration.Platform);

                    context.Options["LinkerGenerateDebugInformation"] = "DebugFastLink";
                    context.CommandLineOptions["LinkerGenerateDebugInformation"] = "/DEBUG:FASTLINK";
                }
                else
                {
                    if (isMicrosoftPlatform && forceFullPDB &&
                         ((context.DevelopmentEnvironment == DevEnv.vs2017) || (context.DevelopmentEnvironment == DevEnv.vs2019)))
                    {
                        context.Options["LinkerGenerateDebugInformation"] = "DebugFull";
                        context.CommandLineOptions["LinkerGenerateDebugInformation"] = "/DEBUG:FULL";
                    }
                    else
                    {
                        context.Options["LinkerGenerateDebugInformation"] = "true";
                        context.CommandLineOptions["LinkerGenerateDebugInformation"] = "/DEBUG";
                    }
                }

                string optionsCompilerProgramDatabaseFile = context.Configuration.CompilerPdbFilePath;
                string optionsLinkerProgramDatabaseFile = context.Configuration.LinkerPdbFilePath;
                string cmdLineOptionsCompilerProgramDatabaseFile = context.Configuration.CompilerPdbFilePath;
                string cmdLineOptionsLinkerProgramDatabaseFile = context.Configuration.LinkerPdbFilePath;

                if (context.Configuration.UseRelativePdbPath)
                {
                    optionsCompilerProgramDatabaseFile = Util.PathGetRelative(context.ProjectDirectory, optionsCompilerProgramDatabaseFile, true);
                    optionsLinkerProgramDatabaseFile = Util.PathGetRelative(context.ProjectDirectory, optionsLinkerProgramDatabaseFile, true);
                    cmdLineOptionsCompilerProgramDatabaseFile = FormatCommandLineOptionPath(context, optionsCompilerProgramDatabaseFile);
                    cmdLineOptionsLinkerProgramDatabaseFile = FormatCommandLineOptionPath(context, optionsLinkerProgramDatabaseFile);
                }

                context.Options["CompilerProgramDatabaseFile"] = string.IsNullOrEmpty(optionsCompilerProgramDatabaseFile)
                    ? FileGeneratorUtilities.RemoveLineTag
                    : optionsCompilerProgramDatabaseFile;
                context.Options["LinkerProgramDatabaseFile"] = string.IsNullOrEmpty(optionsLinkerProgramDatabaseFile)
                    ? FileGeneratorUtilities.RemoveLineTag
                    : optionsLinkerProgramDatabaseFile;

                // %2 is converted by FastBuild
                // Output name of object being compiled, as specified by CompilerOutputPath and the name of discovered objects depending on the Compiler input options (extension is also replace with CompilerOutputExtension).
                if (FastBuildSettings.EnableFastLinkPDBSupport && isFastLink)
                    context.CommandLineOptions["CompilerProgramDatabaseFile"] = @"/Fd""%2.pdb""";
                else if (!string.IsNullOrEmpty(cmdLineOptionsCompilerProgramDatabaseFile))
                    context.CommandLineOptions["CompilerProgramDatabaseFile"] = $@"/Fd""{cmdLineOptionsCompilerProgramDatabaseFile}""";
                else
                    context.CommandLineOptions["CompilerProgramDatabaseFile"] = FileGeneratorUtilities.RemoveLineTag;

                if (!string.IsNullOrEmpty(cmdLineOptionsLinkerProgramDatabaseFile))
                    context.CommandLineOptions["LinkerProgramDatabaseFile"] = $@"/PDB:""{cmdLineOptionsLinkerProgramDatabaseFile}""";
                else
                    context.CommandLineOptions["LinkerProgramDatabaseFile"] = FileGeneratorUtilities.RemoveLineTag;
            };

            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.GenerateDebugInformation.Enable, () => { enableDebugInformation(false); }),
            Options.Option(Options.Vc.Linker.GenerateDebugInformation.EnableFastLink, () =>
            {
                if (optionsContext.HasClrSupport)
                    context.Builder.LogWarningLine("GenerateDebugInformation.EnableFastLink is not supported with CLR/dot net (project: " + context.Project.Name + "), fallback to GenerateDebugInformation.Enable");
                enableDebugInformation(!optionsContext.HasClrSupport);
            }),
            Options.Option(Options.Vc.Linker.GenerateDebugInformation.Disable, () =>
            {
                context.Options["LinkerGenerateDebugInformation"] = "false";
                context.Options["CompilerProgramDatabaseFile"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["LinkerProgramDatabaseFile"] = FileGeneratorUtilities.RemoveLineTag;
                context.Options["FullProgramDatabaseFile"] = FileGeneratorUtilities.RemoveLineTag;

                context.CommandLineOptions["LinkerGenerateDebugInformation"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["CompilerProgramDatabaseFile"] = FileGeneratorUtilities.RemoveLineTag;
                context.CommandLineOptions["LinkerProgramDatabaseFile"] = FileGeneratorUtilities.RemoveLineTag;
            })
            );
        }

        private static void SelectGenerateMapFileOption(IGenerationContext context, ProjectOptionsGenerationContext optionsContext)
        {
            Action enableMapOption = () =>
            {
                context.Options["GenerateMapFile"] = "true";
                string targetNamePrefix = optionsContext.PlatformVcxproj.GetOutputFileNamePrefix(context, context.Configuration.Output);
                string mapFile = Path.Combine(optionsContext.OutputDirectoryRelative, targetNamePrefix + optionsContext.TargetName + ".map");
                context.Options["MapFileName"] = mapFile;

                string mapFileBffRelative = FormatCommandLineOptionPath(context, mapFile);
                if (PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform).IsUsingClang)
                {
                    context.CommandLineOptions["GenerateMapFile"] = @"-Wl,-Map=""" + mapFileBffRelative + @"""";
                }
                else
                {
                    context.CommandLineOptions["GenerateMapFile"] = @"/MAP"":" + mapFileBffRelative + @"""";
                }
            };

            context.SelectOption
            (
            Options.Option(Options.Vc.Linker.GenerateMapFile.Disable, () =>
            {
                context.Options["GenerateMapFile"] = "false";
                context.Options["MapFileName"] = "";
                context.CommandLineOptions["GenerateMapFile"] = FileGeneratorUtilities.RemoveLineTag;
            }),
            Options.Option(Options.Vc.Linker.GenerateMapFile.Normal, enableMapOption),
            Options.Option(Options.Vc.Linker.GenerateMapFile.Full, enableMapOption)
            );
        }

        private static string FormatCommandLineOptionPath(IGenerationContext context, string path)
        {
            return !context.PlainOutput ? Bff.CurrentBffPathKeyCombine(path) : path;
        }
    }
}
