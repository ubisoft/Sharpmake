// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    internal static class OrderableStringsTests
    {
        /// <summary>
        ///     Verify order is the expected one
        /// </summary>
        [Test]
        public static void TestStableSortWithNoOrder()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "b",
                    "c",
                    "a",
                    "d"
                };

            strings.StableSort();

            Assert.That(strings.ToString(), Is.EqualTo("b,c,a,d"));
        }

        /// <summary>
        ///     Verify elements were ordered accordingly to their order
        /// </summary>
        [Test]
        public static void TestStableSortWithOrder()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "b",
                    { "c", 1 },
                    "a",
                    { "d", -1 },
                    "e",
                    { "g", 2 },
                    { "f", -2 }
                };

            strings.StableSort();

            Assert.That(strings[0], Is.EqualTo("f"));
            Assert.That(strings[1], Is.EqualTo("d"));
            Assert.That(strings[2], Is.EqualTo("b"));
            Assert.That(strings[3], Is.EqualTo("a"));
            Assert.That(strings[4], Is.EqualTo("e"));
            Assert.That(strings[5], Is.EqualTo("c"));
            Assert.That(strings[6], Is.EqualTo("g"));
        }

        /// <summary>
        ///     verify if the method <c>Contains</c> return the right value depending on the element with or without order
        /// </summary>
        [Test]
        public static void TestContainsReturnNoOrder()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "u",
                    {"h", -1}
                };

            Assert.That(strings.Contains("u"), Is.True);
            Assert.That(strings.Contains("h"), Is.True);
            Assert.That(strings.Contains("w"), Is.False);
        }

        /// <summary>
        ///     Verify if the returned value is the right index
        /// </summary>
        [Test]
        public static void TestIndexOf()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    "E",
                    "O"
                };

            Assert.That(strings.IndexOf("E"), Is.EqualTo(1));
            Assert.That(strings.IndexOf("O"), Is.EqualTo(2));
            Assert.That(strings.IndexOf("W"), Is.EqualTo(0));
        }

        /// <summary>
        ///     verify if the returned character have the right format
        /// </summary>
        [Test]
        public static void TestToLower()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    "O"
                };

            strings.ToLower();

            Assert.That(strings.ToString(), Is.EqualTo("w,e,o"));
        }

        /// <summary>
        ///     verify if the first and last element were removed
        /// </summary>
        [Test]
        public static void TestRemoveRangeFirstLast()
        {
            IEnumerable<string> stringsList = new List<string>() { "W", "I" };
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    "I"
                };

            strings.RemoveRange(stringsList);

            Assert.That(strings.Count, Is.EqualTo(3));
            Assert.That(strings.ToString(), Is.EqualTo("E,Y,U"));
        }

        /// <summary>
        ///     verify if the elements in between the last and first were removed
        /// </summary>
        [Test]
        public static void TestRemoveRangeBetweenLast()
        {
            IEnumerable<string> stringsList = new List<string>() { "E", "Y", "I" };
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    "I"
                };

            strings.RemoveRange(stringsList);

            Assert.That(strings.Count, Is.EqualTo(2));
            Assert.That(strings.ToString(), Is.EqualTo("W,U"));
        }

        /// <summary>
        ///     verify if the first element and element in between were removed
        /// </summary>
        [Test]
        public static void TestRemoveRangeBetweenFirst()
        {
            IEnumerable<string> stringsList = new List<string>() { "E", "U", "W" };
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    "I"
                };

            strings.RemoveRange(stringsList);

            Assert.That(strings.Count, Is.EqualTo(2));
            Assert.That(strings.ToString(), Is.EqualTo("Y,I"));
        }

        /// <summary>
        ///     verify if all elements were removed
        /// </summary>
        [Test]
        public static void TestRemoveRangeAllElements()
        {
            IEnumerable<string> stringsList = new List<string>() { "I", "Y", "E", "U", "W" };
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    "I"
                };

            strings.RemoveRange(stringsList);

            Assert.That(strings.Count, Is.EqualTo(0));
        }

        /// <summary>
        ///     Verify that no element was removed
        /// </summary>
        [Test]
        public static void TestRemoveRangeNotInclude()
        {
            IEnumerable<string> stringsList = new List<string>() { "l", "a", "f", "q" };
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    "I"
                };

            strings.RemoveRange(stringsList);

            Assert.That(strings.Count, Is.EqualTo(5));
            Assert.That(strings.ToString(), Is.EqualTo("W,E,Y,U,I"));
        }

        /// <summary>
        ///     Verify if the first element was removed
        /// </summary>
        [Test]
        public static void TestRemoveFirst()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    "I"
                };

            Assert.That(strings.Remove("W"), Is.True);
            Assert.That(strings.ToString(), Is.EqualTo("E,Y,U,I"));
        }

        /// <summary>
        ///     Verify if the last element was removed
        /// </summary>
        [Test]
        public static void TestRemoveLast()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    "I"
                };

            Assert.That(strings.Remove("I"), Is.True);
            Assert.That(strings.ToString(), Is.EqualTo("W,E,Y,U"));
        }

        /// <summary>
        ///     Verify if the elements in between were removed
        /// </summary>
        [Test]
        public static void TestRemoveBetween()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"U", 0},
                    "I"
                };

            Assert.That(strings.Remove("U"), Is.True);
            Assert.That(strings.ToString(), Is.EqualTo("W,I"));
        }

        /// <summary>
        ///     Verify if no element was removed
        /// </summary>
        [Test]
        public static void TestRemoveNotInclude()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    "I"
                };

            Assert.That(strings.Remove("o"), Is.False);
            Assert.That(strings.ToString(), Is.EqualTo("W,E,Y,U,I"));
        }

        /// <summary>
        ///     Verify if the right elements were removed or kept
        /// </summary>
        [Test]
        public static void TestIntersectWithRest()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    "I"
                };
            IEnumerable<string> stringsList = new List<string>() { "W", "a", "f", "U" };
            Strings stringsReturn = new Strings();
            Strings stringsTestReturn = new Strings
                {
                    "a",
                    "E",
                    "f",
                    "I",
                    "Y"
                };

            strings.IntersectWith(stringsList, stringsReturn);

            Assert.That(stringsReturn.ToString(), Is.EqualTo(stringsTestReturn.ToString()));
            Assert.That(strings.ToString(), Is.EqualTo("W,U"));
        }


        /// <summary>
        ///     Verify if every elements were removed from the <c>OrderableStrings</c> and added to <c>stringsReturn</c>
        /// </summary>
        [Test]
        public static void TestNoIntersectWithRest()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3}
                };
            IEnumerable<string> stringsList = new List<string>() { "h", "J", "a", "b" };
            Strings stringsReturn = new Strings();
            Strings stringsTestReturn = new Strings
                {
                    "a",
                    "b",
                    "E",
                    "h",
                    "J",
                    "W",
                    "Y"
                };

            strings.IntersectWith(stringsList, stringsReturn);

            Assert.That(stringsReturn.ToString(), Is.EqualTo(stringsTestReturn.ToString()));
            Assert.That(strings.Count, Is.EqualTo(0));
        }

        /// <summary>
        ///     verify if the right elements were removed 
        /// </summary>
        [Test]
        public static void TestIntersectWithNoRest()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    "I"
                };

            IEnumerable<string> stringsList = new List<string>() { "W", "a", "f", "U" };

            strings.IntersectWith(stringsList);

            Assert.That(strings.Count, Is.EqualTo(2));
            Assert.That(strings.ToString(), Is.EqualTo("W,U"));
        }

        /// <summary>
        ///     verify if every element were removed
        /// </summary>
        [Test]
        public static void TestNoIntersectWith()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3}
                };
            IEnumerable<string> stringsList = new List<string>() { "h", "J", "a", "b" };

            strings.IntersectWith(stringsList);

            Assert.That(strings.Count, Is.EqualTo(0));
        }

        /// <summary>
        ///     verify if every element were removed
        /// </summary>
        [Test]
        public static void TestNoIntersectWithNoRest()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W",
                    {"E", -1},
                    {"Y", 3}
                };

            IEnumerable<string> stringsList = new List<string>() { "h", "J" };

            strings.IntersectWith(stringsList);

            Assert.That(strings.Count, Is.EqualTo(0));
        }

        /// <summary>
        ///     verify if every element were added from the List
        /// </summary>
        [Test]
        public static void TestAddRangeIEnumerable()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "W"
                };

            IEnumerable<string> stringsList = new List<string>() { "h", "J" };

            strings.AddRange(stringsList);

            Assert.That(strings.Count, Is.EqualTo(3));
            Assert.That(strings.ToString(), Is.EqualTo("W,h,J"));
        }

        /// <summary>
        ///     verify if the orders were added
        /// </summary>
        [Test]
        public static void TestAddRangeOrderableStrings()
        {
            OrderableStrings string1 = new OrderableStrings
                {
                    {"W", -4},
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    {"I", -3},
                    {"A", 6 }
                };

            OrderableStrings string2 = new OrderableStrings
                {
                    "W",
                    "E",
                    "Y",
                    "U",
                    "I"
                };

            string2.AddRange(string1);

            Assert.That(string2.Count, Is.EqualTo(6));
            Assert.That(string2.GetOrderNumber(0), Is.EqualTo(-4));
            Assert.That(string2.GetOrderNumber(1), Is.EqualTo(-1));
            Assert.That(string2.GetOrderNumber(2), Is.EqualTo(3));
            Assert.That(string2.GetOrderNumber(3), Is.EqualTo(0));
            Assert.That(string2.GetOrderNumber(4), Is.EqualTo(-3));
        }

        /// <summary>
        ///     Verify if adding a range to an element that already has a range throw an error
        /// </summary>
        [Test]
        public static void TestAddRangeOrderableStringsError()
        {
            OrderableStrings string1 = new OrderableStrings
                {
                    {"W", -4},
                    {"E", -1}
                };

            OrderableStrings string2 = new OrderableStrings
                {
                    {"W", -3},
                    "E"
                };

            Assert.Throws<Sharpmake.Error>(() => string2.AddRange(string1));
        }

        /// <summary>
        ///     Verify if adding a range to an element that already has a range throw an error
        /// </summary>
        [Test]
        public static void TestAddString()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "A",
                    "Y"
                };

            strings.Add("W");
            strings.Add("E");
            strings.Add("A");

            Assert.That(strings.Count, Is.EqualTo(4));
            Assert.That(strings.ToString(), Is.EqualTo("A,Y,W,E"));
        }

        /// <summary>
        ///     Verify if the values and the orders were added
        /// </summary>
        [Test]
        public static void TestAddStringInt()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "A",
                    {"Y", -1}
                };

            strings.Add("W", -4);
            strings.Add("A", -4);
            strings.Add("E", 1);
            strings.Add("B", 0);

            Assert.That(strings.ToString(), Is.EqualTo("A,Y,W,E,B"));

            Assert.That(strings.GetOrderNumber(0), Is.EqualTo(-4));
            Assert.That(strings.GetOrderNumber(2), Is.EqualTo(-4));
            Assert.That(strings.GetOrderNumber(3), Is.EqualTo(1));
            Assert.That(strings.GetOrderNumber(4), Is.EqualTo(0));
        }

        /// <summary>
        ///     Verify if adding an element that already exist throw an error
        /// </summary>
        [Test]
        public static void TestAddStringIntError()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "A",
                    {"Y", -1}
                };

            strings.Add("W", -4);
            strings.Add("A", -4);
            strings.Add("E", 1);
            strings.Add("B", 0);

            Assert.Throws<Sharpmake.Error>(() => strings.Add("Y", -2));
        }

        /// <summary>
        ///     Verify if the array of strings was added
        /// </summary>
        [Test]
        public static void TestAddStringTab()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "A",
                    "Y"
                };

            string[] stringTab = { "W", "E" };

            strings.Add(stringTab);

            Assert.That(strings.Count, Is.EqualTo(4));
            Assert.That(strings.ToString(), Is.EqualTo("A,Y,W,E"));
        }

        /// <summary>
        ///     Verify if the prefix and suffix were added to all elements
        /// </summary>
        [Test]
        public static void TestInsertPrefixSuffix()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "LL",
                    "Y",
                    {"UT", -3 }
                };

            strings.InsertPrefixSuffix("A", "O");

            Assert.That(strings.ToString(), Is.EqualTo("ALLO,AYO,AUTO"));
        }

        /// <summary>
        ///     Verify if the suffix were added to all elements
        /// </summary>
        [Test]
        public static void TestInsertSuffix()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "LL",
                    "Y",
                    {"UT", -3 }
                };

            strings.InsertSuffix("A");

            Assert.That(strings.ToString(), Is.EqualTo("LLA,YA,UTA"));
        }

        /// <summary>
        ///     Verify if the suffix were added to all elements
        /// </summary>
        [Test]
        public static void TestInsertSuffixOnlyIfNotAbsent()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "LLA",
                    "YA",
                    {"UT", -3 }
                };

            strings.InsertSuffix("A", false);

            Assert.That(strings.ToString(), Is.EqualTo("LLAA,YAA,UTA"));
        }

        /// <summary>
        ///     Verify if the suffix was added on elements that previously don't have the suffix
        /// </summary>
        [Test]
        public static void TestInsertSuffixOnlyIfAbsent()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "LLA",
                    "YA",
                    {"UT", -3 }
                };

            strings.InsertSuffix("A", true);

            Assert.That(strings.ToString(), Is.EqualTo("LLA,YA,UTA"));
        }

        /// <summary>
        ///     Verify if the suffix was added to the string when absent or the original string
        /// </summary>
        [Test]
        public static void TestInsertSuffixAbsentWithAdditionalSuffixes()
        {
            OrderableStrings strings = new OrderableStrings { "libatomic.a", "libatomic.so", "libcurl", "libthreads.dll" };
            Strings expectedStrings = new Strings("libatomic.a", "libatomic.so", "libcurl.a", "libthreads.dll");

            strings.InsertSuffix(".a", true, new[] { ".so", ".dll" });

            Assert.That(strings.ToString(), Is.EqualTo(expectedStrings.ToString()));
        }

        /// <summary>
        ///     Verify if the suffix was added to the strings if it's absent or not
        /// </summary>
        [Test]
        public static void TestInsertSuffixOnlyIfAbsentFalseWithAdditionalSuffixes()
        {
            OrderableStrings strings = new OrderableStrings { "libatomic.a", "libatomic.so", "libcurl", "libthreads.dll" };
            Strings expectedStrings = new Strings("libatomic.a.a", "libatomic.so.a", "libcurl.a", "libthreads.dll.a");

            strings.InsertSuffix(".a", false, new[] { ".so", ".dll" });

            Assert.That(strings.ToString(), Is.EqualTo(expectedStrings.ToString()));
        }


        /// <summary>
        ///     Verify if the prefix was added
        /// </summary>
        [Test]
        public static void TestInsertPrefix()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "LLA",
                    "YA",
                    {"UT", -3 }
                };

            strings.InsertPrefix("H");

            Assert.That(strings.ToString(), Is.EqualTo("HLLA,HYA,HUT"));
        }

        /// <summary>
        ///     Verify if the element was added to the <c>OrderableStrings</c> at the right index
        /// </summary>
        [Test]
        public static void TestInsert()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "LLA",
                    "YA",
                    {"UT", -3 }
                };

            strings.Insert(2, "test-insert");

            Assert.That(strings.ToString(), Is.EqualTo("LLA,YA,test-insert,UT"));
        }

        /// <summary>
        ///     Verify if the separator was added between the elements 
        /// </summary>
        [Test]
        public static void TestJoinSeparator()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "LLA",
                    "YA",
                    {"UT", -3 }
                };

            Assert.That(strings.JoinStrings("-"), Is.EqualTo("LLA-YA-UT"));
        }

        /// <summary>
        ///     Verify the separator, prefix and suffix was added to the <c>OrderableStrings</c>
        /// </summary>
        [Test]
        public static void TestJoinStringsSeparator()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "LLA",
                    "YA",
                    {"UT", -3 }
                };

            //
            Assert.That(strings.JoinStrings("-", "H", "A"), Is.EqualTo("HLLAA-HYAA-HUTA"));
        }

        /// <summary>
        ///     Verify the separator and prefix was added to the <c>OrderableStrings</c>
        /// </summary>
        [Test]
        public static void TestJoinStringsSeparatorPrefix()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "LLA",
                    "YA",
                    {"UT", -3 }
                };

            Assert.That(strings.JoinStrings("-", "H"), Is.EqualTo("HLLA-HYA-HUT"));
        }

        /// <summary>
        ///     Verify that all element were remove
        /// </summary>
        [Test]
        public static void TestClear()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    "LLA",
                    "YA",
                    {"UT", -3 }
                };

            strings.Clear();

            Assert.That(strings.Count, Is.EqualTo(0));
        }

        /// <summary>
        ///     Verify the value of the right element was changed without changing the order's value
        /// </summary>
        [Test]
        public static void TestSetAtIndex()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    {"W", -4},
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    {"I", -3},
                    {"A", 6 }
                };

            Assert.That(strings.SetOrRemoveAtIndex(0, "H"), Is.EqualTo(0));
            Assert.That(strings.ToString(), Is.EqualTo("H,E,Y,U,I,A"));
            Assert.That(strings.GetOrderNumber(0), Is.EqualTo(-4));
        }

        /// <summary>
        ///     Verify that it has kept the element from the giving index without changing the order's value
        /// </summary>
        [Test]
        public static void TestSetAtIndexSameValue()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    {"W", -4},
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    {"I", -3},
                    {"A", 6 }
                };

            Assert.That(strings.ToString(), Is.EqualTo("W,E,Y,U,I,A"));
            Assert.That(strings.SetOrRemoveAtIndex(0, "W"), Is.EqualTo(0));
            Assert.That(strings.GetOrderNumber(0), Is.EqualTo(-4));
        }

        /// <summary>
        ///     Verify that the element from giving the index is removed 
        /// </summary>
        [Test]
        public static void TestRemoveAtIndex()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    {"W", -4},
                    {"E", -1},
                    {"Y", 3},
                    {"U", 0},
                    {"I", -3},
                    {"A", 6 }
                };

            Assert.That(strings.SetOrRemoveAtIndex(0, "H"), Is.EqualTo(0));
            Assert.That(strings.SetOrRemoveAtIndex(4, "H"), Is.EqualTo(3));
            Assert.That(strings.ToString(), Is.EqualTo("H,E,Y,U,A"));
        }

        /// <summary>
        ///     Verify that the element from giving the index is removed 
        /// </summary>
        [Test]
        public static void TestRemoveAt()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    {"W", -4},
                    {"E", -1},
                };

            strings.RemoveAt(1);

            Assert.That(strings.ToString(), Is.EqualTo("W"));
            Assert.That(strings.GetOrderNumber(0), Is.EqualTo(-4));
        }

        /// <summary>
        ///     Verify if the <c>OrderableStrings</c> was copy in the array
        /// </summary>
        [Test]
        public static void TestCopyTo()
        {
            OrderableStrings strings = new OrderableStrings
                {
                    {"W", -4},
                    {"E", -1},
                };

            string[] returnArray = new string[2];

            strings.CopyTo(returnArray, 0);

            Assert.That(returnArray, Is.EqualTo(new[] { "W", "E" }));
        }
    }
}
