// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    public class ProcessorArchitectureToolchainTests
    {
        private static Target T(Platform platform, ProcessorArchitecture arch, InstructionSetLevel level)
        {
            return new Target(platform, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = arch, InstructionSetLevel = level };
        }

        [TestCase(Platform.win32, 0, 0, "Win32")]
        [TestCase(Platform.win64, 0, 0, "x64")]
        [TestCase(Platform.win64, ProcessorArchitecture.x64, InstructionSetLevel.Baseline, "x64")]
        [TestCase(Platform.win64, 0, InstructionSetLevel.APX, "x64")] // APX is not a Visual Studio platform
        [TestCase(Platform.win64, ProcessorArchitecture.arm64, 0, "ARM64")]
        [TestCase(Platform.linux, 0, 0, "x64")]
        public void ToolchainPlatformString_PerCombination(Platform platform, ProcessorArchitecture arch, InstructionSetLevel level, string expected)
        {
            var target = T(platform, arch, level);

            Assert.That(Util.GetToolchainPlatformString(platform, target), Is.EqualTo(expected));
            Assert.That(target.ToolchainPlatform, Is.EqualTo(expected));
        }

        [Test]
        public void ToolchainPlatformString_Win64WithAForeignArm64Target_StaysX64()
        {
            // Console descriptors ask win64 for its toolchain string with their own target (e.g. Scarlett, Durango)
            var foreignTarget = T(Platform.linux, ProcessorArchitecture.arm64, 0);

            Assert.That(Util.GetToolchainPlatformString(Platform.win64, foreignTarget), Is.EqualTo("x64"));
        }

        [Test]
        public void ToolchainPlatformString_Win64WithNullTarget_IsX64()
        {
            Assert.That(Util.GetToolchainPlatformString(Platform.win64, (ITarget)null), Is.EqualTo("x64"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ToolchainPlatformString_CSharpWin64Arm64_IsArm64(bool isForSolution)
        {
            var target = T(Platform.win64, ProcessorArchitecture.arm64, 0);

            Assert.That(Util.GetToolchainPlatformString(Platform.win64, new CSharpProject(), target, isForSolution), Is.EqualTo("ARM64"));
        }

        [Test]
        public void ToolchainPlatformString_CSharpWin64Default_IsUnchanged()
        {
            Assert.That(Util.GetToolchainPlatformString(Platform.win64, new CSharpProject(), T(Platform.win64, 0, 0), false), Is.EqualTo("x64"));
            Assert.That(Util.GetToolchainPlatformString(Platform.win64, new CSharpProject(), null, false), Is.EqualTo("x64"));
        }

        [TestCase(Platform.win32, 0, 0, "Win32")]
        [TestCase(Platform.win64, 0, 0, "x64")]
        [TestCase(Platform.win64, 0, InstructionSetLevel.APX, "x64-APX")]
        [TestCase(Platform.win64, ProcessorArchitecture.x64, InstructionSetLevel.APX, "x64-APX")]
        [TestCase(Platform.win64, ProcessorArchitecture.arm64, 0, "ARM64")]
        [TestCase(Platform.linux, 0, 0, "x64")]
        public void SolutionPlatformName_PerCombination(Platform platform, ProcessorArchitecture arch, InstructionSetLevel level, string expected)
        {
            Assert.That(Util.GetSolutionPlatformName(T(platform, arch, level)), Is.EqualTo(expected));
        }
    }

    public class ProcessorArchitectureSolutionTests : TestProjectBuilder
    {
        public ProcessorArchitectureSolutionTests()
            : base(InitType.Cpp, typeof(ProcessorArchitectureSolutionProjects.ArchitecturesSolution).Namespace, ConfigureOrder.New)
        {
        }

        [OneTimeSetUp]
        public void BuildOnce()
        {
            Init();
            BuildAndLinkProjects();
            Assert.That(ErrorCount, Is.Zero);
        }

        [Test]
        public void SolutionConfigurations_UseArchitectureAwarePlatformNames()
        {
            Solution solution = GetSolution<ProcessorArchitectureSolutionProjects.ArchitecturesSolution>();

            var platformNames = solution.Configurations.Select(c => c.PlatformName).OrderBy(n => n).ToArray();

            Assert.That(platformNames, Is.EqualTo(new[] { "ARM64", "x64", "x64-APX" }));
        }

        [Test]
        public void SolutionConfiguration_CustomPlatformName_IsNeverModified()
        {
            var conf = new Solution.Configuration { PlatformName = "Custom" };

            Assert.That(conf.PlatformName, Is.EqualTo("Custom"));
        }
    }

    namespace ProcessorArchitectureSolutionProjects
    {
        [Generate]
        public class ArchitecturesProject : Project
        {
            public ArchitecturesProject()
            {
                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(
                    new Target(Platform.win64, DevEnv.vs2026, Optimization.Release),
                    new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { InstructionSetLevel = InstructionSetLevel.APX },
                    new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
            }
        }

        [Generate]
        public class ArchitecturesSolution : Solution
        {
            public ArchitecturesSolution()
            {
                AddTargets(
                    new Target(Platform.win64, DevEnv.vs2026, Optimization.Release),
                    new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { InstructionSetLevel = InstructionSetLevel.APX },
                    new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddProject<ArchitecturesProject>(target);
            }
        }
    }
}
