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
        public static class Vc
        {
            public static class General
            {
                public enum PlatformToolset
                {
                    [Default]
                    Default,  // same as Visual Studio version
                    [DevEnvVersion(minimum = DevEnv.vs2010)]
                    v100, // Visual Studio 2010
                    [DevEnvVersion(minimum = DevEnv.vs2012)]
                    v110, // Visual Studio 2012
                    [DevEnvVersion(minimum = DevEnv.vs2012)]
                    v110_xp, // Visual Studio 2012 - Windows XP
                    [DevEnvVersion(minimum = DevEnv.vs2013)]
                    v120, // Visual Studio 2013
                    [DevEnvVersion(minimum = DevEnv.vs2013)]
                    v120_xp, // Visual Studio 2013 - Windows XP
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    v140, // Visual Studio 2015
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    v140_xp, // Visual Studio 2015 - Windows XP
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    v141, // Visual Studio 2017
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    v141_xp, // Visual Studio 2017 - Windows XP
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    v142, // Visual Studio 2019
                    [DevEnvVersion(minimum = DevEnv.vs2012)]
                    LLVM_vs2012, // LLVM from Visual Studio 2012
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    LLVM_vs2014, // LLVM from Visual Studio 2015
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    LLVM, // LLVM from Visual Studio 2017
                }

                public enum WindowsTargetPlatformVersion
                {
                    v8_1,
                    v10_0_10240_0, // RTM (even if never named like that officially)
                    v10_0_10586_0, // November 2015 Update
                    v10_0_14393_0, // 2016 Anniversary Update
                    v10_0_15063_0, // 2017 Creators Update
                    v10_0_16299_0, // 2017 Fall Creators Update
                    v10_0_17134_0, // 1803, April 2018 Update
                    v10_0_17763_0, // 1809, October 2018 Update
                    v10_0_18362_0, // 1903, May 2019 Update
                    v10_0_19041_0, // 2004, May 2020 Update
                    Latest,        // latest available in host machine
                }

                public enum CharacterSet
                {
                    Default,
                    Unicode,
                    [Default]
                    MultiByte
                }

                public enum WholeProgramOptimization
                {
                    [Default]
                    Disable,
                    LinkTime,
                    Instrument,
                    Optimize,
                    Update
                }

                public enum DebugInformation
                {
                    Disable,
                    C7Compatible,
                    [Default]
                    ProgramDatabase,
                    ProgramDatabaseEnC
                }

                public enum UseDebugLibraries
                {
                    [Default(DefaultTarget.Debug)]
                    Enabled,
                    [Default(DefaultTarget.Release)]
                    Disabled
                }

                public enum WarningLevel
                {
                    Level0,
                    Level1,
                    Level2,
                    Level3,
                    [Default]
                    Level4,
                    EnableAllWarnings
                }

                [Obsolete("Use option TreatWarningsAsErrors instead")]
                public enum TreatWarningAsError
                {
                    Enable,
                    [Default]
                    Disable
                }


                public enum TreatWarningsAsErrors
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum DiagnosticsFormat
                {
                    [Default]
                    Classic, // default, only show the line number
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    ColumnInfo, // line + column
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    Caret // context + caret
                }

                public enum EnableManagedIncrementalBuild
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum CommonLanguageRuntimeSupport
                {
                    [Default]
                    NoClrSupport,
                    ClrSupport,  // clr
                    PureMsilClrSupport,  // clr:pure
                    SafeMsilClrSupport,  // clr:safe
                    SafeMsilClrSupportOldSyntax  // clr:oldSyntax
                }

                public enum MfcSupport
                {
                    [Default]
                    UseMfcStdWin,
                    UseMfcStatic,
                    UseMfcDynamic
                }

                [Obsolete("Please use " + nameof(PreferredToolArchitecture) + " instead", true)]
                public enum NativeEnvironment
                {
                    [DevEnvVersion(minimum = DevEnv.vs2012)]
                    Enable,
                    [Default]
                    Disable
                }

                public enum PreferredToolArchitecture
                {
                    [Default]
                    Default,
                    x86,
                    x64
                }

                public enum DisableFastUpToDateCheck
                {
                    Enable,
                    [Default]
                    Disable
                }
            }

            public static class Compiler
            {
                public enum MultiProcessorCompilation
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum Optimization
                {
                    [Default(DefaultTarget.Debug)]
                    Disable,
                    MinimizeSize,
                    MaximizeSpeed,
                    [Default(DefaultTarget.Release)]
                    FullOptimization
                }

                public enum Inline
                {
                    Default,
                    [Default(DefaultTarget.Debug)]
                    OnlyInline,  // set as debug default because good enough for debug builds and much faster.
                    [Default(DefaultTarget.Release)]
                    AnySuitable,
                    Disable
                }

                public enum Intrinsic
                {
                    [Default]
                    Enable,
                    Disable
                }

                public enum FavorSizeOrSpeed
                {
                    [Default(DefaultTarget.Debug)]
                    Neither,
                    [Default(DefaultTarget.Release)]
                    FastCode,
                    SmallCode
                }

                public enum OmitFramePointers
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum FiberSafe
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum IgnoreStandardIncludePath
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum GenerateProcessorFile
                {
                    [Default]
                    Disable,
                    WithLineNumbers,
                    WithoutLineNumbers
                }

                public enum KeepComment
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum StringPooling
                {
                    Disable,
                    [Default]
                    Enable
                }

                public enum MinimalRebuild
                {
                    [Default(DefaultTarget.Release)]
                    Disable,
                    [Default(DefaultTarget.Debug)]
                    Enable
                }

                public enum Exceptions
                {
                    [Default]
                    Disable,
                    Enable,
                    EnableWithExternC,
                    EnableWithSEH
                }

                public enum TypeChecks
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum RuntimeChecks
                {
                    [Default]
                    Default,
                    StackFrames,
                    UninitializedVariables,
                    Both
                }

                public enum RuntimeLibrary
                {
                    [Default(DefaultTarget.Release)]
                    MultiThreaded,
                    [Default(DefaultTarget.Debug)]
                    MultiThreadedDebug,
                    MultiThreadedDLL,
                    MultiThreadedDebugDLL,
                }

                public enum StructAlignment
                {
                    [Default]
                    Default,
                    Alignment1,
                    Alignment2,
                    Alignment4,
                    Alignment8,
                    Alignment16
                }

                public enum BufferSecurityCheck
                {
                    [Default(DefaultTarget.Release)]
                    Disable,
                    [Default(DefaultTarget.Debug)]
                    Enable,
                }

                public enum OptimizeGlobalData
                {
                    [Default]
                    Disable,
                    [DevEnvVersion(minimum = DevEnv.vs2013)]
                    Enable,
                }

                public enum FunctionLevelLinking
                {
                    [Default(DefaultTarget.Debug)]
                    Disable,
                    [Default(DefaultTarget.Release)]
                    Enable,
                }

                public enum EnhancedInstructionSet
                {
                    [Default]
                    Disable,
                    SIMD,
                    SIMD2,
                    [DevEnvVersion(minimum = DevEnv.vs2012)]
                    AdvancedVectorExtensions,
                    [DevEnvVersion(minimum = DevEnv.vs2013)]
                    AdvancedVectorExtensions2,
                    [DevEnvVersion(minimum = DevEnv.vs2012)]
                    NoEnhancedInstructions,
                }

                public enum FloatingPointModel
                {
                    Precise,
                    Strict,
                    [Default]
                    Fast
                }

                public enum FloatingPointExceptions
                {
                    [Default]
                    Disable,
                    Enable,
                }

                public enum CreateHotPatchableCode
                {
                    [Default]
                    Default,
                    Disable,
                    Enable,
                }

                public enum ConformanceMode
                {
                    [Default]
                    Disable,
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    Enable,
                }

                public enum DisableLanguageExtensions
                {
                    [Default]
                    Disable,
                    Enable,
                }

                public enum CharUnsigned
                {
                    [Default]
                    Disable,
                    Enable,
                }

                [Obsolete("Use option RemoveUnreferencedCodeData instead")]
                public enum RemovedUnreferencedCOMDAT
                {
                    Disable,
                    [Default]
                    Enable,
                }

                public enum RemoveUnreferencedCodeData
                {
                    Disable,
                    [Default]
                    Enable,
                }

                public enum BuiltInWChartType
                {
                    Disable,
                    [Default]
                    Enable,
                }

                public enum ForceLoopScope
                {
                    Disable,
                    [Default]
                    Enable
                }

                public enum RTTI
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum OpenMP
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum GenerateXMLDocumentation
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum CallingConvention
                {
                    [Default]
                    cdecl,
                    fastcall,
                    stdcall,
                }

                public enum CompileAsWinRT
                {
                    [Default]
                    Default,
                    Disable,
                    Enable
                }

                public enum ShowIncludes
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum DefineCPlusPlus
                {
                    Default,
                    Disable,
                    [Default]
                    Enable
                }

                public class DisableSpecificWarnings : Strings
                {
                    public DisableSpecificWarnings(params string[] warnings)
                        : base(warnings)
                    { }
                }

                public class UndefinePreprocessorDefinitions : Strings
                {
                    public UndefinePreprocessorDefinitions(params string[] warnings)
                        : base(warnings)
                    { }
                }

                public class AdditionalUsingDirectories : Strings
                {
                    public AdditionalUsingDirectories(params string[] dirs)
                        : base(dirs)
                    { }
                }

                public enum CppLanguageStandard
                {
                    CPP98,
                    [Default]
                    CPP11,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    CPP14,
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    CPP17,
                    GNU98,
                    GNU11,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    GNU14,
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    GNU17,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Latest
                }

                public enum SupportJustMyCode
                {
                    [Default]
                    Default,
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    Yes,
                    No
                }

                public enum SpectreMitigation
                {
                    [Default]
                    Default,
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    Enabled,
                    Disabled
                }
            }

            public static class CodeAnalysis
            {
                public enum RunCodeAnalysis
                {
                    Enable,
                    [Default]
                    Disable
                }
            }

            public static class Librarian
            {
                public enum TreatLibWarningAsErrors
                {
                    Enable,
                    [Default]
                    Disable
                }

                public class DisableSpecificWarnings : Strings
                {
                    public DisableSpecificWarnings(params string[] warnings)
                        : base(warnings)
                    { }
                }
            }

            public static class Linker
            {
                public enum EmbedManifest
                {
                    [Default]
                    Default,
                    Yes,
                    No
                }

                public enum ShowProgress
                {
                    [Default]
                    NotSet,
                    DisplayAllProgressMessages,
                    DisplaysSomeProgressMessages
                }

                public enum Incremental
                {
                    Default,
                    [Default]
                    Disable,
                    Enable,
                }

                public enum SuppressStartupBanner
                {
                    Disable,
                    [Default]
                    Enable
                }

                public enum LinkLibraryDependencies
                {
                    Default,
                    Enable,
                    [Default]
                    Disable
                }

                // false for a project dependency (only build order), true for a project reference
                public enum ReferenceOutputAssembly
                {
                    Default,
                    Enable,
                    [Default]
                    Disable
                }

                public enum CopyLocalSatelliteAssemblies
                {
                    [Default]
                    Default,
                    Enable,
                    Disable
                }

                public enum IgnoreImportLibrary
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum UseLibraryDependencyInputs
                {
                    [Default]
                    Default,
                    Enable,
                    Disable
                }

                public enum IgnoreAllDefaultLibraries
                {
                    Enable,
                    [Default]
                    Disable
                }

                public class IgnoreSpecificLibraryNames : Strings
                {
                    public IgnoreSpecificLibraryNames(params string[] values)
                        : base(values)
                    { }
                }

                public class DelayLoadDLLs : Strings
                {
                    public DelayLoadDLLs(params string[] values)
                        : base(values)
                    { }
                }

                public enum GenerateManifest
                {
                    [Default]
                    Enable,
                    Disable
                }

                /// <summary>
                /// Enable will write /DEBUG, and let MS linker decide to use FastLink or Full PDBs
                ///   If you want to force DEBUG:FULL, set both GenerateDebugInformation
                ///   and GenerateFullProgramDatabaseFile to Enable
                /// </summary>
                public enum GenerateDebugInformation
                {
                    [Default]
                    Enable,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    EnableFastLink,
                    Disable,
                }

                public enum GenerateFullProgramDatabaseFile
                {
                    [Default]
                    Default,
                    Disable,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Enable
                }

                public enum GenerateMapFile
                {
                    Disable,
                    [Default]
                    Normal,
                    Full
                }

                public enum MapExports
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum AssemblyDebug
                {
                    [Default]
                    NoDebuggableAttributeEmitted,
                    RuntimeTrackingAndDisableOptimizations,
                    NoRuntimeTrackingAndEnableOptimizations
                }

                public enum SubSystem
                {
                    [Default]
                    Console,
                    Application,
                    Native
                }

                public enum DLLDefine
                {
                    [Default]
                    Regular,
                    Extension
                };

                public class HeapSize
                {
                    public int ReserveSize;
                    public int CommintSize;

                    public HeapSize(int reserveSize)
                    {
                        ReserveSize = reserveSize;
                        CommintSize = 0;
                    }

                    public HeapSize(int reserveSize, int commintSize)
                    {
                        ReserveSize = reserveSize;
                        CommintSize = commintSize;
                    }
                }

                public class StackSize
                {
                    public int ReserveSize;
                    public int CommintSize;

                    public StackSize(int reserveSize)
                    {
                        ReserveSize = reserveSize;
                        CommintSize = 0;
                    }

                    public StackSize(int reserveSize, int commintSize)
                    {
                        ReserveSize = reserveSize;
                        CommintSize = commintSize;
                    }
                }


                public enum LargeAddress
                {
                    [Default]
                    Default,
                    NotSupportLargerThan2Gb,
                    SupportLargerThan2Gb
                }

                public enum AllowIsolation
                {
                    [Default]
                    Enabled,
                    Disabled
                }

                public enum Reference
                {
                    [Default(Options.DefaultTarget.Debug)]
                    KeepUnreferencedData,
                    [Default(Options.DefaultTarget.Release)]
                    EliminateUnreferencedData
                }

                public enum EnableCOMDATFolding
                {
                    [Default(Options.DefaultTarget.Debug)]
                    DoNotRemoveRedundantCOMDATs,
                    [Default(Options.DefaultTarget.Release)]
                    RemoveRedundantCOMDATs
                }

                public enum LinkTimeCodeGeneration
                {
                    [Default]
                    Default,
                    UseLinkTimeCodeGeneration,
                    ProfileGuidedOptimizationInstrument,
                    ProfileGuidedOptimizationOptimize,
                    ProfileGuidedOptimizationUpdate
                }

                public class FunctionOrder
                {
                    public FunctionOrder(string argOrder) { Order = argOrder; }

                    public string Order;
                }

                public enum RandomizedBaseAddress
                {
                    [Default]
                    Default,
                    Enable,
                    Disable
                }

                public enum FixedBaseAddress
                {
                    [Default]
                    Default,
                    Enable,
                    Disable
                }

                public enum CreateHotPatchableImage
                {
                    [Default]
                    Disable,
                    Enable,
                    X86Image,
                    X64Image,
                    ItaniumImage
                }

                public enum ForceFileOutput
                {
                    [Default]
                    Default,
                    MultiplyDefinedSymbolOnly
                }

                public class DisableSpecificWarnings : Strings
                {
                    public DisableSpecificWarnings(params string[] warnings)
                        : base(warnings)
                    { }
                }

                public class BaseAddress
                {
                    public string Value = string.Empty;
                    public BaseAddress(string value)
                    {
                        Value = value;
                    }

                    public BaseAddress(object obj)
                    {
                        Value = obj.ToString();
                    }
                }

                public enum GenerateWindowsMetadata
                {
                    [Default]
                    Default,
                    Enable,
                    Disable
                }

                public enum TreatLinkerWarningAsErrors
                {
                    Enable,
                    [Default]
                    Disable
                }
            }

            public static class ManifestTool
            {
                public enum EnableDpiAwareness
                {
                    [Default]
                    Default,
                    Yes,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    PerMonitor,
                    No
                }
            }

            public static class SourceFile
            {
                public enum PrecompiledHeader
                {
                    NotUsingPrecompiledHeaders,
                    CreatePrecompiledHeader,
                    UsePrecompiledHeader
                }
            }

            public static class ResourceCompiler
            {
                public enum ShowProgress
                {
                    [Default]
                    No,
                    Yes
                }

                public class PreprocessorDefinitions : Strings
                {
                    public PreprocessorDefinitions(params string[] definitions)
                        : base(definitions)
                    { }
                }

                public class UndefinePreprocessorDefinitions : Strings
                {
                    public UndefinePreprocessorDefinitions(params string[] definitions)
                        : base(definitions)
                    { }
                }

                [Obsolete("Please use " + nameof(Project.Configuration.ResourceIncludePaths) + " instead", true)]
                public class AdditionalIncludeDirectories : Strings
                {
                    public AdditionalIncludeDirectories(params string[] dirs)
                        : base(dirs)
                    { }
                }
            }

            public static class LLVM
            {
                /// <summary>
                /// Use clang-cl for compiling.  If this option is disabled, the Microsoft compiler (cl.exe) will be used instead.
                /// </summary>
                public enum UseClangCl
                {
                    [Default]
                    Enable,
                    Disable
                }

                /// <summary>
                /// Use lld-link for linking.  If this option is disabled, the Microsoft linker (link.exe) will be used instead.
                /// </summary>
                public enum UseLldLink
                {
                    [Default]
                    Default,
                    Enable,
                    Disable
                }
            }
        }
    }
}
