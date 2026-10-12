// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
                int[] solutionTargetValues = solutionTarget.GetFragmentsValue();

                Platform previousPlatform = Platform._reserved1;

                foreach (var conf in solutionProject.Configurations)
                {
                    Platform currentTargetPlatform = conf.Target.GetPlatform();

                    int[] candidateTargetValues = conf.Target.GetFragmentsValue();
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
