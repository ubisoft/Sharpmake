// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using NUnit.Framework;
using Sharpmake.Generators.FastBuild;

namespace Sharpmake.UnitTests
{
    public class ProcessorArchitectureNamesTests
    {
        private static Target T(Platform platform, ProcessorArchitecture arch, InstructionSetLevel level, DevEnv devEnv = DevEnv.vs2026)
        {
            return new Target(platform, devEnv, Optimization.Release) { ProcessorArchitecture = arch, InstructionSetLevel = level };
        }

        // ---- target string ----

        [Test]
        public void GetTargetString_LegacyTarget_IsPinned()
        {
            // Guards that existing targets keep their exact string (it feeds paths, unity names and the duplicate check).
            var target = new Target(Platform.win64, DevEnv.vs2022, Optimization.Release);

            Assert.That(target.GetTargetString(), Is.EqualTo("NoBlob_MSBuild_vs2022_v4_7_2_Release_Lib_Win64"));
        }

        [TestCase(Platform.win64, ProcessorArchitecture.x64)]
        [TestCase(Platform.win32, ProcessorArchitecture.x86)]
        [TestCase(Platform.linux, ProcessorArchitecture.x64)]
        public void GetTargetString_ExplicitDefaultArchitecture_EqualsUnset(Platform platform, ProcessorArchitecture defaultArchitecture)
        {
            string unset = T(platform, 0, 0).GetTargetString();

            Assert.That(T(platform, defaultArchitecture, 0).GetTargetString(), Is.EqualTo(unset));
            Assert.That(T(platform, defaultArchitecture, InstructionSetLevel.Baseline).GetTargetString(), Is.EqualTo(unset));
            Assert.That(T(platform, 0, InstructionSetLevel.Baseline).GetTargetString(), Is.EqualTo(unset));
        }

        [Test]
        public void GetTargetString_NonDefaultArchitectureOrLevel_IsDistinctAndNamed()
        {
            string unset = T(Platform.win64, 0, 0).GetTargetString();
            string arm64 = T(Platform.win64, ProcessorArchitecture.arm64, 0).GetTargetString();
            string apx = T(Platform.win64, 0, InstructionSetLevel.APX).GetTargetString();

            Assert.That(new[] { unset, arm64, apx }, Is.Unique);
            Assert.That(arm64, Does.Contain("arm64"));
            Assert.That(apx, Does.Contain("APX"));
        }

        // ---- [Configure(...)] matching: AndMask ----

        [TestCase(ProcessorArchitecture.x64, true)]
        [TestCase(ProcessorArchitecture.arm64, false)]
        [TestCase(ProcessorArchitecture.x64 | ProcessorArchitecture.arm64, true)]
        public void AndMask_UnsetArchitecture_UsesPlatformDefault(object mask, bool expected)
        {
            Assert.That(T(Platform.win64, 0, 0).AndMask(mask), Is.EqualTo(expected));
        }

        [TestCase(InstructionSetLevel.Baseline, true)]
        [TestCase(InstructionSetLevel.APX, false)]
        public void AndMask_UnsetLevel_IsBaseline(object mask, bool expected)
        {
            Assert.That(T(Platform.win64, 0, 0).AndMask(mask), Is.EqualTo(expected));
        }

        [Test]
        public void AndMask_ExplicitValues_AreMatchedAsIs()
        {
            var apx = T(Platform.win64, ProcessorArchitecture.x64, InstructionSetLevel.APX);
            var arm64 = T(Platform.win64, ProcessorArchitecture.arm64, 0);

            Assert.That(apx.AndMask(InstructionSetLevel.APX), Is.True);
            Assert.That(apx.AndMask(InstructionSetLevel.Baseline), Is.False);
            Assert.That(arm64.AndMask(ProcessorArchitecture.arm64), Is.True);
            Assert.That(arm64.AndMask(ProcessorArchitecture.x64), Is.False);
        }

        [Test]
        public void AndMask_PlatformWithoutArchitecture_DoesNotMatchArchitectureMask()
        {
            Assert.That(T(Platform.mac, 0, 0, DevEnv.xcode).AndMask(ProcessorArchitecture.x64), Is.False);
            Assert.That(T(Platform.mac, 0, 0, DevEnv.xcode).AndMask(InstructionSetLevel.Baseline), Is.True);
        }

        // ---- IsIncludeIn ----

        [Test]
        public void IsIncludeIn_TwoUnsetTargets_IsTrue()
        {
            Assert.That(T(Platform.win64, 0, 0).IsIncludeIn(T(Platform.win64, 0, 0)), Is.True);
            Assert.That(T(Platform.mac, 0, 0, DevEnv.xcode).IsIncludeIn(T(Platform.mac, 0, 0, DevEnv.xcode)), Is.True);
        }

        [Test]
        public void IsIncludeIn_UnsetAndExplicitDefault_IsTrue()
        {
            Assert.That(T(Platform.win64, 0, 0).IsIncludeIn(T(Platform.win64, ProcessorArchitecture.x64, InstructionSetLevel.Baseline)), Is.True);
            Assert.That(T(Platform.win64, ProcessorArchitecture.x64, 0).IsIncludeIn(T(Platform.win64, 0, 0)), Is.True);
        }

        [Test]
        public void IsIncludeIn_DifferentArchitectureOrLevel_IsFalse()
        {
            Assert.That(T(Platform.win64, 0, 0).IsIncludeIn(T(Platform.win64, ProcessorArchitecture.arm64, 0)), Is.False);
            Assert.That(T(Platform.win64, 0, 0).IsIncludeIn(T(Platform.win64, 0, InstructionSetLevel.APX)), Is.False);
            Assert.That(T(Platform.win64, 0, InstructionSetLevel.APX).IsIncludeIn(T(Platform.win64, 0, 0)), Is.False);
        }

        [Test]
        public void IsIncludeIn_MaskOfArchitectures_IncludesUnsetDefault()
        {
            var mask = T(Platform.win64, ProcessorArchitecture.x64 | ProcessorArchitecture.arm64, 0);

            Assert.That(T(Platform.win64, 0, 0).IsIncludeIn(mask), Is.True);
            Assert.That(T(Platform.win64, ProcessorArchitecture.arm64, 0).IsIncludeIn(mask), Is.True);
        }

        // Many users have their own target class, which implements ITarget without deriving from Target.
        private class CustomTargetWithoutFragments : ITarget
        {
            public Platform Platform;
            public DevEnv DevEnv;
            public Optimization Optimization;
        }

        private class CustomTargetWithFragments : ITarget
        {
            public Platform Platform;
            public DevEnv DevEnv;
            public Optimization Optimization;
            public ProcessorArchitecture ProcessorArchitecture;
            public InstructionSetLevel InstructionSetLevel;
        }

        [Test]
        public void IsIncludeIn_CustomTargetWithoutFragments_ComparesTheFragments()
        {
            var target = new CustomTargetWithoutFragments { Platform = Platform.win64, DevEnv = DevEnv.vs2022, Optimization = Optimization.Release };
            var same = new CustomTargetWithoutFragments { Platform = Platform.win64, DevEnv = DevEnv.vs2022, Optimization = Optimization.Release };
            var mask = new CustomTargetWithoutFragments { Platform = Platform.win64, DevEnv = DevEnv.vs2022, Optimization = Optimization.Debug | Optimization.Release };
            var other = new CustomTargetWithoutFragments { Platform = Platform.win64, DevEnv = DevEnv.vs2026, Optimization = Optimization.Release };

            Assert.That(target.IsIncludeIn(same), Is.True);
            Assert.That(target.IsIncludeIn(mask), Is.True);
            Assert.That(target.IsIncludeIn(other), Is.False);
        }

        [Test]
        public void IsIncludeIn_CustomTargetWithFragments_ResolvesTheUnsetDefaults()
        {
            var unset = new CustomTargetWithFragments { Platform = Platform.win64, DevEnv = DevEnv.vs2026, Optimization = Optimization.Release };
            var explicitDefaults = new CustomTargetWithFragments
            {
                Platform = Platform.win64,
                DevEnv = DevEnv.vs2026,
                Optimization = Optimization.Release,
                ProcessorArchitecture = ProcessorArchitecture.x64,
                InstructionSetLevel = InstructionSetLevel.Baseline,
            };
            var arm64 = new CustomTargetWithFragments { Platform = Platform.win64, DevEnv = DevEnv.vs2026, Optimization = Optimization.Release, ProcessorArchitecture = ProcessorArchitecture.arm64 };

            Assert.That(unset.IsIncludeIn(explicitDefaults), Is.True);
            Assert.That(explicitDefaults.IsIncludeIn(unset), Is.True);
            Assert.That(unset.IsIncludeIn(arm64), Is.False);
        }

        [Test]
        public void IsIncludeIn_CustomTargetOfAnotherType_IsFalse()
        {
            var withoutFragments = new CustomTargetWithoutFragments { Platform = Platform.win64, DevEnv = DevEnv.vs2022, Optimization = Optimization.Release };
            var withFragments = new CustomTargetWithFragments { Platform = Platform.win64, DevEnv = DevEnv.vs2022, Optimization = Optimization.Release };

            Assert.That(withoutFragments.IsIncludeIn(withFragments), Is.False);
            Assert.That(withoutFragments.IsIncludeIn(T(Platform.win64, 0, 0, DevEnv.vs2022)), Is.False);
        }

        [Test]
        public void IsIncludeIn_TargetsOfCustomTargets_FindsTheTarget()
        {
            var target = new CustomTargetWithFragments { Platform = Platform.win64, DevEnv = DevEnv.vs2026, Optimization = Optimization.Release };
            var targets = new Targets(typeof(CustomTargetWithFragments),
                new CustomTargetWithFragments { Platform = Platform.win64, DevEnv = DevEnv.vs2026, Optimization = Optimization.Debug | Optimization.Release });

            Assert.That(target.IsIncludeIn(targets), Is.True);
        }

        // The shape of the custom targets that Sharpmake users write: fragments of their own (a compiler and a CPU architecture, no
        // ProcessorArchitecture or InstructionSetLevel), fragments that default to a mask, and a Name override.
        [Fragment, System.Flags]
        private enum OwnCompiler
        {
            Vc = 0x01,
            Gcc = 0x02,
            Clang = 0x04,
        }

        [Fragment, System.Flags]
        private enum OwnCpuArchitecture
        {
            X64 = 0x01,
            Arm64 = 0x02,
        }

        private class CustomTargetWithOwnFragments : ITarget
        {
            public Blob Blob = Blob.Blob | Blob.NoBlob;
            public Optimization Optimization;
            public Platform Platform;
            public DevEnv DevEnv;
            public OwnCompiler Compiler;
            public OwnCpuArchitecture CpuArchitecture;
            public OutputType OutputType;
            public BuildSystem BuildSystem = BuildSystem.MSBuild;

            public override string Name => Optimization + (Blob == Blob.Blob ? "" : "NoBlob");
        }

        private static CustomTargetWithOwnFragments CreateCustomTargetWithOwnFragments(Platform platform, OwnCompiler compiler, Optimization optimization = Optimization.Release)
        {
            return new CustomTargetWithOwnFragments
            {
                Platform = platform,
                DevEnv = DevEnv.vs2022,
                Compiler = compiler,
                CpuArchitecture = OwnCpuArchitecture.X64,
                Optimization = optimization,
                OutputType = OutputType.Lib,
            };
        }

        [Test]
        public void CustomTargetWithOwnFragments_ExpandedTargets_AreIncludedInTheirMask()
        {
            CustomTargetWithOwnFragments mask = CreateCustomTargetWithOwnFragments(Platform.win64, OwnCompiler.Vc, Optimization.Debug | Optimization.Release);
            var targets = new Targets(typeof(CustomTargetWithOwnFragments), mask);

            // Blob and Optimization are masks: 2 x 2
            Assert.That(targets.TargetObjects.Count(), Is.EqualTo(4));
            Assert.That(targets.TargetObjects.All(t => t.IsIncludeIn(mask)), Is.True);
            Assert.That(targets.TargetObjects.All(t => t.IsIncludeIn(targets)), Is.True);
            Assert.That(targets.TargetObjects.All(t => !t.IsIncludeIn(CreateCustomTargetWithOwnFragments(Platform.win64, OwnCompiler.Clang, Optimization.Debug | Optimization.Release))), Is.True);
        }

        [Test]
        public void CustomTargetWithOwnFragments_HasTheDefaultsOfItsPlatform()
        {
            CustomTargetWithOwnFragments target = CreateCustomTargetWithOwnFragments(Platform.win64, OwnCompiler.Vc);

            Assert.That(Util.GetToolchainPlatformString(Platform.win64, target), Is.EqualTo("x64"));
            Assert.That(Util.GetProcessorArchitecture(target), Is.EqualTo(ProcessorArchitecture.x64));
            Assert.That(Util.GetInstructionSetLevel(target), Is.EqualTo(InstructionSetLevel.Baseline));
            Assert.That(target.GetTargetString(), Does.Not.Contain("Baseline"));
            Assert.That(target.ProjectConfigurationName, Is.EqualTo(target.Name));
            Assert.That(() => Util.VerifyProcessorArchitectureAndLevel(target), Throws.Nothing);
        }

        // ---- names ----

        [TestCase(Platform.win32, 0, 0, "Win32")]
        [TestCase(Platform.win64, 0, 0, "Win64")]
        [TestCase(Platform.win64, ProcessorArchitecture.x64, InstructionSetLevel.Baseline, "Win64")]
        [TestCase(Platform.win64, 0, InstructionSetLevel.APX, "Win64APX")]
        [TestCase(Platform.win64, ProcessorArchitecture.x64, InstructionSetLevel.APX, "Win64APX")]
        [TestCase(Platform.win64, ProcessorArchitecture.arm64, 0, "Win64Armv8")]
        [TestCase(Platform.linux, 0, 0, "Linux")]
        public void GetDisambiguatedPlatformString_AppendsOnlyNonDefaultToken(Platform platform, ProcessorArchitecture arch, InstructionSetLevel level, string expected)
        {
            Assert.That(Util.GetDisambiguatedPlatformString(T(platform, arch, level)), Is.EqualTo(expected));
        }

        [Test]
        public void GetSimplePlatformString_ForPlatform_IsUnchanged()
        {
            Assert.That(Util.GetSimplePlatformString(Platform.win64), Is.EqualTo("Win64"));
        }

        [TestCase(0, 0, "Win64")]
        [TestCase(0, InstructionSetLevel.APX, "Win64APX")]
        [TestCase(ProcessorArchitecture.arm64, 0, "Win64Armv8")]
        public void Resolver_TargetPlatform_UsesTargetAwareString(ProcessorArchitecture arch, InstructionSetLevel level, string expected)
        {
            var resolver = new Resolver();
            resolver.SetParameter("target", T(Platform.win64, arch, level));

            Assert.That(resolver.Resolve("[target.Platform]"), Is.EqualTo(expected));
        }

        // What custom solution platform names append to tell the targets sharing a toolchain platform apart
        [TestCase(Platform.win64, 0, 0, "")]
        [TestCase(Platform.win64, ProcessorArchitecture.x64, InstructionSetLevel.Baseline, "")]
        [TestCase(Platform.win64, 0, InstructionSetLevel.APX, "x64-APX")]
        [TestCase(Platform.win64, ProcessorArchitecture.arm64, 0, "ARM64")]
        [TestCase(Platform.win32, 0, 0, "")]
        [TestCase(Platform.linux, 0, 0, "")]
        public void GetArchitectureDisplaySuffix_IsEmptyForDefaults(Platform platform, ProcessorArchitecture arch, InstructionSetLevel level, string expected)
        {
            Assert.That(Util.GetArchitectureDisplaySuffix(T(platform, arch, level)), Is.EqualTo(expected));
        }

        // The architecture resolves to its effective value, so an unset one and an explicit default give the same string
        [TestCase(Platform.win64, 0, "x64")]
        [TestCase(Platform.win64, ProcessorArchitecture.x64, "x64")]
        [TestCase(Platform.win64, ProcessorArchitecture.arm64, "arm64")]
        [TestCase(Platform.win32, 0, "x86")]
        public void Resolver_TargetProcessorArchitecture_ResolvesToEffectiveValue(Platform platform, ProcessorArchitecture arch, string expected)
        {
            var resolver = new Resolver();
            resolver.SetParameter("target", T(platform, arch, 0));

            Assert.That(resolver.Resolve("[target.ProcessorArchitecture]"), Is.EqualTo(expected));
        }

        [Test]
        public void Resolver_TargetProcessorArchitecture_PlatformWithoutArchitecture_IsAnError()
        {
            var resolver = new Resolver();
            resolver.SetParameter("target", T(Platform.mac, 0, 0));

            var ex = Assert.Throws<Error>(() => resolver.Resolve("[target.ProcessorArchitecture]"));
            Assert.That(ex.Message, Does.Contain("ProcessorArchitecture"));
            Assert.That(ex.Message, Does.Contain("[target.Platform]"));
        }

        [Test]
        public void Resolver_TargetInstructionSetLevel_ResolvesToLevelToken()
        {
            var resolver = new Resolver();
            resolver.SetParameter("target", T(Platform.win64, 0, InstructionSetLevel.APX));

            Assert.That(resolver.Resolve("[target.InstructionSetLevel]"), Is.EqualTo("APX"));
        }

        // Baseline has no name token (it is omitted everywhere): resolving it would give an empty path chunk
        [TestCase(0)]
        [TestCase(InstructionSetLevel.Baseline)]
        public void Resolver_TargetInstructionSetLevel_Baseline_IsAnError(InstructionSetLevel level)
        {
            var resolver = new Resolver();
            resolver.SetParameter("target", T(Platform.win64, 0, level));

            var ex = Assert.Throws<Error>(() => resolver.Resolve("[target.InstructionSetLevel]"));
            Assert.That(ex.Message, Does.Contain("InstructionSetLevel"));
            Assert.That(ex.Message, Does.Contain("[target.Platform]"));
        }

        [TestCase(0, 0, "Release")]
        [TestCase(0, InstructionSetLevel.APX, "Release APX")]
        [TestCase(ProcessorArchitecture.arm64, 0, "Release")]
        public void ProjectConfigurationName_AddsSuffixOnlyWhenSharingTheVsPlatform(ProcessorArchitecture arch, InstructionSetLevel level, string expected)
        {
            var target = T(Platform.win64, arch, level);

            Assert.That(target.ProjectConfigurationName, Is.EqualTo(expected));
            Assert.That(target.Name, Is.EqualTo("Release"), "Name feeds folders and must stay space-free");
        }
    }

    public class ProcessorArchitectureDuplicateTargetTests : CppTestProjectBuilder
    {
        public ProcessorArchitectureDuplicateTargetTests()
            : base(typeof(ProcessorArchitectureProjects.Duplicate.ExplicitDefaultProject).Namespace)
        {
        }

        [Test]
        public void ExplicitDefaultNextToUnset_IsTheSameTarget()
        {
            // Same as adding any target twice today: Targets keeps one per target string.
            var project = GetProject<ProcessorArchitectureProjects.Duplicate.ExplicitDefaultProject>();

            Assert.That(project.Configurations.Count(), Is.EqualTo(1));
        }
    }

    public class ProcessorArchitectureTargetPathTests : CppTestProjectBuilder
    {
        public ProcessorArchitectureTargetPathTests()
            : base(typeof(ProcessorArchitectureProjects.Paths.Win64PathsProject).Namespace)
        {
        }

        private Project.Configuration ConfOf(ProcessorArchitecture arch, InstructionSetLevel level, Optimization optimization = Optimization.Release)
        {
            var project = GetProject<ProcessorArchitectureProjects.Paths.Win64PathsProject>();
            return project.Configurations.Single(c =>
            {
                var t = (Target)c.Target;
                return t.ProcessorArchitecture == arch && t.InstructionSetLevel == level && t.Optimization == optimization;
            });
        }

        private static string Normalized(string path) => path.Replace('\\', '/');

        [Test]
        public void DefaultTargetPath_KeepsLegacyLayoutForDefaultTargets()
        {
            Assert.That(Normalized(ConfOf(0, 0).TargetPath), Does.EndWith("/output/Win64/Release").IgnoreCase);
        }

        [Test]
        public void DefaultTargetPath_Arm64_UsesArchitectureToken()
        {
            Assert.That(Normalized(ConfOf(ProcessorArchitecture.arm64, 0).TargetPath), Does.EndWith("/output/Win64Armv8/Release").IgnoreCase);
        }

        [Test]
        public void DefaultTargetPath_Apx_HasNoSpace()
        {
            string path = Normalized(ConfOf(0, InstructionSetLevel.APX).TargetPath);

            Assert.That(path, Does.EndWith("/output/Win64APX/Release").IgnoreCase);
            Assert.That(path.Substring(path.IndexOf("/output/")), Does.Not.Contain(" "));
        }

        [Test]
        public void DefaultIntermediatePath_Apx_HasNoSpace()
        {
            Assert.That(Normalized(ConfOf(0, InstructionSetLevel.APX).IntermediatePath), Does.EndWith("/obj/Win64APX/Release").IgnoreCase);
        }

        private string ShortNameOf(ProcessorArchitecture arch, InstructionSetLevel level)
        {
            var project = GetProject<ProcessorArchitectureProjects.Paths.Win64PathsProject>();
            return Bff.GetShortProjectName(project, ConfOf(arch, level));
        }

        [Test]
        public void BffShortProjectName_DefaultWin64_KeepsLegacyFormat()
        {
            Assert.That(ShortNameOf(0, 0), Is.EqualTo("Win64PathsProject_Release_Win64"));
        }

        [Test]
        public void BffShortProjectName_Win64Targets_DoNotClash()
        {
            var names = new[]
            {
                ShortNameOf(0, 0),
                ShortNameOf(0, InstructionSetLevel.APX),
                ShortNameOf(ProcessorArchitecture.arm64, 0),
            };

            Assert.That(names, Is.Unique);
            Assert.That(names[2], Does.Contain("Armv8"));
        }

        [Test]
        public void ExplicitTargetPath_IsNeverChanged()
        {
            var conf = ConfOf(ProcessorArchitecture.x64, InstructionSetLevel.APX, Optimization.Retail);

            Assert.That(Normalized(conf.TargetPath), Does.EndWith("/custom out/Retail APX").IgnoreCase);
        }
    }

    namespace ProcessorArchitectureProjects
    {
        namespace Duplicate
        {
            [Sharpmake.Generate]
            public class ExplicitDefaultProject : Project
            {
                public ExplicitDefaultProject()
                {
                    SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release));
                    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.Baseline });
                }
            }
        }

        namespace Paths
        {
            [Sharpmake.Generate]
            public class Win64PathsProject : Project
            {
                public Win64PathsProject()
                {
                    SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release));
                    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { InstructionSetLevel = InstructionSetLevel.APX });
                    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
                    // explicit architecture + APX, with a user-set path
                    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Retail) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.APX });
                }

                [Configure(Optimization.Retail)]
                public void ConfigureRetail(Configuration conf, Target target)
                {
                    conf.TargetPath = "[conf.ProjectPath]/custom out/[conf.Name]";
                }
            }
        }
    }
}
