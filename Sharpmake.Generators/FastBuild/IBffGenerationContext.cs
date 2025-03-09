// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpmake.Generators.FastBuild
{
    public interface IBffGenerationContext : IGenerationContext
    {
        IReadOnlyList<Project.Configuration> ProjectConfigurations { get; }
    }
}
