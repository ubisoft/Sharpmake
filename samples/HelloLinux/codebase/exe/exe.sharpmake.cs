// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace HelloLinux
{
    [Sharpmake.Generate]
    public class ExeProject : CommonProject
    {
        public ExeProject()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "exe";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.Exe;

            conf.PrecompHeader = "stdafx.h";
            conf.PrecompSource = "stdafx.cpp";

            // this tells the shared lib loader to look in the exe dir
            // note: because we write in makefiles we double the $ to escape it
            conf.AdditionalLinkerOptions.Add("-Wl,-rpath='$$ORIGIN'");

            conf.LibraryFiles.Add("libuuid.so");

            conf.AddPrivateDependency<Dll1Project>(target);
            conf.AddPrivateDependency<StaticLib2Project>(target);
            conf.AddPrivateDependency<HeaderOnlyLibProject>(target);
            conf.AddPrivateDependency<ExternalLibProject>(target);
            conf.AddPrivateDependency<LibGroupProject>(target);

            conf.Defines.Add("CREATION_DATE=\"October 2020\"");
        }
    }
}
