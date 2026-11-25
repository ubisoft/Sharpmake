// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
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

        // The solution target leaves the architecture unset (x64): the fallback must pick the project's x64 target
        // and not the arm64 one. Raw values only tie (unset never equals anything), so this relies on the effective values.
        [Test]
        public void MissingProjectFallsBackToConfigurationWithSameArchitecture()
        {
            Solution solution = GetSolution<ArchitectureSolution>();
            Solution.Configuration apxConfiguration = solution.Configurations.Single(c => Util.GetInstructionSetLevel(c.Target) == InstructionSetLevel.APX);
            Solution.ResolvedProject resolvedProject = solution.GetResolvedProjects(solution.Configurations, out _).Single(p => p.Project.GetType() == typeof(ArchitectureLib));

            var match = SolutionConfigMatcher.Match(solution, apxConfiguration, resolvedProject);

            Assert.That(match.PerfectMatch, Is.False);
            Assert.That(Util.GetProcessorArchitecture(match.ProjectTarget), Is.EqualTo(ProcessorArchitecture.x64));
        }

        // The project platform written in the .sln is the one of the project's matched target, not of the solution's:
        // an arm64 solution configuration mapped to a project without arm64 target must point to its x64 configuration
        [Test]
        public void ProjectPlatformFollowsTheMatchedProjectTarget()
        {
            Solution solution = GetSolution<ArchitectureSolution>();
            Solution.Configuration arm64Configuration = solution.Configurations.Single(c => Util.GetProcessorArchitecture(c.Target) == ProcessorArchitecture.arm64);
            var resolvedProjects = solution.GetResolvedProjects(solution.Configurations, out _);

            var x64OnlyMatch = SolutionConfigMatcher.Match(solution, arm64Configuration, resolvedProjects.Single(p => p.Project.GetType() == typeof(X64OnlyLib)));
            var bothMatch = SolutionConfigMatcher.Match(solution, arm64Configuration, resolvedProjects.Single(p => p.Project.GetType() == typeof(ArchitectureLib)));

            Assert.That(SolutionConfigMatcher.GetProjectPlatform(arm64Configuration, x64OnlyMatch, resolvedProjects.Single(p => p.Project.GetType() == typeof(X64OnlyLib))), Is.EqualTo("x64"));
            Assert.That(SolutionConfigMatcher.GetProjectPlatform(arm64Configuration, bothMatch, resolvedProjects.Single(p => p.Project.GetType() == typeof(ArchitectureLib))), Is.EqualTo("ARM64"));
        }

        // A custom platform name that ignores the architecture gives the same "name|platform" to the x64, APX and ARM64
        // configurations: the duplicate error must say why and how to fix it
        [Test]
        public void DuplicateHint_ForConfigurationsDifferingByArchitecture()
        {
            Solution solution = GetSolution<CustomPlatformNameSolution>();

            string hint = SolutionConfigMatcher.GetDuplicateNamesHint(solution.Configurations);

            Assert.That(hint, Does.Contain("GetArchitectureDisplaySuffix"));
        }

        [Test]
        public void DuplicateHint_IsEmptyWhenTheNamesAreDistinct()
        {
            Solution solution = GetSolution<ArchitectureSolution>();

            Assert.That(SolutionConfigMatcher.GetDuplicateNamesHint(solution.Configurations), Is.Empty);
        }

        [Test]
        public void CustomPlatformNameWithDisplaySuffix_GivesDistinctNames()
        {
            Solution solution = GetSolution<SuffixedPlatformNameSolution>();

            var names = solution.Configurations.Select(c => c.Name + "|" + c.PlatformName).ToList();

            Assert.That(names.Distinct().Count(), Is.EqualTo(3));
            Assert.That(SolutionConfigMatcher.GetDuplicateNamesHint(solution.Configurations), Is.Empty);
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

        // Runs the FastBuild tracking over every (project x solution-config) match the same way the
        // solution generators do.
        private static Dictionary<Solution.Configuration, List<string>> CollectFastBuildBuilt(Solution solution)
        {
            var fastBuildBuilt = new Dictionary<Solution.Configuration, List<string>>();
            foreach (Solution.ResolvedProject resolvedProject in solution.GetResolvedProjects(solution.Configurations, out _))
            {
                foreach (Solution.Configuration solutionConfiguration in solution.Configurations)
                {
                    var match = SolutionConfigMatcher.Match(solution, solutionConfiguration, resolvedProject);
                    SolutionConfigMatcher.TrackFastBuildConfig(fastBuildBuilt, solutionConfiguration, match);
                    SolutionConfigMatcher.RecordFastBuildBuilt(fastBuildBuilt, solutionConfiguration, match);
                }
            }
            return fastBuildBuilt;
        }

        [Test]
        public void SolutionWithoutFastBuildProjectsIsNotTracked()
        {
            Solution solution = GetSolution<MatcherSolution>();

            var fastBuildBuilt = CollectFastBuildBuilt(solution);
            SolutionConfigMatcher.ReportFastBuildDiagnostics("MatcherSolution", solution, fastBuildBuilt);

            Assert.That(fastBuildBuilt, Is.Empty);
            Assert.That(ErrorCount, Is.Zero);
        }

        [TestCase(Platform.win32)]
        [TestCase(Platform.win64)]
        public void SolutionConfigurationWithBuiltFastBuildProjectIsNotReported(Platform platform)
        {
            Solution solution = GetSolution<FastBuildSolution>();
            Solution.Configuration solutionConfiguration = GetSolutionConfiguration(solution, platform, Optimization.Debug);

            var fastBuildBuilt = CollectFastBuildBuilt(solution);

            Assert.That(fastBuildBuilt[solutionConfiguration], Is.EqualTo(new[] { "FastBuildLib " + solutionConfiguration.Name }));
        }

        [TestCase(Platform.win32)]
        [TestCase(Platform.win64)]
        public void SolutionConfigurationWithoutBuiltFastBuildProjectIsTracked(Platform platform)
        {
            // Only the inactive FastBuild project is included in Release, so nothing builds it.
            Solution solution = GetSolution<FastBuildSolution>();
            Solution.Configuration solutionConfiguration = GetSolutionConfiguration(solution, platform, Optimization.Release);

            var fastBuildBuilt = CollectFastBuildBuilt(solution);

            Assert.That(fastBuildBuilt[solutionConfiguration], Is.Empty);
        }

        [Test]
        public void SolutionConfigurationsWithoutBuiltFastBuildProjectAreReported()
        {
            Solution solution = GetSolution<FastBuildSolution>();

            SolutionConfigMatcher.ReportFastBuildDiagnostics("FastBuildSolution", solution, CollectFastBuildBuilt(solution));

            // win32|Release and win64|Release
            Assert.That(ErrorCount, Is.EqualTo(2));
        }

        [Test]
        public void MoreThanOneBuiltFastBuildProjectIsReportedWithFastBuildAll()
        {
            Solution solution = GetSolution<MatcherSolution>();
            Assert.That(solution.GenerateFastBuildAllProject, Is.True);

            var fastBuildBuilt = new Dictionary<Solution.Configuration, List<string>>
            {
                { GetSolutionConfiguration(solution, Platform.win64, Optimization.Debug), new List<string> { "A Debug", "B Debug" } },
                { GetSolutionConfiguration(solution, Platform.win64, Optimization.Release), new List<string> { "A Release" } },
            };
            SolutionConfigMatcher.ReportFastBuildDiagnostics("MatcherSolution", solution, fastBuildBuilt);

            Assert.That(ErrorCount, Is.EqualTo(1));
        }

        [Test]
        public void MoreThanOneBuiltFastBuildProjectIsNotReportedWithoutFastBuildAll()
        {
            Solution solution = GetSolution<FastBuildSolution>();
            Assert.That(solution.GenerateFastBuildAllProject, Is.False);

            var fastBuildBuilt = new Dictionary<Solution.Configuration, List<string>>
            {
                { GetSolutionConfiguration(solution, Platform.win64, Optimization.Debug), new List<string> { "A Debug", "B Debug" } },
            };
            SolutionConfigMatcher.ReportFastBuildDiagnostics("FastBuildSolution", solution, fastBuildBuilt);

            Assert.That(ErrorCount, Is.Zero);
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

        public abstract class FastBuildProjectBase : MatcherProjectBase
        {
            public override void ConfigureAll(Configuration conf, Target target)
            {
                base.ConfigureAll(conf, target);
                conf.IsFastBuild = true;
            }
        }

        [Generate]
        public class FastBuildLib : FastBuildProjectBase { }

        [Generate]
        public class InactiveFastBuildLib : FastBuildProjectBase { }

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

        [Generate]
        public class ArchitectureLib : Project
        {
            public ArchitectureLib()
            {
                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release));
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Lib;
            }
        }

        [Generate]
        public class X64OnlyLib : Project
        {
            public X64OnlyLib()
            {
                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release));
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Lib;
            }
        }

        // ArchitectureLib is not in the APX configuration, X64OnlyLib only in the x64 baseline one
        [Generate]
        public class ArchitectureSolution : Solution
        {
            public ArchitectureSolution()
            {
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { InstructionSetLevel = InstructionSetLevel.Baseline | InstructionSetLevel.APX });
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                if (target.InstructionSetLevel == InstructionSetLevel.APX)
                    return;

                conf.AddProject<ArchitectureLib>(target);

                if (target.ProcessorArchitecture != ProcessorArchitecture.arm64)
                    conf.AddProject<X64OnlyLib>(target);
            }
        }

        public abstract class ArchitectureTargetsSolutionBase : Solution
        {
            protected ArchitectureTargetsSolutionBase()
            {
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { InstructionSetLevel = InstructionSetLevel.Baseline | InstructionSetLevel.APX });
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
            }
        }

        // like AP's "Blob-Compiler" names: they know nothing about the architecture
        [Generate]
        public class CustomPlatformNameSolution : ArchitectureTargetsSolutionBase
        {
            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.PlatformName = "Custom";
            }
        }

        [Generate]
        public class SuffixedPlatformNameSolution : ArchitectureTargetsSolutionBase
        {
            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                string suffix = Util.GetArchitectureDisplaySuffix(target);
                conf.PlatformName = suffix.Length == 0 ? "Custom" : "Custom-" + suffix;
            }
        }

        [Generate]
        public class FastBuildSolution : MatcherSolutionBase
        {
            public FastBuildSolution()
            {
                GenerateFastBuildAllProject = false;
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddProject<InactiveFastBuildLib>(target, inactiveProject: true);

                if (target.Optimization == Optimization.Debug)
                    conf.AddProject<FastBuildLib>(target);
            }
        }
    }
}
