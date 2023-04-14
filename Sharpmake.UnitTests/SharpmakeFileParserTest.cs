// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    [TestFixture]
    public class SharpmakeFileParserTest
    {
        private FileInfo _fakeFileInfo;
        private const int _fakeFileLine = 123;

        [OneTimeSetUp]
        public void Init()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            Util.FakePathPrefix = Directory.GetCurrentDirectory();
            _fakeFileInfo = new FileInfo(Path.Combine(Util.FakePathPrefix, "SharpmakeFile.sharpmake.cs"));
        }

        [SetUp]
        public void ClearFakeTreeSetup()
        {
            Util.ClearFakeTree();
        }

        #region Include

        private class AssemblerContext : IAssemblerContext
        {
            public List<string> RuntimeReferences = new List<string>();
            public List<string> BuildReferences = new List<string>();
            public List<string> Sources = new List<string>();

            [Obsolete("Use AddRuntimeReference() instead")]
            public void AddReference(string file) => AddRuntimeReference(file);

            public void AddRuntimeReference(string file)
            {
                RuntimeReferences.Add(file);
            }

            public void AddBuildReference(string file)
            {
                BuildReferences.Add(file);
            }

            public void AddSourceFile(string file)
            {
                Sources.Add(file);
            }

            [Obsolete("Use AddRuntimeReference() instead")]
            public void AddReference(IAssemblyInfo info) => AddRuntimeReference(info);

            public void AddRuntimeReference(IAssemblyInfo info)
            {
                throw new NotImplementedException();
            }

            public IAssemblyInfo BuildAndLoadSharpmakeFiles(params string[] files)
            {
                throw new NotImplementedException();
            }

            public void AddSourceAttributeParser(ISourceAttributeParser parser)
            {
                throw new NotImplementedException();
            }

            public void SetDebugProjectName(string name)
            {
                throw new NotImplementedException();
            }

            public void AddDefine(string define)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void EmptyLine()
        {
            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine("", _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.Sources.Count, Is.EqualTo(0));
        }

        [Test]
        public void SimpleInclude()
        {
            const string sharpmakeIncludedFile = "someproject.sharpmake.cs";
            string sharpmakeIncludeFullPath = Path.Combine(_fakeFileInfo.DirectoryName, sharpmakeIncludedFile);

            Util.AddNewFakeFile(sharpmakeIncludedFile, 0);

            string line = $@"[module: Sharpmake.Include(""{sharpmakeIncludedFile}"")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.Sources.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeIncludeFullPath, assemblerContext.Sources.First());
        }

        [Test]
        public void SimpleIncludeFullPath()
        {
            const string sharpmakeIncludedFile = "someotherproject.sharpmake.cs";
            string sharpmakeIncludeFullPath = Path.Combine(_fakeFileInfo.DirectoryName, sharpmakeIncludedFile);

            Util.AddNewFakeFile(sharpmakeIncludedFile, 0);

            string line = $@"[module: Sharpmake.Include(""{sharpmakeIncludeFullPath}"")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.Sources.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeIncludeFullPath, assemblerContext.Sources.First());
        }

        [Test]
        public void ConvolutedInclude()
        {
            const string sharpmakeIncludedFile = "some project with spaces.sharpmake.cs";
            string sharpmakeIncludeFullPath = Path.Combine(_fakeFileInfo.DirectoryName, sharpmakeIncludedFile);

            Util.AddNewFakeFile(sharpmakeIncludedFile, 0);

            string line = $"   [  module\t : Sharpmake  .\t Include ( @\"{sharpmakeIncludedFile}\" \t) ]  \t";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.Sources.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeIncludeFullPath, assemblerContext.Sources.First());
        }

        [Test]
        public void ConvolutedIncludeWithComments1()
        {
            string[] sharpmakeIncludedFiles = {
                Path.Combine("folder", "sub1", "file1.sharpmake.cs"),
                Path.Combine("folder", "sub1", "file3.sharpmake.cs"),
                Path.Combine("folder", "sub1", "file2.sharpmake.cs"),
                Path.Combine("folder", "sub1", "nottoinclude.sharpmake.cs"),
                Path.Combine("folder", "sub2", "file1.sharpmake.cs"),
                Path.Combine("folder", "sub2", "file3.sharpmake.cs"),
                Path.Combine("folder", "sub2", "nottoinclude.sharpmake.cs")
            };
            string[] sharpmakeIncludesFullPath = sharpmakeIncludedFiles.Select(file => Path.Combine(_fakeFileInfo.DirectoryName, file)).ToArray();

            foreach (string file in sharpmakeIncludedFiles)
                Util.AddNewFakeFile(file, 0);

            string line = @"[module/*:Sharpmake.Include(@""folder/sub1/nottoinclude.sharpmake.cs"")]/**/: Sharpmake/*.Reference*/.Include(/*asda(@"" /*dsa*/@""folder*/sub*/*file*.cs"")]// asdmvie  aas */ asd [module: Sharpmake.Include(@""folder/sub1/nottoinclude.sharpmake.cs"")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.Sources.Count, Is.EqualTo(5));
            CollectionAssert.IsSubsetOf(assemblerContext.Sources, sharpmakeIncludesFullPath);
            foreach (string include in assemblerContext.Sources)
                StringAssert.DoesNotContain(include, "nottoinclude.sharpmake.cs");
        }

        [Test]
        public void ConvolutedIncludeWithComments2()
        {
            string[] sharpmakeIncludedFiles = {
                Path.Combine("folder", "sub1", "file1.sharpmake.cs"),
                Path.Combine("folder", "sub1", "file3.sharpmake.cs"),
                Path.Combine("folder", "sub1", "file2.sharpmake.cs"),
                Path.Combine("folder", "sub1", "nottoinclude.sharpmake.cs"),
                Path.Combine("folder", "sub2", "file1.sharpmake.cs"),
                Path.Combine("folder", "sub2", "file3.sharpmake.cs"),
                Path.Combine("folder", "sub2", "nottoinclude.sharpmake.cs")
            };
            string[] sharpmakeIncludesFullPath = sharpmakeIncludedFiles.Select(file => Path.Combine(_fakeFileInfo.DirectoryName, file)).ToArray();

            foreach (string file in sharpmakeIncludedFiles)
                Util.AddNewFakeFile(file, 0);

            string line = @"/**/[module: Sharpmake.Include(@""folder*/sub*/*file*.cs"")] // asdmvie aas */ [module: Sharpmake.Include(@""folder/sub1/nottoinclude.sharpmake.cs"")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.Sources.Count, Is.EqualTo(5));
            CollectionAssert.IsSubsetOf(assemblerContext.Sources, sharpmakeIncludesFullPath);
            foreach (string include in assemblerContext.Sources)
                StringAssert.DoesNotContain(include, "nottoinclude.sharpmake.cs");
        }

        [Test]
        public void ConvolutedIncludeWithComments_NoInclude()
        {
            string[] sharpmakeIncludedFiles = {
                Path.Combine("folder", "sub1", "file1.sharpmake.cs"),
                Path.Combine("folder", "sub1", "file3.sharpmake.cs"),
                Path.Combine("folder", "sub1", "file2.sharpmake.cs"),
                Path.Combine("folder", "sub1", "nottoinclude.sharpmake.cs"),
                Path.Combine("folder", "sub2", "file1.sharpmake.cs"),
                Path.Combine("folder", "sub2", "file3.sharpmake.cs"),
                Path.Combine("folder", "sub2", "nottoinclude.sharpmake.cs")
            };

            foreach (string file in sharpmakeIncludedFiles)
                Util.AddNewFakeFile(file, 0);

            string line = @"//**/[module: Sharpmake.Include(@""folder*/sub*/*file*.cs"")] // asdmvie aas */ [module: Sharpmake.Include(@""folder/sub1/nottoinclude.sharpmake.cs"")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.Sources.Count, Is.EqualTo(0));
        }

        [Test]
        public void WildcardIncludes()
        {
            string[] sharpmakeIncludedFiles = {
                Path.Combine("folder", "sub1", "file1.sharpmake.cs"),
                Path.Combine("folder", "sub1", "file3.sharpmake.cs"),
                Path.Combine("folder", "sub1", "file2.sharpmake.cs"),
                Path.Combine("folder", "sub1", "anotherfiletonotinclude.sharpmake.cs"),
                Path.Combine("folder", "sub2", "file1.sharpmake.cs"),
                Path.Combine("folder", "sub2", "file3.sharpmake.cs"),
                Path.Combine("folder", "sub2", "anotherfiletonotinclude.sharpmake.cs")
            };
            string[] sharpmakeIncludesFullPath = sharpmakeIncludedFiles.Select(file => Path.Combine(_fakeFileInfo.DirectoryName, file)).ToArray();

            foreach (string file in sharpmakeIncludedFiles)
                Util.AddNewFakeFile(file, 0);

            string line = @"[module: Sharpmake.Include(""folder/*sub*/file*.cs"")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.Sources.Count, Is.EqualTo(5));
            CollectionAssert.IsSubsetOf(assemblerContext.Sources, sharpmakeIncludesFullPath);
            foreach (string include in assemblerContext.Sources)
                StringAssert.DoesNotContain(include, "anotherfiletonotinclude.sharpmake.cs");
        }

        [Test]
        public void IncorrectFormattedInclude()
        {
            const string sharpmakeIncludedFile = "yetanotherproject.sharpmake.cs";

            Util.AddNewFakeFile(sharpmakeIncludedFile, 0);

            string line = $"\t   \t [module:\t \t SharpmakeAInclude(\t stuffstuff \"{sharpmakeIncludedFile}\")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.Sources.Count, Is.EqualTo(0));
        }

        [Test]
        public void SimpleIncludeWithEnvironmentVariable()
        {
            Environment.SetEnvironmentVariable("SIMPLE_INCLUDE_ENV_VAR", "someotherproject");

            const string sharpmakeIncludedFile = "someotherproject.sharpmake.cs";
            string sharpmakeIncludeFullPath = Path.Combine(_fakeFileInfo.DirectoryName, sharpmakeIncludedFile);

            Util.AddNewFakeFile(sharpmakeIncludedFile, 0);

            string line = $@"[module: Sharpmake.Include(""%SIMPLE_INCLUDE_ENV_VAR%.sharpmake.cs"")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.Sources.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeIncludeFullPath, assemblerContext.Sources.First());
        }

        #endregion

        #region Reference
        [Test]
        public void SimpleReference()
        {
            const string sharpmakeReferencedFile = "someassembly.dll";
            string sharpmakeReferenceFullPath = Path.Combine(_fakeFileInfo.DirectoryName, sharpmakeReferencedFile);

            Util.AddNewFakeFile(sharpmakeReferencedFile, 0);

            string line = $@"[module: Sharpmake.Reference(""{sharpmakeReferencedFile}"")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.RuntimeReferences.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeReferenceFullPath, assemblerContext.RuntimeReferences.First());
        }

        [Test]
        public void SimpleReferenceFullPath()
        {
            const string sharpmakeReferencedFile = "someotherassembly.dll";
            string sharpmakeReferenceFullPath = Path.Combine(_fakeFileInfo.DirectoryName, sharpmakeReferencedFile);

            Util.AddNewFakeFile(sharpmakeReferencedFile, 0);

            string line = $@"[module: Sharpmake.Reference(""{sharpmakeReferenceFullPath}"")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.RuntimeReferences.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeReferenceFullPath, assemblerContext.RuntimeReferences.First());
        }

        [Test]
        public void ConvolutedReference()
        {
            const string sharpmakeReferencedFile = "some assembly with spaces.dll";
            string sharpmakeReferenceFullPath = Path.Combine(_fakeFileInfo.DirectoryName, sharpmakeReferencedFile);

            Util.AddNewFakeFile(sharpmakeReferencedFile, 0);

            string line = $"   [  module\t : Sharpmake  .\t Reference ( @\"{sharpmakeReferencedFile}\" \t) ]  \t";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.RuntimeReferences.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeReferenceFullPath, assemblerContext.RuntimeReferences.First());
        }

        [Test]
        public void IncorrectFormattedReference()
        {
            const string sharpmakeReferencedFile = "yetanotherassembly.dll";

            Util.AddNewFakeFile(sharpmakeReferencedFile, 0);

            string line = $"\t   \t [module:\t \t SharpmakeAReference(\t stuffstuff \"{sharpmakeReferencedFile}\")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.RuntimeReferences.Count, Is.EqualTo(0));
        }
        #endregion

        #region DefinesTests

        private abstract class LineTest
        {
            public abstract void Test(IParsingFlowParser parser, FileInfo fileInfo, int index, AssemblerContext assemblerContext);
        }

        private class LineTestParse : LineTest
        {
            public string Line { get; }
            public bool ExpectedResult { get; }

            public LineTestParse(string line, bool expectedResult)
            {
                Line = line;
                ExpectedResult = expectedResult;
            }

            public override void Test(IParsingFlowParser parser, FileInfo fileInfo, int index, AssemblerContext assemblerContext)
            {
                parser.ParseLine(Line, fileInfo, index, assemblerContext);
                bool shouldParseLine = parser.ShouldParseLine();
                Assert.AreEqual(ExpectedResult, shouldParseLine, $"ShouldParseLine for line ({index}) \"{Line}\" should return {ExpectedResult} after evaluation!");
            }
        }

        private class LineTestNestedFile : LineTest
        {
            public LineTest[] Lines { get; }

            public LineTestNestedFile(LineTest[] lines)
            {
                Lines = lines;
            }

            public override void Test(IParsingFlowParser parser, FileInfo fileInfo, int index, AssemblerContext assemblerContext)
            {
                Assert.DoesNotThrow(() => parser.FileParsingBegin(fileInfo.FullName));
                for (int i = 0; i < Lines.Length; i++)
                {
                    Lines[i].Test(parser, fileInfo, i, assemblerContext);
                }
                Assert.DoesNotThrow(() => parser.FileParsingEnd(fileInfo.FullName));
            }
        }

        private void EvaluateLines(LineTest[] lines, HashSet<string> defines)
        {
            var assemblerContext = new AssemblerContext();
            IParsingFlowParser parser = new PreprocessorConditionParser(defines);

            LineTestNestedFile wrapperFile = new LineTestNestedFile(lines);
            wrapperFile.Test(parser, _fakeFileInfo, 0, assemblerContext);
        }

        [Test]
        public void SimpleConditionsTest()
        {
            const string defineA = "DEFINE_A";
            HashSet<string> defines = new HashSet<string>() { defineA };

            LineTest[] lines = new[]
            {
                new LineTestParse("",                true),
                new LineTestParse($"#if {defineA}",  true),
                new LineTestParse("...",             true),
                new LineTestParse("#endif",          true),
                new LineTestParse("#if _{defineA}_", false),
                new LineTestParse("...",             false),
                new LineTestParse("#endif",          true),
                new LineTestParse("",                true),
            };

            EvaluateLines(lines, defines);
        }

        [Test]
        public void SimpleElseConditionsTest()
        {
            HashSet<string> defines = new HashSet<string>() { };

            LineTest[] lines = new[]
            {
                new LineTestParse($"#if NOT_DEFINED", false),
                new LineTestParse("...",              false),
                new LineTestParse("#else",            true),
                new LineTestParse("...",              true),
                new LineTestParse("#endif",           true),
            };

            EvaluateLines(lines, defines);
        }

        [Test]
        public void MultipleBranchesConditionsTest()
        {
            const string defineA = "DEFINE_A";
            const string defineB = "DEFINE_B";
            HashSet<string> defines = new HashSet<string>() { defineA, defineB };

            LineTest[] lines = new[]
            {
                new LineTestParse($"#if NOT_DEFINED", false),
                new LineTestParse("...",              false),
                new LineTestParse($"#elif {defineA}", true),
                new LineTestParse("...",              true),
                new LineTestParse($"#elif {defineB}", false),
                new LineTestParse("...",              false),
                new LineTestParse("#else",            false),
                new LineTestParse("...",              false),
                new LineTestParse("#endif",           true),
            };

            EvaluateLines(lines, defines);
        }

        [Test]
        public void NestedConditionsTest()
        {
            const string defineA = "DEFINE_A";
            const string defineB = "DEFINE_B";
            HashSet<string> defines = new HashSet<string>() { defineA, defineB };

            LineTest[] lines = new[]
            {
                new LineTestParse($"#if NOT_DEFINED",  false),
                new LineTestParse("...",               false),
                new LineTestParse($"#elif {defineA}",  true),
                new LineTestParse("  #if NOT_DEFINED", false),
                new LineTestParse("  ...",             false),
                new LineTestParse($" #elif {defineB}", true),
                new LineTestParse("  ...",             true),
                new LineTestParse("  #else",           false),
                new LineTestParse("  ...",             false),
                new LineTestParse("  #endif",          false),
                new LineTestParse("#else",             false),
                new LineTestParse("...",               false),
                new LineTestParse("#endif",            true),
            };

            EvaluateLines(lines, defines);
        }

        [Test]
        public void NestedFilesTest()
        {
            HashSet<string> defines = new HashSet<string>() { };

            LineTest[] lines = new LineTest[]
            {
                new LineTestParse("...",               true),
                new LineTestNestedFile(new []
                {
                    new LineTestParse("...",true),
                }),
                new LineTestParse("...",               true),
            };

            EvaluateLines(lines, defines);
        }

        [Test]
        public void NestedFilesWithConditionsTest()
        {
            const string defineA = "DEFINE_A";
            HashSet<string> defines = new HashSet<string>() { defineA };

            LineTest[] lines = new LineTest[]
            {
                new LineTestParse($"#if NOT_DEFINED", false),
                new LineTestNestedFile(new []
                {
                    new LineTestParse("...",          false),
                }),
                new LineTestParse($"#elif {defineA}", true),
                new LineTestNestedFile(new []
                {
                    new LineTestParse("...",          true),
                }),
                new LineTestParse($"#endif",          true),
                new LineTestParse("...",              true),
            };

            EvaluateLines(lines, defines);
        }

        #endregion
    }
}
