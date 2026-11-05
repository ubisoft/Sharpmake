// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    internal class XcodeLocatorTests
    {
        private const string LegacyDevDir = "/Applications/Xcode.app/Contents/Developer";

        private static string ResolveDevDir(string developerDirEnv, string xcodeSelectOutput, params string[] existingDirs)
        {
            var existing = new HashSet<string>(existingDirs);
            return XcodeLocator.ResolveDeveloperDir(
                name => name == "DEVELOPER_DIR" ? developerDirEnv : null,
                () => xcodeSelectOutput,
                existing.Contains);
        }

        private static string ResolveToolchain(string developerDir, string xcrunClangPath, params string[] existingFiles)
        {
            var existing = new HashSet<string>(existingFiles);
            return XcodeLocator.ResolveToolchainUsrDir(developerDir, () => xcrunClangPath, existing.Contains);
        }

        [Test]
        public void DeveloperDir_EnvVarWinsWhenItExists()
        {
            Assert.That(
                ResolveDevDir("/Applications/Xcode_26.0.app/Contents/Developer", "/Library/Developer/CommandLineTools",
                    "/Applications/Xcode_26.0.app/Contents/Developer", "/Library/Developer/CommandLineTools"),
                Is.EqualTo("/Applications/Xcode_26.0.app/Contents/Developer"));
        }

        [Test]
        public void DeveloperDir_EnvVarIgnoredWhenFolderIsMissing()
        {
            Assert.That(
                ResolveDevDir("/Nope", "/Applications/Xcode_26.0.app/Contents/Developer",
                    "/Applications/Xcode_26.0.app/Contents/Developer"),
                Is.EqualTo("/Applications/Xcode_26.0.app/Contents/Developer"));
        }

        [Test]
        public void DeveloperDir_XcodeSelectOutputIsTrimmed()
        {
            Assert.That(
                ResolveDevDir(null, "/Applications/Xcode_26.0.app/Contents/Developer\n",
                    "/Applications/Xcode_26.0.app/Contents/Developer"),
                Is.EqualTo("/Applications/Xcode_26.0.app/Contents/Developer"));
        }

        [Test]
        public void DeveloperDir_XcodeSelectIgnoredWhenNullOrMissing()
        {
            Assert.That(ResolveDevDir(null, null), Is.EqualTo(LegacyDevDir));
            Assert.That(ResolveDevDir(null, "/Nope"), Is.EqualTo(LegacyDevDir));
        }

        [Test]
        public void DeveloperDir_FallsBackToLegacyPath()
        {
            Assert.That(ResolveDevDir("", ""), Is.EqualTo(LegacyDevDir));
        }

        [Test]
        public void Toolchain_FullXcodeLayout()
        {
            const string dev = "/Applications/Xcode_26.0.app/Contents/Developer";
            Assert.That(
                ResolveToolchain(dev, null,
                    dev + "/Toolchains/XcodeDefault.xctoolchain/usr/bin/clang",
                    dev + "/usr/bin/clang"),
                Is.EqualTo(dev + "/Toolchains/XcodeDefault.xctoolchain/usr"));
        }

        [Test]
        public void Toolchain_CommandLineToolsLayout()
        {
            const string dev = "/Library/Developer/CommandLineTools";
            Assert.That(
                ResolveToolchain(dev, null, dev + "/usr/bin/clang"),
                Is.EqualTo(dev + "/usr"));
        }

        [Test]
        public void Toolchain_FallsBackToXcrunResult()
        {
            Assert.That(
                ResolveToolchain("/Somewhere/Developer", "/Custom/Toolchain/usr/bin/clang\n"),
                Is.EqualTo("/Custom/Toolchain/usr"));
        }

        [Test]
        public void Toolchain_FallsBackToLegacyLayoutWhenNothingIsFound()
        {
            const string dev = "/FakePath";
            Assert.That(
                ResolveToolchain(dev, null),
                Is.EqualTo(dev + "/Toolchains/XcodeDefault.xctoolchain/usr"));
        }
    }
}
