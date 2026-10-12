// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sharpmake.Generators.VisualStudio;
using Sharpmake.UnitTests.SolutionProjectResolverTestProjects;

namespace Sharpmake.UnitTests
{
    // Covers logic shared by the .sln and .slnx generators (Sln.cs / Slnx.cs both call into
    // SolutionProjectResolver), so a fix here protects both from drifting apart.
    [TestFixture]
    public class SolutionProjectResolverTests
    {
        [TestFixture]
        public class ResolveReferencesByPathTests
        {
            private string _tempDir;

            [SetUp]
            public void SetUp() => _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            [TearDown]
            public void TearDown()
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }

            private string WriteSdkStyleCsprojWithoutGuid(string name)
            {
                Directory.CreateDirectory(_tempDir);
                var path = Path.Combine(_tempDir, name);
                File.WriteAllText(path,
                    "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                    "  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n" +
                    "</Project>");
                return path;
            }

            [Test]
            public void DoesNotThrowForSdkStyleCsprojWithoutProjectGuid()
            {
                // Regression test: SDK-style csproj referenced by path has no <ProjectGuid>.
                // The resolver must fall back to a generated GUID instead of crashing.
                var path = WriteSdkStyleCsprojWithoutGuid("NoGuid.csproj");
                var referencedPaths = new Strings(path);
                var folderTree = new SolutionFolderTree();

                List<Solution.ResolvedProject> resolved = null;
                Assert.DoesNotThrow(() =>
                    resolved = SolutionProjectResolver.ResolveReferencesByPath(new List<Solution.ResolvedProject>(), referencedPaths, folderTree));

                Assert.That(resolved, Has.Count.EqualTo(1));
                var guid = resolved[0].UserData["Guid"] as string;
                Assert.That(guid, Is.Not.Null.And.Not.Empty);
                Assert.That(Guid.TryParse(guid, out _), Is.True);
            }

            [Test]
            public void AssignsSolutionFolderFromPathFoldersMap()
            {
                // Regression test: a pathFolders mapping must place the path-referenced project
                // in the requested solution folder instead of always defaulting to the root.
                var path = WriteSdkStyleCsprojWithoutGuid("Foldered.csproj");
                var referencedPaths = new Strings(path);
                var pathFolders = new Dictionary<string, string> { [path] = "Engine" };

                var resolvedWithFolder = SolutionProjectResolver.ResolveReferencesByPath(new List<Solution.ResolvedProject>(), referencedPaths, new SolutionFolderTree(), pathFolders);
                var resolvedWithoutFolder = SolutionProjectResolver.ResolveReferencesByPath(new List<Solution.ResolvedProject>(), referencedPaths, new SolutionFolderTree());

                Assert.That(resolvedWithFolder[0].UserData["Folder"], Is.Not.Null,
                    "a pathFolders mapping should place the project in a solution folder");
                Assert.That(resolvedWithoutFolder[0].UserData["Folder"], Is.Null,
                    "without a pathFolders mapping, the project should default to the solution root");
            }
        }
    }

    public class SolutionProjectResolverStartupTests : TestProjectBuilder
    {
        public SolutionProjectResolverStartupTests()
            : base(InitType.Cpp, typeof(ExactNameSolution).Namespace, ConfigureOrder.New)
        {
        }

        [OneTimeSetUp]
        public void BuildOnce()
        {
            Init();
            BuildAndLinkProjects();
            Assert.That(ErrorCount, Is.Zero);
        }

        private static string[] ProjectNames(IEnumerable<Solution.ResolvedProject> resolvedProjects)
        {
            return resolvedProjects.Select(p => p.ProjectName).ToArray();
        }

        [Test]
        public void ResolveStartupProjectWithoutConfigurationsReturnsNull()
        {
            var solution = GetSolution<ExactNameSolution>();

            Assert.That(SolutionProjectResolver.ResolveStartupProject(solution, new List<Solution.Configuration>()), Is.Null);
        }

        [Test]
        public void ResolveStartupProjectPrefersExeNamedLikeSolution()
        {
            // "Startup" also prefixes the solution name and sorts first, so only the exact-name rule picks StartupExact.
            var solution = GetSolution<ExactNameSolution>();

            var startupProject = SolutionProjectResolver.ResolveStartupProject(solution, solution.Configurations);

            Assert.That(startupProject.Project, Is.SameAs(GetProject<StartupExactExe>()));
        }

        [Test]
        public void ResolveStartupProjectFallsBackToExeNamePrefixingSolutionName()
        {
            var solution = GetSolution<PrefixNameSolution>();

            var startupProject = SolutionProjectResolver.ResolveStartupProject(solution, solution.Configurations);

            Assert.That(startupProject.Project, Is.SameAs(GetProject<StartupPrefixExe>()));
        }

        [Test]
        public void ResolveStartupProjectFallsBackToFirstExeByFileName()
        {
            var solution = GetSolution<FallbackSolution>();

            var startupProject = SolutionProjectResolver.ResolveStartupProject(solution, solution.Configurations);

            Assert.That(startupProject.Project, Is.SameAs(GetProject<AaaToolExe>()));
        }

        [Test]
        public void ResolveStartupProjectIgnoresLibraries()
        {
            var solution = GetSolution<LibOnlySolution>();

            Assert.That(SolutionProjectResolver.ResolveStartupProject(solution, solution.Configurations), Is.Null);
        }

        [Test]
        public void ResolveSolutionProjectsPutsResolvedStartupProjectFirst()
        {
            var solution = GetSolution<ExactNameSolution>();

            var projects = SolutionProjectResolver.ResolveSolutionProjects(solution, solution.Configurations, new SolutionFolderTree(), out var startupProject);

            Assert.That(ProjectNames(projects), Is.EqualTo(new[] { "StartupExact", "AaaTool", "PlainLib", "Startup" }));
            Assert.That(startupProject, Is.SameAs(projects[0]));
        }

        [Test]
        public void ResolveSolutionProjectsPutsExplicitStartupProjectFirst()
        {
            // The explicit startup project wins over AaaTool, which the name heuristic would pick.
            var solution = GetSolution<ExplicitStartupSolution>();

            var projects = SolutionProjectResolver.ResolveSolutionProjects(solution, solution.Configurations, new SolutionFolderTree(), out var startupProject);

            Assert.That(ProjectNames(projects), Is.EqualTo(new[] { "ZzzTool", "AaaTool", "FolderLib", "PlainLib" }));
            Assert.That(startupProject, Is.SameAs(projects[0]));
            Assert.That(startupProject.Project, Is.SameAs(GetProject<ZzzToolExe>()));
        }

        [Test]
        public void ResolveSolutionProjectsWithoutExeHasNoStartupProject()
        {
            var solution = GetSolution<LibOnlySolution>();

            var projects = SolutionProjectResolver.ResolveSolutionProjects(solution, solution.Configurations, new SolutionFolderTree(), out var startupProject);

            Assert.That(ProjectNames(projects), Is.EqualTo(new[] { "PlainLib" }));
            Assert.That(startupProject, Is.Null);
        }

        [Test]
        public void ResolveSolutionProjectsThrowsOnMultipleStartupProjects()
        {
            var solution = GetSolution<MultipleStartupSolution>();

            var error = Assert.Throws<Error>(() =>
                SolutionProjectResolver.ResolveSolutionProjects(solution, solution.Configurations, new SolutionFolderTree(), out _));
            Assert.That(error.Message, Does.Contain("multiple startup projects"));
        }

        [Test]
        public void ResolveSolutionProjectsFillsUserData()
        {
            var solution = GetSolution<ExplicitStartupSolution>();
            var folderTree = new SolutionFolderTree();

            var projects = SolutionProjectResolver.ResolveSolutionProjects(solution, solution.Configurations, folderTree, out _);

            string vcxprojTypeGuid = ProjectTypeGuids.WindowsVisualCpp.ToString().ToUpper();
            foreach (var project in projects)
            {
                string projectGuid = project.Configurations.First().ProjectGuid;
                Assert.That(projectGuid, Is.Not.Null.And.Not.Empty, project.ProjectName);
                Assert.That(project.UserData["Guid"], Is.EqualTo(projectGuid), project.ProjectName);
                Assert.That(project.UserData["TypeGuid"], Is.EqualTo(vcxprojTypeGuid), project.ProjectName);
            }

            var folderLib = projects.Single(p => p.ProjectName == "FolderLib");
            var folder = (SolutionFolder)folderLib.UserData["Folder"];
            Assert.That(folder.Name, Is.EqualTo("Core"));
            Assert.That(folder.Parent.Name, Is.EqualTo("Libs"));
            Assert.That(folderTree.AllFolders, Is.EquivalentTo(new[] { folder.Parent, folder }));

            foreach (var project in projects.Where(p => p != folderLib))
                Assert.That(project.UserData["Folder"], Is.Null, project.ProjectName);
        }
    }

    namespace SolutionProjectResolverTestProjects
    {
        public abstract class ResolverProjectBase : Project
        {
            private readonly Configuration.OutputType _output;

            protected ResolverProjectBase(string name, Configuration.OutputType output)
            {
                Name = name;
                _output = output;
                // The test builder creates fake source files under the class name, not the project name.
                SourceRootPath = Path.Combine(Directory.GetCurrentDirectory(), GetType().Name);
                AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = _output;
            }
        }

        [Generate]
        public class AaaToolExe : ResolverProjectBase
        {
            public AaaToolExe() : base("AaaTool", Configuration.OutputType.Exe) { }
        }

        [Generate]
        public class ZzzToolExe : ResolverProjectBase
        {
            public ZzzToolExe() : base("ZzzTool", Configuration.OutputType.Exe) { }
        }

        [Generate]
        public class StartupExe : ResolverProjectBase
        {
            public StartupExe() : base("Startup", Configuration.OutputType.Exe) { }
        }

        [Generate]
        public class StartupExactExe : ResolverProjectBase
        {
            public StartupExactExe() : base("StartupExact", Configuration.OutputType.Exe) { }
        }

        [Generate]
        public class StartupPrefixExe : ResolverProjectBase
        {
            public StartupPrefixExe() : base("StartupPrefix", Configuration.OutputType.Exe) { }
        }

        [Generate]
        public class PlainLib : ResolverProjectBase
        {
            public PlainLib() : base("PlainLib", Configuration.OutputType.Lib) { }
        }

        [Generate]
        public class FolderLib : ResolverProjectBase
        {
            public FolderLib() : base("FolderLib", Configuration.OutputType.Lib) { }
        }

        public abstract class ResolverSolutionBase : Solution
        {
            protected ResolverSolutionBase(string name)
            {
                Name = name;
                AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
            }
        }

        [Generate]
        public class ExactNameSolution : ResolverSolutionBase
        {
            public ExactNameSolution() : base("StartupExact") { }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddProject<AaaToolExe>(target);
                conf.AddProject<StartupExe>(target);
                conf.AddProject<StartupExactExe>(target);
                conf.AddProject<PlainLib>(target);
            }
        }

        [Generate]
        public class PrefixNameSolution : ResolverSolutionBase
        {
            public PrefixNameSolution() : base("StartupPrefixSolution") { }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddProject<AaaToolExe>(target);
                conf.AddProject<StartupPrefixExe>(target);
            }
        }

        [Generate]
        public class FallbackSolution : ResolverSolutionBase
        {
            public FallbackSolution() : base("Unrelated") { }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddProject<ZzzToolExe>(target);
                conf.AddProject<AaaToolExe>(target);
                conf.AddProject<PlainLib>(target);
            }
        }

        [Generate]
        public class LibOnlySolution : ResolverSolutionBase
        {
            public LibOnlySolution() : base("LibOnly") { }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddProject<PlainLib>(target);
            }
        }

        [Generate]
        public class ExplicitStartupSolution : ResolverSolutionBase
        {
            public ExplicitStartupSolution() : base("ExplicitStartup") { }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddProject<PlainLib>(target);
                conf.AddProject<FolderLib>(target, solutionFolder: "Libs/Core");
                conf.AddProject<AaaToolExe>(target);
                conf.AddProject<ZzzToolExe>(target);
                conf.SetStartupProject<ZzzToolExe>();
            }
        }

        [Generate]
        public class MultipleStartupSolution : ResolverSolutionBase
        {
            public MultipleStartupSolution() : base("MultipleStartup") { }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddProject<AaaToolExe>(target);
                conf.AddProject<ZzzToolExe>(target);

                if (target.Optimization == Optimization.Debug)
                    conf.SetStartupProject<AaaToolExe>();
                else
                    conf.SetStartupProject<ZzzToolExe>();
            }
        }
    }
}
