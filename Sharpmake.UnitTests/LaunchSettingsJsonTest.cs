// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.UnitTests
{
    // Covers the concurrency protection in LaunchSettingsJson.Generate(): several generated
    // project files (eg. one per devenv/target framework) can share the same project path and
    // therefore race to produce the same launchSettings.json. Only one caller should actually
    // write the file; the others should either silently no-op (identical content) or fail loudly
    // (conflicting content).
    //
    // Note: the dedup state is a static, process-lifetime dictionary keyed by file path (it is
    // never reset between generation sessions), so every test here must use its own unique path.
    [TestFixture]
    public class LaunchSettingsJsonTest
    {
        private Builder _builder;

        [SetUp]
        public void SetUp()
        {
            _builder = new Builder(
                new BuildContext.GenerateAll(writeLog: false, writeGeneratedFiles: false),
                multithreaded: false,
                dumpDependencyGraph: false,
                cleanBlobsOnly: false,
                blobOnly: false,
                skipInvalidPath: false,
                diagnostics: false,
                debugScripts: true,
                TestProjectBuilder.GetGeneratorsManager,
                null
            );
        }

        [TearDown]
        public void TearDown()
        {
            _builder.Dispose();
        }

        private static string NewProjectPath()
        {
            return Path.Combine(Path.GetTempPath(), "SharpmakeLaunchSettingsJsonTest_" + Guid.NewGuid());
        }

        private static Project.Configuration MakeConfiguration(string name, string startArguments)
        {
            return new Project.Configuration
            {
                Name = name,
                CsprojUserFile = new Project.Configuration.CsprojUserFileSettings
                {
                    StartAction = Project.Configuration.CsprojUserFileSettings.StartActionSetting.Project,
                    StartArguments = startArguments,
                }
            };
        }

        [Test]
        public void FirstCall_WritesFileAndReturnsItsPath()
        {
            var project = new Project { Name = "SomeProject" };
            string projectPath = NewProjectPath();

            var generated = new List<string>();
            var skip = new List<string>();
            string path = LaunchSettingsJson.Generate(
                _builder, project, projectPath,
                new[] { MakeConfiguration("Debug", "--same") },
                generated, skip);

            Assert.That(path, Is.Not.Null);
            Assert.That(generated, Has.Count.EqualTo(1));
            Assert.That(generated[0], Is.EqualTo(path));
            Assert.That(skip, Is.Empty);
        }

        [Test]
        public void SecondCallWithIdenticalContent_DoesNotThrowAndDoesNotRewrite()
        {
            var project = new Project { Name = "SomeProject" };
            string projectPath = NewProjectPath();

            var generated1 = new List<string>();
            string path1 = LaunchSettingsJson.Generate(
                _builder, project, projectPath,
                new[] { MakeConfiguration("Debug", "--same") },
                generated1, new List<string>());

            var generated2 = new List<string>();
            var skip2 = new List<string>();
            string path2 = LaunchSettingsJson.Generate(
                _builder, project, projectPath,
                new[] { MakeConfiguration("Release", "--same") },
                generated2, skip2);

            Assert.That(path2, Is.EqualTo(path1));
            Assert.That(generated1, Has.Count.EqualTo(1), "first call should have written the file");

            // The second call produced the same content as the first: it must not throw and must
            // not perform a second write. It currently reports neither Generated nor Skipped for
            // the file (see review note); assert that lack of double-reporting explicitly so this
            // test breaks loudly if that reporting gap is fixed one way or the other.
            Assert.That(generated2, Is.Empty, "second call must not write the file again");
            Assert.That(skip2, Is.Empty);
        }

        [Test]
        public void SecondCallWithConflictingContent_ThrowsAndNamesBothConfigurations()
        {
            var project = new Project { Name = "SomeProject" };
            string projectPath = NewProjectPath();

            LaunchSettingsJson.Generate(
                _builder, project, projectPath,
                new[] { MakeConfiguration("Debug", "--first") },
                new List<string>(), new List<string>());

            var ex = Assert.Throws<Error>(() =>
                LaunchSettingsJson.Generate(
                    _builder, project, projectPath,
                    new[] { MakeConfiguration("Release", "--second") },
                    new List<string>(), new List<string>()));

            Assert.That(ex.Message, Does.Contain("Debug"));
            Assert.That(ex.Message, Does.Contain("Release"));
        }

        [Test]
        public void ConcurrentCallsWithIdenticalContent_OnlyOneWriterAndNoException()
        {
            var project = new Project { Name = "SomeProject" };
            string projectPath = NewProjectPath();

            const int callCount = 16;
            var generatedCounts = new int[callCount];
            var exceptions = new Exception[callCount];

            Parallel.For(0, callCount, i =>
            {
                try
                {
                    var generated = new List<string>();
                    LaunchSettingsJson.Generate(
                        _builder, project, projectPath,
                        new[] { MakeConfiguration("Debug", "--same") },
                        generated, new List<string>());
                    generatedCounts[i] = generated.Count;
                }
                catch (Exception ex)
                {
                    exceptions[i] = ex;
                }
            });

            Assert.That(exceptions, Has.All.Null, "no concurrent call should throw when content is identical");
            Assert.That(generatedCounts.Sum(), Is.EqualTo(1), "exactly one of the concurrent calls should have written the file");
        }

        [Test]
        public void ConcurrentCallsWithConflictingContent_NonWinningMismatchedCallsThrow()
        {
            var project = new Project { Name = "SomeProject" };
            string projectPath = NewProjectPath();

            const int callCount = 16;
            var exceptions = new Exception[callCount];

            Parallel.For(0, callCount, i =>
            {
                try
                {
                    // Half the callers would produce one file content, half another: whichever
                    // content wins the race, every caller with the *other* content must throw.
                    string arguments = (i % 2 == 0) ? "--variantA" : "--variantB";
                    LaunchSettingsJson.Generate(
                        _builder, project, projectPath,
                        new[] { MakeConfiguration("Config" + i, arguments) },
                        new List<string>(), new List<string>());
                }
                catch (Exception ex)
                {
                    exceptions[i] = ex;
                }
            });

            // Exactly one of the two content variants wins the race; every caller using the other
            // variant must have thrown a conflict error, so exactly half of all calls throw.
            int thrownCount = exceptions.Count(ex => ex != null);
            Assert.That(thrownCount, Is.EqualTo(callCount / 2));
            Assert.That(exceptions.Where(ex => ex != null), Has.All.TypeOf<Error>());
        }
    }
}
