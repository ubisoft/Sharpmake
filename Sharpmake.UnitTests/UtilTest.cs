// Copyright (c) 2017-2018, 2020-2021 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using NUnit.Framework;

using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Sharpmake.UnitTests
{
    namespace NiceTypeNameTest
    {
        internal class DummyClass { };
        internal class DummyClass2 { };

        internal class DummyGeneric<T> { };

        internal class DummyGeneric2<T, U> { };

        public class NiceTypeName
        {
            [Test]
            public void NiceTypeNameOnSimpleType()
            {
                Assert.That(typeof(DummyClass).ToNiceTypeName(), Is.EqualTo("Sharpmake.UnitTests.NiceTypeNameTest.DummyClass"));
                Assert.That(typeof(DummyClass2).ToNiceTypeName(), Is.EqualTo("Sharpmake.UnitTests.NiceTypeNameTest.DummyClass2"));
            }

            [Test]
            public void NiceTypeNameOnGenericType()
            {
                Assert.That(typeof(DummyGeneric<DummyClass>).ToNiceTypeName(),
                            Is.EqualTo("Sharpmake.UnitTests.NiceTypeNameTest.DummyGeneric<Sharpmake.UnitTests.NiceTypeNameTest.DummyClass>"));
                Assert.That(typeof(DummyGeneric2<DummyClass, DummyClass2>).ToNiceTypeName(),
                            Is.EqualTo("Sharpmake.UnitTests.NiceTypeNameTest.DummyGeneric2<Sharpmake.UnitTests.NiceTypeNameTest.DummyClass,Sharpmake.UnitTests.NiceTypeNameTest.DummyClass2>"));
            }
        }
    }

    public class PathMakeStandard
    {
        [Test]
        public void LeavesEmptyStringsUntouched()
        {
            Assert.That(Util.PathMakeStandard(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(Util.PathMakeStandard(""), Is.EqualTo(string.Empty));
            Assert.That(Util.PathMakeStandard(""), Is.EqualTo(""));
        }

        [Test]
        public void LeavesVariablesUntouched()
        {
            string expectedResult = "$(Console_SdkPackagesRoot)";
            if (!Util.IsRunningInMono())
                expectedResult = expectedResult.ToLower();
            Assert.That(Util.PathMakeStandard("$(Console_SdkPackagesRoot)"), Is.EqualTo(expectedResult));
        }

        [Test]
        public void ProcessesPathWithTrailingBackslash()
        {
            string expectedResult = Path.Combine("rd", "project", "dev", "projects", "sharpmake", "..", "..", "extern", "Geometrics");
            if (!Util.IsRunningInMono())
                expectedResult = expectedResult.ToLower();
            Assert.That(Util.PathMakeStandard(@"rd\project\dev\projects\sharpmake\..\..\extern\Geometrics\"), Is.EqualTo(expectedResult));
        }

        [Test]
        public void ProcessesPathWithTrailingBackslashAndADot()
        {
            var expectedResult = Path.Combine("rd", "project", "dev", "projects", "sharpmake", "..", "..", "extern", "Microsoft.CNG", "Lib");
            if (!Util.IsRunningInMono())
                expectedResult = expectedResult.ToLower();
            Assert.That(Util.PathMakeStandard(@"rd\project\dev\projects\sharpmake\..\..\extern\Microsoft.CNG\Lib\"), Is.EqualTo(expectedResult));
        }

        [Test]
        public void ProcessesPathWithMultipleTrailingBackslashes()
        {
            var expectedResult = Path.Combine("rd", "project", "dev", "projects", "sharpmake", "..", "..", "extern", "Microsoft.CNG", "Lib");
            if (!Util.IsRunningInMono())
                expectedResult = expectedResult.ToLower();
            Assert.That(Util.PathMakeStandard(@"rd\project\dev\projects\sharpmake\..\..\extern\Microsoft.CNG\Lib\\\"), Is.EqualTo(expectedResult));
        }

        /// <summary>
        ///     Verify that the strings from the list were format as path
        ///     <remark><c>PathMakeStandard</c> lower the path on Windows</remark>
        /// </summary>
        [Test]
        public void ProcessesWithAListAsArgument()
        {
            IList<string> listString = new List<string>()
            {
                Path.Combine("F:","SharpMake","sharpmake","Sharpmake.Application"),
                Path.Combine("F:","SharpMake","sharpmake","Sharpmake.Extensions")
            };
            var expectedList = listString;

            if (!Util.IsRunningInMono())
                expectedList = expectedList.Select((p) => p.ToLower()).ToList();

            Util.PathMakeStandard(listString);

            Assert.AreEqual(expectedList, listString);
        }
    }

    public class SimplifyPath
    {
        /// <summary>
        ///     Verify that an error is thrown when a path begin with three dots
        /// </summary>
        [Test]
        public void ThrowsErrorDot()
        {
            Assert.Throws<ArgumentException>(() => Util.SimplifyPath(".../sharpmake/README.md"));
        }

        /// <summary>
        ///     Verify that an error is thrown when a path start with dots but no slash
        /// </summary>
        [Test]
        public void ThrowsErrorSeparator()
        {
            Assert.Throws<ArgumentException>(() => Util.SimplifyPath("..sharpmake/README.md"));
        }

        [Test]
        public void LeavesEmptyStringsUntouched()
        {
            Assert.That(Util.SimplifyPath(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(Util.SimplifyPath(""), Is.EqualTo(string.Empty));
            Assert.That(Util.SimplifyPath(""), Is.EqualTo(""));
        }

        [Test]
        public void HandlesPathRelativeToCurrentFolder()
        {
            Assert.That(Util.SimplifyPath(@".\project\test.cpp"),
                Is.EqualTo(Path.Combine("project", "test.cpp")));

            Assert.That(Util.SimplifyPath(@".\.\.\.\project\.\test.cpp"),
                Is.EqualTo(Path.Combine("project", "test.cpp")));
        }

        [Test]
        public void HandlesReturningToParentFolder()
        {
            Assert.That(Util.SimplifyPath(@"test\..\test.cpp"),
                Is.EqualTo("test.cpp"));
        }

        [Test]
        public void HandlesReturningToParentFolderRelativeToCurrentFolder()
        {
            Assert.That(Util.SimplifyPath(@".\project\..\test.cpp"),
                Is.EqualTo("test.cpp"));

            Assert.That(Util.SimplifyPath(@".\.\.\.\project\..\test.cpp"),
                Is.EqualTo("test.cpp"));
        }

        [Test]
        public void CollapsesMultipleFolderSeparators()
        {
            Assert.That(Util.SimplifyPath(@".\\\project\..\test.cpp"),
                Is.EqualTo("test.cpp"));

            Assert.That(Util.SimplifyPath(@"\\\folder"),
                Is.EqualTo("folder"));
        }

        [Test]
        public void HandlesSlashesInFullPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            Assert.That(Util.SimplifyPath(currentDirectory + "\\main/test//t.cpp"),
                Is.EqualTo(Path.Combine(currentDirectory, "main", "test", "t.cpp")));
        }

        [Test]
        public void HandlesFolderParentsAtTheEnd()
        {
            Assert.That(Util.SimplifyPath(@"alpha\beta\gamma\sigma\omega\zeta\..\.."),
                Is.EqualTo(Path.Combine("alpha", "beta", "gamma", "sigma")));
        }

        [Test]
        public void LeavesCleanPathUntouched()
        {
            // Check that we do not change dot and dot dot
            Assert.That(".", Is.EqualTo(Util.SimplifyPath(".")));
            Assert.That("..", Is.EqualTo(Util.SimplifyPath("..")));

            Assert.That(Util.SimplifyPath(Util.PathMakeStandard(@"alpha\beta\gamma\sigma\omega\zeta\lambda\phi\")),
                Is.EqualTo(Path.Combine("alpha", "beta", "gamma", "sigma", "omega", "zeta", "lambda", "phi")));
        }
    }

    /// <summary>
    ///     Mock <c>GetEnvironmentVariable</c> by reproducing the logic to make it works on Windows and Linux
    ///     The test cases are: 
    ///     <list type="number">
    ///         <item><description>Testing when an environment variable doesn't exist the default value is returned</description></item>
    ///         <item><description>Testing when an environment variable exists</description></item>
    ///     </list>
    ///  </summary>
    public class MockEnvironmentVariable
    {
        private const string DefaultValue = "Default";
        private const string VariableName = "SharpmakeDoNotExists";
        private const string ExpectedVariableValue = "Variable exists";

        [Test]
        public void GetEnvironmentVariableNotExisting()
        {
            string output = null;

            Assert.AreEqual(DefaultValue, Util.GetEnvironmentVariable(VariableName, DefaultValue, ref output, true));
        }
        [Test]
        public void GetEnvironmentVariableExisting()
        {
            string output = null;
            Assert.AreEqual(DefaultValue, Util.GetEnvironmentVariable(VariableName, DefaultValue, ref output, true));
            Environment.SetEnvironmentVariable(VariableName, ExpectedVariableValue);
            output = null;
            Assert.AreEqual(ExpectedVariableValue, Util.GetEnvironmentVariable(VariableName, DefaultValue, ref output, true));
            Environment.SetEnvironmentVariable(VariableName, null);
            output = null;
            Assert.AreEqual(DefaultValue, Util.GetEnvironmentVariable(VariableName, DefaultValue, ref output, true));
        }
    }

    public class MockPath
    {
        /// <summary>
        ///     Verify that the path recovered their original format after being lowered.
        ///  </summary>
        [Test]
        public void PathGetCapitalizedFile()
        {
            var mockPath1 = Util.GetCapitalizedPath(Path.GetTempFileName());
            var mockPath2 = Util.GetCapitalizedPath(Path.GetTempFileName());
            OrderableStrings paths = new OrderableStrings
            {
                mockPath1,
                mockPath2
            };
            OrderableStrings pathsToLower = new OrderableStrings(paths);
            OrderableStrings pathsToUpper = new OrderableStrings(paths.Select((p) => p.ToUpper()));

            pathsToLower.ToLower();

            Assert.AreEqual(paths, Util.PathGetCapitalized(pathsToLower));
            Assert.AreEqual(paths, Util.PathGetCapitalized(pathsToUpper));

            File.Delete(mockPath1);
            File.Delete(mockPath2);
        }

        /// <summary>
        ///     Verify that the path recovered their original format after being lowered.
        ///  </summary>
        [Test]
        public void PathGetCapitalizedDirectory()
        {
            string temp = Util.GetCapitalizedPath(Path.GetTempPath());

            var tempDirectory1 = Directory.CreateDirectory(temp + @"\test1");
            var tempDirectory2 = Directory.CreateDirectory(temp + @"\test2");

            OrderableStrings paths = new OrderableStrings
            {
                tempDirectory1.FullName.Replace("test1", "TEST1"),
                tempDirectory2.FullName.Replace("test2", "TEST2")
            };

            OrderableStrings expectedOutput = new OrderableStrings
            {
                tempDirectory1.FullName,
                tempDirectory2.FullName
            };
            Assert.AreEqual(expectedOutput, Util.PathGetCapitalized(paths));

            Directory.Delete(tempDirectory1.FullName);
            Directory.Delete(tempDirectory2.FullName);
        }
    }

    public class MockFile
    {
        /// <summary>
        ///     Verify that it returns the name of the current file
        ///  </summary>
        [Test]
        public void GetCurrentSharpMakeFileInfo()
        {
            string[] listFileInfo = Util.GetCurrentSharpmakeFileInfo().FullName.Split('\\');

            Assert.AreEqual("UtilTest.cs", listFileInfo[listFileInfo.Length - 1]);
        }

        /// <summary>
        ///     Verify the right extensions are returned
        ///  </summary>
        [Test]
        public void GetTextTemplateDirectiveParam()
        {
            var mockPath = Path.GetTempFileName();

            File.WriteAllLines(mockPath, new[] { "<#@ output extension=\".txt\" #>", "<#@ log extension=\".dll\" #>" });

            Assert.AreEqual(".txt", Util.GetTextTemplateDirectiveParam(mockPath, "output", "extension"));
            Assert.AreEqual(".dll", Util.GetTextTemplateDirectiveParam(mockPath, "log", "extension"));

            File.Delete(mockPath);
        }

        /// <summary>
        ///     <c>FileWriteIfDifferentInternal</c> verify if the MemoryStream and the file have different values 
        ///     The test cases are: 
        ///     <list type="number">
        ///         <item><description>Testing when the file is readonly</description></item>
        ///         <item><description>Testing when the memorystream and the file are the same</description></item>
        ///         <item><description>Testing when the memorystream and the file are different</description></item>
        ///     </list>
        ///  </summary>
        [Test]
        public void FileWriteIfDifferentInternal()
        {
            var mockPath1 = Path.GetTempFileName();
            var mockPath2 = Path.GetTempFileName();
            var mockPath3 = Path.GetTempFileName();

            File.WriteAllLines(mockPath1, new[] { "test", "memory", "stream" });
            File.WriteAllLines(mockPath2, new[] { "test", "file", "wrap" });
            File.WriteAllLines(mockPath3, new[] { "test", "memory", "streams" });
            FileInfo fileInfo = new FileInfo(mockPath1);
            fileInfo.IsReadOnly = true;

            MemoryStream memoryStream1 = new MemoryStream(File.ReadAllBytes(mockPath1));
            MemoryStream memoryStream2 = new MemoryStream(File.ReadAllBytes(mockPath2));
            MemoryStream memoryStream3 = new MemoryStream(File.ReadAllBytes(mockPath3));

            Assert.False(Util.FileWriteIfDifferentInternal(fileInfo, memoryStream1, true));
            fileInfo.IsReadOnly = false;

            Assert.False(Util.FileWriteIfDifferentInternal(fileInfo, memoryStream1, true));
            Assert.True(Util.FileWriteIfDifferentInternal(fileInfo, memoryStream2, true));
            Assert.True(Util.FileWriteIfDifferentInternal(fileInfo, memoryStream3, true));

            fileInfo.Delete();
            File.Delete(mockPath1);
            File.Delete(mockPath2);
            File.Delete(mockPath3);
        }

        /// <summary>
        ///     Verify that the contains of the source mock file was copied in the destination mock file
        ///  </summary> 
        [Test]
        public void ForceCopy()
        {
            var mockPathSource = Path.GetTempFileName();
            var mockPathDest = Path.GetTempFileName();
            var mockPathExpected = Path.GetTempFileName();

            File.WriteAllLines(mockPathSource, new[] { "MockFile" });
            File.WriteAllBytes(mockPathExpected, File.ReadAllBytes(mockPathSource));
            Util.ForceCopy(mockPathSource, mockPathDest);
            Assert.AreEqual(File.ReadAllText(mockPathExpected), File.ReadAllText(mockPathDest));

            File.WriteAllLines(mockPathSource, new[] { "MockFile Test" });
            File.WriteAllBytes(mockPathExpected, File.ReadAllBytes(mockPathSource));
            Util.ForceCopy(mockPathSource, mockPathDest);
            Assert.AreEqual(File.ReadAllText(mockPathExpected), File.ReadAllText(mockPathDest));

            File.Delete(mockPathSource);
            File.Delete(mockPathDest);
            File.Delete(mockPathExpected);
        }

        /// <summary>
        ///     Verify that a normal file is delete
        ///  </summary> 
        [Test]
        public void TryDeleteFile()
        {
            var mockPath = Path.GetTempFileName();

            Assert.True(Util.TryDeleteFile(mockPath, true));

            File.Delete(mockPath);
        }

        /// <summary>
        ///     Verify that a read only file is not deleted
        ///     <remark>Changing ReadOnly attribute in the Linux pipeline doesn't work so the test is discard on Mono</remark>
        ///  </summary> 
        [Test]
        public void TryDeleteReadOnlyFile()
        {
            if (!Util.IsRunningInMono())
            {
                var mockPath = Path.GetTempFileName();
                var fileInfoWrap = new FileInfo(mockPath);
                fileInfoWrap.IsReadOnly = true;

                Assert.True(fileInfoWrap.IsReadOnly);

                Assert.False(Util.TryDeleteFile(mockPath));
                fileInfoWrap.IsReadOnly = false;
                fileInfoWrap.Delete();
                File.Delete(mockPath);
            }
        }

        /// <summary>
        ///     Verify that the custom message is returned
        ///  </summary> 
        [Test]
        public void GetCompleteExceptionMessage()
        {
            string expectedOutput = "Exception Message";
            Exception e = new Exception(expectedOutput);

            Assert.True(Util.GetCompleteExceptionMessage(e, "   ").Contains(expectedOutput));
        }
    }

    public class ProjectEnvironment
    {
        /// <summary>
        ///     Verify an exception is thrown on a not supported tool version
        ///  </summary>
        [Test]
        public void GetToolVersionStringException()
        {
            Assert.Catch<Exception>(() => Util.GetToolVersionString(DevEnv.xcode4ios));
            Assert.Catch<Exception>(() => Util.GetToolVersionString(DevEnv.eclipse));
            Assert.Catch<NotImplementedException>(() => Util.GetToolVersionString(DevEnv.make));
        }

        /// <summary>
        ///     Verify the right managed project platform name was returned depending on the platform and the project type
        ///  </summary>
        [Test]
        public void GetPlatformString()
        {
            Assert.AreEqual("x86", Util.GetPlatformString(Platform.win32, new CSharpProject(), null, false));
            Assert.AreEqual("x64", Util.GetPlatformString(Platform.win64, new CSharpProject(), null, false));
            Assert.AreEqual("AnyCPU", Util.GetPlatformString(Platform.win64, new PythonProject(), null, false));
            Assert.AreEqual("Any CPU", Util.GetPlatformString(Platform.win64, new PythonProject(), null, true));
            Assert.AreEqual("x64", Util.GetPlatformString(Platform.win64, new AndroidPackageProject(), null, true));
        }

        /// <summary>
        ///     Verify that an exception is thrown if a platform is not supported
        ///  </summary>
        [Test]
        public void GetPlatformStringException()
        {
            Assert.Catch<Exception>(() => Util.GetPlatformString(Platform.android, new CSharpProject(), null, false));
        }
    }

    public class FakeTree
    {
        [SetUp]
        public void Init()
        {
            Util.FakePathPrefix = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location).LocalPath);

            string[] files =
            {
                "./data/mod.el",
                "./code/test.h",
                "code/test.cpp",
                @".\code\main\main.cpp",
                "./code/test/stuff.cpp"
            };

            foreach (string filePath in files.Select(Util.PathMakeStandard))
            {
                Util.AddNewFakeFile(filePath, 0);
            }
        }

        [TearDown]
        public void Shutdown()
        {
            Util.ClearFakeTree();
        }

        [Test, Repeat(2)]
        public void KeepsACountOfFakeFiles()
        {
            // Repetition is to ensure Shutdown() is restoring the global context
            // and not adding each time the Setup() is done
            Assert.That(Util.CountFakeFiles(), Is.EqualTo(5));
        }

        [Test]
        public void CanEmulateDirectories()
        {
            var directory = Path.Combine(Util.FakePathPrefix, "code");
            Assert.That(Util.DirectoryExists(directory), Is.True);

            var subDirectory = Path.Combine(Util.FakePathPrefix, "code", "main");
            Assert.That(Util.DirectoryExists(subDirectory), Is.True);

            var missingDirectory = Path.Combine(Util.FakePathPrefix, "doesnotexist");
            Assert.That(Util.DirectoryExists(missingDirectory), Is.False);
        }

        [Test]
        public void IsCaseInsensitive()
        {
            var directoryLower = Path.Combine(Util.FakePathPrefix, "code");
            var directoryUpper = Path.Combine(Util.FakePathPrefix, "CODE");

            Assert.That(Util.DirectoryExists(directoryLower), Is.True);
            Assert.That(Util.DirectoryExists(directoryUpper), Is.True);
        }

        [Test]
        public void CanListDirectories()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "test"),
                Path.Combine(Util.FakePathPrefix, "code", "main")
            };

            var result = Util.DirectoryGetDirectories(Path.Combine(Util.FakePathPrefix, "code"));
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListDirectoriesWithFilter()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "data"),
                Path.Combine(Util.FakePathPrefix, "code")
            };

            var result = Util.DirectoryGetDirectories(Path.Combine(Util.FakePathPrefix), "*d*");
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListDirectoriesWithFilterAndSearchOption()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "data"),
                Path.Combine(Util.FakePathPrefix, "code"),
                Path.Combine(Util.FakePathPrefix, "code", "main"),
                Path.Combine(Util.FakePathPrefix, "code", "test")
            };

            var result = Util.DirectoryGetDirectories(Path.Combine(Util.FakePathPrefix), "????", SearchOption.AllDirectories);
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListFiles()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "test.h"),
                Path.Combine(Util.FakePathPrefix, "code", "test.cpp"),
                Path.Combine(Util.FakePathPrefix, "code", "main", "main.cpp"),
                Path.Combine(Util.FakePathPrefix, "code", "test", "stuff.cpp")
            };

            var result = Util.DirectoryGetFiles(Path.Combine(Util.FakePathPrefix, "code"));
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListFilesWithFilter()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "test.cpp"),
                Path.Combine(Util.FakePathPrefix, "code", "main", "main.cpp"),
                Path.Combine(Util.FakePathPrefix, "code", "test", "stuff.cpp")
            };

            var result = Util.DirectoryGetFiles(Path.Combine(Util.FakePathPrefix, "code"), "*.cpp");
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListFilesWithFilterAndSearchOption()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "test.cpp"),
            };

            var result = Util.DirectoryGetFiles(Path.Combine(Util.FakePathPrefix, "code"), "*.cpp", SearchOption.TopDirectoryOnly);
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListFilesInSubDirectory()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "main", "main.cpp")
            };

            var result = Util.DirectoryGetFiles(Path.Combine(Util.FakePathPrefix, "code", "main"));
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void TestPathWithWildcards()
        {
            Assert.IsTrue(Util.IsPathWithWildcards(Path.Combine("test", "*test", "test")));
            Assert.IsTrue(Util.IsPathWithWildcards(Path.Combine("test", "*test**", "test")));
            Assert.IsTrue(Util.IsPathWithWildcards(Path.Combine("test", "tes?t", "test")));
            Assert.IsTrue(Util.IsPathWithWildcards(Path.Combine("test", "tes??t", "test")));

            Assert.IsFalse(Util.IsPathWithWildcards(Path.Combine("test", "test", "test")));
        }

        [Test]
        public void ErrorListFileWithWildcards()
        {
            Assert.Catch<ArgumentException>(() => Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "test")));
        }

        [Test]
        public void CanListFileWithWildcards_WithDotDot1()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "test.h"),
                Path.Combine(Util.FakePathPrefix, "code", "test.cpp")
            };

            var result = Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "code", "inexistantFolder", "..", "test*"));
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListFileWithWildcards_WithDotDot2()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "test.cpp")
            };

            var result1 = Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "code", "????", "..", "*.cpp"));
            Assert.That(result1, Is.EquivalentTo(expected));

            var result2 = Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "code", "????", "..", "test.cpp"));
            Assert.That(result2, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListFileWithWildcards1()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "test.h"),
                Path.Combine(Util.FakePathPrefix, "code", "test.cpp")
            };

            var result = Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "code", "test*"));
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListFileWithWildcards2()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "main", "main.cpp"),
                Path.Combine(Util.FakePathPrefix, "code", "test", "stuff.cpp")
            };

            var result = Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "code", "*", "*.cpp"));
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListFileWithWildcards3()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "main", "main.cpp"),
                Path.Combine(Util.FakePathPrefix, "code", "test", "stuff.cpp")
            };

            var result = Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "c*de", "????", "*.cpp"));
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListFileWithWildcards4()
        {
            string[] expected =
            {
                Path.Combine(Util.FakePathPrefix, "code", "main", "main.cpp")
            };

            var result = Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "code", "*", "main.cpp"));
            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test]
        public void CanListFileWithWildcards_NoMatch()
        {
            // Last file doesn't exist in test folder
            Assert.IsEmpty(Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "c?de", "test", "main.cpp")));

            // No folder with only one character exist
            Assert.IsEmpty(Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "code", "?", "main.cpp")));

            // No file with only one character exist
            Assert.IsEmpty(Util.DirectoryGetFilesWithWildcards(Path.Combine(Util.FakePathPrefix, "code", "*", "?")));
        }
    }

    [TestFixture]
    public class ReferencePath
    {
        [Test]
        public void CanBeComputedFromOutputPath()
        {
            const string outputFileFullPath = @"F:\OnePath\With\Output\with\a\file.cs";
            const string outputPath = @"F:\OnePath\With\Output\";
            const string referencePath = @"F:\OnePath\With\Reference\";

            var referenceFileFullPath = outputFileFullPath.ReplaceHeadPath(outputPath, referencePath);

            Assert.That(referenceFileFullPath, Is.EqualTo(
                Util.PathMakeStandard(@"F:\OnePath\With\Reference\with\a\file.cs", false)));
        }

        [Test]
        public void IsCaseInsensitiveButPreservesCase()
        {
            const string outputFileFullPath = @"F:\OnePath\With\Output\with\a\File.cs";
            const string outputPath = @"f:\OnePath\with\output\";
            const string referencePath = @"F:\OnePath\with\Reference\";

            var referenceFileFullPath = outputFileFullPath.ReplaceHeadPath(outputPath, referencePath);

            Assert.That(referenceFileFullPath, Is.EqualTo(
                Util.PathMakeStandard(@"F:\OnePath\with\Reference\with\a\File.cs", false)));
        }

        [Test]
        public void AcceptsOutputPathWithoutTrailingSlash()
        {
            const string outputFileFullPath = @"F:\OnePath\With\Output\with\a\file.cs";
            const string outputPath = @"F:\OnePath\With\Output";
            const string referencePath = @"F:\OnePath\With\Reference\";

            var referenceFileFullPath = outputFileFullPath.ReplaceHeadPath(outputPath, referencePath);

            Assert.That(referenceFileFullPath, Is.EqualTo(
                Util.PathMakeStandard(@"F:\OnePath\With\Reference\with\a\file.cs", false)));
        }
    }

    [TestFixture]
    public class SymbolicLink
    {
        /// <summary>
        ///     <c>CreateSymbolicLink</c> create a symbolic link
        ///     The test cases are: 
        ///     <list type="number">
        ///         <item><description>Testing that a symbolic link is not created on a temporary directory pointing itself</description></item>
        ///         <item><description>Testing that a symbolic link is created on a temporary directory pointing an other directory</description></item>
        ///     </list>
        ///     <remark>Implementation of create symbolic links doesn't work on linux so this test is discard on Mono</remark>
        ///  </summary> 
        [Test]
        [Ignore("Test Broken")]
        public void CreateSymbolicLinkOnDirectory()
        {
            if (!Util.IsRunningInMono())
            {
                var tempDirectory1 = Directory.CreateDirectory(Path.GetTempPath() + Path.DirectorySeparatorChar + "test-source");
                var tempDirectory2 = Directory.CreateDirectory(Path.GetTempPath() + Path.DirectorySeparatorChar + "test-destination");

                Assert.False(Util.CreateSymbolicLink(Path.GetTempPath(), Path.GetTempPath(), true));
                Assert.False(tempDirectory1.Attributes.HasFlag(FileAttributes.ReparsePoint));
                Assert.True(Util.CreateSymbolicLink(tempDirectory1.FullName, tempDirectory2.FullName, true));
                Assert.True(tempDirectory1.Attributes.HasFlag(FileAttributes.ReparsePoint));

                Directory.Delete(tempDirectory1.FullName);
                Directory.Delete(tempDirectory2.FullName);
            }
        }

        /// <summary>
        ///     <c>IsSymbolicLink</c> verify if a file has a symbolic link
        ///     The test cases are: 
        ///     <list type="number">
        ///         <item><description>Testing that the method detected the absence of a symbolic link</description></item>
        ///         <item><description>Testing that the method detected the presence of a symbolic link</description></item>
        ///     </list>
        ///     <remark>Implementation of create symbolic links doesn't work on linux so this test is discard on Mono</remark>
        ///  </summary>
        [Test]
        [Ignore("Test Broken")]
        public void IsSymbolicLink()
        {
            if (!Util.IsRunningInMono())
            {
                var mockPath1 = Path.GetTempFileName();
                var mockPath2 = Path.GetTempFileName();

                Assert.False(Util.IsSymbolicLink(mockPath1));

                Assert.True(Util.CreateSymbolicLink(mockPath1, mockPath2, false));
                Assert.True(Util.IsSymbolicLink(mockPath1));

                File.Delete(mockPath1);
                File.Delete(mockPath2);
            }
        }
    }

    public class StringsOperations
    {
        /// <summary>
        ///     Verify that:
        ///     <list type="number">
        ///         <item><description>Verify that -1 is returned when two versions are different</description></item>
        ///         <item><description>Verify that 0 is returned when two versions are equal</description></item>
        ///         <item><description>Verify that 1 is returned when two versions are different</description></item>
        ///     </list>
        ///  </summary>
        [Test]
        public void VersionStringComparer()
        {
            var versionArray = new[] { "10.2.0", "10.2.9", "11.2.6" };

            IComparer<string> comparer = new Util.VersionStringComparer();

            Assert.AreEqual(-1, comparer.Compare(versionArray[0], versionArray[1]));
            Assert.AreEqual(1, comparer.Compare(versionArray[1], versionArray[0]));
            Assert.AreEqual(0, comparer.Compare(versionArray[0], versionArray[0]));
            Assert.AreEqual(-1, comparer.Compare(versionArray[1], versionArray[2]));
            Assert.AreEqual(1, comparer.Compare(versionArray[2], versionArray[0]));
        }

        /// <summary>
        ///     Verify that the separator was added between elements and the following characters were escaped: <, > and &
        ///  </summary>
        [Test]
        public void JoinStringsCollectionSeparator()
        {
            List<string> list1 = new List<string>()
            {
                "a",
                "b",
                "c",
                "d"
            };
            List<string> list2 = new List<string>()
            {
                "a&",
                "b<",
                "c>",
                "d"
            };

            Assert.AreEqual("a-b-c-d", Util.JoinStrings(list1, "-", false));
            Assert.AreEqual("a&amp;*b&lt;*c&gt;*d", Util.JoinStrings(list2, "*", true));
        }

        /// <summary>
        ///     Verify that the separator and prefix was added between elements and the following characters were escaped: <, > and &
        ///  </summary>
        [Test]
        public void JoinStringsCollectionSeparatorPrefix()
        {
            List<string> list1 = new List<string>()
            {
                "a",
                "b",
                "c",
                "d"
            };
            List<string> list2 = new List<string>()
            {
                "a&",
                "b<",
                "c>",
                "d"
            };

            Assert.AreEqual("prefixa-prefixb-prefixc-prefixd", Util.JoinStrings(list1, "-", "prefix", false));
            Assert.AreEqual("prefixa&amp;*prefixb&lt;*prefixc&gt;*prefixd", Util.JoinStrings(list2, "*", "prefix", true));
        }

        /// <summary>
        ///     Verify that the separator, suffix and prefix was added between elements and the following characters were escaped: <, > and &
        ///  </summary>
        [Test]
        public void JoinStringsCollectionSeparatorPrefixSuffix()
        {
            List<string> list1 = new List<string>()
            {
                "a",
                "b",
                "c",
                "d"
            };
            List<string> list2 = new List<string>()
            {
                "a&",
                "b<",
                "c>",
                "d"
            };

            Assert.AreEqual("prefixasuffix-prefixbsuffix-prefixcsuffix-prefixdsuffix", Util.JoinStrings(list1, "-", "prefix", "suffix", false));
            Assert.AreEqual("prefixa&amp;suffix*prefixb&lt;suffix*prefixc&gt;suffix*prefixdsuffix", Util.JoinStrings(list2, "*", "prefix", "suffix", true));
        }

        /// <summary>
        ///     Verify that two paths with different separator and same separator are still considered equal and different path is not considered equal
        ///  </summary>
        [Test]
        public void PathIsSameDifferentSeparator()
        {
            var path1 = @"C:\Windows\System32\cmd.exe";
            var path2 = @"C:/Windows/System32/cmd.exe";
            var path3 = @"C:\Windows\local\cmd.exe";

            Assert.True(Util.PathIsSame(path1, path2));
            Assert.True(Util.PathIsSame(path1, path1));
            Assert.False(Util.PathIsSame(path3, path2));
            Assert.True(Util.PathIsSame(path2, path2));
        }

        /// <summary>
        ///     Verify that equal string return an empty result and unequal string returns a string with the different properties info
        ///  </summary>
        [Test]
        public void MakeDifferenceString()
        {
            ITarget target1 = new Target(Platform.win64, DevEnv.vs2017, Optimization.Release, OutputType.Dll, Blob.Blob, BuildSystem.FastBuild, DotNetFramework.v3_5);
            ITarget target2 = new Target(Platform.win64, DevEnv.vs2017, Optimization.Release, OutputType.Dll, Blob.Blob, BuildSystem.FastBuild, DotNetFramework.v3_5);
            ITarget target3 = new Target(Platform.win64, DevEnv.vs2017, Optimization.Debug, OutputType.Dll, Blob.Blob, BuildSystem.FastBuild, DotNetFramework.v4_5);

            Assert.True(Util.MakeDifferenceString(target1, target2).Length == 0);
            Assert.True(Util.MakeDifferenceString(target1, target3).Contains("\"net35\" and \"net45\""));
            Assert.True(Util.MakeDifferenceString(target1, target3).Contains("\"Release\" and \"Debug\""));
        }

        /// <summary>
        ///     Verify that the resulted path of <c>EnsureTrailingSeparator</c> only have one separator at the end
        ///  </summary>
        [Test]
        public void EnsureTrailingSeparator()
        {
            var separator = Path.DirectorySeparatorChar;
            List<string> paths = new List<string>{ Util.PathMakeStandard(@"c:\windows\system32\cmd.exe"),
                                                   Util.PathMakeStandard(@"c:\windows\system32\cmd.exe")+separator,
                                                   Util.PathMakeStandard(@"c:\windows\system32\cmd.exe")+separator+separator};
            string expectedOutputPath = Util.PathMakeStandard(@"c:\windows\system32\cmd.exe\") + separator;

            var results = paths.Select((p) => Util.EnsureTrailingSeparator(p));

            Assert.True(results.All((p) => p.Equals(expectedOutputPath)));
        }

        /// <summary>
        ///     Verify that the returned file is the absolute path with the case that return a list
        ///  </summary>
        [Test]
        public void PathGetAbsoluteStringsReturnList()
        {
            var mockPath = Path.GetTempFileName();
            string filename = Path.GetFileName(mockPath);
            string stringsSource = Path.GetDirectoryName(mockPath);

            Assert.AreEqual(Path.Combine(stringsSource.ToLower(), filename), Util.PathGetAbsolute(stringsSource, new Strings(filename))[0]);

            File.Delete(mockPath);
        }

        /// <summary>
        ///     Verify that the returned file is the absolute path with the case that return a string
        ///  </summary>
        [Test]
        public void PathGetAbsoluteStringsReturnString()
        {
            var mockPath = Path.GetTempFileName();
            var separator = Path.DirectorySeparatorChar;
            string stringsSource = mockPath.Substring(0, mockPath.IndexOf(separator));
            string stringsDest = mockPath.Substring(mockPath.IndexOf(separator, mockPath.IndexOf(separator) + 1));
            string expectedOutputPath = stringsSource.ToLower() + stringsDest;
            //v
            Assert.AreEqual(expectedOutputPath, Util.PathGetAbsolute(stringsSource, stringsDest));

            File.Delete(mockPath);
        }

        /// <summary>
        ///     Verify the root was added to the relative path from a Strings
        ///  </summary>
        [Test]
        public void ResolvePathString()
        {
            var mockPath = Path.GetTempFileName();
            Strings paths = new Strings(Path.GetDirectoryName(mockPath));
            string root = mockPath.Substring(0, mockPath.IndexOf(Path.DirectorySeparatorChar));
            Strings expectedOutputPath = new Strings(paths.Select((p) => Path.Combine(root, p)));
            expectedOutputPath.ToLower();

            Util.ResolvePath(root, ref paths);

            Assert.AreEqual(expectedOutputPath, paths);

            File.Delete(mockPath);
        }

        /// <summary>
        ///     Verify the root was added to the relative path from an OrderableStrings
        ///  </summary>
        [Test]
        public void ResolvePathOrderableString()
        {
            var mockPath1 = Path.GetTempFileName();
            var mockPath2 = Path.GetTempFileName();
            var mockPath3 = Path.GetTempFileName();
            OrderableStrings paths = new OrderableStrings
            {
                mockPath1.Substring(mockPath1.IndexOf(Path.DirectorySeparatorChar)),
                mockPath2.Substring(mockPath2.IndexOf(Path.DirectorySeparatorChar)),
                mockPath3.Substring(mockPath3.IndexOf(Path.DirectorySeparatorChar)),
            };
            string root = mockPath1.Substring(0, mockPath1.IndexOf(Path.DirectorySeparatorChar));
            OrderableStrings expectedOutputPath = new OrderableStrings(paths.Select((p) => (root + p).ToLower()));

            Util.ResolvePath(root, ref paths);
            expectedOutputPath.Sort();
            paths.Sort();

            Assert.AreEqual(expectedOutputPath.ToString(), paths.ToString());

            File.Delete(mockPath1);
            File.Delete(mockPath2);
            File.Delete(mockPath3);
        }

        /// <summary>
        ///     Verify that the common path was kept from the intersection
        ///  </summary>
        [Test]
        public void GetPathIntersection()
        {
            string[] pathA = { "f:" + Path.DirectorySeparatorChar, "sharpmake", "sharpmake", "sharpmake.generators", "generic", "makeapplication.cs" };
            string[] pathB = { "f:" + Path.DirectorySeparatorChar, "sharpmake", "sharpmake", "sharpmake.generators", "properties", "assemblyinfo.cs" };
            string[] expectedOutputPath = { "f:" + Path.DirectorySeparatorChar, "sharpmake", "sharpmake", "sharpmake.generators" };

            Assert.AreEqual(Path.Combine(expectedOutputPath) + Path.DirectorySeparatorChar,
                            Util.GetPathIntersection(Path.Combine(pathA),
                            Path.Combine(pathB)));
        }

        /// <summary>
        ///     Verify if the relative path to the destination directory is correct from the source path
        ///  </summary>
        [Test]
        public void PathGetRelativeStrings()
        {
            Strings stringsDest = new Strings(Util.PathMakeStandard(@"C:\Windows\local\cmd.exe"));
            string stringsSource = Util.PathMakeStandard(@"C:\Windows\System32\cmd.exe");
            string expectedString = Util.PathMakeStandard(@"..\..\local\cmd.exe");

            Assert.AreEqual(expectedString, Util.PathGetRelative(stringsSource, stringsDest, false)[0]);
        }

        /// <summary>
        ///     Verify if the relative path to the destination directory is correct from the source path
        ///  </summary>
        [Test]
        public void PathGetRelativeOrderableStrings()
        {
            string stringsSource = Util.PathMakeStandard(@"F:\SharpMake\sharpmake\Sharpmake.Platforms");
            OrderableStrings stringsDest = new OrderableStrings
            {
                @"F:\SharpMake\sharpmake\Sharpmake.Generators\Generic",
                @"F:\SharpMake\sharpmake\Sharpmake.Platforms\subdir\test.txt",
                @"F:\SharpMake\sharpmake\Sharpmake.Platforms\test2.txt"
            };
            Util.PathMakeStandard(stringsDest);
            OrderableStrings listResult = Util.PathGetRelative(stringsSource, stringsDest, false);

            Assert.AreEqual(Util.PathMakeStandard(@"..\Sharpmake.Generators\Generic", !Util.IsRunningInMono()), listResult[0]);
            Assert.AreEqual(Util.PathMakeStandard(@"subdir\test.txt", !Util.IsRunningInMono()), listResult[1]);
            Assert.AreEqual("test2.txt", listResult[2]);
        }

        /// <summary>
        ///     Verify if the relative path to the destination directory is correct from the source path
        ///  </summary>
        [Test]
        public void PathGetRelativeOrderableIEnumerable()
        {
            string stringsSource = Util.PathMakeStandard(@"F:\SharpMake\sharpmake\Sharpmake.Generators\Apple");
            List<string> stringsDest = new List<string>()
            {
                @"F:\SharpMake\sharpmake\Sharpmake.Generators\Generic",
                @"F:\SharpMake\sharpmake\Sharpmake.Generators\Properties"
            };
            Util.PathMakeStandard(stringsDest);

            var result = Util.PathGetRelative(stringsSource, stringsDest, false);
            Assert.AreEqual(Util.PathMakeStandard(@"..\Generic"), result[0]);
            Assert.AreEqual(Util.PathMakeStandard(@"..\Properties"), result[1]);
        }

        /// <summary>
        ///     Verify that the path and the file's name were separated
        ///  </summary>
        [Test]
        public void PathSplitFileNameFromPath()
        {
            string filePath = Util.PathMakeStandard(@"F:\SharpMake\sharpmake\Sharpmake.Generators\Generic");
            string fileName = "MakeProject.cs";
            string fileNameResult, pathNameResult;

            Util.PathSplitFileNameFromPath(Path.Combine(filePath, fileName), out fileNameResult, out pathNameResult);

            Assert.AreEqual(fileName, fileNameResult);
            Assert.AreEqual(filePath, pathNameResult);
        }

        /// <summary>
        ///     Verify that the tab of string was processed to a string with the format of a path
        ///     <remark><c>RegexPathCombine</c> return <c>F:\\\\SharpMake\\\\sharpmake\\\\Sharpmake.Generators\\\\Generic\\\\MakeProject.cs</c> two tests case were needed</remark>
        ///  </summary>
        [Test]
        public void RegexPathCombineString()
        {
            string[] pathParams1 = { "F:", "SharpMake", "sharpmake", "Sharpmake.Generators", "Generic", "MakeProject.cs" };
            string[] pathParams2 = { "F:", "SharpMake", "sharpmake", "Sharpmake.Generators", "Properties", "AssemblyInfo.cs" };
            string[] pathParams3 = { "F:", "SharpMake", "sharpmake", "tmp", "obj", "debug", "configureorder", "ConfigureOrder.dll" };

            if (Util.IsRunningInMono())
            {
                Assert.AreEqual(Path.Combine(pathParams1), Util.RegexPathCombine(pathParams1));
                Assert.AreEqual(Path.Combine(pathParams2), Util.RegexPathCombine(pathParams2));
                Assert.AreEqual(Path.Combine(pathParams3), Util.RegexPathCombine(pathParams3));
            }
            else
            {
                Assert.AreEqual(string.Join(@"\\", pathParams1), Util.RegexPathCombine(pathParams1));
                Assert.AreEqual(string.Join(@"\\", pathParams2), Util.RegexPathCombine(pathParams2));
                Assert.AreEqual(string.Join(@"\\", pathParams3), Util.RegexPathCombine(pathParams3));
            }
        }

        /// <summary>
        ///     Verify that the relative path is converted to an absolute path
        ///  </summary>
        [Test]
        public void GetConvertedRelativePathRoot()
        {
            var mockPath = Path.GetTempFileName();
            var absolutePath = Path.GetDirectoryName(mockPath);
            var fileName = Path.GetFileName(mockPath);

            var root = @"C:\SharpMake\sharpmake\Sharpmake.Application\Properties";

            var newRelativeToFullPath = "";
            Assert.AreEqual(Path.Combine(absolutePath.ToLower(), fileName), Util.GetConvertedRelativePath(absolutePath, fileName, newRelativeToFullPath, false, null));
            Assert.AreEqual(mockPath, Util.GetConvertedRelativePath(absolutePath, mockPath, newRelativeToFullPath, false, root));
            Assert.AreEqual(Path.Combine(absolutePath.ToLower(), fileName), Util.GetConvertedRelativePath(absolutePath, fileName, newRelativeToFullPath, false, ""));
            Assert.AreEqual(absolutePath, Util.GetConvertedRelativePath(absolutePath, null, newRelativeToFullPath, false, null));
            Assert.AreEqual(Path.Combine(root.ToLower(), Path.GetTempPath()), Util.GetConvertedRelativePath(root, Path.GetTempPath(), newRelativeToFullPath, false, null));

            File.Delete(mockPath);
        }

        /// <summary>
        ///     Verify these tests case:
        /// <returns>
        /// <list type="number">
        ///     <item><description>if they are both referring to file edited by sharpmake user (.sharpmake): concatenation of both separated by a line return</description></item>
        ///     <item><description>if only callerInfo2 refer to file edited by sharpmake user (.sharpmake): callerInfo2</description></item>
        ///     <item><description>otherwise: callerInfo1</description></item>
        /// </list>
        /// </returns>
        /// </summary>  
        [Test]
        public void PickOrConcatCallerInfo()
        {
            var callerInfo1 = "solution.sharpmake";
            var callerInfo2 = "solution.cs";
            var callerInfo3 = "application.sharpmake";

            Assert.AreEqual(callerInfo1, Util.PickOrConcatCallerInfo(callerInfo1, callerInfo2));
            Assert.AreEqual(callerInfo1 + Environment.NewLine + callerInfo3, Util.PickOrConcatCallerInfo(callerInfo1, callerInfo3));
            Assert.AreEqual(callerInfo3, Util.PickOrConcatCallerInfo(callerInfo2, callerInfo3));
        }

        [Test]
        public void ContainsIsCaseInsensitive()
        {
            Strings list = new Strings();
            list.Add("SOMETHING");
            Assert.That(list.Contains("SOMETHING"), Is.EqualTo(list.Contains("something")));

            list.Add("something");
            Assert.That(list.Count, Is.EqualTo(1));
        }
    }
}
