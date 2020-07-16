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
using System.Collections.Generic;

namespace Sharpmake.Generators.FastBuild
{
    //
    // TODO: Streamline this with the Vcxproj interfaces. There is no reason why BFF and VCXPROJ
    //       generation need to specify twice the platform defines, why both have to specify
    //       whether it takes a lib- prefix before libraries, etc. Furthermore, these interfaces
    //       should be about providing the information required to generate the files, and less
    //       about participating in the generation.
    //

    /// <summary>
    /// Interface that exposes the required methods and properties to generate a .bff file for
    /// FastBuild using Sharpmake.
    /// </summary>
    public interface IPlatformBff
    {
        /// <summary>
        /// Gets the main `#define` symbol for that platform in the BFF file.
        /// </summary>
        /// <remarks>
        /// Note that this is *NOT* the C or C++ define symbol. The BFF scripts support the
        /// `#define` instruction, and this property returns a symbol that tells the scripts
        /// whether we're dealing with a given platform.
        /// </remarks>
        string BffPlatformDefine { get; }

        /// <summary>
        /// Gets a configuration name for that platform in the .bff file for the code files that
        /// are written in native C code.
        /// </summary>
        string CConfigName(Configuration conf);

        /// <summary>
        /// Gets a configuration name for that platform in the .bff file for the code files that
        /// are written in native C++ code.
        /// </summary>
        string CppConfigName(Configuration conf);

        /// <summary>
        /// Gets whether a library prefix (usually `lib`) is required on that platform when
        /// building libraries.
        /// </summary>
        /// <param name="conf">The <see cref="Configuration"/> under which the check is requested.</param>
        /// <returns>`true` if a prefix is required, `false` otherwise.</returns>
        bool AddLibPrefix(Configuration conf);

        void SelectPreprocessorDefinitionsBff(IBffGenerationContext context);

        /// <summary>
        /// Setups extra linker settings for linking with that platform.
        /// </summary>
        /// <param name="fileGenerator">A <see cref="IFileGenerator"/> for writing the file.</param>
        /// <param name="configuration">The project configuration</param>
        /// <param name="fastBuildOutputFile">The file name of the build output.</param>
        void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration configuration, string fastBuildOutputFile);

        [Obsolete("Use " + nameof(SetupExtraLinkerSettings) + " and pass the conf")]
        void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration.OutputType outputType, string fastBuildOutputFile);

        /// <summary>
        /// Get the extra list of build steps to execute for this platform.
        /// </summary>
        /// <param name="configuration">The project configuration</param>
        /// <param name="fastBuildOutputFile">The file name of the build output.</param>
        /// <returns>The list of post build step to execute.</returns>
        IEnumerable<Project.Configuration.BuildStepBase> GetExtraPostBuildEvents(Project.Configuration configuration, string fastBuildOutputFile);

        /// <summary>
        /// Get the linker output name for this platform.
        /// </summary>
        /// <param name="outputType">The project output type</param>
        /// <param name="fastBuildOutputFile">The original file name of the build output.</param>
        /// <returns>The final file name of the build output.</returns>
        string GetOutputFilename(Project.Configuration.OutputType outputType, string fastBuildOutputFile);

        void AddCompilerSettings(
            IDictionary<string, CompilerSettings> masterCompilerSettings,
            Project.Configuration conf);
    }
}
