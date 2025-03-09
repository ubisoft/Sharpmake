// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
            public Dictionary<string, Strings> BffIncludeToDependencyIncludes = new Dictionary<string, Strings>();
            public readonly Dictionary<string, CompilerSettings> CompilerSettings = new Dictionary<string, CompilerSettings>();
            public readonly Strings AllConfigsSections = new Strings(); // All Configs section when running with a source file filter
            public readonly HashSet<string> WrittenAdditionalPropertyGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                    if (conf.IsFastBuildEnabled() && !(conf.ProjectsWereFiltered && conf.ResolvedProjects.Count == 0))
                        yield return conf;
                }
            }

            public Solution Solution { get; }
            public string BffFilePath { get; }
            public string BffFilePathWithExtension => BffFilePath + FastBuildSettings.FastBuildConfigFileExtension;
            public Solution.Configuration[] Configurations { get; private set; }
            public List<Solution.ResolvedProject> ResolvedProjects { get; }
            public bool ProjectsWereFiltered { get; private set; }

            public void Merge(ConfigurationsPerBff other)
            {
                Debug.Assert(other.BffFilePath == BffFilePath);

                ProjectsWereFiltered = ProjectsWereFiltered && other.ProjectsWereFiltered;

                var merged = new HashSet<Solution.Configuration>(Configurations);
                foreach (Solution.Configuration conf in other)
                    merged.Add(conf);

                ResolvedProjects.AddRange(other.ResolvedProjects);
                Configurations = merged.ToArray();
            }

            public void Sort()
            {
                Configurations = Configurations.OrderBy(c => c.PlatformName).ThenBy(c => c.SolutionFileName).ThenBy(c => c.Name).ToArray();
                ResolvedProjects.Sort((p0, p1) =>
                {
                    int projectNameComparison = p0.ProjectName.CompareTo(p1.ProjectName);
                    if (projectNameComparison != 0)
                        return projectNameComparison;
                    return p0.TargetDefault.CompareTo(p1.TargetDefault);
                });
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

                bool projectsWereFiltered;
                ResolvedProjects = solution.GetResolvedProjects(this, out projectsWereFiltered).ToList();
                ProjectsWereFiltered = projectsWereFiltered;
            }

            private bool IsFastBuildEnabled()
            {
                foreach (var solutionConfiguration in this)
                {
                    foreach (var solutionProject in ResolvedProjects)
                    {
                        var project = solutionProject.Project;

                        // Export projects do not have any bff
                        if (project.SharpmakeProjectType == Project.ProjectTypeAttribute.Export)
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

            IEnumerable<ConfigurationsPerBff> confsPerBffs = ConfigurationsPerBff.Create(solution, solutionConfigurations);
            foreach (ConfigurationsPerBff confsPerBff in confsPerBffs)
            {
                if (confsPerBff.Configurations.Any(conf => conf.SolutionFilePath != conf.MasterBffFilePath))
                {
                    string bffIncludeFilePath = solutionFile + FastBuildSettings.FastBuildConfigFileExtension;
                    if (!Util.PathIsSame(bffIncludeFilePath, confsPerBff.BffFilePathWithExtension))
                        GenerateIncludeBffFileForSolution(builder, bffIncludeFilePath, confsPerBff, generatedFiles, skipFiles);

                    // First collect all solutions and sort them by master BFF, then once we have all of
                    // them, the post-generation event handler will actually generate the BFF.
                    lock (s_confsPerSolutions)
                    {
                        if (!s_postGenerationHandlerInitialized)
                        {
                            builder.EventPostGeneration += (projects, solutions) =>
                            {
                                GenerateMasterBffFiles(builder, s_confsPerSolutions.Values);
                            };
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

        private void GenerateIncludeBffFileForSolution(Builder builder, string bffFilePath, ConfigurationsPerBff confsPerBff, IList<string> generatedFiles, IList<string> skippedFiles)
        {
            var bffFileInfo = new FileInfo(bffFilePath);

            var fileGenerator = new FileGenerator();
            using (fileGenerator.Declare("solutionFileName", Path.GetFileNameWithoutExtension(bffFileInfo.Name)))
            using (fileGenerator.Declare("masterBffFilePath", Util.PathGetRelative(bffFileInfo.DirectoryName, confsPerBff.BffFilePathWithExtension)))
                fileGenerator.Write(Bff.Template.ConfigurationFile.IncludeMasterBff);

            if (builder.Context.WriteGeneratedFile(null, bffFileInfo, fileGenerator))
                generatedFiles.Add(bffFilePath);
            else
                skippedFiles.Add(bffFilePath);
        }

        private static void GenerateMasterBffFiles(Builder builder, IEnumerable<ConfigurationsPerBff> confsPerSolutions)
        {
            foreach (var confsPerBff in confsPerSolutions)
            {
                string bffFilePath = confsPerBff.BffFilePath;
                string bffFilePathWithExtension = Util.PathMakeStandard(bffFilePath + FastBuildSettings.FastBuildConfigFileExtension);
                if (GenerateMasterBffFile(builder, confsPerBff))
                {
                    Project.AddFastbuildMasterGeneratedFile(bffFilePathWithExtension);
                }
                else
                {
                    Project.IncrementFastBuildUpToDateFileCount();
                }
            }
        }

        private static void RegisterBuiltOutputsForConf(
            Project.Configuration conf,
            string fastBuildTargetIdentifier,
            Dictionary<string, (string sourceBff, string sourceNodeIdentifier)> outputsByBffAndNode
        )
        {
            string bffFullPath = Util.GetCapitalizedPath(conf.BffFullFileName) + FastBuildSettings.FastBuildConfigFileExtension;

            void registerOutputAndCheck(string outputFile)
            {
                if (!outputsByBffAndNode.TryGetValue(outputFile, out var pair))
                {
                    outputsByBffAndNode.Add(outputFile, (bffFullPath, fastBuildTargetIdentifier));
                }
                else if (FileSystemStringComparer.StaticCompare(pair.sourceBff, bffFullPath) != 0 || pair.sourceNodeIdentifier != fastBuildTargetIdentifier)
                {
                    throw new Error($"Found identical output '{outputFile}' from multiple sources!");
                }
            }


            registerOutputAndCheck(conf.LinkerPdbFilePath);
            registerOutputAndCheck(Path.Combine(conf.TargetPath, conf.TargetFileFullNameWithExtension));
        }

        private static bool GenerateMasterBffFile(Builder builder, ConfigurationsPerBff configurationsPerBff)
        {
            configurationsPerBff.Sort();

            string masterBffFilePath = Util.GetCapitalizedPath(configurationsPerBff.BffFilePathWithExtension);
            string masterBffDirectory = Path.GetDirectoryName(masterBffFilePath);
            string masterBffFileName = Path.GetFileName(masterBffFilePath);

            // Global configuration file is in the same directory as the master bff but filename suffix added to its filename.
            string globalConfigFullPath = GetGlobalBffConfigFileName(masterBffFilePath);
            string globalConfigFileName = Path.GetFileName(globalConfigFullPath);

            var solutionProjects = configurationsPerBff.ResolvedProjects;
            if (solutionProjects.Count == 0 && configurationsPerBff.ProjectsWereFiltered)
            {
                // We are running in filter mode for submit assistant and all projects were filtered out. 
                // We need to skip generation and delete any existing master bff file.
                Util.TryDeleteFile(masterBffFilePath);
                return false;
            }

            // Start writing Bff
            var fileGenerator = new FileGenerator();

            var masterBffInfo = new MasterBffInfo();

            var bffPreBuildSection = new Dictionary<string, string>();
            var bffCustomPreBuildSection = new Dictionary<string, string>();
            var bffCopyNodes = new Dictionary<string, (string sourceFullPath, string src, string dest)>();
            var masterBffCopySections = new List<string>();
            var masterBffCustomSections = new UniqueList<string>(); // section that is not ordered

            bool mustGenerateFastbuild = false;

            var platformBffCache = new Dictionary<Platform, IPlatformBff>();

            var verificationPostBuildCopies = new Dictionary<string, string>();
            var outputsByBffAndNode = new Dictionary<string, (string sourceBff, string sourceNodeIdentifier)>(FileSystemStringComparer.Default);
            foreach (Solution.Configuration solutionConfiguration in configurationsPerBff)
            {
                foreach (var solutionProject in solutionProjects)
                {
                    var project = solutionProject.Project;

                    // Export projects do not have any bff
                    if (project.SharpmakeProjectType == Project.ProjectTypeAttribute.Export)
                        continue;

                    // When the project has a source file filter, only keep it if the file list is not empty
                    if (project.SourceFilesFilters != null && (project.SourceFilesFiltersCount == 0 || project.SkipProjectWhenFiltersActive))
                        continue;

                    Solution.Configuration.IncludedProjectInfo includedProject = solutionConfiguration.GetProject(project.GetType());
                    bool perfectMatch = includedProject != null && solutionProject.Configurations.Contains(includedProject.Configuration);
                    if (!perfectMatch)
                        continue;

                    var conf = includedProject.Configuration;
                    if (!conf.IsFastBuildEnabledProjectConfig())
                        continue;

                    mustGenerateFastbuild = true;

                    var otherConfigurationsInSameBff = project.Configurations.Where(c => conf.BffFullFileName == c.BffFullFileName);
                    foreach (var c in otherConfigurationsInSameBff)
                    {
                        IPlatformBff platformBff = platformBffCache.GetValueOrAdd(c.Platform, PlatformRegistry.Query<IPlatformBff>(c.Platform));
                        platformBff.AddCompilerSettings(masterBffInfo.CompilerSettings, c);
                    }

                    string fastBuildTargetIdentifier = Bff.GetShortProjectName(project, conf);

                    if (FastBuildSettings.WriteAllConfigsSection && includedProject.ToBuild == Solution.Configuration.IncludedProjectInfo.Build.Yes)
                        masterBffInfo.AllConfigsSections.Add(fastBuildTargetIdentifier);

                    bool isOutputTypeExe = conf.Output == Project.Configuration.OutputType.Exe;
                    bool isOutputTypeDll = conf.Output == Project.Configuration.OutputType.Dll;
                    bool isOutputTypeLib = conf.Output == Project.Configuration.OutputType.Lib;
                    bool isOutputTypeExeOrDll = isOutputTypeExe || isOutputTypeDll;

                    using (fileGenerator.Declare("conf", conf))
                    using (fileGenerator.Declare("target", conf.Target))
                    using (fileGenerator.Declare("project", conf.Project))
                    {
                        var preBuildEvents = new Dictionary<string, Project.Configuration.BuildStepBase>();
                        if (isOutputTypeExeOrDll || conf.ExecuteTargetCopy)
                        {
                            RegisterBuiltOutputsForConf(conf, fastBuildTargetIdentifier, outputsByBffAndNode);

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
                                string currentSourceFullPath = Util.PathGetAbsolute(masterBffDirectory, sourceFile);

                                if (FastBuildSettings.FastBuildValidateCopyFiles)
                                {
                                    string key = sourceFileName + destinationRelativeToGlobal;
                                    if (verificationPostBuildCopies.TryGetValue(key, out var previous))
                                    {
                                        if (FileSystemStringComparer.StaticCompare(previous, currentSourceFullPath) != 0)
                                            builder.LogErrorLine("A post-build copy to the destination '{0}' already exist but from different sources: '{1}' and '{2}'!", Util.PathGetAbsolute(masterBffDirectory, destinationFolder), previous, currentSourceFullPath);
                                    }
                                    else
                                    {
                                        verificationPostBuildCopies.Add(key, currentSourceFullPath);
                                    }
                                }

                                if (!bffCopyNodes.ContainsKey(fastBuildCopyAlias))
                                    bffCopyNodes.Add(fastBuildCopyAlias, (currentSourceFullPath, Bff.CurrentBffPathKeyCombine(sourceFile), Bff.CurrentBffPathKeyCombine(destinationFile)));
                            }
                        }

                        foreach (var eventPair in conf.EventPreBuildExecute)
                        {
                            preBuildEvents.Add(eventPair.Key, eventPair.Value);
                        }

                        string projectPath = new FileInfo(solutionProject.ProjectFile).Directory.FullName;

                        foreach (var buildEvent in conf.ResolvedEventPreBuildExe)
                        {
                            string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, buildEvent, Vcxproj.BuildStep.PreBuild, project.RootPath, projectPath);
                            preBuildEvents.Add(eventKey, buildEvent);
                        }

                        WriteEvents(fileGenerator.Resolver, preBuildEvents, bffPreBuildSection, conf.Project.RootPath, masterBffDirectory);

                        var customPreBuildEvents = new Dictionary<string, Project.Configuration.BuildStepBase>();
                        foreach (var eventPair in conf.EventCustomPrebuildExecute)
                            customPreBuildEvents.Add(eventPair.Key, eventPair.Value);

                        foreach (var buildEvent in conf.ResolvedEventCustomPreBuildExe)
                        {
                            string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, buildEvent, Vcxproj.BuildStep.PreBuildCustomAction, project.RootPath, projectPath);
                            customPreBuildEvents.Add(eventKey, buildEvent);
                        }

                        WriteEvents(fileGenerator.Resolver, customPreBuildEvents, bffCustomPreBuildSection, conf.Project.RootPath, masterBffDirectory);

                        if (includedProject.ToBuild == Solution.Configuration.IncludedProjectInfo.Build.Yes)
                            MergeBffIncludeTreeRecursive(conf, ref masterBffInfo.BffIncludeToDependencyIncludes);
                    }
                }
            }

            if (!mustGenerateFastbuild)
                throw new Error("Sharpmake-FastBuild : Trying to generate a MasterBff with none of its projects having a FastBuild configuration, or having a platform supporting it, or all of them having conf.DoNotGenerateFastBuild = true");

            var afterBffCopies = new Dictionary<string, List<string>>();
            foreach (var copyNode in bffCopyNodes)
            {
                bool foundTargetInBff = outputsByBffAndNode.TryGetValue(copyNode.Value.sourceFullPath, out var bffAndNode);
                using (fileGenerator.Declare("fastBuildCopyAlias", copyNode.Key))
                using (fileGenerator.Declare("fastBuildCopySource", copyNode.Value.src))
                using (fileGenerator.Declare("fastBuildCopyDest", copyNode.Value.dest))
                using (fileGenerator.Declare("fastBuildCopyDependencies", foundTargetInBff ? $"'{bffAndNode.sourceNodeIdentifier}'" : FileGeneratorUtilities.RemoveLineTag))
                {
                    string nodeContent = fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.CopyFileSection);

                    if (!foundTargetInBff)
                    {
                        masterBffCopySections.Add(nodeContent);
                    }
                    else
                    {
                        if (!afterBffCopies.ContainsKey(bffAndNode.sourceBff))
                            afterBffCopies.Add(bffAndNode.sourceBff, new List<string> { nodeContent });
                        else
                            afterBffCopies[bffAndNode.sourceBff].Add(nodeContent);
                    }
                }
            }

            masterBffCopySections.AddRange(bffPreBuildSection.Values);

            masterBffCustomSections.AddRange(bffCustomPreBuildSection.Values);

            GenerateMasterBffGlobalSettingsFile(builder, globalConfigFullPath, masterBffInfo);

            using (fileGenerator.Declare("fastBuildProjectName", masterBffFileName))
            using (fileGenerator.Declare("fastBuildGlobalConfigurationInclude", $"#include \"{globalConfigFileName}\""))
            {
                fileGenerator.Write(Bff.Template.ConfigurationFile.HeaderFile);
                foreach (Platform platform in platformBffCache.Keys) // kind of cheating to use that cache instead of the masterBffInfo.CompilerSettings, but it works :)
                {
                    using (fileGenerator.Declare("fastBuildDefine", Bff.GetPlatformSpecificDefine(platform)))
                        fileGenerator.Write(Bff.Template.ConfigurationFile.Define);
                }
                fileGenerator.Write(Bff.Template.ConfigurationFile.GlobalConfigurationInclude);
            }

            WriteMasterCopySection(fileGenerator, masterBffCopySections);
            WriteMasterCustomSection(fileGenerator, masterBffCustomSections);

            var result = new StringBuilder();
            foreach (var projectBffFullPath in GetMasterIncludeList(masterBffInfo.BffIncludeToDependencyIncludes))
            {
                string projectFullPath = Path.GetDirectoryName(projectBffFullPath);
                var projectPathRelativeFromMasterBff = Util.PathGetRelative(masterBffDirectory, projectFullPath, true);

                string bffKeyRelative = Path.Combine(projectPathRelativeFromMasterBff, Path.GetFileName(projectBffFullPath));

                result.AppendLine($"#include \"{bffKeyRelative}\"");

                if (afterBffCopies.TryGetValue(projectBffFullPath, out List<string> copyNodes))
                {
                    foreach (var copyNode in copyNodes)
                        result.Append(copyNode);
                    afterBffCopies.Remove(projectBffFullPath); // not necessary but just to verify that we wrote all we wanted
                }
            }

            if (afterBffCopies.Count > 0)
                throw new Error("The target source of some postbuild copies was not included in the master bff!");

            string fastBuildMasterBffDependencies = result.Length == 0 ? FileGeneratorUtilities.RemoveLineTag : result.ToString();

            using (fileGenerator.Declare("fastBuildProjectName", masterBffFileName))
            using (fileGenerator.Declare("fastBuildOrderedBffDependencies", fastBuildMasterBffDependencies))
            {
                fileGenerator.Write(Bff.Template.ConfigurationFile.Includes);
            }

            if (masterBffInfo.AllConfigsSections.Count != 0)
            {
                using (fileGenerator.Declare("fastBuildConfigs", UtilityMethods.FBuildFormatList(masterBffInfo.AllConfigsSections.SortedValues, 4)))
                {
                    fileGenerator.Write(Bff.Template.ConfigurationFile.AllConfigsSection);
                }
            }

            // remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();

            // Write master .bff file
            FileInfo bffFileInfo = new FileInfo(masterBffFilePath);
            bool updated = builder.Context.WriteGeneratedFile(null, bffFileInfo, fileGenerator);

            foreach (var confsPerSolution in configurationsPerBff)
                confsPerSolution.Solution.PostGenerationCallback?.Invoke(masterBffDirectory, Path.GetFileNameWithoutExtension(masterBffFileName), FastBuildSettings.FastBuildConfigFileExtension);

            return updated;
        }

        private static void WriteEvents(
            Resolver resolver,
            Dictionary<string, Project.Configuration.BuildStepBase> buildEvents,
            Dictionary<string, string> bffSection,
            string projectRoot,
            string relativeTo
        )
        {
            List<Bff.BffNodeBase> bffNodes = UtilityMethods.GetBffNodesFromBuildSteps(buildEvents, new Strings());

            foreach (var bffNode in bffNodes)
            {
                string nodeIdentifier = resolver.Resolve(bffNode.Identifier);

                if (bffSection.ContainsKey(nodeIdentifier))
                    continue;

                bffSection.Add(nodeIdentifier, bffNode.Resolve(projectRoot, relativeTo, resolver));
            }
        }

        private static void GenerateMasterBffGlobalSettingsFile(
            Builder builder,
            string masterBffGlobalConfigFile,
            MasterBffInfo masterBffInfo
        )
        {
            var fileGenerator = new FileGenerator();

            WriteMasterSettingsSection(fileGenerator, masterBffInfo);
            WriteMasterCompilerSection(fileGenerator, masterBffInfo);

            // remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();

            // Write master bff global settings file
            FileInfo bffFileInfo = new FileInfo(masterBffGlobalConfigFile);
            if (builder.Context.WriteGeneratedFile(null, bffFileInfo, fileGenerator))
            {
                Project.AddFastbuildMasterGeneratedFile(masterBffGlobalConfigFile);
            }
            else
            {
                Project.IncrementFastBuildUpToDateFileCount();
            }
        }

        private static void WriteMasterSettingsSection(FileGenerator masterBffGenerator, MasterBffInfo masterBffInfo)
        {
            string cachePath = FileGeneratorUtilities.RemoveLineTag;
            string cachePluginDLL = FileGeneratorUtilities.RemoveLineTag;
            string workerConnectionLimit = FileGeneratorUtilities.RemoveLineTag;
            if (FastBuildSettings.CachePath != null)
            {
                cachePath = ".CachePath = '" + FastBuildSettings.CachePath + "'";

                if (FastBuildSettings.CachePluginDLLFilePath != null)
                    cachePluginDLL = ".CachePluginDLL = '" + FastBuildSettings.CachePluginDLLFilePath + "'";
            }
            if (FastBuildSettings.FastBuildWorkerConnectionLimit >= 0)
            {
                workerConnectionLimit = ".WorkerConnectionLimit = " + FastBuildSettings.FastBuildWorkerConnectionLimit.ToString();
            }

            string additionalGlobalSettings = FileGeneratorUtilities.RemoveLineTag;
            if (FastBuildSettings.AdditionalGlobalSettings.Any())
            {
                additionalGlobalSettings = string.Join(Environment.NewLine, FastBuildSettings.AdditionalGlobalSettings.Select(setting => "    " + setting));
            }

            string fastBuildPATH = FileGeneratorUtilities.RemoveLineTag;
            if (FastBuildSettings.SetPathToResourceCompilerInEnvironment)
            {
                // !FIX FOR LINK : fatal error LNK1158: cannot run rc.exe!
                //
                // link.exe on win64 executes rc.exe by itself on some occasions
                // if it doesn't find it, link errors can occur
                //
                // link.exe will first search rc.exe next to it, and if it fails
                // it will look for it in the folders listed by the PATH
                // environment variable, so we'll try to replicate that process
                // in sharpmake:
                //
                //       1) Get the linker path
                //       2) Look for rc.exe near it
                //       3) If found, exit
                //       4) If not, add a PATH environment variable pointing to the rc.exe folder

                List<Platform> microsoftPlatforms = PlatformRegistry.GetAvailablePlatforms<IMicrosoftPlatformBff>().ToList();
                var resourceCompilerPaths = new Strings();
                foreach (CompilerSettings setting in masterBffInfo.CompilerSettings.Values)
                {
                    if (!microsoftPlatforms.Any(x => setting.PlatformFlags.HasFlag(x)))
                        continue;

                    string defaultResourceCompilerPath = Path.GetDirectoryName(setting.DevEnv.GetWindowsResourceCompiler(Platform.win64));
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

            var allDevEnv = masterBffInfo.CompilerSettings.Values.Select(s => s.DevEnv).Distinct().ToList();

            string envRemoveGuards = FileGeneratorUtilities.RemoveLineTag;
            string fastBuildEnvironments = string.Empty;

            switch (Util.GetExecutingPlatform())
            {
                case Platform.win64:
                    fastBuildEnvironments += Bff.Template.ConfigurationFile.WinEnvironment;
                    break;
                case Platform.mac:
                    fastBuildEnvironments += Bff.Template.ConfigurationFile.OsxEnvironment;
                    break;
                case Platform.linux:
                    fastBuildEnvironments += Bff.Template.ConfigurationFile.LinuxEnvironment;
                    break;
                default:
                    throw new NotImplementedException($"Environment variables bff config not implemented for platform {Util.GetExecutingPlatform()}");
            }

            string envAdditionalVariables = FileGeneratorUtilities.RemoveLineTag;
            if (FastBuildSettings.AdditionalGlobalEnvironmentVariables.Any())
            {
                envAdditionalVariables = string.Join(Environment.NewLine, FastBuildSettings.AdditionalGlobalEnvironmentVariables.Select(keyValue => $"        \"{keyValue.Key}={keyValue.Value}\""));
            }

            using (masterBffGenerator.Declare("fastBuildProjectName", "Master"))
            {
                masterBffGenerator.Write(Bff.Template.ConfigurationFile.HeaderFile);
            }

            string concurrencyGroupList = FileGeneratorUtilities.RemoveLineTag;
            if (FastBuildSettings.ConcurrencyGroups.Count > 0)
            {
                masterBffGenerator.WriteLine("//------------------------------");
                masterBffGenerator.WriteLine("// Concurrency groups definition");
                masterBffGenerator.WriteLine("//------------------------------");
                List<string> groupSectionList = new List<string>();

                foreach (var group in FastBuildSettings.ConcurrencyGroups)
                {
                    string groupSectionName = $".ConcurrencyGroup{group.Key}";
                    groupSectionList.Add(groupSectionName); 

                    using (masterBffGenerator.Declare("fastBuildConcurrencyGroupName", group.Key))
                    using (masterBffGenerator.Declare("fastBuildConcurrencyGroupSectionName", groupSectionName))
                    using (masterBffGenerator.Declare("fastBuildConcurrencyLimit", group.Value.ConcurrencyLimit.HasValue ? group.Value.ConcurrencyLimit.ToString() : FileGeneratorUtilities.RemoveLineTag))
                    using (masterBffGenerator.Declare("fastBuildConcurrencyPerJobMiB", group.Value.ConcurrencyPerJobMiB.HasValue ? group.Value.ConcurrencyPerJobMiB : FileGeneratorUtilities.RemoveLineTag))
                    {
                        masterBffGenerator.Write(Bff.Template.ConfigurationFile.ConcurrencyGroup);
                    }
                }
                concurrencyGroupList = UtilityMethods.FBuildFormatList(groupSectionList, 4, UtilityMethods.FBuildFormatListOptions.UseCommaBetweenElements);
            }

            using (masterBffGenerator.Declare("CachePath", cachePath))
            using (masterBffGenerator.Declare("CachePluginDLL", cachePluginDLL))
            using (masterBffGenerator.Declare("WorkerConnectionLimit", workerConnectionLimit))
            using (masterBffGenerator.Declare("fastBuildSystemRoot", FastBuildSettings.SystemRoot))
            using (masterBffGenerator.Declare("fastBuildPATH", fastBuildPATH))
            using (masterBffGenerator.Declare("fastBuildAllowDBMigration", FastBuildSettings.FastBuildAllowDBMigration ? "true" : FileGeneratorUtilities.RemoveLineTag))
            using (masterBffGenerator.Declare("AdditionalGlobalSettings", additionalGlobalSettings))
            using (masterBffGenerator.Declare("fastBuildEnvironments", fastBuildEnvironments))
            using (masterBffGenerator.Declare("envRemoveGuards", envRemoveGuards))
            using (masterBffGenerator.Declare("envAdditionalVariables", envAdditionalVariables))
            using (masterBffGenerator.Declare("fastbuildConcurrencyGroupList", concurrencyGroupList))
            {
                masterBffGenerator.Write(Bff.Template.ConfigurationFile.GlobalSettings);
            }
        }

        private static void WriteMasterCompilerSection(FileGenerator masterBffGenerator, MasterBffInfo masterBffInfo)
        {
            var sortedMasterCompileSettings = masterBffInfo.CompilerSettings.OrderBy(x => x.Value.CompilerName);
            foreach (var compiler in sortedMasterCompileSettings)
            {
                var compilerSettings = compiler.Value;
                var compilerPlatform = compilerSettings.PlatformFlags;
                string fastBuildCompilerFamily = UtilityMethods.GetFBuildCompilerFamily(compilerSettings.FastBuildCompilerFamily);

                string fastBuildCompilerUseRelativePaths = FileGeneratorUtilities.RemoveLineTag;
                if (FastBuildSettings.CompilersUsingRelativePaths.Contains(compiler.Key))
                {
                    fastBuildCompilerUseRelativePaths = "true";
                }

                string fastBuildCompilerAdditionalSettings = FileGeneratorUtilities.RemoveLineTag;
                if (FastBuildSettings.AdditionalCompilerSettings.TryGetValue(compiler.Key, out List<string> extraOptions) &&
                    extraOptions.Any())
                {
                    fastBuildCompilerAdditionalSettings = string.Join(Environment.NewLine, extraOptions.Select(option => "    " + option));
                }

                // Check if we got a dependent custom property group referenced by the compiler section
                if (FastBuildSettings.AdditionalCompilerPropertyGroups.TryGetValue(compiler.Key, out string extraCompilerPropertyGroupName))
                {
                    if (FastBuildSettings.AdditionalPropertyGroups.TryGetValue(extraCompilerPropertyGroupName, out List<string> extraPropertySection) &&
                        extraPropertySection.Any())
                    {
                        // Only write each section once.
                        if (masterBffInfo.WrittenAdditionalPropertyGroups.Add(extraCompilerPropertyGroupName))
                        {
                            string section = UtilityMethods.FBuildFormatList(extraPropertySection, 0, UtilityMethods.FBuildFormatListOptions.None);
                            masterBffGenerator.Write(Environment.NewLine);
                            masterBffGenerator.Write(extraCompilerPropertyGroupName);
                            masterBffGenerator.Write(Environment.NewLine);
                            masterBffGenerator.Write(section);
                            masterBffGenerator.Write(Environment.NewLine);
                        }
                    }
                    else
                    {
                        Builder.Instance.LogErrorLine("Additional property group '{0}' is not registered or empty", extraCompilerPropertyGroupName);
                    }
                }

                using (masterBffGenerator.Declare("fastbuildCompilerName", compiler.Key))
                using (masterBffGenerator.Declare("fastBuildCompilerRootPath", compilerSettings.RootPath))
                using (masterBffGenerator.Declare("fastBuildCompilerExecutable", string.IsNullOrEmpty(compilerSettings.Executable) ? FileGeneratorUtilities.RemoveLineTag : compilerSettings.Executable))
                using (masterBffGenerator.Declare("fastBuildExtraFiles", compilerSettings.ExtraFiles.Count > 0 ? UtilityMethods.FBuildCollectionFormat(compilerSettings.ExtraFiles, 28) : FileGeneratorUtilities.RemoveLineTag))
                using (masterBffGenerator.Declare("fastBuildCompilerFamily", string.IsNullOrEmpty(fastBuildCompilerFamily) ? FileGeneratorUtilities.RemoveLineTag : fastBuildCompilerFamily))
                using (masterBffGenerator.Declare("fastBuildCompilerUseRelativePaths", fastBuildCompilerUseRelativePaths))
                using (masterBffGenerator.Declare("fastBuildCompilerAdditionalSettings", fastBuildCompilerAdditionalSettings))
                {
                    masterBffGenerator.Write(Bff.Template.ConfigurationFile.CompilerSetting);
                    foreach (var compilerConfiguration in compilerSettings.Configurations.OrderBy(x => x.Key))
                    {
                        var compConf = compilerConfiguration.Value;
                        string fastBuildLinkerType = UtilityMethods.GetFBuildLinkerType(compConf.FastBuildLinkerType);

                        using (masterBffGenerator.Declare("fastBuildConfigurationName", compilerConfiguration.Key))
                        using (masterBffGenerator.Declare("fastBuildBinPath", compConf.BinPath))
                        using (masterBffGenerator.Declare("fastBuildLinkerPath", compConf.LinkerPath))
                        using (masterBffGenerator.Declare("fastBuildResourceCompiler", compConf.ResourceCompiler))
                        using (masterBffGenerator.Declare("fastBuildResourceCompilerName", compConf.ResourceCompiler != FileGeneratorUtilities.RemoveLineTag ? "RC" + compilerConfiguration.Key : FileGeneratorUtilities.RemoveLineTag))
                        using (masterBffGenerator.Declare("fastBuildMasmCompiler", compConf.Masm))
                        using (masterBffGenerator.Declare("fastBuildMasmCompilerName", "ML" + compilerConfiguration.Key))

                        // TODOANT make sure we have nasm compiler found and used.
                        using (masterBffGenerator.Declare("fastBuildNasmCompiler", compConf.Nasm))
                        using (masterBffGenerator.Declare("fastBuildNasmCompilerName", "Nasm" + compilerConfiguration.Key))

                        using (masterBffGenerator.Declare("fastBuildCompilerName", compConf.Compiler != FileGeneratorUtilities.RemoveLineTag ? compConf.Compiler : compiler.Key))
                        using (masterBffGenerator.Declare("fastBuildLibrarian", compConf.Librarian))
                        using (masterBffGenerator.Declare("fastBuildLinker", compConf.Linker))
                        using (masterBffGenerator.Declare("fastBuildLinkerType", string.IsNullOrEmpty(fastBuildLinkerType) ? FileGeneratorUtilities.RemoveLineTag : fastBuildLinkerType))
                        using (masterBffGenerator.Declare("fastBuildPlatformLibPaths", string.IsNullOrWhiteSpace(compConf.PlatformLibPaths) ? FileGeneratorUtilities.RemoveLineTag : compConf.PlatformLibPaths))
                        using (masterBffGenerator.Declare("fastBuildExecutable", compConf.Executable))
                        using (masterBffGenerator.Declare("fastBuildUsing", compConf.UsingOtherConfiguration))
                        {
                            if (compConf.ResourceCompiler != FileGeneratorUtilities.RemoveLineTag)
                                masterBffGenerator.Write(Bff.Template.ConfigurationFile.ResourceCompilerSettings);

                            if (!string.IsNullOrEmpty(compConf.Masm))
                                masterBffGenerator.Write(Bff.Template.ConfigurationFile.MasmCompilerSettings);

                            // TODOANT
                            if (!string.IsNullOrEmpty(compConf.Nasm))
                                masterBffGenerator.Write(Bff.Template.ConfigurationFile.NasmCompilerSettings);

                            masterBffGenerator.Write(Bff.Template.ConfigurationFile.CompilerConfiguration);
                        }
                    }
                }
            }
        }

        private static void WriteMasterCopySection(IFileGenerator masterBffGenerator, List<string> sections)
        {
            sections.Sort((x, y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase));

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

        private static void MergeBffIncludeTreeRecursive(Project.Configuration conf, ref Dictionary<string, Strings> bffIncludesDependencies)
        {
            string currentBffFullPath = Util.GetCapitalizedPath(conf.BffFullFileName) + FastBuildSettings.FastBuildConfigFileExtension;
            Strings currentBffIncludes = bffIncludesDependencies.GetValueOrAdd(currentBffFullPath, new Strings());
            MergeBffIncludeTreeRecursive(conf, ref bffIncludesDependencies, new HashSet<Project.Configuration>());
        }

        private static void MergeBffIncludeTreeRecursive(
            Project.Configuration conf,
            ref Dictionary<string, Strings> bffIncludesDependencies,
            HashSet<Project.Configuration> visitedConfigurations)
        {
            string currentBffFullPath = Util.GetCapitalizedPath(conf.BffFullFileName) + FastBuildSettings.FastBuildConfigFileExtension;
            foreach (Project.Configuration dependency in conf.ResolvedDependencies)
            {
                if (dependency.Project.SharpmakeProjectType == Project.ProjectTypeAttribute.Export)
                    continue;

                if (!visitedConfigurations.Contains(dependency))
                    MergeBffIncludeTreeRecursive(dependency, ref bffIncludesDependencies, visitedConfigurations);

                if (!dependency.IsFastBuild)
                    continue;

                if (dependency.Project.SourceFilesFilters != null && (dependency.Project.SourceFilesFiltersCount == 0 || dependency.Project.SkipProjectWhenFiltersActive))
                    continue;

                if (conf.Project.SourceFilesFilters != null && (conf.Project.SourceFilesFiltersCount == 0 || conf.Project.SkipProjectWhenFiltersActive))
                    continue; // Only keep used projects in filter mode. TODO: Make this cleaner.

                string dependencyBffFullPath = Util.GetCapitalizedPath(dependency.BffFullFileName) + FastBuildSettings.FastBuildConfigFileExtension;
                Strings currentBffIncludes = bffIncludesDependencies.GetValueOrAdd(currentBffFullPath, new Strings());
                currentBffIncludes.Add(dependencyBffFullPath);
            }

            visitedConfigurations.Add(conf);
        }

        private static Strings GetMasterIncludeList(Dictionary<string, Strings> bffIncludesDependencies)
        {
            var resolved = new Strings();
            if (bffIncludesDependencies.Count > 0)
            {
                var unresolved = new Strings();
                foreach (var bffTuple in bffIncludesDependencies)
                    VisitBffIncludes(bffIncludesDependencies, bffTuple.Key, resolved, unresolved);
            }
            return resolved;
        }

        private static void VisitBffIncludes(
            Dictionary<string, Strings> bffIncludesDependencies,
            string bffToParse,
            Strings resolved,
            Strings unresolved
        )
        {
            unresolved.Add(bffToParse);

            Strings includes;
            if (bffIncludesDependencies.TryGetValue(bffToParse, out includes))
            {
                foreach (var dependency in includes)
                {
                    if (resolved.Contains(dependency))
                        continue;

                    if (unresolved.Contains(dependency))
                        throw new Error("Circular dependency detected!");

                    VisitBffIncludes(bffIncludesDependencies, dependency, resolved, unresolved);
                }
            }

            resolved.Add(bffToParse);
            unresolved.Remove(bffToParse);
        }
    }
}
