// Copyright (c) 2017 Ubisoft Entertainment
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
            public List<string> References = new List<string>();
            public List<string> Sources = new List<string>();

            public void AddReference(string file)
            {
                References.Add(file);
            }

            public void AddSourceFile(string file)
            {
                Sources.Add(file);
            }

            public void AddReference(IAssemblyInfo info)
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

            Assert.That(assemblerContext.References.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeReferenceFullPath, assemblerContext.References.First());
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

            Assert.That(assemblerContext.References.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeReferenceFullPath, assemblerContext.References.First());
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

            Assert.That(assemblerContext.References.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeReferenceFullPath, assemblerContext.References.First());
        }

        [Test]
        public void IncorrectFormattedReference()
        {
            const string sharpmakeReferencedFile = "yetanotherassembly.dll";

            Util.AddNewFakeFile(sharpmakeReferencedFile, 0);

            string line = $"\t   \t [module:\t \t SharpmakeAReference(\t stuffstuff \"{sharpmakeReferencedFile}\")]";

            var assemblerContext = new AssemblerContext();
            new Assembler().ParseSourceAttributesFromLine(line, _fakeFileInfo, _fakeFileLine, assemblerContext);

            Assert.That(assemblerContext.References.Count, Is.EqualTo(0));
        }
        #endregion

        #region DefinesTests

        private struct LineTest
        {
            public string Line { get; }
            public bool ExpectedResult { get; }

            public LineTest(string line, bool expectedResult)
            {
                Line = line;
                ExpectedResult = expectedResult;
            }
        }

        private void EvaluateLines(LineTest[] lines, HashSet<string> defines)
        {
            var assemblerContext = new AssemblerContext();
            IParsingFlowParser parser = new PreprocessorConditionParser(defines);

            for (int i = 0; i < lines.Length; i++)
            {
                parser.ParseLine(lines[i].Line, _fakeFileInfo, i, assemblerContext);
                bool shouldParseLine = parser.ShouldParseLine();
                Assert.AreEqual(lines[i].ExpectedResult, shouldParseLine, $"ShouldParseLine for line ({i}) \"{lines[i].Line}\" should return {lines[i].ExpectedResult}!");
            }
        }

        [Test]
        public void SimpleConditionsTest()
        {
            const string defineA = "DEFINE_A";
            HashSet<string> defines = new HashSet<string>() { defineA };

            LineTest[] lines = new[]
            {
                new LineTest("",                true),
                new LineTest($"#if {defineA}",  true),
                new LineTest("...",             true),
                new LineTest("#endif",          true),
                new LineTest("#if _{defineA}_", false),
                new LineTest("...",             false),
                new LineTest("#endif",          true),
                new LineTest("",                true),
            };

            EvaluateLines(lines, defines);
        }

        [Test]
        public void SimpleElseConditionsTest()
        {
            HashSet<string> defines = new HashSet<string>() { };

            LineTest[] lines = new[]
            {
                new LineTest($"#if NOT_DEFINED", false),
                new LineTest("...",              false),
                new LineTest("#else",            true),
                new LineTest("...",              true),
                new LineTest("#endif",           true),
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
                new LineTest($"#if NOT_DEFINED", false),
                new LineTest("...",              false),
                new LineTest($"#elif {defineA}", true),
                new LineTest("...",              true),
                new LineTest($"#elif {defineB}", false),
                new LineTest("...",              false),
                new LineTest("#else",            false),
                new LineTest("...",              false),
                new LineTest("#endif",           true),
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
                new LineTest($"#if NOT_DEFINED",  false),
                new LineTest("...",               false),
                new LineTest($"#elif {defineA}",  true),
                new LineTest("  #if NOT_DEFINED", false),
                new LineTest("  ...",             false),
                new LineTest($" #elif {defineB}", true),
                new LineTest("  ...",             true),
                new LineTest("  #else",           false),
                new LineTest("  ...",             false),
                new LineTest("  #endif",          false),
                new LineTest("#else",             false),
                new LineTest("...",               false),
                new LineTest("#endif",            true),
            };

            EvaluateLines(lines, defines);
        }

        #endregion
    }
}
