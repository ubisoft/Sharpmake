// Copyright (c) 2017 Ubisoft Entertainment
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
    public abstract partial class BaseApplePlatform
    {
        private const string _linkerOptionsTemplate = @"
    .LinkerOptions          = '-o ""%2"" ""%1""[outputTypeArgument]'
                            // Library Search Path
                            // ---------------------------
                            + ' [cmdLineOptions.AdditionalLibraryDirectories]'
                            // Libraries
                            // ---------------------------
                            + ' [cmdLineOptions.AdditionalDependencies]'
                            // Options
                            //--------
                            + ' [cmdLineOptions.GenerateMapFile]'
                            // Additional linker options
                            //--------------------------
                            + ' [options.AdditionalLinkerOptions]'
";

        private const string _compilerExtraOptionsGeneral = @"
    .CompilerExtraOptions   = ''
            // General options
            // -------------------------
            + ' [cmdLineOptions.AdditionalIncludeDirectories]'
            + ' [cmdLineOptions.AdditionalUsingDirectories]'
            + ' [cmdLineOptions.PreprocessorDefinitions]'
            + ' [cmdLineOptions.LibraryStandard]'
            + ' [cmdLineOptions.SDKRoot]'
            + ' [options.ClangCppLanguageStandard]'
";

        private const string _compilerExtraOptionsAdditional = @"
            // Additional compiler options
            //--------------------------
            + ' [options.AdditionalCompilerOptions]'
";

        private const string _compilerOptimizationOptions =
                @"
    // Optimizations options
    // ---------------------
    .CompilerOptimizations = ''
            + ' [cmdLineOptions.OptimizationLevel]'
";
    }
}
