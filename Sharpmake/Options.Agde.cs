// Copyright (c) 2022 Ubisoft Entertainment
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
        public static class Agde
        {
            public static class General
            {
                /// <summary>
                /// Name of the gradle app folder/module
                /// </summary>
                public class AndroidApplicationModule : StringOption
                {
                    public AndroidApplicationModule(string androidApplicationModule) : base(androidApplicationModule) { }
                }

                /// <summary>
                /// The full path to the directory containing the top-level build.gradle file.
                /// </summary>
                public class AndroidGradleBuildDir : PathOption
                {
                    public AndroidGradleBuildDir(string androidGradleBuildDir)
                       : base(androidGradleBuildDir) { }
                }

                /// <summary>
                /// Intermediate directory of the gradle build process
                /// </summary>
                public class AndroidGradleBuildIntermediateDir : PathOption
                {
                    public AndroidGradleBuildIntermediateDir(string androidGradleBuildIntermediateDir)
                       : base(androidGradleBuildIntermediateDir) { }
                }

                /// <summary>
                /// Output Extra Gradle Arguments for AGDE project which can be set per configuration.
                /// </summary>
                public class AndroidExtraGradleArgs : StringOption
                {
                    public AndroidExtraGradleArgs(string androidExtraGradleArgs)
                       : base(androidExtraGradleArgs) { }
                }

                /// <summary>
                /// Output Apk name for AGDE project which can be set per configuration.
                /// </summary>
                public class AndroidApkName : StringOption
                {
                    public AndroidApkName(string androidApkName)
                       : base(androidApkName) { }
                }

                /// <summary>
                /// The apk file used for debugging which can be set per configuration, is usually for FastBuild configuration.
                /// </summary>
                public class AndroidApkLocation: PathOption
                {
                    public AndroidApkLocation(string androidApkLocation) : base(androidApkLocation) { }
                }

                // This is applicable for arm architecture only
                public enum ThumbMode
                {
                    Thumb,
                    ARM,
                    [Default]
                    Disabled
                }

                public enum UseOfStl
                {
                    GnuStl_Static,
                    GnuStl_Shared,
                    LibCpp_Static,
                    [Default]
                    LibCpp_Shared
                }

                // Link time optimization, may also be required for some sanitizers.
                public enum LinkTimeOptimization
                {
                    [Default(DefaultTarget.Debug)]
                    None,
                    [Default(DefaultTarget.Release)]
                    LinkTimeOptimization,
                    ThinLinkTimeOptimization
                }

                // Set the flag '-fuse-ld=' which specifies which linker to use.
                public enum ClangLinkType
                {
                    DeferToNdk,
                    gold,
                    [Default]
                    lld,
                    bfd
                }

                public enum WarningLevel
                {
                    Default,
                    TurnOffAllWarnings,
                    FormatWarnings,
                    FormatAndSecurityWarnings,
                    [Default]
                    EnableAllWarnings,
                    ExtraWarnings,
                    ExhaustiveWarnings
                }
                public enum TreatWarningsAsErrors
                {
                    Enable,
                    [Default]
                    Disable
                }
            }

            public static class Compiler
            {
                /// <summary>
                /// Multi-processor Compilation
                /// </summary>
                public enum MultiProcessorCompilation
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum CLanguageStandard
                {
                    [Default]
                    Default,
                    C89,
                    C99,
                    C11,
                    C17,
                    GNU_C99,
                    GNU_C11,
                    GNU_C17
                }

                public enum CppLanguageStandard
                {
                    [Default]
                    Default,
                    Cpp98,
                    Cpp03,
                    Cpp11,
                    Cpp14,
                    Cpp1z,
                    Cpp17,
                    GNU_Cpp98,
                    GNU_Cpp03,
                    GNU_Cpp11,
                    GNU_Cpp14,
                    GNU_Cpp1z,
                    GNU_Cpp17
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
                    [Default(DefaultTarget.Release)]
                    None,
                    [Default(DefaultTarget.Debug)]
                    FullDebug,
                    LineNumber
                }

                public enum Exceptions
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum UnwindTables
                {
                    [Default]
                    Yes,
                    No
                }

                public enum AddressSignificanceTable
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum ClangDiagnosticsFormat
                {
                    [Default]
                    Default,
                    MSVC
                }

                public enum StackProtectionLevel
                {
                    None,
                    Basic,
                    [Default]
                    Strong,
                    All,
                    Default
                }

                /// <summary>
                /// Enable Function-Level Linking
                /// </summary>
                /// <remarks>
                /// Allows the compiler to package individual functions in the form of packaged functions (COMDATs). Required for edit and continue to work.
                /// </remarks>
                public enum FunctionLevelLinking
                {
                    [Default(DefaultTarget.Debug)]
                    Disable,
                    [Default(DefaultTarget.Release)]
                    Enable,
                }

                /// <summary>
                /// Omit Frame Pointers
                /// </summary>
                /// <remarks>
                /// Suppresses creation of frame pointers on the call stack.
                /// </remarks>
                public enum OmitFramePointers
                {
                    [Default]
                    Disable,
                    Enable
                }

                /// <summary>
                /// Select option for code optimization; choose Custom to use specific optimization options.
                /// </summary>
                public enum Optimization
                {
                    /// <summary>
                    /// Disable optimization.
                    /// </summary>
                    [Default(DefaultTarget.Debug)]
                    Disable,

                    /// <summary>
                    /// Maximum Optimization (Favor Size)
                    /// </summary>
                    /// <remarks>
                    /// Equivalent to /Os
                    /// </remarks>
                    MinimizeSize,

                    /// <summary>
                    /// Maximum Optimization (Favor Speed)
                    /// </summary>
                    /// <remarks>
                    /// Equivalent to /O2
                    /// </remarks>
                    MaximizeSpeed,

                    /// <summary>
                    /// Optimizations
                    /// </summary>
                    /// <remarks>
                    /// Equivalent to /O3
                    /// </remarks>
                    [Default(DefaultTarget.Release)]
                    FullOptimization
                }

                /// <summary>
                /// Enable Run-Time Type Information
                /// </summary>
                /// <remarks>
                /// Adds code for checking C++ object types at run time (runtime type information).
                /// </remarks>
                public enum RTTI
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum PositionIndependentCode
                {
                    Disable,
                    [Default]
                    Enable
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

                public enum EnableImmediateFunctionBinding
                {
                    [Default]
                    Yes,
                    No
                }

                public enum ExecutableStackRequired
                {
                    Yes,
                    [Default]
                    No
                }

                public enum ReportUnresolvedSymbolReference
                {
                    [Default]
                    Yes,
                    No
                }

                public enum VariableReadOnlyAfterRelocation
                {
                    [Default]
                    Yes,
                    No
                }

                public enum UseThinArchives
                {
                    Enable,
                    [Default]
                    Disable
                }

                /// <summary>
                /// Enable Incremental Linking
                /// </summary>
                public enum Incremental
                {
                    [Default]
                    Disable,
                    Enable,
                }
            }
        }
    }
}
