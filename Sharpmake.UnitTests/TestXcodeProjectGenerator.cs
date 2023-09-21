// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using NUnit.Framework;
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
    }
}
