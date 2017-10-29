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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sharpmake.NuGet;

namespace Sharpmake
{
    public class PackageReferences
    {
        // NuGet package reference
        // https://docs.microsoft.com/fr-fr/nuget/consume-packages/package-references-in-project-files
        // <PackageReference> is new in VS2017 but in VS2015 you can use project.json (which comes from .NET Core toolchain)
        // to add dependencies for .NET Framework applications
        public class PackageReference : IResolverHelper, IComparable<PackageReference>
        {
            internal PackageReference(string name, string version)
            {
                Name = name;
                Version = version;
            }

            public string Name { get; }
            public string Version { get; }

            public string Resolve(Resolver resolver, string template)
            {
                using (resolver.NewScopedParameter("packageName", Name))
                using (resolver.NewScopedParameter("packageVersion", Version))
                {
                    return resolver.Resolve(template);
                }
            }

            public int CompareTo(PackageReference other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                var nameComparison = string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
                if (nameComparison != 0) return nameComparison;
                return string.Compare(Version, other.Version, StringComparison.OrdinalIgnoreCase);
            }
        }

        private class PackageCollection
        {
            public PackageAsSourceCollection Packages { get; } = new PackageAsSourceCollection();
            public Dictionary<string, HashSet<string>> PackageVersions { get; } = new Dictionary<string, HashSet<string>>();

            private readonly object _packageReferencementLock = new object();

            public PackageCollection()
            {
                string excludeJson = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget\\packageAsSource.json");
                if (!File.Exists(excludeJson))
                    return;

                Packages.Read(excludeJson);
            }

            internal void AddVersion(string packageName, string packageVersion)
            {
                lock (_packageReferencementLock)
                {
                    if (!PackageVersions.ContainsKey(packageName))
                        PackageVersions[packageName] = new HashSet<string>();

                    PackageVersions[packageName].Add(packageVersion);
                }
            }
        }

        private static readonly Lazy<PackageCollection> s_excludedPackages = new Lazy<PackageCollection>();

        private readonly Project.Configuration _configuration;

        public PackageReferences(Project.Configuration conf)
        {
            _configuration = conf;
        }

        private readonly UniqueList<PackageReference> _packageReferences = new UniqueList<PackageReference>();

        public void Add(string packageName, string version)
        {
            var ep = s_excludedPackages.Value.Packages.TryGet(packageName);
            if (ep == null)
            {
                // check package unicity
                var existingPackage = _packageReferences.FirstOrDefault(pr => pr.Name == packageName);
                if (existingPackage == null)
                {
                    _packageReferences.Add(new PackageReference(packageName, version));
                    return;
                }

                if (existingPackage.Version != version)
                {
                    Builder.Instance.LogWarningLine($"Package {packageName} was added twice with versions {version} and {existingPackage.Version}. Version {version} will be used.");
                    _packageReferences.Remove(existingPackage);
                    _packageReferences.Add(new PackageReference(packageName, version));
                }

                return;
            }

            // add files instead
            _configuration.NuGetPackageProjectReferencesByPath.AddRange(ep.ProjectFiles);

            s_excludedPackages.Value.AddVersion(packageName, version);
        }

        public int Count => _packageReferences.Count;

        public IEnumerator<PackageReference> GetEnumerator()
        {
            return _packageReferences.GetEnumerator();
        }

        public List<PackageReference> SortedValues => _packageReferences.SortedValues;

        public static void LogPackagesVersionsDiscrepancy()
        {
            var packageCount = s_excludedPackages.Value.PackageVersions.Count;
            if (packageCount == 0)
                return;

            Builder.Instance.LogWriteLine($"  {packageCount} package" + (packageCount > 1 ? "s" : "") + " switched to sources:");
            foreach (var e in s_excludedPackages.Value.PackageVersions)
            {
                Builder.Instance.LogWriteLine($"    {e.Key}");
                if (e.Value.Count > 1)
                    Builder.Instance.LogWarningLine($"Package {e.Key} was referenced with different versions: {string.Join(", ", e.Value)}.");
            }
        }
    }
}
