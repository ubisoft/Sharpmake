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

namespace Sharpmake
{
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
                    Android28
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
                    Cpp14,
                    Cpp17,
                    GnuCpp98,
                    GnuCpp11,
                    GnuCpp14,
                    GnuCpp17,

                    [Obsolete("Enum value obsolue, use Cpp14 instead", error: false)]
                    Cpp1y,
                    [Obsolete("Enum value obsolue, use GnuCpp98 instead", error: false)]
                    GNU_Cpp98,
                    [Obsolete("Enum value obsolue, use GnuCpp11 instead", error: false)]
                    GNU_Cpp11,
                    [Obsolete("Enum value obsolue, use GnuCpp14 instead", error: false)]
                    GNU_Cpp1y
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
            }
        }
    }
}