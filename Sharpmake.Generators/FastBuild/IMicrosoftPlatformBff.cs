// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.FastBuild
{
    /// <summary>
    /// Augments the <see cref="IPlatformBff"/> interface to provide services for Microsoft's
    /// proprietary platforms.
    /// </summary>
    public interface IMicrosoftPlatformBff : IPlatformBff
    {
        bool SupportsResourceFiles { get; }
    }
}
