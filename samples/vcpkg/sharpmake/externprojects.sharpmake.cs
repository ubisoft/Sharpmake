// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace VCPKGSample.Extern
{
    // Add a dependency to this project to be able to use vcpkg packages in a project.
    //
    // This project then setup the necessary include and library paths to be able to reference any installed vcpkg package in
    // our local vcpackage installation.
    //
    // Note: The required vcpkg packages are installed by bootstrap-sample.bat
    //
    [Sharpmake.Export]
    public class VCPKG : ExportProject
    {
        public override void ConfigureRelease(Configuration conf, Target target)
        {
            base.ConfigureRelease(conf, target);

            // Add root include path for vcpkg packages.
            conf.IncludePaths.Add(@"[project.SharpmakeCsPath]\..\extern\vcpkg\installed\x64-windows-static\include");

            // Add root lib path for vcpkg packages.
            conf.LibraryPaths.Add(@"[project.SharpmakeCsPath]\..\extern\vcpkg\installed\x64-windows-static\lib");
        }

        public override void ConfigureDebug(Configuration conf, Target target)
        {
            base.ConfigureDebug(conf, target);

            // Add root include path for vcpkg packages.
            conf.IncludePaths.Add(@"[project.SharpmakeCsPath]\..\extern\vcpkg\installed\x64-windows-static\include");

            // Add root lib path for vcpkg packages.
            conf.LibraryPaths.Add(@"[project.SharpmakeCsPath]\..\extern\vcpkg\installed\x64-windows-static\debug\lib");
        }
    }

    // Curl is a vcpkg package. The package is installed by bootstrap-sample.bat
    [Sharpmake.Export]
    internal class Curl : VCPKG
    {
        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.AddPublicDependency<ZLib>(target);

            // Dependencies on windows libraries when using Curl
            conf.LibraryFiles.Add("Crypt32.lib", "Wldap32.lib", "wsock32", "ws2_32");
        }

        public override void ConfigureDebug(Configuration conf, Target target)
        {
            base.ConfigureDebug(conf, target);

            conf.LibraryFiles.Add(@"libcurl-d.lib");
        }

        public override void ConfigureRelease(Configuration conf, Target target)
        {
            base.ConfigureRelease(conf, target);

            conf.LibraryFiles.Add(@"libcurl.lib");
        }
    }


    // ZLib is a vcpkg package. The package is installed by bootstrap-sample.bat as a dependency to curl.
    [Sharpmake.Export]
    internal class ZLib : VCPKG
    {
        public override void ConfigureDebug(Configuration conf, Target target)
        {
            base.ConfigureDebug(conf, target);

            conf.LibraryFiles.Add("zlibd.lib");
        }

        public override void ConfigureRelease(Configuration conf, Target target)
        {
            base.ConfigureRelease(conf, target);

            conf.LibraryFiles.Add("zlib.lib");
        }
    }

    // RapidJSON is a vcpkg package. The package is installed by bootstrap-sample.bat
    // This project is header only.
    [Sharpmake.Export]
    internal class RapidJSON : VCPKG
    {
    }
}
