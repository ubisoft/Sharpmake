// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public static partial class Linux
    {
        public sealed partial class LinuxPlatform
        {
            private const string _projectStartPlatformConditional =
@"  <PropertyGroup Label=""Globals"" Condition=""'$(Platform)'=='[platformName]' and ([configurationsConditional])"">
";
            private const string _projectConfigurationsCompileTemplate =
                @"    <ClCompile>
      <PrecompiledHeader>[options.UsePrecompiledHeader]</PrecompiledHeader>
      <PreprocessorDefinitions>[EscapeXML:options.PreprocessorDefinitions];%(PreprocessorDefinitions);</PreprocessorDefinitions>
      <ForcedIncludeFiles>[options.ForcedIncludeFiles]</ForcedIncludeFiles>
      <DebugInformationFormat>[options.DebugInformationFormat]</DebugInformationFormat>
      <Warnings>[options.Warnings]</Warnings>
      <ExtraWarnings>[options.ExtraWarnings]</ExtraWarnings>
      <WarningsAsErrors>[options.WarningsAsErrors]</WarningsAsErrors>
      <MultiProcessorCompilation>[options.MultiProcessorCompilation]</MultiProcessorCompilation>
      <ProcessorNumber>[options.ProcessorNumber]</ProcessorNumber>
      <Distributable>[options.Distributable]</Distributable>
      <OptimizationLevel>[options.OptimizationLevel]</OptimizationLevel>
      <PositionIndependentCode>[options.PositionIndependentCode]</PositionIndependentCode>
      <FastMath>[options.FastMath]</FastMath>
      <NoStrictAliasing>[options.NoStrictAliasing]</NoStrictAliasing>
      <UnrollLoops>[options.UnrollLoops]</UnrollLoops>
      <AnsiCompliance>[options.AnsiCompliance]</AnsiCompliance>
      <CharUnsigned>[options.CharUnsigned]</CharUnsigned>
      <MsExtensions>[options.MsExtensions]</MsExtensions>
      <RuntimeTypeInfo>[options.RuntimeTypeInfo]</RuntimeTypeInfo>
      <AdditionalIncludeDirectories>[options.AdditionalIncludeDirectories]</AdditionalIncludeDirectories>
      <AdditionalOptions>[options.AllAdditionalCompilerOptions] %(AdditionalOptions)</AdditionalOptions>
      <PrecompiledHeaderFile>[options.PrecompiledHeaderThrough]</PrecompiledHeaderFile>
      <PrecompiledHeaderOutputFile>[options.PrecompiledHeaderFile]</PrecompiledHeaderOutputFile>
      <CompileAs>Default</CompileAs>
      <LinkTimeOptimization>[options.LinkTimeOptimization]</LinkTimeOptimization>
      <InlinedScopes>[options.InlineFunctionDebugInformation]</InlinedScopes>
    </ClCompile>
";

            private const string _projectConfigurationsStaticLinkTemplate =
                            @"    <Link>
      <GenerateDebugInformation>[options.LinkerGenerateDebugInformation]</GenerateDebugInformation>
      <EnableCOMDATFolding>[options.EnableCOMDATFolding]</EnableCOMDATFolding>
      <OptimizeReferences>[options.OptimizeReferences]</OptimizeReferences>
    </Link>
    <Lib>
      <TargetMachine>[options.TargetMachine]</TargetMachine>
      <SubSystem/>
      <AdditionalOptions>[options.AdditionalLibrarianOptions]</AdditionalOptions>
      <OutputFile>[options.OutputFile]</OutputFile>
      <ThinArchive>[options.UseThinArchives]</ThinArchive>
    </Lib>
";

            private const string _projectConfigurationsLinkTemplate =
                    @"    <Link>
      <OutputFile>[options.OutputFile]</OutputFile>
      <AdditionalLibraryDirectories>[options.AdditionalLibraryDirectories]</AdditionalLibraryDirectories>
      <AdditionalOptions>[options.AdditionalLinkerOptions] %(AdditionalOptions)</AdditionalOptions>
      <AdditionalDependencies>[options.AdditionalDependencies];%(AdditionalDependencies)</AdditionalDependencies>
      <ImportLibrary></ImportLibrary>
      <GenerateMapFile>[options.GenerateMapFile]</GenerateMapFile>
      <MapFileName>[options.MapFileName]</MapFileName>
      <EditAndContinue>[options.EditAndContinue]</EditAndContinue>
      <InfoStripping>[options.InfoStripping]</InfoStripping>
      <DataStripping>[options.DataStripping]</DataStripping>
      <WholeArchiveBegin>[options.WholeArchive]</WholeArchiveBegin>
      <DuplicateStripping>[options.DuplicateStripping]</DuplicateStripping>
      <Addressing>[options.Addressing]</Addressing>
    </Link>
";

            private const string _userFileConfigurationGeneralTemplate =
                @"    <LocalDebuggerCommand>[conf.VcxprojUserFile.LocalDebuggerCommand]</LocalDebuggerCommand>
    <LocalDebuggerCommandArguments>[conf.VcxprojUserFile.LocalDebuggerCommandArguments]</LocalDebuggerCommandArguments>
    <LocalDebuggerWorkingDirectory>[conf.VcxprojUserFile.LocalDebuggerWorkingDirectory]</LocalDebuggerWorkingDirectory>
    <LocalDebuggerAttach>[conf.VcxprojUserFile.LocalDebuggerAttachString]</LocalDebuggerAttach>
    <PreLaunchCommand>[conf.VcxprojUserFile.PreLaunchCommand]</PreLaunchCommand>
    <RemoteDebuggerCommand>[conf.VcxprojUserFile.RemoteDebuggerCommand]</RemoteDebuggerCommand>
    <RemoteDebuggerCommandArguments>[conf.VcxprojUserFile.RemoteDebuggerCommandArguments]</RemoteDebuggerCommandArguments>
    <RemoteDebuggingMode>[conf.VcxprojUserFile.RemoteDebuggingMode]</RemoteDebuggingMode>
    <RemoteDebuggerWorkingDirectory>[conf.VcxprojUserFile.RemoteDebuggerWorkingDirectory]</RemoteDebuggerWorkingDirectory>
    <AdditionalDebuggerCommands>[conf.AdditionalDebuggerCommands]</AdditionalDebuggerCommands>
    <DebuggerFlavor>LinuxDebugger</DebuggerFlavor>
";

            private const string _projectConfigurationsGeneral2 =
                @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <TargetName>[options.OutputFileName]</TargetName>
    <OutDir>[options.OutputDirectoryRemote]</OutDir>
    <IntDir>[options.IntermediateDirectoryRemote]</IntDir>
    <TargetExt>[options.OutputFileExtension]</TargetExt>
    <GenerateManifest>[options.GenerateManifest]</GenerateManifest>
    <PostBuildEventUseInBuild>[options.PostBuildEventEnable]</PostBuildEventUseInBuild>
    <PreBuildEventUseInBuild>[options.PreBuildEventEnable]</PreBuildEventUseInBuild>
    <PreLinkEventUseInBuild>[options.PreLinkEventEnable]</PreLinkEventUseInBuild>
    <LinkIncremental>[options.LinkIncremental]</LinkIncremental>
    <OutputFile>[options.OutputFile]</OutputFile>
    <CustomBuildBeforeTargets>[options.CustomBuildStepBeforeTargets]</CustomBuildBeforeTargets>
    <CustomBuildAfterTargets>[options.CustomBuildStepAfterTargets]</CustomBuildAfterTargets>
    <LocalDebuggerWorkingDirectory>$(TargetDir)</LocalDebuggerWorkingDirectory>
    <RemoteCppCompileToolExe>[options.RemoteCppCompileToolExe]</RemoteCppCompileToolExe>
    <RemoteCCompileToolExe>[options.RemoteCCompileToolExe]</RemoteCCompileToolExe>
    <RemoteLdToolExe>[options.RemoteLdToolExe]</RemoteLdToolExe>
    <LocalRemoteCopySources>[options.CopySources]</LocalRemoteCopySources>
    <RemoteLinkLocalCopyOutput>false</RemoteLinkLocalCopyOutput>
    <RemoteRootDir></RemoteRootDir>
    <RemoteProjectDir>[options.ProjectDirectory]</RemoteProjectDir>
    <RemoteBuildOutputs>[options.RemoteBuildOutputs]</RemoteBuildOutputs>
  </PropertyGroup>
";

            private const string _projectConfigurationsFastBuildMakefile =
            @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <TargetName>[options.OutputFileName]</TargetName>
    <LocalDebuggerWorkingDirectory>$(TargetDir)</LocalDebuggerWorkingDirectory>
    <RemoteRootDir>
    </RemoteRootDir>
    <RemoteProjectDir>[options.ProjectDirectory]</RemoteProjectDir>
    <RemoteBuildOutputs>[options.RemoteBuildOutputs]</RemoteBuildOutputs>
    <OutDir>[options.OutputDirectoryRemote]</OutDir>
    <IntDir>[options.IntermediateDirectoryRemote]</IntDir>
    <BuildCommandLine>cd [fastBuildWorkingDirectory]
[conf.FastBuildCustomActionsBeforeBuildCommand]
[fastBuildMakeCommandBuild]</BuildCommandLine>
    <ReBuildCommandLine>cd [fastBuildWorkingDirectory]
[conf.FastBuildCustomActionsBeforeBuildCommand]
[fastBuildMakeCommandRebuild]</ReBuildCommandLine>
    <LocalRemoteCopySources>[options.CopySources]</LocalRemoteCopySources>
    <CleanCommandLine>del ""[options.IntermediateDirectory]\*unity*.cpp"" &gt;NUL 2&gt;NUL
del ""[options.IntermediateDirectory]\*.obj"" &gt;NUL 2&gt;NUL
del ""[options.IntermediateDirectory]\*.a"" &gt;NUL 2&gt;NUL
del ""[options.IntermediateDirectory]\*.lib"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].exe"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].elf"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].exp"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].ilk"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].lib"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName].pdb"" &gt;NUL 2&gt;NUL
del ""[options.OutputDirectory]\[conf.TargetFileFullName]"" &gt;NUL 2&gt;NUL</CleanCommandLine>
    <NMakeIncludeSearchPath>$(NMakeIncludeSearchPath);[options.AdditionalPlatformIncludeDirectories]</NMakeIncludeSearchPath>
  </PropertyGroup>
  <ItemDefinitionGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <ClCompile>
      <CLanguageStandard>[options.CLanguageStandard]</CLanguageStandard>
      <CppLanguageStandard>[options.CppLanguageStandard]</CppLanguageStandard>
    </ClCompile>
  </ItemDefinitionGroup>
";

            private const string _projectDescriptionPlatformSpecific =
                @"    <ApplicationType>[applicationType]</ApplicationType>
    <ApplicationTypeRevision>[applicationTypeRevision]</ApplicationTypeRevision>
    <TargetLinuxPlatform>[targetLinuxPlatform]</TargetLinuxPlatform>
";
        }
    }
}
