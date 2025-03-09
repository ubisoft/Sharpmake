// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Sharpmake.UnitTests.ConfigureAttributesUtils;

namespace Sharpmake.UnitTests
{
    public static class ConfigureAttributesTestExtensions
    {
        public static void Add(this List<string> list, params string[] values)
        {
            list.AddRange(values);
        }

        public static void InsertRange(this List<string> list, int index, params string[] values)
        {
            list.InsertRange(index, values);
        }
    }

    public class ConfigureAttributesOldOrderingTest : CppTestProjectBuilder
    {
        public ConfigureAttributesOldOrderingTest()
            : base(typeof(ConfigureAttributesTestProjects.SimpleProject).Namespace, ConfigureOrder.Old)
        {
        }

        [Test]
        public void ConfiguresCalled()
        {
            var project = GetProject<ConfigureAttributesTestProjects.SimpleProject>();
            Assert.IsNotNull(project);

            foreach (Project.Configuration configuration in project.Configurations)
            {
                var conf = (CommonProjectConfiguration)configuration;
                var expectedCalls = new List<string> {
                    "CommonProject.ConfigureAll",
                    "CommonProject.ConfigureWin64",
                    "CommonProject.ConfigureVs2017",
                    "CommonProject.Configure" + configuration.Target.GetOptimization(),
                    "CommonProject.Configure" + configuration.Target.GetOptimization() + "Win64",
                };
                CollectionAssert.AreEqual(expectedCalls, conf.ConfigureCalls);
            }
        }

        public static void ConfigureOverrideCalled(Project project)
        {
            Assert.IsNotNull(project);

            foreach (Project.Configuration configuration in project.Configurations)
            {
                var conf = (CommonProjectConfiguration)configuration;

                var expectedCalls = new List<string> {
                    "CommonProject.ConfigureAll",
                    "CommonProject.ConfigureWin64",
                    "CommonProject.ConfigureVs2017",
                    "CommonProject.Configure" + configuration.Target.GetOptimization(),
                };

                // in release, the configure is overridden, which will make it be called first
                if (configuration.Target.GetOptimization() == Optimization.Release)
                {
                    expectedCalls.InsertRange(
                        0,
                        "CommonProject.ConfigureReleaseWin64",
                        project.Name + ".ConfigureReleaseWin64"
                    );
                }
                else
                {
                    expectedCalls.Add("CommonProject.ConfigureDebugWin64");
                }

                CollectionAssert.AreEqual(expectedCalls, conf.ConfigureCalls);
            }
        }

        [Test]
        public void OverrideCalled()
        {
            var project = GetProject<ConfigureAttributesTestProjects.ProjectWithOverride>();
            ConfigureOverrideCalled(project);
        }
    }

    public class ConfigureAttributeWarningTest : CppTestProjectBuilder
    {
        public ConfigureAttributeWarningTest()
            : base(typeof(ConfigureAttributesTestWarningProjects.RedundantOverride).Namespace, ConfigureOrder.New)
        {
        }

        [Test]
        public void OverrideWithRedundantFragments()
        {
            // this test should behave the same as the previous one, redundant fragments are not an error
            var project1 = GetProject<ConfigureAttributesTestWarningProjects.RedundantOverride>();
            ConfigureAttributesOldOrderingTest.ConfigureOverrideCalled(project1);

            // this test should behave the same as the previous one, reordered fragments are not an error
            var project2 = GetProject<ConfigureAttributesTestWarningProjects.ReorderedAndRedundant>();
            ConfigureAttributesOldOrderingTest.ConfigureOverrideCalled(project2);

            // this test should behave the same as the previous one, duplicated fragments are not an error
            var project3 = GetProject<ConfigureAttributesTestWarningProjects.RedundantAndDuplicated>();
            ConfigureAttributesOldOrderingTest.ConfigureOverrideCalled(project3);

            Assert.That(WarningCount, Is.EqualTo(3));
            Assert.That(ErrorCount, Is.EqualTo(0));
        }
    }

    public class ConfigureAttributesThrowingTest : TestProjectBuilder
    {
        public ConfigureAttributesThrowingTest()
            : base(InitType.Cpp, typeof(ConfigureAttributesTestThrowingProjects.Mismatch).Namespace, ConfigureOrder.Old)
        {
        }

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            Init();
        }

        [Test]
        public void MismatchThrows()
        {
            var projectType = typeof(ConfigureAttributesTestThrowingProjects.Mismatch);
            Builder.Arguments.Generate(projectType);
            Assert.Throws<Error>(Builder.BuildProjectAndSolution);
        }
    }

    namespace ConfigureAttributesUtils
    {
        public class CommonProjectConfiguration : Project.Configuration
        {
            public List<string> ConfigureCalls = new List<string>();

            public void LogCaller(MethodBase methodBase)
            {
                ConfigureCalls.Add(methodBase.DeclaringType.Name + "." + methodBase.Name);
            }
        }

        public abstract class CommonProject : Project
        {
            protected CommonProject()
                : base(typeof(Target), typeof(CommonProjectConfiguration))
            {
                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(new Target(Platform.win64, DevEnv.vs2017, Optimization.Debug | Optimization.Release));
            }

            [Configure]
            public virtual void ConfigureAll(CommonProjectConfiguration conf, Target target)
            {
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }

            [Configure(Platform.win64)]
            public virtual void ConfigureWin64(CommonProjectConfiguration conf, Target target)
            {
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }

            [Configure(DevEnv.vs2017)]
            public virtual void ConfigureVs2017(CommonProjectConfiguration conf, Target target)
            {
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }

            [Configure(Optimization.Debug)]
            public virtual void ConfigureDebug(CommonProjectConfiguration conf, Target target)
            {
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }

            [Configure(Optimization.Release)]
            public virtual void ConfigureRelease(CommonProjectConfiguration conf, Target target)
            {
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }

            [Configure(Optimization.Debug, Platform.win64)]
            public virtual void ConfigureDebugWin64(CommonProjectConfiguration conf, Target target)
            {
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }

            [Configure(Optimization.Release, Platform.win64)]
            public virtual void ConfigureReleaseWin64(CommonProjectConfiguration conf, Target target)
            {
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }
        }
    }

    namespace ConfigureAttributesTestProjects
    {
        [Generate]
        public class SimpleProject : CommonProject
        {
            public SimpleProject() { }
        }

        [Generate]
        public class ProjectWithOverride : CommonProject
        {
            public ProjectWithOverride() { }

            public override void ConfigureReleaseWin64(CommonProjectConfiguration conf, Target target)
            {
                base.ConfigureReleaseWin64(conf, target);
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }
        }
    }

    namespace ConfigureAttributesTestWarningProjects
    {
        [Generate]
        public class RedundantOverride : CommonProject
        {
            public RedundantOverride() { }

            [Configure(Optimization.Release, Platform.win64)] // REDUNDANT
            public override void ConfigureReleaseWin64(CommonProjectConfiguration conf, Target target)
            {
                base.ConfigureReleaseWin64(conf, target);
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }
        }

        [Generate]
        public class ReorderedAndRedundant : CommonProject
        {
            public ReorderedAndRedundant() { }

            [Configure(Platform.win64, Optimization.Release)] // REORDERED AND REDUNDANT
            public override void ConfigureReleaseWin64(CommonProjectConfiguration conf, Target target)
            {
                base.ConfigureReleaseWin64(conf, target);
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }
        }

        [Generate]
        public class RedundantAndDuplicated : CommonProject
        {
            public RedundantAndDuplicated() { }

            [Configure(Platform.win64, Optimization.Release, Platform.win64)] // REORDERED, REDUNDANT AND DUPLICATED
            public override void ConfigureReleaseWin64(CommonProjectConfiguration conf, Target target)
            {
                base.ConfigureReleaseWin64(conf, target);
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }
        }
    }

    namespace ConfigureAttributesTestThrowingProjects
    {
        [Generate]
        public class Mismatch : CommonProject
        {
            public Mismatch() { }

            [Configure(Optimization.Debug)] // MISMATCH!
            public override void ConfigureReleaseWin64(CommonProjectConfiguration conf, Target target)
            {
                base.ConfigureReleaseWin64(conf, target);
                conf.LogCaller(MethodBase.GetCurrentMethod());
            }
        }
    }
}
