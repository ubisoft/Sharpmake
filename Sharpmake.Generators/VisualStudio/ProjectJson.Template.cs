// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
