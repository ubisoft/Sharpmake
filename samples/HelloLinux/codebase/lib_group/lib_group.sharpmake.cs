// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace HelloLinux
{
    [Sharpmake.Generate]
    public class LibGroupProject : CommonProject
    {
        public LibGroupProject()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "lib_group";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);
            ConfigureLibraries(conf, target);

            conf.Output = Configuration.OutputType.Dll;
            conf.IncludePaths.Add(SourceRootPath);
            conf.SolutionFolder = "SharedLibs";

            conf.PrecompHeader = "precomp.h";
            conf.PrecompSource = "precomp.cpp";

            conf.AdditionalCompilerOptions.Add("-fPIC");

            conf.Defines.Add("UTIL_DLL_EXPORT");
            conf.ExportDefines.Add("UTIL_DLL_IMPORT");            
        }

        public void ConfigureLibraries(Configuration conf, CommonTarget target) {

            // Enable library group with Clang and GCC to allow libraries that have circular dependencies.
            conf.Options.Add(Options.Makefile.Linker.LibGroup.Enable);

            conf.LibraryFiles.Add("m");
            conf.AddPrivateDependency<StaticLib1Project>(target);
            conf.AddPrivateDependency<Dll1Project>(target);
            conf.AddPrivateDependency<ExternalLibProject>(target);

        }
    }
}
