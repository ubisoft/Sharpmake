// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharpmake.Generators.VisualStudio
{
    // Per-(project x solution-config) match: which project target/configuration a solution
    // configuration resolves to, and whether it builds/deploys. Shared by the .sln and .slnx
    // generators so the two cannot drift on which config maps to which.
    internal struct SolutionProjectConfigMatch
    {
        public ITarget ProjectTarget;
        public Project.Configuration ProjectConf;
        public bool PerfectMatch;
        public Solution.Configuration.IncludedProjectInfo IncludedProject;
        public bool Build;
        public bool ForceDeploy;
        public bool BuildDeploy;
    }

    internal static class SolutionConfigMatcher
    {
        // Computes the (name, category) pair used both as a solution-config's own identity and
        // as the scope key ("name|category") that per-project rules match against. Both must be
        // computed identically or scoped rules silently fail to match any declared config.
        public static void GetNameAndCategory(Solution solution, Solution.Configuration solutionConfiguration, out string name, out string category)
        {
            if (solution.MergePlatformConfiguration)
            {
                name = solutionConfiguration.PlatformName + "-" + solutionConfiguration.Name;
                category = "All Platforms";
            }
            else
            {
                name = solutionConfiguration.Name;
                category = solutionConfiguration.PlatformName;
            }
        }

        // Will check that two configurations in the solution do not share the same name
        public static void VerifySectionNamesDuplicates(
            string solutionFile,
            IReadOnlyList<Solution.Configuration> solutionConfigurations,
            List<string> configurationSectionNames
        )
        {
            int count = configurationSectionNames.Count;
            var distinctSectionNames = configurationSectionNames.Distinct().ToList();
            if (count != distinctSectionNames.Count)
            {
                string hint = GetDuplicateNamesHint(solutionConfigurations);

                throw new Error(
                    "Solution '{0}' contains distinct configurations with the same name, please add something to distinguish them:\n- {1}{2}",
                    solutionFile,
                    string.Join(
                        Environment.NewLine + "- ",
                        solutionConfigurations.Select(
                            sc => $"{sc.Name}|{sc.PlatformName} => '{sc}'"
                        ).OrderBy(name => name)
                    ),
                    hint.Length == 0 ? "" : Environment.NewLine + hint
                );
            }
        }

        // Records, per solution configuration, which FastBuild projects actually build it. Call
        // TrackFastBuildConfig for every match (before any filtering), RecordFastBuildBuilt for the
        // matches that are written out, then ReportFastBuildDiagnostics once all projects are done.
        public static void TrackFastBuildConfig(Dictionary<Solution.Configuration, List<string>> fastBuildBuilt, Solution.Configuration solutionConfiguration, SolutionProjectConfigMatch match)
        {
            if (match.IncludedProject != null && match.IncludedProject.Configuration.IsFastBuild)
                fastBuildBuilt.GetValueOrAdd(solutionConfiguration, new List<string>());
        }

        public static void RecordFastBuildBuilt(Dictionary<Solution.Configuration, List<string>> fastBuildBuilt, Solution.Configuration solutionConfiguration, SolutionProjectConfigMatch match)
        {
            if (match.ProjectConf.IsFastBuild && match.Build)
                fastBuildBuilt[solutionConfiguration].Add(match.ProjectConf.Project.Name + " " + match.ProjectConf.Name);
        }

        public static void ReportFastBuildDiagnostics(string solutionFile, Solution solution, Dictionary<Solution.Configuration, List<string>> fastBuildBuilt)
        {
            foreach (var fb in fastBuildBuilt)
            {
                var solutionConfiguration = fb.Key;
                if (fb.Value.Count == 0)
                    Builder.Instance.LogErrorLine($"{solutionFile} - {solutionConfiguration.Name}|{solutionConfiguration.PlatformName} - has no FastBuild projects to build.");
                else if (solution.GenerateFastBuildAllProject && fb.Value.Count > 1)
                    Builder.Instance.LogErrorLine($"{solutionFile} - {solutionConfiguration.Name}|{solutionConfiguration.PlatformName} - has more than one FastBuild project to build ({string.Join(";", fb.Value)}).");
            }
        }

        // Explains a "same name" error when it is caused by configurations that only differ by processor architecture
        // or instruction set level, which a custom PlatformName that ignores them cannot tell apart. Empty otherwise.
        internal static string GetDuplicateNamesHint(IEnumerable<Solution.Configuration> solutionConfigurations)
        {
            var duplicates = solutionConfigurations
                .GroupBy(sc => $"{sc.Name}|{sc.PlatformName}")
                .Where(g => g.Count() > 1);

            foreach (var group in duplicates)
            {
                var targets = group.Select(sc => sc.Target).ToList();
                bool differByArchitecture = targets.Select(t => Util.GetProcessorArchitecture(t)).Distinct().Count() > 1 ||
                    targets.Select(t => Util.GetInstructionSetLevel(t)).Distinct().Count() > 1;
                if (differByArchitecture)
                {
                    return $"The configurations of '{group.Key}' differ by processor architecture or instruction set level: " +
                        "a custom PlatformName does not include them, append Util.GetArchitectureDisplaySuffix(target) to it " +
                        "(empty for the defaults, \"APX\" or \"ARM64\" otherwise).";
                }
            }

            return string.Empty;
        }

        // The platform of the project configuration a solution configuration is mapped to: it comes from the matched
        // project target, which can have a different architecture than the solution's when the project lacks the exact one.
        internal static string GetProjectPlatform(Solution.Configuration solutionConfiguration, SolutionProjectConfigMatch match, Solution.ResolvedProject solutionProject)
        {
            return Util.GetToolchainPlatformString(match.ProjectTarget.GetPlatform(), solutionProject.Project, match.ProjectTarget, true);
        }

        private static bool HasExplicitArchitectureOrLevel(ITarget target)
        {
            return (target.TryGetFragment(out ProcessorArchitecture architecture) && architecture != 0) ||
                (target.TryGetFragment(out InstructionSetLevel level) && level != 0);
        }

        private static int[] GetFragmentsToCompare(ITarget target, bool effectiveValues)
        {
            return effectiveValues ? target.GetEffectiveFragmentsValue() : target.GetFragmentsValue();
        }

        public static SolutionProjectConfigMatch Match(Solution solution, Solution.Configuration solutionConfiguration, Solution.ResolvedProject solutionProject)
        {
            ITarget solutionTarget = solutionConfiguration.Target;
            ITarget projectTarget = null;

            Solution.Configuration.IncludedProjectInfo includedProject = solutionConfiguration.GetProject(solutionProject.Project.GetType());

            bool perfectMatch = includedProject != null && solutionProject.Configurations.Contains(includedProject.Configuration);
            if (perfectMatch)
            {
                projectTarget = includedProject.Target;
            }
            else
            {
                // try to find the target in the project that is the closest match from the solution one
                int maxEqualFragments = 0;
                // When architectures or levels are in use, an unset one must equal the explicit default, so
                // the effective values are compared. Otherwise (every target leaves them unset) they are left out
                // of the comparison, as before: counting the shared defaults would change which configuration
                // wins the ties of existing win32/win64 projects.
                bool compareEffectiveValues = HasExplicitArchitectureOrLevel(solutionTarget) ||
                    solutionProject.Configurations.Any(c => HasExplicitArchitectureOrLevel(c.Target));
                int[] solutionTargetValues = GetFragmentsToCompare(solutionTarget, compareEffectiveValues);

                Platform previousPlatform = Platform._reserved1;

                foreach (var conf in solutionProject.Configurations)
                {
                    Platform currentTargetPlatform = conf.Target.GetPlatform();

                    int[] candidateTargetValues = GetFragmentsToCompare(conf.Target, compareEffectiveValues);
                    if (solutionTargetValues.Length != candidateTargetValues.Length)
                        continue;

                    int equalFragments = 0;
                    for (int i = 0; i < solutionTargetValues.Length; ++i)
                    {
                        if ((solutionTargetValues[i] & candidateTargetValues[i]) != 0)
                            equalFragments++;
                    }

                    if ((equalFragments == maxEqualFragments && currentTargetPlatform < previousPlatform) || equalFragments > maxEqualFragments)
                    {
                        projectTarget = conf.Target;
                        maxEqualFragments = equalFragments;
                        previousPlatform = currentTargetPlatform;
                    }
                }

                // last resort: if we didn't find a good enough match, fallback to TargetDefault
                if (projectTarget == null)
                    projectTarget = solutionProject.TargetDefault;
            }

            Project.Configuration projectConf = solutionProject.Project.GetConfiguration(projectTarget);

            bool build = false;
            bool forceDeploy = false;
            if (solution is PythonSolution)
            {
                // nothing is built in python solutions
            }
            else if (perfectMatch)
            {
                build = includedProject.ToBuild == Solution.Configuration.IncludedProjectInfo.Build.Yes;
                forceDeploy = includedProject.Project.DeployProjectType == Project.DeployType.AlwaysDeploy || includedProject.Configuration.DeployProjectType == Project.DeployType.AlwaysDeploy;

                // for fastbuild, only build the projects that cannot be built through dependency chain
                if (!projectConf.IsFastBuild)
                    build |= includedProject.ToBuild == Solution.Configuration.IncludedProjectInfo.Build.YesThroughDependency;
            }

            bool buildDeploy = false;
            if (build)
                buildDeploy = includedProject.Project.DeployProjectType == Project.DeployType.OnlyIfBuild || includedProject.Configuration.DeployProjectType == Project.DeployType.OnlyIfBuild;

            return new SolutionProjectConfigMatch
            {
                ProjectTarget = projectTarget,
                ProjectConf = projectConf,
                PerfectMatch = perfectMatch,
                IncludedProject = includedProject,
                Build = build,
                ForceDeploy = forceDeploy,
                BuildDeploy = buildDeploy,
            };
        }
    }
}
