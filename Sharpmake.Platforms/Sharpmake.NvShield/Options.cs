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
    public static partial class NvShield
    {
        public static class Options
        {
            public static class Compiler
            {
                // General
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
                    AllWarnings,
                    Disable
                }

                public enum EchoCommandLines
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum EchoIncludedHeaders
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

                public enum StrictAliasing
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum UnswitchLoops
                {
                    [Default]
                    Enable,
                    Disable
                }

                public class InlineLimit
                {
                    public int Value;
                    public InlineLimit(int value)
                    {
                        Value = value;
                    }
                }

                public enum OmitFramePointers
                {
                    [Default(DefaultTarget.Release)]
                    Enable,
                    [Default(DefaultTarget.Debug)]
                    Disable
                }

                public enum FunctionSections
                {
                    Enable,
                    [Default]
                    Disable
                }

                // Code Generation
                public enum ThumbMode
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum FloatingPointABI
                {
                    Hard,
                    [Default]
                    Soft
                }

                public enum GeneratePositionIndependentCode
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum StackProtection
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum EnableAdvancedSIMD
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum Exceptions
                {
                    Enable,
                    [Default]
                    Disable
                }

                // Language
                public enum ShortEnums
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum DefaultCharUnsigned
                {
                    [Default]
                    Default,
                    Enable,
                    Disable
                }

                public enum CLanguageStandard
                {
                    [Default]
                    Default,
                    C89,
                    C95,
                    C99,
                    C11,
                    GnuC89,
                    GnuC99,
                    GnuC11
                }

                public enum CPPLanguageStandard
                {
                    [Default]
                    Default,
                    CPP98,
                    CPP11,
                    CPP1y,
                    GnuCPP98,
                    GnuCPP11,
                    GnuCPP1y
                }
            }

            public static class Linker
            {
                // General
                public enum EchoCommandLines
                {
                    Enable,
                    [Default]
                    Disable
                }

                // Input
                public class AndroidSystemLibraries : Strings
                {
                    public AndroidSystemLibraries(params string[] values)
                        : base(values)
                    { }
                }

                public enum LinkAgainstThumbVersionOfLibGcc
                {
                    Enable,
                    [Default]
                    Disable
                }

                // Advanced
                public enum ReportUndefinedSymbols
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum LinkerType
                {
                    [Default]
                    Bfd,
                    Gold
                }

                public enum ThinArchive
                {
                    Enable,
                    [Default]
                    Disable
                }
            }
        }
    }
}
