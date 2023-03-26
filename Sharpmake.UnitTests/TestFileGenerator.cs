// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sharpmake.Generators;

namespace Sharpmake.UnitTests
{
    public class TestFileGenerator
    {
        [Resolver.Resolvable]
        private class ResolvableType
        {
            public string Value1 { get; set; } = "Value1_Test";
        }

        private class FileGeneratorTestHelper
        {
            public FileGenerator Generator { get; private set; }
            public MemoryStream Stream { get; private set; } = new MemoryStream();

            public StreamWriter Writer { get; private set; }

            public FileGeneratorTestHelper(Encoding encoding = null)
            {
                if (encoding == null)
                {
                    Generator = new FileGenerator();
                }
                else
                {
                    Generator = new FileGenerator(encoding);

                }
                Writer = new StreamWriter(Stream, Generator.Encoding, leaveOpen: true);
            }

            public void Write(string content)
            {
                Generator.Write(content);
                if (!content.Contains(FileGeneratorUtilities.RemoveLineTag))
                    Writer.Write(content);
            }

            public void WriteLine(string content)
            {
                Generator.WriteLine(content);
                if (!content.Contains(FileGeneratorUtilities.RemoveLineTag))
                    Writer.WriteLine(content);
            }

            public void WriteWithFallback(string content, string fallbackValue, string resolvedContent)
            {
                Generator.Write(content, fallbackValue);
                if (!content.Contains(FileGeneratorUtilities.RemoveLineTag))
                    Writer.Write(resolvedContent);
            }

            public void WriteLineWithFallback(string content, string fallbackValue, string resolvedContent)
            {
                Generator.WriteLine(content, fallbackValue);
                if (!content.Contains(FileGeneratorUtilities.RemoveLineTag))
                    Writer.WriteLine(resolvedContent);
            }

            public void WriteVerbatim(string content)
            {
                Generator.WriteVerbatim(content);
                Writer.Write(content);
            }

            public void WriteLineVerbatim(string content)
            {
                Generator.WriteLineVerbatim(content);
                Writer.WriteLine(content);
            }

            public bool IsEmpty()
            {
                bool r1 = Generator.IsEmpty();
                Writer.Flush();
                bool r2 = Stream.Length == 0;
                Assert.AreEqual(r1, r2);
                return r1;
            }

            public void Clear()
            {
                Generator.Clear();
                Writer.Flush();
                Stream.SetLength(0);
            }

            public void Flush()
            {
                Generator.Flush();
                Writer.Flush();
            }

        }

        // Needed since the argument to the test are complex
        static object[] EncodingTypesParams =
        {
            new object[] { null },
            new object[] { new UTF8Encoding(true) },
        };

        private void VerifyResults(FileGeneratorTestHelper testHelper)
        {
            testHelper.Flush();

            string tmpFile1 = Path.GetTempFileName();
            string tmpFile2 = Path.GetTempFileName();
            try
            {
                // Check that generated content is different than default content of tmp file(empty)
                var fileInfo1 = new FileInfo(tmpFile1);
                var fileInfo2 = new FileInfo(tmpFile2);

                Assert.IsTrue(testHelper.Generator.IsFileDifferent(fileInfo1));
                Assert.IsTrue(testHelper.Generator.IsFileDifferent(fileInfo2));

                // Write the file using reference stream
                Assert.IsTrue(Util.FileWriteIfDifferentInternal(fileInfo1, testHelper.Stream, true));
                fileInfo1.Refresh();

                // Using second file to write a file using generator.
                Assert.IsTrue(testHelper.Generator.FileWriteIfDifferent(fileInfo2, true));
                fileInfo2.Refresh();

                // Verify that generator content is the same.
                Assert.IsFalse(testHelper.Generator.IsFileDifferent(fileInfo1));

                // Verify that written file is identical to stream
                Assert.IsFalse(Util.IsFileDifferent(fileInfo2, testHelper.Stream));

                // Read the two files and verify that they are identical
                var contentFile1 = File.ReadAllBytes(tmpFile1);
                var contentFile2 = File.ReadAllBytes(tmpFile2);
                ReadOnlySpan<byte> span1 = new ReadOnlySpan<byte>(contentFile1);
                ReadOnlySpan<byte> span2 = new ReadOnlySpan<byte>(contentFile2);
                Assert.IsTrue(span1.SequenceEqual(span2));
            }
            finally
            {
                File.Delete(tmpFile1);
                File.Delete(tmpFile2);
            }
        }

        [TestCaseSource(nameof(EncodingTypesParams))]
        public void TestBasicWrites(Encoding encoding)
        {
            var testHelper = new FileGeneratorTestHelper(encoding);
            if (encoding != null)
            {
                Assert.AreEqual(encoding, testHelper.Generator.Encoding);
            }

            testHelper.Write("testWrite");
            testHelper.WriteLine("testWriteLine");
            testHelper.WriteVerbatim("testWriteVerbatim");
            testHelper.WriteLineVerbatim("testWriteLineVerbatim");

            VerifyResults(testHelper);
        }

        [Test]
        public void TestWritesWithResolve()
        {
            var testHelper = new FileGeneratorTestHelper();

            ResolvableType obj = new ResolvableType();

            testHelper.Generator.Resolver.SetParameter("obj", obj);
            testHelper.Generator.Resolver.Resolve(obj);

            testHelper.WriteLineWithFallback("abcdef", "fallback", "abcdef");
            testHelper.WriteLineWithFallback("abc[unkownfield]def", "fallback", "abcfallbackdef");
            testHelper.WriteLineWithFallback("abc[obj.Value1]def", "fallback", "abc" + obj.Value1 + "def");
            testHelper.WriteLineWithFallback("abc[obj.UnknownField]def", "fallback", "abcfallbackdef");

            testHelper.WriteWithFallback("abcdef", "fallback", "abcdef");
            testHelper.WriteWithFallback("abc[unkownfield]def", "fallback", "abcfallbackdef");
            testHelper.WriteWithFallback("abc[obj.Value1]def", "fallback", "abc" + obj.Value1 + "def");
            testHelper.WriteWithFallback("abc[obj.UnknownField]def", "fallback", "abcfallbackdef");

            VerifyResults(testHelper);
        }

        [Test]
        public void TestResolveEnvironmentVariables()
        {
            // Note: This is not an environment variable but the ResolveEnvironmentVariables doesn't really resolve any environment variable
            // so this is the best we can do until we kill this feature.

            var testHelper = new FileGeneratorTestHelper();
            testHelper.WriteLineWithFallback("$(name1)", null, "name1_value");

            // Verify that win64 environment resolver is available. If not it means one time setup fixture wasn't initialized correctly.
            var envVarResolver = PlatformRegistry.Get<IPlatformDescriptor>(Platform.win64).GetPlatformEnvironmentResolver();
            Assert.NotNull(envVarResolver);

            testHelper.Generator.ResolveEnvironmentVariables(Platform.win64,
                new VariableAssignment("name1", "name1_value"));

            VerifyResults(testHelper);
        }

        [Test]
        public void TestRemoveTaggedLines()
        {
            var testHelper = new FileGeneratorTestHelper();

            testHelper.WriteLine("abc");
            testHelper.WriteLine("somestring" + FileGeneratorUtilities.RemoveLineTag + "othertext");
            testHelper.WriteLine(FileGeneratorUtilities.RemoveLineTag);
            testHelper.WriteLine("def");

            testHelper.Generator.RemoveTaggedLines();
            VerifyResults(testHelper);
        }

        [Test]
        public void TestIsEmpty()
        {
            var testHelper = new FileGeneratorTestHelper();

            Assert.IsTrue(testHelper.Generator.IsEmpty());
            testHelper.Write("abc");
            Assert.IsFalse(testHelper.Generator.IsEmpty());
        }

        [Test]
        public void TestClear()
        {
            var testHelper = new FileGeneratorTestHelper();
            testHelper.Write("abc");
            testHelper.Clear();
            Assert.IsTrue(testHelper.Generator.IsEmpty());
        }

        [Test]
        public void TestWriteTo()
        {
            FileGenerator generator1 = new FileGenerator();
            generator1.WriteLine("abc");
            FileGenerator generator2 = new FileGenerator();
            generator1.WriteTo(generator2);

            string tmpFile1 = Path.GetTempFileName();
            try
            {
                // Check that generated content is different than default content of tmp file(empty)
                var fileInfo1 = new FileInfo(tmpFile1);

                // Write the file using reference stream
                Assert.IsTrue(generator1.FileWriteIfDifferent(fileInfo1, true));
                fileInfo1.Refresh();

                // Using second file to write a file using generator.
                Assert.IsFalse(generator2.IsFileDifferent(fileInfo1));
            }
            finally
            {
                File.Delete(tmpFile1);
            }
        }

        [Test]
        public void TestFileGeneratorConstructorsWithResolver()
        {
            Resolver resolver = new Resolver();
            var generator1 = new FileGenerator(resolver);
            Assert.AreSame(resolver, generator1.Resolver);
        }
    }
}
