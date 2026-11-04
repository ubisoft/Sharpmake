// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using Sharpmake;

namespace CustomModules
{
    public class CoreModule : Module
    {
        public CoreModule()
        {
        }

        public override IEnumerable<string> PublicDefines => new[] { "WITH_CORE" };

        public override void Configure(Project.Configuration conf, Target target)
        {
            conf.Defines.Add("CORE_BUILD");
        }
    }
}
