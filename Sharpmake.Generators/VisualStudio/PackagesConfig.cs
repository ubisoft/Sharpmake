// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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

        public void Generate(Builder builder, CSharpProject project, List<Project.Configuration> configurations, string projectPath, IList<string> generatedFiles, IList<string> skipFiles)
        {
            var configuration = configurations[0];
            var frameworkFlags = project.Targets.TargetPossibilities.Select(f => f.GetFragment<DotNetFramework>()).Aggregate((x, y) => x | y);
            var frameworks = ((DotNetFramework[])Enum.GetValues(typeof(DotNetFramework))).Where(f => frameworkFlags.HasFlag(f)).Select(f => f.ToFolderName());

            Generate(builder, configuration, frameworks, projectPath, generatedFiles, skipFiles);
        }

        public void Generate(Builder builder, Project.Configuration configuration, string framework, string projectPath, IList<string> generatedFiles, IList<string> skipFiles)
        {
            Generate(builder, configuration, new[] { framework }, projectPath, generatedFiles, skipFiles);
        }

        public void Generate(Builder builder, Project.Configuration configuration, IEnumerable<string> frameworks, string projectPath, IList<string> generatedFiles, IList<string> skipFiles)
        {
            _builder = builder;
            _projectPath = projectPath;

            GeneratePackagesConfig(configuration, frameworks, generatedFiles, skipFiles);

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

        private void GeneratePackagesConfig(Project.Configuration conf, IEnumerable<string> frameworks, IList<string> generatedFiles, IList<string> skipFiles)
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
                    for (int i = 0; i < conf.ReferencesByNuGetPackage.SortedValues.Count; ++i)
                    {
                        using (fileGenerator.Declare("dependency", conf.ReferencesByNuGetPackage.SortedValues[i]))
                        {
                            foreach (var framework in frameworks)
                            {
                                using (fileGenerator.Declare("framework", framework))
                                    fileGenerator.Write(Template.DependenciesItem);
                            }
                        }
                    }

                    fileGenerator.Write(Template.End);

                    bool written = _builder.Context.WriteGeneratedFile(GetType(), new FileInfo(packagesConfigPath), fileGenerator);
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
