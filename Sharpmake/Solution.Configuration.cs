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
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Sharpmake
{
    public partial class Solution
    {
        [Resolver.Resolvable]
        public class Configuration : Sharpmake.Configuration
        {
            private static int s_count = 0;
            public static int Count { get { return s_count; } }

            public Configuration()
            {
                System.Threading.Interlocked.Increment(ref s_count);
            }

            public Solution Solution { get { return Owner as Solution; } }

            public string Name = "[target.Name]";
            public string SolutionFileName = "[solution.Name]";             // File name for the generated solution without extension, ex: "MySolution"
            public string SolutionPath = "[solution.SharpmakeCsPath]";      // Path of SolutionFileName

            // Can be set to customize solution platform name
            private string _platformName = null;
            public string PlatformName
            {
                get
                {
                    return string.IsNullOrEmpty(_platformName) ? Util.GetSimplePlatformString(Platform) : _platformName;
                }
                set
                {
                    _platformName = value;
                }
            }

            public bool IncludeOnlyFilterProject = false;

            public string CompileCommandLine = String.Empty;

            public void AddProject<TPROJECTTYPE>(
                ITarget projectTarget,
                bool inactiveProject = false,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddProject(typeof(TPROJECTTYPE), projectTarget, inactiveProject, Util.FormatCallerInfo(sourceFilePath, sourceLineNumber));
            }

            public void AddProject(
                Type projectType,
                ITarget projectTarget,
                bool inactiveProject = false,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddProject(projectType, projectTarget, inactiveProject, Util.FormatCallerInfo(sourceFilePath, sourceLineNumber));
            }

            public class IncludedProjectInfo
            {
                // Type of the project, need this to resolve Project instance
                public Type Type;

                // Project
                public Project Project;

                // Project associated configuration
                public Project.Configuration Configuration;

                // Target of the project, need to resolve the Configuration
                public ITarget Target;

                public override string ToString()
                {
                    return String.Format("{0} {1}", Project, Target);
                }

                // either or not to compile this project in the solution
                // if false; project is added to the solution but not compiled (not included in the project dependencies).
                public bool InactiveProject;

                // resolved state, whether this project configuration is built in the solution
                internal enum Build
                {
                    Unknown,
                    No,
                    Yes,
                    YesThroughDependency
                };
                internal Build ToBuild = Build.Unknown;
            }

            //Holds the reference to the startup project. When the project will be resolved, 
            //his full name path will be resolved too which allows us to point to the right project.
            public IncludedProjectInfo StartupProject { get; set; }

            /// <summary>
            /// Holds the path references to projects that should be added in the solution
            /// </summary>
            public Strings ProjectReferencesByPath { get; } = new Strings();

            public List<IncludedProjectInfo> IncludedProjectInfos = new List<IncludedProjectInfo>();

            public IncludedProjectInfo GetProject(Type projectType)
            {
                foreach (IncludedProjectInfo includedProjectInfo in IncludedProjectInfos)
                {
                    if (includedProjectInfo.Type == projectType)
                        return includedProjectInfo;
                }
                return null;
            }

            private void AddProject(Type projectType, ITarget projectTarget, bool inactiveProject, string callerInfo)
            {
                IncludedProjectInfo includedProjectInfo = GetProject(projectType);

                if (includedProjectInfo == null)
                {
                    IncludedProjectInfos.Add(
                        new IncludedProjectInfo
                        {
                            Type = projectType,
                            Target = projectTarget,
                            InactiveProject = inactiveProject
                        }
                    );
                }
                else
                {
                    if (!includedProjectInfo.Target.IsEqualTo(projectTarget))
                    {
                        throw new Error(callerInfo + "error : cannot add twice the project({0}) in the same solution({1}) configuration({2}) using differents project configuration: ({3}) and ({4})",
                            includedProjectInfo.Type.Name,
                            Solution.Name,
                            Target,
                            includedProjectInfo.Target,
                            projectTarget);
                    }
                }
            }

            internal void Resolve(Resolver resolver)
            {
                resolver.SetParameter("conf", this);
                resolver.SetParameter("target", Target);
                resolver.Resolve(this);
                resolver.RemoveParameter("conf");
                resolver.RemoveParameter("target");

                Util.ResolvePath(Solution.SharpmakeCsPath, ref SolutionPath);
                if (Solution.IsFileNameToLower)
                    SolutionFileName = SolutionFileName.ToLower();
            }
        }
    }
}
