// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using NUnit.Framework;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.UnitTests
{
    [TestFixture]
    public class SlnTest
    {
        // -------------------------------------------------------------------------
        // ReadGuidFromProjectFile / ReadOrGenerateGuidFromProjectFile
        // -------------------------------------------------------------------------

        [TestFixture]
        public class ReadGuidFromProjectFileTests
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

            private string WriteTempFile(string name, string content)
            {
                Directory.CreateDirectory(_tempDir);
                var path = Path.Combine(_tempDir, name);
                File.WriteAllText(path, content);
                return path;
            }

            [Test]
            public void ReturnsGuidFromOldStyleVcxproj()
            {
                var path = WriteTempFile("test.vcxproj",
                    "<PropertyGroup Label=\"Globals\">\n" +
                    "  <ProjectGuid>{1A2B3C4D-1234-5678-ABCD-EF0123456789}</ProjectGuid>\n" +
                    "</PropertyGroup>");

                var guid = Sln.ReadGuidFromProjectFile(path);

                Assert.AreEqual("1A2B3C4D-1234-5678-ABCD-EF0123456789", guid);
            }

            [Test]
            public void ReturnsGuidFromOldStyleCsproj()
            {
                var path = WriteTempFile("test.csproj",
                    "<PropertyGroup>\n" +
                    "  <ProjectGuid>{AABBCCDD-AABB-CCDD-EEFF-001122334455}</ProjectGuid>\n" +
                    "</PropertyGroup>");

                var guid = Sln.ReadGuidFromProjectFile(path);

                Assert.AreEqual("AABBCCDD-AABB-CCDD-EEFF-001122334455", guid);
            }

            [Test]
            public void ReturnsNullForSdkStyleCsprojWithoutProjectGuid()
            {
                // SDK-style csproj (modern .NET) typically omits <ProjectGuid>.
                var path = WriteTempFile("sdk_style.csproj",
                    "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                    "  <PropertyGroup>\n" +
                    "    <TargetFramework>net8.0</TargetFramework>\n" +
                    "    <OutputType>Library</OutputType>\n" +
                    "  </PropertyGroup>\n" +
                    "</Project>");

                var guid = Sln.ReadGuidFromProjectFile(path);

                Assert.IsNull(guid, "SDK-style csproj without <ProjectGuid> should return null");
            }

            [Test]
            public void ThrowsForNonExistentFile()
            {
                var missing = Path.Combine(_tempDir, "does_not_exist.csproj");
                Assert.Throws<InvalidOperationException>(() => Sln.ReadGuidFromProjectFile(missing));
            }

            [Test]
            public void GenerateDeterministicGuidIsDeterministic()
            {
                var path = WriteTempFile("no_guid.csproj",
                    "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup/></Project>");

                var guid1 = Sln.ReadOrGenerateGuidFromProjectFile(path);
                var guid2 = Sln.ReadOrGenerateGuidFromProjectFile(path);

                Assert.AreEqual(guid1, guid2, "deterministic GUID must be stable across calls");
            }

            [Test]
            public void GenerateDeterministicGuidDiffersForDifferentPaths()
            {
                var path1 = WriteTempFile("a.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"/>");
                var path2 = WriteTempFile("b.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"/>");

                var guid1 = Sln.ReadOrGenerateGuidFromProjectFile(path1);
                var guid2 = Sln.ReadOrGenerateGuidFromProjectFile(path2);

                Assert.AreNotEqual(guid1, guid2, "different files must get different GUIDs");
            }

            [Test]
            public void GenerateDeterministicGuidIsValidGuidFormat()
            {
                var path = WriteTempFile("no_guid2.csproj",
                    "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup/></Project>");

                var guidStr = Sln.ReadOrGenerateGuidFromProjectFile(path);

                Assert.IsTrue(Guid.TryParse(guidStr, out _),
                    $"Generated value '{guidStr}' must be a valid GUID");
            }

            [Test]
            public void ReturnsExistingGuidWhenPresentRatherThanGenerating()
            {
                const string expected = "DEADBEEF-0000-0000-0000-DEADBEEF0000";
                var path = WriteTempFile("with_guid.csproj",
                    $"<PropertyGroup><ProjectGuid>{{{expected}}}</ProjectGuid></PropertyGroup>");

                var guid = Sln.ReadOrGenerateGuidFromProjectFile(path);

                Assert.AreEqual(expected, guid,
                    "should return the file's own GUID, not generate a new one");
            }
        }
    }
}
