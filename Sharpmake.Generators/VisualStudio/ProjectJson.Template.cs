// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.VisualStudio
{
    internal partial class ProjectJson
    {
        public static class Template
        {
            internal const string Begin =
@"{";

            internal const string End =
@"
}";

            internal const string FrameworksBegin =
@"
  ""frameworks"": {";

            internal const string FrameworksEnd =
@"
  }";

            internal const string FrameworksItem =
@"
    ""[framework]"": { }";

            internal const string RuntimesBegin =
@"
  ""runtimes"": {";

            internal const string RuntimesEnd =
@"
  }";
            internal const string RuntimesItem =
@"
    ""[runtime]"": { }";

            internal const string DependenciesBegin =
@"
  ""dependencies"": {";

            internal const string DependenciesEnd =
@"
  }";

            internal const string DependenciesItem =
@"
    ""[dependency.Name]"": ""[dependency.Version]""";

            /// <remarks>
            /// See : https://github.com/NuGet/Home/wiki/%5BSpec%5D-Managing-dependency-package-assets#suppress-parent
            /// </remarks>
            internal const string BeginDependencyItem =
@"
    ""[dependency.Name]"": {
        ""version"": ""[dependency.Version]""";

            internal const string DependencyPrivateAssets =
@",
        ""suppressParent"": ""[privateAssets]""";

            internal const string DependencyReferenceType =
@",
        ""type"": ""[referenceType]""";

            internal const string EndDependencyItem =
@"
    }";
        }
    }
}
