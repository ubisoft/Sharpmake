// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators.Rider
{
    /// <summary>
    /// Generator for Rider project model json files.
    /// </summary>
    public partial class RiderJson : ISolutionGenerator
    {
        public static bool Minimize = false;
        public static bool IgnoreDefaults = false;
        
        private const string RiderJsonFileExtension = ".rdjson";

        /// <summary>
        /// Helper class to keep all the project information for "Modules" section of json file.
        /// </summary>
        private class RiderProjectInfo
        {
            public string Name { get; }

            public Strings PublicDependencyModules { get; }
            public Strings PrivateDependencyModules { get; }
            public Strings PublicIncludePaths { get; }
            public Strings PrivateIncludePaths { get; }
            public Strings PublicDefinitions { get; }
            public Strings PrivateDefinitions { get; }


            public RiderProjectInfo(Project project)
            {
                Name = project.Name;

                PublicDependencyModules = new Strings();
                PrivateDependencyModules = new Strings();
                PublicIncludePaths = new Strings();
                PrivateIncludePaths = new Strings();
                PublicDefinitions = new Strings();
                PrivateDefinitions = new Strings();
            }

            /// <summary>
            /// Gathers all the needed information from <see cref="Project.Configuration"/>.
            /// </summary>
            public void ReadConfiguration(Project.Configuration config)
            {
                PublicDependencyModules.AddRange(config.ResolvedPublicDependencies.Select(it => it.Project.GetQualifiedName()));
                PrivateDependencyModules.AddRange(config.ResolvedPrivateDependencies.Select(it => it.Project.GetQualifiedName()));
                PublicIncludePaths.AddRange(config.IncludePaths);
                PrivateIncludePaths.AddRange(config.IncludePrivatePaths);
                PublicDefinitions.AddRange(config.ExportDefines);
                PrivateDefinitions.AddRange(config.Defines);
            }

            /// <summary>
            /// Returns OrderedDictionary for json serialization
            /// </summary>
            public OrderedDictionary ToDictionary()
            {
                var resDict = new OrderedDictionary();

                resDict.AddIfCondition("PublicDependencyModules", PublicDependencyModules, PublicDependencyModules.Count != 0);
                resDict.AddIfCondition("PrivateDependencyModules", PrivateDependencyModules, PrivateDependencyModules.Count != 0);
                resDict.AddIfCondition("PublicIncludePaths", PublicIncludePaths, PublicIncludePaths.Count != 0);
                resDict.AddIfCondition("PrivateIncludePaths", PrivateIncludePaths, PrivateIncludePaths.Count != 0);
                resDict.AddIfCondition("PublicDefinitions", PublicDefinitions, PublicDefinitions.Count != 0);
                resDict.AddIfCondition("PrivateDefinitions", PrivateDefinitions, PrivateDefinitions.Count != 0);

                return resDict;
            }
        }

        /// <summary>
        /// Helper class for storing all the project-related information.
        /// </summary>
        private class RiderGenerationContext : IGenerationContext
        {
            public Builder Builder { get; }
            public Project Project { get; }
            public Project.Configuration Configuration { get; }
            public string ProjectDirectory { get; set; }
            public string ProjectFileName { get; }
            public string ProjectPath { get; }
            
            public string SolutionName { get; }
            
            public Resolver Resolver { get; }
            
            public DevEnv DevelopmentEnvironment { get; }
            public Options.ExplicitOptions Options { get; }
            public IDictionary<string, string> CommandLineOptions { get; }
            public string ProjectDirectoryCapitalized { get; }
            public string ProjectSourceCapitalized { get; }
            public bool PlainOutput => true;
            
            public FastBuildMakeCommandGenerator FastBuildMakeCommandGenerator { get; }
            public string FastBuildArguments { get; }

            public RiderGenerationContext(Builder builder, Project project, Project.Configuration configuration,
                string projectPath, string solutionName)
            {
                Builder = builder;
                Resolver = new Resolver();

                FileInfo fileInfo = new FileInfo(projectPath);
                ProjectPath = fileInfo.FullName;
                ProjectDirectory = Path.GetDirectoryName(fileInfo.FullName);
                ProjectFileName = fileInfo.Name;
                Project = project;

                Configuration = configuration;
                ProjectDirectoryCapitalized = Util.GetCapitalizedPath(ProjectDirectory);
                ProjectSourceCapitalized = Util.GetCapitalizedPath(Project.SourceRootPath);
                DevelopmentEnvironment = configuration.Target.GetFragment<DevEnv>();

                SolutionName = solutionName;
                
                Options = new Options.ExplicitOptions();
                CommandLineOptions = new ProjectOptionsGenerator.VcxprojCmdLineOptions();

                if (configuration.IsFastBuild)
                {
                    FastBuildMakeCommandGenerator = new Bff.FastBuildDefaultNMakeCommandGenerator();
                    
                    var fastBuildCommandLineOptions = FastBuildMakeCommandGenerator.GetArguments(Configuration);
                    fastBuildCommandLineOptions.Add(" -config $(SolutionName)" + FastBuildSettings.FastBuildConfigFileExtension);
                    
                    FastBuildArguments = string.Join(" ", fastBuildCommandLineOptions);
                }
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
        
        /// <summary>
        /// Generates "<solutionName>.rdjson" file for <paramref name="solution"/>.
        /// Also gathers information about projects for later usage in "Modules" section.
        /// </summary>
        public void Generate(Builder builder, Solution solution, List<Solution.Configuration> configurations, string solutionFile,
            List<string> generatedFiles, List<string> skipFiles)
        {
            var configurationMapping = new Dictionary<Project, List<Project.Configuration>>();
            var fileInfo = new FileInfo(solutionFile);
            var solutionPath = fileInfo.Directory.FullName;
            
            var solutionFileName = fileInfo.Name;
            var file = new FileInfo(
                Util.GetCapitalizedPath(solutionPath + Path.DirectorySeparatorChar + solutionFileName + RiderJsonFileExtension));

            var projects = new OrderedDictionary();
            
            foreach (var solutionConfig in configurations)
            {
                foreach (var proj in solutionConfig.IncludedProjectInfos)
                {
                    if (proj.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Generate)
                    {
                        continue;
                    }
                    
                    var solutionFolder = string.IsNullOrEmpty(proj.SolutionFolder)
                        ? proj.Configuration.GetSolutionFolder(solution.Name)
                        : proj.SolutionFolder;
                    var projectEntry = solutionFolder + (solutionFolder.EndsWith("/") ? "" : "/") + proj.Project.Name;
                    if (!projects.Contains(projectEntry))
                    {
                        projects.Add(projectEntry, new Dictionary<string, List<object>>());
                    }
                
                    var projObject = projects[projectEntry] as Dictionary<string, List<object>>;
                    var projConfig = new Dictionary<string, string>();

                    var projectConfigurations = configurationMapping.GetValueOrAdd(proj.Project, new List<Project.Configuration>());
                    projectConfigurations.Add(proj.Configuration);
                    
                    projConfig.Add("ProjectConfig", proj.Configuration.Name);
                    projConfig.Add("SolutionConfig", solutionConfig.Name);
                    projConfig.Add("DoBuild", (proj.ToBuild != Solution.Configuration.IncludedProjectInfo.Build.No).ToString());

                    var platformConfigurations = projObject.GetValueOrAdd(proj.Configuration.Platform.ToString(), new List<object>());
                    platformConfigurations.Add(projConfig);
                }
            }

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            using (var serializer = new Util.JsonSerializer(writer) {IsOutputFormatted = true})
            {
                serializer.IsOutputFormatted = !Minimize;
                serializer.Serialize(projects);
                serializer.Flush();

                if (builder.Context.WriteGeneratedFile(null, file, stream))
                {
                    generatedFiles.Add(file.FullName);
                }
                else
                {
                    skipFiles.Add(file.FullName);
                }
            }

            builder.LogWriteLine("          {0,5}", file.Name);
            
            var projectInfos = new Dictionary<string, RiderProjectInfo>();
            foreach (var projectInfo in configurations
                         .SelectMany(solutionConfig => solutionConfig.IncludedProjectInfos))
            {
                if (projectInfo.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Generate)
                {
                    continue;
                }

                GenerateConfiguration(builder, projectInfo.Project, projectInfos, projectInfo.Configuration,
                    Path.Combine(solutionPath, $".{solutionFileName}"), generatedFiles, skipFiles);
            }
        }

        private static void GenerateConfiguration(Builder builder, Project project, Dictionary<string, RiderProjectInfo> projectInfos, Project.Configuration configuration, string projectFile,
            List<string> generatedFiles, List<string> skipFiles)
        {
            var context = new RiderGenerationContext(builder, project, configuration, projectFile,
                    Path.GetFileName(projectFile).Substring(1));

            var projectOptionsGen = new ProjectOptionsGenerator();
            projectOptionsGen.GenerateOptions(context);
            GenerateConfiguration(context, projectInfos, generatedFiles, skipFiles);
        }

        private static void GenerateConfiguration(RiderGenerationContext context, Dictionary<string, RiderProjectInfo> projectInfos, List<string> generatedFiles, List<string> skipFiles)
        {
            var info = new OrderedDictionary();
            var includePaths = new Strings();
            var defines = new Strings();
            var modules = new OrderedDictionary();
            var toolchain = new OrderedDictionary();
            var buildInfo = new OrderedDictionary();
            var sourceFilesInfo = new OrderedDictionary();

            toolchain.AddIfNotDefault("Compiler", context.GetCompiler(), RiderJsonUtil.Compiler.Default);
            toolchain.AddIfNotDefault("CppStandard", context.GetCppStandard(), RiderJsonUtil.CppLanguageStandard.Default);
            toolchain.AddIfNotDefault("Architecture", context.GetArchitecture(), "x64");
            toolchain.AddIfNotDefault("OutputType", context.GetOutputType(), RiderJsonUtil.OutputType.Default);
            toolchain.AddIfNotDefault("bUseRTTI", context.IsRttiEnabled(), false);
            toolchain.AddIfNotDefault("bUseExceptions", context.IsExceptionEnabled(), true);
            toolchain.AddIfNotDefault("bIsBuildingDll", context.Configuration.Output == Project.Configuration.OutputType.Dll, false);
            toolchain.Add("Configuration", context.Configuration.Name);
            toolchain.AddIfNotDefault("bOptimizeCode", context.IsOptimizationEnabled(), false);
            toolchain.AddIfNotDefault("bUseInlining", context.IsInliningEnabled(), false);
            toolchain.AddIfNotDefault("bUseUnity", context.IsBlob(), false);
            toolchain.AddIfNotDefault("bCreateDebugInfo", context.IsDebugInfo(), false);
            toolchain.AddIfNotDefault("bUseAVX", context.IsAvx(), false);
            toolchain.AddIfNotDefault("bStrictConformanceMode", context.IsConformanceMode(), false);
            toolchain.AddIfNotDefault("PrecompiledHeaderAction", context.GetPchAction(), RiderJsonUtil.PchAction.Default);

            using (context.Resolver.NewScopedParameter("SolutionDir", context.ProjectDirectory))
            using (context.Resolver.NewScopedParameter("ProjectDir", context.Configuration.ProjectPath))
            {
                var targetPath = Path.Combine(context.Configuration.TargetPath, context.Configuration.TargetFileFullNameWithExtension);
                buildInfo.Add("TargetPath", targetPath);

                var commands = GetBuildCommands(context);

                buildInfo.AddIfNotDefault("BuildCmd", commands.Build, "");
                buildInfo.AddIfNotDefault("ReBuildCmd", commands.Rebuild, "");
                buildInfo.AddIfNotDefault("CleanCmd", commands.Clean, "");
            }

            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);
            if (context.DevelopmentEnvironment.IsVisualStudio())
            {
                var winIncludePath = context.DevelopmentEnvironment.GetWindowsIncludePath();
                includePaths.AddRange(winIncludePath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            var platformIncludes = platformVcxproj.GetPlatformIncludePaths(context);
            var includesString = context.Options["IncludePath"];
            if (includesString != FileGeneratorUtilities.RemoveLineTag)
            {
                var includesEntries = includesString.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                includePaths.AddRange(includesEntries);
            }
            
            includePaths.AddRange(platformIncludes);
            
            defines.AddRange(platformVcxproj.GetImplicitlyDefinedSymbols(context));
            defines.AddRange(context.Options.ExplicitDefines);
            
            RiderProjectInfo GetOrCreateProjectInfo(Project.Configuration configuration)
            {
                var projectName = configuration.Project.Name;
                if (projectInfos.TryGetValue(projectName, out RiderProjectInfo projectInfo))
                {
                    return projectInfo;
                }

                var newProjectInfo = new RiderProjectInfo(configuration.Project);
                newProjectInfo.ReadConfiguration(configuration);
                projectInfos.Add(projectName, newProjectInfo);

                return projectInfos[projectName];
            }
            
            var curProjectInfo = GetOrCreateProjectInfo(context.Configuration);
            modules.Add(curProjectInfo.Name, curProjectInfo.ToDictionary());
            
            foreach (var dependency in context.Configuration.ResolvedDependencies)
            {
                var dependencyInfo = GetOrCreateProjectInfo(dependency);
                var dependencyName = dependency.Project.GetQualifiedName();
                modules.Add(dependencyName, dependencyInfo.ToDictionary());
            }

            var sourceRoots = new Strings {context.Project.SourceRootPath};
            sourceRoots.AddRange(context.Project.AdditionalSourceRootPaths);
            sourceFilesInfo.Add("SourceRoots", sourceRoots);
            sourceFilesInfo.AddIfCondition("SourceFilesFilters", context.Project.SourceFilesFilters ?? new Strings(), 
                context.Project.SourceFilesFilters != null);
            sourceFilesInfo.AddIfCondition("SourceFiltersRegex",
                context.Project.SourceFilesIncludeRegex.Concat(context.Project.SourceFilesFiltersRegex), 
                context.Project.SourceFilesIncludeRegex.Count > 0 || context.Project.SourceFilesFiltersRegex.Count > 0);
            sourceFilesInfo.AddIfCondition("ExcludeRegex", context.Project.SourceFilesExcludeRegex,
                context.Project.SourceFilesExclude.Count > 0);
            sourceFilesInfo.Add("SourceExtensions", context.Project.SourceFilesExtensions);
            sourceFilesInfo.Add("SourceFiles", context.Project.ResolvedSourceFiles);
            sourceFilesInfo.Add("ExcludedFiles", context.Project.SourceFilesExclude);

            info.Add("Name", context.Configuration.ProjectName);
            info.Add("Configuration", context.Configuration.Name);
            info.Add("Platform", context.Configuration.Platform.ToString());
            info.Add("ToolchainInfo", toolchain);
            info.Add("BuildInfo", buildInfo);

            info.Add("EnvironmentIncludePaths", includePaths);
            info.Add("EnvironmentDefinitions", defines);
            info.Add("Modules", modules);
            info.Add("SourceInfo", sourceFilesInfo);
            
            var file = new FileInfo(Path.Combine(context.ProjectPath, $"{context.Project.Name}_{context.Configuration.Platform}_{context.Configuration.Name}.json"));

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            using (var serializer = new Util.JsonSerializer(writer) { IsOutputFormatted = true })
            {
                serializer.IsOutputFormatted = !Minimize;
                serializer.Serialize(info);
                serializer.Flush();

                if (context.Builder.Context.WriteGeneratedFile(null, file, stream))
                {
                    generatedFiles.Add(Path.Combine(file.DirectoryName, file.Name));
                }
                else
                {
                    skipFiles.Add(Path.Combine(file.DirectoryName, file.Name));
                }
            }
        }

        private struct BuildCommands
        {
            public string Build;
            public string Rebuild;
            public string Clean;
        }

        private static BuildCommands GetBuildCommands(RiderGenerationContext context)
        {
            if (context.Configuration.IsFastBuild)
            {
                var beforeBuildCommand = context.Configuration.FastBuildCustomActionsBeforeBuildCommand;
                if (beforeBuildCommand == FileGeneratorUtilities.RemoveLineTag)
                {
                    beforeBuildCommand = "";
                }
                
                using (context.Resolver.NewScopedParameter("BeforeBuildCommand", beforeBuildCommand))
                {
                    return new BuildCommands
                    {
                        Build = GetFastBuildCommand(context, FastBuildMakeCommandGenerator.BuildType.Build),
                        Rebuild = GetFastBuildCommand(context, FastBuildMakeCommandGenerator.BuildType.Rebuild),
                        Clean = GetFastBuildClean(context)
                    };
                }
            }

            if (context.Configuration.CustomBuildSettings != null)
            {
                var buildSettings = context.Configuration.CustomBuildSettings;
                return new BuildCommands
                {
                    Build = buildSettings.BuildCommand,
                    Rebuild = buildSettings.RebuildCommand,
                    Clean = buildSettings.CleanCommand
                };
            }

            var msBuildProjectFile = Options.PathOption.Get<Options.Rider.MsBuildOverrideProjectFile>(context.Configuration, fallback: null);
            if (msBuildProjectFile == null)
            {
                throw new Error(
                    $"`Options.Rider.MsBuildOverrideProjectFile` should be overriden in \n {context.Configuration} in order to use MSBuild with Rider");
            }
            
            var msBuildConfiguration = Options.StringOption.Get<Options.Rider.MsBuildOverrideConfigurationName>(context.Configuration) ?? context.Configuration.Name;
            var msBuildPlatform = Options.StringOption.Get<Options.Rider.MsBuildOverridePlatformName>(context.Configuration) ?? Util.GetPlatformString(context.Configuration.Platform, context.Project, context.Configuration.Target);
            
            using (context.Resolver.NewScopedParameter("ProjectFile", msBuildProjectFile))
            using (context.Resolver.NewScopedParameter("ConfigurationName", msBuildConfiguration))
            using (context.Resolver.NewScopedParameter("PlatformName", msBuildPlatform))
            {
                return new BuildCommands
                {
                    Build = GetMsBuildCommand(context, "Build"),
                    Rebuild = GetMsBuildCommand(context, "Rebuild"),
                    Clean = GetMsBuildCommand(context, "Clean")
                };
            }
        }
        
        private static string GetFastBuildCommand(RiderGenerationContext context, FastBuildMakeCommandGenerator.BuildType commandType)
        {
            var unresolvedCommand = Template.FastBuildBuildCommand;
            using (context.Resolver.NewScopedParameter("BuildCommand",
                context.FastBuildMakeCommandGenerator.GetCommand(
                    commandType,
                    context.Configuration, context.FastBuildArguments)))
            {
                return context.Resolver.Resolve(unresolvedCommand)
                    .Replace("$(ProjectDir)", context.Configuration.ProjectPath + "\\")
                    .Replace("$(SolutionName)", context.SolutionName);
            }
        }

        private static string GetFastBuildClean(RiderGenerationContext context)
        {
            var unresolvedOutput = Template.FastBuildCleanCommand;
            if (context.Options["IntermediateDirectory"] == FileGeneratorUtilities.RemoveLineTag
                || context.Options["OutputDirectory"] == FileGeneratorUtilities.RemoveLineTag)
            {
                return "";
            }
        
            using (context.Resolver.NewScopedParameter("IntermediateDirectory", context.Options["IntermediateDirectory"])) 
            using (context.Resolver.NewScopedParameter("OutputDirectory", context.Options["OutputDirectory"]))
            using (context.Resolver.NewScopedParameter("TargetFileFullName", context.Configuration.TargetFileFullName))
            {
                return context.Resolver.Resolve(unresolvedOutput);
            }
        }

        private static string GetMsBuildCommand(RiderGenerationContext context, string buildCommand)
        {
            var unresolvedCommand = Template.MsBuildBuildCommand;

            using (context.Resolver.NewScopedParameter("Command", buildCommand))
            {
                return context.Resolver.Resolve(unresolvedCommand);
            }
        }
    }
}
