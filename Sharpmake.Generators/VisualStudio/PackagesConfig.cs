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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sharpmake.Generators.VisualStudio
{
    // The packages.config file maintains a list of packages used in a project.
    // It is superseded by project.json with NuGet 3.0+, but the latter discards certain functionality.
    // This code should be used primarily for getting old projects to work.
    // https://docs.microsoft.com/en-us/nuget/schema/project-json
    internal partial class PackagesConfig
    {
        private Builder _builder;
        private string _projectPath;

        private static readonly HashSet<string> s_packagesConfigGenerated = new HashSet<string>();
        private static readonly object s_packagesConfigLock = new object();

        public bool IsGenerated { get; internal set; } = false;
        public string PackagesConfigPath => Path.Combine(_projectPath, "packages.config");
        public void Generate(Builder builder, CSharpProject project, List<Project.Configuration> configurations, string projectPath, List<string> generatedFiles, List<string> skipFiles)
        {
            _builder = builder;
            _projectPath = projectPath;

            var configuration = configurations[0];
            var frameworkFlags = project.Targets.TargetPossibilities.Select(f => f.GetFragment<DotNetFramework>()).Aggregate((x, y) => x | y);
            GeneratePackagesConfig(configuration, frameworkFlags, generatedFiles, skipFiles);

            _builder = null;
        }

        private static bool IsGenerateNeeded(string packagesConfigPath)
        {
            if (!s_packagesConfigGenerated.Contains(packagesConfigPath))
            {
                s_packagesConfigGenerated.Add(packagesConfigPath);
                return true;
            }

            return false;
        }

        private void GeneratePackagesConfig(Project.Configuration conf, DotNetFramework frameworks, List<string> generatedFiles, List<string> skipFiles)
        {
            var packagesConfigPath = PackagesConfigPath;

            // No NuGet references and no trace of a previous packages.config
            if (conf.ReferencesByNuGetPackage.Count == 0)
            {
                if (!File.Exists(packagesConfigPath))
                    return;
            }

            lock (s_packagesConfigLock)
            {
                if (conf.ReferencesByNuGetPackage.Count == 0)
                {
                    var fi = new FileInfo(packagesConfigPath);
                    if (!fi.IsReadOnly) // Do not delete packages.config submitted in P4
                        Util.TryDeleteFile(packagesConfigPath);
                    return;
                }

                if (IsGenerateNeeded(packagesConfigPath))
                {
                    FileGenerator fileGenerator = new FileGenerator();

                    fileGenerator.Write(Template.Begin);

                    // dependencies
                    DotNetFramework dnfs = ((DotNetFramework[])Enum.GetValues(typeof(DotNetFramework))).First(f => frameworks.HasFlag(f));
                    for (int i = 0; i < conf.ReferencesByNuGetPackage.SortedValues.Count; ++i)
                    {
                        using (fileGenerator.Declare("dependency", conf.ReferencesByNuGetPackage.SortedValues[i]))
                        using (fileGenerator.Declare("framework", dnfs.ToFolderName()))
                            fileGenerator.Write(Template.DependenciesItem);
                    }

                    fileGenerator.Write(Template.End);

                    bool written = _builder.Context.WriteGeneratedFile(GetType(), new FileInfo(packagesConfigPath), fileGenerator.ToMemoryStream());
                    if (written)
                        generatedFiles.Add(packagesConfigPath);
                    else
                        skipFiles.Add(packagesConfigPath);
                }
            }

            IsGenerated = true;
        }
    }
}
