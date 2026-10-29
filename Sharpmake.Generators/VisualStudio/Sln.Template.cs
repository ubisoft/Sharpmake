// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;

namespace Sharpmake.Generators.VisualStudio
{
    public partial class Sln
    {
        public static class Template
        {
            public static class Solution
            {
                [Obsolete("Sharpmake doesn't support vs2010 anymore.")]
                internal const string HeaderBeginVs2010 =
@"Microsoft Visual Studio Solution File, Format Version 11.00
# Visual Studio 2010
";

                [Obsolete("Sharpmake doesn't support vs2012 anymore.")]
                internal const string HeaderBeginVs2012 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2012
";
                [Obsolete("Sharpmake doesn't support vs2013 anymore.")]
                internal const string HeaderBeginVs2013 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2013
";

                internal const string HeaderBeginVs2015 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 14
";

                internal const string HeaderBeginVs2017 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 15
";

                internal const string HeaderBeginVs2019 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29424.173
MinimumVisualStudioVersion = 10.0.40219.1
";

                internal const string HeaderBeginVs2022 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
";

                internal const string HeaderBeginVs2026 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 18
";

                internal const string ProjectBegin =
@"Project(""{[projectTypeGuid]}"") = ""[projectName]"", ""[projectFile]"", ""{[projectGuid]}""
";

                internal const string ProjectDependencyBegin =
@"	ProjectSection(ProjectDependencies) = postProject
";

                internal const string ProjectDependency =
@"		{[projectDependencyGuid]} = {[projectDependencyGuid]}
";

                internal const string SolutionItemBegin =
@"	ProjectSection(SolutionItems) = preProject
";

                internal const string SolutionItem =
@"		[solutionItemPath] = [solutionItemPath]
";

                internal const string ProjectSectionEnd =
@"	EndProjectSection
";

                internal const string ProjectEnd =
@"EndProject
";

                internal const string ProjectFolder =
@"Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""[folderName]"", ""[folderName]"", ""{[folderGuid]}""
";

                internal const string HeaderEnd =
@"EndProject
";

                internal const string GlobalBegin =
@"Global
";

                internal const string GlobalEnd =
@"EndGlobal
";

                internal const string SolutionProperties =
@"	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
";

                [Obsolete("This property is deprecated, scc info shouldn't be stored in the solution files anymore", error: true)]
                internal static readonly string GlobalSectionSolutionSourceCodeControlBegin;

                [Obsolete("This property is deprecated, scc info shouldn't be stored in the solution files anymore", error: true)]
                internal static readonly string GlobalSectionSolutionSourceCodeControlProject;

                [Obsolete("This property is deprecated, scc info shouldn't be stored in the solution files anymore", error: true)]
                internal static readonly string GlobalSectionSolutionSourceCodeControlEnd;

                internal const string GlobalSectionSolutionConfigurationBegin =
@"	GlobalSection(SolutionConfigurationPlatforms) = preSolution
";
                internal const string GlobalSectionSolutionConfiguration =
@"		[configurationName]|[category] = [configurationName]|[category]
";
                internal const string GlobalSectionSolutionConfigurationEnd =
@"	EndGlobalSection
";

                internal const string GlobalSectionProjectConfigurationBegin =
@"	GlobalSection(ProjectConfigurationPlatforms) = postSolution
";

                internal const string GlobalSectionProjectConfigurationActive =
@"		{[projectGuid]}.[configurationName]|[category].ActiveCfg = [projectConf.Name]|[projectPlatform]
";
                internal const string GlobalSectionProjectConfigurationBuild =
@"		{[projectGuid]}.[configurationName]|[category].Build.0 = [projectConf.Name]|[projectPlatform]
";
                internal const string GlobalSectionProjectConfigurationDeploy =
@"		{[projectGuid]}.[configurationName]|[category].Deploy.0 = [projectConf.Name]|[projectPlatform]
";
                internal const string GlobalSectionProjectConfigurationEnd =
@"	EndGlobalSection
";

                internal const string NestedProjectBegin =
@"	GlobalSection(NestedProjects) = preSolution
";
                internal const string NestedProjectItem =
@"		{[nestedChildGuid]} = {[nestedParentGuid]}
";
                internal const string NestedProjectEnd =
@"	EndGlobalSection
";
            }
        }
    }
}
