// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.VisualStudio
{
    public partial class CSproj
    {
        public static class Template
        {
            public static class Project
            {
                internal const string ProjectBegin =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""[toolsVersion]"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
";

                internal const string ProjectBeginVs2017 =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""[toolsVersion]"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
";
                internal const string ProjectBeginNetCore =
@"<Project>
";

                internal const string ProjectEnd =
@"</Project>";

                internal const string ProjectDescription =
@"  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">[options.DefaultConfiguration]</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">[defaultPlatform]</Platform>
    <PlatformTarget Condition="" '$(Platform)' == '' "">[defaultPlatform]</PlatformTarget>
    <ProjectGuid>{[guid]}</ProjectGuid>
    <OutputType>[outputType]</OutputType>
    <AppDesignerFolder>[appDesignerFolder]</AppDesignerFolder>
    <RootNamespace>[project.RootNamespace]</RootNamespace>
    <AssemblyName>[assemblyName]</AssemblyName>
    <SignAssembly>[options.SignAssembly]</SignAssembly>
    <AssemblyOriginatorKeyFile>[options.AssemblyOriginatorKeyFile]</AssemblyOriginatorKeyFile>
    <[targetFrameworkVersionString]>[targetFramework]</[targetFrameworkVersionString]>
    <RollForward>[options.RollForward]</RollForward>
    <FileAlignment>[options.FileAlignment]</FileAlignment>
    <IsWebBootstrapper>[options.IsWebBootstrapper]</IsWebBootstrapper>
    <ProjectTypeGuids>[projectTypeGuids]</ProjectTypeGuids>
    <IsPublishable>[options.IsPublishable]</IsPublishable>
    <PublishUrl>[options.PublishUrl]</PublishUrl>
    <PublishSingleFile>[options.PublishSingleFile]</PublishSingleFile>
    <PublishTrimmed>[options.PublishTrimmed]</PublishTrimmed>
    <InstallUrl>[options.InstallUrl]</InstallUrl>
    <ManifestKeyFile>[options.ManifestKeyFile]</ManifestKeyFile>
    <ManifestCertificateThumbprint>[options.ManifestCertificateThumbprint]</ManifestCertificateThumbprint>
    <GenerateDocumentationFile>[GenerateDocumentationFile]</GenerateDocumentationFile>
    <GenerateManifests>[options.GenerateManifests]</GenerateManifests>
    <SignManifests>[options.SignManifests]</SignManifests>
    <UseVSHostingProcess>[options.UseVSHostingProcess]</UseVSHostingProcess>
    <ProductName>[options.ProductName]</ProductName>
    <PublisherName>[options.PublisherName]</PublisherName>
    <MinimumRequiredVersion>[options.MinimumRequiredVersion]</MinimumRequiredVersion>
    <WebPage>[options.WebPage]</WebPage>
    <OpenBrowserOnPublish>[options.OpenBrowserOnPublish]</OpenBrowserOnPublish>
    <CreateWebPageOnPublish>[options.CreateWebPageOnPublish]</CreateWebPageOnPublish>
    <BootstrapperComponentsUrl>[options.BootstrapperComponentsUrl]</BootstrapperComponentsUrl>
    <Install>[options.Install]</Install>
    <InstallFrom>[options.InstallFrom]</InstallFrom>
    <UpdateEnabled>[options.UpdateEnabled]</UpdateEnabled>
    <UpdateMode>[options.UpdateMode]</UpdateMode>
    <UpdateInterval>[options.UpdateInterval]</UpdateInterval>
    <UpdateIntervalUnits>[options.UpdateIntervalUnits]</UpdateIntervalUnits>
    <UpdatePeriodically>[options.UpdatePeriodically]</UpdatePeriodically>
    <UpdateRequired>[options.UpdateRequired]</UpdateRequired>
    <CopyOutputSymbolsToOutputDirectory>[options.CopyOutputSymbolsToOutputDirectory]</CopyOutputSymbolsToOutputDirectory>
    <MapFileExtensions>[options.MapFileExtensions]</MapFileExtensions>
    <ApplicationRevision>[options.ApplicationRevision]</ApplicationRevision>
    <ApplicationVersion>[options.ApplicationVersion]</ApplicationVersion>
    <UseApplicationTrust>[options.UseApplicationTrust]</UseApplicationTrust>
    <CreateDesktopShortcut>[options.CreateDesktopShortcut]</CreateDesktopShortcut>
    <PublishWizardCompleted>[options.PublishWizardCompleted]</PublishWizardCompleted>
    <BootstrapperEnabled>[options.BootstrapperEnabled]</BootstrapperEnabled>
    <MinimumVisualStudioVersion>[options.MinimumVisualStudioVersion]</MinimumVisualStudioVersion>
    <OldToolsVersion>[options.OldToolsVersion]</OldToolsVersion>
    <UseCodebase>[options.UseCodeBase]</UseCodebase>
    <VisualStudioVersion Condition=""'$(VisualStudioVersion)' == ''"">[options.VisualStudioVersion]</VisualStudioVersion>
    <VSToolsPath Condition=""'$(VSToolsPath)' == ''"">[options.VsToolsPath]</VSToolsPath>
    <ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch>[options.ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch]</ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch>
    <RegisterOutputPackage>[options.RegisterOutputPackage]</RegisterOutputPackage>
    <RegisterWithCodebase>[options.RegisterWithCodebase]</RegisterWithCodebase>
    <GeneratePkgDefFile>[options.GeneratePkgDefFile]</GeneratePkgDefFile>
    <IncludeAssemblyInVSIXContainer>[options.IncludeAssemblyInVSIXContainer]</IncludeAssemblyInVSIXContainer>
    <IncludeDebugSymbolsInVSIXContainer>[options.CreateVsixContainer]</IncludeDebugSymbolsInVSIXContainer>
    <IncludeDebugSymbolsInLocalVSIXDeployment>[options.CreateVsixContainer]</IncludeDebugSymbolsInLocalVSIXDeployment>
    <VsixType>[options.VsixType]</VsixType>
    <ConcordSDKDir>[options.ConcordSDKDir]</ConcordSDKDir>
    <AutoGenerateBindingRedirects>[options.AutoGenerateBindingRedirects]</AutoGenerateBindingRedirects>
    <SonarQubeExclude>[options.SonarQubeExclude]</SonarQubeExclude>
    <EnableDefaultItems>[netCoreEnableDefaultItems]</EnableDefaultItems>
    <EnableDefaultCompileItems>[enableDefaultCompileItems]</EnableDefaultCompileItems>
    <EnableDefaultEmbeddedResourceItems>[enableDefaultEmbeddedResourceItems]</EnableDefaultEmbeddedResourceItems>
    <EnableDefaultNoneItems>[enableDefaultNoneItems]</EnableDefaultNoneItems>
    <EnableDefaultApplicationDefinition>[enableDefaultApplicationDefinition]</EnableDefaultApplicationDefinition>
    <EnableDefaultPageItems>[enableDefaultPageItems]</EnableDefaultPageItems>
    <DefaultItemExcludes>[defaultItemExcludes]</DefaultItemExcludes>
    <GenerateAssemblyInfo>[GeneratedAssemblyConfigTemplate.GenerateAssemblyInfo]</GenerateAssemblyInfo>
    <GenerateAssemblyConfigurationAttribute>[GeneratedAssemblyConfigTemplate.GenerateAssemblyConfigurationAttribute]</GenerateAssemblyConfigurationAttribute>
    <GenerateAssemblyDescriptionAttribute>[GeneratedAssemblyConfigTemplate.GenerateAssemblyDescriptionAttribute]</GenerateAssemblyDescriptionAttribute>
    <GenerateAssemblyProductAttribute>[GeneratedAssemblyConfigTemplate.GenerateAssemblyProductAttribute]</GenerateAssemblyProductAttribute>
    <GenerateAssemblyTitleAttribute>[GeneratedAssemblyConfigTemplate.GenerateAssemblyTitleAttribute]</GenerateAssemblyTitleAttribute>
    <GenerateAssemblyCompanyAttribute>[GeneratedAssemblyConfigTemplate.GenerateAssemblyCompanyAttribute]</GenerateAssemblyCompanyAttribute>
    <GenerateAssemblyFileVersionAttribute>[GeneratedAssemblyConfigTemplate.GenerateAssemblyFileVersionAttribute]</GenerateAssemblyFileVersionAttribute>
    <GenerateAssemblyVersionAttribute>[GeneratedAssemblyConfigTemplate.GenerateAssemblyVersionAttribute]</GenerateAssemblyVersionAttribute>
    <GenerateAssemblyInformationalVersionAttribute>[GeneratedAssemblyConfigTemplate.GenerateAssemblyInformationalVersionAttribute]</GenerateAssemblyInformationalVersionAttribute>
    <RestoreProjectStyle>[NugetRestoreProjectStyleString]</RestoreProjectStyle>
    <ProductVersion>[options.ProductVersion]</ProductVersion>
    <FileVersion>[options.FileVersion]</FileVersion>
    <Version>[options.Version]</Version>
    <Product>[options.Product]</Product>
    <Copyright>[options.Copyright]</Copyright>
    <UseWpf>[options.UseWpf]</UseWpf>
    <UseWindowsForms>[options.UseWindowsForms]</UseWindowsForms>
    <Nullable>[options.Nullable]</Nullable>
    <PublishAot>[options.PublishAot]</PublishAot>
  </PropertyGroup>
";

                internal const string BaseIntermediateOutputPathPropertyGroup =
@"  <PropertyGroup>
    <BaseIntermediateOutputPath>[baseIntermediateOutputPath]</BaseIntermediateOutputPath>
  </PropertyGroup>
";

                internal const string DefaultProjectConfigurationCondition = "'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'";
                internal const string MultiFrameworkProjectConfigurationCondition = "'$(Configuration)|$(Platform)|$(TargetFramework)'=='[conf.Name]|[platformName]|[targetFramework]'";

                internal const string ProjectConfigurationsGeneral =
@"    <PlatformTarget>[platformName]</PlatformTarget>
    <DebugSymbols>[options.DebugSymbols]</DebugSymbols>
    <DebugType>[options.DebugType]</DebugType>
    <Optimize>[options.Optimize]</Optimize>
    <BaseAddress>[options.BaseAddress]</BaseAddress>
    <OutputPath>[options.OutputDirectory]</OutputPath>
    <IntermediateOutputPath>[options.IntermediateDirectory]</IntermediateOutputPath>
    <BaseIntermediateOutputPath>[options.BaseIntermediateOutputPath]</BaseIntermediateOutputPath>
    <DocumentationFile>[options.DocumentationFile]</DocumentationFile>
    <DefineConstants>[options.PreprocessorDefinitions]</DefineConstants>
    <ErrorReport>[options.ErrorReport]</ErrorReport>
    <WarningLevel>[options.WarningLevel]</WarningLevel>
    <AllowUnsafeBlocks>[options.AllowUnsafeBlocks]</AllowUnsafeBlocks>
    <TreatWarningsAsErrors>[options.TreatWarningsAsErrors]</TreatWarningsAsErrors>
    <WarningsNotAsErrors>[options.WarningsNotAsErrors]</WarningsNotAsErrors>
    <WarningsAsErrors>[options.WarningsAsErrors]</WarningsAsErrors>
    <CreateVsixContainer>[options.CreateVsixContainer]</CreateVsixContainer>
    <DeployExtension>[options.DeployExtension]</DeployExtension>
    <Prefer32Bit>[options.Prefer32Bit]</Prefer32Bit>
    <DisableFastUpToDateCheck>[options.DisableFastUpToDateCheck]</DisableFastUpToDateCheck>
    <NoWarn>[options.NoWarn]</NoWarn>
    <StartWorkingDirectory>[options.StartWorkingDirectory]</StartWorkingDirectory>
    <CodeAnalysisRuleSet>[conf.CodeAnalysisRuleSetFilePath]</CodeAnalysisRuleSet>
    <LangVersion>[options.LanguageVersion]</LangVersion>
    <CopyVsixExtensionFiles>[options.CopyVsixExtensionFiles]</CopyVsixExtensionFiles>
    <CopyVsixExtensionLocation>[options.CopyVsixExtensionLocation]</CopyVsixExtensionLocation>
    <ProduceReferenceAssembly>[options.ProduceReferenceAssembly]</ProduceReferenceAssembly>
";

                internal const string ImportProjectItemSimple =
@"  <Import Project=""[importProject]"" />
";
                internal const string ImportProjectItem =
@"  <Import Project=""[importProject]"" Condition=""[importCondition]"" />
";

                internal const string ImportProjectSdkItem =
@"  <Import Project=""[importProject]"" Sdk=""[sdkVersion]"" />
";

                internal const string VsixConfiguration =
@"  <PropertyGroup>
    <VSToolsPath Condition=""'$(VSToolsPath)' == ''"">$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)</VSToolsPath>
    <UseCodebase>true</UseCodebase>
  </PropertyGroup>
";

                internal const string ProjectConfigurationsPreBuildEvent =

@"  <PropertyGroup>
    <PreBuildEvent>[options.PreBuildEvent]
      <Message>[options.PreBuildEventDescription]</Message>
    </PreBuildEvent>
  </PropertyGroup>
";


                internal const string ProjectConfigurationsPostBuildEvent =
@"  <PropertyGroup>
    <PostBuildEvent>[options.PostBuildEvent]
      <Message>[options.PostBuildEventDescription]</Message>
    </PostBuildEvent>
  </PropertyGroup>
";

                internal const string ProjectConfigurationsPreBuildEventConditional =

@" <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <PreBuildEvent>
      [options.PreBuildEvent]
      <Message>[options.PreBuildEventDescription]</Message>
    </PreBuildEvent>
  </PropertyGroup>
";


                internal const string ProjectConfigurationsPostBuildEventConditional =
@"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <PostBuildEvent>
      [options.PostBuildEvent]
      <Message>[options.PostBuildEventDescription]</Message>
    </PostBuildEvent>
  </PropertyGroup>
";

                internal const string ProjectConfigurationsRunPostBuildEvent =
@"  <PropertyGroup>
    <RunPostBuildEvent>[RunPostBuildEvent]</RunPostBuildEvent>
  </PropertyGroup>
";

                internal const string ProjectAspNetMvcDescription =
@"  <PropertyGroup>
    <MvcBuildViews>[MvcBuildViews]</MvcBuildViews>
    <UseIISExpress>[UseIISExpress]</UseIISExpress>
    <IISExpressSSLPort>[IISExpressSSLPort]</IISExpressSSLPort>
    <IISExpressAnonymousAuthentication>[IISExpressAnonymousAuthentication]</IISExpressAnonymousAuthentication>
    <IISExpressWindowsAuthentication>[IISExpressWindowsAuthentication]</IISExpressWindowsAuthentication>
    <IISExpressUseClassicPipelineMode>[IISExpressUseClassicPipelineMode]</IISExpressUseClassicPipelineMode>
    <UseGlobalApplicationHostFile>[UseGlobalApplicationHostFile]</UseGlobalApplicationHostFile>
  </PropertyGroup>
";
            } // END of public static class Project

            internal const string ApplicationIcon =
@"  <PropertyGroup>
    <ApplicationIcon>[iconpath]</ApplicationIcon>
  </PropertyGroup>
";

            internal const string ApplicationManifest =
@"  <PropertyGroup>
    <ApplicationManifest>[applicationmanifest]</ApplicationManifest>
  </PropertyGroup>
";

            internal const string StartupObject =
@"  <PropertyGroup>
    <StartupObject>[startupobject]</StartupObject>
  </PropertyGroup>
";

            internal const string NoWin32Manifest =
@"  <PropertyGroup>
    <NoWin32Manifest>true</NoWin32Manifest>
  </PropertyGroup>
";

            internal const string MSBuild14PropertyGroup =
@"  <PropertyGroup>
    <!-- Find program files path in a 32-bit environment -->
    <ProgramFiles32>$(MSBuildProgramFiles32)</ProgramFiles32>
    <ProgramFiles32 Condition=""'$(ProgramFiles32)'==''"">$(ProgramFiles%28x86%29)</ProgramFiles32>
    <ProgramFiles32 Condition=""'$(ProgramFiles32)'=='' AND '$(PROCESSOR_ARCHITECTURE)'=='AMD64'"">$(ProgramFiles) (x86)</ProgramFiles32>
    <ProgramFiles32 Condition=""'$(ProgramFiles32)'==''"">$(ProgramFiles)</ProgramFiles32>
    <!-- Override default compiler path by the one provided in Microsoft Build Tools 2015 -->
    <CscToolPath Condition=""Exists('$(ProgramFiles32)\MSBuild\14.0\Bin\csc.exe')"">$(ProgramFiles32)\MSBuild\14.0\Bin</CscToolPath>
  </PropertyGroup>
";

            internal const string WebReferenceUrlBegin =
@"    <WebReferenceUrl Include=""[include]"">
";

            internal const string WebReferenceUrlEnd =
@"    </WebReferenceUrl>
";

            internal const string COMReference =
@"    <COMReference Include=""[include]"">
      <Guid>{[guid]}</Guid>
      <VersionMajor>[versionMajor]</VersionMajor>
      <VersionMinor>[versionMinor]</VersionMinor>
      <Lcid>[lcid]</Lcid>
      <WrapperTool>[wrapperTool]</WrapperTool>
      <Isolated>False</Isolated>
      <Private>[private]</Private>
      <EmbedInteropTypes>[EmbedInteropTypes]</EmbedInteropTypes>
    </COMReference>
";

            internal const string UrlBehavior =
                @"      <UrlBehavior>[urlBehavior]</UrlBehavior>
";

            internal const string RelPath =
@"      <RelPath>[relPath]</RelPath>
";

            internal const string UpdateFromURL =
@"      <UpdateFromURL>[updateFromURL]</UpdateFromURL>
";

            internal const string CachedDynamicPropName =
@"      <CachedDynamicPropName>[cachedDynamicPropName]</CachedDynamicPropName>";

            internal const string CachedAppSettingsObjectName =
@"      <CachedAppSettingsObjectName>[cachedAppSettingsObjectName]</CachedAppSettingsObjectName>
";

            internal const string CachedSettingsPropName =
@"      <CachedSettingsPropName>[cachedSettingsPropName]</CachedSettingsPropName>
";

            internal const string PropertyGroupWithConditionStart =
@"  <PropertyGroup Condition=""[projectConfigurationCondition]"">
";

            public static class ItemGroups
            {
                internal const string NoneItemGroupBegin =
@"    <None Include=""[include]"">
";
                internal const string NoneItemGroupEnd =
@"    </None>
";
                internal const string SimpleNone =
@"    <None Include=""[include]"" />
";
                internal const string SimpleNoneWithExclude =
@"    <None Include=""[include]"" Exclude=""[exclude]"" />
";
                internal const string Link =
@"      <Link>[link]</Link>
";

                internal const string LastGenOutput =
@"      <LastGenOutput>[lastGenOutput]</LastGenOutput>
";
                internal const string Generator =
@"      <Generator>[generator]</Generator>
";

                internal const string MergeWithCto =
@"      <MergeWithCTO>[mergeWithCto]</MergeWithCTO>
";

                internal const string DependentUpon =
@"      <DependentUpon>[dependentUpon]</DependentUpon>
";

                internal const string SpecificVersion =
@"      <SpecificVersion>[specificVersion]</SpecificVersion>
";

                internal const string HintPath =
@"      <HintPath>[hintPath]</HintPath>
";

                internal const string SimpleReference =
@"    <Reference Include=""[include]"" />
";

                internal const string ReferenceBegin =
@"    <Reference Include=""[include]"">
";

                internal const string ReferenceEnd =
@"    </Reference>
";

                internal const string ChooseBegin =
@"  <Choose>
";

                internal const string ChooseEnd =
@"  </Choose>
";

                internal const string ChooseConditionBegin =
@"    <When Condition="" [condition] "">
";

                internal const string ChooseConditionEnd =
@"    </When>
";

                internal const string ItemGroupBegin =
@"  <ItemGroup>
";

                internal const string ItemGroupConditionalBegin =
@"  <ItemGroup Condition=""[itemGroupCondition]"">
";

                internal const string ItemGroupTargetFrameworkCondition = "'$(TargetFramework)'=='[targetFramework]'";

                internal const string ItemGroupEnd =
@"  </ItemGroup>
";

                internal const string SplashScreen =
@"      <SplashScreen Include=""[include]"" />
";

                internal const string SimpleWebReference =
@"    <WebReferences Include=""[include]"" />
";

                internal const string Folder =
@"    <Folder Include=""[folder]"" />
";

                internal const string SimpleResource =
@"    <Resource Include=""[resource]"" />
";


                internal const string ResourceBegin =
@"    <Resource Include=""[include]"">
";

                internal const string ResourceEnd =
@"    </Resource>
";

                internal const string WCFMetadata =
@"    <WCFMetadata Include=""[baseStorage]"" />
";

                internal const string WCFMetadataStorage =
@"    <WCFMetadataStorage Include=""[storage]"" />
";

                internal const string VSIXSourceItem =
@"    <VSIXSourceItem Include=""[vsixSourceItem]"" />
";

                internal const string CompileRemove =
@"    <Compile Remove=""[remove]"" />
";

                internal const string NoneUpdate =
@"    <None Update=""[update]"">
      <CopyToOutputDirectory>[copyToOutputDirectory]</CopyToOutputDirectory>
    </None>
";

                internal const string ContentUpdate =
@"    <Content Update=""[update]"">
      <CopyToOutputDirectory>[copyToOutputDirectory]</CopyToOutputDirectory>
    </Content>
";

                internal const string NoneRemove =
@"    <None Remove=""[remove]"" />
";

                internal const string ContentRemove =
@"    <Content Remove=""[remove]"" />
";

                internal const string EmbeddedResourceRemove =
@"    <EmbeddedResource Remove=""[remove]"" />
";

                internal const string PageRemove =
@"    <Page Remove=""[remove]"" />
";

                internal const string ResourceRemove =
@"    <Resource Remove=""[remove]"" />
";

                internal const string PageBegin =
@"    <Page Include=""[include]"">
";
                internal const string PageEnd =
@"    </Page>
";
                internal const string ApplicationDefinitionBegin =
@"    <ApplicationDefinition Include=""[include]"">
";
                internal const string ApplicationDefinitionEnd =
@"    </ApplicationDefinition>
";
                internal const string SubType =
@"      <SubType>[subType]</SubType>
";

                internal const string SimpleEmbeddedResource =
@"    <EmbeddedResource Include=""[include]"" />
";

                internal const string EmbeddedResourceBegin =
@"    <EmbeddedResource Include=""[include]"">
";

                internal const string EmbeddedResourceEnd =
@"    </EmbeddedResource>
";
                internal const string BootstrapperPackage =
@"    <BootstrapperPackage Include=""[include]"">
      <Visible>[visible]</Visible>
      <ProductName>[productName]</ProductName>
      <Install>[install]</Install>
    </BootstrapperPackage>
";
                internal const string FileAssociationItem =
                    @"    <FileAssociation Include=""[include]"">
      <Visible>[visible]</Visible>
      <Description>[description]</Description>
      <Progid>[progid]</Progid>
      <DefaultIcon>[defaultIcon]</DefaultIcon>
    </FileAssociation>
";
                internal const string PublishFile =
                    @"    <PublishFile Include=""[include]"">
      <Visible>[visible]</Visible>
      <Group>[group]</Group>
      <PublishState>[publishState]</PublishState>
      <IncludeHash>[includeHash]</IncludeHash>
      <FileType>[fileType]</FileType>
    </PublishFile>
";

                internal const string ProjectReferenceBegin =
@"    <ProjectReference Include=""[include]"">
";

                internal const string ProjectReferenceSelfClosing =
@"    <ProjectReference Include=""[include]"" />
";

                internal const string ProjectGUID =
@"      <Project>[projectGUID]</Project>
";

                internal const string ProjectRefName =
@"      <Name>[projectRefName]</Name>
";
                internal const string Private =
@"      <Private>[private]</Private>
";
                internal const string EmbedInteropTypes =
@"      <EmbedInteropTypes>[embedInteropTypes]</EmbedInteropTypes>
";
                internal const string ReferenceOutputAssembly =
@"      <ReferenceOutputAssembly>[ReferenceOutputAssembly]</ReferenceOutputAssembly>
";
                internal const string IncludeOutputGroupsInVSIX =
@"      <IncludeOutputGroupsInVSIX>[IncludeOutputGroupsInVSIX]</IncludeOutputGroupsInVSIX>
";
                internal const string ProjectReferenceEnd =
@"    </ProjectReference>
";

                internal const string SimpleCompile =
@"    <Compile Include=""[include]"" />
";

                internal const string CompileBegin =
@"    <Compile Include=""[include]"">
";

                internal const string SimpleCompileWithExclude =
@"    <Compile Include=""[include]"" Exclude=""[exclude]""/>
";

                internal const string CompileBeginWithExclude =
@"    <Compile Include=""[include]"" Exclude=""[exclude]"">
";

                internal const string CompileEnd =
@"    </Compile>
";

                internal const string AutoGen =
@"      <AutoGen>[autoGen]</AutoGen>
";

                internal const string DesignTime =
@"      <DesignTime>[designTime]</DesignTime>
";

                internal const string DesignTimeSharedInput =
@"      <DesignTimeSharedInput>[designTimeSharedInput]</DesignTimeSharedInput>
";

                internal const string Service =
@"    <Service Include=""[include]"" />
";

                internal const string VsctCompileBegin =
@"    <VSCTCompile Include=""[include]"">
";

                internal const string VsdConfigXmlSimple =
@"    <VsdConfigXmlFiles Include=""[include]"" />
";

                internal const string ResourceName =
@"      <ResourceName>[resourceName]</ResourceName>
";

                internal const string VsctCompileEnd =
@"    </VSCTCompile>
";

                internal const string ContentSimple =
@"    <Content Include=""[include]"" />
";

                internal const string ContentBegin =
@"    <Content Include=""[include]"">
";

                internal const string CopyToOutputDirectory =
@"      <CopyToOutputDirectory>[copyToOutputDirectory]</CopyToOutputDirectory>
";

                internal const string Analyzer =
@"    <Analyzer Include=""[include]"" />
";

                internal const string IncludeInVsix =
@"      <IncludeInVSIX>[includeInVsix]</IncludeInVSIX>
";

                internal const string ContentEnd =
@"    </Content>
";
                internal const string EntityDeployBegin =
@"    <EntityDeploy Include=""[include]"">
";

                internal const string EntityDeployEnd =
@"    </EntityDeploy>
";

                internal const string FrameworkReference =
@"    <FrameworkReference Include=""[include]"" />
";

                internal const string Protobuf =
@"    <Protobuf Include=""[include]"" GrpcServices=""Both"" />
";
            }

            public static class UsingTaskElement
            {
                internal const string UsingTask =
@"  <UsingTask AssemblyFile=""[usingTaskElement.AssemblyFile]"" TaskName=""[usingTaskElement.TaskName]"" />
";
            }

            public static class TargetElement
            {
                internal const string CustomTargetNoParameters =
@"  <Target Name=""[targetElement.Name]"">
    [targetElement.CustomTasks]
  </Target>
";

                internal const string CustomTarget =
@"  <Target Name=""[targetElement.Name]"" [targetElement.TargetParameters]>
    [targetElement.CustomTasks]
  </Target>
";
            }

            internal const string ProjectExtensionsWcf =
@"  <ProjectExtensions>
      <VisualStudio>
        <FlavorProperties GUID = ""[WCFExtensionGUID]"">
          <WcfProjectProperties>
            <AutoStart>[AutoStart]</AutoStart>
          </WcfProjectProperties>
        </FlavorProperties>
    </VisualStudio>
  </ProjectExtensions>
";

            internal const string ProjectExtensionsVsto =
@"  <ProjectExtensions>
    <VisualStudio>
      <FlavorProperties GUID=""{BAA0C2D2-18E2-41B9-852F-F413020CAA33}"">
        <ProjectProperties HostName=""[OfficeApplication]"" HostPackage=""{29A7B9D7-A7F1-4328-8EF0-6B2D1A56B2C1}"" OfficeVersion=""[OfficeSDKVersion].0"" VstxVersion=""4.0"" ApplicationType=""[OfficeApplication]"" Language=""cs"" TemplatesPath="""" DebugInfoExeName=""#Software\Microsoft\Office\[OfficeSDKVersion].0\[OfficeApplication]\InstallRoot\Path#[OfficeApplication].exe"" AddItemTemplatesGuid=""{A58A78EB-1C92-4DDD-80CF-E8BD872ABFC4}"" />
        <Host Name=""[OfficeApplication]"" GeneratedCodeNamespace=""[AddInNamespace]"" IconIndex=""0"">
          <HostItem Name=""ThisAddIn"" Code=""ThisAddIn.cs"" CanonicalName=""AddIn"" CanActivate=""false"" IconIndex=""1"" Blueprint=""ThisAddIn.Designer.xml"" GeneratedCode=""ThisAddIn.Designer.cs"" />
        </Host>
      </FlavorProperties>
    </VisualStudio>
  </ProjectExtensions>
  <PropertyGroup>
    <OfficeApplication>[OfficeApplication]</OfficeApplication>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""Office, Version=[OfficeSDKVersion].0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"">
      <Private>False</Private>
      <EmbedInteropTypes>true</EmbedInteropTypes>
    </Reference>
    <Reference Include=""Microsoft.Office.Interop.[OfficeApplication], Version=[OfficeSDKVersion].0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"">
      <Private>False</Private>
      <EmbedInteropTypes>true</EmbedInteropTypes>
    </Reference>
    <Reference Include=""stdole, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"">
      <Private>False</Private>
    </Reference>
    <Reference Include=""Microsoft.Office.Tools.Common.v4.0.Utilities, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"">
      <Private>True</Private>
    </Reference>
    <Reference Include=""Microsoft.Office.Tools.[OfficeApplication].v4.0.Utilities, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"">
      <Private>True</Private>
    </Reference>
    <Reference Include=""Microsoft.Office.Tools.v4.0.Framework, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"" />
    <Reference Include=""Microsoft.VisualStudio.Tools.Applications.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"" />
    <Reference Include=""Microsoft.Office.Tools, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"" />
    <Reference Include=""Microsoft.Office.Tools.Common, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"" />
    <Reference Include=""Microsoft.Office.Tools.[OfficeApplication], Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL"" />
  </ItemGroup>
";

            internal const string ProjectExtensionsAspNetMvc5 =
@"  <ProjectExtensions>
    <VisualStudio>
      <FlavorProperties GUID=""[AspNetMvc5ExtensionGUID]"">
        <WebProjectProperties>
          <UseIIS>[UseIIS]</UseIIS>
          <AutoAssignPort>[AutoAssignPort]</AutoAssignPort>
          <DevelopmentServerPort>[DevelopmentServerPort]</DevelopmentServerPort>
          <DevelopmentServerVPath>[DevelopmentServerVPath]</DevelopmentServerVPath>
          <IISUrl>[IISUrl]</IISUrl>
          <NTLMAuthentication>[NTLMAuthentication]</NTLMAuthentication>
          <UseCustomServer>[UseCustomServer]</UseCustomServer>
          <CustomServerUrl></CustomServerUrl>
          <SaveServerSettingsInUserFile>[SaveServerSettingsInUserFile]</SaveServerSettingsInUserFile>
        </WebProjectProperties>
      </FlavorProperties>
    </VisualStudio>
  </ProjectExtensions>
";

            public static class UserFile
            {
                internal const string StartWithProject =
@"    <StartAction>Project</StartAction>
    <StartArguments>[conf.CsprojUserFile.StartArguments]</StartArguments>
    <StartWorkingDirectory>[conf.CsprojUserFile.WorkingDirectory]</StartWorkingDirectory>";

                internal const string StartWithProgram =
@"    <StartAction>Program</StartAction>
    <StartProgram>[conf.CsprojUserFile.StartProgram]</StartProgram>
    <StartArguments>[conf.CsprojUserFile.StartArguments]</StartArguments>
    <StartWorkingDirectory>[conf.CsprojUserFile.WorkingDirectory]</StartWorkingDirectory>";

                internal const string StartWithUrl =
@"    <StartAction>URL</StartAction>
    <StartURL>[conf.CsprojUserFile.StartURL]</StartURL>
    <StartArguments>[conf.CsprojUserFile.StartArguments]</StartArguments>
    <StartWorkingDirectory>[conf.CsprojUserFile.WorkingDirectory]</StartWorkingDirectory>";

                internal const string DebugUnmanaged =
@"    <EnableUnmanagedDebugging>[unmanagedDebugEnabled]</EnableUnmanagedDebugging>";
            }
        }
    }
}
