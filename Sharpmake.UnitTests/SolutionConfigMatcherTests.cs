// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using NUnit.Framework;
using Sharpmake.Generators.VisualStudio;
using Sharpmake.UnitTests.SolutionConfigMatcherTestProjects;

namespace Sharpmake.UnitTests
{
    public class SolutionConfigMatcherTests : TestProjectBuilder
    {
        public SolutionConfigMatcherTests()
            : base(InitType.Cpp, typeof(MatcherSolution).Namespace, ConfigureOrder.New)
        {
        }

        [OneTimeSetUp]
        public void BuildOnce()
        {
            Init();
            BuildAndLinkProjects();
            Assert.That(ErrorCount, Is.Zero);
        }

        private static Solution.Configuration GetSolutionConfiguration(Solution solution, Platform platform, Optimization optimization)
        {
            return solution.Configurations.Single(c => c.Target.GetFragment<Platform>() == platform && c.Target.GetFragment<Optimization>() == optimization);
        }

        private static Project.Configuration GetProjectConfiguration(Project project, Platform platform, Optimization optimization)
        {
            return project.Configurations.Single(c => c.Target.GetFragment<Platform>() == platform && c.Target.GetFragment<Optimization>() == optimization);
        }

        private SolutionProjectConfigMatch Match<TProject>(Platform platform, Optimization optimization)
        {
            Solution solution = GetSolution<MatcherSolution>();
            Solution.Configuration solutionConfiguration = GetSolutionConfiguration(solution, platform, optimization);
            Solution.ResolvedProject resolvedProject = solution.GetResolvedProjects(solution.Configurations, out _).Single(p => p.Project.GetType() == typeof(TProject));
            return SolutionConfigMatcher.Match(solution, solutionConfiguration, resolvedProject);
        }

        [Test]
        public void GetNameAndCategoryUsesPlatformAsCategory()
        {
            Solution solution = GetSolution<MatcherSolution>();
            Solution.Configuration solutionConfiguration = GetSolutionConfiguration(solution, Platform.win64, Optimization.Debug);

            SolutionConfigMatcher.GetNameAndCategory(solution, solutionConfiguration, out string name, out string category);

            Assert.That(name, Is.EqualTo(solutionConfiguration.Name));
            Assert.That(category, Is.EqualTo(solutionConfiguration.PlatformName));
        }

        [Test]
        public void GetNameAndCategoryMergesPlatformIntoName()
        {
            Solution solution = GetSolution<MergedPlatformSolution>();
            Solution.Configuration solutionConfiguration = GetSolutionConfiguration(solution, Platform.win64, Optimization.Debug);

            SolutionConfigMatcher.GetNameAndCategory(solution, solutionConfiguration, out string name, out string category);

            Assert.That(name, Is.EqualTo(solutionConfiguration.PlatformName + "-" + solutionConfiguration.Name));
            Assert.That(category, Is.EqualTo("All Platforms"));
        }

        [TestCase(Platform.win32, Optimization.Debug)]
        [TestCase(Platform.win32, Optimization.Release)]
        [TestCase(Platform.win64, Optimization.Debug)]
        [TestCase(Platform.win64, Optimization.Release)]
        public void PerfectMatchUsesIncludedConfiguration(Platform platform, Optimization optimization)
        {
            var match = Match<FullLib>(platform, optimization);
            var expectedConf = GetProjectConfiguration(GetProject<FullLib>(), platform, optimization);

            Assert.That(match.PerfectMatch, Is.True);
            Assert.That(match.IncludedProject, Is.Not.Null);
            Assert.That(match.ProjectConf, Is.SameAs(expectedConf));
            Assert.That(match.ProjectTarget, Is.SameAs(match.IncludedProject.Target));
            Assert.That(match.Build, Is.True);
            Assert.That(match.ForceDeploy, Is.False);
            Assert.That(match.BuildDeploy, Is.False);
        }

        [TestCase(Platform.win32)]
        [TestCase(Platform.win64)]
        public void MissingProjectFallsBackToClosestConfiguration(Platform platform)
        {
            // DebugOnlyLib is only in the Debug solution configurations, so in Release it must
            // map to the Debug configuration that shares the most fragments (the same platform).
            var match = Match<DebugOnlyLib>(platform, Optimization.Release);
            var expectedConf = GetProjectConfiguration(GetProject<DebugOnlyLib>(), platform, Optimization.Debug);

            Assert.That(match.PerfectMatch, Is.False);
            Assert.That(match.IncludedProject, Is.Null);
            Assert.That(match.ProjectConf, Is.SameAs(expectedConf));
            Assert.That(match.ProjectTarget.GetFragment<Platform>(), Is.EqualTo(platform));
            Assert.That(match.ProjectTarget.GetFragment<Optimization>(), Is.EqualTo(Optimization.Debug));
            Assert.That(match.Build, Is.False);
            Assert.That(match.ForceDeploy, Is.False);
            Assert.That(match.BuildDeploy, Is.False);
        }

        [TestCase(Platform.win32, Optimization.Debug)]
        [TestCase(Platform.win64, Optimization.Release)]
        public void TiedFallbackPrefersLowestPlatform(Platform platform, Optimization optimization)
        {
            // TieLib is only in win32|Release and win64|Debug. Both candidates share the same number
            // of fragments with win32|Debug and win64|Release, so the lowest platform must win.
            var match = Match<TieLib>(platform, optimization);
            var expectedConf = GetProjectConfiguration(GetProject<TieLib>(), Platform.win32, Optimization.Release);

            Assert.That(match.PerfectMatch, Is.False);
            Assert.That(match.ProjectConf, Is.SameAs(expectedConf));
            Assert.That(match.Build, Is.False);
        }

        [Test]
        public void InactiveProjectIsNotBuilt()
        {
            var match = Match<InactiveLib>(Platform.win64, Optimization.Debug);

            Assert.That(match.PerfectMatch, Is.True);
            Assert.That(match.IncludedProject.ToBuild, Is.EqualTo(Solution.Configuration.IncludedProjectInfo.Build.No));
            Assert.That(match.Build, Is.False);
        }

        [Test]
        public void ProjectBuiltThroughDependencyIsBuiltWithMsBuild()
        {
            var match = Match<ThroughDependencyLib>(Platform.win64, Optimization.Debug);

            Assert.That(match.PerfectMatch, Is.True);
            Assert.That(match.IncludedProject.ToBuild, Is.EqualTo(Solution.Configuration.IncludedProjectInfo.Build.YesThroughDependency));
            Assert.That(match.ProjectConf.IsFastBuild, Is.False);
            Assert.That(match.Build, Is.True);
        }

        [Test]
        public void AlwaysDeployProjectIsForceDeployed()
        {
            var match = Match<AlwaysDeployLib>(Platform.win64, Optimization.Debug);

            Assert.That(match.Build, Is.True);
            Assert.That(match.ForceDeploy, Is.True);
            Assert.That(match.BuildDeploy, Is.False);
        }

        [Test]
        public void OnlyIfBuildConfigurationIsDeployedWhenBuilt()
        {
            var match = Match<OnlyIfBuildExe>(Platform.win64, Optimization.Debug);

            Assert.That(match.Build, Is.True);
            Assert.That(match.ForceDeploy, Is.False);
            Assert.That(match.BuildDeploy, Is.True);
        }

        [Test]
        public void OnlyIfBuildConfigurationIsNotDeployedWhenNotBuilt()
        {
            var match = Match<InactiveOnlyIfBuildLib>(Platform.win64, Optimization.Debug);

            Assert.That(match.Build, Is.False);
            Assert.That(match.ForceDeploy, Is.False);
            Assert.That(match.BuildDeploy, Is.False);
        }
    }

    namespace SolutionConfigMatcherTestProjects
    {
        public abstract class MatcherProjectBase : Project
        {
            private readonly Configuration.OutputType _output;

            protected MatcherProjectBase(Configuration.OutputType output = Configuration.OutputType.Lib)
            {
                _output = output;
                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(new Target(Platform.win32 | Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
            }

            [Configure]
            public virtual void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = _output;
            }
        }

        [Generate]
        public class FullLib : MatcherProjectBase { }

        [Generate]
        public class DebugOnlyLib : MatcherProjectBase { }

        [Generate]
        public class TieLib : MatcherProjectBase { }

        [Generate]
        public class InactiveLib : MatcherProjectBase { }

        [Generate]
        public class ThroughDependencyLib : MatcherProjectBase { }

        [Generate]
        public class AlwaysDeployLib : MatcherProjectBase
        {
            public AlwaysDeployLib()
            {
                DeployProjectType = DeployType.AlwaysDeploy;
            }
        }

        [Generate]
        public class OnlyIfBuildExe : MatcherProjectBase
        {
            public OnlyIfBuildExe()
                : base(Configuration.OutputType.Exe)
            {
            }

            public override void ConfigureAll(Configuration conf, Target target)
            {
                base.ConfigureAll(conf, target);
                conf.DeployProjectType = DeployType.OnlyIfBuild;
                conf.AddPrivateDependency<ThroughDependencyLib>(target);
            }
        }

        [Generate]
        public class InactiveOnlyIfBuildLib : MatcherProjectBase
        {
            public override void ConfigureAll(Configuration conf, Target target)
            {
                base.ConfigureAll(conf, target);
                conf.DeployProjectType = DeployType.OnlyIfBuild;
            }
        }

        public abstract class MatcherSolutionBase : Solution
        {
            protected MatcherSolutionBase()
            {
                AddTargets(new Target(Platform.win32 | Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
            }
        }

        [Generate]
        public class MatcherSolution : MatcherSolutionBase
        {
            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddProject<FullLib>(target);
                conf.AddProject<InactiveLib>(target, inactiveProject: true);
                conf.AddProject<AlwaysDeployLib>(target);
                conf.AddProject<OnlyIfBuildExe>(target);
                conf.AddProject<InactiveOnlyIfBuildLib>(target, inactiveProject: true);

                if (target.Optimization == Optimization.Debug)
                    conf.AddProject<DebugOnlyLib>(target);

                if ((target.Platform == Platform.win32 && target.Optimization == Optimization.Release) ||
                    (target.Platform == Platform.win64 && target.Optimization == Optimization.Debug))
                    conf.AddProject<TieLib>(target);
            }
        }

        [Generate]
        public class MergedPlatformSolution : MatcherSolutionBase
        {
            public MergedPlatformSolution()
            {
                MergePlatformConfiguration = true;
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddProject<FullLib>(target);
            }
        }
    }
}
