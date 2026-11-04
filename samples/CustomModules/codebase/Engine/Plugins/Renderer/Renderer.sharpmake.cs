// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace CustomModules
{
    public class RendererModule : Module
    {
        public RendererModule()
        {
            AddDependency<CoreModule>();
        }

        public override IEnumerable<string> PublicDefines => new[] { "WITH_RENDERER" };
    }
}
