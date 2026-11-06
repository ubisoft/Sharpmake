// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DiffEngine;
using NUnit.Framework;
using PublicApiGenerator;
using VerifyNUnit;
using VerifyTests;

namespace Sharpmake.PublicApiTests
{
    [TestFixture]
    public class PublicApiTests
    {
        private const string SnapshotsDirectory = "Snapshots";

        [OneTimeSetUp]
        public void Setup()
        {
            // Configure inline diff in test output.
            // Makes it easier to see what changed in the API snapshot.
            VerifyDiffPlex.Initialize(VerifyTests.DiffPlex.OutputType.Compact);

            // DiffEngine will automatically launch your preferred diff tool locally.
            // Control it via environment variables:
            //   DiffEngine_ToolOrder=VisualStudioCode  (or AraxisMerge, VisualStudioCode, WinMerge, etc.)

            // You can set Disabled to false locally to launch your diff tool when a snapshot differs.
            // Or you can manually diff the files specified in the test logs.
            DiffRunner.Disabled = true;
        }

        [Test]
        public Task SharpmakePublicApiHasNotChanged()
        {
            var assembly = typeof(Project).Assembly;
            var publicApi = assembly.GeneratePublicApi();

            return Verifier.Verify(publicApi)
                .UseDirectory(SnapshotsDirectory)
                .UseFileName("Sharpmake.PublicApi");
        }

            [Test]
        public Task SharpmakeGeneratorsPublicApiHasNotChanged()
        {
            var assembly = typeof(Generators.CompilerSettings).Assembly;
            var publicApi = assembly.GeneratePublicApi();

            // Sharpmake.Platforms projects (Sharpmake extended) add their own InternalsVisibleTo entries.
            return Verifier.Verify(publicApi)
                .UseDirectory(SnapshotsDirectory)
                .ScrubLines(IsExternalInternalsVisibleTo)
                .UseFileName("Sharpmake.Generators.PublicApi");
        }

        private static bool IsExternalInternalsVisibleTo(string line)
        {
            const string prefix = "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"";
            if (!line.StartsWith(prefix))
                return false;

            string assemblyName = line.Substring(prefix.Length).TrimEnd('"', ')', ']');
            return assemblyName != "Sharpmake" && assemblyName != "Sharpmake.CommonPlatforms" && assemblyName != "Sharpmake.UnitTests";
        }

        [Test]
        public Task SharpmakeCommonPlatformsPublicApiHasNotChanged()
        {
            var assembly = typeof(BasePlatform).Assembly;
            var publicApi = assembly.GeneratePublicApi();


            return Verifier.Verify(publicApi)
                .UseDirectory(SnapshotsDirectory)
                .UseFileName("Sharpmake.CommonPlatforms.PublicApi");
        }
    }
}
