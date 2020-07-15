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
