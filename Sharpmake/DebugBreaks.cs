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
using System.Diagnostics;

namespace Sharpmake
{
    /// <summary>
    /// The DebugBreaks are used to help with debugging, allowing conditional breakpoints that are
    /// built-in in code.  Feel free to add more as you need them while debugging Sharpmake. 
    /// </summary>
    public static class DebugBreaks
    {
        public static bool IsEnabled;
        static DebugBreaks()
        {
            IsEnabled = Debugger.IsAttached;
        }

        [Flags]
        // Context is used to enable or disable specific breaks.
        // Feel free to add more in code for better granularity.
        public enum Context
        {
            AddMatchFiles = 0x1,
            Blobbing = 0x2,
            Resolving = 0x4,
            BlobbingResolving = 0x8,
            All = AddMatchFiles | Blobbing | Resolving | BlobbingResolving
        }

        public static Context BreakOnContext = Context.All;

        // Complete project path to break on; not case-sensitive
        public static string ProjectPath = null;

        // Complete source path to break on; not case-sensitive
        public static string SourcePath = null;

        public static string BlobPath = null;

        public delegate bool FilterProjectConfigurationDelegate(Project.Configuration conf);

        public static FilterProjectConfigurationDelegate FilterProjectConfiguration = null;

        public static string FilterProjectConfigurationStringSubStr = null;

        public static string FilterProjectConfigurationNameSubStr = null;

        public static bool ShouldBreakOnProjectConfiguration(Context context, Project.Configuration conf)
        {
            if (!IsEnabled)
                return false;
            if (!BreakOnContext.HasFlag(context))
                return false;
            if (BlobPath != null && conf.BlobPath != BlobPath)
                return false;
            if (FilterProjectConfigurationStringSubStr != null)
                return conf.ToString().Contains(FilterProjectConfigurationStringSubStr);
            if (FilterProjectConfigurationNameSubStr != null)
                return conf.Name.Contains(FilterProjectConfigurationNameSubStr);
            if (FilterProjectConfiguration != null)
                return FilterProjectConfiguration(conf);
            return false;
        }

        public static bool CanBreakOnProjectConfiguration(Project.Configuration conf)
        {
            if (!IsEnabled)
                return false;
            if (BlobPath != null && conf.BlobPath != BlobPath)
                return false;
            if (FilterProjectConfigurationStringSubStr != null)
                return conf.ToString().Contains(FilterProjectConfigurationStringSubStr);
            if (FilterProjectConfigurationNameSubStr != null)
                return conf.Name.Contains(FilterProjectConfigurationNameSubStr);
            if (FilterProjectConfiguration != null)
                return FilterProjectConfiguration(conf);
            return true;
        }

        public static bool ShouldBreakOnProjectPath(Context context, string projectPath, Project.Configuration conf)
        {
            if (!IsEnabled)
                return false;
            if (!BreakOnContext.HasFlag(context))
                return false;
            if (BlobPath != null && conf.BlobPath != BlobPath)
                return false;
            if (ProjectPath != null &&
                string.Compare(projectPath, ProjectPath, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return CanBreakOnProjectConfiguration(conf);
            }
            return false;
        }

        public static bool ShouldBreakOnProjectPath(Context context, string projectPath)
        {
            if (!IsEnabled)
                return false;
            if (!BreakOnContext.HasFlag(context))
                return false;
            if (ProjectPath != null &&
                string.Compare(projectPath, ProjectPath, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }
            return false;
        }



        public static bool ShouldBreakOnSourcePath(Context context, Strings sourcePaths, Project.Configuration conf)
        {
            if (!IsEnabled)
                return false;
            if (!BreakOnContext.HasFlag(context))
                return false;
            if (BlobPath != null && conf.BlobPath != BlobPath)
                return false;
            if (SourcePath != null && CanBreakOnProjectConfiguration(conf))
            {
                return ShouldBreakOnSourcePath(context, sourcePaths);
            }
            return false;
        }

        public static bool ShouldBreakOnSourcePath(Context context, Strings sourcePaths)
        {
            if (!IsEnabled)
                return false;
            if (!BreakOnContext.HasFlag(context))
                return false;
            if (SourcePath != null)
            {
                if (sourcePaths.Contains(SourcePath))
                    return true;
                foreach (string sourcePath in sourcePaths.Values)
                {
                    if (ShouldBreakOnSourcePath(context, sourcePath))
                        return true;
                }
            }
            return false;
        }
        public static bool ShouldBreakOnSourcePath(Context context, string sourcePath)
        {
            if (!IsEnabled)
                return false;
            if (!BreakOnContext.HasFlag(context))
                return false;
            if (SourcePath != null &&
                string.Compare(sourcePath, SourcePath, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }
            return false;
        }
    }
}
