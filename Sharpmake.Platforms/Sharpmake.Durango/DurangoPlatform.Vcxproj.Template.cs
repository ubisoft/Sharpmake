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
namespace Sharpmake
{
    public static partial class Durango
    {
        public sealed partial class DurangoPlatform
        {
            private const string _projectDescriptionPlatformSpecific =
@"    <XdkEditionTarget>[xdkEditionTarget]</XdkEditionTarget>
    <DurangoXdkCompilers>[durangoXdkCompilers]</DurangoXdkCompilers>
    <DurangoXdkInstallPath>[durangoXdkInstallPath]</DurangoXdkInstallPath>
    <DurangoXdkKitPath>[durangoXdkKitPath]</DurangoXdkKitPath>
    <DurangoXdkTasks>[durangoXdkTasks]</DurangoXdkTasks>
    <TargetPlatformIdentifier>[targetPlatformIdentifier]</TargetPlatformIdentifier>
    <TargetPlatformSdkPath>[targetPlatformSdkPath]</TargetPlatformSdkPath>
    <XdkEditionRootVS2012>[xdkEditionRootVS2012]</XdkEditionRootVS2012>
    <XdkEditionRootVS2015>[xdkEditionRootVS2015]</XdkEditionRootVS2015>
    <XdkEditionRootVS2017>[xdkEditionRootVS2017]</XdkEditionRootVS2017>
    <EnableLegacyXdkHeaders>[enableLegacyXdkHeaders]</EnableLegacyXdkHeaders>
    <GameOSFilePath>[gameOSFilePath]</GameOSFilePath>
    <SDKReferenceDirectoryRoot>[sdkReferenceDirectoryRoot]</SDKReferenceDirectoryRoot>
";

            private const string _applicationEnvironment =
@"    <ApplicationEnvironment>title</ApplicationEnvironment>
";

            private const string _projectConfigurationsCompileTemplate =
                @"    <ClCompile>
      <PrecompiledHeader>[options.UsePrecompiledHeader]</PrecompiledHeader>
      <CompileAsWinRT>[options.CompileAsWinRT]</CompileAsWinRT>
      <WarningLevel>[options.WarningLevel]</WarningLevel>
      <Optimization>[options.Optimization]</Optimization>
      <PreprocessorDefinitions>[options.PreprocessorDefinitions];%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>[options.AdditionalIncludeDirectories]</AdditionalIncludeDirectories>
      <AdditionalUsingDirectories>$(Console_SdkPackagesRoot);$(Console_SdkWindowsMetadataPath);%(AdditionalUsingDirectories)</AdditionalUsingDirectories>
      <DebugInformationFormat>[options.DebugInformationFormat]</DebugInformationFormat>
      <CompileAsManaged>[clrSupport]</CompileAsManaged>
      <SuppressStartupBanner>true</SuppressStartupBanner>
      <TreatWarningAsError>[options.TreatWarningAsError]</TreatWarningAsError>
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
      <PreprocessSuppressLineNumbers>[options.KeepComments]</PreprocessSuppressLineNumbers>
      <PreprocessKeepComments>false</PreprocessKeepComments>
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
      <CreateHotpatchableImage>false</CreateHotpatchableImage>
      <DisableLanguageExtensions>[options.DisableLanguageExtensions]</DisableLanguageExtensions>
      <TreatWChar_tAsBuiltInType>[options.TreatWChar_tAsBuiltInType]</TreatWChar_tAsBuiltInType>
      <RemoveUnreferencedCodeData>[options.RemoveUnreferencedCodeData]</RemoveUnreferencedCodeData>
      <ForceConformanceInForLoopScope>[options.ForceConformanceInForLoopScope]</ForceConformanceInForLoopScope>
      <RuntimeTypeInfo>[options.RuntimeTypeInfo]</RuntimeTypeInfo>
      <OpenMPSupport>[options.OpenMP]</OpenMPSupport>
      <ExpandAttributedSource>false</ExpandAttributedSource>
      <AssemblerOutput>NoListing</AssemblerOutput>
      <GenerateXMLDocumentationFiles>false</GenerateXMLDocumentationFiles>
      <BrowseInformation>false</BrowseInformation>
      <CallingConvention>[options.CallingConvention]</CallingConvention>
      <CompileAs>Default</CompileAs>
      <DisableSpecificWarnings>[options.DisableSpecificWarnings]</DisableSpecificWarnings>
      <UndefinePreprocessorDefinitions>[options.UndefinePreprocessorDefinitions]</UndefinePreprocessorDefinitions>
      <AdditionalOptions>[options.AdditionalCompilerOptions]</AdditionalOptions>
      <PrecompiledHeaderFile>[options.PrecompiledHeaderThrough]</PrecompiledHeaderFile>
      <PrecompiledHeaderOutputFile>[options.PrecompiledHeaderFile]</PrecompiledHeaderOutputFile>
      <ForcedIncludeFiles>[options.ForcedIncludeFiles]</ForcedIncludeFiles>
      <ForcedUsingFiles>[options.ForcedUsingFiles]</ForcedUsingFiles>
      <ProgramDataBaseFileName>[options.CompilerProgramDatabaseFile]</ProgramDataBaseFileName>
    </ClCompile>
";

            private const string _projectConfigurationsLinkTemplate =
                @"    <Link>
      <SubSystem>[options.SubSystem]</SubSystem>
      <GenerateDebugInformation>[options.GenerateDebugInformation]</GenerateDebugInformation>
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
      <TurnOffAssemblyGeneration>false</TurnOffAssemblyGeneration>
      <TargetMachine>[options.TargetMachine]</TargetMachine>
      <Profile>false</Profile>
      <CLRImageType>Default</CLRImageType>
      <LinkErrorReporting>PromptImmediately</LinkErrorReporting>
      <AdditionalOptions>[options.AdditionalLinkerOptions]</AdditionalOptions>
      <AdditionalDependencies>[options.AdditionalDependencies]</AdditionalDependencies>
      <SuppressStartupBanner>[options.SuppressStartupBanner]</SuppressStartupBanner>
      <IgnoreAllDefaultLibraries>[options.IgnoreAllDefaultLibraries]</IgnoreAllDefaultLibraries>
      <IgnoreSpecificDefaultLibraries>[options.IgnoreDefaultLibraryNames]
      </IgnoreSpecificDefaultLibraries>
      <AssemblyDebug>[options.AssemblyDebug]</AssemblyDebug>
      <HeapReserveSize>[options.HeapReserveSize]</HeapReserveSize>
      <HeapCommitSize>[options.HeapCommitSize]</HeapCommitSize>
      <StackReserveSize>[options.StackReserveSize]</StackReserveSize>
      <StackCommitSize>[options.StackCommitSize]</StackCommitSize>
      <LargeAddressAware>true</LargeAddressAware>
      <MapFileName>[options.MapFileName]</MapFileName>
      <ImportLibrary>[options.ImportLibrary]</ImportLibrary>
      <FunctionOrder>[options.FunctionOrder]</FunctionOrder>
      <ForceFileOutput>[options.ForceFileOutput]</ForceFileOutput>
      <GenerateWindowsMetadata>[options.GenerateWindowsMetadata]</GenerateWindowsMetadata>
      <WindowsMetadataFile>[options.WindowsMetadataFile]</WindowsMetadataFile>
    </Link>
";

            private const string _userFileConfigurationGeneralTemplate =
                @"    <LocalDebuggerCommandArguments>[conf.VcxprojUserFile.LocalDebuggerCommandArguments]</LocalDebuggerCommandArguments>
    <DebuggerFlavor>XboxOneVCppDebugger</DebuggerFlavor>
";

            private const string _runFromPCDeployment =
                @"  <Target Name=""PrepareForLayout"">
    <ItemGroup>
      <LayoutSourceFiles Include=""$(FinalAppxManifestName)"" />
    </ItemGroup>
    <MakeDir Condition=""!Exists('$(LayoutDir)')"" Directories=""$(LayoutDir)"" />
    <Exec Command=""[DurangoRunFromPCDeploymentRegisterCommand]"" />
  </Target>
";

            private const string _projectConfigurationsGeneral2 =
                @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <TargetName>[options.OutputFileName]</TargetName>
    <UseDebugLibraries>[options.UseDebugLibraries]</UseDebugLibraries>
    <OutDir>[options.OutputDirectory]\</OutDir>
    <IntDir>[options.IntermediateDirectory]\</IntDir>
    <TargetExt>[options.OutputFileExtension]</TargetExt>
    <CharacterSet>[options.CharacterSet]</CharacterSet>
    <GenerateManifest>[options.GenerateManifest]</GenerateManifest>
    <PostBuildEventUseInBuild>[options.PostBuildEventEnable]</PostBuildEventUseInBuild>
    <PreBuildEventUseInBuild>[options.PreBuildEventEnable]</PreBuildEventUseInBuild>
    <PreLinkEventUseInBuild>[options.PreLinkEventEnable]</PreLinkEventUseInBuild>
    <LinkIncremental>[options.LinkIncremental]</LinkIncremental>
    <OutputFile>[options.OutputFile]</OutputFile>
    <IncludePath>[options.IncludePath]</IncludePath>
    <ReferencePath>[options.ReferencePath]</ReferencePath>
    <LibraryPath>[options.LibraryPath]</LibraryPath>
    <LibraryWPath>[options.LibraryWPath]</LibraryWPath>
    <CustomBuildBeforeTargets>[options.CustomBuildStepBeforeTargets]</CustomBuildBeforeTargets>
    <CustomBuildAfterTargets>[options.CustomBuildStepAfterTargets]</CustomBuildAfterTargets>
    <LayoutDir>[options.LayoutDir]</LayoutDir>
    <PullMappingFile>[options.PullMappingFile]</PullMappingFile>
    <DeployMode>[options.DeployMode]</DeployMode>
    <NetworkSharePath>[options.NetworkSharePath]</NetworkSharePath>
    <PullTemporaryFolder>[options.PullTemporaryFolder]</PullTemporaryFolder>
    <LayoutExtensionFilter>[options.LayoutExtensionFilter]</LayoutExtensionFilter>
    <UseClangCl>[options.UseClangCl]</UseClangCl>
    <UseLldLink>[options.UseLldLink]</UseLldLink>
  </PropertyGroup>
  <PropertyGroup>
    <IsolateConfigurationsOnDeploy>true</IsolateConfigurationsOnDeploy>
  </PropertyGroup>
";

            private const string _projectConfigurationsFastBuildMakefile =
                @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
        <LayoutDir>[options.LayoutDir]</LayoutDir>
        <PullMappingFile>[options.PullMappingFile]</PullMappingFile>
        <DeployMode>[options.DeployMode]</DeployMode>
        <NetworkSharePath>[options.NetworkSharePath]</NetworkSharePath>
        <LayoutExtensionFilter>[options.LayoutExtensionFilter]</LayoutExtensionFilter>
  </PropertyGroup>
      <PropertyGroup>
        <IsolateConfigurationsOnDeploy>true</IsolateConfigurationsOnDeploy>
  </PropertyGroup>
";

            private const string _sdkReferencesBegin =
                @"  <ItemGroup Condition=""'$(Platform)'=='Durango'"">
";

            private const string _sdkReferencesEnd =
                @"  </ItemGroup>
";

            private const string _sdkReference =
                @"    <SDKReference Include=""[sdkReferenceInclude]"" />
";

            private const string _projectPriResource =
                @"    <PRIResource Include=""[file.FileNameProjectRelative]""/>
";

            private const string _projectImgResource =
                @"    <Image Include=""[file.FileNameProjectRelative]""/>
";

            private const string _projectFilesXManifest =
                @"    <AppxManifest Include=""[file.FileNameProjectRelative]"">
      <FileType>Document</FileType>
    </AppxManifest>
";

            private const string _projectFilesBegin =
                @"  <ItemGroup>
";

            public static string _projectFilesEnd =
                @"  </ItemGroup>
";
        }
    }
}
