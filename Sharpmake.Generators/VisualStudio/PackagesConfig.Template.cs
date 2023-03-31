// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.VisualStudio
{
    internal partial class PackagesConfig
    {
        public static class Template
        {
            public static string Begin =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>";

            public static string End =
@"
</packages>";

            public static string DependenciesItem =
@"
  <package id=""[dependency.Name]"" version=""[dependency.Version]"" targetFramework=""[framework]"" />";
        }
    }
}
