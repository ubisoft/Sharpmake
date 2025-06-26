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
                public static string ProjectBegin =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""[toolsVersion]"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
";

                public static string ProjectBeginConfigurationDescription =
                @"  <ItemGroup Label=""ProjectConfigurations"">
";

                public static string ProjectEndConfigurationDescription =
                @"  </ItemGroup>
";

                public static string ProjectConfigurationDescription =
                @"    <ProjectConfiguration Include=""[configName]|[platformName]"">
      <Configuration>[configName]</Configuration>
      <Platform>[platformName]</Platform>
    </ProjectConfiguration>
";

                public static string ProjectDescription =
@"  <PropertyGroup Label=""Globals"">
    <ProjectGuid>{[guid]}</ProjectGuid>
    <TargetFrameworkVersion>[targetFrameworkVersion]</TargetFrameworkVersion>
    <TargetFramework>[targetFramework]</TargetFramework>
    <Keyword>[projectKeyword]</Keyword>
    <DefaultLanguage>en-US</DefaultLanguage>
    <RootNamespace>[projectName]</RootNamespace>
    <ProjectName>[projectName]</ProjectName>
";

                public const string DisableRegistryUse =
@"    <DisableRegistryUse>true</DisableRegistryUse>
";

                internal const string DisableInstalledVcTargetsUse =
@"    <DisableInstalledVCTargetsUse>true</DisableInstalledVCTargetsUse>
";

                public static string ProjectDescriptionStartPlatformConditional =
    @"  <PropertyGroup Label=""Globals"" Condition=""'$(Platform)'=='[platformName]'"">
";

                public static string ProjectDescriptionEnd =
@"  </PropertyGroup>
";

                public static string ImportCppDefaultProps =
@"  <Import Project=""[vcTargetsPath]\Microsoft.Cpp.Default.props"" />
";

                public static string PropertyGroupStart =
                @"  <PropertyGroup>
";

                public static string PropertyGroupEnd =
                @"  </PropertyGroup>
";

                public static string CustomProperty =
                @"    <[custompropertyname]>[custompropertyvalue]</[custompropertyname]>
";

                public static string VCOverridesProperties =
@"    <MSBuildExtensionsPath>[msBuildExtensionsPath]</MSBuildExtensionsPath>
    <[vcInstallDirKey]>[vcInstallDirValue]</[vcInstallDirKey]>
";

                public static string VCTargetsPathOverride =
@"    <[vcTargetsPathKey]>[vcTargetsPath]</[vcTargetsPathKey]>
";

                public static string VCTargetsPathOverrideConditional =
@"    <[vcTargetsPathKey] Condition=""'$(VisualStudioVersion)'=='[vsVersion]'"">[vcTargetsPath]</[vcTargetsPathKey]>
";

                public static string ProjectEnd =
                @"</Project>";

                public static string ProjectAfterConfigurationsGeneral =
@"  <Import Project=""[vcTargetsPath]\Microsoft.Cpp.props"" />
  <ImportGroup Label=""ExtensionSettings"">
";
                public static string ProjectAfterConfigurationsGeneralImportPropertySheets =
@"  <ImportGroup Label=""PropertySheets"">
    <Import Project=""$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props"" Condition=""exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')"" Label=""LocalAppDataPlatform"" />
  </ImportGroup>
";

                public static string ProjectImportedProps =
@"    <Import Project=""[importedPropsFile]"" />
";

                public static string ProjectImportedMasmProps =
@"    <Import Project=""[vcTargetsPath]\BuildCustomizations\masm.props"" />
";

                public static string ProjectImportedNasmProps =
@"    <Import Project=""[importedNasmPropsFile]"" />
";


                public static string ProjectConfigurationImportedProps =
@"    <Import Project=""[importedPropsFile]"" Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" />
";

                public static string ProjectImportedPropsEnd =
@"  </ImportGroup>
";
                public static string ProjectAfterImportedProps =
@"  <PropertyGroup Label=""UserMacros"" />
";

                //<ImportGroup Label=""PropertySheets"" Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
                //  <Import Project=""$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props"" Condition=""exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')"" Label=""LocalAppDataPlatform"" />
                //</ImportGroup>

                public static string ProjectFilesFastBuildFile =
                @"    <None Include=""[fastBuildFile]"" />
";

                public static string ProjectConfigurationBeginItemDefinition =
                @"  <ItemDefinitionGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
";

                public static string ProjectConfigurationEndItemDefinition =
                @"  </ItemDefinitionGroup>
";

                public static string ProjectTargetsBegin =
@"  <Import Project=""[vcTargetsPath]\Microsoft.Cpp.targets"" />
  <ImportGroup Label=""ExtensionTargets"">
";

                public static string ProjectTargetsItem =
@"    <Import Project=""[importedTargetsFile]"" />
";

                public static string ProjectMasmTargetsItem =
@"    <Import Project=""[vcTargetsPath]\BuildCustomizations\masm.targets"" />
";

                public static string ProjectNasmTargetsItem =
@"    <Import Project=""[importedNasmTargetsFile]"" />
";

                public static string ProjectConfigurationImportedTargets =
@"    <Import Project=""[importedTargetsFile]"" Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" />
";

                // Support both regular and native package types, whichever happens to exist
                // possible file extension: .targets and .props
                public static string ProjectNugetReferenceImport =
@"    <Import Project=""$(SolutionDir)\packages\[packageName].[packageVersion]\build\native\[packageName].[fileExtension]"" Condition=""Exists('$(SolutionDir)\packages\[packageName].[packageVersion]\build\native\[packageName].[fileExtension]')"" />
    <Import Project=""$(SolutionDir)\packages\[packageName].[packageVersion]\build\[packageName].[fileExtension]"" Condition=""!Exists('$(SolutionDir)\packages\[packageName].[packageVersion]\build\native\[packageName].[fileExtension]') and Exists('$(SolutionDir)\packages\[packageName].[packageVersion]\build\[packageName].[fileExtension]')"" />
";

                public static string ProjectNugetReferenceError =
@"    <Error Condition=""!Exists('$(SolutionDir)\packages\[packageName].[packageVersion]\build\[packageName].[fileExtension]') and !Exists('$(SolutionDir)\packages\[packageName].[packageVersion]\build\native\[packageName].[fileExtension]')"" Text=""$([[System.String]]::Format('$(ErrorText)', '$(SolutionDir)\packages\[packageName].[packageVersion]\build\native\[packageName].[fileExtension]'))"" />
";

                public static string ProjectTargetsEnd =
@"  </ImportGroup>
";

                public static string ProjectCustomTargetsBegin =
@"  <Target Name=""[targetName]"" BeforeTargets=""[beforeTargets]"">
";

                public static string ProjectCustomTargetsEnd =
@"  </Target>
";

                public static string ProjectConfigurationsResourceCompile =
                @"    <ResourceCompile>[options.ResourceCompileTag]
      <PreprocessorDefinitions>[options.ResourcePreprocessorDefinitions];%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>[options.AdditionalResourceIncludeDirectories];%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
      <ShowProgress>[options.ResourceCompilerShowProgress]</ShowProgress>
    </ResourceCompile>[options.ResourceCompileTag]
";

                public static string ProjectConfigurationsPreBuildEvent =
                @"    <PreBuildEvent>
      <Command>[options.PreBuildEvent]</Command>
      <Message>[options.PreBuildEventDescription]</Message>
    </PreBuildEvent>
";

                public static string ProjectConfigurationsPreLinkEvent =
                @"    <PreLinkEvent>
      <Command>[options.PreLinkEvent]</Command>
      <Message>[options.PreLinkEventDescription]</Message>
    </PreLinkEvent>
";

                public static string ProjectConfigurationsPrePostLinkEvent =
                @"    <PrePostLinkEvent>
      <Command>[options.PrePostLinkEvent]</Command>
      <Message>[options.PrePostLinkEventDescription]</Message>
    </PrePostLinkEvent>
";

                public static string ProjectConfigurationsPostBuildEvent =
                @"    <PostBuildEvent>
      <Command>[options.PostBuildEvent]</Command>
      <Message>[options.PostBuildEventDescription]</Message>
    </PostBuildEvent>
";

                public static string ProjectConfigurationsCustomBuildStep =
                @"    <CustomBuildStep>
      <Command>[options.CustomBuildStep]</Command>
      <Message>[options.CustomBuildStepDescription]</Message>
      <Outputs>[options.CustomBuildStepOutputs]</Outputs>
      <Inputs>[options.CustomBuildStepInputs]</Inputs>
      <TreatOutputAsContent>[options.CustomBuildStepTreatOutputAsContent]</TreatOutputAsContent>
    </CustomBuildStep>
";

                public static string ProjectConfigurationsManifestTool =
                @"    <Manifest>
      <EnableDpiAwareness>[options.EnableDpiAwareness]</EnableDpiAwareness>
      <AdditionalManifestFiles>[options.AdditionalManifestFiles]</AdditionalManifestFiles>
    </Manifest>
";


                public static string ProjectConfigurationsCustomBuildEvent =
                @"    <CustomBuildStep>
      <Command>[options.CustomBuildEvent]</Command>
      <Message>[options.CustomBuildEventDescription]</Message>
      <Outputs>[options.CustomBuildEventOutputs]</Outputs>
    </CustomBuildStep>
";

                public static string ProjectFilesBegin =
                @"  <ItemGroup>
";

                public static string ProjectFilesEnd =
                @"  </ItemGroup>
";

                public static string ProjectFilesHeader =
                @"    <ClInclude Include=""[file.FilePath]"" />
";

                public static string ProjectFilesNatvis =
                @"    <Natvis Include=""[file.FilePath]"" />
";

                public static string ProjectFilesSourceBegin =
                @"    <ClCompile Include=""[file.FilePath]""";

                public static string ProjectFilesResourceBegin =
                @"    <ResourceCompile Include=""[file.FilePath]""";

                public static string ProjectFilesPRIResources =
                @"    <PRIResource Include=""[file.FilePath]"">
      <FileType>Document</FileType>
    </PRIResource>
";

                public static string ProjectFilesNone =
                @"    <None Include=""[file.FilePath]"" />
";

                public static string ProjectFilesCustomSourceBegin =
                @"    <[type] Include=""[file.FilePath]""";


                public static string ProjectFilesResourceEnd =
                @"    </ResourceCompile>
";

                public static string ProjectFilesCustomBuildBegin =
                @"    <CustomBuild Include=""[file]"">
      <FileType>[filetype]</FileType>
";

                public static string ProjectFilesCustomBuildDescription =
                @"      <Message Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[description]</Message>
";

                public static string ProjectFilesCustomBuildCommand =
                @"      <Command Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[command]</Command>
";

                public static string ProjectFilesCustomBuildInputs =
                @"      <AdditionalInputs Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[inputs]</AdditionalInputs>
";

                public static string ProjectFilesCustomBuildOutputs =
                @"      <Outputs Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[outputs]</Outputs>
";

                public static string ProjectFilesCustomBuildLinkObject =
@"      <LinkObjects Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[linkobjects]</LinkObjects>
";

                public static string ProjectFilesCustomBuildOutputItemType =
@"      <OutputItemType>[outputItemType]</OutputItemType>
";

                public static string ProjectFilesCustomBuildEnd =
                @"    </CustomBuild>
";
                public static string ProjectFilesSourceEnd =
                @" />
";

                public static string ProjectFilesCustomSourceEnd =
                @" />
";

                public static string ProjectFilesSourceBeginOptions =
                @">
";

                public static string ProjectFilesCustomSourceBeginOptions =
                @">
";

                public static string ProjectFilesSourceEndOptions =
                @"    </ClCompile>
";

                public static string ProjectFilesCustomSourceEndOptions =
                @"    </[type]>
";

                public static string ProjectFilesSourceObjectFileName =
                @"      <ObjectFileName Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[ObjectFileName]</ObjectFileName>
";


                public static string ProjectFilesSourcePrecompCreate =
                @"      <PrecompiledHeader Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">Create</PrecompiledHeader>
";

                public static string ProjectFilesSourcePrecompNotUsing =
                @"      <PrecompiledHeader Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">NotUsing</PrecompiledHeader>
";

                public static string ProjectFilesForcedIncludeVanilla =
                @"      <ForcedIncludeFiles Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[options.ForcedIncludeFilesVanilla]</ForcedIncludeFiles>
";

                public static string ProjectFilesAdditionalForcedInclude =
                @"      <ForcedIncludeFiles Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">%(ForcedIncludeFiles);[ForcedIncludeFiles]</ForcedIncludeFiles>
";

                public static string ProjectFilesAdditionalForcedIncludeVanilla =
                @"      <ForcedIncludeFiles Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[options.ForcedIncludeFilesVanilla];[ForcedIncludeFiles]</ForcedIncludeFiles>
";

                public static string ProjectFilesSourceExcludeFromBuild =
                @"      <ExcludedFromBuild Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">true</ExcludedFromBuild>
";
                public static string ProjectFilesSourceConsumeWinRTExtensions =
                @"      <CompileAsWinRT Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">true</CompileAsWinRT>
";
                public static string ProjectFilesSourceExcludeWinRTExtensions =
                @"      <CompileAsWinRT Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">false</CompileAsWinRT>
";

                public static string ProjectFilesSourceEnableExceptions =
                @"      <ExceptionHandling Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[exceptionSetting]</ExceptionHandling>
";

                public static string ProjectFilesSourceCompileAsC =
                @"      <CompileAs Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">CompileAsC</CompileAs>
";

                public static string ProjectFilesSourceCompileAsCPP =
                @"      <CompileAs Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">CompileAsCPP</CompileAs>
";

                public static string ProjectFilesSourceCompileAsCLR =
                @"      <CompileAsManaged Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">true</CompileAsManaged>
";

                public static string ProjectFilesSourceDoNotCompileAsCLR =
                @"      <CompileAsManaged Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">false</CompileAsManaged>
";
                public static string ProjectFilesSourceDefine =
                @"      <PreprocessorDefinitions Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">[ProjectFilesSourceDefine];%(PreprocessorDefinitions)</PreprocessorDefinitions>
";

                public static string ProjectFilesSourceExcludeGenerateXmlDocumentation =
                @"      <GenerateXMLDocumentationFiles Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">false</GenerateXMLDocumentationFiles>
";

                public static string SingleReferenceByName =
                    @"    <Reference Include=""[include]"" />
";

                public static string ReferenceByName =
@"    <Reference Include=""[include]"">
      <Private>[private]</Private>
    </Reference>";


                public static string ProjectReference =
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

                public static string ReferenceByPath =
@"    <Reference Include=""[include]"" Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
      <HintPath>[hintPath]</HintPath>
      <Private>[private]</Private>
    </Reference>
";
                public static string ProjectBuildMacroEnvironmentVariable =
@"    <BuildMacro Include=""[environmentVariableName]"">
      <Value>[environmentVariableValue]</Value>
      <EnvironmentVariable>true</EnvironmentVariable>
    </BuildMacro>
";

                public static string ItemGroupBegin =
@"  <ItemGroup>
";

                public static string ItemGroupEnd =
@"  </ItemGroup>
";

                public const string PlatformFolderOverride =
@"    <_PlatformFolder>[platformFolder]</_PlatformFolder>
";

                public const string AdditionalVCTargetsPath =
                    @"    <AdditionalVCTargetsPath>[additionalVCTargetsPath]</AdditionalVCTargetsPath>
    <_VCTargetsPathForToolset>$(AdditionalVCTargetsPath)</_VCTargetsPathForToolset>
";

                internal static class Filters
                {
                    public static string Begin =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""[toolsVersion]"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
";

                    public static string ProjectFiltersEnd =
@"</Project>";

                    public static string FileNoFilter =
@"    <[type] Include=""[file.FilePath]"" />
";

                    public static string FileWithFilter =
@"    <[type] Include=""[file.FilePath]"">
      <Filter>[file.FilterPath]</Filter>
    </[type]>
";

                    public static string FileWithDependencyFilter =
@"    <CustomBuild Include=""[fileName]"" />
";

                    public static string Filter =
@"    <Filter Include=""[name]"">
      <UniqueIdentifier>{[guid]}</UniqueIdentifier>
    </Filter>
";
                }
            }
        }
    }
}
