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
    public partial class Androidproj : IProjectGenerator
    {
        private AndroidPackageProject _Project;
        private List<Project.Configuration> _ProjectConfigurationList;
        private string _ProjectDirectoryCapitalized;
        //private string _ProjectSourceCapitalized;
        private Project.Configuration _ProjectConfiguration;
        private Builder _Builder;
        public const string ProjectExtension = ".androidproj";

        public void Generate(
            Builder builder,
            Project project,
            List<Project.Configuration> configurations,
            string projectFile,
            List<string> generatedFiles,
            List<string> skipFiles)
        {
            _Builder = builder;

            FileInfo fileInfo = new FileInfo(projectFile);
            string projectPath = fileInfo.Directory.FullName;
            string projectFileName = fileInfo.Name;

            if (!(project is AndroidPackageProject))
                throw new ArgumentException("Project is not a AndroidPackageProject");

            Generate((AndroidPackageProject)project, configurations, projectPath, projectFileName, generatedFiles, skipFiles);

            _Builder = null;
        }

        private void Write(string value, TextWriter writer, Resolver resolver)
        {
            string resolvedValue = resolver.Resolve(value);
            StringReader reader = new StringReader(resolvedValue);
            string str = reader.ReadToEnd();
            writer.Write(str);
            writer.Flush();
        }

        private void Generate(
            AndroidPackageProject project,
            List<Project.Configuration> unsortedConfigurations,
            string projectPath,
            string projectFile,
            List<string> generatedFiles,
            List<string> skipFiles)
        {
            // Need to sort by name and platform
            List<Project.Configuration> configurations = new List<Project.Configuration>();
            configurations.AddRange(unsortedConfigurations.OrderBy(conf => conf.Name + conf.Platform));

            // validate that 2 conf name in the same project don't have the same name
            Dictionary<string, Project.Configuration> configurationNameMapping = new Dictionary<string, Project.Configuration>();
            string projectName = null;

            foreach (Project.Configuration conf in configurations)
            {
                if (projectName == null)
                    projectName = conf.ProjectName;
                else if (projectName != conf.ProjectName)
                    throw new Error("Project configurations in the same project files must be the same: {0} != {1} in {2}", projectName, conf.ProjectName, projectFile);

                Project.Configuration otherConf;

                string projectUniqueName = conf.Name + Util.GetPlatformString(conf.Platform, conf.Project);
                if (configurationNameMapping.TryGetValue(projectUniqueName, out otherConf))
                {
                    var differBy = Util.MakeDifferenceString(conf, otherConf);
                    throw new Error(
                        "Project {0} ({5} in {6}) has 2 configurations with the same name: \"{1}\" for {2} and {3}"
                        + Environment.NewLine + "Nb: ps3 and win32 cannot have same conf name: {4}",
                        project.Name, conf.Name, otherConf.Target, conf.Target, differBy, projectFile, projectPath);
                }

                configurationNameMapping[projectUniqueName] = conf;

                // set generator information
                switch (conf.Platform)
                {
                    case Platform.android:
                        conf.GeneratorSetGeneratedInformation("elf", "elf", "so", "pdb");
                        break;
                    default:
                        break;
                }
            }

            Resolver resolver = new Resolver();

            _ProjectDirectoryCapitalized = Util.GetCapitalizedPath(projectPath);
            //_ProjectSourceCapitalized = Util.GetCapitalizedPath(project.SourceRootPath);
            _Project = project;
            _ProjectConfigurationList = configurations;

            MemoryStream memoryStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(memoryStream);

            // xml begin header
            DevEnvRange devEnvRange = new DevEnvRange(unsortedConfigurations);
            using (resolver.NewScopedParameter("toolsVersion", devEnvRange.MinDevEnv.GetVisualProjectToolsVersionString()))
            {
                Write(Template.Project.ProjectBegin, writer, resolver);
            }

            Write(Template.Project.ProjectBeginConfigurationDescription, writer, resolver);
            // xml header contain description of each target
            foreach (Project.Configuration conf in _ProjectConfigurationList)
            {
                using (resolver.NewScopedParameter("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                using (resolver.NewScopedParameter("conf", conf))
                {
                    Write(Template.Project.ProjectConfigurationDescription, writer, resolver);
                }
            }
            Write(Template.Project.ProjectEndConfigurationDescription, writer, resolver);

            // xml end header
            var firstConf = _ProjectConfigurationList.First();
            using (resolver.NewScopedParameter("projectName", projectName))
            using (resolver.NewScopedParameter("guid", firstConf.ProjectGuid))
            using (resolver.NewScopedParameter("toolsVersion", devEnvRange.MinDevEnv.GetVisualProjectToolsVersionString()))
            {
                Write(Template.Project.ProjectDescription, writer, resolver);
            }

            // generate all configuration options once...
            Dictionary<Project.Configuration, Options.ExplicitOptions> options = new Dictionary<Project.Configuration, Options.ExplicitOptions>();
            foreach (Project.Configuration conf in _ProjectConfigurationList)
            {
                _ProjectConfiguration = conf;
                Options.ExplicitOptions option = GenerateOptions(project, projectPath, conf);
                _ProjectConfiguration = null;
                options.Add(conf, option);
            }

            // configuration general
            foreach (Project.Configuration conf in _ProjectConfigurationList)
            {
                using (resolver.NewScopedParameter("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                using (resolver.NewScopedParameter("conf", conf))
                using (resolver.NewScopedParameter("options", options[conf]))
                {
                    Write(Template.Project.ProjectConfigurationsGeneral, writer, resolver);
                }
            }

            // .props files
            Write(Template.Project.ProjectAfterConfigurationsGeneral, writer, resolver);
            Write(Template.Project.ProjectAfterImportedProps, writer, resolver);

            string androidPackageDirectory = project.AntBuildRootDirectory;

            // configuration ItemDefinitionGroup
            foreach (Project.Configuration conf in _ProjectConfigurationList)
            {
                using (resolver.NewScopedParameter("platformName", Util.GetPlatformString(conf.Platform, conf.Project)))
                using (resolver.NewScopedParameter("conf", conf))
                using (resolver.NewScopedParameter("options", options[conf]))
                using (resolver.NewScopedParameter("androidPackageDirectory", androidPackageDirectory))
                {
                    Write(Template.Project.ProjectConfigurationBeginItemDefinition, writer, resolver);
                    {
                        Write(Template.Project.AntPackage, writer, resolver);
                    }
                    Write(Template.Project.ProjectConfigurationEndItemDefinition, writer, resolver);
                }
            }

            GenerateFilesSection(project, writer, resolver, projectPath, projectFile, generatedFiles, skipFiles);

            GenerateProjectReferences(configurations, resolver, writer, options);

            // .targets
            Write(Template.Project.ProjectTargets, writer, resolver);

            Write(Template.Project.ProjectEnd, writer, resolver);

            // Write the project file
            writer.Flush();

            // remove all line that contain RemoveLineTag
            MemoryStream cleanMemoryStream = Util.RemoveLineTags(memoryStream, FileGeneratorUtilities.RemoveLineTag);

            FileInfo projectFileInfo = new FileInfo(Path.Combine(projectPath, projectFile + ProjectExtension));
            if (_Builder.Context.WriteGeneratedFile(project.GetType(), projectFileInfo, cleanMemoryStream))
                generatedFiles.Add(projectFileInfo.FullName);
            else
                skipFiles.Add(projectFileInfo.FullName);

            writer.Close();

            _Project = null;
        }

        private void GenerateFilesSection(
            AndroidPackageProject project,
            StreamWriter writer,
            Resolver resolver,
            string projectPath,
            string projectFileName,
            List<string> generatedFiles,
            List<string> skipFiles)
        {
            Strings projectFiles = _Project.GetSourceFilesForConfigurations(_ProjectConfigurationList);

            // Add source files
            List<ProjectFile> allFiles = new List<ProjectFile>();
            List<ProjectFile> includeFiles = new List<ProjectFile>();
            List<ProjectFile> sourceFiles = new List<ProjectFile>();

            foreach (string file in projectFiles)
            {
                ProjectFile projectFile = new ProjectFile(file, _ProjectDirectoryCapitalized);
                allFiles.Add(projectFile);
            }

            allFiles.Sort((ProjectFile l, ProjectFile r) => { return l.FileNameProjectRelative.CompareTo(r.FileNameProjectRelative); });

            // type -> files
            var customSourceFiles = new Dictionary<string, List<ProjectFile>>();
            foreach (ProjectFile projectFile in allFiles)
            {
                string type = null;
                if (_Project.ExtensionBuildTools.TryGetValue(projectFile.FileExtension, out type))
                {
                    List<ProjectFile> files = null;
                    if (!customSourceFiles.TryGetValue(type, out files))
                    {
                        files = new List<ProjectFile>();
                        customSourceFiles[type] = files;
                    }
                    files.Add(projectFile);
                }
                else if (_Project.SourceFilesCompileExtensions.Contains(projectFile.FileExtension) ||
                         (String.Compare(projectFile.FileExtension, ".rc", StringComparison.OrdinalIgnoreCase) == 0))
                {
                    sourceFiles.Add(projectFile);
                }
                else // if (projectFile.FileExtension == "h")
                {
                    includeFiles.Add(projectFile);
                }
            }

            // Write header files
            Write(Template.Project.ProjectFilesBegin, writer, resolver);
            foreach (ProjectFile file in includeFiles)
            {
                using (resolver.NewScopedParameter("file", file))
                    Write(Template.Project.ProjectFilesHeader, writer, resolver);
            }
            Write(Template.Project.ProjectFilesEnd, writer, resolver);

            Write(Template.Project.ItemGroupBegin, writer, resolver);

            using (resolver.NewScopedParameter("antBuildXml", project.AntBuildXml))
            using (resolver.NewScopedParameter("antProjectPropertiesFile", project.AntProjectPropertiesFile))
            using (resolver.NewScopedParameter("androidManifest", project.AndroidManifest))
            {
                Write(Template.Project.AntBuildXml, writer, resolver);
                Write(Template.Project.AndroidManifest, writer, resolver);
                Write(Template.Project.AntProjectPropertiesFile, writer, resolver);
            }
            Write(Template.Project.ItemGroupEnd, writer, resolver);
        }

        private struct ProjectDependencyInfo
        {
            public string ProjectFullFileNameWithExtension;
            public string ProjectGuid;
        }

        private void GenerateProjectReferences(
            IEnumerable<Project.Configuration> configurations,
            Resolver resolver,
            StreamWriter writer,
            Dictionary<Project.Configuration, Options.ExplicitOptions> optionsDictionary)
        {
            UniqueList<ProjectDependencyInfo> dependencies = new UniqueList<ProjectDependencyInfo>();
            foreach (var c in configurations)
            {
                foreach (var d in c.ConfigurationDependencies)
                {
                    ProjectDependencyInfo depInfo;
                    depInfo.ProjectFullFileNameWithExtension = d.ProjectFullFileNameWithExtension;
                    depInfo.ProjectGuid = d.ProjectGuid;
                    dependencies.Add(depInfo);
                }
            }

            if (dependencies.Count > 0)
            {
                Write(Template.Project.ItemGroupBegin, writer, resolver);
                var conf = configurations.ToList().First();
                foreach (var d in dependencies)
                {
                    string include = Util.PathGetRelative(conf.ProjectPath, d.ProjectFullFileNameWithExtension);
                    using (resolver.NewScopedParameter("include", include))
                    using (resolver.NewScopedParameter("projectGUID", d.ProjectGuid))
                    {
                        Write(Template.Project.ProjectReference, writer, resolver);
                    }
                }
                Write(Template.Project.ItemGroupEnd, writer, resolver);
            }
        }

        private void SelectOption(params Options.OptionAction[] options)
        {
            Options.SelectOption(_ProjectConfiguration, options);
        }

        private Options.ExplicitOptions GenerateOptions(AndroidPackageProject project, string projectPath, Project.Configuration conf)
        {
            Options.ExplicitOptions options = new Options.ExplicitOptions();

            options["OutputFile"] = FileGeneratorUtilities.RemoveLineTag;
            if (_Project.AppLibType != null)
            {
                Project.Configuration appLibConf = conf.ConfigurationDependencies.FirstOrDefault(confDep => (confDep.Project.GetType() == _Project.AppLibType));
                if (appLibConf != null)
                {
                    // The lib name to first load from an AndroidActivity must be a dynamic library.
                    if (appLibConf.Output != Project.Configuration.OutputType.Dll)
                        throw new Error("Cannot use configuration \"{0}\" as app lib for package configuration \"{1}\". Output type must be set to dynamic library.", appLibConf, conf);

                    options["OutputFile"] = appLibConf.TargetFileFullName;
                }
                else
                {
                    throw new Error("Missing dependency of type \"{0}\" in configuration \"{1}\" dependencies.", _Project.AppLibType.ToNiceTypeName(), conf);
                }
            }

            //Options.Vc.General.UseDebugLibraries.
            //    Disable                                 WarnAsError="false"
            //    Enable                                  WarnAsError="true"                              /WX
            SelectOption
            (
            Options.Option(Options.Vc.General.UseDebugLibraries.Disabled, () => { options["UseDebugLibraries"] = "false"; }),
            Options.Option(Options.Vc.General.UseDebugLibraries.Enabled, () => { options["UseDebugLibraries"] = "true"; })
            );

            SelectOption
            (
            Options.Option(Options.Android.General.AndroidAPILevel.Default, () => { options["AndroidAPILevel"] = FileGeneratorUtilities.RemoveLineTag; }),
            Options.Option(Options.Android.General.AndroidAPILevel.Android19, () => { options["AndroidAPILevel"] = "android-19"; }),
            Options.Option(Options.Android.General.AndroidAPILevel.Android21, () => { options["AndroidAPILevel"] = "android-21"; }),
            Options.Option(Options.Android.General.AndroidAPILevel.Android22, () => { options["AndroidAPILevel"] = "android-22"; }),
            Options.Option(Options.Android.General.AndroidAPILevel.Android23, () => { options["AndroidAPILevel"] = "android-23"; }),
            Options.Option(Options.Android.General.AndroidAPILevel.Android24, () => { options["AndroidAPILevel"] = "android-24"; })
            );

            //OutputDirectory
            //    The debugger need a rooted path to work properly.
            //    So we root the relative output directory to $(ProjectDir) to work around this limitation.
            //    Hopefully in a futur version of the cross platform tools will be able to remove this hack.
            string outputDirectoryRelative = Util.PathGetRelative(projectPath, conf.TargetPath);
            options["OutputDirectory"] = outputDirectoryRelative;

            //IntermediateDirectory
            string intermediateDirectoryRelative = Util.PathGetRelative(projectPath, conf.IntermediatePath);
            options["IntermediateDirectory"] = intermediateDirectoryRelative;

            return options;
        }

        public class ProjectFile
        {
            public string FileName;
            public string FileNameProjectRelative;
            public string FileExtension;

            public ProjectFile(string fileName, string projectDirectoryCapitalized)
            {
                FileName = Project.GetCapitalizedFile(fileName);
                if (FileName == null)
                    FileName = fileName;

                FileNameProjectRelative = Util.PathGetRelative(projectDirectoryCapitalized, FileName, true);

                FileExtension = Path.GetExtension(FileName);
            }

            public override string ToString()
            {
                return FileName;
            }
        }
    }
}
