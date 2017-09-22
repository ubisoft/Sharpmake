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
using SimpleNuGet.Impl;

namespace SimpleNuGet
{
    /// <summary>
    /// Packages in %userprofile%/.nuget/PackageAsSource.json are typically added
    /// as source instead of reference in a generated project.
    /// </summary>
    public class PackageAsSourceCollection
    {
        /// <summary>
        /// The path where this configuration is stored.
        /// </summary>
        public static string Path => Environment.ExpandEnvironmentVariables(@"%userprofile%\.nuget\packageAsSource.json");

        public List<PackageAsSource> Packages { get; } = new List<PackageAsSource>();

        /// <summary>
        /// Reads from the config file.
        /// </summary>
        public void Read(string path = null)
        {
            path = path ?? Path;
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                ReadFromString(content);
            }
        }

        /// <summary>
        /// Writes to the config file.
        /// </summary>
        public void Write(string path = null)
        {
            path = path ?? Path;
            var content = WriteToString();
            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Reads the json content into the current object.
        /// </summary>
        public void ReadFromString(string json)
        {
            var newPackages = SimpleJson.DeserializeObject<PackageAsSource[]>(json);
            Packages.Clear();
            Packages.AddRange(newPackages);
        }

        /// <summary>
        /// Returns a json string corresponding to the package list.
        /// </summary>
        public string WriteToString()
        {
            return SimpleJson.SerializeObject(Packages.ToArray());
        }

        /// <summary>
        /// Returns the package if declared in the package as source list (null otherwise).
        /// </summary>
        public PackageAsSource TryGet(string packageName)
        {
            return Packages.FirstOrDefault(x => string.Equals(x.PackageName, packageName, StringComparison.OrdinalIgnoreCase));
        }
    }
}