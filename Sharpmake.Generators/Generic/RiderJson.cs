using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators.Generic
{
    /// <summary>
    /// Generator for Rider project model json files.
    /// </summary>
    public partial class RiderJson : IProjectGenerator, ISolutionGenerator
    {
        public static bool Minimize = false;
        public static bool IgnoreDefaults = false;
        
        /// <summary>
        /// Callback which should be added to <see cref="Builder.EventPostGeneration"/> in order to generate Rider project model.
        /// </summary>
        public static void PostGenerationCallback(List<Project> projects, List<Solution> solutions, ConcurrentDictionary<Type, GenerationOutput> generationReport)
        {
            var builder = Builder.Instance;
            var generator = new RiderJson();
            
            builder.LogWriteLine("      RdJson files generated:");

            foreach (var project in projects)
            {
                foreach (var config in project.Configurations)
                {
                    if (!generator._projectsInfo.ContainsKey(config.Target))
                    {
                        generator._projectsInfo.Add(config.Target, new Dictionary<string, RiderProjectInfo>());
                    }

                    if (!generator._projectsInfo[config.Target].ContainsKey(project.Name))
                    {
                        generator._projectsInfo[config.Target].Add(project.Name, new RiderProjectInfo(project));
                    }
                    
                    var riderProjInfo = generator._projectsInfo[config.Target][project.Name];
                    riderProjInfo.ReadConfiguration(config);
                }
            }
            
            foreach (var solution in solutions)
            {
                foreach (var solutionFileEntry in solution.SolutionFilesMapping)
                {
                    var solutionFolder = Path.GetDirectoryName(solutionFileEntry.Key);

                    var generationOutput = generationReport[solution.GetType()];
                    var fileWithExtension = Path.Combine(solutionFileEntry.Key + ".rdjson");
                    
                    generator.Generate(builder, solution, solutionFileEntry.Value,
                        fileWithExtension, generationOutput.Generated, generationOutput.Skipped);
                    
                    builder.LogWriteLine("          {0,5}", fileWithExtension);

                    var solutionFileName = Path.GetFileName(solutionFileEntry.Key);
                    
                    foreach (var projectInfo in solutionFileEntry.Value.SelectMany(solutionConfig =>
                        solutionConfig.IncludedProjectInfos))
                    {
                        if (projectInfo.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Generate)
                        {
                            continue;
                        }
                        
                        var projectOutput = generationReport[projectInfo.Project.GetType()];
                        generator.Generate(builder, projectInfo.Project,
                            new List<Project.Configuration> {projectInfo.Configuration},
                            Path.Combine(solutionFolder, $".{solutionFileName}"), projectOutput.Generated, projectOutput.Skipped);
                    }
                }
            }
        }

        /// <summary>
        /// Helper class to keep all the project information for "Modules" section of json file.
        /// </summary>
        private class RiderProjectInfo
        {
            public string Name { get; }
            public string SourcePath { get; }
            public Strings SourceExtensions { get; }

            public Strings PublicDependencyModules { get; }
            public Strings PrivateDependencyModules { get; }
            public Strings PublicIncludePaths { get; }
            public Strings PrivateIncludePaths { get; }
            public Strings PublicDefinitions { get; }
            public Strings PrivateDefinitions { get; }
            public Strings Configurations { get; }

            public RiderProjectInfo(Project project)
            {
                Name = project.Name;
                SourceExtensions = project.SourceFilesExtensions;
                SourcePath = project.SourceRootPath;
                
                PublicDependencyModules = new Strings();
                PrivateDependencyModules = new Strings();
                PublicIncludePaths = new Strings();
                PrivateIncludePaths = new Strings();
                PublicDefinitions = new Strings();
                PrivateDefinitions = new Strings();
                Configurations = new Strings();
            }

            /// <summary>
            /// Gathers all the needed information from <see cref="Project.Configuration"/>.
            /// </summary>
            public void ReadConfiguration(Project.Configuration config)
            {
                PublicDependencyModules.AddRange(config.ResolvedPublicDependencies.Select(it => it.Project.Name));
                PrivateDependencyModules.AddRange(config.ResolvedPrivateDependencies.Select(it => it.Project.Name));
                PublicIncludePaths.AddRange(config.IncludePaths);
                PrivateIncludePaths.AddRange(config.IncludePrivatePaths);
                PublicDefinitions.AddRange(config.ExportDefines);
                PrivateDefinitions.AddRange(config.Defines);
                Configurations.Add(config.Name);
            }

            /// <summary>
            /// Returns OrderedDictionary for json serialization
            /// </summary>
            public OrderedDictionary ToDictionary()
            {
                var resDict = new OrderedDictionary { { "SourcePath", SourcePath } };

                if (!IgnoreDefaults || !SourceExtensions.All(new Project().SourceFilesExtensions.Contains))
                {
                    resDict.Add("SourceExtensions", SourceExtensions);
                }

                if (!IgnoreDefaults || PublicDependencyModules.Count != 0)
                {
                    resDict.Add("PublicDependencyModules", PublicDependencyModules);
                }
                
                if (!IgnoreDefaults || PrivateDependencyModules.Count != 0)
                {
                    resDict.Add("PrivateDependencyModules", PrivateDependencyModules);
                }

                if (!IgnoreDefaults || PublicIncludePaths.Count != 0)
                {
                    resDict.Add("PublicIncludePaths", PublicIncludePaths);
                }

                if (!IgnoreDefaults || PrivateIncludePaths.Count != 0)
                {
                    resDict.Add("PrivateIncludePaths", PrivateIncludePaths);
                }

                if (!IgnoreDefaults || PublicDefinitions.Count != 0)
                {
                    resDict.Add("PublicDefinitions", PublicDefinitions);
                }

                if (!IgnoreDefaults || PrivateDefinitions.Count != 0)
                {
                    resDict.Add("PrivateDefinitions", PrivateDefinitions);
                }
                
                return resDict;
            }
        }

        /// <summary>
        /// Maps projects information for later usage in "Modules" section.
        /// </summary>
        private readonly Dictionary<ITarget, Dictionary<string, RiderProjectInfo>> _projectsInfo
            = new Dictionary<ITarget, Dictionary<string, RiderProjectInfo>>();

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
                    FastBuildArguments = string.Join(" ", GetFastBuildOptions());
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
            
            private List<string> GetFastBuildOptions()
            {
                var fastBuildCommandLineOptions = new List<string>();

                if (FastBuildSettings.FastBuildUseIDE)
                    fastBuildCommandLineOptions.Add("-ide");

                if (FastBuildSettings.FastBuildReport)
                    fastBuildCommandLineOptions.Add("-report");

                if (FastBuildSettings.FastBuildNoSummaryOnError)
                    fastBuildCommandLineOptions.Add("-nosummaryonerror");

                if (FastBuildSettings.FastBuildSummary)
                    fastBuildCommandLineOptions.Add("-summary");

                if (FastBuildSettings.FastBuildVerbose)
                    fastBuildCommandLineOptions.Add("-verbose");

                if (FastBuildSettings.FastBuildMonitor)
                    fastBuildCommandLineOptions.Add("-monitor");

                // Configuring cache mode if that configuration is allowed to use caching
                if (Configuration.FastBuildCacheAllowed)
                {
                    // Setting the appropriate cache type commandline for that target.
                    switch (FastBuildSettings.CacheType)
                    {
                        case FastBuildSettings.CacheTypes.CacheRead:
                            fastBuildCommandLineOptions.Add("-cacheread");
                            break;
                        case FastBuildSettings.CacheTypes.CacheWrite:
                            fastBuildCommandLineOptions.Add("-cachewrite");
                            break;
                        case FastBuildSettings.CacheTypes.CacheReadWrite:
                            fastBuildCommandLineOptions.Add("-cache");
                            break;
                        default:
                            break;
                    }
                }

                if (FastBuildSettings.FastBuildDistribution && Configuration.FastBuildDistribution)
                    fastBuildCommandLineOptions.Add("-dist");

                if (FastBuildSettings.FastBuildWait)
                    fastBuildCommandLineOptions.Add("-wait");

                if (FastBuildSettings.FastBuildNoStopOnError)
                    fastBuildCommandLineOptions.Add("-nostoponerror");

                if (FastBuildSettings.FastBuildFastCancel)
                    fastBuildCommandLineOptions.Add("-fastcancel");

                if (FastBuildSettings.FastBuildNoUnity)
                    fastBuildCommandLineOptions.Add("-nounity");

                if (!string.IsNullOrEmpty(Configuration.FastBuildCustomArgs))
                    fastBuildCommandLineOptions.Add(Configuration.FastBuildCustomArgs);

                fastBuildCommandLineOptions.Add(" -config $(SolutionName)" + FastBuildSettings.FastBuildConfigFileExtension);
                
                return fastBuildCommandLineOptions;
            }
        }
        
        /// <summary>
        /// Generates "<solutionName>.rdjson" file for <paramref name="solution"/>.
        /// Also gathers information about projects for later usage in "Modules" section.
        /// </summary>
        public void Generate(Builder builder, Solution solution, List<Solution.Configuration> configurations, string solutionFile,
            List<string> generatedFiles, List<string> skipFiles)
        {
            var projects = new OrderedDictionary();

            foreach (var solutionConfig in configurations)
            {
                foreach (var proj in solutionConfig.IncludedProjectInfos)
                {
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

                    projConfig.Add("ProjectConfig", proj.Configuration.Name);
                    projConfig.Add("SolutionConfig", solutionConfig.Name);
                    if (!IgnoreDefaults)
                    {
                        projConfig.Add("DoBuild", (proj.ToBuild != Solution.Configuration.IncludedProjectInfo.Build.No).ToString());
                    }

                    if (!projObject.ContainsKey(proj.Configuration.Platform.ToString()))
                    {
                        projObject.Add(proj.Configuration.Platform.ToString(), new List<object>());
                    }
                    
                    projObject[proj.Configuration.Platform.ToString()].Add(projConfig);
                }
            }
            
            var file = new FileInfo(solutionFile);

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            using (var serializer = new Util.JsonSerializer(writer) { IsOutputFormatted = true })
            {
                serializer.IsOutputFormatted = !Minimize;
                serializer.Serialize(projects);
                serializer.Flush();

                if (builder.Context.WriteGeneratedFile(null, file, stream))
                {
                    generatedFiles.Add(Path.Combine(file.DirectoryName, file.Name));
                }
                else
                {
                    skipFiles.Add(Path.Combine(file.DirectoryName, file.Name));
                }
            }
        }

        /// <summary>
        /// Generates all <paramref name="project"/>-related configuration files.
        /// </summary>
        public void Generate(Builder builder, Project project, List<Project.Configuration> configurations, string projectFile,
            List<string> generatedFiles, List<string> skipFiles)
        {
            foreach (var config in configurations)
            {
                var context = new RiderGenerationContext(builder, project, config, projectFile,
                        Path.GetFileName(projectFile).Substring(1));

                var projectOptionsGen = new ProjectOptionsGenerator();
                projectOptionsGen.GenerateOptions(context);
                GenerateConfiguration(context, generatedFiles, skipFiles);
            }
        }

        private void GenerateConfiguration(RiderGenerationContext context, List<string> generatedFiles, List<string> skipFiles)
        {
            var info = new OrderedDictionary();
            var includePaths = new Strings();
            var defines = new Strings();
            var modules = new OrderedDictionary();
            var toolchain = new OrderedDictionary();
            var buildInfo = new OrderedDictionary();

            toolchain.Add("Compiler", context.GetCompiler());
            toolchain.Add("CppStandard", context.GetCppStandard());
            toolchain.Add("Architecture", context.GetArchitecture());
            toolchain.Add("bUseRTTI", context.IsRttiEnabled());
            toolchain.Add("bUseExceptions", context.IsExceptionEnabled());
            toolchain.Add("bIsBuildingLibrary", context.Configuration.Output == Project.Configuration.OutputType.Lib);
            toolchain.Add("bIsBuildingDll", context.Configuration.Output == Project.Configuration.OutputType.Dll);
            toolchain.Add("Configuration", context.Configuration.Name);
            toolchain.Add("bOptimizeCode", context.IsOptimizationEnabled());
            toolchain.Add("bUseInlining", context.IsInliningEnabled());
            toolchain.Add("bUseUnity", context.IsBlob());
            toolchain.Add("bCreateDebugInfo", context.IsDebugInfo());
            toolchain.Add("bUseAVX", context.IsAvx());
            toolchain.Add("bStrictConformanceMode", context.IsConformanceMode());
            toolchain.Add("PrecompiledHeaderAction", context.GetPchAction());

            using (context.Resolver.NewScopedParameter("SolutionDir", context.ProjectDirectory))
            using (context.Resolver.NewScopedParameter("ProjectDir", context.Configuration.ProjectPath))
            {
                var targetPath = Path.Combine(context.Configuration.TargetPath, context.Configuration.TargetFileFullNameWithExtension);
                buildInfo.Add("TargetPath", targetPath);

                var commands = GetBuildCommands(context);

                if (commands.Build != "" || !IgnoreDefaults)
                    buildInfo.Add("BuildCmd", commands.Build);
                if (commands.Rebuild != "" || !IgnoreDefaults)
                    buildInfo.Add("ReBuildCmd", commands.Rebuild);
                if (commands.Clean != "" || !IgnoreDefaults)
                    buildInfo.Add("CleanCmd", commands.Clean);
            }

            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);
            includePaths.AddRange(platformVcxproj.GetPlatformIncludePaths(context));

            defines.AddRange(platformVcxproj.GetImplicitlyDefinedSymbols(context));
            defines.AddRange(context.Options.ExplicitDefines);
            
            var curProjectInfo = _projectsInfo[context.Configuration.Target][context.Project.Name];
            modules.Add(curProjectInfo.Name, curProjectInfo.ToDictionary());
            
            foreach (var dependency in context.Configuration.ResolvedDependencies)
            {
                // Dependencies can contain projects with the same names. They will share same project info.
                if (modules.Contains(dependency.Project.Name))
                {
                    continue;
                }
                
                var dependencyInfo = _projectsInfo[dependency.Target][dependency.Project.Name];
                modules.Add(dependencyInfo.Name, dependencyInfo.ToDictionary());
            }

            info.Add("Name", context.Configuration.ProjectName);
            info.Add("Configuration", context.Configuration.Name);
            info.Add("Platform", context.Configuration.Platform.ToString());
            info.Add("ToolchainInfo", toolchain);
            info.Add("BuildInfo", buildInfo);

            info.Add("EnvironmentIncludePaths", includePaths);
            info.Add("EnvironmentDefinitions", defines);
            info.Add("Modules", modules);
            
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

        private BuildCommands GetBuildCommands(RiderGenerationContext context)
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

            using (context.Resolver.NewScopedParameter("ProjectFile", context.Configuration.ProjectFullFileNameWithExtension))
            using (context.Resolver.NewScopedParameter("ConfigurationName", context.Configuration.Name))
            using (context.Resolver.NewScopedParameter("PlatformName", 
                Util.GetPlatformString(context.Configuration.Platform, context.Project, context.Configuration.Target)))
            {
                return new BuildCommands
                {
                    Build = GetMsBuildCommand(context, "Build"),
                    Rebuild = GetMsBuildCommand(context, "Rebuild"),
                    Clean = GetMsBuildCommand(context, "Clean")
                };
            }
        }
        
        private string GetFastBuildCommand(RiderGenerationContext context, FastBuildMakeCommandGenerator.BuildType commandType)
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

        private string GetFastBuildClean(RiderGenerationContext context)
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

        private string GetMsBuildCommand(RiderGenerationContext context, string buildCommand)
        {
            var unresolvedCommand = Template.MsBuildBuildCommand;

            using (context.Resolver.NewScopedParameter("Command", buildCommand))
            {
                return context.Resolver.Resolve(unresolvedCommand);
            }
        }
    }
}
