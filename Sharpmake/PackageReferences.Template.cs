// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public partial class PackageReferences
    {
        /// <remarks>
        /// See : https://docs.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#controlling-dependency-assets
        /// </remarks>
        private const string TemplateBeginPackageReference = "    <PackageReference Include=\"[packageName]\" Version=\"[packageVersion]\"";
        private const string TemplatePackageIncludeAssets = "        <IncludeAssets>[includeAssets]</IncludeAssets>\n";
        private const string TemplatePackageExcludeAssets = "        <ExcludeAssets>[excludeAssets]</ExcludeAssets>\n";
        private const string TemplatePackagePrivateAssets = "        <PrivateAssets>[privateAssets]</PrivateAssets>\n";
        private const string TemplateEndPackageReference = "    </PackageReference>\n";
    }
}
