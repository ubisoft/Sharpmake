// Copyright (c) 2020, 2022 Ubisoft Entertainment
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
using static Sharpmake.Options;

namespace Sharpmake
{
    public static partial class Linux
    {
        public static class Options
        {
            public static class General
            {
                public enum CopySources
                {
                    Enable,
                    [Default]
                    Disable
                }

                /// <summary>
                /// VC Platform Toolset
                /// </summary>
                /// <remarks>
                /// Specifies which build tools will be used for the project in Visual Studio
                /// </remarks>
                public enum VcPlatformToolset
                {
                    [Default]
                    Default,

                    /// <summary>
                    /// GCC for Remote Linux
                    /// </summary>
                    Remote_GCC_1_0,

                    /// <summary>
                    /// Clang for Remote Linux
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    Remote_Clang_1_0,

                    /// <summary>
                    /// GCC for Windows Subsystem for Linux
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    WSL_1_0,

                    /// <summary>
                    /// Clang for Windows Subsystem for Linux
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    WSL_Clang_1_0,

                    /// <summary>
                    /// WSL2 Toolset
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2022)]
                    WSL2_1_0,
                }

                public enum PlatformRemoteTool
                {
                    Gpp, //g++
                    Clang, // Alpine
                    [Default]
                    Clang38
                }
            }

            public static class Compiler
            {
                [Obsolete("Use " + nameof(DebugInformationFormat) + " instead.")]
                public enum GenerateDebugInformation
                {
                    Enable,
                    Disable
                }

                /// <summary>
                /// Controls debug information. Matches the <c>-g</c> family of compiler options.
                /// </summary>
                /// <remarks>
                /// Prefer using this switch over <seealso cref="GenerateDebugInformation"/>.
                /// </remarks>
                public enum DebugInformationFormat
                {
                    /// <summary>
                    /// No debug information at all. Corresponds to the <c>-g0</c> switch.
                    /// </summary>
                    None,

                    /// <summary>
                    /// Outputs some debug information. Corresponds to the <c>-g</c> switch.
                    /// </summary>
                    [Default] MinimalDebugInformation,

                    /// <summary>
                    /// Outputs full debug information. Corresponds to the <c>-g2 -gdwarf-2</c> switches.
                    /// </summary>
                    FullDebugInformation
                }

                public enum Warnings
                {
                    NormalWarnings,
                    [Default]
                    MoreWarnings,
                    Disable
                }

                public enum ExtraWarnings
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum InlineFunctionDebugInformation
                {
                    Enable,
                    [Default]
                    Disable
                }

                public class ProcessorNumber
                {
                    public int Value;
                    public ProcessorNumber(int value)
                    {
                        Value = value;
                    }
                }

                public enum Distributable
                {
                    [Default]
                    Enable,
                    Disable
                }

                // Optimization
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

                public enum FastMath
                {
                    Enable,
                    [Default]
                    Disable
                }

                /// <summary>
                /// Generates position-independent code, suitable for shared libraries.
                /// </summary>
                /// <remarks>
                /// The corresponding clang flags is <c>-fPIC</c>.
                /// </remarks>
                public enum PositionIndependentCode
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum NoStrictAliasing
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum UnrollLoops
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum LinkTimeOptimization
                {
                    Enable,
                    [Default]
                    Disable
                }

                // Code Generation
                // Language
                public enum CheckAnsiCompliance
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum DefaultCharUnsigned
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum MsExtensions
                {
                    Enable,
                    [Default]
                    Disable
                }
            }

            public static class Linker
            {
                /// <summary>
                /// Strip debug symbols
                /// </summary>
                /// <remarks>
                /// Whether to strip debug symbols into a separate file after a build.
                /// This may speed up debugger launch times.
                /// </remarks>
                public enum ShouldStripDebugSymbols
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum Addressing
                {
                    [Default]
                    ASLR,
                    NonASLR
                }

                public enum EditAndContinue
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum InfoStripping
                {
                    [Default]
                    None,
                    StripDebug,
                    StripSymsAndDebug
                }

                public enum DataStripping
                {
                    None,
                    StripFuncs,
                    [Default]
                    StripFuncsAndData
                }

                public enum DuplicateStripping
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum UseThinArchives
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum WholeArchive
                {
                    Enable,
                    [Default]
                    Disable
                }
            }
        }
    }
}
