// Copyright (c) 2019-2020 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    internal static class StringsTests
    {
        /// <summary>
        ///     Verify if the <c>Strings</c> is returned to a string object
        /// </summary>
        [Test]
        public static void TestToString()
        {
            Strings strings = new Strings("aa", "bb", "cc");

            Assert.AreEqual("aa,bb,cc", strings.ToString());
        }

        /// <summary>
        ///     Verify if the returned characters are lowered
        /// </summary>
        [Test]
        public static void TestToLower()
        {
            Strings strings = new Strings("TEST", "AA", "BB", "CC");
            Strings expectedStrings = new Strings("test", "aa", "bb", "cc");

            strings.ToLower();

            Assert.AreEqual(expectedStrings, strings);
        }

        /// <summary>
        ///     Verify if the prefix and suffix were added to the <c>Strings</c>
        /// </summary>
        [Test]
        public static void TestInsertPrefixSuffix()
        {
            Strings strings = new Strings("test", "aa", "bb", "cc");
            Strings expectedStrings = new Strings("atestb", "aaab", "abbb", "accb");

            strings.InsertPrefixSuffix("a", "b");

            Assert.AreEqual(expectedStrings, strings);
        }

        /// <summary>
        ///     Verify if the suffix was added to the string when absent or the original string
        /// </summary>
        [Test]
        public static void TestInsertSuffixAbsent()
        {
            Strings strings = new Strings("test", "aa", "cc", "ppp");
            Strings expectedStrings = new Strings("test", "aat", "cct", "pppt");

            strings.InsertSuffix("t", true);

            Assert.AreEqual(expectedStrings, strings);
        }

        /// <summary>
        ///     Verify if the suffix was added to the strings if it's absent or not
        /// </summary>
        [Test]
        public static void TestInsertSuffixOnlyIfAbsentFalse()
        {
            Strings strings = new Strings("test", "ahhh", "abc", "jjjj");
            Strings expectedStrings = new Strings("testto", "ahhhto", "abcto", "jjjjto");

            strings.InsertSuffix("to", false);

            Assert.AreEqual(expectedStrings, strings);
        }

        /// <summary>
        ///     Verify if the suffix was added to the strings
        /// </summary>
        [Test]
        public static void TestInsertSuffix()
        {
            Strings strings = new Strings("test", "abc", "alll", "efgh");
            Strings expecteStrings = new Strings("testt", "abct", "alllt", "efght");

            strings.InsertSuffix("t");

            Assert.AreEqual(expecteStrings, strings);
        }

        /// <summary>
        ///     Verify if the prefix was added to the strings
        /// </summary>
        [Test]
        public static void TestInsertPrefix()
        {
            Strings strings = new Strings("test", "defg", "klm", "qrst");
            Strings expectedStrings = new Strings("abctest", "abcdefg", "abcklm", "abcqrst");

            strings.InsertPrefix("abc");

            Assert.AreEqual(expectedStrings, strings);
        }

        /// <summary>
        ///     Verify if the prefix and suffix were added to the strings
        /// </summary>
        [Test]
        public static void TestJoinStringsPrefixSuffix()
        {
            Strings strings1 = new Strings("test", "test-no-escape");
            Strings strings2 = new Strings("<escape-test1>", "&escape-test2&");

            Assert.AreEqual("prefixtestsuffixprefixtest-no-escapesuffix", strings1.JoinStrings("", "prefix", "suffix", false));
            Assert.AreEqual("a&amp;escape-test2&amp;ba&lt;escape-test1&gt;b", strings2.JoinStrings("", "a", "b", true));
        }

        /// <summary>
        ///     Verify if the prefix were added to the strings
        /// </summary>
        [Test]
        public static void TestJoinStringsPrefix()
        {
            Strings strings1 = new Strings("test", "-escape");
            Strings strings3 = new Strings("<escape-test>", "&escape-test&");

            Assert.AreEqual("prefix-escapeprefixtest", strings1.JoinStrings("", "prefix", false));
            Assert.AreEqual("1&amp;escape-test&amp;1&lt;escape-test&gt;", strings3.JoinStrings("", "1", true));
        }
    }
}
