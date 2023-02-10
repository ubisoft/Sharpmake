// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    [TestFixture]
    public class JsonSerializerTest
    {
        private TextWriter _writer;
        private Util.JsonSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _writer = new StringWriter();
            _serializer = new Util.JsonSerializer(_writer);
        }

        [TearDown]
        public void TearDown()
        {
            _writer.Dispose();
            _serializer.Dispose();
        }

        [Test]
        public void Dispose()
        {
            _serializer.Dispose();

            Assert.Throws<ObjectDisposedException>(delegate
            { _writer.Write(string.Empty); });
            Assert.Throws<ObjectDisposedException>(delegate
            { _serializer.Serialize(string.Empty); });
        }

        [Test]
        public void SerializeNull()
        {
            _serializer.Serialize(null);
            Assert.That(_writer.ToString(), Is.EqualTo("null"));
        }

        [Test]
        public void SerializeEmptyArray()
        {
            _serializer.Serialize(new List<object>());
            Assert.That(_writer.ToString(), Is.EqualTo("[]"));
        }

        [Test]
        public void SerializeEmptyDictionary()
        {
            _serializer.Serialize(new Dictionary<string, object>());
            Assert.That(_writer.ToString(), Is.EqualTo("{}"));
        }

        [Test]
        public void SerializeString()
        {
            _serializer.Serialize(string.Empty);
            Assert.That(_writer.ToString(), Is.EqualTo("\"\""));
        }

        [Test]
        public void SerializeObject()
        {
            Assert.Throws<ArgumentException>(delegate
            { _serializer.Serialize(new object()); });
        }

        [Test]
        public void SerializeNumbers()
        {
            _serializer.Serialize((float)1);
            _serializer.Serialize((double)1);
            _serializer.Serialize((decimal)1);
            _serializer.Serialize((sbyte)1);
            _serializer.Serialize((short)1);
            _serializer.Serialize((int)1);
            _serializer.Serialize((long)1);
            _serializer.Serialize((byte)1);
            _serializer.Serialize((ushort)1);
            _serializer.Serialize((uint)1);
            _serializer.Serialize((ulong)1);
            Assert.That(_writer.ToString(), Is.EqualTo("11111111111"));
        }

        [Test]
        public void SerializeNegativeDouble()
        {
            _serializer.Serialize(-13.37);
            Assert.That(_writer.ToString(), Is.EqualTo("-13.37"));
        }

        [Test]
        public void SerializeTrue()
        {
            _serializer.Serialize(true);
            Assert.That(_writer.ToString(), Is.EqualTo("true"));
        }

        [Test]
        public void SerializeFalse()
        {
            _serializer.Serialize(false);
            Assert.That(_writer.ToString(), Is.EqualTo("false"));
        }

        [Test]
        public void SerializeNullableCharArray()
        {
            _serializer.Serialize(new char?[] { 'A', 'z', '\n', null });
            Assert.That(_writer.ToString(), Is.EqualTo("[\"A\",\"z\",\"\\n\",null]"));
        }

        [Test]
        public void SerializeDictionaries()
        {
            var inner = new Dictionary<object, object>()
            {
                { "qwerty", "keyboard"},
                { "one", (int?)2 },
                { "true", false },
            };
            var dict = new Dictionary<object, object>()
            {
                {"foo", inner },
                { "256", "The key is a string." }
            };

            _serializer.Serialize(dict);
            Assert.That(_writer.ToString(), Is.EqualTo("{\"foo\":{\"qwerty\":\"keyboard\",\"one\":2,\"true\":false},\"256\":\"The key is a string.\"}"));
        }

        [Test]
        public void SerializeBadDictionary()
        {
            var dict = new Dictionary<object, object>()
            {
                { 256, "The key is not a string." }
            };

            Assert.Throws<InvalidDataException>(delegate
            { _serializer.Serialize(dict); });
        }

        [Test]
        public void SerializeFormatArray()
        {
            _writer.NewLine = "\n";
            _serializer.IsOutputFormatted = true;
            _serializer.Serialize(new[] { 1, 2, 3, });
            Assert.That(_writer.ToString(), Is.EqualTo("[\n\t1,\n\t2,\n\t3\n]"));
        }

        [Test]
        public void SerializeFormatDictionary()
        {
            var dict = new Dictionary<object, object>()
            {
                { "A", 1 },
                { "B", 2 },
                { "C", 3 },
            };
            _writer.NewLine = "\n";
            _serializer.IsOutputFormatted = true;
            _serializer.Serialize(dict);
            Assert.That(_writer.ToString(), Is.EqualTo("{\n\t\"A\":1,\n\t\"B\":2,\n\t\"C\":3\n}"));
        }

        [Test]
        public void SerializeFormatArrayNestedDictionary()
        {
            var inner = new Dictionary<object, object>()
            {
                { "B", new [] { 1, 2 } },
                { "C", 3 },
            };
            var dict1 = new Dictionary<object, object>()
            {
                { "A", inner },
                { "D", 4 }
            };

            var dict2 = new Dictionary<object, object>()
            {
                { "E", 5 },
            };

            _writer.NewLine = "\n";
            _serializer.IsOutputFormatted = true;
            _serializer.Serialize(new[] { dict1, dict2 });
            Assert.That(_writer.ToString(), Is.EqualTo(
                "[\n" +
                "\t{\n" +
                "\t\t\"A\":{\n" +
                "\t\t\t\"B\":[\n" +
                "\t\t\t\t1,\n" +
                "\t\t\t\t2\n" +
                "\t\t\t],\n" +
                "\t\t\t\"C\":3\n" +
                "\t\t},\n" +
                "\t\t\"D\":4\n" +
                "\t},\n" +
                "\t{\n" +
                "\t\t\"E\":5\n" +
                "\t}\n" +
                "]"
           ));
        }

        [Test]
        public void SerializeFormatNestedEmpty()
        {
            _writer.NewLine = "\n";
            _serializer.IsOutputFormatted = true;
            _serializer.Serialize(new object[] { new Dictionary<object, object>(), new List<object>() });
            Assert.That(_writer.ToString(), Is.EqualTo("[\n\t{\n\t},\n\t[\n\t]\n]"));
        }

        [Test]
        public void SerializeCycle()
        {
            var first = new List<IEnumerable>();
            var second = new List<IEnumerable>();
            first.Add(second);
            second.Add(first);

            Assert.Throws<InvalidDataException>(delegate
            { _serializer.Serialize(first); });
        }

        [Test]
        public void EscapeNullString()
        {
            Assert.That(Util.JsonSerializer.EscapeJson(null), Is.EqualTo("null"));
            Assert.That(Util.JsonSerializer.EscapeJson(null, quote: false, nullValue: "foo"), Is.EqualTo("foo"));
            Assert.That(Util.JsonSerializer.EscapeJson("null", quote: false, nullValue: "foo"), Is.EqualTo("null"));
            Assert.That(Util.JsonSerializer.EscapeJson(null, quote: false, nullValue: null), Is.EqualTo(null));

            Assert.That(Util.JsonSerializer.EscapeJson(null, quote: true), Is.EqualTo("null"));
            Assert.That(Util.JsonSerializer.EscapeJson(null, quote: true, nullValue: null), Is.EqualTo(null));
            Assert.That(Util.JsonSerializer.EscapeJson(null, quote: true, nullValue: "foo"), Is.EqualTo("foo"));
            Assert.That(Util.JsonSerializer.EscapeJson("null", quote: true, nullValue: "foo"), Is.EqualTo("\"null\""));
        }

        [Test]
        public void EscapeEmptyString()
        {
            Assert.That(Util.JsonSerializer.EscapeJson(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(Util.JsonSerializer.EscapeJson(string.Empty, quote: true), Is.EqualTo("\"\""));
        }

        [Test]
        public void DoubleEscape()
        {
            Assert.That(Util.JsonSerializer.EscapeJson("~!@#$%/\\/</xml>^&*()_+a\\b\r\n\0"), Is.EqualTo("~!@#$%\\/\\\\\\/<\\/xml>^&*()_+a\\\\b\\r\\n\\u0000"));
        }

        [Test]
        public void EscapeCharacters()
        {
            Assert.That(Util.JsonSerializer.EscapeJson("\n"), Is.EqualTo("\\n"));
            Assert.That(Util.JsonSerializer.EscapeJson("\r"), Is.EqualTo("\\r"));
            Assert.That(Util.JsonSerializer.EscapeJson("\b"), Is.EqualTo("\\b"));
            Assert.That(Util.JsonSerializer.EscapeJson("\f"), Is.EqualTo("\\f"));
            Assert.That(Util.JsonSerializer.EscapeJson("\t"), Is.EqualTo("\\t"));
            Assert.That(Util.JsonSerializer.EscapeJson("\\"), Is.EqualTo("\\\\"));
            Assert.That(Util.JsonSerializer.EscapeJson("/"), Is.EqualTo("\\/"));
            Assert.That(Util.JsonSerializer.EscapeJson("\""), Is.EqualTo("\\\""));

            Assert.That(Util.JsonSerializer.EscapeJson("\n", quote: true), Is.EqualTo("\"\\n\""));
            Assert.That(Util.JsonSerializer.EscapeJson("\r", quote: true), Is.EqualTo("\"\\r\""));
            Assert.That(Util.JsonSerializer.EscapeJson("\b", quote: true), Is.EqualTo("\"\\b\""));
            Assert.That(Util.JsonSerializer.EscapeJson("\f", quote: true), Is.EqualTo("\"\\f\""));
            Assert.That(Util.JsonSerializer.EscapeJson("\t", quote: true), Is.EqualTo("\"\\t\""));
            Assert.That(Util.JsonSerializer.EscapeJson("\\", quote: true), Is.EqualTo("\"\\\\\""));
            Assert.That(Util.JsonSerializer.EscapeJson("/", quote: true), Is.EqualTo("\"\\/\""));
            Assert.That(Util.JsonSerializer.EscapeJson("\"", quote: true), Is.EqualTo("\"\\\"\""));
        }

        [Test]
        public void EscapeControlCharacters()
        {
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x00).ToString()), Is.EqualTo("\\u0000"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x01).ToString()), Is.EqualTo("\\u0001"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x02).ToString()), Is.EqualTo("\\u0002"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x03).ToString()), Is.EqualTo("\\u0003"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x04).ToString()), Is.EqualTo("\\u0004"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x05).ToString()), Is.EqualTo("\\u0005"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x06).ToString()), Is.EqualTo("\\u0006"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x07).ToString()), Is.EqualTo("\\u0007"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x08).ToString()), Is.EqualTo("\\b"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x09).ToString()), Is.EqualTo("\\t"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0A).ToString()), Is.EqualTo("\\n"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0B).ToString()), Is.EqualTo("\\u000B"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0C).ToString()), Is.EqualTo("\\f"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0D).ToString()), Is.EqualTo("\\r"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0E).ToString()), Is.EqualTo("\\u000E"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0F).ToString()), Is.EqualTo("\\u000F"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x10).ToString()), Is.EqualTo("\\u0010"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x11).ToString()), Is.EqualTo("\\u0011"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x12).ToString()), Is.EqualTo("\\u0012"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x13).ToString()), Is.EqualTo("\\u0013"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x14).ToString()), Is.EqualTo("\\u0014"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x15).ToString()), Is.EqualTo("\\u0015"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x16).ToString()), Is.EqualTo("\\u0016"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x17).ToString()), Is.EqualTo("\\u0017"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x18).ToString()), Is.EqualTo("\\u0018"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x19).ToString()), Is.EqualTo("\\u0019"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1A).ToString()), Is.EqualTo("\\u001A"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1B).ToString()), Is.EqualTo("\\u001B"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1C).ToString()), Is.EqualTo("\\u001C"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1D).ToString()), Is.EqualTo("\\u001D"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1E).ToString()), Is.EqualTo("\\u001E"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1F).ToString()), Is.EqualTo("\\u001F"));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x20).ToString()), Is.EqualTo(" "));

            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x00).ToString(), quote: true), Is.EqualTo("\"\\u0000\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x01).ToString(), quote: true), Is.EqualTo("\"\\u0001\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x02).ToString(), quote: true), Is.EqualTo("\"\\u0002\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x03).ToString(), quote: true), Is.EqualTo("\"\\u0003\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x04).ToString(), quote: true), Is.EqualTo("\"\\u0004\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x05).ToString(), quote: true), Is.EqualTo("\"\\u0005\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x06).ToString(), quote: true), Is.EqualTo("\"\\u0006\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x07).ToString(), quote: true), Is.EqualTo("\"\\u0007\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x08).ToString(), quote: true), Is.EqualTo("\"\\b\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x09).ToString(), quote: true), Is.EqualTo("\"\\t\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0A).ToString(), quote: true), Is.EqualTo("\"\\n\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0B).ToString(), quote: true), Is.EqualTo("\"\\u000B\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0C).ToString(), quote: true), Is.EqualTo("\"\\f\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0D).ToString(), quote: true), Is.EqualTo("\"\\r\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0E).ToString(), quote: true), Is.EqualTo("\"\\u000E\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x0F).ToString(), quote: true), Is.EqualTo("\"\\u000F\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x10).ToString(), quote: true), Is.EqualTo("\"\\u0010\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x11).ToString(), quote: true), Is.EqualTo("\"\\u0011\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x12).ToString(), quote: true), Is.EqualTo("\"\\u0012\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x13).ToString(), quote: true), Is.EqualTo("\"\\u0013\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x14).ToString(), quote: true), Is.EqualTo("\"\\u0014\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x15).ToString(), quote: true), Is.EqualTo("\"\\u0015\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x16).ToString(), quote: true), Is.EqualTo("\"\\u0016\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x17).ToString(), quote: true), Is.EqualTo("\"\\u0017\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x18).ToString(), quote: true), Is.EqualTo("\"\\u0018\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x19).ToString(), quote: true), Is.EqualTo("\"\\u0019\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1A).ToString(), quote: true), Is.EqualTo("\"\\u001A\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1B).ToString(), quote: true), Is.EqualTo("\"\\u001B\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1C).ToString(), quote: true), Is.EqualTo("\"\\u001C\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1D).ToString(), quote: true), Is.EqualTo("\"\\u001D\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1E).ToString(), quote: true), Is.EqualTo("\"\\u001E\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x1F).ToString(), quote: true), Is.EqualTo("\"\\u001F\""));
            Assert.That(Util.JsonSerializer.EscapeJson(((char)0x20).ToString(), quote: true), Is.EqualTo("\" \""));
        }
    }
}
