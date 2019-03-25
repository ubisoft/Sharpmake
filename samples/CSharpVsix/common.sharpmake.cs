// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Sharpmake;

namespace Common
{
    public class Common
    {
        public static string GetDevEnvString(DevEnv env)
        {
            switch (env)
            {
                case DevEnv.vs2015: return "2015";
                default: return "";
            }
        }

        public static Target[] GetDefaultTargets()
        {
            return new Target[]{
            new Target(
            Platform.anycpu,
            DevEnv.vs2015,
            Optimization.Debug | Optimization.Release,
            OutputType.Dll,
            Blob.NoBlob,
            BuildSystem.MSBuild,
            DotNetFramework.v4_5)};
        }
    }

    [Sharpmake.Generate]
    public class ProjectTemplate : CSharpProject
    {
        public ProjectTemplate()
        {
            RootPath = @"[project.SharpmakeCsPath]\codebase\";

            // This Path will be used to get all SourceFiles in this Folder and all subFolders
            SourceRootPath = @"[project.RootPath]\[project.Name]";

            AddTargets(Common.GetDefaultTargets());

            // if set to true, dependencies that the project uses will be copied to the output directory
            DependenciesCopyLocal = DependenciesCopyLocalTypes.Default;

            // Set to null if you don't want to use Perforce
            PerforceRootPath = null;

            // Files put in this directory will be added to the project as resources (linked) build Action
            ResourcesPath = RootPath + @"\Resources\";


            // Files put in this directory will be added to the project as Content build Action
            ContentPath = RootPath + @"\Content\";

            //Specify if we want the project file to be LowerCase
            IsFileNameToLower = false;
        }
    }

    [Sharpmake.Generate]
    public class SolutionTemplate : CSharpSolution
    {
        public SolutionTemplate()
        {
            AddTargets(Common.GetDefaultTargets());
        }
        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = string.Format("{0}.{1}.{2}",
                Name,
                Common.GetDevEnvString(target.DevEnv),
                target.Framework.ToVersionString());
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\codebase\";
        }
    }
}