// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
    <AndroidPreApkInstallCommands>[options.AndroidPreApkInstallCommands]</AndroidPreApkInstallCommands>
    <AndroidPostApkInstallCommands>[options.AndroidPostApkInstallCommands]</AndroidPostApkInstallCommands>
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
    <NativeBuildBackend>[options.NativeBuildBackend]</NativeBuildBackend>
    <AndroidEnablePackaging>[options.AndroidEnablePackaging]</AndroidEnablePackaging>
    <SkipAndroidPackaging>[options.SkipAndroidPackaging]</SkipAndroidPackaging>
    <AndroidApplicationModule>[options.AndroidApplicationModule]</AndroidApplicationModule>
    <AndroidGradleBuildDir>[options.AndroidGradleBuildDir]</AndroidGradleBuildDir>
    <AndroidGradleBuildOutputDir>[options.AndroidGradleBuildIntermediateDir]</AndroidGradleBuildOutputDir>
    <AndroidExtraGradleArgs>[options.AndroidExtraGradleArgs]</AndroidExtraGradleArgs>
    <AndroidApkName>[options.AndroidApkName]</AndroidApkName>
    <AndroidGradlePackageOutputName>[options.AndroidGradlePackageOutputName]</AndroidGradlePackageOutputName>
  </PropertyGroup>
";

            private const string _projectConfigurationsFastBuildMakefile =
                @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <AndroidApkName>[options.AndroidApkName]</AndroidApkName>
  </PropertyGroup>
";

            private const string _projectConfigurationsCompileTemplate =
@"    <ClCompile>
      <PrecompiledHeader>[options.UsePrecompiledHeader]</PrecompiledHeader>
      <WarningLevel>[options.WarningLevel]</WarningLevel>
      <Optimization>[options.Optimization]</Optimization>
      <PreprocessorDefinitions>[EscapeXML:options.PreprocessorDefinitions];%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>[options.AdditionalIncludeDirectories];%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
      <ISystem>[options.AdditionalPlatformIncludeDirectories]</ISystem>
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
      <AdditionalOptions>[options.AllAdditionalCompilerOptions]</AdditionalOptions>
      <PrecompiledHeaderFile>[options.PrecompiledHeaderThrough]</PrecompiledHeaderFile>
      <PrecompiledHeaderOutputFileDirectory>[options.PrecompiledHeaderOutputFileDirectory]</PrecompiledHeaderOutputFileDirectory>
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
      <NoWarnOnCreate>true</NoWarnOnCreate>
    </Lib>
";
        }
    }
}
