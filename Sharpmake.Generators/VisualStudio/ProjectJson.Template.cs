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
namespace Sharpmake.Generators.VisualStudio
{
    internal partial class ProjectJson
    {
        public static class Template
        {
            public static string Begin =
@"{";

            public static string End =
@"
}";

            public static string FrameworksBegin =
@"
  ""frameworks"": {";

            public static string FrameworksEnd =
@"
  }";

            public static string FrameworksItem =
@"
    ""[framework]"": { }";

            public static string RuntimesBegin =
@"
  ""runtimes"": {";

            public static string RuntimesEnd =
@"
  }";
            public static string RuntimesItem =
@"
    ""[runtime]"": { }";

            public static string DependenciesBegin =
@"
  ""dependencies"": {";

            public static string DependenciesEnd =
@"
  }";

            public static string DependenciesItem =
@"
    ""[dependency.Name]"": ""[dependency.Version]""";

            /// <remarks>
            /// See : https://github.com/NuGet/Home/wiki/%5BSpec%5D-Managing-dependency-package-assets#suppress-parent
            /// </remarks>
            public static string BeginDependencyItem =
@"
    ""[dependency.Name]"": {
        ""version"": ""[dependency.Version]""";

            public static string DependencyPrivateAssets =
@",
        ""suppressParent"": ""[privateAssets]""";

            public static string DependencyReferenceType =
@",
        ""type"": ""[referenceType]""";

            public static string EndDependencyItem =
@"
    }";
        }
    }
}
