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
            Assert.That(project, Is.Not.Null);
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
            Assert.That(project, Is.Not.Null);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
            }
        }

        [Test]
        public void OnePublicDependencyWithoutLinking()
        {
            var project = GetProject<SharpmakeProjects.OnePublicDependencyWithoutLinkingProject>();
            Assert.That(project, Is.Not.Null);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(0));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
            }
        }

        [Test]
        public void OnePublicOnePrivateDependency()
        {
            var project = GetProject<SharpmakeProjects.OnePublicOnePrivateDependencyProject>();
            Assert.That(project, Is.Not.Null);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(1));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"), Is.True);

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);
            }
        }

        [Test]
        public void TwoPublicDependencies()
        {
            var project = GetProject<SharpmakeProjects.TwoPublicDependenciesProject>();
            Assert.That(project, Is.Not.Null);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"), Is.True);

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);
            }
        }

        [Test]
        public void TwoPrivateDependencies()
        {
            var project = GetProject<SharpmakeProjects.TwoPrivateDependenciesProject>();
            Assert.That(project, Is.Not.Null);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"), Is.True);

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);
            }
        }

        [Test]
        public void InheritOnePublicDependency()
        {
            var project = GetProject<SharpmakeProjects.InheritOnePublicDependencyProject>();
            Assert.That(project, Is.Not.Null);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                // The dependency chain is Public all the way, and "Default", so
                // both NoDependencyProject1 and OnePublicDependencyProject 
                // include and library path should be in the arrays
                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"), Is.True);
            }
        }

        [Test]
        public void InheritOnePrivateDependency()
        {
            var project = GetProject<SharpmakeProjects.InheritOnePrivateDependencyProject>();
            Assert.That(project, Is.Not.Null);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                // The dependency chain is PRIVATE from the base project to OnePublic,
                // *but* is Public from OnePublic to NoDependency, so it should be able to access NoDependency
                // Both NoDependencyProject1 and OnePublicDependencyProject include and library path should be in the array
                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"), Is.True);
            }
        }

        [Test]
        public void InheritOnePrivateDependencyOnlyBuildOrder()
        {
            var project = GetProject<SharpmakeProjects.InheritOnePrivateDependencyOnlyBuildOrderProject>();
            Assert.That(project, Is.Not.Null);
            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

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
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.InheritOnePrivateDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(3));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("InheritOnePrivateDependencyProject", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("InheritOnePrivateDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("InheritOnePrivateDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
            }
        }

        [Test]
        public void InheritPrivateDependenciesWithPublic()
        {
            // ProjectA and ProjectB share the same settings, as the only change
            // is the type of the immediate dependency to InheritOnePrivateDependencyProject
            var project = GetProject<SharpmakeProjects.ProjectB>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));

                // this is where ProjectA and ProjectB differ
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.InheritOnePrivateDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(3));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("InheritOnePrivateDependencyProject", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("InheritOnePrivateDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("InheritOnePrivateDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
            }
        }

        [Test]
        public void InheritPublicToPrivateLeaf()
        {
            // ProjectA and ProjectB share the same settings, as the only change
            // is the type of the immediate dependency to InheritOnePrivateDependencyProject
            var project = GetProject<SharpmakeProjects.ProjectF>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));

                // this is where ProjectA and ProjectB differ
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(1));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.InheritPublicFromPrivateDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePrivateDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(3));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("InheritPublicFromPrivateDependencyProject", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePrivateDependencyProject", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("InheritPublicFromPrivateDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePrivateDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("InheritPublicFromPrivateDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("OnePrivateDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
            }
        }

        [Test]
        public void InheritPublicDependenciesWithPrivate()
        {
            var project = GetProject<SharpmakeProjects.ProjectC>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.InheritOnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(3));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("InheritOnePublicDependencyProject", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("InheritOnePublicDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("InheritOnePublicDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
            }
        }

        [Test]
        public void DLLWithDependenciesPublic()
        {
            var project = GetProject<SharpmakeProjects.ProjectDLLPublic>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"), Is.True);
            }
        }

        [Test]
        public void DLLWithDependenciesPrivate()
        {
            var project = GetProject<SharpmakeProjects.ProjectDLLPrivate>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"), Is.True);
            }
        }

        [Test]
        public void DLLWithDLLDependencyPrivate()
        {
            var project = GetProject<SharpmakeProjects.ProjectDLLPrivateDLL>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivate)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPrivate"), Is.True);
            }
        }

        [Test]
        public void ExeWithDLLPrivate()
        {
            var project = GetProject<SharpmakeProjects.ProjectExePrivate>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivate)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPrivate"), Is.True);
            }
        }

        [Test]
        public void ExeWithDLLPublic()
        {
            var project = GetProject<SharpmakeProjects.ProjectExePublic>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivate)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPrivate"), Is.True);
            }
        }

        [Test]
        public void ExeWithDLLPublicInheritance()
        {
            var project = GetProject<SharpmakeProjects.ProjectExePublicDLLInheritance>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivateDLL)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivate)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPrivateDLL", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPrivateDLL", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPrivateDLL"), Is.True);
            }
        }

        [Test]
        public void ExeDoublePublicDLLInheritance()
        {
            var project = GetProject<SharpmakeProjects.ProjectExeDoublePublicDLLInheritance>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPublicDLL)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPublic)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPublicDLL", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPublic", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPublicDLL", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPublic", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPublicDLL"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPublic"), Is.True);
            }
        }



        [Test]
        public void PrivateDependOnExported()
        {
            var project = GetProject<SharpmakeProjects.ProjectPrivateDependOnExported>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(1));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectExported)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectExported"), Is.True);
            }
        }

        [Test]
        public void PrivateInheritExported()
        {
            var project = GetProject<SharpmakeProjects.ProjectPrivateInheritExported>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectPrivateDependOnExported)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectExported)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectPrivateDependOnExported", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectPrivateDependOnExported", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectPrivateDependOnExported"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectExported"), Is.True);
            }
        }

        [Test]
        public void InheritExportAsPublic()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritExportAsPublic>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectPublicDependOnExported)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectExported)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectPublicDependOnExported", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectPublicDependOnExported", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectPublicDependOnExported"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectExported"), Is.True);
            }
        }

        [Test]
        public void DuplicateInDeepInheritance()
        {
            var project = GetProject<SharpmakeProjects.ProjectDuplicateInDeepInheritance>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectPublicDependOnExported)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectExported)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDeepInheritExport)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectInheritExportAsPublic)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(4));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDeepInheritExport", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectPublicDependOnExported", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectPublicDependOnExported", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectExported", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDeepInheritExport", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectInheritExportAsPublic", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectPublicDependOnExported"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectExported"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectDeepInheritExport"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectInheritExportAsPublic"), Is.True);
            }
        }

        [Test]
        public void InheritAsPrivateAndPublic()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritAsPrivateAndPublic>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(0));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
            }
        }

        [Test]
        public void InheritLibFromDllAndLib()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritLibFromDllAndLib>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(5));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(5));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPrivate)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.AProjectDLL)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(4));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("AProjectDLL", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPrivate", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("AProjectDLL", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("OnePublicDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPrivate"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("AProjectDLL"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
            }
        }

        [Test]
        public void InheritIdenticalFromDll()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritIdenticalFromDll>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(4));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.AProjectDLL)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDLLPublic)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("AProjectDLL", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectDLLPublic", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("AProjectDLL", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectDLLPublic", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("AProjectDLL"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectDLLPublic"), Is.True);
            }
        }

        [Test]
        public void ComplexDllInheritance()
        {
            var project = GetProject<SharpmakeProjects.ProjectComplexDllInheritance>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(4));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDllPubDepWithoutLink)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(0));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
            }
        }

        [Test]
        public void InheritFromComplexDllInheritance()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritComplexDllInheritance>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(5));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(5));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectComplexDllInheritance)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDllPubDepWithoutLink)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectComplexDllInheritance", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectComplexDllInheritance", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectComplexDllInheritance"), Is.True);
            }
        }

        [Test]
        public void InheritComplexDllInheritanceAndDirect()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritComplexDllInheritanceAndDirect>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(5));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(4));

                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject2)), Is.True);

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectComplexDllInheritance)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectDllPubDepWithoutLink)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(4));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectComplexDllInheritance", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePublicDependencyProject", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.IncludeFolder)), Is.True);
                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectComplexDllInheritance", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject2", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectComplexDllInheritance"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject2"), Is.True);
            }
        }

        [Test]
        public void OnlyBuildOrder()
        {
            var project = GetProject<SharpmakeProjects.ProjectOnlyBuildOrder>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(0));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void ProjectInheritBuildOrder()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritBuildOrder>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.ProjectOnlyBuildOrder)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(1));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("ProjectOnlyBuildOrder", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("ProjectOnlyBuildOrder", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("ProjectOnlyBuildOrder"), Is.True);
            }
        }

        [Test]
        public void InheritAndBuildOrder()
        {
            var project = GetProject<SharpmakeProjects.ProjectInheritAndBuildOrder>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.InheritPrivateFromPrivateDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePrivateDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(3));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(3));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("InheritPrivateFromPrivateDependencyProject", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("InheritPrivateFromPrivateDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePrivateDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("InheritPrivateFromPrivateDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("OnePrivateDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
            }
        }

        [Test]
        public void PubToImmediateAndBuildOrder()
        {
            var project = GetProject<SharpmakeProjects.ProjectPubToImmediateAndBuildOrder>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(3));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(3));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePublicDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.OnePrivateDependencyProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyProject1)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("OnePrivateDependencyProject", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("OnePrivateDependencyProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyProject1", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("OnePrivateDependencyProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyProject1"), Is.True);
            }
        }

        [Test]
        public void LibInheritLibAndDLLPublic()
        {
            var project = GetProject<SharpmakeProjects.LibInheritLibAndDLLPublicProject>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(1));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(1));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyDLLProject)), Is.True);
                Assert.That(conf.ResolvedPublicDependencies.ContainsProjectType(typeof(SharpmakeProjects.LibDependOnDLLProject)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("LibDependOnDLLProject", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyDLLProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("LibDependOnDLLProject", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyDLLProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("LibDependOnDLLProject"), Is.True);
            }
        }

        [Test]
        public void LibInheritLibAndDLLPrivate()
        {
            var project = GetProject<SharpmakeProjects.LibInheritLibAndDLLPrivateProject>();
            Assert.That(project, Is.Not.Null);

            foreach (var conf in project.Configurations)
            {
                Assert.That<int>(conf.ResolvedDependencies.Count, Is.EqualTo(2));
                Assert.That<int>(conf.ResolvedPublicDependencies.Count, Is.EqualTo(0));
                Assert.That<int>(conf.ResolvedPrivateDependencies.Count, Is.EqualTo(2));

                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.NoDependencyDLLProject)), Is.True);
                Assert.That(conf.ResolvedPrivateDependencies.ContainsProjectType(typeof(SharpmakeProjects.LibDependOnDLLProject)), Is.True);

                Assert.That(conf.DependenciesIncludePaths.Count, Is.EqualTo(1));
                Assert.That(conf.DependenciesLibraryPaths.Count, Is.EqualTo(2));
                Assert.That(conf.DependenciesLibraryFiles.Count, Is.EqualTo(2));

                Assert.That(conf.DependenciesIncludePaths.ContainsElement(Path.Combine("LibDependOnDLLProject", UTestUtilities.IncludeFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("NoDependencyDLLProject", UTestUtilities.LibOutputFolder)), Is.True);
                Assert.That(conf.DependenciesLibraryPaths.ContainsElement(Path.Combine("LibDependOnDLLProject", UTestUtilities.LibOutputFolder)), Is.True);

                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("NoDependencyDLLProject"), Is.True);
                Assert.That(conf.DependenciesLibraryFiles.ContainsElement("LibDependOnDLLProject"), Is.True);
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
