// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;

namespace Sharpmake
{
    public static partial class Options
    {
        public static class Android // TODO: move this to the CommonPlatforms module
        {
            public static class General
            {
                /// <summary>
                /// Android SDK path
                /// If unset, will use Android.GlobalSettings.AndroidHome
                /// </summary>
                public class AndroidHome : PathOption
                {
                    public AndroidHome(string path)
                        : base(path) { }
                }

                /// <summary>
                /// Android NDK path
                /// If unset, will use Android.GlobalSettings.NdkRoot
                /// </summary>
                public class NdkRoot : PathOption
                {
                    public NdkRoot(string path)
                        : base(path) { }
                }

                /// <summary>
                /// Java SE Development Kit path
                /// If unset, will use Android.GlobalSettings.JavaHome
                /// </summary>
                public class JavaHome : PathOption
                {
                    public JavaHome(string path)
                        : base(path) { }
                }

                /// <summary>
                /// Apache Ant path
                /// If unset, will use Android.GlobalSettings.AntHome
                /// </summary>
                public class AntHome : PathOption
                {
                    public AntHome(string path)
                        : base(path) { }
                }

                /// <summary>
                /// Path to the AndroidProj MSBuild files
                /// Expected to contain the files found in MSBuild\Microsoft\MDD\Android\V150
                /// If unset, line won't be written
                /// </summary>
                public class AndroidTargetsPath : PathOption
                {
                    public AndroidTargetsPath(string path)
                        : base(path) { }
                }

                /// <summary>
                /// Application Type Revision
                /// This must be a valid version string, of the form major.minor[.build[.revision]].
                /// Examples: 1.0, 10.0.0.0
                /// </summary>
                public class ApplicationTypeRevision : StringOption
                {
                    public static readonly string Default = "3.0";
                    public ApplicationTypeRevision(string revision)
                        : base(revision) { }
                }

                /// <summary>
                /// This is applicable for AGDE only
                /// The full path to the directory containing the top-level build.gradle file.
                /// </summary>
                [Obsolete("Use the option in Agde instead.")]
                public class AndroidGradleBuildDir : PathOption
                {
                    public AndroidGradleBuildDir(string androidGradleBuildDir)
                       : base(androidGradleBuildDir) { }
                }

                /// <summary>
                /// Output Apk name for AGDE project which can be set per configuration.
                /// </summary>
                [Obsolete("Use the option in Agde instead.")]
                public class AndroidApkName : StringOption
                {
                    public AndroidApkName(string androidApkName)
                       : base(androidApkName) { }
                }

                /// <summary>
                /// Verbosity of the tasks (vcxproj only)
                /// At the time of this writing, this only control if on build env variables values are printed
                /// </summary>
                public enum ShowAndroidPathsVerbosity
                {
                    [Default]
                    Default,
                    High,
                    Normal,
                    Low
                }

                public enum AndroidAPILevel
                {
                    [Default]
                    Default,
                    Latest, // sharpmake will try and auto-detect the latest installed, or fallback to default: note that the SDK/NDK paths are needed
                    Android16, // Jelly Bean 4.1.x
                    Android17, // Jelly Bean 4.2.x
                    Android18, // Jelly Bean 4.3.x
                    Android19, // KitKat 4.4 - 4.4.4
                    Android20, // Does it really exist?
                    Android21, // Lollipop 5.0 - 5.0.2
                    Android22, // Lollipop 5.1
                    Android23, // Marshmallow 6.0
                    Android24, // Nougat 7.0
                    Android25, // Nougat 7.1
                    Android26, // Oreo 8.0.0
                    Android27, // Oreo 8.1.0
                    Android28, // Pie 9.0
                    Android29, // Android 10
                    Android30, // Android 11
                }

                public enum PlatformToolset
                {
                    [Default]
                    Default,
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Clang_3_6, // needs ApplicationTypeRevision 1.0
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Clang_3_8, // needs ApplicationTypeRevision 2.0 or 3.0
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Clang_5_0, // needs ApplicationTypeRevision 3.0
                    [DevEnvVersion(minimum = DevEnv.vs2015)]
                    Gcc_4_9 // needs ApplicationTypeRevision 1.0 or 2.0 or 3.0
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
                    Default,
                    System,
                    GAbiPP_Static,
                    GAbiPP_Shared,
                    StlPort_Static,
                    StlPort_Shared,
                    GnuStl_Static,
                    GnuStl_Shared,
                    LibCpp_Static,
                    [Default]
                    LibCpp_Shared
                }

                // This is applicable for AGDE only
                // Link time optimization, may also be required for some sanitizers.
                [Obsolete("Use the option in Agde instead.")]
                public enum LinkTimeOptimization
                {
                    [Default]
                    None,
                    LinkTimeOptimization,
                    ThinLinkTimeOptimization
                }

                // This is applicable for AGDE only
                // Set the flag '-fuse-ld=' which specifies which linker to use.
                [Obsolete("Use the option in Agde instead.")]
                public enum ClangLinkType
                {
                    None,
                    DeferToNdk,
                    gold,
                    [Default]
                    lld,
                    bfd
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
                    Cpp11,
                    Cpp1y,
                    Cpp14,
                    Cpp17,
                    Cpp1z,
                    Cpp2a,
                    GNU_Cpp98,
                    GNU_Cpp11,
                    GNU_Cpp1y,
                    GNU_Cpp14,
                    GNU_Cpp17,
                    GNU_Cpp1z,
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
