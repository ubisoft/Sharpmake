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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Sharpmake
{
    [Flags]
    public enum DependencySetting
    {
        OnlyBuildOrder = 0,

        LibraryFiles = 1 << 1,
        LibraryPaths = 1 << 2,
        IncludePaths = 1 << 3,
        Defines = 1 << 4,
        AdditionalUsingDirectories = 1 << 5,

        // Useful masks
        Default = LibraryFiles
                              | LibraryPaths
                              | IncludePaths
                              | Defines,

        DefaultWithoutLinking = IncludePaths
                              | Defines,


        ////////////////////////////////////////////////////////////////////////
        // OLD AND DEPRECATED FLAGS
        [Obsolete("Please use OnlyBuildOrder instead.", error: false)]
        OnlyDependencyInSolution = -1,

        [Obsolete("Please use OnlyBuildOrder instead.", error: false)]
        ForcedDependencyInSolution = -1,

        [Obsolete("Please replace by OnlyBuildOrder if that's what you wanted, otherwise remove it, it isn't needed.", error: false)]
        ProjectReference = -1,

        [Obsolete("Please replace by LibraryFiles.", error: false)]
        InheritLibraryFiles = -1,

        [Obsolete("Please replace by LibraryPaths.", error: false)]
        InheritLibraryPaths = -1,

        [Obsolete("Please replace by IncludePaths.", error: false)]
        InheritIncludePaths = -1,

        [Obsolete("Please replace by Defines.", error: false)]
        InheritDefines = -1,

        [Obsolete("Please remove this.", error: false)]
        InheritDependencies = -1,

        [Obsolete("Please replace by LibraryFiles if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesLibraryFiles = -1,

        [Obsolete("Please replace by LibraryPaths if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesLibraryPaths = -1,

        [Obsolete("Please replace by IncludePaths if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesIncludePaths = -1,

        [Obsolete("Please replace by Defines if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesDefines = -1,

        [Obsolete("Please replace by OnlyBuildOrder if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesNothing = -1,
        [Obsolete("Please replace by OnlyBuildOrder if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesDependencies = -1,
    }

    public enum DependencyType
    {
        Private,
        Public
    }

    public partial class Project
    {
        [Resolver.Resolvable]
        public class Configuration : Sharpmake.Configuration
        {
            public interface IConfigurationTasks
            {
                void SetupDynamicLibraryPaths(Configuration configuration, DependencySetting dependencySetting, Configuration dependency);
                void SetupStaticLibraryPaths(Configuration configuration, DependencySetting dependencySetting, Configuration dependency);
                string GetDefaultOutputExtension(OutputType outputType);
                IEnumerable<string> GetPlatformLibraryPaths(Configuration configuration);
            }

            private static int s_count = 0;
            public static int Count => s_count;

            private const string RemoveLineTag = "REMOVE_LINE_TAG";

            public Configuration()
            {
                PrecompSourceExcludeExtension.Add(".asm");
            }

            public static OutputType SimpleOutputType(OutputType type)
            {
                switch (type)
                {
                    case OutputType.DotNetConsoleApp:
                    case OutputType.DotNetWindowsApp:
                        return OutputType.Exe;
                    case OutputType.DotNetClassLibrary:
                        return OutputType.Dll;
                    default:
                        return type;
                }
            }

            public enum OutputType
            {
                Exe,
                Lib,
                Dll,
                Utility,
                DotNetConsoleApp,
                DotNetClassLibrary,
                DotNetWindowsApp,
                IosApp,
                IosTestBundle,
                None,
            }

            public enum InputFileStrategy
            {
                Include = 0x01,     // Explicitly refer to files in fastbuild configuration files using file lists
                Exclude = 0x02      // Implicitly refer to files in fastbuild configuration files using paths and exclusion file lists.
            }

            /// <summary>
            /// Deoptimization of files by fastbuild when the status of a file is writable.
            /// NoDeoptimization : No deoptimization done, choice by default
            /// DeoptimizeWritableFiles : Deoptimize files with writable status
            /// DeoptimizeWritableFilesWithToken : Deoptimize files with writable status and when the token FASTBUILD_DEOPTIMIZE_OBJECT is specified
            /// </summary>
            public enum DeoptimizationWritableFiles
            {
                NoDeoptimization = 0x01, // default
                DeoptimizeWritableFiles = 0x02,
                DeoptimizeWritableFilesWithToken = 0x04
            }

            public enum UACExecutionLevel
            {
                asInvoker,
                highestAvailable,
                requireAdministrator
            }

            public Strings PathExcludeBuild = new Strings();

            private OutputType _output = OutputType.Exe; // None is default if Export
            /// <summary>
            /// Output type of the current project configuration, exe, lib or dll.
            /// </summary>
            public OutputType Output
            {
                get { return _output; }
                set
                {
                    if (!Project.IsValidConfigurationOutputType(value))
                        throw new Error("The specified configuration output type \"{0}\" is not valid for the project \"{1}\".", value, Project.GetType().ToNiceTypeName());
                    _output = value;
                }
            }

            /// <summary>
            /// Output extension ex: .dll, .self, .exe, .dlu
            /// </summary>
            public string OutputExtension = "";

            /// <summary>
            /// Copy Dependency TargetFileCopy to target path of this project
            /// </summary>
            public bool ExecuteTargetCopy = false;

            /// <summary>
            /// Dependent projects will copy the compiler pdb of this project to their target path
            /// </summary>
            public bool CopyCompilerPdbToDependentTargets = true;

            // Xcopy parameters
            // /d           Copy file only if source time is newer than the destination time.
            // /F           Displays full source and destination file names while copying.
            // /R           Overwrites read-only files.
            // /H           Copies hidden and system files also.
            // /V           Verifies the size of each new file.
            // /Y           Suppresses prompting to confirm you want to overwrite an existing destination file.
            /// <summary>
            /// Command to execute TargetCopyFiles. First parameter is relative path to the file, second parameter is the relative target directory
            /// </summary>
            public delegate string TargetCopyCommandCreator(string relativeSourcePath, string relativeTargetPath, string workingPath);

            public TargetCopyCommandCreator CreateTargetCopyCommand =
                (source, target, workingPath) => string.Format(@"xcopy /d /F /R /H /V /Y ""{0}"" ""{1}"" >nul", source, target);


            /// <summary>
            /// Since Sharpmake handles all dependencies, using AdditionalDependencies field in project
            /// is typically useless for static libraries.  However, when dependents are not generated
            /// by Sharpmake, i.e. a .sln contains Sharpmake generated projects as static libraries as
            /// well as manually maintained dependent projects, then the feature can still be useful.
            /// Setting this boolean to true will make Sharpmake fill the fields in the current static
            /// library project.
            /// </summary>
            public bool ExportAdditionalLibrariesEvenForStaticLib = false;


            public string ProjectName = "[project.Name]";

            /// <summary>
            /// File name for the generated project without extension, ex: "MyProject"
            /// </summary>
            public string ProjectFileName = "[project.Name]";

            /// <summary>
            /// Path of project file to be generated
            /// </summary>
            public string ProjectPath = "[project.SharpmakeCsPath]";

            public string AssemblyName = "[project.AssemblyName]";

            /// <summary>
            /// Full project file name, without the extension because it's generator specific.
            /// </summary>
            public string ProjectFullFileName { get { return Path.Combine(ProjectPath, ProjectFileName); } }


            public string SolutionFolder = "";                                              // Solution Folder

            /// <summary>
            /// If unset, the pdb file names will be the target name with a suffix and the .pdb extension.
            ///
            /// Always put a separate pdb for the compiler in the intermediate path to avoid
            /// conflicts with the one from the linker. 
            /// This helps the following things:
            /// 1. Makes linker go faster
            /// 2. Avoid pdb for dll and .exe growing and growing at each link
            /// 3. Makes incremental link works better.
            /// </summary>
            public string LinkerPdbSuffix = string.Empty;
            public string LinkerPdbFilePath = "[conf.TargetPath]" + Path.DirectorySeparatorChar + "[conf.TargetFileFullName][conf.LinkerPdbSuffix].pdb";
            public string CompilerPdbSuffix = "_compiler";
            public string CompilerPdbFilePath = "[conf.IntermediatePath]" + Path.DirectorySeparatorChar + "[conf.TargetFileFullName][conf.CompilerPdbSuffix].pdb";
            public bool UseRelativePdbPath = true;

            public string ManifestFileSuffix = ".intermediate.manifest";

            /// <summary>
            /// Intermediate devEnv directory 
            /// </summary>
            public string IntermediatePath = "[conf.ProjectPath]" + Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar + "[target.Platform]" + Path.DirectorySeparatorChar + "[target.Name]";

            /// <summary>
            /// Compiler defines, the generator may add some if needed for platform/target
            /// </summary>
            public Strings Defines = new Strings();

            /// <summary>
            /// Exported compiler defines, the added defines will be propagated to dependencies
            /// Note that they will not be added on the config adding them.
            /// </summary>
            public Strings ExportDefines = new Strings();


            /// <summary>
            /// Excluded file from build, while remove this list of file from project.SourceFiles and matched project.SourceFilesRegex
            /// </summary>
            public Strings SourceFilesBuildExclude = new Strings();

            public Strings SourceFilesBuildExcludeRegex = new Strings();

            public Strings SourceFilesFiltersRegex = new Strings();

            /// <summary>
            /// Sources file that match this regex will be compiled as C Files
            /// </summary>
            public Strings SourceFilesCompileAsCRegex = new Strings();

            /// <summary>
            /// Sources file that match this regex will be compiled as CPP Files
            /// </summary>
            public Strings SourceFilesCompileAsCPPRegex = new Strings();

            /// <summary>
            /// Sources file that match this regex will be compiled as CLR Files
            /// </summary>
            public Strings SourceFilesCompileAsCLRRegex = new Strings();

            /// <summary>
            /// Sources file that match this regex will be excluded from CLR Files list.
            /// Used on C++ projects rather than C++/CLI projects.
            /// </summary>
            public Strings SourceFilesCompileAsCLRExcludeRegex = new Strings();

            /// <summary>
            /// Sources file that match this regex will be explicitly not compiled as CLR files.
            /// Used on C++/CLI projects to force certain files to be compiled without /clr switch.
            /// </summary>
            public Strings SourceFilesCompileAsNonCLRRegex = new Strings();

            /// <summary>
            /// Include devEnv path
            /// </summary>
            public OrderableStrings IncludePaths = new OrderableStrings();

            public OrderableStrings DependenciesIncludePaths = new OrderableStrings();


            public OrderableStrings IncludePrivatePaths = new OrderableStrings();


            public OrderableStrings AdditionalCompilerOptions = new OrderableStrings();

            public Strings AdditionalNone = new Strings();

            /// <summary>
            /// Precompiled header source file, absolute or relative to project.SourceRootPath
            /// if null, no precompiled header will be used.
            /// </summary>
            public string PrecompSource = null;

            /// <summary>
            /// Precompiled header file, absolute or relative to project.SourceRootPath. note that source file need to include this file 
            /// relative to the project.SourceRootPath. ex: #include "engine/precomp.h"
            /// if null, no precompiled header will be used.
            /// </summary>
            public string PrecompHeader = null;

            /// <summary>
            /// If defined, used instead of intermediate folder for folder of precompiled header.
            /// </summary>
            public string PrecompHeaderOutputFolder = null;

            /// <summary>
            /// List of file that don't used precompiled header. ex: *.c
            /// </summary>
            public Strings PrecompSourceExclude = new Strings();
            public Strings PrecompSourceExcludeExtension = new Strings();
            public Strings PrecompSourceExcludeFolders = new Strings();

            /// <summary>
            /// List of headers passed to the preprocessor to be parsed.
            /// </summary>
            public Strings ForcedIncludes = new Strings();

            /// <summary>
            /// List of file that are built to consume WinRT Extensions
            /// </summary>
            public Strings ConsumeWinRTExtensions = new Strings();

            /// <summary>
            /// List of file that are built to consume WinRT Extensions, by regex
            /// </summary>
            public Strings SourceFilesCompileAsWinRTRegex = new Strings();

            /// <summary>
            /// List of file that are excluded from being built to consume WinRT Extensions
            /// </summary>
            public Strings ExcludeWinRTExtensions = new Strings();

            /// <summary>
            /// List of file that are excluded from being built to consume WinRT Extensions, by regex
            /// </summary>
            public Strings SourceFilesExcludeAsWinRTRegex = new Strings();

            /// <summary>
            /// List of file marked with exceptions enabled
            /// </summary>
            public Strings SourceFilesExceptionsEnabled = new Strings();
            public Strings SourceFilesExceptionsEnabledWithExternC = new Strings();
            public Strings SourceFilesExceptionsEnabledWithSEH = new Strings();

            // The .ruleset file to use for code analysis
            public string CodeAnalysisRuleSetFilePath = RemoveLineTag;

            /// <summary>
            /// Request sharpmake to dump the dependency graph of this configuration
            /// </summary>
            public bool DumpDependencyGraph = false;

            public void AddSourceFileWithExceptionSetting(string filename, Options.Vc.Compiler.Exceptions exceptionSetting)
            {
                switch (exceptionSetting)
                {
                    case Sharpmake.Options.Vc.Compiler.Exceptions.Enable:
                        {
                            if (SourceFilesExceptionsEnabledWithExternC.Contains(filename) ||
                               SourceFilesExceptionsEnabledWithSEH.Contains(filename))
                                throw new Error("Conflicting exception settings for file " + filename);

                            SourceFilesExceptionsEnabled.Add(filename);
                        }
                        break;
                    case Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithExternC:
                        {
                            if (SourceFilesExceptionsEnabled.Contains(filename) ||
                               SourceFilesExceptionsEnabledWithSEH.Contains(filename))
                                throw new Error("Conflicting exception settings for file " + filename);

                            SourceFilesExceptionsEnabledWithExternC.Add(filename);
                        }
                        break;
                    case Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithSEH:
                        {
                            if (SourceFilesExceptionsEnabled.Contains(filename) ||
                               SourceFilesExceptionsEnabledWithExternC.Contains(filename))
                                throw new Error("Conflicting exception settings for file " + filename);

                            SourceFilesExceptionsEnabledWithSEH.Add(filename);
                        }
                        break;
                    default: throw new NotImplementedException("Exception setting for file " + filename + " not recognized");
                }
            }

            public Options.Vc.Compiler.Exceptions GetExceptionSettingForFile(string filename)
            {
                // If consuming WinRT, file must be compiled with exceptions enabled
                if (ConsumeWinRTExtensions.Contains(filename) || ResolvedSourceFilesWithCompileAsWinRTOption.Contains(filename))
                {
                    return Sharpmake.Options.Vc.Compiler.Exceptions.Enable;
                }

                if (SourceFilesExceptionsEnabled.Contains(filename))
                    return Sharpmake.Options.Vc.Compiler.Exceptions.Enable;
                if (SourceFilesExceptionsEnabledWithExternC.Contains(filename))
                    return Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithExternC;
                if (SourceFilesExceptionsEnabledWithSEH.Contains(filename))
                    return Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithSEH;

                return Sharpmake.Options.Vc.Compiler.Exceptions.Disable;
            }

            /// <summary>
            /// Library linker path
            /// </summary>
            public OrderableStrings LibraryPaths = new OrderableStrings();

            public OrderableStrings DependenciesLibraryPaths = new OrderableStrings();

            /// <summary>
            /// Library linker files
            /// Extension is not needed and will be removed if present
            /// WARNING: be careful of lib names containing dots, in that case
            ///          add an extension
            /// </summary>
            public OrderableStrings LibraryFiles = new OrderableStrings();

            public OrderableStrings AdditionalUsingDirectories = new OrderableStrings();
            public OrderableStrings DependenciesLibraryFiles = new OrderableStrings();
            public UniqueList<Configuration> ConfigurationDependencies = new UniqueList<Configuration>();

            public List<DotNetDependency> DotNetPublicDependencies = new List<DotNetDependency>();
            public List<DotNetDependency> DotNetPrivateDependencies = new List<DotNetDependency>();

            public OrderableStrings AdditionalLinkerOptions = new OrderableStrings();

            public OrderableStrings AdditionalNSOFiles = new OrderableStrings();
            public string MetadataSource = null;

            public Type ExportSymbolThroughProject = null;

            /// <summary>
            /// Target path, where the output files will be compiled, ex: exe, dll, self, xex
            /// </summary>
            public string TargetPath = Path.Combine("[conf.ProjectPath]", "output", "[target.Platform]", "[conf.Name]");

            public string TargetLibraryPath = "[conf.TargetPath]";

            public bool ExportDllSymbols = true;

            /// <summary>
            /// True = As a DotNetClassLibrary project this project also creates a native .lib,
            /// Dependent projects will link against that lib, set to false if the project only creates a managed assembly
            /// </summary>
            public bool CppCliExportsNativeLib = true;

            public bool SkipFilterGeneration = false;

            /// <summary>
            /// Module definition used in dll like maxhammer.dlu
            /// </summary>
            public string ModuleDefinitionFile = null;

            /// <summary>
            /// If specified, it overrides Project.BlobPath
            /// </summary>
            /// 
            private string _blobPath = null;
            public string BlobPath
            {
                get { return _blobPath ?? Project.BlobPath; }
                set { _blobPath = value; }
            }

            string _fastBuildUnityPath = null;
            public string FastBuildUnityPath
            {
                get { return _fastBuildUnityPath ?? _blobPath ?? Project.FastBuildUnityPath; }
                set { _fastBuildUnityPath = value; }
            }

            /// <summary>
            /// If specified, it overrides Project.DefaultBlobWorkFileHeader
            /// </summary>
            public string BlobWorkFileHeader = null;

            /// <summary>
            /// If specified, it overrides Project.DefaultBlobWorkFileFooter
            /// </summary>
            public string BlobWorkFileFooter = null;

            /// <summary>
            /// If specified, it overrides Project.BlobSize
            /// </summary>
            public int BlobSize = 0;

            public int FastBuildUnityCount { get; set; }

            public bool IncludeBlobbedSourceFiles = true;

            // Build writable files individually
            public bool FastBuildUnityInputIsolateWritableFiles = true;

            // Disable isolation when many files are writable
            public int FastBuildUnityInputIsolateWritableFilesLimit = 10;

            /// Custom Actions to do before invoking FastBuildExecutable.
            public string FastBuildCustomActionsBeforeBuildCommand = RemoveLineTag;

            public string BffFileName = "[conf.ProjectFileName]";
            public string BffFullFileName => Path.Combine(ProjectPath, BffFileName);

            // For projects merging multiple targets, sometimes what is wanted is to not generate
            // any FastBuild .bff files, but instead include the ones for all appropriate targets
            public bool DoNotGenerateFastBuild = false;
            public delegate bool FastBuildFileIncludeConditionDelegate(Project.Configuration conf);
            public FastBuildFileIncludeConditionDelegate FastBuildFileIncludeCondition = null;

            // container for executable 
            [Resolver.Resolvable]
            public class BuildStepExecutable : BuildStepBase
            {
                public BuildStepExecutable(
                    string executableFile,
                    string executableInputFileArgumentOption,
                    string executableOutputFileArgumentOption,
                    string executableOtherArguments,
                    string executableWorkingDirectory = "",
                    bool isNameSpecific = false,
                    bool useStdOutAsOutput = false)

                {
                    ExecutableFile = executableFile;
                    ExecutableInputFileArgumentOption = executableInputFileArgumentOption;
                    ExecutableOutputFileArgumentOption = executableOutputFileArgumentOption;
                    ExecutableOtherArguments = executableOtherArguments;
                    ExecutableWorkingDirectory = executableWorkingDirectory;
                    IsNameSpecific = isNameSpecific;
                    FastBuildUseStdOutAsOutput = useStdOutAsOutput;
                }

                public string ExecutableFile = "";
                public string ExecutableInputFileArgumentOption = "";
                public string ExecutableOutputFileArgumentOption = "";
                public string ExecutableOtherArguments = "";
                public string ExecutableWorkingDirectory = "";
                public bool FastBuildUseStdOutAsOutput = false;
            }

            public class FileCustomBuild
            {
                public FileCustomBuild(string description = "Copy files...")
                {
                    Description = description;
                }

                public string Description;
                public Strings CommandLines = new Strings();
                public Strings Inputs = new Strings();
                public Strings Outputs = new Strings();
                public bool LinkObjects = false;
            }

            // container for copy
            [Resolver.Resolvable]
            public class BuildStepCopy : BuildStepBase
            {
                public BuildStepCopy(BuildStepCopy buildStepCopy)
                {
                    SourcePath = buildStepCopy.SourcePath;
                    DestinationPath = buildStepCopy.DestinationPath;

                    IsFileCopy = buildStepCopy.IsFileCopy;
                    IsRecurse = buildStepCopy.IsRecurse;
                    IsNameSpecific = buildStepCopy.IsNameSpecific;
                    CopyPattern = buildStepCopy.CopyPattern;
                }

                public BuildStepCopy(string sourcePath, string destinationPath, bool isNameSpecific = false, string copyPattern = "*")
                {
                    SourcePath = sourcePath;
                    DestinationPath = destinationPath;

                    IsFileCopy = true;
                    IsRecurse = true;
                    IsNameSpecific = isNameSpecific;
                    CopyPattern = copyPattern;
                }
                public string SourcePath = "";
                public string DestinationPath = "";

                public bool IsFileCopy { get; set; }
                public bool IsRecurse { get; set; }
                public string CopyPattern { get; set; }

                public virtual string GetCopyCommand(string workingPath, EnvironmentVariableResolver resolver)
                {
                    string sourceRelativePath = Util.PathGetRelative(workingPath, resolver.Resolve(SourcePath));
                    string destinationRelativePath = Util.PathGetRelative(workingPath, resolver.Resolve(DestinationPath));

                    return string.Join(" ",
                        "robocopy.exe",

                        // file selection options
                        "/xo",  // /XO :: eXclude Older files.

                        // logging options
                        "/ns",  // /NS :: No Size - don't log file sizes.
                        "/nc",  // /NC :: No Class - don't log file classes.
                        "/np",  // /NP :: No Progress - don't display percentage copied.
                        "/njh", // /NJH :: No Job Header.
                        "/njs", // /NJS :: No Job Summary.
                        "/ndl", // /NDL :: No Directory List - don't log directory names.
                        "/nfl", // /NFL :: No File List - don't log file names.

                        // parameters
                        "\"" + sourceRelativePath + "\"",
                        "\"" + destinationRelativePath + "\"",
                        "\"" + CopyPattern + "\"",

                        "> nul", // hide all remaining stdout to nul

                        // Error handling: any value greater than 7 indicates that there was at least one failure during the copy operation.
                        // The type nul is used to clear the errorlevel to 0
                        // see https://ss64.com/nt/robocopy-exit.html for more info
                        "& if %ERRORLEVEL% GEQ 8 (echo Copy failed & exit 1) else (type nul>nul)"
                    );
                }

                internal override void Resolve(Resolver resolver)
                {
                    base.Resolve(resolver);

                    // TODO: that test is very dodgy. Please remove this, and have the user set the property instead, or even create a new BuildStepCopyDir type
                    int index = DestinationPath.LastIndexOf(@"\", StringComparison.Ordinal);
                    var destinationFolder = index < 0 ? DestinationPath : DestinationPath.Substring(index);
                    var destinationIsFolder = !destinationFolder.Contains(".");
                    bool isFolderCopy = destinationIsFolder || (Util.DirectoryExists(SourcePath) && Util.DirectoryExists(DestinationPath));
                    if (isFolderCopy)
                        IsFileCopy = false;
                }
            }

            public abstract class BuildStepBase : IComparable
            {
                public bool IsNameSpecific { get; set; }

                public bool IsResolved { get; private set; } = false;

                // Override this to control the order of BuildStep execution in Build Events
                public virtual int CompareTo(object obj)
                {
                    if (obj == null)
                        return 1;

                    return 0;
                }

                internal virtual void Resolve(Resolver resolver)
                {
                    if (IsResolved)
                        return;

                    resolver.Resolve(this);

                    IsResolved = true;
                }
            }


            /// <summary>
            /// If specified, every obj will be outputed in intermediate directories corresponding to sources hierarchy
            /// WARNING! this will slow down compile time of your project!
            ///          cf. http://stackoverflow.com/a/1999344
            /// </summary>
            public Func<string, string> ObjectFileName = null;

            /// <summary>
            /// Name of the current configuration, ie: name shown in visual studio target drop down. ex: "Debug Exe" for DefaultTarget
            /// </summary>
            public string Name = "[target.ProjectConfigurationName]";
            public string TargetFileName = "[project.Name]";                // "system"
            public string TargetFileSuffix = "";                            // "_rt"
            public string TargetFilePrefix = "";
            public string TargetFileFullName = "[conf.TargetFilePrefix][conf.TargetFileName][conf.TargetFileSuffix]";
            public int TargetFileOrderNumber = 0;
            public int TargetLibraryPathOrderNumber = 0;
            public Strings TargetCopyFiles = new Strings();                     // file to copy to the output directory
            public Strings TargetDependsFiles = new Strings();

            public bool IsExcludedFromBuild = false;

            public FileCustomBuild CopyDependenciesBuildStep = null;            // this is used to add a custom build tool on a dummy file to copy the dependencies' DLLs and PDBs (works better than a PostBuildStep)

            public Strings EventPreBuild = new Strings();
            public string EventPreBuildDescription = "";
            public bool EventPreBuildExcludedFromBuild = false;
            public UniqueList<BuildStepBase> EventPreBuildExe = new UniqueList<BuildStepBase>();
            public UniqueList<BuildStepBase> EventCustomPreBuildExe = new UniqueList<BuildStepBase>();
            public Dictionary<string, BuildStepBase> EventPreBuildExecute = new Dictionary<string, BuildStepBase>();
            public Dictionary<string, BuildStepBase> EventCustomPrebuildExecute = new Dictionary<string, BuildStepBase>();

            public Strings EventPreLink = new Strings();
            public string EventPreLinkDescription = "";
            public bool EventPreLinkExcludedFromBuild = false;

            public Strings EventPrePostLink = new Strings();
            public string EventPrePostLinkDescription = "";
            public bool EventPrePostLinkExcludedFromBuild = false;

            public List<string> EventPostBuild = new List<string>();
            public string EventPostBuildDescription = "";
            public bool EventPostBuildExcludedFromBuild = false;
            public UniqueList<BuildStepBase> EventPostBuildExe = new UniqueList<BuildStepBase>();
            public UniqueList<BuildStepBase> EventCustomPostBuildExe = new UniqueList<BuildStepBase>();
            public Dictionary<string, BuildStepBase> EventPostBuildExecute = new Dictionary<string, BuildStepBase>();
            public Dictionary<string, BuildStepBase> EventCustomPostBuildExecute = new Dictionary<string, BuildStepBase>();
            public HashSet<KeyValuePair<string, string>> EventPostBuildCopies = new HashSet<KeyValuePair<string, string>>(); // <path to file, destination folder>
            public BuildStepExecutable PostBuildStampExe = null;

            public List<string> CustomBuildStep = new List<string>();
            public string CustomBuildStepDescription = "";
            public Strings CustomBuildStepOutputs = new Strings();
            public Strings CustomBuildStepInputs = new Strings();
            public string CustomBuildStepBeforeTargets = "";
            public string CustomBuildStepAfterTargets = "";
            public string CustomBuildStepTreatOutputAsContent = ""; // should be a bool

            // This is all the data specific to a custom build step.
            // The ones stored in the project configuration use absolute paths
            // but we need relative paths when we're ready to export a specific
            // project file.
            public class CustomFileBuildStepData
            {
                // This lets us filter which type of project files should have
                // this custom build step.   This is specifically used to deal with limitations of different build
                // systems.   Visual studio only supports one build action per file, so if you need both compilation and
                // some other build step such as QT or Documentation generation on the same file, you need to put the rule
                // on a different input file that also depends on the real input file.   Fast build is key based instead of
                // file based, so it can have two different operations on the same file.   If you need support for that,
                // you can make two different custom build rules and have one specific to bff and the other excluding bff.
                public enum ProjectFilter
                {
                    // Build step is used for both project file generation and fast build generation.
                    AllProjects,
                    ExcludeBFF,
                    BFFOnly
                };

                // File custom builds are bound to a specific existing file.  They will run when the file is changed.
                public string KeyInput = "";
                // This is the executable that's going to be invoked as the custom build step.
                public string Executable = "";
                // These are the arguments to be passed to the executable.
                // We support [input] and [output] tags in the executable arguments that will auto-resolve to the relative
                // path to KeyInput and Output.
                public string ExecutableArguments = "";
                // This is what will appear in the project file under description, but it's also the key used
                // for fast build, so it should be unique per build step if you want to use fast build.
                public string Description = "";
                // For fast build compatibility, we can only have one input and one output per custom command.
                // This is what we tell the build system we're going to produce.
                public string Output = "";
                // This is not supported by BFF, but if excluded from BFF, additional files that will cause a re-run of this
                // custom build step can be be specified here.
                public Strings AdditionalInputs = new Strings();
                // Filters if this step should run on 
                public ProjectFilter Filter = ProjectFilter.AllProjects;

            }

            public class CustomFileBuildStep : CustomFileBuildStepData
            {
                // Initial resolve pass, in-place.
                public virtual void Resolve(Resolver resolver)
                {
                    KeyInput = resolver.Resolve(KeyInput);
                    Executable = resolver.Resolve(Executable);
                    Description = resolver.Resolve(Description);
                    Output = resolver.Resolve(Output);
                    foreach(var input in AdditionalInputs.Values)
                    {
                        AdditionalInputs.UpdateValue(input, resolver.Resolve(input));
                    }

                    // We don't resolve arguments yet as we need the relative directly first.
                }

                // Pre-save make-relative pass, to set all fields relative to project path.
                // This WILL get called multiple times, so it needs to write to different fields than
                // the original input.
                public virtual CustomFileBuildStepData MakePathRelative(Resolver resolver, Func<string, bool, string> MakeRelativeTool)
                {
                    var relativeData = new CustomFileBuildStepData();
                    relativeData.KeyInput = MakeRelativeTool(KeyInput, true);
                    relativeData.Executable = MakeRelativeTool(Executable, true);
                    relativeData.Output = MakeRelativeTool(Output, true);
                    using (resolver.NewScopedParameter("input", relativeData.KeyInput))
                    using (resolver.NewScopedParameter("output", relativeData.Output))
                    {
                        relativeData.ExecutableArguments = resolver.Resolve(ExecutableArguments);
                    }
                    relativeData.Description = Description;
                    foreach (var input in AdditionalInputs.Values)
                    {
                        relativeData.AdditionalInputs.Add(MakeRelativeTool(input, true));
                    }
                    relativeData.Filter = Filter;
                    return relativeData;
                }
            };
            // Specifies a list of custom builds steps that will be executed when this configuration is active.
            public List<CustomFileBuildStep> CustomFileBuildSteps = new List<CustomFileBuildStep>();

            public string EventCustomBuildDescription = "";
            public Strings EventCustomBuild = new Strings();
            public string EventCustomBuildOutputs = "";

            public string LayoutDir = "";
            public string PullMappingFile = "";
            public string PullTemporaryFolder = "";
            public Strings AdditionalManifestFiles = new Strings();
            public string LayoutExtensionFilter = "";

            // Only used by csproj
            public string StartWorkingDirectory = string.Empty;

            public FileCustomBuild CustomBuildForAllSources = null;
            public FileCustomBuild CustomBuildForAllIncludes = null;

            public Project Project { get { return Owner as Project; } }
            public bool DeployProject = false;

            public bool IsBlobbed = false;
            public string BlobFileDefine = "";

            public UACExecutionLevel ApplicationPermissions = UACExecutionLevel.asInvoker;

            public string ResourceFileDefine = "";

            public string ExecutableExtension { get; private set; }
            public string CompressedExecutableExtension { get; private set; }
            public string DllExtension { get; private set; }
            public string ProgramDatabaseExtension { get; private set; }

            private string _customTargetFileExtension = null;
            public string TargetFileExtension
            {
                get
                {
                    return !string.IsNullOrEmpty(_customTargetFileExtension) ? _customTargetFileExtension :
                                  (Output == OutputType.Dll || Output == OutputType.DotNetClassLibrary ? DllExtension : CompressedExecutableExtension);
                }
                set { _customTargetFileExtension = value; }
            }


            // FastBuild configuration
            public bool IsFastBuild = false;

            [Obsolete("Sharpmake will determine the projects to build.")]
            public bool IsMainProject = false;

            public bool FastBuildBlobbed = true;
            [Obsolete("Use FastBuildDistribution instead.")]
            public bool FastBuildDisableDistribution = false;
            public bool FastBuildDistribution = true;
            public bool FastBuildCacheAllowed = true; // If cache is allowed for this configuration, it will use the value specified in FastBuildSettings.CacheType
            public InputFileStrategy FastBuildBlobbingStrategy = InputFileStrategy.Exclude;
            public InputFileStrategy FastBuildNoBlobStrategy = InputFileStrategy.Include;
            public DeoptimizationWritableFiles FastBuildDeoptimization = DeoptimizationWritableFiles.NoDeoptimization;
            public string FastBuildCustomArgs = string.Empty;   // Custom commandline arguments for fastbuild for that target.

            private Dictionary<KeyValuePair<Type, ITarget>, DependencySetting> _dependenciesSetting = new Dictionary<KeyValuePair<Type, ITarget>, DependencySetting>();

            // These dependencies will not be propagated to other project that depends on us
            internal IDictionary<Type, ITarget> UnResolvedPrivateDependencies { get; } = new Dictionary<Type, ITarget>();
            // These dependencies will be propagated to other dependent project, but not across dll dependencies.
            internal IDictionary<Type, ITarget> UnResolvedProtectedDependencies { get; } = new Dictionary<Type, ITarget>();
            // These dependencies are always propagated to other dependent project.
            internal Dictionary<Type, ITarget> UnResolvedPublicDependencies { get; } = new Dictionary<Type, ITarget>();

            private Strings _resolvedTargetCopyFiles = new Strings();
            public IEnumerable<string> ResolvedTargetCopyFiles => _resolvedTargetCopyFiles;

            private Strings _resolvedTargetDependsFiles = new Strings();
            public IEnumerable<string> ResolvedTargetDependsFiles => _resolvedTargetDependsFiles;

            private UniqueList<BuildStepBase> _resolvedEventPreBuildExe = new UniqueList<BuildStepBase>();
            public IEnumerable<BuildStepBase> ResolvedEventPreBuildExe => _resolvedEventPreBuildExe.SortedValues;

            private UniqueList<BuildStepBase> _resolvedEventPostBuildExe = new UniqueList<BuildStepBase>();
            public IEnumerable<BuildStepBase> ResolvedEventPostBuildExe => _resolvedEventPostBuildExe.SortedValues;

            private UniqueList<BuildStepBase> _resolvedEventCustomPreBuildExe = new UniqueList<BuildStepBase>();
            public IEnumerable<BuildStepBase> ResolvedEventCustomPreBuildExe => _resolvedEventCustomPreBuildExe.SortedValues;

            private UniqueList<BuildStepBase> _resolvedEventCustomPostBuildExe = new UniqueList<BuildStepBase>();
            public IEnumerable<BuildStepBase> ResolvedEventCustomPostBuildExe => _resolvedEventCustomPostBuildExe.SortedValues;

            private UniqueList<BuildStepBase> _resolvedExecFiles = new UniqueList<BuildStepBase>();
            public IEnumerable<BuildStepBase> ResolvedExecFiles => _resolvedExecFiles;

            private UniqueList<BuildStepBase> _resolvedExecDependsFiles = new UniqueList<BuildStepBase>();
            public IEnumerable<BuildStepBase> ResolvedExecDependsFiles => _resolvedExecDependsFiles;


            private string _ProjectGuid = null;
            public string ProjectGuid
            {
                get { return _ProjectGuid; }
                set
                {
                    if (_ProjectGuid != value)
                    {
                        // Makes sure that the GUID is formatted correctly.
                        var parsedGuid = Guid.Parse(value);
                        _ProjectGuid = parsedGuid.ToString("D").ToUpperInvariant();
                    }
                }
            }
            public string ProjectFullFileNameWithExtension = null;

            public void GeneratorSetGeneratedInformation(string executableExtension, string compressedExecutableExtension, string dllExtension, string programDatabaseExtension)
            {
                ExecutableExtension = executableExtension;
                CompressedExecutableExtension = compressedExecutableExtension;
                DllExtension = dllExtension;
                ProgramDatabaseExtension = programDatabaseExtension;
            }

            public Strings ResolvedSourceFilesBuildExclude = new Strings();

            public Strings ResolvedSourceFilesBlobExclude = new Strings();

            public Strings ResolvedSourceFilesWithCompileAsCOption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsCPPOption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsCLROption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsNonCLROption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsWinRTOption = new Strings();
            public Strings ResolvedSourceFilesWithExcludeAsWinRTOption = new Strings();

            public bool NeedsAppxManifestFile = false;
            public string AppxManifestFilePath = "[conf.TargetPath]/[project.Name].appxmanifest";

            public Strings PRIFilesExtensions = new Strings();

            internal override void Construct(object owner, ITarget target)
            {
                base.Construct(owner, target);

                System.Threading.Interlocked.Increment(ref s_count);

                if (target.TestFragment(Optimization.Release) || target.TestFragment(Optimization.Retail))
                    DefaultOption = Sharpmake.Options.DefaultTarget.Release;
                Project project = (Project)owner;

                // Change Output default for Export
                if (project.GetType().IsDefined(typeof(Export), false))
                    Output = OutputType.None;
            }

            internal void Resolve(Resolver resolver)
            {
                if (PrecompHeader == null && PrecompSource != null)
                    throw new Error("Incoherent settings for {0} : PrecompHeader is null but PrecompSource is not", ToString());
                // TODO : Is it OK to comment this or is it a hack ?
                //if (PrecompHeader != null && PrecompSource == null)
                //    throw new Error("Incoherent settings for {0} : PrecompSource is null but PrecompHeader is not", ToString());

                resolver.SetParameter("conf", this);
                resolver.SetParameter("target", Target);
                resolver.Resolve(this);

                Util.ResolvePath(Project.SharpmakeCsPath, ref ProjectPath);
                if (DebugBreaks.ShouldBreakOnProjectPath(DebugBreaks.Context.Resolving, Path.Combine(ProjectPath, ProjectFileName) + (Project is CSharpProject ? ".csproj" : ".vcxproj"), this))
                    System.Diagnostics.Debugger.Break();
                Util.ResolvePath(Project.SharpmakeCsPath, ref IntermediatePath);
                Util.ResolvePath(Project.SharpmakeCsPath, ref LibraryPaths);
                Util.ResolvePath(Project.SharpmakeCsPath, ref TargetCopyFiles);
                Util.ResolvePath(Project.SharpmakeCsPath, ref TargetDependsFiles);
                Util.ResolvePath(Project.SharpmakeCsPath, ref TargetPath);
                Util.ResolvePath(Project.SharpmakeCsPath, ref TargetLibraryPath);
                Util.ResolvePath(Project.SharpmakeCsPath, ref AdditionalUsingDirectories);
                if (_blobPath != null)
                    Util.ResolvePath(Project.SharpmakeCsPath, ref _blobPath);

                // workaround for export projects: they do not generate pdb, so no need to resolve their paths
                if (!Project.GetType().IsDefined(typeof(Export), false))
                {
                    // Reset to the default if the script set it to an empty string.
                    if (!string.IsNullOrEmpty(LinkerPdbFilePath))
                        Util.ResolvePath(Project.SharpmakeCsPath, ref LinkerPdbFilePath);
                    if (!string.IsNullOrEmpty(CompilerPdbFilePath))
                        Util.ResolvePath(Project.SharpmakeCsPath, ref CompilerPdbFilePath);
                }
                if (PrecompHeaderOutputFolder != null)
                    Util.ResolvePath(Project.SharpmakeCsPath, ref PrecompHeaderOutputFolder);

                Util.ResolvePath(Project.SourceRootPath, ref SourceFilesBuildExclude);
                Util.ResolvePath(Project.SourceRootPath, ref IncludePaths);
                Util.ResolvePath(Project.SourceRootPath, ref IncludePrivatePaths);
                Util.ResolvePath(Project.SourceRootPath, ref PrecompSourceExclude);
                Util.ResolvePath(Project.SourceRootPath, ref PrecompSourceExcludeFolders);
                Util.ResolvePath(Project.SourceRootPath, ref ConsumeWinRTExtensions);
                Util.ResolvePath(Project.SourceRootPath, ref ExcludeWinRTExtensions);
                Util.ResolvePath(Project.SourceRootPath, ref SourceFilesExceptionsEnabled);
                Util.ResolvePath(Project.SourceRootPath, ref SourceFilesExceptionsEnabledWithExternC);
                Util.ResolvePath(Project.SourceRootPath, ref SourceFilesExceptionsEnabledWithSEH);
                Util.ResolvePath(Project.SourceRootPath, ref AdditionalManifestFiles);

                if (ModuleDefinitionFile != null)
                {
                    Util.ResolvePath(Project.SourceRootPath, ref ModuleDefinitionFile);
                }

                if (Project.IsFileNameToLower)
                {
                    ProjectFileName = ProjectFileName.ToLowerInvariant();
                    BffFileName = BffFileName.ToLowerInvariant();
                }

                if (Project.IsTargetFileNameToLower)
                {
                    TargetFileName = TargetFileName.ToLowerInvariant();
                    TargetFileFullName = TargetFileFullName.ToLowerInvariant();
                    TargetFileSuffix = TargetFileSuffix.ToLowerInvariant();
                    TargetFilePrefix = TargetFilePrefix.ToLowerInvariant();
                }

                _resolvedTargetDependsFiles.AddRange(TargetDependsFiles);
                _resolvedTargetCopyFiles.AddRange(TargetCopyFiles);

                foreach (var tuple in new[] {
                    Tuple.Create(EventPreBuildExe,        _resolvedEventPreBuildExe),
                    Tuple.Create(EventPostBuildExe,       _resolvedEventPostBuildExe),
                    Tuple.Create(EventCustomPreBuildExe,  _resolvedEventCustomPreBuildExe),
                    Tuple.Create(EventCustomPostBuildExe, _resolvedEventCustomPostBuildExe),
                })
                {
                    UniqueList<BuildStepBase> eventsToResolve = tuple.Item1;
                    UniqueList<BuildStepBase> resolvedEvents = tuple.Item2;

                    foreach (BuildStepBase eventToResolve in eventsToResolve)
                        eventToResolve.Resolve(resolver);

                    resolvedEvents.AddRange(eventsToResolve);
                }

                foreach (var customFileBuildStep in CustomFileBuildSteps)
                {
                    customFileBuildStep.Resolve(resolver);
                    Util.ResolvePath(Project.SourceRootPath, ref customFileBuildStep.KeyInput);
                    Util.ResolvePath(Project.SourceRootPath, ref customFileBuildStep.Executable);
                    Util.ResolvePath(Project.SourceRootPath, ref customFileBuildStep.Output);
                    Util.ResolvePath(Project.SourceRootPath, ref customFileBuildStep.AdditionalInputs);
                }

                foreach (var eventDictionary in new[]{
                    EventPreBuildExecute,
                    EventCustomPrebuildExecute,
                    EventPostBuildExecute,
                    EventCustomPostBuildExecute
                })
                {
                    foreach (KeyValuePair<string, BuildStepBase> eventPair in eventDictionary)
                        eventPair.Value.Resolve(resolver);
                }

                if(PostBuildStampExe != null)
                    PostBuildStampExe.Resolve(resolver);

                string dependencyExtension = Util.GetProjectFileExtension(this);
                ProjectFullFileNameWithExtension = ProjectFullFileName + dependencyExtension;

                if (string.IsNullOrEmpty(ProjectGuid) && !this.Project.GetType().IsDefined(typeof(Compile), false))
                    ProjectGuid = Util.BuildGuid(ProjectFullFileNameWithExtension, Project.GuidReferencePath);

                if (PrecompHeader != null)
                    PrecompHeader = Util.SimplifyPath(PrecompHeader);
                if (PrecompSource != null)
                    PrecompSource = Util.SimplifyPath(PrecompSource);

                resolver.RemoveParameter("conf");
                resolver.RemoveParameter("target");
            }

            private void SetDependency(
                Type projectType,
                ITarget target,
                DependencySetting value
            )
            {
                KeyValuePair<Type, ITarget> pair = new KeyValuePair<Type, ITarget>(projectType, target);
                DependencySetting previousValue;

                if (value < 0) //LCTODO remove when the deprecated dependency settings are removed
                    value = DependencySetting.OnlyBuildOrder;

                if (_dependenciesSetting.TryGetValue(pair, out previousValue) && value != previousValue)
                {
                    _dependenciesSetting[pair] = value | previousValue;
                }
                else
                {
                    _dependenciesSetting[pair] = value;
                }
            }

            // These dependencies are always propagated to other dependent project.
            public void AddPublicDependency<TPROJECT>(
                ITarget target,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPublicDependency(target, typeof(TPROJECT), dependencySetting, sourceFilePath, sourceLineNumber);
            }
            public void AddPublicDependency(
                ITarget target,
                Type projectType,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                if (target == null)
                    return;
                if (HaveDependency(projectType))
                    throw new Error(Util.FormatCallerInfo(sourceFilePath, sourceLineNumber)
                                    + "error: Project configuration {0} already contains dependency to {1} for target {2}",
                                    Owner.GetType().ToNiceTypeName(),
                                    projectType.ToNiceTypeName(),
                                    target.ToString());
                UnResolvedPublicDependencies.Add(projectType, target);
                SetDependency(projectType, target, dependencySetting);
            }

            // These dependencies will be propagated to other dependent project, but not across dll dependencies.
            [Obsolete("Protected dependencies are deprecated, please use public/private instead.", error: false)]
            public void AddProtectedDependency<TPROJECT>(
                ITarget target,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPublicDependency(target, typeof(TPROJECT), dependencySetting, sourceFilePath, sourceLineNumber);
            }

            [Obsolete("Protected dependencies are deprecated, please use public/private instead.", error: false)]
            public void AddProtectedDependency(
                ITarget target,
                Type projectType,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPublicDependency(target, projectType, dependencySetting, sourceFilePath, sourceLineNumber);
            }

            // These dependencies will never be propagated to other project that depends on us
            public void AddPrivateDependency<TPROJECT>(
                ITarget target,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPrivateDependency(target, typeof(TPROJECT), dependencySetting, sourceFilePath, sourceLineNumber);
            }
            public void AddPrivateDependency(
                ITarget target,
                Type projectType,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                if (target == null)
                    return;
                if (HaveDependency(projectType))
                    throw new Error(Util.FormatCallerInfo(sourceFilePath, sourceLineNumber)
                                    + "error: Project configuration {0} already contains dependency to {1} for target {2}",
                                    Owner.GetType().ToNiceTypeName(),
                                    projectType.ToNiceTypeName(),
                                    target.ToString());

                UnResolvedPrivateDependencies.Add(projectType, target);
                SetDependency(projectType, target, dependencySetting);
            }

            // These dependencies will only be added in solution for build ordering
            [Obsolete("Solution only dependencies are deprecated, please use Private with OnlyBuildOrder flag instead.", error: false)]
            public void AddSolutionOnlyDependency<TPROJECT>(
                ITarget target,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPrivateDependency(target, typeof(TPROJECT), DependencySetting.OnlyBuildOrder, sourceFilePath, sourceLineNumber);
            }
            [Obsolete("Solution only dependencies are deprecated, please use Private with OnlyBuildOrder flag instead.", error: false)]
            public void AddSolutionOnlyDependency(
                ITarget target,
                Type projectType,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPrivateDependency(target, projectType, DependencySetting.OnlyBuildOrder, sourceFilePath, sourceLineNumber);
            }

            public bool HaveDependency<TPROJECT>()
            {
                return HaveDependency(typeof(TPROJECT));
            }
            public bool HaveDependency(Type projectType)
            {
                return UnResolvedPrivateDependencies.ContainsKey(projectType) || UnResolvedProtectedDependencies.ContainsKey(projectType) || UnResolvedPublicDependencies.ContainsKey(projectType);
            }

            /// <summary>
            /// Get the dependency setting configuration of the given project type based on this configuration
            /// </summary>
            /// <param name="projectType"></param>
            /// <returns>a dependency setting with all related flags activated</returns>
            public DependencySetting GetDependencySetting(Type projectType)
            {
                DependencySetting dependencyInheritance = DependencySetting.OnlyBuildOrder;
                foreach (var dependency in _dependenciesSetting)
                {
                    if (dependency.Key.Key == projectType)
                        dependencyInheritance |= dependency.Value;
                }
                return dependencyInheritance;
            }

            private Configuration GetDependencyConfiguration(Builder builder, Configuration visitedConfiguration, KeyValuePair<Type, ITarget> pair)
            {
                Project dependencyProject = builder.GetProject(pair.Key);
                if (dependencyProject == null)
                    throw new Error("resolving dependencies for {0}: cannot find project dependency of type {1} induced by {2}",
                        Owner.GetType().ToNiceTypeName(), pair.Key.ToNiceTypeName(), visitedConfiguration.ToString());

                Project.Configuration dependencyConf = dependencyProject.GetConfiguration(pair.Value);
                if (dependencyConf == null)
                {
                    string message =
                        string.Format(
                            "resolving dependencies for {0}: cannot find dependency project configuration {1} in project {2} induced by {3}",
                            Owner.GetType().ToNiceTypeName(), pair.Value, pair.Key.ToNiceTypeName(), visitedConfiguration.ToString());
                    if (pair.Value.GetType() == dependencyProject.Targets.TargetType)
                    {
                        message += string.Format(
                            ".  The target type is correct.  The error can be caused by missing calls to AddTargets or unwanted calls to AddFragmentMask in the constructor of {0}.",
                            dependencyProject.GetType().ToNiceTypeName());
                    }
                    else
                    {
                        message += string.Format(
                            ".  Are you passing the appropriate target type in AddDependency<{0}>(...)?  It should be type {1}.",
                            dependencyProject.GetType().ToNiceTypeName(), dependencyProject.Targets.TargetType.ToNiceTypeName());
                    }
                    System.Diagnostics.Trace.WriteLine(message);
                    System.Diagnostics.Debugger.Break();

                    throw new Error(message);
                }

                if (!dependencyConf.Target.IsEqualTo(pair.Value))
                    throw new Error(
                        "resolving dependencies for {0}: project {1} cannot depends other project on many target: {2} {3} (induced by {4})",
                        Owner.GetType().ToNiceTypeName(), dependencyProject.GetType().ToNiceTypeName(), dependencyConf.Target, pair.Value, visitedConfiguration.ToString());

                return dependencyConf;
            }

            private void GetRecursiveDependencies(
                HashSet<Configuration> resolved,
                HashSet<Configuration> unresolved
            )
            {
                foreach (Configuration c in ResolvedDependencies)
                {
                    if (resolved.Contains(c))
                        continue;

                    if (!unresolved.Add(c))
                        throw new Error($"Cyclic dependency detected while following dependency chain of configuration: {this}");

                    c.GetRecursiveDependencies(resolved, unresolved);

                    resolved.Add(c);
                    unresolved.Remove(c);
                }
            }

            internal List<Configuration> GetRecursiveDependencies()
            {
                var result = new HashSet<Configuration>();
                GetRecursiveDependencies(result, new HashSet<Configuration>());

                return result.ToList();
            }

            public DotNetReferenceCollection DotNetReferences = new DotNetReferenceCollection();

            public Strings ProjectReferencesByPath = new Strings();
            public Strings ReferencesByName = new Strings();
            public Strings ReferencesByNameExternal = new Strings();
            public Strings ReferencesByPath = new Strings();
            public string ConditionalReferencesByPathCondition = string.Empty;
            public Strings ConditionalReferencesByPath = new Strings();
            public Strings ForceUsingFiles = new Strings();

            // NuGet packages (only C# for now)
            public PackageReferences ReferencesByNuGetPackage = new PackageReferences();

            public bool? ReferenceOutputAssembly = null;

            private List<Configuration> _resolvedDependencies;
            public IEnumerable<Configuration> ResolvedDependencies => _resolvedDependencies;

            private List<Configuration> _resolvedPrivateDependencies;
            public IEnumerable<Configuration> ResolvedPrivateDependencies => _resolvedPrivateDependencies;

            private List<Configuration> _resolvedPublicDependencies;
            public IEnumerable<Configuration> ResolvedPublicDependencies => _resolvedPublicDependencies;

            private static int SortConfigurationForLink(Configuration l, Configuration r)
            {
                if (l.Project.DependenciesOrder != r.Project.DependenciesOrder)
                    return l.Project.DependenciesOrder.CompareTo(r.Project.DependenciesOrder);
                else
                    return l.Project.FullClassName.CompareTo(r.Project.FullClassName);
                //return l.Target.CompareTo(r.Target);
            }

            internal class DependencyNode
            {
                internal DependencyNode(Configuration inConfiguration, DependencySetting inDependencySetting)
                {
                    _configuration = inConfiguration;
                    _dependencySetting = inDependencySetting;
                }

                internal Configuration _configuration;
                internal DependencySetting _dependencySetting;
                internal Dictionary<DependencyNode, DependencyType> _childNodes = new Dictionary<DependencyNode, DependencyType>();
            }

            public class VcxprojUserFileSettings
            {
                public string LocalDebuggerCommand = RemoveLineTag;
                public string LocalDebuggerCommandArguments = RemoveLineTag;
                public string LocalDebuggerEnvironment = RemoveLineTag;
                public string LocalDebuggerWorkingDirectory = RemoveLineTag;
                public bool OverwriteExistingFile = true;
            }

            public VcxprojUserFileSettings VcxprojUserFile = null;

            public class CsprojUserFileSettings
            {
                public enum StartActionSetting
                {
                    Project,
                    Program,
                    URL
                }

                public StartActionSetting StartAction = StartActionSetting.Project;
                public string StartProgram = RemoveLineTag;
                public string StartURL = RemoveLineTag;
                public string StartArguments = RemoveLineTag;
                public string WorkingDirectory = RemoveLineTag;
                public bool OverwriteExistingFile = true;
            }
            public CsprojUserFileSettings CsprojUserFile = null;

            internal class PropagationSettings
            {
                internal PropagationSettings(DependencySetting inDependencySetting, bool inIsImmediate, bool inHasPublicPathToRoot, bool inHasPublicPathToImmediate, bool inGoesThroughDLL)
                {
                    _dependencySetting = inDependencySetting;
                    _isImmediate = inIsImmediate;
                    _hasPublicPathToRoot = inHasPublicPathToRoot;
                    _hasPublicPathToImmediate = inHasPublicPathToImmediate;
                    _goesThroughDLL = inGoesThroughDLL;
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj)) return false;
                    if (ReferenceEquals(this, obj)) return true;
                    if (obj.GetType() != GetType()) return false;

                    PropagationSettings other = (PropagationSettings)obj;

                    return _dependencySetting == other._dependencySetting &&
                           _isImmediate == other._isImmediate &&
                           _hasPublicPathToRoot == other._hasPublicPathToRoot &&
                           _hasPublicPathToImmediate == other._hasPublicPathToImmediate &&
                           _goesThroughDLL == other._goesThroughDLL;
                }

                public override int GetHashCode()
                {
                    unchecked // Overflow is fine, just wrap
                    {
                        int hash = 17;
                        hash = hash * 23 + _dependencySetting.GetHashCode();
                        hash = hash * 23 + _isImmediate.GetHashCode();
                        hash = hash * 23 + _hasPublicPathToRoot.GetHashCode();
                        hash = hash * 23 + _hasPublicPathToImmediate.GetHashCode();
                        hash = hash * 23 + _goesThroughDLL.GetHashCode();
                        return hash;
                    }
                }

                internal DependencySetting _dependencySetting;
                internal bool _isImmediate;
                internal bool _hasPublicPathToRoot;
                internal bool _hasPublicPathToImmediate;
                internal bool _goesThroughDLL;
            }

            internal void Link(Builder builder)
            {
                if (builder.DumpDependencyGraph)
                {
                    DependencyTracker.Instance.AddDependency(DependencyType.Public, Project, this, UnResolvedPublicDependencies, _dependenciesSetting);
                    DependencyTracker.Instance.AddDependency(DependencyType.Private, Project, this, UnResolvedPrivateDependencies, _dependenciesSetting);
                }

                // Check if we need to add dependencies on lib that we compile (in the current solution)
                bool explicitDependenciesGlobal =
                    Sharpmake.Options.GetObject<Options.Vc.Linker.LinkLibraryDependencies>(this) !=
                    Sharpmake.Options.Vc.Linker.LinkLibraryDependencies.Enable;

                DependencyNode rootNode = new DependencyNode(this, DependencySetting.Default);

                Dictionary<Configuration, DependencyNode> visited = new Dictionary<Configuration, DependencyNode>();

                Stack<DependencyNode> visiting = new Stack<DependencyNode>();
                visiting.Push(rootNode);
                while (visiting.Count > 0)
                {
                    DependencyNode visitedNode = visiting.Pop();
                    Configuration visitedConfiguration = visitedNode._configuration;

                    // if we already know that configuration, just reattach its children to the current node
                    DependencyNode alreadyExisting = null;
                    if (visited.TryGetValue(visitedConfiguration, out alreadyExisting))
                    {
                        foreach (var child in alreadyExisting._childNodes)
                        {
                            System.Diagnostics.Debug.Assert(!visitedNode._childNodes.ContainsKey(child.Key));
                            visitedNode._childNodes.Add(child.Key, child.Value);
                        }
                        continue;
                    }

                    visited.Add(visitedConfiguration, visitedNode);

                    var unresolvedDependencies = new[] { visitedConfiguration.UnResolvedPublicDependencies, visitedConfiguration.UnResolvedPrivateDependencies };
                    foreach (Dictionary<Type, ITarget> dependencies in unresolvedDependencies)
                    {
                        if (dependencies.Count == 0)
                            continue;

                        bool isPrivateDependency = dependencies == visitedConfiguration.UnResolvedPrivateDependencies;
                        DependencyType dependencyType = isPrivateDependency ? DependencyType.Private : DependencyType.Public;

                        foreach (KeyValuePair<Type, ITarget> pair in dependencies)
                        {
                            Configuration dependencyConf = GetDependencyConfiguration(builder, visitedConfiguration, pair);

                            // Get the dependency settings from the owner of the dependency.
                            DependencySetting dependencySetting;
                            if (!visitedConfiguration._dependenciesSetting.TryGetValue(pair, out dependencySetting))
                                dependencySetting = DependencySetting.Default;

                            DependencyNode childNode = new DependencyNode(dependencyConf, dependencySetting);
                            System.Diagnostics.Debug.Assert(!visitedNode._childNodes.ContainsKey(childNode));
                            visitedNode._childNodes.Add(childNode, dependencyType);

                            visiting.Push(childNode);
                        }
                    }
                }

                HashSet<Configuration> resolvedPublicDependencies = new HashSet<Configuration>();
                HashSet<Configuration> resolvedPrivateDependencies = new HashSet<Configuration>();

                var resolvedDotNetPublicDependencies = new HashSet<DotNetDependency>();
                var resolvedDotNetPrivateDependencies = new HashSet<DotNetDependency>();

                var visitedNodes = new Dictionary<DependencyNode, List<PropagationSettings>>();
                var visitingNodes = new Stack<Tuple<DependencyNode, PropagationSettings>>();
                visitingNodes.Push(Tuple.Create(rootNode, new PropagationSettings(DependencySetting.Default, true, true, true, false)));

                while (visitingNodes.Count > 0)
                {
                    var visitedTuple = visitingNodes.Pop();

                    var visitedNode = visitedTuple.Item1;
                    var propagationSetting = visitedTuple.Item2;

                    bool nodeAlreadyVisited = visitedNodes.ContainsKey(visitedNode);
                    if (nodeAlreadyVisited && visitedNodes[visitedNode].Contains(propagationSetting))
                        continue;

                    if (!nodeAlreadyVisited)
                        visitedNodes[visitedNode] = new List<PropagationSettings>();
                    visitedNodes[visitedNode].Add(propagationSetting);

                    Configuration dependency = visitedNode._configuration;

                    bool isRoot = visitedNode == rootNode;
                    bool isImmediate = propagationSetting._isImmediate;
                    bool hasPublicPathToRoot = propagationSetting._hasPublicPathToRoot;
                    bool hasPublicPathToImmediate = propagationSetting._hasPublicPathToImmediate;
                    bool goesThroughDLL = propagationSetting._goesThroughDLL;

                    foreach (var childNode in visitedNode._childNodes)
                    {
                        var childTuple = Tuple.Create(
                            childNode.Key,
                            new PropagationSettings(
                                isRoot ? childNode.Key._dependencySetting : (propagationSetting._dependencySetting & childNode.Key._dependencySetting), // propagate the parent setting by masking it
                                isRoot, // only children of root are immediate
                                (isRoot || hasPublicPathToRoot) && childNode.Value == DependencyType.Public,
                                (isImmediate || hasPublicPathToImmediate) && childNode.Value == DependencyType.Public,
                                !isRoot && (goesThroughDLL || visitedNode._configuration.Output == OutputType.Dll)
                            )
                        );

                        visitingNodes.Push(childTuple);
                    }

                    if (isRoot)
                        continue;

                    if (hasPublicPathToRoot)
                    {
                        resolvedPrivateDependencies.Remove(dependency);
                        resolvedPublicDependencies.Add(dependency);
                    }
                    else if (!resolvedPublicDependencies.Contains(dependency))
                    {
                        resolvedPrivateDependencies.Add(dependency);
                    }

                    var dependencySetting = propagationSetting._dependencySetting;
                    if (dependencySetting != DependencySetting.OnlyBuildOrder)
                    {
                        _resolvedEventPreBuildExe.AddRange(dependency.EventPreBuildExe);
                        _resolvedEventPostBuildExe.AddRange(dependency.EventPostBuildExe);
                        _resolvedEventCustomPreBuildExe.AddRange(dependency.EventCustomPreBuildExe);
                        _resolvedEventCustomPostBuildExe.AddRange(dependency.EventCustomPostBuildExe);
                        _resolvedTargetCopyFiles.AddRange(dependency.TargetCopyFiles);
                        _resolvedTargetDependsFiles.AddRange(dependency.TargetCopyFiles);
                        _resolvedTargetDependsFiles.AddRange(dependency.TargetDependsFiles);
                        _resolvedExecDependsFiles.AddRange(dependency.EventPreBuildExe);
                        _resolvedExecDependsFiles.AddRange(dependency.EventPostBuildExe);
                    }

                    bool isExport = dependency.Project.GetType().IsDefined(typeof(Export), false);
                    bool compile = dependency.Project.GetType().IsDefined(typeof(Generate), false) ||
                                   dependency.Project.GetType().IsDefined(typeof(Compile), false);

                    if (dependency.Output == OutputType.Lib || dependency.Output == OutputType.Dll || dependency.Output == OutputType.None)
                    {
                        bool wantIncludePaths = isImmediate || hasPublicPathToImmediate;
                        if (wantIncludePaths && dependencySetting.HasFlag(DependencySetting.IncludePaths))
                        {
                            DependenciesIncludePaths.AddRange(dependency.IncludePaths);

                            // Is there a case where we want the defines but *not* the include paths?
                            if (dependencySetting.HasFlag(DependencySetting.Defines))
                                Defines.AddRange(dependency.ExportDefines);
                        }
                    }

                    switch (dependency.Output)
                    {
                        case OutputType.None:
                        case OutputType.Lib:
                            {
                                bool dependencyOutputLib = dependency.Output == OutputType.Lib;

                                if (dependencyOutputLib && !goesThroughDLL &&
                                    (Output == OutputType.Lib ||
                                     dependency.ExportSymbolThroughProject == null ||
                                     dependency.ExportSymbolThroughProject == Project.GetType())
                                )
                                {
                                    if (explicitDependenciesGlobal || !compile)
                                        PlatformRegistry.Get<IConfigurationTasks>(dependency.Platform).SetupStaticLibraryPaths(this, dependencySetting, dependency);
                                    if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                                        ConfigurationDependencies.Add(dependency);
                                }

                                if (!goesThroughDLL)
                                {
                                    if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                                        DependenciesLibraryPaths.AddRange(dependency.LibraryPaths);

                                    if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                                        DependenciesLibraryFiles.AddRange(dependency.LibraryFiles);
                                }
                            }
                            break;
                        case OutputType.Dll:
                            {
                                if (dependency.ExportDllSymbols && (isImmediate || hasPublicPathToRoot || !goesThroughDLL))
                                {
                                    if (explicitDependenciesGlobal || !compile)
                                        PlatformRegistry.Get<IConfigurationTasks>(dependency.Platform).SetupDynamicLibraryPaths(this, dependencySetting, dependency);
                                    if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                                        ConfigurationDependencies.Add(dependency);

                                    // check if that case is valid: dll with additional libs
                                    if (isExport && !goesThroughDLL)
                                    {
                                        if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                                            DependenciesLibraryPaths.AddRange(dependency.LibraryPaths);

                                        if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                                            DependenciesLibraryFiles.AddRange(dependency.LibraryFiles);
                                    }
                                }

                                if (dependencySetting.HasFlag(DependencySetting.AdditionalUsingDirectories))
                                    AdditionalUsingDirectories.Add(dependency.TargetPath);

                                if ((Output == OutputType.Exe || ExecuteTargetCopy) && dependency.TargetPath != TargetPath)
                                {
                                    _resolvedTargetCopyFiles.Add(Path.Combine(dependency.TargetPath, dependency.TargetFileFullName + ".dll"));
                                    if (!isExport) // Add PDBs only if the dependency is not an [export] project
                                    {
                                        _resolvedTargetCopyFiles.Add(dependency.LinkerPdbFilePath);

                                        if (dependency.CopyCompilerPdbToDependentTargets)
                                            _resolvedTargetCopyFiles.Add(dependency.CompilerPdbFilePath);
                                    }
                                    _resolvedEventPreBuildExe.AddRange(dependency.EventPreBuildExe);
                                    _resolvedEventPostBuildExe.AddRange(dependency.EventPostBuildExe);
                                    _resolvedEventCustomPreBuildExe.AddRange(dependency.EventCustomPreBuildExe);
                                    _resolvedEventCustomPostBuildExe.AddRange(dependency.EventCustomPostBuildExe);
                                }
                                _resolvedTargetDependsFiles.Add(Path.Combine(TargetPath, dependency.TargetFileFullName + ".dll"));

                                if (Util.IsDotNet(this))
                                {
                                    if (hasPublicPathToRoot)
                                        resolvedDotNetPublicDependencies.Add(new DotNetDependency(dependency));
                                    else if (isImmediate)
                                        resolvedDotNetPrivateDependencies.Add(new DotNetDependency(dependency));
                                }
                            }
                            break;
                        case OutputType.Exe:
                            {
                                if (Output != OutputType.Utility && Output != OutputType.Exe && Output != OutputType.None)
                                    throw new Error("Project {0} cannot depend on OutputType {1} {2}", this, Output, dependency);

                                if (hasPublicPathToRoot)
                                    resolvedDotNetPublicDependencies.Add(new DotNetDependency(dependency));
                                else if (isImmediate)
                                    resolvedDotNetPrivateDependencies.Add(new DotNetDependency(dependency));

                                ConfigurationDependencies.Add(dependency);
                            }
                            break;
                        case OutputType.Utility: throw new NotImplementedException(dependency.Project.Name + " " + dependency.Output);
                        case OutputType.DotNetConsoleApp:
                        case OutputType.DotNetClassLibrary:
                        case OutputType.DotNetWindowsApp:
                            {
                                if (dependencySetting.HasFlag(DependencySetting.AdditionalUsingDirectories))
                                    AdditionalUsingDirectories.Add(dependency.TargetPath);

                                bool? referenceOutputAssembly = ReferenceOutputAssembly;
                                if (dependencySetting == DependencySetting.OnlyBuildOrder)
                                    referenceOutputAssembly = false;

                                var dotNetDependency = new DotNetDependency(dependency)
                                {
                                    ReferenceOutputAssembly = referenceOutputAssembly
                                };

                                if (!resolvedDotNetPublicDependencies.Contains(dotNetDependency))
                                {
                                    if (hasPublicPathToRoot)
                                    {
                                        resolvedDotNetPrivateDependencies.Remove(dotNetDependency);
                                        resolvedDotNetPublicDependencies.Add(dotNetDependency);
                                    }
                                    else if ((isImmediate || hasPublicPathToImmediate))
                                    {
                                        resolvedDotNetPrivateDependencies.Add(dotNetDependency);
                                    }
                                }
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (resolvedPublicDependencies.Overlaps(resolvedPrivateDependencies) || resolvedDotNetPublicDependencies.Overlaps(resolvedDotNetPrivateDependencies))
                    throw new InternalError("Something goes wrong in Project.Configuration.ResolveDependencies(): same dependency resolved in public and private lists");

                // Will include to the project:
                //  - lib,dll: include paths
                //  - lib,dll: library paths and files
                //  - dll: copy dll to the output executable directory 
                _resolvedPublicDependencies = resolvedPublicDependencies.ToList();

                // Will include to the project to act as a project bridge:
                //  - lib: add Library paths and files to be able to link the executable
                //  - dll: Copy dll to the ouput path
                _resolvedPrivateDependencies = resolvedPrivateDependencies.ToList();

                DotNetPublicDependencies = resolvedDotNetPublicDependencies.ToList();
                DotNetPrivateDependencies = resolvedDotNetPrivateDependencies.ToList();

                // sort base on DependenciesOrder
                _resolvedPublicDependencies.Sort(SortConfigurationForLink);
                _resolvedPrivateDependencies.Sort(SortConfigurationForLink);

                _resolvedDependencies = new List<Configuration>();
                _resolvedDependencies.AddRange(_resolvedPublicDependencies);
                _resolvedDependencies.AddRange(_resolvedPrivateDependencies);
            }

            internal void SetDefaultOutputExtension()
            {
                if (string.IsNullOrEmpty(OutputExtension))
                    OutputExtension = PlatformRegistry.Get<IConfigurationTasks>(Platform).GetDefaultOutputExtension(Output);
            }
        }
    }
}
