// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    /// <summary>
    /// Interface for objects that describe the general properties of a platform.
    /// </summary>
    public interface IPlatformDescriptor
    {
        /// <summary>
        /// A simple, human-readable string that describes the platform.
        /// This is used to generate file and folder paths, symbol names, and error messages.
        /// </summary>
        string SimplePlatformString { get; }

        /// <summary>
        /// Returns the name used by the toolchain to identify the platform under a given build target.
        /// In Visual Studio, this is the second part of the configuration name; for example, for
        /// "Debug|x64" you would receive "x64" even though Sharpmake calls this platform "Win64". It depends on the
        /// target: a Win64 target with the arm64 architecture gives "ARM64".
        /// </summary>
        string GetToolchainPlatformString(ITarget target);

        /// <summary>
        /// Gets whether this is a proprietary platform owned by Microsoft Corporation.
        /// </summary>
        bool IsMicrosoftPlatform { get; }

        /// <summary>
        /// Gets whether this is a PC platform. (Mac, Windows, etc.)
        /// </summary>
        bool IsPcPlatform { get; }

        /// <summary>
        /// Gets whether this platform supports Clang.
        /// </summary>
        bool IsUsingClang { get; }

        /// <summary>
        /// Get whether this platform launch the linker via the compiler
        /// </summary>
        bool IsLinkerInvokedViaCompiler { get; }

        /// <summary>
        /// Gets whether this is a .NET platform.
        /// </summary>
        bool HasDotNetSupport { get; }

        /// <summary>
        /// Gets whether that platform supports shared libraries. (aka: dynamic link libraries.)
        /// </summary>
        bool HasSharedLibrarySupport { get; }

        /// <summary>
        /// Gets whether precompiled headers are supported for that platform.
        /// </summary>
        bool HasPrecompiledHeaderSupport { get; }

        /// <summary>
        /// Gets an environment variable resolver suited for this platform.
        /// </summary>
        /// <param name="variables">A list of <see cref="VariableAssignment"/> that describe the environment variables to resolve.</param>
        /// <returns>An <see cref="EnvironmentVariableResolver"/> instance suited for the platform.</returns>
        EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] variables);

        /// <summary>
        /// Gets the processor architecture used by targets of this platform that don't set a
        /// <see cref="ProcessorArchitecture"/> fragment. 0 means the descriptor doesn't declare the platform's architecture yet.
        /// </summary>
        /// <remarks>
        /// Descriptors written before <see cref="ProcessorArchitecture"/> existed keep the default (0):
        /// they accept no explicit architecture and only <see cref="InstructionSetLevel.Baseline"/>,
        /// so new combinations are rejected rather than silently generated wrong.
        /// </remarks>
        ProcessorArchitecture DefaultProcessorArchitecture => 0;

        /// <summary>
        /// Gets the processor architectures that targets of this platform may set explicitly.
        /// </summary>
        ProcessorArchitecture SupportedProcessorArchitectures => 0;

        /// <summary>
        /// Gets the instruction set levels that targets of this platform may use with the given architecture.
        /// </summary>
        InstructionSetLevel GetSupportedInstructionSetLevels(ProcessorArchitecture architecture) => InstructionSetLevel.Baseline;

        /// <summary>
        /// Gets the platform name of the solution configuration of a target, shown in the Visual Studio platform dropdown.
        /// Defaults to <see cref="GetToolchainPlatformString(ITarget)"/>. Differs from it when targets share a toolchain
        /// platform but must be selectable separately (e.g. "x64-APX" next to "x64").
        /// Not the project-level platform written in the .vcxproj: that one is <see cref="GetToolchainPlatformString(ITarget)"/>.
        /// </summary>
        string GetSolutionPlatformName(ITarget target) => GetToolchainPlatformString(target);

        /// <summary>
        /// Like <see cref="SimplePlatformString"/>, but for a given target: adds a token for a non-default
        /// architecture or instruction set level (e.g. "Win64APX"). Used to build folder paths, so it never contains spaces.
        /// </summary>
        string GetDisambiguatedPlatformString(ITarget target) => Util.AppendArchitectureTokens(SimplePlatformString, target);

        /// <summary>
        /// Gets the suffix, with its leading space, appended to the project configuration name of targets that
        /// share a toolchain platform with another instruction set level (e.g. " APX" gives "Release APX|x64").
        /// </summary>
        string GetConfigurationNameSuffix(ITarget target) => Util.DefaultConfigurationNameSuffix(target);
    }
}
