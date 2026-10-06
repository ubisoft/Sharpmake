// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Slnx : ISolutionGenerator
    {
        public const string SolutionExtension = ".slnx";

        private readonly SolutionFolderTree _folderTree = new SolutionFolderTree();
        private Builder _builder;

        /// <summary>
        /// Validates that the requested <see cref="Solution.SolutionFormat"/> is compatible with the
        /// resolved <see cref="DevEnv"/>. The XML (<c>.slnx</c>) format is only supported on vs2026.
        /// </summary>
        public static void ValidateDevEnv(string solutionName, Solution.SolutionFormat format, DevEnv devEnv)
        {
            if (format == Solution.SolutionFormat.Xml && devEnv != DevEnv.vs2026)
            {
                throw new Error(
                    "Solution '{0}': SolutionFormat.Xml (.slnx) requires DevEnv.vs2026 but the solution targets {1}.",
                    solutionName, devEnv);
            }
        }

        public void Generate(
            Builder builder,
            Solution solution,
            List<Solution.Configuration> configurations,
            string solutionFile,
            List<string> generatedFiles,
            List<string> skipFiles)
        {
            _builder = builder;

            FileInfo fileInfo = new FileInfo(solutionFile);
            string solutionPath = fileInfo.Directory.FullName;
            string solutionFileName = fileInfo.Name;
            bool addMasterBff = FastBuildSettings.IncludeBFFInProjects && FastBuild.UtilityMethods.HasFastBuildConfig(configurations);

            Solution.Configuration[] sortedConfigurations;
            if (solution.MergePlatformConfiguration)
                sortedConfigurations = configurations.OrderBy(conf => conf.Name).ToArray();
            else
                sortedConfigurations = configurations.OrderBy(conf => $"{conf.Name}|{conf.PlatformName}").ToArray();

            bool updated;
            string solutionFileResult = Generate(solution, sortedConfigurations, solutionPath, solutionFileName, addMasterBff, out updated);
            if (updated)
                generatedFiles.Add(solutionFileResult);
            else
                skipFiles.Add(solutionFileResult);

            _builder = null;
        }

        // slnx folder name form: "/A/B/" with forward slashes and leading + trailing slash.
        private static string ToSlnxFolderName(SolutionFolder folder)
        {
            string path = folder.Path.Replace('\\', '/').Replace(System.IO.Path.DirectorySeparatorChar, '/');
            return "/" + path.Trim('/') + "/";
        }

        private string Generate(
            Solution solution,
            IReadOnlyList<Solution.Configuration> solutionConfigurations,
            string solutionPath,
            string solutionFile,
            bool addMasterBff,
            out bool updated)
        {
            _folderTree.Clear();

            FileInfo solutionFileInfo = new FileInfo(Util.GetCapitalizedPath(solutionPath + Path.DirectorySeparatorChar + solutionFile + SolutionExtension));

            DevEnv devEnv = solutionConfigurations[0].Target.GetFragment<DevEnv>();
            ValidateDevEnv(solution.Name, solution.Format, devEnv);

            List<Solution.ResolvedProject> solutionProjects = SolutionProjectResolver.ResolveSolutionProjects(solution, solutionConfigurations, _folderTree, out Solution.ResolvedProject startupProject);

            if (solutionProjects.Count == 0)
            {
                updated = solutionFileInfo.Exists;
                if (updated)
                    Util.TryDeleteFile(solutionFileInfo.FullName);
                return solutionFileInfo.FullName;
            }

            List<Solution.ResolvedProject> resolvedPathReferences = SolutionProjectResolver.ResolveReferencesByPath(solutionProjects, solutionConfigurations[0].ProjectReferencesByPath, _folderTree, solutionConfigurations[0].ProjectReferencesByPathFolders);
            var guidlist = solutionProjects.Select(p => p.UserData["Guid"]);
            resolvedPathReferences = resolvedPathReferences.Where(r => !guidlist.Contains(r.UserData["Guid"])).ToList();

            // Master-bff solution folder (FastBuild).
            SolutionFolder masterBffFolder = null;
            if (addMasterBff)
            {
                masterBffFolder = _folderTree.GetSolutionFolder(solution.FastBuildMasterBffSolutionFolder);
                if (masterBffFolder == null)
                    throw new Error("FastBuildMasterBffSolutionFolder needs to be set in solution " + solutionFile);
            }

            // FastBuildAll dependency target (optional).
            Solution.ResolvedProject fastBuildAllProjectForSolutionDependency = null;
            if (solution.FastBuildAllSlnDependencyFromExe)
            {
                var fastBuildAllProjects = solutionProjects.Where(p => p.Project.IsFastBuildAll).ToArray();
                if (fastBuildAllProjects.Length > 1)
                    throw new Error("More than one FastBuildAll project");
                if (fastBuildAllProjects.Length == 1)
                    fastBuildAllProjectForSolutionDependency = fastBuildAllProjects[0];
            }

            // Build folder -> files map (master-bff bff items + ExtraItems).
            var folderFiles = new Dictionary<SolutionFolder, SortedSet<string>>();
            if (masterBffFolder != null)
            {
                var bffFilesPaths = new SortedSet<string>(new FileSystemStringComparer());
                foreach (var conf in solutionConfigurations)
                {
                    string masterBffFilePath = conf.MasterBffFilePath + FastBuildSettings.FastBuildConfigFileExtension;
                    bffFilesPaths.Add(Util.PathGetRelative(solutionPath, masterBffFilePath));
                    bffFilesPaths.Add(Util.PathGetRelative(solutionPath, FastBuild.MasterBff.GetGlobalBffConfigFileName(masterBffFilePath)));
                }
                bffFilesPaths.Add(solutionFile + FastBuildSettings.FastBuildConfigFileExtension);
                folderFiles[masterBffFolder] = bffFilesPaths;
            }
            foreach (var items in solution.ExtraItems)
            {
                var folder = _folderTree.GetSolutionFolder(items.Key);
                if (!folderFiles.TryGetValue(folder, out var set))
                {
                    set = new SortedSet<string>(new FileSystemStringComparer());
                    folderFiles[folder] = set;
                }
                foreach (string file in items.Value)
                    set.Add(Util.PathGetRelative(solutionPath, file));
            }

            // Distinct project list (projects + path references), folder grouping.
            var allProjects = solutionProjects.Concat(resolvedPathReferences).Distinct(new Solution.ResolvedProjectGuidComparer()).ToList();
            var projectsByFolder = new Dictionary<SolutionFolder, List<Solution.ResolvedProject>>();
            var rootProjects = new List<Solution.ResolvedProject>();
            foreach (var rp in allProjects)
            {
                var folder = rp.UserData["Folder"] as SolutionFolder;
                if (folder == null)
                {
                    rootProjects.Add(rp);
                }
                else
                {
                    if (!projectsByFolder.TryGetValue(folder, out var list))
                    {
                        list = new List<Solution.ResolvedProject>();
                        projectsByFolder[folder] = list;
                    }
                    list.Add(rp);
                }
            }

            // Precompute, per resolved project, the slnx config rules and build dependencies.
            var projectRules = ComputeProjectRules(solution, solutionConfigurations, solutionProjects, fastBuildAllProjectForSolutionDependency, solutionFileInfo);

            // Ensure folders are always emitted in a stable order.
            _folderTree.SortStably();

            var fileGenerator = new FileGenerator();
            fileGenerator.Write(Template.SolutionBegin);

            // <Configurations>: distinct build types + platforms from the solution configs.
            WriteConfigurations(fileGenerator, solution, solutionConfigurations);

            // Folders (each with its files and nested projects).
            foreach (SolutionFolder folder in _folderTree.AllFolders)
            {
                using (fileGenerator.Declare("folderName", SecurityElement.Escape(ToSlnxFolderName(folder))))
                    fileGenerator.Write(Template.FolderBegin);

                if (folderFiles.TryGetValue(folder, out var files))
                {
                    foreach (var path in files)
                    {
                        using (fileGenerator.Declare("filePath", SecurityElement.Escape(path)))
                            fileGenerator.Write(Template.FolderFile);
                    }
                }

                if (projectsByFolder.TryGetValue(folder, out var folderProjects))
                {
                    foreach (var rp in folderProjects.OrderBy(p => p.ProjectName, StringComparer.InvariantCultureIgnoreCase))
                        WriteProject(fileGenerator, rp, projectRules, solutionFileInfo, indentInFolder: true, isStartup: rp == startupProject);
                }

                fileGenerator.Write(Template.FolderEnd);
            }

            // Root projects (no folder).
            foreach (var rp in rootProjects.OrderBy(p => p.ProjectName, StringComparer.InvariantCultureIgnoreCase))
                WriteProject(fileGenerator, rp, projectRules, solutionFileInfo, indentInFolder: false, isStartup: rp == startupProject);

            fileGenerator.Write(Template.SolutionEnd);

            updated = _builder.Context.WriteGeneratedFile(solution.GetType(), solutionFileInfo, fileGenerator);
            solution.PostGenerationCallback?.Invoke(solutionPath, solutionFile, SolutionExtension);

            return solutionFileInfo.FullName;
        }

        private void WriteConfigurations(FileGenerator fileGenerator, Solution solution, IReadOnlyList<Solution.Configuration> solutionConfigurations)
        {
            // Build the (buildType, platform) names exactly as the solution config names are computed.
            var buildTypes = new List<string>();
            var platforms = new List<string>();
            foreach (var sc in solutionConfigurations)
            {
                SolutionConfigMatcher.GetNameAndCategory(solution, sc, out string buildTypeName, out string platformName);
                if (!buildTypes.Contains(buildTypeName))
                    buildTypes.Add(buildTypeName);
                if (!platforms.Contains(platformName))
                    platforms.Add(platformName);
            }
            buildTypes.Sort(StringComparer.InvariantCultureIgnoreCase);
            platforms.Sort(StringComparer.InvariantCultureIgnoreCase);

            fileGenerator.Write(Template.ConfigurationsBegin);
            foreach (var bt in buildTypes)
            {
                using (fileGenerator.Declare("buildTypeName", SecurityElement.Escape(bt)))
                    fileGenerator.Write(Template.ConfigurationBuildType);
            }
            foreach (var pf in platforms)
            {
                using (fileGenerator.Declare("platformName", SecurityElement.Escape(pf)))
                    fileGenerator.Write(Template.ConfigurationPlatform);
            }
            fileGenerator.Write(Template.ConfigurationsEnd);
        }

        // A per-(project x solution-config) rule, resolved lazily against the real indent once
        // the project's folder nesting is known (see WriteProject).
        private readonly struct ProjectRule
        {
            public readonly string Template;
            public readonly (string token, string value)[] Values;

            public ProjectRule(string template, params (string token, string value)[] values)
            {
                Template = template;
                Values = values;
            }
        }

        // Per (project x solution-config) computed rules, plus build-dependency paths.
        private class ResolvedRules
        {
            public readonly List<ProjectRule> Rules = new List<ProjectRule>();
            public readonly List<string> DependencyPaths = new List<string>();
        }

        private Dictionary<string, ResolvedRules> ComputeProjectRules(
            Solution solution,
            IReadOnlyList<Solution.Configuration> solutionConfigurations,
            List<Solution.ResolvedProject> solutionProjects,
            Solution.ResolvedProject fastBuildAllProjectForSolutionDependency,
            FileInfo solutionFileInfo)
        {
            var result = new Dictionary<string, ResolvedRules>();

            bool containsMultiDotNetFramework = solutionConfigurations.All(sc => sc.Target.HaveFragment<DotNetFramework>()) &&
                                                solutionConfigurations.Select(sc => sc.Target.GetFragment<DotNetFramework>()).Distinct().Count() > 1;
            var multiDotNetFrameworkConfigurationNames = new HashSet<string>();

            foreach (Solution.ResolvedProject solutionProject in solutionProjects)
            {
                string guid = solutionProject.UserData["Guid"] as string;
                var rules = new ResolvedRules();
                result[guid] = rules;

                if (containsMultiDotNetFramework)
                    multiDotNetFrameworkConfigurationNames.Clear();

                foreach (Solution.Configuration solutionConfiguration in solutionConfigurations)
                {
                    // NOTE: this reuses the exact same project<->solution-config matching and
                    // build/deploy computation as Sln.cs (via SolutionConfigMatcher), but Sln.cs's
                    // per-solution-config FastBuild diagnostic (counting buildable FastBuild
                    // projects and erroring when a config has zero, or more than one with
                    // GenerateFastBuildAllProject) is intentionally NOT reproduced here. It is
                    // diagnostic-only and does not affect which project config maps to a solution
                    // config. This is a known, documented divergence from the legacy .sln path; see
                    // the "Known limitations" section of docs/superpowers/plans/2026-06-01-slnx-vs2026.md.
                    SolutionProjectConfigMatch match = SolutionConfigMatcher.Match(solution, solutionConfiguration, solutionProject);
                    Solution.Configuration.IncludedProjectInfo includedProject = match.IncludedProject;
                    Project.Configuration projectConf = match.ProjectConf;
                    Platform projectPlatform = match.ProjectTarget.GetPlatform();

                    SolutionConfigMatcher.GetNameAndCategory(solution, solutionConfiguration, out string configurationName, out string category);

                    if (containsMultiDotNetFramework && includedProject?.Project is CSharpProject)
                    {
                        if (multiDotNetFrameworkConfigurationNames.Contains(configurationName))
                            continue;
                        multiDotNetFrameworkConfigurationNames.Add(configurationName);
                    }

                    string solutionConfigScope = configurationName + "|" + category;
                    string projectPlatformString = Util.GetToolchainPlatformString(projectPlatform, solutionProject.Project, solutionConfiguration.Target, true);
                    string projectBuildType = projectConf.Name;

                    // Emit BuildType / Platform remap rules when the project's resolved config name
                    // or platform differs from the solution config (ActiveCfg semantics).
                    // Comparisons use the raw (unescaped) values; only the values written into the XML
                    // attributes are escaped via SecurityElement.Escape (the '|' scope separator is
                    // structural and is left intact by Escape).
                    string escapedSolutionConfigScope = SecurityElement.Escape(solutionConfigScope);
                    if (!string.Equals(projectBuildType, configurationName, StringComparison.Ordinal))
                    {
                        rules.Rules.Add(new ProjectRule(Template.RuleBuildType, ("solutionConfig", escapedSolutionConfigScope), ("projectBuildType", SecurityElement.Escape(projectBuildType))));
                    }
                    if (!string.Equals(projectPlatformString, category, StringComparison.Ordinal))
                    {
                        rules.Rules.Add(new ProjectRule(Template.RulePlatform, ("solutionConfig", escapedSolutionConfigScope), ("projectPlatform", SecurityElement.Escape(projectPlatformString))));
                    }

                    // Default in slnx is "built". Emit a disable rule only when NOT built.
                    if (!match.Build)
                    {
                        rules.Rules.Add(new ProjectRule(Template.RuleBuildDisabled, ("solutionConfig", escapedSolutionConfigScope)));
                    }

                    if (match.ForceDeploy || match.BuildDeploy)
                    {
                        rules.Rules.Add(new ProjectRule(Template.RuleDeploy, ("solutionConfig", escapedSolutionConfigScope)));
                    }
                }

                // Build dependencies (non-FastBuild) -> referenced by project path.
                var depPaths = new SortedSet<string>(new FileSystemStringComparer());
                foreach (var depConf in solutionProject.Configurations.SelectMany(c => c.GenericBuildDependencies).Where(dep => !dep.IsFastBuild))
                {
                    string depFullPath = depConf.ProjectFullFileNameWithExtension;
                    depPaths.Add(Util.PathGetRelative(solutionFileInfo.Directory.FullName, depFullPath));
                }
                if (fastBuildAllProjectForSolutionDependency != null)
                {
                    bool writeDependencyToFastBuildAll = solutionProject.Configurations.Any(conf => conf.IsFastBuild && conf.Output == Project.Configuration.OutputType.Exe) ||
                                                         solution.ProjectsDependingOnFastBuildAllForThisSolution.Contains(solutionProject.Project);
                    if (writeDependencyToFastBuildAll)
                    {
                        var faInfo = new FileInfo(fastBuildAllProjectForSolutionDependency.ProjectFile);
                        depPaths.Add(Util.PathGetRelative(solutionFileInfo.Directory.FullName, faInfo.FullName));
                    }
                }
                rules.DependencyPaths.AddRange(depPaths);
            }

            return result;
        }

        // Declares [token]=value for each pair (plus [indent]) and writes the resolved template,
        // disposing the declarations via nested `using` regardless of exceptions.
        private static void WriteRule(FileGenerator fileGenerator, string template, string indent, (string token, string value)[] values, int index = 0)
        {
            if (index == values.Length)
            {
                using (fileGenerator.Declare("indent", indent))
                    fileGenerator.Write(template);
                return;
            }

            using (fileGenerator.Declare(values[index].token, values[index].value))
                WriteRule(fileGenerator, template, indent, values, index + 1);
        }

        private void WriteProject(
            FileGenerator fileGenerator,
            Solution.ResolvedProject resolvedProject,
            Dictionary<string, ResolvedRules> projectRules,
            FileInfo solutionFileInfo,
            bool indentInFolder,
            bool isStartup)
        {
            FileInfo projectFileInfo = new FileInfo(resolvedProject.ProjectFile);
            string relPath = Util.PathGetRelative(solutionFileInfo.Directory.FullName, projectFileInfo.FullName);
            string guid = resolvedProject.UserData["Guid"] as string;

            projectRules.TryGetValue(guid, out var rules);
            bool hasBody = rules != null && (rules.Rules.Count > 0 || rules.DependencyPaths.Count > 0);

            string indent = indentInFolder ? "  " : "";
            string defaultStartup = isStartup ? Template.DefaultStartupAttribute : "";

            if (!hasBody)
            {
                using (fileGenerator.Declare("projectPath", SecurityElement.Escape(relPath)))
                using (fileGenerator.Declare("defaultStartup", defaultStartup))
                    fileGenerator.Write(indentInFolder ? Template.FolderProjectSelfClosing : Template.ProjectSelfClosing);
                return;
            }

            using (fileGenerator.Declare("projectPath", SecurityElement.Escape(relPath)))
            using (fileGenerator.Declare("defaultStartup", defaultStartup))
                fileGenerator.Write(indentInFolder ? Template.FolderProjectBegin : Template.ProjectBegin);

            foreach (var rule in rules.Rules)
                WriteRule(fileGenerator, rule.Template, indent, rule.Values);

            foreach (var depPath in rules.DependencyPaths)
            {
                using (fileGenerator.Declare("indent", indent))
                using (fileGenerator.Declare("dependencyProjectPath", SecurityElement.Escape(depPath)))
                    fileGenerator.Write(Template.BuildDependency);
            }

            fileGenerator.Write(indentInFolder ? Template.FolderProjectEnd : Template.ProjectEnd);
        }
    }
}
