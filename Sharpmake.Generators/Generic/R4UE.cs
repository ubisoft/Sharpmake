using System;
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
    public partial class R4UE : IProjectGenerator
    {
        /// <summary>
        /// Callback which should be added to <see cref="Builder.EventPostGeneration"/> in order to generate Rider project model.
        /// </summary>
        public static void PostGenerationCallback(List<Project> projects, List<Solution> solutions)
        {
            var builder = Builder.Instance;
            var riderFolder = Path.Combine(solutions.First().SharpmakeCsPath, ".Rider");
            var generator = new R4UE();
            var configs = solutions.First().Configurations.ToList();
            
            // Do not move. Acquires all the information about projects for later usage in "Modules" sections.
            generator.Generate(builder, projects, configs, Path.Combine(riderFolder, "root.json"), new List<string>(),
                               new List<string>());
            
            foreach (var project in projects)
            {
                generator.Generate(builder, project, project.Configurations.ToList(), Path.Combine(riderFolder, project.Name),
                                   new List<string>(), new List<string>());
            }

        }

        /// <summary>
        /// Helper class to keep all the project information for "Modules" section of json file.
        /// </summary>
        private class RiderProjectInfo
        {
            public string Name { get; }
            public Strings PublicDependencyModules { get; }
            public Strings PublicIncludePaths { get; }
            public Strings PrivateIncludePaths { get; }
            public Strings PublicDefinitions { get; }
            public Strings PrivateDefinitions { get; }

            public RiderProjectInfo(string projectName)
            {
                Name = projectName;
                PublicDependencyModules = new Strings();
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
                PublicDependencyModules.AddRange(config.ResolvedPublicDependencies.Select(it => it.Project.Name));
                PublicIncludePaths.AddRange(config.IncludePaths);
                PrivateIncludePaths.AddRange(config.IncludePrivatePaths);
                PublicDefinitions.AddRange(config.ExportDefines);
                PrivateDefinitions.AddRange(config.Defines);
            }

            /// <summary>
            /// Returns OrderedDictionary for json serialization
            /// </summary>
            /// <returns></returns>
            public OrderedDictionary ToDictionary()
            {
                var resDict = new OrderedDictionary();
                resDict.Add("PublicDependencyModules", PublicDependencyModules);
                resDict.Add("PublicIncludePaths", PublicIncludePaths);
                resDict.Add("PrivateIncludePaths", PrivateIncludePaths);
                resDict.Add("PublicDefinitions", PublicDefinitions);
                resDict.Add("PrivateDefinitions", PrivateDefinitions);
                return resDict;
            }
        }

        /// <summary>
        /// Maps projects information for later usage in "Modules" section.
        /// </summary>
        private Dictionary<string, RiderProjectInfo> _projectsInfo = new Dictionary<string, RiderProjectInfo>();

        /// <summary>
        /// Helper class for storing all the project-related information.
        /// </summary>
        private class RiderGenerationContext : IGenerationContext
        {
            public Builder Builder { get; }
            public Project Project { get; }
            public Project.Configuration Configuration { get; }
            public string ProjectDirectory { get; }
            public string ProjectFileName { get; }
            public string ProjectPath { get; }
            
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
                string projectPath)
            {
                Builder = builder;
                Resolver = new Resolver();

                FileInfo fileInfo = new FileInfo(projectPath);
                ProjectPath = fileInfo.FullName;
                ProjectDirectory = Path.GetDirectoryName(ProjectPath);
                ProjectFileName = Path.GetFileName(ProjectPath);
                
                ProjectDirectory = Path.GetDirectoryName(fileInfo.FullName);
                ProjectFileName = fileInfo.Name;
                Project = project;

                Configuration = configuration;
                ProjectDirectoryCapitalized = Util.GetCapitalizedPath(ProjectDirectory);
                ProjectSourceCapitalized = Util.GetCapitalizedPath(Project.SourceRootPath);
                DevelopmentEnvironment = configuration.Target.GetFragment<DevEnv>();

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

                return fastBuildCommandLineOptions;
            }
        }
        
        private void GenerateConfOptions(RiderGenerationContext context)
        {
            var projectOptionsGen = new ProjectOptionsGenerator();
            projectOptionsGen.GenerateOptions(context);
        }
        
        /// <summary>
        /// Generates "root.json" file, gathers information about projects for later usage in "Modules" section.
        /// </summary>
        public void Generate(Builder builder, List<Project> projects, List<Solution.Configuration> configurations, string rootFile, List<string> generatedFiles,
            List<string> skipFiles)
        {
            var info = new OrderedDictionary();
            foreach (var project in projects)
            {
                var riderProjInfo = new RiderProjectInfo(project.Name);
                var projInfo = new Dictionary<string, List<string>>();

                foreach (var config in project.Configurations)
                {
                    riderProjInfo.ReadConfiguration(config);
                    if (!projInfo.ContainsKey(config.Platform.ToString()))
                    {
                        
                        projInfo.Add(config.Platform.ToString(), new List<string>());
                    }
                    
                    projInfo[config.Platform.ToString()].Add(config.Name);
                }
                
                _projectsInfo.Add(riderProjInfo.Name, riderProjInfo);
                info.Add(project.Name, projInfo);
            }
            
            var file = new FileInfo(rootFile);

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            using (var serializer = new Util.JsonSerializer(writer) { IsOutputFormatted = true })
            {
                serializer.Serialize(info);
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
        public void Generate(Builder builder, Project project, List<Project.Configuration> configurations, string projectFile, List<string> generatedFiles,
            List<string> skipFiles)
        {
            foreach (var config in configurations)
            {
                var context = new RiderGenerationContext(builder, project, config, projectFile);
                GenerateConfOptions(context);
                GenerateConfiguration(context, generatedFiles, skipFiles);
            }
        }

        private void GenerateConfiguration(RiderGenerationContext context, List<string> generatedFiles, List<string> skipFiles)
        {
            var info = new OrderedDictionary();
            var includePaths = new Strings();
            var modules = new OrderedDictionary();
            var toolchain = new OrderedDictionary();
            
            toolchain.Add("CppStandard", GetCppStandart(context.Configuration));
            toolchain.Add("bUseRTTI", IsRTTIEnabled(context.Configuration));
            toolchain.Add("bUseExceptions", IsExceptionEnabled(context.Configuration));
            toolchain.Add("bIsBuildingLibrary", context.Configuration.Output == Project.Configuration.OutputType.Dll
                                                || context.Configuration.Output == Project.Configuration.OutputType.Lib);
            toolchain.Add("bIsBuildingDll", context.Configuration.Output == Project.Configuration.OutputType.Dll);
            toolchain.Add("Configuration", context.Configuration.Name);
            toolchain.Add("bOptimizeCode", IsOptimizationEnabled(context.Configuration));
            toolchain.Add("bUseInlining", IsInliningEnabled(context.Configuration));
            toolchain.Add("bUseUnity", IsBlob(context.Configuration));
            toolchain.Add("bCreateDebugInfo", IsDebugInfo(context.Configuration));
            toolchain.Add("bUseAVX", IsAvx(context.Configuration));
            toolchain.Add("Compiler", context.Configuration.Compiler.ToString());
            toolchain.Add("bStrictConformanceMode", IsConformanceMode(context.Configuration));
            toolchain.Add("PrecompiledHeaderAction", GetPchAction(context));

            if (context.Configuration.IsFastBuild)
            {
                var beforeBuildCommand = context.Configuration.FastBuildCustomActionsBeforeBuildCommand;
                if (beforeBuildCommand == FileGeneratorUtilities.RemoveLineTag)
                {
                    beforeBuildCommand = "";
                }
                
                using (context.Resolver.NewScopedParameter("BeforeBuildCommand", beforeBuildCommand))
                {
                    toolchain.Add("BuildCmd", GetBuildCommand(context));
                    toolchain.Add("ReBuildCmd", GetReBuildCommand(context));
                    toolchain.Add("CleanCmd", GetCleanCommand(context));
                }
            }

            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);
            includePaths.AddRange(platformVcxproj.GetPlatformIncludePaths(context));

            var curProjectInfo = _projectsInfo[context.Project.Name];
            modules.Add(curProjectInfo.Name, curProjectInfo.ToDictionary());

            foreach (var dependency in context.Configuration.ResolvedDependencies.Select(it => it.ProjectName))
            {
                var dependencyInfo = _projectsInfo[dependency];
                modules.Add(dependencyInfo.Name, dependencyInfo.ToDictionary());
            }

            info.Add("Name", context.Configuration.ProjectName);
            info.Add("Configuration", context.Configuration.Name);
            info.Add("Platform", context.Configuration.Platform.ToString());
            info.Add("ToolchainInfo", toolchain);
            info.Add("EnvironmentIncludePaths", includePaths);
            info.Add("EnvironmentDefinitions", context.Configuration.Defines);
            info.Add("Modules", modules);
            
            var file = new FileInfo($"{context.ProjectPath}_{context.Configuration.Platform}_{context.Configuration.Name}.json");

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            using (var serializer = new Util.JsonSerializer(writer) { IsOutputFormatted = true })
            {
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

        private string GetCppStandart(Project.Configuration config)
        {
            string res = "Default";

            var stdOptions = new List<object>(); 
            stdOptions.AddRange(Options.GetObjects<Options.Vc.Compiler.CppLanguageStandard>(config).Select(it => it as object));
            stdOptions.AddRange(Options.GetObjects<Options.Clang.Compiler.CppLanguageStandard>(config).Select(it => it as object));
            stdOptions.AddRange(Options.GetObjects<Options.Makefile.Compiler.CppLanguageStandard>(config).Select(it => it as object));
            stdOptions.AddRange(Options.GetObjects<Options.XCode.Compiler.CppLanguageStandard>(config).Select(it => it as object));
            stdOptions.AddRange(Options.GetObjects<Options.Android.Compiler.CppLanguageStandard>(config).Select(it => it as object));

            if (stdOptions.Count > 0)
            {
                res = stdOptions.First().ToString();
            }
            
            return res;
        }
        
        private bool IsRTTIEnabled(Project.Configuration config)
        {
            return !config.CheckOptions(Options.Vc.Compiler.RTTI.Disable,
                         Options.Makefile.Compiler.Rtti.Disable,
                         Options.XCode.Compiler.RTTI.Disable,
                         Options.Clang.Compiler.Rtti.Disable);
        }
        
        private bool IsExceptionEnabled(Project.Configuration config)
        {
            return !config.CheckOptions(Options.Vc.Compiler.Exceptions.Disable,
                                        Options.Makefile.Compiler.Exceptions.Disable,
                                        Options.XCode.Compiler.Exceptions.Disable,
                                        Options.Clang.Compiler.Exceptions.Disable,
                                        Options.Android.Compiler.Exceptions.Disable);
        }
        
        private bool IsOptimizationEnabled(Project.Configuration config)
        {
            return !config.CheckOptions(Options.Vc.Compiler.Optimization.Disable,
                                        Options.Makefile.Compiler.OptimizationLevel.Disable,
                                        Options.XCode.Compiler.OptimizationLevel.Disable,
                                        Options.Clang.Compiler.OptimizationLevel.Disable);
        }
        
        private bool IsInliningEnabled(Project.Configuration config)
        {
            return !config.CheckOptions(Options.Vc.Compiler.Inline.Disable);
        }
        
        private bool IsBlob(Project.Configuration config)
        {
            return config.IsBlobbed || config.FastBuildBlobbed;
        }
        
        private bool IsDebugInfo(Project.Configuration config)
        {
            return !config.CheckOptions(Options.Vc.General.DebugInformation.Disable,
                                        Options.Makefile.Compiler.GenerateDebugInformation.Disable,
                                        Options.XCode.Compiler.GenerateDebuggingSymbols.Disable,
                                        Options.Clang.Compiler.GenerateDebugInformation.Disable,
                                        Options.Android.Compiler.DebugInformationFormat.None);
        }
        
        private bool IsAvx(Project.Configuration config)
        {
            return config.CheckOptions(Options.Vc.Compiler.EnhancedInstructionSet.AdvancedVectorExtensions,
                                       Options.Vc.Compiler.EnhancedInstructionSet.AdvancedVectorExtensions2,
                                       Options.Vc.Compiler.EnhancedInstructionSet.AdvancedVectorExtensions512);
        }
        
        private bool IsConformanceMode(Project.Configuration config)
        {
            return config.CheckOptions(Options.Vc.Compiler.ConformanceMode.Enable);
        }

        private string GetPchAction(RiderGenerationContext context)
        {
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);

            if (!Options.HasOption<Options.Vc.SourceFile.PrecompiledHeader>(context.Configuration))
            {
                if (platformVcxproj.HasPrecomp(context))
                {
                    return "Include";
                }

                return "None";
            }
            
            var pchOption = Options.GetObject<Options.Vc.SourceFile.PrecompiledHeader>(context.Configuration);
            switch (pchOption)
            {
                case Options.Vc.SourceFile.PrecompiledHeader.UsePrecompiledHeader:
                    return "Include";
                case Options.Vc.SourceFile.PrecompiledHeader.CreatePrecompiledHeader:
                    return "Create";
                default:
                    return "None";
            }
        }

        private string GetBuildCommand(RiderGenerationContext context)
        {
            var unresolvedCommand = Template.FastBuildBuildCommand;
            using (context.Resolver.NewScopedParameter("BuildCommand",
                                                       context.FastBuildMakeCommandGenerator.GetCommand(
                                                           FastBuildMakeCommandGenerator.BuildType.Build,
                                                           context.Configuration, context.FastBuildArguments)))
            {
                return context.Resolver.Resolve(unresolvedCommand);
            }
        }
        
        private string GetReBuildCommand(RiderGenerationContext context)
        {
            var unresolvedCommand = Template.FastBuildReBuildCommand;
            using (context.Resolver.NewScopedParameter("RebuildCommand",
                                                       context.FastBuildMakeCommandGenerator.GetCommand(
                                                           FastBuildMakeCommandGenerator.BuildType.Rebuild,
                                                           context.Configuration, context.FastBuildArguments)))
            {
                return context.Resolver.Resolve(unresolvedCommand);
            }
        }
        
        private string GetCleanCommand(RiderGenerationContext context)
        {
            var unresolvedOutput = Template.FastBuildCleanCommand;
            
            using (context.Resolver.NewScopedParameter("IntermediateDirectory", context.Options["IntermediateDirectory"])) 
            using (context.Resolver.NewScopedParameter("OutputDirectory", context.Options["OutputDirectory"]))
            using (context.Resolver.NewScopedParameter("TargetFileFullName", context.Configuration.TargetFileFullName))
            {
                return context.Resolver.Resolve(unresolvedOutput);
            }
        }
    }
}
