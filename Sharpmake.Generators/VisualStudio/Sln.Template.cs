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
    public partial class Sln
    {
        public static class Template
        {
            public static class Solution
            {
                public static string HeaderBeginVs2010 =
@"Microsoft Visual Studio Solution File, Format Version 11.00
# Visual Studio 2010
";

                public static string HeaderBeginVs2012 =
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2012
";

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

                public static string GlobalSectionSolutionSourceCodeControlBegin =
@"	GlobalSection(SourceCodeControl) = preSolution
		SccNumberOfProjects = [sccNumberOfProjects]
";

                public static string GlobalSectionSolutionSourceCodeControlProject =
@"		SccProjectUniqueName[i] = [sccProjectUniqueName]
		SccProjectTopLevelParentUniqueName[i] = [sccProjectTopLevelParentUniqueName]
		SccProjectName[i] = Perforce\u0020Project
		SccLocalPath[i] = [sccLocalPath]
		SccProvider[i] = MSSCCI:Perforce\u0020SCM
		SccProjectFilePathRelativizedFromConnection[i] = [sccProjectFilePathRelativizedFromConnection]\\
";

                public static string GlobalSectionSolutionSourceCodeControlEnd =
@"	EndGlobalSection
";

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
