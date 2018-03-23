// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using NUnit.Framework;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;

namespace SharpmakeUnitTests
{
    [TestFixture]
    public class CSharpDependencyPropagation
    {
        private Builder _builder;

        public static IGeneratorManager GetGeneratorsManager()
        {
            return new Sharpmake.Generators.GeneratorManager();
        }

        [OneTimeSetUp]
        public void Init()
        {
            bool debugLog = true;
            bool multithreaded = false;
            bool writeFiles = false;
            bool dumpDependency = true;

            DependencyTracker.GraphWriteLegend = false;

            _builder = new Builder(
                new Sharpmake.BuildContext.GenerateAll(debugLog, writeFiles),
                multithreaded,
                dumpDependency,
                false,
                false,
                false,
                false,
                GetGeneratorsManager
            );

            // Force the test to load and register CommonPlatforms.dll as a Sharpmake extension
            // because sometimes you get the "no implementation of XX for platform YY."
            var platformDotNetType = typeof(DotNetPlatform);
            PlatformRegistry.RegisterExtensionAssembly(platformDotNetType.Assembly);

            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            // Allow message log from builder.
            Builder.OutputDelegate log = (msg, args) =>
            {
                Console.Write(msg, args);
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debug.Write(string.Format(msg, args));
            };
            _builder.EventOutputError += log;
            _builder.EventOutputWarning += log;
            _builder.EventOutputMessage += log;
            _builder.EventOutputDebug += log;

            ////////////////////////////////////////////////////////////////////
            // Register projects to generate here
            var sharpmakeProjects = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && t.Namespace == typeof(CSharpTestProjects.CSharpNoDependencyProject1).Namespace);

            // Also create some random source files
            Util.FakePathPrefix = Directory.GetCurrentDirectory();
            foreach (var sharpmakeProject in sharpmakeProjects)
            {
                Util.AddNewFakeFile(Util.PathMakeStandard(Path.Combine(sharpmakeProject.Name, sharpmakeProject.Name + "_source.cs")), 0);
            }

            foreach (var sharpmakeProject in sharpmakeProjects)
            {
                _builder.Arguments.Generate(sharpmakeProject);
            }
            ////////////////////////////////////////////////////////////////////

            _builder.BuildProjectAndSolution();

            var outputs = _builder.Generate();
            if (dumpDependency)
                DependencyTracker.Instance.DumpGraphs(outputs);
        }

        [OneTimeTearDown]
        public void Shutdown()
        {
            _builder.Dispose();
        }

        [Test]
        public void CSharpNoDependency()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpNoDependencyProject1)];
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(0));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void CSharpOnePublicDependency()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpOnePublicDependencyProject)];
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(1));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject1)));
            }
        }

        [Test]
        public void CSharpOnePublicOnePrivateDependency()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpOnePublicOnePrivateDependencyProject)];
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(1));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(1));

                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject1)));
                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject2)));
            }
        }

        [Test]
        public void CSharpTwoPublicDependencies()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpTwoPublicDependenciesProject)];
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(2));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject1)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject2)));
            }
        }

        [Test]
        public void CSharpTwoPrivateDependencies()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpTwoPrivateDependenciesProject)];
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(0));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject1)));
                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject2)));
            }
        }

        [Test]
        public void CSharpInheritOnePublicDependency()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpInheritOnePublicDependencyProject)];
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(2));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpOnePublicDependencyProject)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject1)));
            }
        }

        [Test]
        public void CSharpInheritOnePrivateDependency()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpInheritOnePrivateDependencyProject)];
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(0));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpOnePublicDependencyProject)));
                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject1)));
            }
        }

        [Test]
        public void CSharpInheritPrivateDependenciesWithPrivate()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpProjectA)];
            Assert.IsNotNull(project);

            // CSharpProjectA has private dependency to CSharpInheritOnePrivateDependencyProject
            // CSharpInheritOnePrivateDependencyProject has private dependency to CSharpOnePublicDependencyProject
            // CSharpOnePublicDependencyProject has public dependency to CSharpNoDependencyProject1

            foreach (var conf in project.Configurations)
            {
                // CSharpProjectA should have 1 private dependency.
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(0));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(1));

                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpInheritOnePrivateDependencyProject)));
            }
        }

        [Test]
        public void CSharpInheritPrivateDependenciesWithPublic()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpProjectB)];
            Assert.IsNotNull(project);

            // CSharpProjectB has a private dependency on CSharpOnePublicDependencyProject
            // CSharpOnePublicDependencyProject has a public dependency on CSharpNoDependencyProject1

            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(0));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpOnePublicDependencyProject)));
                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject1)));
            }
        }

        [Test]
        public void CSharpInheritPublicToPrivateLeaf()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpProjectD)];
            Assert.IsNotNull(project);

            // CSharpProjectD has a public dependency on CSharpInheritPublicFromPrivateDependencyProject
            // CSharpInheritPublicFromPrivateDependencyProject has a public dependency on CSharpOnePrivateDependencyProject
            // CSharpOnePrivateDependencyProject has a private dependency on CSharpNoDependencyProject1

            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(2));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpInheritPublicFromPrivateDependencyProject)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpOnePrivateDependencyProject)));
            }
        }

        [Test]
        public void CSharpInheritPublicDependenciesWithPrivate()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpProjectC)];
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(0));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(3));

                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpInheritOnePublicDependencyProject)));
                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpOnePublicDependencyProject)));
                Assert.True(conf.DotNetPrivateDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject1)));
            }
        }

        [Test]
        public void CSharpDuplicateInDeepInheritance()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpProjectDuplicateInDeepInheritance)];
            Assert.IsNotNull(project);

            // CSharpProjectDuplicateInDeepInheritance
            //
            //     + --- CSharpInheritOnePublicDependencyProject (Public)
            //               + --- CSharpOnePublicDependencyProject (Public)
            //                         + --- CSharpNoDependencyProject1 (Public)
            //
            //     + --- CSharpInheritOnePrivateDependencyProject (Public)
            //               + --- CSharpOnePublicDependencyProject (Private)
            //                         + --- CSharpNoDependencyProject1 (Public)
            //
            //     + --- CSharpTwoPrivateDependenciesProject (Public)
            //               + --- CSharpNoDependencyProject1 (Private)
            //               + --- CSharpNoDependencyProject2 (Private)
            //
            //     + --- CSharpOnePublicDependencyProject (Public)
            //               + --- CSharpNoDependencyProject1 (Public)

            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(5));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpTwoPrivateDependenciesProject)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpOnePublicDependencyProject)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpInheritOnePublicDependencyProject)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpInheritOnePrivateDependencyProject)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject1)));
                Assert.False(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject2)));
            }
        }

        [Test]
        public void CSharpDependenciesOnlyBuildOrder()
        {
            var project = _builder._projects[typeof(CSharpTestProjects.CSharpProjectReferenceOutputAssemblyInheritance)];
            Assert.IsNotNull(project);

            // CSharpProjectReferenceOutputAssemblyInheritance
            //     + --- CSharpInheritOnePublicDependencyProject (Public)
            //               + --- CSharpOnePublicDependencyProject (Public)
            //                         + --- CSharpNoDependencyProject1 (Public)
            //     + --- CSharpOnlyBuildOrderDependency (Public)
            //               + --- CSharpNoDependencyProject2 (Public|OnlyBuildOrder)

            foreach (var conf in project.Configurations)
            {
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(5));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpInheritOnePublicDependencyProject)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpOnlyBuildOrderDependency)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject2)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpOnePublicDependencyProject)));
                Assert.True(conf.DotNetPublicDependencies.ContainsProjectType(typeof(CSharpTestProjects.CSharpNoDependencyProject1)));

                foreach (var dependency in conf.DotNetPublicDependencies)
                {
                    if (dependency.Configuration.Project.GetType() == typeof(CSharpTestProjects.CSharpNoDependencyProject2))
                        Assert.False(dependency.ReferenceOutputAssembly);
                    else
                        Assert.IsNull(dependency.ReferenceOutputAssembly);
                }
            }
        }
    }


    public class CSharpUnitTestCommonProject : CSharpProject
    {
        public CSharpUnitTestCommonProject()
            : base(typeof(Target))
        {
            IsFileNameToLower = false;

            SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
            AddTargets(new Target(Platform.anycpu, DevEnv.vs2017, Optimization.Debug));
        }

        [ConfigurePriority(-100)]
        [Configure()]
        public void Configure(Configuration conf, Target target)
        {
            conf.Output = Configuration.OutputType.DotNetClassLibrary;
            conf.DumpDependencyGraph = true;
        }
    }


    namespace CSharpTestProjects
    {
        [Generate]
        public class CSharpNoDependencyProject1 : CSharpUnitTestCommonProject
        {
            public CSharpNoDependencyProject1() { }
        }

        [Generate]
        public class CSharpNoDependencyProject2 : CSharpUnitTestCommonProject
        {
            public CSharpNoDependencyProject2() { }
        }

        [Generate]
        public class CSharpOnePublicDependencyProject : CSharpUnitTestCommonProject
        {
            public CSharpOnePublicDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpNoDependencyProject1>(target);
            }
        }

        [Generate]
        public class CSharpOnePrivateDependencyProject : CSharpUnitTestCommonProject
        {
            public CSharpOnePrivateDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<CSharpNoDependencyProject1>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpOnePublicOnePrivateDependencyProject : CSharpUnitTestCommonProject
        {
            public CSharpOnePublicOnePrivateDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpNoDependencyProject1>(target);
                conf.AddPrivateDependency<CSharpNoDependencyProject2>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpTwoPublicDependenciesProject : CSharpUnitTestCommonProject
        {
            public CSharpTwoPublicDependenciesProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpNoDependencyProject1>(target);
                conf.AddPublicDependency<CSharpNoDependencyProject2>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpTwoPrivateDependenciesProject : CSharpUnitTestCommonProject
        {
            public CSharpTwoPrivateDependenciesProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<CSharpNoDependencyProject1>(target);
                conf.AddPrivateDependency<CSharpNoDependencyProject2>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpInheritOnePublicDependencyProject : CSharpUnitTestCommonProject
        {
            public CSharpInheritOnePublicDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpOnePublicDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpInheritOnePrivateDependencyProject : CSharpUnitTestCommonProject
        {
            public CSharpInheritOnePrivateDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<CSharpOnePublicDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpInheritPublicFromPrivateDependencyProject : CSharpUnitTestCommonProject
        {
            public CSharpInheritPublicFromPrivateDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpOnePrivateDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpInheritPrivateFromPrivateDependencyProject : CSharpUnitTestCommonProject
        {
            public CSharpInheritPrivateFromPrivateDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<CSharpOnePrivateDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpInheritPublicFromPublicDependencyProject : CSharpUnitTestCommonProject
        {
            public CSharpInheritPublicFromPublicDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpInheritOnePublicDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpProjectA : CSharpUnitTestCommonProject
        {
            public CSharpProjectA() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<CSharpInheritOnePrivateDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpProjectB : CSharpUnitTestCommonProject
        {
            public CSharpProjectB() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<CSharpOnePublicDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpProjectC : CSharpUnitTestCommonProject
        {
            public CSharpProjectC() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<CSharpInheritOnePublicDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpProjectD : CSharpUnitTestCommonProject
        {
            public CSharpProjectD() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpInheritPublicFromPrivateDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpProjectDuplicateInDeepInheritance : CSharpUnitTestCommonProject
        {
            public CSharpProjectDuplicateInDeepInheritance() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpInheritOnePublicDependencyProject>(target);
                conf.AddPublicDependency<CSharpInheritOnePrivateDependencyProject>(target);
                conf.AddPublicDependency<CSharpTwoPrivateDependenciesProject>(target);
                conf.AddPublicDependency<CSharpOnePublicDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class CSharpOnlyBuildOrderDependency : CSharpUnitTestCommonProject
        {
            public CSharpOnlyBuildOrderDependency() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpNoDependencyProject2>(target, DependencySetting.OnlyBuildOrder);
            }
        }

        [Sharpmake.Generate]
        public class CSharpProjectReferenceOutputAssemblyInheritance : CSharpUnitTestCommonProject
        {
            public CSharpProjectReferenceOutputAssemblyInheritance() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpInheritOnePublicDependencyProject>(target);
                conf.AddPublicDependency<CSharpOnlyBuildOrderDependency>(target);
            }
        }
    }
}
