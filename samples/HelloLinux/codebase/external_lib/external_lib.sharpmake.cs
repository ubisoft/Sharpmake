// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System;
using Sharpmake;
using System.Linq;

namespace HelloLinux
{
    [Export]
    //This export keyword will:
    // 1 - Prevent the generation of a dedicated makefile for this project
    // 2 - Prevent the inclusion of this project into the LDDEPS list of your makefile 
    // In this example only the configured projectName will be added into the LDLIBS flag in the generated makefile.
    public class ExternalLibProject : CommonProject
    {
        public ExternalLibProject()
        {
            AddTargets(CommonTarget.GetDefaultTargets());

            //We name this project voluntarly curl in order to link to a library already existing in this OS. 
            Name = "curl";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            //We use the static library format because it's the one already existing inside the OS.
            conf.Output = Configuration.OutputType.Lib;
        }
    }
}
