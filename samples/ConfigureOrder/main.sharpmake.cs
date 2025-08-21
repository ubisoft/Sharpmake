// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

[module: Sharpmake.Include("ConfigureOrdering.sharpmake.cs")]
[module: Sharpmake.Include("Util.sharpmake.cs")]

namespace ConfigureOrdering
{
    public class main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_22621_0);

            arguments.Generate<ConfigureOrderingSolution>();
        }
    }
}
