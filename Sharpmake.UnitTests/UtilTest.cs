// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;

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
        public void ThrowWhenPathIsNull()
        {
            string nullPath = null;

            Assert.Catch<ArgumentNullException>(() => Util.PathMakeStandard(nullPath));
        }

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
        public void LeaveUnixRootPathUntouched()
        {
            var notFullyQualifiedUnixPath = "MountedDiskName:";
            var fullyQualifiedRoot = Path.DirectorySeparatorChar.ToString();

            Assert.AreEqual(fullyQualifiedRoot, Util.PathMakeStandard(fullyQualifiedRoot));

            // Check case sensitivness on Unix 
            if (!Util.IsRunningOnUnix())
                notFullyQualifiedUnixPath = notFullyQualifiedUnixPath.ToLower();

            Assert.AreEqual(notFullyQualifiedUnixPath, Util.PathMakeStandard(notFullyQualifiedUnixPath));
        }

        [Test]
        public void LeaveDriveRelativePathAsNotFullyQualified()
        {
            // For information about what is a drive relative path please check https://learn.microsoft.com/en-us/dotnet/standard/io/file-path-formats

            var expectedResult = Path.Combine("d:toto", "tata");
            var driveRelativePath = @"d:toto\tata\";
            var fullyQualifiedPath = Path.Combine("d:", "toto", "tata");

            Assert.AreEqual(expectedResult, Util.PathMakeStandard(driveRelativePath));
            Assert.AreNotEqual(fullyQualifiedPath, Util.PathMakeStandard(driveRelativePath));
        }

        [Test]
        public void ReturnFullyQualifiedRootPathOnWindows()
        {
            if (!Util.IsRunningOnUnix())
            {
                var notFullyQualifiedRoot = "d:";
                var fullyQualifiedRoot = @"d:\";

                Assert.AreEqual(fullyQualifiedRoot, Util.PathMakeStandard(notFullyQualifiedRoot));
                Assert.AreEqual(fullyQualifiedRoot, Util.PathMakeStandard(fullyQualifiedRoot));
            }
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
        [Test]
        public void ThrowsOnInvalidNameWithOnlyDots()
        {
            Assert.Throws<ArgumentException>(() => Util.SimplifyPath(".../sharpmake/README.md"));

            Assert.Throws<ArgumentException>(() => Util.SimplifyPath("sharpmake/..../README.md"));

            Assert.Throws<ArgumentException>(() => Util.SimplifyPath("sharpmake/..."));
        }

        [Test]
        public void LeavesEmptyStringsUntouched()
        {
            Assert.That(Util.SimplifyPath(""),
                Is.EqualTo(""));
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
        public void HandlesDotsInFileNameOrFolder()
        {
            Assert.That(Util.SimplifyPath(@".test.cpp"),
                Is.EqualTo(".test.cpp"));

            Assert.That(Util.SimplifyPath(@"test.cpp."),
                Is.EqualTo("test.cpp."));

            Assert.That(Util.SimplifyPath(@"test.cpp.."),
                Is.EqualTo("test.cpp.."));

            Assert.That(Util.SimplifyPath(@"test.cpp..."),
                Is.EqualTo("test.cpp..."));

            Assert.That(Util.SimplifyPath(@".test\.test.cpp"),
                Is.EqualTo(Path.Combine(".test", ".test.cpp")));

            Assert.That(Util.SimplifyPath(@"test.\test.cpp."),
                Is.EqualTo(Path.Combine("test.", "test.cpp.")));

            Assert.That(Util.SimplifyPath(@"..test\..test.cpp"),
                Is.EqualTo(Path.Combine("..test", "..test.cpp")));

            Assert.That(Util.SimplifyPath(@"test..\test.cpp.."),
                Is.EqualTo(Path.Combine("test..", "test.cpp..")));

            Assert.That(Util.SimplifyPath(@"...test\...test.cpp"),
                Is.EqualTo(Path.Combine("...test", "...test.cpp")));

            Assert.That(Util.SimplifyPath(@"test...\test.cpp..."),
                Is.EqualTo(Path.Combine("test...", "test.cpp...")));
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
            Assert.That(Util.SimplifyPath("c:\\main/test//t.cpp"),
                Is.EqualTo(Path.Combine("c:", "main", "test", "t.cpp")));

            Assert.That(Util.SimplifyPath("c:\\"),
                Is.EqualTo("c:\\"));

            Assert.That(Util.SimplifyPath("/main/test/t.cpp"),
                Is.EqualTo(Path.Combine("/", "main", "test", "t.cpp")));

            Assert.That(Util.SimplifyPath("/"),
                Is.EqualTo("/"));
        }

        [Test]
        public void LeaveTrailingPathSeparator()
        {
            Assert.That(Util.SimplifyPath(@"alpha\beta\"),
                Is.EqualTo(Path.Combine("alpha", $"beta{Path.DirectorySeparatorChar}")));

            Assert.That(Util.SimplifyPath(@"alpha\beta\\"),
                Is.EqualTo(Path.Combine("alpha", $"beta{Path.DirectorySeparatorChar}")));
        }

        [Test]
        public void HandlesFolderParentsAtTheEnd()
        {
            Assert.That(Util.SimplifyPath(@"a\b\c\..\.."),
                Is.EqualTo("a"));

            Assert.That(Util.SimplifyPath(@"a\b\c\..\..\"),
                Is.EqualTo($"a{Path.DirectorySeparatorChar}"));
        }

        [Test]
        public void LeavesCleanPathUntouched()
        {
            // Check that we do not change dot and dot dot
            Assert.That(Util.SimplifyPath("."), Is.EqualTo("."));
            Assert.That(Util.SimplifyPath(".."), Is.EqualTo(".."));

            Assert.That(Util.SimplifyPath(@"alpha\beta\gamma\sigma\omega\zeta\lambda\phi"),
                Is.EqualTo(Path.Combine("alpha", "beta", "gamma", "sigma", "omega", "zeta", "lambda", "phi")));
        }
    }

    public class PathGetRelative
    {
        [Test]
        public void PathGetRelative_Highter()
        {
            Assert.That(Util.PathGetRelative("c:/folder1/folder2",  "c:/folder1"),  Is.EqualTo(".."));
            Assert.That(Util.PathGetRelative("c:/folder1/folder2/", "c:/folder1"),  Is.EqualTo(".."));
            Assert.That(Util.PathGetRelative("c:/folder1/folder2",  "c:/folder1/"), Is.EqualTo(".."));
            Assert.That(Util.PathGetRelative("c:/folder1/folder2/", "c:/folder1/"), Is.EqualTo(".."));

            Assert.That(Util.PathGetRelative("c:/folder1/folder2/folder3",  "c:/folder1"),  Is.EqualTo(Path.Combine("..", "..")));
            Assert.That(Util.PathGetRelative("c:/folder1/folder2/folder3/", "c:/folder1"),  Is.EqualTo(Path.Combine("..", "..")));
            Assert.That(Util.PathGetRelative("c:/folder1/folder2/folder3",  "c:/folder1/"), Is.EqualTo(Path.Combine("..", "..")));
            Assert.That(Util.PathGetRelative("c:/folder1/folder2/folder3/", "c:/folder1/"), Is.EqualTo(Path.Combine("..", "..")));
        }

        [Test]
        public void PathGetRelative_Deeper()
        {
            Assert.That(Util.PathGetRelative("c:/folder1",  "c:/folder1/folder2"),  Is.EqualTo("folder2"));
            Assert.That(Util.PathGetRelative("c:/folder1/", "c:/folder1/folder2"),  Is.EqualTo("folder2"));
            Assert.That(Util.PathGetRelative("c:/folder1",  "c:/folder1/folder2/"), Is.EqualTo("folder2")); // standard keep the last dirSep != sharpmake that remove it
            Assert.That(Util.PathGetRelative("c:/folder1/", "c:/folder1/folder2/"), Is.EqualTo("folder2")); // standard keep the last dirSep != sharpmake that remove it

            Assert.That(Util.PathGetRelative("c:/folder1",  "c:/folder1/folder2/folder3"),  Is.EqualTo(Path.Combine("folder2", "folder3")));
            Assert.That(Util.PathGetRelative("c:/folder1/", "c:/folder1/folder2/folder3"),  Is.EqualTo(Path.Combine("folder2", "folder3")));
            Assert.That(Util.PathGetRelative("c:/folder1",  "c:/folder1/folder2/folder3/"), Is.EqualTo(Path.Combine("folder2", "folder3"))); // standard keep the last dirSep != sharpmake that remove it
            Assert.That(Util.PathGetRelative("c:/folder1/", "c:/folder1/folder2/folder3/"), Is.EqualTo(Path.Combine("folder2", "folder3"))); // standard keep the last dirSep != sharpmake that remove it
        }

        [Test]
        public void PathGetRelativeSimple_Parallel()
        {
            Assert.That(Util.PathGetRelative("c:/folder1",  "c:/folder2"),  Is.EqualTo(Path.Combine("..", "folder2")));
            Assert.That(Util.PathGetRelative("c:/folder1/", "c:/folder2"),  Is.EqualTo(Path.Combine("..", "folder2")));
            Assert.That(Util.PathGetRelative("c:/folder1",  "c:/folder2/"), Is.EqualTo(Path.Combine("..", "folder2"))); // standard keep the last dirSep != sharpmake that remove it
            Assert.That(Util.PathGetRelative("c:/folder1/", "c:/folder2/"), Is.EqualTo(Path.Combine("..", "folder2"))); // standard keep the last dirSep != sharpmake that remove it

            Assert.That(Util.PathGetRelative("c:/folder1/folderA",  "c:/folder2/folderB"),  Is.EqualTo(Path.Combine("..", "..", "folder2", "folderB")));
            Assert.That(Util.PathGetRelative("c:/folder1/folderA/", "c:/folder2/folderB"),  Is.EqualTo(Path.Combine("..", "..", "folder2", "folderB")));
            Assert.That(Util.PathGetRelative("c:/folder1/folderA",  "c:/folder2/folderB/"), Is.EqualTo(Path.Combine("..", "..", "folder2", "folderB"))); // standard keep the last dirSep != sharpmake that remove it
            Assert.That(Util.PathGetRelative("c:/folder1/folderA/", "c:/folder2/folderB/"), Is.EqualTo(Path.Combine("..", "..", "folder2", "folderB"))); // standard keep the last dirSep != sharpmake that remove it
        }

        [Test]
        public void PathGetRelative_Same()
        {
            Assert.That(Util.PathGetRelative("c:/folder1",  "c:/folder1"),  Is.EqualTo("."));
            Assert.That(Util.PathGetRelative("c:/folder1/", "c:/folder1"),  Is.EqualTo("."));
            Assert.That(Util.PathGetRelative("c:/folder1",  "c:/folder1/"), Is.EqualTo("."));
            Assert.That(Util.PathGetRelative("c:/folder1/", "c:/folder1/"), Is.EqualTo("."));

            Assert.That(Util.PathGetRelative("c:/", "c:/"), Is.EqualTo("."));
            Assert.That(Util.PathGetRelative("/",   "/"),   Is.EqualTo("."));
        }

        [Test]
        public void PathGetRelative_DifferentRoot()
        {
            Assert.That(Util.PathGetRelative("c:/folder1",  "d:/folder2"),  Is.EqualTo(Path.Combine("d:", "folder2")));
            Assert.That(Util.PathGetRelative("c:/folder1/", "d:/folder2"),  Is.EqualTo(Path.Combine("d:", "folder2")));
            Assert.That(Util.PathGetRelative("c:/folder1",  "d:/folder2/"), Is.EqualTo(Path.Combine("d:", $"folder2{Path.DirectorySeparatorChar}")));
            Assert.That(Util.PathGetRelative("c:/folder1/", "d:/folder2/"), Is.EqualTo(Path.Combine("d:", $"folder2{Path.DirectorySeparatorChar}")));
        }

        [Test]
        public void PathGetRelative_Tricky()
        {
            // Names are halfway the same (common part must be properly computed)
            Assert.That(Util.PathGetRelative("c:/abc",     "c:/abx/folder"), Is.EqualTo(Path.Combine("..", "abx", "folder")));
            Assert.That(Util.PathGetRelative("c:/abc",     "c:/abc_"),       Is.EqualTo(Path.Combine("..", "abc_")));
            Assert.That(Util.PathGetRelative("c:/abc_",    "c:/abc"),        Is.EqualTo(Path.Combine("..", "abc")));
            Assert.That(Util.PathGetRelative("c:/abc_def", "c:/abc_xyz"),    Is.EqualTo(Path.Combine("..", "abc_xyz")));

            // One character names
            Assert.That(Util.PathGetRelative("c:/1/2/3", "c:/1"),     Is.EqualTo(Path.Combine("..", "..")));
            Assert.That(Util.PathGetRelative("c:/1/",    "c:/1/2/3"), Is.EqualTo(Path.Combine("2", "3")));
        }

        [Test]
        public void PathGetRelative_Invalid()
        {
            // Not rooted
            Assert.That(Util.PathGetRelative("c:/folder1", "folder2"), Is.EqualTo("folder2")); // Should probably throw as not rooted
            Assert.That(Util.PathGetRelative("folder1", "c:/folder2"), Is.EqualTo(Path.Combine("c:", "folder2"))); // Should probably throw as not rooted

            // Empty
            Assert.That(Util.PathGetRelative("", "c:/folder2"), Is.EqualTo(Path.Combine("c:", "folder2"))); // Should probably throw as empty
            Assert.That(Util.PathGetRelative("c:/folder2", ""), Is.EqualTo("")); // Should probably throw as empty

            // Null
            Assert.Throws<NullReferenceException>(() => Util.PathGetRelative(null, "c:/folder2"));
            Assert.Throws<NullReferenceException>(() => Util.PathGetRelative("c:/folder2", (string)null));
        }


        [Test]
        public void PathGetRelative_IgnoreCase()
        {
            Assert.Inconclusive("The implementation expose a 'ignoreCase' argument, but never use it (it always ignore the case even with ignoreCase == false)");

            Assert.That(Util.PathGetRelative("c:/folder1/folder2", "c:/Folder1", ignoreCase: true),  Is.EqualTo(".."));
            Assert.That(Util.PathGetRelative("c:/folder1/folder2", "c:/Folder1", ignoreCase: false), Is.EqualTo(Path.Combine("..", "..", "Folder1"))); // ori always ignore case, whatever the user ask

            Assert.That(Util.PathGetRelative("c:/folder1", "C:/folder1", ignoreCase: true),  Is.EqualTo("."));
            Assert.That(Util.PathGetRelative("c:/folder1", "C:/folder1", ignoreCase: false), Is.EqualTo(Path.Combine("C:", "folder1"))); // ori always ignore case, whatever the user ask
        }

        [Test]
        public void PathGetRelativeStrings()
        {
            Strings stringsDest = new Strings(Util.PathMakeStandard(@"C:\Windows\local\cmd.exe"));
            string stringsSource = Util.PathMakeStandard(@"C:\Windows\System32\cmd.exe");
            string expectedString = Util.PathMakeStandard(@"..\..\local\cmd.exe");

            Assert.AreEqual(expectedString, Util.PathGetRelative(stringsSource, stringsDest, false)[0]);
        }

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
    }

    public class FindCommonRootPath
    {
        [Test]
        public void NullIfEmpty()
        {
            Assert.IsNull(Util.FindCommonRootPath(Enumerable.Empty<string>()));
        }

        [Test]
        public void EmptyString()
        {
            Assert.IsNull(Util.FindCommonRootPath(new[] { "" }));
            Assert.IsNull(Util.FindCommonRootPath(new[] { "", "" }));
        }

        [Test]
        public void DifferentDrives()
        {
            Assert.IsNull(Util.FindCommonRootPath(new[] {
                @"C:\bla",
                @"D:\bla"
            }));
        }

        [Test]
        public void OnlyRoot()
        {
            Assert.AreEqual(Util.PathMakeStandard("/"), Util.FindCommonRootPath(new[] {"/bla", "/bli"}));
            Assert.AreEqual(Util.PathMakeStandard(@"c:\"), Util.FindCommonRootPath(new[] {@"c:\bla", @"c:\bli"}));
        }

        [Test]
        public void SameDir()
        {
            Assert.AreEqual(
                Util.PathMakeStandard(@"C:\bla"),
                Util.FindCommonRootPath(new[] {
                    @"C:\bla",
                    @"C:\bla"
                })
            );

            Assert.AreEqual(
                Util.PathMakeStandard(@"C:\bla"),
                Util.FindCommonRootPath(new[] {
                    @"C:\bla\",
                    @"C:\bla"
                })
            );

            Assert.AreEqual(
                Util.PathMakeStandard(@"C:\bla"),
                Util.FindCommonRootPath(new[] {
                    @"C:\\bla\",
                    @"C:\bla"
                })
            );

            Assert.AreEqual(
                Util.PathMakeStandard(@"C:\bla"),
                Util.FindCommonRootPath(new[] {
                    @"C:\bla",
                    @"C:\bla",
                    @"C:\bla",
                    @"C:\bla"
                })
            );
        }

        [Test]
        public void LongPaths()
        {
            Assert.AreEqual(
                Util.PathMakeStandard(@"C:\a\b\c\d"),
                Util.FindCommonRootPath(new[] {
                    @"C:\a\b\c\d\e\f",
                    @"C:\a\b\c\d\file1.ext",
                    @"C:\a\b\c\d\file2.ext",
                    @"C:\a\b\c\d\e\file3.ext",
                    @"C:\a\b\c\d\e\f\g\h\file4.ext",
                })
            );

            // check that it's case sensitive on unix and not on windows
            if (Util.IsRunningOnUnix())
            {
                Assert.AreEqual(
                    Util.PathMakeStandard(@"/a/b"),
                    Util.FindCommonRootPath(new[] {
                        @"/a/b/c/d/e/f",
                        @"/a/b/C/d/file1.ext",
                        @"/a/b/c/d/file2.ext",
                        @"/a/b/c/D/E/file3.ext",
                        @"/a/b/c/d/e/f/g/h/file4.ext",
                    })
                );
            }
            else
            {
                Assert.AreEqual(
                    Util.PathMakeStandard(@"C:\a\b\c\d"),
                    Util.FindCommonRootPath(new[] {
                        @"C:\a\B\c\d\e\f",
                        @"C:\a\b\c\d\file1.ext",
                        @"C:\a\b\C\d\file2.ext",
                        @"C:\a\b\c\D\E\file3.ext",
                        @"C:\a\b\c\d\e\f\g\h\file4.ext",
                    })
                );
            }
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

    public class TestFileOperations
    {
        // 
        /// <summary>
        /// Prepare a random memory stream with random data for tests
        /// </summary>
        /// <param name="size">size </param>
        /// <param name="seed">see of random generator</param>
        /// <param name="modificationOffset">offset for some modifications of a random subset span in the stream. -1= Not used</param>
        /// <param name="modificationSize">Size of the modified span. Must be bigger than 0 when offset is different than -1 </param>
        /// <returns>the new stream</returns>
        private MemoryStream PrepareMemoryStream(int size, int seed, int modificationOffset, int modificationSize)
        {
            Random r = new Random(seed);
            MemoryStream s = new MemoryStream();
            {
                byte[] buffer = new byte[size + 1];
                r.NextBytes(buffer);
                int sizeToWrite = size;

                // optionally apply some modifications to the buffer
                if (modificationOffset != -1)
                {
                    Trace.Assert(modificationSize > 0);
                    r.NextBytes(new Span<byte>(buffer, modificationOffset, modificationSize));
                }

                // Write to stream
                s.Write(buffer, 0, sizeToWrite);
            }
            return s;
        }

        /// <summary>
        /// Fill a file with random data for tests.
        /// </summary>
        /// <param name="filePath">file path to write</param>
        /// <param name="size">size of file</param>
        /// <param name="seed">seed of random generator</param>
        private void PrepareFile(string filePath, int size, int seed)
        {
            using (var s = PrepareMemoryStream(size, seed, -1, 0))
            using (FileStream fStream = new FileStream(filePath, FileMode.Create))
            {
                s.WriteTo(fStream);
            }
        }

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

        [TestCase(12, 42, 12, 42, -1, 0, ExpectedResult = false)] // Identical small file
        [TestCase(5000, 42, 5000, 42, -1, 0, ExpectedResult = false)] // Identical medium file
        [TestCase(32, 42, 33, 42, -1, 0, ExpectedResult = true)] // Small file identical but last byte different(one byte bigger file)
        [TestCase(5000, 42, 5001, 42, -1, 0, ExpectedResult = true)] // medium file identical but last byte different(one byte bigger file)
        [TestCase(4096, 42, 5000, 42, -1, 0, ExpectedResult = true)] // Same content for first 4096 bytes.
        [TestCase(32, 42, 32, 1000, -1, 0, ExpectedResult = true)] // Same size but different file
        [TestCase(512, 42, 512, 42, 150, 75, ExpectedResult = true)] // Same size but subset of file has been changed
        [TestCase(5000, 42, 5000, 42, 4200, 64, ExpectedResult = true)] // Same size but subset of file has been changed after first 4096 bytes
        public bool TestFileWriteIfDifferentInternal(int size1, int seed1, int size2, int seed2, int streamModOffset, int streamModSize)
        {
            var filePath1 = Path.GetTempFileName();
            try
            {
                PrepareFile(filePath1, size1, seed1);
                using (MemoryStream s = PrepareMemoryStream(size2, seed2, streamModOffset, streamModSize))
                {
                    bool result = Util.FileWriteIfDifferentInternal(new FileInfo(filePath1), s, true);

                    // File should never be different anymore after the call above
                    Assert.IsFalse(Util.IsFileDifferent(new FileInfo(filePath1), s));

                    return result;
                }
            }
            finally
            {
                File.Delete(filePath1);
            }
        }

        [Test]
        public void TestFileWriteIfDifferentInternalInexistingFile()
        {
            var filePath1 = Path.GetTempFileName();
            try
            {
                File.Delete(filePath1);
                using (MemoryStream s = PrepareMemoryStream(12, 42, -1, 0))
                {
                    bool result = Util.FileWriteIfDifferentInternal(new FileInfo(filePath1), s, true);

                    // File should never be different anymore after the call above
                    Assert.IsFalse(Util.IsFileDifferent(new FileInfo(filePath1), s));
                }
            }
            finally
            {
                File.Delete(filePath1);
            }
        }

        [Test]
        public void TestFileWriteIfDifferentInternalReadOnlyFile()
        {
            var filePath1 = Path.GetTempFileName();
            try
            {
                // Prepare a file and set it readonly
                PrepareFile(filePath1, 12, 50);
                var fileInfo = new FileInfo(filePath1);
                fileInfo.IsReadOnly = true;

                // Prepare a different memory stream
                using (MemoryStream s = PrepareMemoryStream(12, 42, -1, 0))
                {
                    bool result = Util.FileWriteIfDifferentInternal(fileInfo, s, true);

                    // File should never be different anymore after the call above
                    Assert.IsFalse(Util.IsFileDifferent(fileInfo, s));
                }
            }
            finally
            {
                File.Delete(filePath1);
            }
        }

        [Test]
        public void TestFileWriteIfDifferentInternalDirectoryDoesNotExist()
        {
            var tempPath = Path.GetTempPath();
            string filePath1 = Path.Combine(tempPath, Guid.NewGuid().ToString(), "file.txt");
            try
            {
                var fileInfo = new FileInfo(filePath1);
                Assert.IsFalse(fileInfo.Directory.Exists);
                Assert.IsFalse(fileInfo.Exists);

                // Prepare a different memory stream
                using (MemoryStream s = PrepareMemoryStream(12, 42, -1, 0))
                {
                    bool result = Util.FileWriteIfDifferentInternal(fileInfo, s, true);

                    // File should never be different anymore after the call above
                    fileInfo.Refresh();
                    Assert.IsFalse(Util.IsFileDifferent(fileInfo, s));
                }
            }
            finally
            {
                File.Delete(filePath1);
            }
        }

        [TestCase(12, 42, 12, 42, -1, 0, ExpectedResult = false)] // Identical small file
        [TestCase(5000, 42, 5000, 42, -1, 0, ExpectedResult = false)] // Identical medium file
        [TestCase(32, 42, 33, 42, -1, 0, ExpectedResult = true)] // Small file identical but last byte different(one byte bigger file)
        [TestCase(5000, 42, 5001, 42, -1, 0, ExpectedResult = true)] // medium file identical but last byte different(one byte bigger file)
        [TestCase(4096, 42, 5000, 42, -1, 0, ExpectedResult = true)] // Same content for first 4096 bytes.
        [TestCase(32, 42, 32, 1000, -1, 0, ExpectedResult = true)] // Same size but different file
        [TestCase(512, 42, 512, 42, 150, 75, ExpectedResult = true)] // Same size but subset of file has been changed
        [TestCase(5000, 42, 5000, 42, 4200, 64, ExpectedResult = true)] // Same size but subset of file has been changed after first 4096 bytes
        public bool TestIsFileDifferent(int size1, int seed1, int size2, int seed2, int streamModOffset, int streamModSize)
        {
            var filePath1 = Path.GetTempFileName();
            try
            {
                PrepareFile(filePath1, size1, seed1);
                using (MemoryStream s = PrepareMemoryStream(size2, seed2, streamModOffset, streamModSize))
                {
                    return Util.IsFileDifferent(new FileInfo(filePath1), s);
                }
            }
            finally
            {
                File.Delete(filePath1);
            }
        }

        [Test]
        public void TestIsFileDifferentInexistingFile()
        {
            using (var s = new MemoryStream())
            {
                Util.IsFileDifferent(new FileInfo("inexistingfile"), s);
            }
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
            Assert.Catch<Error>(() => Util.GetToolVersionString(DevEnv.xcode));
            Assert.Catch<Error>(() => Util.GetToolVersionString(DevEnv.eclipse));
            Assert.Catch<Error>(() => Util.GetToolVersionString(DevEnv.make));
        }

        /// <summary>
        ///     Verify the right managed project platform name was returned depending on the platform and the project type
        ///  </summary>
        [Test]
        public void GetVisualStudioPlatformString()
        {
            Assert.AreEqual("x86", Util.GetToolchainPlatformString(Platform.win32, new CSharpProject(), null, false));
            Assert.AreEqual("x64", Util.GetToolchainPlatformString(Platform.win64, new CSharpProject(), null, false));
            Assert.AreEqual("AnyCPU", Util.GetToolchainPlatformString(Platform.win64, new PythonProject(), null, false));
            Assert.AreEqual("Any CPU", Util.GetToolchainPlatformString(Platform.win64, new PythonProject(), null, true));
            Assert.AreEqual("x64", Util.GetToolchainPlatformString(Platform.win64, new AndroidPackageProject(), null, true));
        }

        /// <summary>
        ///     Verify that an exception is thrown if a platform is not supported
        ///  </summary>
        [Test]
        public void GetVisualStudioPlatformStringException()
        {
            Assert.Catch<Exception>(() => Util.GetToolchainPlatformString(Platform.android, new CSharpProject(), null, false));
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
        public void CreateSymbolicLinkOnDirectory()
        {
            var tempBase = Path.Combine(Path.GetTempPath(), $"test-symlink-dir-{Guid.NewGuid():N}");
            var tempSource = Path.Combine(tempBase, $"test-source");
            var tempSourceRelative = Path.Combine(tempBase, $"..\\{new DirectoryInfo(tempBase).Name}\\test-source");
            var tempSourceNonEmpty = Path.Combine(tempBase, $"test-source-nonempty");
            var tempSourceNonEmptyFile = Path.Combine(tempSourceNonEmpty, $"test1.txt");
            var tempDestination1 = Path.Combine(tempBase, $"test-destination1");
            var tempDestination1File = Path.Combine(tempDestination1, $"test2.txt");
            var tempDestination2 = Path.Combine(tempBase, $"test-destination2");

            try
            {
                Directory.CreateDirectory(tempDestination1);
                File.WriteAllText(tempDestination1File, "Some content");
                Directory.CreateDirectory(tempDestination2);

                // Invalid case: source and destination are the same
                Assert.Throws<IOException>(() => Util.CreateOrUpdateSymbolicLink(Path.GetTempPath(), Path.GetTempPath(), true));

                // Validate creation of a new symbolic link
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSource, tempDestination1, true), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.Created));
                Assert.True(new DirectoryInfo(tempSource).Attributes.HasFlag(FileAttributes.ReparsePoint));
                Assert.That(Directory.ResolveLinkTarget(tempSource, false).FullName, Is.EqualTo(tempDestination1));

                // Validate updating a symbolic
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSource, tempDestination2, true), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.Updated));
                Assert.True(new DirectoryInfo(tempSource).Attributes.HasFlag(FileAttributes.ReparsePoint));
                Assert.That(Directory.ResolveLinkTarget(tempSource, false).FullName, Is.EqualTo(tempDestination2));
                Assert.True(File.Exists(tempDestination1File)); // Verify that the content of the old symlink is still there

                // Validate noop when the symbolic link is already up to date
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSource, tempDestination2, true), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.AlreadyUpToDate));

                // Validate with alt directory separator char
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSource.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), tempDestination2, true), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.AlreadyUpToDate));

                // Validate with relative path
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSourceRelative, tempDestination2, true), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.AlreadyUpToDate));

                // Validate that creating a symbolic link on a non empty directory succeeds (deleting its content)
                Directory.CreateDirectory(tempSourceNonEmpty);
                File.WriteAllText(tempSourceNonEmptyFile, "Some content");
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSourceNonEmpty, tempDestination1, true), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.Updated));
                Assert.False(File.Exists(tempSourceNonEmptyFile)); // Should have been deleted when the symlink was created
            }
            finally
            {
                try { Directory.Delete(tempBase, true); } catch { }
            }
        }

        /// <summary>
        ///     <c>CreateSymbolicLinkOnFile</c> create a symbolic link for files
        ///     The test cases are:
        ///     <list type="number">
        ///         <item><description>Testing that a symbolic link is not created on a file pointing itself</description></item>
        ///         <item><description>Testing that a symbolic link is created on a file pointing to another file</description></item>
        ///         <item><description>Testing updating a file symbolic link to point to a different target</description></item>
        ///         <item><description>Testing that no operation occurs when file symbolic link is already up to date</description></item>
        ///     </list>
        ///     <remark>Implementation of create symbolic links doesn't work on linux so this test is discard on Mono</remark>
        ///  </summary>
        [Test]
        public void CreateSymbolicLinkOnFile()
        {
            var tempBase = Path.Combine(Path.GetTempPath(), $"test-symlink-file-{Guid.NewGuid():N}");
            var tempSource = Path.Combine(tempBase, $"test-file-source.txt");
            var tempSourceRelative = Path.Combine(tempBase, $"..\\{new DirectoryInfo(tempBase).Name}\\test-file-source.txt");
            var tempDestination1 = Path.Combine(tempBase, $"test-file-destination1.txt");
            var tempDestination2 = Path.Combine(tempBase, $"test-file-destination2.txt");

            try
            {
                // Create destination files with some content
                Directory.CreateDirectory(tempBase);
                File.WriteAllText(tempDestination1, "Destination 1 content");
                File.WriteAllText(tempDestination2, "Destination 2 content");

                // Invalid case: source and destination are the same
                Assert.Throws<IOException>(() => Util.CreateOrUpdateSymbolicLink(tempDestination1, tempDestination1, false));

                // Validate creation of a new symbolic link
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSource, tempDestination1, false), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.Created));
                Assert.True(new FileInfo(tempSource).Attributes.HasFlag(FileAttributes.ReparsePoint));
                Assert.That(File.ResolveLinkTarget(tempSource, false).FullName, Is.EqualTo(tempDestination1));
                Assert.True(Util.IsSymbolicLink(tempSource));

                // Validate updating a symbolic link
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSource, tempDestination2, false), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.Updated));
                Assert.True(new FileInfo(tempSource).Attributes.HasFlag(FileAttributes.ReparsePoint));
                Assert.That(File.ResolveLinkTarget(tempSource, false).FullName, Is.EqualTo(tempDestination2));
                Assert.True(Util.IsSymbolicLink(tempSource));

                // Validate noop when the symbolic link is already up to date
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSource, tempDestination2, false), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.AlreadyUpToDate));

                // Validate with alt directory separator char
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSource.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), tempDestination2, false), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.AlreadyUpToDate));

                // Validate with relative path
                Assert.That(Util.CreateOrUpdateSymbolicLink(tempSourceRelative, tempDestination2, false), Is.EqualTo(Util.CreateOrUpdateSymbolicLinkResult.AlreadyUpToDate));
            }
            finally
            {
                try { Directory.Delete(tempBase, true); } catch { }
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
        public void IsSymbolicLink()
        {
            var mockPath1 = Path.GetTempFileName();
            var mockPath2 = Path.GetTempFileName();

            Assert.False(Util.IsSymbolicLink(mockPath1));

            Assert.DoesNotThrow(() => Util.CreateOrUpdateSymbolicLink(mockPath1, mockPath2, false));
            Assert.True(Util.IsSymbolicLink(mockPath1));

            File.Delete(mockPath1);
            File.Delete(mockPath2);
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
            ITarget target1 = new Target(Platform.win64, DevEnv.vs2017, Optimization.Release, OutputType.Dll, Blob.Blob, BuildSystem.FastBuild, DotNetFramework.net8_0);
            ITarget target2 = new Target(Platform.win64, DevEnv.vs2017, Optimization.Release, OutputType.Dll, Blob.Blob, BuildSystem.FastBuild, DotNetFramework.net8_0);
            ITarget target3 = new Target(Platform.win64, DevEnv.vs2017, Optimization.Debug, OutputType.Dll, Blob.Blob, BuildSystem.FastBuild, DotNetFramework.net10_0);

            Assert.True(Util.MakeDifferenceString(target1, target2).Length == 0);
            Assert.True(Util.MakeDifferenceString(target1, target3).Contains("\"net8.0\" and \"net10.0\""));
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

            Assert.AreEqual(Path.Combine(absolutePath.ToLower(), fileName),
                Util.GetConvertedRelativePath(absolutePath, fileName, newRelativeToFullPath: "", false, null));

            Assert.AreEqual(mockPath,
                Util.GetConvertedRelativePath(absolutePath, mockPath, newRelativeToFullPath: "", false, root));

            Assert.AreEqual(Path.Combine(absolutePath.ToLower(), fileName),
                Util.GetConvertedRelativePath(absolutePath, fileName, newRelativeToFullPath: "", false, ""));

            Assert.AreEqual(absolutePath,
                Util.GetConvertedRelativePath(absolutePath, null, newRelativeToFullPath: "", false, null));

            Assert.AreEqual(Path.GetTempPath(),
                Util.GetConvertedRelativePath(root, Path.GetTempPath(), newRelativeToFullPath: "", false, null));

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

    [TestFixture]
    public class PathIsUnderRoot
    {
        [Test]
        public void PathCombinationAbsoluteRelative()
        {
            var rootPath = "D:\\versioncontrol\\solutionname\\projectname\\src\\";

            var absoluteFilePathUnderRoot = rootPath + "\\code\\factory.cs";
            var absoluteFolderPathUnderRoot = rootPath + "\\code\\";
            var relativeFilePathUnderRoot = "..\\src\\code\\factory.cs";
            var relativeFolderPathUnderRoot = "..\\src\\code\\";

            var absoluteFilePathNotUnderRoot = "C:\\.nuget\\dd\\llvm\\build\\native\\llvm.sharpmake.cs";
            var absoluteFolderPathNotUnderRoot = "C:\\.nuget\\dd\\llvm\\build\\native\\";
            var relativeFilePathNotUnderRoot = "..\\otherfolder\\code\\factory.cs";
            var relativeFolderPathNotUnderRoot = "..\\otherfolder\\code\\";

            Assert.IsTrue(Util.PathIsUnderRoot(rootPath, absoluteFilePathUnderRoot));
            Assert.IsTrue(Util.PathIsUnderRoot(rootPath, absoluteFolderPathUnderRoot));
            Assert.IsTrue(Util.PathIsUnderRoot(rootPath, relativeFilePathUnderRoot));
            Assert.IsTrue(Util.PathIsUnderRoot(rootPath, relativeFolderPathUnderRoot));

            Assert.IsFalse(Util.PathIsUnderRoot(rootPath, absoluteFilePathNotUnderRoot));
            Assert.IsFalse(Util.PathIsUnderRoot(rootPath, absoluteFolderPathNotUnderRoot));
            Assert.IsFalse(Util.PathIsUnderRoot(rootPath, relativeFilePathNotUnderRoot));
            Assert.IsFalse(Util.PathIsUnderRoot(rootPath, relativeFolderPathNotUnderRoot));

        }

        [Test]
        public void AssertsOnInvalidArguments()
        {
            var invalidRootPath = "projectname\\src\\";
            var relativeFilePathUnderRoot = "..\\src\\code\\factory.cs";

            Assert.Throws<ArgumentException>(() => Util.PathIsUnderRoot(invalidRootPath, relativeFilePathUnderRoot));
        }

        [Test]
        public void RootFolderWithDotInName()
        {
            var rootPath = "D:\\versioncontrol\\solutionname\\projectname\\src\\version0.1";
            var pathNotUnderRoot = "C:\\.nuget\\dd\\androidsdk";
            var pathUnderRoot = rootPath + "\\foo\\bar";

            Assert.IsFalse(Util.PathIsUnderRoot(rootPath, pathNotUnderRoot));
            Assert.IsTrue(Util.PathIsUnderRoot(rootPath, pathUnderRoot));
        }

        [Test]
        public void RootFilePath()
        {
            var root = @"..\..\..\..\samples\CPPCLI\";
            root = Path.GetFullPath(root);
            var rootWithFile = root + "CLRTest.sharpmake.cs";
            var pathUnderRoot = root + "\\foo\\bar";

            Assert.IsTrue(Util.PathIsUnderRoot(rootWithFile, pathUnderRoot));
        }

        [Test]
        public void RootDirectoryPathOneIntersectionAway()
        {
            var root = @"D:\versioncontrol\solutionname\projectname\";
            var rootWithExtraDir = root + "CLRTest";
            var pathNotUnderRoot = root + @"\foo\";

            Assert.IsFalse(Util.PathIsUnderRoot(rootWithExtraDir, pathNotUnderRoot));
        }

        [Test]
        public void RootIsDrive()
        {
            if (Util.IsRunningOnUnix())
            {
                var fullyQualifiedRoot = Util.UnixSeparator.ToString();
                var pathUnderRoot = "/versioncontrol/solutionname/projectname/src/code/factory.cs";

                Assert.IsTrue(Util.PathIsUnderRoot(fullyQualifiedRoot, pathUnderRoot));
            }
            else
            {
                var fullyQualifiedRoot = @"D:\";
                var pathUnderRoot = @"D:\versioncontrol\solutionname\projectname\src\code\factory.cs";

                Assert.IsTrue(Util.PathIsUnderRoot(fullyQualifiedRoot, pathUnderRoot));
            }
        }
    }

    [TestFixture]
    public class TrimAllLeadingDotDot
    {
        [Test]
        public void TrimsRelativePath()
        {
            var windowsFilePath = "..\\..\\..\\code\\file.cs";
            var windowsFolderPath = "..\\..\\..\\code\\";
            var unixFilePath = "../../../code/file.cs";
            var unixFolderPath = "../../../code/";

            Assert.AreEqual("code\\file.cs", Util.TrimAllLeadingDotDot(windowsFilePath));
            Assert.AreEqual(Util.TrimAllLeadingDotDot(windowsFolderPath), "code\\");
            Assert.AreEqual(Util.TrimAllLeadingDotDot(unixFilePath), "code/file.cs");
            Assert.AreEqual(Util.TrimAllLeadingDotDot(unixFolderPath), "code/");
        }


        [Test]
        public void DoesntTrimFolderNames()
        {
            var dotFolderRelativeWindows = "..\\.nuget\\packages";
            var dotFolderRelativeUnix = "../.nuget/packages";

            Assert.AreEqual(Util.TrimAllLeadingDotDot(dotFolderRelativeWindows), ".nuget\\packages");
            Assert.AreEqual(Util.TrimAllLeadingDotDot(dotFolderRelativeUnix), ".nuget/packages");
        }

        [Test]
        public void TrimsMixedSeparators()
        {
            var mixedSeparatorPath = "..\\../..\\code\\";

            Assert.AreEqual(Util.TrimAllLeadingDotDot(mixedSeparatorPath), "code\\");
        }
    }
}
