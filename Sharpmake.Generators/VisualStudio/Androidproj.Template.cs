// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Androidproj
    {
        private static class Template
        {
            public static class Project
            {
                internal const string ProjectBegin = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""[toolsVersion]"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
";

                internal const string ProjectEnd =
@"</Project>";

                internal const string ProjectBeginConfigurationDescription =
@"  <ItemGroup Label=""ProjectConfigurations"">
";

                internal const string ProjectEndConfigurationDescription =
@"  </ItemGroup>
";

                internal const string ProjectConfigurationDescription =
@"    <ProjectConfiguration Include=""[conf.Name]|[platformName]"">
      <Configuration>[conf.Name]</Configuration>
      <Platform>[platformName]</Platform>
    </ProjectConfiguration>
";

                internal const string ProjectDescription =
@"  <PropertyGroup Label=""Globals"">
    <AndroidBuildType>[androidBuildType]</AndroidBuildType>
    <ProjectGuid>{[guid]}</ProjectGuid>
    <RootNamespace>[projectName]</RootNamespace>
    <MinimumVisualStudioVersion>[toolsVersion]</MinimumVisualStudioVersion>
    <ProjectVersion>1.0</ProjectVersion>
    <ProjectName>[projectName]</ProjectName>
    <AndroidTargetsPath>[androidTargetsPath]</AndroidTargetsPath>
";

                internal const string ImportAndroidDefaultProps =
@"  <Import Project=""$(AndroidTargetsPath)\Android.Default.props"" />
";

                // The output directory is converted to a rooted path by prefixing it with $(ProjectDir) to work around
                // an issue with VS Android build scripts. When a project dependency has its project folder not at the
                // same folder level as the AndroidPackageProject, VS can't locate its output properly using its relative path.
                internal const string ProjectConfigurationsGeneral =
@"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" Label=""Configuration"">
    <UseDebugLibraries>[options.UseDebugLibraries]</UseDebugLibraries>
    <AndroidAPILevel>[options.AndroidAPILevel]</AndroidAPILevel>
    <OutDir>$(ProjectDir)[options.OutputDirectory]\</OutDir>
    <IntDir>[options.IntermediateDirectory]\</IntDir>
    <TargetName>[options.OutputFile]</TargetName>
    <ShowAndroidPathsVerbosity>[options.ShowAndroidPathsVerbosity]</ShowAndroidPathsVerbosity>
  </PropertyGroup>
";

                internal const string ProjectAfterConfigurationsGeneral =
@"  <Import Project=""$(AndroidTargetsPath)\Android.props"" />
  <ImportGroup Label=""ExtensionSettings"">
";

                internal const string ProjectAfterImportedProps =
@"  </ImportGroup>
    <ImportGroup Label=""Shared"" />
  <PropertyGroup Label=""UserMacros"" />
";

                internal const string ProjectConfigurationBeginItemDefinition =
@"  <ItemDefinitionGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
";

                internal const string ProjectConfigurationEndItemDefinition =
@"  </ItemDefinitionGroup>
";

                internal const string AntPackage =
@"    <AntPackage>
      <WorkingDirectory>[androidPackageDirectory]</WorkingDirectory>
      <AndroidAppLibName>[options.AndroidAppLibName]</AndroidAppLibName>
    </AntPackage>
";

                internal const string ProjectTargets =
@"  <Import Project=""$(AndroidTargetsPath)\Android.targets"" />
  <ImportGroup Label=""ExtensionTargets"" />
";

                internal const string AntBuildXml =
@"    <AntBuildXml Include=""[antBuildXml]"" />
";

                internal const string AntProjectPropertiesFile =
@"    <AntProjectPropertiesFile Include=""[antProjectPropertiesFile]"" />
";

                internal const string AndroidManifest =
@"    <AndroidManifest Include=""[androidManifest]"" />
";

                internal const string ItemGroupBegin =
@"  <ItemGroup>
";

                internal const string ItemGroupEnd =
@"  </ItemGroup>
";

                internal const string ProjectReference =
@"    <ProjectReference Include=""[include]"">
      <Project>{[projectGUID]}</Project>
    </ProjectReference>
";

                internal const string ProjectFilesBegin =
@"  <ItemGroup>
";

                internal const string ProjectFilesEnd =
@"  </ItemGroup>
";

                internal const string ProjectFilesHeader =
@"    <ClInclude Include=""[file.FileNameProjectRelative]"" />
";

                internal const string ContentSimple =
@"    <Content Include=""[file.FileNameSourceRelative]"" />
";

                internal const string GradleTemplate =
@"    <GradleTemplate Include=""[gradleTemplateFile]"" />
";

                internal const string GradlePackage =
@"    <GradlePackage>
      <GradlePlugin>[gradlePlugin]</GradlePlugin>
      <GradleVersion>[gradleVersion]</GradleVersion>
    </GradlePackage>
";
            }
        }
    }
}
