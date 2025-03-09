// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    public class TargetTests
    {
        [Test]
        public void GetFragment_WithExistingField_ReturnsExpected()
        {
            var target = new Target(Platform.win64, DevEnv.VisualStudio, Optimization.Release);

            var devEnv = target.GetFragment<DevEnv>();

            Assert.That(devEnv, Is.EqualTo(DevEnv.VisualStudio));
        }

        [Test]
        public void GetFragment_WithNonexistentField_ThrowsException()
        {
            var target = new Target(Platform.win64, DevEnv.VisualStudio, Optimization.Release);

            Assert.That(() => target.GetFragment<IncludeType>(), Throws.Exception);
        }

        [Test]
        public void SetFragment_SetValue_ReturnsSameValue()
        {
            var target = new Target(Platform.win64, DevEnv.VisualStudio, Optimization.Release);

            target.SetFragment(Optimization.Debug);

            var optimization = target.GetFragment<Optimization>();
            Assert.That(optimization, Is.EqualTo(Optimization.Debug));
        }

        [Test]
        public void SetFragment_WithIndirection_SetValue_ReturnsSameValue()
        {
            var target = new Target(Platform.win64, DevEnv.VisualStudio, Optimization.Release);
            object o = Optimization.Debug;

            target.SetFragment(o);

            var optimization = target.GetFragment<Optimization>();
            Assert.That(optimization, Is.EqualTo(Optimization.Debug));
        }

        [TestCase(Optimization.Release, true)]
        [TestCase(Optimization.Debug, false)]
        [TestCase(Optimization.Retail, false)]
        [TestCase(IncludeType.Relative, false)]
        [TestCase(Optimization.Release | Optimization.Debug, false)]
        [TestCase(Optimization.Release | Optimization.Retail, false)]
        [TestCase(Optimization.Release | Optimization.Debug | Optimization.Retail, false)]
        public void TestFragment_With1Argument_ReturnsExpected(object test, bool expected)
        {
            var target = new Target(Platform.win64, DevEnv.VisualStudio, Optimization.Release);

            // TestFragment expect to have at least all values in parameter set in the field to be true
            // Other values in the field are ignored
            var result = target.TestFragment(test);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(Optimization.Release, true)]
        [TestCase(Optimization.Debug, true)]
        [TestCase(Optimization.Retail, false)]
        [TestCase(IncludeType.Relative, false)]
        [TestCase(Optimization.Release | Optimization.Debug, true)]
        [TestCase(Optimization.Release | Optimization.Retail, false)]
        [TestCase(Optimization.Release | Optimization.Debug | Optimization.Retail, false)]
        public void TestFragment_With2Arguments_ReturnsExpected(object test, bool expected)
        {
            var target = new Target(Platform.win64, DevEnv.VisualStudio, Optimization.Release | Optimization.Debug);

            // TestFragment expect to have at least all values in parameter set in the field to be true
            // Other values in the field are ignored
            var result = target.TestFragment(test);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void HaveFragment_WithExistingField_ReturnsTrue()
        {
            var target = new Target(Platform.win64, DevEnv.VisualStudio, Optimization.Release | Optimization.Debug);

            var result = target.HaveFragment<Optimization>();

            Assert.That(result, Is.True);
        }

        [Test]
        public void HaveFragment_WithNonexistentField_ReturnsFalse()
        {
            var target = new Target(Platform.win64, DevEnv.VisualStudio, Optimization.Release | Optimization.Debug);

            var result = target.HaveFragment<IncludeType>();

            Assert.That(result, Is.False);
        }

        [Flags, Fragment]
        enum BogusFragment
        {
            one = 1 << 0,
            two = 1 << 1
        }

        [TestCase(Optimization.Release, true)]
        [TestCase(Optimization.Debug, true)]
        [TestCase(Platform.win64, true)]
        [TestCase(OutputType.Dll, true)]
        [TestCase(BogusFragment.one, false)]
        public void HaveFragmentOfSameType_returnsExpected(object fragment, bool expectedResult)
        {
            var target = new Target();
            Assert.AreEqual(expectedResult, target.HaveFragmentOfSameType(fragment));
        }

        [TestCase(Optimization.Release, true)]
        [TestCase(Optimization.Debug, false)]
        [TestCase(Optimization.Retail, false)]
        [TestCase(IncludeType.Relative, true)] // Verify the behavior: nonexistent field returns true
        [TestCase(Optimization.Release | Optimization.Debug, true)]
        [TestCase(Optimization.Release | Optimization.Retail, true)]
        [TestCase(Optimization.Release | Optimization.Debug | Optimization.Retail, true)]
        public void AndMask_With1Argument_ReturnsExpected(object test, bool expected)
        {
            var target = new Target(Platform.win64, DevEnv.VisualStudio, Optimization.Release);

            // AndMask expect to have at least all value in field set in the parameter to be true
            // Other values in the parameter are ignored
            var result = target.AndMask(test);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(Optimization.Release, false)]
        [TestCase(Optimization.Debug, false)]
        [TestCase(Optimization.Retail, false)]
        [TestCase(IncludeType.Relative, true)] // Verify the behavior: nonexistent field returns true
        [TestCase(Optimization.Release | Optimization.Debug, true)]
        [TestCase(Optimization.Release | Optimization.Retail, false)]
        [TestCase(Optimization.Release | Optimization.Debug | Optimization.Retail, true)]
        public void AndMask_With2Arguments_ReturnsExpected(object test, bool expected)
        {
            var target = new Target(Platform.win64, DevEnv.VisualStudio, Optimization.Release | Optimization.Debug);

            // AndMask expect to have at least all value in field set in the parameter to be true
            // Other values in the parameter are ignored
            var result = target.AndMask(test);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
