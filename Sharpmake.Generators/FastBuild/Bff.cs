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
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators.FastBuild
{
    public partial class Bff : IProjectGenerator
    {
        private class BffGenerationContext : IBffGenerationContext
        {
            private Resolver _envVarResolver;

            public Builder Builder { get; }

            public Project Project { get; }

            public Project.Configuration Configuration { get; set; }

            public IReadOnlyList<Project.Configuration> ProjectConfigurations { get; }

            public string ProjectDirectory { get; }

            public Options.ExplicitOptions Options { get; set; } = new Options.ExplicitOptions();

            public IDictionary<string, string> CommandLineOptions { get; set; } = new ProjectOptionsGenerator.VcxprojCmdLineOptions();

            public DevEnv DevelopmentEnvironment => Configuration.Compiler;

            public string ProjectDirectoryCapitalized { get; }

            public string ProjectSourceCapitalized { get; }

            public bool PlainOutput { get { return false; } }

            public Resolver EnvironmentVariableResolver
            {
                get
                {
                    Debug.Assert(_envVarResolver != null);
                    return _envVarResolver;
                }
                set
                {
                    _envVarResolver = value;
                }
            }

            public IReadOnlyDictionary<Platform, IPlatformBff> PresentPlatforms { get; }

            public BffGenerationContext(Builder builder, Project project, string projectDir, IEnumerable<Project.Configuration> projectConfigurations)
            {
                Builder = builder;
                Project = project;
                ProjectDirectory = projectDir;
                ProjectDirectoryCapitalized = Util.GetCapitalizedPath(projectDir);
                ProjectSourceCapitalized = Util.GetCapitalizedPath(project.SourceRootPath);
                ProjectConfigurations = projectConfigurations as IReadOnlyList<Project.Configuration>;
                PresentPlatforms = ProjectConfigurations.Select(conf => conf.Platform).Distinct().ToDictionary(p => p, PlatformRegistry.Get<IPlatformBff>);
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

        // NOTE: The dot slash prefix is a workaround because FastBuild sometimes resolve the current bff dir as an empty string,
        // which if followed by a directory separator could create an invalid path...
        // Once the bug is fixed upstream, nuke it!
        public static readonly string CurrentBffPathKey = "." + Path.DirectorySeparatorChar + "$_CURRENT_BFF_DIR_$";

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

        public static string GetShortProjectName(Project project, Project.Configuration conf)
        {
            string platformString = conf.Platform.ToString();
            if (conf.Platform != Platform.win64) // this is to reduce changes compared to old format
            {
                // use custom platform name if a reserved platform or append it if different
                string fullPlatformString = Util.GetPlatformString(conf.Platform, project, conf.Target, isForSolution: false).ToLowerInvariant();
                if (conf.Platform >= Platform._reserved9)
                    platformString = fullPlatformString;
                else if (!fullPlatformString.Equals(platformString, StringComparison.OrdinalIgnoreCase))
                    platformString += "_" + fullPlatformString;
            }

            string dirtyConfigName = string.Join("_", project.Name, conf.Name, platformString);
            return string.Join("_", dirtyConfigName.Split(new[] { ' ', ':', '.' }, StringSplitOptions.RemoveEmptyEntries));
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
        }

        private static ConcurrentDictionary<DevEnv, string> s_LatestTargetPlatformVersions = new ConcurrentDictionary<DevEnv, string>();

        /// <summary>
        /// Find the latest usable kit root
        /// </summary>
        /// <param name="devEnv"></param>
        /// <returns></returns>
        private static string GetLatestTargetPlatformVersion(DevEnv devEnv)
        {
            string value;
            if (!s_LatestTargetPlatformVersions.TryGetValue(devEnv, out value))
            {
                value = FileGeneratorUtilities.RemoveLineTag;
                KitsRootEnum kitsRootVersion = KitsRootPaths.GetUseKitsRootForDevEnv(devEnv);
                if (kitsRootVersion != KitsRootEnum.KitsRoot81)
                {
                    string kitRoot = KitsRootPaths.GetRoot(kitsRootVersion);
                    Options.Vc.General.WindowsTargetPlatformVersion[] platformVersionsEnumValues = (Options.Vc.General.WindowsTargetPlatformVersion[])Enum.GetValues(Options.Vc.General.WindowsTargetPlatformVersion.Latest.GetType());

                    foreach (var version in platformVersionsEnumValues.Reverse())
                    {
                        string binPath = Path.Combine(kitRoot, "bin", version.ToVersionString());
                        if (Directory.Exists(binPath))
                        {
                            // Stop once we found something
                            value = version.ToVersionString();
                            break;
                        }
                    }
                }
                s_LatestTargetPlatformVersions.TryAdd(devEnv, value);
            }
            return value;
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
            var context = new BffGenerationContext(builder, project, projectPath, configurations);
            string projectBffFile = Bff.GetBffFileName(projectPath, firstConf.BffFileName); // TODO: bff file name could be different per conf, hence we would generate more than one file
            string fastBuildClrSupport = Util.IsDotNet(firstConf) ? "/clr" : FileGeneratorUtilities.RemoveLineTag;
            List<Vcxproj.ProjectFile> filesInNonDefaultSection;
            var confSourceFiles = GetGeneratedFiles(context, configurations, out filesInNonDefaultSection);

            // Generate all configuration options onces...
            var options = new Dictionary<Project.Configuration, Options.ExplicitOptions>();
            var cmdLineOptions = new Dictionary<Project.Configuration, ProjectOptionsGenerator.VcxprojCmdLineOptions>();
            var additionalDependenciesPerConf = new Dictionary<Project.Configuration, OrderableStrings>();
            var projectOptionsGen = new ProjectOptionsGenerator();
            foreach (Project.Configuration conf in configurations)
            {
                context.Options = new Options.ExplicitOptions();
                context.CommandLineOptions = new ProjectOptionsGenerator.VcxprojCmdLineOptions();
                context.Configuration = conf;

                GenerateBffOptions(projectOptionsGen, context, additionalDependenciesPerConf);

                options.Add(conf, context.Options);
                cmdLineOptions.Add(conf, (ProjectOptionsGenerator.VcxprojCmdLineOptions)context.CommandLineOptions);

                // Validation of unsupported cases
                if (conf.EventPreLink.Count > 0)
                    throw new Error("Sharpmake-FastBuild : Pre-Link Events not yet supported.");
                if (context.Options["IgnoreImportLibrary"] == "true")
                    throw new Error("Sharpmake-FastBuild : IgnoreImportLibrary not yet supported.");

                if (conf.Output != Project.Configuration.OutputType.None && conf.FastBuildBlobbed)
                {
                    ConfigureUnities(context, confSourceFiles);
                }
            }

            ResolveUnities(project, projectPath);

            // Start writing Bff
            Resolver resolver = new Resolver();
            var bffGenerator = new FileGenerator(resolver);
            var bffWholeFileGenerator = new FileGenerator(resolver);

            using (bffWholeFileGenerator.Declare("fastBuildProjectName", projectName))
            {
                bffWholeFileGenerator.Write(Template.ConfigurationFile.HeaderFile);
            }

            int configIndex = 0;

            var defaultTuple = GetDefaultTupleConfig();

            var configurationsToBuild = confSourceFiles.Keys.OrderBy(x => x.Platform).ToList();
            foreach (Project.Configuration conf in configurationsToBuild)
            {
                if (!conf.Platform.IsSupportedFastBuildPlatform())
                    continue;

                var platformBff = PlatformRegistry.Get<IPlatformBff>(conf.Platform);
                var clangPlatformBff = PlatformRegistry.Query<IClangPlatformBff>(conf.Platform);
                var microsoftPlatformBff = PlatformRegistry.Query<IMicrosoftPlatformBff>(conf.Platform);

                // TODO: really not ideal, refactor and move the properties we need from it someplace else
                var vcxprojPlatform = PlatformRegistry.Query<IPlatformVcxproj>(conf.Platform);

                using (resolver.NewScopedParameter("conf", conf))
                {
                    if (conf.IsBlobbed && conf.FastBuildBlobbed)
                    {
                        throw new Error("Sharpmake-FastBuild: Configuration " + conf + " is configured for blobbing by fastbuild and sharpmake. This is illegal.");
                    }

                    var confSubConfigs = confSourceFiles[conf];
                    ProjectOptionsGenerator.VcxprojCmdLineOptions confCmdLineOptions = cmdLineOptions[conf];

                    // We will need as many "sub"-libraries as subConfigs to generate the final library
                    int subConfigIndex = 0;
                    Strings subConfigObjectList = new Strings();
                    bool isUnity = false;

                    if (configIndex == 0 || configurationsToBuild[configIndex - 1].Platform != conf.Platform)
                    {
                        using (bffGenerator.Declare("fastBuildDefine", GetPlatformSpecificDefine(conf.Platform)))
                            bffGenerator.Write(Template.ConfigurationFile.PlatformBeginSection);
                    }
                    List<string> resourceFilesSections = new List<string>();
                    List<string> embeddedResourceFilesSections = new List<string>();
                    List<string> additionalLibs = new List<string>();

                    Options.ExplicitOptions confOptions = options[conf];

                    bool confUseLibraryDependencyInputs = Options.GetObject<Options.Vc.Linker.UseLibraryDependencyInputs>(conf) == Options.Vc.Linker.UseLibraryDependencyInputs.Enable;
                    string outputFile = confOptions["OutputFile"];

                    bool isOutputTypeExe = conf.Output == Project.Configuration.OutputType.Exe;
                    bool isOutputTypeDll = conf.Output == Project.Configuration.OutputType.Dll;
                    bool isOutputTypeLib = conf.Output == Project.Configuration.OutputType.Lib;
                    bool isOutputTypeExeOrDll = isOutputTypeExe || isOutputTypeDll;

                    OrderableStrings additionalDependencies = additionalDependenciesPerConf[conf];

                    foreach (var tuple in confSubConfigs.Keys)
                    {
                        var scopedOptions = new List<Options.ScopedOption>();

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

                        if (isConsumeWinRTExtensions)
                        {
                            if (isCompileAsCFile)
                                throw new Error("A C file cannot be marked to consume WinRT.");
                            isCompileAsCFile = false;
                        }

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

                        switch (exceptionsSetting)
                        {
                            case Options.Vc.Compiler.Exceptions.Enable:
                                scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "ExceptionHandling", "/EHsc"));
                                break;
                            case Options.Vc.Compiler.Exceptions.EnableWithExternC:
                                scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "ExceptionHandling", "/EHs"));
                                break;
                            case Options.Vc.Compiler.Exceptions.EnableWithSEH:
                                scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "ExceptionHandling", "/EHa"));
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

                        string fastBuildOutputFile = CurrentBffPathKeyCombine(Util.PathGetRelative(projectPath, outputFile, true));
                        fastBuildOutputFile = platformBff.GetOutputFilename(conf.Output, fastBuildOutputFile);

                        bool useObjectLists = confUseLibraryDependencyInputs;

                        string fastBuildOutputFileShortName = GetShortProjectName(project, conf);
                        var fastBuildProjectDependencies = new List<string>();
                        var fastBuildBuildOnlyDependencies = new List<string>();
                        var fastBuildProjectExeUtilityDependencyList = new List<string>();

                        bool mustGenerateLibrary = confSubConfigs.Count > 1 && !useObjectLists && isLastSubConfig && isOutputTypeLib;

                        if (!useObjectLists && confSubConfigs.Count > 1 && !isLastSubConfig)
                        {
                            useObjectLists = true;
                        }

                        if (isOutputTypeExeOrDll)
                        {
                            var orderedProjectDeps = UtilityMethods.GetOrderedFlattenedProjectDependencies(conf, false);
                            foreach (var depProjConfig in orderedProjectDeps)
                            {
                                if (depProjConfig.Project == project)
                                    throw new Error("Sharpmake-FastBuild : Project dependencies refers to itself.");
                                if (!conf.ResolvedDependencies.Contains(depProjConfig))
                                    throw new Error("Sharpmake-FastBuild : dependency was not resolved.");
                                bool isExport = depProjConfig.Project.SharpmakeProjectType == Project.ProjectTypeAttribute.Export;
                                if (isExport)
                                    continue;

                                if (depProjConfig.Output != Project.Configuration.OutputType.Exe &&
                                    depProjConfig.Output != Project.Configuration.OutputType.Utility)
                                {
                                    fastBuildProjectDependencies.Add(GetShortProjectName(depProjConfig.Project, depProjConfig));
                                }
                                else
                                {
                                    fastBuildProjectExeUtilityDependencyList.Add(GetShortProjectName(depProjConfig.Project, depProjConfig));
                                }
                            }

                            orderedProjectDeps = UtilityMethods.GetOrderedFlattenedBuildOnlyDependencies(conf);
                            foreach (var depProjConfig in orderedProjectDeps)
                            {
                                if (depProjConfig.Project == project)
                                    throw new Error("Sharpmake-FastBuild : Project dependencies refers to itself.");

                                bool isExport = depProjConfig.Project.SharpmakeProjectType == Project.ProjectTypeAttribute.Export;
                                if (isExport)
                                    continue;

                                if (depProjConfig.Output != Project.Configuration.OutputType.Exe &&
                                    depProjConfig.Output != Project.Configuration.OutputType.Utility)
                                {
                                    fastBuildBuildOnlyDependencies.Add(GetShortProjectName(depProjConfig.Project, depProjConfig));
                                }
                            }
                        }

                        string librarianAdditionalInputs = FileGeneratorUtilities.RemoveLineTag;

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
                                fastBuildOutputFileShortName += "_" + subConfigIndex.ToString();

                                var staticLibExtension = vcxprojPlatform.StaticLibraryFileExtension;

                                fastBuildOutputFile = Path.ChangeExtension(fastBuildOutputFile, null); // removes the extension
                                fastBuildOutputFile += "_" + subConfigIndex.ToString();

                                if (!staticLibExtension.StartsWith(".", StringComparison.Ordinal))
                                    fastBuildOutputFile += '.';
                                fastBuildOutputFile += staticLibExtension;

                                subConfigObjectList.Add(fastBuildOutputFileShortName);
                                additionalLibs.Add(fastBuildOutputFileShortName + "_objects");
                            }
                            else
                            {
                                StringBuilder result = new StringBuilder();

                                foreach (string subConfigObject in subConfigObjectList)
                                {
                                    if (!useObjectLists && conf.Output != Project.Configuration.OutputType.Dll && conf.Output != Project.Configuration.OutputType.Exe)
                                        fastBuildProjectDependencies.Add(subConfigObject + "_" + outputType);
                                    else
                                        fastBuildProjectDependencies.Add(subConfigObject + "_objects");
                                }
                            }
                        }

                        string fastBuildPCHForceInclude = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildCompilerPCHOptions = isUsePrecomp ? Template.ConfigurationFile.UsePrecomp : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildCompilerPCHOptionsClang = isUsePrecomp ? Template.ConfigurationFile.UsePrecompClang : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildLinkerOutputFile = fastBuildOutputFile;
                        string fastBuildStampExecutable = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildStampArguments = FileGeneratorUtilities.RemoveLineTag;

                        var postBuildEvents = new Dictionary<string, Project.Configuration.BuildStepBase>();

                        Strings preBuildTargets = new Strings();

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
                                preBuildTargets.AddRange(conf.EventPreBuildExecute.Select(e => e.Key));
                                preBuildTargets.AddRange(conf.ResolvedEventPreBuildExe.Select(e => ProjectOptionsGenerator.MakeBuildStepName(conf, e, Vcxproj.BuildStep.PreBuild)));

                                preBuildTargets.AddRange(conf.EventCustomPrebuildExecute.Select(e => e.Key));
                                preBuildTargets.AddRange(conf.ResolvedEventCustomPreBuildExe.Select(e => ProjectOptionsGenerator.MakeBuildStepName(conf, e, Vcxproj.BuildStep.PreBuildCustomAction)));
                            }

                            fastBuildTargetSubTargets.AddRange(fastBuildProjectExeUtilityDependencyList);

                            if (conf.Output == Project.Configuration.OutputType.Lib && useObjectLists)
                            {
                                fastBuildTargetSubTargets.Add(fastBuildOutputFileShortName + "_objects");
                            }
                            else if (conf.Output == Project.Configuration.OutputType.None && project.IsFastBuildAll)
                            {
                                // filter to only get the configurations of projects that were explicitly added, not the dependencies
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

                                if (conf.PostBuildStepTest != null)
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

                        if (additionalDependencies != null && additionalDependencies.Any())
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "AdditionalDependencies", string.Join($"'{Environment.NewLine}                            + ' ", additionalDependencies)));
                        else
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "AdditionalDependencies", FileGeneratorUtilities.RemoveLineTag));

                        string fastBuildConsumeWinRTExtension = isConsumeWinRTExtensions ? "/ZW" : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildUsingPlatformConfig = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildSourceFileType;
                        string clangFileLanguage = String.Empty;

                        if (isCompileAsCFile)
                        {
                            fastBuildUsingPlatformConfig = platformBff.CConfigName(conf);
                            // Do not take cpp Language conformance into account while compiling in C
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "CppLanguageStd", FileGeneratorUtilities.RemoveLineTag));
                            scopedOptions.Add(new Options.ScopedOption(confOptions, "ClangCppLanguageStandard", FileGeneratorUtilities.RemoveLineTag));
                            if (clangPlatformBff != null)
                                clangFileLanguage = "-x c "; // Compiler option to indicate that its a C file
                            fastBuildSourceFileType = "/TC";
                        }
                        else
                        {
                            fastBuildSourceFileType = "/TP";
                            fastBuildUsingPlatformConfig = platformBff.CppConfigName(conf);
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
                        }

                        string llvmClangCompilerOptions = null;
                        if (!isConsumeWinRTExtensions)
                        {
                            var platformToolset = Options.GetObject<Options.Vc.General.PlatformToolset>(conf);
                            if (platformToolset.IsLLVMToolchain() && Options.GetObject<Options.Vc.LLVM.UseClangCl>(context.Configuration) == Options.Vc.LLVM.UseClangCl.Enable)
                            {
                                switch (platformToolset)
                                {
                                    case Options.Vc.General.PlatformToolset.LLVM_vs2012:
                                        // <!-- Set the value of _MSC_VER to claim for compatibility -->
                                        llvmClangCompilerOptions = "-m64 -fmsc-version=1700";
                                        fastBuildPCHForceInclude = @"/FI""[cmdLineOptions.PrecompiledHeaderThrough]""";
                                        break;
                                    case Options.Vc.General.PlatformToolset.LLVM_vs2014:
                                        // <!-- Set the value of _MSC_VER to claim for compatibility -->
                                        llvmClangCompilerOptions = "-m64 -fmsc-version=1900";
                                        fastBuildPCHForceInclude = @"/FI""[cmdLineOptions.PrecompiledHeaderThrough]""";
                                        break;
                                    case Options.Vc.General.PlatformToolset.LLVM:
                                        // <!-- Set the value of _MSC_VER to claim for compatibility -->
                                        // TODO: figure out what version number to put there
                                        // maybe use the DevEnv value
                                        string mscVer = Options.GetString<Options.Clang.Compiler.MscVersion>(conf);
                                        if (string.IsNullOrEmpty(mscVer))
                                        {
                                            Options.Vc.General.PlatformToolset overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
                                            if (Options.WithArgOption<Options.Vc.General.PlatformToolset>.Get<Options.Clang.Compiler.LLVMVcPlatformToolset>(conf, ref overridenPlatformToolset)
                                                && overridenPlatformToolset != Options.Vc.General.PlatformToolset.Default
                                                && !overridenPlatformToolset.IsDefaultToolsetForDevEnv(context.DevelopmentEnvironment))
                                            {
                                                switch (overridenPlatformToolset)
                                                {
                                                    case Options.Vc.General.PlatformToolset.v141:
                                                    case Options.Vc.General.PlatformToolset.v141_xp:
                                                        mscVer = "1910";
                                                        break;
                                                    case Options.Vc.General.PlatformToolset.v142:
                                                        mscVer = "1920";
                                                        break;
                                                    default:
                                                        throw new Error("LLVMVcPlatformToolset! Platform toolset override '{0}' not supported", overridenPlatformToolset);
                                                }
                                            }
                                            else
                                            {
                                                switch (context.DevelopmentEnvironment)
                                                {
                                                    case DevEnv.vs2017:
                                                        mscVer = "1910";
                                                        break;
                                                    case DevEnv.vs2019:
                                                        mscVer = "1920";
                                                        break;
                                                    default:
                                                        throw new Error("Clang-cl used with unsupported DevEnv: " + context.DevelopmentEnvironment.ToString());
                                                }
                                            }
                                        }
                                        llvmClangCompilerOptions = string.Format("-m64 -fmsc-version={0}", mscVer); // -m$(PlatformArchitecture)
                                        fastBuildPCHForceInclude = @"/FI""[cmdLineOptions.PrecompiledHeaderThrough]""";
                                        break;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(llvmClangCompilerOptions))
                        {
                            if (fastBuildAdditionalCompilerOptionsFromCode == FileGeneratorUtilities.RemoveLineTag)
                                fastBuildAdditionalCompilerOptionsFromCode = llvmClangCompilerOptions;
                            else
                                fastBuildAdditionalCompilerOptionsFromCode += " " + llvmClangCompilerOptions;
                        }

                        // c1xx: warning C4199: two-phase name lookup is not supported for C++/CLI, C++/CX, or OpenMP; use /Zc:twoPhase-
                        if (isConsumeWinRTExtensions && context.DevelopmentEnvironment >= DevEnv.vs2017)
                        {
                            if (conf.AdditionalCompilerOptions.Contains("/permissive-") && !conf.AdditionalCompilerOptions.Contains("/Zc:twoPhase-"))
                            {
                                if (fastBuildAdditionalCompilerOptionsFromCode == FileGeneratorUtilities.RemoveLineTag)
                                    fastBuildAdditionalCompilerOptionsFromCode = "/Zc:twoPhase-";
                                else
                                    fastBuildAdditionalCompilerOptionsFromCode += " /Zc:twoPhase-";
                            }
                        }

                        if (conf.ReferencesByName.Count > 0)
                        {
                            throw new Exception("Use ReferencesByPath instead of ReferencesByName for FastBuild support; ");
                        }

                        if (conf.ForceUsingDependencies.Any() || conf.DependenciesForceUsingFiles.Any() || conf.ForceUsingFiles.Any())
                        {
                            StringBuilder builderForceUsingFiles = new StringBuilder();
                            foreach (var fuConfig in conf.ForceUsingDependencies)
                            {
                                builderForceUsingFiles.AppendFormat(@" /FU""{0}.dll""", GetOutputFileName(fuConfig));
                            }
                            foreach (var f in conf.ForceUsingFiles.Union(conf.DependenciesForceUsingFiles))
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
                            linkObjects = confUseLibraryDependencyInputs;
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
                        bool projectHasEmbeddedResources = false;
                        string fastBuildSourceFiles = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildResourceFiles = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildEmbeddedResources = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildEmbeddedOutputPrefix = conf.EmbeddedResourceOutputPrefix;

                        {
                            List<string> fastbuildSourceFilesList = new List<string>();
                            List<string> fastbuildResourceFilesList = new List<string>();
                            List<string> fastbuildEmbeddedResourceFilesList = new List<string>();

                            var sourceFiles = confSubConfigs[tuple];
                            foreach (var sourceFile in sourceFiles)
                            {
                                string sourceFileName = CurrentBffPathKeyCombine(sourceFile.FileNameProjectRelative);

                                if (isUsePrecomp && conf.PrecompSource != null && sourceFile.FileName.EndsWith(conf.PrecompSource, StringComparison.OrdinalIgnoreCase))
                                {
                                    fastBuildPrecompiledSourceFile = sourceFileName;
                                }
                                else if (string.Compare(sourceFile.FileExtension, ".rc", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    if (microsoftPlatformBff != null && microsoftPlatformBff.SupportsResourceFiles)
                                    {
                                        fastbuildResourceFilesList.Add(sourceFileName);
                                        projectHasResourceFiles = true;
                                    }
                                }
                                else if (string.Compare(sourceFile.FileExtension, ".resx", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    if (microsoftPlatformBff != null && microsoftPlatformBff.SupportsResourceFiles)
                                    {
                                        fastbuildEmbeddedResourceFilesList.Add(sourceFileName);
                                        projectHasEmbeddedResources = true;
                                    }
                                }
                                else
                                {
                                    bool isSourceFileExtension = project.SourceFilesCompileExtensions.Contains(sourceFile.FileExtension);
                                    bool isBlobbed = project.SourceFilesBlobExtensions.Contains(sourceFile.FileExtension);
                                    if ((isSourceFileExtension && !isBlobbed) ||
                                        conf.ResolvedSourceFilesBlobExclude.Contains(sourceFile.FileName) ||
                                        isNoBlobImplicitConfig ||
                                        !isUnity)
                                    {
                                        if (!IsFileInInputPathList(fullInputPaths, sourceFile.FileName))
                                            fastbuildSourceFilesList.Add(sourceFileName);
                                    }
                                }
                            }
                            fastBuildSourceFiles = UtilityMethods.FBuildFormatList(fastbuildSourceFilesList, 32);
                            fastBuildResourceFiles = UtilityMethods.FBuildFormatList(fastbuildResourceFilesList, 30);
                            fastBuildEmbeddedResources = UtilityMethods.FBuildFormatList(fastbuildEmbeddedResourceFilesList, 30);
                        }

                        Strings fastBuildPreBuildDependencies = new Strings();
                        UniqueList<Project.Configuration> orderedForceUsingDeps = UtilityMethods.GetOrderedFlattenedProjectDependencies(conf, false, true);
                        fastBuildPreBuildDependencies.AddRange(orderedForceUsingDeps.Select(dep => GetShortProjectName(dep.Project, dep)));
                        fastBuildPreBuildDependencies.AddRange(preBuildTargets);

                        if (projectHasResourceFiles)
                            resourceFilesSections.Add(fastBuildOutputFileShortName + "_resources");
                        if (projectHasEmbeddedResources)
                        {
                            embeddedResourceFilesSections.Add(fastBuildOutputFileShortName + "_embedded");
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "EmbedResources", "/ASSEMBLYRESOURCE:\"%3\""));
                        }
                        else
                        {
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "EmbedResources", FileGeneratorUtilities.RemoveLineTag));
                        }

                        if (mustGenerateLibrary)
                        {
                            librarianAdditionalInputs = UtilityMethods.FBuildFormatList(additionalLibs, 33);
                        }

                        // It is useless to have an input pattern defined if there is no input path
                        if (fastBuildInputPath == FileGeneratorUtilities.RemoveLineTag)
                            fastBuildCompilerInputPattern = FileGeneratorUtilities.RemoveLineTag;

                        fastBuildProjectDependencies.AddRange(resourceFilesSections);
                        fastBuildProjectDependencies.Add("[fastBuildOutputFileShortName]_objects");
                        string fastBuildObjectListEmbeddedResources = FormatListPartForTag(embeddedResourceFilesSections, 32, true);

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
                                    using (bffGenerator.Declare("fastBuildInputPath", fastBuildInputPath))
                                    using (bffGenerator.Declare("fastBuildCompilerInputPattern", fastBuildCompilerInputPattern))
                                    using (bffGenerator.Declare("fastBuildInputExcludedFiles", fastBuildInputExcludedFiles))
                                    using (bffGenerator.Declare("fastBuildSourceFiles", fastBuildSourceFiles))
                                    using (bffGenerator.Declare("fastBuildResourceFiles", fastBuildResourceFiles))
                                    using (bffGenerator.Declare("fastBuildEmbeddedResources", fastBuildEmbeddedResources))
                                    using (bffGenerator.Declare("fastBuildPrecompiledSourceFile", fastBuildPrecompiledSourceFile))
                                    using (bffGenerator.Declare("fastBuildProjectDependencies", UtilityMethods.FBuildFormatList(fastBuildProjectDependencies, 30)))
                                    using (bffGenerator.Declare("fastBuildBuildOnlyDependencies", UtilityMethods.FBuildFormatList(fastBuildBuildOnlyDependencies, 30)))
                                    using (bffGenerator.Declare("fastBuildPreBuildTargets", UtilityMethods.FBuildFormatList(fastBuildPreBuildDependencies.ToList(), 28)))
                                    using (bffGenerator.Declare("fastBuildObjectListEmbeddedResources", fastBuildObjectListEmbeddedResources))
                                    using (bffGenerator.Declare("fastBuildCompilerPCHOptions", fastBuildCompilerPCHOptions))
                                    using (bffGenerator.Declare("fastBuildCompilerPCHOptionsClang", fastBuildCompilerPCHOptionsClang))
                                    using (bffGenerator.Declare("fastBuildPCHForceInclude", isUsePrecomp ? fastBuildPCHForceInclude : FileGeneratorUtilities.RemoveLineTag))
                                    using (bffGenerator.Declare("fastBuildConsumeWinRTExtension", fastBuildConsumeWinRTExtension))
                                    using (bffGenerator.Declare("fastBuildOutputType", outputType))
                                    using (bffGenerator.Declare("fastBuildLibrarianAdditionalInputs", librarianAdditionalInputs))
                                    using (bffGenerator.Declare("fastBuildCompileAsC", fastBuildCompileAsC))
                                    using (bffGenerator.Declare("fastBuildUnityName", fastBuildUnityName ?? FileGeneratorUtilities.RemoveLineTag))
                                    using (bffGenerator.Declare("fastBuildClangFileLanguage", clangFileLanguage))
                                    using (bffGenerator.Declare("fastBuildDeoptimizationWritableFiles", fastBuildDeoptimizationWritableFiles))
                                    using (bffGenerator.Declare("fastBuildDeoptimizationWritableFilesWithToken", fastBuildDeoptimizationWritableFilesWithToken))
                                    using (bffGenerator.Declare("fastBuildCompilerForceUsing", fastBuildCompilerForceUsing))
                                    using (bffGenerator.Declare("fastBuildSourceFileType", fastBuildSourceFileType))
                                    using (bffGenerator.Declare("fastBuildAdditionalCompilerOptionsFromCode", fastBuildAdditionalCompilerOptionsFromCode))
                                    using (bffGenerator.Declare("fastBuildStampExecutable", fastBuildStampExecutable))
                                    using (bffGenerator.Declare("fastBuildStampArguments", fastBuildStampArguments))
                                    using (bffGenerator.Declare("fastBuildEmbeddedOutputPrefix", fastBuildEmbeddedOutputPrefix))
                                    {
                                        if (projectHasResourceFiles)
                                        {
                                            bffGenerator.Write(Template.ConfigurationFile.ResourcesBeginSection);
                                            bffGenerator.Write(Template.ConfigurationFile.ResourceCompilerExtraOptions);
                                            bffGenerator.Write(Template.ConfigurationFile.ResourceCompilerOptions);
                                            bffGenerator.Write(Template.ConfigurationFile.EndSection);
                                        }

                                        if (projectHasEmbeddedResources)
                                        {
                                            // Only declare the compiler here to avoid potential exceptions caused by GetFragment in targets without a .Net framework
                                            using (bffGenerator.Declare("fastBuildEmbeddedResourceCompiler", KitsRootPaths.GetNETFXToolsDir(conf.Target.GetFragment<DotNetFramework>()) + "resgen.exe"))
                                            {
                                                bffGenerator.Write(Template.ConfigurationFile.EmbeddedResourcesBeginSection);
                                                bffGenerator.Write(Template.ConfigurationFile.EmbeddedResourceCompilerOptions);
                                                bffGenerator.Write(Template.ConfigurationFile.EndSection);
                                            }
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

                                            if (fastBuildPreBuildDependencies.Any())
                                                bffGenerator.Write(Template.ConfigurationFile.PreBuildDependencies);

                                            bffGenerator.Write(Template.ConfigurationFile.EndSection);
                                        }

                                        if (isOutputTypeDll && !isLastSubConfig)
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

                                            if (fastBuildPreBuildDependencies.Any())
                                                bffGenerator.Write(Template.ConfigurationFile.PreBuildDependencies);

                                            bffGenerator.Write(Template.ConfigurationFile.EndSection);
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

                                                    if (!useObjectLists)
                                                    {
                                                        bffGenerator.Write(Template.ConfigurationFile.LibrarianAdditionalInputs);
                                                        bffGenerator.Write(Template.ConfigurationFile.LibrarianOptions);
                                                    }
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
                                                        if (!useObjectLists)
                                                        {
                                                            bffGenerator.Write(Template.ConfigurationFile.LibrarianAdditionalInputs);
                                                            bffGenerator.Write(Template.ConfigurationFile.LibrarianOptionsClang);
                                                        }
                                                    }
                                                }

                                                if (fastBuildPreBuildDependencies.Any())
                                                    bffGenerator.Write(Template.ConfigurationFile.PreBuildDependencies);
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
                                                        bffGenerator.Write(Template.ConfigurationFile.GenericExecutableSection);
                                                    }
                                                    else
                                                    {
                                                        throw new Exception(string.Format("Command key '{0}' duplicates another command.  Command is:\n{1}", key, bffGenerator.Resolver.Resolve(Template.ConfigurationFile.GenericExecutableSection)));
                                                    }
                                                    return false;
                                                });
                                            // These are all pre-build steps, at least in principle, so insert them before the other build steps.
                                            fastBuildTargetSubTargets.InsertRange(0, fileCustomBuildKeys);

                                            // Convert build steps to Bff resolvable objects
                                            var resolvableBuildSteps = UtilityMethods.GetResolvablesFromBuildSteps(postBuildEvents);
                                            // Resolve objects using the current project path
                                            var resolvedBuildSteps = resolvableBuildSteps.Select(b => b.Resolve(project.RootPath, projectPath, resolver));

                                            foreach (var buildStep in resolvedBuildSteps)
                                            {
                                                bffGenerator.Write(buildStep);
                                            }

                                            // Write Target Alias
                                            if (isLastSubConfig)
                                            {
                                                string genLibName = "'" + fastBuildOutputFileShortName + "_" + outputType + "'";
                                                using (bffGenerator.Declare("fastBuildTargetSubTargets", mustGenerateLibrary ? genLibName : UtilityMethods.FBuildFormatList(fastBuildTargetSubTargets, 15)))
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

                        scopedOptions.ForEach(scopedOption => scopedOption.Dispose());

                        string outputDirectory = Path.GetDirectoryName(fastBuildOutputFile);

                        bffGenerator.ResolveEnvironmentVariables(conf.Platform,
                            new VariableAssignment("ProjectName", projectName),
                            new VariableAssignment("outputDirectory", outputDirectory));

                        subConfigIndex++;
                    }

                    if (configIndex == (configurationsToBuild.Count - 1) || configurationsToBuild[configIndex + 1].Platform != conf.Platform)
                    {
                        using (bffGenerator.Declare("fastBuildDefine", GetPlatformSpecificDefine(conf.Platform)))
                            bffGenerator.Write(Template.ConfigurationFile.PlatformEndSection);
                    }
                }

                ++configIndex;
            }

            // Write all unity sections together at the beginning of the .bff just after the header.
            foreach (var unityFile in _unities.Keys.OrderBy(u => u.UnityName))
            {
                using (bffWholeFileGenerator.Declare("unityFile", unityFile))
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

        internal static string CmdLineConvertIncludePathsFunc(IGenerationContext context, Resolver resolver, string include, string prefix)
        {
            // if the include is below the global root, we compute the relative path,
            // otherwise it's probably a system include for which we keep the full path
            string resolvedInclude = resolver.Resolve(include);
            if (resolvedInclude.StartsWith(context.Project.RootPath, StringComparison.OrdinalIgnoreCase))
                resolvedInclude = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectory, resolvedInclude, true));
            return $@"{prefix}{Util.DoubleQuotes}{resolvedInclude}{Util.DoubleQuotes}";
        }

        private static void GenerateBffOptions(
            ProjectOptionsGenerator projectOptionsGen,
            BffGenerationContext context,
            Dictionary<Project.Configuration, OrderableStrings> additionalDependenciesPerConf
        )
        {
            // resolve targetPlatformVersion as it may be used in includes
            string targetPlatformVersionString = "";
            if (context.Configuration.Compiler.IsVisualStudio())
            {
                targetPlatformVersionString = GetLatestTargetPlatformVersion(context.Configuration.Compiler);
            }

            var platformBff = context.PresentPlatforms[context.Configuration.Platform];

            var resolverParams = new[] {
                    new VariableAssignment("project", context.Project),
                    new VariableAssignment("target", context.Configuration.Target),
                    new VariableAssignment("conf", context.Configuration),
                    new VariableAssignment("latesttargetplatformversion", targetPlatformVersionString)
                };
            var platformDescriptor = PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform);
            context.EnvironmentVariableResolver = platformDescriptor.GetPlatformEnvironmentResolver(resolverParams);
            projectOptionsGen.GenerateOptions(context);
            platformBff.SelectPreprocessorDefinitionsBff(context);

            FillIncludeDirectoriesOptions(context);

            FillLinkerOptions(context);

            OrderableStrings additionalDependencies = FillLibrariesOptions(context);
            additionalDependenciesPerConf.Add(context.Configuration, additionalDependencies);
        }

        private static void FillIncludeDirectoriesOptions(BffGenerationContext context)
        {
            // TODO: really not ideal, refactor and move the properties we need from it someplace else
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);

            var includePaths = new OrderableStrings(platformVcxproj.GetIncludePaths(context));
            var resourceIncludePaths = new OrderableStrings(platformVcxproj.GetResourceIncludePaths(context));
            context.CommandLineOptions["AdditionalIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalResourceIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalUsingDirectories"] = FileGeneratorUtilities.RemoveLineTag;

            var platformDescriptor = PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform);
            if (context.EnvironmentVariableResolver != null)
            {
                string defaultCmdLineIncludePrefix = platformDescriptor.IsUsingClang ? "-I " : "/I";

                // Fill include dirs
                var dirs = new List<string>();
                dirs.AddRange(includePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p, defaultCmdLineIncludePrefix)));

                var platformIncludePaths = platformVcxproj.GetPlatformIncludePathsWithPrefix(context);
                var platformIncludePathsPrefixed = platformIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p.Path, p.CmdLinePrefix)).ToList();
                dirs.AddRange(platformIncludePathsPrefixed);
                if (dirs.Any())
                    context.CommandLineOptions["AdditionalIncludeDirectories"] = string.Join($"'{Environment.NewLine}            + ' ", dirs);

                // Fill resource include dirs
                var resourceDirs = new List<string>();
                resourceDirs.AddRange(resourceIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p, defaultCmdLineIncludePrefix)));

                if (Options.GetObject<Options.Vc.General.PlatformToolset>(context.Configuration).IsLLVMToolchain() &&
                    Options.GetObject<Options.Vc.LLVM.UseClangCl>(context.Configuration) == Options.Vc.LLVM.UseClangCl.Enable)
                {
                    // with LLVM as toolchain, we are still using the default resource compiler, so we need the default include prefix
                    // TODO: this is not great, ideally we would need the prefix to be per "compiler", and a platform can have many
                    var platformIncludePathsDefaultPrefix = platformIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p.Path, defaultCmdLineIncludePrefix));
                    resourceDirs.AddRange(platformIncludePathsDefaultPrefix);
                }
                else
                {
                    resourceDirs.AddRange(platformIncludePathsPrefixed);
                }

                if (resourceDirs.Any())
                    context.CommandLineOptions["AdditionalResourceIncludeDirectories"] = string.Join($"'{Environment.NewLine}                                    + ' ", resourceDirs);

                // Fill using dirs
                Strings additionalUsingDirectories = Options.GetStrings<Options.Vc.Compiler.AdditionalUsingDirectories>(context.Configuration);
                additionalUsingDirectories.AddRange(context.Configuration.AdditionalUsingDirectories);
                additionalUsingDirectories.AddRange(platformVcxproj.GetCxUsingPath(context));
                if (additionalUsingDirectories.Any())
                {
                    var cmdAdditionalUsingDirectories = additionalUsingDirectories.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p, "/AI"));
                    context.CommandLineOptions["AdditionalUsingDirectories"] = string.Join($"'{Environment.NewLine}            + ' ", cmdAdditionalUsingDirectories);
                }
            }
        }

        private static void FillLinkerOptions(BffGenerationContext context)
        {
            FillEmbeddedNatvisOptions(context);
        }

        private static void FillEmbeddedNatvisOptions(BffGenerationContext context)
        {
            if (context.Configuration.Project.NatvisFiles.Count > 0)
            {
                var cmdNatvisFiles = context.Configuration.Project.NatvisFiles.Select(n => Bff.CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, n, "/NATVIS:"));
                string linkerNatvis = string.Join($"'{Environment.NewLine}                            + ' ", cmdNatvisFiles);

                context.CommandLineOptions["LinkerNatvisFiles"] = linkerNatvis;
            }
            else
            {
                context.CommandLineOptions["LinkerNatvisFiles"] = FileGeneratorUtilities.RemoveLineTag;
            }
        }

        private static OrderableStrings FillLibrariesOptions(BffGenerationContext context)
        {
            OrderableStrings additionalDependencies = null;

            // TODO: really not ideal, refactor and move the properties we need from it someplace else
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);

            var libFiles = new OrderableStrings(context.Configuration.LibraryFiles);
            libFiles.AddRange(context.Configuration.DependenciesOtherLibraryFiles);
            libFiles.AddRange(platformVcxproj.GetLibraryFiles(context));

            if (context.Configuration.Platform.IsMicrosoft())
            {
                Strings delayedDLLs = Options.GetStrings<Options.Vc.Linker.DelayLoadDLLs>(context.Configuration);
                if (delayedDLLs.Any())
                    libFiles.Add("Delayimp.lib");
            }

            libFiles.Sort();

            Strings ignoreSpecificLibraryNames = Options.GetStrings<Options.Vc.Linker.IgnoreSpecificLibraryNames>(context.Configuration);
            ignoreSpecificLibraryNames.ToLower();
            ignoreSpecificLibraryNames.InsertSuffix("." + platformVcxproj.StaticLibraryFileExtension, true);

            context.CommandLineOptions["AdditionalDependencies"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalLibraryDirectories"] = FileGeneratorUtilities.RemoveLineTag;

            if (!(context.Configuration.Output == Project.Configuration.OutputType.None || context.Configuration.Output == Project.Configuration.OutputType.Lib && !context.Configuration.ExportAdditionalLibrariesEvenForStaticLib))
            {
                //AdditionalLibraryDirectories
                //                                            AdditionalLibraryDirectories="dir1;dir2"    /LIBPATH:"dir1" /LIBPATH:"dir2"
                SelectAdditionalLibraryDirectoriesOption(context);

                //AdditionalDependencies
                //                                            AdditionalDependencies="lib1;lib2"      "lib1;lib2" 
                additionalDependencies = SelectAdditionalDependenciesOption(context, libFiles, ignoreSpecificLibraryNames);
            }

            ////IgnoreSpecificLibraryNames
            ////                                            IgnoreDefaultLibraryNames=[lib]         /NODEFAULTLIB:[lib]
            if (ignoreSpecificLibraryNames.Any())
            {
                var result = new StringBuilder();
                foreach (string ignoreLib in ignoreSpecificLibraryNames.SortedValues)
                    result.Append(@"/NODEFAULTLIB:""" + ignoreLib + @""" ");
                result.Remove(result.Length - 1, 1);
                context.CommandLineOptions["IgnoreDefaultLibraryNames"] = result.ToString();
            }
            else
            {
                context.CommandLineOptions["IgnoreDefaultLibraryNames"] = FileGeneratorUtilities.RemoveLineTag;
            }

            return additionalDependencies;
        }

        private static void SelectAdditionalLibraryDirectoriesOption(BffGenerationContext context)
        {
            // TODO: really not ideal, refactor and move the properties we need from it someplace else
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);

            context.CommandLineOptions["AdditionalLibraryDirectories"] = FileGeneratorUtilities.RemoveLineTag;

            var libDirs = new OrderableStrings(context.Configuration.LibraryPaths);
            libDirs.AddRange(context.Configuration.DependenciesOtherLibraryPaths);
            libDirs.AddRange(platformVcxproj.GetLibraryPaths(context));

            libDirs.Sort();

            if (context.EnvironmentVariableResolver != null)
            {
                var configTasks = PlatformRegistry.Get<Project.Configuration.IConfigurationTasks>(context.Configuration.Platform);
                libDirs.AddRange(configTasks.GetPlatformLibraryPaths(context.Configuration));
                if (libDirs.Count > 0)
                {
                    string linkOption;
                    if (!PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform).IsUsingClang)
                        linkOption = @"/LIBPATH:";
                    else
                        linkOption = @"-L ";

                    var cmdAdditionalLibDirectories = libDirs.Select(p => Bff.CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p, linkOption));

                    context.CommandLineOptions["AdditionalLibraryDirectories"] = string.Join($"'{Environment.NewLine}                            + ' ", cmdAdditionalLibDirectories);
                }
            }
        }

        private static OrderableStrings SelectAdditionalDependenciesOption(
            BffGenerationContext context,
            OrderableStrings libraryFiles,
            Strings ignoreSpecificLibraryNames
        )
        {
            // TODO: really not ideal, refactor and move the properties we need from it someplace else
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);

            string platformLibraryExtension = string.Empty;
            string platformOutputLibraryExtension = string.Empty;
            string platformPrefix = string.Empty;
            platformVcxproj.SetupPlatformLibraryOptions(ref platformLibraryExtension, ref platformOutputLibraryExtension, ref platformPrefix);
            string libPrefix = platformVcxproj.GetOutputFileNamePrefix(context, Project.Configuration.OutputType.Lib);

            var additionalDependencies = new OrderableStrings();

            for (int i = 0; i < libraryFiles.Count; ++i)
            {
                string libraryFile = libraryFiles[i];

                // convert all root paths to be relative to the project folder
                if (Path.IsPathRooted(libraryFile))
                {
                    // if the path is below the global root, we compute the relative path, otherwise we keep the full path
                    if (libraryFile.StartsWith(context.Project.RootPath, StringComparison.OrdinalIgnoreCase))
                        additionalDependencies.Add(CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectory, libraryFile, true)), libraryFiles.GetOrderNumber(i));
                    else
                        additionalDependencies.Add(libraryFile, libraryFiles.GetOrderNumber(i));
                }
                else
                {
                    // If not a path, we've got two kinds of way of listing a library:
                    // - With a filename without extension we must add the potential prefix and potential extension.
                    //      Ex:  On clang we add -l (supposedly because the exact file is named lib<library>.a)
                    // - With a filename with a static or shared lib extension (eg. .a/.lib/.so), we shouldn't touch it as it's already set by the script.
                    string extension = Path.GetExtension(libraryFile).ToLower();
                    if (extension.StartsWith(".", StringComparison.Ordinal))
                        extension = extension.Substring(1);

                    // here we could also verify that the path is rooted
                    if (extension != platformVcxproj.StaticLibraryFileExtension && extension != platformVcxproj.SharedLibraryFileExtension)
                    {
                        libraryFile = libPrefix + libraryFile;
                        if (!string.IsNullOrEmpty(platformVcxproj.StaticLibraryFileExtension))
                            libraryFile += "." + platformVcxproj.StaticLibraryFileExtension;
                    }
                    libraryFile = platformPrefix + libraryFile + platformOutputLibraryExtension;

                    // LCTODO: this might be broken, clarify the rules for which this is supposed to work
                    if (!ignoreSpecificLibraryNames.Contains(libraryFile))
                        additionalDependencies.Add(libraryFile);
                    else
                        ignoreSpecificLibraryNames.Remove(libraryFile);
                }
            }

            var finalDependencies = new OrderableStrings();
            if (context.EnvironmentVariableResolver != null)
            {
                var platformAdditionalDependencies = platformVcxproj.GetPlatformLibraryFiles(context);

                // Joins the list of dependencies with a ; and then re-split them after a resolve.
                // We have to do it that way because a token can be resolved into a
                // semicolon -separated list of dependencies.
                var resolvedAdditionalDependencies = new Strings(context.EnvironmentVariableResolver.Resolve(
                        string.Join(";", additionalDependencies.Concat(platformAdditionalDependencies))
                    ).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

                if (resolvedAdditionalDependencies.Any())
                {
                    foreach (string additionalDependency in resolvedAdditionalDependencies)
                        finalDependencies.Add(@"""" + additionalDependency + @"""");
                }
            }
            return finalDependencies;
        }


        /// <summary>
        /// Method that allows to determine for a specified dependency if it's a library or an object list. if a dep is within
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

        private Dictionary<Unity, List<Project.Configuration>> _unities = new Dictionary<Unity, List<Project.Configuration>>();

        private string GetUnityName(Project.Configuration conf)
        {
            if (_unities.Count > 0)
            {
                var match = _unities.First(x => x.Value.Contains(conf));
                return match.Key.UnityName;
            }

            return null;
        }

        private void ConfigureUnities(IGenerationContext context, Dictionary<Project.Configuration, Dictionary<Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>>, List<Vcxproj.ProjectFile>>> confSourceFiles)
        {
            var conf = context.Configuration;
            var unityTuple = GetDefaultTupleConfig();
            var confSubConfigs = confSourceFiles[conf];
            var sourceFiles = confSubConfigs[unityTuple];
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
            if (unityCount > 0)
                fastBuildUnityCount = unityCount.ToString(CultureInfo.InvariantCulture);

            var fastbuildUnityInputExcludePathList = new Strings(project.SourcePathsBlobExclude.Select(Util.GetCapitalizedPath));

            bool srcDirsAreEmpty = true;
            var items = new List<string>();

            // Fastbuild will process as unity all files contained in source Root folder and all additional roots.
            var unityInputPaths = new Strings(context.ProjectSourceCapitalized);
            unityInputPaths.AddRange(project.AdditionalSourceRootPaths.Select(Util.GetCapitalizedPath));

            foreach (var file in sourceFiles)
            {
                bool isBlobbed = project.SourceFilesBlobExtensions.Contains(file.FileExtension);
                if (isBlobbed &&
                   (conf.PrecompSource == null || !file.FileName.EndsWith(conf.PrecompSource, StringComparison.OrdinalIgnoreCase)) &&
                   !conf.ResolvedSourceFilesBlobExclude.Contains(file.FileName))
                {
                    if (conf.FastBuildBlobbingStrategy == Project.Configuration.InputFileStrategy.Include || !IsFileInInputPathList(unityInputPaths, file.FileName))
                    {
                        string sourceFileRelative = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectoryCapitalized, file.FileName));
                        items.Add(sourceFileRelative);
                    }

                    srcDirsAreEmpty = false;
                }
            }

            // Conditional statement depending on the blobbing strategy
            if (items.Count == 0 && srcDirsAreEmpty)
            {
                fastBuildUnityInputFiles = FileGeneratorUtilities.RemoveLineTag;
            }
            else if (conf.FastBuildBlobbingStrategy == Project.Configuration.InputFileStrategy.Include)
            {
                fastBuildUnityInputFiles = UtilityMethods.FBuildFormatList(items, spaceLength);
            }
            else
            {
                fastBuildUnityInputFiles = UtilityMethods.FBuildFormatList(items, spaceLength);

                // check if there's some static blobs lying around to exclude
                if (IsFileInInputPathList(unityInputPaths, conf.BlobPath))
                    fastbuildUnityInputExcludePathList.Add(conf.BlobPath);

                // Remove any excluded paths(exclusion has priority)
                unityInputPaths.RemoveRange(fastbuildUnityInputExcludePathList);
                var unityInputRelativePaths = new Strings(unityInputPaths.Select(
                    p =>
                    {
                        if (p.StartsWith(context.Project.RootPath, StringComparison.OrdinalIgnoreCase))
                            return CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectoryCapitalized, p, true));
                        return p;
                    }
                ));

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
                fastBuildUnityPaths == FileGeneratorUtilities.RemoveLineTag)
            {
                // completely drop the subconfig in case it was only a unity tuple, without any files
                if (sourceFiles.Count == 0)
                    confSubConfigs.Remove(unityTuple);

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

        private void ResolveUnities(Project project, string projectPath)
        {
            if (_unities.Count == 0)
                return;

            UnityResolver.ResolveUnities(project, projectPath, ref _unities);
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
            string targetName = conf.Project is CSharpProject ? conf.TargetFileName : conf.TargetFileFullName;
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

        private static Dictionary<Project.Configuration, Dictionary<Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>>, List<Vcxproj.ProjectFile>>>
        GetGeneratedFiles(
            IGenerationContext context,
            List<Project.Configuration> configurations,
            out List<Vcxproj.ProjectFile> filesInNonDefaultSections
        )
        {
            var confSubConfigs = new Dictionary<Project.Configuration, Dictionary<Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>>, List<Vcxproj.ProjectFile>>>();
            filesInNonDefaultSections = new List<Vcxproj.ProjectFile>();

            // Add source files
            var allFiles = new List<Vcxproj.ProjectFile>();
            Strings projectFiles = context.Project.GetSourceFilesForConfigurations(configurations);
            foreach (string file in projectFiles)
            {
                var projectFile = new Vcxproj.ProjectFile(context, file);
                allFiles.Add(projectFile);
            }
            allFiles.Sort((l, r) => string.Compare(l.FileNameProjectRelative, r.FileNameProjectRelative, StringComparison.InvariantCulture));

            var sourceFiles = new List<Vcxproj.ProjectFile>();
            foreach (var projectFile in allFiles)
            {
                if (context.Project.SourceFilesCompileExtensions.Contains(projectFile.FileExtension) ||
                    (String.Compare(projectFile.FileExtension, ".rc", StringComparison.OrdinalIgnoreCase) == 0) ||
                    (String.Compare(projectFile.FileExtension, ".resx", StringComparison.OrdinalIgnoreCase) == 0))
                    sourceFiles.Add(projectFile);
            }

            foreach (var file in sourceFiles)
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
                if (conf.FastBuildBlobbed && (sourceFiles.Count > 0 || conf.Project.IsFastBuildAll))
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
                if (!sourceFileRelativeTmp.StartsWith("..", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
