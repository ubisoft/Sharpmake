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
    }
}
