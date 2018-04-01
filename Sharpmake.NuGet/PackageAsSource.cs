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
using System.Runtime.Serialization;

namespace Sharpmake.NuGet
{
    [DataContract]
    public class PackageAsSource
    {
        /// <summary>
        /// The name of the package.
        /// </summary>
        [DataMember(Name = "packageName")]
        public string PackageName { get; set; }

        /// <summary>
        /// The project files corresponding to the package.
        /// </summary>
        [DataMember(Name = "projectFiles")]
        public List<string> ProjectFiles { get; set; } = new List<string>();
    }
}