// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Vcxproj
    {
        public static class Template
        {
            public static class Project
            {
                internal const string ProjectBegin =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""[toolsVersion]"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
";

                internal const string ProjectBeginConfigurationDescription =
                @"  <ItemGroup Label=""ProjectConfigurations"">
";

                internal const string ProjectEndConfigurationDescription =
                @"  </ItemGroup>
";

                internal const string ProjectConfigurationDescription =
                @"    <ProjectConfiguration Include=""[configName]|[platformName]"">
      <Configuration>[configName]</Configuration>
      <Platform>[platformName]</Platform>
    </ProjectConfiguration>
";

                internal const string ProjectDescription =
@"  <PropertyGroup Label=""Globals"">
    <ProjectGuid>{[guid]}</ProjectGuid>
    <TargetFrameworkVersion>[targetFrameworkVersion]</TargetFrameworkVersion>
    <TargetFramework>[targetFramework]</TargetFramework>
    <Keyword>[projectKeyword]</Keyword>
    <DefaultLanguage>en-US</DefaultLanguage>
    <RootNamespace>[projectName]</RootNamespace>
    <ProjectName>[projectName]</ProjectName>
";

                internal const string DisableRegistryUse =
@"    <DisableRegistryUse>true</DisableRegistryUse>
";

                internal const string DisableInstalledVcTargetsUse =
@"    <DisableInstalledVCTargetsUse>true</DisableInstalledVCTargetsUse>
";

                internal const string ProjectDescriptionStartPlatformConditional =
    @"  <PropertyGroup Label=""Globals"" Condition=""'$(Platform)'=='[platformName]'"">
";

                internal const string ProjectDescriptionEnd =
@"  </PropertyGroup>
";

                internal const string ImportCppDefaultProps =
@"  <Import Project=""[vcTargetsPath]\Microsoft.Cpp.Default.props"" />
";

                internal const string PropertyGroupStart =
                @"  <PropertyGroup>
";

                internal const string PropertyGroupEnd =
                @"  </PropertyGroup>
";

                internal const string CustomProperty =
                @"    <[custompropertyname]>[custompropertyvalue]</[custompropertyname]>
";

                internal const string VCOverridesProperties =
@"    <MSBuildExtensionsPath>[msBuildExtensionsPath]</MSBuildExtensionsPath>
    <[vcInstallDirKey]>[vcInstallDirValue]</[vcInstallDirKey]>
";

                internal const string VCTargetsPathOverride =
@"    <[vcTargetsPathKey]>[vcTargetsPath]</[vcTargetsPathKey]>
";

                internal const string VCTargetsPathOverrideConditional =
@"    <[vcTargetsPathKey] Condition=""'$(VisualStudioVersion)'=='[vsVersion]'"">[vcTargetsPath]</[vcTargetsPathKey]>
";

                internal const string ProjectEnd =
                @"</Project>";

                internal const string ProjectAfterConfigurationsGeneral =
@"  <Import Project=""[vcTargetsPath]\Microsoft.Cpp.props"" />
  <ImportGroup Label=""ExtensionSettings"">
";
                internal const string ProjectAfterConfigurationsGeneralImportPropertySheets =
@"  <ImportGroup Label=""PropertySheets"">
    <Import Project=""$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props"" Condition=""exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')"" Label=""LocalAppDataPlatform"" />
  </ImportGroup>
";

                internal const string ProjectImportedProps =
@"    <Import Project=""[importedPropsFile]"" />
";

                internal const string ProjectImportedMasmProps =
@"    <Import Project=""[vcTargetsPath]\BuildCustomizations\masm.props"" />
";

                internal const string ProjectImportedNasmProps =
@"    <Import Project=""[importedNasmPropsFile]"" />
";


                internal const string ProjectConfigurationImportedProps =
@"    <Import Project=""[importedPropsFile]"" Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" />
";

                internal const string ProjectImportedPropsEnd =
@"  </ImportGroup>
";
                internal const string ProjectAfterImportedProps =
@"  <PropertyGroup Label=""UserMacros"" />
";

                //<ImportGroup Label=""PropertySheets"" Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
                //  <Import Project=""$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props"" Condition=""exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')"" Label=""LocalAppDataPlatform"" />
                //</ImportGroup>

                internal const string ProjectFilesFastBuildFile =
                @"    <None Include=""[fastBuildFile]"" />
";

                internal const string ProjectConfigurationBeginItemDefinition =
                @"  <ItemDefinitionGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
";

                internal const string ProjectConfigurationEndItemDefinition =
                @"  </ItemDefinitionGroup>
";

                internal const string ProjectTargetsBegin =
@"  <Import Project=""[vcTargetsPath]\Microsoft.Cpp.targets"" />
  <ImportGroup Label=""ExtensionTargets"">
";

                internal const string ProjectTargetsItem =
@"    <Import Project=""[importedTargetsFile]"" />
";

                internal const string ProjectMasmTargetsItem =
@"    <Import Project=""[vcTargetsPath]\BuildCustomizations\masm.targets"" />
";

                internal const string ProjectNasmTargetsItem =
@"    <Import Project=""[importedNasmTargetsFile]"" />
";

                internal const string ProjectConfigurationImportedTargets =
@"    <Import Project=""[importedTargetsFile]"" Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" />
";

                // Support both regular and native package types, whichever happens to exist
                // possible file extension: .targets and .props
                internal const string ProjectNugetReferenceImport =
@"    <Import Project=""$(SolutionDir)\packages\[packageName].[packageVersion]\build\native\[packageName].[fileExtension]"" Condition=""Exists('$(SolutionDir)\packages\[packageName].[packageVersion]\build\native\[packageName].[fileExtension]')"" />
    <Import Project=""$(SolutionDir)\packages\[packageName].[packageVersion]\build\[packageName].[fileExtension]"" Condition=""!Exists('$(SolutionDir)\packages\[packageName].[packageVersion]\build\native\[packageName].[fileExtension]') and Exists('$(SolutionDir)\packages\[packageName].[packageVersion]\build\[packageName].[fileExtension]')"" />
";

                internal const string ProjectNugetReferenceError =
@"    <Error Condition=""!Exists('$(SolutionDir)\packages\[packageName].[packageVersion]\build\[packageName].[fileExtension]') and !Exists('$(SolutionDir)\packages\[packageName].[packageVersion]\build\native\[packageName].[fileExtension]')"" Text=""$([[System.String]]::Format('$(ErrorText)', '$(SolutionDir)\packages\[packageName].[packageVersion]\build\native\[packageName].[fileExtension]'))"" />
";

                internal const string ProjectTargetsEnd =
@"  </ImportGroup>
";

                internal const string ProjectCustomTargetsBegin =
@"  <Target Name=""[targetName]"" BeforeTargets=""[beforeTargets]"">
";

                internal const string ProjectCustomTargetsEnd =
@"  </Target>
";

                internal const string ProjectConfigurationsResourceCompile =
                @"    <ResourceCompile>[options.ResourceCompileTag]
      <PreprocessorDefinitions>[options.ResourcePreprocessorDefinitions];%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>[options.AdditionalResourceIncludeDirectories];%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
      <ShowProgress>[options.ResourceCompilerShowProgress]</ShowProgress>
    </ResourceCompile>[options.ResourceCompileTag]
";

                internal const string ProjectConfigurationsPreBuildEvent =
                @"    <PreBuildEvent>
      <Command>[options.PreBuildEvent]</Command>
      <Message>[options.PreBuildEventDescription]</Message>
    </PreBuildEvent>
";

                internal const string ProjectConfigurationsPreLinkEvent =
                @"    <PreLinkEvent>
      <Command>[options.PreLinkEvent]</Command>
      <Message>[options.PreLinkEventDescription]</Message>
    </PreLinkEvent>
";

                internal const string ProjectConfigurationsPrePostLinkEvent =
                @"    <PrePostLinkEvent>
      <Command>[options.PrePostLinkEvent]</Command>
      <Message>[options.PrePostLinkEventDescription]</Message>
    </PrePostLinkEvent>
";

                internal const string ProjectConfigurationsPostBuildEvent =
                @"    <PostBuildEvent>
      <Command>[options.PostBuildEvent]</Command>
      <Message>[options.PostBuildEventDescription]</Message>
    </PostBuildEvent>
";

                internal const string ProjectConfigurationsCustomBuildStep =
                @"    <CustomBuildStep>
      <Command>[options.CustomBuildStep]</Command>
      <Message>[options.CustomBuildStepDescription]</Message>
      <Outputs>[options.CustomBuildStepOutputs]</Outputs>
      <Inputs>[options.CustomBuildStepInputs]</Inputs>
      <TreatOutputAsContent>[options.CustomBuildStepTreatOutputAsContent]</TreatOutputAsContent>
    </CustomBuildStep>
";

                internal const string ProjectConfigurationsManifestTool =
                @"    <Manifest>
      <EnableDpiAwareness>[options.EnableDpiAwareness]</EnableDpiAwareness>
      <AdditionalManifestFiles>[options.AdditionalManifestFiles]</AdditionalManifestFiles>
    </Manifest>
";


                internal const string ProjectConfigurationsCustomBuildEvent =
                @"    <CustomBuildStep>
      <Command>[options.CustomBuildEvent]</Command>
      <Message>[options.CustomBuildEventDescription]</Message>
      <Outputs>[options.CustomBuildEventOutputs]</Outputs>
    </CustomBuildStep>
";

                internal const string ProjectFilesBegin =
                @"  <ItemGroup>
";

                internal const string ProjectFilesEnd =
                @"  </ItemGroup>
";

                internal const string ProjectFilesHeader =
                @"    <ClInclude Include=""[file.FilePath]"" />
";

                internal const string ProjectFilesNatvis =
                @"    <Natvis Include=""[file.FilePath]"" />
";

                internal const string ProjectFilesSourceBegin =
                @"    <ClCompile Include=""[file.FilePath]""";

                internal const string ProjectFilesResourceBegin =
                @"    <ResourceCompile Include=""[file.FilePath]""";

                internal const string ProjectFilesPRIResources =
                @"    <PRIResource Include=""[file.FilePath]"">
      <FileType>Document</FileType>
    </PRIResource>
";

                internal const string ProjectFilesNone =
                @"    <None Include=""[file.FilePath]"" />
";

                internal const string ProjectFilesCustomSourceBegin =
                @"    <[type] Include=""[file.FilePath]""";


                internal const string ProjectFilesResourceEnd =
                @"    </ResourceCompile>
";

                internal const string ProjectFilesCustomBuildBegin =
                @"    <CustomBuild Include=""[file]"">
      <FileType>[filetype]</FileType>
";

                internal const string ProjectFilesCustomBuildDescription =
                @"      <Message Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[description]</Message>
";

                internal const string ProjectFilesCustomBuildCommand =
                @"      <Command Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[command]</Command>
";

                internal const string ProjectFilesCustomBuildInputs =
                @"      <AdditionalInputs Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[inputs]</AdditionalInputs>
";

                internal const string ProjectFilesCustomBuildOutputs =
                @"      <Outputs Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[outputs]</Outputs>
";

                internal const string ProjectFilesCustomBuildLinkObject =
@"      <LinkObjects Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[linkobjects]</LinkObjects>
";

                internal const string ProjectFilesCustomBuildOutputItemType =
@"      <OutputItemType>[outputItemType]</OutputItemType>
";

                internal const string ProjectFilesCustomBuildEnd =
                @"    </CustomBuild>
";
                internal const string ProjectFilesSourceEnd =
                @" />
";

                internal const string ProjectFilesCustomSourceEnd =
                @" />
";

                internal const string ProjectFilesSourceBeginOptions =
                @">
";

                internal const string ProjectFilesCustomSourceBeginOptions =
                @">
";

                internal const string ProjectFilesSourceEndOptions =
                @"    </ClCompile>
";

                internal const string ProjectFilesCustomSourceEndOptions =
                @"    </[type]>
";

                internal const string ProjectFilesSourceObjectFileName =
                @"      <ObjectFileName Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[ObjectFileName]</ObjectFileName>
";


                internal const string ProjectFilesSourcePrecompCreate =
                @"      <PrecompiledHeader Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">Create</PrecompiledHeader>
";

                internal const string ProjectFilesSourcePrecompNotUsing =
                @"      <PrecompiledHeader Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">NotUsing</PrecompiledHeader>
";

                internal const string ProjectFilesForcedIncludeVanilla =
                @"      <ForcedIncludeFiles Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[options.ForcedIncludeFilesVanilla]</ForcedIncludeFiles>
";

                internal const string ProjectFilesAdditionalForcedInclude =
                @"      <ForcedIncludeFiles Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">%(ForcedIncludeFiles);[ForcedIncludeFiles]</ForcedIncludeFiles>
";

                internal const string ProjectFilesAdditionalForcedIncludeVanilla =
                @"      <ForcedIncludeFiles Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[options.ForcedIncludeFilesVanilla];[ForcedIncludeFiles]</ForcedIncludeFiles>
";

                internal const string ProjectFilesSourceExcludeFromBuild =
                @"      <ExcludedFromBuild Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">true</ExcludedFromBuild>
";
                internal const string ProjectFilesSourceConsumeWinRTExtensions =
                @"      <CompileAsWinRT Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">true</CompileAsWinRT>
";
                internal const string ProjectFilesSourceExcludeWinRTExtensions =
                @"      <CompileAsWinRT Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">false</CompileAsWinRT>
";

                internal const string ProjectFilesSourceEnableExceptions =
                @"      <ExceptionHandling Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[exceptionSetting]</ExceptionHandling>
";

                internal const string ProjectFilesSourceCompileAsC =
                @"      <CompileAs Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">CompileAsC</CompileAs>
";

                internal const string ProjectFilesSourceCompileAsCPP =
                @"      <CompileAs Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">CompileAsCPP</CompileAs>
";

                internal const string ProjectFilesSourceCompileAsCLR =
                @"      <CompileAsManaged Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">true</CompileAsManaged>
";

                internal const string ProjectFilesSourceDoNotCompileAsCLR =
                @"      <CompileAsManaged Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">false</CompileAsManaged>
";
                internal const string ProjectFilesSourceDefine =
                @"      <PreprocessorDefinitions Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[ProjectFilesSourceDefine];%(PreprocessorDefinitions)</PreprocessorDefinitions>
";

                internal const string ProjectFilesSourceExcludeGenerateXmlDocumentation =
                @"      <GenerateXMLDocumentationFiles Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">false</GenerateXMLDocumentationFiles>
";

                internal const string SingleReferenceByName =
                    @"    <Reference Include=""[include]"" />
";

                internal const string ReferenceByName =
@"    <Reference Include=""[include]"">
      <Private>[private]</Private>
    </Reference>";


                internal const string ProjectReference =
@"    <ProjectReference Include=""[include]"">
      <Project>{[projectGUID]}</Project>
      <Name>[projectRefName]</Name>
      <Private>[private]</Private>
      <ReferenceOutputAssembly>[options.ReferenceOutputAssembly]</ReferenceOutputAssembly>
      <CopyLocalSatelliteAssemblies>[options.CopyLocalSatelliteAssemblies]</CopyLocalSatelliteAssemblies>
      <LinkLibraryDependencies>[options.LinkLibraryDependencies]</LinkLibraryDependencies>
      <UseLibraryDependencyInputs>[options.UseLibraryDependencyInputs]</UseLibraryDependencyInputs>
    </ProjectReference>
";

                internal const string ReferenceByPath =
@"    <Reference Include=""[include]"" Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
      <HintPath>[hintPath]</HintPath>
      <Private>[private]</Private>
    </Reference>
";
                internal const string ProjectBuildMacroEnvironmentVariable =
@"    <BuildMacro Include=""[environmentVariableName]"">
      <Value>[environmentVariableValue]</Value>
      <EnvironmentVariable>true</EnvironmentVariable>
    </BuildMacro>
";

                internal const string ItemGroupBegin =
@"  <ItemGroup>
";

                internal const string ItemGroupEnd =
@"  </ItemGroup>
";

                internal const string PlatformFolderOverride =
@"    <_PlatformFolder>[platformFolder]</_PlatformFolder>
";

                internal const string AdditionalVCTargetsPath =
                    @"    <AdditionalVCTargetsPath>[additionalVCTargetsPath]</AdditionalVCTargetsPath>
    <_VCTargetsPathForToolset>$(AdditionalVCTargetsPath)</_VCTargetsPathForToolset>
";

                internal static class Filters
                {
                    internal const string Begin =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""[toolsVersion]"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
";

                    internal const string ProjectFiltersEnd =
@"</Project>";

                    internal const string FileNoFilter =
@"    <[type] Include=""[file.FilePath]"" />
";

                    internal const string FileWithFilter =
@"    <[type] Include=""[file.FilePath]"">
      <Filter>[file.FilterPath]</Filter>
    </[type]>
";

                    internal const string FileWithDependencyFilter =
@"    <CustomBuild Include=""[fileName]"" />
";

                    internal const string Filter =
@"    <Filter Include=""[name]"">
      <UniqueIdentifier>{[guid]}</UniqueIdentifier>
    </Filter>
";
                }
            }

            public static class TargetElement
            {
                public static string CustomTarget =
@"  <Target Name=""[targetElement.Name]"" [targetElement.TargetParameters]>
    [targetElement.CustomTasks]
  </Target>
";
            }
        }
    }
}
