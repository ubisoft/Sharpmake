// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Pyproj
    {
        private class Template
        {
            public static class Project
            {
                internal const string ProjectBegin =
                @"<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
";

                internal const string ProjectEnd =
                @"</Project>
";

                internal const string ProjectItemGroupBegin =
                @"  <ItemGroup>
";

                internal const string ProjectItemGroupEnd =
                @"  </ItemGroup>
";


                internal const string ProjectDescription =
@"  <PropertyGroup>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>[guid]</ProjectGuid>
    <ProjectHome>[projectHome]</ProjectHome>
    <StartupFile>[startupFile]</StartupFile>
    <SearchPath>[searchPath]</SearchPath>
    <WorkingDirectory>.</WorkingDirectory>
    <OutputPath>.</OutputPath>
    <ProjectTypeGuids>{888888a0-9f3d-457c-b088-3a5042f75d52}</ProjectTypeGuids>
    <LaunchProvider>Standard Python launcher</LaunchProvider>
    <InterpreterId>[interpreterId]</InterpreterId>
    <InterpreterVersion>[interpreterVersion]</InterpreterVersion>
  </PropertyGroup>
  <PropertyGroup>
    <VisualStudioVersion Condition="" '$(VisualStudioVersion)' == '' "">10.0</VisualStudioVersion>
    <PtvsTargetsFile>[ptvsTargetsFile]</PtvsTargetsFile>
  </PropertyGroup>
";

                internal const string Interpreter =
@"  <Interpreter Include=""[basePath]"">
    <Id>{[guid]}</Id>
    <Version>[version]</Version>
    <Description>Python [version]</Description>
    <InterpreterPath>[basePath]Scripts\python.exe</InterpreterPath>
    <WindowsInterpreterPath>[basePath]Scripts\pythonw.exe</WindowsInterpreterPath>
    <LibraryPath>[basePath]Lib\</LibraryPath>
    <PathEnvironmentVariable>PYTHONPATH</PathEnvironmentVariable>
    <Architecture>X86</Architecture>
  </Interpreter>
";

                internal const string VirtualEnvironmentInterpreter =
@"    <Interpreter Include=""[basePath]"">
      <Id>{[guid]}</Id>
      <BaseInterpreter>[baseGuid]</BaseInterpreter>
      <Version>[version]</Version>
      <Description>[name]</Description>
      <InterpreterPath>Scripts\python.exe</InterpreterPath>
      <WindowsInterpreterPath>Scripts\pythonw.exe</WindowsInterpreterPath>
      <LibraryPath>Lib\</LibraryPath>
      <PathEnvironmentVariable>PYTHONPATH</PathEnvironmentVariable>
      <Architecture>X86</Architecture>
    </Interpreter>
";

                internal const string ImportPythonTools =
@"  <Import Project=""$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\Python Tools\Microsoft.PythonTools.targets"" />
";

                internal const string InterpreterReference =
@"  <InterpreterReference Include=""[guid]\[version]"" />
";

                internal const string ProjectReferenceBegin =
@"    <ProjectReference Include=""[include]"">
";

                internal const string ProjectReferenceEnd =
@"    </ProjectReference>
";
                internal const string Private =
@"      <Private>[private]</Private>
";
                internal const string ReferenceOutputAssembly =
@"      <ReferenceOutputAssembly>[ReferenceOutputAssembly]</ReferenceOutputAssembly>
";
                internal const string ProjectGUID =
@"      <Project>[projectGUID]</Project>
";
                internal const string ProjectRefName =
@"      <Name>[projectRefName]</Name>
";
            }
        };
    }
}
