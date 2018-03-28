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
using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;

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


        [Test]
        public void SimpleInclude()
        {
            const string sharpmakeIncludedFile = "someproject.sharpmake.cs";
            string sharpmakeIncludeFullPath = Path.Combine(_fakeFileInfo.DirectoryName, sharpmakeIncludedFile);

            Util.AddNewFakeFile(sharpmakeIncludedFile, 0);

            string line = $@"[module: Sharpmake.Include(""{sharpmakeIncludedFile}"")]";

            var includes = new List<string>();
            Assembler.GetSharpmakeIncludesFromLine(line, _fakeFileInfo, _fakeFileLine, ref includes);

            Assert.That(includes.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeIncludeFullPath, includes.First());
        }

        [Test]
        public void SimpleIncludeFullPath()
        {
            const string sharpmakeIncludedFile = "someotherproject.sharpmake.cs";
            string sharpmakeIncludeFullPath = Path.Combine(_fakeFileInfo.DirectoryName, sharpmakeIncludedFile);

            Util.AddNewFakeFile(sharpmakeIncludedFile, 0);

            string line = $@"[module: Sharpmake.Include(""{sharpmakeIncludeFullPath}"")]";

            var includes = new List<string>();
            Assembler.GetSharpmakeIncludesFromLine(line, _fakeFileInfo, _fakeFileLine, ref includes);

            Assert.That(includes.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeIncludeFullPath, includes.First());
        }


        [Test]
        public void WeirdlyFormattedInclude()
        {
            const string sharpmakeIncludedFile = "yetanotherproject.sharpmake.cs";
            string sharpmakeIncludeFullPath = Path.Combine(_fakeFileInfo.DirectoryName, sharpmakeIncludedFile);

            Util.AddNewFakeFile(sharpmakeIncludedFile, 0);

            string line = $"\t   \t [module:\t \t SharpmakeAInclude(\t stuffstuff \"{sharpmakeIncludedFile}\")]";

            var includes = new List<string>();
            Assembler.GetSharpmakeIncludesFromLine(line, _fakeFileInfo, _fakeFileLine, ref includes);

            Assert.That(includes.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeIncludeFullPath, includes.First());

            // now test the full path include
            includes.Clear();
            line = $@"[module: Sharpmake.Include(""{sharpmakeIncludeFullPath}"")]";

            Assembler.GetSharpmakeIncludesFromLine(line, _fakeFileInfo, _fakeFileLine, ref includes);

            Assert.That(includes.Count, Is.EqualTo(1));
            StringAssert.AreEqualIgnoringCase(sharpmakeIncludeFullPath, includes.First());
        }
    }
}
