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
    public partial class Bff
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

        private static object s_masterBffLock = new object();
        private static Builder s_masterBffBuilder = null;

        private static Dictionary<KeyValuePair<DevEnv, Platform>, UniqueList<string>> s_masterBffCopySections = new Dictionary<KeyValuePair<DevEnv, Platform>, UniqueList<string>>();
        private static UniqueList<string> s_masterBffCustomSections = new UniqueList<string>(); // section that is not ordered
        private static Dictionary<string, CompilerSettings> s_masterCompilerSettings = new Dictionary<string, CompilerSettings>();
        private static Strings s_masterBffFilenames = new Strings();

        public const string CurrentBffPathVariable = ".CurrentBffPath";
        public const string CurrentBffPathKey = "$CurrentBffPath$";

        public static IUnityResolver UnityResolver = new HashUnityResolver();

        public static bool IsMasterBffFilename(string filename)
        {
            return s_masterBffFilenames.Contains(filename);
        }

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

        internal static string GetMasterBffPath(Project.Configuration conf)
        {
            string platformBFFDirectory = "";

            // There is a bug in fastbuild and we can't have 2 separate bff in the same directory so put them in their respective platform directories.
            if (FastBuildSettings.SeparateMasterBFFPerPlatform)
            {
                platformBFFDirectory = Path.DirectorySeparatorChar + conf.Platform.ToString();
            }
            string masterPath = conf.Project.GuidReferencePath + Path.DirectorySeparatorChar + FastBuildSettings.FastBuildMasterBFFPath + platformBFFDirectory;
            masterPath = Util.SimplifyPath(masterPath);
            return Path.GetFullPath(masterPath);
        }

        public static string GetFastBuildExecutableRelativeToMasterBffPath(Project.Configuration conf)
        {
            string makePath = conf.Project.GuidReferencePath + Path.DirectorySeparatorChar + FastBuildSettings.FastBuildMakeCommand;
            makePath = Util.SimplifyPath(makePath);
            string fastBuildFullName = Path.GetFullPath(makePath);
            string masterBffPath = GetMasterBffPath(conf);
            string relativeToMasterBffPath = Util.PathGetRelative(masterBffPath, fastBuildFullName, true);
            return relativeToMasterBffPath;
        }

        public static string GetMasterBffFileName(Project.Configuration conf)
        {
            StringBuilder filenameBuilder = new StringBuilder(128);
            filenameBuilder.Append(FastBuildSettings.MasterBffFileName);
            if (FastBuildSettings.IncludeCompilerInMasterBFFFilename)
                filenameBuilder.Append($".{conf.Compiler}");

            string bffFileName = filenameBuilder.ToString();
            return GetBffFileName(GetMasterBffPath(conf), bffFileName);
        }

        internal static string GetGlobalBffConfigFileName(string masterBffFileName)
        {
            string globalConfigFile = masterBffFileName;
            globalConfigFile = globalConfigFile.Insert(masterBffFileName.IndexOf(".bff"), "-globalsettings");
            return globalConfigFile;
        }

        private static bool IsSupportedFastBuildPlatform(Platform platform)
        {
            return PlatformRegistry.Has<IPlatformBff>(platform);
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

        private class MasterBffInfo
        {
            // Dependency dictionary based on the include string (many projects might be in one .bff or a single project might generate many
            public Dictionary<string, Strings> BffIncludeToDependencyIncludes = new Dictionary<string, Strings>();
            public DevEnv DevEnv;
            public UniqueList<Platform> Platforms = new UniqueList<Platform>();
            public List<string> AllConfigsSections = new List<string>(); // All Configs section when running with a source file filter
        }

        private struct DependencyId
        {
            public DependencyId(Platform platform, string masterBffFileName)
            {
                Platform = platform;
                MasterBffFileName = masterBffFileName;
            }
            public Platform Platform;
            public string MasterBffFileName;
        }

        private static void BuilderEventPostGeneration(List<Project> projects, List<Solution> solutions)
        {
            if (!FastBuildSettings.FastBuildSupportEnabled)
                return;

            List<Project> sortedProjects = projects;
            sortedProjects.Sort((x, y) => string.Compare(x.Name, y.Name));
            var masterBffToInfo = new Dictionary<string, MasterBffInfo>();

            foreach (Project project in sortedProjects)
            {
                // Export projects should not have any master bff
                if (project.GetType().IsDefined(typeof(Export), false))
                    continue;

                // When the project has a source file filter, only keep it if the file list is not empty
                if (project.SourceFilesFilters != null && (project.SourceFilesFiltersCount == 0 || project.SkipProjectWhenFiltersActive))
                    continue;

                foreach (var conf in project.Configurations)
                {
                    if (conf.IsFastBuild && IsSupportedFastBuildPlatform(conf.Platform) && conf.DoNotGenerateFastBuild == false)
                    {
                        string masterBffFileName = GetMasterBffFileName(conf);
                        MasterBffInfo info;
                        if (!masterBffToInfo.TryGetValue(masterBffFileName, out info))
                        {
                            info = new MasterBffInfo();
                            masterBffToInfo[masterBffFileName] = info;
                            info.DevEnv = conf.Compiler;
                        }
                        info.Platforms.Add(conf.Platform);

                        if (FastBuildSettings.WriteAllConfigsSection)
                            info.AllConfigsSections.Add(GetShortProjectName(project, conf));

                        string bffFullFileNameCapitalized = Util.GetCapitalizedPath(conf.BffFullFileName);
                        string projectBffFullPath = $"{bffFullFileNameCapitalized}{FastBuildSettings.FastBuildConfigFileExtension}";

                        var currentBffDependencyIncludes = info.BffIncludeToDependencyIncludes.GetValueOrAdd(projectBffFullPath, new Strings());

                        // Generate bff include list
                        // --------------------------------------
                        var orderedProjectDeps = GetOrderedFlattenedProjectDependencies(conf);
                        foreach (var depProjConfig in orderedProjectDeps)
                        {
                            // Export projects should not have any master bff
                            if (depProjConfig.Project.GetType().IsDefined(typeof(Export), false))
                                continue;

                            // When the project has a source file filter, only keep it if the file list is not empty
                            if (depProjConfig.Project.SourceFilesFilters != null && (depProjConfig.Project.SourceFilesFiltersCount == 0 || depProjConfig.Project.SkipProjectWhenFiltersActive))
                                continue;

                            Trace.Assert(depProjConfig.Project != project, "Sharpmake-FastBuild : Project dependencies refers to itself.");

                            string depBffFullFileNameCapitalized = Util.GetCapitalizedPath(depProjConfig.BffFullFileName);
                            currentBffDependencyIncludes.Add($"{depBffFullFileNameCapitalized}{FastBuildSettings.FastBuildConfigFileExtension}");
                        }
                    }
                }
            }

            foreach (var masterBffEntry in masterBffToInfo)
            {
                string masterBffPath = Path.GetDirectoryName(masterBffEntry.Key);
                string fastBuildMasterBffDependencies = FileGeneratorUtilities.RemoveLineTag;

                // Need to keep track of which include we already added.
                Strings totalIncludeList = new Strings();
                StringBuilder result = new StringBuilder();

                if (masterBffEntry.Value.BffIncludeToDependencyIncludes.Count > 0)
                {
                    foreach (var includeList in masterBffEntry.Value.BffIncludeToDependencyIncludes)
                    {
                        Trace.Assert(!includeList.Value.Contains(includeList.Key), "Sharpmake-FastBuild: Circular dependency detected!");

                        totalIncludeList.AddRange(includeList.Value.Values);

                        // need to add current BFF in case not already in the list.
                        totalIncludeList.Add(includeList.Key);
                    }

                    foreach (var projectBffFullPath in totalIncludeList)
                    {
                        string projectFullPath = Path.GetDirectoryName(projectBffFullPath);
                        var projectPathRelativeFromMasterBff = Util.PathGetRelative(masterBffPath, projectFullPath, true);

                        string bffKeyRelative = Path.Combine(CurrentBffPathKey, Path.GetFileName(projectBffFullPath));

                        string include = string.Join(
                            Environment.NewLine,
                            "{",
                            $"    {CurrentBffPathVariable} = \"{projectPathRelativeFromMasterBff}\"",
                            $"    #include \"{bffKeyRelative}\"",
                            "}"
                        );

                        result.AppendLine(include);
                    }

                    fastBuildMasterBffDependencies = result.ToString();
                }
                GenerateMasterBffFile(masterBffEntry.Value, masterBffEntry.Key, fastBuildMasterBffDependencies);
            }
        }

        private static void GenerateMasterBffGlobalSettingsFile(string masterBffGlobalConfigFile, MasterBffInfo masterBffInfo)
        {
            var fileGenerator = new FileGenerator();

            WriteMasterSettingsSection(fileGenerator, masterBffInfo);
            WriteMasterCompilerSection(fileGenerator, masterBffInfo);

            // remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();
            MemoryStream bffCleanMemoryStream = fileGenerator.ToMemoryStream();

            // Write master bff file
            FileInfo bffFileInfo = new FileInfo(masterBffGlobalConfigFile);
            if (s_masterBffBuilder.Context.WriteGeneratedFile(null, bffFileInfo, bffCleanMemoryStream))
            {
                Project.FastBuildGeneratedFileCount++;
                Project.FastBuildMasterGeneratedFiles.Add(masterBffGlobalConfigFile);
            }
            else
            {
                Project.FastBuildUpToDateFileCount++;
            }
        }

        private static void GenerateMasterBffFile(
            MasterBffInfo bffInfo,
            string masterBffFileName,
            string fastBuildMasterBffDependencies
        )
        {
            // Start writing Bff
            var fileGenerator = new FileGenerator();

            // Global configuration file is in the same directory as the master bff but filename suffix added to its filename.
            string globalConfigFile = GetGlobalBffConfigFileName(masterBffFileName);
            GenerateMasterBffGlobalSettingsFile(globalConfigFile, bffInfo);
            string globalConfigFileRelative = Path.GetFileName(globalConfigFile);

            using (fileGenerator.Declare("fastBuildProjectName", "Master"))
            using (fileGenerator.Declare("fastBuildGlobalConfigurationInclude", string.Format("#include \"{0}\"", globalConfigFileRelative)))
            {
                fileGenerator.Write(Template.ConfigurationFile.HeaderFile);
                foreach (Platform platform in bffInfo.Platforms)
                {
                    using (fileGenerator.Declare("fastBuildDefine", GetPlatformSpecificDefine(platform)))
                        fileGenerator.Write(Template.ConfigurationFile.Define);
                }
                fileGenerator.Write(Template.ConfigurationFile.GlobalConfigurationInclude);
            }

            WriteMasterCopySection(fileGenerator, bffInfo);
            WriteMasterCustomSection(fileGenerator);
            WriteMasterIncludeSection(fileGenerator, fastBuildMasterBffDependencies);

            if (bffInfo.AllConfigsSections.Count != 0)
            {
                using (fileGenerator.Declare("fastBuildConfigs", FBuildFormatList(bffInfo.AllConfigsSections, 4)))
                    fileGenerator.Write(Template.ConfigurationFile.AllConfigsSection);
            }

            // remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();
            MemoryStream bffCleanMemoryStream = fileGenerator.ToMemoryStream();

            // Write master bff file
            s_masterBffFilenames.Add(masterBffFileName);
            FileInfo bffFileInfo = new FileInfo(masterBffFileName);
            if (s_masterBffBuilder.Context.WriteGeneratedFile(null, bffFileInfo, bffCleanMemoryStream))
            {
                Project.FastBuildGeneratedFileCount++;
                Project.FastBuildMasterGeneratedFiles.Add(bffFileInfo.FullName);
            }
            else
                Project.FastBuildUpToDateFileCount++;
        }

        private static void WriteMasterSettingsSection(IFileGenerator masterBffGenerator, MasterBffInfo masterBffInfo)
        {
            string tempFolder = Path.GetTempPath();

            string cachePath = FileGeneratorUtilities.RemoveLineTag;
            string cachePluginDLL = FileGeneratorUtilities.RemoveLineTag;
            if (FastBuildSettings.CachePath != null)
            {
                cachePath = ".CachePath = '" + FastBuildSettings.CachePath + "'";

                if (FastBuildSettings.CachePluginDLLFilePath != null)
                    cachePluginDLL = ".CachePluginDLL = '" + FastBuildSettings.CachePluginDLLFilePath + "'";
            }

            string fastBuildPATH = FileGeneratorUtilities.RemoveLineTag;
            if (FastBuildSettings.SetPathToResourceCompilerInEnvironment && masterBffInfo.Platforms.Any(p => PlatformRegistry.Has<IMicrosoftPlatformBff>(p)))
            {
                // !FIX FOR LINK : fatal error LNK1158: cannot run rc.exe!
                //
                // link.exe on win64 executes rc.exe by itself on some occasions
                // if it doesn't find it, link errors can occur
                //
                // link.exe will first search rc.exe next to it, and if it fails
                // it will look for it in the folers listed by the PATH
                // environment variable, so we'll try to replicate that process
                // in sharpmake:
                //
                //       1) Get the linker path
                //       2) Look for rc.exe near it
                //       3) If found, exit
                //       4) If not, add a PATH environment variable pointing to the rc.exe folder

                Platform platform = masterBffInfo.Platforms.First();

                var platformSettings =
                    s_masterCompilerSettings
                        .Where(x => x.Value.DevEnv == masterBffInfo.DevEnv)
                        .Where(x => x.Value.PlatformFlags.HasFlag(platform))
                        .Select(x => x.Value as CompilerSettings);

                string defaultResourceCompilerPath = Path.GetDirectoryName(masterBffInfo.DevEnv.GetWindowsResourceCompiler(Platform.win64));

                Strings resourceCompilerPaths = new Strings();
                foreach (var setting in platformSettings)
                {
                    foreach (var configurationPair in setting.Configurations)
                    {
                        var configuration = configurationPair.Value as CompilerSettings.Configuration;

                        // check if the configuration has a linker
                        if (configuration.LinkerPath != FileGeneratorUtilities.RemoveLineTag)
                        {
                            // if so, try to find a rc.exe near it
                            if (!File.Exists(Path.Combine(configuration.LinkerPath, "rc.exe")))
                            {
                                // if not found, get the folder of the custom
                                // rc.exe or the default one to add it to PATH
                                if (configuration.ResourceCompiler != FileGeneratorUtilities.RemoveLineTag)
                                    resourceCompilerPaths.Add(Path.GetDirectoryName(configuration.ResourceCompiler));
                                else
                                    resourceCompilerPaths.Add(defaultResourceCompilerPath);
                            }
                        }
                    }
                }

                if (resourceCompilerPaths.Count == 1)
                    fastBuildPATH = Util.GetCapitalizedPath(resourceCompilerPaths.First());
                else if (resourceCompilerPaths.Count > 1)
                    throw new Error("Multiple conflicting resource compilers found in PATH! Please verify your ResourceCompiler settings.");
            }

            using (masterBffGenerator.Declare("fastBuildProjectName", "Master"))
            using (masterBffGenerator.Declare("fastBuildTempFolder", tempFolder))
            using (masterBffGenerator.Declare("CachePath", cachePath))
            using (masterBffGenerator.Declare("CachePluginDLL", cachePluginDLL))
            using (masterBffGenerator.Declare("fastBuildSystemRoot", FastBuildSettings.SystemRoot))
            using (masterBffGenerator.Declare("fastBuildUserProfile", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
            using (masterBffGenerator.Declare("fastBuildPATH", fastBuildPATH))
            {
                masterBffGenerator.Write(Template.ConfigurationFile.HeaderFile);
                masterBffGenerator.Write(Template.ConfigurationFile.GlobalSettings);
            }
        }

        private static void WriteMasterCompilerSection(IFileGenerator masterBffGenerator, MasterBffInfo masterBffInfo)
        {
            //masterBffWriter = new StreamWriter(masterBffMemoryStream);
            var sortedMasterCompileSettings =
                s_masterCompilerSettings
                    .Where(x => x.Value.DevEnv == masterBffInfo.DevEnv)
                    .Where(x => masterBffInfo.Platforms.TestPlatformFlags(x.Value.PlatformFlags))
                    .OrderBy(x => x.Value.CompilerName);

            foreach (var compiler in sortedMasterCompileSettings)
            {
                var compilerSettings = compiler.Value as CompilerSettings;
                Platform compilerPlatform = compiler.Value.PlatformFlags;
                string fastBuildVS2012EnumBugWorkaround = FileGeneratorUtilities.RemoveLineTag;
                if (FastBuildSettings.EnableVS2012EnumBugWorkaround &&
                    compiler.Value.DevEnv == DevEnv.vs2012 &&
                    compilerPlatform.HasFlag(Platform.win64))
                    fastBuildVS2012EnumBugWorkaround = ".VS2012EnumBugFix = true";

                using (masterBffGenerator.Declare("fastbuildCompilerName", compiler.Key))
                using (masterBffGenerator.Declare("fastBuildVisualStudioEnvironment", compilerSettings.RootPath))
                using (masterBffGenerator.Declare("fastBuildCompilerExecutable", string.IsNullOrEmpty(compilerSettings.Executable) ? FileGeneratorUtilities.RemoveLineTag : compilerSettings.Executable))
                using (masterBffGenerator.Declare("fastBuildExtraFiles", compilerSettings.ExtraFiles.Count > 0 ? FBuildCollectionFormat(compilerSettings.ExtraFiles, 20) : FileGeneratorUtilities.RemoveLineTag))
                using (masterBffGenerator.Declare("fastBuildVS2012EnumBugWorkaround", fastBuildVS2012EnumBugWorkaround))
                {
                    masterBffGenerator.Write(Template.ConfigurationFile.CompilerSetting);
                    foreach (var compilerConfiguration in compilerSettings.Configurations)
                    {
                        var compConf = compilerConfiguration.Value as CompilerSettings.Configuration;

                        if (!masterBffInfo.Platforms.Contains(compConf.Platform))
                            continue;

                        using (masterBffGenerator.Declare("fastBuildConfigurationName", compilerConfiguration.Key))
                        using (masterBffGenerator.Declare("fastBuildBinPath", compConf.BinPath))
                        using (masterBffGenerator.Declare("fastBuildLinkerPath", compConf.LinkerPath))
                        using (masterBffGenerator.Declare("fastBuildResourceCompiler", compConf.ResourceCompiler))
                        using (masterBffGenerator.Declare("fastBuildCompilerName", compConf.Compiler != FileGeneratorUtilities.RemoveLineTag ? compConf.Compiler : compiler.Key))
                        using (masterBffGenerator.Declare("fastBuildLibrarian", compConf.Librarian))
                        using (masterBffGenerator.Declare("fastBuildLinker", compConf.Linker))
                        using (masterBffGenerator.Declare("fastBuildPlatformLibPaths", string.IsNullOrWhiteSpace(compConf.PlatformLibPaths) ? FileGeneratorUtilities.RemoveLineTag : compConf.PlatformLibPaths))
                        using (masterBffGenerator.Declare("fastBuildExecutable", compConf.Executable))
                        using (masterBffGenerator.Declare("fastBuildUsing", compConf.UsingOtherConfiguration))
                        {
                            masterBffGenerator.Write(Template.ConfigurationFile.CompilerConfiguration);
                        }
                    }
                }
            }
        }

        private static void WriteMasterCopySection(IFileGenerator masterBffGenerator, MasterBffInfo bffInfo)
        {
            Strings sections = new Strings();
            foreach (Platform platform in bffInfo.Platforms)
            {
                var devEnvPlatform = new KeyValuePair<DevEnv, Platform>(bffInfo.DevEnv, platform);
                if (s_masterBffCopySections.ContainsKey(devEnvPlatform))
                    sections.AddRange(s_masterBffCopySections[devEnvPlatform]);
            }

            var sortedSections = sections.GetValuesWithCustomSort((x, y) => string.Compare(x, y, StringComparison.Ordinal));

            foreach (var copySection in sortedSections)
                masterBffGenerator.Write(new StringReader(copySection).ReadToEnd());
        }

        private static void WriteMasterCustomSection(IFileGenerator masterBffGenerator)
        {
            if (s_masterBffCustomSections.Count != 0)
                masterBffGenerator.Write(Template.ConfigurationFile.CustomSectionHeader);
            foreach (var customSection in s_masterBffCustomSections)
                masterBffGenerator.Write(new StringReader(customSection).ReadToEnd());
        }

        private static void WriteMasterIncludeSection(IFileGenerator masterBffGenerator, string fastBuildMasterBffDependencies)
        {
            using (masterBffGenerator.Declare("fastBuildProjectName", "Master"))
            using (masterBffGenerator.Declare("fastBuildOrderedBffDependencies", fastBuildMasterBffDependencies))
            {
                masterBffGenerator.Write(Template.ConfigurationFile.Includes);
            }
        }

        public static void InitializeBuilder(Builder builder)
        {
            // Order is important and this must be registered before any other post generation callbacks.
            s_masterBffBuilder = builder;
            builder.EventPostGeneration += BuilderEventPostGeneration;

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
            string projectBffFile = Bff.GetBffFileName(projectPath, firstConf.BffFileName);
            string fastBuildClrSupport = Util.IsDotNet(firstConf) ? "/clr" : FileGeneratorUtilities.RemoveLineTag;
            List<Vcxproj.ProjectFile> filesInNonDefaultSection;
            var confSourceFiles = GetGeneratedFiles(context, configurations, out filesInNonDefaultSection);

            var bffPreBuildSection = new Dictionary<string, string>();
            var bffCustomPreBuildSection = new Dictionary<string, string>();
            var bffMasterPerPlatformSections = new Dictionary<KeyValuePair<DevEnv, Platform>, Dictionary<string, string>>();
            var bffCompilerSection = new Dictionary<string, Tuple<string, Platform>>();

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

                var devEnvPlatformPair = new KeyValuePair<DevEnv, Platform>(conf.Compiler, conf.Platform);
                var bffMasterSection = bffMasterPerPlatformSections.GetValueOrAdd(devEnvPlatformPair, new Dictionary<string, string>());

                string masterBffPath = Bff.GetMasterBffPath(conf);
                string sourceFilesRelativeInputPath = Util.GetConvertedRelativePath(projectPath, context.ProjectSourceCapitalized, masterBffPath, true, project.RootPath);

                if (IsSupportedFastBuildPlatform(conf.Platform) && confSourceFiles.ContainsKey(conf))
                {
                    if (conf.IsBlobbed && conf.FastBuildBlobbed)
                    {
                        throw new Error("Sharpmake-FastBuild: Configuration " + conf + " is configured for blobbing by fastbuild and sharpmake. This is illegal.");
                    }

                    var defaultTuple = GetDefaultTupleConfig();
                    var confSubConfigs = confSourceFiles[conf];

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

                        ProjectOptionsGenerator.VcxprojCmdLineOptions confCmdLineOptions = cmdLineOptions[conf];

                        Strings fastBuildCompilerInputPatternList = isCompileAsCFile ? new Strings { ".c" } : project.SourceFilesCPPExtensions;
                        Strings fastBuildCompilerInputPatternTransformedList = new Strings(fastBuildCompilerInputPatternList.Select((s) => { return "*" + s; }));

                        string fastBuildCompilerInputPattern = FBuildCollectionFormat(fastBuildCompilerInputPatternTransformedList, 32);

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

                        string platformToolSetPath = Path.Combine(conf.Compiler.GetVisualStudioDir(), "VC");

                        string compilerName = "Compiler-" + Util.GetSimplePlatformString(conf.Platform) + "-" + confOptions["PlatformToolset"] + "-" + conf.Target.GetFragment<DevEnv>().ToString();

                        if (!bffCompilerSection.ContainsKey(compilerName))
                            bffCompilerSection.Add(compilerName, Tuple.Create(platformToolSetPath, conf.Platform));

                        bool useObjectLists = Sharpmake.Options.GetObject<Options.Vc.Linker.UseLibraryDependencyInputs>(conf) == Sharpmake.Options.Vc.Linker.UseLibraryDependencyInputs.Enable;
                        string outputFile = confOptions["OutputFile"];
                        string fastBuildOutputFile = Util.SimplifyPath(Util.GetConvertedRelativePath(projectPath, outputFile, masterBffPath, true, project.RootPath));
                        string fastBuildOutputFileShortName = GetShortProjectName(project, conf);
                        string fastBuildProjectDependencies = "''";
                        List<string> fastBuildProjectDependencyList = new List<string>();
                        List<string> fastBuildProjectExeUtilityDependencyList = new List<string>();

                        if (conf.Output == Project.Configuration.OutputType.Exe ||
                            conf.Output == Project.Configuration.OutputType.Dll)
                        {
                            StringBuilder result = new StringBuilder();
                            result.Append("\n");

                            var orderedProjectDeps = GetOrderedFlattenedProjectDependencies(conf, false);
                            foreach (var depProjConfig in orderedProjectDeps)
                            {
                                Trace.Assert(depProjConfig.Project != project, "Sharpmake-FastBuild : Project dependencies refers to itself.");
                                Trace.Assert(conf.ResolvedDependencies.Contains(depProjConfig));
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
                        if (confSubConfigs.Keys.Count > 1)
                        {
                            if (subConfigIndex != confSubConfigs.Keys.Count - 1)
                            {
                                partialLibInfo = "[Partial Lib of " + fastBuildOutputFileShortName + "]";
                                fastBuildOutputFileShortName += "_" + subConfigIndex.ToString();
                                fastBuildOutputFile = fastBuildOutputFile.Insert(fastBuildOutputFile.Length - 4, "_" + subConfigIndex.ToString());
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
                                        result.Append((i++ != 0 ? "                                '" : "'") + subConfigObject + "',\n");
                                    else
                                        result.Append((i++ != 0 ? "                                '" : "'") + subConfigObject + "_objects',\n");
                                }
                                if (result.Length > 0)
                                    result.Remove(result.Length - 1, 1);
                                fastBuildObjectListDependencies = result.ToString();
                            }
                        }

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

                        string fastBuildCompilerPCHOptions = isUsePrecomp ? Template.ConfigurationFile.UsePrecomp : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildCompilerPCHOptionsClang = isUsePrecomp ? Template.ConfigurationFile.UsePrecompClang : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildLinkerOutputFile = fastBuildOutputFile;
                        string fastBuildStampExecutable = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildStampArguments = FileGeneratorUtilities.RemoveLineTag;
                        var fastBuildTargetSubTargets = new List<string>();
                        {
                            if (conf.Output == Project.Configuration.OutputType.Exe || conf.ExecuteTargetCopy)
                            {
                                if(conf.CopyDependenciesBuildStep != null)
                                    throw new NotImplementedException("CopyDependenciesBuildStep are not supported with FastBuild");

                                var copies = ProjectOptionsGenerator.ConvertPostBuildCopiesToRelative(conf, masterBffPath);
                                foreach (var copy in copies)
                                {
                                    var sourceFile = copy.Key;
                                    var destinationFolder = copy.Value;

                                    var destinationFile = Path.Combine(destinationFolder, Path.GetFileName(sourceFile));

                                    string fastBuildCopyAlias = GetFastBuildCopyAlias(sourceFile, destinationFile);
                                    fastBuildTargetSubTargets.Add(fastBuildCopyAlias);

                                    using (bffGenerator.Declare("fastBuildCopyAlias", fastBuildCopyAlias))
                                    using (bffGenerator.Declare("fastBuildCopySource", sourceFile))
                                    using (bffGenerator.Declare("fastBuildCopyDest", destinationFile))
                                    {
                                        if (!bffMasterSection.ContainsKey(fastBuildCopyAlias))
                                            bffMasterSection.Add(fastBuildCopyAlias, bffGenerator.Resolver.Resolve(Template.ConfigurationFile.CopyFileSection));
                                    }
                                }
                            }

                            foreach (var preEvent in conf.EventPreBuildExecute)
                            {
                                string fastBuildPreBuildAlias = preEvent.Key;
                                fastBuildTargetSubTargets.Add(fastBuildPreBuildAlias);
                            }
                            foreach (var customPreEvent in conf.EventCustomPrebuildExecute)
                            {
                                string fastBuildCustomPreBuildAlias = customPreEvent.Key;
                                fastBuildTargetSubTargets.Add(fastBuildCustomPreBuildAlias);
                            }

                            fastBuildTargetSubTargets.AddRange(fastBuildProjectExeUtilityDependencyList);

                            if (conf.Output == Project.Configuration.OutputType.Lib && useObjectLists)
                                fastBuildTargetSubTargets.Add(fastBuildOutputFileShortName + "_objects");
                            else
                                fastBuildTargetSubTargets.Add(fastBuildOutputFileShortName + "_" + outputType);


                            foreach (var postEvent in conf.EventPostBuildExecute)
                            {
                                string fastBuildPostBuildAlias = postEvent.Key;
                                fastBuildTargetSubTargets.Add(fastBuildPostBuildAlias);
                            }

                            if (conf.Output != Project.Configuration.OutputType.Dll)
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
                        string confCmdLineOptionsAddDeps = confCmdLineOptions["AdditionalDependencies"];
                        if (confCmdLineOptionsAddDeps != FileGeneratorUtilities.RemoveLineTag)
                        {
                            Regex pathSeparatorRegex = new Regex(@"\""(.*?)\""", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

                            StringBuilder result = new StringBuilder();

                            Match match = pathSeparatorRegex.Match(confCmdLineOptionsAddDeps);
                            for (; match.Success; match = match.NextMatch())
                            {
                                string additionalDependency = match.Groups[1].ToString();

                                // Properly compute dependency identifier
                                int subStringStartIndex = 0;
                                int subStringLength = additionalDependency.Length;
                                if (additionalDependency.EndsWith(".lib", StringComparison.OrdinalIgnoreCase))
                                    subStringLength -= 4;
                                if (additionalDependency.StartsWith("-l", StringComparison.Ordinal))
                                {
                                    subStringStartIndex = 2;
                                    subStringLength -= 2;
                                }
                                string testedDep = additionalDependency.Substring(subStringStartIndex, subStringLength);

                                if (!fastBuildProjectDependencyList.Contains(testedDep) && !IsObjectList(fastBuildProjectDependencyList, testedDep))
                                {
                                    if (clangPlatformBff == null)
                                    {
                                        result.Append(@"""" + additionalDependency + @""" ");
                                    }
                                    else
                                    {
                                        if (additionalDependency.StartsWith("-l", StringComparison.Ordinal))
                                        {
                                            additionalDependency = "lib" + additionalDependency.Substring(2);
                                        }
                                        if (Path.GetExtension(additionalDependency) == String.Empty)
                                            additionalDependency = additionalDependency + ".a";

                                        result.Append(@"""" + Util.SimplifyPath(Util.PathMakeStandard(additionalDependency)) + @""" ");
                                    }
                                }
                            }

                            if (result.Length > 0)
                                result.Remove(result.Length - 1, 1);

                            string finalDeps = result.ToString();
                            confCmdLineOptions["AdditionalDependencies"] = finalDeps.Length > 0 ? finalDeps : FileGeneratorUtilities.RemoveLineTag;
                        }

                        string fastBuildConsumeWinRTExtension = isConsumeWinRTExtensions ? "/ZW" : FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildUsingPlatformConfig = FileGeneratorUtilities.RemoveLineTag;
                        string clangFileLanguage = String.Empty;
                        string clangStd = "-std=gnu++14";

                        if (isCompileAsCFile)
                        {
                            fastBuildUsingPlatformConfig = platformBff.CConfigName;
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
                                fastBuildAdditionalCompilerOptionsFromCode += "/FU\"" + refByPath + "\" ";
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
                            foreach (var file in conf.ForceUsingFiles)
                            {
                                builderForceUsingFiles.AppendFormat(@" /FU""{0}""", file);
                            }
                            fastBuildCompilerForceUsing = builderForceUsingFiles.ToString();
                        }

                        if ((conf.Output == Project.Configuration.OutputType.Exe || conf.Output == Project.Configuration.OutputType.Dll) && conf.PostBuildStampExe != null)
                        {
                            fastBuildStampExecutable = Util.GetConvertedRelativePath(projectPath, conf.PostBuildStampExe.ExecutableFile, masterBffPath, true, project.RootPath);
                            fastBuildStampArguments = String.Format("{0} {1} {2}",
                                conf.PostBuildStampExe.ExecutableInputFileArgumentOption,
                                conf.PostBuildStampExe.ExecutableOutputFileArgumentOption,
                                conf.PostBuildStampExe.ExecutableOtherArguments);
                        }

                        bool linkObjects = false;
                        if (conf.Output == Project.Configuration.OutputType.Exe || conf.Output == Project.Configuration.OutputType.Dll)
                        {
                            linkObjects = (confOptions["UseLibraryDependencyInputs"] == "true");
                        }

                        Strings inputPaths = new Strings();
                        string fastBuildInputPath = FileGeneratorUtilities.RemoveLineTag;
                        string fastBuildInputExcludedFiles = FileGeneratorUtilities.RemoveLineTag;
                        {
                            Strings excludedSourceFiles = new Strings();
                            if (isNoBlobImplicitConfig && isDefaultTuple)
                            {
                                inputPaths.Add(sourceFilesRelativeInputPath);
                                inputPaths.AddRange(project.AdditionalSourceRootPaths.Select((p) => Util.GetConvertedRelativePath(p, "", masterBffPath, true, project.RootPath)));

                                excludedSourceFiles.AddRange(filesInNonDefaultSection.Select((f) => { return f.FileName; }));
                            }

                            if (isDefaultTuple && conf.FastBuildBlobbingStrategy == Project.Configuration.InputFileStrategy.Exclude && conf.FastBuildBlobbed)
                            {
                                // Adding the folders excluded from unity to the folders to build without unity(building each file individually)
                                inputPaths.AddRange(project.SourcePathsBlobExclude.Select((p) => Util.GetConvertedRelativePath(p, "", masterBffPath, true, project.RootPath)));
                            }

                            if (project.SourceFilesFiltersRegex.Count == 0)
                            {
                                fastBuildInputPath = FBuildCollectionFormat(inputPaths, 32);
                            }
                            else
                            {
                                inputPaths.Clear();
                            }

                            excludedSourceFiles.AddRange(conf.ResolvedSourceFilesBuildExclude);
                            excludedSourceFiles.AddRange(conf.PrecompSourceExclude);

                            Strings excludedSourceFilesRelative = new Strings();

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
                                string fileExtension = Path.GetExtension(file);
                                if (project.SourceFilesCompileExtensions.Contains(fileExtension))
                                {
                                    string inputPathRelative;
                                    if (IsFileIsInputPathList(masterBffPath, inputPaths, file, out inputPathRelative))
                                    {
                                        excludedSourceFilesRelative.Add(inputPathRelative);
                                    }
                                }
                            }
                            if (excludedSourceFilesRelative.Count > 0)
                            {
                                Strings includedExtensions = isCompileAsCFile ? new Strings { ".c" } : project.SourceFilesCPPExtensions;
                                fastBuildInputExcludedFiles = FBuildCollectionFormat(excludedSourceFilesRelative, 34, includedExtensions);
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
                                string sourceFileName = Util.GetConvertedRelativePath(projectPath, sourceFile.FileNameProjectRelative, masterBffPath, true, project.RootPath);

                                if (isUsePrecomp && conf.PrecompSource != null && sourceFile.FileName.EndsWith(conf.PrecompSource, StringComparison.OrdinalIgnoreCase))
                                {
                                    fastBuildPrecompiledSourceFile = sourceFileName;
                                }
                                else if (String.Compare(sourceFile.FileExtension, ".rc", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    if (microsoftPlatformBff.SupportsResourceFiles)
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
                                        if (!IsRelativeFilenameInInputPathList(inputPaths, sourceFileName))
                                        {
                                            fastbuildSourceFilesList.Add(sourceFileName);
                                        }
                                    }
                                }
                            }
                            fastBuildSourceFiles = FBuildFormatList(fastbuildSourceFilesList, 32);
                            fastBuildResourceFiles = FBuildFormatList(fastbuildResourceFilesList, 30);
                        }

                        if (projectHasResourceFiles)
                            resourceFilesSections.Add(fastBuildOutputFileShortName + "_resources");

                        // It is useless to have an input pattern defined if there is no input path
                        if (fastBuildInputPath == FileGeneratorUtilities.RemoveLineTag)
                            fastBuildCompilerInputPattern = FileGeneratorUtilities.RemoveLineTag;

                        string fastBuildObjectListResourceDependencies = FormatListPartForTag(resourceFilesSections, 32, true);

                        switch (conf.Output)
                        {
                            case Project.Configuration.OutputType.Lib:
                            case Project.Configuration.OutputType.Exe:
                            case Project.Configuration.OutputType.Dll:
                                using (bffGenerator.Declare("conf", conf))
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
                                using (bffGenerator.Declare("fastBuildClangStd", clangStd))
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
                                    if ((conf.Output == Project.Configuration.OutputType.Exe ||
                                            conf.Output == Project.Configuration.OutputType.Dll) && subConfigIndex == confSubConfigs.Keys.Count - 1 // only last subconfig will generate objectlist
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

                                    if (conf.Output == Project.Configuration.OutputType.Dll && subConfigIndex != confSubConfigs.Keys.Count - 1)
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
                                                    if (subConfigIndex == confSubConfigs.Keys.Count - 1)
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
                                            platformBff.SetupExtraLinkerSettings(bffGenerator, conf.Output, fastBuildOutputFile);
                                        }

                                        bffGenerator.Write(Template.ConfigurationFile.EndSection);

                                        foreach (var postEvent in conf.EventPostBuildExecute)
                                        {
                                            if (postEvent.Value is Project.Configuration.BuildStepExecutable)
                                            {
                                                var execCommand = postEvent.Value as Project.Configuration.BuildStepExecutable;

                                                using (bffGenerator.Declare("fastBuildPreBuildName", postEvent.Key))
                                                using (bffGenerator.Declare("fastBuildPrebuildExeFile", execCommand.ExecutableFile))
                                                using (bffGenerator.Declare("fastBuildPreBuildInputFile", execCommand.ExecutableInputFileArgumentOption))
                                                using (bffGenerator.Declare("fastBuildPreBuildOutputFile", execCommand.ExecutableOutputFileArgumentOption))
                                                using (bffGenerator.Declare("fastBuildPreBuildArguments", execCommand.ExecutableOtherArguments))
                                                using (bffGenerator.Declare("fastBuildPrebuildWorkingPath", execCommand.ExecutableWorkingDirectory))
                                                using (bffGenerator.Declare("fastBuildPrebuildUseStdOutAsOutput", execCommand.FastBuildUseStdOutAsOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                                                {
                                                    bffGenerator.Write(Template.ConfigurationFile.GenericExcutableSection);
                                                }
                                            }
                                            else if (postEvent.Value is Project.Configuration.BuildStepCopy)
                                            {
                                                var copyCommand = postEvent.Value as Project.Configuration.BuildStepCopy;

                                                using (bffGenerator.Declare("fastBuildCopyAlias", postEvent.Key))
                                                using (bffGenerator.Declare("fastBuildCopySource", copyCommand.SourcePath))
                                                using (bffGenerator.Declare("fastBuildCopyDest", copyCommand.DestinationPath))
                                                using (bffGenerator.Declare("fastBuildCopyDirName", postEvent.Key))
                                                using (bffGenerator.Declare("fastBuildCopyDirSourcePath", Util.EnsureTrailingSeparator(copyCommand.SourcePath)))
                                                using (bffGenerator.Declare("fastBuildCopyDirDestinationPath", Util.EnsureTrailingSeparator(copyCommand.DestinationPath)))
                                                using (bffGenerator.Declare("fastBuildCopyDirRecurse", copyCommand.IsRecurse.ToString().ToLower()))
                                                using (bffGenerator.Declare("fastBuildCopyDirPattern", GetBffFileCopyPattern(copyCommand.CopyPattern)))
                                                {
                                                    bffGenerator.Write(Template.ConfigurationFile.CopyFileSection);
                                                }
                                            }
                                        }

                                        foreach (var preEvent in conf.EventPreBuildExecute)
                                        {
                                            if (preEvent.Value is Project.Configuration.BuildStepExecutable)
                                            {
                                                var execCommand = preEvent.Value as Project.Configuration.BuildStepExecutable;

                                                using (bffGenerator.Declare("fastBuildPreBuildName", preEvent.Key))
                                                using (bffGenerator.Declare("fastBuildPrebuildExeFile", execCommand.ExecutableFile))
                                                using (bffGenerator.Declare("fastBuildPreBuildInputFile", execCommand.ExecutableInputFileArgumentOption))
                                                using (bffGenerator.Declare("fastBuildPreBuildOutputFile", execCommand.ExecutableOutputFileArgumentOption))
                                                using (bffGenerator.Declare("fastBuildPreBuildArguments", execCommand.ExecutableOtherArguments))
                                                using (bffGenerator.Declare("fastBuildPrebuildWorkingPath", execCommand.ExecutableWorkingDirectory))
                                                using (bffGenerator.Declare("fastBuildPrebuildUseStdOutAsOutput", execCommand.FastBuildUseStdOutAsOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                                                {
                                                    if (!bffPreBuildSection.ContainsKey(preEvent.Key))
                                                        bffPreBuildSection.Add(preEvent.Key, bffGenerator.Resolver.Resolve(Template.ConfigurationFile.GenericExcutableSection));
                                                }
                                            }
                                            else if (preEvent.Value is Project.Configuration.BuildStepCopy)
                                            {
                                                var copyCommand = preEvent.Value as Project.Configuration.BuildStepCopy;

                                                using (bffGenerator.Declare("fastBuildCopyAlias", preEvent.Key))
                                                using (bffGenerator.Declare("fastBuildCopySource", copyCommand.SourcePath))
                                                using (bffGenerator.Declare("fastBuildCopyDest", copyCommand.DestinationPath))
                                                using (bffGenerator.Declare("fastBuildCopyDirName", preEvent.Key))
                                                using (bffGenerator.Declare("fastBuildCopyDirSourcePath", Util.EnsureTrailingSeparator(copyCommand.SourcePath)))
                                                using (bffGenerator.Declare("fastBuildCopyDirDestinationPath", Util.EnsureTrailingSeparator(copyCommand.DestinationPath)))
                                                using (bffGenerator.Declare("fastBuildCopyDirRecurse", copyCommand.IsRecurse.ToString().ToLower()))
                                                using (bffGenerator.Declare("fastBuildCopyDirPattern", GetBffFileCopyPattern(copyCommand.CopyPattern)))
                                                {
                                                    if (!bffPreBuildSection.ContainsKey(preEvent.Key) && copyCommand.IsFileCopy)
                                                        bffPreBuildSection.Add(preEvent.Key, bffGenerator.Resolver.Resolve(Template.ConfigurationFile.CopyFileSection));
                                                    else if (!bffPreBuildSection.ContainsKey(preEvent.Key))
                                                        bffPreBuildSection.Add(preEvent.Key, bffGenerator.Resolver.Resolve(Template.ConfigurationFile.CopyDirSection));
                                                }
                                            }
                                        }

                                        foreach (var customEvent in conf.EventCustomPrebuildExecute)
                                        {
                                            if (customEvent.Value is Project.Configuration.BuildStepExecutable)
                                            {
                                                var exeCommand = customEvent.Value as Project.Configuration.BuildStepExecutable;

                                                using (bffGenerator.Declare("fastBuildPreBuildName", customEvent.Key))
                                                using (bffGenerator.Declare("fastBuildPrebuildExeFile", exeCommand.ExecutableFile))
                                                using (bffGenerator.Declare("fastBuildPreBuildInputFile", exeCommand.ExecutableInputFileArgumentOption))
                                                using (bffGenerator.Declare("fastBuildPreBuildOutputFile", exeCommand.ExecutableOutputFileArgumentOption))
                                                using (bffGenerator.Declare("fastBuildPreBuildArguments", exeCommand.ExecutableOtherArguments))
                                                using (bffGenerator.Declare("fastBuildPrebuildWorkingPath", exeCommand.ExecutableWorkingDirectory))
                                                using (bffGenerator.Declare("fastBuildPrebuildUseStdOutAsOutput", exeCommand.FastBuildUseStdOutAsOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                                                {
                                                    if (!bffCustomPreBuildSection.ContainsKey(customEvent.Key))
                                                        bffCustomPreBuildSection.Add(customEvent.Key, bffGenerator.Resolver.Resolve(Template.ConfigurationFile.GenericExcutableSection));
                                                }
                                            }
                                            else if (customEvent.Value is Project.Configuration.BuildStepCopy)
                                            {
                                                var copyCommand = customEvent.Value as Project.Configuration.BuildStepCopy;

                                                using (bffGenerator.Declare("fastBuildCopyAlias", customEvent.Key))
                                                using (bffGenerator.Declare("fastBuildCopySource", copyCommand.SourcePath))
                                                using (bffGenerator.Declare("fastBuildCopyDest", copyCommand.DestinationPath))
                                                using (bffGenerator.Declare("fastBuildCopyDirName", customEvent.Key))
                                                using (bffGenerator.Declare("fastBuildCopyDirSourcePath", copyCommand.SourcePath))
                                                using (bffGenerator.Declare("fastBuildCopyDirDestinationPath", copyCommand.DestinationPath))
                                                using (bffGenerator.Declare("fastBuildCopyDirRecurse", copyCommand.IsRecurse.ToString().ToLower()))
                                                using (bffGenerator.Declare("fastBuildCopyDirPattern", copyCommand.CopyPattern))
                                                {
                                                    if (!bffCustomPreBuildSection.ContainsKey(customEvent.Key) && copyCommand.IsFileCopy)
                                                        bffCustomPreBuildSection.Add(customEvent.Key, bffGenerator.Resolver.Resolve(Template.ConfigurationFile.CopyFileSection));
                                                    else if (!bffCustomPreBuildSection.ContainsKey(customEvent.Key))
                                                        bffCustomPreBuildSection.Add(customEvent.Key, bffGenerator.Resolver.Resolve(Template.ConfigurationFile.CopyDirSection));
                                                }
                                            }
                                        }

                                        // Write Target Alias
                                        using (bffGenerator.Declare("fastBuildTargetSubTargets", FBuildFormatList(fastBuildTargetSubTargets, 15)))
                                        {
                                            bffGenerator.Write(Template.ConfigurationFile.TargetSection);
                                        }

                                        lock (s_masterBffLock)
                                        {
                                            UniqueList<string> list;
                                            if (!s_masterBffCopySections.TryGetValue(devEnvPlatformPair, out list))
                                            {
                                                list = new UniqueList<string>();
                                                s_masterBffCopySections.Add(devEnvPlatformPair, list);
                                            }

                                            list.AddRange(list.Union(bffPreBuildSection.Values));
                                            s_masterBffCustomSections.AddRange(s_masterBffCustomSections.Union(bffCustomPreBuildSection.Values));
                                            list.AddRange(list.Union(bffMasterSection.Values));

                                            foreach (var compiler in bffCompilerSection)
                                            {
                                                var rootPath = compiler.Value.Item1;
                                                var platform = compiler.Value.Item2;
                                                PlatformRegistry.Query<IPlatformBff>(platform)?.AddCompilerSettings(s_masterCompilerSettings, compiler.Key, rootPath, conf.Compiler, conf.Project.RootPath);
                                            }
                                        }
                                    }
                                }
                                break;
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
                Project.FastBuildGeneratedFileCount++;
                generatedFiles.Add(bffFileInfo.FullName);
            }
            else
            {
                Project.FastBuildUpToDateFileCount++;
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

            string masterBffPath = Bff.GetMasterBffPath(conf);
            string projectPath = new FileInfo(Util.GetCapitalizedPath(conf.BffFullFileName)).Directory.FullName;
            string sourceFilesRelativeInputPath = Util.GetConvertedRelativePath(projectPath, context.ProjectSourceCapitalized, masterBffPath, true, project.RootPath);

            const int spaceLength = 42;

            string fastBuildUnityInputFiles = FileGeneratorUtilities.RemoveLineTag;
            string fastBuildUnityInputExcludedfiles = FileGeneratorUtilities.RemoveLineTag;
            string fastBuildUnityPaths = FileGeneratorUtilities.RemoveLineTag;

            string fastBuildUnityInputExcludePath = FileGeneratorUtilities.RemoveLineTag;
            string fastBuildUnityCount = FileGeneratorUtilities.RemoveLineTag;

            int unityCount = conf.FastBuildUnityCount > 0 ? conf.FastBuildUnityCount : project.BlobCount;
            if(unityCount > 0)
                fastBuildUnityCount = unityCount.ToString(CultureInfo.InvariantCulture);

            var fastbuildUnityInputExcludePathList = new Strings();
            if (!string.IsNullOrEmpty(conf.BlobPath))
            {
                // TODO: only exclude if under the inclusion path, otherwise useless: maybe with IsFileIsInputPathList
                fastbuildUnityInputExcludePathList.Add(Util.GetConvertedRelativePath(projectPath, conf.BlobPath, masterBffPath, true, project.RootPath));
            }

            fastbuildUnityInputExcludePathList.AddRange(project.SourcePathsBlobExclude.Select((excludedPath) => Util.GetConvertedRelativePath(excludedPath, "", masterBffPath, true, project.RootPath)));
            fastBuildUnityInputExcludePath = FBuildCollectionFormat(fastbuildUnityInputExcludePathList, spaceLength:spaceLength);

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
                        string sourceFileRelative = Util.PathGetRelative(masterBffPath, file.FileName, true);
                        items.Add(sourceFileRelative);
                    }
                }
                fastBuildUnityInputFiles = FBuildFormatList(items, spaceLength);
            }
            else
            {
                // Fastbuild will process as unity all files contained in source Root folder and all additional roots.
                Strings unityInputPaths = new Strings();
                unityInputPaths.Add(sourceFilesRelativeInputPath);
                unityInputPaths.AddRange(project.AdditionalSourceRootPaths.Select((p) => Util.GetConvertedRelativePath(p, "", masterBffPath, true, project.RootPath)));

                // Remove any excluded paths(exclusion has priority)
                unityInputPaths.RemoveRange(fastbuildUnityInputExcludePathList);
                fastBuildUnityPaths = FBuildCollectionFormat(unityInputPaths, spaceLength);

                Strings excludedSourceFiles = new Strings();

                excludedSourceFiles.AddRange(conf.ResolvedSourceFilesBlobExclude);
                excludedSourceFiles.AddRange(conf.ResolvedSourceFilesBuildExclude);
                excludedSourceFiles.AddRange(conf.PrecompSourceExclude);
                Strings excludedSourceFilesRelative = new Strings();

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
                    string inputPathRelative;
                    if (IsFileIsInputPathList(masterBffPath, unityInputPaths, file, out inputPathRelative))
                    {
                        excludedSourceFilesRelative.Add(inputPathRelative);
                    }
                }
                if (excludedSourceFilesRelative.Count > 0)
                    fastBuildUnityInputExcludedfiles = FBuildCollectionFormat(excludedSourceFilesRelative, spaceLength, project.SourceFilesBlobExtensions);
            }

            if (fastBuildUnityInputFiles == FileGeneratorUtilities.RemoveLineTag &&
                fastBuildUnityPaths      == FileGeneratorUtilities.RemoveLineTag)
            {
                // no input path nor files => no unity
                return;
            }

            Unity unityFile = new Unity
            {
                // Note that the UnityName and UnityOutputPattern are intentionally left empty: they will be set in the Resolve
                UnityOutputPath = Util.GetConvertedRelativePath(projectPath, conf.FastBuildUnityPath, masterBffPath, true, project.RootPath),
                UnityInputIsolateWritableFiles = conf.FastBuildUnityInputIsolateWritableFiles.ToString().ToLower(),
                UnityInputIsolateWritableFilesLimit = conf.FastBuildUnityInputIsolateWritableFiles ? conf.FastBuildUnityInputIsolateWritableFilesLimit.ToString() : FileGeneratorUtilities.RemoveLineTag,
                UnityPCH = conf.PrecompHeader ?? FileGeneratorUtilities.RemoveLineTag,
                UnityInputExcludePath = fastBuildUnityInputExcludePath,
                UnityNumFiles = fastBuildUnityCount,
                UnityInputPath = fastBuildUnityPaths,
                UnityInputFiles = fastBuildUnityInputFiles,
                UnityInputExcludedFiles = fastBuildUnityInputExcludedfiles
            };

            // _unitites being a dictionary, a new entry will be created only
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

        private static string FBuildFormatList(List<string> items, int spaceLength)
        {
            if (items.Count == 0)
                return FileGeneratorUtilities.RemoveLineTag;

            StringBuilder strBuilder = new StringBuilder(1024 * 16);

            //
            // Write all selected items.
            //

            if (items.Count == 1)
            {
                strBuilder.AppendFormat("'{0}'", items.First());
            }
            else
            {
                string indent = new string(' ', spaceLength);

                strBuilder.Append("{");
                strBuilder.AppendLine();

                int itemIndex = 0;
                foreach (string item in items)
                {
                    strBuilder.AppendFormat("{0}    '{1}'", indent, item);
                    if (++itemIndex < items.Count)
                        strBuilder.AppendLine(",");
                    else
                        strBuilder.AppendLine();
                }
                strBuilder.AppendFormat("{0}}}", indent);
            }

            return strBuilder.ToString();
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

        private static string FBuildCollectionFormat(Strings collection, int spaceLength, Strings includedExtensions = null)
        {
            // Select items.
            List<string> items = new List<string>(collection.Count);

            foreach (string collectionItem in collection.SortedValues)
            {
                if (includedExtensions == null)
                {
                    items.Add(collectionItem);
                }
                else
                {
                    string extension = Path.GetExtension(collectionItem);
                    if (includedExtensions.Contains(extension))
                    {
                        items.Add(collectionItem);
                    }
                }
            }

            return FBuildFormatList(items, spaceLength);
        }

        private static string GetFastBuildCopyAlias(string sourceFileName, string destinationFileName)
        {
            string fastBuildCopyAlias = String.Format("Copy_{0}_{1}", Path.GetFileName(sourceFileName), (sourceFileName + destinationFileName).GetHashCode().ToString("X8"));
            return fastBuildCopyAlias;
        }

        private static UniqueList<Project.Configuration> GetOrderedFlattenedProjectDependencies(Project.Configuration conf, bool allDependencies = true)
        {
            var dependencies = new UniqueList<Project.Configuration>();
            GetOrderedFlattenedProjectDependenciesInternal(conf, dependencies, allDependencies);
            return dependencies;
        }

        private static void GetOrderedFlattenedProjectDependenciesInternal(Project.Configuration conf, UniqueList<Project.Configuration> dependencies, bool allDependencies)
        {
            if (conf.IsFastBuild)
            {
                IEnumerable<Project.Configuration> confDependencies = allDependencies ? conf.ResolvedDependencies : conf.ConfigurationDependencies;

                if (confDependencies.Contains(conf))
                    throw new Error("Cyclic dependency detected in project " + conf);

                if (!allDependencies)
                {
                    UniqueList<Project.Configuration> tmpDeps = new UniqueList<Project.Configuration>();
                    foreach (var dep in confDependencies)
                    {
                        GetOrderedFlattenedProjectDependenciesInternal(dep, tmpDeps, true);
                        tmpDeps.Add(dep);
                    }
                    foreach (var dep in tmpDeps)
                    {
                        if (dep.IsFastBuild && confDependencies.Contains(dep) && (conf != dep))
                            dependencies.Add(dep);
                    }
                }
                else
                {
                    foreach (var dep in confDependencies)
                    {
                        if (dependencies.Contains(dep))
                            continue;

                        GetOrderedFlattenedProjectDependenciesInternal(dep, dependencies, true);
                        if (dep.IsFastBuild)
                            dependencies.Add(dep);
                    }
                }
            }
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
            var confSubConfigs = new Dictionary<Configuration, Dictionary<Tuple<bool, bool, bool, bool, bool, bool, Options.Vc.Compiler.Exceptions, Tuple<bool>>, List<Vcxproj.ProjectFile>>>(); // What the fuck?
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

        private string GetBffFileCopyPattern(string copyPattern)
        {
            if (string.IsNullOrEmpty(copyPattern))
                return copyPattern;

            string[] patterns = copyPattern.Split(null);

            if (patterns == null || patterns.Length < 2)
                return "'" + copyPattern + "'";

            return "{ " + string.Join(", ", patterns.Select(p => "'" + p + "'")) + " }";
        }

        private bool IsFileIsInputPathList(
            string masterBffPath,
            Strings inputPaths,
            string filename,
            out string sourceFileRelative
        )
        {
            // Convert each of file paths to each of the input paths and try to
            // find the first one not starting from ..(ie the file is in the tested input path)
            foreach (string inputPath in inputPaths)
            {
                string inputAbsPath = Util.PathGetAbsolute(masterBffPath, inputPath);
                string sourceFileRelativeTmp = Util.PathGetRelative(inputAbsPath, filename, true);
                if (!sourceFileRelativeTmp.StartsWith(".."))
                {
                    sourceFileRelative = sourceFileRelativeTmp;
                    return true;
                }
            }

            sourceFileRelative = string.Empty;
            return false;
        }
        private bool IsRelativeFilenameInInputPathList(Strings inputPaths, string filename)
        {
            foreach (string inputPath in inputPaths)
            {
                if (filename.StartsWith(inputPath))
                    return true;
            }

            return false;
        }
    }
}
