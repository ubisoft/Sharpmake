// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Slnx
    {
        public static class Template
        {
            internal const string SolutionBegin =
@"<Solution>
";
            internal const string SolutionEnd =
@"</Solution>
";
            internal const string ConfigurationsBegin =
@"  <Configurations>
";
            internal const string ConfigurationsEnd =
@"  </Configurations>
";
            internal const string ConfigurationBuildType =
@"    <BuildType Name=""[buildTypeName]"" />
";
            internal const string ConfigurationPlatform =
@"    <Platform Name=""[platformName]"" />
";

            // Folder with nested content. [folderName] is the slnx path form, e.g. "/FastBuild/".
            internal const string FolderBegin =
@"  <Folder Name=""[folderName]"">
";
            internal const string FolderEnd =
@"  </Folder>
";
            internal const string FolderFile =
@"    <File Path=""[filePath]"" />
";

            internal const string DefaultStartupAttribute = @" DefaultStartup=""true""";

            // Project at solution root (no folder).
            internal const string ProjectBegin =
@"  <Project Path=""[projectPath]""[defaultStartup]>
";
            internal const string ProjectEnd =
@"  </Project>
";
            internal const string ProjectSelfClosing =
@"  <Project Path=""[projectPath]""[defaultStartup] />
";
            // Project nested inside a <Folder> (one extra indent level).
            internal const string FolderProjectBegin =
@"    <Project Path=""[projectPath]""[defaultStartup]>
";
            internal const string FolderProjectEnd =
@"    </Project>
";
            internal const string FolderProjectSelfClosing =
@"    <Project Path=""[projectPath]""[defaultStartup] />
";

            // Per-(project x solution-config) rule elements. [indent] is "" at root, "  " inside a folder.
            internal const string RuleBuildType =
@"[indent]    <BuildType Solution=""[solutionConfig]"" Project=""[projectBuildType]"" />
";
            internal const string RulePlatform =
@"[indent]    <Platform Solution=""[solutionConfig]"" Project=""[projectPlatform]"" />
";
            internal const string RuleBuildDisabled =
@"[indent]    <Build Solution=""[solutionConfig]"" Project=""false"" />
";
            internal const string RuleDeploy =
@"[indent]    <Deploy Solution=""[solutionConfig]"" />
";
            internal const string BuildDependency =
@"[indent]    <BuildDependency Project=""[dependencyProjectPath]"" />
";
        }
    }
}
