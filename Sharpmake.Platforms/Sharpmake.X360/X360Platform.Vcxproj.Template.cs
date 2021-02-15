// Copyright (c) 2017, 2020 Ubisoft Entertainment
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
    public static partial class X360
    {
        public sealed partial class X360Platform
        {
            private const string _projectConfigurationsCompileTemplate =
                @"    <ClCompile>
      <PrecompiledHeader>[options.UsePrecompiledHeader]</PrecompiledHeader>
      <WarningLevel>[options.WarningLevel]</WarningLevel>
      <Optimization>[options.Optimization]</Optimization>
      <PreprocessorDefinitions>[options.PreprocessorDefinitions]</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>[options.AdditionalIncludeDirectories]</AdditionalIncludeDirectories>
      <AdditionalUsingDirectories>
      </AdditionalUsingDirectories>
      <DebugInformationFormat>[options.DebugInformationFormat]</DebugInformationFormat>
      <ProgramDataBaseFileName>[options.LinkerProgramDatabaseFile]</ProgramDataBaseFileName>
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
      <ForceConformanceInForLoopScope>[options.ForceConformanceInForLoopScope]</ForceConformanceInForLoopScope>
      <RuntimeTypeInfo>[options.RuntimeTypeInfo]</RuntimeTypeInfo>
      <OpenMPSupport>[options.OpenMP]</OpenMPSupport>
      <ExpandAttributedSource>false</ExpandAttributedSource>
      <AssemblerOutput>NoListing</AssemblerOutput>
      <GenerateXMLDocumentationFiles>false</GenerateXMLDocumentationFiles>
      <BrowseInformation>false</BrowseInformation>
      <CallingConvention>[options.CallingConvention]</CallingConvention>
      <CompileAs>Default</CompileAs>
      <DisableSpecificWarnings>[options.DisableSpecificWarnings]
      </DisableSpecificWarnings>
      <UndefinePreprocessorDefinitions>[options.UndefinePreprocessorDefinitions]
      </UndefinePreprocessorDefinitions>
      <AdditionalOptions>[options.AdditionalCompilerOptions]</AdditionalOptions>
      <PrecompiledHeaderFile>[options.PrecompiledHeaderThrough]</PrecompiledHeaderFile>
      <PrecompiledHeaderOutputFile>[options.PrecompiledHeaderFile]</PrecompiledHeaderOutputFile>
      <PreschedulingOptimization>[options.X360Prescheduling]</PreschedulingOptimization>
      <CallAttributedProfiling>[options.X360CallAttributedProfiling]</CallAttributedProfiling>
    </ClCompile>
";

            private const string _projectConfigurationsLinkSharedTemplate =
                @"    <Link>
      <GenerateDebugInformation>[options.LinkerGenerateDebugInformation]</GenerateDebugInformation>
      <OutputFile>[options.OutputFile]</OutputFile>
      <ShowProgress>[options.ShowProgress]</ShowProgress>
      <AdditionalLibraryDirectories>[options.AdditionalLibraryDirectories]</AdditionalLibraryDirectories>
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
      <SetChecksum>[options.X360SetChecksum]</SetChecksum>
      <RandomizedBaseAddress>[options.RandomizedBaseAddress]</RandomizedBaseAddress>
      <TurnOffAssemblyGeneration>false</TurnOffAssemblyGeneration>
      <TargetMachine>[options.TargetMachine]</TargetMachine>
      <Profile>false</Profile>
      <CLRImageType>Default</CLRImageType>
      <LinkErrorReporting>PromptImmediately</LinkErrorReporting>
      <AdditionalOptions>[options.AdditionalLinkerOptions]</AdditionalOptions>
      <AdditionalDependencies>[options.AdditionalDependencies];%(AdditionalDependencies)</AdditionalDependencies>
      <SuppressStartupBanner>[options.SuppressStartupBanner]</SuppressStartupBanner>
      <IgnoreAllDefaultLibraries>[options.IgnoreAllDefaultLibraries]</IgnoreAllDefaultLibraries>
      <IgnoreSpecificDefaultLibraries>[options.IgnoreDefaultLibraryNames]</IgnoreSpecificDefaultLibraries>
      <AssemblyDebug>[options.AssemblyDebug]</AssemblyDebug>
      <HeapReserveSize>[options.HeapReserveSize]</HeapReserveSize>
      <HeapCommitSize>[options.HeapCommitSize]</HeapCommitSize>
      <StackReserveSize>[options.StackReserveSize]</StackReserveSize>
      <StackCommitSize>[options.StackCommitSize]</StackCommitSize>
      <LargeAddressAware>true</LargeAddressAware>
      <MapFileName>[options.MapFileName]</MapFileName>
      <ImportLibrary>[options.ImportLibrary]</ImportLibrary>
      <FunctionOrder>[options.FunctionOrder]</FunctionOrder>
    </Link>
    <ImageXex>
      <AdditionalSections>[options.X360AdditionalSections]</AdditionalSections>
      <ConfigurationFile>[options.X360ProjectDefaults]</ConfigurationFile>
      <Pal50Incompatible>[options.X360Pal50Incompatible]</Pal50Incompatible>
    </ImageXex>
    <Deploy>
      <DeploymentType>CopyToHardDrive</DeploymentType>
    </Deploy>
    <Deploy>
      <SuppressStartupBanner>true</SuppressStartupBanner>
    </Deploy>
    <Deploy>
      <ExcludedFromBuild>false</ExcludedFromBuild>
      <Progress>false</Progress>
      <ForceCopy>false</ForceCopy>
      <DeploymentFiles>$(RemoteRoot)=$(ImagePath);[options.AdditionalDeploymentFolders]</DeploymentFiles>
      <DvdEmulationType>ZeroSeekTimes</DvdEmulationType>
      <LayoutFile>[options.X360LayoutFile]</LayoutFile>
    </Deploy>
";

            private const string _projectConfigurationsStaticLinkTemplate =
                @"    <Link>
      <GenerateDebugInformation>[options.LinkerGenerateDebugInformation]</GenerateDebugInformation>
      <EnableCOMDATFolding>[options.EnableCOMDATFolding]</EnableCOMDATFolding>
      <OptimizeReferences>[options.OptimizeReferences]</OptimizeReferences>
    </Link>
    <Lib>
      <TargetMachine>[options.TargetMachine]</TargetMachine>
    </Lib>
    <Lib>
      <SubSystem>
      </SubSystem>
    </Lib>
    <Lib>
      <LinkTimeCodeGeneration>[options.LinkTimeCodeGeneration]</LinkTimeCodeGeneration>
      <AdditionalOptions>[options.AdditionalLibrarianOptions]</AdditionalOptions>
      <OutputFile>[options.OutputFile]</OutputFile>
    </Lib>
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
    <ImageXexOutput>[options.ImageXexOutput]</ImageXexOutput>
    <OutputFile>[options.OutputFile]</OutputFile>
    <EmbedManifest>[options.EmbedManifest]</EmbedManifest>
    <IgnoreImportLibrary>[options.IgnoreImportLibrary]</IgnoreImportLibrary>
    <RunCodeAnalysis>[options.RunCodeAnalysis]</RunCodeAnalysis>
    <RemoteRoot>[options.X360RemotePath]</RemoteRoot>
    <CustomBuildBeforeTargets>[options.CustomBuildStepBeforeTargets]</CustomBuildBeforeTargets>
    <CustomBuildAfterTargets>[options.CustomBuildStepAfterTargets]</CustomBuildAfterTargets>
    <ExecutablePath>[options.ExecutablePath]</ExecutablePath>
    <IncludePath>[options.IncludePath]</IncludePath>
    <LibraryPath>[options.LibraryPath]</LibraryPath>
    <ExcludePath>[options.ExcludePath]</ExcludePath>
    <DisableFastUpToDateCheck>[options.DisableFastUpToDateCheck]</DisableFastUpToDateCheck>
  </PropertyGroup>
";
        }
    }
}
