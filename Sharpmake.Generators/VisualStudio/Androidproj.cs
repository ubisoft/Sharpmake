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
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Androidproj : IProjectGenerator
    {
        public const string ProjectExtension = ".androidproj";

        private class GenerationContext : IVcxprojGenerationContext
        {
            #region IVcxprojGenerationContext implementation
            public Builder Builder { get; }
            public Project Project { get; }
            public Project.Configuration Configuration { get; internal set; }
            public string ProjectDirectory { get; }
            public DevEnv DevelopmentEnvironment => Configuration.Target.GetFragment<DevEnv>();
            public Options.ExplicitOptions Options
            {
                get
                {
                    Debug.Assert(_projectConfigurationOptions.ContainsKey(Configuration));
                    return _projectConfigurationOptions[Configuration];
                }
            }
            public IDictionary<string, string> CommandLineOptions { get; set; }
            public string ProjectDirectoryCapitalized { get; }
            public string ProjectSourceCapitalized { get; }
            public bool PlainOutput { get; }
            public void SelectOption(params Options.OptionAction[] options)
            {
                Sharpmake.Options.SelectOption(Configuration, options);
            }

            public void SelectOptionWithFallback(Action fallbackAction, params Options.OptionAction[] options)
            {
                Sharpmake.Options.SelectOptionWithFallback(Configuration, fallbackAction, options);
            }

            public string ProjectPath { get; }
            public string ProjectFileName { get; }
            public IReadOnlyList<Project.Configuration> ProjectConfigurations { get; }
            public IReadOnlyDictionary<Project.Configuration, Options.ExplicitOptions> ProjectConfigurationOptions => _projectConfigurationOptions;
            public DevEnvRange DevelopmentEnvironmentsRange { get; }
            public IReadOnlyDictionary<Platform, IPlatformVcxproj> PresentPlatforms { get; }
            public Resolver EnvironmentVariableResolver { get; internal set; }
            #endregion

            private Dictionary<Project.Configuration, Options.ExplicitOptions> _projectConfigurationOptions;

            public void SetProjectConfigurationOptions(Dictionary<Project.Configuration, Options.ExplicitOptions> projectConfigurationOptions)
            {
                _projectConfigurationOptions = projectConfigurationOptions;
            }

            internal AndroidPackageProject AndroidPackageProject { get; }

            public GenerationContext(Builder builder, string projectPath, Project project, IEnumerable<Project.Configuration> projectConfigurations)
            {
                Builder = builder;

                FileInfo fileInfo = new FileInfo(projectPath);
                ProjectPath = fileInfo.FullName;
                ProjectDirectory = Path.GetDirectoryName(ProjectPath);
                ProjectFileName = Path.GetFileName(ProjectPath);
                Project = project;
                AndroidPackageProject = (AndroidPackageProject)Project;

                ProjectDirectoryCapitalized = Util.GetCapitalizedPath(ProjectDirectory);
                ProjectSourceCapitalized = Util.GetCapitalizedPath(Project.SourceRootPath);

                ProjectConfigurations = VsUtil.SortConfigurations(projectConfigurations, Path.Combine(ProjectDirectoryCapitalized, ProjectFileName + ProjectExtension)).ToArray();
                DevelopmentEnvironmentsRange = new DevEnvRange(ProjectConfigurations);

                PresentPlatforms = ProjectConfigurations.Select(conf => conf.Platform).Distinct().ToDictionary(p => p, p => PlatformRegistry.Get<IPlatformVcxproj>(p));
            }

            public void Reset()
            {
                CommandLineOptions = null;
                Configuration = null;
                EnvironmentVariableResolver = null;
            }
        }

        public void Generate(
            Builder builder,
            Project project,
            List<Project.Configuration> configurations,
            string projectFile,
            List<string> generatedFiles,
            List<string> skipFiles)
        {
            if (!(project is AndroidPackageProject))
                throw new ArgumentException("Project is not a AndroidPackageProject");

            var context = new GenerationContext(builder, projectFile, project, configurations);
            GenerateImpl(context, generatedFiles, skipFiles);
        }

        private void GenerateConfOptions(GenerationContext context)
        {
            // generate all configuration options once...
            var projectOptionsGen = new ProjectOptionsGenerator();
            var projectConfigurationOptions = new Dictionary<Project.Configuration, Options.ExplicitOptions>();
            context.SetProjectConfigurationOptions(projectConfigurationOptions);
            foreach (Project.Configuration conf in context.ProjectConfigurations)
            {
                context.Configuration = conf;

                // set generator information
                var platformVcxproj = context.PresentPlatforms[conf.Platform];
                var configurationTasks = PlatformRegistry.Get<Project.Configuration.IConfigurationTasks>(conf.Platform);
                conf.GeneratorSetGeneratedInformation(
                    platformVcxproj.ExecutableFileExtension,
                    platformVcxproj.PackageFileExtension,
                    configurationTasks.GetDefaultOutputExtension(Project.Configuration.OutputType.Dll),
                    platformVcxproj.ProgramDatabaseFileExtension);

                projectConfigurationOptions.Add(conf, new Options.ExplicitOptions());
                context.CommandLineOptions = new ProjectOptionsGenerator.VcxprojCmdLineOptions();

                projectOptionsGen.GenerateOptions(context);
                GenerateOptions(context);

                context.Reset(); // just a safety, not necessary to clean up
            }
        }

        private void GenerateImpl(
            GenerationContext context,
            List<string> generatedFiles,
            List<string> skipFiles)
        {
            GenerateConfOptions(context);

            var fileGenerator = new XmlFileGenerator();

            // xml begin header
            string toolsVersion = context.DevelopmentEnvironmentsRange.MinDevEnv.GetVisualProjectToolsVersionString();
            using (fileGenerator.Declare("toolsVersion", toolsVersion))
                fileGenerator.Write(Template.Project.ProjectBegin);

            VsProjCommon.WriteCustomProperties(context.Project.CustomProperties, fileGenerator);

            VsProjCommon.WriteProjectConfigurationsDescription(context.ProjectConfigurations, fileGenerator);

            // xml end header

            string androidTargetsPath = Options.GetConfOption<Options.Android.General.AndroidTargetsPath>(context.ProjectConfigurations, rootpath: context.ProjectDirectoryCapitalized);

            var firstConf = context.ProjectConfigurations.First();
            using (fileGenerator.Declare("projectName", firstConf.ProjectName))
            using (fileGenerator.Declare("guid", firstConf.ProjectGuid))
            using (fileGenerator.Declare("toolsVersion", toolsVersion))
            using (fileGenerator.Declare("androidTargetsPath", Util.EnsureTrailingSeparator(androidTargetsPath)))
            {
                fileGenerator.Write(Template.Project.ProjectDescription);
            }

            fileGenerator.Write(VsProjCommon.Template.PropertyGroupEnd);

            foreach (var platform in context.PresentPlatforms.Values)
                platform.GeneratePlatformSpecificProjectDescription(context, fileGenerator);

            fileGenerator.Write(Template.Project.ImportAndroidDefaultProps);

            foreach (var platform in context.PresentPlatforms.Values)
                platform.GeneratePostDefaultPropsImport(context, fileGenerator);

            // configuration general
            foreach (Project.Configuration conf in context.ProjectConfigurations)
            {
                context.Configuration = conf;

                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
                using (fileGenerator.Declare("conf", conf))
                using (fileGenerator.Declare("options", context.ProjectConfigurationOptions[conf]))
                {
                    fileGenerator.Write(Template.Project.ProjectConfigurationsGeneral);
                }
            }

            // .props files
            fileGenerator.Write(Template.Project.ProjectAfterConfigurationsGeneral);

            VsProjCommon.WriteProjectCustomPropsFiles(context.Project.CustomPropsFiles, context.ProjectDirectoryCapitalized, fileGenerator);
            VsProjCommon.WriteConfigurationsCustomPropsFiles(context.ProjectConfigurations, context.ProjectDirectoryCapitalized, fileGenerator);

            fileGenerator.Write(Template.Project.ProjectAfterImportedProps);

            string androidPackageDirectory = context.AndroidPackageProject.AntBuildRootDirectory;

            // configuration ItemDefinitionGroup
            foreach (Project.Configuration conf in context.ProjectConfigurations)
            {
                context.Configuration = conf;

                using (fileGenerator.Declare("platformName", Util.GetPlatformString(conf.Platform, conf.Project, conf.Target)))
                using (fileGenerator.Declare("conf", conf))
                using (fileGenerator.Declare("options", context.ProjectConfigurationOptions[conf]))
                using (fileGenerator.Declare("androidPackageDirectory", androidPackageDirectory))
                {
                    fileGenerator.Write(Template.Project.ProjectConfigurationBeginItemDefinition);
                    {
                        fileGenerator.Write(Template.Project.AntPackage);
                    }
                    fileGenerator.Write(Template.Project.ProjectConfigurationEndItemDefinition);
                }
            }

            GenerateFilesSection(context, fileGenerator);

            // .targets
            fileGenerator.Write(Template.Project.ProjectTargets);

            GenerateProjectReferences(context, fileGenerator);

            // Environment variables
            var environmentVariables = context.ProjectConfigurations.Select(conf => conf.Platform).Distinct().SelectMany(platform => context.PresentPlatforms[platform].GetEnvironmentVariables(context));
            VsProjCommon.WriteEnvironmentVariables(environmentVariables, fileGenerator);

            fileGenerator.Write(Template.Project.ProjectEnd);

            // remove all line that contain RemoveLineTag
            fileGenerator.RemoveTaggedLines();
            MemoryStream cleanMemoryStream = fileGenerator.ToMemoryStream();

            FileInfo projectFileInfo = new FileInfo(context.ProjectPath + ProjectExtension);
            if (context.Builder.Context.WriteGeneratedFile(context.Project.GetType(), projectFileInfo, cleanMemoryStream))
                generatedFiles.Add(projectFileInfo.FullName);
            else
                skipFiles.Add(projectFileInfo.FullName);
        }

        private void GenerateFilesSection(
            GenerationContext context,
            IFileGenerator fileGenerator)
        {
            Strings projectFiles = context.Project.GetSourceFilesForConfigurations(context.ProjectConfigurations);

            // Add source files
            var allFiles = new List<Vcxproj.ProjectFile>();
            var includeFiles = new List<Vcxproj.ProjectFile>();
            var sourceFiles = new List<Vcxproj.ProjectFile>();
            var contentFiles = new List<Vcxproj.ProjectFile>();

            foreach (string file in projectFiles)
            {
                var projectFile = new Vcxproj.ProjectFile(context, file);
                allFiles.Add(projectFile);
            }

            allFiles.Sort((l, r) => { return string.Compare(l.FileNameProjectRelative, r.FileNameProjectRelative, StringComparison.InvariantCultureIgnoreCase); });

            // type -> files
            var customSourceFiles = new Dictionary<string, List<Vcxproj.ProjectFile>>();
            foreach (var projectFile in allFiles)
            {
                string type = null;
                if (context.Project.ExtensionBuildTools.TryGetValue(projectFile.FileExtension, out type))
                {
                    List<Vcxproj.ProjectFile> files = null;
                    if (!customSourceFiles.TryGetValue(type, out files))
                    {
                        files = new List<Vcxproj.ProjectFile>();
                        customSourceFiles[type] = files;
                    }
                    files.Add(projectFile);
                }
                else if (context.Project.SourceFilesCompileExtensions.Contains(projectFile.FileExtension) ||
                         (String.Compare(projectFile.FileExtension, ".rc", StringComparison.OrdinalIgnoreCase) == 0))
                {
                    sourceFiles.Add(projectFile);
                }
                else if (String.Compare(projectFile.FileExtension, ".h", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    includeFiles.Add(projectFile);
                }
                else
                {
                    contentFiles.Add(projectFile);
                }
            }

            // Write header files
            fileGenerator.Write(Template.Project.ProjectFilesBegin);
            foreach (var file in includeFiles)
            {
                using (fileGenerator.Declare("file", file))
                    fileGenerator.Write(Template.Project.ProjectFilesHeader);
            }
            fileGenerator.Write(Template.Project.ProjectFilesEnd);

            // Write content files
            fileGenerator.Write(Template.Project.ProjectFilesBegin);
            foreach (var file in contentFiles)
            {
                using (fileGenerator.Declare("file", file))
                    fileGenerator.Write(Template.Project.ContentSimple);
            }
            fileGenerator.Write(Template.Project.ProjectFilesEnd);


            // Write Android project files
            fileGenerator.Write(Template.Project.ItemGroupBegin);

            using (fileGenerator.Declare("antBuildXml", context.AndroidPackageProject.AntBuildXml))
            using (fileGenerator.Declare("antProjectPropertiesFile", context.AndroidPackageProject.AntProjectPropertiesFile))
            using (fileGenerator.Declare("androidManifest", context.AndroidPackageProject.AndroidManifest))
            {
                fileGenerator.Write(Template.Project.AntBuildXml);
                fileGenerator.Write(Template.Project.AndroidManifest);
                fileGenerator.Write(Template.Project.AntProjectPropertiesFile);
            }
            fileGenerator.Write(Template.Project.ItemGroupEnd);
        }

        private struct ProjectDependencyInfo
        {
            public string ProjectFullFileNameWithExtension;
            public string ProjectGuid;
        }

        private void GenerateProjectReferences(
            GenerationContext context,
            IFileGenerator fileGenerator)
        {
            var dependencies = new UniqueList<ProjectDependencyInfo>();
            foreach (var c in context.ProjectConfigurations)
            {
                foreach (var d in c.ConfigurationDependencies)
                {
                    // Ignore projects marked as Export
                    if (d.Project.SharpmakeProjectType == Project.ProjectTypeAttribute.Export)
                        continue;

                    ProjectDependencyInfo depInfo;
                    depInfo.ProjectFullFileNameWithExtension = d.ProjectFullFileNameWithExtension;

                    if (d.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Compile)
                        depInfo.ProjectGuid = d.ProjectGuid;
                    else
                        throw new NotImplementedException("Sharpmake.Compile not supported as a dependency by this generator.");
                    dependencies.Add(depInfo);
                }
            }

            if (dependencies.Count > 0)
            {
                fileGenerator.Write(Template.Project.ItemGroupBegin);
                foreach (var d in dependencies)
                {
                    string include = Util.PathGetRelative(context.ProjectDirectory, d.ProjectFullFileNameWithExtension);
                    using (fileGenerator.Declare("include", include))
                    using (fileGenerator.Declare("projectGUID", d.ProjectGuid))
                    {
                        fileGenerator.Write(Template.Project.ProjectReference);
                    }
                }
                fileGenerator.Write(Template.Project.ItemGroupEnd);
            }
        }

        private void GenerateOptions(GenerationContext context)
        {
            var options = context.Options;
            var conf = context.Configuration;

            //OutputFile ( APK File )
            options["OutputFile"] = conf.TargetFileFullName;

            //AndroidAppLibName Native Library Packaged into the APK
            options["AndroidAppLibName"] = FileGeneratorUtilities.RemoveLineTag;
            if (context.AndroidPackageProject.AppLibType != null)
            {
                Project.Configuration appLibConf = conf.ConfigurationDependencies.FirstOrDefault(confDep => (confDep.Project.GetType() == context.AndroidPackageProject.AppLibType));
                if (appLibConf != null)
                {
                    // The lib name to first load from an AndroidActivity must be a dynamic library.
                    if (appLibConf.Output != Project.Configuration.OutputType.Dll)
                        throw new Error("Cannot use configuration \"{0}\" as app lib for package configuration \"{1}\". Output type must be set to dynamic library.", appLibConf, conf);

                    options["AndroidAppLibName"] = appLibConf.TargetFileFullName;
                }
                else
                {
                    throw new Error("Missing dependency of type \"{0}\" in configuration \"{1}\" dependencies.", context.AndroidPackageProject.AppLibType.ToNiceTypeName(), conf);
                }
            }
            //OutputDirectory
            //    The debugger need a rooted path to work properly.
            //    So we root the relative output directory to $(ProjectDir) to work around this limitation.
            //    Hopefully in a future version of the cross platform tools will be able to remove this hack.
            string outputDirectoryRelative = Util.PathGetRelative(context.ProjectDirectoryCapitalized, conf.TargetPath);
            options["OutputDirectory"] = outputDirectoryRelative;

            //IntermediateDirectory
            string intermediateDirectoryRelative = Util.PathGetRelative(context.ProjectDirectoryCapitalized, conf.IntermediatePath);
            options["IntermediateDirectory"] = intermediateDirectoryRelative;
        }
    }
}
