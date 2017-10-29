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
using System.Linq;

namespace Sharpmake.NuGet
{
    /// <summary>
    /// Version range
    /// </summary>
    /// <remarks>More info on versions here: https://docs.nuget.org/ndocs/create-packages/dependency-versions#version-ranges</remarks>
    public class VersionRange
    {
        private readonly string _versionRange;

        public VersionRange(string versionRange)
        {
            _versionRange = versionRange;
        }

        /// <summary>
        /// Parse the version range string
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the input is not of valid form.</exception>
        public static VersionRange Parse(string input)
        {
            if (!IsValidVersionRange(input)) { throw new ArgumentException("Invalid version range", nameof(input)); }

            return new VersionRange(input);
        }

        /// <summary>
        /// Try to parse the input as a semantic version, returns false and null when it fails.
        /// </summary>
        public static bool TryParse(string input, out VersionRange result)
        {
            if (string.IsNullOrEmpty(input))
            {
                result = null;
                return false;
            }

            if (!IsValidVersionRange(input))
            {
                result = null;
                return false;
            }

            result = new VersionRange(input);
            return true;
        }

        private static bool IsValidVersionRange(string input)
        {
            if (input == null)
                return false;

            Version ignored;
            return input.TrimStart('[', '(').TrimEnd(')', ']').Split(',').All(part => string.IsNullOrWhiteSpace(part) || Version.TryParse(part, out ignored));
        }

        /// <summary>
        /// Convert a version range into an exact version number. Very naive implementation for now.
        //  More infos on version ranges here https://docs.nuget.org/ndocs/create-packages/dependency-versions#version-ranges
        /// </summary>
        /// <returns>The package version to load that best matches the range.</returns>
        public SemanticVersion GetPackageVersion()
        {
            // Some libraries use that syntax "[3.5.0, )" for minimum version which is similar to "3.5.0"
            var versionRange = _versionRange.Replace(" ", "").Replace(",)", "");

            if (versionRange.Contains(',') | versionRange.Contains('(') | versionRange.Contains(')'))
                throw new NotSupportedException("Complex version range not supported.");

            versionRange = versionRange.TrimStart('[').TrimEnd(']');
            return SemanticVersion.Parse(versionRange);
        }

        public override string ToString()
        {
            return $"{_versionRange}";
        }

        protected bool Equals(VersionRange other)
        {
            return string.Equals(_versionRange, other._versionRange);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((VersionRange)obj);
        }

        public override int GetHashCode()
        {
            return _versionRange?.GetHashCode() ?? 0;
        }
    }
}