// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.FastBuild
{
    public interface IApplePlatformBff : IClangPlatformBff
    {
        bool IsSwiftSupported();
        /// <summary>
        /// Gets a configuration name for that platform in the .bff file for the code files that
        /// are written in swift code.
        /// </summary>
        string SwiftConfigName(Configuration conf);
        void SetupSwiftOptions(IFileGenerator generator);
    }
}
