// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

[module: Sharpmake.Include("baseclasses.sharpmake.cs")]
[module: Sharpmake.Include("externprojects.sharpmake.cs")]
[module: Sharpmake.Include("projects.sharpmake.cs")]


namespace VCPKGSample
{
    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            FileInfo sharpmakeFileInfo = Util.GetCurrentSharpmakeFileInfo();
            string sharpmakeFileDirectory = Util.PathMakeStandard(sharpmakeFileInfo.DirectoryName);
            string absoluteRootPath = Util.PathGetAbsolute(sharpmakeFileDirectory, @"..\tmp");

            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2019, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_19041_0);

            // This is necessary since there is no rc.exe in the same directory than link.exe
            FastBuildSettings.SetPathToResourceCompilerInEnvironment = true;
            FastBuildSettings.FastBuildMakeCommand = Util.PathGetAbsolute(sharpmakeFileDirectory, @"..\..\..\tools\FastBuild\Windows-x64\FBuild.exe");

            Util.FilesAutoCleanupDBPath = absoluteRootPath;
            Util.FilesAutoCleanupActive = true;
            arguments.Generate<VCPKGSampleSolution>();
        }
    }
}
