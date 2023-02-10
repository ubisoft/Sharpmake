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
    internal static class VsUtil
    {
        public static IEnumerable<Project.Configuration> SortConfigurations(
            IEnumerable<Project.Configuration> unsortedConfigurations,
            string projectFileFullPath
        )
        {
            // Need to sort by name and platform
            var configurations = new List<Project.Configuration>();
            configurations.AddRange(unsortedConfigurations.OrderBy(conf => conf.Name + Util.GetPlatformString(conf.Platform, conf.Project, conf.Target), StringComparer.OrdinalIgnoreCase));

            // Make sure that all configurations use the same project name,
            // and validate that 2 conf in the same project have a distinct tuple name + platform
            var configurationNameMapping = new Dictionary<string, Project.Configuration>();

            string projectName = null;

            foreach (Project.Configuration conf in configurations)
            {
                if (projectName == null)
                    projectName = conf.ProjectName;
                else if (projectName != conf.ProjectName)
                    throw new Error("Project configurations in the same project files must be the same: {0} != {1} in {2}", projectName, conf.ProjectName, projectFileFullPath);

                var projectUniqueName = conf.Name + Util.GetPlatformString(conf.Platform, conf.Project, conf.Target);

                Project.Configuration previousConf;
                if (configurationNameMapping.TryGetValue(projectUniqueName, out previousConf))
                {
                    throw new Error(
                        "Project '{0}' contains distinct configurations with the same name, please add something to distinguish them:\n- {1}",
                        projectFileFullPath,
                        string.Join(
                            Environment.NewLine + "- ",
                            configurations.Select(
                                pc => pc.Name + '|' + Util.GetPlatformString(pc.Platform, pc.Project, pc.Target) + $"  => '{pc.Target.GetTargetString()}'"
                            ).OrderBy(name => name)
                        )
                    );
                }
                configurationNameMapping[projectUniqueName] = conf;
            }

            return configurations;
        }
    }
}
