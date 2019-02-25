using NUnit.Framework;
using Sharpmake;

namespace Sharpmake.UnitTests
{
    class OrderableStringsTests
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
