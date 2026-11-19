// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators.FastBuild
{
    /// <summary>
    /// Optional interface for platforms whose C and C++ files need different platform include paths.
    /// When implemented, the paths returned replace the ones from
    /// <see cref="IPlatformVcxproj.GetPlatformIncludePathsWithPrefix"/> for files compiled as C.
    /// </summary>
    internal interface ICPlatformIncludesBff
    {
        IEnumerable<IncludeWithPrefix> GetPlatformIncludePathsWithPrefixForC(IGenerationContext context);
    }
}
