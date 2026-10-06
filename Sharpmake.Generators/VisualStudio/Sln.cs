// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sharpmake.Generators.VisualStudio
{
    // Based on http://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
    public static class ProjectTypeGuids
    {
        public static Guid WindowsCSharp = new Guid("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
        public static Guid WindowsVB = new Guid("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}");
        public static Guid WindowsVisualCpp = new Guid("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}");
        public static Guid WebApplication = new Guid("{349C5851-65DF-11DA-9384-00065B846F21}");
        public static Guid WebSite = new Guid("{E24C65DC-7377-472B-9ABA-BC803B73C61A}");
        public static Guid DistributedSystem = new Guid("{F135691A-BF7E-435D-8960-F99683D2D49C}");
        public static Guid WindowsCommunicationFoundation = new Guid("{3D9AD99F-2412-4246-B90B-4EAA41C64699}");
        public static Guid WindowsPresentationFoundation = new Guid("{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}");
        public static Guid VisualDatabaseTools = new Guid("{C252FEB5-A946-4202-B1D4-9916A0590387}");
        public static Guid Database = new Guid("{A9ACE9BB-CECE-4E62-9AA4-C7E7C5BD2124}");
        public static Guid DatabaseOtherProjectTypes = new Guid("{4F174C21-8C12-11D0-8340-0000F80270F8}");
        public static Guid Test = new Guid("{3AC096D0-A1C2-E12C-1390-A8335801FDAB}");
        public static Guid Legacy2003SmartDeviceCSharp = new Guid("{20D4826A-C6FA-45DB-90F4-C717570B9F32}");
        public static Guid Legacy2003SmartDeviceVB = new Guid("{CB4CE8C6-1BDB-4DC7-A4D3-65A1999772F8}");
        public static Guid SmartDeviceCSharp = new Guid("{4D628B5B-2FBC-4AA6-8C16-197242AEB884}");
        public static Guid SmartDeviceVB = new Guid("{68B1623D-7FB9-47D8-8664-7ECEA3297D4F}");
        public static Guid WorkflowCSharp = new Guid("{14822709-B5A1-4724-98CA-57A101D1B079}");
        public static Guid WorkflowVB = new Guid("{D59BE175-2ED0-4C54-BE3D-CDAA9F3214C8}");
        public static Guid DeploymentMergeModule = new Guid("{06A35CCD-C46D-44D5-987B-CF40FF872267}");
        public static Guid DeploymentCab = new Guid("{3EA9E505-35AC-4774-B492-AD1749C4943A}");
        public static Guid DeploymentSetup = new Guid("{978C614F-708E-4E1A-B201-565925725DBA}");
        public static Guid DeploymentSmartDeviceCab = new Guid("{AB322303-2255-48EF-A496-5904EB18DA55}");
        public static Guid VisualStudioToolsForApplications = new Guid("{A860303F-1F3F-4691-B57E-529FC101A107}");
        public static Guid VisualStudioToolsForOffice = new Guid("{BAA0C2D2-18E2-41B9-852F-F413020CAA33}");
        public static Guid SharePointWorkflow = new Guid("{F8810EC1-6754-47FC-A15F-DFABD2E3FA90}");
        public static Guid XNAWindows = new Guid("{6D335F3A-9D43-41b4-9D22-F6F17C4BE596}");
        public static Guid XNAXBox = new Guid("{2DF5C3F4-5A5F-47a9-8E94-23B4456F55E2}");
        public static Guid XNAZune = new Guid("{D399B71A-8929-442a-A9AC-8BEC78BB2433}");
        public static Guid SharePointVB = new Guid("{EC05E597-79D4-47f3-ADA0-324C4F7C7484}");
        public static Guid SharePointCSharp = new Guid("{593B0543-81F6-4436-BA1E-4747859CAAE2}");
        public static Guid Silverlight = new Guid("{A1591282-1198-4647-A2B1-27E5FF5F6F3B}");
        public static Guid Extensibility = new Guid("{82B43B9B-A64C-4715-B499-D71E9CA2BD60}");
        public static Guid Python = new Guid("{888888A0-9F3D-457C-B088-3A5042F75D52}");
        public static Guid AspNet5 = new Guid("{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}");
        public static Guid AspNetMvc1 = new Guid("{603C0E0B-DB56-11DC-BE95-000D561079B0}");
        public static Guid AspNetMvc2 = new Guid("{F85E285D-A4E0-4152-9332-AB1D724D3325}");
        public static Guid AspNetMvc3 = new Guid("{E53F8FEA-EAE0-44A6-8774-FFD645390401}");
        public static Guid AspNetMvc4 = new Guid("{E3E379DF-F4C6-4180-9B81-6769533ABE47}");
        public static Guid AspNetMvc5 = new Guid("{349C5851-65DF-11DA-9384-00065B846F21}");
        public static Guid Android = new Guid("{EAAC564B-F271-4B9C-99B6-F18BE0B11958}");

        public static string ToOption(Guid[] projectTypes)
        {
            return string.Join(";", projectTypes.Select(g => g.ToString("B").ToUpper()));
        }

        // Combined project type
        public static Guid[] CSharpTestProject = { Test, WindowsCSharp };
        public static Guid[] VsixProject = { Extensibility, WindowsPresentationFoundation, WindowsCSharp };
        public static Guid[] VstoProject = { VisualStudioToolsForOffice, WindowsCSharp };
        public static Guid[] WpfProject = { WindowsPresentationFoundation, WindowsCSharp };
        public static Guid[] WcfProject = { WindowsCommunicationFoundation, WindowsCSharp };
        public static Guid[] AspNetMvc5Project = { AspNetMvc5, WindowsCSharp };
    }

    public partial class Sln : ISolutionGenerator
    {
        public const string SolutionExtension = ".sln";

        private readonly SolutionFolderTree _folderTree = new SolutionFolderTree();
        private Builder _builder;

        private static Regex s_projectGuidRegex = new Regex(
            "(\\s*ProjectGUID=\"\\s*{(?<GUID>([0-9A-Fa-f\\-]+))}\\s*\")|" +
            "(\\s*<ProjectGuid>\\s*{(?<GUID>([0-9A-Fa-f\\-]+))}</ProjectGuid>)",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);


        public void Generate(
            Builder builder,
            Solution solution,
            List<Solution.Configuration> configurations,
            string solutionFile,
            List<string> generatedFiles,
            List<string> skipFiles
        )
        {
            _builder = builder;

            FileInfo fileInfo = new FileInfo(solutionFile);
            string solutionPath = fileInfo.Directory.FullName;
            string solutionFileName = fileInfo.Name;
            bool addMasterBff = FastBuildSettings.IncludeBFFInProjects && FastBuild.UtilityMethods.HasFastBuildConfig(configurations);

            // sort configurations because that's the way they are sorted in a solution
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

        // TODO: We should keep the GUIDS generated by sharpmake to avoid reading vcxproj files!
        private static readonly ConcurrentDictionary<string, Lazy<string>> s_projectGUIDS = new ConcurrentDictionary<string, Lazy<string>>();

        public static string ReadGuidFromProjectFile(string projectFile)
        {
            string key = Path.GetFullPath(projectFile).ToLower();
            Lazy<string> guid = s_projectGUIDS.GetOrAdd(key, new Lazy<string>(() =>
            {
                if (!File.Exists(key))
                {
                    throw new InvalidOperationException($"Error when reading GUID from project. {projectFile} does not exist");
                }

                var projectFileInfo = new FileInfo(projectFile);
                using (StreamReader projectFileStream = projectFileInfo.OpenText())
                {
                    string line = projectFileStream.ReadLine();
                    while (line != null)
                    {
                        Match match = s_projectGuidRegex.Match(line);
                        if (match.Success)
                        {
                            return match.Groups["GUID"].ToString().ToUpper();
                        }
                        line = projectFileStream.ReadLine();
                    }
                }

                return null;
            }));

            return guid.Value;
        }

        // SDK-style csproj files omit <ProjectGuid>. Generate a stable GUID from the
        // normalised file path so that solution references remain consistent across runs.
        internal static string GenerateDeterministicGuid(string projectFile)
        {
            string normalised = Path.GetFullPath(projectFile).ToLowerInvariant();
            return Util.BuildGuid(normalised).ToString("D").ToUpperInvariant();
        }

        internal static string ReadOrGenerateGuidFromProjectFile(string projectFile)
        {
            return ReadGuidFromProjectFile(projectFile) ?? GenerateDeterministicGuid(projectFile);
        }

        private static readonly ConcurrentDictionary<string, string> s_projectTypeGUIDS = new ConcurrentDictionary<string, string>();

        // ReadTypeGuidFromProjectFile is currently just looking at the file extension to associate a type.
        public static string ReadTypeGuidFromProjectFile(string projectFile)
        {
            string filenameLC = Path.GetFullPath(projectFile).ToLower();
            string guid;
            if (s_projectTypeGUIDS.TryGetValue(filenameLC, out guid))
                return guid;

            string fileExt = Path.GetExtension(filenameLC);
            switch (fileExt)
            {
                case Vcxproj.ProjectExtension:
                    guid = ProjectTypeGuids.WindowsVisualCpp.ToString().ToUpper();
                    break;
                case CSproj.ProjectExtension:
                    guid = ProjectTypeGuids.WindowsCSharp.ToString().ToUpper();
                    break;
                case Pyproj.ProjectExtension:
                    guid = ProjectTypeGuids.Python.ToString().ToUpper();
                    break;
                case Androidproj.ProjectExtension:
                    guid = ProjectTypeGuids.Android.ToString().ToUpper();
                    break;
                default:
                    throw new Error("Unknown file extension {0} : unable to detect file type GUID [{1}]", fileExt, projectFile);
            }

            if (!string.IsNullOrEmpty(guid))
            {
                s_projectTypeGUIDS.TryAdd(filenameLC, guid);
            }
            return guid;
        }

        private string Generate(
            Solution solution,
            IReadOnlyList<Solution.Configuration> solutionConfigurations,
            string solutionPath,
            string solutionFile,
            bool addMasterBff,
            out bool updated
        )
        {
            // reset current solution state
            _folderTree.Clear();

            FileInfo solutionFileInfo = new FileInfo(Util.GetCapitalizedPath(solutionPath + Path.DirectorySeparatorChar + solutionFile + SolutionExtension));

            string solutionGuid = Util.BuildGuid(solutionFileInfo.FullName, solution.SharpmakeCsPath);

            DevEnv devEnv = solutionConfigurations[0].Target.GetFragment<DevEnv>();
            List<Solution.ResolvedProject> solutionProjects = SolutionProjectResolver.ResolveSolutionProjects(solution, solutionConfigurations, _folderTree, out _);

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

            var fileGenerator = new FileGenerator();

            // write solution header
            switch (devEnv)
            {
                case DevEnv.vs2015:
                    fileGenerator.Write(Template.Solution.HeaderBeginVs2015);
                    break;
                case DevEnv.vs2017:
                    fileGenerator.Write(Template.Solution.HeaderBeginVs2017);
                    break;
                case DevEnv.vs2019:
                    fileGenerator.Write(Template.Solution.HeaderBeginVs2019);
                    break;
                case DevEnv.vs2022:
                    fileGenerator.Write(Template.Solution.HeaderBeginVs2022);
                    break;
                case DevEnv.vs2026:
                    fileGenerator.Write(Template.Solution.HeaderBeginVs2026);
                    break;

                default:
                    throw new Error($"Unsupported DevEnv {devEnv} for solution {solution.Name}");
            }

            SolutionFolder masterBffFolder = null;
            if (addMasterBff)
            {
                masterBffFolder = _folderTree.GetSolutionFolder(solution.FastBuildMasterBffSolutionFolder);
                if (masterBffFolder == null)
                    throw new Error("FastBuildMasterBffSolutionFolder needs to be set in solution " + solutionFile);
            }

            // Write all needed folders before the projects to make sure the proper startup project is selected.

            // Ensure folders are always in the same order to avoid random shuffles
            _folderTree.SortStably();

            foreach (SolutionFolder folder in _folderTree.AllFolders)
            {
                using (fileGenerator.Declare("folderName", folder.Name))
                using (fileGenerator.Declare("folderGuid", folder.Guid.ToString().ToUpper()))
                {
                    fileGenerator.Write(Template.Solution.ProjectFolder);
                    if (masterBffFolder == folder)
                    {
                        var bffFilesPaths = new SortedSet<string>(new FileSystemStringComparer());

                        foreach (var conf in solutionConfigurations)
                        {
                            string masterBffFilePath = conf.MasterBffFilePath + FastBuildSettings.FastBuildConfigFileExtension;
                            bffFilesPaths.Add(Util.PathGetRelative(solutionPath, masterBffFilePath));
                            bffFilesPaths.Add(Util.PathGetRelative(solutionPath, FastBuild.MasterBff.GetGlobalBffConfigFileName(masterBffFilePath)));
                        }

                        // This always needs to be created so make sure it's there.
                        bffFilesPaths.Add(solutionFile + FastBuildSettings.FastBuildConfigFileExtension);

                        fileGenerator.Write(Template.Solution.SolutionItemBegin);
                        {
                            foreach (var path in bffFilesPaths)
                            {
                                using (fileGenerator.Declare("solutionItemPath", path))
                                    fileGenerator.Write(Template.Solution.SolutionItem);
                            }
                        }
                        fileGenerator.Write(Template.Solution.ProjectSectionEnd);
                    }
                    fileGenerator.Write(Template.Solution.ProjectEnd);
                }
            }

            Solution.ResolvedProject fastBuildAllProjectForSolutionDependency = null;
            if (solution.FastBuildAllSlnDependencyFromExe)
            {
                var fastBuildAllProjects = solutionProjects.Where(p => p.Project.IsFastBuildAll).ToArray();
                if (fastBuildAllProjects.Length > 1)
                    throw new Error("More than one FastBuildAll project");
                if (fastBuildAllProjects.Length == 1)
                    fastBuildAllProjectForSolutionDependency = fastBuildAllProjects[0];
            }

            using (fileGenerator.Declare("solution", solution))
            using (fileGenerator.Declare("solutionGuid", solutionGuid))
            {
                foreach (Solution.ResolvedProject resolvedProject in solutionProjects.Concat(resolvedPathReferences).Distinct(new Solution.ResolvedProjectGuidComparer()))
                {
                    FileInfo projectFileInfo = new FileInfo(resolvedProject.ProjectFile);
                    using (fileGenerator.Declare("project", resolvedProject.Project))
                    using (fileGenerator.Declare("projectName", resolvedProject.ProjectName))
                    using (fileGenerator.Declare("projectFile", Util.PathGetRelative(solutionFileInfo.Directory.FullName, projectFileInfo.FullName)))
                    using (fileGenerator.Declare("projectGuid", resolvedProject.UserData["Guid"]))
                    using (fileGenerator.Declare("projectTypeGuid", resolvedProject.UserData["TypeGuid"]))
                    {
                        fileGenerator.Write(Template.Solution.ProjectBegin);
                        Strings buildDepsGuids = new Strings(resolvedProject.Configurations.SelectMany(
                            c => c.GenericBuildDependencies
                                .Where(dep => !dep.IsFastBuild)
                                .Select(p => p.ProjectGuid ?? ReadGuidFromProjectFile(p.ProjectFullFileNameWithExtension)
                            )
                        ));

                        if (fastBuildAllProjectForSolutionDependency != null)
                        {
                            bool writeDependencyToFastBuildAll = (resolvedProject.Configurations.Any(conf => conf.IsFastBuild && conf.Output == Project.Configuration.OutputType.Exe)) ||
                                                                 solution.ProjectsDependingOnFastBuildAllForThisSolution.Contains(resolvedProject.Project);

                            if (writeDependencyToFastBuildAll)
                                buildDepsGuids.Add(fastBuildAllProjectForSolutionDependency.UserData["Guid"] as string);
                        }

                        if (buildDepsGuids.Any())
                        {
                            fileGenerator.Write(Template.Solution.ProjectDependencyBegin);
                            foreach (string guid in buildDepsGuids.SortedValues)
                            {
                                using (fileGenerator.Declare("projectDependencyGuid", guid))
                                    fileGenerator.Write(Template.Solution.ProjectDependency);
                            }
                            fileGenerator.Write(Template.Solution.ProjectSectionEnd);
                        }
                        fileGenerator.Write(Template.Solution.ProjectEnd);
                    }
                }
            }

            // Write extra solution items
            // TODO: What happens if we define an existing folder?
            foreach (var items in solution.ExtraItems)
            {
                var folder = _folderTree.GetSolutionFolder(items.Key);

                using (fileGenerator.Declare("folderName", folder.Name))
                using (fileGenerator.Declare("folderGuid", folder.Guid))
                using (fileGenerator.Declare("solution", solution))
                {
                    fileGenerator.Write(Template.Solution.ProjectFolder);
                    {
                        fileGenerator.Write(Template.Solution.SolutionItemBegin);
                        foreach (string file in items.Value)
                        {
                            using (fileGenerator.Declare("solutionItemPath", Util.PathGetRelative(solutionPath, file)))
                                fileGenerator.Write(Template.Solution.SolutionItem);
                        }
                        fileGenerator.Write(Template.Solution.ProjectSectionEnd);
                    }
                    fileGenerator.Write(Template.Solution.ProjectEnd);
                }
            }

            fileGenerator.Write(Template.Solution.GlobalBegin);

            // write solution configurations
            string visualStudioExe = GetVisualStudioIdePath(devEnv) + Util.WindowsSeparator + "devenv.com";

            var configurationSectionNames = new List<string>();

            bool containsMultiDotNetFramework = solutionConfigurations.All(sc => sc.Target.HaveFragment<DotNetFramework>()) &&
                                                solutionConfigurations.Select(sc => sc.Target.GetFragment<DotNetFramework>()).Distinct().Count() > 1;

            var multiDotNetFrameworkConfigurationNames = new HashSet<string>();

            fileGenerator.Write(Template.Solution.GlobalSectionSolutionConfigurationBegin);
            foreach (Solution.Configuration solutionConfiguration in solutionConfigurations)
            {
                SolutionConfigMatcher.GetNameAndCategory(solution, solutionConfiguration, out string configurationName, out string category);

                if (containsMultiDotNetFramework)
                {
                    if (multiDotNetFrameworkConfigurationNames.Contains(configurationName))
                        continue;

                    multiDotNetFrameworkConfigurationNames.Add(configurationName);
                }

                using (fileGenerator.Declare("configurationName", configurationName))
                using (fileGenerator.Declare("category", category))
                {
                    configurationSectionNames.Add(fileGenerator.Resolver.Resolve(Template.Solution.GlobalSectionSolutionConfiguration));
                }

                // set the compile command line 
                if (File.Exists(visualStudioExe))
                {
                    solutionConfiguration.CompileCommandLine = string.Format(@"""{0}"" ""{1}"" /build ""{2}|{3}""",
                        visualStudioExe, solutionFileInfo.FullName, configurationName, category);
                }
            }

            configurationSectionNames.Sort();

            VerifySectionNamesDuplicates(solutionFileInfo.FullName, solutionConfigurations, configurationSectionNames);

            foreach (string configurationSectionName in configurationSectionNames)
                fileGenerator.Write(configurationSectionName);

            fileGenerator.Write(Template.Solution.GlobalSectionSolutionConfigurationEnd);

            // write all project target and match then to a solution target
            fileGenerator.Write(Template.Solution.GlobalSectionProjectConfigurationBegin);

            var solutionConfigurationFastBuildBuilt = new Dictionary<Solution.Configuration, List<string>>();
            foreach (Solution.ResolvedProject solutionProject in solutionProjects)
            {
                if (containsMultiDotNetFramework)
                    multiDotNetFrameworkConfigurationNames.Clear();

                foreach (Solution.Configuration solutionConfiguration in solutionConfigurations)
                {
                    SolutionProjectConfigMatch match = SolutionConfigMatcher.Match(solution, solutionConfiguration, solutionProject);
                    Solution.Configuration.IncludedProjectInfo includedProject = match.IncludedProject;
                    Project.Configuration projectConf = match.ProjectConf;

                    if (includedProject != null && includedProject.Configuration.IsFastBuild)
                        solutionConfigurationFastBuildBuilt.GetValueOrAdd(solutionConfiguration, new List<string>());

                    Platform projectPlatform = match.ProjectTarget.GetPlatform();

                    SolutionConfigMatcher.GetNameAndCategory(solution, solutionConfiguration, out string configurationName, out string category);

                    if (containsMultiDotNetFramework && includedProject?.Project is CSharpProject)
                    {
                        if (multiDotNetFrameworkConfigurationNames.Contains(configurationName))
                            continue;

                        multiDotNetFrameworkConfigurationNames.Add(configurationName);
                    }

                    // for fastbuild, record which project actually builds each solution config for the diagnostic below.
                    if (projectConf.IsFastBuild && match.Build)
                        solutionConfigurationFastBuildBuilt[solutionConfiguration].Add(projectConf.Project.Name + " " + projectConf.Name);

                    using (fileGenerator.Declare("solutionConf", solutionConfiguration))
                    using (fileGenerator.Declare("projectGuid", solutionProject.UserData["Guid"]))
                    using (fileGenerator.Declare("projectConf", projectConf))
                    using (fileGenerator.Declare("projectPlatform", Util.GetToolchainPlatformString(projectPlatform, solutionProject.Project, solutionConfiguration.Target, true)))
                    using (fileGenerator.Declare("category", category))
                    using (fileGenerator.Declare("configurationName", configurationName))
                    {
                        fileGenerator.Write(Template.Solution.GlobalSectionProjectConfigurationActive);
                        if (match.Build)
                            fileGenerator.Write(Template.Solution.GlobalSectionProjectConfigurationBuild);

                        if (match.ForceDeploy || match.BuildDeploy)
                        {
                            fileGenerator.Write(Template.Solution.GlobalSectionProjectConfigurationDeploy);
                        }
                    }
                }
            }

            foreach (var fb in solutionConfigurationFastBuildBuilt)
            {
                var solutionConfiguration = fb.Key;
                if (fb.Value.Count == 0)
                    Builder.Instance.LogErrorLine($"{solutionFile} - {solutionConfiguration.Name}|{solutionConfiguration.PlatformName} - has no FastBuild projects to build.");
                else if (solution.GenerateFastBuildAllProject && fb.Value.Count > 1)
                    Builder.Instance.LogErrorLine($"{solutionFile} - {solutionConfiguration.Name}|{solutionConfiguration.PlatformName} - has more than one FastBuild project to build ({string.Join(";", fb.Value)}).");
            }

            fileGenerator.Write(Template.Solution.GlobalSectionProjectConfigurationEnd);

            fileGenerator.Write(Template.Solution.SolutionProperties);

            // Write nested folders

            if (_folderTree.AllFolders.Count != 0)
            {
                fileGenerator.Write(Template.Solution.NestedProjectBegin);

                foreach (SolutionFolder folder in _folderTree.AllFolders)
                {
                    if (folder.Parent != null)
                    {
                        using (fileGenerator.Declare("nestedChildGuid", folder.Guid.ToString().ToUpper()))
                        using (fileGenerator.Declare("nestedParentGuid", folder.Parent.Guid.ToString().ToUpper()))
                        {
                            fileGenerator.Write(Template.Solution.NestedProjectItem);
                        }
                    }
                }

                foreach (Solution.ResolvedProject resolvedProject in solutionProjects.Concat(resolvedPathReferences))
                {
                    SolutionFolder folder = resolvedProject.UserData["Folder"] as SolutionFolder;

                    if (folder != null)
                    {
                        using (fileGenerator.Declare("nestedChildGuid", resolvedProject.UserData["Guid"].ToString().ToUpper()))
                        using (fileGenerator.Declare("nestedParentGuid", folder.Guid.ToString().ToUpper()))
                        {
                            fileGenerator.Write(Template.Solution.NestedProjectItem);
                        }
                    }
                }

                fileGenerator.Write(Template.Solution.NestedProjectEnd);
            }

            fileGenerator.Write(Template.Solution.GlobalEnd);

            // Write the solution file
            updated = _builder.Context.WriteGeneratedFile(solution.GetType(), solutionFileInfo, fileGenerator);

            solution.PostGenerationCallback?.Invoke(solutionPath, solutionFile, SolutionExtension);

            return solutionFileInfo.FullName;
        }

        // Will check that two configurations in the solution do not share the same name
        private void VerifySectionNamesDuplicates(
            string solutionFile,
            IReadOnlyList<Solution.Configuration> solutionConfigurations,
            List<string> configurationSectionNames
        )
        {
            int count = configurationSectionNames.Count;
            var distinctSectionNames = configurationSectionNames.Distinct().ToList();
            if (count != distinctSectionNames.Count)
            {
                throw new Error(
                    "Solution '{0}' contains distinct configurations with the same name, please add something to distinguish them:\n- {1}",
                    solutionFile,
                    string.Join(
                        Environment.NewLine + "- ",
                        solutionConfigurations.Select(
                            sc => $"{sc.Name}|{sc.PlatformName} => '{sc}'"
                        ).OrderBy(name => name)
                    )
                );
            }
        }

        private static string GetVisualStudioIdePath(DevEnv devEnv)
        {
            string commonToolsPath = devEnv.GetCommonToolsPath();
            if (commonToolsPath != null)
            {
                DirectoryInfo path = new DirectoryInfo(Path.Combine(commonToolsPath, "..", "IDE"));
                if (path.Exists)
                    return path.FullName;
            }
            return string.Empty;
        }
    }
}
