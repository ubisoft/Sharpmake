// Copyright (c) 2018-2020 Ubisoft Entertainment
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
using System.Linq;

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
        public void PackageReferencesAdded()
        {
            var project = GetProject<PackageReferencesTestProjects.PublicAndPrivatePackageReferencesProject>();
            Assert.IsNotNull(project);
            foreach (var configuration in project.Configurations)
            {
                Assert.AreEqual(2, configuration.ReferencesByNuGetPackage.Count);
                Assert.True(configuration.ReferencesByNuGetPackage.SortedValues.Any(item => item.PrivateAssets == PackageReferences.DefaultPrivateAssets && item.Name == "NUnit"));
                Assert.True(configuration.ReferencesByNuGetPackage.SortedValues.Any(item => item.PrivateAssets != PackageReferences.DefaultPrivateAssets && item.Name == "NUnit.Console"));
            }
        }

        [Test]
        public void InheritedPublicPackageReferencesToPrivateStillPublic()
        {
            var project = GetProject<PackageReferencesTestProjects.PrivateInheritedPackageReferencesProject>();
            Assert.IsNotNull(project);
            foreach (var configuration in project.Configurations)
            {
                Assert.AreEqual(1, configuration.ReferencesByNuGetPackage.Count);
                Assert.True(configuration.ReferencesByNuGetPackage.SortedValues.Any(item => item.PrivateAssets == PackageReferences.DefaultPrivateAssets && item.Name == "NUnit"));
            }
        }

        [Test]
        public void InheritedPrivatePackageReferencesToPublicBecomePublic()
        {
            var project = GetProject<PackageReferencesTestProjects.PublicInheritedPackageReferencesProject>();
            Assert.IsNotNull(project);
            foreach (var configuration in project.Configurations)
            {
                Assert.AreEqual(1, configuration.ReferencesByNuGetPackage.Count);
                Assert.True(configuration.ReferencesByNuGetPackage.SortedValues.Any(item => item.PrivateAssets == PackageReferences.DefaultPrivateAssets && item.Name == "NUnit"));
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
