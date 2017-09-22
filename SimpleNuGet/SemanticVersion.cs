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
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using SimpleNuGet.Impl;

namespace SimpleNuGet
{
    /// <summary>
    /// A semantic version identifies a package build
    /// </summary>
    /// <remarks>More info on versions here: http://semver.org/ </remarks>
    [TypeConverter(typeof(SemanticVersionTypeConverter))]
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        /// <summary>
        /// The numeric version in the form (Major.Minor.Revision.Patch)
        /// </summary>
        public string VersionNumber { get; }

        /// <summary>
        /// The pre release tag, for example: -beta2.3
        /// </summary>
        public string PreReleaseTag { get; }

        /// <summary>
        /// A pre release version has a pre release tag. For example: 1.1-alpha
        /// </summary>
        public bool IsPreRelease => !string.IsNullOrEmpty(PreReleaseTag);

        public SemanticVersion(string versionNumber, string preReleaseTag)
        {
            if (string.IsNullOrEmpty(versionNumber)) { throw new ArgumentException(nameof(versionNumber)); }
            if (!IsValidVersionNumber(versionNumber)) { throw new ArgumentException(nameof(versionNumber), "Invalid version number"); }

            if (!string.IsNullOrEmpty(preReleaseTag))
            {
                preReleaseTag = preReleaseTag.TrimStart('-');
            }

            VersionNumber = versionNumber;
            PreReleaseTag = preReleaseTag;
        }

        /// <summary>
        /// Parse the version string
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the input is not of valid form.</exception>
        public static SemanticVersion Parse(string input)
        {
            if (string.IsNullOrEmpty(input)) { throw new ArgumentException(nameof(input)); }

            input = input.TrimStart('v', '.');
            var parts = input.Split('-');

            if (!IsValidVersionNumber(parts[0]))
            {
                throw new ArgumentException($"{parts[0]} is not a valid version number. Expected a version in the form 2.3.4");
            }

            return new SemanticVersion(parts[0], string.Join("-", parts.Skip(1)));
        }

        /// <summary>
        /// Try to parse the input as a semantic version, returns false and null when it fails.
        /// </summary>
        public static bool TryParse(string input, out SemanticVersion result)
        {
            if (string.IsNullOrEmpty(input))
            {
                result = null;
                return false;
            }

            input = input.TrimStart('v', '.');
            var parts = input.Split('-');

            if (!IsValidVersionNumber(parts[0]))
            {
                result = null;
                return false;
            }

            result = new SemanticVersion(parts[0], String.Join("-", parts.Skip(1)));
            return true;
        }

        private static bool IsValidVersionNumber(string versionNumber)
        {
            Version ignored;
            return Version.TryParse(versionNumber, out ignored);
        }

        public override string ToString()
        {
            if (IsPreRelease)
            {
                return $"{VersionNumber}-{PreReleaseTag}";
            }

            return $"{VersionNumber}";
        }

        /// <summary>
        /// NuGet does not support a standard semantic version v2, we have to format it in a special way.
        /// </summary>
        /// <returns></returns>
        public string ToNuGetString()
        {
            var number = VersionNumber;
            for (int i = VersionNumber.Count(x => x == '.'); i < 2; i++)
            {
                number += ".0";
            }

            if (IsPreRelease)
            {
                return $"{number}-{PreReleaseTag}";
            }

            return $"{number}";
        }

        #region Operator overloading

        /// <summary>
        /// Gets the hash code for this unique key.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (VersionNumber ?? "").GetHashCode() ^ (PreReleaseTag ?? "").GetHashCode();
        }

        public int CompareTo(SemanticVersion x)
        {
            return CompareTo(this, x);
        }

        public bool Equals(SemanticVersion x)
        {
            return CompareTo(this, x) == 0;
        }

        public static bool operator <(SemanticVersion x, SemanticVersion y)
        {
            return CompareTo(x, y) < 0;
        }

        public static bool operator >(SemanticVersion x, SemanticVersion y)
        {
            return CompareTo(x, y) > 0;
        }

        public static bool operator <=(SemanticVersion x, SemanticVersion y)
        {
            return CompareTo(x, y) <= 0;
        }

        public static bool operator >=(SemanticVersion x, SemanticVersion y)
        {
            return CompareTo(x, y) >= 0;
        }

        public static bool operator ==(SemanticVersion x, SemanticVersion y)
        {
            return CompareTo(x, y) == 0;
        }

        public static bool operator !=(SemanticVersion x, SemanticVersion y)
        {
            return CompareTo(x, y) != 0;
        }

        public override bool Equals(object obj)
        {
            return (obj is SemanticVersion) && (CompareTo(this, (SemanticVersion)obj) == 0);
        }

        private static int CompareTo(SemanticVersion a, SemanticVersion b)
        {
            // A null version is unspecified and should be considered larger than a fixed one.
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                {
                    return 0;
                }

                return 1;
            }

            if (ReferenceEquals(b, null))
            {
                return -1;
            }

            var versionComparison = new NumericString(a.VersionNumber).CompareTo(new NumericString(b.VersionNumber));
            if (versionComparison != 0)
            {
                return versionComparison;
            }

            var isPreReleaseComparison = a.IsPreRelease.CompareTo(b.IsPreRelease) * -1;
            if (isPreReleaseComparison != 0)
            {
                return isPreReleaseComparison;
            }

            return new NumericString(a.PreReleaseTag).CompareTo(new NumericString(b.PreReleaseTag));
        }

        #endregion

        #region Components

        /// <summary>
        /// Returns the value of one component in the semantic version (Major, Minor, Patch).
        /// </summary>
        public int GetComponent(SemanticVersionComponent component)
        {
            var parts = VersionNumber.Split('.');
            if (parts.Length <= (int)component)
            {
                return 0;
            }
            return int.Parse(parts[(int)component]);
        }

        /// <summary>
        /// Increments the value of a component in the version.
        /// </summary>
        [Pure]
        public SemanticVersion Increment(SemanticVersionComponent component)
        {
            var actual = GetComponent(component);
            return SetComponent(actual + 1, component);
        }

        /// <summary>
        /// Sets the value of a component and returns a new version with that value.
        /// </summary>
        [Pure]
        public SemanticVersion SetComponent(int newValue, SemanticVersionComponent component)
        {
            var parts = VersionNumber.Split('.').ToList();
            while (parts.Count <= (int)component)
            {
                parts.Add("0");
            }

            parts[(int)component] = newValue.ToString();
            return new SemanticVersion(string.Join(".", parts), PreReleaseTag);
        }

        #endregion
    }

    /// <summary>
    /// Represents in order the different components of a semantic version.
    /// </summary>
    public enum SemanticVersionComponent
    {
        /// <summary>
        /// MAJOR version when you make incompatible API changes,
        /// </summary>
        Major,
        /// <summary>
        /// MINOR version when you add functionality in a backwards-compatible manner
        /// </summary>
        Minor,
        /// <summary>
        /// PATCH version when you make backwards-compatible bug fixes.
        /// </summary>
        Patch
    }

    public class SemanticVersionTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
                return SemanticVersion.Parse((string)value);

            return default(SemanticVersion);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return ((SemanticVersion)value).ToString();
        }
    }
}