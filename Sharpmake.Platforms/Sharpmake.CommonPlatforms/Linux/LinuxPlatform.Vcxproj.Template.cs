// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Sharpmake
{
    public static partial class Linux
    {
        public sealed partial class LinuxPlatform
        {
            private const string _projectConfigurationsCompileTemplate =
                @"    <ClCompile>
                  <PrecompiledHeader>[options.UsePrecompiledHeader]</PrecompiledHeader>
                  <PreprocessorDefinitions>[options.PreprocessorDefinitions];%(PreprocessorDefinitions);</PreprocessorDefinitions>
                  <ForcedIncludeFiles>[options.ForcedIncludeFiles]</ForcedIncludeFiles>
                  <GenerateDebugInformation>[options.GenerateDebugInformation]</GenerateDebugInformation>
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
                  <AdditionalOptions>[options.AdditionalCompilerOptions] %(AdditionalOptions)</AdditionalOptions>
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
        <OutDir>[options.OutputDirectoryRemote]\</OutDir>
        <IntDir>[options.IntermediateDirectoryRemote]\</IntDir>
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
      </PropertyGroup>
    ";

            private const string _projectConfigurationsFastBuildMakefile =
            @"    <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
<TargetName>[options.OutputFileName]</TargetName>
<LocalDebuggerWorkingDirectory>$(TargetDir)</LocalDebuggerWorkingDirectory>
<RemoteRootDir></RemoteRootDir>
<RemoteProjectDir>[options.ProjectDirectory]</RemoteProjectDir>
<OutDir>[options.OutputDirectoryRemote]\</OutDir>
<IntDir>[options.IntermediateDirectoryRemote]\</IntDir>
<BuildCommandLine>cd [relativeMasterBffPath]
[conf.FastBuildCustomActionsBeforeBuildCommand]
[fastBuildMakeCommandBuild]</BuildCommandLine>
<ReBuildCommandLine>cd [relativeMasterBffPath]
[conf.FastBuildCustomActionsBeforeBuildCommand]
[fastBuildMakeCommandRebuild]</ReBuildCommandLine>
<LocalRemoteCopySources>[options.CopySources]</LocalRemoteCopySources>
<CleanCommandLine>del ""[options.IntermediateDirectory]\*unity*.cpp"" >NUL 2>NUL
del ""$(ProjectDir)[options.IntermediateDirectory]\*.obj"" >NUL 2>NUL
del ""$(ProjectDir)[options.IntermediateDirectory]\*.a"" >NUL 2>NUL
del ""$(ProjectDir)[options.IntermediateDirectory]\*.lib"" >NUL 2>NUL
del ""$(ProjectDir)[options.OutputDirectory]\[conf.TargetFileFullName].exe"" >NUL 2>NUL
del ""$(ProjectDir)[options.OutputDirectory]\[conf.TargetFileFullName].elf"" >NUL 2>NUL
del ""$(ProjectDir)[options.OutputDirectory]\[conf.TargetFileFullName].exp"" >NUL 2>NUL
del ""$(ProjectDir)[options.OutputDirectory]\[conf.TargetFileFullName].ilk"" >NUL 2>NUL
del ""$(ProjectDir)[options.OutputDirectory]\[conf.TargetFileFullName].lib"" >NUL 2>NUL
del ""$(ProjectDir)[options.OutputDirectory]\[conf.TargetFileFullName].pdb"" >NUL 2>NUL
del ""$(ProjectDir)[options.OutputDirectory]\[conf.TargetFileFullName]"" >NUL 2>NUL</CleanCommandLine>
</PropertyGroup>
    ";

            private const string _projectDescriptionPlatformSpecific =
                @"    <ApplicationType>[applicationType]</ApplicationType>
    <ApplicationTypeRevision>[applicationTypeRevision]</ApplicationTypeRevision>
    <TargetLinuxPlatform>[targetLinuxPlatform]</TargetLinuxPlatform>
";
        }
    }
}
