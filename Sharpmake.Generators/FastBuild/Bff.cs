// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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
            string platformString = Util.GetSimplePlatformString(conf.Platform);
            if (conf.Platform != Platform.win64) // this is to reduce changes compared to old format
            {
                // Append the toolchain-specific platform name if it differs from the simple name,
                // in order to prevent clashes between different build targets on the same platform
                string vsPlatformString = Util.GetToolchainPlatformString(conf.Platform, project, conf.Target, isForSolution: false);
                if (!vsPlatformString.Equals(platformString, StringComparison.OrdinalIgnoreCase))
                    platformString += "_" + vsPlatformString;
            }

            string dirtyConfigName = string.Join("_", project.Name, conf.Name, platformString);
            return string.Join("_", dirtyConfigName.Split(new[] { ' ', ':', '.' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public static string GetPlatformSpecificDefine(Platform platform)
        {
            string define = PlatformRegistry.Get<IPlatformBff>(platform).BffPlatformDefine;
            if (define == null)
                throw new NotImplementedException($"Please add {Util.GetSimplePlatformString(platform)} specific define for bff sections, ideally the same as ExplicitDefine, to get Intellisense.");

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
        [Flags]
        private enum Languages
        {
            None = 0,
            Asm = 1,
            C = 2,
            CPP = 4,
            ObjC = 8,
            ObjCPP = 16,
            Swift = 32,
            Nasm = 64
        }

        [Flags]
        private enum LanguageFeatures
        {
            None = 0,
            ConsumeWinRTExtensions = 1,
            CLR = 2,
            NonCLR = 4 // TODO: remove this
        }

        private struct SubConfig
        {
            public SubConfig()
            { }

            public bool IsUsePrecomp = true;
            public Languages Languages = Languages.None;
            public LanguageFeatures LanguageFeatures = LanguageFeatures.None;
            public Options.Vc.Compiler.Exceptions Exceptions = Options.Vc.Compiler.Exceptions.Disable;
        }

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
            List<Vcxproj.ProjectFile> filesInNonDefaultSection;
            Dictionary<Project.Configuration, Dictionary<SubConfig, List<Vcxproj.ProjectFile>>> confSourceFiles;
            using (builder.CreateProfilingScope("BffGenerator.Generate:GetGeneratedFiles"))
            {
                confSourceFiles = GetGeneratedFiles(context, configurations, out filesInNonDefaultSection);
            }

            // Generate all configuration options onces...
            var options = new Dictionary<Project.Configuration, Options.ExplicitOptions>();
            var cmdLineOptions = new Dictionary<Project.Configuration, ProjectOptionsGenerator.VcxprojCmdLineOptions>();
            var dependenciesInfoPerConf = new Dictionary<Project.Configuration, DependenciesInfo>();
            ProjectOptionsGenerator projectOptionsGen;
            using (builder.CreateProfilingScope("BffGenerator.Generate:ProjectOptionsGenerator()"))
            {
                projectOptionsGen = new ProjectOptionsGenerator();
            }
            using (builder.CreateProfilingScope("BffGenerator.Generate:confs1"))
            {
                foreach (Project.Configuration conf in configurations)
                {
                    context.Options = new Options.ExplicitOptions();
                    context.CommandLineOptions = new ProjectOptionsGenerator.VcxprojCmdLineOptions();
                    context.Configuration = conf;

                    GenerateBffOptions(projectOptionsGen, context, dependenciesInfoPerConf);

                    options.Add(conf, context.Options);
                    cmdLineOptions.Add(conf, (ProjectOptionsGenerator.VcxprojCmdLineOptions)context.CommandLineOptions);

                    // Validation of unsupported cases
                    if (conf.EventPreLink.Count > 0)
                        throw new Error("Sharpmake-FastBuild : Pre-Link Events not yet supported.");
                    if (context.Options["IgnoreImportLibrary"] == "true" && conf.ExportDllSymbols)
                        throw new Error("Sharpmake-FastBuild : IgnoreImportLibrary not yet supported, set ExportDllSymbols to false for similar behavior.");

                    if (conf.Output != Project.Configuration.OutputType.None && conf.FastBuildBlobbed)
                    {
                        ConfigureUnities(context, confSourceFiles);
                    }
                }
            }
            ResolveUnities(project, projectPath);

            // Start writing Bff
            Resolver resolver = new Resolver();
            var bffGenerator = new FileGenerator(resolver);
            var bffGeneratorProject = new FileGenerator(resolver);
            var bffWholeFileGenerator = new FileGenerator(resolver);

            using (bffWholeFileGenerator.Declare("fastBuildProjectName", projectName))
            {
                bffWholeFileGenerator.Write(Template.ConfigurationFile.HeaderFile);
            }

            int configIndex = 0;

            var allFileCustomBuild = new Dictionary<string, Project.Configuration.CustomFileBuildStepData>();

            Dictionary<string, bool> confBffHasMasters = new Dictionary<string, bool>();

            var configurationsToBuild = confSourceFiles.Keys.OrderBy(x => x.Platform).ToList();
            foreach (Project.Configuration conf in configurationsToBuild)
            {
                if (!conf.Platform.IsSupportedFastBuildPlatform())
                    continue;

                var platformBff = PlatformRegistry.Get<IPlatformBff>(conf.Platform);
                var clangPlatformBff = PlatformRegistry.Query<IClangPlatformBff>(conf.Platform);
                var applePlatformBff = PlatformRegistry.Query<IApplePlatformBff>(conf.Platform);
                var microsoftPlatformBff = PlatformRegistry.Query<IMicrosoftPlatformBff>(conf.Platform);
                var dotNetConf = Util.IsDotNet(conf);

                if (conf.FastBuildMasterBffList.Any())
                    confBffHasMasters[conf.BffFullFileName] = true;
                else
                    confBffHasMasters.TryAdd(conf.BffFullFileName, false);

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
                    bool isOutputTypeAppleApp = conf.Output == Project.Configuration.OutputType.AppleApp;
                    bool isOutputTypeDll = conf.Output == Project.Configuration.OutputType.Dll;
                    bool isOutputTypeLib = conf.Output == Project.Configuration.OutputType.Lib;
                    bool isOutputTypeExeOrDllOrAppleApp = isOutputTypeExe || isOutputTypeDll || isOutputTypeAppleApp;

                    var dependenciesInfo = dependenciesInfoPerConf[conf];
                    OrderableStrings additionalDependencies = dependenciesInfo.AdditionalDependencies;

                    foreach (var subConfig in confSubConfigs.Keys)
                    {
                        var scopedOptions = new List<Options.ScopedOption>();

                        bool isDefaultSubConfig = s_DefaultSubConfig.Equals(subConfig);

                        bool isUsePrecomp = subConfig.IsUsePrecomp && conf.PrecompSource != null;
                        bool isCompileAsCFile = subConfig.Languages.HasFlag(Languages.C);
                        bool isCompileAsCPPFile = subConfig.Languages.HasFlag(Languages.CPP);
                        bool isCompileAsObjCFile = subConfig.Languages.HasFlag(Languages.ObjC);
                        bool isCompileAsObjCPPFile = subConfig.Languages.HasFlag(Languages.ObjCPP);
                        bool isCompileAsSwiftFile = subConfig.Languages.HasFlag(Languages.Swift);
                        bool isASMFileSection = subConfig.Languages.HasFlag(Languages.Asm);
                        bool isNASMFileSection = subConfig.Languages.HasFlag(Languages.Nasm);
                        bool isCompileAsCLRFile = subConfig.LanguageFeatures.HasFlag(LanguageFeatures.CLR);
                        bool isCompileAsNonCLRFile = subConfig.LanguageFeatures.HasFlag(LanguageFeatures.NonCLR);
                        bool isConsumeWinRTExtensions = subConfig.LanguageFeatures.HasFlag(LanguageFeatures.ConsumeWinRTExtensions) || (Options.GetObject<Options.Vc.Compiler.CompileAsWinRT>(conf) == Options.Vc.Compiler.CompileAsWinRT.Enable);
                        Options.Vc.Compiler.Exceptions exceptionsSetting = subConfig.Exceptions;

                        bool isFirstSubConfig = subConfigIndex == 0;
                        bool isLastSubConfig = subConfigIndex == confSubConfigs.Keys.Count - 1;

                        if (isConsumeWinRTExtensions)
                        {
                            if (isCompileAsCFile)
                                throw new Error("A C file cannot be marked to consume WinRT.");
                            isCompileAsCFile = false;
                        }

                        // For now, this will do.
                        if (conf.FastBuildBlobbed && isDefaultSubConfig && !isUnity)
                        {
                            isUnity = true;
                        }
                        else
                        {
                            isUnity = false;
                        }

                        var useClr = dotNetConf && !isCompileAsNonCLRFile || isCompileAsCLRFile;
                        var fastBuildSubConfigClrSupport = useClr ? "/clr" : FileGeneratorUtilities.RemoveLineTag;

                        Trace.Assert(!isCompileAsCLRFile || !isCompileAsNonCLRFile, "Sharpmake-FastBuild : a file cannot be simultaneously compiled with and without the CLR");

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
                        var fastBuildProjectDependencies = new Strings();
                        var fastBuildBuildOnlyDependencies = new Strings();
                        var fastBuildProjectExeUtilityDependencyList = new Strings();
                        var fastBuildTargetSubTargets = new Strings();

                        bool mustGenerateLibrary = confSubConfigs.Count > 1 && !useObjectLists && isLastSubConfig && isOutputTypeLib;

                        if (!useObjectLists && confSubConfigs.Count > 1 && !isLastSubConfig)
                        {
                            useObjectLists = true;
                        }

                        if (isOutputTypeExeOrDllOrAppleApp)
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
                                    depProjConfig.Output != Project.Configuration.OutputType.AppleApp &&
                                    depProjConfig.Output != Project.Configuration.OutputType.Utility)
                                {
                                    string shortProjectName = GetShortProjectName(depProjConfig.Project, depProjConfig);
                                    if (!dependenciesInfo.IgnoredLibraryNames.Contains(depProjConfig.TargetFileFullNameWithExtension))
                                        fastBuildProjectDependencies.Add(shortProjectName + "_LibraryDependency");
                                    if (depProjConfig.EventPostBuildExecute.Count != 0)
                                    {
                                        fastBuildTargetSubTargets.Add(shortProjectName);
                                    }
                                }
                                else if (!depProjConfig.IsExcludedFromBuild)
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
                                    depProjConfig.Output != Project.Configuration.OutputType.AppleApp &&
                                    depProjConfig.Output != Project.Configuration.OutputType.Utility)
                                {
                                    fastBuildBuildOnlyDependencies.Add(GetShortProjectName(depProjConfig.Project, depProjConfig));
                                }
                                else
                                {
                                    fastBuildProjectExeUtilityDependencyList.Add(GetShortProjectName(depProjConfig.Project, depProjConfig));
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
                            case Project.Configuration.OutputType.AppleApp:
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

                                fastBuildOutputFile = Path.ChangeExtension(fastBuildOutputFile, null); // removes the extension
                                fastBuildOutputFile += "_" + subConfigIndex.ToString();
                                fastBuildOutputFile += vcxprojPlatform.StaticLibraryFileFullExtension;

                                subConfigObjectList.Add(fastBuildOutputFileShortName);
                                additionalLibs.Add(fastBuildOutputFileShortName + "_objects");
                            }
                            else
                            {
                                StringBuilder result = new StringBuilder();

                                foreach (string subConfigObject in subConfigObjectList)
                                {
                                    if (!useObjectLists && conf.Output != Project.Configuration.OutputType.Dll && conf.Output != Project.Configuration.OutputType.Exe && conf.Output != Project.Configuration.OutputType.AppleApp)
                                        fastBuildProjectDependencies.Add(subConfigObject + "_" + outputType);
                                    else
                                        fastBuildProjectDependencies.Add(subConfigObject + "_objects");
                                }
                            }
                        }

                        string fastBuildPCHForceInclude = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildCompilerPCHOptions = isUsePrecomp ? Template.ConfigurationFile.UsePrecomp : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildCompilerPCHOptionsClang = isUsePrecomp ? Template.ConfigurationFile.UsePrecompClang : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildCompilerDeoptimizeOptionClang = isCompileAsSwiftFile ? "-Onone" : "-O0";
                        string fastBuildLinkerOutputFile = fastBuildOutputFile;
                        string fastBuildStampExecutable = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildStampArguments = FileGeneratorUtilities.RemoveLineTag;

                        var postBuildEvents = new Dictionary<string, Project.Configuration.BuildStepBase>();

                        Strings preBuildTargets = new Strings();

                        var fastBuildTargetLibraryDependencies = new Strings();
                        {
                            if (isLastSubConfig) // post-build steps on the last subconfig
                            {
                                if (isOutputTypeExe || isOutputTypeAppleApp || conf.ExecuteTargetCopy)
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
                                        fastBuildBuildOnlyDependencies.Add(fastBuildCopyAlias);
                                    }
                                }
                            }

                            // When we have a Library/Dll/Executable section, put the prebuild dependencies there (which is the last subconfig).
                            // Otherwise put it on the first object list
                            var preBuildTargetsOnLastSubconfig = isOutputTypeExeOrDllOrAppleApp || (isOutputTypeLib && !confUseLibraryDependencyInputs);
                            if ((preBuildTargetsOnLastSubconfig && isLastSubConfig) || (!preBuildTargetsOnLastSubconfig && isFirstSubConfig))
                            {
                                // the pre-steps are written in the master bff, we only need to refer their aliases
                                preBuildTargets.AddRange(conf.EventPreBuildExecute.Select(e => e.Key));
                                preBuildTargets.AddRange(conf.ResolvedEventPreBuildExe.Select(e => ProjectOptionsGenerator.MakeBuildStepName(conf, e, Vcxproj.BuildStep.PreBuild, project.RootPath, projectPath)));

                                preBuildTargets.AddRange(conf.EventCustomPrebuildExecute.Select(e => e.Key));
                                preBuildTargets.AddRange(conf.ResolvedEventCustomPreBuildExe.Select(e => ProjectOptionsGenerator.MakeBuildStepName(conf, e, Vcxproj.BuildStep.PreBuildCustomAction, project.RootPath, projectPath)));
                            }

                            fastBuildTargetSubTargets.AddRange(fastBuildProjectExeUtilityDependencyList);

                            if (conf.Output == Project.Configuration.OutputType.Lib && useObjectLists)
                            {
                                string objectList = fastBuildOutputFileShortName + "_objects";
                                fastBuildTargetLibraryDependencies.Add(objectList);
                                fastBuildTargetSubTargets.Add(objectList);
                            }
                            else if (conf.Output == Project.Configuration.OutputType.None && project.IsFastBuildAll)
                            {
                                // filter to only get the configurations of projects that were explicitly added, not the dependencies
                                var minResolvedConf = conf.ResolvedPrivateDependencies.Where(x => conf.UnResolvedPrivateDependencies.ContainsKey(x.Project.GetType()));
                                foreach (var dep in minResolvedConf)
                                {
                                    if (dep.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Export &&
                                        dep.Output != Project.Configuration.OutputType.None &&
                                        dep.Output != Project.Configuration.OutputType.Utility)
                                    {
                                        fastBuildTargetSubTargets.Add(GetShortProjectName(dep.Project, dep));
                                    }
                                }
                            }
                            else
                            {
                                string targetId = fastBuildOutputFileShortName + "_" + outputType;
                                fastBuildTargetLibraryDependencies.Add(targetId);
                                fastBuildTargetSubTargets.Add(targetId);
                            }

                            if (isLastSubConfig) // post-build steps on the last subconfig
                            {
                                foreach (var eventPair in conf.EventPostBuildExecute)
                                {
                                    fastBuildTargetSubTargets.Add(eventPair.Key);
                                    postBuildEvents.Add(eventPair.Key, eventPair.Value);
                                }

                                var extraPlatformEvents = new List<Project.Configuration.BuildStepBase>();
                                if (!FastBuildSettings.FastBuildSupportLinkerStampList && isOutputTypeExeOrDllOrAppleApp)
                                    extraPlatformEvents.AddRange(platformBff.GetExtraStampEvents(conf, fastBuildOutputFile).Select(step => { step.Resolve(resolver); return step; }));

                                extraPlatformEvents.AddRange(platformBff.GetExtraPostBuildEvents(conf, fastBuildOutputFile).Select(step => { step.Resolve(resolver); return step; }));
                                foreach (var buildEvent in extraPlatformEvents.Concat(conf.ResolvedEventPostBuildExe))
                                {
                                    string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, buildEvent, Vcxproj.BuildStep.PostBuild, project.RootPath, projectPath);
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
                                    string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, buildEvent, Vcxproj.BuildStep.PostBuildCustomAction, project.RootPath, projectPath);
                                    fastBuildTargetSubTargets.Add(eventKey);
                                    postBuildEvents.Add(eventKey, buildEvent);
                                }

                                if (conf.PostBuildStepTest != null)
                                {
                                    string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, conf.PostBuildStepTest, Vcxproj.BuildStep.PostBuildCustomAction, project.RootPath, projectPath);
                                    fastBuildTargetSubTargets.Add(eventKey);
                                    postBuildEvents.Add(eventKey, conf.PostBuildStepTest);
                                }
                            }

                            if (conf.Output != Project.Configuration.OutputType.Dll && conf.Output != Project.Configuration.OutputType.Exe && conf.Output != Project.Configuration.OutputType.AppleApp)
                            {
                                foreach (var subConfigObject in subConfigObjectList)
                                {
                                    string subTarget;
                                    if (useObjectLists)
                                        subTarget = subConfigObject + "_objects";
                                    else
                                        subTarget = subConfigObject + "_" + outputType;

                                    if (!fastBuildTargetSubTargets.Contains(subTarget))
                                        fastBuildTargetSubTargets.Add(subTarget);
                                    if (!fastBuildTargetLibraryDependencies.Contains(subTarget))
                                        fastBuildTargetLibraryDependencies.Add(subTarget);
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
                        string clangFileLanguage = string.Empty;

                        if (isCompileAsCFile)
                        {
                            fastBuildUsingPlatformConfig = platformBff.CConfigName(conf);
                            // Do not take Cpp Language conformance into account while compiling in C
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "CppLanguageStd", FileGeneratorUtilities.RemoveLineTag));
                            scopedOptions.Add(new Options.ScopedOption(confOptions, "ClangCppLanguageStandard", FileGeneratorUtilities.RemoveLineTag));
                            // and remove the stdlib specification as well
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "StdLib", FileGeneratorUtilities.RemoveLineTag));
                            // MSVC
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "LanguageStandard", FileGeneratorUtilities.RemoveLineTag));

                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "ClangEnableObjC_ARC", FileGeneratorUtilities.RemoveLineTag));

                            if (clangPlatformBff != null)
                                clangFileLanguage = "-x c "; // Compiler option to indicate that its a C file
                            fastBuildSourceFileType = "/TC";
                        }
                        else if (isCompileAsObjCFile)
                        {
                            // Do not take Cpp Language conformance into account while compiling in objc
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "CppLanguageStd", FileGeneratorUtilities.RemoveLineTag));
                            scopedOptions.Add(new Options.ScopedOption(confOptions, "ClangCppLanguageStandard", FileGeneratorUtilities.RemoveLineTag));
                            // and remove the stdlib specification as well
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "StdLib", FileGeneratorUtilities.RemoveLineTag));
                            // MSVC
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "LanguageStandard", FileGeneratorUtilities.RemoveLineTag));

                            if (clangPlatformBff != null)
                                clangFileLanguage = "-x objective-c ";
                            fastBuildUsingPlatformConfig = platformBff.CConfigName(conf);

                            fastBuildSourceFileType = "";
                        }
                        else if (isCompileAsObjCPPFile)
                        {
                            // Do not take C Language conformance into account while compiling in objcpp
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "CLanguageStd", FileGeneratorUtilities.RemoveLineTag));
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "ClangCLanguageStandard", FileGeneratorUtilities.RemoveLineTag));
                            // MSVC
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "LanguageStandard_C", FileGeneratorUtilities.RemoveLineTag));

                            if (clangPlatformBff != null)
                                clangFileLanguage = "-x objective-c++ ";
                            fastBuildUsingPlatformConfig = platformBff.CppConfigName(conf);

                            fastBuildSourceFileType = "";
                        }
                        else if (isCompileAsSwiftFile)
                        {
                            clangFileLanguage = "";
                            fastBuildUsingPlatformConfig = applePlatformBff.SwiftConfigName(conf);
                            fastBuildSourceFileType = "";
                        }
                        else
                        {
                            // Do not take C Language conformance into account while compiling in Cpp
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "CLanguageStd", FileGeneratorUtilities.RemoveLineTag));
                            scopedOptions.Add(new Options.ScopedOption(confOptions, "ClangCLanguageStandard", FileGeneratorUtilities.RemoveLineTag));
                            // MSVC
                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "LanguageStandard_C", FileGeneratorUtilities.RemoveLineTag));

                            scopedOptions.Add(new Options.ScopedOption(confCmdLineOptions, "ClangEnableObjC_ARC", FileGeneratorUtilities.RemoveLineTag));

                            // if files are specifically c++, we need to add the language flag to make sure the compiler sees them as c++
                            if (isCompileAsCPPFile && clangPlatformBff != null)
                                clangFileLanguage = "-x c++ ";

                            fastBuildSourceFileType = "/TP";
                            fastBuildUsingPlatformConfig = platformBff.CppConfigName(conf);
                        }

                        // TODOANT: Add nasm/masm change
                        if (isASMFileSection)
                        {
                            fastBuildUsingPlatformConfig += Template.ConfigurationFile.MasmConfigNameSuffix;
                        }
                        if (isNASMFileSection)
                        {
                            fastBuildUsingPlatformConfig += Template.ConfigurationFile.NasmConfigNameSuffix;
                        }

                        string fastBuildCompilerExtraOptions = Template.ConfigurationFile.CPPCompilerExtraOptions;
                        if (isASMFileSection)
                        {
                            fastBuildCompilerExtraOptions = Template.ConfigurationFile.MasmCompilerExtraOptions;
                        }
                        if (isNASMFileSection)
                        {
                            fastBuildCompilerExtraOptions = Template.ConfigurationFile.NasmCompilerExtraOptions;
                        }

                        string fastBuildCompilerOptionsDeoptimize = FileGeneratorUtilities.RemoveLineTag;
                        if (!isASMFileSection && conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                            fastBuildCompilerOptionsDeoptimize = Template.ConfigurationFile.CPPCompilerOptionsDeoptimize;

                        string compilerOptions = Template.ConfigurationFile.CompilerOptionsCPP;
                        if (isASMFileSection)
                        {
                            compilerOptions = Template.ConfigurationFile.CompilerOptionsMasm;
                        }
                        if (isNASMFileSection)
                        {
                            compilerOptions = Template.ConfigurationFile.CompilerOptionsNasm;
                        }
                        compilerOptions += Template.ConfigurationFile.CompilerOptionsCommon;

                        string compilerOptionsClang = Template.ConfigurationFile.CompilerOptionsClang;
                        if (isNASMFileSection)
                        {
                            compilerOptionsClang = Template.ConfigurationFile.CompilerOptionsNasm;
                        }
                        compilerOptionsClang += Template.ConfigurationFile.CompilerOptionsCommon;

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
                                if (ShouldMakePathRelative(refByPath, context.Project))
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
                                    case Options.Vc.General.PlatformToolset.LLVM:
                                    case Options.Vc.General.PlatformToolset.ClangCL:
                                        llvmClangCompilerOptions = "-m64"; // -m$(PlatformArchitecture)
                                        fastBuildPCHForceInclude = @"/FI""[cmdLineOptions.PrecompiledHeaderThrough]""";


                                        // <!-- Set the value of _MSC_VER and _MSC_FULL_VER to claim for compatibility -->
                                        Project.Configuration.FastBuildClangMscVersionDetectionType detectionType = conf.FastBuildClangMscVersionDetectionInfo;
                                        string overridenMscVer = Options.GetString<Options.Clang.Compiler.MscVersion>(conf);
                                        Options.Vc.General.PlatformToolset overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
                                        Options.WithArgOption<Options.Vc.General.PlatformToolset>.Get<Options.Clang.Compiler.LLVMVcPlatformToolset>(conf, ref overridenPlatformToolset);

                                        CompilerVersionForClangCl detectedVersion = DetectCompilerVersionForClangCl(
                                            detectionType, overridenMscVer, overridenPlatformToolset, context.DevelopmentEnvironment, conf.Target.GetPlatform());

                                        switch (detectedVersion.versionType)
                                        {
                                            case CompilerVersionForClangClType.MscVersion :
                                                llvmClangCompilerOptions += string.Format(" -fmsc-version={0}", detectedVersion.mscVersion);
                                                break;
                                            case CompilerVersionForClangClType.MsCompatibilityVersion :
                                                llvmClangCompilerOptions += string.Format(" -fms-compatibility-version={0}.{1}.{2}", detectedVersion.msCompatibilityVersion.Major, detectedVersion.msCompatibilityVersion.Minor, detectedVersion.msCompatibilityVersion.Build);
                                                break;
                                        }
                                        
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
                                builderForceUsingFiles.AppendFormat(@" /FU""{0}.dll""", fuConfig.TargetFileFullName);
                            }
                            foreach (var f in conf.ForceUsingFiles.Union(conf.DependenciesForceUsingFiles))
                            {
                                string file = f;
                                if (ShouldMakePathRelative(f, context.Project))
                                    file = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectory, f));

                                builderForceUsingFiles.AppendFormat(@" /FU""{0}""", file);
                            }
                            fastBuildCompilerForceUsing = builderForceUsingFiles.ToString();
                        }

                        if (isOutputTypeExeOrDllOrAppleApp)
                        {
                            var extraPlatformEvents = new List<Project.Configuration.BuildStepExecutable>();
                            if (FastBuildSettings.FastBuildSupportLinkerStampList)
                                extraPlatformEvents.AddRange(platformBff.GetExtraStampEvents(conf, fastBuildOutputFile).Select(step => { step.Resolve(resolver); return step; }));

                            if (conf.PostBuildStampExe != null || conf.PostBuildStampExes.Any() || extraPlatformEvents.Any())
                            {
                                var fastbuildStampExecutableList = new List<string>();
                                var fastBuildStampArgumentsList = new List<string>();

                                foreach (var stampExe in extraPlatformEvents.Concat(conf.PostBuildStampExes.Prepend(conf.PostBuildStampExe)).Where(x => x != null))
                                {
                                    fastbuildStampExecutableList.Add(CurrentBffPathKeyCombine(Util.PathGetRelative(projectPath, stampExe.ExecutableFile, true)));
                                    fastBuildStampArgumentsList.Add(string.Format("{0} {1} {2}",
                                        stampExe.ExecutableInputFileArgumentOption,
                                        stampExe.ExecutableOutputFileArgumentOption,
                                        stampExe.ExecutableOtherArguments));
                                }

                                fastBuildStampExecutable = UtilityMethods.FBuildFormatList(fastbuildStampExecutableList, 30);
                                fastBuildStampArguments = UtilityMethods.FBuildFormatList(fastBuildStampArgumentsList, 30);
                            }
                        }

                        bool linkObjects = false;
                        if (isOutputTypeExeOrDllOrAppleApp)
                        {
                            linkObjects = confUseLibraryDependencyInputs;
                        }

                        Strings fullInputPaths = new Strings();
                        string fastBuildInputPath = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildInputExcludedFiles = FileGeneratorUtilities.RemoveLineTag;
                        {
                            Strings excludedSourceFiles = new Strings();
                            if (isNoBlobImplicitConfig && isDefaultSubConfig)
                            {
                                fullInputPaths.Add(context.ProjectSourceCapitalized);
                                fullInputPaths.AddRange(project.AdditionalSourceRootPaths.Select(Util.GetCapitalizedPath));

                                excludedSourceFiles.AddRange(filesInNonDefaultSection.Select(f => f.FileName));
                            }

                            if (isDefaultSubConfig && conf.FastBuildBlobbingStrategy == Project.Configuration.InputFileStrategy.Exclude && conf.FastBuildBlobbed)
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

                            var sourceFiles = confSubConfigs[subConfig];
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

                        var fileCustomBuildKeys = new Strings();
                        UtilityMethods.WriteConfigCustomBuildStepsAsGenericExecutable(context.ProjectDirectoryCapitalized, bffGenerator, context.Project, conf,
                            key =>
                            {
                                if (!allFileCustomBuild.TryGetValue(key.Description, out var alreadyRegistered))
                                {
                                    allFileCustomBuild.Add(key.Description, key);
                                    bffGenerator.Write(Template.ConfigurationFile.GenericExecutableSection);
                                }
                                else if (key.Executable != alreadyRegistered.Executable ||
                                        key.KeyInput != alreadyRegistered.KeyInput ||
                                        key.Output != alreadyRegistered.Output ||
                                        key.ExecutableArguments != alreadyRegistered.ExecutableArguments)
                                {
                                    throw new Exception(string.Format("Command key '{0}' duplicates another command.  Command is:\n{1}", key, bffGenerator.Resolver.Resolve(Template.ConfigurationFile.GenericExecutableSection)));
                                }

                                fileCustomBuildKeys.Add(key.Description);

                                return false;
                            });

                        Strings fastBuildPreBuildDependencies = new Strings();
                        var orderedForceUsingDeps = UtilityMethods.GetOrderedFlattenedProjectDependencies(conf, false, true);
                        fastBuildPreBuildDependencies.AddRange(orderedForceUsingDeps.Select(dep => GetShortProjectName(dep.Project, dep)));

                        // fastBuildBuildOnlyDependencies only gets added to exe/dll sections.
                        // Add the prebuild steps to fastBuildPreBuildDependencies if we are building a lib
                        if (isOutputTypeExeOrDllOrAppleApp)
                        {
                            fastBuildBuildOnlyDependencies.AddRange(preBuildTargets);
                            fastBuildBuildOnlyDependencies.AddRange(fileCustomBuildKeys);
                        }
                        else if (isOutputTypeLib)
                        {
                            fastBuildPreBuildDependencies.AddRange(preBuildTargets);
                            if (isLastSubConfig)
                                fastBuildPreBuildDependencies.AddRange(fileCustomBuildKeys);
                        }

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

                        string fastBuildInputFilesRootPath = FileGeneratorUtilities.RemoveLineTag;

                        if (conf.FastBuildInputFilesRootPath != null)
                        {
                            fastBuildInputFilesRootPath = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectory, conf.FastBuildInputFilesRootPath));
                        }

                        using (bffGenerator.Declare("conf", conf))
                        using (bffGenerator.Declare("project", project))
                        using (bffGenerator.Declare("target", conf.Target))
                        {
                            switch (conf.Output)
                            {
                                case Project.Configuration.OutputType.Lib:
                                case Project.Configuration.OutputType.Exe:
                                case Project.Configuration.OutputType.AppleApp:
                                case Project.Configuration.OutputType.Dll:
                                    using (bffGenerator.Declare("$(ProjectName)", projectName))
                                    using (bffGenerator.Declare("options", confOptions))
                                    using (bffGenerator.Declare("cmdLineOptions", confCmdLineOptions))
                                    using (bffGenerator.Declare("fastBuildUsingPlatformConfig", "Using( " + fastBuildUsingPlatformConfig + " )"))
                                    using (bffGenerator.Declare("fastBuildProjectName", projectName))
                                    using (bffGenerator.Declare("fastBuildClrSupport", fastBuildSubConfigClrSupport))
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
                                    using (bffGenerator.Declare("fastBuildProjectDependencies", UtilityMethods.FBuildFormatList(fastBuildProjectDependencies.Values, 30)))
                                    using (bffGenerator.Declare("fastBuildBuildOnlyDependencies", UtilityMethods.FBuildFormatList(fastBuildBuildOnlyDependencies.Values, 30)))
                                    using (bffGenerator.Declare("fastBuildPreBuildTargets", UtilityMethods.FBuildFormatList(fastBuildPreBuildDependencies.Values, 28)))
                                    using (bffGenerator.Declare("fastBuildObjectListEmbeddedResources", fastBuildObjectListEmbeddedResources))
                                    using (bffGenerator.Declare("fastBuildCompilerPCHOptions", fastBuildCompilerPCHOptions))
                                    using (bffGenerator.Declare("fastBuildCompilerPCHOptionsClang", fastBuildCompilerPCHOptionsClang))
                                    using (bffGenerator.Declare("fastBuildCompilerDeoptimizeOptionClang", fastBuildCompilerDeoptimizeOptionClang))
                                    using (bffGenerator.Declare("fastBuildPCHForceInclude", isUsePrecomp ? fastBuildPCHForceInclude : FileGeneratorUtilities.RemoveLineTag))
                                    using (bffGenerator.Declare("fastBuildConsumeWinRTExtension", fastBuildConsumeWinRTExtension))
                                    using (bffGenerator.Declare("fastBuildOutputType", outputType))
                                    using (bffGenerator.Declare("fastBuildLibrarianAdditionalInputs", librarianAdditionalInputs))
                                    using (bffGenerator.Declare("fastBuildCompileAsC", fastBuildCompileAsC))
                                    using (bffGenerator.Declare("fastBuildUnityName", fastBuildUnityName ?? FileGeneratorUtilities.RemoveLineTag))
                                    using (bffGenerator.Declare("fastBuildInputFilesRootPath", fastBuildInputFilesRootPath))
                                    using (bffGenerator.Declare("fastBuildClangFileLanguage", clangFileLanguage))
                                    using (bffGenerator.Declare("fastBuildDeoptimizationWritableFiles", fastBuildDeoptimizationWritableFiles))
                                    using (bffGenerator.Declare("fastBuildDeoptimizationWritableFilesWithToken", fastBuildDeoptimizationWritableFilesWithToken))
                                    using (bffGenerator.Declare("fastBuildCompilerForceUsing", fastBuildCompilerForceUsing))
                                    using (bffGenerator.Declare("fastBuildSourceFileType", fastBuildSourceFileType))
                                    using (bffGenerator.Declare("fastBuildAdditionalCompilerOptionsFromCode", fastBuildAdditionalCompilerOptionsFromCode))
                                    using (bffGenerator.Declare("fastBuildStampExecutable", fastBuildStampExecutable))
                                    using (bffGenerator.Declare("fastBuildStampArguments", fastBuildStampArguments))
                                    using (bffGenerator.Declare("fastBuildEmbeddedOutputPrefix", fastBuildEmbeddedOutputPrefix))
                                    using (bffGenerator.Declare("fastbuildConcurrencyGroupName", conf.FastBuildLinkConcurrencyGroup ?? FileGeneratorUtilities.RemoveLineTag))
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

                                        // Exe, DLL and AppleApp will always add an extra objectlist
                                        if (isOutputTypeExeOrDllOrAppleApp && isLastSubConfig // only last subconfig will generate objectlist
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
                                                if (isCompileAsSwiftFile)
                                                    applePlatformBff?.SetupSwiftOptions(bffGenerator);
                                                else  if (!isNASMFileSection)
                                                    clangPlatformBff?.SetupClangOptions(bffGenerator); // TODO: This checks twice if the platform supports Clang -- fix?
                                                else
                                                    bffGenerator.Write(fastBuildCompilerExtraOptions);

                                                if (conf.Platform.IsUsingClang())
                                                {
                                                    if (isUsePrecomp)
                                                        bffGenerator.Write(Template.ConfigurationFile.PCHOptionsClang);
                                                    bffGenerator.Write(compilerOptionsClang);
                                                    if (conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                                                    {
                                                        if (isUsePrecomp)
                                                            bffGenerator.Write(Template.ConfigurationFile.PCHOptionsDeoptimize);
                                                        bffGenerator.Write(Template.ConfigurationFile.ClangCompilerOptionsDeoptimize);
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
                                                if (isCompileAsSwiftFile)
                                                    applePlatformBff?.SetupSwiftOptions(bffGenerator);
                                                else if (!isNASMFileSection)
                                                    clangPlatformBff?.SetupClangOptions(bffGenerator);  // TODO: This checks twice if the platform supports Clang -- fix?
                                                else
                                                    bffGenerator.Write(fastBuildCompilerExtraOptions);

                                                if (conf.Platform.IsUsingClang())
                                                {
                                                    if (isUsePrecomp)
                                                        bffGenerator.Write(Template.ConfigurationFile.PCHOptionsClang);
                                                    bffGenerator.Write(compilerOptionsClang);

                                                    if (conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                                                    {
                                                        if (isUsePrecomp)
                                                            bffGenerator.Write(Template.ConfigurationFile.PCHOptionsDeoptimize);
                                                        bffGenerator.Write(Template.ConfigurationFile.ClangCompilerOptionsDeoptimize);
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
                                                case Project.Configuration.OutputType.AppleApp:
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
                                                    if (isCompileAsSwiftFile)
                                                        applePlatformBff?.SetupSwiftOptions(bffGenerator);
                                                    else if (!isNASMFileSection)
                                                        clangPlatformBff?.SetupClangOptions(bffGenerator);  // TODO: This checks twice if the platform supports Clang -- fix?
                                                    else
                                                        bffGenerator.Write(fastBuildCompilerExtraOptions);;

                                                    if (conf.Platform.IsUsingClang())
                                                    {
                                                        if (isUsePrecomp)
                                                            bffGenerator.Write(Template.ConfigurationFile.PCHOptionsClang);

                                                        bffGenerator.Write(compilerOptionsClang);
                                                        if (conf.FastBuildDeoptimization != Project.Configuration.DeoptimizationWritableFiles.NoDeoptimization)
                                                        {
                                                            if (isUsePrecomp)
                                                                bffGenerator.Write(Template.ConfigurationFile.PCHOptionsDeoptimize);
                                                            bffGenerator.Write(Template.ConfigurationFile.ClangCompilerOptionsDeoptimize);
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

                                            // Resolve node name of the prebuild dependency for PostBuildEvents.
                                            string resolvedSectionNodeIdentifier;
                                            if (beginSectionType == Template.ConfigurationFile.ObjectListBeginSection)
                                            {
                                                resolvedSectionNodeIdentifier = resolver.Resolve("[fastBuildOutputFileShortName]_objects");
                                            }
                                            else
                                            {
                                                resolvedSectionNodeIdentifier = resolver.Resolve("[fastBuildOutputFileShortName]_[fastBuildOutputType]");
                                            }

                                            // Convert build steps to Bff resolvable objects
                                            var resolvableBuildSteps = UtilityMethods.GetBffNodesFromBuildSteps(postBuildEvents, new Strings(resolvedSectionNodeIdentifier));
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
                                                using (bffGenerator.Declare("fastBuildTargetSubTargets", mustGenerateLibrary ? genLibName : UtilityMethods.FBuildFormatList(fastBuildTargetSubTargets.Values, 15)))
                                                using (bffGenerator.Declare("fastBuildOutputFileShortName", fastBuildOutputFileShortName))
                                                using (bffGenerator.Declare("fastBuildTargetLibraryDependencies", mustGenerateLibrary ? genLibName : UtilityMethods.FBuildFormatList(fastBuildTargetLibraryDependencies.Values, 15)))
                                                {
                                                    bffGenerator.Write(Template.ConfigurationFile.TargetSection);
                                                    bffGenerator.Write(Template.ConfigurationFile.TargetForLibraryDependencySection);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case Project.Configuration.OutputType.None:
                                    {
                                        // Write Target Alias
                                        using (bffGenerator.Declare("fastBuildOutputFileShortName", fastBuildOutputFileShortName))
                                        using (bffGenerator.Declare("fastBuildTargetSubTargets", UtilityMethods.FBuildFormatList(fastBuildTargetSubTargets.Values, 15)))
                                        using (bffGenerator.Declare("fastBuildTargetLibraryDependencies", UtilityMethods.FBuildFormatList(fastBuildTargetLibraryDependencies.Values, 15)))
                                        {
                                            bffGenerator.Write(Template.ConfigurationFile.TargetSection);
                                            if (!project.IsFastBuildAll)
                                                bffGenerator.Write(Template.ConfigurationFile.TargetForLibraryDependencySection);
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

                bffGenerator.WriteTo(bffGeneratorProject);
                bffGenerator.Clear();
                ++configIndex;
            }

            foreach (string masterlessBff in confBffHasMasters.Where(x => !x.Value).Select(x => x.Key))
                Builder.Instance.LogWarningLine("Bff {0} doesn't appear in any master bff, it won't be buildable.", masterlessBff + FastBuildSettings.FastBuildConfigFileExtension);

            // Write all unity sections together at the beginning of the .bff just after the header.
            if (_unities.Any())
            {
                foreach (var unityFile in _unities.Keys.OrderBy(u => u.UnityName))
                {
                    using (bffWholeFileGenerator.Declare("unityFile", unityFile))
                        bffWholeFileGenerator.Write(Template.ConfigurationFile.UnitySection);

                    // Record the unities in the autocleanupdb to allow auto removal when they become stale.
                    // Note that can't record them as 'generated', since they are created by FastBuild and not by us.
                    int nbUnities = 1;
                    if (unityFile.UnityNumFiles != FileGeneratorUtilities.RemoveLineTag)
                    {
                        if (!int.TryParse(unityFile.UnityNumFiles, out nbUnities))
                            throw new Error("'{0}' cannot be converted to int!", unityFile.UnityNumFiles);
                    }

                    string outputPattern = unityFile.UnityOutputPattern == FileGeneratorUtilities.RemoveLineTag ? Sharpmake.Generators.FastBuild.Bff.Unity.DefaultUnityOutputPatternExtension : unityFile.UnityOutputPattern;
                    int wildcardIndex = outputPattern.IndexOf('*');
                    if (wildcardIndex == -1)
                        throw new Error("UnityOutputPattern must include a '*', but none was found in '{0}'!", unityFile.UnityNumFiles);

                    string firstStringChunk = outputPattern.Substring(0, wildcardIndex);
                    string lastStringChunk = outputPattern.Substring(wildcardIndex + 1);
                    for (int i = 1; i <= nbUnities; ++i)
                    {
                        string fullPath = Path.Combine(unityFile.UnityFullOutputPath, $"{firstStringChunk}{i}{lastStringChunk}");
                        Util.RecordInAutoCleanupDatabase(fullPath);
                    }
                }
            }

            // Now combine all the streams.
            bffGeneratorProject.WriteTo(bffWholeFileGenerator);

            // remove all line that contain RemoveLineTag
            bffWholeFileGenerator.RemoveTaggedLines();

            // Write bff file
            FileInfo bffFileInfo = new FileInfo(projectBffFile);

            if (builder.Context.WriteGeneratedFile(project.GetType(), bffFileInfo, bffWholeFileGenerator))
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
            if (ShouldMakePathRelative(resolvedInclude, context.Project))
                resolvedInclude = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectory, resolvedInclude, true));
            return $@"{prefix}{Util.DoubleQuotes}{resolvedInclude}{Util.DoubleQuotes}";
        }

        private class DependenciesInfo
        {
            public OrderableStrings AdditionalDependencies;
            public Strings IgnoredLibraryNames;
        }

        private static void GenerateBffOptions(
            ProjectOptionsGenerator projectOptionsGen,
            BffGenerationContext context,
            Dictionary<Project.Configuration, DependenciesInfo> dependenciesInfoPerConf
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
            platformBff.SelectAdditionalCompilerOptionsBff(context);

            FillIncludeDirectoriesOptions(context);

            FillLinkerOptions(context);

            var dependenciesInfo = FillLibrariesOptions(context);
            dependenciesInfoPerConf.Add(context.Configuration, dependenciesInfo);

            FillNasmOptions(context);
        }

        internal enum CompilerVersionForClangClType
        {
            /// <summary>
            /// Version is for the -fmsc-version compilation flag
            /// </summary>
            MscVersion,
            /// <summary>
            /// Version is for the -fms-compatibility-version compilation flag
            /// </summary>
            MsCompatibilityVersion,
            /// <summary>
            /// Version is not set
            /// </summary>
            None
        }

        internal class CompilerVersionForClangCl
        {
            public CompilerVersionForClangClType versionType { get; }

            public string mscVersion { get; }
            public System.Version msCompatibilityVersion { get; }
            

            public CompilerVersionForClangCl(System.Version version)
            {
                versionType = CompilerVersionForClangClType.MsCompatibilityVersion;
                msCompatibilityVersion = version;
            }
            public CompilerVersionForClangCl(string version)
            {
                versionType = CompilerVersionForClangClType.MscVersion;
                mscVersion = version;
            }

            public CompilerVersionForClangCl()
            {
                versionType = CompilerVersionForClangClType.None;
            }

            public override string ToString()
            {
                switch (versionType)
                {
                    case CompilerVersionForClangClType.MsCompatibilityVersion:
                        return string.Format("MsCompatibilityVersion : {0}.{1}.{2}", msCompatibilityVersion.Major, msCompatibilityVersion.Minor, msCompatibilityVersion.Build);
                    case CompilerVersionForClangClType.MscVersion:
                        return string.Format("MscVersion : {0}", mscVersion);
                    case CompilerVersionForClangClType.None:
                    default:
                        return string.Format("No version");
                }
            }

            #region IEquatable
            public override bool Equals(object obj)
            {
                CompilerVersionForClangCl other = obj as CompilerVersionForClangCl;
                if (other != null)
                {
                    return Equals(other);
                }
                else
                {
                    return false;
                }
            }
            public override int GetHashCode()
            {
                switch (versionType)
                {
                    case CompilerVersionForClangClType.MsCompatibilityVersion:
                        return msCompatibilityVersion.GetHashCode();
                    case CompilerVersionForClangClType.MscVersion:
                        return Int32.TryParse(mscVersion, out int numValue) ? numValue : -1;
                    case CompilerVersionForClangClType.None:
                        return 0;
                    default:
                        return -2;
                }
            }

            public bool Equals(CompilerVersionForClangCl other)
            {
                if (other.versionType != versionType)
                {
                    return false;
                }

                switch (versionType)
                {
                    case CompilerVersionForClangClType.MsCompatibilityVersion:
                        return other.msCompatibilityVersion == msCompatibilityVersion;
                    case CompilerVersionForClangClType.MscVersion:
                        return other.mscVersion == mscVersion;
                    case CompilerVersionForClangClType.None:
                    default:
                        return true;
                }
            }
            #endregion
        }
        internal static CompilerVersionForClangCl DetectCompilerVersionForClangCl(
            Project.Configuration.FastBuildClangMscVersionDetectionType detectionType, string overridenMscVer, 
            Options.Vc.General.PlatformToolset overridenPlatformToolset, DevEnv devenv, Platform platform)
        {
            switch (detectionType)
            {
                case Project.Configuration.FastBuildClangMscVersionDetectionType.MajorVersion :
                    {
                        if (!string.IsNullOrEmpty(overridenMscVer))
                        {
                            return new CompilerVersionForClangCl(overridenMscVer);
                        }

                        string mscVer = DetectMscVerForClang(devenv, overridenPlatformToolset);
                        return new CompilerVersionForClangCl(mscVer);
                    }

                case Project.Configuration.FastBuildClangMscVersionDetectionType.FullVersion:
                    {
                        if (!string.IsNullOrEmpty(overridenMscVer))
                        {
                            throw new Error("Options.Clang.Compiler.MscVersion and FastBuildClangMscVersionDetection.FullVersion are both set but are mutually exclusive.");
                        }

                        System.Version mscFullVer = new System.Version();
                        try
                        {
                            mscFullVer = devenv.GetVisualStudioVCToolsCompilerVersion(platform);
                        }
                        catch
                        {
                            // mscFullVer couldn't be retrieved, fallback to MajorVersion behavior
                            string mscVer = DetectMscVerForClang(devenv, overridenPlatformToolset);
                            return new CompilerVersionForClangCl(mscVer);
                        }
                        return new CompilerVersionForClangCl(mscFullVer);
                    }

                case Project.Configuration.FastBuildClangMscVersionDetectionType.Disabled:
                    {
                        if (!string.IsNullOrEmpty(overridenMscVer))
                        {
                            // Detection is disabled but the Clang option version is set, set the version
                            return new CompilerVersionForClangCl(overridenMscVer);
                        }

                        return new CompilerVersionForClangCl();
                    }
                default:
                    {
                        return new CompilerVersionForClangCl();
                    }
            }
        }

        internal static string DetectMscVerForClang(DevEnv devenv, Options.Vc.General.PlatformToolset overridenPlatformToolset)
        {
            if (overridenPlatformToolset != Options.Vc.General.PlatformToolset.Default
                && !overridenPlatformToolset.IsDefaultToolsetForDevEnv(devenv))
            {
                switch (overridenPlatformToolset)
                {
                    case Options.Vc.General.PlatformToolset.v141:
                    case Options.Vc.General.PlatformToolset.v141_xp:
                        return "1910";
                    case Options.Vc.General.PlatformToolset.v142:
                        return "1920";
                    case Options.Vc.General.PlatformToolset.v143:
                        return "1930";
                    default:
                        throw new Error("LLVMVcPlatformToolset! Platform toolset override '{0}' not supported", overridenPlatformToolset);
                }
            }
            else
            {
                switch (devenv)
                {
                    case DevEnv.vs2017:
                        return "1910";
                    case DevEnv.vs2019:
                        return "1920";
                    case DevEnv.vs2022:
                        return "1930";
                    default:
                        throw new Error("Clang-cl used with unsupported DevEnv: " + devenv.ToString());
                }
            }
        }

        private static void FillIncludeDirectoriesOptions(BffGenerationContext context)
        {
            // TODO: really not ideal, refactor and move the properties we need from it someplace else
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);

            var includePaths = new OrderableStrings(platformVcxproj.GetIncludePaths(context));
            var resourceIncludePaths = new OrderableStrings(platformVcxproj.GetResourceIncludePaths(context));
            var assemblyIncludePaths = new OrderableStrings(platformVcxproj.GetAssemblyIncludePaths(context));
            context.CommandLineOptions["AdditionalIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["SwiftAdditionalIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalResourceIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalUsingDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalAssemblyIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;

            var platformDescriptor = PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform);
            if (context.EnvironmentVariableResolver != null)
            {
                string defaultCmdLineIncludePrefix = platformDescriptor.IsUsingClang ? "-I " : "/I";

                // Fill include dirs
                var platformIncludePaths = platformVcxproj.GetPlatformIncludePathsWithPrefix(context);

                var dirs = new List<string>();
                dirs.AddRange(includePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p, defaultCmdLineIncludePrefix)));
                dirs.AddRange(platformIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p.Path, p.CmdLinePrefix)));
                if (dirs.Any())
                    context.CommandLineOptions["AdditionalIncludeDirectories"] = string.Join($"'{Environment.NewLine}            + ' ", dirs);

                var applePlatformBff = PlatformRegistry.Query<IApplePlatformBff>(context.Configuration.Platform);
                if (applePlatformBff != null && applePlatformBff.IsSwiftSupported())
                {
                    string swifttCmdLineIncludePrefix = "-Xcc " + defaultCmdLineIncludePrefix.Replace(" ", " -Xcc ");
                    var swiftDirs = new List<string>();
                    swiftDirs.AddRange(includePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p, swifttCmdLineIncludePrefix)));
                    swiftDirs.AddRange(platformIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p.Path, "-Xcc " + p.CmdLinePrefix.Replace(" ", " -Xcc "))));
                    if (swiftDirs.Any())
                        context.CommandLineOptions["SwiftAdditionalIncludeDirectories"] = string.Join($"'{Environment.NewLine}            + ' ", swiftDirs);
                }

                // Fill resource include dirs
                var resourceDirs = new List<string>();
                resourceDirs.AddRange(resourceIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p, defaultCmdLineIncludePrefix)));

                // with LLVM as toolchain, we are still using the default resource compiler, so we need the default include prefix
                // TODO: this is not great, ideally we would need the prefix to be per "compiler", and a platform can have many
                var platformIncludePathsDefaultPrefix = platformIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p.Path, defaultCmdLineIncludePrefix));
                resourceDirs.AddRange(platformIncludePathsDefaultPrefix);

                if (resourceDirs.Any())
                    context.CommandLineOptions["AdditionalResourceIncludeDirectories"] = string.Join($"'{Environment.NewLine}                                    + ' ", resourceDirs);

                // Fill Assembly include dirs
                var assemblyDirs = new List<string>();
                assemblyDirs.AddRange(assemblyIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p, defaultCmdLineIncludePrefix)));
                if (assemblyDirs.Any())
                    context.CommandLineOptions["AdditionalAssemblyIncludeDirectories"] = string.Join($"'{Environment.NewLine}                                    + ' ", assemblyDirs);

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

        private static Strings CollectNatvisFiles(BffGenerationContext context)
        {
            Project.Configuration projectConfig = context.Configuration;

            var natvisFiles = new Strings(projectConfig.Project.NatvisFiles);
            if (projectConfig.Output == Project.Configuration.OutputType.Dll || projectConfig.Output == Project.Configuration.OutputType.Exe || projectConfig.Output == Project.Configuration.OutputType.AppleApp)
            {
                var visitedProjects = new HashSet<Project>();
                foreach (Project.Configuration resolvedDepConfig in projectConfig.ResolvedDependencies)
                {
                    if (resolvedDepConfig.Output != Project.Configuration.OutputType.Dll && resolvedDepConfig.Output != Project.Configuration.OutputType.Exe && resolvedDepConfig.Output != Project.Configuration.OutputType.AppleApp)
                    {
                        if (!visitedProjects.Contains(resolvedDepConfig.Project))
                        {
                            visitedProjects.Add(resolvedDepConfig.Project);
                            foreach (string natvisFile in resolvedDepConfig.Project.NatvisFiles)
                            {
                                natvisFiles.Add(natvisFile);
                            }
                        }
                    }
                }
            }
            return natvisFiles;
        }

        private static void FillEmbeddedNatvisOptions(BffGenerationContext context)
        {
            Strings natvisFiles = CollectNatvisFiles(context);

            if (natvisFiles.Count > 0)
            {
                var cmdNatvisFiles = natvisFiles.SortedValues.Select(n => Bff.CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, n, "/NATVIS:"));
                string linkerNatvis = string.Join($"'{Environment.NewLine}                            + ' ", cmdNatvisFiles);

                context.CommandLineOptions["LinkerNatvisFiles"] = linkerNatvis;
            }
            else
            {
                context.CommandLineOptions["LinkerNatvisFiles"] = FileGeneratorUtilities.RemoveLineTag;
            }
        }

        private static DependenciesInfo FillLibrariesOptions(BffGenerationContext context)
        {
            var dependenciesInfo = new DependenciesInfo();

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
            ignoreSpecificLibraryNames.InsertSuffix(platformVcxproj.StaticLibraryFileFullExtension, true, new[] { platformVcxproj.SharedLibraryFileFullExtension });
            dependenciesInfo.IgnoredLibraryNames = ignoreSpecificLibraryNames;

            context.CommandLineOptions["AdditionalDependencies"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalLibraryDirectories"] = FileGeneratorUtilities.RemoveLineTag;

            if (!(context.Configuration.Output == Project.Configuration.OutputType.None || context.Configuration.Output == Project.Configuration.OutputType.Lib && !context.Configuration.ExportAdditionalLibrariesEvenForStaticLib))
            {
                //AdditionalLibraryDirectories
                //                                            AdditionalLibraryDirectories="dir1;dir2"    /LIBPATH:"dir1" /LIBPATH:"dir2"
                SelectAdditionalLibraryDirectoriesOption(context);

                //AdditionalDependencies
                //                                            AdditionalDependencies="lib1;lib2"      "lib1;lib2" 
                dependenciesInfo.AdditionalDependencies = SelectAdditionalDependenciesOption(context, libFiles, ignoreSpecificLibraryNames);
            }
            else
            {
                dependenciesInfo.AdditionalDependencies = new OrderableStrings();
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

            return dependenciesInfo;
        }

        private static void FillNasmOptions(BffGenerationContext context)
        {
            // Compiler path for nasm
            context.CommandLineOptions["PathExe"] = context.Project.NasmExePath;

            // Pre included files for NASM syntax
            var preIncludedFiles = new List<string>();
            preIncludedFiles.AddRange(context.Project.NasmPreIncludedFiles.Select(p => "-P\"" + p + "\""));
            string preIncludedFilesJoined = string.Join(' ', preIncludedFiles);
            context.CommandLineOptions["PreIncludedFiles"] = preIncludedFilesJoined;

            // Fill Assembly include dirs in nasm syntax
            var nasmAssemblyDirs = new List<string>();
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);
            var assemblyIncludePaths = new OrderableStrings(platformVcxproj.GetAssemblyIncludePaths(context));
            nasmAssemblyDirs.AddRange(assemblyIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, context.EnvironmentVariableResolver, p, "-I ")));

            if (nasmAssemblyDirs.Any())
            {
                context.CommandLineOptions["AdditionalAssemblyNasmIncludeDirectories"] = string.Join($"'{Environment.NewLine}                                    + ' ", nasmAssemblyDirs);
            }
            else
            {
                context.CommandLineOptions["AdditionalAssemblyNasmIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            }

            // Defines in NASM syntax
            var defines = new Strings();
            defines.AddRange(context.Options.ExplicitDefines);
            defines.AddRange(context.Configuration.Defines);

            if (defines.Count > 0)
            {
                var fastBuildNasmDefines = new List<string>();

                foreach (string define in defines.SortedValues)
                {
                    if (!string.IsNullOrWhiteSpace(define))
                        fastBuildNasmDefines.Add(string.Format(@"{0}{1}{2}{1}", "-D", Util.DoubleQuotes, define.Replace(Util.DoubleQuotes, Util.EscapedDoubleQuotes)));
                }
                context.CommandLineOptions["NasmPreprocessorDefinitions"] = string.Join($"'{Environment.NewLine}            + ' ", fastBuildNasmDefines);
            }
            else
            {
                context.CommandLineOptions["NasmPreprocessorDefinitions"] = FileGeneratorUtilities.RemoveLineTag;
            }
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
            var configurationTasks = PlatformRegistry.Get<Project.Configuration.IConfigurationTasks>(context.Configuration.Platform);

            // TODO: really not ideal, refactor and move the properties we need from it someplace else
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);

            platformVcxproj.SetupPlatformLibraryOptions(out var platformLibraryExtension, out var platformOutputLibraryExtension, out var platformPrefix, out var libPrefix);

            var additionalDependencies = new OrderableStrings();

            for (int i = 0; i < libraryFiles.Count; ++i)
            {
                string libraryFile = libraryFiles[i];

                // convert all root paths to be relative to the project folder
                if (Path.IsPathRooted(libraryFile))
                {
                    // if the path is below the global root, we compute the relative path, otherwise we keep the full path
                    if (ShouldMakePathRelative(libraryFile, context.Project))
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
                    string filenameOnly = Path.GetFileNameWithoutExtension(libraryFile);
                    string finalFilename = null;
                    if (string.IsNullOrEmpty(extension))
                    {
                        finalFilename = libPrefix + filenameOnly + platformOutputLibraryExtension;
                    }
                    else if (extension != platformVcxproj.StaticLibraryFileFullExtension && extension != platformVcxproj.SharedLibraryFileFullExtension)
                    {
                        // Handle case such SomeLib.Platform
                        finalFilename = libPrefix + libraryFile + platformOutputLibraryExtension;
                    }
                    else
                    {
                        finalFilename = libraryFile;
                    }

                    string libDependencyFile = platformPrefix + finalFilename;

                    // LCTODO: this might be broken, clarify the rules for which this is supposed to work
                    if (!ignoreSpecificLibraryNames.Contains(finalFilename))
                        additionalDependencies.Add(libDependencyFile);
                    else
                        ignoreSpecificLibraryNames.Remove(finalFilename);
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

        private void ConfigureUnities(IGenerationContext context, Dictionary<Project.Configuration, Dictionary<SubConfig, List<Vcxproj.ProjectFile>>> confSourceFiles)
        {
            var conf = context.Configuration;
            // Only add unity build to non blobbed projects -> which they will be blobbed by FBuild
            if (!conf.FastBuildBlobbed)
                return;

            if (!confSourceFiles.ContainsKey(conf)) // no source files, so no unity section
                return;

            var confSubConfigs = confSourceFiles[conf];
            var unitySubConfig = s_DefaultSubConfig;
            var sourceFiles = confSubConfigs[unitySubConfig];
            var project = context.Project;

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

            string fastBuildUnityInputIsolateListFile = FileGeneratorUtilities.RemoveLineTag;

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
                        if (ShouldMakePathRelative(p, context.Project))
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
                // completely drop the subconfig in case it was only a unity subConfig, without any files
                if (sourceFiles.Count == 0)
                    confSubConfigs.Remove(unitySubConfig);

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

            if (!string.IsNullOrEmpty(conf.FastBuildUnityInputIsolateListFile))
                fastBuildUnityInputIsolateListFile = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectoryCapitalized, conf.FastBuildUnityInputIsolateListFile, true));

            Unity unityFile = new Unity
            {
                // Note that the UnityName and UnityOutputPattern are intentionally left empty: they will be set in the Resolve
                UnityOutputPath = CurrentBffPathKeyCombine(Util.PathGetRelative(context.ProjectDirectoryCapitalized, conf.FastBuildUnityPath, true)),
                UnityFullOutputPath = Path.Combine(context.ProjectDirectoryCapitalized, conf.FastBuildUnityPath),
                UnityInputIsolateWritableFiles = conf.FastBuildUnityInputIsolateWritableFiles.ToString().ToLower(),
                UnityInputIsolateWritableFilesLimit = conf.FastBuildUnityInputIsolateWritableFiles ? conf.FastBuildUnityInputIsolateWritableFilesLimit.ToString() : FileGeneratorUtilities.RemoveLineTag,
                UnityInputIsolateListFile = fastBuildUnityInputIsolateListFile,
                UnityPCH = conf.PrecompHeader ?? FileGeneratorUtilities.RemoveLineTag,
                UnityInputExcludePath = fastBuildUnityInputExcludePath,
                UnityNumFiles = fastBuildUnityCount,
                UnityInputPath = fastBuildUnityPaths,
                UnityInputFiles = fastBuildUnityInputFiles,
                UnityInputExcludedFiles = fastBuildUnityInputExcludedfiles,
                UnityInputPattern = fastBuildUnityInputPattern,
                UseRelativePaths = conf.FastBuildUnityUseRelativePaths ? "true" : FileGeneratorUtilities.RemoveLineTag,
                UnitySectionBucket = conf.FastBuildUnitySectionBucket,
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
        private static SubConfig s_DefaultSubConfig = new SubConfig();

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

        private static void Write(string value, TextWriter writer, Resolver resolver)
        {
            string resolvedValue = resolver.Resolve(value);
            StringReader reader = new StringReader(resolvedValue);
            string str = reader.ReadToEnd();
            writer.Write(str);
            writer.Flush();
        }

        private static Dictionary<Project.Configuration, Dictionary<SubConfig, List<Vcxproj.ProjectFile>>>
        GetGeneratedFiles(
            IGenerationContext context,
            List<Project.Configuration> configurations,
            out List<Vcxproj.ProjectFile> filesInNonDefaultSections
        )
        {
            var confSubConfigs = new Dictionary<Project.Configuration, Dictionary<SubConfig, List<Vcxproj.ProjectFile>>>();
            filesInNonDefaultSections = new List<Vcxproj.ProjectFile>();

            // Add source files
            var allFiles = new List<Vcxproj.ProjectFile>();
            Strings projectFiles = context.Project.GetSourceFilesForConfigurations(configurations);
            foreach (string file in projectFiles)
            {
                var projectFile = new Vcxproj.ProjectFile(context, file);
                allFiles.Add(projectFile);
            }
            allFiles.Sort((l, r) => string.Compare(l.FileNameProjectRelative, r.FileNameProjectRelative, StringComparison.OrdinalIgnoreCase));

            var sourceFiles = new List<Vcxproj.ProjectFile>();
            foreach (var projectFile in allFiles)
            {
                if (context.Project.SourceFilesCompileExtensions.Contains(projectFile.FileExtension) ||
                    (string.Compare(projectFile.FileExtension, ".rc", StringComparison.OrdinalIgnoreCase) == 0) ||
                    (string.Compare(projectFile.FileExtension, ".resx", StringComparison.OrdinalIgnoreCase) == 0))
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
                        bool isCompileAsObjCFile = conf.ResolvedSourceFilesWithCompileAsObjCOption.Contains(file.FileName);
                        bool isCompileAsObjCPPFile = conf.ResolvedSourceFilesWithCompileAsObjCPPOption.Contains(file.FileName);
                        bool isCompileAsCLRFile = conf.ResolvedSourceFilesWithCompileAsCLROption.Contains(file.FileName);
                        bool isCompileAsNonCLRFile = conf.ResolvedSourceFilesWithCompileAsNonCLROption.Contains(file.FileName);
                        bool isConsumeWinRTExtensions = (conf.ConsumeWinRTExtensions.Contains(file.FileName) ||
                                                        conf.ResolvedSourceFilesWithCompileAsWinRTOption.Contains(file.FileName)) &&
                                                        !(conf.ExcludeWinRTExtensions.Contains(file.FileName) ||
                                                        conf.ResolvedSourceFilesWithExcludeAsWinRTOption.Contains(file.FileName));
                        // TODOANT: Also trigger on .nasm files
                        bool isASMFile = string.Compare(file.FileExtension, ".asm", StringComparison.OrdinalIgnoreCase) == 0;
                        bool isSwiftFile = string.Compare(file.FileExtension, ".swift", StringComparison.OrdinalIgnoreCase) == 0 &&
                                           (PlatformRegistry.Query<IApplePlatformBff>(conf.Platform)?.IsSwiftSupported() ?? false);
                        bool isNASMFile = string.Compare(file.FileExtension, ".nasm", StringComparison.OrdinalIgnoreCase) == 0;

                        Options.Vc.Compiler.Exceptions exceptionSetting = conf.GetExceptionSettingForFile(file.FileName);

                        if (isCompileAsCLRFile || isConsumeWinRTExtensions)
                            isDontUsePrecomp = true;
                        if (!isCompileAsCPPFile && string.Compare(file.FileExtension, ".c", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            isDontUsePrecomp = true;
                            isCompileAsCFile = true;
                        }
                        else if (isCompileAsObjCFile || isCompileAsObjCPPFile || isSwiftFile)
                        {
                            isDontUsePrecomp = true;
                        }
                        Languages languageKind = Languages.None;
                        if (isCompileAsCFile)
                            languageKind |= Languages.C;
                        if (isCompileAsCPPFile)
                            languageKind |= Languages.CPP;
                        if (isCompileAsObjCFile)
                            languageKind |= Languages.ObjC;
                        if (isCompileAsObjCPPFile)
                            languageKind |= Languages.ObjCPP;
                        if (isSwiftFile)
                            languageKind |= Languages.Swift;
                        if (isASMFile)
                            languageKind |= Languages.Asm;
                        if (isNASMFile)
                            languageKind |= Languages.Nasm;

                        LanguageFeatures languageFeatures = LanguageFeatures.None;
                        if (isCompileAsCLRFile)
                            languageFeatures |= LanguageFeatures.CLR;
                        if (isCompileAsNonCLRFile)
                            languageFeatures |= LanguageFeatures.NonCLR;
                        if (isConsumeWinRTExtensions)
                            languageFeatures |= LanguageFeatures.ConsumeWinRTExtensions;

                        var subConfig = new SubConfig()
                        {
                            IsUsePrecomp = !isDontUsePrecomp,
                            Languages = languageKind,
                            LanguageFeatures = languageFeatures,
                            Exceptions = exceptionSetting
                        };

                        Dictionary<SubConfig, List<Vcxproj.ProjectFile>> subConfigs = null;
                        if (!confSubConfigs.TryGetValue(conf, out subConfigs))
                        {
                            subConfigs = new Dictionary<SubConfig, List<Vcxproj.ProjectFile>>();
                            confSubConfigs.Add(conf, subConfigs);
                        }
                        List<Vcxproj.ProjectFile> subConfigFiles = null;
                        if (!subConfigs.TryGetValue(subConfig, out subConfigFiles))
                        {
                            subConfigFiles = new List<Vcxproj.ProjectFile>();
                            subConfigs.Add(subConfig, subConfigFiles);
                        }
                        subConfigFiles.Add(file);

                        if (!subConfig.Equals(s_DefaultSubConfig))
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
                    var subConfig = s_DefaultSubConfig;

                    Dictionary<SubConfig, List<Vcxproj.ProjectFile>> subConfigs = null;
                    if (!confSubConfigs.TryGetValue(conf, out subConfigs))
                    {
                        subConfigs = new Dictionary<SubConfig, List<Vcxproj.ProjectFile>>();
                        confSubConfigs.Add(conf, subConfigs);
                    }
                    List<Vcxproj.ProjectFile> subConfigFiles = null;
                    if (!subConfigs.TryGetValue(subConfig, out subConfigFiles))
                    {
                        subConfigFiles = new List<Vcxproj.ProjectFile>();
                        subConfigs.Add(subConfig, subConfigFiles);
                    }
                }
            }

            return confSubConfigs;
        }

        private static bool ShouldMakePathRelative(string path, Project project)
        {
            string rootPath = FastBuildSettings.WorkspaceRoot ?? project.RootPath;
            return path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase);
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
