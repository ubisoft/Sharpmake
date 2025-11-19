// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace ConfigureOrdering
{
    public class Util
    {
        private static Target s_defaultTarget;
        public static Target DefaultTarget
        {
            get
            {
                if (s_defaultTarget == null)
                {
                    s_defaultTarget = new Target(
                        Platform.win32,
                        DevEnv.vs2022,
                        Optimization.Release,
                        OutputType.Lib,
                        Blob.NoBlob,
                        BuildSystem.MSBuild,
                        DotNetFramework.net8_0
                    );
                }
                return s_defaultTarget;
            }
        }
    }
}
