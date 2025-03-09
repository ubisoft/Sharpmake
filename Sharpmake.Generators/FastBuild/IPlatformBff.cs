// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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

        void SelectPreprocessorDefinitionsBff(IBffGenerationContext context);
        void SelectAdditionalCompilerOptionsBff(IBffGenerationContext context);

        /// <summary>
        /// Setups extra linker settings for linking with that platform.
        /// </summary>
        /// <param name="fileGenerator">A <see cref="IFileGenerator"/> for writing the file.</param>
        /// <param name="configuration">The project configuration</param>
        /// <param name="fastBuildOutputFile">The file name of the build output.</param>
        void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration configuration, string fastBuildOutputFile);

        /// <summary>
        /// Get the extra list of build steps to execute for this platform.
        /// </summary>
        /// <param name="configuration">The project configuration</param>
        /// <param name="fastBuildOutputFile">The file name of the build output.</param>
        /// <returns>The list of post build step to execute.</returns>
        IEnumerable<Project.Configuration.BuildStepBase> GetExtraPostBuildEvents(Project.Configuration configuration, string fastBuildOutputFile);

        /// <summary>
        /// Get the extra list of stamp steps to execute for this platform.
        /// </summary>
        /// <param name="configuration">The project configuration</param>
        /// <param name="fastBuildOutputFile">The file name of the build output.</param>
        /// <returns>The list of stamp step to execute.</returns>
        IEnumerable<Project.Configuration.BuildStepExecutable> GetExtraStampEvents(Project.Configuration configuration, string fastBuildOutputFile);

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
