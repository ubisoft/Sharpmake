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
using Microsoft.Win32;

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Pyproj : IProjectGenerator
    {
        internal class ItemGroups
        {
            internal CSproj.ItemGroups.ItemGroup<ProjectReference> ProjectReferences = new CSproj.ItemGroups.ItemGroup<ProjectReference>();

            internal string Resolve(Resolver resolver)
            {
                var writer = new StringWriter();
                writer.Write(ProjectReferences.Resolve(resolver));
                return writer.ToString();
            }

            internal class ProjectReference : CSproj.ItemGroups.ItemGroupItem, CSproj.IResolvable
            {
                public Guid Project;
                public string Name;
                public string Private;
                public bool? ReferenceOutputAssembly = null;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("projectRefName", Name))
                    using (resolver.NewScopedParameter("projectGUID", Project.ToString("B").ToUpper()))
                    using (resolver.NewScopedParameter("private", Private))
                    using (resolver.NewScopedParameter("ReferenceOutputAssembly", ReferenceOutputAssembly))
                    {
                        var writer = new StringWriter();

                        writer.Write(Template.Project.ProjectReferenceBegin);
                        writer.Write(Template.Project.ProjectRefName);
                        writer.Write(Template.Project.ProjectGUID);
                        writer.Write(Template.Project.Private);
                        if (ReferenceOutputAssembly.HasValue)
                            writer.Write(Template.Project.ReferenceOutputAssembly);
                        writer.Write(Template.Project.ProjectReferenceEnd);
                        return resolver.Resolve(writer.ToString());
                    }
                }
            }
        }

        private PythonProject _project;
        private List<Project.Configuration> _projectConfigurationList;
        private Builder _builder;
        public const string ProjectExtension = ".pyproj";

        private void Write(string value, TextWriter writer, Resolver resolver)
        {
            writer.Write(resolver.Resolve(value));
        }

        public void Generate(Builder builder, Project project, List<Project.Configuration> configurations, string projectFile, List<string> generatedFiles, List<string> skipFiles)
        {
            _builder = builder;

            FileInfo fileInfo = new FileInfo(projectFile);
            string projectPath = fileInfo.Directory.FullName;
            string projectFileName = fileInfo.Name;
            bool updated;

            if (!(project is PythonProject))
                throw new ArgumentException("Project is not a PythonProject");

            string projectFileResult = Generate((PythonProject)project, configurations, projectPath, projectFileName, out updated);
            if (updated)
                generatedFiles.Add(projectFileResult);
            else
                skipFiles.Add(projectFileResult);

            _builder = null;
        }

        private string Generate(PythonProject project, List<Project.Configuration> unsortedConfigurations, string projectPath, string projectFile, out bool updated)
        {
            var itemGroups = new ItemGroups();

            // Need to sort by name and platform
            List<Project.Configuration> configurations = new List<Project.Configuration>();
            configurations.AddRange(unsortedConfigurations.OrderBy(conf => conf.Name + conf.Platform));
            string sourceRootPath = project.IsSourceFilesCaseSensitive ? Util.GetCapitalizedPath(project.SourceRootPath) : project.SourceRootPath;

            Resolver resolver = new Resolver();

            using (resolver.NewScopedParameter("guid", configurations.First().ProjectGuid))
            using (resolver.NewScopedParameter("projectHome", Util.PathGetRelative(projectPath, sourceRootPath)))
            using (resolver.NewScopedParameter("startupFile", project.StartupFile))
            using (resolver.NewScopedParameter("searchPath", project.SearchPaths.JoinStrings(";")))
            {
                _project = project;
                _projectConfigurationList = configurations;

                DevEnvRange devEnvRange = new DevEnvRange(unsortedConfigurations);
                bool needsPypatching = devEnvRange.MinDevEnv >= DevEnv.vs2017;

                if (!needsPypatching && (devEnvRange.MinDevEnv != devEnvRange.MaxDevEnv))
                {
                    Builder.Instance.LogWarningLine("There are mixed devEnvs for one project. VS2017 or higher Visual Studio solutions will require manual updates.");
                }

                MemoryStream memoryStream = new MemoryStream();
                StreamWriter writer = new StreamWriter(memoryStream);

                // xml begin header
                Write(Template.Project.ProjectBegin, writer, resolver);

                string defaultInterpreterRegisterKeyName = $@"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\{
                        devEnvRange.MinDevEnv.GetVisualVersionString()
                    }\PythonTools\Options\Interpreters";
                var defaultInterpreter = (string)Registry.GetValue(defaultInterpreterRegisterKeyName, "DefaultInterpreter", "{}") ?? "{00000000-0000-0000-0000-000000000000}";
                var defaultInterpreterVersion = (string)Registry.GetValue(defaultInterpreterRegisterKeyName, "DefaultInterpreterVersion", "2.7") ?? "2.7";

                string currentInterpreterId = defaultInterpreter;
                string currentInterpreterVersion = defaultInterpreterVersion;
                string ptvsTargetsFile = $@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\Python Tools\Microsoft.PythonTools.targets";

                // environments
                foreach (PythonEnvironment pyEnvironment in _project.Environments)
                {
                    if (pyEnvironment.IsActivated)
                    {
                        string interpreterRegisterKeyName =
                            $@"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\{
                                devEnvRange.MinDevEnv.GetVisualVersionString()
                            }\PythonTools\Interpreters\{{{pyEnvironment.Guid}}}";
                        string interpreterDescription = (string)Registry.GetValue(interpreterRegisterKeyName, "Description", "");
                        if (interpreterDescription != string.Empty)
                        {
                            currentInterpreterId = $"{{{pyEnvironment.Guid}}}";
                            currentInterpreterVersion = (string)Registry.GetValue(interpreterRegisterKeyName, "Version", currentInterpreterVersion);
                        }
                    }
                }

                // virtual environments
                foreach (PythonVirtualEnvironment virtualEnvironment in _project.VirtualEnvironments)
                {
                    if (virtualEnvironment.IsDefault)
                    {
                        string baseInterpreterRegisterKeyName =
                            $@"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\{
                                devEnvRange.MinDevEnv.GetVisualVersionString()
                            }\PythonTools\Interpreters\{{{virtualEnvironment.BaseInterpreterGuid}}}";
                        string baseInterpreterDescription = (string)Registry.GetValue(baseInterpreterRegisterKeyName, "Description", "");
                        if (baseInterpreterDescription != string.Empty)
                        {
                            currentInterpreterId = $"{{{virtualEnvironment.Guid}}}";
                            currentInterpreterVersion = (string)Registry.GetValue(baseInterpreterRegisterKeyName, "Version", currentInterpreterVersion);
                        }
                    }
                }

                // Project description
                if (needsPypatching)
                {
                    currentInterpreterId = $"MSBuild|debug|$(MSBuildProjectFullPath)";
                    ptvsTargetsFile = FileGeneratorUtilities.RemoveLineTag;
                }

                using (resolver.NewScopedParameter("interpreterId", currentInterpreterId))
                using (resolver.NewScopedParameter("interpreterVersion", currentInterpreterVersion))
                using (resolver.NewScopedParameter("ptvsTargetsFile", ptvsTargetsFile))
                {
                    Write(Template.Project.ProjectDescription, writer, resolver);
                }

                GenerateItems(writer, resolver);

                string baseGuid = FileGeneratorUtilities.RemoveLineTag;

                foreach (PythonVirtualEnvironment virtualEnvironment in _project.VirtualEnvironments)
                {
                    baseGuid = needsPypatching ? baseGuid : virtualEnvironment.BaseInterpreterGuid.ToString();

                    Write(Template.Project.ProjectItemGroupBegin, writer, resolver);
                    using (resolver.NewScopedParameter("name", virtualEnvironment.Name))
                    using (resolver.NewScopedParameter("version", currentInterpreterVersion))
                    using (resolver.NewScopedParameter("basePath", virtualEnvironment.Path))
                    using (resolver.NewScopedParameter("baseGuid", baseGuid))
                    using (resolver.NewScopedParameter("guid", virtualEnvironment.Guid))
                    {
                        Write(Template.Project.VirtualEnvironmentInterpreter, writer, resolver);
                    }
                    Write(Template.Project.ProjectItemGroupEnd, writer, resolver);
                }

                Write(Template.Project.ProjectItemGroupBegin, writer, resolver);

                if (_project.Environments.Count > 0)
                {
                    foreach (PythonEnvironment pyEnvironment in _project.Environments)
                    {
                        // Verify if the interpreter exists in the register.
                        string interpreterRegisterKeyName =
                            $@"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\{
                                devEnvRange.MinDevEnv.GetVisualVersionString()
                            }\PythonTools\Interpreters\{{{pyEnvironment.Guid}}}";
                        string interpreterDescription = (string)Registry.GetValue(interpreterRegisterKeyName, "Description", "");
                        if (interpreterDescription != string.Empty)
                        {
                            string interpreterVersion = (string)Registry.GetValue(interpreterRegisterKeyName, "Version", currentInterpreterVersion);
                            using (resolver.NewScopedParameter("guid", $"{{{pyEnvironment.Guid}}}"))
                            using (resolver.NewScopedParameter("version", interpreterVersion))
                            {
                                Write(Template.Project.InterpreterReference, writer, resolver);
                            }
                        }
                    }
                }
                else if (_project.VirtualEnvironments.Count == 0) // Set the default interpreter
                {
                    using (resolver.NewScopedParameter("guid", currentInterpreterId))
                    using (resolver.NewScopedParameter("version", currentInterpreterVersion))
                    {
                        Write(Template.Project.InterpreterReference, writer, resolver);
                    }
                }
                Write(Template.Project.ProjectItemGroupEnd, writer, resolver);

                // configuration general
                foreach (Project.Configuration conf in _projectConfigurationList)
                {
                    foreach (var dependencies in new[] { conf.ResolvedPublicDependencies, conf.DotNetPrivateDependencies.Select(x => x.Configuration) })
                    {
                        foreach (var dependency in dependencies)
                        {
                            string relativeToProjectFile = Util.PathGetRelative(sourceRootPath, dependency.ProjectFullFileNameWithExtension);
                            bool privateDependency = project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.ProjectReferences);
                            conf.GetDependencySetting(dependency.Project.GetType());

                            itemGroups.ProjectReferences.Add(new ItemGroups.ProjectReference
                            {
                                Include = relativeToProjectFile,
                                Name = dependency.ProjectName,
                                Private = privateDependency ? "True" : "False",
                                Project = new Guid(dependency.ProjectGuid)
                            });
                        }
                    }
                }

                GenerateFolders(writer, resolver);

                // Import native Python Tools project
                if (needsPypatching)
                {
                    Write(Template.Project.ImportPythonTools, writer, resolver);
                }

                writer.Write(itemGroups.Resolve(resolver));

                Write(Template.Project.ProjectEnd, writer, resolver);

                // Write the project file
                writer.Flush();

                // remove all line that contain RemoveLineTag
                memoryStream = Util.RemoveLineTags(memoryStream, FileGeneratorUtilities.RemoveLineTag);
                memoryStream.Seek(0, SeekOrigin.Begin);

                FileInfo projectFileInfo = new FileInfo(projectPath + @"\" + projectFile + ProjectExtension);
                updated = _builder.Context.WriteGeneratedFile(project.GetType(), projectFileInfo, memoryStream);

                writer.Close();

                _project = null;

                return projectFileInfo.FullName;
            }
        }

        private void GenerateItems(StreamWriter writer, Resolver resolver)
        {
            Strings projectFiles = _project.GetSourceFilesForConfigurations(_projectConfigurationList);

            // Add source files
            ProjectDirectory rootDirectory = new ProjectDirectory(null, _project.SourceRootPath);
            foreach (string file in projectFiles)
            {
                string relativeFile = GetProperRelativePathToSourcePath(file);
                relativeFile = relativeFile.Trim('.', '\\', '/');

                string[] splitFile = relativeFile.Split('\\', '/');

                ProjectDirectory directory = rootDirectory;
                for (int i = 0; i < splitFile.Length - 1; ++i)
                {
                    directory = directory.GetSubDirectory(splitFile[i]);
                }

                directory.AddFile(file);
            }

            Write(Template.Project.ProjectItemGroupBegin, writer, resolver);
            foreach (ProjectDirectory subDirectory in rootDirectory.Directories)
                WriteProjectDirectory(writer, subDirectory, "    ", true);
            foreach (string file in rootDirectory.Files)
                WriteProjectFile(writer, file, "    ");

            Write(Template.Project.ProjectItemGroupEnd, writer, resolver);
        }

        private void GenerateFolders(StreamWriter writer, Resolver resolver)
        {
            Strings projectFiles = _project.GetSourceFilesForConfigurations(_projectConfigurationList);

            // Add source files
            ProjectDirectory rootDirectory = new ProjectDirectory(null, _project.SourceRootPath);
            foreach (string file in projectFiles)
            {
                string relativeFile = GetProperRelativePathToSourcePath(file);
                relativeFile = relativeFile.Trim('.', '\\', '/');

                string[] splitFile = relativeFile.Split('\\', '/');

                ProjectDirectory directory = rootDirectory;
                for (int i = 0; i < splitFile.Length - 1; ++i)
                {
                    directory = directory.GetSubDirectory(splitFile[i]);
                }

                directory.AddFile(file);
            }

            Write(Template.Project.ProjectItemGroupBegin, writer, resolver);
            foreach (ProjectDirectory subDirectory in rootDirectory.Directories)
                WriteProjectDirectory(writer, subDirectory, "    ", false);

            Write(Template.Project.ProjectItemGroupEnd, writer, resolver);
        }

        private void WriteProjectDirectory(StreamWriter writer, ProjectDirectory directory, string prefix, bool writeFiles)
        {
            if (writeFiles)
            {
                foreach (string filename in directory.Files)
                {
                    WriteProjectFile(writer, filename, prefix);
                }
            }
            else
            {
                writer.WriteLine(prefix + "<Folder Include=\"" + GetProperRelativePathToSourcePath(directory.Path) + "\" />");
            }

            foreach (ProjectDirectory subDirectory in directory.Directories)
            {
                WriteProjectDirectory(writer, subDirectory, prefix, writeFiles);
            }
        }

        private void WriteProjectFile(StreamWriter writer, string filename, string prefix)
        {
            string fileTag = (Path.GetExtension(filename) == ".py") ? "Compile" : "Content";
            writer.WriteLine(prefix + "<" + fileTag + " Include=\"" + GetProperRelativePathToSourcePath(filename) + "\" />");
        }

        private string GetProperRelativePathToSourcePath(string path)
        {
            return Util.PathGetRelative(_project.SourceRootPath, _project.IsSourceFilesCaseSensitive ? Util.GetCapitalizedPath(path) : path);
        }

        private class ProjectDirectory
        {
            public string Name;
            public string Path;
            public List<string> Files = new List<string>();
            public List<ProjectDirectory> Directories = new List<ProjectDirectory>();

            public ProjectDirectory(string name, string path)
            {
                Name = name;
                Path = path;
            }

            public ProjectDirectory GetSubDirectory(string name)
            {
                foreach (ProjectDirectory dir in Directories)
                {
                    if (dir.Name == name)
                        return dir;
                }
                ProjectDirectory newDir = new ProjectDirectory(name, Path + "\\" + name);
                Directories.Add(newDir);
                return newDir;
            }

            public void AddFile(string filename)
            {
                Files.Add(filename);
            }
        }
    }
}
