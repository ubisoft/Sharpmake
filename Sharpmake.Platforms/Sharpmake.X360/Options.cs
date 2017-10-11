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
using static Sharpmake.Options;

namespace Sharpmake
{
    public static partial class X360
    {
        public static class Options
        {
            public static class Compiler
            {
                public enum CallAttributedProfiling
                {
                    [Default]
                    Disable,
                    Fastcap,
                    Callcap
                }

                public enum PreschedulingOptimization
                {
                    [Default(DefaultTarget.Debug)]
                    Disable,
                    [Default(DefaultTarget.Release)]
                    Enable
                }
            }

            public static class Linker
            {
                /// <summary>
                /// Xbox360 Image Conversion > Configuration File
                /// </summary>
                public class ProjectDefaults
                {
                    public string Value;
                    public ProjectDefaults(string xmlFile)
                    {
                        Value = xmlFile;
                    }
                }

                public enum SetChecksum
                {
                    [Default(DefaultTarget.Release)]
                    Enable,
                    [Default(DefaultTarget.Debug)]
                    Disable
                }

                public class RemotePath
                {
                    public static readonly string Default = @"devkit:\$(USERNAME)\$(SolutionName)";
                    public string Value;
                    public RemotePath(string value)
                    {
                        Value = value;
                    }
                }

                public class AdditionalDeploymentFolders
                {
                    public static readonly string Default = "";
                    public string Value;
                    public AdditionalDeploymentFolders(string value)
                    {
                        Value = value;
                    }
                }

                public class LayoutFile
                {
                    public string Value;
                    public LayoutFile(string value)
                    {
                        Value = value;
                    }
                }
            }

            public static class ImageConversion
            {
                public class AdditionalSections
                {
                    public string Value;
                    public AdditionalSections(string value)
                    {
                        Value = value;
                    }
                }

                public enum PAL50Incompatible
                {
                    Enable,
                    [Default]
                    Disable
                }
            }
        }
    }
}
