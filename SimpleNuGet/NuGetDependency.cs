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
using System.Linq;

namespace SimpleNuGet
{
    /// <summary>
    /// Simple implementation of package dependencies
    /// </summary>
    public class NuGetDependency : INuGetDependency
    {
        public NuGetDependency(string name, VersionRange versionRange)
        {
            Name = name;
            VersionRange = versionRange;
        }

        public string Name { get; }

        public VersionRange VersionRange { get; }

        public override string ToString()
        {
            return $"{Name} { VersionRange}";
        }

        /// <summary>
        /// Convert a version range into an exact version number. Very naive implementation for now.
        //  More infos on version ranges here https://docs.nuget.org/ndocs/create-packages/dependency-versions#version-ranges
        /// </summary>
        /// <returns>The package version to load that best matches the range.</returns>
        public SemanticVersion GetPackageVersion()
        {
            return VersionRange.GetPackageVersion();
        }
    }
}