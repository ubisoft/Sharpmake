// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpmake.Generators.VisualStudio
{
    internal static partial class VsProjCommon
    {
        public static void WriteCustomProperties(Dictionary<string, string> customProperties, IFileGenerator fileGenerator)
            => WritePropertyGroup(customProperties, Template.PropertyGroupStart, fileGenerator);

        public static void WriteConfigurationsCustomProperties(Project.Configuration conf, IFileGenerator fileGenerator) 
            => WritePropertyGroup(conf.CustomProperties, Template.PropertyGroupWithConditionStart, fileGenerator);
        
        private static void WritePropertyGroup(
            Dictionary<string, string> props,
            string headerTemplate,
            IFileGenerator fileGenerator
        )
        {
            if (props.Any())
            {
                fileGenerator.Write(headerTemplate);
                foreach (KeyValuePair<string, string> kvp in props)
                {
                    using (fileGenerator.Declare("custompropertyname", kvp.Key))
                    using (fileGenerator.Declare("custompropertyvalue", kvp.Value))
                    {
                        fileGenerator.Write(VsProjCommon.Template.CustomProperty);
                    }
                }
                fileGenerator.Write(Template.PropertyGroupEnd);
            }
        }

        public static void WriteProjectConfigurationsDescription(IEnumerable<Project.Configuration> configurations, IFileGenerator fileGenerator)
        {
            fileGenerator.Write(Template.Project.ProjectBeginConfigurationDescription);

            var platformNames = new Strings();
            var configNames = new Strings();
            foreach (var conf in configurations)
            {
                var platformName = Util.GetPlatformString(conf.Platform, conf.Project, conf.Target);
                platformNames.Add(platformName);
                configNames.Add(conf.Name);
            }

            // write all combinations to avoid "Incomplete Configuration" VS warning
            foreach (var configName in configNames.SortedValues)
            {
                foreach (var platformName in platformNames.SortedValues)
                {
                    using (fileGenerator.Declare("platformName", platformName))
                    using (fileGenerator.Declare("configName", configName))
                    {
                        fileGenerator.Write(Template.Project.ProjectConfigurationDescription);
                    }
                }
            }

            fileGenerator.Write(Template.Project.ProjectEndConfigurationDescription);
        }

        public static void WriteProjectCustomPropsFiles(
            IEnumerable<string> customProps,
            string projectDirectoryCapitalized,
            IFileGenerator fileGenerator
        )
        {
            foreach (string propsFile in customProps)
            {
                string capitalizedFile = Project.GetCapitalizedFile(propsFile) ?? propsFile;

                string relativeFile = Util.PathGetRelative(projectDirectoryCapitalized, capitalizedFile);
                using (fileGenerator.Declare("importedPropsFile", relativeFile))
                {
                    fileGenerator.Write(Template.Project.ProjectImportProps);
                }
            }
        }

        public static void WriteConfigurationsCustomPropsFiles(
            IEnumerable<Project.Configuration> configurations,
            string projectDirectoryCapitalized,
            IFileGenerator fileGenerator
        )
        {
            foreach (Project.Configuration conf in configurations)
            {
                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
                using (fileGenerator.Declare("conf", conf))
                {
                    foreach (string propsFile in conf.CustomPropsFiles)
                    {
                        string capitalizedFile = Project.GetCapitalizedFile(propsFile) ?? propsFile;

                        string relativeFile = Util.PathGetRelative(projectDirectoryCapitalized, capitalizedFile);
                        using (fileGenerator.Declare("importedPropsFile", relativeFile))
                        {
                            fileGenerator.Write(Template.Project.ProjectConfigurationImportProps);
                        }
                    }
                }
            }
        }

        public static void WriteEnvironmentVariables(
            IEnumerable<VariableAssignment> environmentVariables,
            IFileGenerator fileGenerator
        )
        {
            if (!environmentVariables.Any())
                return;

            fileGenerator.Write(Template.ItemGroupBegin);
            foreach (var environmentTuple in environmentVariables)
            {
                using (fileGenerator.Declare("environmentVariableName", environmentTuple.Identifier))
                using (fileGenerator.Declare("environmentVariableValue", environmentTuple.Value))
                    fileGenerator.Write(Template.Project.ProjectBuildMacroEnvironmentVariable);
            }
            fileGenerator.Write(Template.ItemGroupEnd);
        }
    }
}
