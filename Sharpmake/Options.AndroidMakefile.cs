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
namespace Sharpmake
{
    public static partial class Options
    {
        public static class AndroidMakefile
        {
            public class AppPlatform : StringOption
            {
                public AppPlatform(string option)
                    : base(option)
                {
                }
            }

            public enum ArmMode
            {
                [Default]
                Thumb,
                Arm,
            }

            public class CompilerFlags : Strings
            {
                public CompilerFlags(params string[] options)
                    : base(options)
                {
                }
            }

            public class CompilerExportedFlags : Strings
            {
                public CompilerExportedFlags(params string[] options)
                    : base(options)
                {
                }
            }

            public class ExportedDefines : Strings
            {
                public ExportedDefines(params string[] defines)
                    : base(defines)
                {
                }
            }

            public enum GroupStaticLibraries
            {
                Enable,
                [Default]
                Disable,
            }

            public class PrebuiltStaticLibraries
            {
                public PrebuiltStaticLibraries(string moduleName, string libraryPath, ArmMode armMode = ArmMode.Thumb)
                {
                    ModuleName = moduleName;
                    LibraryPath = libraryPath;
                    Mode = armMode;
                }

                public string ModuleName { get; set; }
                public string LibraryPath { get; set; }
                public ArmMode Mode { get; set; }
            }

            public enum StandardLibrary
            {
                [Default]
                System,
                GAbiPP_Static,
                GAbiPP_Shared,
                StlPort_Static,
                StlPort_Shared,
                GnuStl_Static,
                GnuStl_Shared,
            }

            public enum ShortCommands
            {
                Enable,
                [Default]
                Disable,
            }

            public class SupportedABIs : Strings
            {
                public SupportedABIs(params string[] options)
                    : base(options)
                {
                }
            }

            public class ToolchainVersion : StringOption
            {
                public ToolchainVersion(string value)
                    : base(value)
                {
                }
            }
        }
    }
}