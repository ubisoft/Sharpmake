// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Sharpmake.Generators.VisualStudio;

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
}
