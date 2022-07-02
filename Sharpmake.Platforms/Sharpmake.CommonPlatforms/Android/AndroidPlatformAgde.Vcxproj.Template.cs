// Copyright (c) 2021-2022 Ubisoft Entertainment
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
    public static partial class Android
    {
        public sealed partial class AndroidAgdePlatform
        {
            private const string _projectStartPlatformConditional =
    @"  <PropertyGroup Label=""Globals"" Condition=""'$(Platform)'=='Android-arm64-v8a' Or '$(Platform)'=='Android-x86_64' Or '$(Platform)'=='Android-armeabi-v7a' Or '$(Platform)'=='Android-x86'"">
";

            private const string _projectDescriptionPlatformSpecific =
@"    <AndroidNdkDirectory>[ndkRoot]</AndroidNdkDirectory>
    <AndroidNdkVersion>[androidNdkVersion]</AndroidNdkVersion>
    <AndroidSdk>[androidHome]</AndroidSdk>
    <AndroidMinSdkVersion>[androidMinSdkVersion]</AndroidMinSdkVersion>
    <VS_JavaHome>[javaHome]</VS_JavaHome>
    <PlatformToolset>Clang</PlatformToolset>
";

            private const string _projectPropertySheets =
@"  <ImportGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|Android-arm64-v8a'"" Label=""PropertySheets"">
    <Import Project = ""$(AdditionalVCTargetsPath)Platforms\Android-arm64-v8a\Platform.default.props"" />
    <Import Project=""$(AdditionalVCTargetsPath)Platforms\Android-arm64-v8a\PlatformToolsets\Clang\Toolset.props"" />
  </ImportGroup>
  <ImportGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|Android-armeabi-v7a'"" Label=""PropertySheets"">
    <Import Project = ""$(AdditionalVCTargetsPath)Platforms\Android-armeabi-v7a\Platform.default.props"" />
    <Import Project=""$(AdditionalVCTargetsPath)Platforms\Android-armeabi-v7a\PlatformToolsets\Clang\Toolset.props"" />
  </ImportGroup>
  <ImportGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|Android-x86'"" Label=""PropertySheets"">
    <Import Project = ""$(AdditionalVCTargetsPath)Platforms\Android-x86\Platform.default.props"" />
    <Import Project=""$(AdditionalVCTargetsPath)Platforms\Android-x86\PlatformToolsets\Clang\Toolset.props"" />
  </ImportGroup>
  <ImportGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|Android-x86_64'"" Label=""PropertySheets"">
    <Import Project = ""$(AdditionalVCTargetsPath)Platforms\Android-x86_64\Platform.default.props"" />
    <Import Project=""$(AdditionalVCTargetsPath)Platforms\Android-x86_64\PlatformToolsets\Clang\Toolset.props"" />
  </ImportGroup>
";

            private const string _projectConfigurationsGeneralTemplate =
@"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" Label=""Configuration"">
    <ConfigurationType>[options.ConfigurationType]</ConfigurationType>
    <UseDebugLibraries>[options.UseDebugLibraries]</UseDebugLibraries>
    <PlatformToolset>[options.PlatformToolset]</PlatformToolset>
    <UseOfStl>[options.UseOfStl]</UseOfStl>
    <ThumbMode>[options.ThumbMode]</ThumbMode>
    <LinkTimeOptimization>[options.LinkTimeOptimization]</LinkTimeOptimization>
    <ClangLinkType>[options.ClangLinkType]</ClangLinkType>
    <CppLanguageStandard>[options.CppLanguageStandard]</CppLanguageStandard>
    <CLanguageStandard>[options.CLanguageStandard]</CLanguageStandard>
    <AndroidApkLocation>[options.AndroidApkLocation]</AndroidApkLocation>
  </PropertyGroup>
";

            // The output directory is converted to a rooted path by prefixing it with $(ProjectDir) to work around
            // an issue with VS Android build scripts. When a project dependency has its project folder not at the
            // same folder level as the AndroidPackageProject, VS can't locate its output properly using its relative path.
            private const string _projectConfigurationsGeneral2Template =
@"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <TargetName>[options.OutputFileName]</TargetName>
    <OutDir>$(ProjectDir)[options.OutputDirectory]\</OutDir>
    <IntDir>[options.IntermediateDirectory]\</IntDir>
    <TargetExt>[options.OutputFileExtension]</TargetExt>
    <PostBuildEventUseInBuild>[options.PostBuildEventEnable]</PostBuildEventUseInBuild>
    <PreBuildEventUseInBuild>[options.PreBuildEventEnable]</PreBuildEventUseInBuild>
    <PreLinkEventUseInBuild>[options.PreLinkEventEnable]</PreLinkEventUseInBuild>
    <OutputFile>[options.OutputFile]</OutputFile>
    <CustomBuildBeforeTargets>[options.CustomBuildStepBeforeTargets]</CustomBuildBeforeTargets>
    <CustomBuildAfterTargets>[options.CustomBuildStepAfterTargets]</CustomBuildAfterTargets>
    <ExecutablePath>[options.ExecutablePath]</ExecutablePath>
    <IncludePath>[options.IncludePath]</IncludePath>
    <LibraryPath>[options.LibraryPath]</LibraryPath>
    <ExcludePath>[options.ExcludePath]</ExcludePath>
    <UseMultiToolTask>[options.UseMultiToolTask]</UseMultiToolTask>
    <AndroidEnablePackaging>[options.AndroidEnablePackaging]</AndroidEnablePackaging>
    <AndroidApplicationModule>[options.AndroidApplicationModule]</AndroidApplicationModule>
    <AndroidGradleBuildDir>[options.AndroidGradleBuildDir]</AndroidGradleBuildDir>
    <AndroidGradleBuildOutputDir>[options.AndroidGradleBuildIntermediateDir]</AndroidGradleBuildOutputDir>
    <AndroidExtraGradleArgs>[options.AndroidExtraGradleArgs]</AndroidExtraGradleArgs>
    <AndroidApkName>[options.AndroidApkName]</AndroidApkName>
  </PropertyGroup>
";

            private const string _projectConfigurationsCompileTemplate =
@"    <ClCompile>
      <PrecompiledHeader>[options.UsePrecompiledHeader]</PrecompiledHeader>
      <WarningLevel>[options.WarningLevel]</WarningLevel>
      <Optimization>[options.Optimization]</Optimization>
      <PreprocessorDefinitions>[options.PreprocessorDefinitions];%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>[options.AdditionalIncludeDirectories];%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
      <ClangDebugInformationFormat>[options.ClangDebugInformationFormat]</ClangDebugInformationFormat>
      <LimitDebugInfo>[options.LimitDebugInfo]</LimitDebugInfo>
      <FloatABI>[options.FloatABI]</FloatABI>
      <TreatWarningAsError>[options.TreatWarningAsError]</TreatWarningAsError>
      <OmitFramePointers>[options.OmitFramePointers]</OmitFramePointers>
      <UndefineAllPreprocessorDefinitions>false</UndefineAllPreprocessorDefinitions>
      <ExceptionHandling>[options.ExceptionHandling]</ExceptionHandling>
      <StackProtectionLevel>[options.StackProtectionLevel]</StackProtectionLevel>
      <FunctionLevelLinking>[options.EnableFunctionLevelLinking]</FunctionLevelLinking>
      <DataLevelLinking>[options.EnableDataLevelLinking]</DataLevelLinking>
      <RuntimeTypeInfo>[options.RuntimeTypeInfo]</RuntimeTypeInfo>
      <AssemblerOutput>NoListing</AssemblerOutput>
      <CompileAs>Default</CompileAs>
      <UndefinePreprocessorDefinitions>[options.UndefinePreprocessorDefinitions]</UndefinePreprocessorDefinitions>
      <AdditionalOptions>[options.AdditionalCompilerOptions]</AdditionalOptions>
      <PrecompiledHeaderFile>[options.PrecompiledHeaderThrough]</PrecompiledHeaderFile>
      <ShowIncludes>[options.ShowIncludes]</ShowIncludes>
      <ForcedIncludeFiles>[options.ForcedIncludeFiles]</ForcedIncludeFiles>
      <UnwindTables>[options.UnwindTables]</UnwindTables>
      <AddressSignificanceTable>[options.AddressSignificanceTable]</AddressSignificanceTable>
      <ClangDiagnosticsFormat>[options.ClangDiagnosticsFormat]</ClangDiagnosticsFormat>
      <PositionIndependentCode>[options.PositionIndependentCode]</PositionIndependentCode>
    </ClCompile>
";

            private const string _projectConfigurationsSharedLinkTemplate =
@"    <Link>
      <DebuggerSymbolInformation>[options.DebuggerSymbolInformation]</DebuggerSymbolInformation>
      <OutputFile>[options.OutputFile]</OutputFile>
      <AdditionalLibraryDirectories>[options.AdditionalLibraryDirectories];%(AdditionalLibraryDirectories)</AdditionalLibraryDirectories>
      <AdditionalOptions>[options.AdditionalLinkerOptions]</AdditionalOptions>
      <AdditionalDependencies>[options.AdditionalDependencies];%(AdditionalDependencies);</AdditionalDependencies>
      <IgnoreSpecificDefaultLibraries>[options.IgnoreDefaultLibraryNames]</IgnoreSpecificDefaultLibraries>
      <GenerateMapFile>[options.MapFileName]</GenerateMapFile>
      <IncrementalLink>[options.IncrementalLink]</IncrementalLink>
      <FunctionBinding>[options.FunctionBinding]</FunctionBinding>
      <NoExecStackRequired>[options.NoExecStackRequired]</NoExecStackRequired>
      <UnresolvedSymbolReferences>[options.UnresolvedSymbolReferences]</UnresolvedSymbolReferences>
      <Relocation>[options.Relocation]</Relocation>
    </Link>
";

            private const string _projectConfigurationsStaticLinkTemplate =
@"    <Lib>
      <AdditionalOptions>[options.AdditionalLibrarianOptions]</AdditionalOptions>
      <OutputFile>[options.OutputFile]</OutputFile>
    </Lib>
";
        }
    }
}
