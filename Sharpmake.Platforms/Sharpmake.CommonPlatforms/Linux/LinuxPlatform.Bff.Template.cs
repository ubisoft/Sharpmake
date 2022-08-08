// Copyright (c) 2020, 2022 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Sharpmake
{
    public static partial class Linux
    {
        public sealed partial class LinuxPlatform
        {
            public const string _linkerOptionsTemplate = @"
    .LinkerOptions          = '-o ""%2""'
                            + ' [cmdLineOptions.WholeArchiveBegin]'
                            + ' ""%1""'
                            + ' [cmdLineOptions.WholeArchiveEnd]'
                            + ' [sharedOption]'
                            + ' [cmdLineOptions.BffSysRoot]'
                            // Library Search Path
                            // ---------------------------
                            + ' [cmdLineOptions.AdditionalLibraryDirectories]'
                            // Libraries
                            // ---------------------------
                            + ' [cmdLineOptions.AdditionalDependencies]'
                            // Options
                            //--------
                            + ' [cmdLineOptions.StdLib]'
                            + ' [cmdLineOptions.EditAndContinue]'
                            + ' [cmdLineOptions.InfoStripping]'
                            + ' [cmdLineOptions.DataStripping]'
                            + ' [cmdLineOptions.DuplicateStripping]'
                            + ' [cmdLineOptions.Addressing]'
                            + ' [cmdLineOptions.GenerateMapFile]'
                            // Additional linker options
                            //--------------------------
                            + ' [options.AdditionalLinkerOptions]'
";

            public const string _compilerExtraOptions = @"
    .CompilerExtraOptions   = ''
            // General options
            // -------------------------
            + ' [cmdLineOptions.StdLib]'
            + ' [cmdLineOptions.DebugInformationFormat]'
            + ' [cmdLineOptions.AdditionalIncludeDirectories]'
            + ' [cmdLineOptions.AdditionalUsingDirectories]'
            + ' [cmdLineOptions.PreprocessorDefinitions]'
            + ' [cmdLineOptions.UndefinePreprocessorDefinitions]'
            + ' [cmdLineOptions.UndefineAllPreprocessorDefinitions]'
            + ' [cmdLineOptions.Warnings]'
            + ' [cmdLineOptions.ExtraWarnings]'
            + ' [cmdLineOptions.WarningsAsErrors]'
            + ' [cmdLineOptions.PositionIndependentCode]'
            + ' [cmdLineOptions.FastMath]'
            + ' [cmdLineOptions.NoStrictAliasing]'
            + ' [cmdLineOptions.UnrollLoops]'
            + ' [cmdLineOptions.LinkTimeOptimization]'
            + ' [cmdLineOptions.AnsiCompliance]'
            + ' [cmdLineOptions.RuntimeTypeInfo]'
            + ' [cmdLineOptions.CharUnsigned]'
            + ' [cmdLineOptions.MsExtensions]'
            + ' [cmdLineOptions.CppLanguageStd]'
            + ' [cmdLineOptions.CLanguageStd]'
            + ' [cmdLineOptions.BffSysRoot]'
            // Additional compiler options
            //--------------------------
            + ' [options.AdditionalCompilerOptions]'
";

            public const string _compilerOptimizationOptions =
                @"
    // Optimizations options
    // ---------------------
    .CompilerOptimizations = ''
            + ' [cmdLineOptions.OptimizationLevel]'
";
        }
    }
}
