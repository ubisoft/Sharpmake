// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public abstract partial class BaseMicrosoftPlatform
    {
        private const string _projectConfigurationsMasmTemplate =
            @"    <MASM>
      <PreprocessorDefinitions>[EscapeXML:options.PreprocessorDefinitions];%(PreprocessorDefinitions);$(PreprocessorDefinitions)</PreprocessorDefinitions>
      <IncludePaths>[options.AdditionalAssemblyIncludeDirectories]</IncludePaths>
    </MASM>
";
    }
}
