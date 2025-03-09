// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sharpmake.Generators.Apple;
using static Sharpmake.Generators.Apple.XCodeProj;

namespace Sharpmake.UnitTests
{
    public class TestXcodeProjectGenerator
    {
        [Test]
        public void TestProjectBuildFile()
        {
            //identical filename under different folder
            var file1 = "/test/val/test.cpp";
            var file2 = "/test/opt/test.cpp";
            var projectFile1 = new ProjectFile(ItemSection.PBXBuildFile, file1);
            var projectFile2 = new ProjectFile(ItemSection.PBXBuildFile, file2);
            var projectBuildFile1 = new ProjectBuildFile(projectFile1);
            var projectBuildFile2 = new ProjectBuildFile(projectFile2);

            Assert.IsFalse(projectBuildFile1.Equals(projectBuildFile2));
        }

        [Test]
        public void TestUncompilableNotInCompileSources()
        {
            string xCodeTargetName = "test";
            var srcRoot = Directory.GetCurrentDirectory();
            List<string> sourceFiles = new List<string> { Path.Combine(srcRoot, "test.sc") };
            Project project = new Project();
            Project.Configuration configuration = new Project.Configuration();

            configuration.ProjectFullFileNameWithExtension = "./test/test.xcodeproj";

            var xcodePrj = new XCodeProj();

            project.SourceRootPath = srcRoot;
            xcodePrj._sourcesBuildPhases = new Dictionary<string, ProjectSourcesBuildPhase>();
            var projectSourcesBuildPhase = new ProjectSourcesBuildPhase(xCodeTargetName, 2147483647);
            xcodePrj._projectItems.Add(projectSourcesBuildPhase);
            xcodePrj._sourcesBuildPhases.Add(xCodeTargetName, projectSourcesBuildPhase);
            xcodePrj.SetRootGroup(project, configuration);
            xcodePrj.PrepareSourceFiles(xCodeTargetName, sourceFiles, project, configuration, false);
            var compileSources = xcodePrj._projectItems.Where(item => item is ProjectBuildFile);

            Assert.IsTrue(compileSources.Count() == 0);
        }

        [Test]
        public void TestGetLongestCommonPath()
        {
            string folder = "sourceroot";
            string refFolder = "differentSourceRoot";
            string retFolder = XCodeProj.GetLongestCommonPath(folder, refFolder);
            Assert.IsTrue(retFolder.Equals(string.Empty));

            folder = Path.Combine("sourceRoot", "source");
            refFolder = "sourceRoot";
            retFolder = XCodeProj.GetLongestCommonPath(folder, refFolder);
            Assert.IsTrue(retFolder.Equals(Path.Combine("sourceRoot", "source")));

            folder = Path.Combine("sourceRoot", "source");
            refFolder = Path.Combine("sourceRoot", "other");
            retFolder = XCodeProj.GetLongestCommonPath(folder, refFolder);
            Assert.IsTrue(retFolder.Equals(Path.Combine("sourceRoot", "source")));

            folder = "sourceRoot";
            refFolder = Path.Combine("sourceRoot", "source");
            retFolder = XCodeProj.GetLongestCommonPath(folder, refFolder);
            Assert.IsTrue(retFolder.Equals("sourceRoot"));
        }
    }
}
