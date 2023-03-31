// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sharpmake
{
    public partial class PackageReferences
    {
        // NuGet package reference
        // https://docs.microsoft.com/fr-fr/nuget/consume-packages/package-references-in-project-files
        // <PackageReference> is new in VS2017 but in VS2015 you can use project.json (which comes from .NET Core toolchain)
        // For VS2012 you can use packages.config and references
        // to add dependencies for .NET Framework applications
        [DebuggerDisplay("{Name} {Version}")]
        public class PackageReference : IResolverHelper, IComparable<PackageReference>
        {
            internal PackageReference(string name, string version, string dotNetHint, AssetsDependency privateAssets, string referenceType)
            {
                Name = name;
                Version = version;
                DotNetHint = dotNetHint;
                PrivateAssets = privateAssets;
                ReferenceType = referenceType;
            }

            internal PackageReference(string name, string version, string dotNetHint, AssetsDependency privateAssets)
                : this(name, version, dotNetHint, privateAssets, null)
            {
            }

            public string Name { get; internal set; }
            public string Version { get; internal set; }
            public string DotNetHint { get; internal set; }
            public string ReferenceType { get; internal set; }

            public AssetsDependency PrivateAssets { get; internal set; }

            public string Resolve(Resolver resolver)
            {
                using (resolver.NewScopedParameter("packageName", Name))
                using (resolver.NewScopedParameter("packageVersion", Version))
                {
                    if (PrivateAssets == DefaultPrivateAssets)
                    {
                        return resolver.Resolve($"{TemplateBeginPackageReference} />\n");
                    }
                    else
                    {
                        using (resolver.NewScopedParameter("privateAssets", string.Join(";", GetFormatedAssetsDependency(PrivateAssets))))
                            return resolver.Resolve($"{TemplateBeginPackageReference}>\n{TemplatePackagePrivateAssets}{TemplateEndPackageReference}");
                    }
                }
            }

            public string Resolve(Resolver resolver, string customTemplate)
            {
                using (resolver.NewScopedParameter("packageName", Name))
                using (resolver.NewScopedParameter("packageVersion", Version))
                {
                    return resolver.Resolve(customTemplate);
                }
            }

            public int CompareTo(PackageReference other)
            {
                if (ReferenceEquals(this, other))
                    return 0;
                if (ReferenceEquals(null, other))
                    return 1;
                var nameComparison = string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
                if (nameComparison != 0)
                    return nameComparison;
                var versionComparison = string.Compare(Version, other.Version, StringComparison.OrdinalIgnoreCase);
                if (versionComparison != 0)
                    return versionComparison;
                var referenceTypeComparison = string.Compare(ReferenceType, other.ReferenceType, StringComparison.OrdinalIgnoreCase);
                if (referenceTypeComparison != 0)
                    return referenceTypeComparison;
                return string.Compare(string.Join(",", GetFormatedAssetsDependency(PrivateAssets)), string.Join(",", GetFormatedAssetsDependency(other.PrivateAssets)), StringComparison.OrdinalIgnoreCase);
            }

            internal static IEnumerable<string> GetFormatedAssetsDependency(AssetsDependency dependency)
            {
                if (dependency == AssetsDependency.None)
                {
                    yield return "none";
                    yield break;
                }

                if (dependency == AssetsDependency.All)
                {
                    yield return "all";
                    yield break;
                }

                if (dependency.HasFlag(AssetsDependency.Compile))
                {
                    yield return "compile";
                }

                if (dependency.HasFlag(AssetsDependency.Runtime))
                {
                    yield return "runtime";
                }

                if (dependency.HasFlag(AssetsDependency.ContentFiles))
                {
                    yield return "contentFiles";
                }

                if (dependency.HasFlag(AssetsDependency.Build))
                {
                    yield return "build";
                }

                if (dependency.HasFlag(AssetsDependency.Analyzers))
                {
                    yield return "analyzers";
                }

                if (dependency.HasFlag(AssetsDependency.Native))
                {
                    yield return "native";
                }

                if (dependency.HasFlag(AssetsDependency.BuildMultitargeting))
                {
                    yield return "buildMultitargeting";
                }

                if (dependency.HasFlag(AssetsDependency.BuildTransitive))
                {
                    yield return "buildTransitive";
                }
            }
        }

        private readonly UniqueList<PackageReference> _packageReferences = new UniqueList<PackageReference>();

        public void Add(string packageName, string version, string dotNetHint = null, AssetsDependency privateAssets = DefaultPrivateAssets, string referenceType = null)
        {
            // check package unicity
            var existingPackage = _packageReferences.FirstOrDefault(pr => pr.Name == packageName);
            if (existingPackage == null)
            {
                _packageReferences.Add(new PackageReference(packageName, version, null, privateAssets, referenceType));
                return;
            }

            if (existingPackage.Version != version)
            {
                Builder.Instance.LogWarningLine($"Package {packageName} was added twice with versions {version} and {existingPackage.Version}. Version {version} will be used.");
                existingPackage.Version = version;
            }

            if (privateAssets != existingPackage.PrivateAssets)
            {
                existingPackage.PrivateAssets &= privateAssets;
                Builder.Instance.LogWarningLine($"Package {packageName} was added twice with different private assets. Kept assets are {string.Join(",", PackageReference.GetFormatedAssetsDependency(existingPackage.PrivateAssets))}.");
            }
        }

        public void Add(string packageName, string version, string dotNetHint, AssetsDependency privateAssets)
        {
            Add(packageName, version, dotNetHint, privateAssets, null);
        }

        public int Count => _packageReferences.Count;

        public IEnumerator<PackageReference> GetEnumerator()
        {
            return _packageReferences.GetEnumerator();
        }

        public List<PackageReference> SortedValues => _packageReferences.SortedValues;

        [Flags]
        public enum AssetsDependency : uint
        {
            None = 0,
            Compile = 1 << 0,
            Runtime = 1 << 1,
            [Obsolete("Use " + nameof(ContentFiles) + " instead")]
            ContentFile = 1 << 2,
            ContentFiles = 1 << 2,
            Build = 1 << 3,
            [Obsolete("Use " + nameof(Analyzers) + " instead")]
            Analysers = 1 << 4,
            Analyzers = 1 << 4,
            Native = 1 << 5,
            BuildMultitargeting = 1 << 6,
            BuildTransitive = 1 << 7,
            All = Compile | Runtime | ContentFiles | Build | Analyzers | Native | BuildMultitargeting | BuildTransitive
        }

        internal const AssetsDependency DefaultPrivateAssets =
            AssetsDependency.ContentFiles | AssetsDependency.Analyzers | AssetsDependency.Build;
    }
}
