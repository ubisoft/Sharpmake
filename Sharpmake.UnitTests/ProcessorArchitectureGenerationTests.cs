// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sharpmake.Generators;

namespace Sharpmake.UnitTests
{
    public class ProcessorArchitectureGenerationTests : CppTestProjectBuilder
    {
        public ProcessorArchitectureGenerationTests()
            : base(typeof(ProcessorArchitectureGenerationProjects.GenerationProject).Namespace)
        {
        }

        private class FakeGenerationContext : IGenerationContext
        {
            public FakeGenerationContext(Project project, Project.Configuration configuration)
            {
                Project = project;
                Configuration = configuration;
            }

            public Builder Builder => Builder.Instance;
            public Project Project { get; }
            public Project.Configuration Configuration { get; }
            public string ProjectDirectory => Configuration.ProjectPath;
            public DevEnv DevelopmentEnvironment => Configuration.Target.GetFragment<DevEnv>();
            public Options.ExplicitOptions Options { get; } = new Options.ExplicitOptions();
            public IDictionary<string, string> CommandLineOptions { get; } = new Dictionary<string, string>();
            public string ProjectDirectoryCapitalized => ProjectDirectory;
            public string ProjectSourceCapitalized => Project.SourceRootPath;
            public bool PlainOutput => true;
            public void SelectOption(params Sharpmake.Options.OptionAction[] options) { }
            public void SelectOptionWithFallback(Action fallbackAction, params Sharpmake.Options.OptionAction[] options) { }
        }

        private FakeGenerationContext ContextOf(ProcessorArchitecture architecture)
        {
            var project = GetProject<ProcessorArchitectureGenerationProjects.GenerationProject>();
            var conf = project.Configurations.Single(c => ((Target)c.Target).ProcessorArchitecture == architecture);
            return new FakeGenerationContext(project, conf);
        }

        [TestCase(ProcessorArchitecture.x64, "MachineX64", "/MACHINE:X64", "-fwin64")]
        [TestCase(ProcessorArchitecture.arm64, "MachineARM64", "/MACHINE:ARM64", "-fwin64")]
        public void Win64_TargetMachine_FollowsTheArchitecture(ProcessorArchitecture architecture, string machine, string commandLineMachine, string nasmFormat)
        {
            var context = ContextOf(architecture);

            new Windows.Win64Platform().SetupPlatformTargetOptions(context);

            Assert.That(context.Options["TargetMachine"], Is.EqualTo(machine));
            Assert.That(context.CommandLineOptions["TargetMachine"], Is.EqualTo(commandLineMachine));
            Assert.That(context.CommandLineOptions["NasmCompilerFormat"], Is.EqualTo(nasmFormat));
        }

        private FakeGenerationContext ApxContextOf(Optimization optimization)
        {
            var project = GetProject<ProcessorArchitectureGenerationProjects.ApxToolsetProject>();
            var conf = project.Configurations.Single(c => ((Target)c.Target).Optimization == optimization);
            return new FakeGenerationContext(project, conf);
        }

        [Test]
        public void Win64_ApxWithTheDefaultToolset_IsValid()
        {
            var context = ApxContextOf(Optimization.Release);

            Assert.DoesNotThrow(() => new Windows.Win64Platform().SetupPlatformTargetOptions(context));
        }

        [Test]
        public void Win64_ApxWithAnOlderToolset_IsAnError()
        {
            var context = ApxContextOf(Optimization.Debug);

            var ex = Assert.Throws<Error>(() => new Windows.Win64Platform().SetupPlatformTargetOptions(context));
            Assert.That(ex.Message, Does.Contain("APX"));
            Assert.That(ex.Message, Does.Contain("v143"));
        }

        [TestCase(ProcessorArchitecture.x64, ".win64Config")]
        [TestCase(ProcessorArchitecture.arm64, ".winarm64Config")]
        public void Win64_FastBuildConfigName_FollowsTheArchitecture(ProcessorArchitecture architecture, string expected)
        {
            var conf = ContextOf(architecture).Configuration;

            Assert.That(new Windows.Win64Platform().CConfigName(conf), Is.EqualTo(expected));
        }
    }

    namespace ProcessorArchitectureGenerationProjects
    {
        [Sharpmake.Generate]
        public class ApxToolsetProject : Project
        {
            public ApxToolsetProject()
            {
                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Debug | Optimization.Release) { InstructionSetLevel = InstructionSetLevel.APX });
            }

            [Configure(Optimization.Debug)]
            public void ConfigureDebug(Configuration conf, Target target)
            {
                conf.Options.Add(Options.Vc.General.PlatformToolset.v143);
            }
        }

        [Sharpmake.Generate]
        public class GenerationProject : Project
        {
            public GenerationProject()
            {
                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64 });
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
            }
        }
    }
}
