using System;
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
        private Builder _masterBffBuilder = null;

        [Obsolete("This method is not supported anymore.")]
        public static bool IsMasterBffFilename(string filename)
        {
            return false;
        }

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
            public DevEnv? DevEnv;
            public UniqueList<Platform> Platforms = new UniqueList<Platform>();
            public List<string> AllConfigsSections = new List<string>(); // All Configs section when running with a source file filter
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

            _masterBffBuilder = builder;

            FileInfo fileInfo = new FileInfo(solutionFile);
            string masterBffPath = fileInfo.Directory.FullName;
            string masterBffFileName = fileInfo.Name;

            bool updated;
            string masterBffFileResult = GenerateMasterBffFile(solution, solutionConfigurations, masterBffPath, masterBffFileName, out updated);
            if (masterBffFileResult != null)
            {
                if (updated)
                {
                    Project.AddFastbuildMasterGeneratedFile(masterBffFileName);
                    generatedFiles.Add(masterBffFileResult);
                }
                else
                {
                    skipFiles.Add(masterBffFileResult);
                    Project.IncrementFastBuildUpToDateFileCount();
                }
            }

            _masterBffBuilder = null;
        }

        private string GenerateMasterBffFile(
            Solution solution,
            List<Solution.Configuration> solutionConfigurations,
            string masterBffPath,
            string masterBffFileNameWithoutExtension,
            out bool updated
        )
        {
            string masterBffFileName = masterBffFileNameWithoutExtension + FastBuildSettings.FastBuildConfigFileExtension;
            string masterBffFullPath = Util.GetCapitalizedPath(masterBffPath + Path.DirectorySeparatorChar + masterBffFileName);

            // Global configuration file is in the same directory as the master bff but filename suffix added to its filename.
            string globalConfigFullPath = GetGlobalBffConfigFileName(masterBffFullPath);
            string globalConfigFileName = Path.GetFileName(globalConfigFullPath);
            bool projectsWereFiltered;
            var solutionProjects = solution.GetResolvedProjects(solutionConfigurations, out projectsWereFiltered);
            if (solutionProjects.Count == 0 && projectsWereFiltered)
            {
                // We are running in filter mode for submit assistant and all projects were filtered out. 
                // We need to skip generation and delete any existing master bff file.
                Util.TryDeleteFile(masterBffFullPath);
                updated = false;
                return null;
            }

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

            foreach (Solution.Configuration solutionConfiguration in solutionConfigurations)
            {
                foreach (var solutionProject in solutionProjects)
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

                    projectRootPath = project.RootPath;

                    var conf = includedProject.Configuration;
                    if (!conf.IsFastBuildEnabledProjectConfig())
                        continue;

                    mustGenerateFastbuild = true;

                    string bffFullFileNameCapitalized = Util.GetCapitalizedPath(conf.BffFullFileName);
                    string projectBffFullPath = $"{bffFullFileNameCapitalized}{FastBuildSettings.FastBuildConfigFileExtension}";

                    masterBffInfo.Platforms.Add(conf.Platform);
                    var devEnv = conf.Target.GetFragment<DevEnv>();
                    if (masterBffInfo.DevEnv == null)
                        masterBffInfo.DevEnv = devEnv;
                    else if (devEnv != masterBffInfo.DevEnv)
                        throw new Error($"Master bff {masterBffFileName} cannot contain varying devEnvs: {masterBffInfo.DevEnv} {devEnv}!");

                    if (FastBuildSettings.WriteAllConfigsSection && includedProject.ToBuild == Solution.Configuration.IncludedProjectInfo.Build.Yes)
                        masterBffInfo.AllConfigsSections.Add(Bff.GetShortProjectName(project, conf));

                    using (fileGenerator.Declare("conf", conf))
                    using (fileGenerator.Declare("project", conf.Project))
                    {

                        var preBuildEvents = new Dictionary<string, Project.Configuration.BuildStepBase>();
                        if (conf.Output == Project.Configuration.OutputType.Exe || conf.ExecuteTargetCopy)
                        {
                            var copies = ProjectOptionsGenerator.ConvertPostBuildCopiesToRelative(conf, masterBffPath);
                            foreach (var copy in copies)
                            {
                                var sourceFile = copy.Key;
                                var sourceFileName = Path.GetFileName(sourceFile);
                                var destinationFolder = copy.Value;
                                var destinationFile = Path.Combine(destinationFolder, sourceFileName);

                                // use the global root for alias computation, as the project has not idea in which master bff it has been included
                                var destinationRelativeToGlobal = Util.GetConvertedRelativePath(masterBffPath, destinationFolder, conf.Project.RootPath, true, conf.Project.RootPath);
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

                            foreach (var eventPair in conf.EventPreBuildExecute)
                            {
                                preBuildEvents.Add(eventPair.Key, eventPair.Value);
                            }

                            foreach (var buildEvent in conf.ResolvedEventPreBuildExe)
                            {
                                string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, buildEvent, Vcxproj.BuildStep.PreBuild);
                                preBuildEvents.Add(eventKey, buildEvent);
                            }

                            WriteEvents(fileGenerator, preBuildEvents, bffPreBuildSection, masterBffPath);
                        }
                    }


                    var customPreBuildEvents = new Dictionary<string, Project.Configuration.BuildStepBase>();
                    foreach (var eventPair in conf.EventCustomPrebuildExecute)
                        customPreBuildEvents.Add(eventPair.Key, eventPair.Value);

                    foreach (var buildEvent in conf.ResolvedEventCustomPreBuildExe)
                    {
                        string eventKey = ProjectOptionsGenerator.MakeBuildStepName(conf, buildEvent, Vcxproj.BuildStep.PreBuildCustomAction);
                        customPreBuildEvents.Add(eventKey, buildEvent);
                    }

                    WriteEvents(fileGenerator, customPreBuildEvents, bffCustomPreBuildSection, masterBffPath);

                    if (includedProject.ToBuild == Solution.Configuration.IncludedProjectInfo.Build.Yes)
                        MergeBffIncludeTreeRecursive(conf, ref masterBffInfo.BffIncludeToDependencyIncludes);
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
                var projectPathRelativeFromMasterBff = Util.PathGetRelative(masterBffPath, projectFullPath, true);

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

            string fastBuildMasterBffDependencies = result.Length == 0 ? FileGeneratorUtilities.RemoveLineTag : result.ToString();

            var masterCompilerSettings = new Dictionary<string, CompilerSettings>();

            // TODO: test what happens when using multiple devenv, one per platform, in the same master bff: should it even be allowed?
            string platformToolSetPath = Path.Combine(masterBffInfo.DevEnv.Value.GetVisualStudioDir(), "VC");
            foreach (var platform in masterBffInfo.Platforms)
            {
                string compilerName = "Compiler-" + Util.GetSimplePlatformString(platform) + "-" + masterBffInfo.DevEnv.Value;
                PlatformRegistry.Query<IPlatformBff>(platform)?.AddCompilerSettings(masterCompilerSettings, compilerName, platformToolSetPath, masterBffInfo.DevEnv.Value, projectRootPath);

            }

            GenerateMasterBffGlobalSettingsFile(globalConfigFullPath, masterBffInfo, masterCompilerSettings);

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
            FileInfo bffFileInfo = new FileInfo(masterBffFullPath);
            updated = _masterBffBuilder.Context.WriteGeneratedFile(null, bffFileInfo, bffCleanMemoryStream);

            solution.PostGenerationCallback?.Invoke(masterBffPath, masterBffFileNameWithoutExtension, FastBuildSettings.FastBuildConfigFileExtension);

            return bffFileInfo.FullName;
        }

        private static void WriteEvents(
            FileGenerator fileGenerator,
            Dictionary<string, Project.Configuration.BuildStepBase> buildEvents,
            Dictionary<string, string> bffSection,
            string relativeTo
        )
        {
            foreach (var buildEvent in buildEvents)
            {
                if (buildEvent.Value is Project.Configuration.BuildStepExecutable)
                {
                    var execCommand = buildEvent.Value as Project.Configuration.BuildStepExecutable;

                    using (fileGenerator.Declare("fastBuildPreBuildName", buildEvent.Key))
                    using (fileGenerator.Declare("fastBuildPrebuildExeFile", Util.PathGetRelative(relativeTo, execCommand.ExecutableFile)))
                    using (fileGenerator.Declare("fastBuildPreBuildInputFile", Util.PathGetRelative(relativeTo, execCommand.ExecutableInputFileArgumentOption)))
                    using (fileGenerator.Declare("fastBuildPreBuildOutputFile", Util.PathGetRelative(relativeTo, execCommand.ExecutableOutputFileArgumentOption)))
                    using (fileGenerator.Declare("fastBuildPreBuildArguments", execCommand.ExecutableOtherArguments))
                    using (fileGenerator.Declare("fastBuildPrebuildWorkingPath", execCommand.ExecutableWorkingDirectory == string.Empty ? FileGeneratorUtilities.RemoveLineTag : Util.PathGetRelative(relativeTo, execCommand.ExecutableWorkingDirectory)))
                    using (fileGenerator.Declare("fastBuildPrebuildUseStdOutAsOutput", execCommand.FastBuildUseStdOutAsOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                    {
                        if (!bffSection.ContainsKey(buildEvent.Key))
                            bffSection.Add(buildEvent.Key, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.GenericExcutableSection));
                    }
                }
                else if (buildEvent.Value is Project.Configuration.BuildStepCopy)
                {
                    var copyCommand = buildEvent.Value as Project.Configuration.BuildStepCopy;

                    string sourcePath = Util.PathGetRelative(relativeTo, copyCommand.SourcePath);
                    string destinationPath = Util.PathGetRelative(relativeTo, copyCommand.DestinationPath);

                    using (fileGenerator.Declare("fastBuildCopyAlias", buildEvent.Key))
                    using (fileGenerator.Declare("fastBuildCopySource", sourcePath))
                    using (fileGenerator.Declare("fastBuildCopyDest", destinationPath))
                    using (fileGenerator.Declare("fastBuildCopyDirName", buildEvent.Key))
                    using (fileGenerator.Declare("fastBuildCopyDirSourcePath", Util.EnsureTrailingSeparator(sourcePath)))
                    using (fileGenerator.Declare("fastBuildCopyDirDestinationPath", Util.EnsureTrailingSeparator(destinationPath)))
                    using (fileGenerator.Declare("fastBuildCopyDirRecurse", copyCommand.IsRecurse.ToString().ToLower()))
                    using (fileGenerator.Declare("fastBuildCopyDirPattern", UtilityMethods.GetBffFileCopyPattern(copyCommand.CopyPattern)))
                    {
                        if (!bffSection.ContainsKey(buildEvent.Key) && copyCommand.IsFileCopy)
                            bffSection.Add(buildEvent.Key, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.CopyFileSection));
                        else if (!bffSection.ContainsKey(buildEvent.Key))
                            bffSection.Add(buildEvent.Key, fileGenerator.Resolver.Resolve(Bff.Template.ConfigurationFile.CopyDirSection));
                    }
                }
            }
        }

        private void GenerateMasterBffGlobalSettingsFile(
            string masterBffGlobalConfigFile, MasterBffInfo masterBffInfo,
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
            if (_masterBffBuilder.Context.WriteGeneratedFile(null, bffFileInfo, bffCleanMemoryStream))
            {
                Project.AddFastbuildMasterGeneratedFile(masterBffGlobalConfigFile);
            }
            else
            {
                Project.IncrementFastBuildUpToDateFileCount();
            }
        }

        private void WriteMasterSettingsSection(
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

        private void WriteMasterCompilerSection(
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

        private void WriteMasterCustomSection(IFileGenerator masterBffGenerator, UniqueList<string> masterBffCustomSections)
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
                if (dependency.Project.GetType().IsDefined(typeof(Export), false))
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