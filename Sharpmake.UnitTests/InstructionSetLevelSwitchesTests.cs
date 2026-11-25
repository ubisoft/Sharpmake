// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using NUnit.Framework;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.UnitTests
{
    public class InstructionSetLevelSwitchesTests
    {
        private static Target T(ProcessorArchitecture architecture, InstructionSetLevel level)
        {
            return new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = architecture, InstructionSetLevel = level };
        }

        [TestCase("Msvc", "/feature:APX")]
        [TestCase("ClangCl", "/clang:-mapxf")]
        [TestCase("ClangGnu", "-mapxf")]
        public void ApxTarget_EmitsTheSwitchOfTheDriver(string driverName, string expected)
        {
            var target = T(0, InstructionSetLevel.APX);
            var driver = System.Enum.Parse<CompilerDriver>(driverName);

            Assert.That(InstructionSetLevelSwitches.GetCompilerSwitch(target, driver), Is.EqualTo(expected));
        }

        [Test]
        public void BaselineTarget_EmitsNothing()
        {
            Assert.That(InstructionSetLevelSwitches.GetCompilerSwitch(T(0, 0), CompilerDriver.Msvc), Is.Null);
            Assert.That(InstructionSetLevelSwitches.GetCompilerSwitch(T(ProcessorArchitecture.arm64, 0), CompilerDriver.Msvc), Is.Null);
        }

        // ---- toolchain requirements of the APX level ----

        private static string NoClang() { throw new System.InvalidOperationException("the clang version must not be needed"); }

        [TestCase(Options.Vc.General.PlatformToolset.Default)]
        [TestCase(Options.Vc.General.PlatformToolset.v145)]
        public void ApxTarget_WithTheVs2026Toolset_IsValid(Options.Vc.General.PlatformToolset toolset)
        {
            Assert.DoesNotThrow(() => InstructionSetLevelSwitches.VerifyToolchain(T(0, InstructionSetLevel.APX), toolset, false, NoClang));
        }

        [TestCase(Options.Vc.General.PlatformToolset.v143)]
        [TestCase(Options.Vc.General.PlatformToolset.v142)]
        [TestCase(Options.Vc.General.PlatformToolset.v140_xp)]
        public void ApxTarget_WithAnOlderToolset_IsAnError(Options.Vc.General.PlatformToolset toolset)
        {
            var ex = Assert.Throws<Error>(() => InstructionSetLevelSwitches.VerifyToolchain(T(0, InstructionSetLevel.APX), toolset, false, NoClang));
            Assert.That(ex.Message, Does.Contain("APX"));
            Assert.That(ex.Message, Does.Contain(toolset.ToString()));
        }

        [TestCase(Options.Vc.General.PlatformToolset.v143)]
        [TestCase(Options.Vc.General.PlatformToolset.v140_xp)]
        public void BaselineTarget_WithAnOlderToolset_IsValid(Options.Vc.General.PlatformToolset toolset)
        {
            Assert.DoesNotThrow(() => InstructionSetLevelSwitches.VerifyToolchain(T(0, 0), toolset, false, NoClang));
        }

        [TestCase("22.1.0")]
        [TestCase("22")]
        [TestCase("23.0.1")]
        public void ApxTarget_WithClang22OrNewer_IsValid(string version)
        {
            Assert.DoesNotThrow(() => InstructionSetLevelSwitches.VerifyToolchain(T(0, InstructionSetLevel.APX), Options.Vc.General.PlatformToolset.ClangCL, true, () => version));
        }

        [TestCase("21.1.8")]
        [TestCase("7.0.0")]
        public void ApxTarget_WithAnOlderClang_IsAnError(string version)
        {
            var ex = Assert.Throws<Error>(() => InstructionSetLevelSwitches.VerifyToolchain(T(0, InstructionSetLevel.APX), Options.Vc.General.PlatformToolset.LLVM, true, () => version));
            Assert.That(ex.Message, Does.Contain(version));
            Assert.That(ex.Message, Does.Contain("22"));
        }

        [Test]
        public void ApxTarget_WithClangToolsetButMicrosoftCompiler_DoesNotNeedAClangVersion()
        {
            // UseClangCl.Disable builds with cl.exe
            Assert.DoesNotThrow(() => InstructionSetLevelSwitches.VerifyToolchain(T(0, InstructionSetLevel.APX), Options.Vc.General.PlatformToolset.LLVM, false, NoClang));
        }
    }
}
