// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using NUnit.Framework;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.UnitTests
{
    [TestFixture]
    public class CsprojTest
    {
        [TestFixture]
        public class GetProjectLinkedFolder
        {
            [Test]
            public void FileUnderSourceRootPath()
            {
                var filePath = "..\\..\\codebase\\helloworld\\program.cs";
                var projectPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld";
                var sourceRootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\codebase\\helloworld";

                var project = new Project() { SourceRootPath = sourceRootPath };

                var result = CSproj.GetProjectLinkedFolder(filePath, projectPath, project);

                Assert.That(result, Is.EqualTo(""));
            }

            [Test]
            public void FileUnderRootPath()
            {
                var filePath = "..\\..\\codebase\\helloworld\\program.cs";
                var projectPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld";
                var sourceRootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\source\\helloworld";
                var rootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\codebase\\helloworld";

                var project = new Project() { SourceRootPath = sourceRootPath, RootPath = rootPath };

                var result = CSproj.GetProjectLinkedFolder(filePath, projectPath, project);

                Assert.That(result, Is.EqualTo(""));
            }

            [Test]
            public void RootAndSourcePathCorrectOrder()
            {
                var filePath = "..\\..\\codebase\\helloworld\\program.cs";
                var projectPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld";
                var sourceRootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\codebase\\helloworld";
                var rootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld";

                var project = new Project() { SourceRootPath = sourceRootPath, RootPath = rootPath };

                var result = CSproj.GetProjectLinkedFolder(filePath, projectPath, project);

                Assert.That(result, Is.Not.EqualTo("codebase\\helloworld"));
            }

            [Test]
            public void FileUnderProjectPath()
            {
                var filePath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld\\program.cs";
                var projectPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld";
                var sourceRootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\codebase\\helloworld";
                var rootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld";

                var project = new Project() { SourceRootPath = sourceRootPath, RootPath = rootPath };

                var result = CSproj.GetProjectLinkedFolder(filePath, projectPath, project);

                Assert.That(result, Is.Null);
            }

            [Test]
            public void AbsoluteFilePath()
            {
                var filePath = "c:\\.nuget\\dd\\llvm\\build\\native\\llvm.sharpmake.cs";
                var projectPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld";
                var sourceRootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\codebase\\helloworld";
                var rootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld";

                var project = new Project() { SourceRootPath = sourceRootPath, RootPath = rootPath };

                var result = CSproj.GetProjectLinkedFolder(filePath, projectPath, project);

                Assert.That(result, Is.EqualTo(".nuget\\dd\\llvm\\build\\native"));
            }

            [Test]
            public void RelativePathFileOutsideProject()
            {
                var filePath = "..\\..\\..\\..\\code\\platform\\standalone.main.sharpmake.cs";
                var projectPath =       "d:\\versioncontrol\\workspace\\generated\\platform\\sharpmake\\debugsolution";
                var sourceRootPath =    "d:\\versioncontrol\\workspace\\generated\\platform\\sharpmake\\debugsolution";
                var rootPath =          "d:\\versioncontrol\\workspace\\generated\\platform\\sharpmake\\debugsolution";

                var project = new Project() { SourceRootPath = sourceRootPath, RootPath = rootPath };

                var result = CSproj.GetProjectLinkedFolder(filePath, projectPath, project);

                Assert.That(result, Is.EqualTo("code\\platform"));
            }

            [Test]
            public void AbsolutePathFileInProjectFolder()
            {
                var filePath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld\\program.cs";
                var projectPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld";
                var sourceRootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\codebase\\helloworld";

                var project = new Project() { SourceRootPath = sourceRootPath, RootPath = sourceRootPath };

                var result = CSproj.GetProjectLinkedFolder(filePath, projectPath, project);

                Assert.That(result, Is.Null);
            }

            [Test]
            public void RelativePathFileInProjectFolder()
            {
                var filePath = "..\\helloworld\\program.cs";
                var projectPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\projects\\helloworld";
                var sourceRootPath = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\codebase\\helloworld";

                var project = new Project() { SourceRootPath = sourceRootPath, RootPath = sourceRootPath };

                var result = CSproj.GetProjectLinkedFolder(filePath, projectPath, project);

                Assert.That(result, Is.Null);
            }

            [Test]
            public void CasingUnchanged()
            {
                var filePathLowerCase = "..\\..\\codebase\\helloworld\\program.cs";
                var projectPathLowerCase = "D:\\Git\\Sharpmake\\sharpmake\\samples\\CSharpHelloWorld\\projects\\helloworld";
                var sourceRootPathLowerCase = "d:\\git\\sharpmake\\sharpmake\\samples\\csharphelloworld\\codebase\\";

                var filePathCamelCase = "..\\..\\CodeBase\\HelloWorld\\Program.cs";
                var projectPathCamelCase = "D:\\Git\\Sharpmake\\Sharpmake\\Samples\\CSharpHelloWorld\\Projects\\HelloWorld";
                var sourceRootPathCamelCase = "D:\\Git\\Sharpmake\\Sharpmake\\Samples\\CSharpHelloWorld\\Codebase\\";

                var projectLowerCase = new Project() { SourceRootPath = sourceRootPathLowerCase };
                var result = CSproj.GetProjectLinkedFolder(filePathLowerCase, projectPathLowerCase, projectLowerCase);

                Assert.That(string.Equals("helloworld", result, System.StringComparison.Ordinal), Is.True);
                Assert.That(string.Equals("HelloWorld", result, System.StringComparison.Ordinal), Is.False);

                var projectCamelCase = new Project() { SourceRootPath = sourceRootPathCamelCase };
                result = CSproj.GetProjectLinkedFolder(filePathCamelCase, projectPathCamelCase, projectCamelCase);

                Assert.That(string.Equals("HelloWorld", result, System.StringComparison.Ordinal), Is.True);
                Assert.That(string.Equals("helloworld", result, System.StringComparison.Ordinal), Is.False);
            }
        }
    }
}
