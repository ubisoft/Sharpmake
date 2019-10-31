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
    using System;

    public static partial class Options
    {
        public static class Android
        {
            public static class General
            {
                public enum AndroidAPILevel
                {
                    [Default]
                    Default,
                    Android19,
                    Android21,
                    Android22,
                    Android23,
                    Android24,
                    Android25,
                    Android26,
                    Android27,
                    Android28,
                }

                public enum PlatformToolset
                {
                    [Default]
                    Default,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Clang_3_8,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Clang_5_0,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Gcc_4_9
                }

                // This is applicable for arm architecture only
                public enum ThumbMode
                {
                    [Default]
                    Default,
                    Thumb,
                    ARM,
                    Disabled
                }

                public enum UseOfStl
                {
                    [Default]
                    Default,
                    System,
                    GAbiPP_Static,
                    GAbiPP_Shared,
                    StlPort_Static,
                    StlPort_Shared,
                    GnuStl_Static,
                    GnuStl_Shared,
                    LibCpp_Static,
                    LibCpp_Shared
                }

                public enum WarningLevel
                {
                    TurnOffAllWarnings,
                    [Default]
                    EnableAllWarnings
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
                    Cpp1y,
                    Cpp14,
                    Cpp17,
                    Cpp2a,
                    GNU_Cpp98,
                    GNU_Cpp11,
                    GNU_Cpp1y,
                    GNU_Cpp14,
                    GNU_Cpp17,
                    GNU_Cpp2a
                }

                public enum DataLevelLinking
                {
                    [Default(DefaultTarget.Debug)]
                    Disable,

                    [Default(DefaultTarget.Release)]
                    Enable,
                }

                public enum DebugInformationFormat
                {
                    None,
                    [Default]
                    FullDebug,
                    LineNumber
                }

                public enum Exceptions
                {
                    [Default]
                    Disable,
                    Enable,
                    UnwindTables
                }
            }

            public static class Linker
            {
                public enum DebuggerSymbolInformation
                {
                    [Default]
                    IncludeAll,
                    OmitUnneededSymbolInformation,
                    OmitDebuggerSymbolInformation,
                    OmitAllSymbolInformation
                }

                public enum LibGroup
                {
                    [Default]
                    Disable,
                    Enable
                }
            }
        }
    }
}
