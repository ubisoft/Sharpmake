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
                    GnuCpp98,
                    GnuCpp11,
                    GnuCpp14
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
        }
    }
}
