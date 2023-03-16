// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public static partial class Apple
    {
        public sealed partial class watchOsPlatform
        {
            public const string _compilerExtraOptionsGeneral = @"
            + ' [cmdLineOptions.WatchOSDeploymentTarget]'
";
        }
    }
}
