// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.FastBuild
{
    /// <summary>
    /// Augments the <see cref="IPlatformBff"/> interface to provide services for platforms that
    /// are based on Clang.
    /// </summary>
    public interface IClangPlatformBff : IPlatformBff
    {
        void SetupClangOptions(IFileGenerator generator);
    }
}
