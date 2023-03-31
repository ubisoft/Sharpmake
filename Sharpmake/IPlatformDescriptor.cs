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
        /// Gets a simple string that describes the platform.
        /// </summary>
        string SimplePlatformString { get; }

        string GetPlatformString(ITarget target);

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
    }
}
