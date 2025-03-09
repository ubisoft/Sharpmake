// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public static partial class Options
    {
        public static class Makefile
        {
            public static class General
            {
                public enum PlatformToolset
                {
                    [Default]
                    Gcc,
                    Clang
                }
            }

            public static class Compiler
            {
                public enum CppLanguageStandard
                {
                    [Default]
                    Default,
                    Cpp98,
                    Cpp11,
                    Cpp14,
                    Cpp17,
                    Cpp2a,
                    GnuCpp98,
                    GnuCpp11,
                    GnuCpp14,
                    GnuCpp17,
                    GnuCpp2a,
                }

                public enum Exceptions
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum ExtraWarnings
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum GenerateDebugInformation
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum OptimizationLevel
                {
                    [Default(DefaultTarget.Debug)]
                    Disable,
                    Standard,
                    Full,
                    [Default(DefaultTarget.Release)]
                    FullWithInlining,
                    ForSize
                }

                public enum Rtti
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum TreatWarningsAsErrors
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum Warnings
                {
                    NormalWarnings,
                    [Default]
                    MoreWarnings,
                    Disable
                }
            }

            public static class Linker
            {
                public enum LibGroup
                {
                    Enable,
                    [Default]
                    Disable
                }
            }
        }
    }
}
