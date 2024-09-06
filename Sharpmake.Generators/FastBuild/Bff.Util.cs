// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
            public string UnityName = string.Empty; // Name of unity
            public string UnityOutputPath = string.Empty; // Path to output generated Unity files
            public string UnityInputPath = FileGeneratorUtilities.RemoveLineTag; // (optional) Path (or paths) to find files
            public string UnityInputExcludePath = FileGeneratorUtilities.RemoveLineTag; // (optional) Path (or paths) in which to ignore files
            public string UnityInputExcludePattern = FileGeneratorUtilities.RemoveLineTag; // (optional) Wildcard pattern(s) of files/folders to exclude
            public string UnityInputPattern = FileGeneratorUtilities.RemoveLineTag; // (optional) Pattern(s) of files to find (default *cpp)
            public string UnityInputPathRecurse = FileGeneratorUtilities.RemoveLineTag; // (optional) Recurse when searching for files (default true)
            public string UnityInputFiles = FileGeneratorUtilities.RemoveLineTag; // (optional) Explicit list of files to include
            public string UnityInputExcludedFiles = FileGeneratorUtilities.RemoveLineTag; // (optional) Explicit list of excluded files (partial, root-relative of full path)
            public string UnityInputObjectLists = FileGeneratorUtilities.RemoveLineTag; // (optional) ObjectList(s) to use as input
            public string UnityInputIsolateWritableFiles = FileGeneratorUtilities.RemoveLineTag; // (optional) Build writable files individually (default false)
            public string UnityInputIsolateWritableFilesLimit = FileGeneratorUtilities.RemoveLineTag; // (optional) Disable isolation when many files are writable (default 0)
            public string UnityInputIsolateListFile = FileGeneratorUtilities.RemoveLineTag; // (optional) Text file containing list of files to isolate
            public string UnityOutputPattern = FileGeneratorUtilities.RemoveLineTag; // (optional) Pattern of output Unity file names (default Unity*cpp)
            public string UnityNumFiles = FileGeneratorUtilities.RemoveLineTag; // (optional) Number of Unity files to generate (default 1)
            public string UnityPCH = FileGeneratorUtilities.RemoveLineTag; // (optional) Precompiled Header file to add to generated Unity files
            public string UseRelativePaths = FileGeneratorUtilities.RemoveLineTag; // (optional) Use relative paths for generated Unity files
            public Byte UnitySectionBucket = 0; // Internal sharpmake field used to force separate unity sections in certain cases.
            public const string DefaultUnityInputPatternExtension = ".cpp";
            public const string DefaultUnityOutputPatternExtension = "Unity*.cpp";

            internal string UnityFullOutputPath = string.Empty; // Path to output generated Unity files

            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;
                    hash = hash * 23 + UnityName.GetDeterministicHashCode();
                    hash = hash * 23 + UnityOutputPath.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputPath.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputExcludePath.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputExcludePattern.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputPattern.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputPathRecurse.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputFiles.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputExcludedFiles.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputObjectLists.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputIsolateWritableFiles.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputIsolateWritableFilesLimit.GetDeterministicHashCode();
                    hash = hash * 23 + UnityInputIsolateListFile.GetDeterministicHashCode();
                    hash = hash * 23 + UnityOutputPattern.GetDeterministicHashCode();
                    hash = hash * 23 + UnityNumFiles.GetDeterministicHashCode();
                    hash = hash * 23 + UnityPCH.GetDeterministicHashCode();
                    hash = hash * 23 + UseRelativePaths.GetDeterministicHashCode();
                    hash = hash * 23 + UnitySectionBucket;

                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != GetType())
                    return false;

                return Equals((Unity)obj);
            }

            private bool Equals(Unity unity)
            {
                return string.Equals(UnityName, unity.UnityName)
                    && string.Equals(UnityOutputPath, unity.UnityOutputPath)
                    && string.Equals(UnityInputPath, unity.UnityInputPath)
                    && string.Equals(UnityInputExcludePath, unity.UnityInputExcludePath)
                    && string.Equals(UnityInputExcludePattern, unity.UnityInputExcludePattern)
                    && string.Equals(UnityInputPattern, unity.UnityInputPattern)
                    && string.Equals(UnityInputPathRecurse, unity.UnityInputPathRecurse)
                    && string.Equals(UnityInputFiles, unity.UnityInputFiles)
                    && string.Equals(UnityInputExcludedFiles, unity.UnityInputExcludedFiles)
                    && string.Equals(UnityInputObjectLists, unity.UnityInputObjectLists)
                    && string.Equals(UnityInputIsolateWritableFiles, unity.UnityInputIsolateWritableFiles)
                    && string.Equals(UnityInputIsolateWritableFilesLimit, unity.UnityInputIsolateWritableFilesLimit)
                    && string.Equals(UnityInputIsolateListFile, unity.UnityInputIsolateListFile)
                    && string.Equals(UnityOutputPattern, unity.UnityOutputPattern)
                    && string.Equals(UnityNumFiles, unity.UnityNumFiles)
                    && string.Equals(UnityPCH, unity.UnityPCH)
                    && string.Equals(UseRelativePaths, unity.UseRelativePaths)
                    && UnitySectionBucket == unity.UnitySectionBucket;
            }
        }

        internal interface IResolvable
        {
            string Resolve(string rootPath, string bffFilePath, Resolver resolver);
        }

        internal abstract class BffNodeBase : IResolvable
        {
            public string Identifier;

            public abstract string Resolve(string rootPath, string bffFilePath, Resolver resolver);
        }

        internal abstract class BffDependentNode : BffNodeBase
        {
            public Strings Dependencies = new Strings();
        }

        internal class ExecNode : BffDependentNode
        {
            public string ExecutableFile;
            public Strings InputFiles;
            public string OutputFile;
            public string Arguments;
            public string WorkingPath;
            public bool UseStdOutAsOutput;
            public bool AlwaysShowOutput;
            public bool ExecAlways;

            public ExecNode(string buildStepKey, Project.Configuration.BuildStepExecutable buildStep)
            {
                Identifier = buildStepKey;
                ExecutableFile = buildStep.ExecutableFile;

                IEnumerable<string> inputFiles = buildStep.FastBuildExecutableInputFiles.Count > 0 ? buildStep.FastBuildExecutableInputFiles : Enumerable.Repeat(buildStep.ExecutableInputFileArgumentOption, 1);
                InputFiles = new Strings(inputFiles);

                OutputFile = buildStep.ExecutableOutputFileArgumentOption;
                Arguments = buildStep.ExecutableOtherArguments;
                WorkingPath = buildStep.ExecutableWorkingDirectory;
                UseStdOutAsOutput = buildStep.FastBuildUseStdOutAsOutput;
                AlwaysShowOutput = buildStep.FastBuildAlwaysShowOutput;
                ExecAlways = buildStep.FastBuildExecAlways;
            }

            public override string Resolve(string rootPath, string bffFilePath, Resolver resolver)
            {
                var inputFiles = InputFiles.Select(f => UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, f));

                using (resolver.NewScopedParameter("fastBuildPreBuildName", Identifier))
                using (resolver.NewScopedParameter("fastBuildPrebuildExeFile", UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, ExecutableFile)))
                using (resolver.NewScopedParameter("fastBuildPreBuildInputFiles", UtilityMethods.FBuildFormatList(inputFiles.ToList(), 26)))
                using (resolver.NewScopedParameter("fastBuildPreBuildOutputFile", UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, OutputFile)))
                using (resolver.NewScopedParameter("fastBuildPreBuildArguments", string.IsNullOrWhiteSpace(Arguments) ? FileGeneratorUtilities.RemoveLineTag : Arguments))
                using (resolver.NewScopedParameter("fastBuildPrebuildWorkingPath", UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, WorkingPath)))
                using (resolver.NewScopedParameter("fastBuildPrebuildUseStdOutAsOutput", UseStdOutAsOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                using (resolver.NewScopedParameter("fastBuildPrebuildAlwaysShowOutput", AlwaysShowOutput ? "true" : FileGeneratorUtilities.RemoveLineTag))
                using (resolver.NewScopedParameter("fastBuildExecPreBuildDependencies", Dependencies.Count > 0 ? UtilityMethods.FBuildFormatList(Dependencies.Values, 26) : FileGeneratorUtilities.RemoveLineTag))
                using (resolver.NewScopedParameter("fastBuildExecAlways", ExecAlways ? "true" : FileGeneratorUtilities.RemoveLineTag))
                {
                    return resolver.Resolve(Bff.Template.ConfigurationFile.GenericExecutableSection);
                }
            }
        }

        internal class CopyNode : BffDependentNode
        {
            public string Source;
            public string Destination;

            public CopyNode(string buildStepKey, Project.Configuration.BuildStepCopy buildStep)
            {
                Identifier = buildStepKey;
                Source = buildStep.SourcePath;
                Destination = buildStep.DestinationPath;
            }

            public override string Resolve(string rootPath, string bffFilePath, Resolver resolver)
            {
                var normalizedSource = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Source);
                var normalizedDestination = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Destination);

                using (resolver.NewScopedParameter("fastBuildCopyAlias", Identifier))
                using (resolver.NewScopedParameter("fastBuildCopySource", normalizedSource))
                using (resolver.NewScopedParameter("fastBuildCopyDest", normalizedDestination))
                using (resolver.NewScopedParameter("fastBuildCopyDependencies", Dependencies.Count > 0 ? UtilityMethods.FBuildFormatList(Dependencies.ToList(), 28) : FileGeneratorUtilities.RemoveLineTag))
                {
                    return resolver.Resolve(Bff.Template.ConfigurationFile.CopyFileSection);
                }
            }
        }

        internal class CopyDirNode : BffDependentNode
        {
            public string Source;
            public string Destination;
            public bool Recurse;
            public string FilePattern;

            public CopyDirNode(string buildStepKey, Project.Configuration.BuildStepCopy buildStep)
            {
                Identifier = buildStepKey;
                Source = buildStep.SourcePath;
                Destination = buildStep.DestinationPath;
                Recurse = buildStep.IsRecurse;
                FilePattern = buildStep.CopyPattern;

                if (buildStep.Mirror)
                    throw new Exception("Copy build step with the '{nameof(Project.Configuration.BuildStepCopy.Mirror)}' option enabled is not supported in a FastBuild context");
            }

            public override string Resolve(string rootPath, string bffFilePath, Resolver resolver)
            {
                var normalizedSource = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Source);
                var normalizedDestination = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Destination);

                using (resolver.NewScopedParameter("fastBuildCopyDirName", Identifier))
                using (resolver.NewScopedParameter("fastBuildCopyDirSourcePath", Util.EnsureTrailingSeparator(normalizedSource)))
                using (resolver.NewScopedParameter("fastBuildCopyDirDestinationPath", Util.EnsureTrailingSeparator(normalizedDestination)))
                using (resolver.NewScopedParameter("fastBuildCopyDirRecurse", Recurse.ToString().ToLower()))
                using (resolver.NewScopedParameter("fastBuildCopyDirPattern", UtilityMethods.GetBffFileCopyPattern(FilePattern)))
                using (resolver.NewScopedParameter("fastBuildCopyDirDependencies", Dependencies.Count > 0 ? UtilityMethods.FBuildFormatList(Dependencies.ToList(), 42) : FileGeneratorUtilities.RemoveLineTag))
                {
                    return resolver.Resolve(Bff.Template.ConfigurationFile.CopyDirSection);
                }
            }
        }

        internal class TestNode : BffDependentNode
        {
            public string Executable;
            public string WorkingDir;
            public string Output;
            public string Arguments;
            public int TimeOutInSeconds;
            public bool AlwaysShowOutput;

            public TestNode(string buildStepKey, Project.Configuration.BuildStepTest buildStep)
            {
                Identifier = buildStepKey;
                Executable = buildStep.TestExecutable;
                WorkingDir = buildStep.TestWorkingDir;
                Output = buildStep.TestOutput;
                Arguments = buildStep.TestArguments;
                TimeOutInSeconds = buildStep.TestTimeOutInSecond;
                AlwaysShowOutput = buildStep.TestAlwaysShowOutput;
            }

            public override string Resolve(string rootPath, string bffFilePath, Resolver resolver)
            {
                var normalizedExecutable = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Executable);
                var normalizedWorkingDir = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, WorkingDir);
                var normalizedOutput = UtilityMethods.GetNormalizedPathForBuildStep(rootPath, bffFilePath, Output);

                using (resolver.NewScopedParameter("fastBuildTest", Identifier))
                using (resolver.NewScopedParameter("fastBuildTestExecutable", normalizedExecutable))
                using (resolver.NewScopedParameter("fastBuildTestWorkingDir", normalizedWorkingDir))
                using (resolver.NewScopedParameter("fastBuildTestOutput", normalizedOutput))
                using (resolver.NewScopedParameter("fastBuildTestArguments", string.IsNullOrWhiteSpace(Arguments) ? FileGeneratorUtilities.RemoveLineTag : Arguments))
                using (resolver.NewScopedParameter("fastBuildTestTimeOut", TimeOutInSeconds == 0 ? FileGeneratorUtilities.RemoveLineTag : TimeOutInSeconds.ToString()))
                using (resolver.NewScopedParameter("fastBuildTestAlwaysShowOutput", AlwaysShowOutput.ToString().ToLower()))
                using (resolver.NewScopedParameter("fastBuildTestPreBuildDependencies", Dependencies.Count > 0 ? UtilityMethods.FBuildFormatList(Dependencies.ToList(), 27) : FileGeneratorUtilities.RemoveLineTag))
                {
                    return resolver.Resolve(Bff.Template.ConfigurationFile.TestSection);
                }
            }
        }

        /// <summary>
        /// This class is used as a helper for generating fastbuild commands for vcxproj and xcode projects. 
        /// By default is uses the default fastbuild executable defined in FastBuildSettings but scripts could define another class to 
        /// to launch a totally different launcher with other parameters.
        /// </summary>
        public class FastBuildDefaultCommandGenerator : FastBuildMakeCommandGenerator
        {
            public override string GetExecutablePath(Sharpmake.Project.Configuration conf)
            {
                string fastbuildMakeCommand = FastBuildSettings.FastBuildMakeCommand;
                if (fastbuildMakeCommand != null)
                {
                    if (!Path.IsPathRooted(FastBuildSettings.FastBuildMakeCommand))
                        fastbuildMakeCommand = Path.Combine(conf.Project.RootPath, FastBuildSettings.FastBuildMakeCommand);
                    fastbuildMakeCommand = Util.SimplifyPath(fastbuildMakeCommand);
                }
                return fastbuildMakeCommand ?? "<Please define FastBuildSettings.FastBuildMakeCommand>";
            }

            public override string GetArguments(BuildType buildType, Sharpmake.Project.Configuration conf, string fastbuildArguments)
            {
                // Note: XCode is special, the target identifier is written in the xcode project file for each target using the FASTBUILD_TARGET special variable.
                string targetIdentifier = "";
                if (!conf.Target.TryGetFragment<DevEnv>(out DevEnv devEnv) || devEnv != DevEnv.xcode)
                {
                    targetIdentifier = GetTargetIdentifier(conf);
                }

                string buildCommand = buildType == BuildType.Rebuild ? " -clean" : "";

                return $@"{buildCommand} {targetIdentifier} {fastbuildArguments}";
            }

            public override string GetTargetIdentifier(Sharpmake.Project.Configuration conf)
            {
                return Bff.GetShortProjectName(conf.Project, conf);
            }
        }

        public interface IUnityResolver
        {
            void ResolveUnities(Project project, string projectPath, ref Dictionary<Unity, List<Project.Configuration>> unities);
        }

        // Unity file resolver based on the hash of the property values of the Unity object.
        // Pros: simple and fast.
        // Cons: - Output names are not readable.
        //       - Also resulting unity names are sensitive to small changes, which can affect build determinism between computers.
        //         Example: Build Machines may deactivate UnityInputIsolateWritableFiles, resulting in a different name than for dev machines.
        public class HashUnityResolver : IUnityResolver
        {
            public void ResolveUnities(Project project, string projectPath, ref Dictionary<Unity, List<Project.Configuration>> unities)
            {
                string projectRelativePath = Util.PathGetRelative(project.RootPath, projectPath, true);
                int projectRelativePathHash = projectRelativePath.GetDeterministicHashCode();

                foreach (var unitySection in unities)
                {
                    var unity = unitySection.Key;
                    var unityConfigurations = unitySection.Value;

                    // Don't use Object.GetHashCode() on a int[] object from GetMergedFragmentValuesAcrossConfigurations() as it is
                    // non-deterministic and depends on order of execution.
                    int hashcode = unity.GetHashCode() ^ projectRelativePathHash ^ string.Join("_", unityConfigurations).GetDeterministicHashCode();

                    unity.UnityName = $"{project.Name}_unity_{hashcode:X8}";
                    unity.UnityOutputPattern = unity.UnityName.ToLower() + "*.cpp";
                }
            }
        }

        // Unity file resolver based on the fragments of the configurations that share it.
        // The fragment names that are not relevant are discarded from the output name.
        // Pros: - User friendly names.
        //       - Independent from the content of the Unity object.
        // Cons: - Output name length can be come long if many fragment values are involved.
        //       - A bit complex, involving smartness to discard fragments.
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

                        AppendFragmentUnityName(fragmentsInfos[i], unityFragments[i], ref fragmentString);
                    }
                    unity.UnityName = project.Name + fragmentString + "_unity";
                    unity.UnityOutputPattern = unity.UnityName.ToLower() + "*.cpp";
                }
            }
        }

        // Unity file resolver based on the hash of the fragments of the configurations that share it.
        // It is simpler/faster than FragmentUnityResolver, because it doesn't need to discard useless fragments.
        // Pros: - Relatively simple and fast.
        //       - Independent from the content of the Unity object.
        //       - More deterministic than FragmentUnityResolver.
        //         Discarding of useless fragments, which could affect determinism when adding/removing unrelated targets.
        // Cons: - Names are not user friendly.
        public class FragmentHashUnityResolver : IUnityResolver
        {
            public void ResolveUnities(Project project, string projectPath, ref Dictionary<Unity, List<Project.Configuration>> unities)
            {
                // we need to compute the missing member values in the Unity objects:
                // UnityName and UnityOutputPattern

                List<FieldInfo> fragmentsInfos = null;

                // merge the fragment values of all configurations sharing a unity
                foreach (var unitySection in unities)
                {
                    var unity = unitySection.Key;
                    var configurations = unitySection.Value;

                    var fragmentValuesPerConfig = configurations.Select(x => x.Target.GetFragmentsValue()).ToList();
                    var unityFragments = GetMergedFragments(fragmentValuesPerConfig);

                    // get the fragment info from the first configuration target,
                    // which works as they all share the same Target type
                    if (fragmentsInfos == null)
                        fragmentsInfos = new List<FieldInfo>(configurations.First().Target.GetFragmentFieldInfo());

                    string fragmentString = string.Empty;
                    for (int i = 0; i < unityFragments.Length; ++i)
                        AppendFragmentUnityName(fragmentsInfos[i], unityFragments[i], ref fragmentString);

                    int hashcode = fragmentString.ToLowerInvariant().GetDeterministicHashCode();
                    unity.UnityName = $"{project.Name}_unity_{hashcode:X8}";
                    unity.UnityOutputPattern = unity.UnityName.ToLower() + "*.cpp";
                }
            }
        }

        private static void AppendFragmentUnityName(FieldInfo fragmentFieldInfo, int unityFragment, ref string fragmentString)
        {
            // Convert from int to the fragment enum type, so we can ToString() them.
            // Fragments are enums by contract, so Enum.ToObject works
            var typedFragment = Enum.ToObject(fragmentFieldInfo.FieldType, unityFragment);

            if (typedFragment is Platform platformFragment)
            {
                foreach (Platform platformEnum in Enum.GetValues(typeof(Platform)))
                {
                    if (!platformFragment.HasFlag(platformEnum))
                        continue;

                    fragmentString += "_" + SanitizeForUnityName(Util.GetSimplePlatformString(platformEnum)).ToLower();
                }
            }
            else
            {
                fragmentString += "_" + SanitizeForUnityName(typedFragment.ToString());
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

    public static class UtilityMethods
    {
        internal static string GetFBuildCompilerFamily(this CompilerFamily compilerFamily)
        {
            switch (compilerFamily)
            {
                case CompilerFamily.Auto:
                    return string.Empty;
                case CompilerFamily.MSVC:
                    return "msvc";
                case CompilerFamily.Clang:
                    return "clang";
                case CompilerFamily.GCC:
                    return "gcc";
                case CompilerFamily.SNC:
                    return "snc";
                case CompilerFamily.CodeWarriorWii:
                    return "codewarrior-wii";
                case CompilerFamily.GreenHillsWiiU:
                    return "greenhills-wiiu";
                case CompilerFamily.CudaNVCC:
                    return "cuda-nvcc";
                case CompilerFamily.QtRCC:
                    return "qt-rcc";
                case CompilerFamily.VBCC:
                    return "vbcc";
                case CompilerFamily.OrbisWavePsslc:
                    return "orbis-wave-psslc";
                case CompilerFamily.ClangCl:
                    return "clang-cl";
                case CompilerFamily.CSharp:
                    return "csharp";
                case CompilerFamily.Custom:
                    return "custom";
                default:
                    throw new Exception("Unrecognized compiler family");
            }
        }

        internal static string GetFBuildLinkerType(this CompilerSettings.LinkerType linkerType)
        {
            switch (linkerType)
            {
                case CompilerSettings.LinkerType.CodeWarriorLd:
                    return "codewarrior-ld";
                case CompilerSettings.LinkerType.GCC:
                    return "gcc";
                case CompilerSettings.LinkerType.GreenHillsExlr:
                    return "greenhills-exlr";
                case CompilerSettings.LinkerType.MSVC:
                    return "msvc";
                case CompilerSettings.LinkerType.ClangOrbis:
                    return "clang-orbis";
                case CompilerSettings.LinkerType.SNCPS3:
                    return "snc-ps3";
                case CompilerSettings.LinkerType.Auto:
                    return string.Empty;
                default:
                    throw new Exception("Unrecognized linker type");
            }
        }

        internal static bool TestPlatformFlags(this UniqueList<Platform> platforms, Platform platformFlags)
        {
            return platforms.Any(platform => platformFlags.HasFlag(platform));
        }

        internal static bool IsSupportedFastBuildPlatform(this Platform platform)
        {
            return PlatformRegistry.Has<IPlatformBff>(platform);
        }

        internal static bool IsFastBuildEnabledProjectConfig(this Project.Configuration conf)
        {
            return conf.IsFastBuild && conf.Platform.IsSupportedFastBuildPlatform() && !conf.DoNotGenerateFastBuild;
        }

        internal static string GetFastBuildCopyAlias(string sourceFileName, string destinationFolder)
        {
            string fastBuildCopyAlias = string.Format("Copy_{0}_{1}", sourceFileName, (destinationFolder + sourceFileName).GetDeterministicHashCode().ToString("X8"));
            return fastBuildCopyAlias;
        }

        internal static string GetBffFileCopyPattern(string copyPattern)
        {
            if (string.IsNullOrEmpty(copyPattern))
                return FileGeneratorUtilities.RemoveLineTag;

            string[] patterns = copyPattern.Split(null);

            if (patterns == null || patterns.Length < 2)
                return "'" + copyPattern + "'";

            return "{ " + string.Join(", ", patterns.Select(p => "'" + p + "'")) + " }";
        }

        internal static string GetFastBuildCommandLineArguments(this Project.Configuration conf)
        {
            // FastBuild command line
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
            if (conf.FastBuildCacheAllowed)
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

            if (FastBuildSettings.FastBuildDistribution && conf.FastBuildDistribution)
                fastBuildCommandLineOptions.Add("-dist");

            if (FastBuildSettings.FastBuildWait)
                fastBuildCommandLineOptions.Add("-wait");

            if (FastBuildSettings.FastBuildNoStopOnError)
                fastBuildCommandLineOptions.Add("-nostoponerror");

            if (FastBuildSettings.FastBuildFastCancel)
                fastBuildCommandLineOptions.Add("-fastcancel");

            if (FastBuildSettings.FastBuildNoUnity)
                fastBuildCommandLineOptions.Add("-nounity");

            if (!string.IsNullOrEmpty(conf.FastBuildCustomArgs))
                fastBuildCommandLineOptions.Add(conf.FastBuildCustomArgs);

            if (!string.IsNullOrEmpty(FastBuildSettings.FastBuildCustomArguments))
                fastBuildCommandLineOptions.Add(FastBuildSettings.FastBuildCustomArguments);

            string commandLine = string.Join(" ", fastBuildCommandLineOptions);
            return commandLine;
        }

        internal static List<Project.Configuration> GetOrderedFlattenedProjectDependencies(Project.Configuration conf, bool allDependencies = true, bool fuDependencies = false)
        {
            var dependencies = new UniqueList<Project.Configuration>();
            GetOrderedFlattenedProjectDependenciesInternal(conf, dependencies, allDependencies, fuDependencies);
            return dependencies.OrderBy(c => c.ProjectGuid).ToList();
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

        internal static List<Project.Configuration> GetOrderedFlattenedBuildOnlyDependencies(Project.Configuration conf)
        {
            var dependencies = new UniqueList<Project.Configuration>();
            GetOrderedFlattenedBuildOnlyDependenciesInternal(conf, dependencies);
            return dependencies.OrderBy(c => c.ProjectGuid).ToList();
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

        internal static string FBuildCollectionFormat(Strings collection, int spaceLength, Strings includedExtensions = null)
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

        internal static string FBuildFormatSingleListItem(string item)
        {
            return string.Format("'{0}'", item);
        }

        /// <summary>
        /// Format list options. Can combine many of them.
        /// </summary>
        public enum FBuildFormatListOptions
        {
            /// <summary>
            /// No formatting option
            /// </summary>
            None = 0,
            /// <summary>
            /// Quote Items ?
            /// </summary>
            QuoteItems = 1 << 0,
            /// <summary>
            /// Use single element short format ?
            /// </summary>
            UseSingleElementShortFormat = 1 << 1,
            /// <summary>
            /// Use Comma between each element ? 
            /// </summary>
            UseCommaBetweenElements = 1 << 2,
        }

        /// <summary>
        /// Build a list of string in the format of BFF array, on multiple lines if needed, indenting using spaceLength spaces.
        /// </summary>
        /// <param name="items">The list of values to put in the BFF array.</param>
        /// <param name="spaceLength">The indentation size, in spaces, in case multiple lines are generated.</param>
        /// <param name="options">output options</param>
        /// <returns>The formatted string, or <see cref="FileGeneratorUtilities.RemoveLineTag"/> if the list is empty.</returns>
        public static string FBuildFormatList(List<string> items, int spaceLength, FBuildFormatListOptions options = FBuildFormatListOptions.QuoteItems | FBuildFormatListOptions.UseSingleElementShortFormat | FBuildFormatListOptions.UseCommaBetweenElements)
        {
            if (items.Count == 0)
                return FileGeneratorUtilities.RemoveLineTag;

            if (options.HasAnyFlag(FBuildFormatListOptions.UseSingleElementShortFormat))
            {
                if (items.Count == 1)
                    return FBuildFormatSingleListItem(items.First());
            }

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
                if (options.HasAnyFlag(FBuildFormatListOptions.QuoteItems))
                    strBuilder.AppendFormat("{0}    '{1}'", indent, item);
                else
                    strBuilder.AppendFormat("{0}    {1}", indent, item);

                if (++itemIndex < items.Count && options.HasAnyFlag(FBuildFormatListOptions.UseCommaBetweenElements))
                    strBuilder.AppendLine(",");
                else
                    strBuilder.AppendLine();
            }
            strBuilder.AppendFormat("{0}}}", indent);

            return strBuilder.ToString();
        }

        internal static void WriteCustomBuildStepAsGenericExecutable(string projectRoot, FileGenerator bffGenerator, Project.Configuration.CustomFileBuildStep buildStep, Func<Project.Configuration.CustomFileBuildStepData, bool> functor)
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
            using (bffGenerator.Declare("fastBuildExecPreBuildDependencies", FileGeneratorUtilities.RemoveLineTag))
            using (bffGenerator.Declare("fastBuildExecAlways", FileGeneratorUtilities.RemoveLineTag))
            {
                functor(relativeBuildStep);
            }
        }

        internal static void WriteConfigCustomBuildStepsAsGenericExecutable(string projectRoot, FileGenerator bffGenerator, Project project, Project.Configuration config, Func<Project.Configuration.CustomFileBuildStepData, bool> functor)
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

        internal static bool HasFastBuildConfig(List<Solution.Configuration> configurations)
        {
            bool hasFastBuildConfig = configurations.Any(
                x => x.IncludedProjectInfos.Any(
                    y => IsFastBuildEnabledProjectConfig(y.Configuration)
                )
            );
            return hasFastBuildConfig;
        }

        internal static string GetNormalizedPathForBuildStep(string projectRootPath, string projectFolderPath, string path)
        {
            if (string.IsNullOrEmpty(path))
                return FileGeneratorUtilities.RemoveLineTag;

            if (path.StartsWith(projectRootPath, StringComparison.OrdinalIgnoreCase))
                return Bff.CurrentBffPathKeyCombine(Util.PathGetRelative(projectFolderPath, path));

            // keep the full path for the source if outside of the global root
            return path;
        }

        private static Bff.BffNodeBase GetBffNodesFromBuildStep(string buildStepKey, Project.Configuration.BuildStepBase buildStep, Strings dependencies)
        {
            Bff.BffDependentNode node = null;

            if (buildStep is Project.Configuration.BuildStepExecutable)
            {
                node = new Bff.ExecNode(buildStepKey, buildStep as Project.Configuration.BuildStepExecutable);
            }

            if (buildStep is Project.Configuration.BuildStepCopy)
            {
                var copyStep = buildStep as Project.Configuration.BuildStepCopy;

                if (copyStep.IsFileCopy)
                {
                    node = new Bff.CopyNode(buildStepKey, copyStep);
                }
                else
                {
                    node = new Bff.CopyDirNode(buildStepKey, copyStep);
                }
            }

            if (buildStep is Project.Configuration.BuildStepTest)
            {
                node = new Bff.TestNode(buildStepKey, buildStep as Project.Configuration.BuildStepTest);
            }

            if (node == null)
            {
                throw new Error("BuildStep not supported: {0}", buildStep.GetType().FullName);
            }

            node.Dependencies.AddRange(dependencies);

            return node;
        }

        internal static List<Bff.BffNodeBase> GetBffNodesFromBuildSteps(Dictionary<string, Project.Configuration.BuildStepBase> buildSteps, Strings preBuildDependencies)
        {
            var result = new List<Bff.BffNodeBase>();

            foreach (var buildStep in buildSteps.OrderBy(kvp => kvp.Value))
            {
                // If CompareTo between two BuildSteps does not return 0, we create an explicit dependency to enforce execution order.
                var dependencySteps = buildSteps.Where(kvp => kvp.Value.CompareTo(buildStep.Value) < 0).Select(kvp => kvp.Key);

                Strings nodePreBuildDependencies = new Strings(preBuildDependencies);
                nodePreBuildDependencies.AddRange(dependencySteps);

                result.Add(GetBffNodesFromBuildStep(buildStep.Key, buildStep.Value, nodePreBuildDependencies));
            }

            return result;
        }
    }
}
