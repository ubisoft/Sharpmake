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

            Assert.AreEqual("b,c,a,d", strings.ToString());
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

            Assert.AreEqual("f", strings[0]);
            Assert.AreEqual("d", strings[1]);
            Assert.AreEqual("b", strings[2]);
            Assert.AreEqual("a", strings[3]);
            Assert.AreEqual("e", strings[4]);
            Assert.AreEqual("c", strings[5]);
            Assert.AreEqual("g", strings[6]);
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

            Assert.True(strings.Contains("u"));
            Assert.True(strings.Contains("h"));
            Assert.False(strings.Contains("w"));
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

            Assert.AreEqual(1, strings.IndexOf("E"));
            Assert.AreEqual(2, strings.IndexOf("O"));
            Assert.AreEqual(0, strings.IndexOf("W"));
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

            Assert.AreEqual("w,e,o", strings.ToString());
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

            Assert.AreEqual(3, strings.Count);
            Assert.AreEqual("E,Y,U", strings.ToString());
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

            Assert.AreEqual(2, strings.Count);
            Assert.AreEqual("W,U", strings.ToString());
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

            Assert.AreEqual(2, strings.Count);
            Assert.AreEqual("Y,I", strings.ToString());
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

            Assert.AreEqual(0, strings.Count);
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

            Assert.AreEqual(5, strings.Count);
            Assert.AreEqual("W,E,Y,U,I", strings.ToString());
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

            Assert.True(strings.Remove("W"));
            Assert.AreEqual("E,Y,U,I", strings.ToString());
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

            Assert.True(strings.Remove("I"));
            Assert.AreEqual("W,E,Y,U", strings.ToString());
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

            Assert.True(strings.Remove("U"));
            Assert.AreEqual("W,I", strings.ToString());
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

            Assert.False(strings.Remove("o"));
            Assert.AreEqual("W,E,Y,U,I", strings.ToString());
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

            Assert.AreEqual(stringsTestReturn.ToString(), stringsReturn.ToString());
            Assert.AreEqual("W,U", strings.ToString());
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

            Assert.AreEqual(stringsTestReturn.ToString(), stringsReturn.ToString());
            Assert.AreEqual(0, strings.Count);
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

            Assert.AreEqual(2, strings.Count);
            Assert.AreEqual("W,U", strings.ToString());
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

            Assert.AreEqual(0, strings.Count);
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

            Assert.AreEqual(0, strings.Count);
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

            Assert.AreEqual(3, strings.Count);
            Assert.AreEqual("W,h,J", strings.ToString());
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

            Assert.AreEqual(6, string2.Count);
            Assert.AreEqual(-4, string2.GetOrderNumber(0));
            Assert.AreEqual(-1, string2.GetOrderNumber(1));
            Assert.AreEqual(3, string2.GetOrderNumber(2));
            Assert.AreEqual(0, string2.GetOrderNumber(3));
            Assert.AreEqual(-3, string2.GetOrderNumber(4));
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

            Assert.AreEqual(4, strings.Count);
            Assert.AreEqual("A,Y,W,E", strings.ToString());
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

            Assert.AreEqual("A,Y,W,E,B", strings.ToString());

            Assert.AreEqual(-4, strings.GetOrderNumber(0));
            Assert.AreEqual(-4, strings.GetOrderNumber(2));
            Assert.AreEqual(1, strings.GetOrderNumber(3));
            Assert.AreEqual(0, strings.GetOrderNumber(4));
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

            Assert.AreEqual(4, strings.Count);
            Assert.AreEqual("A,Y,W,E", strings.ToString());
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

            Assert.AreEqual("ALLO,AYO,AUTO", strings.ToString());
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

            Assert.AreEqual("LLA,YA,UTA", strings.ToString());
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

            Assert.AreEqual("LLAA,YAA,UTA", strings.ToString());
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

            Assert.AreEqual("LLA,YA,UTA", strings.ToString());
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

            Assert.AreEqual(expectedStrings.ToString(), strings.ToString());
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

            Assert.AreEqual(expectedStrings.ToString(), strings.ToString());
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

            Assert.AreEqual("HLLA,HYA,HUT", strings.ToString());
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

            Assert.AreEqual("LLA,YA,test-insert,UT", strings.ToString());
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

            Assert.AreEqual("LLA-YA-UT", strings.JoinStrings("-"));
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
            Assert.AreEqual("HLLAA-HYAA-HUTA", strings.JoinStrings("-", "H", "A"));
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

            Assert.AreEqual("HLLA-HYA-HUT", strings.JoinStrings("-", "H"));
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

            Assert.AreEqual(0, strings.Count);
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

            Assert.AreEqual(0, strings.SetOrRemoveAtIndex(0, "H"));
            Assert.AreEqual("H,E,Y,U,I,A", strings.ToString());
            Assert.AreEqual(-4, strings.GetOrderNumber(0));
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

            Assert.AreEqual("W,E,Y,U,I,A", strings.ToString());
            Assert.AreEqual(0, strings.SetOrRemoveAtIndex(0, "W"));
            Assert.AreEqual(-4, strings.GetOrderNumber(0));
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

            Assert.AreEqual(0, strings.SetOrRemoveAtIndex(0, "H"));
            Assert.AreEqual(3, strings.SetOrRemoveAtIndex(4, "H"));
            Assert.AreEqual("H,E,Y,U,A", strings.ToString());
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

            Assert.AreEqual("W", strings.ToString());
            Assert.AreEqual(-4, strings.GetOrderNumber(0));
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

            Assert.AreEqual(new[] { "W", "E" }, returnArray);
        }
    }
}
