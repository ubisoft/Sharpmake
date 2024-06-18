// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace HelloXCode
{
    [Sharpmake.Generate]
    public class Dll1Project : CommonProject
    {
        public Dll1Project()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "dll1";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "SharedLibs";

            conf.Output = Configuration.OutputType.Dll;

            conf.PrecompHeader = "precomp.h";
            conf.PrecompSource = "precomp.cpp";

            conf.Defines.Add("UTIL_DLL_EXPORT");
            conf.ExportDefines.Add("UTIL_DLL_IMPORT");

            conf.IncludePaths.Add(SourceRootPath);

            conf.AddPrivateDependency<StaticLib1Project>(target);
            conf.IncludeSystemPaths.Add("[project.SourceRootPath]/systemincludedll");
        }

        public override void ConfigureMac(Configuration conf, CommonTarget target)
        {
            base.ConfigureMac(conf, target);

            conf.Options.Add(new Options.XCode.Linker.DyLibInstallName(@"@executable_path/libdll1.dylib"));
        }
    }
}
