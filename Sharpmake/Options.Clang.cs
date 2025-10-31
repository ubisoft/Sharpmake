// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Sharpmake
{
    public static partial class Options
    {
        public static class Clang
        {
            /// <summary>
            /// Target format : <arch><sub>-<vendor>-<sys>-<abi>
            /// https://clang.llvm.org/docs/CrossCompilation.html#target-triple
            /// </summary>
            /// <param name="arch"></param>
            /// <param name="sub"></param>
            /// <param name="vendor"></param>
            /// <param name="sys"></param>
            /// <param name="abi"></param>
            /// <returns></returns>
            public static string GetTargetTriple(string arch, string sub, string vendor, string sys, string abi)
            {
                var targetElements = new List<string>();
                targetElements.Add($"{arch}{sub}");
                if (!string.IsNullOrEmpty(vendor))
                    targetElements.Add(vendor);
                if (!string.IsNullOrEmpty(sys))
                    targetElements.Add(sys);
                if (!string.IsNullOrEmpty(abi))
                    targetElements.Add(abi);
                return string.Join("-", targetElements);
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
                    Cpp2a,
                    GnuCpp98,
                    GnuCpp11,
                    GnuCpp14,
                    GnuCpp17,
                    GnuCpp2a
                }

                public enum CLanguageStandard
                {
                    [Default]
                    Default,
                    C89,
                    C90,
                    C99,
                    C11,
                    C17,
                    GnuC89,
                    GnuC90,
                    GnuC99,
                    GnuC11,
                    GnuC17,
                }

                public enum Exceptions
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum ExtraWarnings
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum GenerateDebugInformation
                {
                    [Default]
                    Enable,
                    Disable
                }

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

                public static class ProfileGuidedOptimization
                {
                    /// <summary>
                    /// Use profile data for profile-guided optimization.
                    /// </summary>
                    /// <remarks>
                    /// Note that Sharpmake's usual placeholder strings can be used in path.
                    /// For more information, see https://clang.llvm.org/docs/UsersManual.html#cmdoption-fprofile-use
                    /// </remarks>
                    public class Use : PathOption
                    {
                        public Use(string path) : base(path) { }
                    }

                    /// <summary>
                    /// Generate instrumented code to collect raw profile data in the directory specified as directoryPath.
                    /// </summary>
                    /// <remarks>
                    /// This option cannot be used at the same time as the GenerateCS option.
                    /// Note that Sharpmake's usual placeholder strings can be used in directoryPath.
                    /// For more information, see https://clang.llvm.org/docs/UsersManual.html#cmdoption-fprofile-generate
                    /// </remarks>
                    public class Generate : PathOption
                    {
                        public Generate(string directoryPath = null) : base(directoryPath) { }
                    }

                    /// <summary>
                    /// Generate context-sensitive (i.e. post-inlining) instrumented code to collect raw profile data in the directory specified as directoryPath.
                    /// </summary>
                    /// <remarks>
                    /// This option cannot be used at the same time as the Generate option.
                    /// Note that Sharpmake's usual placeholder strings can be used in directoryPath.
                    /// For more information, see https://clang.llvm.org/docs/UsersManual.html#cmdoption-fcs-profile-generate
                    /// </remarks>
                    public class GenerateCS: PathOption
                    {
                        public GenerateCS(string directoryPath = null) : base(directoryPath) { }
                    }
                }

                public class ValueProfileCountersPerSite : IntOption
                {
                    public ValueProfileCountersPerSite(int counter) : base(counter) { }
                }

                public enum Rtti
                {
                    [Default]
                    Disable,
                    Enable
                }

                public enum TreatWarningsAsErrors
                {
                    Enable,
                    [Default]
                    Disable
                }

                public enum Warnings
                {
                    NormalWarnings,
                    [Default]
                    MoreWarnings,
                    Disable
                }

                public class MscVersion : StringOption
                {
                    public MscVersion(string option) : base(option) { }
                }

                /// <summary>
                /// Foce a platform toolset different from the devenv when using LLVM platform toolset
                /// </summary>
                /// The platform toolset option used to enable the LLVM toolchain doesn't allow choosing
                /// the version of the toolset we want to mimic. By
                public class LLVMVcPlatformToolset : WithArgOption<Vc.General.PlatformToolset>
                {
                    public LLVMVcPlatformToolset(Vc.General.PlatformToolset vcPlatformToolset) : base(vcPlatformToolset) { }
                }
            }

            public static class Linker
            {
                public enum ExtTspBlockPlacement
                {
                    [Default]
                    Disable,
                    Enable
                }
            }
        }
    }
}
