// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;

namespace Sharpmake
{
    public abstract class FastBuildMakeCommandGenerator
    {
        public enum BuildType
        {
            Build,
            Rebuild
        };

        public abstract string GetCommand(BuildType buildType, Sharpmake.Project.Configuration conf, string fastbuildArguments);
    }


    public static class FastBuildSettings
    {
        public const string FastBuildConfigFileExtension = ".bff";
        public const string MasterBffFileName = "fbuild";

        /// <summary>
        /// Full path to the %WINDIR% directory.
        /// Usually equals to `C:\WINDOWS`.
        /// </summary>
        public static string SystemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        /// <summary>
        /// Full path to the system dll folder where the ucrtbase.dll and api-ms-win-*.dll can be found.
        /// If left null, dlls will be searched in the Redist\ucrt\DLLs\x64 subfolder of the WinSDK10 indicated in the KitsRootPaths.
        /// </summary>
        public static string SystemDllRoot = null;

        /// <summary>
        /// Cache path can be
        /// - a local path
        /// - a network path
        /// - an url if using the Ubisoft asset store plugin dll. In that case, the CachePluginDLLFilePath must
        /// also be set to the filepath of the plugin dll. 
        /// Url is the form
        /// http://<address>|<project code>
        /// Ex: http://assetstore/assetstoreservice/v0.3/assetstoreservice.svc|ACE_FB
        /// </summary>
        public static string CachePath = null;

        /// <summary>
        /// Path to the fastbuild plugin dll if any. This typically will be the path to the Ubisoft asset store plugin DLL but could be any other compatible implementation.
        /// CachePath must also be set to an appropriate url.
        /// </summary>
        public static string CachePluginDLLFilePath = null;

        // Cache configuration types
        public enum CacheTypes
        {
            Disabled,   // Cache is disabled
            CacheRead,  // Cache is in read-only mode
            CacheWrite, // Cache is in write-only mode
            CacheReadWrite // Cache is in read-write mode
        };

        /// <summary>
        /// Cache configuration type for targets allowing the cache
        /// </summary>
        public static CacheTypes CacheType = CacheTypes.Disabled;

        public static bool FastBuildReport = false;
        public static bool FastBuildSummary = true;
        public static bool FastBuildNoSummaryOnError = false;
        public static bool FastBuildVerbose = false;
        public static bool FastBuildDistribution = false;
        public static bool FastBuildWait = false;
        public static bool FastBuildNoStopOnError = false;
        public static bool FastBuildMonitor = false;
        public static bool FastBuildFastCancel = false;
        public static bool FastBuildUseIDE = true;
        public static bool FastBuildNoUnity = false;

        /// <summary>
        /// Allows retention of build state across BFF changes. Requires v0.97
        /// </summary>
        public static bool FastBuildAllowDBMigration = false;

        // Limit of distributed workers. In FastBuild code the default is 15
        public static int FastBuildWorkerConnectionLimit = -1;

        // Configuration Files Generation Settings

        /// <summary>
        /// Include the IDE version in master bff filename
        /// </summary>
        [Obsolete("MasterBff is now named after the solution")]
        public static bool IncludeCompilerInMasterBFFFilename = true;

        /// <summary>
        /// Separate the Master bff content per platform
        /// </summary>
        [Obsolete("MasterBff contains what its solution contains")]
        public static bool SeparateMasterBFFPerPlatform = false;

        /// <summary>
        /// The path of the master BFF is the folder relative to the source tree root.
        /// ex: "projects"
        /// </summary>
        [Obsolete("MasterBff is now in the same folder as the solution")]
        public static string FastBuildMasterBFFPath = null; // PLEASE OVERRIDE this in your Sharpmake main

        /// <summary>
        /// The path to the executable used to start a fastbuild compilation. This path is relative to the source tree root.
        /// ex: @"tools\FastBuild\start-fbuild.bat"
        /// </summary>
        public static string FastBuildMakeCommand = null; // PLEASE OVERRIDE this in your Sharpmake main

        public static FastBuildMakeCommandGenerator MakeCommandGenerator = null;


        /// <summary>
        /// Can be set to false to override all FastBuild settings and disable it
        /// </summary>
        public static volatile bool FastBuildSupportEnabled = true;

        /// <summary>
        /// Include the .bff files in the visual studio project files. Some programmer don't like that as when they do finds in Visual Studio, this
        /// adds useless results to their finds whenever the string they are searching matches the name of a .cpp file.
        /// </summary>
        public static bool IncludeBFFInProjects = true;

        /// <summary>
        /// If true, activate PDB Support for FastLink. Instead of having a single .pdb file for a whole project, split in many smaller .pdb files.
        /// This is incompatible with FastBuildSettings.EnablePrecompiledHeaders.
        /// </summary>
        public static bool EnableFastLinkPDBSupport = false;

        /// <summary>
        /// Adds an alias to the Master Bff containing all the configs
        /// This section is used for example in the submit assistant on AC
        /// </summary>
        public static bool WriteAllConfigsSection = false;

        /// <summary>
        /// link.exe on win64 executes rc.exe by itself on some occasions
        /// if it doesn't find it, link errors can occur, like:
        /// fatal error LNK1158: cannot run rc.exe!
        /// 
        /// Setting this to true will have sharpmake detect if a rc.exe can
        /// be found in the same folder as link.exe, and if not add the path
        /// to one in the global settings Environment section, in the PATH variable
        /// </summary>
        public static bool SetPathToResourceCompilerInEnvironment = false;

        /// <summary>
        /// This is used to activate a workaround in fastbuild for the VS2012 preprocessor enum bug. 
        /// 
        /// Notes: Only win64 is affected by this bug it seems.
        /// </summary>
        /// <remarks>
        /// VS 2012 sometimes generates corrupted code when preprocessing an already preprocessed file when it encounters
        /// enum definitions.
        /// Example:
        ///enum dateorder
        ///{
        ///    no_order, dmy, mdy, ymd, ydm
        ///};
        /// Become :
        ///enummdateorder
        ///{
        ///    no_order, dmy, mdy, ymd, ydm
        ///};
        /// And then compilation fails.
        /// 
        /// It seems that by adding a space between the enum keyword and the name it avoids that problem that looks like memory corruption in the compiler.
        /// Also it seems that this doesn't occurs with VS2013.
        /// </remarks>
        public static bool EnableVS2012EnumBugWorkaround = false; // activate workaround for VS2012 enum bug(corrupted preprocessor output).
    }
}
