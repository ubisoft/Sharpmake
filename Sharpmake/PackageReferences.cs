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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sharpmake
{
    public class PackageReferences
    {
        // NuGet package reference
        // https://docs.microsoft.com/fr-fr/nuget/consume-packages/package-references-in-project-files
        // <PackageReference> is new in VS2017 but in VS2015 you can use project.json (which comes from .NET Core toolchain)
        // For VS2012 you can use packages.config and references
        // to add dependencies for .NET Framework applications
        [DebuggerDisplay("{Name} {Version}")]
        public class PackageReference : IResolverHelper, IComparable<PackageReference>
        {
            internal PackageReference(string name, string version, string dotNetHint)
            {
                Name = name;
                Version = version;
                DotNetHint = dotNetHint;
            }

            public string Name { get; internal set; }
            public string Version { get; internal set; }
            public string DotNetHint { get; internal set; }

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

        private readonly UniqueList<PackageReference> _packageReferences = new UniqueList<PackageReference>();

        public void Add(string packageName, string version, string dotNetHint = null)
        {
            // check package unicity
            var existingPackage = _packageReferences.FirstOrDefault(pr => pr.Name == packageName);
            if (existingPackage == null)
            {
                _packageReferences.Add(new PackageReference(packageName, version, dotNetHint));
                return;
            }

            if (existingPackage.Version != version)
            {
                Builder.Instance.LogWarningLine($"Package {packageName} was added twice with versions {version} and {existingPackage.Version}. Version {version} will be used.");
                existingPackage.Version = version;
            }
        }

        public int Count => _packageReferences.Count;

        public IEnumerator<PackageReference> GetEnumerator()
        {
            return _packageReferences.GetEnumerator();
        }

        public List<PackageReference> SortedValues => _packageReferences.SortedValues;
    }
}
