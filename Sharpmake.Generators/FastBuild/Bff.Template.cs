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
}
";

                public const string WinEnvironment =
@"#if __WINDOWS__[envRemoveGuards]
    #import TMP
    #import TEMP
    #import USERPROFILE
    .Environment =
    {
        ""TMP=$TMP$"",
        ""TEMP=$TEMP$"",
        ""USERPROFILE=$USERPROFILE$"",
        ""SystemRoot=[fastBuildSystemRoot]""
        ""PATH=[fastBuildPATH]""
    }
#endif[envRemoveGuards]
";

                public const string OsxEnvironment =
@"#if __OSX__[envRemoveGuards]
    #import TMPDIR
    .Environment =
    {
        ""TMPDIR=$TMPDIR$"",
        ""PATH=[fastBuildPATH]""
    }
#endif[envRemoveGuards]
";

                public static string MasmConfigNameSuffix = "Masm";
                public static string Win64ConfigName = ".win64Config";

                public static string CompilerSetting = @"
//=================================================================================================================
Compiler( '[fastbuildCompilerName]' )
{
    .ExecutableRootPath     = '[fastBuildCompilerRootPath]'
    .Executable             = '[fastBuildCompilerExecutable]'
    .ExtraFiles             = [fastBuildExtraFiles]
    .CompilerFamily         = '[fastBuildCompilerFamily]'
    [fastBuildVS2012EnumBugWorkaround]
}
";

                public static string CompilerConfiguration = @"
[fastBuildConfigurationName] =
[
    Using( [fastBuildUsing] )
    .BinPath                = '[fastBuildBinPath]'
    .LinkerPath             = '[fastBuildLinkerPath]'
    .ResourceCompiler       = '[fastBuildResourceCompiler]'
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
                            // Input files
                            // ---------------------------
                            + ' ""%1""'
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
                            + ' $CompilerExtraOptions$'
                            + ' $CompilerOptimizations$'

";
                public static string PCHOptionsClang = @"
    // Precompiled Header options for Clang
    // ------------------------------------
    .PCHInputFile           = '[fastBuildPrecompiledSourceFile]'
    .PCHOutputFile          = '[cmdLineOptions.PrecompiledHeaderFile]'
    .PCHOptions             = '-o ""%2"" -c -x c++-header ""%1""'
                            + ' $CompilerExtraOptions$'
                            + ' $CompilerOptimizations$'

";
                public static string PCHOptionsDeoptimize = @"
    .PCHOptionsDeoptimized = .PCHOptions
";

                public static string UsePrecompClang = @"-include-pch $PCHOutputFile$";
                public static string UsePrecomp = @"/Yu""[cmdLineOptions.PrecompiledHeaderThrough]"" /Fp""$PCHOutputFile$""'
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
            // General options
            // ---------------------------
            + ' /nologo'
            + ' /W3'
            + ' /errorReport:queue'
            // Preprocessor options
            // ---------------------------
            + ' [cmdLineOptions.PreprocessorDefinitions]'
";

                public static string CPPCompilerExtraOptions = @"
    .CompilerExtraOptions   = ''
            // General options
            // ---------------------------
            + ' [cmdLineOptions.AdditionalIncludeDirectories]'
            + ' [cmdLineOptions.AdditionalUsingDirectories]'
            + ' [cmdLineOptions.DebugInformationFormat]'
            + ' [fastBuildClrSupport]'
            + ' [fastBuildConsumeWinRTExtension]'
            + ' [cmdLineOptions.SuppressStartupBanner]'
            + ' [cmdLineOptions.WarningLevel]'
            + ' [cmdLineOptions.TreatWarningAsError]'
            + ' [cmdLineOptions.DiagnosticsFormat]'
            + ' [fastBuildCompileAsC]'
            // Multi-threaded build is already handled by FASTBuild
            // + ' [cmdLineOptions.MultiProcessorCompilation]'
            + ' [cmdLineOptions.ConfigurationType]'
            // Preprocessor options
            // ---------------------------
            + ' [cmdLineOptions.PreprocessorDefinitions]'
            + ' [cmdLineOptions.UndefinePreprocessorDefinitions]'
            + ' [cmdLineOptions.UndefineAllPreprocessorDefinitions]'
            + ' [cmdLineOptions.IgnoreStandardIncludePath]'
            + ' [cmdLineOptions.GeneratePreprocessedFile]'
            + ' [cmdLineOptions.KeepComments]'
            // Code Generation options
            // ---------------------------
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
            + ' [cmdLineOptions.CreateHotpatchableImage]'
            + ' [cmdLineOptions.SupportJustMyCode]'
            + ' [cmdLineOptions.SpectreMitigation]'
            // Language options
            // ---------------------------
            + ' [cmdLineOptions.DisableLanguageExtensions]'
            + ' [cmdLineOptions.TreatWChar_tAsBuiltInType]'
            + ' [cmdLineOptions.ForceConformanceInForLoopScope]'
            + ' [cmdLineOptions.RemoveUnreferencedCodeData]'
            + ' [cmdLineOptions.RuntimeTypeInfo]'
            + ' [cmdLineOptions.OpenMP]'
            + ' [cmdLineOptions.LanguageStandard]'
            // Output Files options
            // ---------------------------
            + ' [cmdLineOptions.CompilerProgramDatabaseFile]'
            // Advanced options
            // ---------------------------
            + ' [cmdLineOptions.CallingConvention]'
            + ' [cmdLineOptions.DisableSpecificWarnings]'
            + ' [cmdLineOptions.ForcedIncludeFiles]'
            + ' [fastBuildSourceFileType]'
            + ' [fastBuildAdditionalCompilerOptionsFromCode]'
            + ' /errorReport:queue'
            // Character Set
            // ---------------------------
            + ' [cmdLineOptions.CharacterSet]'
            // Additional compiler options
            //--------------------------
            + ' [options.AdditionalCompilerOptions]'
            + ' [fastBuildCompilerForceUsing]'
";
                public static string CPPCompilerOptimizationOptions =
@"
    // Optimizations options
    // ---------------------
    .CompilerOptimizations = ''
            + ' [cmdLineOptions.Optimization]'
            + ' [cmdLineOptions.InlineFunctionExpansion]'
            + ' [cmdLineOptions.EnableIntrinsicFunctions]'
            + ' [cmdLineOptions.FavorSizeOrSpeed]'
            + ' [cmdLineOptions.OmitFramePointers]'
            + ' [cmdLineOptions.EnableFiberSafeOptimizations]'
            + ' [cmdLineOptions.CompilerWholeProgramOptimization]'
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
                            + ' -O0'
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
                public static string CopyFileSection = @"
//=================================================================================================================
Copy( '[fastBuildCopyAlias]' )
{
    .Source = '[fastBuildCopySource]'
    .Dest = '[fastBuildCopyDest]'
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
    .LinkerStampExe         = '[fastBuildStampExecutable]'
    .LinkerStampExeArgs     = '[fastBuildStampArguments]'
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
}

";
                [Obsolete("Section has been renamed to: GenericExecutableSection")]
                public static string GenericExcutableSection = GenericExecutableSection;

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
    .UnityOutputPath                    = '[unityFile.UnityOutputPath]'
    .UnityOutputPattern                 = '[unityFile.UnityOutputPattern]'
    .UnityNumFiles                      =  [unityFile.UnityNumFiles]
    .UnityPCH                           = '[unityFile.UnityPCH]'
}
";

                public static string CopyDirSection = @"
CopyDir( '[fastBuildCopyDirName]' )
{
    .SourcePaths                        = '[fastBuildCopyDirSourcePath]'
    .SourcePathsPattern                 = [fastBuildCopyDirPattern]
    .SourcePathsRecurse                 = [fastBuildCopyDirRecurse]
    .Dest                               = '[fastBuildCopyDirDestinationPath]'
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
