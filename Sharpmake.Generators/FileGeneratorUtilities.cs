// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators
{
    /// <summary>
    /// Utilities for file generation.
    /// </summary>
    public static class FileGeneratorUtilities
    {
        /// <summary>
        /// A string that, when put on a line of a file during generation, will cause the line to
        /// be removed from the generated files.
        /// </summary>
        public const string RemoveLineTag = "REMOVE_LINE_TAG";
    }
}
