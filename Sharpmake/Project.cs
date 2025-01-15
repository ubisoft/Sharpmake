// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Sharpmake
{
    [Resolver.Resolvable]
    public partial class Project : Configurable<Project.Configuration>
    {
        private string _name = "[project.ClassName]";                                     // Project Name
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public string XcodeUnitTestTargetName = "XCTest";

        private bool _isFileNameToLower = true;                                         // Makes the ProjectName ToLower or not
        public bool IsFileNameToLower
        {
            get { return _isFileNameToLower; }
            set { SetProperty(ref _isFileNameToLower, value); }
        }

        public string LowerName
        {
            get { return _name.ToLower(); }
        }

        private bool _isTargetFileNameToLower = true;                                         // Makes the ProjectName ToLower or not
        public bool IsTargetFileNameToLower
        {
            get { return _isTargetFileNameToLower; }
            set { SetProperty(ref _isTargetFileNameToLower, value); }
        }

        private ProjectTypeAttribute _sharpmakeProjectType = ProjectTypeAttribute.Unknown;
        public ProjectTypeAttribute SharpmakeProjectType
        {
            get
            {
                Trace.Assert(_sharpmakeProjectType != ProjectTypeAttribute.Unknown);
                return _sharpmakeProjectType;
            }
            internal set { _sharpmakeProjectType = value; }
        }

        public string ClassName { get; private set; }                                     // Project Class Name, ex: "MyProject"
        public string FullClassName { get; private set; }                                 // Full class name with is namespace, ex: "Sharpmake.Sample.MyProject"
        public string SharpmakeCsFileName { get; private set; }                           // File name of the c# project configuration, ex: "MyProject.sharpmake"
        public string SharpmakeCsPath { get; private set; }                               // Path of the CsFileName, ex: "c:\dev\MyProject"
        public string SharpmakeCsProjectPath => SharpmakeCsPath;                          // TODO LC: check what is expected

        private string _assemblyName = string.Empty;
        public string AssemblyName
        {
            get { return _assemblyName; }
            set { SetProperty(ref _assemblyName, value); }
        }

        private string _sourceRootPath = "[project.SharpmakeCsPath]";                     // Root path of source file for this project, ex: "c:\dev\MyProject\src"
        public string SourceRootPath
        {
            get { return _sourceRootPath; }
            set { SetProperty(ref _sourceRootPath, value); }
        }

        [Obsolete("This property is deprecated, scc info shouldn't be stored in the project files anymore", error: true)]
        public string PerforceRootPath;

        private string _rootPath = "";                                                    // RootPath used as key to generate ProjectGuid and as a path helper for finding source files
        public string RootPath
        {
            get { return _rootPath; }
            set { SetProperty(ref _rootPath, value); }
        }


        private NuGetPackageMode _nuGetReferenceType = NuGetPackageMode.VersionDefault;   // Determines the type of NuGet references generated for this project
        public NuGetPackageMode NuGetReferenceType
        {
            get { return _nuGetReferenceType; }
            set { SetProperty(ref _nuGetReferenceType, value); }
        }

        private DependenciesCopyLocalTypes _dependenciesCopyLocal = DependenciesCopyLocalTypes.Default; //used primarily for the .Net Framework
        public DependenciesCopyLocalTypes DependenciesCopyLocal
        {
            get { return _dependenciesCopyLocal; }
            set { SetProperty(ref _dependenciesCopyLocal, value); }
        }

        private string _guidReferencePath;
        public string GuidReferencePath
        {
            get { return _guidReferencePath ?? (!string.IsNullOrEmpty(RootPath) ? RootPath : SharpmakeCsPath); }
            set { SetProperty(ref _guidReferencePath, value); }
        }

        public Strings AdditionalSourceRootPaths = new Strings();  // More source directories to parse for files in addition to SourceRootPath
        public Strings SourceFiles = new Strings();                                     // Files in the project, may be full path of partial path from SourceRootPath
        private Strings _preFilterSourceFiles = new Strings();                          // Files in the project before we applied fitlering, may be full path of partial path from SourceRootPath
        public Strings PreFilterSourceFiles { get { return _preFilterSourceFiles; } }


        public Strings SourceFilesExtensions = new Strings(".cpp", ".c", ".cc", ".h", ".inl", ".hpp", ".hh", ".asm", ".nasm");// All files under SourceRootPath are evaluated, if match found, it will be added to SourceFiles
        public Strings SourceFilesCompileExtensions = new Strings(".cpp", ".cc", ".c", ".asm", ".nasm");         // File that match this regex compile
        public Strings SourceFilesCPPExtensions = new Strings(".cpp", ".cc");

        public Strings SourceFilesFilters = null;                                        // if !=  null, include only file in this filter

        private int _sourceFilesFiltersCount = 0;
        public int SourceFilesFiltersCount
        {
            get { return _sourceFilesFiltersCount; }
            set { SetProperty(ref _sourceFilesFiltersCount, value); }
        }

        public Strings SourceFilesExclude = new Strings();                              // Excluded files from the project, removed from SourceFiles

        public Strings SourceFilesIncludeRegex = new Strings();                         // files that match SourceFilesIncludeRegex and SourceFilesExtension from source directory will make SourceFiles

        public Strings SourceFilesFiltersRegex = new Strings();                         // Filters SourceFiles list

        public Strings SourceFilesExcludeRegex = new Strings();                         // Sources file that match this regex will be excluded from build

        public Strings SourceFilesBuildExclude = new Strings();                         // Sources file to exclude from build from SourceFiles
        public Strings SourceFilesBuildExcludeRegex = new Strings();
        public Strings SourceFilesBuildFiltersRegex = new Strings();

        public Strings SourceFilesCompileAsCRegex = new Strings();                      // Sources file that match this regex will be compiled as C Files
        public Strings SourceFilesCompileAsCPPRegex = new Strings();                    // Sources file that match this regex will be compiled as CPP Files

        public Strings SourceFilesCompileAsObjCRegex = new Strings();                   // Sources file that match this regex will be compiled as Obj-C Files
        public Strings SourceFilesCompileAsObjCPPRegex = new Strings();                 // Sources file that match this regex will be compiled as Obj-CPP Files

        public Strings SourceFilesCompileAsCLRRegex = new Strings();                    // Sources file that match this regex will be compiled as CLR Files
        public Strings SourceFilesCompileAsCLRExcludeRegex = new Strings();             // Sources files that match this regex will not be compiled as CLR Files
        public Strings SourceFilesCompileAsNonCLRRegex = new Strings();                 // Sources files that match this regex will specifically be compiled with the CLR flag set to false.

        public Strings SourceFilesCompileAsWinRTRegex = new Strings();                  // Sources file that match this regex will be compiled as WinRT Files
        public Strings SourceFilesExcludeAsWinRTRegex = new Strings();                  // Sources file that match this regex will be excluded from compilation as WinRT Files

        public Strings SourceFilesBlobExclude = new Strings();
        public Strings SourceFilesBlobExcludeRegex = new Strings();
        public Strings SourceFilesBlobExtensions = new Strings(".cpp");
        public Strings SourcePathsBlobExclude = new Strings();                          // List of paths excluded from blobs. This maps cleanly to UnityInputExcludePath but is also supported in msbuild.

        public Strings ResourceFiles = new Strings();
        public Strings ResourceFilesExtensions = new Strings();

        public string XcodeUnitTestSourceRootPath;                                      // Source root path in the project for Xcode unit test target
        public Strings XcodeUnitTestSourceFiles = new Strings();                        // Source files in the project for Xcode unit test target
        public Strings XcodeUnitTestSourceFilesBuildExclude = new Strings();            // Source files to be excluded from build from XcodeUnitTestSourceFiles

        public Strings NatvisFiles = new Strings();
        public Strings NatvisFilesExtensions = new Strings(".natvis");

        public Strings PRIFiles = new Strings();
        public Strings PRIFilesExtensions = new Strings(".resw");

        public Strings NoneFiles = new Strings();
        public Strings NoneExtensions = new Strings();

        public Strings NoneFilesCopyIfNewer = new Strings();
        public Strings NoneExtensionsCopyIfNewer = new Strings();

        // .Net builds support protofiles, https://docs.microsoft.com/en-us/aspnet/core/grpc/dotnet-grpc?view=aspnetcore-5.0
        public Strings ProtoFiles = new Strings();
        public Strings ProtoExtensions = new Strings();

        public Strings XResourcesResw = new Strings();

        public class XResourcesImgContainer : IEnumerable<string>
        {
            /// <summary>
            /// Adds a new XResourcesImg path, with an optional link
            /// </summary>
            /// <param name="path">The path</param>
            /// <param name="link">An optional link</param>
            public void Add(string path, string link = null)
            {
                _xResourcesImg[path] = link;
            }

            public int Count => _xResourcesImg.Count;

            public bool IsResolved { get; private set; } = false;

            public IEnumerator<string> GetEnumerator()
            {
                return _xResourcesImg.Keys.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _xResourcesImg.Keys.GetEnumerator();
            }

            public Dictionary<string, string> GetXResourcesImg() { return _xResourcesImg; }

            internal void Resolve(string sourceRootPath, Resolver resolver)
            {
                if (IsResolved)
                    return;

                if (_xResourcesImg.Any())
                {
                    var resolvedDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var xResourcePair in _xResourcesImg)
                    {
                        string path = resolver.Resolve(xResourcePair.Key);
                        Util.ResolvePath(sourceRootPath, ref path);
                        string link = resolver.Resolve(xResourcePair.Value);
                        resolvedDictionary.Add(path, link);
                    }
                    _xResourcesImg = resolvedDictionary;
                }

                IsResolved = true;
            }

            private Dictionary<string, string> _xResourcesImg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public XResourcesImgContainer XResourcesImg = new XResourcesImgContainer();

        public Strings CustomPropsFiles = new Strings();  // vs2010+ .props files
        public Strings CustomTargetsFiles = new Strings();  // vs2010+ .targets files

        public string NasmPropsFile = "";
        public string NasmTargetsFile = "";
        public string NasmExePath = "";
        public Strings NasmPreIncludedFiles = new Strings();

        public Strings LibraryPathsExcludeFromWarningRegex = new Strings();                 // Library paths where we want to ignore the path doesn't exist warning
        public Strings IncludePathsExcludeFromWarningRegex = new Strings();                 // Include paths where we want to ignore the path doesn't exist warning

        public Dictionary<string, string> ExtensionBuildTools = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);  // extension -> tool name

        public Dictionary<string, string> PreImportCustomProperties = new Dictionary<string, string>();      // pre import properties are added before any imports to the project xml as <Key>Value</Key>
        public Dictionary<string, string> CustomProperties = new Dictionary<string, string>();      // custom properties are added to the project xml as <Key>Value</Key>

        public Dictionary<string, string> CustomFilterMapping = new Dictionary<string, string>();  /// maps relative source directory to a custom filter path for vcxproj.filter files

        public bool ContainsASM = false;
        public bool ContainsNASM = false;

        // Some projects don't support changelist filter. and will generate build errors when trying to compile the resulting projects.
        public bool SkipProjectWhenFiltersActive = false;

        // Dependencies order is used to link project together in a specified order, something is used especially for the link where
        // symbols need to be near of each other for some libs. so project will link it's dependency starting form smallest DependenciesOrder.
        public const uint DependenciesOrderFirst = 0;
        public const uint DependenciesOrderNormal = uint.MaxValue / 2;
        public const uint DependenciesOrderLast = uint.MaxValue;

        private uint _dependenciesOrder = DependenciesOrderNormal;
        public uint DependenciesOrder
        {
            get { return _dependenciesOrder; }
            set { SetProperty(ref _dependenciesOrder, value); }
        }

        // For projects that output both dll and lib depending on the configuration (often the case in TG projects)
        // Setting this to true will force dependencies regardless of different output types.
        public bool AllowInconsistentDependencies = false;

        private string _blobPath = "[project.SourceRootPath]" + Path.DirectorySeparatorChar + "blob";
        public string BlobPath
        {
            get { return _blobPath; }
            set { SetProperty(ref _blobPath, value); }
        }

        private string _fastBuildUnityPath = null;
        public string FastBuildUnityPath
        {
            get { return _fastBuildUnityPath ?? _blobPath; }
            set { SetProperty(ref _fastBuildUnityPath, value); }
        }

        private int _blobCount;
        public int BlobCount
        {
            get { return _blobCount; }
            set { SetProperty(ref _blobCount, value); }
        }

        private int _blobSize = 2 * 1024 * 1024;
        public int BlobSize
        {
            get { return _blobSize; }
            set { SetProperty(ref _blobSize, value); }
        }

        private int _blobSizeOverflow = 128 * 1024;
        public int BlobSizeOverflow
        {
            get { return _blobSizeOverflow; }
            set { SetProperty(ref _blobSizeOverflow, value); }
        }

        private int _blobWorkFileCount = 0;
        public int BlobWorkFileCount
        {
            get { return _blobWorkFileCount; }
            set { SetProperty(ref _blobWorkFileCount, value); }
        }

        private bool _blobWorkEnabled = false;
        public bool BlobWorkEnabled
        {
            get { return _blobWorkEnabled; }
            set { SetProperty(ref _blobWorkEnabled, value); }
        }

        private string _blobWorkFileHeader = null;
        public string DefaultBlobWorkFileHeader
        {
            get { return _blobWorkFileHeader; }
            set { SetProperty(ref _blobWorkFileHeader, value); }
        }

        private string _blobWorkFileFooter = null;
        public string DefaultBlobWorkFileFooter
        {
            get { return _blobWorkFileFooter; }
            set { SetProperty(ref _blobWorkFileFooter, value); }
        }

        /// <summary>
        /// This property controls the casing of includes within the generated blob files.
        /// If true, the case will be fixed. Default is false.
        /// </summary>
        public bool BlobFixCasing
        {
            get { return _blobFixCasing; }
            set { SetProperty(ref _blobFixCasing, value); }
        }
        private bool _blobFixCasing = false;

        public static int BlobGenerated { get; private set; }

        public static int BlobUpdateToDate { get; private set; }

        public static bool BlobPragmaMessageEnabled { get; set; } = true;

        private static int s_FastBuildGeneratedFileCount = 0;
        public static int FastBuildGeneratedFileCount { get { return s_FastBuildGeneratedFileCount; } }

        public static void IncrementFastBuildGeneratedFileCount()
        {
            Interlocked.Increment(ref s_FastBuildGeneratedFileCount);
        }

        private static int s_FastBuildUpToDateFileCount = 0;
        public static int FastBuildUpToDateFileCount { get { return s_FastBuildUpToDateFileCount; } }

        public static void IncrementFastBuildUpToDateFileCount()
        {
            Interlocked.Increment(ref s_FastBuildUpToDateFileCount);
        }

        public static List<string> FastBuildMasterGeneratedFiles { get; } = new List<string>();

        public static void AddFastbuildMasterGeneratedFile(string file)
        {
            lock (FastBuildMasterGeneratedFiles)
            {
                FastBuildMasterGeneratedFiles.Add(file);
                IncrementFastBuildGeneratedFileCount();
            }
        }

        [Obsolete("DeployProject is obsolete, use DeployProjectType instead (DeployType.OnlyIfBuild for the case where DeployProject == true)", false)]
        public bool DeployProject
        {
            get { return DeployProjectType != DeployType.NoDeploy; }
            set { SetProperty(ref _deployProjectType, value ? DeployType.OnlyIfBuild : DeployType.NoDeploy); }
        }

        public enum DeployType
        {
            // The project would not be deployed.
            NoDeploy,
            // The project will be deployed only if it explicitly marked for build in visual studio
            // (which is true for exe, but could be turned off automatically if FastBuildAll project is generated). 
            OnlyIfBuild,
            // The project will be always deployed.
            AlwaysDeploy
        }

        private DeployType _deployProjectType = DeployType.NoDeploy;
        public DeployType DeployProjectType
        {
            get { return _deployProjectType; }
            set { SetProperty(ref _deployProjectType, value); }
        }

        public static int BlobCleaned { get; private set; }

        public static int BlobAlreadyCleaned { get; private set; }

        public bool UseResolvedSdkEnvironmentVariables = false;

        public bool UseRunFromPcDeployment = false;

        private string _runFromPcDeploymentRegisterCommand = null;
        public string RunFromPcDeploymentRegisterCommand
        {
            get { return _runFromPcDeploymentRegisterCommand; }
            set { SetProperty(ref _runFromPcDeploymentRegisterCommand, value); }
        }

        // If true, remove the source files from a FastBuild project's associated vcxproj file.
        private bool _stripFastBuildSourceFiles = true;
        public bool StripFastBuildSourceFiles
        {
            get { return _stripFastBuildSourceFiles; }
            set { SetProperty(ref _stripFastBuildSourceFiles, value); }
        }

        public bool IsFastBuildAll
        {
            get { return this is FastBuildAllProject; }
        }

        private IEnumerable<Strings> GetStringFields()
        {
            yield return AdditionalSourceRootPaths;
            yield return SourceFiles;
            yield return SourceFilesExtensions;
            yield return SourceFilesCompileExtensions;
            yield return SourceFilesFilters;
            yield return SourceFilesExclude;
            yield return SourceFilesIncludeRegex;
            yield return SourceFilesFiltersRegex;
            yield return SourceFilesExcludeRegex;
            yield return SourceFilesBuildExclude;
            yield return SourceFilesBuildExcludeRegex;
            yield return SourceFilesBuildFiltersRegex;
            yield return SourceFilesCompileAsCRegex;
            yield return SourceFilesCompileAsCPPRegex;
            yield return SourceFilesCompileAsObjCRegex;
            yield return SourceFilesCompileAsObjCPPRegex;
            yield return SourceFilesCompileAsCLRRegex;
            yield return SourceFilesCompileAsCLRExcludeRegex;
            yield return SourceFilesCompileAsNonCLRRegex;
            yield return SourceFilesCompileAsWinRTRegex;
            yield return SourceFilesExcludeAsWinRTRegex;
            yield return SourceFilesBlobExclude;
            yield return SourceFilesBlobExcludeRegex;
            yield return SourceFilesBlobExtensions;
            yield return ResourceFiles;
            yield return ResourceFilesExtensions;
            yield return XcodeUnitTestSourceFiles;
            yield return XcodeUnitTestSourceFilesBuildExclude;
            yield return NatvisFiles;
            yield return NatvisFilesExtensions;
            yield return PRIFilesExtensions;
            yield return CustomPropsFiles;
            yield return CustomTargetsFiles;
            yield return ResolvedSourceFiles;
            yield return LibraryPathsExcludeFromWarningRegex;
            yield return IncludePathsExcludeFromWarningRegex;
        }

        public Project(Type targetType = null, Type configurationType = null)
        {
            Initialize(targetType ?? typeof(Target), configurationType ?? typeof(Project.Configuration));
        }

        /// <summary>
        /// Special constructor for utility projects generated internally,
        /// since these projects must handle paths differently
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="configurationType"></param>
        /// <param name="isInternal">Indicates if the class is defined within Sharpmake</param>
        internal Project(Type targetType, Type configurationType, bool isInternal)
        {
            Initialize(targetType, configurationType, isInternal);
        }

        protected override void PreInvokeConfiguration()
        {
            GetStringFields().Where(strings => strings != null).ForEach(elt => elt.SetReadOnly(true));
        }

        protected override void PostInvokeConfiguration()
        {
            GetStringFields().Where(strings => strings != null).ForEach(elt => elt.SetReadOnly(false));
        }

        public string ResolveString(string input, Configuration conf = null, ITarget target = null)
        {
            Resolver resolver = new Resolver();
            resolver.SetParameter("project", this);
            if (conf != null)
                resolver.SetParameter("conf", conf);
            if (target != null)
                resolver.SetParameter("target", target);
            return resolver.Resolve(input);
        }

        public static string GetPlatformDisableOptimizationString(Platform platform)
        {
            if (platform.IsUsingClang())
            {
                return "#pragma clang optimize off";
            }
            else
            {
                return "#pragma optimize(\"\", off)";
            }
        }

        public static string GetPlatformEnableOptimizationString(Platform platform)
        {
            if (platform.IsUsingClang())
            {
                return "#pragma clang optimize on";
            }
            else
            {
                return "#pragma optimize(\"\", on)";
            }
        }

        private protected static void AddMatchExtensionFiles(UniqueList<string> sourceFiles, Strings filesListToUpdate, Strings extensions)
        {
            foreach (string file in sourceFiles.Values)
            {
                // Support composite extensions(such as .sharpmake.cs)
                foreach (string matchExtension in extensions.Values)
                {
                    if (file.EndsWith(matchExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        filesListToUpdate.Add(file);
                        break;
                    }
                }
            }
        }

        private protected static bool AddMatchFiles(string rootPath, List<string> sourceFilesRelative, Strings sourceFilesAbsolute, Strings filesListToUpdate, IEnumerable<CachedRegex> regexList)
        {
            bool breakMatch = false;
            var sourceFilesAbsoluteUnsorted = sourceFilesAbsolute.Values;

            foreach (CachedRegex regex in regexList)
            {
                int arraySize = sourceFilesRelative.Count;
                for (int i = 0; i < arraySize; ++i)
                {
                    string relativeFilePath = sourceFilesRelative[i];
                    if (regex.Match(relativeFilePath).Success)
                    {
                        if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.AddMatchFiles, sourceFilesAbsoluteUnsorted[i]))
                            breakMatch = true;

                        // Remove .. occurrences from middle of the absolute path.
                        string simplifiedPath = Util.SimplifyPath(sourceFilesAbsoluteUnsorted[i]);
                        filesListToUpdate.Add(simplifiedPath); // TODO: remove from sourceFilesRelative if match
                    }
                }
            }
            return breakMatch;
        }

        private protected static bool AddNoMatchFiles(string rootPath, List<string> sourceFilesRelative, Strings sourceFilesAbsolute, Strings filesListToUpdate, IEnumerable<CachedRegex> regexList)
        {
            bool breakMatch = false;
            var sourceFilesAbsoluteUnsorted = sourceFilesAbsolute.Values;

            int arraySize = sourceFilesAbsoluteUnsorted.Count;
            for (int i = 0; i < arraySize; ++i)
            {
                string relativeFilePath = sourceFilesRelative[i];
                bool match = false;
                foreach (CachedRegex regex in regexList)
                {
                    if (regex.Match(relativeFilePath).Success)
                    {
                        match = true;
                        break;
                    }
                }
                if (!match)
                {
                    if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.AddMatchFiles, sourceFilesAbsoluteUnsorted[i]))
                        breakMatch = true;
                    filesListToUpdate.Add(sourceFilesAbsoluteUnsorted[i]);
                }
            }
            return breakMatch;
        }

        public Strings ResolvedSourceFiles = new Strings();
        internal Strings ResolvedSourceFilesBuildExclude = new Strings();


        // BlobPath defines the folder where blobs files are; since BlobPath can
        // be different between configurations, this class keeps the information
        // for the same BlobPath, which can be for all configurations, one or some.
        internal class BlobPathContent
        {
            public Strings ResolvedBlobSourceFiles = new Strings();       // [project.Name]_xyz.blob.cpp
            public Strings ResolvedBlobbedSourceFiles = new Strings();    // all source include by [project.Name]_xyz.blob.cpp

            public Strings AlwaysExclusions;  // excluded in all configurations
            public Strings PartialExclusions;  // excluded only in some configurations

            public Strings ResolvedBlobSourceFilesFromOtherContents = new Strings();  // should be excluded

            public List<Configuration> Configurations = new List<Configuration>();

            public string WorkBlobFileHeader = null;
            public string WorkBlobFileFooter = null;
            public BlobPathContent(Strings initialResolvedSourceFilesBuildExclude, Strings initialResolvedSourceFilesBlobExclude, string workBlobHeader, string workBlobFooter)
            {
                AlwaysExclusions = new Strings(initialResolvedSourceFilesBuildExclude);
                AlwaysExclusions.AddRange(initialResolvedSourceFilesBlobExclude);
                PartialExclusions = new Strings();
                WorkBlobFileHeader = workBlobHeader;
                WorkBlobFileFooter = workBlobFooter;
            }
            public void RegisterExclusion(Strings anotherResolvedSourceFilesBuildExclude, Strings anotherResolvedSourceFilesBlobExclude)
            {
                AlwaysExclusions.IntersectWith(anotherResolvedSourceFilesBuildExclude, PartialExclusions);
            }
        }

        // BlobPath -> BlobPathContent
        private Dictionary<string, BlobPathContent> _blobPathContents = new Dictionary<string, BlobPathContent>();

        // NoBlobbed files are not specific to a BlobPathContent, which might look weird at first.
        // NoBlobbed files are the sum of all the NoBlobbed resulting from all the BlobPaths in every
        // configurations.  The idea is that once a file cannot be in some blobs because it is
        // excluded from some of its configurations, then it must be excluded from all blobs in
        // all BlobPathContent, since multiple BlobPathContent can end up in the same project.
        // It could be implemented to look exactly which project files are using which BlobPaths, but
        // it's a complication not worth it, since typically BlobPaths should be tweaked to have
        // everything blobbed anyway.
        internal Strings ResolvedNoBlobbedSourceFiles = new Strings();  // all source excluded from by [project.Name]_xyz.blob.cpp

        public HashSet<string> GetAllConfigurationBuildExclude(IEnumerable<Configuration> configurations)
        {
            HashSet<string> result = new HashSet<string>();
            result.UnionWith(ResolvedSourceFiles);

            foreach (Project.Configuration conf in configurations)
            {
                result.IntersectWith(conf.ResolvedSourceFilesBuildExclude);
            }

            return result;
        }

        public Strings GetSourceFilesForConfigurations(IEnumerable<Configuration> configurations)
        {
            using (Builder.Instance.CreateProfilingScope("Project.GetSourceFilesForConfigurations", configurations.Count()))
            {
                // Remove blob files ?
                bool allBlobbed = true;
                bool allNoBlobbed = true;
                bool includeBlobbedSourceFiles = true;

                foreach (Project.Configuration conf in configurations)
                {
                    bool isBlobbed = conf.IsBlobbed;
                    allBlobbed &= isBlobbed;
                    allNoBlobbed &= !isBlobbed;
                    includeBlobbedSourceFiles &= conf.IncludeBlobbedSourceFiles;
                }

                Strings result = new Strings();
                result.AddRange(ResourceFiles);

                if (allBlobbed && !includeBlobbedSourceFiles)
                {
                    foreach (var entry in _blobPathContents)
                    {
                        result.AddRange(entry.Value.ResolvedBlobSourceFiles);
                    }
                    result.AddRange(ResolvedNoBlobbedSourceFiles);
                    result.RemoveRange(GetAllConfigurationBuildExclude(configurations));
                }
                else if (allNoBlobbed)
                {
                    result.AddRange(ResolvedSourceFiles);
                    foreach (var entry in _blobPathContents)
                    {
                        result.RemoveRange(entry.Value.ResolvedBlobSourceFiles);
                    }
                }
                else
                {
                    result.AddRange(ResolvedSourceFiles);
                }

                return result;
            }
        }

        internal Strings GetConfigurationsNoBlobSourceFiles(Strings sourceFiles)
        {
            Trace.Assert(_blobPathContents.Count == 0);

            Strings precompSource = new Strings();              // full path
            Strings noBlobbebSourceFiles = new Strings();       // partial path

            if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.Blobbing, sourceFiles))
                Debugger.Break();

            // Add all precomp files
            foreach (Configuration conf in Configurations)
            {
                if (!string.IsNullOrEmpty(conf.PrecompSource))
                    precompSource.Add(conf.PrecompSource);
            }

            // Go through all different BlobPath values to know partial exclusions and total exclusions
            foreach (Configuration conf in Configurations)
            {
                string blobPath = conf.BlobPath;
                BlobPathContent content = null;
                if (!_blobPathContents.TryGetValue(blobPath, out content))
                {
                    if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.Blobbing, conf.ResolvedSourceFilesBuildExclude, conf))
                        Debugger.Break();
                    if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.Blobbing, conf.ResolvedSourceFilesBlobExclude, conf))
                        Debugger.Break();
                    string workBlobHeader = conf.BlobWorkFileHeader ?? DefaultBlobWorkFileHeader;
                    string workBlobFooter = conf.BlobWorkFileFooter ?? DefaultBlobWorkFileFooter;
                    content = new BlobPathContent(conf.ResolvedSourceFilesBuildExclude, conf.ResolvedSourceFilesBlobExclude, workBlobHeader, workBlobFooter);
                    _blobPathContents.Add(blobPath, content);
                }
                else
                {
                    if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.Blobbing, conf.ResolvedSourceFilesBuildExclude, conf))
                        Debugger.Break();
                    if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.Blobbing, conf.ResolvedSourceFilesBlobExclude, conf))
                        Debugger.Break();
                    content.RegisterExclusion(conf.ResolvedSourceFilesBuildExclude, conf.ResolvedSourceFilesBlobExclude);
                }
                content.Configurations.Add(conf);
            }

            Strings excludedInAllBlobPaths = null;
            foreach (var entry in _blobPathContents)
            {
                if (excludedInAllBlobPaths == null)
                    excludedInAllBlobPaths = new Strings(entry.Value.AlwaysExclusions);
                else
                    excludedInAllBlobPaths.IntersectWith(entry.Value.AlwaysExclusions);
            }

            foreach (string sourceFile in sourceFiles)
            {
                if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.Blobbing, sourceFile))
                    Debugger.Break();

                foreach (string precompSourceFile in precompSource)
                {
                    if (sourceFile.EndsWith(precompSourceFile, StringComparison.OrdinalIgnoreCase))
                        noBlobbebSourceFiles.Add(sourceFile);
                }

                if (ResolvedSourceFilesBuildExclude.Contains(sourceFile))
                    noBlobbebSourceFiles.Add(sourceFile);

                if (excludedInAllBlobPaths.Contains(sourceFile))
                    noBlobbebSourceFiles.Add(sourceFile);

                // Only unblob files that are excluded in some configs but not all
                foreach (var entry in _blobPathContents)
                {
                    if (entry.Value.PartialExclusions.Contains(sourceFile))
                        noBlobbebSourceFiles.Add(sourceFile);
                }

                foreach (string blobExcludedPath in SourcePathsBlobExclude)
                {
                    string excludedPath = Util.SimplifyPath(blobExcludedPath);
                    if (sourceFile.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        noBlobbebSourceFiles.Add(sourceFile);
                        break;
                    }
                }
            }

            return noBlobbebSourceFiles;
        }

        private class RegexResolver
        {
            private static readonly ThreadLocal<System.Security.Cryptography.MD5> s_md5Hasher = new ThreadLocal<System.Security.Cryptography.MD5>(() => System.Security.Cryptography.MD5.Create());
            public RegexResolver()
            {
                ConfRegexesHashToAnyConf = new Dictionary<string, Configuration>();
                ConfToRegexesHash = new Dictionary<Configuration, string>();
            }
            public void Register(Project.Configuration conf, Strings str)
            {
                string md5 = "";
                if (str.Count != 0)
                {
                    var stream = new MemoryStream(200);
                    var writer = new BinaryWriter(stream);
                    writer.Write(str.Count);
                    foreach (var val in str)
                        writer.Write(val);
                    byte[] data = s_md5Hasher.Value.ComputeHash(stream);
                    md5 = BitConverter.ToString(data);
                }
                ConfToRegexesHash[conf] = md5;
                ConfRegexesHashToAnyConf[md5] = conf;
            }

            public void Register(Project.Configuration conf, params Strings[] strs)
            {
                string md5 = "";
                bool hasSomeRegexes = false;
                foreach (var str in strs)
                {
                    if (str.Count != 0)
                    {
                        hasSomeRegexes = true;
                        break;
                    }
                }
                if (hasSomeRegexes)
                {
                    var stream = new MemoryStream(500);
                    var writer = new BinaryWriter(stream);
                    foreach (var str in strs)
                    {
                        writer.Write(str.Count);
                        foreach (var val in str)
                            writer.Write(val);
                    }
                    byte[] buffer = stream.ToArray();
                    byte[] data = s_md5Hasher.Value.ComputeHash(stream.ToArray());
                    md5 = BitConverter.ToString(data);
                }
                ConfToRegexesHash[conf] = md5;
                ConfRegexesHashToAnyConf[md5] = conf;
            }

            public Project.Configuration GetCorrespondingConf(Project.Configuration conf)
            {
                return ConfRegexesHashToAnyConf[ConfToRegexesHash[conf]];
            }

            public Dictionary<string, Configuration> ConfRegexesHashToAnyConf;
            public Dictionary<Configuration, string> ConfToRegexesHash;
        }

        internal virtual void ResolveSourceFiles(Builder builder)
        {
            var sourceFilesIncludeRegex = RegexCache.GetCachedRegexes(SourceFilesIncludeRegex);
            var sourceFilesFiltersRegex = RegexCache.GetCachedRegexes(SourceFilesFiltersRegex);
            var sourceFilesExcludeRegex = RegexCache.GetCachedRegexes(SourceFilesExcludeRegex);
            var sourceFilesBuildExcludeRegex = RegexCache.GetCachedRegexes(SourceFilesBuildExcludeRegex);
            var sourceFilesBuildFiltersRegex = RegexCache.GetCachedRegexes(SourceFilesBuildFiltersRegex);
            var sourceFilesCompileAsCRegex = RegexCache.GetCachedRegexes(SourceFilesCompileAsCRegex);
            var sourceFilesCompileAsCPPRegex = RegexCache.GetCachedRegexes(SourceFilesCompileAsCPPRegex);
            var sourceFilesCompileAsObjCRegex = RegexCache.GetCachedRegexes(SourceFilesCompileAsObjCRegex);
            var sourceFilesCompileAsObjCPPRegex = RegexCache.GetCachedRegexes(SourceFilesCompileAsObjCPPRegex);
            var sourceFilesCompileAsCLRRegex = RegexCache.GetCachedRegexes(SourceFilesCompileAsCLRRegex);
            var sourceFilesCompileAsCLRExcludeRegex = RegexCache.GetCachedRegexes(SourceFilesCompileAsCLRExcludeRegex);
            var sourceFilesCompileAsNonCLRRegex = RegexCache.GetCachedRegexes(SourceFilesCompileAsNonCLRRegex);
            var sourceFilesCompileAsWinRTRegex = RegexCache.GetCachedRegexes(SourceFilesCompileAsWinRTRegex);
            var sourceFilesExcludeAsWinRTRegex = RegexCache.GetCachedRegexes(SourceFilesExcludeAsWinRTRegex);
            var sourceFilesBlobExcludeRegex = RegexCache.GetCachedRegexes(SourceFilesBlobExcludeRegex);

            if (!Util.DirectoryExists(SourceRootPath))
            {
                ResolveNonExistingSourcePath();
            }

            if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.Resolving, SourceFilesBuildExclude))
                Debugger.Break();
            ResolvedSourceFilesBuildExclude.AddRange(SourceFilesBuildExclude);

            if (NoneExtensionsCopyIfNewer.Count != 0)
            {
                NoneExtensions.RemoveAll(s => NoneExtensionsCopyIfNewer.Contains(s));
            }

            // Only scan directory for files if needed
            if (SourceFilesExtensions.Count != 0 || ResourceFilesExtensions.Count != 0 || PRIFilesExtensions.Count != 0 || NoneExtensions.Count != 0 || NoneExtensionsCopyIfNewer.Count != 0)
            {
                string capitalizedSourceRootPath = Util.GetCapitalizedPath(SourceRootPath);

                // Query all files in source directory
                DirectoryInfo sourceRootPathInfo = new DirectoryInfo(capitalizedSourceRootPath);
                Strings files = new Strings(GetDirectoryFiles(sourceRootPathInfo));

                AddMatchExtensionFiles(files, SourceFiles, SourceFilesExtensions);

                if (SourceFilesIncludeRegex.Count != 0)
                {
                    if (AddMatchFiles(RootPath, Util.PathGetRelative(RootPath, files), files, SourceFiles, sourceFilesIncludeRegex))
                        Debugger.Break();
                }

                // Additional source directories if any
                foreach (var additionalSourceRootPath in AdditionalSourceRootPaths)
                {
                    string capitalizedAdditionalSourceRootPath = Util.GetCapitalizedPath(additionalSourceRootPath);
                    DirectoryInfo additionalSourceRootPathInfo = new DirectoryInfo(capitalizedAdditionalSourceRootPath);
                    Strings additionalFiles = new Strings(GetDirectoryFiles(additionalSourceRootPathInfo));
                    AddMatchExtensionFiles(additionalFiles, SourceFiles, SourceFilesExtensions);

                    if (SourceFilesIncludeRegex.Count != 0)
                    {
                        if (AddMatchFiles(RootPath, Util.PathGetRelative(RootPath, additionalFiles), additionalFiles, SourceFiles, sourceFilesIncludeRegex))
                            Debugger.Break();
                    }

                    AddMatchExtensionFiles(additionalFiles, PRIFiles, PRIFilesExtensions);
                    AddMatchExtensionFiles(additionalFiles, ResourceFiles, ResourceFilesExtensions);
                    AddMatchExtensionFiles(additionalFiles, NatvisFiles, NatvisFilesExtensions);
                    AddMatchExtensionFiles(additionalFiles, NoneFiles, NoneExtensions);
                    AddMatchExtensionFiles(additionalFiles, NoneFilesCopyIfNewer, NoneExtensionsCopyIfNewer);
                    AddMatchExtensionFiles(additionalFiles, ProtoFiles, ProtoExtensions);
                }

                // Apply Filters 
                if (SourceFilesFiltersRegex.Count != 0)
                {
                    Strings allSourceFile = SourceFiles;
                    SourceFiles = new Strings();

                    if (AddMatchFiles(RootPath, Util.PathGetRelative(RootPath, allSourceFile), allSourceFile, SourceFiles, sourceFilesFiltersRegex))
                        Debugger.Break();
                }

                Util.ResolvePath(SourceRootPath, ref SourceFiles);

                if (SourceFilesBuildFiltersRegex.Count != 0)
                {
                    AddNoMatchFiles(RootPath, Util.PathGetRelative(RootPath, SourceFiles), SourceFiles, ResolvedSourceFilesBuildExclude, sourceFilesBuildFiltersRegex);
                }

                AddMatchExtensionFiles(files, PRIFiles, PRIFilesExtensions);
                Util.ResolvePath(SourceRootPath, ref PRIFiles);

                AddMatchExtensionFiles(files, ResourceFiles, ResourceFilesExtensions);
                Util.ResolvePath(SourceRootPath, ref ResourceFiles);

                AddMatchExtensionFiles(files, NatvisFiles, NatvisFilesExtensions);
                Util.ResolvePath(SourceRootPath, ref NatvisFiles);

                AddMatchExtensionFiles(files, NoneFiles, NoneExtensions);
                Util.ResolvePath(SourceRootPath, ref NoneFiles);

                AddMatchExtensionFiles(files, NoneFilesCopyIfNewer, NoneExtensionsCopyIfNewer);
                Util.ResolvePath(SourceRootPath, ref NoneFilesCopyIfNewer);

                AddMatchExtensionFiles(files, ProtoFiles, ProtoExtensions);
                Util.ResolvePath(SourceRootPath, ref ProtoFiles);
            }

            _preFilterSourceFiles.AddRange(SourceFiles);

            if (SourceFilesFilters != null)
            {
                // keep precomp
                Strings keepFiles = new Strings();
                Strings confPrecomps = new Strings();
                foreach (Configuration conf in Configurations)
                    if (conf.PrecompSource != null)
                        confPrecomps.Add(conf.PrecompSource);
                foreach (string sourceFile in SourceFiles)
                    foreach (string confPrecomp in confPrecomps)
                        if (sourceFile.EndsWith(confPrecomp, StringComparison.OrdinalIgnoreCase))
                            keepFiles.Add(sourceFile);

                SourceFiles.IntersectWith(SourceFilesFilters);
                SourceFiles.RemoveRange(SourceFilesExclude);
                SourceFilesFiltersCount = SourceFiles.Count(f => SourceFilesCompileExtensions.Contains(Path.GetExtension(f)));
                SourceFiles.AddRange(keepFiles);
                ResourceFiles.IntersectWith(SourceFilesFilters);
                NatvisFiles.IntersectWith(SourceFilesFilters);
                NoneFiles.IntersectWith(SourceFilesFilters);
                NoneFilesCopyIfNewer.IntersectWith(SourceFilesFilters);
                ProtoFiles.IntersectWith(SourceFilesFilters);
            }

            using (builder.CreateProfilingScope("Project.ResolveSourceFiles:AdditionalFiltering"))
                AdditionalFiltering(SourceFiles, ref SourceFilesExclude);

            // Add source files
            ResolvedSourceFiles.AddRange(SourceFiles);

            //Exclude files in IntermediatePath and OutputPath
            ExcludeOutputFiles();

            // Remove file that match SourceFilesExcludeRegex
            if (AddMatchFiles(RootPath, Util.PathGetRelative(RootPath, SourceFiles), SourceFiles, SourceFilesExclude, sourceFilesExcludeRegex))
                Debugger.Break();
            if (AddMatchFiles(RootPath, Util.PathGetRelative(RootPath, ResourceFiles), ResourceFiles, SourceFilesExclude, sourceFilesExcludeRegex))
                Debugger.Break();
            if (AddMatchFiles(RootPath, Util.PathGetRelative(RootPath, NatvisFiles), NatvisFiles, SourceFilesExclude, sourceFilesExcludeRegex))
                Debugger.Break();
            if (AddMatchFiles(RootPath, Util.PathGetRelative(RootPath, NoneFiles), NoneFiles, SourceFilesExclude, sourceFilesExcludeRegex))
                Debugger.Break();
            if (AddMatchFiles(RootPath, Util.PathGetRelative(RootPath, ProtoFiles), ProtoFiles, SourceFilesExclude, sourceFilesExcludeRegex))
                Debugger.Break();

            // Remove exclude file
            foreach (string excludeSourceFile in SourceFilesExclude)
            {
                ResolvedSourceFiles.Remove(excludeSourceFile);
                ResourceFiles.Remove(excludeSourceFile);
                NatvisFiles.Remove(excludeSourceFile);
                NoneFiles.Remove(excludeSourceFile);
                ProtoFiles.Remove(excludeSourceFile);
            }
            var resolvedSourceFilesRelative = Util.PathGetRelative(RootPath, ResolvedSourceFiles);

            // check if we need to blob the project
            bool oneBlobbed = false;
            bool fastBuildBlobs = false;

            var resolvedSourceFilesBuildExclude = new Strings();
            var resolvedSourceFilesWithCompileAsCOption = new Strings();
            var resolvedSourceFilesWithCompileAsCPPOption = new Strings();
            var resolvedSourceFilesWithCompileAsObjCOption = new Strings();
            var resolvedSourceFilesWithCompileAsObjCPPOption = new Strings();
            var resolvedSourceFilesWithCompileAsCLROption = new Strings();
            var compileAsClrFilesExclude = new Strings();
            var resolvedSourceFilesWithCompileAsNonCLROption = new Strings();
            var resolvedSourceFilesWithCompileAsWinRTOption = new Strings();
            var resolvedSourceFilesWithExcludeAsWinRTOption = new Strings();
            using (builder.CreateProfilingScope("Project.ResolveSourceFiles:ApplyProjectRegexes"))
            {
                // Do first part that is specific to project to not repeat for each config
                AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, resolvedSourceFilesBuildExclude, sourceFilesBuildExcludeRegex);
                AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, resolvedSourceFilesWithCompileAsCOption, sourceFilesCompileAsCRegex);
                AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, resolvedSourceFilesWithCompileAsCPPOption, sourceFilesCompileAsCPPRegex);
                AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, resolvedSourceFilesWithCompileAsObjCOption, sourceFilesCompileAsObjCRegex);
                AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, resolvedSourceFilesWithCompileAsObjCPPOption, sourceFilesCompileAsObjCPPRegex);
                AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, resolvedSourceFilesWithCompileAsCLROption, sourceFilesCompileAsCLRRegex);
                AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, compileAsClrFilesExclude, sourceFilesCompileAsCLRExcludeRegex);
                AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, resolvedSourceFilesWithCompileAsNonCLROption, sourceFilesCompileAsNonCLRRegex);
                AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, resolvedSourceFilesWithCompileAsWinRTOption, sourceFilesCompileAsWinRTRegex);
                AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, resolvedSourceFilesWithExcludeAsWinRTOption, sourceFilesExcludeAsWinRTRegex);
            }

            // Optimization: with projects with huge number of configurations, regexes are the exact same in multiple confs.
            // Here we group confs with exact same md5 of regexes to do a lot of matching a single time.
            var md5Hasher = System.Security.Cryptography.MD5.Create();
            var confRegexesHashToAnyConf = new Dictionary<string, Configuration>();
            var confToRegexesHash = new Dictionary<Configuration, string>();
            using (builder.CreateProfilingScope("Project.ResolveSourceFiles:GroupCommonRegexes", Configurations.Count))
            {
                foreach (Configuration conf in Configurations)
                {
                    var stream = new MemoryStream(2000);
                    var writer = new BinaryWriter(stream);
                    writer.Write(conf.SourceFilesBuildExcludeRegex.Count);
                    foreach (string val in conf.SourceFilesBuildExcludeRegex)
                        writer.Write(val);
                    writer.Write(conf.SourceFilesCompileAsCRegex.Count);
                    foreach (string val in conf.SourceFilesCompileAsCRegex)
                        writer.Write(val);
                    writer.Write(conf.SourceFilesCompileAsCPPRegex.Count);
                    foreach (string val in conf.SourceFilesCompileAsCPPRegex)
                        writer.Write(val);
                    writer.Write(conf.SourceFilesCompileAsObjCRegex.Count);
                    foreach (string val in conf.SourceFilesCompileAsObjCRegex)
                        writer.Write(val);
                    writer.Write(conf.SourceFilesCompileAsObjCPPRegex.Count);
                    foreach (string val in conf.SourceFilesCompileAsObjCPPRegex)
                        writer.Write(val);
                    writer.Write(conf.SourceFilesCompileAsCLRRegex.Count);
                    foreach (string val in conf.SourceFilesCompileAsCLRRegex)
                        writer.Write(val);
                    writer.Write(conf.SourceFilesCompileAsCLRExcludeRegex.Count);
                    foreach (string val in conf.SourceFilesCompileAsCLRExcludeRegex)
                        writer.Write(val);
                    writer.Write(conf.SourceFilesCompileAsNonCLRRegex.Count);
                    foreach (string val in conf.SourceFilesCompileAsNonCLRRegex)
                        writer.Write(val);
                    writer.Write(conf.SourceFilesCompileAsWinRTRegex.Count);
                    foreach (string val in conf.SourceFilesCompileAsWinRTRegex)
                        writer.Write(val);
                    writer.Write(conf.SourceFilesExcludeAsWinRTRegex.Count);
                    foreach (string val in conf.SourceFilesExcludeAsWinRTRegex)
                        writer.Write(val);
                    writer.Write(conf.SourceFilesFiltersRegex.Count);
                    foreach (string val in conf.SourceFilesFiltersRegex)
                        writer.Write(val);

                    byte[] data = md5Hasher.ComputeHash(stream.GetBuffer());
                    string md5 = BitConverter.ToString(data);
                    confToRegexesHash[conf] = md5;
                    confRegexesHashToAnyConf[md5] = conf;
                }
            }

            using (builder.CreateProfilingScope("Project.ResolveSourceFiles:ApplyCommonRegexes", confRegexesHashToAnyConf.Count))
            {
                foreach (var conf in confRegexesHashToAnyConf.Values)
                {
                    conf.ResolvedSourceFilesBuildExclude.AddRange(resolvedSourceFilesBuildExclude);
                    var configSourceFilesBuildExcludeRegex = RegexCache.GetCachedRegexes(conf.SourceFilesBuildExcludeRegex);
                    AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, conf.ResolvedSourceFilesBuildExclude, configSourceFilesBuildExcludeRegex);

                    // Resolve files that will be built as C Files 
                    conf.ResolvedSourceFilesWithCompileAsCOption.AddRange(resolvedSourceFilesWithCompileAsCOption);
                    var configSourceFilesCompileAsCRegex = RegexCache.GetCachedRegexes(conf.SourceFilesCompileAsCRegex);
                    AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, conf.ResolvedSourceFilesWithCompileAsCOption, configSourceFilesCompileAsCRegex);

                    // Resolve files that will be built as CPP Files 
                    conf.ResolvedSourceFilesWithCompileAsCPPOption.AddRange(resolvedSourceFilesWithCompileAsCPPOption);
                    var configSourceFilesCompileAsCPPRegex = RegexCache.GetCachedRegexes(conf.SourceFilesCompileAsCPPRegex);
                    AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, conf.ResolvedSourceFilesWithCompileAsCPPOption, configSourceFilesCompileAsCPPRegex);

                    // Resolve files that will be built as ObjC Files 
                    conf.ResolvedSourceFilesWithCompileAsObjCOption.AddRange(resolvedSourceFilesWithCompileAsObjCOption);
                    var configSourceFilesCompileAsObjCRegex = RegexCache.GetCachedRegexes(conf.SourceFilesCompileAsObjCRegex);
                    AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, conf.ResolvedSourceFilesWithCompileAsObjCOption, configSourceFilesCompileAsObjCRegex);

                    // Resolve files that will be built as ObjCPP Files 
                    conf.ResolvedSourceFilesWithCompileAsObjCPPOption.AddRange(resolvedSourceFilesWithCompileAsObjCPPOption);
                    var configSourceFilesCompileAsObjCPPRegex = RegexCache.GetCachedRegexes(conf.SourceFilesCompileAsObjCPPRegex);
                    AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, conf.ResolvedSourceFilesWithCompileAsObjCPPOption, configSourceFilesCompileAsObjCPPRegex);

                    // Resolve files that will be built as CLR Files 
                    conf.ResolvedSourceFilesWithCompileAsCLROption.AddRange(resolvedSourceFilesWithCompileAsCLROption);
                    var configSourceFilesCompileAsCLRRegex = RegexCache.GetCachedRegexes(conf.SourceFilesCompileAsCLRRegex);
                    AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, conf.ResolvedSourceFilesWithCompileAsCLROption, configSourceFilesCompileAsCLRRegex);

                    // Remove file that match SourceFilesCompileAsCLRExcludeRegex
                    var configCompileAsClrFilesExclude = new Strings();
                    configCompileAsClrFilesExclude.AddRange(compileAsClrFilesExclude);
                    var configSourceFilesCompileAsCLRExcludeRegex = RegexCache.GetCachedRegexes(conf.SourceFilesCompileAsCLRExcludeRegex);
                    AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, configCompileAsClrFilesExclude, configSourceFilesCompileAsCLRExcludeRegex);
                    conf.ResolvedSourceFilesWithCompileAsCLROption.RemoveRange(configCompileAsClrFilesExclude);

                    // Resolve non-CLR files.
                    conf.ResolvedSourceFilesWithCompileAsNonCLROption.AddRange(resolvedSourceFilesWithCompileAsNonCLROption);
                    var configSourceFilesCompileAsNonCLRRegex = RegexCache.GetCachedRegexes(conf.SourceFilesCompileAsNonCLRRegex);
                    AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, conf.ResolvedSourceFilesWithCompileAsNonCLROption, configSourceFilesCompileAsNonCLRRegex);

                    // Resolve files that will be built as WinRT Files 
                    conf.ResolvedSourceFilesWithCompileAsWinRTOption.AddRange(resolvedSourceFilesWithCompileAsWinRTOption);
                    var configSourceFilesCompileAsWinRTRegex = RegexCache.GetCachedRegexes(conf.SourceFilesCompileAsWinRTRegex);
                    AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, conf.ResolvedSourceFilesWithCompileAsWinRTOption, configSourceFilesCompileAsWinRTRegex);

                    // Resolve files that will not be built as WinRT Files 
                    conf.ResolvedSourceFilesWithExcludeAsWinRTOption.AddRange(resolvedSourceFilesWithExcludeAsWinRTOption);
                    var configSourceFilesExcludeAsWinRTRegex = RegexCache.GetCachedRegexes(conf.SourceFilesExcludeAsWinRTRegex);
                    AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, conf.ResolvedSourceFilesWithExcludeAsWinRTOption, configSourceFilesExcludeAsWinRTRegex);

                    conf.ResolvedSourceFilesBlobExclude.AddRange(conf.ResolvedSourceFilesWithCompileAsCOption);
                    conf.ResolvedSourceFilesBlobExclude.AddRange(conf.ResolvedSourceFilesWithCompileAsCPPOption);
                    conf.ResolvedSourceFilesBlobExclude.AddRange(conf.ResolvedSourceFilesWithCompileAsObjCOption);
                    conf.ResolvedSourceFilesBlobExclude.AddRange(conf.ResolvedSourceFilesWithCompileAsObjCPPOption);
                    conf.ResolvedSourceFilesBlobExclude.AddRange(conf.ResolvedSourceFilesWithCompileAsNonCLROption);
                    conf.ResolvedSourceFilesBlobExclude.AddRange(conf.ResolvedSourceFilesWithCompileAsCLROption);
                    conf.ResolvedSourceFilesBlobExclude.AddRange(conf.ResolvedSourceFilesWithCompileAsWinRTOption);
                    conf.ResolvedSourceFilesBlobExclude.AddRange(conf.ResolvedSourceFilesWithExcludeAsWinRTOption);

                    var configSourceFilesFiltersRegex = RegexCache.GetCachedRegexes(conf.SourceFilesFiltersRegex).ToArray();
                    if (conf.SourceFilesFiltersRegex.Count != 0)
                    {
                        foreach (var sourceFile in SourceFiles)
                        {
                            if (!configSourceFilesFiltersRegex.Any(regex => regex.Match(sourceFile).Success))
                                conf.ResolvedSourceFilesBuildExclude.Add(sourceFile);
                        }
                        Util.ResolvePath(SourceRootPath, ref conf.ResolvedSourceFilesBuildExclude);
                    }

                    // To allow sharing with other configs
                    conf.ResolvedSourceFilesWithCompileAsCOption.SetReadOnly(true);
                    conf.ResolvedSourceFilesWithCompileAsCPPOption.SetReadOnly(true);
                    conf.ResolvedSourceFilesWithCompileAsObjCOption.SetReadOnly(true);
                    conf.ResolvedSourceFilesWithCompileAsObjCPPOption.SetReadOnly(true);
                    conf.ResolvedSourceFilesWithCompileAsCLROption.SetReadOnly(true);
                    conf.ResolvedSourceFilesWithCompileAsNonCLROption.SetReadOnly(true);
                    conf.ResolvedSourceFilesWithCompileAsWinRTOption.SetReadOnly(true);
                    conf.ResolvedSourceFilesWithExcludeAsWinRTOption.SetReadOnly(true);
                }
            }

            using (builder.CreateProfilingScope("Project.ResolveSourceFiles:ApplyRegexesToConfigs", Configurations.Count))
            {
                foreach (Configuration conf in Configurations)
                {
                    if (conf.IsBlobbed)
                        oneBlobbed = true;

                    if (conf.IsFastBuild && conf.FastBuildBlobbed)
                        fastBuildBlobs = true;

                    conf.ResolvedSourceFilesBuildExclude.AddRange(SourceFilesExclude);

                    // add SourceFilesBuildExclude from the project
                    if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.Resolving, ResolvedSourceFilesBuildExclude))
                        Debugger.Break();
                    conf.ResolvedSourceFilesBuildExclude.AddRange(ResolvedSourceFilesBuildExclude);
                    if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.Resolving, conf.SourceFilesBuildExclude))
                        Debugger.Break();
                    conf.ResolvedSourceFilesBuildExclude.AddRange(conf.SourceFilesBuildExclude);

                    var doneConfWithSameRegexes = confRegexesHashToAnyConf[confToRegexesHash[conf]];

                    if (!Object.ReferenceEquals(doneConfWithSameRegexes, conf))
                    {
                        conf.ResolvedSourceFilesBuildExclude.AddRange(doneConfWithSameRegexes.ResolvedSourceFilesBuildExclude);

                        // Copy references here to be faster, these must not be modified from now on
                        conf.ResolvedSourceFilesWithCompileAsCOption = doneConfWithSameRegexes.ResolvedSourceFilesWithCompileAsCOption;
                        conf.ResolvedSourceFilesWithCompileAsCPPOption = doneConfWithSameRegexes.ResolvedSourceFilesWithCompileAsCPPOption;
                        conf.ResolvedSourceFilesWithCompileAsObjCOption = doneConfWithSameRegexes.ResolvedSourceFilesWithCompileAsObjCOption;
                        conf.ResolvedSourceFilesWithCompileAsObjCPPOption = doneConfWithSameRegexes.ResolvedSourceFilesWithCompileAsObjCPPOption;
                        conf.ResolvedSourceFilesWithCompileAsCLROption = doneConfWithSameRegexes.ResolvedSourceFilesWithCompileAsCLROption;
                        conf.ResolvedSourceFilesWithCompileAsNonCLROption = doneConfWithSameRegexes.ResolvedSourceFilesWithCompileAsNonCLROption;
                        conf.ResolvedSourceFilesWithCompileAsWinRTOption = doneConfWithSameRegexes.ResolvedSourceFilesWithCompileAsWinRTOption;
                        conf.ResolvedSourceFilesWithExcludeAsWinRTOption = doneConfWithSameRegexes.ResolvedSourceFilesWithExcludeAsWinRTOption;
                    }
                }
            }

            foreach (string sourceFile in ResolvedSourceFiles)
            {
                if (sourceFile.EndsWith(".asm", StringComparison.OrdinalIgnoreCase))
                {
                    ContainsASM = true;
                }
                if (sourceFile.EndsWith(".nasm", StringComparison.OrdinalIgnoreCase))
                {
                    ContainsNASM = true;
                }
            }

            if (oneBlobbed || fastBuildBlobs)
            {
                // Generator will use ResolvedSourceFiles and Configuration.ResolvedSourceFilesExclude, 
                // allow us to handle the blob here instead than in each generator

                // Don't blob precomp source file
                Strings configurationsNoBlobbedSourceFiles = GetConfigurationsNoBlobSourceFiles(ResolvedSourceFiles);

                // Exclude from blob all files that match any SourceFilesBlobExcludeRegex.
                if (AddMatchFiles(RootPath, resolvedSourceFilesRelative, ResolvedSourceFiles, SourceFilesBlobExclude, sourceFilesBlobExcludeRegex))
                    Debugger.Break();

                foreach (var conf in Configurations)
                    conf.ResolvedSourceFilesBlobExclude.AddRange(SourceFilesBlobExclude);

                // Use ResolvedSourceFiles sorted by relative paths
                var sortedResolvedSourceFiles = ResolvedSourceFiles.Zip(resolvedSourceFilesRelative, (file, relFile) => new Tuple<string, string>(file, relFile))
                                                                   .OrderBy(t => t.Item2, StringComparer.OrdinalIgnoreCase)
                                                                   .Select(t => t.Item1);

                foreach (string sourceFile in sortedResolvedSourceFiles)
                {
                    if (DebugBreaks.ShouldBreakOnSourcePath(DebugBreaks.Context.BlobbingResolving, sourceFile))
                        Debugger.Break();
                    string sourceFileExtension = Path.GetExtension(sourceFile);

                    if (SourceFilesCompileExtensions.Contains(sourceFileExtension))
                    {
                        if (SourceFilesBlobExtensions.Contains(sourceFileExtension) &&
                            !configurationsNoBlobbedSourceFiles.Contains(sourceFile) &&
                            !SourceFilesBlobExclude.Contains(sourceFile))
                        {
                            foreach (var entry in _blobPathContents)
                            {
                                if (!entry.Value.AlwaysExclusions.Contains(sourceFile) &&
                                    !entry.Value.PartialExclusions.Contains(sourceFile))
                                    entry.Value.ResolvedBlobbedSourceFiles.Add(sourceFile);
                            }
                        }
                        else
                        {
                            ResolvedNoBlobbedSourceFiles.Add(sourceFile);
                        }
                    }
                }

                foreach (var entry in _blobPathContents)
                {
                    BlobGenerateFiles(
                        entry.Key,
                        entry.Value.ResolvedBlobbedSourceFiles,
                        entry.Value.ResolvedBlobSourceFiles,
                        entry.Value.Configurations,
                        entry.Value.WorkBlobFileHeader,
                        entry.Value.WorkBlobFileFooter,
                        oneBlobbed
                    );
                    ResolvedSourceFiles.AddRange(entry.Value.ResolvedBlobSourceFiles);
                }

                // Set blob files from other blob paths
                foreach (var entry in _blobPathContents)
                {
                    foreach (var entry2 in _blobPathContents)
                    {
                        if (entry2.Key != entry.Key)
                        {
                            entry.Value.ResolvedBlobSourceFilesFromOtherContents.AddRange(entry2.Value.ResolvedBlobSourceFiles);
                        }
                    }
                }

                // Exclude blob files in other blob paths
                foreach (var config in Configurations)
                {
                    config.ResolvedSourceFilesBuildExclude.AddRange(_blobPathContents[config.BlobPath].ResolvedBlobSourceFilesFromOtherContents);
                }
            }

            foreach (Configuration conf in Configurations)
            {
                BlobPathContent blobContent = null;
                if (!_blobPathContents.TryGetValue(conf.BlobPath, out blobContent))
                {
                    continue;
                }

                if (conf.IsBlobbed)
                    conf.ResolvedSourceFilesBuildExclude.AddRange(blobContent.ResolvedBlobbedSourceFiles);
                else
                    conf.ResolvedSourceFilesBuildExclude.AddRange(blobContent.ResolvedBlobSourceFiles);
            }
        }

        public virtual void AdditionalFiltering(Strings sourceFiles, ref Strings sourceFilesExclude)
        {
        }

        public virtual bool ResolveFilterPath(string relativePath, out string filterPath)
        {
            filterPath = null;
            return false;
        }

        public virtual bool ResolveFilterPathForFile(string relativeFilePath, out string filterPath)
        {
            filterPath = null;
            return false;
        }

        protected virtual void ExcludeOutputFiles()
        {
        }

        private string BlobGenerateFile(string blobPath, IEnumerable<string> sourceFiles, string filename, List<Configuration> configurations, string header, string footer)
        {
            string blobFileName = Path.Combine(blobPath, filename + BlobExtension);

            FileInfo blobFileInfo = new FileInfo(blobFileName);
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);

            writer.Write("// generated by sharpmake" + Environment.NewLine + Environment.NewLine);

            Configuration configurationPrecomp = null;
            foreach (Configuration conf in configurations)
            {
                if (conf.PrecompHeader != null && conf.PrecompSource != null)
                {
                    if (configurationPrecomp == null)
                        configurationPrecomp = conf;
                    else if (conf.PrecompHeader != configurationPrecomp.PrecompHeader)
                        throw new Error("blob error: {0} project configurations using the same blob must all have the same precomp header : {1} and {2}", ClassName, configurationPrecomp.Target, conf.Target);
                }
            }

            // include precomp if needed
            if (configurationPrecomp != null)
                writer.Write("#include \"{0}\"" + Environment.NewLine + Environment.NewLine, configurationPrecomp.PrecompHeader.Replace(Util.WindowsSeparator, Util.UnixSeparator));

            if (header != null)
                writer.WriteLine(header + Environment.NewLine);

            foreach (string sourceFileFullPath in sourceFiles)
            {
                string sourceFile = BlobFixCasing ? Util.GetCapitalizedPath(sourceFileFullPath) : sourceFileFullPath;
                string sourceFileRelative = Util.PathGetRelative(blobFileInfo.Directory.FullName, sourceFile).Replace(Util.WindowsSeparator, Util.UnixSeparator);

                // Visual Studio will append the relative include path to the blob file path and will not resolve it before it is searching
                // for the included file in the file system. If that search path is longer than 256 characters it will fail to find it.
                // If the relative include file path that Visual Studio will use internally is too long use the absolute file path
                if (blobFileInfo.Directory.FullName.Length + sourceFileRelative.Length > 255)
                    sourceFileRelative = sourceFile.Replace(Util.WindowsSeparator, Util.UnixSeparator);

                // If the file path is too long the include will fail
                if (sourceFileRelative.Length > 256)
                    throw new Error("include path too long error: '{0}'", sourceFileRelative);

                string sourceFileDisplay = Util.PathGetRelative(SourceRootPath, sourceFile).Replace(Util.WindowsSeparator, Util.UnixSeparator);
                if (sourceFileDisplay.StartsWith("./", StringComparison.Ordinal))
                    sourceFileDisplay = sourceFileDisplay.Substring(2, sourceFileDisplay.Length - 2);

                if (BlobPragmaMessageEnabled)
                {
                    writer.Write("#pragma message(\"{0} - {1}\")" + Environment.NewLine, blobFileInfo.Name, sourceFileDisplay);
                }
                writer.Write("#include \"{0}\"" + Environment.NewLine + Environment.NewLine, sourceFileRelative);
            }

            if (footer != null)
                writer.WriteLine(footer + Environment.NewLine);

            // Write the blob file
            writer.Flush();

            bool written = Builder.Instance.Context.WriteGeneratedFile(null, blobFileInfo, stream);
            if (written)
                BlobGenerated++;
            else
                BlobUpdateToDate++;
            writer.Close();

            return blobFileName;
        }

        internal void CleanBlobs()
        {
            var doneFiles = new HashSet<string>();
            foreach (var entry in _blobPathContents)
            {
                foreach (var blob in entry.Value.ResolvedBlobSourceFiles)
                {
                    if (!doneFiles.Contains(blob))
                    {
                        ++BlobCleaned;
                        if ((new FileInfo(blob)).Length > 0)
                        {
                            File.WriteAllText(blob, "");
                        }
                        else
                        {
                            ++BlobAlreadyCleaned;
                        }
                        doneFiles.Add(blob);
                    }
                }
            }
        }

        private struct SourceFile
        {
            public SourceFile(string path, bool isWorkBlobCandidate)
            {
                Path = path;
                IsWorkBlobCandidate = isWorkBlobCandidate;
            }
            public string Path;
            public bool IsWorkBlobCandidate;
        }

        private void BlobGenerateFiles(
            string blobPath,
            Strings sourceFiles,
            Strings blobFiles,
            List<Configuration> configurations,
            string WorkBlobFileHeader,
            string WorkBlobFileFooter,
            bool writeBlobsOnDisk
        )
        {
            // Blob per directory, this make blob file more stable so it's change less 
            // when adding new sources files -> will save compile time :)
            List<SourceFile> currentBlobSourceFiles = new List<SourceFile>();
            var allBlobsFiles = new List<List<SourceFile>>();

            List<string> workBlobSourceFiles = null;
            if (BlobWorkEnabled)
                workBlobSourceFiles = new List<string>();

            int blobSize = configurations[0].BlobSize;
            foreach (var config in configurations)
            {
                if (config.BlobSize != blobSize)
                {
                    throw new Error(
                        "Cannot specify 2 different BlobSize, " + blobSize + " and " + config.BlobSize +
                        ", for blob path " + blobPath);
                }
            }
            if (blobSize == 0)
                blobSize = BlobSize;

            uint currentBlobSize = 0;
            uint totalWorkBlobSize = 0;

            string lastDirectoryFullName = "";

            foreach (string sourcefile in sourceFiles)
            {
                FileInfo sourceFileInfo = new FileInfo(sourcefile);
                if (sourceFileInfo.Directory.FullName != lastDirectoryFullName || (currentBlobSize > (blobSize + BlobSizeOverflow)))
                {
                    lastDirectoryFullName = sourceFileInfo.Directory.FullName;

                    if (currentBlobSize > blobSize)
                    {
                        allBlobsFiles.Add(currentBlobSourceFiles);
                        currentBlobSourceFiles = new List<SourceFile>();
                        currentBlobSize = 0;
                    }
                }

                uint currentFileSize = 0;
                if (Util.CountFakeFiles() > 0)
                {
                    currentFileSize = (uint)Util.GetFakeFileLength(sourceFileInfo.FullName);
                    if (currentFileSize == 0)
                        continue;
                }
                else if (sourceFileInfo.Exists)
                {
                    currentFileSize = (uint)sourceFileInfo.Length;
                }

                currentBlobSize += currentFileSize;

                bool isWorkBlobCandidate = (BlobWorkEnabled && !sourceFileInfo.IsReadOnly);
                currentBlobSourceFiles.Add(new SourceFile(sourcefile, isWorkBlobCandidate));
                if (isWorkBlobCandidate)
                {
                    workBlobSourceFiles.Add(sourcefile);
                    totalWorkBlobSize += currentFileSize;
                }
            }

            if (currentBlobSourceFiles.Count != 0)
            {
                allBlobsFiles.Add(currentBlobSourceFiles);
                currentBlobSourceFiles = null;
            }
            // Deactivate work blobs if too much files
            bool isBlobWorkEnabled = BlobWorkEnabled;
            if (isBlobWorkEnabled && totalWorkBlobSize > (BlobWorkFileCount * (blobSize + BlobSizeOverflow)))
            {
                isBlobWorkEnabled = false;
                workBlobSourceFiles.Clear();  //  to flush content of work blobs
            }

            // index of nb of blob created
            int nbBlobCreated = allBlobsFiles.Count;
            BlobCount = nbBlobCreated;

            // make the number of blobs a conf generates available to generators
            if (nbBlobCreated > 0)
            {
                foreach (Configuration conf in configurations)
                    conf.GeneratableBlobCount = nbBlobCreated;
            }

            // Capping the number of blob work to the number of blobs. It makes no sense to have more work blobs than blobs.
            if (BlobWorkFileCount > BlobCount)
                BlobWorkFileCount = BlobCount;

            // Write blobs
            if (writeBlobsOnDisk)
            {
                for (int i = 0; i < allBlobsFiles.Count; ++i)
                {
                    string blobFileName = string.Format(@"{0}_{1:000}", Name.ToLower(), i);
                    var blobbedFiles = (isBlobWorkEnabled) ?
                        from j in allBlobsFiles[i] where !j.IsWorkBlobCandidate select j.Path :
                        from j in allBlobsFiles[i] select j.Path;
                    blobFiles.Add(BlobGenerateFile(blobPath, blobbedFiles, blobFileName, configurations, null, null));
                }

                // write work blob size
                if (BlobWorkEnabled)
                {
                    var workBlobFiles = new List<List<string>>(BlobWorkFileCount);

                    for (int i = 0; i < BlobWorkFileCount; ++i)
                        workBlobFiles.Add(new List<string>());

                    foreach (string workkBlobSourceFile in workBlobSourceFiles)
                    {
                        string relativeWorkkBlobSourceFile = Util.PathGetRelative(SourceRootPath, workkBlobSourceFile);
                        int index = Math.Abs(Util.BuildGuid(relativeWorkkBlobSourceFile).GetHashCode() % workBlobFiles.Count);
                        workBlobFiles[index].Add(workkBlobSourceFile);
                    }

                    for (int i = 0; i < workBlobFiles.Count; ++i)
                    {
                        string blobFileName = string.Format(@"{0}_work_{1:000}", Name.ToLower(), i);
                        blobFiles.Add(BlobGenerateFile(blobPath, workBlobFiles[i], blobFileName, configurations, WorkBlobFileHeader, WorkBlobFileFooter));
                    }
                }
            }
        }

        [Flags]
        public enum DependenciesCopyLocalTypes
        {
            None = 0x00,
            ProjectReferences = 0x01,
            ExternalReferences = 0x02,
            DotNetReferences = 0x04,
            DotNetExtensions = 0x08,
            Default = DotNetExtensions | ProjectReferences | ExternalReferences,
        }

        public enum NuGetPackageMode
        {
            VersionDefault,
            PackageConfig,
            ProjectJson,
            PackageReference,
        }

        #region Internal


        internal HashSet<Type> GetUnresolvedDependenciesTypes()
        {
            HashSet<Type> dependencies = new HashSet<Type>();

            foreach (Project.Configuration conf in Configurations)
            {
                dependencies.UnionWith(conf.UnResolvedPublicDependencies.Keys);
                dependencies.UnionWith(conf.UnResolvedPrivateDependencies.Keys);
            }
            return dependencies;
        }


        internal bool Resolved { get; private set; }
        public Dictionary<string, List<Project.Configuration>> ProjectFilesMapping { get; } = new Dictionary<string, List<Configuration>>();

        public enum ProjectTypeAttribute
        {
            Unknown,
            Generate,
            Export,
            Compile
        }

        internal static Project CreateProject(Type projectType, List<object> fragmentMasks, ProjectTypeAttribute projectTypeAttribute)
        {
            Project project;

            project = Activator.CreateInstance(projectType) as Project;
            project.SharpmakeProjectType = projectTypeAttribute;

            project.Targets.SetGlobalFragmentMask(fragmentMasks.ToArray());
            project.Targets.BuildTargets();
            return project;
        }

        public Project.Configuration GetConfiguration(ITarget target)
        {
            if (target.GetType() != Targets.TargetType)
                return null;

            Configuration config;
            bool hasConfig = ConfigurationsCache.TryGetValue(target, out config);
            if (!hasConfig)
            {
                ReportError($"ConfigurationsCache in project '{Name}' does not contain configuration for '{target}'");
            }

            return config;
        }

        internal void Initialize(Type targetType, Type configurationType, bool isInternal = false)
        {
            var expectedType = typeof(Project.Configuration);
            if (configurationType == null || (configurationType != expectedType && !configurationType.IsSubclassOf(expectedType)))
                throw new InternalError("configuration type '{0}' must be a subclass of '{1}'", configurationType?.FullName ?? "<null>", expectedType.FullName);

            ConfigurationType = configurationType;

            ExtensionBuildTools[".asm"] = "MASM";
            ExtensionBuildTools[".nasm"] = "NASM";

            ClassName = GetType().Name;
            FullClassName = GetType().FullName;
            Targets.Initialize(targetType);

            string file;
            if (isInternal)
            {
                SharpmakeCsPath = Util.PathMakeStandard(AppDomain.CurrentDomain.BaseDirectory);
            }
            else if (Util.GetStackSourceFileTopMostTypeOf(GetType(), out file))
            {
                FileInfo fileInfo = new FileInfo(file);
                SharpmakeCsFileName = Util.PathMakeStandard(fileInfo.FullName);
                SharpmakeCsPath = Util.PathMakeStandard(fileInfo.DirectoryName);
            }
            else
            {
                throw new InternalError("Cannot locate cs source for type: {0}", GetType().Name);
            }
        }

        /// <summary>
        /// Called before configuration
        /// </summary>
        public virtual void PreConfigure()
        {
        }

        public virtual void AfterConfigure()
        {
            foreach (Project.Configuration conf in Configurations)
            {
                if (conf.IsFastBuild && SourceFilesFiltersRegex.Count > 0)
                {
                    if (conf.FastBuildBlobbed)
                    {
                        if (conf.FastBuildBlobbingStrategy != Configuration.InputFileStrategy.Include)
                            throw new Error("conf.FastBuildBlobbingStrategy must be set to Configuration.InputFileStrategy.Include when SourceFilesFiltersRegex is not empty. Config:" + conf);
                    }
                    else
                    {
                        if (conf.FastBuildNoBlobStrategy != Configuration.InputFileStrategy.Include)
                            throw new Error("conf.FastBuildNoBlobStrategy must be set to Configuration.InputFileStrategy.Include when SourceFilesFiltersRegex is not empty. Config:" + conf);
                    }
                }
            }
        }

        public virtual void PreResolveSourceFiles()
        {
        }

        private readonly ConcurrentBag<string> _alreadyReported = new ConcurrentBag<string>();

        private void ReportError(string message, bool onlyWarn = false)
        {
            if (_alreadyReported.Contains(message))
                return;

            _alreadyReported.Add(message);

            if (onlyWarn)
            {
                Builder.Instance.LogWarningLine(message);
            }
            else
            {
                Builder.Instance.LogErrorLine(message);
            }
        }

        public virtual void PostResolve()
        {
            // below checks are very very very costly, may take up to about 20 sec for huge codebase.
            if (Builder.Instance.Diagnostics && Util.CountFakeFiles() == 0)
            {
                // Cache the following File/Directory.Exists, avoiding redundant call for files in multiple configs
                var pathExistsCache = new Dictionary<string, bool>(256);
                bool PathExists(string path, Func<string, bool> checker)
                {
                    bool result;
                    if (!pathExistsCache.TryGetValue(path, out result))
                        pathExistsCache.Add(path, result = checker(path));
                    return result;
                }
                bool FileExists(string path) => PathExists(path, File.Exists);
                bool DirectoryExists(string path) => PathExists(path, Directory.Exists);

                foreach (Configuration conf in Configurations)
                {
                    var includePathsExcludeFromWarningRegex = RegexCache.GetCachedRegexes(IncludePathsExcludeFromWarningRegex).ToArray();
                    var libraryPathsExcludeFromWarningRegex = RegexCache.GetCachedRegexes(LibraryPathsExcludeFromWarningRegex).ToArray();

                    // check if the files marked with specific options still exist
                    foreach (var array in new Dictionary<Strings, string> {
                            {conf.ResolvedSourceFilesWithCompileAsCLROption,      nameof(conf.ResolvedSourceFilesWithCompileAsCLROption)},
                            {conf.ResolvedSourceFilesWithCompileAsCOption,        nameof(conf.ResolvedSourceFilesWithCompileAsCOption)},
                            {conf.ResolvedSourceFilesWithCompileAsCPPOption,      nameof(conf.ResolvedSourceFilesWithCompileAsCPPOption)},
                            {conf.ResolvedSourceFilesWithCompileAsObjCOption,     nameof(conf.ResolvedSourceFilesWithCompileAsObjCOption)},
                            {conf.ResolvedSourceFilesWithCompileAsObjCPPOption,   nameof(conf.ResolvedSourceFilesWithCompileAsObjCPPOption)},
                            {conf.ResolvedSourceFilesWithCompileAsNonCLROption,   nameof(conf.ResolvedSourceFilesWithCompileAsNonCLROption)},
                            {conf.ResolvedSourceFilesWithCompileAsWinRTOption,    nameof(conf.ResolvedSourceFilesWithCompileAsWinRTOption)},
                        })
                    {
                        foreach (string file in array.Key)
                        {
                            if (!FileExists(file))
                            {
                                ReportError($@"{conf.Project.SharpmakeCsFileName}: Error: File contained in {array.Value} doesn't exist: {file}.");
                            }
                        }
                    }

                    // check if the files marked as excluded from build still exist
                    foreach (var array in new Dictionary<Strings, string> {
                            {conf.ResolvedSourceFilesBuildExclude,                nameof(conf.ResolvedSourceFilesBuildExclude)},
                            {conf.ResolvedSourceFilesBlobExclude,                 nameof(conf.ResolvedSourceFilesBlobExclude)},
                            {conf.ResolvedSourceFilesWithExcludeAsWinRTOption,    nameof(conf.ResolvedSourceFilesWithExcludeAsWinRTOption)},
                            {conf.PrecompSourceExclude,                           nameof(conf.PrecompSourceExclude)}
                        })
                    {
                        foreach (string file in array.Key)
                        {
                            if (!FileExists(file))
                            {
                                ReportError($@"{conf.Project.SharpmakeCsFileName}: Warning: File contained in {array.Value} doesn't exist: {file}.", onlyWarn: true);
                            }
                        }
                    }


                    // check if the inclusion paths exist
                    foreach (var includeArray in new[] { conf.IncludePaths, conf.IncludePrivatePaths })
                    {
                        foreach (string folder in includeArray)
                        {
                            if (!folder.StartsWith("$", StringComparison.Ordinal) && !includePathsExcludeFromWarningRegex.Any(regex => regex.Match(folder).Success) && !DirectoryExists(folder))
                            {
                                ReportError($@"{conf.Project.SharpmakeCsFileName}: Warning: Folder contained in include paths doesn't exist: {folder}.", true);
                            }
                        }
                    }

                    // check if the library paths exist, and if the libs can be found in them
                    var allLibraryFiles = new OrderableStrings(conf.LibraryFiles);
                    var allLibraryPaths = new OrderableStrings(conf.LibraryPaths);

                    var configTasks = PlatformRegistry.Get<Configuration.IConfigurationTasks>(conf.Platform);
                    var platformLibraryPaths = configTasks.GetPlatformLibraryPaths(conf);
                    allLibraryPaths.AddRange(platformLibraryPaths);

                    // remove all the rooted files
                    var allRootedFiles = allLibraryFiles.Where(file => Path.IsPathRooted(file)).ToArray();
                    allLibraryFiles.RemoveRange(allRootedFiles);

                    string platformLibPrefix = configTasks.GetOutputFileNamePrefix(Configuration.OutputType.Lib);
                    string platformLibExtension = configTasks.GetDefaultOutputFullExtension(Configuration.OutputType.Lib);
                    foreach (string folder in allLibraryPaths)
                    {
                        if (!folder.StartsWith("$", StringComparison.Ordinal) && !libraryPathsExcludeFromWarningRegex.Any(regex => regex.Match(folder).Success) && !DirectoryExists(folder))
                        {
                            ReportError($@"{conf.Project.SharpmakeCsFileName}: Warning: Folder contained in conf.LibraryPaths doesn't exist. Folder: {folder}.", true);
                        }
                        else
                        {
                            // now check every library files, and remove them from the total array if it exists on disk
                            var toRemove = new List<string>();
                            foreach (string file in allLibraryFiles)
                            {
                                string path = Path.Combine(folder, file);
                                if (FileExists(path) || FileExists(path + platformLibExtension) || FileExists(Path.Combine(folder, platformLibPrefix + file + platformLibExtension)))
                                    toRemove.Add(file);
                            }

                            allLibraryFiles.RemoveRange(toRemove);
                        }
                    }

                    // Add all rooted files that are missing
                    allLibraryFiles.Add(allRootedFiles.Where(file => !FileExists(file)).ToArray());

                    // everything that remains is a missing library file
                    foreach (string file in allLibraryFiles)
                    {
                        ReportError($@"{conf.Project.SharpmakeCsFileName}: Warning: File contained in conf.LibraryFiles doesn't exist. File: {file}. Conf: {conf}", true);
                    }
                }
            }
        }

        internal virtual void Resolve(Builder builder, bool skipInvalidPath)
        {
            if (Resolved)
                return;

            using (builder.CreateProfilingScope("ResolveProject"))
            {
                // valid work blob parameters, set BlobWorkEnabled to false if 0 work blob specified
                if (BlobWorkEnabled && BlobWorkFileCount == 0)
                    BlobWorkEnabled = false;

                Resolver resolver = new Resolver();
                resolver.SetParameter("project", this);

                if (skipInvalidPath)
                    resolver.Resolve(this, fallbackValue: false);
                else
                    resolver.Resolve(this);

                // Resolve full paths
                _rootPath = Util.SimplifyPath(RootPath);
                Util.ResolvePath(SharpmakeCsPath, ref _sourceRootPath);
                Util.ResolvePath(SharpmakeCsPath, ref AdditionalSourceRootPaths);
                Util.ResolvePath(SourceRootPath, ref SourceFiles);
                Util.ResolvePath(SourceRootPath, ref SourceFilesExclude);
                Util.ResolvePath(SourceRootPath, ref SourceFilesBlobExclude);
                Util.ResolvePath(SourceRootPath, ref SourceFilesBuildExclude);
                Util.ResolvePath(SharpmakeCsPath, ref _blobPath);

                if (SourceFilesFilters != null)
                    Util.ResolvePath(SharpmakeCsPath, ref SourceFilesFilters);

                XResourcesImg.Resolve(SourceRootPath, resolver);

                using (builder.CreateProfilingScope("ResolveConfs (" + Configurations.Count + ")"))
                {
                    // Resolve Configuration
                    foreach (Project.Configuration conf in Configurations)
                        conf.Resolve(resolver);
                }

                if (SharpmakeProjectType == ProjectTypeAttribute.Generate)
                {
                    using (builder.CreateProfilingScope("PreResolveSourceFiles"))
                        PreResolveSourceFiles();
                    using (builder.CreateProfilingScope("ResolveSourceFiles"))
                        ResolveSourceFiles(builder);
                }
                using (builder.CreateProfilingScope("PostResolve"))
                    PostResolve();

                if (builder.DumpDependencyGraph && !IsFastBuildAll)
                {
                    foreach (Configuration conf in Configurations)
                        DependencyTracker.Instance.UpdateConfiguration(this, conf);
                }
                Resolved = true;
            }
        }

        /// <summary>
        /// Validate that a configuration output type is supported by the current project type.
        /// </summary>
        /// <param name="outputType">The configuration output type to validate.</param>
        /// <returns>Returns true if the current project type supports the specified output type, otherwise false.</returns>
        public virtual bool IsValidConfigurationOutputType(Project.Configuration.OutputType outputType)
        {
            return true;
        }

        public virtual void ResolveNonExistingSourcePath()
        {
            throw new Error("[project.SourceRootPath] must exist: {0}", SourceRootPath);
        }

        public virtual void PostLink()
        {
        }

        internal void Link(Builder builder)
        {
            if (_resolvedDependencies)
                return;

            if (!Resolved)
                throw new InternalError("try to apply dependencies but the project have not been resolved {0}", GetType().Name);

            foreach (Project.Configuration conf in Configurations)
            {
                conf.Link(builder);

                // Build ProjectFilesMapping
                string configurationFile = Path.Combine(conf.ProjectPath, conf.ProjectFileName);
                var fileConfigurationList = ProjectFilesMapping.GetValueOrAdd(configurationFile, new List<Configuration>());
                fileConfigurationList.Add(conf);
            }

            PostLink();

            _resolvedDependencies = true;
        }

        #endregion

        #region Private

        public static readonly string BlobExtension = ".blob.cpp";

        internal static List<string> GetDirectoryFiles(DirectoryInfo directoryInfo)
        {
            string directoryCapitalizedFullName = directoryInfo.FullName;
            directoryCapitalizedFullName = Util.GetCapitalizedPath(directoryCapitalizedFullName);
            List<string> files;
            if (!s_cachedDirectoryFiles.TryGetValue(directoryCapitalizedFullName, out files))
            {
                files = new List<string>();

                string[] filesList = Util.DirectoryGetFiles(directoryCapitalizedFullName, "*.*", SearchOption.AllDirectories);

                foreach (string file in filesList)
                {
                    if (!file.EndsWith(BlobExtension, StringComparison.OrdinalIgnoreCase))
                        files.Add(file);

                    Util.RegisterCapitalizedPath(file);
                }
                s_cachedDirectoryFiles[directoryCapitalizedFullName] = files;
            }


            // remove all file container
            return files;
        }

        internal static List<string> GetDirectoryFiles(DirectoryInfo directoryInfo, Strings extensions)
        {
            return GetDirectoryFiles(directoryInfo).Where(file => extensions.Contains(Path.GetExtension(file))).ToList();
        }


        private bool _resolvedDependencies = false;
        private static ConcurrentDictionary<string, List<string>> s_cachedDirectoryFiles = new ConcurrentDictionary<string, List<string>>();

        public static string GetCapitalizedFile(string file)
        {
            return Util.GetCapitalizedPath(file);
        }

        #endregion
    }

    [Sharpmake.Generate]
    internal class FastBuildAllProject : Project
    {
        public FastBuildAllProject(Type targetType)
            : base(targetType, typeof(Project.Configuration), true)
        {
            // Disable automatic source files discovery
            SourceFilesExtensions.Clear();
            ResourceFilesExtensions.Clear();
            PRIFilesExtensions.Clear();
        }
    }

    public class WebReferenceUrl
    {
        public string Name;
        public string UrlBehavior;
        public string RelPath;
        public string UpdateFromURL;
        public string ServiceLocationURL;
        public string CachedDynamicPropName;
        public string CachedAppSettingsObjectName;
        public string CachedSettingsPropName;
    }

    public class ComReference
    {
        public string Name;
        public Guid Guid;
        public int VersionMajor;
        public int VersionMinor;
        public int Lcid;
        public WrapperToolEnum WrapperTool = WrapperToolEnum.tlbimp;
        public bool? Private;
        public bool? EmbedInteropTypes;

        public enum WrapperToolEnum
        {
            tlbimp,
            primary,
            aximp
        }
    }

    public class ImportProject
    {
        public string Project;
        public string Condition;
        public override int GetHashCode()
        {
            return (Project + Condition).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ImportProject other = (ImportProject)obj;
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(Project, other.Project) && string.Equals(Condition, other.Condition);
        }
    }

    public class BootstrapperPackage
    {
        public string Include;
        public bool Visible;
        public string ProductName;
        public bool Install;
    }

    public class FileAssociationItem
    {
        public string Include;
        public bool Visible;
        public string Description;
        public string Progid;
        public string DefaultIcon;
    }

    public enum PublishState
    {
        Auto,
        DataFile,
        Exclude,
        Include,
        Prerequisite
    }

    public enum FileType
    {
        Assembly,
        File
    }

    public class PublishFile
    {
        public string Include;
        public bool Visible;
        public string Group = string.Empty;
        public PublishState PublishState = PublishState.Include;
        public bool IncludeHash = true;
        public FileType FileType = FileType.File;
    }

    [Resolver.Resolvable]
    public class GlobSetting
    {
        public string Include;
        public string Exclude;
    }

    public enum CSharpProjectType
    {
        Test,
        Vsix,
        Vsto,
        Wpf,
        Wcf,
        AspNetMvc5,
        Default
    }

    public enum CSharpProjectSchema
    {
        NetCore,
        NetFramework,
        Default
    }

    public enum NetCoreSdkTypes
    {
        Default,
        Web,
        Razor,
        Worker,
        WindowsDesktop
    }

    public class CSharpVstoProject : CSharpProject
    {
        public enum OfficeApplication
        {
            Outlook,
            Word,
            Excel,
            PowerPoint,
        }

        public OfficeApplication Application { get; }
        public string OfficeSdkVersion { get; }

        public CSharpVstoProject(OfficeApplication application, string officeSdkVersion)
        {
            Application = application;
            OfficeSdkVersion = officeSdkVersion;
            ProjectTypeGuids = CSharpProjectType.Vsto;
        }
    }

    public interface IAspNetProject
    {
        bool? MvcBuildViews { get; set; }
        bool? UseIISExpress { get; set; }
        int? IISExpressSSLPort { get; set; }
        bool? IISExpressAnonymousAuthentication { get; set; }
        bool? IISExpressWindowsAuthentication { get; set; }
        bool? IISExpressUseClassicPipelineMode { get; set; }
        bool? UseGlobalApplicationHostFile { get; set; }
        bool? UseIIS { get; set; }
        bool? AutoAssignPort { get; set; }
        int? DevelopmentServerPort { get; set; }
        string DevelopmentServerVPath { get; set; }
        string IISUrl { get; set; }
        bool? NTLMAuthentication { get; set; }
        bool? UseCustomServer { get; set; }
        bool? SaveServerSettingsInUserFile { get; set; }
    }

    public static class CSharpProjectExtensions
    {
        public static void InitAspNetProject(this CSharpProject aspNetProject)
        {
            if (!(aspNetProject is IAspNetProject))
                throw new Error($"project {aspNetProject.Name} does not implement IAspNetProject");

            aspNetProject.ProjectTypeGuids = CSharpProjectType.AspNetMvc5;
            aspNetProject.SourceFilesExtensions.Add(".asax");

            string[] contentExtension = {
                ".cshtml", ".js", ".map",
                ".css", ".scss",
                ".eot", ".svg", ".ttf", ".woff", ".woff2",
                ".ico", ".png", ".jpg", ".gif", ".config"
            };

            aspNetProject.ContentExtension.Add(contentExtension);

            aspNetProject.ResourceFilesExtensions.Remove(contentExtension);
            aspNetProject.EmbeddedResourceExtensions.Remove(contentExtension);

            aspNetProject.NoneExtensions.Add(".pubxml");

            aspNetProject.CustomTargets.Add(new CSharpProject.CustomTargetElement()
            {
                Name = "MvcBuildViews",
                TargetParameters = @"AfterTargets=""AfterBuild"" Condition=""'$(MvcBuildViews)' == 'true'""",
                CustomTasks = @"<AspNetCompiler VirtualPath=""temp"" PhysicalPath=""$(WebProjectOutputDir)"" />"
            });

            aspNetProject.DependenciesCopyLocal = Project.DependenciesCopyLocalTypes.Default;
        }

        public static void AddAspNetReferences(CSharpProject.Configuration conf)
        {
            conf.ReferencesByName.Add("Microsoft.CSharp");
            conf.ReferencesByName.Add("System");
            conf.ReferencesByName.Add("System.ComponentModel.DataAnnotations");
            conf.ReferencesByName.Add("System.Configuration");
            conf.ReferencesByName.Add("System.Core");
            conf.ReferencesByName.Add("System.Data");
            conf.ReferencesByName.Add("System.Data.DataSetExtensions");
            conf.ReferencesByName.Add("System.Drawing");
            conf.ReferencesByName.Add("System.EnterpriseServices");
            conf.ReferencesByName.Add("System.ServiceProcess");
            conf.ReferencesByName.Add("System.Web");
            conf.ReferencesByName.Add("System.Web.Abstractions");
            conf.ReferencesByName.Add("System.Web.ApplicationServices");
            conf.ReferencesByName.Add("System.Web.DynamicData");
            conf.ReferencesByName.Add("System.Web.Entity");
            conf.ReferencesByName.Add("System.Web.Extensions");
            conf.ReferencesByName.Add("System.Net.Http");
            conf.ReferencesByName.Add("System.Net.Http.WebRequest");
            conf.ReferencesByName.Add("System.Web.Routing");
            conf.ReferencesByName.Add("System.Web.Services");
            conf.ReferencesByName.Add("System.Xml");
            conf.ReferencesByName.Add("System.Xml.Linq");
        }
    }

    public class AspNetProject : CSharpProject, IAspNetProject
    {
        public AspNetProject()
            : this(typeof(Target))
        { }

        public AspNetProject(Type targetType)
            : base(targetType)
        {
            this.InitAspNetProject();
        }

        public void AddDefaultReferences(Configuration conf)
        {
            CSharpProjectExtensions.AddAspNetReferences(conf);
        }

        public bool? MvcBuildViews { get; set; }

        public bool? UseIISExpress { get; set; }

        public int? IISExpressSSLPort { get; set; }

        public bool? IISExpressAnonymousAuthentication { get; set; }

        public bool? IISExpressWindowsAuthentication { get; set; }

        public bool? IISExpressUseClassicPipelineMode { get; set; }

        public bool? UseGlobalApplicationHostFile { get; set; }

        public bool? UseIIS { get; set; }

        public bool? AutoAssignPort { get; set; }

        public int? DevelopmentServerPort { get; set; }

        public string DevelopmentServerVPath { get; set; }

        public string IISUrl { get; set; }

        public bool? NTLMAuthentication { get; set; }

        public bool? UseCustomServer { get; set; }

        public bool? SaveServerSettingsInUserFile { get; set; }
    }

    public class CSharpProject : Project
    {
        public Strings ContentExtension = new Strings();
        public Strings VsctExtension = new Strings(".vsct");
        public CSharpProjectType ProjectTypeGuids = CSharpProjectType.Default;
        public CSharpProjectSchema ProjectSchema = CSharpProjectSchema.Default;
        public NetCoreSdkTypes NetCoreSdkType = NetCoreSdkTypes.Default;
        public string ResourcesPath = null;
        public string ContentPath = null;
        public string BaseIntermediateOutputPath = string.Empty;
        public string ApplicationIcon = string.Empty;
        public string ApplicationManifest = "app.manifest";
        public string ApplicationSplashScreen = string.Empty;
        public string StartupObject = string.Empty;
        public bool NoWin32Manifest = false;
        public bool UseMSBuild14IfAvailable = false;
        public Strings PublicResourceFiles = new Strings();
        // If true, recreate the relative folder hierarchy for content files instead of grouping them up.
        public bool PreserveLinkFolderPaths = false;
        public Strings ApplicationDefinitionFilenames = new Strings();
        public Strings ResolvedResourcesFullFileNames = new Strings();
        public Strings ResolvedContentFullFileNames = new Strings();
        public Strings AdditionalEmbeddedResource = new Strings();
        public Strings AdditionalEmbeddedResourceAlwaysCopy = new Strings();
        public Strings AdditionalEmbeddedResourceCopyIfNewer = new Strings();
        public Strings AdditionalEmbeddedAssemblies = new Strings();
        public Strings AdditionalNone = new Strings();
        public Strings SourceNoneFilesExcludeRegex = new Strings();
        public Strings AdditionalContent = new Strings();
        public Strings AdditionalContentAlwaysCopy = new Strings();
        public Strings AdditionalContentCopyIfNewer = new Strings();
        public Strings AdditionalContentAlwaysIncludeInVsix = new Strings();
        public int VSIXProjectVersion = -1; // -1 : Omit from csproj. Version 3 is needed for VS2017. See https://github.com/Microsoft/visualstudio-docs/blob/master/docs/extensibility/faq-2017.md#can-i-build-a-vsix-v3-with-visual-studio-2015
        public Strings AdditionalNoneAlwaysCopy = new Strings();
        public Strings AdditionalNoneCopyIfNewer = new Strings();
        public Strings AdditionalRuntimeTemplates = new Strings();
        public Strings VsctCompileFiles = new Strings();
        public Strings VsdConfigXmlFiles = new Strings();
        public Strings VSIXSourceItems = new Strings();
        public Strings WebReferences = new Strings();
        public Strings Services = new Strings();
        public Strings AnalyzerDllFilePaths = new Strings();
        public Strings AdditionalFolders = new Strings();
        public List<BootstrapperPackage> BootstrapperPackages = new List<BootstrapperPackage>();
        public List<FileAssociationItem> FileAssociationItems = new List<FileAssociationItem>();
        public List<PublishFile> PublishFiles = new List<PublishFile>();
        public List<GlobSetting> Globs = new List<GlobSetting>();

        /// <summary>
        /// If set to true. Will explicit the RestoreProjectStyle in the project file
        /// </summary>
        public bool ExplicitNugetRestoreProjectStyle = false;

        /// <summary>
        /// Enable or disable the property [EnableDefaultItems] in NetCore Project Schema
        /// </summary>
        public bool EnableDefaultItems { get; set; } = false;
        public Strings DefaultItemExcludes = new Strings();

        public bool IncludeResxAsResources = true;
        public string RootNamespace;
        public Platform? DefaultPlatform;
        public Strings EmbeddedResourceExtensions; // this is used mainly for WinForms, for WPF applications use Resources for embedded and Content for linked
        public List<WebReferenceUrl> WebReferenceUrls = new List<WebReferenceUrl>();
        public List<ComReference> ComReferences = new List<ComReference>();
        public List<ImportProject> PreImportProjects = new List<ImportProject>();
        public List<ImportProject> ImportProjects = new List<ImportProject>();
        public List<CustomTargetElement> CustomTargets = new List<CustomTargetElement>();
        public List<UsingTask> UsingTasks = new List<UsingTask>();

        public bool? WcfAutoStart; // Wcf Auto-Start service when debugging
        public string WcfBaseStorage = "Service References" + Path.DirectorySeparatorChar;

        // writes Pre/Post BuildEvents per configuration instead of one for all, this will make editing events in Visual Studio impossible
        public bool ConfigurationSpecificEvents = false;

        public GeneratedAssemblyConfig GeneratedAssemblyConfig = new GeneratedAssemblyConfig();

        public Options.CSharp.RunPostBuildEvent RunPostBuildEvent = Options.CSharp.RunPostBuildEvent.OnBuildSuccess;

        public string CodeAnalysisRuleSetFileName;

        public const string DefaultImportProject = @"$(MSBuildBinPath)\Microsoft.CSharp.targets";

        /// <summary>
        /// Generate a file containing API documentation.
        /// </summary>
        public bool GenerateDocumentationFile = false;

        [Resolver.Resolvable]
        public class CustomTargetElement
        {
            public string Name;
            public string TargetParameters;
            public string CustomTasks;

            public CustomTargetElement()
            { }

            public CustomTargetElement(string name, string targetParameters, string customTasks)
            {
                Name = name;
                TargetParameters = targetParameters;
                CustomTasks = customTasks;
            }
        }

        [Resolver.Resolvable]
        public class UsingTask
        {
            public string AssemblyFile;
            public string TaskName;

            public UsingTask() { }

            public UsingTask(string assemblyFile, string taskName)
            {
                AssemblyFile = assemblyFile;
                TaskName = taskName;
            }
        }

        private void InitCSharpSpecifics()
        {
            SourceFilesExtensions = new Strings(".cs", ".xaml", ".sharpmake", ".edmx");
            EmbeddedResourceExtensions = new Strings(".resx", ".licx", ".lic", ".cur", ".template"); // this is used mainly for WinForms, for WPF applications use Resources for embedded and Content for linked
            ResourceFilesExtensions = new Strings(EmbeddedResourceExtensions.Union(ResourceFilesExtensions)); //assures that embeddedResources will be contained in the Resources
            RootNamespace = "[project.Name]";
            AssemblyName = "[project.Name]";
            IsFileNameToLower = false;
            IsTargetFileNameToLower = false;
            ResourcesPath = @"[project.RootPath]\Resources\";
            ContentPath = @"[project.RootPath]\Content\";
            ImportProjects.Add(new ImportProject { Project = DefaultImportProject });
            ApplicationDefinitionFilenames.Add("App.xaml", "MainApplication.xaml");

            //Default Excludes
            SourceFilesExcludeRegex.Add(@"\b(bin|obj)\\");

            NoneExtensions.Add(
                ".config",
                ".settings",
                ".map",
                ".wsdl",
                ".datasource",
                ".cd",
                ".doc",
                ".docx",
                ".xsd",
                ".xss",
                ".xsc",
                ".txt",
                ".bat",
                ".xml",
                ".tt",
                ".svcmap",
                ".svcinfo",
                ".disco",
                ".manifest"
            );

            ProtoExtensions.Add(".proto");
        }

        public CSharpProject()
            : this(typeof(Target))
        { }
        public CSharpProject(Type targetType)
            : this(targetType, typeof(Project.Configuration))
        { }
        public CSharpProject(Type targetType, Type configurationType)
            : this(targetType, configurationType, isInternal: false)
        { }
        internal CSharpProject(Type targetType, Type configurationType, bool isInternal)
            : base(targetType, configurationType, isInternal)
        {
            InitCSharpSpecifics();
        }

        internal override void Resolve(Builder builder, bool skipInvalidPath)
        {
            base.Resolve(builder, skipInvalidPath);

            if (!string.IsNullOrEmpty(ApplicationIcon))
                Util.ResolvePath(SourceRootPath, ref ApplicationIcon);
            if (!string.IsNullOrEmpty(ApplicationManifest))
                Util.ResolvePath(SourceRootPath, ref ApplicationManifest);
            if (!string.IsNullOrEmpty(ApplicationSplashScreen))
                Util.ResolvePath(SourceRootPath, ref ApplicationSplashScreen);
        }

        public void AddCSharpSpecificImportProjects(List<ImportProject> importProjects, DevEnv devEnv)
        {
            if (ProjectTypeGuids == CSharpProjectType.Vsix)
            {
                importProjects.Add(new ImportProject { Project = @"$(VSToolsPath)\VSSDK\Microsoft.VsSDK.targets", Condition = @"'$(VSToolsPath)' != ''" });
            }

            if (ProjectTypeGuids == CSharpProjectType.AspNetMvc5)
            {
                importProjects.Add(new ImportProject { Project = @"$(VSToolsPath)\WebApplications\Microsoft.WebApplication.targets", Condition = "'$(VSToolsPath)' != ''" });
                importProjects.Add(new ImportProject { Project = @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\WebApplications\Microsoft.WebApplication.targets", Condition = "false" });
            }
        }


        public override bool IsValidConfigurationOutputType(Configuration.OutputType outputType)
        {
            return (outputType == Configuration.OutputType.DotNetClassLibrary
                    || outputType == Configuration.OutputType.DotNetConsoleApp
                    || outputType == Configuration.OutputType.DotNetWindowsApp
                    || outputType == Configuration.OutputType.None);
        }

        internal override void ResolveSourceFiles(Builder builder)
        {
            ResourceFiles.AddRange(PublicResourceFiles);

            base.ResolveSourceFiles(builder);

            //Getting CorrectCaseVersion
            if (!string.IsNullOrEmpty(ResourcesPath) && Directory.Exists(ResourcesPath))
            {
                ResolvedResourcesFullFileNames = new Strings(GetDirectoryFiles(new DirectoryInfo(ResourcesPath)).Select(GetCapitalizedFile));
            }

            if (!string.IsNullOrEmpty(ContentPath) && Directory.Exists(ContentPath))
            {
                ResolvedContentFullFileNames = new Strings(GetDirectoryFiles(new DirectoryInfo(ContentPath)).Select(GetCapitalizedFile));
            }

            if (SourceFilesExcludeRegex.Count != 0 || ContentExtension.Count != 0 || VsctExtension.Count != 0)
            {
                var sourceFilesExcludeRegex = RegexCache.GetCachedRegexes(SourceFilesExcludeRegex);
                var sourceFiles = new Strings(GetDirectoryFiles(new DirectoryInfo(SourceRootPath)).Select(GetCapitalizedFile));

                sourceFiles = FilterSourceFiles(sourceFiles);

                AddMatchExtensionFiles(sourceFiles, ResolvedContentFullFileNames, ContentExtension);
                AddMatchExtensionFiles(sourceFiles, VsctCompileFiles, VsctExtension);
                if (AddMatchFiles(RootPath, Util.PathGetRelative(RootPath, VsctCompileFiles), VsctCompileFiles, SourceFilesExclude, sourceFilesExcludeRegex))
                    Debugger.Break();
            }

            if (ResolvedResourcesFullFileNames.Count == 0 && ResolvedContentFullFileNames.Count == 0)
                return;

            foreach (string excludeSourceFile in SourceFilesExclude)
            {
                ResolvedResourcesFullFileNames.Remove(excludeSourceFile);
                ResolvedContentFullFileNames.Remove(excludeSourceFile);
                VsctCompileFiles.Remove(excludeSourceFile);
            }
        }

        private Strings FilterSourceFiles(Strings sourceFiles)
        {
            var sourceFilesExcludeRegex = RegexCache.GetCachedRegexes(SourceFilesExcludeRegex);
            var filterSourceFiles = new Strings();
            foreach (string sourceFile in sourceFiles)
            {
                bool exclude = false;
                foreach (CachedRegex reg in sourceFilesExcludeRegex)
                {
                    if (reg.Match(sourceFile).Success)
                    {
                        exclude = true;
                        break;
                    }
                }
                if (!exclude)
                    filterSourceFiles.Add(sourceFile);
            }
            sourceFiles = filterSourceFiles;
            return sourceFiles;
        }

        protected override void ExcludeOutputFiles()
        {
            // Don't put back this, this is pure evil
            /*foreach (var conf in Configurations)
            {
                SourceFilesExcludeRegex.Add(conf.TargetPath.Replace("\\", "\\\\"));
                SourceFilesExcludeRegex.Add(conf.IntermediatePath.Replace("\\", "\\\\"));
            }*/
        }

        private List<string> _filteredEmbeddedAssemblies = null;
        public virtual string GetLinkFolder(string file)
        {
            // file is under the influence of the project and is not a link
            if (Util.PathIsUnderRoot(RootPath, file))
                return null;

            if (PreserveLinkFolderPaths)
            {
                string relativePath = Util.PathGetRelative(SourceRootPath, Path.GetDirectoryName(file));

                // Remove the root, if it exists.
                // This will only happen if file is rooted *and* doesn't share the same root as SourceRootPath.
                if (Path.IsPathRooted(relativePath))
                {
                    relativePath = relativePath.Substring(Path.GetPathRoot(relativePath).Length);
                }

                // If the relative path is elsewhere, we leave the file in the root.
                if (relativePath.Contains(".."))
                {
                    return string.Empty;
                }

                return relativePath;
            }

            if (_filteredEmbeddedAssemblies == null)
            {
                _filteredEmbeddedAssemblies = new List<string>();

                foreach (var assemblyPath in AdditionalEmbeddedAssemblies)
                {
                    string simplifiedPath = Util.SimplifyPath(assemblyPath).ToLower();
                    _filteredEmbeddedAssemblies.Add(simplifiedPath);
                }
            }

            // .dll and .exe are put in the root of the output folder.
            string extension = Path.GetExtension(file);
            if (string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".snk", StringComparison.OrdinalIgnoreCase))
            {
                var config = Configurations.First();
                string absPath = Util.PathGetAbsolute(config.ProjectPath, file);

                if (!_filteredEmbeddedAssemblies.Contains(absPath))
                    return string.Empty;
            }

            return "Resources";
        }
    }

    public class PythonVirtualEnvironment
    {
        public string Name;
        public string Path;
        public Guid Guid;
        public bool IsDefault;
        public Guid BaseInterpreterGuid;
        public string Version;

        public PythonVirtualEnvironment(string name, string path, bool isDefault)
            : this(name, path, string.Empty, isDefault, default(Guid))
        { }

        public PythonVirtualEnvironment(string name, string path, string version, bool isDefault)
            : this(name, path, version, isDefault, default(Guid))
        { }

        public PythonVirtualEnvironment(string name, string path, bool isDefault, Guid baseInterpreterGuid)
            : this(name, path, string.Empty, isDefault, baseInterpreterGuid)
        { }

        public PythonVirtualEnvironment(string name, string path, string version, bool isDefault, Guid baseInterpreterGuid)
        {
            Name = name;
            Path = path;
            Version = version;
            Guid = Util.BuildGuid(path);
            IsDefault = isDefault;
            BaseInterpreterGuid = baseInterpreterGuid;
        }
    }

    public class PythonEnvironment
    {
        public Guid Guid;
        public bool IsActivated;

        public PythonEnvironment(Guid guid, bool isActivated = false)
        {
            Guid = guid;
            IsActivated = isActivated;
        }
    }

    public class PythonProject : Project
    {
        public List<PythonEnvironment> Environments = new List<PythonEnvironment>();
        public List<PythonVirtualEnvironment> VirtualEnvironments = new List<PythonVirtualEnvironment>();
        public Strings SearchPaths = new Strings();
        public string StartupFile = string.Empty;
        public bool IsSourceFilesCaseSensitive = true;

        private void InitPythonSpecifics()
        {
            SourceFilesExtensions = new Strings(".py", ".yml", ".html", ".js", ".css", ".csv", ".xml", ".json");
        }

        public PythonProject()
            : this(typeof(Target))
        {
            InitPythonSpecifics();
        }
        public PythonProject(Type targetType)
            : base(targetType)
        {
            InitPythonSpecifics();
        }
    }

    /// <summary>
    /// A package project for deployment on Android device.
    /// </summary>
    public class AndroidPackageProject : Project
    {
        private const string RemoveLineTag = "REMOVE_LINE_TAG";

        public string AndroidManifest { get; set; } = "AndroidManifest.xml";

        public string AntBuildRootDirectory { get; set; } = @"$(OutDir)Package\";

        public string AntBuildXml { get; set; } = "build.xml";

        public string AntProjectPropertiesFile { get; set; } = "project.properties";

        public Strings GradleTemplateFiles = new Strings();

        public string GradlePlugin { get; set; } = RemoveLineTag;

        public string GradleVersion { get; set; } = RemoveLineTag;

        /// <summary>
        /// The project type to lookup in the dependencies of the package to be used as the application library.
        /// This library is the first to be loaded when the package is started.
        /// </summary>
        /// <remarks>
        /// It is an error if the specified type can't be found in the configuration dependencies.
        /// </remarks>
        public Type AppLibType { get; set; }

        public AndroidPackageProject() : this(typeof(Target))
        {
        }

        public AndroidPackageProject(Type targetType) : base(targetType)
        {
        }
    }
}
