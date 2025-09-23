// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    [TestFixture]
    public class CSharpDependencyPropagation : CSharpTestProjectBuilder
    {
        public CSharpDependencyPropagation()
            : base(typeof(CSharpTestProjects.CSharpNoDependencyProject1).Namespace)
        {
        }

        [Test]
        public void CSharpNoDependency()
        {
            var project = GetProject<CSharpTestProjects.CSharpNoDependencyProject1>();
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
            var project = GetProject<CSharpTestProjects.CSharpOnePublicDependencyProject>();
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
            var project = GetProject<CSharpTestProjects.CSharpOnePublicOnePrivateDependencyProject>();
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
            var project = GetProject<CSharpTestProjects.CSharpTwoPublicDependenciesProject>();
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
            var project = GetProject<CSharpTestProjects.CSharpTwoPrivateDependenciesProject>();
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
            var project = GetProject<CSharpTestProjects.CSharpInheritOnePublicDependencyProject>();
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
            var project = GetProject<CSharpTestProjects.CSharpInheritOnePrivateDependencyProject>();
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
            var project = GetProject<CSharpTestProjects.CSharpProjectA>();
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
            var project = GetProject<CSharpTestProjects.CSharpProjectB>();
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
            var project = GetProject<CSharpTestProjects.CSharpProjectD>();
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
            var project = GetProject<CSharpTestProjects.CSharpProjectC>();
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
            var project = GetProject<CSharpTestProjects.CSharpProjectDuplicateInDeepInheritance>();
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
            var project = GetProject<CSharpTestProjects.CSharpProjectReferenceOutputAssemblyInheritance>();
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
                    Assert.IsNull(dependency.ReferenceOutputAssembly);
                }
            }
        }

        [Test]
        public void CSharpDependenciesSwappedWithDLL()
        {
            var project = GetProject<CSharpTestProjects.CSharpProjectDependencySwappedToDLL>();
            foreach (var conf in project.Configurations)
            {
                // Dep 1 (swapped):     CSharpInheritOnePublicDependencyProject -> CSharpOnePublicDependencyProject -> CSharpNoDependencyProject1
                // Dep 2 (not swapped): CSharpProjectB
                Assert.That(conf.DotNetPublicDependencies.Count, Is.EqualTo(4));
                Assert.That(conf.DotNetPrivateDependencies.Count, Is.EqualTo(0));
                
                foreach (var dependency in conf.DotNetPublicDependencies)
                {
                    // All of them should be Swapped except CsharpProjectB (because it's transitive)
                    if (dependency.Configuration.Project.FullClassName == typeof(CSharpTestProjects.CSharpProjectB).FullName)
                        Assert.False(dependency.ReferenceSwappedWithOutputAssembly);
                    else
                        Assert.True(dependency.ReferenceSwappedWithOutputAssembly);
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
        
        [Sharpmake.Generate]
        public class CSharpProjectDependencySwappedToDLL : CSharpUnitTestCommonProject
        {
            public CSharpProjectDependencySwappedToDLL() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<CSharpInheritOnePublicDependencyProject>(target, DependencySetting.Default | DependencySetting.DependOnAssemblyOutput);
                conf.AddPublicDependency<CSharpProjectB>(target);
            }
        }
    }
}
