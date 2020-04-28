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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpmake.Generators.VisualStudio
{
    internal static partial class VsProjCommon
    {
        public static class Template
        {
            public static string PropertyGroupStart =
                @"  <PropertyGroup>
";

            public static string PropertyGroupEnd =
                @"  </PropertyGroup>
";

            public static string ItemGroupBegin =
                @"  <ItemGroup>
";

            public static string ItemGroupEnd =
                @"  </ItemGroup>
";

            public static string CustomProperty =
                @"    <[custompropertyname]>[custompropertyvalue]</[custompropertyname]>
";

            public static class Project
            {
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

                public static string ProjectImportProps =
                    @"    <Import Project=""[importedPropsFile]"" />
";

                public static string ProjectConfigurationImportProps =
                    @"    <Import Project=""[importedPropsFile]"" Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" />
";

                public static string ProjectBuildMacroEnvironmentVariable =
                    @"    <BuildMacro Include=""[environmentVariableName]"">
      <Value>[environmentVariableValue]</Value>
      <EnvironmentVariable>true</EnvironmentVariable>
    </BuildMacro>
";
            }
        }
    }
}
