// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
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
            Assert.That(target.HaveFragmentOfSameType(fragment), Is.EqualTo(expectedResult));
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

        [Test]
        public void ProcessorArchitectureAndInstructionSetLevel_DefaultToZero()
        {
            var target = new Target(Platform.win64, DevEnv.vs2022, Optimization.Release);

            Assert.That((int)target.ProcessorArchitecture, Is.EqualTo(0));
            Assert.That((int)target.InstructionSetLevel, Is.EqualTo(0));
        }

        [Test]
        public void Initializer_WithArchitectureAndLevel_SetsFragments()
        {
            var target = new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.APX };

            Assert.That(target.GetFragment<ProcessorArchitecture>(), Is.EqualTo(ProcessorArchitecture.x64));
            Assert.That(target.GetFragment<InstructionSetLevel>(), Is.EqualTo(InstructionSetLevel.APX));
        }

        [Test]
        public void GetTargetString_WithDefaultArchitectureAndLevel_IsUnchanged()
        {
            var legacy = new Target(Platform.win64, DevEnv.vs2022, Optimization.Release);
            var explicitZero = new Target(Platform.win64, DevEnv.vs2022, Optimization.Release) { ProcessorArchitecture = 0, InstructionSetLevel = 0 };

            Assert.That(explicitZero.GetTargetString(), Is.EqualTo(legacy.GetTargetString()));
        }

        [Test]
        public void GetTargetString_WithArchitectureOrLevel_IsDistinct()
        {
            var baseline = new Target(Platform.win64, DevEnv.vs2026, Optimization.Release);
            var arm64 = new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 };
            var apx = new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { InstructionSetLevel = InstructionSetLevel.APX };

            var strings = new[] { baseline.GetTargetString(), arm64.GetTargetString(), apx.GetTargetString() };
            Assert.That(strings, Is.Unique);
        }

        [Test]
        public void Clone_PreservesArchitectureAndLevel()
        {
            var target = new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.APX };

            var clone = (Target)target.Clone();

            Assert.That(clone.ProcessorArchitecture, Is.EqualTo(ProcessorArchitecture.x64));
            Assert.That(clone.InstructionSetLevel, Is.EqualTo(InstructionSetLevel.APX));
        }

        [Test]
        public void Targets_ArchitectureMask_ExpandsToOneTargetPerValue()
        {
            var targets = new Targets(typeof(Target),
                new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64 | ProcessorArchitecture.arm64 });

            Assert.That(targets.TargetObjects.Count(), Is.EqualTo(2));
            Assert.That(targets.TargetObjects.Select(t => ((Target)t).ProcessorArchitecture),
                Is.EquivalentTo(new[] { ProcessorArchitecture.x64, ProcessorArchitecture.arm64 }));
        }

        [Test]
        public void Targets_LevelMask_ExpandsToOneTargetPerValue()
        {
            var targets = new Targets(typeof(Target),
                new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { InstructionSetLevel = InstructionSetLevel.Baseline | InstructionSetLevel.APX });

            Assert.That(targets.TargetObjects.Count(), Is.EqualTo(2));
            Assert.That(targets.TargetObjects.Select(t => ((Target)t).InstructionSetLevel),
                Is.EquivalentTo(new[] { InstructionSetLevel.Baseline, InstructionSetLevel.APX }));
        }

        [Test]
        public void Targets_WithoutArchitectureOrLevel_KeepsThemUnset()
        {
            var targets = new Targets(typeof(Target),
                new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));

            Assert.That(targets.TargetObjects.Count(), Is.EqualTo(2));
            Assert.That(targets.TargetObjects.All(t => (int)((Target)t).ProcessorArchitecture == 0 && (int)((Target)t).InstructionSetLevel == 0));
        }

        [Test]
        public void AddTargetsFromTargets_KeepsTheCallerOfTheSource()
        {
            var source = new Targets(typeof(Target));
            source.AddTargets("Source.cs(1): ", new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
            var destination = new Targets(typeof(Target));

            destination.AddTargets("Destination.cs(2): ", source);

            string message = destination.DescribeInvalidTarget(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug), "bad target");
            Assert.That(message, Does.StartWith("Source.cs(1): "));
        }

        [Test]
        public void AddTargetsFromTargets_WithoutSourceCaller_FallsBackToThisCall()
        {
            // The Targets constructor records no caller
            var source = new Targets(typeof(Target), new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug | Optimization.Release));
            var destination = new Targets(typeof(Target));

            destination.AddTargets("Destination.cs(2): ", source);

            string message = destination.DescribeInvalidTarget(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug), "bad target");
            Assert.That(message, Does.StartWith("Destination.cs(2): "));
        }
    }
}
