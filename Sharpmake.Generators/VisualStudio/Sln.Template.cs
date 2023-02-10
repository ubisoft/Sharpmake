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
                public static string HeaderBeginVs2010 =
@"Microsoft Visual Studio Solution File, Format Version 11.00
# Visual Studio 2010
";

                [Obsolete("Sharpmake doesn't support vs2012 anymore.")]
                public static string HeaderBeginVs2012 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2012
";
                [Obsolete("Sharpmake doesn't support vs2013 anymore.")]
                public static string HeaderBeginVs2013 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2013
";

                public static string HeaderBeginVs2015 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 14
";

                public static string HeaderBeginVs2017 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 15
";

                public static string HeaderBeginVs2019 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29424.173
MinimumVisualStudioVersion = 10.0.40219.1
";

                public static string HeaderBeginVs2022 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
";

                public static string ProjectBegin =
@"Project(""{[projectTypeGuid]}"") = ""[projectName]"", ""[projectFile]"", ""{[projectGuid]}""
";

                public static string ProjectDependencyBegin =
@"	ProjectSection(ProjectDependencies) = postProject
";

                public static string ProjectDependency =
@"		{[projectDependencyGuid]} = {[projectDependencyGuid]}
";

                public static string SolutionItemBegin =
@"	ProjectSection(SolutionItems) = preProject
";

                public static string SolutionItem =
@"		[solutionItemPath] = [solutionItemPath]
";

                public static string ProjectSectionEnd =
@"	EndProjectSection
";

                public static string ProjectEnd =
@"EndProject
";

                public static string ProjectFolder =
@"Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""[folderName]"", ""[folderName]"", ""{[folderGuid]}""
";

                public static string HeaderEnd =
@"EndProject
";

                public static string GlobalBegin =
@"Global
";

                public static string GlobalEnd =
@"EndGlobal
";

                public static string SolutionProperties =
@"	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
";

                [Obsolete("This property is deprecated, scc info shouldn't be stored in the solution files anymore", error: true)]
                public static string GlobalSectionSolutionSourceCodeControlBegin;

                [Obsolete("This property is deprecated, scc info shouldn't be stored in the solution files anymore", error: true)]
                public static string GlobalSectionSolutionSourceCodeControlProject;

                [Obsolete("This property is deprecated, scc info shouldn't be stored in the solution files anymore", error: true)]
                public static string GlobalSectionSolutionSourceCodeControlEnd;

                public static string GlobalSectionSolutionConfigurationBegin =
@"	GlobalSection(SolutionConfigurationPlatforms) = preSolution
";
                public static string GlobalSectionSolutionConfiguration =
@"		[configurationName]|[category] = [configurationName]|[category]
";
                public static string GlobalSectionSolutionConfigurationEnd =
@"	EndGlobalSection
";

                public static string GlobalSectionProjectConfigurationBegin =
@"	GlobalSection(ProjectConfigurationPlatforms) = postSolution
";

                public static string GlobalSectionProjectConfigurationActive =
@"		{[projectGuid]}.[configurationName]|[category].ActiveCfg = [projectConf.Name]|[projectPlatform]
";
                public static string GlobalSectionProjectConfigurationBuild =
@"		{[projectGuid]}.[configurationName]|[category].Build.0 = [projectConf.Name]|[projectPlatform]
";
                public static string GlobalSectionProjectConfigurationDeploy =
@"		{[projectGuid]}.[configurationName]|[category].Deploy.0 = [projectConf.Name]|[projectPlatform]
";
                public static string GlobalSectionProjectConfigurationEnd =
@"	EndGlobalSection
";

                public static string NestedProjectBegin =
@"	GlobalSection(NestedProjects) = preSolution
";
                public static string NestedProjectItem =
@"		{[nestedChildGuid]} = {[nestedParentGuid]}
";
                public static string NestedProjectEnd =
@"	EndGlobalSection
";
            }
        }
    }
}
