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

namespace Sharpmake.Generators.FastBuild
{
    partial class Bff
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
                if(ReferenceEquals(null, obj)) return false;
                if(ReferenceEquals(this, obj)) return true;
                if(obj.GetType() != GetType()) return false;

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

        // This makefile command generator is for supporting legacy code without any client code change.
        internal class FastBuildDefaultMakeCommandGenerator : FastBuildMakeCommandGenerator
        {
            public override string GetCommand(BuildType buildType, Sharpmake.Project.Configuration conf, string fastbuildArguments)
            {
                Project project = conf.Project;
                string fastBuildShortProjectName = Bff.GetShortProjectName(project, conf);
                string fastBuildExecutable = Bff.GetFastBuildExecutableRelativeToMasterBffPath(conf);

                string rebuildCmd = buildType == BuildType.Rebuild ? " -clean" : "";

                return $"{fastBuildExecutable}{rebuildCmd} {fastBuildShortProjectName} {fastbuildArguments}";
            }
        }

        public interface IUnityResolver
        {
            void ResolveUnities(Project project, ref Dictionary<Unity, List<Project.Configuration>> unities);
        }

        public class HashUnityResolver : IUnityResolver
        {
            public void ResolveUnities(Project project, ref Dictionary<Unity, List<Project.Configuration>> unities)
            {
                foreach (var unityFile in unities)
                {
                    var unity = unityFile.Key;

                    unity.UnityName = $"{project.Name}_unity_{unity.GetHashCode():X8}";
                    unity.UnityOutputPattern = unity.UnityName.ToLower() + "*.cpp";
                }
            }
        }

        public class FragmentUnityResolver : IUnityResolver
        {
            public void ResolveUnities(Project project, ref Dictionary<Unity, List<Project.Configuration>> unities)
            {
                // we need to compute the missing member values in the Unity objects:
                // UnityName and UnityOutputPattern
                var masks = new List<Tuple<Unity, int[]>>();

                List<FieldInfo> fragmentsInfos = null;

                // first we merge the fragment values of all configurations sharing a unity
                foreach (var unityFile in unities)
                {
                    var configurations = unityFile.Value;

                    // get the fragment info from the first configuration target,
                    // which works as they all share the same Target type
                    if (fragmentsInfos == null)
                        fragmentsInfos = new List<FieldInfo>(configurations.First().Target.GetFragmentFieldInfo());

                    var fragments = configurations.Select(x => x.Target.GetFragmentsValue()).ToList();
                    var merged = fragments[0];
                    for (int i = 1; i < fragments.Count; ++i)
                    {
                        var toMerge = fragments[i];
                        for (int j = 0; j < toMerge.Length; ++j)
                            merged[j] |= toMerge[j];
                    }
                    masks.Add(Tuple.Create(unityFile.Key, merged));
                }

                // then, figure out which fragments are different *across* unities
                var differentFragmentIndices = new UniqueList<int>();
                var fragmentValuesComparisonBase = masks[0].Item2;
                for (int i = 1; i < masks.Count; ++i)
                {
                    var fragmentValues = masks[i].Item2;
                    for (int j = 0; j < fragmentValues.Length; ++j)
                    {
                        if (fragmentValuesComparisonBase[j] != fragmentValues[j])
                            differentFragmentIndices.Add(j);
                    }
                }

                // finally, create a unity name that only contains the varying fragments
                foreach (var unityFile in masks)
                {
                    var unity = unityFile.Item1;
                    var fragments = unityFile.Item2;

                    string fragmentString = string.Empty;
                    for (int i = 0; i < fragments.Length; ++i)
                    {
                        // if not a differentiating fragment, skip
                        if (!differentFragmentIndices.Contains(i))
                            continue;

                        // Convert from int to the fragment enum type, so we can ToString() them.
                        // Fragments are enums by contract, so Enum.ToObject works
                        var typedFragment = Enum.ToObject(fragmentsInfos[i].FieldType, fragments[i]);

                        fragmentString += "_" + typedFragment.ToString().Replace(",", "").Replace(" ", "");
                    }
                    unity.UnityName = project.Name + fragmentString + "_unity";
                    unity.UnityOutputPattern = unity.UnityName.ToLower() + "*.cpp";
                }
            }
        }
    }

    internal static class UtilityMethods
    {
        public static bool TestPlatformFlags(this UniqueList<Platform> platforms, Platform platformFlags)
        {
            return platforms.Any(platform => platformFlags.HasFlag(platform));
        }
    }
}
