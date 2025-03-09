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
                public const string ProjectBegin = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""[toolsVersion]"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
";

                public const string ProjectEnd =
@"</Project>";

                public const string ProjectBeginConfigurationDescription =
@"  <ItemGroup Label=""ProjectConfigurations"">
";

                public const string ProjectEndConfigurationDescription =
@"  </ItemGroup>
";

                public const string ProjectConfigurationDescription =
@"    <ProjectConfiguration Include=""[conf.Name]|[platformName]"">
      <Configuration>[conf.Name]</Configuration>
      <Platform>[platformName]</Platform>
    </ProjectConfiguration>
";

                public const string ProjectDescription =
@"  <PropertyGroup Label=""Globals"">
    <AndroidBuildType>[androidBuildType]</AndroidBuildType>
    <ProjectGuid>{[guid]}</ProjectGuid>
    <RootNamespace>[projectName]</RootNamespace>
    <MinimumVisualStudioVersion>[toolsVersion]</MinimumVisualStudioVersion>
    <ProjectVersion>1.0</ProjectVersion>
    <ProjectName>[projectName]</ProjectName>
    <AndroidTargetsPath>[androidTargetsPath]</AndroidTargetsPath>
";

                public const string ImportAndroidDefaultProps =
@"  <Import Project=""$(AndroidTargetsPath)\Android.Default.props"" />
";

                // The output directory is converted to a rooted path by prefixing it with $(ProjectDir) to work around
                // an issue with VS Android build scripts. When a project dependency has its project folder not at the
                // same folder level as the AndroidPackageProject, VS can't locate its output properly using its relative path.
                public const string ProjectConfigurationsGeneral =
@"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" Label=""Configuration"">
    <UseDebugLibraries>[options.UseDebugLibraries]</UseDebugLibraries>
    <AndroidAPILevel>[options.AndroidAPILevel]</AndroidAPILevel>
    <OutDir>$(ProjectDir)[options.OutputDirectory]\</OutDir>
    <IntDir>[options.IntermediateDirectory]\</IntDir>
    <TargetName>[options.OutputFile]</TargetName>
    <ShowAndroidPathsVerbosity>[options.ShowAndroidPathsVerbosity]</ShowAndroidPathsVerbosity>
  </PropertyGroup>
";

                public const string ProjectAfterConfigurationsGeneral =
@"  <Import Project=""$(AndroidTargetsPath)\Android.props"" />
  <ImportGroup Label=""ExtensionSettings"">
";

                public const string ProjectAfterImportedProps =
@"  </ImportGroup>
    <ImportGroup Label=""Shared"" />
  <PropertyGroup Label=""UserMacros"" />
";

                public const string ProjectConfigurationBeginItemDefinition =
@"  <ItemDefinitionGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
";

                public const string ProjectConfigurationEndItemDefinition =
@"  </ItemDefinitionGroup>
";

                public const string AntPackage =
@"    <AntPackage>
      <WorkingDirectory>[androidPackageDirectory]</WorkingDirectory>
      <AndroidAppLibName>[options.AndroidAppLibName]</AndroidAppLibName>
    </AntPackage>
";

                public const string ProjectTargets =
@"  <Import Project=""$(AndroidTargetsPath)\Android.targets"" />
  <ImportGroup Label=""ExtensionTargets"" />
";

                public const string AntBuildXml =
@"    <AntBuildXml Include=""[antBuildXml]"" />
";

                public const string AntProjectPropertiesFile =
@"    <AntProjectPropertiesFile Include=""[antProjectPropertiesFile]"" />
";

                public const string AndroidManifest =
@"    <AndroidManifest Include=""[androidManifest]"" />
";

                public const string ItemGroupBegin =
@"  <ItemGroup>
";

                public const string ItemGroupEnd =
@"  </ItemGroup>
";

                public const string ProjectReference =
@"    <ProjectReference Include=""[include]"">
      <Project>{[projectGUID]}</Project>
    </ProjectReference>
";

                public const string ProjectFilesBegin =
@"  <ItemGroup>
";

                public const string ProjectFilesEnd =
@"  </ItemGroup>
";

                public const string ProjectFilesHeader =
@"    <ClInclude Include=""[file.FileNameProjectRelative]"" />
";

                public static string ContentSimple =
@"    <Content Include=""[file.FileNameSourceRelative]"" />
";

                public static string GradleTemplate =
@"    <GradleTemplate Include=""[gradleTemplateFile]"" />
";

                public static string GradlePackage =
@"    <GradlePackage>
      <GradlePlugin>[gradlePlugin]</GradlePlugin>
      <GradleVersion>[gradleVersion]</GradleVersion>
    </GradlePackage>
";
            }
        }
    }
}
