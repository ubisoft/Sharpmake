// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    // Sites that used to assume "win64 means x64": they must follow the resolved processor architecture.
    public class ProcessorArchitectureAuditTests
    {
        [TestCase(DevEnv.vs2017, ProcessorArchitecture.x86, "x86")]
        [TestCase(DevEnv.vs2017, ProcessorArchitecture.x64, "x64")]
        [TestCase(DevEnv.vs2017, ProcessorArchitecture.arm64, "arm64")]
        [TestCase(DevEnv.vs2022, ProcessorArchitecture.arm64, "arm64")]
        [TestCase(DevEnv.vs2026, ProcessorArchitecture.arm64, "arm64")]
        [TestCase(DevEnv.vs2015, ProcessorArchitecture.x86, "")]
        [TestCase(DevEnv.vs2015, ProcessorArchitecture.x64, "amd64")]
        public void ToolsetDirectory_PerArchitecture(DevEnv devEnv, ProcessorArchitecture architecture, string expected)
        {
            Assert.That(ExtensionMethods.GetVisualStudioToolsetDirectory(devEnv, architecture), Is.EqualTo(expected));
        }

        [Test]
        public void ToolsetDirectory_Arm64WithVs2015_IsAnError()
        {
            var ex = Assert.Throws<Error>(() => ExtensionMethods.GetVisualStudioToolsetDirectory(DevEnv.vs2015, ProcessorArchitecture.arm64));
            Assert.That(ex.Message, Does.Contain("arm64"));
        }

        [Test]
        public void ToolsetDirectory_UnknownArchitecture_IsAnError()
        {
            Assert.Throws<Error>(() => ExtensionMethods.GetVisualStudioToolsetDirectory(DevEnv.vs2022, 0));
        }

        // The Platform overload of GetVisualStudioBinPath predates processor architectures: these are the folders it always gave
        [TestCase(DevEnv.vs2015, Platform.win32, ProcessorArchitecture.x86)]
        [TestCase(DevEnv.vs2015, Platform.win64, ProcessorArchitecture.x64)]
        [TestCase(DevEnv.vs2015, Platform.anycpu, ProcessorArchitecture.x86)]
        [TestCase(DevEnv.vs2015, Platform.durango, ProcessorArchitecture.x86)]
        [TestCase(DevEnv.vs2017, Platform.win32, ProcessorArchitecture.x86)]
        [TestCase(DevEnv.vs2022, Platform.win32, ProcessorArchitecture.x86)]
        [TestCase(DevEnv.vs2022, Platform.win64, ProcessorArchitecture.x64)]
        [TestCase(DevEnv.vs2022, Platform.anycpu, ProcessorArchitecture.x64)]
        [TestCase(DevEnv.vs2022, Platform.durango, ProcessorArchitecture.x64)]
        [TestCase(DevEnv.vs2026, Platform.win64, ProcessorArchitecture.x64)]
        public void VisualStudioBinArchitecture_PerPlatform(DevEnv devEnv, Platform platform, ProcessorArchitecture expected)
        {
            Assert.That(ExtensionMethods.GetVisualStudioBinArchitecture(devEnv, platform), Is.EqualTo(expected));
        }

        [Test]
        public void VisualStudioBinArchitecture_NotVisualStudio_IsAnError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ExtensionMethods.GetVisualStudioBinArchitecture(DevEnv.xcode, Platform.win64));
        }

        // vs2015 has no Host* folder
        [TestCase(DevEnv.vs2015, ProcessorArchitecture.x86, new[] { "bin" })]
        [TestCase(DevEnv.vs2015, ProcessorArchitecture.x64, new[] { "bin", "amd64" })]
        [TestCase(DevEnv.vs2017, ProcessorArchitecture.x86, new[] { "bin", "HostX64", "x86" })]
        [TestCase(DevEnv.vs2022, ProcessorArchitecture.x64, new[] { "bin", "HostX64", "x64" })]
        [TestCase(DevEnv.vs2026, ProcessorArchitecture.arm64, new[] { "bin", "HostX64", "arm64" })]
        public void VisualStudioBinSubPath_PerArchitecture(DevEnv devEnv, ProcessorArchitecture architecture, string[] expectedParts)
        {
            Assume.That(Environment.Is64BitOperatingSystem, "The compiler host folder is HostX86 on a 32-bit system");
            Assert.That(ExtensionMethods.GetVisualStudioBinSubPath(devEnv, architecture), Is.EqualTo(System.IO.Path.Combine(expectedParts)));
        }

        [TestCase(ProcessorArchitecture.x86, "x86")]
        [TestCase(ProcessorArchitecture.x64, "x64")]
        [TestCase(ProcessorArchitecture.arm64, "arm64")]
        public void SdkLibraryDirectory_PerArchitecture(ProcessorArchitecture architecture, string expected)
        {
            Assert.That(ExtensionMethods.GetWindowsSdkLibraryDirectory(architecture), Is.EqualTo(expected));
        }

        // SDK tools (rc.exe, ...) run on the build machine: an arm64 target is built with the x64 tools
        [TestCase(ProcessorArchitecture.x86, "x86")]
        [TestCase(ProcessorArchitecture.x64, "x64")]
        [TestCase(ProcessorArchitecture.arm64, "x64")]
        public void SdkToolDirectory_PerArchitecture(ProcessorArchitecture architecture, string expected)
        {
            Assert.That(ExtensionMethods.GetWindowsSdkToolDirectory(architecture), Is.EqualTo(expected));
        }

        private class TestRustProject : RustProject
        {
            public TestRustProject()
                : base(typeof(Target), "Cargo.toml")
            {
            }

            public void AddBuildStep(Configuration conf, Target target)
            {
                AddRustBuildStep(conf, target);
            }
        }

        [Test]
        public void RustProject_NonDefaultArchitecture_IsAnError()
        {
            // cargo builds for the machine's architecture: an arm64 target would silently link an x64 library
            var project = new TestRustProject();
            var target = new Target(Platform.win64, DevEnv.vs2026, Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 };

            var ex = Assert.Throws<Error>(() => project.AddBuildStep(new Project.Configuration(), target));
            Assert.That(ex.Message, Does.Contain("Rust"));
            Assert.That(ex.Message, Does.Contain("arm64"));
        }

        [Test]
        public void Win64MasterCompilerSettings_WithoutArchitecture_IsObsoleteAsAnError()
        {
            // Callers must say which compiler they want: the overload without an architecture always gave the x64 one
            var overload = typeof(Windows.Win64Platform).GetMethods()
                .Single(m => m.Name == nameof(Windows.Win64Platform.GetMasterCompilerSettings) && !m.GetParameters().Any(p => p.ParameterType == typeof(ProcessorArchitecture)));

            var obsolete = (ObsoleteAttribute)Attribute.GetCustomAttribute(overload, typeof(ObsoleteAttribute));
            Assert.That(obsolete, Is.Not.Null);
            Assert.That(obsolete.IsError, Is.True);
            Assert.That(obsolete.Message, Does.Contain(nameof(ProcessorArchitecture)));
        }
    }
}
