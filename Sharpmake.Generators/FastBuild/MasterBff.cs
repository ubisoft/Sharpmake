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

            IEnumerable<ConfigurationsPerBff> confsPerBffs = ConfigurationsPerBff.Create(solution, solutionConfigurations).ToArray();
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

            using (var bffFileStream = fileGenerator.ToMemoryStream())
            {
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
                    Project.AddFastbuildMasterGeneratedFile(bffFilePathWithExtension);
                }
                else
                {
                    Project.IncrementFastBuildUpToDateFileCount();
                }
            }
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
            var bffMasterSection = new Dictionary<string, string>();
            var masterBffCopySections = new List<string>();
            var masterBffCustomSections = new UniqueList<string>(); // section that is not ordered

            bool mustGenerateFastbuild = false;

            var platformBffCache = new Dictionary<Platform, IPlatformBff>();

            var verificationPostBuildCopies = new Dictionary<string, string>();
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

                    Solution.Configuration.IncludedProjectInfo includedProject = solutionConfiguration.GetProject(solutionProject.Project.GetType());
                    bool perfectMatch = includedProject != null && solutionProject.Configurations.Contains(includedProject.Configuration);
                    if (!perfectMatch)
                        continue;

                    var conf = includedProject.Configuration;
                    if (!conf.IsFastBuildEnabledProjectConfig())
                        continue;

                    mustGenerateFastbuild = true;

                    IPlatformBff platformBff = platformBffCache.GetValueOrAdd(conf.Platform, PlatformRegistry.Query<IPlatformBff>(conf.Platform));

                    platformBff.AddCompilerSettings(masterBffInfo.CompilerSettings, conf);

                    if (FastBuildSettings.WriteAllConfigsSection && includedProject.ToBuild == Solution.Configuration.IncludedProjectInfo.Build.Yes)
                        masterBffInfo.AllConfigsSections.Add(Bff.GetShortProjectName(project, conf));

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
                            var copies = ProjectOptionsGenerator.ConvertPostBuildCopiesToRelative(conf, masterBffDirectory);
                            foreach (var copy in copies)
                            {
                                var sourceFile = copy.Key;
                                var sourceFileName = Path.GetFileName(sourceFile);
                                var destinationFolder = copy.Value;
                                var destinationFile = Path.Combine(destinationFolder, sourceFileName);

                                // use the global root for alias computation, as the project has not idea in which master bff it has been included
                                var destinationRelativeToGlobal = Util.GetConvertedRelativePath(masterBffDirectory, destinationFolder, conf.Project.RootPath, true, conf.Project.RootPath);

                                {
                                    string key = sourceFileName + destinationRelativeToGlobal;
                                    string currentSourceFullPath = Util.PathGetAbsolute(masterBffDirectory, sourceFile);
                                    string previous;
                                    if (verificationPostBuildCopies.TryGetValue(key, out previous))
                                    {
                                        if (previous != currentSourceFullPath)
                                            builder.LogErrorLine("A post-build copy to the destination '{0}' already exist but from different sources: '{1}' and '{2}'!", Util.PathGetAbsolute(masterBffDirectory, destinationFolder), previous, currentSourceFullPath);
                                    }
                                    else
                                    {
                                        verificationPostBuildCopies.Add(key, currentSourceFullPath);
                                    }
                                }

                                string fastBuildCopyAlias = UtilityMethods.GetFastBuildCopyAlias(sourceFileName, destinationRelativeToGlobal);
                                {
                                    using (fileGenerator.Declare("fastBuildCopyAlias", fastBuildCopyAlias))
                                    using (fileGenerator.Declare("fastBuildCopySource", Bff.CurrentBffPathKeyCombine(sourceFile)))
                                    using (fileGenerator.Declare("fastBuildCopyDest", Bff.CurrentBffPathKeyCombine(destinationFile)))
                                    {
                                        if (!bffMasterSection.ContainsKey(fastBuildCopyAlias))
                                            bffMasterSection.Add(fastBuildCopyAlias, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.CopyFileSection));
                                    }
                                }
                            }
                        }

                        foreach (var eventPair in conf.EventPreBuildExecute)
                        {
                            preBuildEvents.Add(eventPair.Key, eventPair.Value);
                        }

                        foreach (var buildEvent in conf.ResolvedEventPreBuildExe)
                        {
                            string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, buildEvent, Vcxproj.BuildStep.PreBuild);
                            preBuildEvents.Add(eventKey, buildEvent);
                        }

                        WriteEvents(fileGenerator.Resolver, preBuildEvents, bffPreBuildSection, conf.Project.RootPath, masterBffDirectory);

                        var customPreBuildEvents = new Dictionary<string, Project.Configuration.BuildStepBase>();
                        foreach (var eventPair in conf.EventCustomPrebuildExecute)
                            customPreBuildEvents.Add(eventPair.Key, eventPair.Value);

                        foreach (var buildEvent in conf.ResolvedEventCustomPreBuildExe)
                        {
                            string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, buildEvent, Vcxproj.BuildStep.PreBuildCustomAction);
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

            masterBffCopySections.AddRange(bffMasterSection.Values);
            masterBffCopySections.AddRange(bffPreBuildSection.Values);

            masterBffCustomSections.AddRange(bffCustomPreBuildSection.Values);

            var result = new StringBuilder();
            foreach (var projectBffFullPath in GetMasterIncludeList(masterBffInfo.BffIncludeToDependencyIncludes))
            {
                string projectFullPath = Path.GetDirectoryName(projectBffFullPath);
                var projectPathRelativeFromMasterBff = Util.PathGetRelative(masterBffDirectory, projectFullPath, true);

                string bffKeyRelative = Path.Combine(projectPathRelativeFromMasterBff, Path.GetFileName(projectBffFullPath));

                result.AppendLine($"#include \"{bffKeyRelative}\"");
            }

            string fastBuildMasterBffDependencies = result.Length == 0 ? FileGeneratorUtilities.RemoveLineTag : result.ToString();

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
            MemoryStream bffCleanMemoryStream = fileGenerator.ToMemoryStream();

            // Write master .bff file
            FileInfo bffFileInfo = new FileInfo(masterBffFilePath);
            bool updated = builder.Context.WriteGeneratedFile(null, bffFileInfo, bffCleanMemoryStream);

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
            foreach (var buildEvent in buildEvents)
            {
                string eventKey = resolver.Resolve(buildEvent.Key);

                if (bffSection.ContainsKey(eventKey))
                    continue;

                var resolveableBuildStep = UtilityMethods.GetResolveableFromBuildStep(buildEvent.Key, buildEvent.Value);
                var resolvedBuildStep = resolveableBuildStep.Resolve(projectRoot, relativeTo, resolver);

                bffSection.Add(eventKey, resolvedBuildStep);
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
            MemoryStream bffCleanMemoryStream = fileGenerator.ToMemoryStream();

            // Write master bff global settings file
            FileInfo bffFileInfo = new FileInfo(masterBffGlobalConfigFile);
            if (builder.Context.WriteGeneratedFile(null, bffFileInfo, bffCleanMemoryStream))
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
            if (allDevEnv.Contains(DevEnv.xcode4ios))
            {
                // we'll keep the #if guards if we have other devenv in the file
                if (allDevEnv.Count > 1)
                {
                    envRemoveGuards = string.Empty;
                    fastBuildEnvironments += Bff.Template.ConfigurationFile.WinEnvironment;
                }
                fastBuildEnvironments += Bff.Template.ConfigurationFile.OsxEnvironment;
            }
            else
            {
                fastBuildEnvironments += Bff.Template.ConfigurationFile.WinEnvironment;
            }

            using (masterBffGenerator.Declare("fastBuildProjectName", "Master"))
            using (masterBffGenerator.Declare("CachePath", cachePath))
            using (masterBffGenerator.Declare("CachePluginDLL", cachePluginDLL))
            using (masterBffGenerator.Declare("WorkerConnectionLimit", workerConnectionLimit))
            using (masterBffGenerator.Declare("fastBuildSystemRoot", FastBuildSettings.SystemRoot))
            using (masterBffGenerator.Declare("fastBuildPATH", fastBuildPATH))
            using (masterBffGenerator.Declare("fastBuildAllowDBMigration", FastBuildSettings.FastBuildAllowDBMigration ? "true" : FileGeneratorUtilities.RemoveLineTag))
            using (masterBffGenerator.Declare("fastBuildEnvironments", fastBuildEnvironments))
            using (masterBffGenerator.Declare("envRemoveGuards", envRemoveGuards))
            {
                masterBffGenerator.Write(Bff.Template.ConfigurationFile.HeaderFile);
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

                string fastBuildVS2012EnumBugWorkaround = FileGeneratorUtilities.RemoveLineTag;
                if (FastBuildSettings.EnableVS2012EnumBugWorkaround &&
                    compilerSettings.DevEnv == DevEnv.vs2012 &&
                    compilerPlatform.HasFlag(Platform.win64))
                {
                    fastBuildVS2012EnumBugWorkaround = ".VS2012EnumBugFix = true";
                }

                using (masterBffGenerator.Declare("fastbuildCompilerName", compiler.Key))
                using (masterBffGenerator.Declare("fastBuildCompilerRootPath", compilerSettings.RootPath))
                using (masterBffGenerator.Declare("fastBuildCompilerExecutable", string.IsNullOrEmpty(compilerSettings.Executable) ? FileGeneratorUtilities.RemoveLineTag : compilerSettings.Executable))
                using (masterBffGenerator.Declare("fastBuildExtraFiles", compilerSettings.ExtraFiles.Count > 0 ? UtilityMethods.FBuildCollectionFormat(compilerSettings.ExtraFiles, 28) : FileGeneratorUtilities.RemoveLineTag))
                using (masterBffGenerator.Declare("fastBuildCompilerFamily", string.IsNullOrEmpty(fastBuildCompilerFamily) ? FileGeneratorUtilities.RemoveLineTag : fastBuildCompilerFamily))
                using (masterBffGenerator.Declare("fastBuildVS2012EnumBugWorkaround", fastBuildVS2012EnumBugWorkaround))
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
                        using (masterBffGenerator.Declare("fastBuildCompilerName", compConf.Compiler != FileGeneratorUtilities.RemoveLineTag ? compConf.Compiler : compiler.Key))
                        using (masterBffGenerator.Declare("fastBuildLibrarian", compConf.Librarian))
                        using (masterBffGenerator.Declare("fastBuildLinker", compConf.Linker))
                        using (masterBffGenerator.Declare("fastBuildLinkerType", string.IsNullOrEmpty(fastBuildLinkerType) ? FileGeneratorUtilities.RemoveLineTag : fastBuildLinkerType))
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

        [Obsolete("This method is not supported anymore.")]
        public static bool IsMasterBffFilename(string filename)
        {
            return false;
        }
    }
}
