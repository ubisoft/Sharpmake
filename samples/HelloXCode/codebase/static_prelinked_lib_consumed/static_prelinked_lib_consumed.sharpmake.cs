// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Sharpmake;

namespace HelloXCode
{
    /// <summary>
    /// This project tests the XCode's Pre-Linked libraries feature.
    /// The library exposes methods and variables that will never be directly linked into EXE file,
    /// but instead, will be pre-linked in "Consumer" library.
    /// </summary>
    [Sharpmake.Generate]
    public class StaticPrelinkedLibConsumed : CommonProject
    {
        public StaticPrelinkedLibConsumed()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "static_prelinked_lib_consumed";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "StaticLibs";

            conf.IncludePaths.Add(SourceRootPath);
        }
    }
}
