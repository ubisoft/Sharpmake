// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;
using Sharpmake.Generators.FastBuild;

[module: Sharpmake.Include("HelloLinux.*.sharpmake.cs")]
[module: Sharpmake.Include("codebase/*.sharpmake.cs")]
[module: Sharpmake.Include("codebase/*/*.sharpmake.cs")]

namespace HelloLinux
{
    public static class Globals
    {
        // branch root path relative to current sharpmake file location
        public const string RelativeRootPath = @".\codebase";
        public static string RootDirectory;
        public static string TmpDirectory { get { return Path.Combine(RootDirectory, "temp"); } }
        public static string OutputDirectory { get { return Path.Combine(TmpDirectory, "bin"); } }
    }

    public static class Main
    {
        private static void ConfigureRootDirectory()
        {
            FileInfo fileInfo = Util.GetCurrentSharpmakeFileInfo();
            string rootDirectory = Path.Combine(fileInfo.DirectoryName, Globals.RelativeRootPath);
            Globals.RootDirectory = Util.SimplifyPath(rootDirectory);
        }

        private static void ConfigureAutoCleanup()
        {
            Util.FilesAutoCleanupActive = true;
            Util.FilesAutoCleanupDBPath = Path.Combine(Globals.TmpDirectory, "sharpmake");

            if (!Directory.Exists(Util.FilesAutoCleanupDBPath))
                Directory.CreateDirectory(Util.FilesAutoCleanupDBPath);
        }

        public static void OverrideLinuxPlatformDescriptor()
        {
            // Workaround for regression introduced in Sharpmake@0.20.0.
            // Default value for IsLinkerInvokedViaCompiler should be true when generating makefile on Linux.
            // See Sharpmake commit e3142832edb3af7f5cc1401365d1e0c1c84fc08e.

            var platformDescriptor = (Linux.LinuxPlatform)PlatformRegistry.Get<IPlatformDescriptor>(Platform.linux);
            platformDescriptor.IsLinkerInvokedViaCompiler = true;   
        }

        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            ConfigureRootDirectory();
            ConfigureAutoCleanup();

            FastBuildSettings.FastBuildWait = true;
            FastBuildSettings.FastBuildSummary = false;
            FastBuildSettings.FastBuildNoSummaryOnError = true;
            FastBuildSettings.FastBuildDistribution = false;
            FastBuildSettings.FastBuildMonitor = true;
            FastBuildSettings.FastBuildAllowDBMigration = true;

            OverrideLinuxPlatformDescriptor();

            // for the purpose of this sample, we'll reuse the FastBuild executables that live in the sharpmake source repo
            string sharpmakeFastBuildDir = Util.PathGetAbsolute(Globals.RootDirectory, @"..\..\..\tools\FastBuild");
            switch (Util.GetExecutingPlatform())
            {
                case Platform.linux:
                    FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeFastBuildDir, "Linux-x64", "fbuild");
                    break;
                case Platform.mac:
                    FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeFastBuildDir, "OSX-x64", "FBuild");
                    break;
                case Platform.win64:
                default:
                    FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeFastBuildDir, "Windows-x64", "FBuild.exe");
                    break;
            }

            Bff.UnityResolver = new Bff.FragmentUnityResolver();

            foreach (Type solutionType in Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(CommonSolution))))
                arguments.Generate(solutionType);
        }
    }
}
