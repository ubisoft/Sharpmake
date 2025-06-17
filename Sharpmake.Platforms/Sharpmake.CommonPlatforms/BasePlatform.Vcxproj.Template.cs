// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public abstract partial class BasePlatform
    {
        private const string _projectConfigurationsCompileTemplate =
            @"    <ClCompile>
      <PrecompiledHeader>[options.UsePrecompiledHeader]</PrecompiledHeader>
      <CompileAsWinRT>[options.CompileAsWinRT]</CompileAsWinRT>
      <WarningLevel>[options.WarningLevel]</WarningLevel>
      <TreatAngleIncludeAsExternal>[options.TreatAngleIncludeAsExternal]</TreatAngleIncludeAsExternal>
      <ExternalWarningLevel>[options.ExternalWarningLevel]</ExternalWarningLevel>
      <ExternalTemplatesDiagnostics>[options.ExternalTemplatesDiagnostics]</ExternalTemplatesDiagnostics>
      <Optimization>[options.Optimization]</Optimization>
      <PreprocessorDefinitions>[EscapeXML:options.PreprocessorDefinitions];%(PreprocessorDefinitions);$(PreprocessorDefinitions)</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>[options.AdditionalIncludeDirectories]</AdditionalIncludeDirectories>
      <AdditionalUsingDirectories>[options.AdditionalUsingDirectories]</AdditionalUsingDirectories>
      <DebugInformationFormat>[options.DebugInformationFormat]</DebugInformationFormat>
      <CompileAsManaged>[compileAsManaged]</CompileAsManaged>
      <SuppressStartupBanner>true</SuppressStartupBanner>
      <TreatWarningAsError>[options.TreatWarningAsError]</TreatWarningAsError>
      <DiagnosticsFormat>[options.DiagnosticsFormat]</DiagnosticsFormat>
      <MultiProcessorCompilation>[options.MultiProcessorCompilation]</MultiProcessorCompilation>
      <UseUnicodeForAssemblerListing>false</UseUnicodeForAssemblerListing>
      <InlineFunctionExpansion>[options.InlineFunctionExpansion]</InlineFunctionExpansion>
      <IntrinsicFunctions>[options.EnableIntrinsicFunctions]</IntrinsicFunctions>
      <FavorSizeOrSpeed>[options.FavorSizeOrSpeed]</FavorSizeOrSpeed>
      <OmitFramePointers>[options.OmitFramePointers]</OmitFramePointers>
      <EnableFiberSafeOptimizations>[options.EnableFiberSafeOptimizations]</EnableFiberSafeOptimizations>
      <WholeProgramOptimization>[options.CompilerWholeProgramOptimization]</WholeProgramOptimization>
      <UndefineAllPreprocessorDefinitions>false</UndefineAllPreprocessorDefinitions>
      <IgnoreStandardIncludePath>[options.IgnoreStandardIncludePath]</IgnoreStandardIncludePath>
      <PreprocessToFile>[options.GeneratePreprocessedFile]</PreprocessToFile>
      <PreprocessSuppressLineNumbers>[options.PreprocessSuppressLineNumbers]</PreprocessSuppressLineNumbers>
      <PreprocessKeepComments>[options.KeepComments]</PreprocessKeepComments>
      <UseStandardPreprocessor>[options.UseStandardConformingPreprocessor]</UseStandardPreprocessor>
      <StringPooling>[options.StringPooling]</StringPooling>
      <MinimalRebuild>[options.MinimalRebuild]</MinimalRebuild>
      <ExceptionHandling>[options.ExceptionHandling]</ExceptionHandling>
      <SmallerTypeCheck>[options.SmallerTypeCheck]</SmallerTypeCheck>
      <BasicRuntimeChecks>[options.BasicRuntimeChecks]</BasicRuntimeChecks>
      <RuntimeLibrary>[options.RuntimeLibrary]</RuntimeLibrary>
      <StructMemberAlignment>[options.StructMemberAlignment]</StructMemberAlignment>
      <BufferSecurityCheck>[options.BufferSecurityCheck]</BufferSecurityCheck>
      <FunctionLevelLinking>[options.EnableFunctionLevelLinking]</FunctionLevelLinking>
      <EnableEnhancedInstructionSet>[options.EnableEnhancedInstructionSet]</EnableEnhancedInstructionSet>
      <FloatingPointModel>[options.FloatingPointModel]</FloatingPointModel>
      <FloatingPointExceptions>[options.FloatingPointExceptions]</FloatingPointExceptions>
      <CreateHotpatchableImage>[options.CompilerCreateHotpatchableImage]</CreateHotpatchableImage>
      <ConformanceMode>[options.ConformanceMode]</ConformanceMode>
      <DisableLanguageExtensions>[options.DisableLanguageExtensions]</DisableLanguageExtensions>
      <TreatWChar_tAsBuiltInType>[options.TreatWChar_tAsBuiltInType]</TreatWChar_tAsBuiltInType>
      <RemoveUnreferencedCodeData>[options.RemoveUnreferencedCodeData]</RemoveUnreferencedCodeData>
      <ForceConformanceInForLoopScope>[options.ForceConformanceInForLoopScope]</ForceConformanceInForLoopScope>
      <RuntimeTypeInfo>[options.RuntimeTypeInfo]</RuntimeTypeInfo>
      <OpenMPSupport>[options.OpenMP]</OpenMPSupport>
      <LanguageStandard_C>[options.LanguageStandard_C]</LanguageStandard_C>
      <LanguageStandard>[options.LanguageStandard]</LanguageStandard>
      <ExpandAttributedSource>false</ExpandAttributedSource>
      <AssemblerOutput>NoListing</AssemblerOutput>
      <GenerateXMLDocumentationFiles>[options.GenerateXMLDocumentation]</GenerateXMLDocumentationFiles>
      <BrowseInformation>false</BrowseInformation>
      <CallingConvention>[options.CallingConvention]</CallingConvention>
      <CompileAs>Default</CompileAs>
      <DisableSpecificWarnings>[options.DisableSpecificWarnings]</DisableSpecificWarnings>
      <UndefinePreprocessorDefinitions>[options.UndefinePreprocessorDefinitions]</UndefinePreprocessorDefinitions>
      <AdditionalOptions>[options.AllAdditionalCompilerOptions]</AdditionalOptions>
      <PrecompiledHeaderFile>[options.PrecompiledHeaderThrough]</PrecompiledHeaderFile>
      <PrecompiledHeaderOutputFile>[options.PrecompiledHeaderFile]</PrecompiledHeaderOutputFile>
      <ProgramDatabaseFileName>[options.CompilerProgramDatabaseFile]</ProgramDatabaseFileName>
      <RuntimeLibrary>[options.RuntimeLibrary]</RuntimeLibrary>
      <ShowIncludes>[options.ShowIncludes]</ShowIncludes>
      <ForcedIncludeFiles>[options.ForcedIncludeFiles]</ForcedIncludeFiles>
      <ForcedUsingFiles>[options.ForcedUsingFiles]</ForcedUsingFiles>
      <SupportJustMyCode>[options.SupportJustMyCode]</SupportJustMyCode>
      <MaxFilesInUnityFile>[options.MaxFilesPerJumboFile]</MaxFilesInUnityFile>
      <MinFilesInUnityFile>[options.MinFilesPerJumboFile]</MinFilesInUnityFile>
      <MinUnityFiles>[options.MinJumboFiles]</MinUnityFiles>
    </ClCompile>
";

        private const string _projectConfigurationsLinkTemplate =
            @"    <Link>
      <SubSystem>[options.SubSystem]</SubSystem>
      <GenerateDebugInformation>[options.LinkerGenerateDebugInformation]</GenerateDebugInformation>
      <FullProgramDatabaseFile>[options.FullProgramDatabaseFile]</FullProgramDatabaseFile>
      <OutputFile>[options.OutputFile]</OutputFile>
      <ShowProgress>[options.ShowProgress]</ShowProgress>
      <AdditionalLibraryDirectories>[options.AdditionalLibraryDirectories]</AdditionalLibraryDirectories>
      <ManifestFile>[options.ManifestFile]</ManifestFile>
      <ProgramDatabaseFile>[options.LinkerProgramDatabaseFile]</ProgramDatabaseFile>
      <GenerateMapFile>[options.GenerateMapFile]</GenerateMapFile>
      <MapExports>[options.MapExports]</MapExports>
      <SwapRunFromCD>false</SwapRunFromCD>
      <SwapRunFromNET>false</SwapRunFromNET>
      <Driver>NotSet</Driver>
      <OptimizeReferences>[options.OptimizeReferences]</OptimizeReferences>
      <EnableCOMDATFolding>[options.EnableCOMDATFolding]</EnableCOMDATFolding>
      <ProfileGuidedDatabase>[options.ProfileGuidedDatabase]
      </ProfileGuidedDatabase>
      <LinkTimeCodeGeneration>[options.LinkTimeCodeGeneration]</LinkTimeCodeGeneration>
      <IgnoreEmbeddedIDL>false</IgnoreEmbeddedIDL>
      <TypeLibraryResourceID>1</TypeLibraryResourceID>
      <NoEntryPoint>false</NoEntryPoint>
      <SetChecksum>false</SetChecksum>
      <RandomizedBaseAddress>[options.RandomizedBaseAddress]</RandomizedBaseAddress>
      <FixedBaseAddress>[options.FixedBaseAddress]</FixedBaseAddress>
      <TurnOffAssemblyGeneration>false</TurnOffAssemblyGeneration>
      <TargetMachine>[options.TargetMachine]</TargetMachine>
      <Profile>false</Profile>
      <CLRImageType>Default</CLRImageType>
      <LinkErrorReporting>PromptImmediately</LinkErrorReporting>
      <AdditionalOptions>[options.AdditionalLinkerOptions]</AdditionalOptions>
      <AdditionalDependencies>[options.AdditionalDependencies]</AdditionalDependencies>
      <SuppressStartupBanner>[options.SuppressStartupBanner]</SuppressStartupBanner>
      <IgnoreAllDefaultLibraries>[options.IgnoreAllDefaultLibraries]</IgnoreAllDefaultLibraries>
      <IgnoreSpecificDefaultLibraries>[options.IgnoreDefaultLibraryNames]</IgnoreSpecificDefaultLibraries>
      <AssemblyDebug>[options.AssemblyDebug]</AssemblyDebug>
      <HeapReserveSize>[options.HeapReserveSize]</HeapReserveSize>
      <HeapCommitSize>[options.HeapCommitSize]</HeapCommitSize>
      <StackReserveSize>[options.StackReserveSize]</StackReserveSize>
      <StackCommitSize>[options.StackCommitSize]</StackCommitSize>
      <LargeAddressAware>[options.LargeAddressAware]</LargeAddressAware>
      <MapFileName>[options.MapFileName]</MapFileName>
      <ImportLibrary>[options.ImportLibrary]</ImportLibrary>
      <FunctionOrder>[options.FunctionOrder]</FunctionOrder>
      <ForceFileOutput>[options.ForceFileOutput]</ForceFileOutput>
      <ModuleDefinitionFile>[options.ModuleDefinitionFile]</ModuleDefinitionFile>
      <DelayLoadDLLs>[options.DelayLoadedDLLs]</DelayLoadDLLs>
      <BaseAddress>[options.BaseAddress]</BaseAddress>
      <UACExecutionLevel>[options.UACExecutionLevel]</UACExecutionLevel>
      <AllowIsolation>[options.AllowIsolation]</AllowIsolation>
      <GenerateWindowsMetadata>[options.GenerateWindowsMetadata]</GenerateWindowsMetadata>
      <WindowsMetadataFile>[options.WindowsMetadataFile]</WindowsMetadataFile>
      <TreatLinkerWarningAsErrors>[options.TreatLinkerWarningAsErrors]</TreatLinkerWarningAsErrors>
      <CreateHotPatchableImage>[options.LinkerCreateHotPatchableImage]</CreateHotPatchableImage>
    </Link>
";

        private const string _projectConfigurationsStaticLinkTemplate =
            @"    <Link>
      <GenerateDebugInformation>[options.LinkerGenerateDebugInformation]</GenerateDebugInformation>
      <FullProgramDatabaseFile>[options.FullProgramDatabaseFile]</FullProgramDatabaseFile>
      <EnableCOMDATFolding>[options.EnableCOMDATFolding]</EnableCOMDATFolding>
      <OptimizeReferences>[options.OptimizeReferences]</OptimizeReferences>
      <TreatLinkerWarningAsErrors>[options.TreatLinkerWarningAsErrors]</TreatLinkerWarningAsErrors>
    </Link>
    <Lib>
      <TargetMachine>[options.TargetMachine]</TargetMachine>
      <SubSystem/>
      <LinkTimeCodeGeneration>[options.LinkTimeCodeGeneration]</LinkTimeCodeGeneration>
      <AdditionalOptions>[options.AdditionalLibrarianOptions]</AdditionalOptions>
      <TreatLibWarningAsErrors>[options.TreatLibWarningAsErrors]</TreatLibWarningAsErrors>
      <OutputFile>[options.OutputFile]</OutputFile>
      <AdditionalLibraryDirectories>[options.AdditionalLibraryDirectories]</AdditionalLibraryDirectories>
      <AdditionalDependencies>[options.AdditionalDependencies]</AdditionalDependencies>
    </Lib>
";

        private const string _userFileConfigurationGeneralTemplate =
            @"    <LocalDebuggerCommand>[conf.VcxprojUserFile.LocalDebuggerCommand]</LocalDebuggerCommand>
    <LocalDebuggerCommandArguments>[conf.VcxprojUserFile.LocalDebuggerCommandArguments]</LocalDebuggerCommandArguments>
    <LocalDebuggerEnvironment>[conf.VcxprojUserFile.LocalDebuggerEnvironment]</LocalDebuggerEnvironment>
    <LocalDebuggerWorkingDirectory>[conf.VcxprojUserFile.LocalDebuggerWorkingDirectory]</LocalDebuggerWorkingDirectory>
    <LocalDebuggerAttach>[conf.VcxprojUserFile.LocalDebuggerAttachString]</LocalDebuggerAttach>
    <DebuggerFlavor>WindowsLocalDebugger</DebuggerFlavor>
";

        private const string _projectConfigurationsGeneral =
            @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" Label=""Configuration"">
    <ConfigurationType>[options.ConfigurationType]</ConfigurationType>
    <UseDebugLibraries>[options.UseDebugLibraries]</UseDebugLibraries>
    <PreferredToolArchitecture>[options.PreferredToolArchitecture]</PreferredToolArchitecture>
    <TranslateIncludes>[options.TranslateIncludes]</TranslateIncludes>
    <CharacterSet>[options.CharacterSet]</CharacterSet>
    <UseOfMfc>[options.UseOfMfc]</UseOfMfc>
    <WholeProgramOptimization>[options.WholeProgramOptimization]</WholeProgramOptimization>
    <PlatformToolset>[options.PlatformToolset]</PlatformToolset>
    <TrackFileAccess>[options.TrackFileAccess]</TrackFileAccess>
    <CLRSupport>[options.CLRSupport]</CLRSupport>
    <WindowsTargetPlatformVersion>[options.WindowsTargetPlatformVersion]</WindowsTargetPlatformVersion>
    <SpectreMitigation>[options.SpectreMitigation]</SpectreMitigation>
    <EnableASAN>[options.EnableASAN]</EnableASAN>
    <EnableUnitySupport>[options.JumboBuild]</EnableUnitySupport>
  </PropertyGroup>
";

        private const string _projectConfigurationsGeneral2 =
            @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <TargetName>[options.OutputFileName]</TargetName>
    <OutDir>[options.OutputDirectory]\</OutDir>
    <IntDir>[options.IntermediateDirectory]\</IntDir>
    <TargetExt>[options.OutputFileExtension]</TargetExt>
    <GenerateManifest>[options.GenerateManifest]</GenerateManifest>
    <PostBuildEventUseInBuild>[options.PostBuildEventEnable]</PostBuildEventUseInBuild>
    <PreBuildEventUseInBuild>[options.PreBuildEventEnable]</PreBuildEventUseInBuild>
    <PreLinkEventUseInBuild>[options.PreLinkEventEnable]</PreLinkEventUseInBuild>
    <LinkIncremental>[options.LinkIncremental]</LinkIncremental>
    <OutputFile>[options.OutputFile]</OutputFile>
    <EmbedManifest>[options.EmbedManifest]</EmbedManifest>
    <IgnoreImportLibrary>[options.IgnoreImportLibrary]</IgnoreImportLibrary>
    <RunCodeAnalysis>[options.RunCodeAnalysis]</RunCodeAnalysis>
    <EnableMicrosoftCodeAnalysis>[options.MicrosoftCodeAnalysis]</EnableMicrosoftCodeAnalysis>
    <EnableClangTidyCodeAnalysis>[options.ClangTidyCodeAnalysis]</EnableClangTidyCodeAnalysis>
    <CAExcludepath>[options.CAexcludePaths];%(CAExcludepath)</CAExcludepath>
    <CustomBuildBeforeTargets>[options.CustomBuildStepBeforeTargets]</CustomBuildBeforeTargets>
    <CustomBuildAfterTargets>[options.CustomBuildStepAfterTargets]</CustomBuildAfterTargets>
    <ExecutablePath>[options.ExecutablePath]</ExecutablePath>
    <IncludePath>[options.IncludePath]</IncludePath>
    <ExternalIncludePath>[options.ExternalIncludePath]</ExternalIncludePath>
    <ReferencePath>[options.ReferencePath]</ReferencePath>
    <LibraryPath>[options.LibraryPath]</LibraryPath>
    <LibraryWPath>[options.LibraryWPath]</LibraryWPath>
    <SourcePath>[options.SourcePath]</SourcePath>
    <ExcludePath>[options.ExcludePath]</ExcludePath>
    <PublicIncludeDirectories>[options.PublicIncludeDirectories]</PublicIncludeDirectories>
    <AllProjectIncludesArePublic>[options.AllProjectIncludesArePublic]</AllProjectIncludesArePublic>
    <PublicModuleDirectories>[options.PublicModuleDirectories]</PublicModuleDirectories>
    <AllProjectBMIsArePublic>[options.AllProjectBMIsArePublic]</AllProjectBMIsArePublic>
    <DisableFastUpToDateCheck>[options.DisableFastUpToDateCheck]</DisableFastUpToDateCheck>
    <EnableManagedIncrementalBuild>[options.EnableManagedIncrementalBuild]</EnableManagedIncrementalBuild>
    <UseClangCl>[options.UseClangCl]</UseClangCl>
    <UseLldLink>[options.UseLldLink]</UseLldLink>
    <CopyLocalDeploymentContent>[options.CopyLocalDeploymentContent]</CopyLocalDeploymentContent>
    <CopyLocalProjectReference>[options.CopyLocalProjectReference]</CopyLocalProjectReference>
    <CopyLocalDebugSymbols>[options.CopyLocalDebugSymbols]</CopyLocalDebugSymbols>
    <CopyCppRuntimeToOutputDir>[options.CopyCppRuntimeToOutputDir]</CopyCppRuntimeToOutputDir>
  </PropertyGroup>
";

        private const string _windowsSDKOverridesBegin =
            @"  <PropertyGroup Label=""Globals"">
    <UCRTContentRoot>[UCRTContentRoot]</UCRTContentRoot>
    <UniversalCRTSdkDir_10>[UniversalCRTSdkDir_10]</UniversalCRTSdkDir_10>
    <[windowsSdkDirKey]>[windowsSdkDirValue]</[windowsSdkDirKey]>
    <WindowsSdkDir>$([windowsSdkDirKey])</WindowsSdkDir>
    <WindowsTargetPlatformVersion>[targetPlatformVersion]</WindowsTargetPlatformVersion>
";
        private const string _windowsSDKOverridesEnd =
            @"  </PropertyGroup>
";

        // Notes:
        // Clean:
        // Visual Studio automatically cleans most files from the intermediate directory as soon as the clean command is active. However, it doesn't
        // clean them all, so we manually delete everything!
        // Also, it doesn't erase the target file and its related file extensions. We now clean them manually. Feel free to add more extensions if some were forgotten.
        // Removing unity blobs is useful if the number of blobs changes this will erase the ones that are no longer used.
        // Implementation note:
        // del *.* doesn't work and Visual studio seems to have a protection against this so we must delete explicit files or extensions...
        private const string _projectConfigurationsFastBuildMakefile =
            @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <OutDir>[options.OutputDirectory]\</OutDir>
    <IntDir>[options.IntermediateDirectory]\</IntDir>
    <NMakeBuildCommandLine>cd [fastBuildWorkingDirectory]
[conf.FastBuildCustomActionsBeforeBuildCommand]
[fastBuildMakeCommandBuild] </NMakeBuildCommandLine>
    <NMakeReBuildCommandLine>cd [fastBuildWorkingDirectory]
[conf.FastBuildCustomActionsBeforeBuildCommand]
[fastBuildMakeCommandRebuild] </NMakeReBuildCommandLine>
    <NMakeCleanCommandLine>del ""[options.IntermediateDirectory]\*unity*.cpp"" &gt;NUL 2&gt;NUL
del ""[options.IntermediateDirectory]\*.obj"" &gt;NUL 2&gt;NUL
del ""[options.IntermediateDirectory]\*.a"" &gt;NUL 2&gt;NUL
del ""[options.IntermediateDirectory]\*.lib"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].exe"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].elf"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].exp"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].ilk"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].lib"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].pdb"" &gt;NUL 2&gt;NUL</NMakeCleanCommandLine>
    <NMakeOutput>[options.OutputFile]</NMakeOutput>
    <NMakePreprocessorDefinitions>[EscapeXML:options.PreprocessorDefinitions][EscapeXML:options.IntellisenseAdditionalDefines]</NMakePreprocessorDefinitions>
    <NMakeIncludeSearchPath>[options.NMakeIncludeSearchPath]</NMakeIncludeSearchPath>
    <NMakeForcedIncludes>[options.ForcedIncludeFiles]</NMakeForcedIncludes>
    <AdditionalOptions>[options.IntellisenseCommandLineOptions]</AdditionalOptions>
  </PropertyGroup>
  <ItemDefinitionGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <NMakeCompile>
      <NMakeCompileFileCommandLine>cd [fastBuildWorkingDirectory]
[conf.FastBuildCustomActionsBeforeBuildCommand]
[fastBuildMakeCommandCompileFile] </NMakeCompileFileCommandLine>
    </NMakeCompile>
  </ItemDefinitionGroup>
";

        private const string _projectConfigurationsCustomMakefile =
            @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <OutDir>[options.OutputDirectory]\</OutDir>
    <IntDir>[options.IntermediateDirectory]\</IntDir>
    <NMakeBuildCommandLine>[conf.CustomBuildSettings.BuildCommand]</NMakeBuildCommandLine>
    <NMakeReBuildCommandLine>[conf.CustomBuildSettings.RebuildCommand]</NMakeReBuildCommandLine>
    <NMakeCleanCommandLine>[conf.CustomBuildSettings.CleanCommand]</NMakeCleanCommandLine>
    <NMakeOutput>[conf.CustomBuildSettings.OutputFile]</NMakeOutput>
    <NMakePreprocessorDefinitions>[EscapeXML:options.PreprocessorDefinitions]</NMakePreprocessorDefinitions>
    <NMakeIncludeSearchPath>[options.NMakeIncludeSearchPath]</NMakeIncludeSearchPath>
    <NMakeForcedIncludes>[options.ForcedIncludeFiles]</NMakeForcedIncludes>
    <AdditionalOptions>[options.IntellisenseCommandLineOptions]</AdditionalOptions>
  </PropertyGroup>
  <ItemDefinitionGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <NMakeCompile>
      <NMakeCompileFileCommandLine>[conf.CustomBuildSettings.CompileFileCommand]</NMakeCompileFileCommandLine>
    </NMakeCompile>
  </ItemDefinitionGroup>
";

        private const string _projectConfigurationsNasmTemplate =
    @"    <NASM>
      <PreprocessorDefinitions>[EscapeXML:options.PreprocessorDefinitions];%(PreprocessorDefinitions);$(PreprocessorDefinitions)</PreprocessorDefinitions>
      <IncludePaths>[options.AdditionalAssemblyIncludeDirectories]</IncludePaths>
      <Path>[ExePath]</Path>
      <PreIncludeFiles>[PreIncludedFiles]</PreIncludeFiles>
    </NASM>
";
    }
}
