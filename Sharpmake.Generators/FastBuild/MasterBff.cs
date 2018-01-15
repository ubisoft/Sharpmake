using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators.FastBuild
{
    public class MasterBff : ISolutionGenerator
    {
        private static readonly Dictionary<string, ConfigurationsPerBff> s_confsPerSolutions = new Dictionary<string, ConfigurationsPerBff>();
        private static bool s_postGenerationHandlerInitialized = false;

        internal static string GetGlobalBffConfigFileName(string masterBffFileName)
        {
            string globalConfigFile = masterBffFileName;
            globalConfigFile = globalConfigFile.Insert(masterBffFileName.IndexOf(FastBuildSettings.FastBuildConfigFileExtension, StringComparison.Ordinal), "-globalsettings");
            return globalConfigFile;
        }

        private class MasterBffInfo
        {
            // Dependency dictionary based on the include string (many projects might be in one .bff or a single project might generate many
            public Dictionary<string, Dictionary<string, int>> BffIncludeToDependencyIncludes = new Dictionary<string, Dictionary<string, int>>();
            public DevEnv? DevEnv;
            public UniqueList<Platform> Platforms = new UniqueList<Platform>();
            public List<string> AllConfigsSections = new List<string>(); // All Configs section when running with a source file filter
        }
        private class ConfigurationsPerBff : IEnumerable<Solution.Configuration>
        {
            public static IEnumerable<ConfigurationsPerBff> Create(Solution solution, IEnumerable<Solution.Configuration> configurations)
            {
                var confsPerBffs = from conf in configurations
                                   group conf by conf.MasterBffFilePath into confsByBff
                                   select new ConfigurationsPerBff(solution, confsByBff.Key, confsByBff);

                foreach (var conf in confsPerBffs)
                {
                    if (conf.IsFastBuildEnabled())
                        yield return conf;
                }
            }

            public static IEnumerable<Solution.ResolvedProject> GetResolvedSolutionsProjects(IEnumerable<ConfigurationsPerBff> configurationsPerSolutions)
            {
                var result = new HashSet<Solution.ResolvedProject>();
                foreach (var solution in configurationsPerSolutions)
                {
                    foreach (var project in solution.ResolvedProjects)
                        result.Add(project);
                }

                return result;
            }

            public Solution Solution { get; }
            public string BffFilePath { get; }
            public string BffFilePathWithExtension => BffFilePath + FastBuildSettings.FastBuildConfigFileExtension;
            public Solution.Configuration[] Configurations { get; private set; }
            public Solution.ResolvedProject[] ResolvedProjects { get; }

            public void Merge(ConfigurationsPerBff other)
            {
                Debug.Assert(other.Solution == Solution);
                Debug.Assert(other.BffFilePath == BffFilePath);

                var merged = new HashSet<Solution.Configuration>(Configurations);
                foreach (var conf in other)
                    merged.Add(conf);
                Configurations = merged.ToArray();
            }

            public IEnumerator<Solution.Configuration> GetEnumerator()
            {
                return Configurations.Cast<Solution.Configuration>().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Configurations.GetEnumerator();
            }

            private ConfigurationsPerBff(Solution solution, string bffFilePath, IEnumerable<Solution.Configuration> configurations)
            {
                Solution = solution;
                BffFilePath = Util.SimplifyPath(bffFilePath);
                Configurations = configurations.ToArray();
                ResolvedProjects = solution.GetResolvedProjects(this).ToArray();
            }

            private bool IsFastBuildEnabled()
            {
                foreach (var solutionConfiguration in this)
                {
                    foreach (var solutionProject in ResolvedProjects)
                    {
                        var project = solutionProject.Project;

                        // Export projects do not have any bff
                        if (project.GetType().IsDefined(typeof(Export), false))
                            continue;

                        // When the project has a source file filter, only keep it if the file list is not empty
                        if (project.SourceFilesFilters != null && (project.SourceFilesFiltersCount == 0 || project.SkipProjectWhenFiltersActive))
                            continue;

                        Solution.Configuration.IncludedProjectInfo includedProject = solutionConfiguration.GetProject(solutionProject.Project.GetType());
                        bool perfectMatch = includedProject != null && solutionProject.Configurations.Contains(includedProject.Configuration);
                        if (!perfectMatch)
                            continue;

                        var conf = includedProject.Configuration;
                        if (!conf.IsFastBuildEnabledProjectConfig())
                            continue;

                        return true;
                    }
                }

                return false;
            }
        }

        public void Generate(
            Builder builder,
            Solution solution,
            List<Solution.Configuration> solutionConfigurations,
            string solutionFile,
            List<string> generatedFiles,
            List<string> skipFiles
        )
        {
            if (!FastBuildSettings.FastBuildSupportEnabled)
                return;

            //
            // In every case, we need a BFF with the name of the generated solution to start a
            // build from Visual Studio with the generated projects. If the name of the BFF to
            // generate for that solution happens to be the same as the solution's name, we
            // generate everything in that file. Otherwise, we generate the content of the BFF in
            // the appropriate file and we generate an *additional* BFF with the same name as the
            // solution which simply includes the "real" bff.
            //
            // The reason we have to do it like that is to enable building a specific project in
            // Visual Studio. A project can be included in several solutions, and can be built
            // differently depending on the solution. This means we can't generate a specific
            // master BFF name in the project's make command because which BFF is needed depends on
            // the solution, not the project. So instead, when VS builds a project, we use
            // $(SolutionName).bff as the BFF make command.
            //
            // So, if you want a shared master BFF instead of doing it per-solution or change the
            // name, that's great, but we *need* $(SolutionName).bff for things to work in Visual
            // Studio, even if all it does is include the real BFF.
            //

            IEnumerable<ConfigurationsPerBff> confsPerBffs = ConfigurationsPerBff.Create(solution, solutionConfigurations).ToArray();
            var retargetedConfsPerBffs = new List<ConfigurationsPerBff>();
            foreach (var confsPerBff in confsPerBffs)
            {
                if (confsPerBff.Configurations.Any(conf => conf.SolutionFilePath != conf.MasterBffFilePath))
                {
                    retargetedConfsPerBffs.Add(confsPerBff);
                    GenerateIncludeBffFileForSolution(builder, solutionFile, confsPerBff, generatedFiles, skipFiles);

                    // First collect all solutions and sort them by master BFF, then once we have all of
                    // them, the post-generation event handler will actually generate the BFF.
                    lock (s_confsPerSolutions)
                    {
                        if (!s_postGenerationHandlerInitialized)
                        {
                            builder.Generated += Builder_Generated;
                            s_postGenerationHandlerInitialized = true;
                        }

                        ConfigurationsPerBff other;
                        if (s_confsPerSolutions.TryGetValue(confsPerBff.BffFilePath, out other))
                            other.Merge(confsPerBff);
                        else
                            s_confsPerSolutions.Add(confsPerBff.BffFilePath, confsPerBff);
                    }
                }
                else
                {
                    GenerateMasterBffFiles(builder, new[] { confsPerBff });
                }
            }
        }

        private void GenerateIncludeBffFileForSolution(Builder builder, string solutionFilePath, ConfigurationsPerBff confsPerBff, IList<string> generatedFiles, IList<string> skippedFiles)
        {
            var fileGenerator = new FileGenerator();
            using (fileGenerator.Declare("solutionFileName", Path.GetFileName(solutionFilePath)))
            using (fileGenerator.Declare("masterBffFilePath", confsPerBff.BffFilePathWithExtension))
                fileGenerator.Write(Bff.Template.ConfigurationFile.IncludeMasterBff);

            using (var bffFileStream = fileGenerator.ToMemoryStream())
            {
                string bffFilePath = solutionFilePath + FastBuildSettings.FastBuildConfigFileExtension;
                var bffFileInfo = new FileInfo(bffFilePath);
                if (builder.Context.WriteGeneratedFile(null, bffFileInfo, bffFileStream))
                    generatedFiles.Add(bffFilePath);
                else
                    skippedFiles.Add(bffFilePath);
            }
        }

        private static void GenerateMasterBffFiles(Builder builder, IEnumerable<ConfigurationsPerBff> confsPerSolutions)
        {
            foreach (var confsPerBff in confsPerSolutions)
            {
                string bffFilePath = confsPerBff.BffFilePath;
                string bffFilePathWithExtension = Util.PathMakeStandard(bffFilePath + FastBuildSettings.FastBuildConfigFileExtension);
                if (GenerateMasterBffFile(builder, confsPerBff))
                {
                    Project.FastBuildGeneratedFileCount++;
                    Project.FastBuildMasterGeneratedFiles.Add(bffFilePathWithExtension);
                }
                else
                {
                    Project.FastBuildUpToDateFileCount++;
                }
            }
        }

        private static bool GenerateMasterBffFile(Builder builder, ConfigurationsPerBff configurationsPerBff)
        {
            string masterBffFilePath = Util.GetCapitalizedPath(configurationsPerBff.BffFilePathWithExtension);
            string masterBffDirectory = Path.GetDirectoryName(masterBffFilePath);
            string masterBffFileName = Path.GetFileName(masterBffFilePath);

            // Global configuration file is in the same directory as the master bff but filename suffix added to its filename.
            string globalConfigFullPath = GetGlobalBffConfigFileName(masterBffFilePath);
            string globalConfigFileName = Path.GetFileName(globalConfigFullPath);

            var solutionProjects = configurationsPerBff.ResolvedProjects;

            // Start writing Bff
            var fileGenerator = new FileGenerator();

            var masterBffInfo = new MasterBffInfo();

            var bffPreBuildSection = new Dictionary<string, string>();
            var bffCustomPreBuildSection = new Dictionary<string, string>();
            var bffMasterSection = new Dictionary<string, string>();
            var masterBffCopySections = new List<string>();
            var masterBffCustomSections = new UniqueList<string>(); // section that is not ordered

            string projectRootPath = null;
            bool mustGenerateFastbuild = false;

            foreach (Solution.Configuration solutionConfiguration in configurationsPerBff)
            {
                foreach (var solutionProject in solutionProjects)
                {
                    var includedProject = solutionConfiguration.GetProject(solutionProject.Project.GetType());
                    if (includedProject == null)
                        continue;

                    var project = solutionProject.Project;
                    var conf = includedProject.Configuration;

                    projectRootPath = project.RootPath;
                    mustGenerateFastbuild = true;

                    string bffFullFileNameCapitalized = Util.GetCapitalizedPath(conf.BffFullFileName);
                    string projectBffFullPath = $"{bffFullFileNameCapitalized}{FastBuildSettings.FastBuildConfigFileExtension}";

                    masterBffInfo.Platforms.Add(conf.Platform);
                    var devEnv = conf.Target.GetFragment<DevEnv>();
                    if (masterBffInfo.DevEnv == null)
                        masterBffInfo.DevEnv = devEnv;
                    else if (devEnv != masterBffInfo.DevEnv)
                        throw new Error($"Master bff {masterBffFileName} cannot contain varying devEnvs: {masterBffInfo.DevEnv} {devEnv}!");

                    if (FastBuildSettings.WriteAllConfigsSection)
                        masterBffInfo.AllConfigsSections.Add(Bff.GetShortProjectName(project, conf));

                    using (fileGenerator.Declare("conf", conf))
                    using (fileGenerator.Declare("target", conf.Target))
                    using (fileGenerator.Declare("project", conf.Project))
                    {
                        if (conf.Output == Project.Configuration.OutputType.Exe || conf.ExecuteTargetCopy)
                        {
                            var copies = ProjectOptionsGenerator.ConvertPostBuildCopiesToRelative(conf, masterBffDirectory);
                            foreach (var copy in copies)
                            {
                                var sourceFile = copy.Key;
                                var sourceFileName = Path.GetFileName(sourceFile);
                                var destinationFolder = copy.Value;
                                var destinationFile = Path.Combine(destinationFolder, sourceFileName);

                                // use the global root for alias computation, as the project has not idea in which master bff it has been included
                                var destinationRelativeToGlobal = Util.GetConvertedRelativePath(masterBffDirectory, destinationFolder, conf.Project.RootPath, true, conf.Project.RootPath);
                                string fastBuildCopyAlias = UtilityMethods.GetFastBuildCopyAlias(sourceFileName, destinationRelativeToGlobal);
                                {
                                    using (fileGenerator.Declare("fastBuildCopyAlias", fastBuildCopyAlias))
                                    using (fileGenerator.Declare("fastBuildCopySource", sourceFile))
                                    using (fileGenerator.Declare("fastBuildCopyDest", destinationFile))
                                    {
                                        if (!bffMasterSection.ContainsKey(fastBuildCopyAlias))
                                            bffMasterSection.Add(fastBuildCopyAlias, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.CopyFileSection));
                                    }
                                }
                            }
                        }

                        foreach (var preEvent in conf.EventPreBuildExecute)
                        {
                            if (preEvent.Value is Project.Configuration.BuildStepExecutable)
                            {
                                var execCommand = preEvent.Value as Project.Configuration.BuildStepExecutable;

                                using (fileGenerator.Declare("fastBuildPreBuildName", preEvent.Key))
                                using (fileGenerator.Declare("fastBuildPrebuildExeFile", execCommand.ExecutableFile))
                                using (fileGenerator.Declare("fastBuildPreBuildInputFile", execCommand.ExecutableInputFileArgumentOption))
                                using (fileGenerator.Declare("fastBuildPreBuildOutputFile", execCommand.ExecutableOutputFileArgumentOption))
                                using (fileGenerator.Declare("fastBuildPreBuildArguments", execCommand.ExecutableOtherArguments))
                                using (fileGenerator.Declare("fastBuildPrebuildWorkingPath", execCommand.ExecutableWorkingDirectory))
                                using (fileGenerator.Declare("fastBuildPrebuildUseStdOutAsOutput", execCommand.FastBuildUseStdOutAsOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                                {
                                    string eventKey = fileGenerator.Resolver.Resolve(preEvent.Key);
                                    if (!bffPreBuildSection.ContainsKey(eventKey))
                                        bffPreBuildSection.Add(eventKey, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.GenericExcutableSection));
                                }
                            }
                            else if (preEvent.Value is Project.Configuration.BuildStepCopy)
                            {
                                var copyCommand = preEvent.Value as Project.Configuration.BuildStepCopy;

                                using (fileGenerator.Declare("fastBuildCopyAlias", preEvent.Key))
                                using (fileGenerator.Declare("fastBuildCopySource", copyCommand.SourcePath))
                                using (fileGenerator.Declare("fastBuildCopyDest", copyCommand.DestinationPath))
                                using (fileGenerator.Declare("fastBuildCopyDirName", preEvent.Key))
                                using (fileGenerator.Declare("fastBuildCopyDirSourcePath", Util.EnsureTrailingSeparator(copyCommand.SourcePath)))
                                using (fileGenerator.Declare("fastBuildCopyDirDestinationPath", Util.EnsureTrailingSeparator(copyCommand.DestinationPath)))
                                using (fileGenerator.Declare("fastBuildCopyDirRecurse", copyCommand.IsRecurse.ToString().ToLower()))
                                using (fileGenerator.Declare("fastBuildCopyDirPattern", UtilityMethods.GetBffFileCopyPattern(copyCommand.CopyPattern)))
                                {
                                    string eventKey = fileGenerator.Resolver.Resolve(preEvent.Key);
                                    if (!bffPreBuildSection.ContainsKey(eventKey))
                                    {
                                        if (copyCommand.IsFileCopy)
                                            bffPreBuildSection.Add(eventKey, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.CopyFileSection));
                                        else
                                            bffPreBuildSection.Add(eventKey, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.CopyDirSection));
                                    }
                                }
                            }
                        }

                        foreach (var customEvent in conf.EventCustomPrebuildExecute)
                        {
                            if (customEvent.Value is Project.Configuration.BuildStepExecutable)
                            {
                                var exeCommand = customEvent.Value as Project.Configuration.BuildStepExecutable;

                                using (fileGenerator.Declare("fastBuildPreBuildName", customEvent.Key))
                                using (fileGenerator.Declare("fastBuildPrebuildExeFile", exeCommand.ExecutableFile))
                                using (fileGenerator.Declare("fastBuildPreBuildInputFile", exeCommand.ExecutableInputFileArgumentOption))
                                using (fileGenerator.Declare("fastBuildPreBuildOutputFile", exeCommand.ExecutableOutputFileArgumentOption))
                                using (fileGenerator.Declare("fastBuildPreBuildArguments", exeCommand.ExecutableOtherArguments))
                                using (fileGenerator.Declare("fastBuildPrebuildWorkingPath", exeCommand.ExecutableWorkingDirectory))
                                using (fileGenerator.Declare("fastBuildPrebuildUseStdOutAsOutput", exeCommand.FastBuildUseStdOutAsOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                                {
                                    string eventKey = fileGenerator.Resolver.Resolve(customEvent.Key);
                                    if (!bffCustomPreBuildSection.ContainsKey(eventKey))
                                        bffCustomPreBuildSection.Add(eventKey, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.GenericExcutableSection));
                                }
                            }
                            else if (customEvent.Value is Project.Configuration.BuildStepCopy)
                            {
                                var copyCommand = customEvent.Value as Project.Configuration.BuildStepCopy;

                                using (fileGenerator.Declare("fastBuildCopyAlias", customEvent.Key))
                                using (fileGenerator.Declare("fastBuildCopySource", copyCommand.SourcePath))
                                using (fileGenerator.Declare("fastBuildCopyDest", copyCommand.DestinationPath))
                                using (fileGenerator.Declare("fastBuildCopyDirName", customEvent.Key))
                                using (fileGenerator.Declare("fastBuildCopyDirSourcePath", copyCommand.SourcePath))
                                using (fileGenerator.Declare("fastBuildCopyDirDestinationPath", copyCommand.DestinationPath))
                                using (fileGenerator.Declare("fastBuildCopyDirRecurse", copyCommand.IsRecurse.ToString().ToLower()))
                                using (fileGenerator.Declare("fastBuildCopyDirPattern", copyCommand.CopyPattern))
                                {
                                    if (!bffCustomPreBuildSection.ContainsKey(customEvent.Key))
                                    {
                                        string eventKey = fileGenerator.Resolver.Resolve(customEvent.Key);
                                        if (copyCommand.IsFileCopy)
                                            bffCustomPreBuildSection.Add(eventKey, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.CopyFileSection));
                                        else
                                            bffCustomPreBuildSection.Add(eventKey, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.CopyDirSection));
                                    }
                                }
                            }
                        }
                    }

                    if (includedProject.ToBuild == Solution.Configuration.IncludedProjectInfo.Build.Yes)
                    {
                        var currentBffDependencyIncludes = masterBffInfo.BffIncludeToDependencyIncludes.GetValueOrAdd(projectBffFullPath, new Dictionary<string, int>());

                        // Generate bff include list
                        // --------------------------------------
                        var orderedProjectDeps = UtilityMethods.GetOrderedFlattenedProjectDependencies(conf);
                        int order = 0;
                        foreach (var depProjConfig in orderedProjectDeps)
                        {
                            // Export projects should not have any master bff
                            if (depProjConfig.Project.GetType().IsDefined(typeof(Export), false))
                                continue;

                            // When the project has a source file filter, only keep it if the file list is not empty
                            if (depProjConfig.Project.SourceFilesFilters != null && (depProjConfig.Project.SourceFilesFiltersCount == 0 || depProjConfig.Project.SkipProjectWhenFiltersActive))
                                continue;

                            Trace.Assert(depProjConfig.Project != project, "Sharpmake-FastBuild : Project dependencies refers to itself.");

                            string depBffFullFileNameCapitalized = Util.GetCapitalizedPath(depProjConfig.BffFullFileName) + FastBuildSettings.FastBuildConfigFileExtension;
                            int previousOrder;
                            if (currentBffDependencyIncludes.TryGetValue(depBffFullFileNameCapitalized, out previousOrder))
                            {
                                if (order > previousOrder)
                                    currentBffDependencyIncludes[depBffFullFileNameCapitalized] = order;
                            }
                            else
                            {
                                currentBffDependencyIncludes.Add(depBffFullFileNameCapitalized, order);
                            }
                            ++order;
                        }
                    }
                }
            }

            if (!mustGenerateFastbuild)
                throw new Error("Sharpmake-FastBuild : Trying to generate a MasterBff with none of its projects having a FastBuild configuration, or having a platform supporting it, or all of them having conf.DoNotGenerateFastBuild = true");

            masterBffCopySections.AddRange(bffMasterSection.Values);
            masterBffCopySections.AddRange(bffPreBuildSection.Values);

            masterBffCustomSections.AddRange(bffCustomPreBuildSection.Values);


            string fastBuildMasterBffDependencies = FileGeneratorUtilities.RemoveLineTag;

            // Need to keep track of which include we already added.
            Strings totalIncludeList = new Strings();
            StringBuilder result = new StringBuilder();

            if (masterBffInfo.BffIncludeToDependencyIncludes.Count > 0)
            {
                foreach (var bffToDependencyIncludes in masterBffInfo.BffIncludeToDependencyIncludes)
                {
                    var currentBff = bffToDependencyIncludes.Key;
                    var dependenciesDictionary = bffToDependencyIncludes.Value;
                    Trace.Assert(!dependenciesDictionary.ContainsKey(currentBff), "Sharpmake-FastBuild: Circular dependency detected!");

                    var includeList = dependenciesDictionary.ToList();
                    includeList.Sort((a, b) => a.Value.CompareTo(b.Value));

                    totalIncludeList.AddRange(includeList.Select(x => x.Key));

                    // need to add current BFF in case not already in the list.
                    totalIncludeList.Add(currentBff);
                }

                foreach (var projectBffFullPath in totalIncludeList)
                {
                    string projectFullPath = Path.GetDirectoryName(projectBffFullPath);
                    var projectPathRelativeFromMasterBff = Util.PathGetRelative(masterBffDirectory, projectFullPath, true);

                    string bffKeyRelative = Path.Combine(Bff.CurrentBffPathKey, Path.GetFileName(projectBffFullPath));

                    string include = string.Join(
                        Environment.NewLine,
                        "{",
                        $"    {Bff.CurrentBffPathVariable} = \"{projectPathRelativeFromMasterBff}\"",
                        $"    #include \"{bffKeyRelative}\"",
                        "}"
                    );

                    result.AppendLine(include);
                }

                fastBuildMasterBffDependencies = result.ToString();
            }

            var masterCompilerSettings = new Dictionary<string, CompilerSettings>();

            // TODO: test what happens when using multiple devenv, one per platform, in the same master bff: should it even be allowed?
            string platformToolSetPath = Path.Combine(masterBffInfo.DevEnv.Value.GetVisualStudioDir(), "VC");
            foreach (var platform in masterBffInfo.Platforms)
            {
                string compilerName = "Compiler-" + Util.GetSimplePlatformString(platform) + "-" + masterBffInfo.DevEnv.Value;
                PlatformRegistry.Query<IPlatformBff>(platform)?.AddCompilerSettings(masterCompilerSettings, compilerName, platformToolSetPath, masterBffInfo.DevEnv.Value, projectRootPath);

            }

            GenerateMasterBffGlobalSettingsFile(builder, globalConfigFullPath, masterBffInfo, masterCompilerSettings);

            using (fileGenerator.Declare("fastBuildProjectName", masterBffFileName))
            using (fileGenerator.Declare("fastBuildGlobalConfigurationInclude", $"#include \"{globalConfigFileName}\""))
            {
                fileGenerator.Write(Bff.Template.ConfigurationFile.HeaderFile);
                foreach (Platform platform in masterBffInfo.Platforms)
                {
                    using (fileGenerator.Declare("fastBuildDefine", Bff.GetPlatformSpecificDefine(platform)))
                        fileGenerator.Write(Bff.Template.ConfigurationFile.Define);
                }
                fileGenerator.Write(Bff.Template.ConfigurationFile.GlobalConfigurationInclude);
            }

            WriteMasterCopySection(fileGenerator, masterBffCopySections);
            WriteMasterCustomSection(fileGenerator, masterBffCustomSections);

            using (fileGenerator.Declare("fastBuildProjectName", masterBffFileName))
            using (fileGenerator.Declare("fastBuildOrderedBffDependencies", fastBuildMasterBffDependencies))
            {
                fileGenerator.Write(Bff.Template.ConfigurationFile.Includes);
            }

            if (masterBffInfo.AllConfigsSections.Count != 0)
            {
                using (fileGenerator.Declare("fastBuildConfigs", UtilityMethods.FBuildFormatList(masterBffInfo.AllConfigsSections, 4)))
                {
                    fileGenerator.Write(Bff.Template.ConfigurationFile.AllConfigsSection);
                }
            }

            // remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();
            MemoryStream bffCleanMemoryStream = fileGenerator.ToMemoryStream();

            // Write master bff file
            FileInfo bffFileInfo = new FileInfo(masterBffFilePath);
            bool updated = builder.Context.WriteGeneratedFile(null, bffFileInfo, bffCleanMemoryStream);

            foreach (var confsPerSolution in configurationsPerBff)
                confsPerSolution.Solution.PostGenerationCallback?.Invoke(masterBffDirectory, Path.GetFileNameWithoutExtension(masterBffFileName), FastBuildSettings.FastBuildConfigFileExtension);

            return updated;
        }

        private static void GenerateMasterBffGlobalSettingsFile(
            Builder builder,
            string masterBffGlobalConfigFile,
            MasterBffInfo masterBffInfo,
            Dictionary<string, CompilerSettings> masterCompilerSettings
        )
        {
            var fileGenerator = new FileGenerator();

            WriteMasterSettingsSection(fileGenerator, masterBffInfo, masterCompilerSettings);
            WriteMasterCompilerSection(fileGenerator, masterBffInfo, masterCompilerSettings);

            // remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();
            MemoryStream bffCleanMemoryStream = fileGenerator.ToMemoryStream();

            // Write master bff global settings file
            FileInfo bffFileInfo = new FileInfo(masterBffGlobalConfigFile);
            if (builder.Context.WriteGeneratedFile(null, bffFileInfo, bffCleanMemoryStream))
            {
                Project.FastBuildGeneratedFileCount++;
                Project.FastBuildMasterGeneratedFiles.Add(masterBffGlobalConfigFile);
            }
            else
            {
                Project.FastBuildUpToDateFileCount++;
            }
        }

        private static void WriteMasterSettingsSection(
            FileGenerator masterBffGenerator, MasterBffInfo masterBffInfo,
            Dictionary<string, CompilerSettings> masterCompilerSettings
        )
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
                    masterCompilerSettings
                        .Where(x => x.Value.DevEnv == masterBffInfo.DevEnv)
                        .Where(x => x.Value.PlatformFlags.HasFlag(platform))
                        .Select(x => x.Value);

                string defaultResourceCompilerPath = Path.GetDirectoryName(masterBffInfo.DevEnv.Value.GetWindowsResourceCompiler(Platform.win64));

                Strings resourceCompilerPaths = new Strings();
                foreach (var setting in platformSettings)
                {
                    foreach (var configurationPair in setting.Configurations)
                    {
                        var configuration = configurationPair.Value;

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
                masterBffGenerator.Write(Bff.Template.ConfigurationFile.HeaderFile);
                masterBffGenerator.Write(Bff.Template.ConfigurationFile.GlobalSettings);
            }
        }

        private static void WriteMasterCompilerSection(
            FileGenerator masterBffGenerator, MasterBffInfo masterBffInfo,
            Dictionary<string, CompilerSettings> masterCompilerSettings
        )
        {
            var sortedMasterCompileSettings =
                masterCompilerSettings
                    .Where(x => x.Value.DevEnv == masterBffInfo.DevEnv)
                    .Where(x => masterBffInfo.Platforms.TestPlatformFlags(x.Value.PlatformFlags))
                    .OrderBy(x => x.Value.CompilerName);

            foreach (var compiler in sortedMasterCompileSettings)
            {
                var compilerSettings = compiler.Value;
                var compilerPlatform = compilerSettings.PlatformFlags;

                string fastBuildVS2012EnumBugWorkaround = FileGeneratorUtilities.RemoveLineTag;
                if (FastBuildSettings.EnableVS2012EnumBugWorkaround &&
                    compilerSettings.DevEnv == DevEnv.vs2012 &&
                    compilerPlatform.HasFlag(Platform.win64))
                {
                    fastBuildVS2012EnumBugWorkaround = ".VS2012EnumBugFix = true";
                }

                using (masterBffGenerator.Declare("fastbuildCompilerName", compiler.Key))
                using (masterBffGenerator.Declare("fastBuildVisualStudioEnvironment", compilerSettings.RootPath))
                using (masterBffGenerator.Declare("fastBuildCompilerExecutable", string.IsNullOrEmpty(compilerSettings.Executable) ? FileGeneratorUtilities.RemoveLineTag : compilerSettings.Executable))
                using (masterBffGenerator.Declare("fastBuildExtraFiles", compilerSettings.ExtraFiles.Count > 0 ? UtilityMethods.FBuildCollectionFormat(compilerSettings.ExtraFiles, 20) : FileGeneratorUtilities.RemoveLineTag))
                using (masterBffGenerator.Declare("fastBuildVS2012EnumBugWorkaround", fastBuildVS2012EnumBugWorkaround))
                {
                    masterBffGenerator.Write(Bff.Template.ConfigurationFile.CompilerSetting);
                    foreach (var compilerConfiguration in compilerSettings.Configurations)
                    {
                        var compConf = compilerConfiguration.Value;

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
                            masterBffGenerator.Write(Bff.Template.ConfigurationFile.CompilerConfiguration);
                        }
                    }
                }
            }
        }

        private static void WriteMasterCopySection(IFileGenerator masterBffGenerator, List<string> sections)
        {
            sections.Sort((x, y) => string.Compare(x, y, StringComparison.Ordinal));

            foreach (var copySection in sections)
                masterBffGenerator.Write(new StringReader(copySection).ReadToEnd());
        }

        private static void WriteMasterCustomSection(IFileGenerator masterBffGenerator, UniqueList<string> masterBffCustomSections)
        {
            if (masterBffCustomSections.Count != 0)
                masterBffGenerator.Write(Bff.Template.ConfigurationFile.CustomSectionHeader);
            foreach (var customSection in masterBffCustomSections)
                masterBffGenerator.Write(new StringReader(customSection).ReadToEnd());
        }

        private static void Builder_Generated(object sender, GenerationEventArgs e)
        {
            Builder builder = (Builder)sender;
            GenerateMasterBffFiles(builder, s_confsPerSolutions.Values);
        }
    }
}
