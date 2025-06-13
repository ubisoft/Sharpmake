// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Sharpmake
{
    public abstract class FastBuildMakeCommandGenerator
    {
        public enum BuildType
        {
            Build,
            Rebuild,
            CompileFile
        };

        public abstract string GetTargetIdentifier(Sharpmake.Project.Configuration conf);
        public abstract string GetExecutablePath(Sharpmake.Project.Configuration conf);
        public abstract string GetArguments(BuildType buildType, Sharpmake.Project.Configuration conf, string fastbuildArguments);
        public virtual string GetWorkingDirectory(Sharpmake.Project.Configuration conf) => "$(SolutionDir)";
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
        /// Full path under which files and folders are considered part of the workspace and can be expressed as relative to one another.
        /// If left null, project.RootPath will be used instead.
        /// </summary>
        public static string WorkspaceRoot = null;

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
        /// Additional settings to add to the global settings node.
        /// </summary>
        public static readonly IList<string> AdditionalGlobalSettings = new List<string>();

        /// <summary>
        /// Additional environment variables to add to the global environment settings node (key, value)
        /// </summary>
        public static readonly IDictionary<string, string> AdditionalGlobalEnvironmentVariables = new Dictionary<string, string>();

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
        public static bool FastBuildValidateCopyFiles = true;

        /// <summary>
        /// Controls whether FastBuild supports a list of LinkerStamp steps
        /// </summary>
        public static bool FastBuildSupportLinkerStampList = false;

        /// <summary>
        /// Allows retention of build state across BFF changes. Requires v0.97
        /// </summary>
        public static bool FastBuildAllowDBMigration = false;

        // Limit of distributed workers. In FastBuild code the default is 15
        public static int FastBuildWorkerConnectionLimit = -1;

        // Configuration Files Generation Settings

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
        public static bool SetPathToResourceCompilerInEnvironment = true;

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
        [Obsolete("Sharpmake doesn't support generating for vs2012 anymore, so this setting is useless.", error: false)]
        public static bool EnableVS2012EnumBugWorkaround = false; // activate workaround for VS2012 enum bug(corrupted preprocessor output).

        /// <summary>
        /// FastBuild names of compilers to set the 'UseRelativePaths_Experimental' option for.
        /// </summary>
        public static readonly ISet<string> CompilersUsingRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Additional settings to add to the Compiler node, keyed by compiler name.
        /// </summary>
        /// 
        public static readonly IDictionary<string, List<string>> AdditionalCompilerSettings = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Additional Section referred by a compiler node, keyed by compiler name
        /// </summary>
        public static readonly IDictionary<string, string> AdditionalCompilerPropertyGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Additional custom property groups. Only those referred will be written to the bff files.
        /// </summary>
        public static readonly IDictionary<string, List<string>> AdditionalPropertyGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Custom arguments pass to fastbuild
        /// </summary>
        public static string FastBuildCustomArguments = null;

        /// <summary>
        /// Enable the use of Fastbuild concurrency groups. You can only enable it if your fastbuild version supports this feature.
        /// </summary>
        public static bool EnableConcurrencyGroups { get; set; } = false;

        /// <summary>
        /// This struct is used to define concurrency groups. See fastbuild documentation for more details
        /// </summary>
        public struct ConcurrencyGroup
        {
            public int? ConcurrencyLimit; // Max number of concurrent job for this group
            public int? ConcurrencyPerJobMiB; // Arbitrary limit of memory per job in MiB. This is used to limit the number of concurrent jobs based on memory usage.
        }

        private static Dictionary<string, ConcurrencyGroup> _concurrencyGroups = new Dictionary<string, ConcurrencyGroup>();

        /// <summary>
        /// List of concurrency groups used by sharpmake when defining the fastbuild configurations. 
        /// Concurrency groups can be used to limit the number of parallel processes using the same concurrency group. See fastbuild documentation for more details.
        /// It is an optional feature and will only be used when EnableConcurrencyGroups is set to true.
        /// </summary>
        public static IReadOnlyDictionary<string, ConcurrencyGroup> ConcurrencyGroups = _concurrencyGroups;

        /// <summary>
        /// Add a concurrency group to the list of concurrency groups.
        /// </summary>
        /// <param name="groupName">concurrency group name</param>
        /// <param name="group">group params</param>
        /// <exception cref="Error"></exception>
        public static void AddConcurrencyGroup(string groupName, ConcurrencyGroup group)
        {
            // Validate the group name... We use the group name to build the concurrency struct identifier so it has to be a valid identifier.
            if (!System.Text.RegularExpressions.Regex.IsMatch(groupName, "^[a-zA-Z0-9_\\-]+$"))
            {
                throw new Error($"Fastbuild concurrency group name must be a valid identifier. Name: {groupName}");
            }

            if (!group.ConcurrencyLimit.HasValue && !group.ConcurrencyPerJobMiB.HasValue)
            {
                throw new Error($"Concurrency group must have at least one of ConcurrencyLimit or ConcurrencyPerJobMiB set. Group: {groupName}");
            }

            _concurrencyGroups.Add(groupName, group);
        }
    }
}
