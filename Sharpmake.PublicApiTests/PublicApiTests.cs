// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
            //   DiffEngine_Disabled=true               (to disable external diff tool entirely)
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

            return Verifier.Verify(publicApi)
                .UseDirectory(SnapshotsDirectory)
                .UseFileName("Sharpmake.Generators.PublicApi");
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
