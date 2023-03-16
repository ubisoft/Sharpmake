// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    /// <summary>
    /// Interface for objects that expose additional command line interfaces for a given platform.
    /// This allows platforms to extend the command line interface of Sharpmake.
    /// </summary>
    public interface ICommandLineInterface
    {
        /// <summary>
        /// Validates that the command line arguments are valid.
        /// </summary>
        void Validate();
    }
}
