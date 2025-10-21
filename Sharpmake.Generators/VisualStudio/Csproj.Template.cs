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
                public static string ProjectBegin =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""[toolsVersion]"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
";

                public static string ProjectBeginVs2017 =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""[toolsVersion]"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
";
                public static string ProjectBeginNetCore =
@"<Project>
";

                public static string ProjectEnd =
@"</Project>";

                public static string ProjectDescription =
@"  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">[options.DefaultConfiguration]</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">[defaultPlatform]</Platform>
    <PlatformTarget Condition="" '$(Platform)' == '' "">[defaultPlatform]</PlatformTarget>
    <ProjectGuid>{[guid]}</ProjectGuid>
    <OutputType>[outputType]</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
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

                public const string DefaultProjectConfigurationCondition = "'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'";
                public const string MultiFrameworkProjectConfigurationCondition = "'$(Configuration)|$(Platform)|$(TargetFramework)'=='[conf.Name]|[platformName]|[targetFramework]'";

                public static string ProjectConfigurationsGeneral =
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

                public static string ImportProjectItemSimple =
@"  <Import Project=""[importProject]"" />
";
                public static string ImportProjectItem =
@"  <Import Project=""[importProject]"" Condition=""[importCondition]"" />
";

                public static string ImportProjectSdkItem =
@"  <Import Project=""[importProject]"" Sdk=""[sdkVersion]"" />
";

                public static string VsixConfiguration =
@"  <PropertyGroup>
    <VSToolsPath Condition=""'$(VSToolsPath)' == ''"">$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)</VSToolsPath>
    <UseCodebase>true</UseCodebase>
  </PropertyGroup>
";

                public static string ProjectConfigurationsPreBuildEvent =

@"  <PropertyGroup>
    <PreBuildEvent>[options.PreBuildEvent]
      <Message>[options.PreBuildEventDescription]</Message>
    </PreBuildEvent>
  </PropertyGroup>
";


                public static string ProjectConfigurationsPostBuildEvent =
@"  <PropertyGroup>
    <PostBuildEvent>[options.PostBuildEvent]
      <Message>[options.PostBuildEventDescription]</Message>
    </PostBuildEvent>
  </PropertyGroup>
";

                public static string ProjectConfigurationsPreBuildEventConditional =

@" <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <PreBuildEvent>
      [options.PreBuildEvent]
      <Message>[options.PreBuildEventDescription]</Message>
    </PreBuildEvent>
  </PropertyGroup>
";


                public static string ProjectConfigurationsPostBuildEventConditional =
@"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
    <PostBuildEvent>
      [options.PostBuildEvent]
      <Message>[options.PostBuildEventDescription]</Message>
    </PostBuildEvent>
  </PropertyGroup>
";

                public static string ProjectConfigurationsRunPostBuildEvent =
@"  <PropertyGroup>
    <RunPostBuildEvent>[RunPostBuildEvent]</RunPostBuildEvent>
  </PropertyGroup>
";

                public static string ProjectAspNetMvcDescription =
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

            public static string ApplicationIcon =
@"  <PropertyGroup>
    <ApplicationIcon>[iconpath]</ApplicationIcon>
  </PropertyGroup>
";

            public static string ApplicationManifest =
@"  <PropertyGroup>
    <ApplicationManifest>[applicationmanifest]</ApplicationManifest>
  </PropertyGroup>
";

            public static string StartupObject =
@"  <PropertyGroup>
    <StartupObject>[startupobject]</StartupObject>
  </PropertyGroup>
";

            public static string NoWin32Manifest =
@"  <PropertyGroup>
    <NoWin32Manifest>true</NoWin32Manifest>
  </PropertyGroup>
";

            public static string MSBuild14PropertyGroup =
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

            public static string WebReferenceUrlBegin =
@"    <WebReferenceUrl Include=""[include]"">
";

            public static string WebReferenceUrlEnd =
@"    </WebReferenceUrl>
";

            public static string COMReference =
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

            public static string UrlBehavior =
                @"      <UrlBehavior>[urlBehavior]</UrlBehavior>
";

            public static string RelPath =
@"      <RelPath>[relPath]</RelPath>
";

            public static string UpdateFromURL =
@"      <UpdateFromURL>[updateFromURL]</UpdateFromURL>
";

            public static string CachedDynamicPropName =
@"      <CachedDynamicPropName>[cachedDynamicPropName]</CachedDynamicPropName>";

            public static string CachedAppSettingsObjectName =
@"      <CachedAppSettingsObjectName>[cachedAppSettingsObjectName]</CachedAppSettingsObjectName>
";

            public static string CachedSettingsPropName =
@"      <CachedSettingsPropName>[cachedSettingsPropName]</CachedSettingsPropName>
";

            public static string PropertyGroupWithConditionStart =
@"  <PropertyGroup Condition=""[projectConfigurationCondition]"">
";

            public static class ItemGroups
            {
                public static string NoneItemGroupBegin =
@"    <None Include=""[include]"">
";
                public static string NoneItemGroupEnd =
@"    </None>
";
                public static string SimpleNone =
@"    <None Include=""[include]"" />
";
                public static string Link =
@"      <Link>[link]</Link>
";

                public static string LastGenOutput =
@"      <LastGenOutput>[lastGenOutput]</LastGenOutput>
";
                public static string Generator =
@"      <Generator>[generator]</Generator>
";

                public static string MergeWithCto =
@"      <MergeWithCTO>[mergeWithCto]</MergeWithCTO>
";

                public static string DependentUpon =
@"      <DependentUpon>[dependentUpon]</DependentUpon>
";

                public static string SpecificVersion =
@"      <SpecificVersion>[specificVersion]</SpecificVersion>
";

                public static string HintPath =
@"      <HintPath>[hintPath]</HintPath>
";

                public static string SimpleReference =
@"    <Reference Include=""[include]"" />
";

                public static string ReferenceBegin =
@"    <Reference Include=""[include]"">
";

                public static string ReferenceEnd =
@"    </Reference>
";

                public static string ChooseBegin =
@"  <Choose>
";

                public static string ChooseEnd =
@"  </Choose>
";

                public static string ChooseConditionBegin =
@"    <When Condition="" [condition] "">
";

                public static string ChooseConditionEnd =
@"    </When>
";

                public static string ItemGroupBegin =
@"  <ItemGroup>
";

                public static string ItemGroupConditionalBegin =
@"  <ItemGroup Condition=""[itemGroupCondition]"">
";

                public static string ItemGroupTargetFrameworkCondition = "'$(TargetFramework)'=='[targetFramework]'";

                public static string ItemGroupEnd =
@"  </ItemGroup>
";

                public static string SplashScreen =
@"      <SplashScreen Include=""[include]"" />
";

                public static string SimpleWebReference =
@"    <WebReferences Include=""[include]"" />
";

                public static string Folder =
@"    <Folder Include=""[folder]"" />
";

                public static string SimpleResource =
@"    <Resource Include=""[resource]"" />
";


                public static string ResourceBegin =
@"    <Resource Include=""[include]"">
";

                public static string ResourceEnd =
@"    </Resource>
";

                public static string WCFMetadata =
@"    <WCFMetadata Include=""[baseStorage]"" />
";

                public static string WCFMetadataStorage =
@"    <WCFMetadataStorage Include=""[storage]"" />
";

                public static string VSIXSourceItem =
@"    <VSIXSourceItem Include=""[vsixSourceItem]"" />
";

                public static string PageBegin =
@"    <Page Include=""[include]"">
";
                public static string PageEnd =
@"    </Page>
";
                public static string ApplicationDefinitionBegin =
@"    <ApplicationDefinition Include=""[include]"">
";
                public static string ApplicationDefinitionEnd =
@"    </ApplicationDefinition>
";
                public static string SubType =
@"      <SubType>[subType]</SubType>
";

                public static string SimpleEmbeddedResource =
@"    <EmbeddedResource Include=""[include]"" />
";

                public static string EmbeddedResourceBegin =
@"    <EmbeddedResource Include=""[include]"">
";

                public static string EmbeddedResourceEnd =
@"    </EmbeddedResource>
";
                public static string BootstrapperPackage =
@"    <BootstrapperPackage Include=""[include]"">
      <Visible>[visible]</Visible>
      <ProductName>[productName]</ProductName>
      <Install>[install]</Install>
    </BootstrapperPackage>
";
                public static string FileAssociationItem =
                    @"    <FileAssociation Include=""[include]"">
      <Visible>[visible]</Visible>
      <Description>[description]</Description>
      <Progid>[progid]</Progid>
      <DefaultIcon>[defaultIcon]</DefaultIcon>
    </FileAssociation>
";
                public static string PublishFile =
                    @"    <PublishFile Include=""[include]"">
      <Visible>[visible]</Visible>
      <Group>[group]</Group>
      <PublishState>[publishState]</PublishState>
      <IncludeHash>[includeHash]</IncludeHash>
      <FileType>[fileType]</FileType>
    </PublishFile>
";

                public static string ProjectReferenceBegin =
@"    <ProjectReference Include=""[include]"">
";

                public static string ProjectGUID =
@"      <Project>[projectGUID]</Project>
";

                public static string ProjectRefName =
@"      <Name>[projectRefName]</Name>
";
                public static string Private =
@"      <Private>[private]</Private>
";
                public static string EmbedInteropTypes =
@"      <EmbedInteropTypes>[embedInteropTypes]</EmbedInteropTypes>
";
                public static string ReferenceOutputAssembly =
@"      <ReferenceOutputAssembly>[ReferenceOutputAssembly]</ReferenceOutputAssembly>
";
                public static string IncludeOutputGroupsInVSIX =
@"      <IncludeOutputGroupsInVSIX>[IncludeOutputGroupsInVSIX]</IncludeOutputGroupsInVSIX>
";
                public static string ProjectReferenceEnd =
@"    </ProjectReference>
";

                public static string SimpleCompile =
@"    <Compile Include=""[include]"" />
";

                public static string CompileBegin =
@"    <Compile Include=""[include]"">
";

                public static string SimpleCompileWithExclude =
@"    <Compile Include=""[include]"" Exclude=""[exclude]""/>
";

                public static string CompileBeginWithExclude =
@"    <Compile Include=""[include]"" Exclude=""[exclude]"">
";

                public static string CompileEnd =
@"    </Compile>
";

                public static string AutoGen =
@"      <AutoGen>[autoGen]</AutoGen>
";

                public static string DesignTime =
@"      <DesignTime>[designTime]</DesignTime>
";

                public static string DesignTimeSharedInput =
@"      <DesignTimeSharedInput>[designTimeSharedInput]</DesignTimeSharedInput>
";

                public static string Service =
@"    <Service Include=""[include]"" />
";

                public static string VsctCompileBegin =
@"    <VSCTCompile Include=""[include]"">
";

                public static string VsdConfigXmlSimple =
@"    <VsdConfigXmlFiles Include=""[include]"" />
";

                public static string ResourceName =
@"      <ResourceName>[resourceName]</ResourceName>
";

                public static string VsctCompileEnd =
@"    </VSCTCompile>
";

                public static string ContentSimple =
@"    <Content Include=""[include]"" />
";

                public static string ContentBegin =
@"    <Content Include=""[include]"">
";

                public static string CopyToOutputDirectory =
@"      <CopyToOutputDirectory>[copyToOutputDirectory]</CopyToOutputDirectory>
";

                public static string Analyzer =
@"    <Analyzer Include=""[include]"" />
";

                public static string IncludeInVsix =
@"      <IncludeInVSIX>[includeInVsix]</IncludeInVSIX>
";

                public static string ContentEnd =
@"    </Content>
";
                public static string EntityDeployBegin =
@"    <EntityDeploy Include=""[include]"">
";

                public static string EntityDeployEnd =
@"    </EntityDeploy>
";

                public static string FrameworkReference =
@"    <FrameworkReference Include=""[include]"" />
";

                public static string Protobuf =
@"    <Protobuf Include=""[include]"" GrpcServices=""Both"" />
";
            }

            public static class UsingTaskElement
            {
                public static string UsingTask =
@"  <UsingTask AssemblyFile=""[usingTaskElement.AssemblyFile]"" TaskName=""[usingTaskElement.TaskName]"" />
";
            }

            public static class TargetElement
            {
                public static string CustomTargetNoParameters =
@"  <Target Name=""[targetElement.Name]"">
    [targetElement.CustomTasks]
  </Target>
";

                public static string CustomTarget =
@"  <Target Name=""[targetElement.Name]"" [targetElement.TargetParameters]>
    [targetElement.CustomTasks]
  </Target>
";
            }

            public static string ProjectExtensionsWcf =
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

            public static string ProjectExtensionsVsto =
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

            public static string ProjectExtensionsAspNetMvc5 =
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
                public static readonly string StartWithProject =
@"    <StartAction>Project</StartAction>
    <StartArguments>[conf.CsprojUserFile.StartArguments]</StartArguments>
    <StartWorkingDirectory>[conf.CsprojUserFile.WorkingDirectory]</StartWorkingDirectory>";

                public static readonly string StartWithProgram =
@"    <StartAction>Program</StartAction>
    <StartProgram>[conf.CsprojUserFile.StartProgram]</StartProgram>
    <StartArguments>[conf.CsprojUserFile.StartArguments]</StartArguments>
    <StartWorkingDirectory>[conf.CsprojUserFile.WorkingDirectory]</StartWorkingDirectory>";

                public static readonly string StartWithUrl =
@"    <StartAction>URL</StartAction>
    <StartURL>[conf.CsprojUserFile.StartURL]</StartURL>
    <StartArguments>[conf.CsprojUserFile.StartArguments]</StartArguments>
    <StartWorkingDirectory>[conf.CsprojUserFile.WorkingDirectory]</StartWorkingDirectory>";

                public static readonly string DebugUnmanaged =
@"    <EnableUnmanagedDebugging>[unmanagedDebugEnabled]</EnableUnmanagedDebugging>";
            }
        }
    }
}
