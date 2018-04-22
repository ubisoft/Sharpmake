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
using System.Text;

namespace Sharpmake.Generators.Generic
{
    // TODO: Pre and post build commands.
    // TODO: Precompiled header support.
    // TODO: Dynamic library support.

    /// <summary>
    ///
    /// </summary>
    public partial class Makefile : IProjectGenerator, ISolutionGenerator
    {
        private const string MakeExtension = ".make";
        private const string ObjectExtension = ".o";

        // TODO: Yet another ProjectFile! Would be a good idea to move this into a common class.
        private class ProjectFile
        {
            public string FileName;
            public string DirectorySourceRelative;
            public string FileNameProjectRelative;
            public string FileNameWithoutExtension;
            public string FileExtensionLower;
            public int FileIndex; // When the file name is used multiple times

            public ProjectFile(string fileName, string projectPathCapitalized, string projectSourceCapitalized, int index)
            {
                FileName = Project.GetCapitalizedFile(fileName) ?? fileName; // LC TODO can it really return null ???
                FileIndex = index;

                FileNameProjectRelative = Util.PathGetRelative(projectPathCapitalized, FileName, true);
                string fileNameSourceRelative = Util.PathGetRelative(projectSourceCapitalized, FileName, true);

                FileExtensionLower = Path.GetExtension(FileName).ToLower();
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);

                int lastPathSeparator = fileNameSourceRelative.LastIndexOf(Path.DirectorySeparatorChar);
                if (lastPathSeparator != -1)
                {
                    DirectorySourceRelative = fileNameSourceRelative.Substring(0, lastPathSeparator);
                    DirectorySourceRelative = DirectorySourceRelative.Trim('.', Path.DirectorySeparatorChar);
                }
                else
                {
                    DirectorySourceRelative = "";
                }
            }

            public string GetObjectFileName()
            {
                if (FileIndex > 0)
                {
                    return FileNameWithoutExtension + FileIndex + ObjectExtension;
                }
                else
                {
                    return FileNameWithoutExtension + ObjectExtension;
                }
            }
        }

        #region Solution

        public void Generate(
            Builder builder,
            Solution solution,
            List<Solution.Configuration> configurations,
            string solutionFile,
            List<string> generatedFiles,
            List<string> skipFiles)
        {
            ValidateSolutionConfigurations(solution, configurations);

            FileInfo fileInfo = new FileInfo(solutionFile);
            string solutionPath = fileInfo.Directory.FullName;
            string solutionFileName = fileInfo.Name;

            bool updated;
            string solutionFileResult = GenerateSolution(builder, solution, configurations, solutionPath, solutionFileName, out updated);
            if (updated)
                generatedFiles.Add(solutionFileResult);
            else
                skipFiles.Add(solutionFileResult);
        }

        private string GenerateSolution(
            Builder builder,
            Solution solution,
            List<Solution.Configuration> configurations,
            string solutionPath,
            string solutionFile,
            out bool updated)
        {
            FileInfo solutionFileInfo = new FileInfo(Util.GetCapitalizedPath(solutionPath + Path.DirectorySeparatorChar + solutionFile + MakeExtension));
            bool projectsWereFiltered = false;
            List<Solution.ResolvedProject> solutionProjects = solution.GetResolvedProjects(configurations, out projectsWereFiltered).ToList();
            solutionProjects.Sort((a, b) => string.Compare(a.ProjectName, b.ProjectName)); // Ensure all projects are always in the same order to avoid random shuffles

            if (solutionProjects.Count == 0)
            {
                // Erase solution file if solution has no projects.
                updated = solutionFileInfo.Exists;
                if (updated)
                    Util.TryDeleteFile(solutionFileInfo.FullName);
                return solutionFileInfo.FullName;
            }

            // Write it all in memory to not overwrite if no changes detected.
            var fileGenerator = new FileGenerator();
            using (fileGenerator.Declare("defaultConfig", configurations[0].Name.ToLower()))
            {
                fileGenerator.Write(Template.Solution.Header);
            }

            fileGenerator.WriteVerbatim(Template.Solution.ProjectsVariableBegin);
            foreach (Solution.ResolvedProject resolvedProject in solutionProjects)
            {
                using (fileGenerator.Declare("projectName", resolvedProject.ProjectName))
                {
                    fileGenerator.Write(Template.Solution.ProjectsVariableElement);
                }
            }
            fileGenerator.WriteVerbatim(Template.Solution.ProjectsVariableEnd);

            fileGenerator.WriteVerbatim(Template.Solution.PhonyTargets);

            fileGenerator.WriteVerbatim(Template.Solution.AllRule);

            // Projects rules
            foreach (Solution.ResolvedProject resolvedProject in solutionProjects)
            {
                FileInfo projectFileInfo = new FileInfo(resolvedProject.ProjectFile);
                using (fileGenerator.Declare("projectName", resolvedProject.ProjectName))
                using (fileGenerator.Declare("projectFileDirectory", PathMakeUnix(Util.PathGetRelative(solutionFileInfo.DirectoryName, projectFileInfo.DirectoryName))))
                using (fileGenerator.Declare("projectFileName", projectFileInfo.Name))
                {
                    fileGenerator.Write(Template.Solution.ProjectRuleBegin);
                    foreach (Solution.ResolvedProject resolvedDependency in resolvedProject.Dependencies)
                    {
                        using (fileGenerator.Declare("dependencyName", resolvedDependency.ProjectName))
                        {
                            fileGenerator.Write(Template.Solution.ProjectRuleDependency);
                        }
                    }
                    fileGenerator.Write(Template.Solution.ProjectRuleEnd);
                }
            }

            // Clean rule
            fileGenerator.WriteVerbatim(Template.Solution.CleanRuleBegin);
            foreach (Solution.ResolvedProject resolvedProject in solutionProjects)
            {
                FileInfo projectFileInfo = new FileInfo(resolvedProject.ProjectFile);
                using (fileGenerator.Declare("projectFileDirectory", PathMakeUnix(Util.PathGetRelative(solutionFileInfo.DirectoryName, projectFileInfo.DirectoryName))))
                using (fileGenerator.Declare("projectFileName", projectFileInfo.Name))
                {
                    fileGenerator.Write(Template.Solution.CleanRuleProject);
                }
            }
            fileGenerator.WriteVerbatim(Template.Solution.CleanRuleEnd);

            // Help rule
            fileGenerator.WriteVerbatim(Template.Solution.HelpRuleBegin);
            foreach (Project.Configuration conf in solutionProjects.First().Configurations)
            {
                // Optimizations enumeration rely on the fact that all projects share the same targets as the solution.
                using (fileGenerator.Declare("optimization", conf.Target.GetOptimization().ToString().ToLower()))
                {
                    fileGenerator.Write(Template.Solution.HelpRuleConfiguration);
                }
            }
            fileGenerator.WriteVerbatim(Template.Solution.HelpRuleTargetsBegin);
            foreach (Solution.ResolvedProject resolvedProject in solutionProjects)
            {
                using (fileGenerator.Declare("projectName", resolvedProject.ProjectName))
                {
                    fileGenerator.Write(Template.Solution.HelpRuleTarget);
                }
            }
            fileGenerator.WriteVerbatim(Template.Solution.HelpRuleEnd);

            // Write the solution file
            updated = builder.Context.WriteGeneratedFile(solution.GetType(), solutionFileInfo, fileGenerator.ToMemoryStream());

            solution.PostGenerationCallback?.Invoke(solutionPath, solutionFile, MakeExtension);

            return solutionFileInfo.FullName;
        }

        /// <summary>
        /// Validate that all solution configurations meet the requirements of the generator.
        /// </summary>
        /// <exception cref="Error">The solution contains an invalid configuration.</exception>
        private void ValidateSolutionConfigurations(Solution solution, List<Solution.Configuration> configurations)
        {
            // Validate that all solution configuration name are unique for a given platform.
            // This is a requirement for the generated project makefile.
            Dictionary<string, Solution.Configuration> solutionNameMapping = new Dictionary<string, Solution.Configuration>();
            foreach (Solution.Configuration conf in configurations)
            {
                Solution.Configuration otherConf;

                string configurationPlatformAndName = conf.Name + "|" + conf.PlatformName;

                if (solutionNameMapping.TryGetValue(configurationPlatformAndName, out otherConf))
                    throw new Error("Solution {0} has 2 configurations with the same name: \"{1}\" for {2} and {3}", solution.Name, conf.Name, otherConf.Target, conf.Target);

                solutionNameMapping[configurationPlatformAndName] = conf;
            }
        }

        #endregion

        #region Project

        public void Generate(
            Builder builder,
            Project project,
            List<Project.Configuration> configurations,
            string projectFile,
            List<string> generatedFiles,
            List<string> skipFiles)
        {
            var projectFileInfo = new FileInfo(Util.GetCapitalizedPath(projectFile + MakeExtension));

            ValidateProjectConfigurations(project, configurations, projectFileInfo);

            bool updated;
            string projectFileResult = GenerateProject(builder, project, configurations, projectFileInfo, out updated);
            if (updated)
                generatedFiles.Add(projectFileResult);
            else
                skipFiles.Add(projectFileResult);
        }

        private string GenerateProject(
            Builder builder,
            Project project,
            List<Project.Configuration> unsortedConfigurations,
            FileInfo projectFileInfo,
            out bool updated)
        {
            // Need to sort by name and platform
            List<Project.Configuration> configurations = new List<Project.Configuration>();
            configurations.AddRange(unsortedConfigurations.OrderBy(conf => conf.Name + conf.Platform));

            // Build source files list.
            List<ProjectFile> sourceFiles = GetSourceFiles(project, configurations, projectFileInfo);

            // Generate options.
            Dictionary<Project.Configuration, Options.ExplicitOptions> options = new Dictionary<Project.Configuration, Options.ExplicitOptions>();
            foreach (Project.Configuration conf in configurations)
            {
                Options.ExplicitOptions option = GenerateOptions(conf, projectFileInfo);
                options.Add(conf, option);
            }

            var fileGenerator = new FileGenerator();
            {
                fileGenerator.Write(Template.Project.Header);

                // Configurations variables.
                foreach (Project.Configuration conf in configurations)
                {
                    using (fileGenerator.Declare("name", conf.Name.ToLower()))
                    using (fileGenerator.Declare("options", options[conf]))
                    {
                        fileGenerator.Write(Template.Project.ProjectConfigurationVariables);
                    }
                }

                // Objects variables
                foreach (Project.Configuration conf in configurations)
                {
                    using (fileGenerator.Declare("name", conf.Name.ToLower()))
                    {
                        fileGenerator.Write(Template.Project.ObjectsVariableBegin);
                        foreach (ProjectFile file in sourceFiles)
                        {
                            // Excluded source files are written to the makefile but are commented out.
                            // This support the use case where you have a huge unit tests suite that take too long to compile.
                            // In this case, you just exclude all unit tests from the build and manually uncomment only the unit tests you want to build.
                            using (fileGenerator.Declare("excludeChar", conf.ResolvedSourceFilesBuildExclude.Contains(file.FileName) ? "#" : ""))
                            using (fileGenerator.Declare("objectFile", file.GetObjectFileName()))
                            {
                                fileGenerator.Write(Template.Project.ObjectsVariableElement);
                            }
                        }
                        fileGenerator.Write(Template.Project.ObjectsVariableEnd);
                    }
                }

                // General rules
                using (fileGenerator.Declare("projectName", project.Name))
                {
                    fileGenerator.Write(Template.Project.ProjectRulesGeneral);
                }

                // Source file rules
                // Since we write excluded source files commented. Here we write rules for all files
                // in case one of the commented out object file is manually uncomment.
                foreach (ProjectFile file in sourceFiles)
                {
                    using (fileGenerator.Declare("objectFile", file.GetObjectFileName()))
                    using (fileGenerator.Declare("sourceFile", PathMakeUnix(file.FileNameProjectRelative)))
                    {
                        if (file.FileExtensionLower == ".c")
                        {
                            fileGenerator.Write(Template.Project.ObjectRuleC);
                        }
                        else
                        {
                            fileGenerator.Write(Template.Project.ObjectRuleCxx);
                        }
                    }
                }

                fileGenerator.Write(Template.Project.Footer);

                // Write the project file
                updated = builder.Context.WriteGeneratedFile(project.GetType(), projectFileInfo, fileGenerator.ToMemoryStream());
            }

            return projectFileInfo.FullName;
        }

        /// <summary>
        /// Validate that all project configurations meet the requirements of the generator.
        /// </summary>
        /// <exception cref="Error">The project contains an invalid configuration.</exception>
        private void ValidateProjectConfigurations(
            Project project,
            List<Project.Configuration> configurations,
            FileInfo projectFileInfo)
        {
            Dictionary<string, Project.Configuration> configurationNameMapping = new Dictionary<string, Project.Configuration>();
            string projectName = null;

            foreach (Project.Configuration conf in configurations)
            {
                // All configurations must share the same project name.
                if (projectName == null)
                    projectName = conf.ProjectName;
                else if (projectName != conf.ProjectName)
                    throw new Error("Project configurations in the same project files must be the same: {0} != {1} in {2}", projectName, conf.ProjectName, projectFileInfo.Name);


                // Validate that 2 conf name in the same project and for a given platform don't have the same name.
                Project.Configuration otherConf;
                string projectUniqueName = conf.Name + Util.GetPlatformString(conf.Platform, conf.Project, conf.Target);
                if (configurationNameMapping.TryGetValue(projectUniqueName, out otherConf))
                {
                    throw new Error(
                        "Project {0} ({4} in {5}) has 2 configurations with the same name: \"{1}\" for {2} and {3}",
                        project.Name, conf.Name, otherConf.Target, conf.Target, projectFileInfo.Name, projectFileInfo.DirectoryName);
                }

                configurationNameMapping[projectUniqueName] = conf;

                // set generator information
                switch (conf.Platform)
                {
                    case Platform.linux:
                        conf.GeneratorSetGeneratedInformation("elf", "elf", "so", "pdb");
                        break;
                    default:
                        break;
                }
            }
        }

        private Options.ExplicitOptions GenerateOptions(Project.Configuration conf, FileInfo projectFileInfo)
        {
            Options.ExplicitOptions options = new Options.ExplicitOptions();

            // CompilerToUse
            SelectOption(conf,
                Options.Option(Options.Makefile.General.PlatformToolset.Gcc, () => { options["CompilerToUse"] = "g++"; }),
                Options.Option(Options.Makefile.General.PlatformToolset.Clang, () => { options["CompilerToUse"] = "clang++"; })
                );

            // IntermediateDirectory
            options["IntermediateDirectory"] = PathMakeUnix(Util.PathGetRelative(projectFileInfo.DirectoryName, conf.IntermediatePath));

            // OutputDirectory
            string outputDirectory = PathMakeUnix(GetOutputDirectory(conf, projectFileInfo));
            options["OutputDirectory"] = outputDirectory;

            #region Compiler

            // Defines
            Strings defines = new Strings();
            defines.AddRange(conf.Defines);
            defines.InsertPrefix("-D");
            options["Defines"] = defines.JoinStrings(" ");

            // Includes
            OrderableStrings includePaths = new OrderableStrings();
            includePaths.AddRange(Util.PathGetRelative(projectFileInfo.DirectoryName, Util.PathGetCapitalized(conf.IncludePrivatePaths)));
            includePaths.AddRange(Util.PathGetRelative(projectFileInfo.DirectoryName, Util.PathGetCapitalized(conf.IncludePaths)));
            includePaths.AddRange(Util.PathGetRelative(projectFileInfo.DirectoryName, Util.PathGetCapitalized(conf.DependenciesIncludePaths)));
            PathMakeUnix(includePaths);
            includePaths.InsertPrefix("-I");
            options["Includes"] = includePaths.JoinStrings(" ");

            if (conf.ForcedIncludes.Count > 0)
            {
                OrderableStrings relativeForceIncludes = new OrderableStrings(Util.PathGetRelative(projectFileInfo.DirectoryName, conf.ForcedIncludes));
                PathMakeUnix(relativeForceIncludes);
                relativeForceIncludes.InsertPrefix("-include ");
                options["Includes"] += " " + relativeForceIncludes.JoinStrings(" ");
            }

            // CFLAGS
            {
                StringBuilder cflags = new StringBuilder();

                // ExtraWarnings
                SelectOption(conf,
                    Options.Option(Options.Makefile.Compiler.ExtraWarnings.Enable, () => { cflags.Append("-Wextra "); }),
                    Options.Option(Options.Makefile.Compiler.ExtraWarnings.Disable, () => { cflags.Append(""); })
                    );

                // GenerateDebugInformation
                SelectOption(conf,
                    Options.Option(Options.Makefile.Compiler.GenerateDebugInformation.Enable, () => { cflags.Append("-g "); }),
                    Options.Option(Options.Makefile.Compiler.GenerateDebugInformation.Disable, () => { cflags.Append(""); })
                    );

                // OptimizationLevel
                SelectOption(conf,
                    Options.Option(Options.Makefile.Compiler.OptimizationLevel.Disable, () => { cflags.Append(""); }),
                    Options.Option(Options.Makefile.Compiler.OptimizationLevel.Standard, () => { cflags.Append("-O1 "); }),
                    Options.Option(Options.Makefile.Compiler.OptimizationLevel.Full, () => { cflags.Append("-O2 "); }),
                    Options.Option(Options.Makefile.Compiler.OptimizationLevel.FullWithInlining, () => { cflags.Append("-O3 "); }),
                    Options.Option(Options.Makefile.Compiler.OptimizationLevel.ForSize, () => { cflags.Append("-Os "); })
                    );

                // Warnings
                SelectOption(conf,
                    Options.Option(Options.Makefile.Compiler.Warnings.NormalWarnings, () => { cflags.Append(""); }),
                    Options.Option(Options.Makefile.Compiler.Warnings.MoreWarnings, () => { cflags.Append("-Wall "); }),
                    Options.Option(Options.Makefile.Compiler.Warnings.Disable, () => { cflags.Append("-w "); })
                    );

                // WarningsAsErrors
                SelectOption(conf,
                    Options.Option(Options.Makefile.Compiler.TreatWarningsAsErrors.Enable, () => { cflags.Append("-Werror "); }),
                    Options.Option(Options.Makefile.Compiler.TreatWarningsAsErrors.Disable, () => { cflags.Append(""); })
                    );

                // AdditionalCompilerOptions
                cflags.Append(conf.AdditionalCompilerOptions.JoinStrings(" "));

                options["CFLAGS"] = cflags.ToString();
            }

            // CXXFLAGS
            {
                StringBuilder cxxflags = new StringBuilder();

                // CppLanguageStandard
                SelectOption(conf,
                    Options.Option(Options.Makefile.Compiler.CppLanguageStandard.Cpp17, () => { cxxflags.Append("-std=c++17 "); }),
                    Options.Option(Options.Makefile.Compiler.CppLanguageStandard.Cpp14, () => { cxxflags.Append("-std=c++14 "); }),
                    Options.Option(Options.Makefile.Compiler.CppLanguageStandard.Cpp11, () => { cxxflags.Append("-std=c++11 "); }),
                    Options.Option(Options.Makefile.Compiler.CppLanguageStandard.Cpp98, () => { cxxflags.Append("-std=c++98 "); }),
                    Options.Option(Options.Makefile.Compiler.CppLanguageStandard.GnuCpp11, () => { cxxflags.Append("-std=gnu++11 "); }),
                    Options.Option(Options.Makefile.Compiler.CppLanguageStandard.GnuCpp98, () => { cxxflags.Append("-std=gnu++98 "); }),
                    Options.Option(Options.Makefile.Compiler.CppLanguageStandard.Default, () => { cxxflags.Append(""); })
                    );

                // Exceptions
                SelectOption(conf,
                    Options.Option(Options.Makefile.Compiler.Exceptions.Enable, () => { cxxflags.Append("-fexceptions "); }),
                    Options.Option(Options.Makefile.Compiler.Exceptions.Disable, () => { cxxflags.Append("-fno-exceptions "); })
                    );

                // RTTI
                SelectOption(conf,
                    Options.Option(Options.Makefile.Compiler.Rtti.Enable, () => { cxxflags.Append("-frtti "); }),
                    Options.Option(Options.Makefile.Compiler.Rtti.Disable, () => { cxxflags.Append("-fno-rtti "); })
                    );

                options["CXXFLAGS"] = cxxflags.ToString();
            }

            #endregion

            #region Linker

            // OutputFile
            options["OutputFile"] = FormatOutputFileName(conf);

            // DependenciesLibraryFiles
            OrderableStrings dependenciesLibraryFiles = new OrderableStrings(conf.DependenciesLibraryFiles);
            PathMakeUnix(dependenciesLibraryFiles);
            dependenciesLibraryFiles.InsertPrefix("-l");
            options["DependenciesLibraryFiles"] = dependenciesLibraryFiles.JoinStrings(" ");

            // LibraryFiles
            OrderableStrings libraryFiles = new OrderableStrings(conf.LibraryFiles);
            libraryFiles.InsertPrefix("-l");
            options["LibraryFiles"] = libraryFiles.JoinStrings(" ");

            // LibraryPaths
            OrderableStrings libraryPaths = new OrderableStrings();
            libraryPaths.AddRange(Util.PathGetRelative(projectFileInfo.DirectoryName, conf.LibraryPaths));
            libraryPaths.AddRange(Util.PathGetRelative(projectFileInfo.DirectoryName, conf.DependenciesOtherLibraryPaths));
            libraryPaths.AddRange(Util.PathGetRelative(projectFileInfo.DirectoryName, conf.DependenciesBuiltTargetsLibraryPaths));
            PathMakeUnix(libraryPaths);
            libraryPaths.InsertPrefix("-L");
            options["LibraryPaths"] = libraryPaths.JoinStrings(" ");

            // Dependencies
            var deps = new OrderableStrings();
            foreach (Project.Configuration depConf in conf.ResolvedDependencies)
            {
                switch (depConf.Output)
                {
                    case Project.Configuration.OutputType.None: continue;
                    case Project.Configuration.OutputType.Lib:
                    case Project.Configuration.OutputType.Dll:
                    case Project.Configuration.OutputType.DotNetClassLibrary:
                        deps.Add(Path.Combine(depConf.TargetLibraryPath, FormatOutputFileName(depConf)), depConf.TargetFileOrderNumber);
                        break;
                    default:
                        deps.Add(Path.Combine(depConf.TargetPath, FormatOutputFileName(depConf)), depConf.TargetFileOrderNumber);
                        break;
                }
            }
            var depsRelative = Util.PathGetRelative(projectFileInfo.DirectoryName, deps);
            PathMakeUnix(depsRelative);
            options["LDDEPS"] = depsRelative.JoinStrings(" ");

            // LinkCommand
            if (conf.Output == Project.Configuration.OutputType.Lib)
            {
                options["LinkCommand"] = Template.Project.LinkCommandLib;
            }
            else
            {
                options["LinkCommand"] = Template.Project.LinkCommandExe;
            }

            if (conf.AdditionalLibrarianOptions.Any())
                throw new NotImplementedException(nameof(conf.AdditionalLibrarianOptions) + " not supported with Makefile generator");

            string linkerAdditionalOptions = conf.AdditionalLinkerOptions.JoinStrings(" ");
            options["AdditionalLinkerOptions"] = linkerAdditionalOptions;

            // this is supported in both gcc and clang
            SelectOption(conf,
                Options.Option(Options.Makefile.Linker.LibGroup.Enable, () => { options["LibsStartGroup"] = " -Wl,--start-group "; options["LibsEndGroup"] = " -Wl,--end-group "; }),
                Options.Option(Options.Makefile.Linker.LibGroup.Disable, () => { options["LibsStartGroup"] = string.Empty; options["LibsEndGroup"] = string.Empty; })
                );

            #endregion

            return options;
        }

        private List<ProjectFile> GetSourceFiles(
            Project project,
            List<Project.Configuration> configurations,
            FileInfo projectFileInfo)
        {
            Dictionary<string, int> fileNamesOccurences = new Dictionary<string, int>();

            Strings projectSourceFiles = project.GetSourceFilesForConfigurations(configurations);
            projectSourceFiles.RemoveRange(project.GetAllConfigurationBuildExclude(configurations));

            // Add source files
            List<ProjectFile> allFiles = new List<ProjectFile>();
            List<ProjectFile> sourceFiles = new List<ProjectFile>();

            foreach (string file in projectSourceFiles)
            {
                string fileName = Path.GetFileName(file);
                int fileNameOccurences = 0;
                if (fileNamesOccurences.TryGetValue(fileName, out fileNameOccurences))
                {
                    fileNamesOccurences[fileName] = fileNameOccurences++;
                }
                else
                {
                    fileNamesOccurences.Add(fileName, fileNameOccurences);
                }

                ProjectFile projectFile = new ProjectFile(file, projectFileInfo.DirectoryName, Util.GetCapitalizedPath(project.SourceRootPath), fileNameOccurences);
                allFiles.Add(projectFile);
            }

            allFiles.Sort((l, r) => string.Compare(l.FileNameProjectRelative, r.FileNameProjectRelative, System.StringComparison.OrdinalIgnoreCase));

            // type -> files
            foreach (ProjectFile projectFile in allFiles)
            {
                string type;
                if (project.ExtensionBuildTools.TryGetValue(projectFile.FileExtensionLower, out type))
                {
                    // Ignored.
                }
                else if (project.SourceFilesCompileExtensions.Contains(projectFile.FileExtensionLower))
                {
                    sourceFiles.Add(projectFile);
                }
            }

            return sourceFiles;
        }

        private string GetOutputDirectory(Project.Configuration conf, FileInfo projectFileInfo)
        {
            if (conf.Output == Project.Configuration.OutputType.Lib)
                return Util.PathGetRelative(projectFileInfo.DirectoryName, conf.TargetLibraryPath);
            else
                return Util.PathGetRelative(projectFileInfo.DirectoryName, conf.TargetPath);
        }

        private static string FormatOutputFileName(Project.Configuration conf)
        {
            string outputExtension = !string.IsNullOrEmpty(conf.OutputExtension) ? "." + conf.OutputExtension : "";
            string targetNamePrefix = (conf.Output == Project.Configuration.OutputType.Lib) ? "lib" : "";
            return (targetNamePrefix + conf.TargetFileFullName + outputExtension);
        }

        #endregion

        #region Utils

        private static string PathMakeUnix(string path)
        {
            return path.Replace(Util.WindowsSeparator, Util.UnixSeparator).TrimEnd(Util.UnixSeparator);
        }

        private static void PathMakeUnix(IList<string> paths)
        {
            for (int i = 0; i < paths.Count; ++i)
                paths[i] = PathMakeUnix(paths[i]);
        }

        private void SelectOption(Project.Configuration conf, params Options.OptionAction[] options)
        {
            Options.SelectOption(conf, options);
        }

        #endregion
    }
}
