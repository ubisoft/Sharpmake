// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;

namespace Sharpmake
{
    public static partial class Options
    {
        public static class Vc
        {
            public static class General
            {
                /// <summary>
                /// Platform Toolset
                /// </summary>
                /// <remarks>
                /// Specifies which build tools will be used.
                /// </remarks>
                public enum PlatformToolset
                {
                    [Default]
                    Default,  // same as Visual Studio version
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
                    [DevEnvVersion(minimum = DevEnv.vs2022)]
                    v143, // Visual Studio 2022
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    LLVM, // LLVM from Visual Studio 2017
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    ClangCL, // LLVM as of Visual Studio 2019 official extension

                    [Obsolete("Use either LLVM or ClangCL", error: true)]
                    LLVM_vs2012,
                    [Obsolete("Use either LLVM or ClangCL", error: true)]
                    LLVM_vs2014,

                    [Obsolete("Sharpmake doesn't support this toolset anymore.", error: true)]
                    v100, // Visual Studio 2010
                    [Obsolete("Sharpmake doesn't support this toolset anymore.", error: true)]
                    v110, // Visual Studio 2012
                    [Obsolete("Sharpmake doesn't support this toolset anymore.", error: true)]
                    v110_xp, // Visual Studio 2012 - Windows XP
                    [Obsolete("Sharpmake doesn't support this toolset anymore.", error: true)]
                    v120, // Visual Studio 2013
                    [Obsolete("Sharpmake doesn't support this toolset anymore.", error: true)]
                    v120_xp, // Visual Studio 2013 - Windows XP
                }

                /// <summary>
                /// Windows SDK Version
                /// </summary>
                /// <remarks>
                /// Specifies which version of the Windows SDK to use.
                /// </remarks>
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
                    v10_0_20348_0, // 2104/21H1
                    v10_0_22000_0, // Windows 11
                    v10_0_22621_0, // Windows 11 22H2
                    v10_0_26100_0, // Windows 11 24H2
                    Latest,        // latest available in host machine
                }

                /// <summary>
                /// Translate Includes to Imports
                /// </summary>
                /// <remarks>
                /// Enables the compiler to translate #include directives into import directives for the available header units.  (/translateInclude)
                /// </remarks>
                public enum TranslateIncludes
                {
                    Enable,
                    [Default]
                    Disable
                }

                /// <summary>
                /// Character Set
                /// </summary>
                /// <remarks>
                /// Tells the compiler to use the specified character set; aids in localization issues.
                /// </remarks>
                public enum CharacterSet
                {
                    /// <summary>
                    /// Not Set
                    /// </summary>
                    Default,

                    /// <summary>
                    /// Use Unicode Character Set
                    /// </summary>
                    Unicode,

                    /// <summary>
                    /// Use Multi-Byte Character Set
                    /// </summary>
                    [Default]
                    MultiByte
                }

                /// <summary>
                /// Whole Program Optimization
                /// </summary>
                /// <remarks>
                /// Enables cross-module optimizations by delaying code generation to link time; requires that linker option 'Link Time Code Generation' be turned on.
                /// </remarks>
                public enum WholeProgramOptimization
                {
                    /// <summary>
                    /// No Whole Program Optimization
                    /// </summary>
                    [Default]
                    Disable,

                    /// <summary>
                    /// Use Link Time Code Generation
                    /// </summary>
                    LinkTime,

                    /// <summary>
                    /// Profile Guided Optimization - Instrument
                    /// </summary>
                    Instrument,

                    /// <summary>
                    /// Profile Guided Optimization - Optimize
                    /// </summary>
                    Optimize,

                    /// <summary>
                    /// Profile Guided Optimization - Update
                    /// </summary>
                    Update
                }

                /// <summary>
                /// Debug Information Format
                /// </summary>
                /// <remarks>
                /// Specifies the type of debugging information generated by the compiler.  This requires compatible linker settings.
                /// </remarks>
                public enum DebugInformation
                {
                    /// <summary>
                    /// None
                    /// </summary>
                    /// <remarks>
                    /// Produces no debugging information, so compilation may be faster.
                    /// </remarks>
                    Disable,

                    /// <summary>
                    /// C7 compatible
                    /// </summary>
                    /// <remarks>
                    /// Select the type of debugging information created for your program and whether this information is kept in object (.obj) files or in a program database (PDB).
                    /// </remarks>
                    C7Compatible,

                    /// <summary>
                    /// Program Database
                    /// </summary>
                    /// <remarks>
                    /// Produces a program database (PDB) that contains type information and symbolic debugging information for use with the debugger. The symbolic debugging information includes the names and types of variables, as well as functions and line numbers.
                    /// </remarks>
                    [Default]
                    ProgramDatabase,

                    /// <summary>
                    /// Program Database for Edit And Continue
                    /// </summary>
                    /// <remarks>
                    /// Produces a program database, as described above, in a format that supports the Edit and Continue feature.
                    /// </remarks>
                    ProgramDatabaseEnC
                }

                /// <summary>
                /// Use Debug Libraries
                /// </summary>
                /// <remarks>
                /// Specifies whether this configuration should use debug libraries and debug switches
                /// </remarks>
                public enum UseDebugLibraries
                {
                    [Default(DefaultTarget.Debug)]
                    Enabled,
                    [Default(DefaultTarget.Release)]
                    Disabled
                }

                /// <summary>
                /// Warning Level
                /// </summary>
                /// <remarks>
                /// Select how strict you want the compiler to be about code errors.
                /// </remarks>
                public enum WarningLevel
                {
                    /// <summary>
                    /// Turn Off All Warnings
                    /// </summary>
                    /// <remarks>
                    /// Level 0 disables all warnings.
                    /// </remarks>
                    Level0,

                    /// <summary>
                    /// Level 1 displays severe warnings. Level 1 is the default warning level at the command line.
                    /// </summary>
                    Level1,

                    /// <summary>
                    /// Level 2 displays all level 1 warnings and warnings less severe than level 1.
                    /// </summary>
                    Level2,

                    /// <summary>
                    /// Level 3 displays all level 2 warnings and all other warnings recommended for production purposes.
                    /// </summary>
                    Level3,

                    /// <summary>
                    /// Level 4 displays all level 3 warnings plus informational warnings, which in most cases can be safely ignored.
                    /// </summary>
                    [Default]
                    Level4,

                    /// <summary>
                    /// Enables all warnings, including those disabled by default.
                    /// </summary>
                    EnableAllWarnings
                }

                /// <summary>
                /// Treat Warnings As Errors
                /// </summary>
                /// <remarks>
                /// Treats all compiler warnings as errors. For a new project, it may be best to use /WX in all compilations; resolving all warnings will ensure the fewest possible hard-to-find code defects.
                /// </remarks>
                public enum TreatWarningsAsErrors
                {
                    Enable,
                    [Default]
                    Disable
                }

                /// <summary>
                /// Diagnostics Format
                /// </summary>
                /// <remarks>
                /// Enables rich diagnostics, with column information and source context in diagnostic messages.
                /// </remarks>
                public enum DiagnosticsFormat
                {
                    /// <summary>
                    /// Provides column information in the diagnostic message, as well as outputting the relevant line of source code with a caret indicating the offending column.
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    Caret,

                    /// <summary>
                    /// Additionally provides the column number within the line where the diagnostic is issued, where applicable.
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    ColumnInfo,

                    /// <summary>
                    /// Retains the prior, concise diagnostic messages with the line number.
                    /// </summary>
                    [Default]
                    Classic
                }

                /// <summary>
                /// Treat Files Included with Angle Brackets as External
                /// </summary>
                /// <remarks>
                /// Specifies whether to treat files included with angle brackets as external.   (/external:anglebrackets)
                /// </remarks>
                public enum TreatAngleIncludeAsExternal
                {
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    Enable,
                    [Default]
                    Disable
                }

                /// <summary>
                /// External Header Warning Level
                /// </summary>
                /// <remarks>
                /// Select how strict you want the compiler to be about code errors in external headers.     (/external:W0 - /external:W4)
                /// </remarks>
                public enum ExternalWarningLevel
                {
                    /// <summary>
                    /// Turn Off All Warnings
                    /// </summary>
                    /// <remarks>
                    /// Level 0 disables all warnings.
                    /// </remarks>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    Level0,

                    /// <summary>
                    /// Level 1 displays severe warnings. Level 1 is the default warning level at the command line.
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    Level1,

                    /// <summary>
                    /// Level 2 displays all level 1 warnings and warnings less severe than level 1.
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    Level2,

                    /// <summary>
                    /// Level 3 displays all level 2 warnings and all other warnings recommended for production purposes.
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    Level3,

                    /// <summary>
                    /// Level 4 displays all level 3 warnings plus informational warnings, which in most cases can be safely ignored.
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    Level4,

                    /// <summary>
                    /// Inherit Project Warning Level
                    /// </summary>
                    [Default]
                    InheritWarningLevel
                }

                /// <summary>
                /// Template Diagnostics in External Headers
                /// </summary>
                /// <remarks>
                /// Specifies whether to evaluate warning level across template instantiation chain.   (/external:templates-)
                /// </remarks>
                public enum ExternalTemplatesDiagnostics
                {
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    Enable,
                    [Default]
                    Disable
                }

                /// <summary>
                /// Enable Managed Incremental Build
                /// </summary>
                /// <remarks>
                /// Enables managed incremental build scenarios using metagen.
                /// </remarks>
                public enum EnableManagedIncrementalBuild
                {
                    [Default]
                    Enable,
                    Disable
                }

                /// <summary>
                /// Common Language RunTime Support
                /// </summary>
                /// <remarks>
                /// Use the .NET runtime service.  This switch is incompatible with some other switches; see the documentation on the /clr family of switches for details.
                /// </remarks>
                public enum CommonLanguageRuntimeSupport
                {
                    /// <summary>
                    /// No Common Language RunTime Support
                    /// </summary>
                    [Default]
                    NoClrSupport,

                    /// <summary>
                    /// Common Language RunTime Support
                    /// </summary>
                    /// <remarks>
                    /// Creates metadata for your application that can be consumed by other CLR applications, and allows your application to consume types and data in the metadata of other CLR components.
                    /// </remarks>
                    ClrSupport,  // clr

                    /// <summary>
                    /// Pure MSIL Common Language RunTime Support
                    /// </summary>
                    /// <remarks>
                    /// Produces an MSIL-only output file with no native executable code, although it can contain native types compiled to MSIL.
                    /// </remarks>
                    PureMsilClrSupport,  // clr:pure

                    /// <summary>
                    /// Safe MSIL Common Language RunTime Support
                    /// </summary>
                    /// <remarks>
                    /// Produces an MSIL-only (no native executable code) and verifiable output file.
                    /// </remarks>
                    SafeMsilClrSupport,  // clr:safe

                    [Obsolete("This option is not supported by msvc anymore.", true)]
                    SafeMsilClrSupportOldSyntax,  // clr:oldSyntax

                    /// <summary>
                    /// Common Language RunTime Support for .NET Core
                    /// </summary>
                    /// <remarks>
                    /// Creates metadata and code for the component using the latest cross-platform .NET framework, also known as .NET Core. The metadata can be consumed by other .NET Core applications. And, the option enables the component to consume types and data in the metadata of other .NET Core components.
                    /// </remarks>
                    ClrNetCoreSupport // clr:netcore
                }

                public enum MfcSupport
                {
                    [Default]
                    UseMfcStdWin,
                    UseMfcStatic,
                    UseMfcDynamic
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

            public static class Advanced
            {
                public enum CopyLocalDeploymentContent
                {
                    [DevEnvVersion(minimum = DevEnv.vs2019)] // Introduced in Visual Studio 2019 version 16.7.
                    Enable,
                    [Default]
                    Disable
                }

                public enum CopyLocalProjectReference
                {
                    [DevEnvVersion(minimum = DevEnv.vs2019)] // Introduced in Visual Studio 2019 version 16.7.
                    Enable,
                    [Default]
                    Disable
                }

                public enum CopyLocalDebugSymbols
                {
                    [DevEnvVersion(minimum = DevEnv.vs2019)] // Introduced in Visual Studio 2019 version 16.7.
                    Enable,
                    [Default]
                    Disable
                }

                public enum CopyCppRuntimeToOutputDir
                {
                    [DevEnvVersion(minimum = DevEnv.vs2019)] // Introduced in Visual Studio 2019 version 16.7.
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
                    /// Equivalent to /Og /Os /Oy /Ob2 /Gs /GF /Gy
                    /// </remarks>
                    MinimizeSize,

                    /// <summary>
                    /// Maximum Optimization (Favor Speed)
                    /// </summary>
                    /// <remarks>
                    /// Equivalent to /Og /Oi /Ot /Oy /Ob2 /Gs /GF /Gy
                    /// </remarks>
                    MaximizeSpeed,

                    /// <summary>
                    /// Optimizations (Favor Speed)
                    /// </summary>
                    /// <remarks>
                    /// Equivalent to /Og /Oi /Ot /Oy /Ob2
                    /// </remarks>
                    [Default(DefaultTarget.Release)]
                    FullOptimization
                }

                /// <summary>
                /// Inline Function Expansion
                /// </summary>
                /// <remarks>
                /// Select the level of inline function expansion for the build.
                /// </remarks>
                public enum Inline
                {
                    Default,

                    /// <summary>
                    /// Only __inline
                    /// </summary>
                    /// <remarks>
                    /// Expands only functions marked as inline, __inline, __forceinline or __inline or, in a C++ member function, defined within a class declaration.
                    /// </remarks>
                    [Default(DefaultTarget.Debug)]
                    OnlyInline,  // set as debug default because good enough for debug builds and much faster.

                    /// <summary>
                    /// Any Suitable
                    /// </summary>
                    /// <remarks>
                    /// Expands functions marked as inline or __inline and any other function that the compiler chooses (expansion occurs at the compiler's discretion, often referred to as auto-inlining).
                    /// </remarks>
                    [Default(DefaultTarget.Release)]
                    AnySuitable,

                    /// <summary>
                    /// Disables inline expansion, which is on by default.
                    /// </summary>
                    Disable
                }

                /// <summary>
                /// Enable Intrinsic Functions
                /// </summary>
                /// <remarks>
                /// Using intrinsic functions generates faster, but possibly larger, code.
                /// </remarks>
                public enum Intrinsic
                {
                    [Default]
                    Enable,
                    Disable
                }

                /// <summary>
                /// Favor Size Or Speed
                /// </summary>
                /// <remarks>
                /// Whether to favor code size or code speed; 'Global Optimization' must be turned on.
                /// </remarks>
                public enum FavorSizeOrSpeed
                {
                    /// <summary>
                    /// No size nor speed optimization.
                    /// </summary>
                    [Default(DefaultTarget.Debug)]
                    Neither,

                    /// <summary>
                    /// Favor Fast Code. Maximizes the speed of EXEs and DLLs by instructing the compiler to favor speed over size. (This is the default.)
                    /// </summary>
                    [Default(DefaultTarget.Release)]
                    FastCode,

                    /// <summary>
                    /// Favor Small Code. Minimizes the size of EXEs and DLLs by instructing the compiler to favor size over speed.
                    /// </summary>
                    SmallCode
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
                /// Enable Fiber-Safe Optimizations
                /// </summary>
                /// <remarks>
                /// Enables memory space optimization when using fibers and thread local storage access.
                /// </remarks>
                public enum FiberSafe
                {
                    [Default]
                    Disable,
                    Enable
                }

                /// <summary>
                /// Ignore Standard Include Paths
                /// </summary>
                /// <remarks>
                /// Prevents the compiler from searching for include files in directories specified in the INCLUDE environment variables.
                /// </remarks>
                public enum IgnoreStandardIncludePath
                {
                    [Default]
                    Disable,
                    Enable
                }

                /// <summary>
                /// Preprocess to a File
                /// </summary>
                /// <remarks>
                /// Preprocesses C and C++ source files and writes the preprocessed output to a file. This option suppresses compilation, thus it does not produce an .obj file.
                /// </remarks>
                public enum GenerateProcessorFile
                {
                    [Default]
                    Disable,

                    WithLineNumbers,

                    /// <summary>
                    /// Preprocess without #line directives.
                    /// </summary>
                    WithoutLineNumbers
                }

                /// <summary>
                /// Suppresses comment strip from source code; requires that one of the 'Preprocessing' options be set.
                /// </summary>
                public enum KeepComment
                {
                    [Default]
                    Disable,
                    Enable
                }

                /// <summary>
                /// Enables a token-based preprocessor that conforms to C99 and C++11 and later standards.
                /// </summary>
                public enum UseStandardConformingPreprocessor
                {
                    [Default]
                    Default,
                    Disable,
                    Enable
                }

                /// <summary>
                /// Enables the compiler to create a single read-only copy of identical strings in the program image and in memory during execution, resulting in smaller programs, an optimization called string pooling. /O1, /O2, and /ZI  automatically set /GF option.
                /// </summary>
                public enum StringPooling
                {
                    Disable,
                    [Default]
                    Enable
                }

                /// <summary>
                /// Enables minimal rebuild, which determines whether C++ source files that include changed C++ class definitions (stored in header (.h) files) need to be recompiled.
                /// </summary>
                public enum MinimalRebuild
                {
                    [Default(DefaultTarget.Release)]
                    Disable,
                    [Default(DefaultTarget.Debug)]
                    Enable
                }

                /// <summary>
                /// Enable C++ Exceptions
                /// </summary>
                /// <remarks>
                /// Specifies the model of exception handling to be used by the compiler.
                /// </remarks>
                public enum Exceptions
                {
                    /// <summary>
                    /// No exception handling.
                    /// </summary>
                    [Default]
                    Disable,

                    /// <summary>
                    /// The exception-handling model that catches C++ exceptions only and tells the compiler to assume that extern C functions never throw a C++ exception. (/EHsc)
                    /// </summary>
                    Enable,

                    /// <summary>
                    /// The exception-handling model that catches C++ exceptions only and tells the compiler to assume that extern C functions do throw an exception. (/EHs)
                    /// </summary>
                    EnableWithExternC,

                    /// <summary>
                    /// The exception-handling model that catches asynchronous (structured) and synchronous (C++) exceptions. (/EHa)
                    /// </summary>
                    EnableWithSEH
                }

                /// <summary>
                /// Smaller Type Check
                /// </summary>
                /// <remarks>
                /// Enable checking for conversion to smaller types, incompatible with any optimization type other than debug.
                /// </remarks>
                public enum TypeChecks
                {
                    [Default]
                    Disable,
                    Enable
                }

                /// <summary>
                /// Perform basic runtime error checks, incompatible with any optimization type other than debug.
                /// </summary>
                public enum RuntimeChecks
                {
                    [Default]
                    Default,

                    /// <summary>
                    /// Enables stack frame run-time error checking.
                    /// </summary>
                    StackFrames,

                    /// <summary>
                    /// Reports when a variable is used without having been initialized.
                    /// </summary>
                    UninitializedVariables,

                    /// <summary>
                    /// Both (/RTC1, equiv. to /RTCsu)
                    /// </summary>
                    Both
                }

                /// <summary>
                /// Specify runtime library for linking.
                /// </summary>
                public enum RuntimeLibrary
                {
                    /// <summary>
                    /// Multi-threaded
                    /// </summary>
                    /// <remarks>
                    /// Causes your application to use the multithread, static version of the run-time library.
                    /// </remarks>
                    [Default(DefaultTarget.Release)]
                    MultiThreaded,

                    /// <summary>
                    /// Multi-threaded Debug
                    /// </summary>
                    /// <remarks>
                    /// Defines _DEBUG and _MT. This option also causes the compiler to place the library name LIBCMTD.lib into the .obj file so that the linker will use LIBCMTD.lib to resolve external symbols.
                    /// </remarks>
                    [Default(DefaultTarget.Debug)]
                    MultiThreadedDebug,

                    /// <summary>
                    /// Multi-threaded DLL
                    /// </summary>
                    /// <remarks>
                    /// Causes your application to use the multithread- and DLL-specific version of the run-time library. Defines _MT and _DLL and causes the compiler to place the library name MSVCRT.lib into the .obj file.
                    /// </remarks>
                    MultiThreadedDLL,

                    /// <summary>
                    /// Multi-threaded Debug DLL
                    /// </summary>
                    /// <remarks>
                    /// Defines _DEBUG, _MT, and _DLL and causes your application to use the debug multithread- and DLL-specific version of the run-time library. It also causes the compiler to place the library name MSVCRTD.lib into the .obj file.
                    /// </remarks>
                    MultiThreadedDebugDLL,
                }

                /// <summary>
                /// Struct Member Alignment
                /// </summary>
                /// <remarks>
                /// Specifies 1, 2, 4, or 8-byte boundaries for struct member alignment.
                /// </remarks>
                public enum StructAlignment
                {
                    /// <summary>
                    /// Default alignment settings.
                    /// </summary>
                    [Default]
                    Default,

                    /// <summary>
                    /// 1 Byte
                    /// </summary>
                    /// <remarks>
                    /// Packs structures on 1-byte boundaries. Same as /Zp.
                    /// </remarks>
                    Alignment1,

                    /// <summary>
                    /// 2 Bytes
                    /// </summary>
                    /// <remarks>
                    /// Packs structures on 2-byte boundaries.
                    /// </remarks>
                    Alignment2,

                    /// <summary>
                    /// 4 Byte
                    /// </summary>
                    /// <remarks>
                    /// Packs structures on 4-byte boundaries.
                    /// </remarks>
                    Alignment4,

                    /// <summary>
                    /// 8 Bytes
                    /// </summary>
                    /// <remarks>
                    /// Packs structures on 8-byte boundaries (default).
                    /// </remarks>
                    Alignment8,

                    /// <summary>
                    /// 16 Bytes
                    /// </summary>
                    /// <remarks>
                    /// Packs structures on 16-byte boundaries.
                    /// </remarks>
                    Alignment16
                }

                /// <summary>
                /// Security Check
                /// </summary>
                /// <remarks>
                /// The Security Check helps detect stack-buffer over-runs, a common attempted attack upon a program's security.
                /// </remarks>
                public enum BufferSecurityCheck
                {
                    /// <summary>
                    /// Disable Security Check. (/GS-)
                    /// </summary>
                    [Default(DefaultTarget.Release)]
                    Disable,

                    /// <summary>
                    /// Enable Security Check. (/GS)
                    /// </summary>
                    [Default(DefaultTarget.Debug)]
                    Enable,
                }

                public enum OptimizeGlobalData
                {
                    [Default]
                    Disable,
                    Enable,
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
                /// Enable Enhanced Instruction Set
                /// </summary>
                /// <remarks>
                /// Enable use of instructions found on processors that support enhanced instruction sets, e.g., the SSE, SSE2, AVX, AVX2 and AVX-512 enhancements to IA-32; AVX, AVX2 and AVX-512 to x64. Currently /arch:SSE and /arch:SSE2 are only available when building for the x86 architecture. If no option is specified, the compiler will use instructions found on processors that support SSE2. Use of enhanced instructions can be disabled with /arch:IA32.   (/arch:SSE, /arch:SSE2, /arch:AVX, /arch:AVX2, /arch:AVX512, /arch:IA32)
                /// </remarks>
                public enum EnhancedInstructionSet
                {
                    /// <summary>
                    /// Not Set
                    /// </summary>
                    [Default]
                    Disable,

                    /// <summary>
                    /// Streaming SIMD Extensions. (/arch:SSE)
                    /// </summary>
                    SIMD,

                    /// <summary>
                    /// Streaming SIMD Extensions 2. (/arch:SSE2)
                    /// </summary>
                    SIMD2,

                    /// <summary>
                    /// Advanced Vector Extensions. (/arch:AVX)
                    /// </summary>
                    AdvancedVectorExtensions,

                    /// <summary>
                    /// Advanced Vector Extensions 2. (/arch:AVX2)
                    /// </summary>
                    AdvancedVectorExtensions2,

                    /// <summary>
                    /// Advanced Vector Extensions 512. (/arch:AVX512)
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    AdvancedVectorExtensions512,

                    /// <summary>
                    /// No Enhanced Instructions. (/arch:IA32)
                    /// </summary>
                    NoEnhancedInstructions,
                }

                /// <summary>
                /// Floating Point Model
                /// </summary>
                /// <remarks>
                /// Sets the floating point model.
                /// </remarks>
                public enum FloatingPointModel
                {
                    /// <summary>
                    /// Improves the consistency of floating-point tests for equality and inequality.
                    /// </summary>
                    Precise,

                    /// <summary>
                    /// The strictest floating-point model. /fp:strict causes fp_contract to be OFF and fenv_access to be ON. /fp:except is implied and can be disabled by explicitly specifying /fp:except-. When used with /fp:except-, /fp:strict enforces strict floating-point semantics but without respect for exceptional events.
                    /// </summary>
                    Strict,

                    /// <summary>
                    /// Creates the fastest code in the majority of cases.
                    /// </summary>
                    [Default]
                    Fast
                }

                /// <summary>
                /// Enable Floating Point Exceptions
                /// </summary>
                /// <remarks>
                /// Reliable floating-point exception model. Exceptions will be raised immediately after they are triggered.
                /// </remarks>
                public enum FloatingPointExceptions
                {
                    [Default]
                    Disable,
                    Enable,
                }

                /// <summary>
                /// Create Hotpatchable Image
                /// </summary>
                /// <remarks>
                /// When hotpatching is on, the compiler ensures that first instruction of each function is two bytes, which is required for hot patching.
                /// </remarks>
                public enum CreateHotPatchableCode
                {
                    [Default]
                    Default,
                    Disable,
                    Enable,
                }

                /// <summary>
                /// Conformance mode
                /// </summary>
                /// <remarks>
                /// Enables or suppresses conformance mode. (/permissive-, /permissive).
                /// </remarks>
                public enum ConformanceMode
                {
                    /// <summary>
                    /// No
                    /// </summary>
                    [Default]
                    Disable,

                    /// <summary>
                    /// Yes
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    Enable,
                }

                /// <summary>
                /// Disable Language Extensions
                /// </summary>
                /// <remarks>
                /// Suppresses or enables language extensions.
                /// </remarks>
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

                /// <summary>
                /// Remove unreferenced code and data
                /// </summary>
                /// <remarks>
                /// When specified, compiler no longer generates symbol information for unreferenced code and data.
                /// </remarks>
                public enum RemoveUnreferencedCodeData
                {
                    Disable,
                    [Default]
                    Enable,
                }

                /// <summary>
                /// Treat WChar_t As Built in Type
                /// </summary>
                /// <remarks>
                /// When specified, the type wchar_t becomes a native type that maps to __wchar_t in the same way that short maps to __int16. /Zc:wchar_t is on by default.
                /// </remarks>
                public enum BuiltInWChartType
                {
                    Disable,
                    [Default]
                    Enable,
                }

                /// <summary>
                /// Force Conformance in For Loop Scope
                /// </summary>
                /// <remarks>
                /// Used to implement standard C++ behavior for the for statement loops with Microsoft extensions (/Za, /Ze (Disable Language Extensions)). /Zc:forScope is on by default.
                /// </remarks>
                public enum ForceLoopScope
                {
                    Disable,
                    [Default]
                    Enable
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

                public enum OpenMP
                {
                    Default,
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

                /// <summary>
                /// Calling Convention
                /// </summary>
                /// <remarks>
                /// Select the default calling convention for your application (can be overridden by function).
                /// </remarks>
                public enum CallingConvention
                {
                    /// <summary>
                    /// __cdecl
                    /// </summary>
                    /// <remarks>
                    /// Specifies the __cdecl calling convention for all functions except C++ member functions and functions marked __stdcall or __fastcall.
                    /// </remarks>
                    [Default]
                    cdecl,

                    /// <summary>
                    /// __fastcall
                    /// </summary>
                    /// <remarks>
                    /// Specifies the __fastcall calling convention for all functions except C++ member sfunctions and functions marked __cdecl or __stdcall. All __fastcall functions must have prototypes.
                    /// </remarks>
                    fastcall,

                    /// <summary>
                    /// __stdcall
                    /// </summary>
                    /// <remarks>
                    /// Specifies the __stdcall calling convention for all functions except C++ member functions and functions marked __cdecl or __fastcall. All __stdcall functions must have prototypes.
                    /// </remarks>
                    stdcall,

                    /// <summary>
                    /// __vectorcall
                    /// </summary>
                    /// <remarks>
                    /// Specifies the __vectorcall calling convention for all functions except C++ member functions and functions marked __cdecl, __fastcall, or __stdcall. All __vectorcall functions must have prototypes.
                    /// </remarks>
                    vectorcall
                }

                /// <summary>
                /// Consume Windows Runtime Extension
                /// </summary>
                /// <remarks>
                /// Consume the Windows Run Time languages extensions.
                /// </remarks>
                public enum CompileAsWinRT
                {
                    [Default]
                    Default,
                    Disable,
                    Enable
                }

                /// <summary>
                /// Show Includes
                /// </summary>
                /// <remarks>
                /// Generates a list of include files with compiler output.
                /// </remarks>
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

                /// <summary>
                /// C++ Language Standard
                /// </summary>
                /// <remarks>
                /// Determines the C++ language standard the compiler will enforce. It is recommended to use the latest version when possible. (/std:c++14, /std:c++17, /std:c++20, /std:c++latest)
                /// </remarks>
                public enum CppLanguageStandard
                {
                    CPP98,

                    [Default]
                    CPP11,

                    /// <summary>
                    /// ISO C++14 Standard
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    CPP14,

                    /// <summary>
                    /// ISO C++17 Standard
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    CPP17,

                    /// <summary>
                    /// ISO C++20 Standard
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    CPP20,

                    GNU98,
                    GNU11,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    GNU14,
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    GNU17,

                    /// <summary>
                    /// Preview - Features from the Latest C++ Working Draft
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Latest
                }

                /// <summary>
                /// C Language Standard
                /// </summary>
                /// <remarks>
                /// Determines the C language standard the compiler will enforce. It is recommended to use the latest version when possible.
                /// </remarks>
                public enum CLanguageStandard
                {
                    /// <summary>
                    /// Default (Legacy MSVC)
                    /// </summary>
                    [Default]
                    Legacy,

                    /// <summary>
                    /// ISO C11 Standard
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    C11,

                    /// <summary>
                    /// ISO C17 (2018) Standard
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    C17
                }

                /// <summary>
                /// Support Just My Code Debugging
                /// </summary>
                /// <remarks>
                /// Adds supporting code for enabling Just My Code debugging in this compilation unit.
                /// </remarks>
                public enum SupportJustMyCode
                {
                    [Default]
                    Default,
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    Yes,
                    No
                }

                /// <summary>
                /// Spectre Mitigation
                /// </summary>
                /// <remarks>
                /// Spectre mitigations for CVE 2017-5753.
                /// </remarks>
                public enum SpectreMitigation
                {
                    [Default]
                    Default,

                    /// <summary>
                    /// Enable Spectre mitigation feature for CVE 2017-5753
                    /// </summary>
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    Spectre,

                    /// <summary>
                    /// All Loads
                    /// </summary>
                    /// <remarks>
                    /// Enable Spectre mitigations for all load instructions
                    /// </remarks>
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    SpectreLoad,

                    /// <summary>
                    /// All Control Flow Loads
                    /// </summary>
                    /// <remarks>
                    /// Enable Spectre mitigations for all control flow load instructions
                    /// </remarks>
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    SpectreLoadCF,

                    /// <summary>
                    /// Disabled
                    /// </summary>
                    /// <remarks>
                    /// Not Set.
                    /// </remarks>
                    [DevEnvVersion(minimum = DevEnv.vs2017)]
                    Disabled,

                    [Obsolete("Use '" + nameof(Spectre) + "' enum entry instead", error: false)]
                    Enabled = Spectre
                }

                /// <summary>
                /// Enable Address Sanitizer
                /// </summary>
                /// <remarks>
                /// Compiles and links program with AddressSanitizer. Currently available for x86 and x64 builds.
                /// </remarks>
                public enum EnableAsan
                {
                    [Default]
                    Disable,
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    Enable
                }

                /// <summary>
                /// Enable Jumbo/Unity builds for msbuild. Only usable with msbuild.
                /// </summary>
                /// <remarks>
                /// Merges multiple translation units together
                /// </remarks>
                public enum JumboBuild
                {
                    [Default]
                    Disable,
                    [DevEnvVersion(minimum = DevEnv.vs2019)]
                    Enable
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

                public enum MicrosoftCodeAnalysis
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum ClangTidyCodeAnalysis
                {
                    Enable,
                    [Default]
                    Disable
                }

                public class CodeAnalysisExcludePaths : PathOption
                {
                    public CodeAnalysisExcludePaths(string value)
                        : base(value)
                    { }
                }
            }

            public static class Librarian
            {
                /// <summary>
                /// Treat Lib Warning As Errors
                /// </summary>
                /// <remarks>
                /// Causes no output file to be generated if lib generates a warning.
                /// </remarks>
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

                /// <summary>
                /// Show Progress
                /// </summary>
                /// <remarks>
                /// Prints Linker Progress Messages
                /// </remarks>
                public enum ShowProgress
                {
                    /// <summary>
                    /// Not Set
                    /// </summary>
                    /// <remarks>
                    /// No verbosity.
                    /// </remarks>
                    [Default]
                    NotSet,

                    /// <summary>
                    /// Display all progress messages
                    /// </summary>
                    LinkVerbose,

                    /// <summary>
                    /// For Libraries Searched
                    /// </summary>
                    /// <remarks>
                    /// Displays progress messages indicating just the libraries searched.
                    /// </remarks>
                    LinkVerboseLib,

                    /// <summary>
                    /// About COMDAT folding during optimized linking
                    /// </summary>
                    /// <remarks>
                    /// Displays information about COMDAT folding during optimized linking.
                    /// </remarks>
                    LinkVerboseICF,

                    /// <summary>
                    /// About data removed during optimized linking
                    /// </summary>
                    /// <remarks>
                    /// Displays information about functions and data removed during optimized linking.
                    /// </remarks>
                    LinkVerboseREF,

                    /// <summary>
                    /// About Modules incompatible with SEH
                    /// </summary>
                    /// <remarks>
                    /// Displays information about modoules incompatible with Safe Exception Handling.
                    /// </remarks>
                    LinkVerboseSAFESEH,

                    /// <summary>
                    /// About linker activity related to managed code
                    /// </summary>
                    /// <remarks>
                    /// Display information about linker activity related to managed code.
                    /// </remarks>
                    LinkVerboseCLR,

                    [Obsolete("Use '" + nameof(LinkVerbose) + "' enum entry instead", error: false)]
                    DisplayAllProgressMessages = LinkVerbose,
                    [Obsolete("Use '" + nameof(LinkVerboseLib) + "' enum entry instead", error: false)]
                    DisplaysSomeProgressMessages = LinkVerboseLib
                }

                /// <summary>
                /// Enable Incremental Linking
                /// </summary>
                public enum Incremental
                {
                    Default,
                    [Default]
                    Disable,
                    Enable,
                }

                /// <summary>
                /// Suppress Startup Banner
                /// </summary>
                /// <remarks>
                /// Prevents display of the copyright message and version number.
                /// </remarks>
                public enum SuppressStartupBanner
                {
                    Disable,
                    [Default]
                    Enable
                }

                /// <summary>
                /// Link Library Dependencies
                /// </summary>
                /// <remarks>
                /// Specifies whether or not library outputs from project dependencies are automatically linked in.
                /// </remarks>
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

                /// <summary>
                /// Ignore Import Library
                /// </summary>
                /// <remarks>
                /// Specifies that the import library generated by this configuration should not be imported into dependent projects.
                /// </remarks>
                public enum IgnoreImportLibrary
                {
                    Enable,
                    [Default]
                    Disable
                }

                /// <summary>
                /// Use Library Dependency Inputs
                /// </summary>
                /// <remarks>
                /// Specifies whether or not the inputs to the librarian tool are used rather than the library file itself when linking in library outputs of project dependencies.
                /// </remarks>
                public enum UseLibraryDependencyInputs
                {
                    [Default]
                    Default,
                    Enable,
                    Disable
                }

                /// <summary>
                /// Ignore All Default Libraries
                /// </summary>
                /// <remarks>
                /// The /NODEFAULTLIB option tells the linker to remove one or more default libraries from the list of libraries it searches when resolving external references. 
                /// </remarks>
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

                /// <summary>
                /// Generate Manifest
                /// </summary>
                /// <remarks>
                /// Specifies that the linker should create a side-by-side manifest file.
                /// </remarks>
                public enum GenerateManifest
                {
                    [Default]
                    Enable,
                    Disable
                }

                /// <summary>
                /// Generate Debug Info
                /// </summary>
                /// <remarks>
                /// This option enables creation of debugging information for the .exe file or the DLL.
                ///
                /// Enable will write /DEBUG, and let MS linker decide to use FastLink or Full PDBs
                ///   If you want to force DEBUG:FULL, set both GenerateDebugInformation
                ///   and GenerateFullProgramDatabaseFile to Enable
                /// </remarks>
                public enum GenerateDebugInformation
                {
                    /// <summary>
                    /// Generate Debug Information
                    /// </summary>
                    /// <remarks>
                    /// Create a complete Program Database (PDB) ideal for distribution to Microsoft Symbol Server.
                    /// </remarks>
                    [Default]
                    Enable,

                    /// <summary>
                    /// Generate Debug Information optimized for faster links
                    /// </summary>
                    /// <remarks>
                    /// Produces a program database (PDB) ideal for edit-link-debug cycle.
                    /// </remarks>
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    EnableFastLink,

                    /// <summary>
                    /// No
                    /// </summary>
                    /// <remarks>
                    /// Produces no debugging information.
                    /// </remarks>
                    Disable,
                }

                /// <summary>
                /// Generate Full Program Database File
                /// </summary>
                /// <remarks>
                /// This option generates a full PDB from a partial PDB generated when /Debug:fastlink is specified. Full PDB allows sharing the binary and the PDB with others.
                /// </remarks>
                public enum GenerateFullProgramDatabaseFile
                {
                    [Default]
                    Default,
                    Disable,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Enable
                }

                /// <summary>
                /// Generate Map File
                /// </summary>
                /// <remarks>
                /// The /MAP option tells the linker to create a mapfile.
                /// </remarks>
                public enum GenerateMapFile
                {
                    Disable,
                    [Default]
                    Normal,
                    Full
                }

                /// <summary>
                /// Map Exports
                /// </summary>
                /// <remarks>
                /// The /MAPINFO option tells the linker to include the specified information in a mapfile, which is created if you specify the /MAP option. EXPORTS tells the linker to include exported functions.
                /// </remarks>
                public enum MapExports
                {
                    Enable,
                    [Default]
                    Disable
                }

                /// <summary>
                /// Debuggable Assembly
                /// </summary>
                /// <remarks>
                /// /ASSEMBLYDEBUG emits the DebuggableAttribute attribute with debug information tracking and disables JIT optimizations.
                /// </remarks>
                public enum AssemblyDebug
                {
                    [Default]
                    NoDebuggableAttributeEmitted,
                    RuntimeTrackingAndDisableOptimizations,
                    NoRuntimeTrackingAndEnableOptimizations
                }

                /// <summary>
                /// The /SUBSYSTEM option tells the operating system how to run the .exe file.The choice of subsystem affects the entry point symbol (or entry point function) that the linker will choose.
                /// </summary>
                public enum SubSystem
                {
                    /// <summary>
                    /// Not Set
                    /// </summary>
                    /// <remarks>
                    /// No subsystem set.
                    /// </remarks>
                    NotSet,

                    /// <summary>
                    /// Win32 character-mode application. Console applications are given a console by the operating system. If main or wmain is defined, CONSOLE is the default.
                    /// </summary>
                    [Default]
                    Console,

                    /// <summary>
                    /// Application does not require a console, probably because it creates its own windows for interaction with the user. If WinMain or wWinMain is defined, WINDOWS is the default.
                    /// </summary>
                    Windows,

                    /// <summary>
                    /// Device drivers for Windows NT. If /DRIVER:WDM is specified, NATIVE is the default.
                    /// </summary>
                    Native,

                    /// <summary>
                    /// EFI Application.
                    /// </summary>
                    EFI_Application,

                    /// <summary>
                    /// EFI Boot Service Driver.
                    /// </summary>
                    EFI_Boot_Service_Driver,

                    /// <summary>
                    /// EFI ROM.
                    /// </summary>
                    EFI_ROM,

                    /// <summary>
                    /// EFI Runtime.
                    /// </summary>
                    EFI_Runtime,

                    /// <summary>
                    /// Application that runs with the POSIX subsystem in Windows NT.
                    /// </summary>
                    POSIX,

                    [Obsolete("Use '" + nameof(Windows) + "' enum entry instead", error: false)]
                    Application = Windows
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

                /// <summary>
                /// Enable Large Addresses
                /// </summary>
                /// <remarks>
                /// The /LARGEADDRESSAWARE option tells the linker that the application can handle addresses larger than 2 gigabytes. By default, /LARGEADDRESSAWARE:NO is enabled if /LARGEADDRESSAWARE is not otherwise specified on the linker line.
                /// </remarks>
                public enum LargeAddress
                {
                    [Default]
                    Default,
                    NotSupportLargerThan2Gb,
                    SupportLargerThan2Gb
                }

                /// <summary>
                /// Allow Isolation
                /// </summary>
                /// <remarks>
                /// Specifies behavior for manifest lookup.
                /// </remarks>
                public enum AllowIsolation
                {
                    [Default]
                    Enabled,
                    Disabled
                }

                /// <summary>
                /// References
                /// </summary>
                /// <remarks>
                /// EliminateUnreferencedData (/OPT:REF) eliminates functions and/or data that are never referenced while KeepUnreferencedData (/OPT:NOREF) keeps functions and/or data that are never referenced.
                /// </remarks>
                public enum Reference
                {
                    [Default(Options.DefaultTarget.Debug)]
                    KeepUnreferencedData,
                    [Default(Options.DefaultTarget.Release)]
                    EliminateUnreferencedData
                }

                /// <summary>
                /// Enable COMDAT Folding
                /// </summary>
                public enum EnableCOMDATFolding
                {
                    [Default(Options.DefaultTarget.Debug)]
                    DoNotRemoveRedundantCOMDATs,
                    [Default(Options.DefaultTarget.Release)]
                    RemoveRedundantCOMDATs
                }

                /// <summary>
                /// Link Time Code Generation
                /// </summary>
                /// <remarks>
                /// Specifies link-time code generation.
                /// </remarks>
                public enum LinkTimeCodeGeneration
                {
                    /// <summary>
                    /// Default LTCG setting.
                    /// </summary>
                    [Default]
                    Default,

                    /// <summary>
                    /// Use Fast Link Time Code Generation
                    /// </summary>
                    /// <remarks>
                    /// Use Link Time Code Generation.
                    /// </remarks>
                    UseFastLinkTimeCodeGeneration,

                    /// <summary>
                    /// Use Link Time Code Generation
                    /// </summary>
                    /// <remarks>
                    /// Use Link Time Code Generation.
                    /// </remarks>
                    UseLinkTimeCodeGeneration,

                    /// <summary>
                    /// Profile Guided Optimization - Instrument
                    /// </summary>
                    /// <remarks>
                    /// Specifies link-time code generation.
                    /// </remarks>
                    ProfileGuidedOptimizationInstrument,

                    /// <summary>
                    /// Profile Guided Optimization - Optimization
                    /// </summary>
                    /// <remarks>
                    /// Specifies that the linker should use the profile data created after running the instrumented binary to create an optimized image.
                    /// </remarks>
                    ProfileGuidedOptimizationOptimize,

                    /// <summary>
                    /// Profile Guided Optimization - Update
                    /// </summary>
                    /// <remarks>
                    /// Allows and tracks list of input files to be added or modified from what was specified in the :PGINSTRUMENT phase.
                    /// </remarks>
                    ProfileGuidedOptimizationUpdate
                }

                public class FunctionOrder
                {
                    public FunctionOrder(string argOrder) { Order = argOrder; }

                    public string Order;
                }

                /// <summary>
                /// Randomized Base Address
                /// </summary>
                public enum RandomizedBaseAddress
                {
                    [Default]
                    Default,
                    Enable,
                    Disable
                }

                /// <summary>
                /// Fixed Base Address
                /// </summary>
                /// <remarks>
                /// Creates a program that can be loaded only at its preferred base address.
                /// </remarks>
                public enum FixedBaseAddress
                {
                    [Default]
                    Default,
                    Enable,
                    Disable
                }

                /// <summary>
                /// Create Hot Patchable Image
                /// </summary>
                /// <remarks>
                /// Prepares an image for hotpatching.
                /// </remarks>
                public enum CreateHotPatchableImage
                {
                    [Default]
                    Disable,

                    /// <summary>
                    /// Prepares an image for hotpatching.
                    /// </summary>
                    Enable,

                    /// <summary>
                    /// X86 Image Only
                    /// </summary>
                    /// <remarks>
                    /// Prepares an X86 image for hotpatching.
                    /// </remarks>
                    X86Image,

                    /// <summary>
                    /// X64 Image Only
                    /// </summary>
                    /// <remarks>
                    /// Prepares an X64 image for hotpatching.
                    /// </remarks>
                    X64Image,

                    /// <summary>
                    /// Itanium Image Only
                    /// </summary>
                    /// <remarks>
                    /// Prepares an Itanium image for hotpatching.
                    /// </remarks>
                    ItaniumImage
                }

                /// <summary>
                /// Force File Output
                /// </summary>
                /// <remarks>
                /// Tells the linker to create an .exe file or DLL even if a symbol is referenced but not defined or is multiply defined. It may create invalid exe file.
                /// </remarks>
                public enum ForceFileOutput
                {
                    [Default]
                    Default,

                    /// <summary>
                    /// /FORCE with no arguments implies both multiple and unresolved.
                    /// </summary>
                    Enable,

                    /// <summary>
                    /// Multiply Defined Symbol Only
                    /// </summary>
                    /// <remarks>
                    /// Use /FORCE:MULTIPLE to create an output file whether or not LINK finds more than one definition for a symbol.
                    /// </remarks>
                    MultiplyDefinedSymbolOnly,

                    /// <summary>
                    /// Undefined Symbol Only
                    /// </summary>
                    /// <remarks>
                    /// Use /FORCE:UNRESOLVED to create an output file whether or not LINK finds an undefined symbol. /FORCE:UNRESOLVED is ignored if the entry point symbol is unresolved.
                    /// </remarks>
                    UndefinedSymbolOnly
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

                /// <summary>
                /// Generate Windows Metadata
                /// </summary>
                /// <remarks>
                /// Enables or disable generation of Windows Metadata.
                /// </remarks>
                public enum GenerateWindowsMetadata
                {
                    [Default]
                    Default,

                    /// <summary>
                    /// Yes
                    /// </summary>
                    /// <remarks>
                    /// Enable generation of Windows Metadata files.
                    /// </remarks>
                    Enable,

                    /// <summary>
                    /// No
                    /// </summary>
                    /// <remarks>
                    /// Disable the generation of Windows Metadata files.
                    /// </remarks>
                    Disable
                }

                /// <summary>
                /// Treat Linker Warning As Errors
                /// </summary>
                /// <remarks>
                /// Causes no output file to be generated if the linker generates a warning.
                /// </remarks>
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
                /// <summary>
                /// Precompiled Header
                /// </summary>
                /// <remarks>
                /// Create/Use Precompiled Header : Enables creation or use of a precompiled header during the build.
                /// </remarks>
                public enum PrecompiledHeader
                {
                    /// <summary>
                    /// Not Using Precompiled Headers
                    /// </summary>
                    NotUsingPrecompiledHeaders,

                    /// <summary>
                    /// Instructs the compiler to create a precompiled header (.pch) file that represents the state of compilation at a certain point.
                    /// </summary>
                    CreatePrecompiledHeader,

                    /// <summary>
                    /// Instructs the compiler to use an existing precompiled header (.pch) file in the current compilation.
                    /// </summary>
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
