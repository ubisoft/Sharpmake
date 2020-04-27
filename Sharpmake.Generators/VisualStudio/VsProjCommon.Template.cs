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
