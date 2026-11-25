// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sharpmake.Generators;

namespace Sharpmake.UnitTests
{
    // Keeps the content of the generated files in memory instead of writing them
    internal class CapturingBuildContext : BuildContext.BaseBuildContext
    {
        public Dictionary<string, string> Files { get; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        public override bool WriteLog => false;

        public override bool WriteGeneratedFile(System.Type type, FileInfo path, MemoryStream generated)
        {
            Files[path.FullName] = new UTF8Encoding(false).GetString(generated.ToArray());
            return true;
        }

        public override bool WriteGeneratedFile(System.Type type, FileInfo path, IFileGenerator generator)
        {
            var temp = new FileInfo(Path.GetTempFileName());
            try
            {
                generator.FileWriteIfDifferent(temp, bypassAutoCleanupDatabase: true);
                Files[path.FullName] = File.ReadAllText(temp.FullName);
            }
            finally
            {
                temp.Delete();
            }
            return true;
        }
    }

    public abstract class GeneratedOutputTestBase : CppTestProjectBuilder
    {
        private CapturingBuildContext _context;

        protected GeneratedOutputTestBase(string namespaceName)
            : base(namespaceName)
        {
        }

        protected override BuildContext.BaseBuildContext CreateBuildContext(bool debugLog, bool writeFiles)
        {
            _context = new CapturingBuildContext();
            return _context;
        }

        protected IEnumerable<KeyValuePair<string, string>> GeneratedFiles() => _context.Files;

        protected string GeneratedFile(string fileNameEnd)
        {
            return _context.Files.Single(f => f.Key.EndsWith(fileNameEnd, System.StringComparison.OrdinalIgnoreCase)).Value;
        }

        // the text of the ItemDefinitionGroup of one project configuration
        protected static string ItemDefinitionOf(string vcxproj, string configuration)
        {
            string header = $"<ItemDefinitionGroup Condition=\"'$(Configuration)|$(Platform)'=='{configuration}'\">";
            int start = vcxproj.IndexOf(header, System.StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), header);
            int end = vcxproj.IndexOf("</ItemDefinitionGroup>", start, System.StringComparison.Ordinal);
            return vcxproj.Substring(start, end - start);
        }
    }

    public class ProcessorArchitectureApxSwitchTests : GeneratedOutputTestBase
    {
        public ProcessorArchitectureApxSwitchTests()
            : base(typeof(ProcessorArchitectureApxSwitchProjects.ApxSwitchProject).Namespace)
        {
        }

        private string Vcxproj => GeneratedFile("ApxSwitchProject.vcxproj");

        [Test]
        public void ApxLevel_EmitsTheSwitch()
        {
            Assert.That(ItemDefinitionOf(Vcxproj, "Debug APX|x64"), Does.Contain("/feature:APX"));
            Assert.That(ItemDefinitionOf(Vcxproj, "Release APX|x64"), Does.Contain("/feature:APX"));
        }

        [Test]
        public void BaselineLevel_DoesNotEmitTheSwitch()
        {
            Assert.That(ItemDefinitionOf(Vcxproj, "Retail|x64"), Does.Not.Contain("/feature:APX"));
        }

        [Test]
        public void ApxLevel_KeepsTheEnhancedInstructionSetOfTheProject()
        {
            foreach (string configuration in new[] { "Debug APX|x64", "Release APX|x64", "Retail|x64" })
            {
                Assert.That(ItemDefinitionOf(Vcxproj, configuration), Does.Contain("<EnableEnhancedInstructionSet>AdvancedVectorExtensions2</EnableEnhancedInstructionSet>"), configuration);
            }
        }
    }

    public class ProcessorArchitectureVcxprojTests : GeneratedOutputTestBase
    {
        public ProcessorArchitectureVcxprojTests()
            : base(typeof(ProcessorArchitectureOutputProjects.ArchitectureOutputProject).Namespace)
        {
        }

        private string Vcxproj => GeneratedFile("ArchitectureOutputProject.vcxproj");

        private string Sln => GeneratedFile("ArchitectureOutputSolution.sln");

        private string ItemDefinitionOf(string configuration) => ItemDefinitionOf(Vcxproj, configuration);

        [Test]
        public void Vcxproj_TargetMachineFollowsTheArchitecture()
        {
            Assert.That(ItemDefinitionOf("Release|x64"), Does.Contain("<TargetMachine>MachineX64</TargetMachine>"));
            Assert.That(ItemDefinitionOf("Release APX|x64"), Does.Contain("<TargetMachine>MachineX64</TargetMachine>"));
            Assert.That(ItemDefinitionOf("Release|ARM64"), Does.Contain("<TargetMachine>MachineARM64</TargetMachine>"));
        }

        [Test]
        public void Vcxproj_ApxSwitchIsOnlyInTheApxConfiguration()
        {
            Assert.That(ItemDefinitionOf("Release APX|x64"), Does.Contain("/feature:APX"));
            Assert.That(ItemDefinitionOf("Release|x64"), Does.Not.Contain("/feature:APX"));
            Assert.That(ItemDefinitionOf("Release|ARM64"), Does.Not.Contain("/feature:APX"));
        }

        [Test]
        public void Vcxproj_OutputFoldersHaveNoSpace()
        {
            var folders = System.Text.RegularExpressions.Regex.Matches(Vcxproj, "<(OutDir|IntDir)>([^<]*)</")
                .Select(m => m.Groups[2].Value)
                .ToList();

            Assert.That(folders, Has.Count.EqualTo(6));
            Assert.That(folders, Has.All.Not.Contain(" "));
            Assert.That(folders, Does.Contain("output\\win64apx\\release\\"));
            Assert.That(folders, Does.Contain("obj\\win64apx\\release\\"));
            Assert.That(folders, Does.Contain("output\\win64armv8\\release\\"));
        }

        [Test]
        public void Sln_HasADistinctSolutionPlatformPerArchitectureAndLevel()
        {
            Assert.That(Sln, Does.Contain("Release|x64 = Release|x64"));
            Assert.That(Sln, Does.Contain("Release|x64-APX = Release|x64-APX"));
            Assert.That(Sln, Does.Contain("Release|ARM64 = Release|ARM64"));
            Assert.That(Sln, Does.Contain(".Release|x64-APX.ActiveCfg = Release APX|x64"));
            Assert.That(Sln, Does.Contain(".Release|ARM64.ActiveCfg = Release|ARM64"));
            Assert.That(Sln, Does.Contain(".Release|x64.ActiveCfg = Release|x64"));
        }

        [Test]
        public void Vcxproj_HasOneConfigurationPerArchitectureAndLevel()
        {
            Assert.That(Vcxproj, Does.Contain("Include=\"Release|x64\""));
            Assert.That(Vcxproj, Does.Contain("Include=\"Release APX|x64\""));
            Assert.That(Vcxproj, Does.Contain("Include=\"Release|ARM64\""));
        }
    }

    public class ProcessorArchitectureBffTests : GeneratedOutputTestBase
    {
        public ProcessorArchitectureBffTests()
            : base(typeof(ProcessorArchitectureBffProjects.BffArchitectureProject).Namespace)
        {
        }

        private string Bff => GeneratedFile("BffArchitectureProject.bff");

        // the text of one Library() block of the project bff
        private string LibraryBlock(string name)
        {
            string header = $"Library( '{name}' )";
            int start = Bff.IndexOf(header, System.StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), header);
            int end = Bff.IndexOf("Alias(", start, System.StringComparison.Ordinal);
            return Bff.Substring(start, end - start);
        }

        [Test]
        public void Bff_UsesTheToolchainConfigOfTheArchitecture()
        {
            Assert.That(LibraryBlock("BffArchitectureProject_Release_Win64_Library"), Does.Contain("Using( .win64Config )"));
            Assert.That(LibraryBlock("BffArchitectureProject_Release_APX_Win64APX_Library"), Does.Contain("Using( .win64Config )"), "APX is x64 code");
            Assert.That(LibraryBlock("BffArchitectureProject_Release_Win64Armv8_Library"), Does.Contain("Using( .winarm64Config )"));
        }

        [Test]
        public void Bff_ApxSwitchIsOnlyInTheApxLibrary()
        {
            Assert.That(LibraryBlock("BffArchitectureProject_Release_APX_Win64APX_Library"), Does.Contain("/feature:APX"));
            Assert.That(LibraryBlock("BffArchitectureProject_Release_Win64_Library"), Does.Not.Contain("/feature:APX"));
            Assert.That(LibraryBlock("BffArchitectureProject_Release_Win64Armv8_Library"), Does.Not.Contain("/feature:APX"));
        }

        [Test]
        public void Bff_FoldersAndNamesHaveNoSpace()
        {
            Assert.That(LibraryBlock("BffArchitectureProject_Release_APX_Win64APX_Library"), Does.Contain("\\obj\\win64apx\\release\\"));
            Assert.That(LibraryBlock("BffArchitectureProject_Release_APX_Win64APX_Library"), Does.Contain("\\output\\win64apx\\release\\bffarchitectureproject.lib"));
            Assert.That(LibraryBlock("BffArchitectureProject_Release_Win64Armv8_Library"), Does.Contain("\\output\\win64armv8\\release\\bffarchitectureproject.lib"));
        }
    }

    namespace ProcessorArchitectureBffProjects
    {
        [Sharpmake.Generate]
        public class BffArchitectureProject : Project
        {
            public BffArchitectureProject()
            {
                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.Baseline | InstructionSetLevel.APX });
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.ProjectPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                conf.Output = Configuration.OutputType.Lib;
                conf.IsFastBuild = true;
            }
        }

        [Sharpmake.Generate]
        public class BffArchitectureSolution : Solution
        {
            public BffArchitectureSolution()
            {
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.Baseline | InstructionSetLevel.APX });
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.SolutionPath = Directory.GetCurrentDirectory() + "/[solution.Name]";
                conf.AddProject<BffArchitectureProject>(target);
            }
        }
    }

    namespace ProcessorArchitectureApxSwitchProjects
    {
        [Sharpmake.Generate]
        public class ApxSwitchProject : Project
        {
            public ApxSwitchProject()
            {
                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Debug | Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.APX });
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Retail) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.Baseline });
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.ProjectPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                conf.Output = Configuration.OutputType.Lib;
                conf.Options.Add(Options.Vc.Compiler.EnhancedInstructionSet.AdvancedVectorExtensions2);
            }
        }
    }

    namespace ProcessorArchitectureOutputProjects
    {
        [Sharpmake.Generate]
        public class ArchitectureOutputProject : Project
        {
            public ArchitectureOutputProject()
            {
                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.Baseline | InstructionSetLevel.APX });
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.ProjectPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                conf.Output = Configuration.OutputType.Lib;
            }
        }

        [Sharpmake.Generate]
        public class ArchitectureOutputSolution : Solution
        {
            public ArchitectureOutputSolution()
            {
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.Baseline | InstructionSetLevel.APX });
                AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
            }

            [Configure]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.SolutionPath = Directory.GetCurrentDirectory() + "/[solution.Name]";
                conf.AddProject<ArchitectureOutputProject>(target);
            }
        }
    }
}
