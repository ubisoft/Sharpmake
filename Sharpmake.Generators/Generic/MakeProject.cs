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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sharpmake.Generators.Generic
{
    public partial class MakeProject : IProjectGenerator
    {
        private const string _makefileExtension = ".mk";
        private const string RemoveLineTag = FileGeneratorUtilities.RemoveLineTag;

        private Builder _builder;

        public void Generate(Builder builder, Project project, List<Project.Configuration> configurations, string projectFile, List<string> generatedFiles, List<string> skipFiles)
        {
            _builder = builder;

            FileInfo fileInfo = new FileInfo(projectFile);
            string projectPath = fileInfo.Directory.FullName;
            string projectFileName = fileInfo.Name;

            bool updated;
            string projectFileResult = GenerateProject(project, configurations, projectPath, projectFileName, out updated);
            if (updated)
                generatedFiles.Add(projectFileResult);
            else
                skipFiles.Add(projectFileResult);

            _builder = null;
        }

        public string GenerateProject(Project project, List<Project.Configuration> configurations, string projectPath, string projectFileName, out bool updated)
        {
            string projectDirectory = Util.GetCapitalizedPath(projectPath);
            Directory.CreateDirectory(projectDirectory);

            string fileFullPath = projectDirectory + Path.DirectorySeparatorChar + projectFileName + _makefileExtension;
            FileInfo fileInfo = new FileInfo(fileFullPath);

            var fileGenerator = new FileGenerator();
            using (fileGenerator.Declare("item", new ProjectSettings(project, configurations, fileGenerator.Resolver)))
            {
                fileGenerator.Write(Template.GlobalTemplate);
            }

            fileGenerator.RemoveTaggedLines();

            updated = _builder.Context.WriteGeneratedFile(project.GetType(), fileInfo, fileGenerator.ToMemoryStream());

            return fileInfo.FullName;
        }

        private static void ResolveProjectPaths(Project project, Strings stringsToResolve)
        {
            foreach (string value in stringsToResolve.Values)
            {
                string newValue = ResolveProjectPaths(project, value);
                stringsToResolve.UpdateValue(value, newValue);
            }
        }

        private static string ResolveProjectPaths(Project project, string stringToResolve)
        {
            string resolvedString = stringToResolve.Replace("[project.SharpmakeCsPath]", project.SharpmakeCsPath).Replace("[project.SharpmakeCsProjectPath]", project.SharpmakeCsProjectPath);
            return Util.SimplifyPath(resolvedString);
        }

        private class ProjectSettings
        {
            private string _includePaths;
            private string _moduleName;
            private string _armMode;
            private string _shortCommands;
            private string _cFlagsDebug;
            private string _cFlagsRelease;
            private string _cFlagsFinal;
            private string _cFlagsExportedDebug;
            private string _cFlagsExportedRelease;
            private string _cFlagsExportedFinal;
            private string _sourcePaths;
            private string _groupStaticLibraries;
            private string _buildType;
            private string _prebuiltStaticLibraries;
            private string _prebuiltStaticLibrariesDebug;
            private string _prebuiltStaticLibrariesRelease;
            private readonly string _prebuiltStaticLibrariesFinal;

            public ProjectSettings(Project project, List<Project.Configuration> configurations, Resolver resolver)
            {
                Project.Configuration configurationDebug = configurations.FirstOrDefault(conf => conf.Target.GetFragment<Optimization>() == Optimization.Debug);
                Project.Configuration configurationRelease = configurations.FirstOrDefault(conf => conf.Target.GetFragment<Optimization>() == Optimization.Release);
                Project.Configuration configurationRetail = configurations.FirstOrDefault(conf => conf.Target.GetFragment<Optimization>() == Optimization.Retail);

                if (configurationDebug == null || configurationRelease == null || configurationRetail == null)
                    throw new Error("Android makefiles require a debug, release and final configuration. ");

                configurationDebug.Defines.Add("_DEBUG");
                configurationRelease.Defines.Add("NDEBUG");
                configurationRetail.Defines.Add("NDEBUG");

                _includePaths = "";
                foreach (string includePath in configurationDebug.IncludePaths)
                {
                    using (resolver.NewScopedParameter("Path", includePath))
                    {
                        _includePaths += resolver.Resolve(Template.IncludePathTemplate) + "\r\n";
                    }
                }

                _sourcePaths = "";
                Strings sourceFiles = project.GetSourceFilesForConfigurations(configurations);
                ResolveProjectPaths(project, sourceFiles);
                foreach (string sourcePath in sourceFiles)
                {
                    string extension = sourcePath.Substring(sourcePath.LastIndexOf('.'));
                    if (project.SourceFilesCompileExtensions.Contains(extension))
                    {
                        if (!configurationDebug.ResolvedSourceFilesBuildExclude.Contains(sourcePath))
                        {
                            using (resolver.NewScopedParameter("Path", sourcePath))
                            {
                                _sourcePaths += resolver.Resolve(Template.SourceFileTemplate) + "\r\n";
                            }
                        }
                    }
                }

                _moduleName = project.Name;

                Options.SelectOption(configurationDebug,
                    Options.Option(Options.AndroidMakefile.ArmMode.Thumb, () => _armMode = RemoveLineTag),
                    Options.Option(Options.AndroidMakefile.ArmMode.Arm, () => _armMode = "arm")
                );

                Options.SelectOption(configurationDebug,
                    Options.Option(Options.AndroidMakefile.ShortCommands.Disable, () => _shortCommands = "false"),
                    Options.Option(Options.AndroidMakefile.ShortCommands.Enable, () => _shortCommands = "true")
                );

                Options.SelectOption(configurationDebug,
                    Options.Option(Options.AndroidMakefile.GroupStaticLibraries.Disable, () => _groupStaticLibraries = "false"),
                    Options.Option(Options.AndroidMakefile.GroupStaticLibraries.Enable, () => _groupStaticLibraries = "true")
                );

                Strings compilerFlagsDebug = Options.GetStrings<Options.AndroidMakefile.CompilerFlags>(configurationDebug);
                Strings compilerFlagsRelease = Options.GetStrings<Options.AndroidMakefile.CompilerFlags>(configurationRelease);
                Strings compilerFlagsFinal = Options.GetStrings<Options.AndroidMakefile.CompilerFlags>(configurationRetail);

                _cFlagsDebug = configurationDebug.Defines.Select(define => "-D" + define).Union(compilerFlagsDebug).DefaultIfEmpty("").Aggregate((first, next) => first + " " + next);
                _cFlagsRelease = configurationRelease.Defines.Select(define => "-D" + define).Union(compilerFlagsRelease).DefaultIfEmpty("").Aggregate((first, next) => first + " " + next);
                _cFlagsFinal = configurationRetail.Defines.Select(define => "-D" + define).Union(compilerFlagsFinal).DefaultIfEmpty("").Aggregate((first, next) => first + " " + next);

                //Strings compilerExportedFlagsDebug = Options.GetStrings<Options.AndroidMakefile.CompilerExportedFlags>(configurationDebug);
                //Strings compilerExportedFlagsRelease = Options.GetStrings<Options.AndroidMakefile.CompilerExportedFlags>(configurationRelease);
                //Strings compilerExportedFlagsFinal = Options.GetStrings<Options.AndroidMakefile.CompilerExportedFlags>(configurationRetail);
                Strings exportedDefinesDebug = Options.GetStrings<Options.AndroidMakefile.ExportedDefines>(configurationDebug);
                Strings exportedDefinesRelease = Options.GetStrings<Options.AndroidMakefile.ExportedDefines>(configurationRelease);
                Strings exportedDefinesFinal = Options.GetStrings<Options.AndroidMakefile.ExportedDefines>(configurationRetail);

                _cFlagsExportedDebug = exportedDefinesDebug.Select(define => "-D" + define).Union(compilerFlagsDebug).DefaultIfEmpty("").Aggregate((first, next) => first + " " + next);
                _cFlagsExportedRelease = exportedDefinesRelease.Select(define => "-D" + define).Union(compilerFlagsRelease).DefaultIfEmpty("").Aggregate((first, next) => first + " " + next);
                _cFlagsExportedFinal = exportedDefinesFinal.Select(define => "-D" + define).Union(compilerFlagsFinal).DefaultIfEmpty("").Aggregate((first, next) => first + " " + next);

                List<PrebuiltStaticLibrary> allPrebuiltStaticLibrariesDebug = new List<PrebuiltStaticLibrary>();
                List<PrebuiltStaticLibrary> allPrebuiltStaticLibrariesRelease = new List<PrebuiltStaticLibrary>();
                List<PrebuiltStaticLibrary> allPrebuiltStaticLibrariesFinal = new List<PrebuiltStaticLibrary>();
                foreach (var library in Options.GetObjects<Options.AndroidMakefile.PrebuiltStaticLibraries>(configurationDebug))
                {
                    PrebuiltStaticLibrary internalLibrary = new PrebuiltStaticLibrary(project, library);
                    allPrebuiltStaticLibrariesDebug.Add(internalLibrary);
                }
                foreach (var library in Options.GetObjects<Options.AndroidMakefile.PrebuiltStaticLibraries>(configurationRelease))
                {
                    PrebuiltStaticLibrary internalLibrary = new PrebuiltStaticLibrary(project, library);
                    allPrebuiltStaticLibrariesRelease.Add(internalLibrary);
                }
                foreach (var library in Options.GetObjects<Options.AndroidMakefile.PrebuiltStaticLibraries>(configurationRetail))
                {
                    PrebuiltStaticLibrary internalLibrary = new PrebuiltStaticLibrary(project, library);
                    allPrebuiltStaticLibrariesFinal.Add(internalLibrary);
                }
                IEnumerable<PrebuiltStaticLibrary> prebuiltStaticLibraries = allPrebuiltStaticLibrariesDebug.Intersect(allPrebuiltStaticLibrariesRelease).Intersect(allPrebuiltStaticLibrariesFinal);
                IEnumerable<PrebuiltStaticLibrary> prebuiltStaticLibrariesDebug = allPrebuiltStaticLibrariesDebug.Except(prebuiltStaticLibraries);
                IEnumerable<PrebuiltStaticLibrary> prebuiltStaticLibrariesRelease = allPrebuiltStaticLibrariesRelease.Except(prebuiltStaticLibraries);
                IEnumerable<PrebuiltStaticLibrary> prebuiltStaticLibrariesFinal = allPrebuiltStaticLibrariesFinal.Except(prebuiltStaticLibraries);

                _prebuiltStaticLibraries = prebuiltStaticLibraries.Any() ? prebuiltStaticLibraries.Select(item => { using (resolver.NewScopedParameter("item", item)) return resolver.Resolve(Template.PrebuiltStaticLibraryTemplate); }).Aggregate((first, next) => first + "\r\n" + next) : "";
                _prebuiltStaticLibrariesDebug = prebuiltStaticLibrariesDebug.Any() ? prebuiltStaticLibrariesDebug.Select(item => { using (resolver.NewScopedParameter("item", item)) return resolver.Resolve(Template.PrebuiltStaticLibraryTemplate); }).Aggregate((first, next) => first + "\r\n" + next) : "";
                _prebuiltStaticLibrariesRelease = prebuiltStaticLibrariesRelease.Any() ? prebuiltStaticLibrariesRelease.Select(item => { using (resolver.NewScopedParameter("item", item)) return resolver.Resolve(Template.PrebuiltStaticLibraryTemplate); }).Aggregate((first, next) => first + "\r\n" + next) : "";
                _prebuiltStaticLibrariesFinal = prebuiltStaticLibrariesFinal.Any() ? prebuiltStaticLibrariesFinal.Select(item => { using (resolver.NewScopedParameter("item", item)) return resolver.Resolve(Template.PrebuiltStaticLibraryTemplate); }).Aggregate((first, next) => first + "\r\n" + next) : "";

                switch (configurationDebug.Output)
                {
                    case Project.Configuration.OutputType.Lib:
                        _buildType = Template.BuildStaticLibraryTemplate;
                        break;

                    case Project.Configuration.OutputType.Dll:
                        _buildType = Template.BuildSharedLibraryTemplate;
                        break;

                    default:
                        _buildType = RemoveLineTag;
                        break;
                }
            }

            public string IncludePaths { get { return _includePaths; } }
            public string ModuleName { get { return _moduleName; } }
            public string ArmMode { get { return _armMode; } }
            public string ShortCommands { get { return _shortCommands; } }
            public string GroupStaticLibraries { get { return _groupStaticLibraries; } }
            public string CFlagsDebug { get { return _cFlagsDebug; } }
            public string CFlagsRelease { get { return _cFlagsRelease; } }
            public string CFlagsFinal { get { return _cFlagsFinal; } }
            public string CFlagsExportedDebug { get { return _cFlagsExportedDebug; } }
            public string CFlagsExportedRelease { get { return _cFlagsExportedRelease; } }
            public string CFlagsExportedFinal { get { return _cFlagsExportedFinal; } }
            public string SourcePaths { get { return _sourcePaths; } }
            public string BuildType { get { return _buildType; } }
            public string PrebuiltStaticLibraries { get { return _prebuiltStaticLibraries; } }
            public string PrebuiltStaticLibrariesDebug { get { return _prebuiltStaticLibrariesDebug; } }
            public string PrebuiltStaticLibrariesRelease { get { return _prebuiltStaticLibrariesRelease; } }
            public string PrebuiltStaticLibrariesFinal { get { return _prebuiltStaticLibrariesFinal; } }
        }

        private class PrebuiltStaticLibrary
        {
            private Options.AndroidMakefile.PrebuiltStaticLibraries _option;
            private string _armMode;
            private readonly string _libraryPath;

            public PrebuiltStaticLibrary(Project project, Options.AndroidMakefile.PrebuiltStaticLibraries option)
            {
                _option = option;
                if (_option.Mode == Options.AndroidMakefile.ArmMode.Arm)
                    _armMode = "arm";
                else
                    _armMode = RemoveLineTag;
                _libraryPath = ResolveProjectPaths(project, _option.LibraryPath);
            }

            public override bool Equals(object obj)
            {
                PrebuiltStaticLibrary other = obj as PrebuiltStaticLibrary;
                if (other == null)
                    return false;
                return ModuleName == other.ModuleName && LibraryPath == other.LibraryPath && ArmMode == other.ArmMode;
            }

            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;
                    hash = hash * 23 + ModuleName.GetHashCode();
                    hash = hash * 23 + LibraryPath.GetHashCode();
                    hash = hash * 23 + LibraryPath.GetHashCode();
                    return hash;
                }
            }

            public string ArmMode { get { return _armMode; } }
            public string ModuleName { get { return _option.ModuleName; } }
            public string LibraryPath { get { return _libraryPath; } }
        }
    }
}
