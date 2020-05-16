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
    // The project.json file maintains a list of packages used in a project, known as a package reference format.
    // It supersedes packages.config but is in turn superseded by PackageReference with NuGet 4.0+.
    // https://docs.microsoft.com/en-us/nuget/schema/project-json
    internal partial class ProjectJson
    {
        private Builder _builder;
        private string _projectPath;

        private static readonly HashSet<string> s_projectJsonGenerated = new HashSet<string>();
        private static readonly object s_projectJsonLock = new object();

        public bool IsGenerated { get; internal set; } = false;
        public string ProjectJsonPath => Path.Combine(_projectPath, "project.json");
        public string ProjectJsonLockPath => Path.Combine(_projectPath, "project.lock.json");

        public const string RemoveLineTag = FileGeneratorUtilities.RemoveLineTag;

        public void Generate(Builder builder, CSharpProject project, List<Project.Configuration> configurations, string projectPath, List<string> generatedFiles, List<string> skipFiles)
        {
            _builder = builder;
            _projectPath = projectPath;

            var configuration = configurations[0];
            var frameworkFlags = project.Targets.TargetPossibilities.Select(f => f.GetFragment<DotNetFramework>()).Aggregate((x, y) => x | y);
            GenerateProjectJson(configuration, frameworkFlags, generatedFiles, skipFiles);

            _builder = null;
        }

        private static bool IsGenerateNeeded(string projectJsonPath)
        {
            if (!s_projectJsonGenerated.Contains(projectJsonPath))
            {
                s_projectJsonGenerated.Add(projectJsonPath);
                return true;
            }

            return false;
        }

        private void GenerateProjectJson(Project.Configuration conf, DotNetFramework frameworks, List<string> generatedFiles, List<string> skipFiles)
        {
            var projectJsonPath = ProjectJsonPath;
            var projectJsonLockPath = ProjectJsonLockPath;

            // No NuGet references and no trace of a previous project.json
            if (conf.ReferencesByNuGetPackage.Count == 0)
            {
                if (!File.Exists(projectJsonPath))
                    return;
            }

            lock (s_projectJsonLock)
            {
                if (conf.ReferencesByNuGetPackage.Count == 0)
                {
                    var fi = new FileInfo(projectJsonPath);
                    if (!fi.IsReadOnly) // Do not delete project.json submitted in P4
                    {
                        Util.TryDeleteFile(projectJsonPath);
                        Util.TryDeleteFile(projectJsonLockPath);
                    }
                    return;
                }

                if (IsGenerateNeeded(projectJsonPath))
                {
                    FileGenerator fileGenerator = new FileGenerator();

                    fileGenerator.Write(Template.Begin);

                    // frameworks
                    fileGenerator.Write(Template.FrameworksBegin);
                    DotNetFramework[] dnfs = ((DotNetFramework[])Enum.GetValues(typeof(DotNetFramework))).Where(f => frameworks.HasFlag(f)).ToArray();
                    for (int i = 0; i < dnfs.Length; ++i)
                    {
                        if (i != 0)
                            fileGenerator.Write(",");
                        using (fileGenerator.Declare("framework", dnfs[i].ToFolderName()))
                            fileGenerator.Write(Template.FrameworksItem);
                    }
                    fileGenerator.Write(Template.FrameworksEnd);

                    fileGenerator.Write(",");

                    // runtimes
                    fileGenerator.Write(Template.RuntimesBegin);
                    var runtimes = new[] { "win-x64", "win-x86", "win-anycpu", "win" };
                    for (int i = 0; i < runtimes.Length; ++i)
                    {
                        if (i != 0)
                            fileGenerator.Write(",");
                        using (fileGenerator.Declare("runtime", runtimes[i]))
                            fileGenerator.Write(Template.RuntimesItem);
                    }
                    fileGenerator.Write(Template.RuntimesEnd);

                    fileGenerator.Write(",");

                    // dependencies
                    fileGenerator.Write(Template.DependenciesBegin);
                    for (int i = 0; i < conf.ReferencesByNuGetPackage.SortedValues.Count; ++i)
                    {
                        if (i != 0)
                            fileGenerator.Write(",");
                        var packageReference = conf.ReferencesByNuGetPackage.SortedValues[i];
                        bool hasOptions = false;

                        // Check for private assets
                        string privateAssets = null;
                        if (packageReference.PrivateAssets != PackageReferences.DefaultPrivateAssets)
                        {
                            privateAssets = string.Join(",", PackageReferences.PackageReference.GetFormatedAssetsDependency(packageReference.PrivateAssets));
                            hasOptions = true;
                        }

                        // Check for a custom type
                        string referenceType = null;
                        if (!string.IsNullOrEmpty(packageReference.ReferenceType))
                        {
                            referenceType = packageReference.ReferenceType;
                            hasOptions = true;
                        }

                        if (!hasOptions)
                        {
                            using (fileGenerator.Declare("dependency", packageReference))
                                fileGenerator.Write(Template.DependenciesItem);
                        }
                        else
                        {
                            using (fileGenerator.Declare("dependency", packageReference))
                            {
                                fileGenerator.Write($"{Template.BeginDependencyItem}");

                                if (!string.IsNullOrEmpty(privateAssets))
                                {
                                    using (fileGenerator.Declare("privateAssets", privateAssets))
                                        fileGenerator.Write($"{Template.DependencyPrivateAssets}");
                                }
                                if (!string.IsNullOrEmpty(referenceType))
                                {
                                    using (fileGenerator.Declare("referenceType", referenceType))
                                        fileGenerator.Write($"{Template.DependencyReferenceType}");
                                }

                                fileGenerator.Write($"{Template.EndDependencyItem}");
                            }
                        }
                    }
                    fileGenerator.Write(Template.DependenciesEnd);

                    fileGenerator.Write(Template.End);
                    fileGenerator.RemoveTaggedLines();

                    bool written = _builder.Context.WriteGeneratedFile(GetType(), new FileInfo(projectJsonPath), fileGenerator.ToMemoryStream());
                    if (written)
                        generatedFiles.Add(projectJsonPath);
                    else
                        skipFiles.Add(projectJsonPath);
                }
            }

            IsGenerated = true;
        }
    }
}
