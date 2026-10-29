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
                internal const string HeaderFile = @"
//=================================================================================================================
// [fastBuildProjectName] FASTBuild config file
//=================================================================================================================
#once

";

                internal const string Define = @"#define [fastBuildDefine]
";


                internal const string CustomSectionHeader = @"
//=================================================================================================================
// FASTBuild custom section
//=================================================================================================================
";


                internal const string Includes = @"
//=================================================================================================================
// [fastBuildProjectName] .bff includes
//=================================================================================================================
[fastBuildOrderedBffDependencies]
";

                internal const string GlobalConfigurationInclude = @"
//=================================================================================================================
// Global Configuration include
//=================================================================================================================
[fastBuildGlobalConfigurationInclude]
";

                internal const string GlobalSettings = @"
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

                internal const string ConcurrencyGroup =
@"[fastBuildConcurrencyGroupSectionName] =
[
    .ConcurrencyGroupName = '[fastBuildConcurrencyGroupName]'
    .ConcurrencyLimit = [fastBuildConcurrencyLimit]
    .ConcurrencyPerJobMiB = [fastBuildConcurrencyPerJobMiB]
]
";
                internal const string AllowCaching = @"
    .AllowCaching = [fastBuildAllowCaching]
";

                internal const string WinEnvironment =
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

                internal const string OsxEnvironment =
@"    #import TMPDIR
    .Environment =
    {
        ""TMPDIR=$TMPDIR$"",
        ""PATH=[fastBuildPATH]""
[envAdditionalVariables]
    }
";

                internal const string LinuxEnvironment =
@"    .Environment =
    {
        ""PATH=[fastBuildPATH]""
[envAdditionalVariables]
    }
";

                internal const string MasmConfigNameSuffix = "Masm";
                internal const string NasmConfigNameSuffix = "Nasm";
                internal const string Win64ConfigName = ".win64Config";

                internal const string CompilerSetting = @"
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

                internal const string ResourceCompilerSettings = @"
Compiler( '[fastBuildResourceCompilerName]' )
{
    .Executable             = '[fastBuildResourceCompiler]'
    .CompilerFamily         = 'custom'
}
";

                internal const string MasmCompilerSettings = @"
Compiler( '[fastBuildMasmCompilerName]' )
{
    .Executable             = '[fastBuildMasmCompiler]'
    .CompilerFamily         = 'custom'
}
";

                // TODOANT
                internal const string NasmCompilerSettings = @"
Compiler( '[fastBuildNasmCompilerName]' )
{
    .Executable             = '[fastBuildNasmCompiler]'
    .CompilerFamily         = 'custom'
}
";

                internal const string CompilerConfiguration = @"
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

                internal const string LinkerOptions = @"
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

                internal const string PCHOptions = @"
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
                internal const string PCHOptionsClang = @"
    // Precompiled Header options for Clang
    // ------------------------------------
    .PCHInputFile           = '[fastBuildPrecompiledSourceFile]'
    .PCHOutputFile          = '[cmdLineOptions.PrecompiledHeaderFile]'
    .PCHOptions             = '-o ""%2"" -c -x c++-header ""%1""'
                            + ' [options.AdditionalCompilerOptionsOnPCHCreate]'
                            + ' $CompilerExtraOptions$'
                            + ' $CompilerOptimizations$'

";
                internal const string PCHOptionsDeoptimize = @"
    .PCHOptionsDeoptimized = .PCHOptions
";

                internal const string UsePrecompClang = @"-include-pch $PCHOutputFile$'
                            + ' [options.AdditionalCompilerOptionsOnPCHUse]'
                            + '";
                internal const string UsePrecomp = @"/Yu""[cmdLineOptions.PrecompiledHeaderThrough]"" /Fp""$PCHOutputFile$""'
                            + ' [options.AdditionalCompilerOptionsOnPCHUse]'
                            + ' [fastBuildPCHForceInclude]";

                internal const string ResourceCompilerOptions = @"
    // Resource Compiler options
    // -------------------------
    .Compiler               = .ResourceCompiler
    .CompilerOutputExtension= '.res'
    .CompilerOptions        = '/fo""%2"" $ResourceCompilerExtraOptions$ ""%1""'
    .CompilerOutputPath     = '$Intermediate$'
    .CompilerInputFiles     = [fastBuildResourceFiles]

";

                internal const string ResourceCompilerExtraOptions = @"
    .ResourceCompilerExtraOptions   = ' /l 0x0409 /nologo'
                                    + ' [cmdLineOptions.AdditionalResourceIncludeDirectories]'
                                    + ' [cmdLineOptions.ResourcePreprocessorDefinitions]'
";

                internal const string EmbeddedResourceCompilerOptions = @"
    // Resource Compiler options
    // -------------------------
    .Compiler               = '[fastBuildEmbeddedResourceCompiler]'
    .CompilerOutputPrefix   = '[fastBuildEmbeddedOutputPrefix]'
    .CompilerOutputExtension= '.resources'
    .CompilerOptions        = '/useSourcePath ""%1"" ""%2""'
    .CompilerOutputPath     = '$Intermediate$'
    .CompilerInputFiles     = [fastBuildEmbeddedResources]

";

                internal const string CompilerOptionsCommon = @"
    .CompilerInputUnity       = '[fastBuildUnityName]'
    .CompilerOutputPath       = '$Intermediate$'
    .CompilerInputPath        = [fastBuildInputPath]
    .CompilerInputPattern     = [fastBuildCompilerInputPattern]
    .CompilerInputExcludedFiles = [fastBuildInputExcludedFiles]
    .CompilerInputFiles       = [fastBuildSourceFiles]
    .CompilerInputFilesRoot   = '[fastBuildInputFilesRootPath]'

";

                internal const string CompilerOptionsCPP = @"
    // Compiler options
    // ----------------
    .CompilerOptions        = '""%1"" /Fo""%2"" /c'
                            + ' [fastBuildCompilerPCHOptions]'
                            + ' $CompilerExtraOptions$'
                            + ' $CompilerOptimizations$'
";
                internal const string CompilerOptionsMasm = @"
    // Compiler options
    // ----------------
    .CompilerOptions        = ' $CompilerExtraOptions$'
                            + ' /Fo""%2"" /c /Ta ""%1""'
";
                // TODOANT
                internal const string CompilerOptionsNasm = @"
    // Compiler options
    // ----------------
    .CompilerOptions        = ' $CompilerExtraOptions$'
                            + ' -Xvc -Ox -o""%2"" ""%1""'
                            + ' [cmdLineOptions.NasmCompilerFormat] '
";
                internal const string CompilerOptionsClang = @"
    // Compiler options
    // ----------------
    .CompilerOptions        = '[fastBuildClangFileLanguage]""%1"" -o ""%2"" -c'
                            + ' [fastBuildCompilerPCHOptionsClang]'
                            + ' $CompilerExtraOptions$'
                            + ' $CompilerOptimizations$'
";

                internal const string LibrarianAdditionalInputs = @"
    .LibrarianAdditionalInputs = [fastBuildLibrarianAdditionalInputs]
";

                internal const string LibrarianOptions = @"
    .LibrarianOutput        = '[fastBuildOutputFile]'
    .LibrarianOptions       = '""%1"" /OUT:""%2""'
                            + ' [cmdLineOptions.LinkerSuppressStartupBanner]'
                            + ' [cmdLineOptions.TreatLibWarningAsErrors]'
                            + ' [options.AdditionalLibrarianOptions]'

";

                internal const string LibrarianOptionsClang = @"
    .LibrarianOutput        = '[fastBuildOutputFile]'
    .LibrarianOptions       = 'rcs[cmdLineOptions.UseThinArchives] ""%2"" ""%1""'
                            + ' [options.AdditionalLibrarianOptions]'

";

                internal const string MasmCompilerExtraOptions = @"
    .CompilerExtraOptions   = ''
            + ' [cmdLineOptions.AdditionalAssemblyIncludeDirectories]'
            + ' /nologo'
            + ' /W3'
            + ' /errorReport:queue'
            + ' [cmdLineOptions.PreprocessorDefinitions]'
";

                // TODOANT: NasmCompilerExtraOptions
                internal const string NasmCompilerExtraOptions = @"
    .CompilerExtraOptions   = ''
            + ' [cmdLineOptions.AdditionalAssemblyNasmIncludeDirectories]'
            + ' [cmdLineOptions.NasmPreprocessorDefinitions]'
            + ' [cmdLineOptions.PreIncludedFiles]'
";

                internal const string CPPCompilerExtraOptions = @"
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
                internal const string CPPCompilerOptimizationOptions =
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

                internal const string CPPCompilerOptionsDeoptimize = @"
    .CompilerOptionsDeoptimized = '""%1"" /Fo""%2"" /c'
                            + ' [fastBuildCompilerPCHOptions]'
                            + ' [fastBuildPCHForceInclude]'
                            + ' $CompilerExtraOptions$'
                            + ' /Od'
";

                internal const string ClangCompilerOptionsDeoptimize = @"
    .CompilerOptionsDeoptimized = '[fastBuildClangFileLanguage]""%1"" -o ""%2"" -c'
                            + ' [fastBuildCompilerPCHOptionsClang]'
                            + ' $CompilerExtraOptions$'
                            + ' [fastBuildCompilerDeoptimizeOptionClang]'
";
                internal const string DeOptimizeOption = @"
    .DeoptimizeWritableFiles = [fastBuildDeoptimizationWritableFiles]
    .DeoptimizeWritableFilesWithToken = [fastBuildDeoptimizationWritableFilesWithToken]
";
                internal const string PreBuildDependencies = @"
    .PreBuildDependencies = [fastBuildPreBuildTargets]
";

                internal const string PlatformBeginSection = @"
////////////////////////////////////////////////////////////////////////////////
// PLATFORM SPECIFIC SECTION
#if [fastBuildDefine]
";

                internal const string PlatformEndSection = @"
#endif // [fastBuildDefine]
////////////////////////////////////////////////////////////////////////////////
";


                internal const string LibBeginSection = @"
//=================================================================================================================
Library( '[fastBuildOutputFileShortName]_[fastBuildOutputType]' )
{
    [fastBuildUsingPlatformConfig]
    .Intermediate           = '[cmdLineOptions.IntermediateDirectory]\'
";

                internal const string EndSection = "}\n\n";

                internal const string TargetSection = @"
//=================================================================================================================
Alias( '[fastBuildOutputFileShortName]' )
{
    .Targets = [fastBuildTargetSubTargets]
}

";

                internal const string TargetForLibraryDependencySection = @"
//=================================================================================================================
Alias( '[fastBuildOutputFileShortName]_LibraryDependency' )
{
    .Targets = [fastBuildTargetLibraryDependencies]
}

";

                internal const string CopyFileSection = @"
//=================================================================================================================
Copy( '[fastBuildCopyAlias]' )
{
    .Source = '[fastBuildCopySource]'
    .Dest = '[fastBuildCopyDest]'
    .PreBuildDependencies = [fastBuildCopyDependencies]
}

";

                internal const string ExeDllBeginSection = @"
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

                internal const string ResourcesBeginSection = @"
//=================================================================================================================
ObjectList( '[fastBuildOutputFileShortName]_resources' )
{
    [fastBuildUsingPlatformConfig]
    .Intermediate           = '[cmdLineOptions.IntermediateDirectory]\'
";

                internal const string EmbeddedResourcesBeginSection = @"
//=================================================================================================================
ObjectList( '[fastBuildOutputFileShortName]_embedded' )
{
    [fastBuildUsingPlatformConfig]
    .Intermediate           = '[cmdLineOptions.IntermediateDirectory]\'
";

                internal const string ObjectListBeginSection = @"
//=================================================================================================================
ObjectList( '[fastBuildOutputFileShortName]_objects' )
{
    [fastBuildUsingPlatformConfig]
    .Intermediate           = '[cmdLineOptions.IntermediateDirectory]\'
";

                internal const string GenericExecutableSection = @"
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

                internal const string TestSection = @"
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

                internal const string UnityBeginSection = @"
//=================================================================================================================
// Master .bff Unity/Blob files (shared across configs)
//=================================================================================================================
";
                internal const string UnitySection = @"
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

                internal const string CopyDirSection = @"
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
                internal const string AllConfigsSection = @"
//=================================================================================================================
// All Configs Alias
//=================================================================================================================
Alias( 'All-Configs' )
{
    .Targets =
    [fastBuildConfigs]
}
";

                internal const string IncludeMasterBff = @"
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
