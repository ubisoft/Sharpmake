// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
            internal const string PropertyGroupStart =
                @"  <PropertyGroup>
";

            internal const string PropertyGroupWithConditionStart=
                @"  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"">
";

            internal const string PropertyGroupEnd =
                @"  </PropertyGroup>
";

            internal const string ItemGroupBegin =
                @"  <ItemGroup>
";

            internal const string ItemGroupEnd =
                @"  </ItemGroup>
";

            internal const string ItemDefinitionGroupBegin =
                @"  <ItemDefinitionGroup>
";

            internal const string ItemDefinitionGroupEnd =
                @"  </ItemDefinitionGroup>
";

            internal const string CustomProperty =
                @"    <[custompropertyname]>[custompropertyvalue]</[custompropertyname]>
";

            public static class Project
            {
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

                internal const string ProjectImportProps =
                    @"    <Import Project=""[importedPropsFile]"" />
";

                internal const string ProjectConfigurationImportProps =
                    @"    <Import Project=""[importedPropsFile]"" Condition=""'$(Configuration)|$(Platform)'=='[conf.Name]|[platformName]'"" />
";

                internal const string ProjectBuildMacroEnvironmentVariable =
                    @"    <BuildMacro Include=""[environmentVariableName]"">
      <Value>[environmentVariableValue]</Value>
      <EnvironmentVariable>true</EnvironmentVariable>
    </BuildMacro>
";
            }
        }
    }
}
