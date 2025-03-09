// Copyright (c) 2020-2021 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sharpmake;
using Sharpmake.Generators.FastBuild;

[module: Sharpmake.Include("HelloIOS.*.sharpmake.cs")]
[module: Sharpmake.Include("codebase/*.sharpmake.cs")]
[module: Sharpmake.Include("codebase/*/*.sharpmake.cs")]

namespace HelloIOS
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

            // for the purpose of this sample, we'll reuse the FastBuild executables that live in the sharpmake source repo
            string sharpmakeFastBuildDir = Util.PathGetAbsolute(Globals.RootDirectory, @"..\..\..\tools\FastBuild");
            switch (Util.GetExecutingPlatform())
            {
                case Platform.linux:
                    FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeFastBuildDir, "Linux-x64", "fbuild");
                    break;
                case Platform.mac:
                    FastBuildSettings.FastBuildMakeCommand = Path.Combine(sharpmakeFastBuildDir, "OSX-x64", "FBuild");
                    IFastBuildCompilerSettings iOSSettings = PlatformRegistry.Get<IFastBuildCompilerSettings>(Platform.ios);
                    iOSSettings.LinkerInvokedViaCompiler[DevEnv.xcode] = true;
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
