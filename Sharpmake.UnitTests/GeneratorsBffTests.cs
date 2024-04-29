// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using NUnit.Framework;
using Sharpmake;
using static Sharpmake.Options;
using Sharpmake.Generators.FastBuild;
using static Sharpmake.Generators.FastBuild.Bff;
using System;

namespace Sharpmake.UnitTests
{
    internal class GeneratorsBffTests
    {
        private static bool HasVSCompiler(DevEnv devenv, Platform platform)
        {
            try
            {
                devenv.GetVisualStudioVCToolsCompilerVersion(platform);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [TestCase(Platform.win64)]
        [TestCase(Platform._reserved7)]
        public void HasVS2019orVS2022Compiler(Platform platform)
        {
            // Some setup on CI machines might test only one version of Visual Studio
            // Ensure at least one of the supported version is installed.
            Assert.That(HasVSCompiler(DevEnv.vs2019, platform) || HasVSCompiler(DevEnv.vs2022, platform), Is.EqualTo(true));
        }

        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v143)]
        public void DetectCompilerVersionForClangCl_FullVersionOverrideToolset(DevEnv devenv, Platform platform, Options.Vc.General.PlatformToolset overridenPlatformToolset)
        {
            if (!HasVSCompiler(devenv, platform))
            {
                // Probably on a CI machine having only one of the VS version (e.g. only VS2022), avoid testing this version
                return;
            }

            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.FullVersion;
            var overridenMscVer = "";
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version is the one returned GetVisualStudioVCToolsCompilerVersion, which is the full version of the visual studio compiler (cl.exe) installed on the computer.
            // Note : This can be the Visual Studio installation path or a path to another visual studio build tools
            // Note : The overridenPlatformToolset shouldn't have any impact in the case of FastBuildClangMscVersionDetectionType.FullVersion
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(devenv.GetVisualStudioVCToolsCompilerVersion(platform));

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2022, Platform.win64)]
        [TestCase(DevEnv.vs2022, Platform._reserved7)]
        [TestCase(DevEnv.vs2019, Platform.win64)]
        [TestCase(DevEnv.vs2019, Platform._reserved7)]
        public void DetectCompilerVersionForClangCl_FullVersionOverrideMscVer(DevEnv devenv, Platform platform)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.FullVersion;
            var overridenMscVer = "1936";
            var overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;

            // This should always return an error since FastBuildClangMscVersionDetectionType.FullVersion and overridenMscVer are mutually exclusive
            var result = Assert.Throws<Error>(() => Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform));
        }

        [TestCase(DevEnv.vs2022, Platform.win64)]
        [TestCase(DevEnv.vs2022, Platform._reserved7)]
        [TestCase(DevEnv.vs2019, Platform.win64)]
        [TestCase(DevEnv.vs2019, Platform._reserved7)]
        public void DetectCompilerVersionForClangCl_FullVersionOverrideMscVerZero(DevEnv devenv, Platform platform)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.FullVersion;
            var overridenMscVer = "0";
            var overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;

            // This should always return an error since FastBuildClangMscVersionDetectionType.FullVersion and overridenMscVer are mutually exclusive, even if overridenMscVer is 0 which means not set _MSC_VER
            var result = Assert.Throws<Error>(() => Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform));
        }

        [TestCase(DevEnv.vs2019, Platform.win64)]
        [TestCase(DevEnv.vs2019, Platform._reserved7)]
        [TestCase(DevEnv.vs2022, Platform.win64)]
        [TestCase(DevEnv.vs2022, Platform._reserved7)]
        public void DetectCompilerVersionForClangCl_MajorVersionNoOverride(DevEnv devenv, Platform platform)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.MajorVersion;
            var overridenMscVer = "";
            var overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version is the one returned by Bff.DetectMscVerForClang, which return the major version
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(Bff.DetectMscVerForClang(devenv, overridenPlatformToolset));

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v142, "1920")]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v143, "1930")]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v142, "1920")]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v143, "1930")]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v142, "1920")]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v143, "1930")]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v142, "1920")]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v143, "1930")]
        public void DetectCompilerVersionForClangCl_MajorVersionOverrideToolsetSupported(DevEnv devenv, Platform platform, Options.Vc.General.PlatformToolset overridenPlatformToolset, string exepctedVersion)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.MajorVersion;
            var overridenMscVer = "";
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version is the one returned by Bff.DetectMscVerForClang, which returns the major version, e.g. any vs2022 toolset is "1930"
            // Note : The version is directly impacted by the PlatformToolset
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(exepctedVersion);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v140)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v140)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v140)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v140)]
        public void DetectCompilerVersionForClangCl_MajorVersionOverrideToolsetNotSupported(DevEnv devenv, Platform platform, Options.Vc.General.PlatformToolset overridenPlatformToolset)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.MajorVersion;
            var overridenMscVer = "";

            // This should always return an error since the overriden platform toolset is not supported
            var result = Assert.Throws<Error>(() => Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform));
        }

        [TestCase(DevEnv.vs2019, Platform.win64)]
        [TestCase(DevEnv.vs2019, Platform._reserved7)]
        [TestCase(DevEnv.vs2022, Platform.win64)]
        [TestCase(DevEnv.vs2022, Platform._reserved7)]
        public void DetectCompilerVersionForClangCl_MajorVersionOverrideMscVer(DevEnv devenv, Platform platform)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.MajorVersion;
            var overridenMscVer = "1936";
            var overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version is the overridden one, taking precedence over the detection
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(overridenMscVer);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v142)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v142)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v142)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v142)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v143)]
        public void DetectCompilerVersionForClangCl_MajorVersionOverrideToolsetSupportedAndMscVer(DevEnv devenv, Platform platform, Options.Vc.General.PlatformToolset overridenPlatformToolset)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.MajorVersion;
            var overridenMscVer = "1936";
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version is the overridden one, taking precedence over the detection and over the overridden toolset (even if it's a supported one)
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(overridenMscVer);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v140)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v140)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v140)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v140)]
        public void DetectCompilerVersionForClangCl_MajorVersionOverrideToolsetNotSupportedAndMscVer(DevEnv devenv, Platform platform, Options.Vc.General.PlatformToolset overridenPlatformToolset)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.MajorVersion;
            var overridenMscVer = "1936";
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version is the overridden one, taking precedence over the detection and over the overridden toolset (even if it's not a supported one)
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(overridenMscVer);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2019, Platform.win64)]
        [TestCase(DevEnv.vs2019, Platform._reserved7)]
        [TestCase(DevEnv.vs2022, Platform.win64)]
        [TestCase(DevEnv.vs2022, Platform._reserved7)]
        public void DetectCompilerVersionForClangCl_MajorVersionOverrideMscVerZero(DevEnv devenv, Platform platform)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.MajorVersion;
            var overridenMscVer = "0";
            var overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version is the overridden one, taking precedence over the detection, even if the overriden version is 0
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(overridenMscVer);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v142)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v142)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v142)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v142)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v143)]
        public void DetectCompilerVersionForClangCl_MajorVersionOverrideToolsetSupportedAndMscVerZero(DevEnv devenv, Platform platform, Options.Vc.General.PlatformToolset overridenPlatformToolset)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.MajorVersion;
            var overridenMscVer = "0";
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version is the overridden one, taking precedence over the detection and over the overridden toolset, even if the overriden version is 0
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(overridenMscVer);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v140)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v140)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v140)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v140)]
        public void DetectCompilerVersionForClangCl_MajorVersionOverrideToolsetNotSupportedAndMscVerZero(DevEnv devenv, Platform platform, Options.Vc.General.PlatformToolset overridenPlatformToolset)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.MajorVersion;
            var overridenMscVer = "0";
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version is the overridden one, taking precedence over the detection and over the overridden toolset, even if the overriden version is 0
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(overridenMscVer);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2022, Platform.win64)]
        [TestCase(DevEnv.vs2022, Platform._reserved7)]
        [TestCase(DevEnv.vs2019, Platform.win64)]
        [TestCase(DevEnv.vs2019, Platform._reserved7)]
        public void DetectCompilerVersionForClangCl_DisabledNoOverride(DevEnv devenv, Platform platform)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.Disabled;
            var overridenMscVer = "";
            var overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version should be an empty one (i.e. no version will be passed to ClangCl) since detection is disabled
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl();

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v143)]
        public void DetectCompilerVersionForClangCl_DisabledOverrideMscVer(DevEnv devenv, Platform platform, Options.Vc.General.PlatformToolset overridenPlatformToolset)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.Disabled;
            var overridenMscVer = "1936";
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version should be the overridden version (no detection, but a version is passed). overriden toolset shouldn't be taken into account
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(overridenMscVer);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform.win64, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2019, Platform._reserved7, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform.win64, Options.Vc.General.PlatformToolset.v143)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.Default)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.ClangCL)]
        [TestCase(DevEnv.vs2022, Platform._reserved7, Options.Vc.General.PlatformToolset.v143)]
        public void DetectCompilerVersionForClangCl_DisabledOverrideMscVerZero(DevEnv devenv, Platform platform, Options.Vc.General.PlatformToolset overridenPlatformToolset)
        {
            var detectionType = Project.Configuration.FastBuildClangMscVersionDetectionType.Disabled;
            var overridenMscVer = "0";
            var result = Bff.DetectCompilerVersionForClangCl(detectionType, overridenMscVer, overridenPlatformToolset, devenv, platform);

            // The expected version should be the overridden version 0 (no detection, but the version 0 is passed). overriden toolset shouldn't be taken into account
            CompilerVersionForClangCl expected = new CompilerVersionForClangCl(overridenMscVer);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
