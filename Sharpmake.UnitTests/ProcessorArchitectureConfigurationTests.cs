// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    public class ProcessorArchitectureInvalidTargetTests : TestProjectBuilder
    {
        public ProcessorArchitectureInvalidTargetTests()
            : base(InitType.Cpp, typeof(ProcessorArchitectureProjects.Invalid.Win32Arm64Project).Namespace, ConfigureOrder.New)
        {
        }

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            Init();
        }

        [Test]
        public void ProjectWithUnsupportedArchitecture_ThrowsWhenConfiguring()
        {
            var projects = new[] { typeof(ProcessorArchitectureProjects.Invalid.Win32Arm64Project) };

            var ex = Assert.Throws<Error>(() => GenerateAndBuildProjects(projects));
            Assert.That(ex.Message, Does.Contain("processor architecture arm64 is not supported"));
            Assert.That(ex.Message, Does.Contain("platform Win32"));
            Assert.That(ex.Message, Does.Contain("AddTargets"), "the caller is named");
            Assert.That(ex.Message, Does.Not.Contain("mask"), "a single target is not a mask");
        }
    }

    public class ProcessorArchitectureMaskTests : TestProjectBuilder
    {
        public ProcessorArchitectureMaskTests()
            : base(InitType.Cpp, typeof(ProcessorArchitectureProjects.Masks.Win32Win64ApxMaskProject).Namespace, ConfigureOrder.New)
        {
        }

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            Init();
        }

        [Test]
        public void InvalidCombinationOfAMask_NamesTheMaskAndTheAddTargetsCaller()
        {
            var projects = new[] { typeof(ProcessorArchitectureProjects.Masks.Win32Win64ApxMaskProject) };

            var ex = Assert.Throws<Error>(() => GenerateAndBuildProjects(projects));
            Assert.That(ex.Message, Does.Contain("instruction set level APX"));
            Assert.That(ex.Message, Does.Contain("AddTargets"));
            Assert.That(ex.Message, Does.Contain("ProcessorArchitectureConfigurationTests.cs"));
            Assert.That(ex.Message, Does.Contain("win32, win64"), "the mask is shown");
            Assert.That(ex.Message, Does.Contain("expands to an invalid combination"), "the message is about the mask");
            Assert.That(ex.Message, Does.Contain("Split the AddTargets"));
        }
    }

    public class ProcessorArchitectureCSharpTargetTests : TestProjectBuilder
    {
        public ProcessorArchitectureCSharpTargetTests()
            : base(InitType.CSharp, typeof(ProcessorArchitectureProjects.CSharpApx.CSharpApxProject).Namespace, ConfigureOrder.New)
        {
        }

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            Init();
        }

        [Test]
        public void CSharpProjectWithNonBaselineLevel_ThrowsWhenConfiguring()
        {
            var projects = new[] { typeof(ProcessorArchitectureProjects.CSharpApx.CSharpApxProject) };

            var ex = Assert.Throws<Error>(() => GenerateAndBuildProjects(projects));
            Assert.That(ex.Message, Does.Contain("C#"));
            Assert.That(ex.Message, Does.Contain("APX"));
        }
    }

    public class ProcessorArchitectureValidTargetTests : CppTestProjectBuilder
    {
        public ProcessorArchitectureValidTargetTests()
            : base(typeof(ProcessorArchitectureProjects.Valid.Win64ArchitecturesProject).Namespace)
        {
        }

        [Test]
        public void Win64WithArchitectureAndLevelMasks_ConfiguresOneConfigurationPerTarget()
        {
            var project = GetProject<ProcessorArchitectureProjects.Valid.Win64ArchitecturesProject>();

            // x64 (default), x64+APX, arm64
            Assert.That(project.Configurations.Count(), Is.EqualTo(3));
            Assert.That(project.Configurations.Select(c => ((Target)c.Target).GetTargetString()).Distinct().Count(), Is.EqualTo(3));
        }
    }

    namespace ProcessorArchitectureProjects
    {
        namespace Invalid
        {
            [Sharpmake.Generate]
            public class Win32Arm64Project : Project
            {
                public Win32Arm64Project()
                {
                    SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                    AddTargets(new Target(Platform.win32, DevEnv.vs2022, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
                }
            }
        }

        namespace Masks
        {
            [Sharpmake.Generate]
            public class Win32Win64ApxMaskProject : Project
            {
                public Win32Win64ApxMaskProject()
                {
                    SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                    // the win32 part of the mask is not a valid combination
                    AddTargets(new Target(Platform.win32 | Platform.win64, DevEnv.vs2026, Optimization.Release) { InstructionSetLevel = InstructionSetLevel.APX });
                }
            }
        }

        namespace CSharpApx
        {
            [Sharpmake.Generate]
            public class CSharpApxProject : CSharpProject
            {
                public CSharpApxProject()
                {
                    SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { InstructionSetLevel = InstructionSetLevel.APX });
                }
            }
        }

        namespace Valid
        {
            [Sharpmake.Generate]
            public class Win64ArchitecturesProject : Project
            {
                public Win64ArchitecturesProject()
                {
                    SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";

                    // default architecture, no level
                    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release));
                    // x64 with APX
                    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.APX });
                    // arm64
                    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
                }
            }
        }
    }
}
