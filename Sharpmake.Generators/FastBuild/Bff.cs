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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators.FastBuild
{
    public partial class Bff : IProjectGenerator
    {
        class BffGenerationContext : IGenerationContext
        {
            public Builder Builder { get; }

            public Project Project { get; }

            public Project.Configuration Configuration { get; set; }

            public string ProjectDirectory { get; }

            public Options.ExplicitOptions Options { get; set; } = new Options.ExplicitOptions();

            public IDictionary<string, string> CommandLineOptions { get; set; } = new ProjectOptionsGenerator.VcxprojCmdLineOptions();

            public DevEnv DevelopmentEnvironment => Configuration.Compiler;

            public string ProjectDirectoryCapitalized { get; }

            public string ProjectSourceCapitalized { get; }

            public BffGenerationContext(Builder builder, Project project, string projectDir)
            {
                Builder = builder;
                Project = project;
                ProjectDirectory = projectDir;
                ProjectDirectoryCapitalized = Util.GetCapitalizedPath(projectDir);
                ProjectSourceCapitalized = Util.GetCapitalizedPath(project.SourceRootPath);

            }

            public void SelectOption(params Options.OptionAction[] options)
            {
                Sharpmake.Options.SelectOption(Configuration, options);
            }

            public void SelectOptionWithFallback(Action fallbackAction, params Options.OptionAction[] options)
            {
                Sharpmake.Options.SelectOptionWithFallback(Configuration, fallbackAction, options);
            }
        }

        public const string CurrentBffPathVariable = ".CurrentBffPath";
        public const string CurrentBffPathKey = "$CurrentBffPath$";

        public static IUnityResolver UnityResolver = new HashUnityResolver();

        private static ConcurrentDictionary<Project.Configuration, string> s_configurationArguments = new ConcurrentDictionary<Project.Configuration, string>(); // fastbuild arguments for a specific configuration

        internal static void SetCommandLineArguments(Project.Configuration conf, string arguments)
        {
            s_configurationArguments.TryAdd(conf, arguments);
        }

        public static string GetCommandLineArguments(Project.Configuration conf)
        {
            string value;
            s_configurationArguments.TryGetValue(conf, out value);
            return value;
        }

        internal static string GetBffFileName(string path, string bffFileName)
        {
            return Path.Combine(path, bffFileName + FastBuildSettings.FastBuildConfigFileExtension);
        }

        public static string GetShortProjectName(Project project, Configuration conf)
        {
            return (project.Name + "_" + conf.Target.Name + "_" + conf.Target.GetPlatform()).Replace(' ', '_');
        }

        public static string GetPlatformSpecificDefine(Platform platform)
        {
            string define = PlatformRegistry.Get<IPlatformBff>(platform).BffPlatformDefine;
            if (define == null)
                throw new NotImplementedException($"Please add {platform} specific define for bff sections, ideally the same as ExplicitDefine, to get Intellisense.");

            return define;
        }

        public static void InitializeBuilder(Builder builder)
        {
            if (FastBuildSettings.MakeCommandGenerator == null)
                FastBuildSettings.MakeCommandGenerator = new FastBuildDefaultMakeCommandGenerator();
        }

        // ===================================================================================
        // BFF Generation
        // ===================================================================================
        public void Generate(
            Builder builder,
            Project project,
            List<Project.Configuration> configurations,
            string projectFile,
            List<string> generatedFiles,
            List<string> skipFiles
        )
        {
            if (!FastBuildSettings.FastBuildSupportEnabled)
                return;

            //To make sure that all the projects are fastbuild
            configurations = configurations.Where(x => x.IsFastBuild && !x.DoNotGenerateFastBuild).OrderBy(x => x.Platform).ToList();
            if (!configurations.Any())
                return;

            Project.Configuration firstConf = configurations.First();
            string projectName = firstConf.ProjectName;
            string projectPath = new FileInfo(projectFile).Directory.FullName;
            var context = new BffGenerationContext(builder, project, projectPath);
            string projectBffFile = Bff.GetBffFileName(projectPath, firstConf.BffFileName); // TODO: bff file name could be different per conf, hence we would generate more than one file
            string fastBuildClrSupport = Util.IsDotNet(firstConf) ? "/clr" : FileGeneratorUtilities.RemoveLineTag;
            List<Vcxproj.ProjectFile> filesInNonDefaultSection;
            var confSourceFiles = GetGeneratedFiles(context, configurations, out filesInNonDefaultSection);

            // Generate all configuration options onces...
            var options = new Dictionary<Project.Configuration, Options.ExplicitOptions>();
            var cmdLineOptions = new Dictionary<Project.Configuration, ProjectOptionsGenerator.VcxprojCmdLineOptions>();
            var projectOptionsGen = new ProjectOptionsGenerator();
            foreach (Project.Configuration conf in configurations)
            {
                context.Configuration = conf;
                context.Options = new Options.ExplicitOptions();
                context.CommandLineOptions = new ProjectOptionsGenerator.VcxprojCmdLineOptions();
                projectOptionsGen.GenerateOptions(context);
                options.Add(conf, context.Options);
                cmdLineOptions.Add(conf, (ProjectOptionsGenerator.VcxprojCmdLineOptions)context.CommandLineOptions);

                // Validation of unsupported cases
                if (conf.EventPreLink.Count > 0)
                    throw new Error("Sharpmake-FastBuild : Pre-Link Events not yet supported.");
                if (context.Options["IgnoreImportLibrary"] == "true")
                    throw new Error("Sharpmake-FastBuild : IgnoreImportLibrary not yet supported.");

                if (conf.Output != Project.Configuration.OutputType.None && conf.FastBuildBlobbed)
                {
                    var unityTuple = GetDefaultTupleConfig();
                    var confSubConfigs = confSourceFiles[conf];
                    ConfigureUnities(context, confSubConfigs[unityTuple]);
                }
            }

            ResolveUnities(project);

            // Start writing Bff
            Resolver resolver = new Resolver();
            var bffGenerator = new FileGenerator(resolver);
            var bffWholeFileGenerator = new FileGenerator(resolver);

            using (bffWholeFileGenerator.Declare("fastBuildProjectName", projectName))
            {
                bffWholeFileGenerator.Write(Template.ConfigurationFile.HeaderFile);
            }

            int configIndex = 0;
            foreach (Project.Configuration conf in configurations)
            {
                var platformBff = PlatformRegistry.Get<IPlatformBff>(conf.Platform);
                var clangPlatformBff = PlatformRegistry.Query<IClangPlatformBff>(conf.Platform);
                var microsoftPlatformBff = PlatformRegistry.Query<IMicrosoftPlatformBff>(conf.Platform);

                // TODO: really not ideal, refactor and move the properties we need from it someplace else
                var vcxprojPlatform = PlatformRegistry.Query<IPlatformVcxproj>(conf.Platform);

                if (conf.Platform.IsSupportedFastBuildPlatform() && confSourceFiles.ContainsKey(conf))
                {
                    if (conf.IsBlobbed && conf.FastBuildBlobbed)
                    {
                        throw new Error("Sharpmake-FastBuild: Configuration " + conf + " is configured for blobbing by fastbuild and sharpmake. This is illegal.");
                    }

                    var defaultTuple = GetDefaultTupleConfig();
                    var confSubConfigs = confSourceFiles[conf];
                    ProjectOptionsGenerator.VcxprojCmdLineOptions confCmdLineOptions = cmdLineOptions[conf];

                    // We will need as many "sub"-libraries as subConfigs to generate the final library
                    int subConfigIndex = 0;
                    Strings subConfigLibs = new Strings();
                    Strings subConfigObjectList = new Strings();
                    bool isUnity = false;

                    if (configIndex == 0 || configurations[configIndex - 1].Platform != conf.Platform)
                    {
                        using (bffGenerator.Declare("fastBuildDefine", GetPlatformSpecificDefine(conf.Platform)))
                            bffGenerator.Write(Template.ConfigurationFile.PlatformBeginSection);
                    }
                    List<string> resourceFilesSections = new List<string>();

                    var additionalDependencies = new Strings();
                    {
                        string confCmdLineOptionsAddDeps = confCmdLineOptions["AdditionalDependencies"];
                        if (confCmdLineOptionsAddDeps != FileGeneratorUtilities.RemoveLineTag)
                            additionalDependencies.Add(confCmdLineOptionsAddDeps.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries));
                    }

                    foreach (var tuple in confSubConfigs.Keys)
                    {
                        bool isDefaultTuple = defaultTuple.Equals(tuple);

                        bool isUsePrecomp = tuple.Item1 && conf.PrecompSource != null;
                        bool isCompileAsCFile = tuple.Item2;
                        bool isCompileAsCPPFile = tuple.Item3;
                        bool isCompileAsCLRFile = tuple.Item4;
                        bool isConsumeWinRTExtensions = tuple.Item5 || (Options.GetObject<Options.Vc.Compiler.CompileAsWinRT>(conf) == Options.Vc.Compiler.CompileAsWinRT.Enable);
                        bool isASMFileSection = tuple.Item6;
                        Options.Vc.Compiler.Exceptions exceptionsSetting = tuple.Item7;
                        bool isCompileAsNonCLRFile = tuple.Rest.Item1;

                        bool isFirstSubConfig = subConfigIndex == 0;
                        bool isLastSubConfig = subConfigIndex == confSubConfigs.Keys.Count - 1;

                        // For now, this will do.
                        if (conf.FastBuildBlobbed && isDefaultTuple && !isUnity)
                        {
                            isUnity = true;
                        }
                        else
                        {
                            isUnity = false;
                        }

                        Trace.Assert(!isCompileAsCPPFile, "Sharpmake-FastBuild : CompiledAsCPP isn't yet supported.");
                        Trace.Assert(!isCompileAsCLRFile, "Sharpmake-FastBuild : CompiledAsCLR isn't yet supported.");
                        Trace.Assert(!isCompileAsNonCLRFile, "Sharpmake-FastBuild : !CompiledAsCLR isn't yet supported.");

                        Strings fastBuildCompilerInputPatternList = isCompileAsCFile ? new Strings { ".c" } : project.SourceFilesCPPExtensions;
                        Strings fastBuildCompilerInputPatternTransformedList = new Strings(fastBuildCompilerInputPatternList.Select((s) => { return "*" + s; }));

                        string fastBuildCompilerInputPattern = UtilityMethods.FBuildCollectionFormat(fastBuildCompilerInputPatternTransformedList, 32);

                        string fastBuildPrecompiledSourceFile = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildCompileAsC = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildUnityName = isUnity ? GetUnityName(conf) : null;

                        string previousExceptionSettings = confCmdLineOptions["ExceptionHandling"];
                        switch (exceptionsSetting)
                        {
                            case Sharpmake.Options.Vc.Compiler.Exceptions.Enable:
                                confCmdLineOptions["ExceptionHandling"] = "/EHsc";
                                break;
                            case Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithExternC:
                                confCmdLineOptions["ExceptionHandling"] = "/EHs";
                                break;
                            case Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithSEH:
                                confCmdLineOptions["ExceptionHandling"] = "/EHa";
                                break;
                        }

                        bool isNoBlobImplicitConfig = false;
                        if (conf.FastBuildNoBlobStrategy == Project.Configuration.InputFileStrategy.Exclude &&
                            conf.IsBlobbed == false &&
                            conf.FastBuildBlobbed == false
                        )
                        {
                            if (isCompileAsCPPFile == false && isCompileAsCFile == false && !isConsumeWinRTExtensions)
                            {
                                isNoBlobImplicitConfig = true;
                            }
                        }

                        Options.ExplicitOptions confOptions = options[conf];

                        bool useObjectLists = Sharpmake.Options.GetObject<Options.Vc.Linker.UseLibraryDependencyInputs>(conf) == Sharpmake.Options.Vc.Linker.UseLibraryDependencyInputs.Enable;
                        string outputFile = confOptions["OutputFile"];
                        string fastBuildOutputFile = CurrentBffPathKeyCombine(Util.PathGetRelative(projectPath, outputFile, true));
                        fastBuildOutputFile = platformBff.GetOutputFilename(conf.Output, fastBuildOutputFile);
                        string fastBuildOutputFileShortName = GetShortProjectName(project, conf);
                        string fastBuildProjectDependencies = "''";
                        List<string> fastBuildProjectDependencyList = new List<string>();
                        List<string> fastBuildProjectExeUtilityDependencyList = new List<string>();

                        bool isOutputTypeExe = conf.Output == Project.Configuration.OutputType.Exe;
                        bool isOutputTypeDll = conf.Output == Project.Configuration.OutputType.Dll;
                        bool isOutputTypeExeOrDll = isOutputTypeExe || isOutputTypeDll;

                        if (isOutputTypeExeOrDll)
                        {
                            StringBuilder result = new StringBuilder();
                            result.Append("\n");

                            var orderedProjectDeps = UtilityMethods.GetOrderedFlattenedProjectDependencies(conf, false);
                            foreach (var depProjConfig in orderedProjectDeps)
                            {
                                if (depProjConfig.Project == project)
                                    throw new Error("Sharpmake-FastBuild : Project dependencies refers to itself.");
                                if (!conf.ResolvedDependencies.Contains(depProjConfig))
                                    throw new Error("Sharpmake-FastBuild : dependency was not resolved.");

                                if (depProjConfig.Output != Project.Configuration.OutputType.Exe &&
                                    depProjConfig.Output != Project.Configuration.OutputType.Utility)
                                {
                                    result.Append("                                '" + GetShortProjectName(depProjConfig.Project, depProjConfig) + "',\n");
                                    fastBuildProjectDependencyList.Add(GetOutputFileName(depProjConfig));
                                }
                                else
                                {
                                    fastBuildProjectExeUtilityDependencyList.Add(GetShortProjectName(depProjConfig.Project, depProjConfig));
                                }
                            }
                            if (result.Length > 0)
                                result.Remove(result.Length - 1, 1);
                            fastBuildProjectDependencies = result.ToString();
                        }

                        string partialLibInfo = "";
                        string partialLibs = FileGeneratorUtilities.RemoveLineTag;
                        string librarianAdditionalInputs = FileGeneratorUtilities.RemoveLineTag; // TODO: implement
                        string fastBuildObjectListDependencies = FileGeneratorUtilities.RemoveLineTag;

                        string outputType;
                        switch (conf.Output)
                        {
                            case Project.Configuration.OutputType.Lib:
                                outputType = "Library";
                                break;
                            case Project.Configuration.OutputType.Exe:
                                outputType = "Executable";
                                break;
                            case Project.Configuration.OutputType.Dll:
                                outputType = "DLL";
                                break;
                            default:
                                outputType = "Unknown";
                                break;
                        }

                        if (confSubConfigs.Keys.Count > 1)
                        {
                            if (!isLastSubConfig)
                            {
                                partialLibInfo = "[Partial Lib of " + fastBuildOutputFileShortName + "]";
                                fastBuildOutputFileShortName += "_" + subConfigIndex.ToString();

                                var staticLibExtension = vcxprojPlatform.StaticLibraryFileExtension;

                                fastBuildOutputFile = Path.ChangeExtension(fastBuildOutputFile, null); // removes the extension
                                fastBuildOutputFile += "_" + subConfigIndex.ToString();

                                if (!staticLibExtension.StartsWith(".", StringComparison.Ordinal))
                                    fastBuildOutputFile += '.';
                                fastBuildOutputFile += staticLibExtension;

                                subConfigLibs.Add(fastBuildOutputFile);
                                subConfigObjectList.Add(fastBuildOutputFileShortName);
                            }
                            else
                            {
                                partialLibs = subConfigLibs.JoinStrings(" ");

                                StringBuilder result = new StringBuilder();
                                result.Append("\n");
                                foreach (string subConfigLib in subConfigLibs)
                                    result.Append("                                   '" + subConfigLib + "',\n");
                                if (result.Length > 1)
                                    result.Remove(result.Length - 2, 2);

                                result.Clear();
                                int i = 0;
                                foreach (string subConfigObject in subConfigObjectList)
                                {
                                    if (!useObjectLists && conf.Output != Project.Configuration.OutputType.Dll)
                                        result.Append((i++ != 0 ? "                                '" : "'") + subConfigObject + "_" + outputType + "',\n");
                                    else
                                        result.Append((i++ != 0 ? "                                '" : "'") + subConfigObject + "_objects',\n");
                                }
                                if (result.Length > 0)
                                    result.Remove(result.Length - 1, 1);
                                fastBuildObjectListDependencies = result.ToString();
                            }
                        }

                        string fastBuildCompilerPCHOptions = isUsePrecomp ? Template.ConfigurationFile.UsePrecomp : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildCompilerPCHOptionsClang = isUsePrecomp ? Template.ConfigurationFile.UsePrecompClang : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildLinkerOutputFile = fastBuildOutputFile;
                        string fastBuildStampExecutable = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildStampArguments = FileGeneratorUtilities.RemoveLineTag;

                        var postBuildEvents = new Dictionary<string, Project.Configuration.BuildStepBase>();

                        var fastBuildTargetSubTargets = new List<string>();
                        {
                            if (isLastSubConfig) // post-build steps on the last subconfig
                            {
                                if (isOutputTypeExe || conf.ExecuteTargetCopy)
                                {
                                    if (conf.CopyDependenciesBuildStep != null)
                                        throw new NotImplementedException("CopyDependenciesBuildStep are not supported with FastBuild");

                                    var copies = ProjectOptionsGenerator.ConvertPostBuildCopiesToRelative(conf, projectPath);
                                    foreach (var copy in copies)
                                    {
                                        var sourceFile = copy.Key;
                                        var destinationFolder = copy.Value;

                                        // use the global root for alias computation, as the project has not idea in which master bff it has been included
                                        var destinationRelativeToGlobal = Util.GetConvertedRelativePath(projectPath, destinationFolder, conf.Project.RootPath, true, conf.Project.RootPath);
                                        string fastBuildCopyAlias = UtilityMethods.GetFastBuildCopyAlias(Path.GetFileName(sourceFile), destinationRelativeToGlobal);
                                        fastBuildTargetSubTargets.Add(fastBuildCopyAlias);
                                    }
                                }
                            }

                            if (isFirstSubConfig) // pre-build steps on the first config
                            {
                                // the pre-steps are written in the master bff, we only need to refer their aliases
                                fastBuildTargetSubTargets.AddRange(conf.EventPreBuildExecute.Select(e => e.Key));
                                fastBuildTargetSubTargets.AddRange(conf.ResolvedEventPreBuildExe.Select(e => ProjectOptionsGenerator.MakeBuildStepName(conf, e, Vcxproj.BuildStep.PreBuild)));

                                fastBuildTargetSubTargets.AddRange(conf.EventCustomPrebuildExecute.Select(e => e.Key));
                                fastBuildTargetSubTargets.AddRange(conf.ResolvedEventCustomPreBuildExe.Select(e => ProjectOptionsGenerator.MakeBuildStepName(conf, e, Vcxproj.BuildStep.PreBuildCustomAction)));
                            }

                            fastBuildTargetSubTargets.AddRange(fastBuildProjectExeUtilityDependencyList);

                            if (conf.Output == Project.Configuration.OutputType.Lib && useObjectLists)
                            {
                                fastBuildTargetSubTargets.Add(fastBuildOutputFileShortName + "_objects");
                            }
                            else if (conf.Output == Project.Configuration.OutputType.None && project is FastBuildAllProject)
                            {
                                // filter to only get the configurations of projects that were explicitely added, not the dependencies
                                var minResolvedConf = conf.ResolvedPrivateDependencies.Where(x => conf.UnResolvedPrivateDependencies.ContainsKey(x.Project.GetType()));
                                foreach (var dep in minResolvedConf)
                                    fastBuildTargetSubTargets.Add(GetShortProjectName(dep.Project, dep));
                            }
                            else
                            {
                                fastBuildTargetSubTargets.Add(fastBuildOutputFileShortName + "_" + outputType);
                            }

                            if (isLastSubConfig) // post-build steps on the last subconfig
                            {
                                foreach (var eventPair in conf.EventPostBuildExecute)
                                {
                                    fastBuildTargetSubTargets.Add(eventPair.Key);
                                    postBuildEvents.Add(eventPair.Key, eventPair.Value);
                                }
                                
                                var extraPlatformEvents = platformBff.GetExtraPostBuildEvents(conf, fastBuildOutputFile).Select(step => { step.Resolve(resolver); return step; });
                                foreach (var buildEvent in extraPlatformEvents.Concat(conf.ResolvedEventPostBuildExe))
                                {
                                    string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, buildEvent, Vcxproj.BuildStep.PostBuild);
                                    fastBuildTargetSubTargets.Add(eventKey);
                                    postBuildEvents.Add(eventKey, buildEvent);
                                }

                                foreach (var eventPair in conf.EventCustomPostBuildExecute)
                                {
                                    fastBuildTargetSubTargets.Add(eventPair.Key);
                                    postBuildEvents.Add(eventPair.Key, eventPair.Value);
                                }

                                foreach (var buildEvent in conf.ResolvedEventCustomPostBuildExe)
                                {
                                    string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, buildEvent, Vcxproj.BuildStep.PostBuildCustomAction);
                                    fastBuildTargetSubTargets.Add(eventKey);
                                    postBuildEvents.Add(eventKey, buildEvent);
                                }

                                if(conf.PostBuildStepTest != null)
                                {
                                    string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, conf.PostBuildStepTest, Vcxproj.BuildStep.PostBuildCustomAction);
                                    fastBuildTargetSubTargets.Add(eventKey);
                                    postBuildEvents.Add(eventKey, conf.PostBuildStepTest);
                                }
                            }

                            if (conf.Output != Project.Configuration.OutputType.Dll && conf.Output != Project.Configuration.OutputType.Exe)
                            {
                                foreach (var subConfig in subConfigObjectList)
                                {
                                    string subTarget;
                                    if (useObjectLists)
                                        subTarget = subConfig + "_objects";
                                    else
                                        subTarget = subConfig + "_" + outputType;

                                    if (!fastBuildTargetSubTargets.Contains(subTarget))
                                        fastBuildTargetSubTargets.Add(subTarget);
                                }
                            }
                        }

                        // Remove from cmdLineOptions["AdditionalDependencies"] dependencies that are already listed in fastBuildProjectDependencyList
                        {
                            string libExt = ".lib";
                            string outExt = ".a";
                            string prefixExt = "-l";
                            vcxprojPlatform.SetupPlatformLibraryOptions(ref libExt, ref outExt, ref prefixExt);

                            // test prefixes, usually it is either -l or lib, to know if we can shorten it
                            // Note that the output filename prefix is not case sensitive (ideally it should depend on the OS)
                            Tuple<string, StringComparison>[] prefixesToTest = {
                                Tuple.Create(prefixExt, StringComparison.Ordinal),
                                Tuple.Create(vcxprojPlatform.GetOutputFileNamePrefix(context, Project.Configuration.OutputType.Lib), StringComparison.OrdinalIgnoreCase)
                            };

                            var finalDependencies = new Strings();
                            foreach (var additionalDependency in additionalDependencies)
                            {
                                // compute dependency identifier by removing platform lib prefix and extension (if necessary)
                                int subStringStartIndex = 0;
                                int subStringLength = additionalDependency.Length;
                                if (additionalDependency.EndsWith(libExt, StringComparison.OrdinalIgnoreCase))
                                    subStringLength -= libExt.Length;

                                foreach (var prefixTuple in prefixesToTest)
                                {
                                    string prefix = prefixTuple.Item1;
                                    if (additionalDependency.StartsWith(prefix, prefixTuple.Item2))
                                    {
                                        subStringStartIndex = prefix.Length;
                                        subStringLength -= prefix.Length;

                                        break;
                                    }
                                }

                                string testedDep = additionalDependency.Substring(subStringStartIndex, subStringLength);

                                // add this link dependency if it's not a project dependency nor a project object file
                                if (!fastBuildProjectDependencyList.Contains(testedDep) && !IsObjectList(fastBuildProjectDependencyList, testedDep))
                                {
                                    if (clangPlatformBff == null)
                                    {
                                        // just add the original dependency
                                        finalDependencies.Add(@"""" + additionalDependency + @"""");
                                    }
                                    else
                                    {
                                        if (subStringStartIndex != 0 && !additionalDependency.Contains(Util.UnixSeparator) && !additionalDependency.Contains(Util.WindowsSeparator))
                                        {
                                            // the dependency is a "global" lib (ie it doesn't contain a file path)
                                            // use the -l switch to link it.
                                            finalDependencies.Add(@"""-l" + testedDep + @"""");
                                        }
                                        else
                                        {
                                            // the dependency is a "local" lib use the file path to link it
                                            finalDependencies.Add(@"""" + additionalDependency + @"""");
                                        }
                                    }
                                }
                            }

                            if(finalDependencies.Any())
                                confCmdLineOptions["AdditionalDependencies"] = string.Join($"'{Environment.NewLine}                            + ' ", finalDependencies);
                            else
                                confCmdLineOptions["AdditionalDependencies"] = FileGeneratorUtilities.RemoveLineTag;
                        }

                        string fastBuildConsumeWinRTExtension = isConsumeWinRTExtensions ? "/ZW" : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildUsingPlatformConfig = FileGeneratorUtilities.RemoveLineTag;
                        string clangFileLanguage = String.Empty;

                        if (isCompileAsCFile)
                        {
                            fastBuildUsingPlatformConfig = platformBff.CConfigName;
                            // Do not take cpp Language conformance into account while compiling in C
                            confCmdLineOptions["CppLanguageStd"] = FileGeneratorUtilities.RemoveLineTag;
                            if (clangPlatformBff != null)
                                clangFileLanguage = "-x c "; // Compiler option to indicate that its a C file
                        }
                        else
                        {
                            fastBuildUsingPlatformConfig = platformBff.CppConfigName;
                        }

                        if (isASMFileSection)
                        {
                            fastBuildUsingPlatformConfig += Template.ConfigurationFile.MasmConfigNameSuffix;
                        }

                        string fastBuildCompilerExtraOptions = !isASMFileSection ? Template.ConfigurationFile.CPPCompilerExtraOptions : Template.ConfigurationFile.MasmCompilerExtraOptions;
                        string fastBuildCompilerOptionsDeoptimize = FileGeneratorUtilities.RemoveLineTag;
                        if (!isASMFileSection && conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                            fastBuildCompilerOptionsDeoptimize = Template.ConfigurationFile.CPPCompilerOptionsDeoptimize;

                        string compilerOptions = !isASMFileSection ? Template.ConfigurationFile.CompilerOptionsCPP : Template.ConfigurationFile.CompilerOptionsMasm;
                        compilerOptions += Template.ConfigurationFile.CompilerOptionsCommon;

                        string compilerOptionsClang = Template.ConfigurationFile.CompilerOptionsClang +
                                                        Template.ConfigurationFile.CompilerOptionsCommon;

                        string compilerOptionsClangDeoptimized = FileGeneratorUtilities.RemoveLineTag;
                        if (conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                            compilerOptionsClangDeoptimized =
                                Template.ConfigurationFile.ClangCompilerOptionsDeoptimize +
                                Template.ConfigurationFile.CompilerOptionsCommon;

                        string fastBuildDeoptimizationWritableFiles = null;
                        string fastBuildDeoptimizationWritableFilesWithToken = null;
                        Project.Configuration.DeoptimizationWritableFiles deoptimizeSetting = conf.FastBuildDeoptimization;
                        if (isASMFileSection)
                            deoptimizeSetting = Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization;
                        switch (deoptimizeSetting)
                        {
                            case Project.Configuration.DeoptimizationWritableFiles.DeoptimizeWritableFiles:
                                fastBuildDeoptimizationWritableFiles = "true";
                                fastBuildDeoptimizationWritableFilesWithToken = FileGeneratorUtilities.RemoveLineTag;
                                break;
                            case Project.Configuration.DeoptimizationWritableFiles.DeoptimizeWritableFilesWithToken:
                                fastBuildDeoptimizationWritableFiles = FileGeneratorUtilities.RemoveLineTag;
                                fastBuildDeoptimizationWritableFilesWithToken = "true";
                                break;

                            default:
                                fastBuildDeoptimizationWritableFiles = FileGeneratorUtilities.RemoveLineTag;
                                fastBuildDeoptimizationWritableFilesWithToken = FileGeneratorUtilities.RemoveLineTag;
                                break;
                        }

                        string fastBuildCompilerForceUsing = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildAdditionalCompilerOptionsFromCode = FileGeneratorUtilities.RemoveLineTag;

                        if (conf.ReferencesByPath.Count > 0)  // only ref by path supported
                        {
                            fastBuildAdditionalCompilerOptionsFromCode = "";
                            foreach (var refByPath in conf.ReferencesByPath)
                            {
                                string refByPathCopy = refByPath;
                                if (refByPath.StartsWith(context.Project.RootPath, StringComparison.OrdinalIgnoreCase))
                                    refByPathCopy = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectory, refByPath));

                                fastBuildAdditionalCompilerOptionsFromCode += "/FU\"" + refByPathCopy + "\" ";
                            }
                            //compilerOptions += Template.ConfigurationFile.CompilerForceUsing;

                        }
                        if (conf.ReferencesByName.Count > 0)
                        {
                            throw new Exception("Use ReferencesByPath instead of ReferencesByName for FastBuild support; ");
                        }

                        if (conf.ForceUsingFiles.Count() != 0)
                        {
                            StringBuilder builderForceUsingFiles = new StringBuilder();
                            foreach (var f in conf.ForceUsingFiles)
                            {
                                string file = f;
                                if (f.StartsWith(context.Project.RootPath, StringComparison.OrdinalIgnoreCase))
                                    file = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectory, f));

                                builderForceUsingFiles.AppendFormat(@" /FU""{0}""", file);
                            }
                            fastBuildCompilerForceUsing = builderForceUsingFiles.ToString();
                        }

                        if (isOutputTypeExeOrDll && conf.PostBuildStampExe != null)
                        {
                            fastBuildStampExecutable = CurrentBffPathKeyCombine(Util.PathGetRelative(projectPath, conf.PostBuildStampExe.ExecutableFile, true));
                            fastBuildStampArguments = String.Format("{0} {1} {2}",
                                conf.PostBuildStampExe.ExecutableInputFileArgumentOption,
                                conf.PostBuildStampExe.ExecutableOutputFileArgumentOption,
                                conf.PostBuildStampExe.ExecutableOtherArguments);
                        }

                        bool linkObjects = false;
                        if (isOutputTypeExeOrDll)
                        {
                            linkObjects = (confOptions["UseLibraryDependencyInputs"] == "true");
                        }

                        Strings fullInputPaths = new Strings();
                        string fastBuildInputPath = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildInputExcludedFiles = FileGeneratorUtilities.RemoveLineTag;
                        {
                            Strings excludedSourceFiles = new Strings();
                            if (isNoBlobImplicitConfig && isDefaultTuple)
                            {
                                fullInputPaths.Add(context.ProjectSourceCapitalized);
                                fullInputPaths.AddRange(project.AdditionalSourceRootPaths.Select(Util.GetCapitalizedPath));

                                excludedSourceFiles.AddRange(filesInNonDefaultSection.Select(f => f.FileName));
                            }

                            if (isDefaultTuple && conf.FastBuildBlobbingStrategy == Project.Configuration.InputFileStrategy.Exclude && conf.FastBuildBlobbed)
                            {
                                // Adding the folders excluded from unity to the folders to build without unity(building each file individually)
                                fullInputPaths.AddRange(project.SourcePathsBlobExclude.Select(Util.GetCapitalizedPath));
                            }

                            if (project.SourceFilesFiltersRegex.Count == 0)
                            {
                                var relativePaths = new Strings(fullInputPaths.Select(p => CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectory, p, true))));
                                fastBuildInputPath = UtilityMethods.FBuildCollectionFormat(relativePaths, 32);
                            }
                            else
                            {
                                fullInputPaths.Clear();
                            }

                            excludedSourceFiles.AddRange(conf.ResolvedSourceFilesBuildExclude);
                            excludedSourceFiles.AddRange(conf.PrecompSourceExclude);

                            // Converting the excluded filenames to relative path to the input path so that this
                            // can work properly with subst usage when running with fastbuild caching active. 
                            //
                            // Also exclusion checks in fastbuild assume that the exclusion filenames are
                            // relative to the .UnityInputPath and checks that paths are ending with the specified
                            // path which means that any filename starting with a .. will never be excluded by fastbuild.
                            //
                            // Note: Ideally fastbuild should expect relative paths to the bff file path instead of the .UnityInputPath but
                            // well I guess we are stuck with this.                                                    
                            var excludedSourceFilesRelative = new Strings();
                            foreach (string file in excludedSourceFiles.SortedValues)
                            {
                                string fileExtension = Path.GetExtension(file);
                                if (project.SourceFilesCompileExtensions.Contains(fileExtension))
                                {
                                    if (IsFileInInputPathList(fullInputPaths, file))
                                        excludedSourceFilesRelative.Add(CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectoryCapitalized, file)));
                                }
                            }
                            if (excludedSourceFilesRelative.Count > 0)
                            {
                                Strings includedExtensions = isCompileAsCFile ? new Strings { ".c" } : project.SourceFilesCPPExtensions;
                                fastBuildInputExcludedFiles = UtilityMethods.FBuildCollectionFormat(excludedSourceFilesRelative, 34, includedExtensions);
                            }
                        }

                        bool projectHasResourceFiles = false;
                        string fastBuildSourceFiles = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildResourceFiles = FileGeneratorUtilities.RemoveLineTag;

                        {
                            List<string> fastbuildSourceFilesList = new List<string>();
                            List<string> fastbuildResourceFilesList = new List<string>();

                            var sourceFiles = confSubConfigs[tuple];
                            foreach (Vcxproj.ProjectFile sourceFile in sourceFiles)
                            {
                                string sourceFileName = CurrentBffPathKeyCombine(sourceFile.FileNameProjectRelative);

                                if (isUsePrecomp && conf.PrecompSource != null && sourceFile.FileName.EndsWith(conf.PrecompSource, StringComparison.OrdinalIgnoreCase))
                                {
                                    fastBuildPrecompiledSourceFile = sourceFileName;
                                }
                                else if (String.Compare(sourceFile.FileExtension, ".rc", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    if (microsoftPlatformBff != null && microsoftPlatformBff.SupportsResourceFiles)
                                    {
                                        fastbuildResourceFilesList.Add(sourceFileName);
                                        projectHasResourceFiles = true;
                                    }
                                }
                                else
                                {
                                    // TODO: use SourceFileExtension array instead of ".cpp"
                                    if ((String.Compare(sourceFile.FileExtension, ".cpp", StringComparison.OrdinalIgnoreCase) != 0) ||
                                        conf.ResolvedSourceFilesBlobExclude.Contains(sourceFile.FileName) ||
                                        (!isUnity && !isNoBlobImplicitConfig))
                                    {
                                        if (!IsFileInInputPathList(fullInputPaths, sourceFile.FileName))
                                            fastbuildSourceFilesList.Add(sourceFileName);
                                    }
                                }
                            }
                            fastBuildSourceFiles = UtilityMethods.FBuildFormatList(fastbuildSourceFilesList, 32);
                            fastBuildResourceFiles = UtilityMethods.FBuildFormatList(fastbuildResourceFilesList, 30);
                        }

                        if (projectHasResourceFiles)
                            resourceFilesSections.Add(fastBuildOutputFileShortName + "_resources");

                        // It is useless to have an input pattern defined if there is no input path
                        if (fastBuildInputPath == FileGeneratorUtilities.RemoveLineTag)
                            fastBuildCompilerInputPattern = FileGeneratorUtilities.RemoveLineTag;

                        string fastBuildObjectListResourceDependencies = FormatListPartForTag(resourceFilesSections, 32, true);

                        using (bffGenerator.Declare("conf", conf))
                        using (bffGenerator.Declare("project", project))
                        using (bffGenerator.Declare("target", conf.Target))
                        {
                            switch (conf.Output)
                            {
                                case Project.Configuration.OutputType.Lib:
                                case Project.Configuration.OutputType.Exe:
                                case Project.Configuration.OutputType.Dll:
                                    using (bffGenerator.Declare("$(ProjectName)", projectName))
                                    using (bffGenerator.Declare("options", confOptions))
                                    using (bffGenerator.Declare("cmdLineOptions", confCmdLineOptions))
                                    using (bffGenerator.Declare("fastBuildUsingPlatformConfig", "Using( " + fastBuildUsingPlatformConfig + " )"))
                                    using (bffGenerator.Declare("fastBuildProjectName", projectName))
                                    using (bffGenerator.Declare("fastBuildClrSupport", fastBuildClrSupport))
                                    using (bffGenerator.Declare("fastBuildOutputFileShortName", fastBuildOutputFileShortName))
                                    using (bffGenerator.Declare("fastBuildOutputFile", fastBuildOutputFile))
                                    using (bffGenerator.Declare("fastBuildLinkerOutputFile", fastBuildLinkerOutputFile))
                                    using (bffGenerator.Declare("fastBuildLinkerLinkObjects", linkObjects ? "true" : "false"))
                                    using (bffGenerator.Declare("fastBuildPartialLibInfo", partialLibInfo))
                                    using (bffGenerator.Declare("fastBuildInputPath", fastBuildInputPath))
                                    using (bffGenerator.Declare("fastBuildCompilerInputPattern", fastBuildCompilerInputPattern))
                                    using (bffGenerator.Declare("fastBuildInputExcludedFiles", fastBuildInputExcludedFiles))
                                    using (bffGenerator.Declare("fastBuildSourceFiles", fastBuildSourceFiles))
                                    using (bffGenerator.Declare("fastBuildResourceFiles", fastBuildResourceFiles))
                                    using (bffGenerator.Declare("fastBuildPrecompiledSourceFile", fastBuildPrecompiledSourceFile))
                                    using (bffGenerator.Declare("fastBuildProjectDependencies", fastBuildProjectDependencies))
                                    using (bffGenerator.Declare("fastBuildObjectListResourceDependencies", fastBuildObjectListResourceDependencies))
                                    using (bffGenerator.Declare("fastBuildObjectListDependencies", fastBuildObjectListDependencies))
                                    using (bffGenerator.Declare("fastBuildCompilerPCHOptions", fastBuildCompilerPCHOptions))
                                    using (bffGenerator.Declare("fastBuildCompilerPCHOptionsClang", fastBuildCompilerPCHOptionsClang))
                                    using (bffGenerator.Declare("fastBuildConsumeWinRTExtension", fastBuildConsumeWinRTExtension))
                                    using (bffGenerator.Declare("fastBuildPartialLibs", partialLibs))
                                    using (bffGenerator.Declare("fastBuildOutputType", outputType))
                                    using (bffGenerator.Declare("fastBuildLibrarianAdditionalInputs", librarianAdditionalInputs))
                                    using (bffGenerator.Declare("fastBuildCompileAsC", fastBuildCompileAsC))
                                    using (bffGenerator.Declare("fastBuildUnityName", fastBuildUnityName ?? FileGeneratorUtilities.RemoveLineTag))
                                    using (bffGenerator.Declare("fastBuildClangFileLanguage", clangFileLanguage))
                                    using (bffGenerator.Declare("fastBuildDeoptimizationWritableFiles", fastBuildDeoptimizationWritableFiles))
                                    using (bffGenerator.Declare("fastBuildDeoptimizationWritableFilesWithToken", fastBuildDeoptimizationWritableFilesWithToken))
                                    using (bffGenerator.Declare("fastBuildCompilerForceUsing", fastBuildCompilerForceUsing))
                                    using (bffGenerator.Declare("fastBuildAdditionalCompilerOptionsFromCode", fastBuildAdditionalCompilerOptionsFromCode))
                                    using (bffGenerator.Declare("fastBuildStampExecutable", fastBuildStampExecutable))
                                    using (bffGenerator.Declare("fastBuildStampArguments", fastBuildStampArguments))
                                    {
                                        if (projectHasResourceFiles)
                                        {
                                            bffGenerator.Write(Template.ConfigurationFile.ResourcesBeginSection);
                                            bffGenerator.Write(Template.ConfigurationFile.ResourceCompilerExtraOptions);
                                            bffGenerator.Write(Template.ConfigurationFile.ResourceCompilerOptions);
                                            bffGenerator.Write(Template.ConfigurationFile.EndSection);
                                        }

                                        // Exe and DLL will always add an extra objectlist
                                        if (isOutputTypeExeOrDll && isLastSubConfig // only last subconfig will generate objectlist
                                        )
                                        {
                                            bffGenerator.Write(Template.ConfigurationFile.ObjectListBeginSection);

                                            if (conf.Platform.IsMicrosoft())
                                            {
                                                bffGenerator.Write(fastBuildCompilerExtraOptions);
                                                bffGenerator.Write(Template.ConfigurationFile.CPPCompilerOptimizationOptions);

                                                if (isUsePrecomp)
                                                    bffGenerator.Write(Template.ConfigurationFile.PCHOptions);
                                                bffGenerator.Write(compilerOptions);
                                                if (conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                                                {
                                                    if (isUsePrecomp)
                                                        bffGenerator.Write(Template.ConfigurationFile.PCHOptionsDeoptimize);
                                                    bffGenerator.Write(fastBuildCompilerOptionsDeoptimize);
                                                    bffGenerator.Write(Template.ConfigurationFile.DeOptimizeOption);
                                                }
                                            }
                                            else
                                            {
                                                //  CLANG Specific

                                                // TODO: This checks twice if the platform supports Clang -- fix?
                                                clangPlatformBff?.SetupClangOptions(bffGenerator);

                                                if (conf.Platform.IsUsingClang())
                                                {
                                                    if (isUsePrecomp)
                                                        bffGenerator.Write(Template.ConfigurationFile.PCHOptionsClang);
                                                    bffGenerator.Write(compilerOptionsClang);
                                                    if (conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                                                    {
                                                        bffGenerator.Write(Template.ConfigurationFile.ClangCompilerOptionsDeoptimize);
                                                        if (isUsePrecomp)
                                                            bffGenerator.Write(Template.ConfigurationFile.PCHOptionsDeoptimize);
                                                        bffGenerator.Write(compilerOptionsClangDeoptimized);
                                                        bffGenerator.Write(Template.ConfigurationFile.DeOptimizeOption);
                                                    }
                                                }
                                            }

                                            bffGenerator.Write(Template.ConfigurationFile.EndSection);
                                        }

                                        if (isOutputTypeDll && !isLastSubConfig)
                                        {
                                            using (bffGenerator.Declare("objectListName", fastBuildOutputFileShortName))
                                            {
                                                bffGenerator.Write(Template.ConfigurationFile.GenericObjectListBeginSection);

                                                if (conf.Platform.IsMicrosoft())
                                                {
                                                    bffGenerator.Write(fastBuildCompilerExtraOptions);
                                                    bffGenerator.Write(Template.ConfigurationFile.CPPCompilerOptimizationOptions);

                                                    if (isUsePrecomp)
                                                        bffGenerator.Write(Template.ConfigurationFile.PCHOptions);
                                                    bffGenerator.Write(compilerOptions);
                                                    if (conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                                                    {
                                                        if (isUsePrecomp)
                                                            bffGenerator.Write(Template.ConfigurationFile.PCHOptionsDeoptimize);
                                                        bffGenerator.Write(fastBuildCompilerOptionsDeoptimize);
                                                        bffGenerator.Write(Template.ConfigurationFile.DeOptimizeOption);
                                                    }
                                                }
                                                else
                                                {
                                                    // CLANG Specific

                                                    // TODO: This checks twice if the platform supports Clang -- fix?
                                                    clangPlatformBff?.SetupClangOptions(bffGenerator);

                                                    if (conf.Platform.IsUsingClang())
                                                    {
                                                        if (isUsePrecomp)
                                                            bffGenerator.Write(Template.ConfigurationFile.PCHOptionsClang);
                                                        bffGenerator.Write(compilerOptionsClang);

                                                        if (conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                                                        {
                                                            if (isUsePrecomp)
                                                                bffGenerator.Write(Template.ConfigurationFile.PCHOptionsDeoptimize);
                                                            bffGenerator.Write(compilerOptionsClangDeoptimized);
                                                            bffGenerator.Write(Template.ConfigurationFile.DeOptimizeOption);
                                                        }
                                                    }

                                                    // TODO: Add BFF generation for Win64 on Windows/Mac/Linux?

                                                }

                                                bffGenerator.Write(Template.ConfigurationFile.EndSection);
                                            }
                                        }
                                        else
                                        {
                                            bool outputLib = false;
                                            string beginSectionType = null;
                                            switch (conf.Output)
                                            {
                                                case Project.Configuration.OutputType.Exe:
                                                    {
                                                        if (isLastSubConfig)
                                                        {
                                                            beginSectionType = Template.ConfigurationFile.ExeDllBeginSection;
                                                        }
                                                        else
                                                        {
                                                            // in the case the lib has the flag force to be an objectlist, change the template
                                                            if (useObjectLists)
                                                                beginSectionType = Template.ConfigurationFile.ObjectListBeginSection;
                                                            else
                                                                beginSectionType = Template.ConfigurationFile.LibBeginSection;
                                                            outputLib = true;
                                                        }
                                                    }
                                                    break;
                                                case Project.Configuration.OutputType.Dll:
                                                    {
                                                        beginSectionType = Template.ConfigurationFile.ExeDllBeginSection;
                                                    }
                                                    break;
                                                case Project.Configuration.OutputType.Lib:
                                                    {
                                                        // in the case the lib has the flag force to be an objectlist, change the template
                                                        if (useObjectLists)
                                                            beginSectionType = Template.ConfigurationFile.ObjectListBeginSection;
                                                        else
                                                            beginSectionType = Template.ConfigurationFile.LibBeginSection;
                                                        outputLib = true;
                                                    }
                                                    break;
                                            }

                                            bffGenerator.Write(beginSectionType);

                                            if (outputLib)
                                            {
                                                if (conf.Platform.IsMicrosoft())
                                                {
                                                    bffGenerator.Write(fastBuildCompilerExtraOptions);
                                                    bffGenerator.Write(Template.ConfigurationFile.CPPCompilerOptimizationOptions);

                                                    if (isUsePrecomp)
                                                        bffGenerator.Write(Template.ConfigurationFile.PCHOptions);

                                                    bffGenerator.Write(compilerOptions);

                                                    bffGenerator.Write(Template.ConfigurationFile.LibrarianAdditionalInputs);
                                                    bffGenerator.Write(Template.ConfigurationFile.LibrarianOptions);
                                                    if (conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                                                    {
                                                        if (isUsePrecomp)
                                                            bffGenerator.Write(Template.ConfigurationFile.PCHOptionsDeoptimize);
                                                        bffGenerator.Write(fastBuildCompilerOptionsDeoptimize);
                                                        bffGenerator.Write(Template.ConfigurationFile.DeOptimizeOption);
                                                    }
                                                }
                                                else
                                                {
                                                    // TODO: This checks twice if the platform supports Clang -- fix?
                                                    clangPlatformBff?.SetupClangOptions(bffGenerator);

                                                    if (conf.Platform.IsUsingClang())
                                                    {
                                                        if (isUsePrecomp)
                                                            bffGenerator.Write(Template.ConfigurationFile.PCHOptionsClang);

                                                        bffGenerator.Write(Template.ConfigurationFile.CompilerOptionsCommon);
                                                        bffGenerator.Write(Template.ConfigurationFile.CompilerOptionsClang);
                                                        if (conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                                                        {
                                                            if (isUsePrecomp)
                                                                bffGenerator.Write(Template.ConfigurationFile.PCHOptionsDeoptimize);
                                                            bffGenerator.Write(compilerOptionsClangDeoptimized);
                                                            bffGenerator.Write(Template.ConfigurationFile.DeOptimizeOption);
                                                        }
                                                        bffGenerator.Write(Template.ConfigurationFile.LibrarianAdditionalInputs);
                                                        bffGenerator.Write(Template.ConfigurationFile.LibrarianOptionsClang);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                platformBff.SetupExtraLinkerSettings(bffGenerator, conf, fastBuildOutputFile);
                                            }

                                            bffGenerator.Write(Template.ConfigurationFile.EndSection);

                                            var fileCustomBuildKeys = new Strings();
                                            UtilityMethods.WriteConfigCustomBuildStepsAsGenericExecutable(context.ProjectDirectoryCapitalized, bffGenerator, context.Project, conf,
                                                key =>
                                                {
                                                    if (!fileCustomBuildKeys.Contains(key))
                                                    {
                                                        fileCustomBuildKeys.Add(key);
                                                        bffGenerator.Write(Template.ConfigurationFile.GenericExcutableSection);
                                                    }
                                                    else
                                                    {
                                                        throw new Exception(string.Format("Command key '{0}' duplicates another command.  Command is:\n{1}", key, bffGenerator.Resolver.Resolve(Template.ConfigurationFile.GenericExcutableSection)));
                                                    }
                                                    return false;
                                                });
                                            // These are all pre-build steps, at least in principle, so insert them before the other build steps.
                                            fastBuildTargetSubTargets.InsertRange(0, fileCustomBuildKeys);


                                            foreach (var postBuildEvent in postBuildEvents)
                                            {
                                                if (postBuildEvent.Value is Project.Configuration.BuildStepExecutable)
                                                {
                                                    var execCommand = postBuildEvent.Value as Project.Configuration.BuildStepExecutable;

                                                    using (bffGenerator.Declare("fastBuildPreBuildName", postBuildEvent.Key))
                                                    using (bffGenerator.Declare("fastBuildPrebuildExeFile", UtilityMethods.GetNormalizedPathForPostBuildEvent(project.RootPath, projectPath, execCommand.ExecutableFile)))
                                                    using (bffGenerator.Declare("fastBuildPreBuildInputFile", UtilityMethods.GetNormalizedPathForPostBuildEvent(project.RootPath, projectPath, execCommand.ExecutableInputFileArgumentOption)))
                                                    using (bffGenerator.Declare("fastBuildPreBuildOutputFile", UtilityMethods.GetNormalizedPathForPostBuildEvent(project.RootPath, projectPath, execCommand.ExecutableOutputFileArgumentOption)))
                                                    using (bffGenerator.Declare("fastBuildPreBuildArguments", execCommand.ExecutableOtherArguments))
                                                    using (bffGenerator.Declare("fastBuildPrebuildWorkingPath", UtilityMethods.GetNormalizedPathForPostBuildEvent(project.RootPath, projectPath, execCommand.ExecutableWorkingDirectory)))
                                                    using (bffGenerator.Declare("fastBuildPrebuildUseStdOutAsOutput", execCommand.FastBuildUseStdOutAsOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                                                    {
                                                        bffGenerator.Write(Template.ConfigurationFile.GenericExcutableSection);
                                                    }
                                                }
                                                else if (postBuildEvent.Value is Project.Configuration.BuildStepCopy)
                                                {
                                                    var copyCommand = postBuildEvent.Value as Project.Configuration.BuildStepCopy;

                                                    string sourcePath = UtilityMethods.GetNormalizedPathForPostBuildEvent(project.RootPath, projectPath, copyCommand.SourcePath);
                                                    string destinationPath = UtilityMethods.GetNormalizedPathForPostBuildEvent(project.RootPath, projectPath, copyCommand.DestinationPath);

                                                    using (bffGenerator.Declare("fastBuildCopyAlias", postBuildEvent.Key))
                                                    using (bffGenerator.Declare("fastBuildCopySource", sourcePath))
                                                    using (bffGenerator.Declare("fastBuildCopyDest", destinationPath))
                                                    using (bffGenerator.Declare("fastBuildCopyDirName", postBuildEvent.Key))
                                                    using (bffGenerator.Declare("fastBuildCopyDirSourcePath", Util.EnsureTrailingSeparator(sourcePath)))
                                                    using (bffGenerator.Declare("fastBuildCopyDirDestinationPath", Util.EnsureTrailingSeparator(destinationPath)))
                                                    using (bffGenerator.Declare("fastBuildCopyDirRecurse", copyCommand.IsRecurse.ToString().ToLower()))
                                                    using (bffGenerator.Declare("fastBuildCopyDirPattern", UtilityMethods.GetBffFileCopyPattern(copyCommand.CopyPattern)))
                                                    {
                                                        bffGenerator.Write(Template.ConfigurationFile.CopyFileSection);
                                                    }
                                                }
                                                else if(postBuildEvent.Value is Project.Configuration.BuildStepTest)
                                                {
                                                    var testCommand = postBuildEvent.Value as Project.Configuration.BuildStepTest;

                                                    string fastBuildTestExecutable = UtilityMethods.GetNormalizedPathForPostBuildEvent(project.RootPath, projectPath, conf.PostBuildStepTest.TestExecutable);
                                                    string fastBuildTestOutput = UtilityMethods.GetNormalizedPathForPostBuildEvent(project.RootPath, projectPath, conf.PostBuildStepTest.TestOutput);
                                                    string fastBuildTestWorkingDir = UtilityMethods.GetNormalizedPathForPostBuildEvent(project.RootPath, projectPath, conf.PostBuildStepTest.TestWorkingDir);

                                                    using (bffGenerator.Declare("fastBuildTest", postBuildEvent.Key))
                                                    using (bffGenerator.Declare("fastBuildTestExecutable", fastBuildTestExecutable))
                                                    using (bffGenerator.Declare("fastBuildTestWorkingDir", fastBuildTestWorkingDir))
                                                    using (bffGenerator.Declare("fastBuildTestOutput", fastBuildTestOutput))
                                                    using (bffGenerator.Declare("fastBuildTestArguments", conf.PostBuildStepTest.TestArguments))
                                                    using (bffGenerator.Declare("fastBuildTestTimeOut", conf.PostBuildStepTest.TestTimeOutInSecond.ToString()))
                                                    using (bffGenerator.Declare("fastBuildTestAlwaysShowOutput", conf.PostBuildStepTest.TestAlwaysShowOutput ? "true" : "false"))
                                                    {
                                                        bffGenerator.Write(Template.ConfigurationFile.TestSection);
                                                    }
                                                }
                                                else
                                                {
                                                    throw new Error("error, BuildStep not supported: {0}", postBuildEvent.GetType().FullName);
                                                }
                                            }

                                            // Write Target Alias
                                            if (isLastSubConfig)
                                            {
                                                using (bffGenerator.Declare("fastBuildTargetSubTargets", UtilityMethods.FBuildFormatList(fastBuildTargetSubTargets, 15)))
                                                {
                                                    bffGenerator.Write(Template.ConfigurationFile.TargetSection);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case Project.Configuration.OutputType.None:
                                    {
                                        // Write Target Alias
                                        using (resolver.NewScopedParameter("fastBuildOutputFileShortName", fastBuildOutputFileShortName))
                                        using (resolver.NewScopedParameter("fastBuildTargetSubTargets", UtilityMethods.FBuildFormatList(fastBuildTargetSubTargets, 15)))
                                        {
                                            bffGenerator.Write(Template.ConfigurationFile.TargetSection);
                                        }
                                    }
                                    break;
                            }
                        }

                        confCmdLineOptions["ExceptionHandling"] = previousExceptionSettings;

                        string outputDirectory = Path.GetDirectoryName(fastBuildOutputFile);

                        bffGenerator.ResolveEnvironmentVariables(conf.Platform,
                            new VariableAssignment("ProjectName", projectName),
                            new VariableAssignment("outputDirectory", outputDirectory));

                        subConfigIndex++;
                    }

                    if (configIndex == (configurations.Count - 1) || configurations[configIndex + 1].Platform != conf.Platform)
                    {
                        using (bffGenerator.Declare("fastBuildDefine", GetPlatformSpecificDefine(conf.Platform)))
                            bffGenerator.Write(Template.ConfigurationFile.PlatformEndSection);
                    }
                }
                else if (!confSourceFiles.ContainsKey(conf))
                {
                    Console.WriteLine("[Bff.cs] Unable to find {0} in source files dictionary.", conf.Name);
                }

                ++configIndex;
            }

            // Write all unity sections together at the beginning of the .bff just after the header.
            foreach (var unityFile in _unities)
            {
                using (bffWholeFileGenerator.Declare("unityFile", unityFile.Key))
                    bffWholeFileGenerator.Write(Template.ConfigurationFile.UnitySection);
            }

            // Now combine all the streams.
            bffWholeFileGenerator.Write(bffGenerator.ToString());

            // remove all line that contain RemoveLineTag
            bffWholeFileGenerator.RemoveTaggedLines();
            MemoryStream bffCleanMemoryStream = bffWholeFileGenerator.ToMemoryStream();

            // Write bff file
            FileInfo bffFileInfo = new FileInfo(projectBffFile);

            if (builder.Context.WriteGeneratedFile(project.GetType(), bffFileInfo, bffCleanMemoryStream))
            {
                Project.IncrementFastBuildGeneratedFileCount();
                generatedFiles.Add(bffFileInfo.FullName);
            }
            else
            {
                Project.IncrementFastBuildUpToDateFileCount();
                skipFiles.Add(bffFileInfo.FullName);
            }
        }

        /// <summary>
        /// Method that allows to determine for a speicified dependency if it's a library or an object list. if a dep is within 
        /// the list, the second condition check if objects is present which means that the current dependency is considered to be 
        /// a force objectlist.
        /// </summary>
        /// <param name="dependencies">all the dependencies of a specific project configuration</param>
        /// <param name="dep">additional dependency clear of additional suffix</param>
        /// <returns>return boolean value of presence of a dep within the containing dependencies list</returns>
        private bool IsObjectList(IEnumerable<string> dependencies, string dep)
        {
            return dependencies.Any(dependency => dependency.Contains(dep) && dependency.Contains("objects"));
        }

        Dictionary<Unity, List<Project.Configuration>> _unities = new Dictionary<Unity, List<Project.Configuration>>();

        string GetUnityName(Project.Configuration conf)
        {
            if (_unities.Count > 0)
            {
                var match = _unities.First(x => x.Value.Contains(conf));
                return match.Key.UnityName;
            }

            return null;
        }

        void ConfigureUnities(IGenerationContext context, List<Vcxproj.ProjectFile> sourceFiles)
        {
            var conf = context.Configuration;
            var project = context.Project;

            // Only add unity build to non blobbed projects -> which they will be blobbed by FBuild
            if (!conf.FastBuildBlobbed)
                return;

            const int spaceLength = 42;

            string fastBuildUnityInputFiles = FileGeneratorUtilities.RemoveLineTag;
            string fastBuildUnityInputExcludedfiles = FileGeneratorUtilities.RemoveLineTag;
            string fastBuildUnityPaths = FileGeneratorUtilities.RemoveLineTag;
            string fastBuildUnityInputPattern = FileGeneratorUtilities.RemoveLineTag;

            string fastBuildUnityInputExcludePath = FileGeneratorUtilities.RemoveLineTag;
            string fastBuildUnityCount = FileGeneratorUtilities.RemoveLineTag;

            int unityCount = conf.FastBuildUnityCount > 0 ? conf.FastBuildUnityCount : conf.GeneratableBlobCount;
            if(unityCount > 0)
                fastBuildUnityCount = unityCount.ToString(CultureInfo.InvariantCulture);

            var fastbuildUnityInputExcludePathList = new Strings(project.SourcePathsBlobExclude);

            // Conditional statement depending on the blobbing strategy
            if (conf.FastBuildBlobbingStrategy == Project.Configuration.InputFileStrategy.Include)
            {
                List<string> items = new List<string>();

                foreach(var file in sourceFiles)
                {
                    // TODO: use SourceFileExtension array instead of ".cpp"
                    if((string.Compare(file.FileExtension, ".cpp", StringComparison.OrdinalIgnoreCase) == 0) &&
                       (conf.PrecompSource == null || !file.FileName.EndsWith(conf.PrecompSource, StringComparison.OrdinalIgnoreCase)) &&
                       !conf.ResolvedSourceFilesBlobExclude.Contains(file.FileName))
                    {
                        string sourceFileRelative = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectoryCapitalized, file.FileName));
                        items.Add(sourceFileRelative);
                    }
                }
                fastBuildUnityInputFiles = UtilityMethods.FBuildFormatList(items, spaceLength);
            }
            else
            {
                // Fastbuild will process as unity all files contained in source Root folder and all additional roots.
                var unityInputPaths = new Strings(context.ProjectSourceCapitalized);
                unityInputPaths.AddRange(project.AdditionalSourceRootPaths);

                // check if there's some static blobs lying around to exclude
                if (IsFileInInputPathList(unityInputPaths, conf.BlobPath))
                    fastbuildUnityInputExcludePathList.Add(conf.BlobPath);

                // Remove any excluded paths(exclusion has priority)
                unityInputPaths.RemoveRange(fastbuildUnityInputExcludePathList);
                var unityInputRelativePaths = new Strings(unityInputPaths.Select(p => CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectoryCapitalized, p, true))));
                fastBuildUnityPaths = UtilityMethods.FBuildCollectionFormat(unityInputRelativePaths, spaceLength);

                var excludedSourceFiles = new Strings(conf.ResolvedSourceFilesBlobExclude);
                excludedSourceFiles.AddRange(conf.ResolvedSourceFilesBuildExclude);
                excludedSourceFiles.AddRange(conf.PrecompSourceExclude);

                var excludedSourceFilesRelative = new Strings();

                // Converting the excluded filenames to relative path to the input path so that this
                // can work properly with subst usage when running with fastbuild caching active.
                //
                // Also exclusion checks in fastbuild assume that the exclusion filenames are
                // relative to the .UnityInputPath and checks that paths are ending with the specified
                // path which means that any filename starting with a .. will never be excluded by fastbuild.
                //
                // Note: Ideally fastbuild should expect relative paths to the bff file path instead of the .UnityInputPath but
                // well I guess we are stuck with this.
                foreach (string file in excludedSourceFiles.SortedValues)
                {
                    if (IsFileInInputPathList(unityInputPaths, file))
                        excludedSourceFilesRelative.Add(CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectoryCapitalized, file, true)));
                }
                if (excludedSourceFilesRelative.Count > 0)
                    fastBuildUnityInputExcludedfiles = UtilityMethods.FBuildCollectionFormat(excludedSourceFilesRelative, spaceLength, project.SourceFilesBlobExtensions);
            }

            if (fastBuildUnityInputFiles == FileGeneratorUtilities.RemoveLineTag &&
                fastBuildUnityPaths      == FileGeneratorUtilities.RemoveLineTag)
            {
                // no input path nor files => no unity
                return;
            }

            if (fastbuildUnityInputExcludePathList.Any())
            {
                var unityInputExcludePathRelative = new Strings(fastbuildUnityInputExcludePathList.Select(p => CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectoryCapitalized, p, true))));
                fastBuildUnityInputExcludePath = UtilityMethods.FBuildCollectionFormat(unityInputExcludePathRelative, spaceLength);
            }

            // only write UnityInputPattern if it's not FastBuild's default value of .cpp
            if (project.SourceFilesBlobExtensions.Count != 1 || !project.SourceFilesBlobExtensions.Contains(Unity.DefaultUnityInputPatternExtension))
            {
                var inputPatterns = new Strings(project.SourceFilesBlobExtensions);
                inputPatterns.InsertPrefix("*");
                fastBuildUnityInputPattern = UtilityMethods.FBuildCollectionFormat(inputPatterns, spaceLength);
            }

            Unity unityFile = new Unity
            {
                // Note that the UnityName and UnityOutputPattern are intentionally left empty: they will be set in the Resolve
                UnityOutputPath = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectoryCapitalized, conf.FastBuildUnityPath, true)),
                UnityInputIsolateWritableFiles = conf.FastBuildUnityInputIsolateWritableFiles.ToString().ToLower(),
                UnityInputIsolateWritableFilesLimit = conf.FastBuildUnityInputIsolateWritableFiles ? conf.FastBuildUnityInputIsolateWritableFilesLimit.ToString() : FileGeneratorUtilities.RemoveLineTag,
                UnityPCH = conf.PrecompHeader ?? FileGeneratorUtilities.RemoveLineTag,
                UnityInputExcludePath = fastBuildUnityInputExcludePath,
                UnityNumFiles = fastBuildUnityCount,
                UnityInputPath = fastBuildUnityPaths,
                UnityInputFiles = fastBuildUnityInputFiles,
                UnityInputExcludedFiles = fastBuildUnityInputExcludedfiles,
                UnityInputPattern = fastBuildUnityInputPattern
            };

            // _unities being a dictionary, a new entry will be created only
            // if the combination of options forming that unity was never seen before
            var confListForUnity = _unities.GetValueOrAdd(unityFile, new List<Project.Configuration>());

            // add the current conf in the list that this unity serves
            confListForUnity.Add(conf);
        }

        void ResolveUnities(Project project)
        {
            if (_unities.Count == 0)
                return;

            UnityResolver.ResolveUnities(project, ref _unities);
        }

        // For now, this will do.
        private static Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>> GetDefaultTupleConfig()
        {
            bool isConsumeWinRTExtensions = false;
            bool isCompileAsCLRFile = false;
            bool isCompileAsNonCLRFile = false;
            bool isASMFile = false;
            bool isCompileAsCPPFile = false;
            bool isCompileAsCFile = false;
            bool usePrecomp = true;
            Options.Vc.Compiler.Exceptions exceptionSetting = Options.Vc.Compiler.Exceptions.Disable;
            var tuple = Tuple.Create(usePrecomp, isCompileAsCFile, isCompileAsCPPFile, isCompileAsCLRFile, isConsumeWinRTExtensions, isASMFile, exceptionSetting, isCompileAsNonCLRFile);
            return tuple;
        }


        private static string FormatListPartForTag(List<string> items, int spaceLength, bool addSeparatorAfterList)
        {
            if (items.Count == 0)
                return FileGeneratorUtilities.RemoveLineTag;

            StringBuilder strBuilder = new StringBuilder(1024 * 16);
            string indent = new string(' ', spaceLength);

            // Write all selected items.
            string separator = "," + Environment.NewLine + indent;
            strBuilder.Append(string.Join(separator, items.Select(i => $"'{i}'")));

            if (addSeparatorAfterList)
                strBuilder.Append(",");

            return strBuilder.ToString();
        }

        private static string GetOutputFileName(Project.Configuration conf)
        {
            string targetNamePrefix = "";

            if (conf.OutputExtension == "")
            {
                bool addLibPrefix = false;

                if (conf.Output != Project.Configuration.OutputType.Exe)
                    addLibPrefix = PlatformRegistry.Get<IPlatformBff>(conf.Platform).AddLibPrefix(conf);

                if (addLibPrefix)
                    targetNamePrefix = "lib";
            }
            string targetName = conf.TargetFileFullName;
            return targetNamePrefix + targetName;
        }

        private static void Write(string value, TextWriter writer, Resolver resolver)
        {
            string resolvedValue = resolver.Resolve(value);
            StringReader reader = new StringReader(resolvedValue);
            string str = reader.ReadToEnd();
            writer.Write(str);
            writer.Flush();
        }

        private static Dictionary<Configuration, Dictionary<Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>>, List<Vcxproj.ProjectFile>>>
        GetGeneratedFiles(
            IGenerationContext context,
            List<Project.Configuration> configurations,
            out List<Vcxproj.ProjectFile> filesInNonDefaultSections
        )
        {
            var confSubConfigs = new Dictionary<Configuration, Dictionary<Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>>, List<Vcxproj.ProjectFile>>>();
            filesInNonDefaultSections = new List<Vcxproj.ProjectFile>();

            // Add source files
            List<Vcxproj.ProjectFile> allFiles = new List<Vcxproj.ProjectFile>();
            Strings projectFiles = context.Project.GetSourceFilesForConfigurations(configurations);
            foreach (string file in projectFiles)
            {
                Vcxproj.ProjectFile projectFile = new Vcxproj.ProjectFile(context, file);
                allFiles.Add(projectFile);
            }
            allFiles.Sort((l, r) => string.Compare(l.FileNameProjectRelative, r.FileNameProjectRelative, StringComparison.InvariantCulture));

            List<Vcxproj.ProjectFile> sourceFiles = new List<Vcxproj.ProjectFile>();
            foreach (Vcxproj.ProjectFile projectFile in allFiles)
            {
                if (context.Project.SourceFilesCompileExtensions.Contains(projectFile.FileExtension) ||
                    (String.Compare(projectFile.FileExtension, ".rc", StringComparison.OrdinalIgnoreCase) == 0))
                    sourceFiles.Add(projectFile);
            }

            foreach (Vcxproj.ProjectFile file in sourceFiles)
            {
                foreach (Project.Configuration conf in configurations)
                {
                    bool isExcludeFromBuild = conf.ResolvedSourceFilesBuildExclude.Contains(file.FileName);
                    if (!isExcludeFromBuild)
                    {
                        bool isDontUsePrecomp = conf.PrecompSourceExclude.Contains(file.FileName) ||
                                                conf.PrecompSourceExcludeFolders.Any(folder => file.FileName.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) ||
                                                conf.PrecompSourceExcludeExtension.Contains(file.FileExtension);
                        bool isCompileAsCFile = conf.ResolvedSourceFilesWithCompileAsCOption.Contains(file.FileName);
                        bool isCompileAsCPPFile = conf.ResolvedSourceFilesWithCompileAsCPPOption.Contains(file.FileName);
                        bool isCompileAsCLRFile = conf.ResolvedSourceFilesWithCompileAsCLROption.Contains(file.FileName);
                        bool isCompileAsNonCLRFile = conf.ResolvedSourceFilesWithCompileAsNonCLROption.Contains(file.FileName);
                        bool isConsumeWinRTExtensions = (conf.ConsumeWinRTExtensions.Contains(file.FileName) ||
                                                        conf.ResolvedSourceFilesWithCompileAsWinRTOption.Contains(file.FileName)) &&
                                                        !(conf.ExcludeWinRTExtensions.Contains(file.FileName) ||
                                                        conf.ResolvedSourceFilesWithExcludeAsWinRTOption.Contains(file.FileName));
                        bool isASMFile = String.Compare(file.FileExtension, ".asm", StringComparison.OrdinalIgnoreCase) == 0;

                        Options.Vc.Compiler.Exceptions exceptionSetting = conf.GetExceptionSettingForFile(file.FileName);

                        if (isCompileAsCLRFile || isConsumeWinRTExtensions)
                            isDontUsePrecomp = true;
                        if (String.Compare(file.FileExtension, ".c", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            isDontUsePrecomp = true;
                            isCompileAsCFile = true;
                        }

                        var tuple = Tuple.Create(
                            !isDontUsePrecomp,
                            isCompileAsCFile,
                            isCompileAsCPPFile,
                            isCompileAsCLRFile,
                            isConsumeWinRTExtensions,
                            isASMFile,
                            exceptionSetting,
                            isCompileAsNonCLRFile);

                        Dictionary<Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>>, List<Vcxproj.ProjectFile>> subConfigs = null;
                        if (!confSubConfigs.TryGetValue(conf, out subConfigs))
                        {
                            subConfigs = new Dictionary<Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>>, List<Vcxproj.ProjectFile>>();
                            confSubConfigs.Add(conf, subConfigs);
                        }
                        List<Vcxproj.ProjectFile> subConfigFiles = null;
                        if (!subConfigs.TryGetValue(tuple, out subConfigFiles))
                        {
                            subConfigFiles = new List<Vcxproj.ProjectFile>();
                            subConfigs.Add(tuple, subConfigFiles);
                        }
                        subConfigFiles.Add(file);

                        var defaultTuple = GetDefaultTupleConfig();
                        if (!tuple.Equals(defaultTuple))
                        {
                            filesInNonDefaultSections.Add(file);
                        }
                    }
                }
            }

            // Check if we need to add a compatible config for unity build - For now this is limited to C++ files compiled with no special options.... 
            foreach (Project.Configuration conf in configurations)
            {
                if (conf.FastBuildBlobbed)
                {
                    // For now, this will do.
                    var tuple = GetDefaultTupleConfig();

                    Dictionary<Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>>, List<Vcxproj.ProjectFile>> subConfigs = null;
                    if (!confSubConfigs.TryGetValue(conf, out subConfigs))
                    {
                        subConfigs = new Dictionary<Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>>, List<Vcxproj.ProjectFile>>();
                        confSubConfigs.Add(conf, subConfigs);
                    }
                    List<Vcxproj.ProjectFile> subConfigFiles = null;
                    if (!subConfigs.TryGetValue(tuple, out subConfigFiles))
                    {
                        subConfigFiles = new List<Vcxproj.ProjectFile>();
                        subConfigs.Add(tuple, subConfigFiles);
                    }
                }
            }

            return confSubConfigs;
        }

        private bool IsFileInInputPathList(Strings inputPaths, string path)
        {
            // Convert each of file paths to each of the input paths and try to
            // find the first one not starting from ..(ie the file is in the tested input path)
            foreach (string inputAbsPath in inputPaths)
            {
                string sourceFileRelativeTmp = Util.PathGetRelative(inputAbsPath, path, true);
                if (!sourceFileRelativeTmp.StartsWith(".."))
                    return true;
            }

            return false;
        }
    }
}
