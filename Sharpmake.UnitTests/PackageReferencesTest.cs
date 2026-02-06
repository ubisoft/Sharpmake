// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Linq;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    [TestFixture]
    public class PackageReferencesCSharpTests : CSharpTestProjectBuilder
    {
        public PackageReferencesCSharpTests()
            : base(typeof(PackageReferencesTestProjects.PublicAndPrivatePackageReferencesProject).Namespace)
        {
        }

        [Test]
        public void PackageReferencesAssetsDependency()
        {
            foreach (PackageReferences.AssetsDependency dep in System.Enum.GetValues(typeof(PackageReferences.AssetsDependency)))
            {
                var formatted = PackageReferences.PackageReference.GetFormatedAssetsDependency(dep);
                Assert.That(formatted.Count(), Is.EqualTo(1));
                Assert.That(formatted.First().ToLower(), Is.EqualTo(dep.ToString().ToLower()));
            }
        }

        [Test]
        public void PackageReferencesAdded()
        {
            var project = GetProject<PackageReferencesTestProjects.PublicAndPrivatePackageReferencesProject>();
            Assert.That(project, Is.Not.Null);
            foreach (var configuration in project.Configurations)
            {
                Assert.That(configuration.ReferencesByNuGetPackage.Count, Is.EqualTo(2));
                Assert.That(configuration.ReferencesByNuGetPackage.SortedValues.Any(item => item.PrivateAssets == PackageReferences.DefaultPrivateAssets && item.Name == "NUnit"), Is.True);
                Assert.That(configuration.ReferencesByNuGetPackage.SortedValues.Any(item => item.PrivateAssets != PackageReferences.DefaultPrivateAssets && item.Name == "NUnit.Console"), Is.True);
            }
        }

        [Test]
        public void InheritedPublicPackageReferencesToPrivateStillPublic()
        {
            var project = GetProject<PackageReferencesTestProjects.PrivateInheritedPackageReferencesProject>();
            Assert.That(project, Is.Not.Null);
            foreach (var configuration in project.Configurations)
            {
                Assert.That(configuration.ReferencesByNuGetPackage.Count, Is.EqualTo(1));
                Assert.That(configuration.ReferencesByNuGetPackage.SortedValues.Any(item => item.PrivateAssets == PackageReferences.DefaultPrivateAssets && item.Name == "NUnit"), Is.True);
            }
        }

        [Test]
        public void InheritedPrivatePackageReferencesToPublicBecomePublic()
        {
            var project = GetProject<PackageReferencesTestProjects.PublicInheritedPackageReferencesProject>();
            Assert.That(project, Is.Not.Null);
            foreach (var configuration in project.Configurations)
            {
                Assert.That(configuration.ReferencesByNuGetPackage.Count, Is.EqualTo(1));
                Assert.That(configuration.ReferencesByNuGetPackage.SortedValues.Any(item => item.PrivateAssets == PackageReferences.DefaultPrivateAssets && item.Name == "NUnit"), Is.True);
            }
        }
    }

    namespace PackageReferencesTestProjects
    {
        [Generate]
        public class PublicAndPrivatePackageReferencesProject : CSharpUnitTestCommonProject
        {
            public PublicAndPrivatePackageReferencesProject() { }

            [Configure()]
            public void ConfigureAll(Configuration conf, Target target)
            {
                conf.ReferencesByNuGetPackage.Add("NUnit", "3.4.1");
                conf.ReferencesByNuGetPackage.Add("NUnit.Console", "3.4.1", privateAssets: PackageReferences.AssetsDependency.All);
            }
        }

        [Generate]
        public class PublicBasePackageReferencesProject : CSharpUnitTestCommonProject
        {
            public PublicBasePackageReferencesProject() { }

            [Configure()]
            public virtual void ConfigureAll(Configuration conf, Target target)
            {
                conf.ReferencesByNuGetPackage.Add("NUnit", "3.4.1");
            }
        }

        [Generate]
        public class PrivateBasePackageReferencesProject : CSharpUnitTestCommonProject
        {
            public PrivateBasePackageReferencesProject() { }

            [Configure()]
            public virtual void ConfigureAll(Configuration conf, Target target)
            {
                conf.ReferencesByNuGetPackage.Add("NUnit", "3.4.1", privateAssets: PackageReferences.AssetsDependency.All);
            }
        }

        [Generate]
        public class PrivateInheritedPackageReferencesProject : PublicBasePackageReferencesProject
        {
            public PrivateInheritedPackageReferencesProject() { }

            public override void ConfigureAll(Configuration conf, Target target)
            {
                base.ConfigureAll(conf, target);

                conf.ReferencesByNuGetPackage.Add("NUnit", "3.4.1", privateAssets: PackageReferences.AssetsDependency.All);
            }
        }

        [Generate]
        public class PublicInheritedPackageReferencesProject : PrivateBasePackageReferencesProject
        {
            public PublicInheritedPackageReferencesProject() { }

            public override void ConfigureAll(Configuration conf, Target target)
            {
                base.ConfigureAll(conf, target);

                conf.ReferencesByNuGetPackage.Add("NUnit", "3.4.1");
            }
        }
    }
}
