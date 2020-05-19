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
                public enum PlatformRemoteTool
                {
                    Gpp, //g++
                    [Default]
                    Clang38
                }
            }

            public static class Compiler
            {
                public enum GenerateDebugInformation
                {
                    [Default]
                    Enable,
                    Disable
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
