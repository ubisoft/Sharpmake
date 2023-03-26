// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    [TestFixture]
    public class DependencyPropagation : CppTestProjectBuilder
    {
        public DependencyPropagation()
            : base(typeof(SharpmakeProjects.NoDependencyProject1).Namespace)
        {
        }

        [Test]
        public void NoDependency()
        {
            var project = GetProject<SharpmakeProjects.NoDependencyProject1>();
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void OnePublicDependency()
        {
            var project = GetProject<SharpmakeProjects.OnePublicDependencyProject>();
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
            }
        }

        [Test]
        public void OnePublicDependencyWithoutLinking()
        {
            var project = GetProject<SharpmakeProjects.OnePublicDependencyWithoutLinkingProject>();
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(0));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
            }
        }

        [Test]
        public void OnePublicOnePrivateDependency()
        {
            var project = GetProject<SharpmakeProjects.OnePublicOnePrivateDependencyProject>();
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(1));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));
            }
        }

        [Test]
        public void TwoPublicDependencies()
        {
            var project = GetProject<SharpmakeProjects.TwoPublicDependenciesProject>();
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));
            }
        }

        [Test]
        public void TwoPrivateDependencies()
        {
            var project = GetProject<SharpmakeProjects.TwoPrivateDependenciesProject>();
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));
            }
        }

        [Test]
        public void InheritOnePublicDependency()
        {
            var project = GetProject<SharpmakeProjects.InheritOnePublicDependencyProject>();
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                // The dependency chain is Public all the way, and "Default", so
                // both NoDependencyProject1 and OnePublicDependencyProject 
                // include and library path should be in the arrays
                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"));
            }
        }

        [Test]
        public void InheritOnePrivateDependency()
        {
            var project = GetProject<SharpmakeProjects.InheritOnePrivateDependencyProject>();
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                // The dependency chain is PRIVATE from the base project to OnePublic,
                // *but* is Public from OnePublic to NoDependency, so it should be able to access NoDependency
                // Both NoDependencyProject1 and OnePublicDependencyProject include and library path should be in the array
                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"));
            }
        }

        [Test]
        public void InheritOnePrivateDependencyOnlyBuildOrder()
        {
            var project = GetProject<SharpmakeProjects.InheritOnePrivateDependencyOnlyBuildOrderProject>();
            Assert.IsNotNull(project);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void InheritPrivateDependenciesWithPrivate()
        {
            // ProjectA and ProjectB share the same dependencies settings, as the only change
            // is the type of the immediate dependency to InheritOnePrivateDependencyProject
            var project = GetProject<SharpmakeProjects.ProjectA>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.InheritOnePrivateDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(3));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("InheritOnePrivateDependencyProject", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("InheritOnePrivateDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("InheritOnePrivateDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
            }
        }

        [Test]
        public void InheritPrivateDependenciesWithPublic()
        {
            // ProjectA and ProjectB share the same settings, as the only change
            // is the type of the immediate dependency to InheritOnePrivateDependencyProject
            var project = GetProject<SharpmakeProjects.ProjectB>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));

                // this is where ProjectA and ProjectB differ
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.InheritOnePrivateDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(3));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("InheritOnePrivateDependencyProject", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("InheritOnePrivateDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("InheritOnePrivateDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
            }
        }

        [Test]
        public void InheritPublicToPrivateLeaf()
        {
            // ProjectA and ProjectB share the same settings, as the only change
            // is the type of the immediate dependency to InheritOnePrivateDependencyProject
            var project = GetProject<SharpmakeProjects.ProjectF>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));

                // this is where ProjectA and ProjectB differ
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(1));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.InheritPublicFromPrivateDependencyProject)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePrivateDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(3));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("InheritPublicFromPrivateDependencyProject", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePrivateDependencyProject", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("InheritPublicFromPrivateDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePrivateDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("InheritPublicFromPrivateDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("OnePrivateDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
            }
        }

        [Test]
        public void InheritPublicDependenciesWithPrivate()
        {
            var project = GetProject<SharpmakeProjects.ProjectC>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.InheritOnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(3));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("InheritOnePublicDependencyProject", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("InheritOnePublicDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("InheritOnePublicDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
            }
        }

        [Test]
        public void DLLWithDependenciesPublic()
        {
            var project = GetProject<SharpmakeProjects.ProjectDLLPublic>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"));
            }
        }

        [Test]
        public void DLLWithDependenciesPrivate()
        {
            var project = GetProject<SharpmakeProjects.ProjectDLLPrivate>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"));
            }
        }

        [Test]
        public void DLLWithDLLDependencyPrivate()
        {
            var project = GetProject<SharpmakeProjects.ProjectDLLPrivateDLL>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivate)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPrivate"));
            }
        }

        [Test]
        public void ExeWithDLLPrivate()
        {
            var project = GetProject<SharpmakeProjects.ProjectExePrivate>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivate)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPrivate"));
            }
        }

        [Test]
        public void ExeWithDLLPublic()
        {
            var project = GetProject<SharpmakeProjects.ProjectExePublic>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivate)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPrivate"));
            }
        }

        [Test]
        public void ExeWithDLLPublicInheritance()
        {
            var project = GetProject<SharpmakeProjects.ProjectExePublicDLLInheritance>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivateDLL)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivate)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPrivateDLL", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPrivateDLL", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPrivateDLL"));
            }
        }

        [Test]
        public void ExeDoublePublicDLLInheritance()
        {
            var project = GetProject<SharpmakeProjects.ProjectExeDoublePublicDLLInheritance>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPublicDLL)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPublic)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPublicDLL", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPublic", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPublicDLL", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPublic", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPublicDLL"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPublic"));
            }
        }



        [Test]
        public void PrivateDependOnExported()
        {
            var project = GetProject<SharpmakeProjects.ProjectPrivateDependOnExported>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(1));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectExported)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectExported"));
            }
        }

        [Test]
        public void PrivateInheritExported()
        {
            var project = GetProject<SharpmakeProjects.ProjectPrivateInheritExported>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectPrivateDependOnExported)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectExported)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectPrivateDependOnExported", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectPrivateDependOnExported", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectPrivateDependOnExported"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectExported"));
            }
        }

        [Test]
        public void InheritExportAsPublic()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritExportAsPublic>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectPublicDependOnExported)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectExported)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectPublicDependOnExported", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectPublicDependOnExported", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectPublicDependOnExported"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectExported"));
            }
        }

        [Test]
        public void DuplicateInDeepInheritance()
        {
            var project = GetProject<SharpmakeProjects.ProjectDuplicateInDeepInheritance>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectPublicDependOnExported)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectExported)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDeepInheritExport)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectInheritExportAsPublic)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(4));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDeepInheritExport", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectPublicDependOnExported", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectPublicDependOnExported", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDeepInheritExport", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectInheritExportAsPublic", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectPublicDependOnExported"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectExported"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectDeepInheritExport"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectInheritExportAsPublic"));
            }
        }

        [Test]
        public void InheritAsPrivateAndPublic()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritAsPrivateAndPublic>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
            }
        }

        [Test]
        public void InheritLibFromDllAndLib()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritLibFromDllAndLib>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(5));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(5));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivate)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.AProjectDLL)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(4));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("AProjectDLL", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("AProjectDLL", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPrivate"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("AProjectDLL"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
            }
        }

        [Test]
        public void InheritIdenticalFromDll()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritIdenticalFromDll>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(4));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.AProjectDLL)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPublic)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("AProjectDLL", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPublic", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("AProjectDLL", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPublic", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("AProjectDLL"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPublic"));
            }
        }

        [Test]
        public void ComplexDllInheritance()
        {
            var project = GetProject<SharpmakeProjects.ProjectComplexDllInheritance>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDllPubDepWithoutLink)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(0));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
            }
        }

        [Test]
        public void InheritFromComplexDllInheritance()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritComplexDllInheritance>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(5));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(5));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectComplexDllInheritance)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDllPubDepWithoutLink)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectComplexDllInheritance", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectComplexDllInheritance", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectComplexDllInheritance"));
            }
        }

        [Test]
        public void InheritComplexDllInheritanceAndDirect()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritComplexDllInheritanceAndDirect>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(5));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(4));

                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectComplexDllInheritance)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDllPubDepWithoutLink)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectComplexDllInheritance", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)));
                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectComplexDllInheritance", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectComplexDllInheritance"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"));
            }
        }

        [Test]
        public void OnlyBuildOrder()
        {
            var project = GetProject<SharpmakeProjects.ProjectOnlyBuildOrder>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void ProjectInheritBuildOrder()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritBuildOrder>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectOnlyBuildOrder)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectOnlyBuildOrder", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectOnlyBuildOrder", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("ProjectOnlyBuildOrder"));
            }
        }

        [Test]
        public void InheritAndBuildOrder()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritAndBuildOrder>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.InheritPrivateFromPrivateDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePrivateDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(3));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("InheritPrivateFromPrivateDependencyProject", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("InheritPrivateFromPrivateDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePrivateDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("InheritPrivateFromPrivateDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("OnePrivateDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
            }
        }

        [Test]
        public void PubToImmediateAndBuildOrder()
        {
            var project = GetProject<SharpmakeProjects.ProjectPubToImmediateAndBuildOrder>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePrivateDependencyProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePrivateDependencyProject", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePrivateDependencyProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("OnePrivateDependencyProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"));
            }
        }

        [Test]
        public void LibInheritLibAndDLLPublic()
        {
            var project = GetProject<SharpmakeProjects.LibInheritLibAndDLLPublicProject>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(1));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyDLLProject)));
                Assert.True(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.LibDependOnDLLProject)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("LibDependOnDLLProject", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyDLLProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("LibDependOnDLLProject", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyDLLProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("LibDependOnDLLProject"));
            }
        }

        [Test]
        public void LibInheritLibAndDLLPrivate()
        {
            var project = GetProject<SharpmakeProjects.LibInheritLibAndDLLPrivateProject>();
            Assert.IsNotNull(project);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyDLLProject)));
                Assert.True(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.LibDependOnDLLProject)));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.True(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("LibDependOnDLLProject", UTestUtilities.IncludeFolder)));

                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyDLLProject", UTestUtilities.LibOutputFolder)));
                Assert.True(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("LibDependOnDLLProject", UTestUtilities.LibOutputFolder)));

                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyDLLProject"));
                Assert.True(conf.DependenciesLibraryFiles.ContainsElement("LibDependOnDLLProject"));
            }
        }
    }

    public static class UTestUtilities
    {
        public static readonly string IncludeFolder = "include";
        public static readonly string LibOutputFolder = "lib_output";

        public static bool ContainsProjectType(this System.Collections.Generic.IEnumerable<Project.Configuration> dependencies, System.Type projectType)
        {
            return dependencies.Select(x => x.Project.GetType()).Contains(projectType);
        }

        public static bool ContainsProjectType(this IEnumerable<DotNetDependency> dependencies, Type projectType)
        {
            return dependencies.Any(x => x.Configuration.Project.GetType() == projectType);
        }

        public static bool ContainsElement(this OrderableStrings array, string element)
        {
            foreach (var v in array)
            {
                if (v.IndexOf(element, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        public class UnitTestCommonProject : Project
        {
            public UnitTestCommonProject() : base(typeof(Target))
            {
                IsFileNameToLower = false;

                SourceRootPath = Directory.GetCurrentDirectory() + "/[project.Name]";
                AddTargets(new Target(Platform.win64, DevEnv.vs2015, Optimization.Debug));
            }

            [ConfigurePriority(-100)]
            [Configure()]
            public void Configure(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Lib;

                conf.IncludePaths.Add(Directory.GetCurrentDirectory() + "/[project.Name]/" + IncludeFolder);
                conf.TargetLibraryPath = Directory.GetCurrentDirectory() + "/[project.Name]/" + LibOutputFolder;

                conf.DumpDependencyGraph = true;
            }
        }
    }

    namespace SharpmakeProjects
    {
        [Generate]
        public class NoDependencyProject1 : UTestUtilities.UnitTestCommonProject
        {
            public NoDependencyProject1() { }
        }

        [Generate]
        public class NoDependencyProject2 : UTestUtilities.UnitTestCommonProject
        {
            public NoDependencyProject2() { }
        }

        [Generate]
        public class OnePublicDependencyProject : UTestUtilities.UnitTestCommonProject
        {
            public OnePublicDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<NoDependencyProject1>(target);
            }
        }

        [Generate]
        public class OnePublicDependencyWithoutLinkingProject : UTestUtilities.UnitTestCommonProject
        {
            public OnePublicDependencyWithoutLinkingProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<NoDependencyProject1>(target, DependencySetting.DefaultWithoutLinking);
            }
        }

        [Generate]
        public class OnePrivateDependencyProject : UTestUtilities.UnitTestCommonProject
        {
            public OnePrivateDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<NoDependencyProject1>(target);
            }
        }

        [Sharpmake.Generate]
        public class OnePublicOnePrivateDependencyProject : UTestUtilities.UnitTestCommonProject
        {
            public OnePublicOnePrivateDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<NoDependencyProject1>(target);
                conf.AddPrivateDependency<NoDependencyProject2>(target);
            }
        }

        [Sharpmake.Generate]
        public class TwoPublicDependenciesProject : UTestUtilities.UnitTestCommonProject
        {
            public TwoPublicDependenciesProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<NoDependencyProject1>(target);
                conf.AddPublicDependency<NoDependencyProject2>(target);
            }
        }

        [Sharpmake.Generate]
        public class TwoPrivateDependenciesProject : UTestUtilities.UnitTestCommonProject
        {
            public TwoPrivateDependenciesProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<NoDependencyProject1>(target);
                conf.AddPrivateDependency<NoDependencyProject2>(target);
            }
        }

        [Sharpmake.Generate]
        public class InheritOnePublicDependencyProject : UTestUtilities.UnitTestCommonProject
        {
            public InheritOnePublicDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<OnePublicDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class InheritOnePrivateDependencyProject : UTestUtilities.UnitTestCommonProject
        {
            public InheritOnePrivateDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<OnePublicDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class InheritOnePrivateDependencyOnlyBuildOrderProject : UTestUtilities.UnitTestCommonProject
        {
            public InheritOnePrivateDependencyOnlyBuildOrderProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<OnePublicDependencyProject>(target, DependencySetting.OnlyBuildOrder);
            }
        }

        [Sharpmake.Generate]
        public class InheritPublicFromPrivateDependencyProject : UTestUtilities.UnitTestCommonProject
        {
            public InheritPublicFromPrivateDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<OnePrivateDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class InheritPrivateFromPrivateDependencyProject : UTestUtilities.UnitTestCommonProject
        {
            public InheritPrivateFromPrivateDependencyProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<OnePrivateDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectA : UTestUtilities.UnitTestCommonProject
        {
            public ProjectA() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<InheritOnePrivateDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectB : UTestUtilities.UnitTestCommonProject
        {
            public ProjectB() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<InheritOnePrivateDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectC : UTestUtilities.UnitTestCommonProject
        {
            public ProjectC() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<InheritOnePublicDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class AProjectDLL : UTestUtilities.UnitTestCommonProject
        {
            public AProjectDLL() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Dll;

                conf.AddPrivateDependency<NoDependencyProject1>(target);
                conf.AddPrivateDependency<NoDependencyProject2>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectDLLPrivate : UTestUtilities.UnitTestCommonProject
        {
            public ProjectDLLPrivate() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Dll;

                conf.AddPrivateDependency<NoDependencyProject1>(target);
                conf.AddPrivateDependency<NoDependencyProject2>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectDLLPublic : UTestUtilities.UnitTestCommonProject
        {
            public ProjectDLLPublic() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Dll;

                conf.AddPublicDependency<NoDependencyProject1>(target);
                conf.AddPublicDependency<NoDependencyProject2>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectDLLPrivateDLL : UTestUtilities.UnitTestCommonProject
        {
            public ProjectDLLPrivateDLL() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Dll;

                conf.AddPrivateDependency<ProjectDLLPrivate>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectDLLPublicDLL : UTestUtilities.UnitTestCommonProject
        {
            public ProjectDLLPublicDLL() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Dll;

                conf.AddPublicDependency<ProjectDLLPublic>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectExePrivate : UTestUtilities.UnitTestCommonProject
        {
            public ProjectExePrivate() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Exe;

                conf.AddPrivateDependency<ProjectDLLPrivate>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectExePublic : UTestUtilities.UnitTestCommonProject
        {
            public ProjectExePublic() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Exe;

                conf.AddPublicDependency<ProjectDLLPrivate>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectExePublicDLLInheritance : UTestUtilities.UnitTestCommonProject
        {
            public ProjectExePublicDLLInheritance() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Exe;

                conf.AddPublicDependency<ProjectDLLPrivateDLL>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectExeDoublePublicDLLInheritance : UTestUtilities.UnitTestCommonProject
        {
            public ProjectExeDoublePublicDLLInheritance() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Exe;

                conf.AddPublicDependency<ProjectDLLPublicDLL>(target);
            }
        }

        [Sharpmake.Export]
        public class ProjectExported : UTestUtilities.UnitTestCommonProject
        {
            public ProjectExported() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.None;
                conf.LibraryPaths.Add(Directory.GetCurrentDirectory() + "/[project.Name]/" + UTestUtilities.LibOutputFolder);
                conf.LibraryFiles.Add(conf.TargetFileFullName);
            }
        }

        [Sharpmake.Generate]
        public class ProjectF : UTestUtilities.UnitTestCommonProject
        {
            public ProjectF() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<InheritPublicFromPrivateDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectPrivateDependOnExported : UTestUtilities.UnitTestCommonProject
        {
            public ProjectPrivateDependOnExported() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<ProjectExported>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectPublicDependOnExported : UTestUtilities.UnitTestCommonProject
        {
            public ProjectPublicDependOnExported() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<ProjectExported>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectPrivateInheritExported : UTestUtilities.UnitTestCommonProject
        {
            public ProjectPrivateInheritExported() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<ProjectPrivateDependOnExported>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectInheritAsPrivateAndPublic : UTestUtilities.UnitTestCommonProject
        {
            public ProjectInheritAsPrivateAndPublic() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<OnePublicDependencyProject>(target);
                conf.AddPrivateDependency<NoDependencyProject1>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectInheritExportAsPublic : UTestUtilities.UnitTestCommonProject
        {
            public ProjectInheritExportAsPublic() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<ProjectPublicDependOnExported>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectDeepInheritExport : UTestUtilities.UnitTestCommonProject
        {
            public ProjectDeepInheritExport() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<ProjectInheritExportAsPublic>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectDuplicateInDeepInheritance : UTestUtilities.UnitTestCommonProject
        {
            public ProjectDuplicateInDeepInheritance() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<ProjectDeepInheritExport>(target);
                conf.AddPublicDependency<ProjectPublicDependOnExported>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectInheritLibFromDllAndLib : UTestUtilities.UnitTestCommonProject
        {
            public ProjectInheritLibFromDllAndLib() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<AProjectDLL>(target);
                conf.AddPrivateDependency<ProjectDLLPrivate>(target);
                conf.AddPrivateDependency<OnePublicDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class NoDependencyDLLProject : UTestUtilities.UnitTestCommonProject
        {
            public NoDependencyDLLProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Dll;
            }
        }

        [Sharpmake.Generate]
        public class LibDependOnDLLProject : UTestUtilities.UnitTestCommonProject
        {
            public LibDependOnDLLProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<NoDependencyDLLProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class LibInheritLibAndDLLPublicProject : UTestUtilities.UnitTestCommonProject
        {
            public LibInheritLibAndDLLPublicProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<LibDependOnDLLProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class LibInheritLibAndDLLPrivateProject : UTestUtilities.UnitTestCommonProject
        {
            public LibInheritLibAndDLLPrivateProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<LibDependOnDLLProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectInheritIdenticalFromDll : UTestUtilities.UnitTestCommonProject
        {
            public ProjectInheritIdenticalFromDll() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<AProjectDLL>(target);
                conf.AddPrivateDependency<ProjectDLLPublic>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectDllPubDepWithoutLink : UTestUtilities.UnitTestCommonProject
        {
            public ProjectDllPubDepWithoutLink() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.Output = Configuration.OutputType.Dll;

                conf.AddPublicDependency<NoDependencyProject2>(target, DependencySetting.DefaultWithoutLinking);
            }
        }

        [Sharpmake.Generate]
        public class ProjectComplexDllInheritance : UTestUtilities.UnitTestCommonProject
        {
            public ProjectComplexDllInheritance() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPublicDependency<OnePublicDependencyProject>(target, DependencySetting.DefaultWithoutLinking);
                conf.AddPrivateDependency<ProjectDllPubDepWithoutLink>(target, DependencySetting.OnlyBuildOrder);
                conf.AddPublicDependency<NoDependencyProject1>(target, DependencySetting.DefaultWithoutLinking);
                conf.AddPrivateDependency<NoDependencyProject2>(target, DependencySetting.OnlyBuildOrder);
            }
        }

        [Sharpmake.Generate]
        public class ProjectInheritComplexDllInheritance : UTestUtilities.UnitTestCommonProject
        {
            public ProjectInheritComplexDllInheritance() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<ProjectComplexDllInheritance>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectInheritComplexDllInheritanceAndDirect : UTestUtilities.UnitTestCommonProject
        {
            public ProjectInheritComplexDllInheritanceAndDirect() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<ProjectComplexDllInheritance>(target);
                conf.AddPublicDependency<NoDependencyProject2>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectOnlyBuildOrder : UTestUtilities.UnitTestCommonProject
        {
            public ProjectOnlyBuildOrder() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<OnePublicDependencyProject>(target, DependencySetting.OnlyBuildOrder);
            }
        }

        [Sharpmake.Generate]
        public class ProjectInheritBuildOrder : UTestUtilities.UnitTestCommonProject
        {
            public ProjectInheritBuildOrder() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<ProjectOnlyBuildOrder>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectInheritAndBuildOrder : UTestUtilities.UnitTestCommonProject
        {
            public ProjectInheritAndBuildOrder() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<OnePrivateDependencyProject>(target, DependencySetting.OnlyBuildOrder);
                conf.AddPrivateDependency<InheritPrivateFromPrivateDependencyProject>(target);
            }
        }

        [Sharpmake.Generate]
        public class ProjectPubToImmediateAndBuildOrder : UTestUtilities.UnitTestCommonProject
        {
            public ProjectPubToImmediateAndBuildOrder() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.AddPrivateDependency<OnePublicDependencyProject>(target, DependencySetting.OnlyBuildOrder);
                conf.AddPrivateDependency<OnePrivateDependencyProject>(target);
            }
        }
    }
}
