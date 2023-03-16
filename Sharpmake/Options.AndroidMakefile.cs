// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
