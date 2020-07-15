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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sharpmake.Generators.FastBuild
{
    public partial class Bff
    {
        public class Unity
        {
            public string UnityName                           = string.Empty; // Name of unity
            public string UnityOutputPath                     = string.Empty; // Path to output generated Unity files
            public string UnityInputPath                      = FileGeneratorUtilities.RemoveLineTag; // (optional) Path (or paths) to find files
            public string UnityInputExcludePath               = FileGeneratorUtilities.RemoveLineTag; // (optional) Path (or paths) in which to ignore files
            public string UnityInputExcludePattern            = FileGeneratorUtilities.RemoveLineTag; // (optional) Wildcard pattern(s) of files/folders to exclude
            public string UnityInputPattern                   = FileGeneratorUtilities.RemoveLineTag; // (optional) Pattern(s) of files to find (default *cpp)
            public string UnityInputPathRecurse               = FileGeneratorUtilities.RemoveLineTag; // (optional) Recurse when searching for files (default true)
            public string UnityInputFiles                     = FileGeneratorUtilities.RemoveLineTag; // (optional) Explicit list of files to include
            public string UnityInputExcludedFiles             = FileGeneratorUtilities.RemoveLineTag; // (optional) Explicit list of excluded files (partial, root-relative of full path)
            public string UnityInputObjectLists               = FileGeneratorUtilities.RemoveLineTag; // (optional) ObjectList(s) to use as input
            public string UnityInputIsolateWritableFiles      = FileGeneratorUtilities.RemoveLineTag; // (optional) Build writable files individually (default false)
            public string UnityInputIsolateWritableFilesLimit = FileGeneratorUtilities.RemoveLineTag; // (optional) Disable isolation when many files are writable (default 0)
            public string UnityOutputPattern                  = FileGeneratorUtilities.RemoveLineTag; // (optional) Pattern of output Unity file names (default Unity*cpp)
            public string UnityNumFiles                       = FileGeneratorUtilities.RemoveLineTag; // (optional) Number of Unity files to generate (default 1)
            public string UnityPCH                            = FileGeneratorUtilities.RemoveLineTag; // (optional) Precompiled Header file to add to generated Unity files

            public const string DefaultUnityInputPatternExtension = ".cpp";

            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;
                    hash = hash * 23 + UnityName.GetHashCode();
                    hash = hash * 23 + UnityOutputPath.GetHashCode();
                    hash = hash * 23 + UnityInputPath.GetHashCode();
                    hash = hash * 23 + UnityInputExcludePath.GetHashCode();
                    hash = hash * 23 + UnityInputExcludePattern.GetHashCode();
                    hash = hash * 23 + UnityInputPattern.GetHashCode();
                    hash = hash * 23 + UnityInputPathRecurse.GetHashCode();
                    hash = hash * 23 + UnityInputFiles.GetHashCode();
                    hash = hash * 23 + UnityInputExcludedFiles.GetHashCode();
                    hash = hash * 23 + UnityInputObjectLists.GetHashCode();
                    hash = hash * 23 + UnityInputIsolateWritableFiles.GetHashCode();
                    hash = hash * 23 + UnityInputIsolateWritableFilesLimit.GetHashCode();
                    hash = hash * 23 + UnityOutputPattern.GetHashCode();
                    hash = hash * 23 + UnityNumFiles.GetHashCode();
                    hash = hash * 23 + UnityPCH.GetHashCode();

                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;

                return Equals((Unity)obj);
            }

            private bool Equals(Unity unity)
            {
                return string.Equals(UnityName,                           unity.UnityName)
                    && string.Equals(UnityOutputPath,                     unity.UnityOutputPath)
                    && string.Equals(UnityInputPath,                      unity.UnityInputPath)
                    && string.Equals(UnityInputExcludePath,               unity.UnityInputExcludePath)
                    && string.Equals(UnityInputExcludePattern,            unity.UnityInputExcludePattern)
                    && string.Equals(UnityInputPattern,                   unity.UnityInputPattern)
                    && string.Equals(UnityInputPathRecurse,               unity.UnityInputPathRecurse)
                    && string.Equals(UnityInputFiles,                     unity.UnityInputFiles)
                    && string.Equals(UnityInputExcludedFiles,             unity.UnityInputExcludedFiles)
                    && string.Equals(UnityInputObjectLists,               unity.UnityInputObjectLists)
                    && string.Equals(UnityInputIsolateWritableFiles,      unity.UnityInputIsolateWritableFiles)
                    && string.Equals(UnityInputIsolateWritableFilesLimit, unity.UnityInputIsolateWritableFilesLimit)
                    && string.Equals(UnityOutputPattern,                  unity.UnityOutputPattern)
                    && string.Equals(UnityNumFiles,                       unity.UnityNumFiles)
                    && string.Equals(UnityPCH,                            unity.UnityPCH);
            }
        }

        internal interface IResolvable
        {
            string Resolve(string rootPath, string bffFilePath, Resolver resolver);
        }

        internal class ExecNode : IResolvable
        {
            public string BuildStepKey;
            public string ExecutableFile;
            public Strings InputFiles;
            public string OutputFile;
            public string Arguments;
            public string WorkingPath;
            public bool UseStdOutAsOutput;
            public bool AlwaysShowOutput;

            public ExecNode(string buildStepKey, Project.Configuration.BuildStepExecutable buildStep)
            {
                BuildStepKey = buildStepKey;
                ExecutableFile = buildStep.ExecutableFile;

                IEnumerable<string> inputFiles = buildStep.FastBuildExecutableInputFiles.Count > 0 ? buildStep.FastBuildExecutableInputFiles : Enumerable.Repeat(buildStep.ExecutableInputFileArgumentOption, 1);
                InputFiles = new Strings(inputFiles);

                OutputFile = buildStep.ExecutableOutputFileArgumentOption;
                Arguments = buildStep.ExecutableOtherArguments;
                WorkingPath = buildStep.ExecutableWorkingDirectory;
                UseStdOutAsOutput = buildStep.FastBuildUseStdOutAsOutput;
                AlwaysShowOutput = buildStep.FastBuildAlwaysShowOutput;
            }

            public string Resolve(string rootPath, string bffFilePath, Resolver resolver)
            {
                var inputFiles = InputFiles.Select(f => UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, f));

                using (resolver.NewScopedParameter("fastBuildPreBuildName", BuildStepKey))
                using (resolver.NewScopedParameter("fastBuildPrebuildExeFile", UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, ExecutableFile)))
                using (resolver.NewScopedParameter("fastBuildPreBuildInputFiles", UtilityMethods.FBuildFormatList(inputFiles.ToList(), 26)))
                using (resolver.NewScopedParameter("fastBuildPreBuildOutputFile", UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, OutputFile)))
                using (resolver.NewScopedParameter("fastBuildPreBuildArguments", string.IsNullOrWhiteSpace(Arguments) ? FileGeneratorUtilities.RemoveLineTag : Arguments))
                using (resolver.NewScopedParameter("fastBuildPrebuildWorkingPath", UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, WorkingPath)))
                using (resolver.NewScopedParameter("fastBuildPrebuildUseStdOutAsOutput", UseStdOutAsOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                using (resolver.NewScopedParameter("fastBuildPrebuildAlwaysShowOutput", AlwaysShowOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                {
                    return resolver.Resolve(Bff.Template.ConfigurationFile.GenericExecutableSection);
                }
            }
        }

        internal class CopyNode : IResolvable
        {
            public string BuildStepKey;
            public string Source;
            public string Destination;

            public CopyNode(string buildStepKey, Project.Configuration.BuildStepCopy buildStep)
            {
                BuildStepKey = buildStepKey;
                Source = buildStep.SourcePath;
                Destination = buildStep.DestinationPath;
            }

            public string Resolve(string rootPath, string bffFilePath, Resolver resolver)
            {
                var normalizedSource = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Source);
                var normalizedDestination = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Destination);

                using (resolver.NewScopedParameter("fastBuildCopyAlias", BuildStepKey))
                using (resolver.NewScopedParameter("fastBuildCopySource", normalizedSource))
                using (resolver.NewScopedParameter("fastBuildCopyDest", normalizedDestination))
                {
                    return resolver.Resolve(Bff.Template.ConfigurationFile.CopyFileSection);
                }
            }
        }

        internal class CopyDirNode : IResolvable
        {
            public string BuildStepKey;
            public string Source;
            public string Destination;
            public bool Recurse;
            public string FilePattern;

            public CopyDirNode(string buildStepKey, Project.Configuration.BuildStepCopy buildStep)
            {
                BuildStepKey = buildStepKey;
                Source = buildStep.SourcePath;
                Destination = buildStep.DestinationPath;
                Recurse = buildStep.IsRecurse;
                FilePattern = buildStep.CopyPattern;
            }

            public string Resolve(string rootPath, string bffFilePath, Resolver resolver)
            {
                var normalizedSource = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Source);
                var normalizedDestination = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Destination);

                using (resolver.NewScopedParameter("fastBuildCopyDirName", BuildStepKey))
                using (resolver.NewScopedParameter("fastBuildCopyDirSourcePath", Util.EnsureTrailingSeparator(normalizedSource)))
                using (resolver.NewScopedParameter("fastBuildCopyDirDestinationPath", Util.EnsureTrailingSeparator(normalizedDestination)))
                using (resolver.NewScopedParameter("fastBuildCopyDirRecurse", Recurse.ToString().ToLower()))
                using (resolver.NewScopedParameter("fastBuildCopyDirPattern", UtilityMethods.GetBffFileCopyPattern(FilePattern)))
                {
                    return resolver.Resolve(Bff.Template.ConfigurationFile.CopyDirSection);
                }
            }
        }

        internal class TestNode : IResolvable
        {
            public string BuildStepKey;
            public string Executable;
            public string WorkingDir;
            public string Output;
            public string Arguments;
            public int TimeOutInSeconds;
            public bool AlwaysShowOutput;

            public TestNode(string buildStepKey, Project.Configuration.BuildStepTest buildStep)
            {
                BuildStepKey = buildStepKey;
                Executable = buildStep.TestExecutable;
                WorkingDir = buildStep.TestWorkingDir;
                Output = buildStep.TestOutput;
                Arguments = buildStep.TestArguments;
                TimeOutInSeconds = buildStep.TestTimeOutInSecond;
                AlwaysShowOutput = buildStep.TestAlwaysShowOutput;
            }

            public string Resolve(string rootPath, string bffFilePath, Resolver resolver)
            {
                var normalizedExecutable = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Executable);
                var normalizedWorkingDir = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, WorkingDir);
                var normalizedOutput = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Output);

                using (resolver.NewScopedParameter("fastBuildTest", BuildStepKey))
                using (resolver.NewScopedParameter("fastBuildTestExecutable", normalizedExecutable))
                using (resolver.NewScopedParameter("fastBuildTestWorkingDir", normalizedWorkingDir))
                using (resolver.NewScopedParameter("fastBuildTestOutput", normalizedOutput))
                using (resolver.NewScopedParameter("fastBuildTestArguments", string.IsNullOrWhiteSpace(Arguments) ? FileGeneratorUtilities.RemoveLineTag : Arguments))
                using (resolver.NewScopedParameter("fastBuildTestTimeOut", TimeOutInSeconds == 0 ? FileGeneratorUtilities.RemoveLineTag : TimeOutInSeconds.ToString()))
                using (resolver.NewScopedParameter("fastBuildTestAlwaysShowOutput", AlwaysShowOutput.ToString().ToLower()))
                {
                    return resolver.Resolve(Bff.Template.ConfigurationFile.TestSection);
                }
            }
        }

        // This NMake command generator is for supporting legacy code without any client code change.
        internal class FastBuildDefaultNMakeCommandGenerator : FastBuildMakeCommandGenerator
        {
            public override string GetCommand(BuildType buildType, Sharpmake.Project.Configuration conf, string fastbuildArguments)
            {
                Project project = conf.Project;
                string fastBuildShortProjectName = Bff.GetShortProjectName(project, conf);

                string makePath = FastBuildSettings.FastBuildMakeCommand;
                if (!Path.IsPathRooted(FastBuildSettings.FastBuildMakeCommand))
                    makePath = conf.Project.RootPath + Path.DirectorySeparatorChar + FastBuildSettings.FastBuildMakeCommand;
                makePath = Util.SimplifyPath(makePath);

                string fastBuildExecutable = Util.PathGetRelative(conf.ProjectPath, makePath, true);

                string rebuildCmd = buildType == BuildType.Rebuild ? " -clean" : "";

                // $(ProjectDir) has a trailing slash
                return $@"$(ProjectDir){fastBuildExecutable}{rebuildCmd} {fastBuildShortProjectName} {fastbuildArguments}";
            }
        }

        public interface IUnityResolver
        {
            void ResolveUnities(Project project, string projectPath, ref Dictionary<Unity, List<Project.Configuration>> unities);
        }

        public class HashUnityResolver : IUnityResolver
        {
            public void ResolveUnities(Project project, string projectPath, ref Dictionary<Unity, List<Project.Configuration>> unities)
            {
                foreach (var unitySection in unities)
                {
                    var unity = unitySection.Key;
                    var unityConfigurations = unitySection.Value;

                    // Don't use Object.GetHashCode() on a int[] object from GetMergedFragmentValuesAcrossConfigurations() as it is
                    // non-deterministic and depends on order of execution. String.GetHashCode() is stable as long as we don't use
                    // <UseRandomizedStringHashAlgorithm enabled="1" /> in the application config file, or as long as we don't use
                    // .NET Core. It can change between .NET versions, however; naming should be stable between different runs on the same machine.
                    int hashcode = unity.GetHashCode() ^ projectPath.GetHashCode() ^ string.Join("_", unityConfigurations).GetHashCode();

                    unity.UnityName = $"{project.Name}_unity_{hashcode:X8}";
                    unity.UnityOutputPattern = unity.UnityName.ToLower() + "*.cpp";
                }
            }
        }

        public class FragmentUnityResolver : IUnityResolver
        {
            public void ResolveUnities(Project project, string projectPath, ref Dictionary<Unity, List<Project.Configuration>> unities)
            {
                // we need to compute the missing member values in the Unity objects:
                // UnityName and UnityOutputPattern
                var masks = new List<Tuple<Unity, int[]>>();

                List<FieldInfo> fragmentsInfos = null;

                // merge the fragment values of all configurations sharing a unity
                foreach (var unitySection in unities)
                {
                    var configurations = unitySection.Value;
                    var unityFragments = configurations.Select(x => x.Target.GetFragmentsValue()).ToList();

                    // get the fragment info from the first configuration target,
                    // which works as they all share the same Target type
                    if (fragmentsInfos == null)
                        fragmentsInfos = new List<FieldInfo>(configurations.First().Target.GetFragmentFieldInfo());

                    var unityMerged = GetMergedFragments(unityFragments);
                    masks.Add(Tuple.Create(unitySection.Key, unityMerged));
                }

                // get the fragment values for the whole project: could span multiple bff files
                IEnumerable<Project.Configuration> fastBuildConfigurations = project.Configurations.Where(c => c.IsFastBuild);
                int[] merged = GetMergedFragmentValuesAcrossConfigurations(fastBuildConfigurations);

                // finally, create a unity name that only contains the varying fragments
                foreach (var unitySection in masks)
                {
                    var unity = unitySection.Item1;
                    var unityFragments = unitySection.Item2;
                    var differentFragmentIndices = GetDifferentFragmentIndices(merged, unityFragments);

                    string fragmentString = string.Empty;
                    for (int i = 0; i < unityFragments.Length; ++i)
                    {
                        // if not a differentiating fragment, skip
                        if (!differentFragmentIndices.Contains(i))
                            continue;

                        // Convert from int to the fragment enum type, so we can ToString() them.
                        // Fragments are enums by contract, so Enum.ToObject works
                        var typedFragment = Enum.ToObject(fragmentsInfos[i].FieldType, unityFragments[i]);

                        if (typedFragment is Platform)
                        {
                            Platform platformFragment = (Platform)typedFragment;
                            foreach (Platform platformEnum in Enum.GetValues(typeof(Platform)))
                            {
                                if (!platformFragment.HasFlag(platformEnum))
                                    continue;

                                string platformString = platformEnum.ToString();
                                if (platformEnum >= Platform._reserved9)
                                    platformString = Util.GetSimplePlatformString(platformEnum);
                                fragmentString += "_" + SanitizeForUnityName(platformString).ToLower();
                            }
                        }
                        else
                        {
                            fragmentString += "_" + SanitizeForUnityName(typedFragment.ToString());
                        }
                    }
                    unity.UnityName = project.Name + fragmentString + "_unity";
                    unity.UnityOutputPattern = unity.UnityName.ToLower() + "*.cpp";
                }
            }
        }

        private static string SanitizeForUnityName(string name)
        {
            return string.Join("_", name.Split(new[] { ' ', ':', '.', ',' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public static string CurrentBffPathKeyCombine(string relativePath)
        {
            string simplified = Util.SimplifyPath(relativePath);
            if (simplified == ".")
                return Bff.CurrentBffPathKey;

            return Path.Combine(Bff.CurrentBffPathKey, relativePath);
        }

        private static int[] GetMergedFragments(List<int[]> fragments)
        {
            var merged = fragments[0];

            for (int i = 1; i < fragments.Count; ++i)
            {
                var toMerge = fragments[i];
                for (int j = 0; j < toMerge.Length; ++j)
                    merged[j] |= toMerge[j];
            }

            return merged;
        }

        private static int[] GetMergedFragmentValuesAcrossConfigurations(IEnumerable<Project.Configuration> configurations)
        {
            var fragments = configurations.Select(x => x.Target.GetFragmentsValue()).ToList();
            var merged = GetMergedFragments(fragments);

            return merged;
        }

        private static List<int> GetDifferentFragmentIndices(int[] merged, int[] fragmentValuesComparisonBase)
        {
            var differentFragmentIndices = new List<int>();

            // we want to store in that list the indices of fragments that are different from the base
            for (int j = 0; j < merged.Length; ++j)
            {
                int value = fragmentValuesComparisonBase[j];
                if (value != 0 && value != merged[j])
                    differentFragmentIndices.Add(j);
            }

            return differentFragmentIndices;
        }
    }

    internal static class UtilityMethods
    {
        public static string GetFBuildCompilerFamily(this CompilerFamily compilerFamily)
        {
            switch (compilerFamily)
            {
                case CompilerFamily.MSVC: return "msvc";
                case CompilerFamily.Clang: return "clang";
                case CompilerFamily.GCC: return "gcc";
                case CompilerFamily.SNC: return "snc";
                case CompilerFamily.CodeWarriorWii: return "codewarrior-wii";
                case CompilerFamily.CudaNVCC: return "cuda-nvcc";
                case CompilerFamily.QtRCC: return "qt-rcc";
                case CompilerFamily.VBCC: return "vbcc";
                case CompilerFamily.OrbisWavePsslc: return "orbis-wave-psslc";
                case CompilerFamily.ClangCl: return "clang-cl";
                case CompilerFamily.Auto: return string.Empty;
                default: throw new Exception("Unrecognized compiler family");
            }
        }

        public static string GetFBuildLinkerType(this CompilerSettings.LinkerType linkerType)
        {
            switch (linkerType)
            {
                case CompilerSettings.LinkerType.CodeWarriorLd: return "codewarrior-ld";
                case CompilerSettings.LinkerType.GCC: return "gcc";
                case CompilerSettings.LinkerType.GreenHillsExlr: return "greenhills-exlr";
                case CompilerSettings.LinkerType.MSVC: return "msvc";
                case CompilerSettings.LinkerType.ClangOrbis: return "clang-orbis";
                case CompilerSettings.LinkerType.SNCPS3: return "snc-ps3";
                case CompilerSettings.LinkerType.Auto: return string.Empty;
                default: throw new Exception("Unrecognized linker type");
            }
        }

        public static bool TestPlatformFlags(this UniqueList<Platform> platforms, Platform platformFlags)
        {
            return platforms.Any(platform => platformFlags.HasFlag(platform));
        }

        public static bool IsSupportedFastBuildPlatform(this Platform platform)
        {
            return PlatformRegistry.Has<IPlatformBff>(platform);
        }

        public static bool IsFastBuildEnabledProjectConfig(this Project.Configuration conf)
        {
            return conf.IsFastBuild && conf.Platform.IsSupportedFastBuildPlatform() && !conf.DoNotGenerateFastBuild;
        }

        public static string GetFastBuildCopyAlias(string sourceFileName, string destinationFolder)
        {
            string fastBuildCopyAlias = string.Format("Copy_{0}_{1}", sourceFileName, (destinationFolder + sourceFileName).GetHashCode().ToString("X8"));
            return fastBuildCopyAlias;
        }

        public static string GetBffFileCopyPattern(string copyPattern)
        {
            if (string.IsNullOrEmpty(copyPattern))
                return copyPattern;

            string[] patterns = copyPattern.Split(null);

            if (patterns == null || patterns.Length < 2)
                return "'" + copyPattern + "'";

            return "{ " + string.Join(", ", patterns.Select(p => "'" + p + "'")) + " }";
        }

        public static UniqueList<Project.Configuration> GetOrderedFlattenedProjectDependencies(Project.Configuration conf, bool allDependencies = true, bool fuDependencies = false)
        {
            var dependencies = new UniqueList<Project.Configuration>();
            GetOrderedFlattenedProjectDependenciesInternal(conf, dependencies, allDependencies, fuDependencies);
            return dependencies;
        }

        private static void GetOrderedFlattenedProjectDependenciesInternal(Project.Configuration conf, UniqueList<Project.Configuration> dependencies, bool allDependencies, bool fuDependencies)
        {
            if (!conf.IsFastBuild)
                return;

            var confDependencies = allDependencies ? conf.ResolvedDependencies : fuDependencies ? conf.ForceUsingDependencies : conf.ConfigurationDependencies;

            if (confDependencies.Contains(conf))
                throw new Error("Cyclic dependency detected in project " + conf);

            if (!allDependencies)
            {
                var tmpDeps = new UniqueList<Project.Configuration>();
                foreach (Project.Configuration dep in confDependencies)
                {
                    GetOrderedFlattenedProjectDependenciesInternal(dep, tmpDeps, true, fuDependencies);
                    tmpDeps.Add(dep);
                }
                foreach (Project.Configuration dep in tmpDeps)
                {
                    if (dep.IsFastBuild && confDependencies.Contains(dep) && (conf != dep))
                        dependencies.Add(dep);
                }
            }
            else
            {
                foreach (Project.Configuration dep in confDependencies)
                {
                    if (dependencies.Contains(dep))
                        continue;

                    GetOrderedFlattenedProjectDependenciesInternal(dep, dependencies, true, fuDependencies);
                    if (dep.IsFastBuild)
                        dependencies.Add(dep);
                }
            }
        }

        internal static UniqueList<Project.Configuration> GetOrderedFlattenedBuildOnlyDependencies(Project.Configuration conf)
        {
            var dependencies = new UniqueList<Project.Configuration>();
            GetOrderedFlattenedBuildOnlyDependenciesInternal(conf, dependencies);
            return dependencies;
        }

        private static void GetOrderedFlattenedBuildOnlyDependenciesInternal(Project.Configuration conf, UniqueList<Project.Configuration> dependencies)
        {
            if (!conf.IsFastBuild)
                return;

            IEnumerable<Project.Configuration> confDependencies = conf.BuildOrderDependencies;

            if (confDependencies.Contains(conf))
                throw new Error("Cyclic dependency detected in project " + conf);

            UniqueList<Project.Configuration> tmpDeps = new UniqueList<Project.Configuration>();
            foreach (var dep in confDependencies)
            {
                GetOrderedFlattenedBuildOnlyDependenciesInternal(dep, tmpDeps);
                tmpDeps.Add(dep);
            }
            foreach (var dep in tmpDeps)
            {
                if (dep.IsFastBuild && confDependencies.Contains(dep) && (conf != dep))
                    dependencies.Add(dep);
            }
        }

        public static string FBuildCollectionFormat(Strings collection, int spaceLength, Strings includedExtensions = null)
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

        public static string FBuildFormatSingleListItem(string item)
        {
            return string.Format("'{0}'", item);
        }

        public static string FBuildFormatList(List<string> items, int spaceLength)
        {
            if (items.Count == 0)
                return FileGeneratorUtilities.RemoveLineTag;

            if (items.Count == 1)
                return FBuildFormatSingleListItem(items.First());

            //
            // Write all selected items.
            //
            StringBuilder strBuilder = new StringBuilder(1024 * 16);

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

            return strBuilder.ToString();
        }

        public static void WriteCustomBuildStepAsGenericExecutable(string projectRoot, FileGenerator bffGenerator, Project.Configuration.CustomFileBuildStep buildStep, Func<string, bool> functor)
        {
            var relativeBuildStep = buildStep.MakePathRelative(bffGenerator.Resolver,
                (path, commandRelative) =>
            {
                string relativePath = Util.SimplifyPath(Util.PathGetRelative(projectRoot, path));
                if (commandRelative)
                    return Bff.CurrentBffPathKeyCombine(relativePath);
                else
                    return relativePath;
            });

            using (bffGenerator.Declare("fastBuildPreBuildName", relativeBuildStep.Description))
            using (bffGenerator.Declare("fastBuildPrebuildExeFile", relativeBuildStep.Executable))
            using (bffGenerator.Declare("fastBuildPreBuildInputFiles", FBuildFormatSingleListItem(relativeBuildStep.KeyInput)))
            using (bffGenerator.Declare("fastBuildPreBuildOutputFile", relativeBuildStep.Output))
            using (bffGenerator.Declare("fastBuildPreBuildArguments", string.IsNullOrWhiteSpace(relativeBuildStep.ExecutableArguments) ? FileGeneratorUtilities.RemoveLineTag : relativeBuildStep.ExecutableArguments))
            // This is normally the project directory.
            using (bffGenerator.Declare("fastBuildPrebuildWorkingPath", FileGeneratorUtilities.RemoveLineTag))
            using (bffGenerator.Declare("fastBuildPrebuildUseStdOutAsOutput", FileGeneratorUtilities.RemoveLineTag))
            using (bffGenerator.Declare("fastBuildPrebuildAlwaysShowOutput", FileGeneratorUtilities.RemoveLineTag))
            {
                functor(relativeBuildStep.Description);
            }
        }

        public static void WriteConfigCustomBuildStepsAsGenericExecutable(string projectRoot, FileGenerator bffGenerator, Project project, Project.Configuration config, Func<string, bool> functor)
        {
            using (bffGenerator.Resolver.NewScopedParameter("project", project))
            using (bffGenerator.Resolver.NewScopedParameter("config", config))
            using (bffGenerator.Resolver.NewScopedParameter("target", config.Target))
            {
                foreach (var customBuildStep in config.CustomFileBuildSteps)
                {
                    if (customBuildStep.Filter == Project.Configuration.CustomFileBuildStep.ProjectFilter.ExcludeBFF)
                        continue;
                    UtilityMethods.WriteCustomBuildStepAsGenericExecutable(projectRoot, bffGenerator, customBuildStep, functor);
                }
            }
        }

        public static bool HasFastBuildConfig(List<Solution.Configuration> configurations)
        {
            bool hasFastBuildConfig = configurations.Any(
                x => x.IncludedProjectInfos.Any(
                    y => IsFastBuildEnabledProjectConfig(y.Configuration)
                )
            );
            return hasFastBuildConfig;
        }

        public static string GetNormalizedPathForBuildStep(string projectRootPath, string projectFolderPath, string path)
        {
            if (string.IsNullOrEmpty(path))
                return FileGeneratorUtilities.RemoveLineTag;

            if (path.StartsWith(projectRootPath, StringComparison.OrdinalIgnoreCase))
                return Bff.CurrentBffPathKeyCombine(Util.PathGetRelative(projectFolderPath, path));

            // keep the full path for the source if outside of the global root
            return path;
        }

        public static Bff.IResolvable GetResolveableFromBuildStep(string buildStepKey, Project.Configuration.BuildStepBase buildStep)
        {
            if (buildStep is Project.Configuration.BuildStepExecutable)
            {
                return new Bff.ExecNode(buildStepKey, buildStep as Project.Configuration.BuildStepExecutable);
            }

            if (buildStep is Project.Configuration.BuildStepCopy)
            {
                var copyStep = buildStep as Project.Configuration.BuildStepCopy;

                if (copyStep.IsFileCopy)
                {
                    return new Bff.CopyNode(buildStepKey, copyStep);
                }

                return new Bff.CopyDirNode(buildStepKey, copyStep);
            }

            if (buildStep is Project.Configuration.BuildStepTest)
            {
                return new Bff.TestNode(buildStepKey, buildStep as Project.Configuration.BuildStepTest);
            }

            throw new Error("error, BuildStep not supported: {0}", buildStep.GetType().FullName);
        }

        public static List<Bff.IResolvable> GetResolvablesFromBuildSteps(Dictionary<string, Project.Configuration.BuildStepBase> buildSteps)
        {
            return buildSteps.Select(e => GetResolveableFromBuildStep(e.Key, e.Value)).ToList();
        }
    }
}
