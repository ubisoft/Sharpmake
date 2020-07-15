// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
namespace Sharpmake.Generators.VisualStudio
{
    public partial class Pyproj
    {
        private class Template
        {
            public static class Project
            {
                public static string ProjectBegin =
                @"<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
";

                public static string ProjectEnd =
                @"</Project>
";

                public static string ProjectItemGroupBegin =
                @"  <ItemGroup>
";

                public static string ProjectItemGroupEnd =
                @"  </ItemGroup>
";


                public static string ProjectDescription =
@"  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
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
  <PropertyGroup Condition=""'$(Configuration)' == 'Debug'"" />
  <PropertyGroup>
    <VisualStudioVersion Condition="" '$(VisualStudioVersion)' == '' "">10.0</VisualStudioVersion>
    <PtvsTargetsFile>[ptvsTargetsFile]</PtvsTargetsFile>
  </PropertyGroup>
";

                public static string Interpreter =
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

                public static string VirtualEnvironmentInterpreter =
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

                public static string ImportPythonTools =
@"  <Import Project=""$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\Python Tools\Microsoft.PythonTools.targets"" />
";

                public static string InterpreterReference =
@"  <InterpreterReference Include=""[guid]\[version]"" />
";

                public static string ProjectReferenceBegin =
@"    <ProjectReference Include=""[include]"">
";

                public static string ProjectReferenceEnd =
@"    </ProjectReference>
";
                public static string Private =
@"      <Private>[private]</Private>
";
                public static string ReferenceOutputAssembly =
@"      <ReferenceOutputAssembly>[ReferenceOutputAssembly]</ReferenceOutputAssembly>
";
                public static string ProjectGUID =
@"      <Project>[projectGUID]</Project>
";
                public static string ProjectRefName =
@"      <Name>[projectRefName]</Name>
";
            }
        };
    }
}
