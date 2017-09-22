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
using System.Collections.Generic;
using System.IO;
using SimpleNuGet.Impl;

namespace SimpleNuGet
{
    /// <summary>
    /// The project.json file allows to specify nuget references in a more convenient way.
    /// </summary>
    /// <remarks>More info on project.json here: https://docs.microsoft.com/en-us/dotnet/articles/core/tools/project-json </remarks>
    public class ProjectJson : JsonObjectWrapper
    {
        public ProjectJson()
            : base(new JsonObject())
        {
        }

        public ProjectDependencies Dependencies => new ProjectDependencies(GetOrCreate("dependencies"));

        public class ProjectDependencies : JsonDictionaryWrapper<INuGetDependency>
        {
            internal ProjectDependencies(JsonObject jsonObject)
                : base(jsonObject)
            {
            }

            public void Add(string packageName, VersionRange range)
            {
                base.Add(packageName, range.ToString());
            }

            protected override INuGetDependency Wrap(KeyValuePair<string, object> property)
            {
                return new NuGetDependency(property.Key, new VersionRange(property.Value.ToString()));
            }
        }

        public ProjectFrameworks Frameworks => new ProjectFrameworks(GetOrCreate("frameworks"));

        public class ProjectFrameworks : JsonDictionaryWrapper<Framework>
        {
            internal ProjectFrameworks(JsonObject jsonObject)
                : base(jsonObject)
            {
            }

            public void Add(string frameworkName)
            {
                base.Add(frameworkName, new JsonObject());
            }

            protected override Framework Wrap(KeyValuePair<string, object> property)
            {
                return new Framework(property.Key);
            }
        }

        public class Framework
        {
            public string Name { get; }

            public Framework(string name)
            {
                Name = name;
            }
        }

        public ProjectRuntimes Runtimes => new ProjectRuntimes(GetOrCreate("runtimes"));

        public class ProjectRuntimes : JsonDictionaryWrapper<Runtime>
        {
            internal ProjectRuntimes(JsonObject jsonObject)
                : base(jsonObject)
            {
            }

            public void Add(string frameworkName)
            {
                base.Add(frameworkName, new JsonObject());
            }

            protected override Runtime Wrap(KeyValuePair<string, object> property)
            {
                return new Runtime(property.Key);
            }
        }

        public class Runtime
        {
            public string Name { get; }

            public Runtime(string name)
            {
                Name = name;
            }
        }
    }
}