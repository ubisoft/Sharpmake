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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Sharpmake
{
    public partial class Solution
    {
        [Resolver.Resolvable]
        [DebuggerDisplay("{SolutionFileName}:{Name}:{PlatformName}")]
        public class Configuration : Sharpmake.Configuration
        {
            /// <summary>
            /// Gets the number of <see cref="Configuration"/> instances created so far during
            /// Sharpmake's execution.
            /// </summary>
            private static int s_count = 0;
            public static int Count => s_count;

            /// <summary>
            /// Creates a new <see cref="Configuration"/> instance.
            /// </summary>
            public Configuration()
            {
                System.Threading.Interlocked.Increment(ref s_count);
            }

            /// <summary>
            /// Gets the <see cref="Solution"/> instance that owns this configuration.
            /// </summary>
            public Solution Solution => Owner as Solution;

            /// <summary>
            /// Name of this solution configuration.
            /// </summary>
            /// <remarks>
            /// This name will be displayed in Visual Studio's configuration drop down list. (Or
            /// other development tools that support multiple configuration per workspace.)
            /// </remarks>
            public string Name = "[target.Name]";

            /// <summary>
            /// File name (without extension) of the solution that this
            /// configuration must be written into.
            /// </summary>
            public string SolutionFileName = "[solution.Name]";

            /// <summary>
            /// Directory of the solution that this configuration must be written into.
            /// </summary>
            public string SolutionPath = "[solution.SharpmakeCsPath]";

            /// <summary>
            /// Gets the file name (without extension) of the solution that this configuration must
            /// be written info.
            /// </summary>
            public string SolutionFilePath => Path.Combine(SolutionPath, SolutionFileName);

            /// <summary>
            /// File name (without extension) of the master BFF for this solution configuration.
            /// </summary>
            public string MasterBffFileName = "[conf.SolutionFileName]";

            /// <summary>
            /// Directory of the master BFF for this solution configuration.
            /// </summary>
            public string MasterBffDirectory = "[conf.SolutionPath]";

            /// <summary>
            /// Gets the file path (without extension) of the master BFF for this solution
            /// configuration.
            /// </summary>
            public string MasterBffFilePath => Path.Combine(MasterBffDirectory, MasterBffFileName);

            // Can be set to customize solution platform name
            private string _platformName = null;
            public string PlatformName
            {
                get
                {
                    return string.IsNullOrEmpty(_platformName) ? Util.GetPlatformString(Platform, null, Target) : _platformName;
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
                string solutionFolder = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddProjectInternal(typeof(TPROJECTTYPE), projectTarget, inactiveProject, solutionFolder, Util.FormatCallerInfo(sourceFilePath, sourceLineNumber));
            }

            public void AddProject(
                Type projectType,
                ITarget projectTarget,
                bool inactiveProject = false,
                string solutionFolder = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddProjectInternal(projectType, projectTarget, inactiveProject, solutionFolder, Util.FormatCallerInfo(sourceFilePath, sourceLineNumber));
            }

            public void SetStartupProject<TPROJECTTYPE>(
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                IncludedProjectInfo includedProjectInfo = GetProject(typeof(TPROJECTTYPE));

                if (includedProjectInfo == null)
                {
                    throw new Error(string.Format("{0} error : Can't set project {1} as startup project of solution {2} and target {3} since it is not included in the configuration.",
                        Util.FormatCallerInfo(sourceFilePath, sourceLineNumber),
                        typeof(TPROJECTTYPE).Name,
                        Solution.Name,
                        Target));
                }

                StartupProject = includedProjectInfo;
            }

            [DebuggerDisplay("{Project == null ? Type.Name : Project.Name} {Configuration == null ? Target.Name : Configuration.Name}")]
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

                /// <summary>
                /// The solution folder to use for the project in this solution. It overrides <see cref="Sharpmake.Project.Configuration.SolutionFolder"/>
                /// </summary>
                public string SolutionFolder;

                public override string ToString()
                {
                    return String.Format("{0} {1}", Project, Target);
                }

                // either or not to compile this project in the solution
                // if false; project is added to the solution but not compiled (not included in the project dependencies).
                public bool InactiveProject;

                // resolved state, whether this project configuration is built in the solution
                public enum Build
                {
                    Unknown,
                    No,
                    Yes,
                    YesThroughDependency
                };

                public Build ToBuild { get; internal set; } = Build.Unknown;
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

            public IncludedProjectInfo GetProject<TPROJECTTYPE>()
            {
                return GetProject(typeof(TPROJECTTYPE));
            }

            private void AddProjectInternal(Type projectType, ITarget projectTarget, bool inactiveProject, string solutionFolder, string callerInfo)
            {
                IncludedProjectInfo includedProjectInfo = GetProject(projectType);

                if (includedProjectInfo == null)
                {
                    IncludedProjectInfos.Add(
                        new IncludedProjectInfo
                        {
                            Type = projectType,
                            Target = projectTarget,
                            InactiveProject = inactiveProject,
                            SolutionFolder = solutionFolder
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
                Util.ResolvePath(Solution.SharpmakeCsPath, ref MasterBffDirectory);
                if (Solution.IsFileNameToLower)
                {
                    SolutionFileName = SolutionFileName.ToLower();
                    MasterBffFileName = MasterBffFileName.ToLower();
                }
            }
        }
    }
}
