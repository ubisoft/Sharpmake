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
    public static partial class NvShield
    {
        public sealed partial class NvShieldPlatform
        {
            private const string _projectConfigurationsCompileTemplate =
                @"    <ClCompile>
      <AdditionalIncludeDirectories>[options.AdditionalIncludeDirectories]</AdditionalIncludeDirectories>
      <GenerateDebugInformation>[options.GenerateDebugInformation]</GenerateDebugInformation>
      <Warnings>[options.Warnings]</Warnings>
      <WarningsAsErrors>[options.WarningsAsErrors]</WarningsAsErrors>
      <EchoCommandLines>[options.EchoCommandLinesCompiler]</EchoCommandLines>
      <EchoIncludedHeaders>[options.EchoIncludedHeaders]</EchoIncludedHeaders>
      <ProcessMax>[options.ProcessorNumber]</ProcessMax>
      <OptimizationLevel>[options.OptimizationLevel]</OptimizationLevel>
      <StrictAliasing>[options.StrictAliasing]</StrictAliasing>
      <UnswitchLoops>[options.UnswitchLoops]</UnswitchLoops>
      <InlineLimit>[options.InlineLimit]</InlineLimit>
      <OmitFramePointer>[options.OmitFramePointers]</OmitFramePointer>
      <FunctionSections>[options.FunctionSections]</FunctionSections>
      <PreprocessorDefinitions>[options.PreprocessorDefinitions];%(PreprocessorDefinitions);</PreprocessorDefinitions>
      <ThumbMode>[options.ThumbMode]</ThumbMode>
      <FloatAbi>[options.FloatAbi]</FloatAbi>
      <PositionIndependentCode>[options.PositionIndependentCode]</PositionIndependentCode>
      <StackProtector>[options.StackProtector]</StackProtector>
      <FpuNeon>[options.FpuNeon]</FpuNeon>
      <GccExceptionHandling>[options.GccExceptionHandling]</GccExceptionHandling>
      <RuntimeTypeInfo>[options.RuntimeTypeInfo]</RuntimeTypeInfo>
      <ShortEnums>[options.ShortEnums]</ShortEnums>
      <SignedChar>[options.SignedChar]</SignedChar>
      <CLanguageStandard>[options.CLanguageStandard]</CLanguageStandard>
      <CppLanguageStandard>[options.CppLanguageStandard]</CppLanguageStandard>
      <PrecompiledHeader>[options.UsePrecompiledHeader]</PrecompiledHeader>
      <PrecompiledHeaderFile>[options.PrecompiledHeaderThrough]</PrecompiledHeaderFile>
      <AdditionalOptions>[options.AdditionalCompilerOptions] %(AdditionalOptions)</AdditionalOptions>
    </ClCompile>
";

            private const string _projectConfigurationsStaticLinkTemplate =
                @"    <Lib>
      <OutputFile>[options.OutputFile]</OutputFile>
      <AdditionalLibraryDirectories>[options.AdditionalLibraryDirectories]</AdditionalLibraryDirectories>
      <AdditionalDependencies>[options.AdditionalDependencies]</AdditionalDependencies>
      <ThinArchive>[options.ThinArchive]</ThinArchive>
      <EchoCommandLines>[options.EchoCommandLinesLinker]</EchoCommandLines>
    </Lib>
";

            private const string _projectConfigurationsLinkTemplate =
                @"    <Link>
      <OutputFile>[options.OutputFile]</OutputFile>
      <AdditionalLibraryDirectories>[options.AdditionalLibraryDirectories]</AdditionalLibraryDirectories>
      <EchoCommandLines>[options.EchoCommandLinesLinker]</EchoCommandLines>
      <AdditionalDependencies>[options.AdditionalDependencies]</AdditionalDependencies>
      <IgnoreAllDefaultLibraries>[options.IgnoreAllDefaultLibraries]</IgnoreAllDefaultLibraries>
      <AndroidSystemLibs>[options.AndroidSystemLibs]</AndroidSystemLibs>
      <LinkGccLibThumb>[options.LinkGccLibThumb]</LinkGccLibThumb>
      <ReportUndefinedSymbols>[options.ReportUndefinedSymbols]</ReportUndefinedSymbols>
      UseLinker>[options.UseLinker]</UseLinker>
      <AdditionalOptions>[options.AdditionalLinkerOptions] %(AdditionalOptions)</AdditionalOptions>
    </Link>
";

            private const string _nvShieldSdkDeclarationTemplate =
                @"  <PropertyGroup Label=""NsightTegraProject"">
    <NsightTegraProjectRevisionNumber>11</NsightTegraProjectRevisionNumber>
  </PropertyGroup>
";

            private const string _projectConfigurationsGeneral =
                @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" Label=""Configuration"">
    <ConfigurationType>[options.ConfigurationType]</ConfigurationType>
    <AndroidMinAPI>android-23</AndroidMinAPI>
    <AndroidTargetAPI>android-23</AndroidTargetAPI>
    <AndroidNativeAPI>UseTarget</AndroidNativeAPI>
    <AndroidArch>arm64-v8a</AndroidArch>
    <NdkToolchainVersion>DefaultClang</NdkToolchainVersion>
  </PropertyGroup>
";

            public static string _projectConfigurationsGeneral2 =
                @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <TargetName>[options.OutputFileName]</TargetName>
    <OutDir>[options.OutputDirectory]\</OutDir>
    <IntDir>[options.IntermediateDirectory]\</IntDir>
    <PostBuildEventUseInBuild>[options.PostBuildEventEnable]</PostBuildEventUseInBuild>
    <PreBuildEventUseInBuild>[options.PreBuildEventEnable]</PreBuildEventUseInBuild>
    <PreLinkEventUseInBuild>[options.PreLinkEventEnable]</PreLinkEventUseInBuild>
  </PropertyGroup>
";
        }
    }
}
