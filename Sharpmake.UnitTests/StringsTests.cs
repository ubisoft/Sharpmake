// Copyright (c) 2017 Ubisoft Entertainment
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
using Sharpmake;

namespace Sharpmake.UnitTests
{
    internal class OrderableStringsTests
    {
        [Test]
        public static void TestStableSortWithNoOrder()
        {
            OrderableStrings strings = new OrderableStrings();
            strings.Add("b");
            strings.Add("c");
            strings.Add("a");
            strings.Add("d");

            strings.StableSort();

            // Verify order is the expected one
            Assert.AreEqual(strings[0], "b");
            Assert.AreEqual(strings[1], "c");
            Assert.AreEqual(strings[2], "a");
            Assert.AreEqual(strings[3], "d");
        }

        [Test]
        public static void TestStableSortWithOrder()
        {
            OrderableStrings strings = new OrderableStrings();
            strings.Add("b");
            strings.Add("c", 1);
            strings.Add("a");
            strings.Add("d", -1);
            strings.Add("e");
            strings.Add("g", 2);
            strings.Add("f", -2);

            strings.StableSort();

            // Verify order is the expected one
            Assert.AreEqual(strings[0], "f");
            Assert.AreEqual(strings[1], "d");
            Assert.AreEqual(strings[2], "b");
            Assert.AreEqual(strings[3], "a");
            Assert.AreEqual(strings[4], "e");
            Assert.AreEqual(strings[5], "c");
            Assert.AreEqual(strings[6], "g");
        }
    }
}
