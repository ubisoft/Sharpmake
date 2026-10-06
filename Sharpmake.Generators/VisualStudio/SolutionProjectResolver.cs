// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sharpmake.Generators.VisualStudio
{
    // Project resolution logic shared by the .sln and .slnx generators: which projects belong
    // in the solution, their GUIDs/folders, the startup project, and path-based references.
    internal static class SolutionProjectResolver
    {
        public static IEnumerable<Solution.ResolvedProject> GetResolvedProjectsFromPaths(IEnumerable<string> paths)
        {
            return paths.Select(GetResolvedProjectFromPath);
        }

        public static Solution.ResolvedProject GetResolvedProjectFromPath(string path)
        {
            return new Solution.ResolvedProject
            {
                ProjectFile = path,
                ProjectName = Path.GetFileNameWithoutExtension(path)
            };
        }

        public static Solution.Configuration.IncludedProjectInfo ResolveStartupProject(Solution solution, IReadOnlyList<Solution.Configuration> solutionConfigurations)
        {
            // Set the default startup project.
            var configuration = solutionConfigurations.FirstOrDefault();
            if (configuration == null)
                return null;

            // Find all executable projects
            var executableProjects = solutionConfigurations
                .SelectMany(e => e.IncludedProjectInfos)
                .Where(e =>
                    e.Configuration.Output == Project.Configuration.OutputType.DotNetConsoleApp ||
                    e.Configuration.Output == Project.Configuration.OutputType.DotNetWindowsApp ||
                    e.Configuration.Output == Project.Configuration.OutputType.Exe)
                .GroupBy(e => e.Configuration.ProjectFileName)
                .OrderBy(filename => filename.Key, StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            // If there is more than one, set the one with the same name as the solution
            if (executableProjects.Count > 1)
            {
                var sameName = executableProjects.FirstOrDefault(e => solution.Name.Equals(e.First().Configuration.ProjectName, StringComparison.OrdinalIgnoreCase));
                if (sameName != null)
                    return sameName.First();

                // If none, try to find a project that the name is at the beginning of the solution name
                // (It can happen that a project "Application" is in a solution named "ApplicationSolution")
                sameName = executableProjects.FirstOrDefault(e => solution.Name.StartsWith(e.First().Configuration.ProjectName, StringComparison.OrdinalIgnoreCase));
                if (sameName != null)
                    return sameName.First();
            }

            return executableProjects.FirstOrDefault()?.First();
        }

        public static List<Solution.ResolvedProject> ResolveSolutionProjects(
            Solution solution,
            IReadOnlyList<Solution.Configuration> solutionConfigurations,
            SolutionFolderTree folderTree,
            out Solution.ResolvedProject resolvedStartupProject)
        {
            resolvedStartupProject = null;
            bool projectsWereFiltered;
            var resolvedProjects = solution.GetResolvedProjects(solutionConfigurations, out projectsWereFiltered);

            var filtered = resolvedProjects.Where(sp =>
            {
                var onlyNeeded = sp.SolutionConfigurationsBuild.All(
                    scb => scb.Key.IncludeOnlyNeededFastBuildProjects && (scb.Value == Solution.Configuration.IncludedProjectInfo.Build.No || scb.Value == Solution.Configuration.IncludedProjectInfo.Build.YesThroughDependency)
                );
                if (onlyNeeded)
                {
                    if (!sp.Project.IsFastBuildAll && (sp.Configurations.All(pc => pc.IsFastBuild && !pc.DoNotGenerateFastBuild && !(pc.AddFastBuildProjectToSolutionCallback?.Invoke() ?? false))))
                        return false;
                }
                return true;
            });

            // Ensure all projects are always in the same order to avoid random shuffles
            var solutionProjects = filtered
                .OrderBy(p => p.ProjectName, StringComparer.InvariantCultureIgnoreCase)
                .ThenBy(p => p.ProjectFile, StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            // Validate and handle startup project.
            IEnumerable<Solution.Configuration> confWithStartupProjects = solutionConfigurations.Where(conf => conf.StartupProject != null);
            var startupProjectGroups = confWithStartupProjects.GroupBy(conf => conf.StartupProject.Configuration.ProjectFullFileName).ToArray();
            if (startupProjectGroups.Length > 1)
            {
                throw new Error("Solution {0} contains multiple startup projects; this is not supported. Startup projects: {1}", Path.Combine(solutionConfigurations[0].SolutionPath, solutionConfigurations[0].SolutionFileName), string.Join(", ", startupProjectGroups.Select(group => group.Key)));
            }

            Solution.Configuration.IncludedProjectInfo startupProject = startupProjectGroups.Select(group => group.First().StartupProject).FirstOrDefault();
            if (startupProject == null)
                startupProject = ResolveStartupProject(solution, solutionConfigurations);

            if (startupProject != null)
            {
                // put the startup project at the top of the project list. Visual Studio will put it as the default startup project.
                resolvedStartupProject = solutionProjects.FirstOrDefault(x => x.OriginalProjectFile == startupProject.Configuration.ProjectFullFileName);
                if (resolvedStartupProject != null)
                {
                    solutionProjects.Remove(resolvedStartupProject);
                    solutionProjects.Insert(0, resolvedStartupProject);
                }
            }

            // Read project Guid and append project extension
            foreach (Solution.ResolvedProject resolvedProject in solutionProjects)
            {
                Project.Configuration firstConf = resolvedProject.Configurations.First();
                if (firstConf.ProjectGuid == null)
                {
                    if (firstConf.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Compile)
                        throw new Error("cannot read guid from existing project, project must have Compile attribute: {0}", resolvedProject.ProjectFile);
                    firstConf.ProjectGuid = Sln.ReadGuidFromProjectFile(resolvedProject.ProjectFile);
                }

                resolvedProject.UserData["Guid"] = firstConf.ProjectGuid;
                resolvedProject.UserData["TypeGuid"] = Sln.ReadTypeGuidFromProjectFile(resolvedProject.ProjectFile);
                resolvedProject.UserData["Folder"] = folderTree.GetSolutionFolder(resolvedProject.SolutionFolder);
            }

            return solutionProjects;
        }

        public static List<Solution.ResolvedProject> ResolveReferencesByPath(
            List<Solution.ResolvedProject> solutionProjects,
            Strings referencedProjectPaths,
            SolutionFolderTree folderTree,
            Dictionary<string, string> pathFolders = null)
        {
            // solution's referenced projects
            var resolvedPathReferences = GetResolvedProjectsFromPaths(referencedProjectPaths).ToList();

            foreach (Solution.ResolvedProject resolvedProject in resolvedPathReferences)
            {
                resolvedProject.UserData["Guid"] = Sln.ReadOrGenerateGuidFromProjectFile(resolvedProject.ProjectFile);
                resolvedProject.UserData["TypeGuid"] = Sln.ReadTypeGuidFromProjectFile(resolvedProject.ProjectFile);
                if (pathFolders != null && pathFolders.TryGetValue(resolvedProject.ProjectFile, out string folder))
                    resolvedProject.SolutionFolder = folder;
                resolvedProject.UserData["Folder"] = folderTree.GetSolutionFolder(resolvedProject.SolutionFolder);
            }

            // user's projects references
            var projectRefByPathInfos = solutionProjects
                                            .SelectMany(p => p.Configurations)
                                            .SelectMany(c => c.ProjectReferencesByPath.ProjectsInfos)
                                            .Distinct();

            foreach (var projectRefByPathInfo in projectRefByPathInfos)
            {
                var resolvedProject = GetResolvedProjectFromPath(projectRefByPathInfo.projectFilePath);

                var projectGuid = projectRefByPathInfo.projectGuid;
                if (projectGuid == Guid.Empty)
                    projectGuid = new Guid(Sln.ReadOrGenerateGuidFromProjectFile(projectRefByPathInfo.projectFilePath));
                resolvedProject.UserData["Guid"] = projectGuid.ToString("D").ToUpperInvariant();

                var projectTypeGuid = projectRefByPathInfo.projectTypeGuid;
                if (projectTypeGuid == Guid.Empty)
                    projectTypeGuid = new Guid(Sln.ReadTypeGuidFromProjectFile(projectRefByPathInfo.projectFilePath)); // currently, just use the extension
                resolvedProject.UserData["TypeGuid"] = projectTypeGuid.ToString("D").ToUpperInvariant();

                resolvedProject.UserData["Folder"] = folderTree.GetSolutionFolder(resolvedProject.SolutionFolder);
                resolvedPathReferences.Add(resolvedProject);
            }

            return resolvedPathReferences;
        }
    }
}
