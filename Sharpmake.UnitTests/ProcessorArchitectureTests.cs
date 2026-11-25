// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    public class ProcessorArchitectureTests
    {
        private static Target T(Platform platform, ProcessorArchitecture arch, InstructionSetLevel level, DevEnv devEnv = DevEnv.vs2026)
        {
            return new Target(platform, devEnv, Optimization.Release) { ProcessorArchitecture = arch, InstructionSetLevel = level };
        }

        // A descriptor written before ProcessorArchitecture existed: implements nothing new.
        private class LegacyDescriptor : IPlatformDescriptor
        {
            public string SimplePlatformString => "Legacy";
            public string GetToolchainPlatformString(ITarget target) => "Legacy";
            public bool IsMicrosoftPlatform => false;
            public bool IsPcPlatform => false;
            public bool IsUsingClang => false;
            public bool IsLinkerInvokedViaCompiler => false;
            public bool HasDotNetSupport => false;
            public bool HasSharedLibrarySupport => false;
            public bool HasPrecompiledHeaderSupport => false;
            public EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] variables) => null;
        }

        // The effective values replace the unset architecture and level by the defaults, the other fragments are untouched
        [TestCase(Platform.win64, 0, 0, ProcessorArchitecture.x64, InstructionSetLevel.Baseline)]
        [TestCase(Platform.win64, ProcessorArchitecture.arm64, InstructionSetLevel.APX, ProcessorArchitecture.arm64, InstructionSetLevel.APX)]
        [TestCase(Platform.win32, 0, 0, ProcessorArchitecture.x86, InstructionSetLevel.Baseline)]
        public void GetEffectiveFragmentsValue_ResolvesDefaults(Platform platform, ProcessorArchitecture arch, InstructionSetLevel level, ProcessorArchitecture expectedArch, InstructionSetLevel expectedLevel)
        {
            var target = T(platform, arch, level);

            int[] raw = target.GetFragmentsValue();
            int[] effective = target.GetEffectiveFragmentsValue();
            var fields = target.GetFragmentFieldInfo();

            Assert.That(effective.Length, Is.EqualTo(raw.Length));
            for (int i = 0; i < raw.Length; ++i)
            {
                if (fields[i].FieldType == typeof(ProcessorArchitecture))
                    Assert.That(effective[i], Is.EqualTo((int)expectedArch));
                else if (fields[i].FieldType == typeof(InstructionSetLevel))
                    Assert.That(effective[i], Is.EqualTo((int)expectedLevel));
                else
                    Assert.That(effective[i], Is.EqualTo(raw[i]));
            }
        }

        [Test]
        public void LegacyDescriptor_FailsSafe()
        {
            IPlatformDescriptor legacy = new LegacyDescriptor();

            Assert.That((int)legacy.DefaultProcessorArchitecture, Is.EqualTo(0));
            Assert.That((int)legacy.SupportedProcessorArchitectures, Is.EqualTo(0));
            Assert.That(legacy.GetSupportedInstructionSetLevels(ProcessorArchitecture.x64), Is.EqualTo(InstructionSetLevel.Baseline));
        }

        [TestCase(Platform.win32, ProcessorArchitecture.x86)]
        [TestCase(Platform.win64, ProcessorArchitecture.x64)]
        [TestCase(Platform.linux, ProcessorArchitecture.x64)]
        public void GetProcessorArchitecture_Unset_ReturnsPlatformDefault(Platform platform, ProcessorArchitecture expected)
        {
            Assert.That(Util.GetProcessorArchitecture(T(platform, 0, 0)), Is.EqualTo(expected));
        }

        [TestCase(Platform.mac)]
        [TestCase(Platform.ios)]
        [TestCase(Platform.android)]
        public void GetProcessorArchitecture_PlatformWithoutDefault_ReturnsUnset(Platform platform)
        {
            Assert.That((int)Util.GetProcessorArchitecture(T(platform, 0, 0)), Is.EqualTo(0));
        }

        [Test]
        public void GetProcessorArchitecture_Explicit_Wins()
        {
            Assert.That(Util.GetProcessorArchitecture(T(Platform.win64, ProcessorArchitecture.arm64, 0)), Is.EqualTo(ProcessorArchitecture.arm64));
        }

        [Test]
        public void GetInstructionSetLevel_UnsetIsBaseline_ExplicitWins()
        {
            Assert.That(Util.GetInstructionSetLevel(T(Platform.win64, 0, 0)), Is.EqualTo(InstructionSetLevel.Baseline));
            Assert.That(Util.GetInstructionSetLevel(T(Platform.win64, 0, InstructionSetLevel.APX)), Is.EqualTo(InstructionSetLevel.APX));
        }

        [Test]
        public void GetResolvers_CustomTargetWithoutFragments_UsePlatformDefaults()
        {
            var target = new CustomTargetWithoutArchitecture { Platform = Platform.win64, DevEnv = DevEnv.vs2022 };

            Assert.That(Util.GetProcessorArchitecture(target), Is.EqualTo(ProcessorArchitecture.x64));
            Assert.That(Util.GetInstructionSetLevel(target), Is.EqualTo(InstructionSetLevel.Baseline));
        }

        private class CustomTargetWithoutArchitecture : ITarget
        {
            public Platform Platform;
            public DevEnv DevEnv;
        }

        [TestCase(Platform.win32, 0, 0)]
        [TestCase(Platform.win64, 0, 0)]
        [TestCase(Platform.mac, 0, 0)]
        [TestCase(Platform.android, 0, 0)]
        [TestCase(Platform.win64, ProcessorArchitecture.x64, 0)]
        [TestCase(Platform.win64, ProcessorArchitecture.arm64, 0)]
        [TestCase(Platform.win64, ProcessorArchitecture.x64, InstructionSetLevel.Baseline)]
        [TestCase(Platform.win64, 0, InstructionSetLevel.APX)]
        [TestCase(Platform.win64, ProcessorArchitecture.x64, InstructionSetLevel.APX)]
        [TestCase(Platform.win64, ProcessorArchitecture.arm64, InstructionSetLevel.Baseline)]
        [TestCase(Platform.win32, ProcessorArchitecture.x86, InstructionSetLevel.Baseline)]
        [TestCase(Platform.linux, ProcessorArchitecture.x64, 0)]
        [TestCase(Platform.mac, 0, InstructionSetLevel.Baseline)]
        public void Verify_ValidCombination_DoesNotThrow(Platform platform, ProcessorArchitecture arch, InstructionSetLevel level)
        {
            Assert.That(() => Util.VerifyProcessorArchitectureAndLevel(T(platform, arch, level)), Throws.Nothing);
        }

        [TestCase(Platform.win32, ProcessorArchitecture.arm64, 0, TestName = "win32+arm64")]
        [TestCase(Platform.win32, ProcessorArchitecture.x64, 0, TestName = "win32+x64")]
        [TestCase(Platform.win64, ProcessorArchitecture.x86, 0, TestName = "win64+x86")]
        [TestCase(Platform.mac, ProcessorArchitecture.arm64, 0, TestName = "mac+arm64 (not yet supported)")]
        [TestCase(Platform.android, ProcessorArchitecture.arm64, 0, TestName = "android+arm64 (not yet supported)")]
        [TestCase(Platform.linux, ProcessorArchitecture.arm64, 0, TestName = "linux+arm64 (not yet supported)")]
        [TestCase(Platform.win32, 0, InstructionSetLevel.APX, TestName = "win32+APX")]
        [TestCase(Platform.win32, ProcessorArchitecture.x86, InstructionSetLevel.APX, TestName = "win32+x86+APX")]
        [TestCase(Platform.win64, ProcessorArchitecture.arm64, InstructionSetLevel.APX, TestName = "win64+arm64+APX")]
        [TestCase(Platform.linux, 0, InstructionSetLevel.APX, TestName = "linux+APX")]
        [TestCase(Platform.mac, 0, InstructionSetLevel.APX, TestName = "mac+APX")]
        public void Verify_InvalidCombination_Throws(Platform platform, ProcessorArchitecture arch, InstructionSetLevel level)
        {
            var target = T(platform, arch, level);

            var ex = Assert.Throws<Error>(() => Util.VerifyProcessorArchitectureAndLevel(target));
            Assert.That(ex.Message, Does.Contain(target.GetTargetString()));
        }

        [TestCase(DevEnv.vs2015)]
        [TestCase(DevEnv.vs2017)]
        [TestCase(DevEnv.vs2019)]
        [TestCase(DevEnv.vs2022)]
        [TestCase(DevEnv.xcode)]
        [TestCase(DevEnv.make)]
        public void Verify_ApxWithOlderOrNonVisualStudioDevEnv_Throws(DevEnv devEnv)
        {
            var ex = Assert.Throws<Error>(() => Util.VerifyProcessorArchitectureAndLevel(T(Platform.win64, 0, InstructionSetLevel.APX, devEnv)));
            Assert.That(ex.Message, Does.Contain("vs2026"));
        }

        [Test]
        public void Verify_ApxWithVs2026_DoesNotThrow()
        {
            Assert.That(() => Util.VerifyProcessorArchitectureAndLevel(T(Platform.win64, 0, InstructionSetLevel.APX, DevEnv.vs2026)), Throws.Nothing);
        }
    }
}
