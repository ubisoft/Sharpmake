// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Slnx
    {
        public static class Template
        {
            public static string SolutionBegin =
@"<Solution>
";
            public static string SolutionEnd =
@"</Solution>
";
            public static string ConfigurationsBegin =
@"  <Configurations>
";
            public static string ConfigurationsEnd =
@"  </Configurations>
";
            public static string ConfigurationBuildType =
@"    <BuildType Name=""[buildTypeName]"" />
";
            public static string ConfigurationPlatform =
@"    <Platform Name=""[platformName]"" />
";

            // Folder with nested content. [folderName] is the slnx path form, e.g. "/FastBuild/".
            public static string FolderBegin =
@"  <Folder Name=""[folderName]"">
";
            public static string FolderEnd =
@"  </Folder>
";
            public static string FolderFile =
@"    <File Path=""[filePath]"" />
";

            public static string DefaultStartupAttribute = @" DefaultStartup=""true""";

            // Project at solution root (no folder).
            public static string ProjectBegin =
@"  <Project Path=""[projectPath]""[defaultStartup]>
";
            public static string ProjectEnd =
@"  </Project>
";
            public static string ProjectSelfClosing =
@"  <Project Path=""[projectPath]""[defaultStartup] />
";
            // Project nested inside a <Folder> (one extra indent level).
            public static string FolderProjectBegin =
@"    <Project Path=""[projectPath]""[defaultStartup]>
";
            public static string FolderProjectEnd =
@"    </Project>
";
            public static string FolderProjectSelfClosing =
@"    <Project Path=""[projectPath]""[defaultStartup] />
";

            // Per-(project x solution-config) rule elements. [indent] is "" at root, "  " inside a folder.
            public static string RuleBuildType =
@"[indent]    <BuildType Solution=""[solutionConfig]"" Project=""[projectBuildType]"" />
";
            public static string RulePlatform =
@"[indent]    <Platform Solution=""[solutionConfig]"" Project=""[projectPlatform]"" />
";
            public static string RuleBuildDisabled =
@"[indent]    <Build Solution=""[solutionConfig]"" Project=""false"" />
";
            public static string RuleDeploy =
@"[indent]    <Deploy Solution=""[solutionConfig]"" />
";
            public static string BuildDependency =
@"[indent]    <BuildDependency Project=""[dependencyProjectPath]"" />
";
        }
    }
}
