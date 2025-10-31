// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.FastBuild
{
    public partial class Bff
    {
        public static class Template
        {
            public static class ConfigurationFile
            {
                public static string HeaderFile = @"
//=================================================================================================================
// [fastBuildProjectName] FASTBuild config file
//=================================================================================================================
#once

";

                public static string Define = @"#define [fastBuildDefine]
";


                public static string CustomSectionHeader = @"
//=================================================================================================================
// FASTBuild custom section
//=================================================================================================================
";


                public static string Includes = @"
//=================================================================================================================
// [fastBuildProjectName] .bff includes
//=================================================================================================================
[fastBuildOrderedBffDependencies]
";

                public static string GlobalConfigurationInclude = @"
//=================================================================================================================
// Global Configuration include
//=================================================================================================================
[fastBuildGlobalConfigurationInclude]
";

                public static string GlobalSettings = @"
//=================================================================================================================
// Global Settings
//=================================================================================================================
Settings
{
[fastBuildEnvironments]
    [CachePluginDLL]
    [CachePath]
    [WorkerConnectionLimit]
    .AllowDBMigration_Experimental = [fastBuildAllowDBMigration]
    .ConcurrencyGroups = [fastbuildConcurrencyGroupList]
[AdditionalGlobalSettings]
}
";

                public static string ConcurrencyGroup =
@"[fastBuildConcurrencyGroupSectionName] =
[
    .ConcurrencyGroupName = '[fastBuildConcurrencyGroupName]'
    .ConcurrencyLimit = [fastBuildConcurrencyLimit]
    .ConcurrencyPerJobMiB = [fastBuildConcurrencyPerJobMiB]
]
";

                public const string WinEnvironment =
@"    #import TMP
    #import TEMP
    #import USERPROFILE
    #import COMSPEC
    .Environment =
    {
        ""TMP=$TMP$"",
        ""TEMP=$TEMP$"",
        ""USERPROFILE=$USERPROFILE$"",
        ""COMSPEC=$COMSPEC$"",
        ""SystemRoot=[fastBuildSystemRoot]""
        ""PATH=[fastBuildPATH]""
[envAdditionalVariables]
    }
";

                public const string OsxEnvironment =
@"    #import TMPDIR
    .Environment =
    {
        ""TMPDIR=$TMPDIR$"",
        ""PATH=[fastBuildPATH]""
[envAdditionalVariables]
    }
";

                public const string LinuxEnvironment =
@"    .Environment =
    {
        ""PATH=[fastBuildPATH]""
[envAdditionalVariables]
    }
";

                public static string MasmConfigNameSuffix = "Masm";
                public static string NasmConfigNameSuffix = "Nasm";
                public static string Win64ConfigName = ".win64Config";

                public static string CompilerSetting = @"
//=================================================================================================================
Compiler( '[fastbuildCompilerName]' )
{
    .ExecutableRootPath     = '[fastBuildCompilerRootPath]'
    .Executable             = '[fastBuildCompilerExecutable]'
    .ExtraFiles             = [fastBuildExtraFiles]
    .CompilerFamily         = '[fastBuildCompilerFamily]'
    .UseRelativePaths_Experimental = [fastBuildCompilerUseRelativePaths]
[fastBuildCompilerAdditionalSettings]
}
";

                internal static string ResourceCompilerSettings = @"
Compiler( '[fastBuildResourceCompilerName]' )
{
    .Executable             = '[fastBuildResourceCompiler]'
    .CompilerFamily         = 'custom'
}
";

                internal static string MasmCompilerSettings = @"
Compiler( '[fastBuildMasmCompilerName]' )
{
    .Executable             = '[fastBuildMasmCompiler]'
    .CompilerFamily         = 'custom'
}
";

                // TODOANT
                internal static string NasmCompilerSettings = @"
Compiler( '[fastBuildNasmCompilerName]' )
{
    .Executable             = '[fastBuildNasmCompiler]'
    .CompilerFamily         = 'custom'
}
";

                public static string CompilerConfiguration = @"
[fastBuildConfigurationName] =
[
    Using( [fastBuildUsing] )
    .BinPath                = '[fastBuildBinPath]'
    .LinkerPath             = '[fastBuildLinkerPath]'
    .ResourceCompiler       = '[fastBuildResourceCompilerName]'
    .Compiler               = '[fastBuildCompilerName]'
    .Librarian              = '[fastBuildLibrarian]'
    .Linker                 = '[fastBuildLinker]'
    .PlatformLibPaths       = '[fastBuildPlatformLibPaths]'
    .Executable             = '[fastBuildExecutable]'
    .LinkerType             = '[fastBuildLinkerType]'
]
";

                public static string LinkerOptions = @"
    .LinkerOptions          = '/OUT:""%2""[dllOption]'
                            // Input files
                            // ---------------------------
                            + ' ""%1""'
                            // General
                            // ---------------------------
                            + ' [cmdLineOptions.ShowProgress]'
                            + ' [cmdLineOptions.LinkIncremental]'
                            + ' [cmdLineOptions.LinkerSuppressStartupBanner]'
                            + ' [cmdLineOptions.AdditionalLibraryDirectories]'
                            + ' [cmdLineOptions.ForceFileOutput]'
                            + ' [cmdLineOptions.TreatLinkerWarningAsErrors]'
                            // Input
                            // ---------------------------
                            + ' [cmdLineOptions.AdditionalDependencies]'
                            + ' [cmdLineOptions.IgnoreAllDefaultLibraries]'
                            + ' [cmdLineOptions.IgnoreDefaultLibraryNames]'
                            + ' [cmdLineOptions.DelayLoadedDLLs]'
                            + ' [cmdLineOptions.EmbedResources]'
                            // Manifest
                            // ---------------------------
                            + ' [cmdLineOptions.GenerateManifest]'
                            + ' [cmdLineOptions.ManifestInputs]'
                            + ' [cmdLineOptions.ManifestFile]'
                            // Debugging
                            // ---------------------------
                            + ' [cmdLineOptions.LinkerGenerateDebugInformation]'
                            + ' [cmdLineOptions.LinkerNatvisFiles]'
                            + ' [cmdLineOptions.LinkerProgramDatabaseFile]'
                            + ' [cmdLineOptions.GenerateMapFile]'
                            + ' [cmdLineOptions.MapExports]'
                            + ' [cmdLineOptions.AssemblyDebug]'
                            // System
                            // ---------------------------
                            + ' [cmdLineOptions.SubSystem]'
                            + ' [cmdLineOptions.HeapReserveSize]'
                            + ' [cmdLineOptions.HeapCommitSize]'
                            + ' [cmdLineOptions.StackReserveSize]'
                            + ' [cmdLineOptions.StackCommitSize]'
                            + ' [cmdLineOptions.AllowIsolation]'
                            + ' [cmdLineOptions.LargeAddressAware]'
                            // Optimization
                            // ---------------------------
                            + ' [cmdLineOptions.OptimizeReference]'
                            + ' [cmdLineOptions.EnableCOMDATFolding]'
                            + ' [cmdLineOptions.FunctionOrder]'
                            + ' [cmdLineOptions.ProfileGuidedDatabase]'
                            + ' [cmdLineOptions.LinkTimeCodeGeneration]'
                            // Embedded IDL
                            // ---------------------------
                            + ' /TLBID:1'
                            // Windows Metadata
                            // ---------------------------
                            + ' [cmdLineOptions.GenerateWindowsMetadata]'
                            + ' [cmdLineOptions.WindowsMetadataFile]'
                            // Advanced
                            // ---------------------------
                            + ' [cmdLineOptions.BaseAddress]'
                            + ' [cmdLineOptions.RandomizedBaseAddress]'
                            + ' [cmdLineOptions.FixedBaseAddress]'
                            + ' [cmdLineOptions.ImportLibrary]'
                            + ' [cmdLineOptions.TargetMachine]'
                            + ' [cmdLineOptions.LinkerCreateHotPatchableImage]'
                            + ' /errorReport:queue'
                            + ' [cmdLineOptions.ModuleDefinitionFile]'
                            // Additional linker options
                            //--------------------------
                            + ' [options.AdditionalLinkerOptions]'
";

                public static string PCHOptions = @"
    // Precompiled Headers options
    // ---------------------------
    .PCHInputFile           = '[fastBuildPrecompiledSourceFile]'
    .PCHOutputFile          = '[cmdLineOptions.PrecompiledHeaderFile]'
    .PCHOptions             = '""%1"" /Fp""%2"" /Fo""%3"" /c'
                            + ' /Yc""[cmdLineOptions.PrecompiledHeaderThrough]""'
                            + ' [fastBuildPCHForceInclude]'
                            + ' [options.AdditionalCompilerOptionsOnPCHCreate]'
                            + ' $CompilerExtraOptions$'
                            + ' $CompilerOptimizations$'

";
                public static string PCHOptionsClang = @"
    // Precompiled Header options for Clang
    // ------------------------------------
    .PCHInputFile           = '[fastBuildPrecompiledSourceFile]'
    .PCHOutputFile          = '[cmdLineOptions.PrecompiledHeaderFile]'
    .PCHOptions             = '-o ""%2"" -c -x c++-header ""%1""'
                            + ' [options.AdditionalCompilerOptionsOnPCHCreate]'
                            + ' $CompilerExtraOptions$'
                            + ' $CompilerOptimizations$'

";
                public static string PCHOptionsDeoptimize = @"
    .PCHOptionsDeoptimized = .PCHOptions
";

                public static string UsePrecompClang = @"-include-pch $PCHOutputFile$'
                            + ' [options.AdditionalCompilerOptionsOnPCHUse]'
                            + '";
                public static string UsePrecomp = @"/Yu""[cmdLineOptions.PrecompiledHeaderThrough]"" /Fp""$PCHOutputFile$""'
                            + ' [options.AdditionalCompilerOptionsOnPCHUse]'
                            + ' [fastBuildPCHForceInclude]";

                public static string ResourceCompilerOptions = @"
    // Resource Compiler options
    // -------------------------
    .Compiler               = .ResourceCompiler
    .CompilerOutputExtension= '.res'
    .CompilerOptions        = '/fo""%2"" $ResourceCompilerExtraOptions$ ""%1""'
    .CompilerOutputPath     = '$Intermediate$'
    .CompilerInputFiles     = [fastBuildResourceFiles]

";

                public static string ResourceCompilerExtraOptions = @"
    .ResourceCompilerExtraOptions   = ' /l 0x0409 /nologo'
                                    + ' [cmdLineOptions.AdditionalResourceIncludeDirectories]'
                                    + ' [cmdLineOptions.ResourcePreprocessorDefinitions]'
";

                public static string EmbeddedResourceCompilerOptions = @"
    // Resource Compiler options
    // -------------------------
    .Compiler               = '[fastBuildEmbeddedResourceCompiler]'
    .CompilerOutputPrefix   = '[fastBuildEmbeddedOutputPrefix]'
    .CompilerOutputExtension= '.resources'
    .CompilerOptions        = '/useSourcePath ""%1"" ""%2""'
    .CompilerOutputPath     = '$Intermediate$'
    .CompilerInputFiles     = [fastBuildEmbeddedResources]

";

                public static string CompilerOptionsCommon = @"
    .CompilerInputUnity       = '[fastBuildUnityName]'
    .CompilerOutputPath       = '$Intermediate$'
    .CompilerInputPath        = [fastBuildInputPath]
    .CompilerInputPattern     = [fastBuildCompilerInputPattern]
    .CompilerInputExcludedFiles = [fastBuildInputExcludedFiles]
    .CompilerInputFiles       = [fastBuildSourceFiles]
    .CompilerInputFilesRoot   = '[fastBuildInputFilesRootPath]'

";

                public static string CompilerOptionsCPP = @"
    // Compiler options
    // ----------------
    .CompilerOptions        = '""%1"" /Fo""%2"" /c'
                            + ' [fastBuildCompilerPCHOptions]'
                            + ' $CompilerExtraOptions$'
                            + ' $CompilerOptimizations$'
";
                public static string CompilerOptionsMasm = @"
    // Compiler options
    // ----------------
    .CompilerOptions        = ' $CompilerExtraOptions$'
                            + ' /Fo""%2"" /c /Ta ""%1""'
";
                // TODOANT
                public static string CompilerOptionsNasm = @"
    // Compiler options
    // ----------------
    .CompilerOptions        = ' $CompilerExtraOptions$'
                            + ' -Xvc -Ox -o""%2"" ""%1""'
                            + ' [cmdLineOptions.NasmCompilerFormat] '
";
                public static string CompilerOptionsClang = @"
    // Compiler options
    // ----------------
    .CompilerOptions        = '[fastBuildClangFileLanguage]""%1"" -o ""%2"" -c'
                            + ' [fastBuildCompilerPCHOptionsClang]'
                            + ' $CompilerExtraOptions$'
                            + ' $CompilerOptimizations$'
";

                public static string LibrarianAdditionalInputs = @"
    .LibrarianAdditionalInputs = [fastBuildLibrarianAdditionalInputs]
";

                public static string LibrarianOptions = @"
    .LibrarianOutput        = '[fastBuildOutputFile]'
    .LibrarianOptions       = '""%1"" /OUT:""%2""'
                            + ' [cmdLineOptions.LinkerSuppressStartupBanner]'
                            + ' [cmdLineOptions.TreatLibWarningAsErrors]'
                            + ' [options.AdditionalLibrarianOptions]'

";

                public static string LibrarianOptionsClang = @"
    .LibrarianOutput        = '[fastBuildOutputFile]'
    .LibrarianOptions       = 'rcs[cmdLineOptions.UseThinArchives] ""%2"" ""%1""'

";

                public static string MasmCompilerExtraOptions = @"
    .CompilerExtraOptions   = ''
            + ' [cmdLineOptions.AdditionalAssemblyIncludeDirectories]'
            + ' /nologo'
            + ' /W3'
            + ' /errorReport:queue'
            + ' [cmdLineOptions.PreprocessorDefinitions]'
";

                // TODOANT: NasmCompilerExtraOptions
                public static string NasmCompilerExtraOptions = @"
    .CompilerExtraOptions   = ''
            + ' [cmdLineOptions.AdditionalAssemblyNasmIncludeDirectories]'
            + ' [cmdLineOptions.NasmPreprocessorDefinitions]'
            + ' [cmdLineOptions.PreIncludedFiles]'
";

                public static string CPPCompilerExtraOptions = @"
    .CompilerExtraOptions   = ''
            + ' [cmdLineOptions.AdditionalIncludeDirectories]'
            + ' [cmdLineOptions.AdditionalUsingDirectories]'
            + ' [cmdLineOptions.DebugInformationFormat]'
            + ' [fastBuildClrSupport]'
            + ' [fastBuildConsumeWinRTExtension]'
            + ' [cmdLineOptions.SuppressStartupBanner]'
            + ' [cmdLineOptions.WarningLevel]'
            + ' [cmdLineOptions.TreatWarningAsError]'
            + ' [cmdLineOptions.ExternalWarningLevel]'
            + ' [cmdLineOptions.DiagnosticsFormat]'
            + ' [cmdLineOptions.EnableASAN]'
            + ' [fastBuildCompileAsC]'
            + ' [cmdLineOptions.ConfigurationType]'
            + ' [cmdLineOptions.PreprocessorDefinitions]'
            + ' [cmdLineOptions.UndefinePreprocessorDefinitions]'
            + ' [cmdLineOptions.UndefineAllPreprocessorDefinitions]'
            + ' [cmdLineOptions.IgnoreStandardIncludePath]'
            + ' [cmdLineOptions.GeneratePreprocessedFile]'
            + ' [cmdLineOptions.KeepComments]'
            + ' [cmdLineOptions.UseStandardConformingPreprocessor]'
            + ' [cmdLineOptions.StringPooling]'
            + ' [cmdLineOptions.MinimalRebuild]'
            + ' [cmdLineOptions.ExceptionHandling]'
            + ' [cmdLineOptions.SmallerTypeCheck]'
            + ' [cmdLineOptions.BasicRuntimeChecks]'
            + ' [cmdLineOptions.RuntimeLibrary]'
            + ' [cmdLineOptions.StructMemberAlignment]'
            + ' [cmdLineOptions.BufferSecurityCheck]'
            + ' [cmdLineOptions.EnableFunctionLevelLinking]'
            + ' [cmdLineOptions.EnableEnhancedInstructionSet]'
            + ' [cmdLineOptions.FloatingPointModel]'
            + ' [cmdLineOptions.FloatingPointExceptions]'
            + ' [cmdLineOptions.CompilerCreateHotpatchableImage]'
            + ' [cmdLineOptions.SupportJustMyCode]'
            + ' [cmdLineOptions.SpectreMitigation]'
            + ' [cmdLineOptions.DisableLanguageExtensions]'
            + ' [cmdLineOptions.TreatWChar_tAsBuiltInType]'
            + ' [cmdLineOptions.ForceConformanceInForLoopScope]'
            + ' [cmdLineOptions.RemoveUnreferencedCodeData]'
            + ' [cmdLineOptions.RuntimeTypeInfo]'
            + ' [cmdLineOptions.OpenMP]'
            + ' [cmdLineOptions.LanguageStandard_C]'
            + ' [cmdLineOptions.LanguageStandard]'
            + ' [cmdLineOptions.ConformanceMode]'
            + ' [cmdLineOptions.CompilerProgramDatabaseFile]'
            + ' [cmdLineOptions.CallingConvention]'
            + ' [cmdLineOptions.DisableSpecificWarnings]'
            + ' [cmdLineOptions.ForcedIncludeFiles]'
            + ' [fastBuildSourceFileType]'
            + ' [fastBuildAdditionalCompilerOptionsFromCode]'
            + ' /errorReport:queue'
            + ' [cmdLineOptions.TranslateIncludes]'
            + ' [cmdLineOptions.TreatAngleIncludeAsExternal]'
            + ' [cmdLineOptions.ExternalTemplatesDiagnostics]'
            + ' [cmdLineOptions.CharacterSet]'
            + ' [options.AdditionalCompilerOptions]'
            + ' [fastBuildCompilerForceUsing]'
";
                public static string CPPCompilerOptimizationOptions =
@"
    .CompilerOptimizations = ''
            + ' [cmdLineOptions.Optimization]'
            + ' [cmdLineOptions.InlineFunctionExpansion]'
            + ' [cmdLineOptions.EnableIntrinsicFunctions]'
            + ' [cmdLineOptions.FavorSizeOrSpeed]'
            + ' [cmdLineOptions.OmitFramePointers]'
            + ' [cmdLineOptions.EnableFiberSafeOptimizations]'
            + ' [cmdLineOptions.CompilerWholeProgramOptimization]'
            + ' [cmdLineOptions.GenerateProfileGuidedOptimizationData]'
            + ' [cmdLineOptions.UseProfileGuidedOptimizationData]'
            + ' [options.AdditionalCompilerOptimizeOptions]'
";

                public static string CPPCompilerOptionsDeoptimize = @"
    .CompilerOptionsDeoptimized = '""%1"" /Fo""%2"" /c'
                            + ' [fastBuildCompilerPCHOptions]'
                            + ' [fastBuildPCHForceInclude]'
                            + ' $CompilerExtraOptions$'
                            + ' /Od'
";

                public const string ClangCompilerOptionsDeoptimize = @"
    .CompilerOptionsDeoptimized = '[fastBuildClangFileLanguage]""%1"" -o ""%2"" -c'
                            + ' [fastBuildCompilerPCHOptionsClang]'
                            + ' $CompilerExtraOptions$'
                            + ' [fastBuildCompilerDeoptimizeOptionClang]'
";
                public static string DeOptimizeOption = @"
    .DeoptimizeWritableFiles = [fastBuildDeoptimizationWritableFiles]
    .DeoptimizeWritableFilesWithToken = [fastBuildDeoptimizationWritableFilesWithToken]
";
                public static string PreBuildDependencies = @"
    .PreBuildDependencies = [fastBuildPreBuildTargets]
";

                public static string PlatformBeginSection = @"
////////////////////////////////////////////////////////////////////////////////
// PLATFORM SPECIFIC SECTION
#if [fastBuildDefine]
";

                public static string PlatformEndSection = @"
#endif // [fastBuildDefine]
////////////////////////////////////////////////////////////////////////////////
";


                public static string LibBeginSection = @"
//=================================================================================================================
Library( '[fastBuildOutputFileShortName]_[fastBuildOutputType]' )
{
    [fastBuildUsingPlatformConfig]
    .Intermediate           = '[cmdLineOptions.IntermediateDirectory]\'
";

                public static string EndSection = "}\n\n";

                public static string TargetSection = @"
//=================================================================================================================
Alias( '[fastBuildOutputFileShortName]' )
{
    .Targets = [fastBuildTargetSubTargets]
}

";

                public static string TargetForLibraryDependencySection = @"
//=================================================================================================================
Alias( '[fastBuildOutputFileShortName]_LibraryDependency' )
{
    .Targets = [fastBuildTargetLibraryDependencies]
}

";

                public static string CopyFileSection = @"
//=================================================================================================================
Copy( '[fastBuildCopyAlias]' )
{
    .Source = '[fastBuildCopySource]'
    .Dest = '[fastBuildCopyDest]'
    .PreBuildDependencies = [fastBuildCopyDependencies]
}

";

                public static string ExeDllBeginSection = @"
//=================================================================================================================
[fastBuildOutputType]( '[fastBuildOutputFileShortName]_[fastBuildOutputType]' )
{
    [fastBuildUsingPlatformConfig]
    .Intermediate           = '[cmdLineOptions.IntermediateDirectory]\'
    .Libraries              = [fastBuildProjectDependencies]
    .PreBuildDependencies   = [fastBuildBuildOnlyDependencies]
    .LinkerAssemblyResources = { [fastBuildObjectListEmbeddedResources] }
    .LinkerOutput           = '[fastBuildLinkerOutputFile]'
    .LinkerLinkObjects      = [fastBuildLinkerLinkObjects]
    .LinkerStampExe         = [fastBuildStampExecutable]
    .LinkerStampExeArgs     = [fastBuildStampArguments]
    .ConcurrencyGroupName   = '[fastbuildConcurrencyGroupName]'
";

                public static string ResourcesBeginSection = @"
//=================================================================================================================
ObjectList( '[fastBuildOutputFileShortName]_resources' )
{
    [fastBuildUsingPlatformConfig]
    .Intermediate           = '[cmdLineOptions.IntermediateDirectory]\'
";

                public static string EmbeddedResourcesBeginSection = @"
//=================================================================================================================
ObjectList( '[fastBuildOutputFileShortName]_embedded' )
{
    [fastBuildUsingPlatformConfig]
    .Intermediate           = '[cmdLineOptions.IntermediateDirectory]\'
";

                public static string ObjectListBeginSection = @"
//=================================================================================================================
ObjectList( '[fastBuildOutputFileShortName]_objects' )
{
    [fastBuildUsingPlatformConfig]
    .Intermediate           = '[cmdLineOptions.IntermediateDirectory]\'
";

                public static string GenericExecutableSection = @"
//=================================================================================================================
Exec( '[fastBuildPreBuildName]' )
{
  .ExecExecutable       = '[fastBuildPrebuildExeFile]'
  .ExecInput            = [fastBuildPreBuildInputFiles]
  .ExecOutput           = '[fastBuildPreBuildOutputFile]'
  .ExecArguments        = '[fastBuildPreBuildArguments]'
  .ExecWorkingDir       = '[fastBuildPrebuildWorkingPath]'
  .ExecUseStdOutAsOutput = [fastBuildPrebuildUseStdOutAsOutput]
  .ExecAlwaysShowOutput =  [fastBuildPrebuildAlwaysShowOutput]
  .PreBuildDependencies = [fastBuildExecPreBuildDependencies]
  .ExecAlways           = [fastBuildExecAlways]
}

";

                public static string TestSection = @"
//=================================================================================================================
Test( '[fastBuildTest]' )
{
  .TestExecutable        = '[fastBuildTestExecutable]'
  .TestOutput            = '[fastBuildTestOutput]'
  .TestArguments         = '[fastBuildTestArguments]'
  .TestWorkingDir        = '[fastBuildTestWorkingDir]'
  .TestTimeOut           =  [fastBuildTestTimeOut]
  .TestAlwaysShowOutput  =  [fastBuildTestAlwaysShowOutput]
  .TestInput             = [fastBuildTestPreBuildDependencies]
}

";

                public static string UnityBeginSection = @"
//=================================================================================================================
// Master .bff Unity/Blob files (shared across configs)
//=================================================================================================================
";
                public static string UnitySection = @"
Unity( '[unityFile.UnityName]' )
{
    .UnityInputPath                     = [unityFile.UnityInputPath]
    .UnityInputExcludePath              = [unityFile.UnityInputExcludePath]
    .UnityInputExcludePattern           = [unityFile.UnityInputExcludePattern]
    .UnityInputPattern                  = [unityFile.UnityInputPattern]
    .UnityInputPathRecurse              = '[unityFile.UnityInputPathRecurse]'
    .UnityInputFiles                    = [unityFile.UnityInputFiles]
    .UnityInputExcludedFiles            = [unityFile.UnityInputExcludedFiles]
    .UnityInputObjectLists              = [unityFile.UnityInputObjectLists]
    .UnityInputIsolateWritableFiles     =  [unityFile.UnityInputIsolateWritableFiles]
    .UnityInputIsolateWritableFilesLimit = [unityFile.UnityInputIsolateWritableFilesLimit]
    .UnityInputIsolateListFile          = '[unityFile.UnityInputIsolateListFile]'
    .UnityOutputPath                    = '[unityFile.UnityOutputPath]'
    .UnityOutputPattern                 = '[unityFile.UnityOutputPattern]'
    .UnityNumFiles                      =  [unityFile.UnityNumFiles]
    .UnityPCH                           = '[unityFile.UnityPCH]'
    .UseRelativePaths_Experimental      = [unityFile.UseRelativePaths]
}
";

                public static string CopyDirSection = @"
//=================================================================================================================
CopyDir( '[fastBuildCopyDirName]' )
{
    .SourcePaths                        = '[fastBuildCopyDirSourcePath]'
    .SourcePathsPattern                 = [fastBuildCopyDirPattern]
    .SourcePathsRecurse                 = [fastBuildCopyDirRecurse]
    .Dest                               = '[fastBuildCopyDirDestinationPath]'
    .PreBuildDependencies               = [fastBuildCopyDirDependencies]
}
";
                // All config sections. For now this section is used for submit assistant(when there is a source file filter)
                public static string AllConfigsSection = @"
//=================================================================================================================
// All Configs Alias
//=================================================================================================================
Alias( 'All-Configs' )
{
    .Targets =
    [fastBuildConfigs]
}
";

                public static string IncludeMasterBff = @"
//=================================================================================================================
// Entry BFF for [solutionFileName]
//=================================================================================================================
#once
#include ""[masterBffFilePath]""
";
            }
        }
    }
}
