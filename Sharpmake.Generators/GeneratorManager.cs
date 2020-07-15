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
using System.Collections.Generic;
using Sharpmake.Generators.Apple;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.Generic;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators
{
    public class GeneratorManager : IGeneratorManager
    {
        #region Generators
        // Solution generators
        private Sln _slnGenerator = null;
        public Sln SlnGenerator => _slnGenerator ?? (_slnGenerator = new Sln());

        private MasterBff _masterBffGenerator = null;
        public MasterBff MasterBffGenerator => _masterBffGenerator ?? (_masterBffGenerator = new MasterBff());

        private XCWorkspace _xcodeWorkspaceGenerator = null;
        public XCWorkspace XCWorkspaceGenerator => _xcodeWorkspaceGenerator ?? (_xcodeWorkspaceGenerator = new XCWorkspace());

        private MakeApplication _makeApplicationGenerator = null;
        public MakeApplication MakeApplicationGenerator => _makeApplicationGenerator ?? (_makeApplicationGenerator = new MakeApplication());

        // Project generators
        private CSproj _csprojGenerator = null;
        public CSproj CsprojGenerator => _csprojGenerator ?? (_csprojGenerator = new CSproj());

        private Pyproj _pyprojGenerator = null;
        public Pyproj PyprojGenerator => _pyprojGenerator ?? (_pyprojGenerator = new Pyproj());

        private Vcxproj _vcxprojGenerator = null;
        public Vcxproj VcxprojGenerator => _vcxprojGenerator ?? (_vcxprojGenerator = new Vcxproj());

        private Bff _bffGenerator = null;
        public Bff BffGenerator => _bffGenerator ?? (_bffGenerator = new Bff());

        private XCodeProj _xcodeProjectGenerator = null;
        public XCodeProj XCodeProjectGenerator => _xcodeProjectGenerator ?? (_xcodeProjectGenerator = new XCodeProj());

        private MakeProject _makeProjectGenerator = null;
        public MakeProject MakeProjectGenerator => _makeProjectGenerator ?? (_makeProjectGenerator = new MakeProject());

        private Makefile _makefileGenerator = null;
        public Makefile MakefileGenerator => _makefileGenerator ?? (_makefileGenerator = new Makefile());
        #endregion

        // singleton
        private Androidproj _AndroidprojGenerator = null;
        public Androidproj AndroidprojGenerator => _AndroidprojGenerator ?? (_AndroidprojGenerator = new Androidproj());

        public void InitializeBuilder(Builder builder)
        {
            Bff.InitializeBuilder(builder);
        }

        public void Generate(Builder builder,
                             Project project,
                             List<Project.Configuration> configurations,
                             string projectFile,
                             List<string> generatedFiles,
                             List<string> skipFiles)
        {
            if (project is CSharpProject)
            {
                CsprojGenerator.Generate(builder, project, configurations, projectFile, generatedFiles, skipFiles);
            }
            else if (project is PythonProject)
            {
                PyprojGenerator.Generate(builder, project, configurations, projectFile, generatedFiles, skipFiles);
            }
            else if (project is AndroidPackageProject)
            {
                AndroidprojGenerator.Generate(builder, project, configurations, projectFile, generatedFiles, skipFiles);
            }
            else
            {
                switch (configurations[0].Target.GetFragment<DevEnv>())
                {
                    case DevEnv.make:
                        {
                            if (configurations[0].Platform == Platform.android)
                                MakeProjectGenerator.Generate(builder, project, configurations, projectFile, generatedFiles, skipFiles);
                            else
                                MakefileGenerator.Generate(builder, project, configurations, projectFile, generatedFiles, skipFiles);
                            break;
                        }
                    case DevEnv.vs2010:
                    case DevEnv.vs2012:
                    case DevEnv.vs2013:
                    case DevEnv.vs2015:
                    case DevEnv.vs2017:
                    case DevEnv.vs2019:
                        {
                            VcxprojGenerator.Generate(builder, project, configurations, projectFile, generatedFiles, skipFiles);
                            BffGenerator.Generate(builder, project, configurations, projectFile, generatedFiles, skipFiles);
                            break;
                        }
                    case DevEnv.xcode4ios:
                        {
                            XCodeProjectGenerator.Generate(builder, project, configurations, projectFile, generatedFiles, skipFiles);
                            BffGenerator.Generate(builder, project, configurations, projectFile, generatedFiles, skipFiles);
                            break;
                        }
                    default:
                        {
                            throw new Error("Generate called with unknown DevEnv: " + configurations[0].Target.GetFragment<DevEnv>());
                        }
                }
            }
        }

        public void Generate(Builder builder,
                             Solution solution,
                             List<Solution.Configuration> configurations,
                             string solutionFile,
                             List<string> generatedFiles,
                             List<string> skipFiles)
        {
            if (configurations[0].Platform == Platform.ios || configurations[0].Platform == Platform.mac)
            {
                XCWorkspaceGenerator.Generate(builder, solution, configurations, solutionFile, generatedFiles, skipFiles);
                if (UtilityMethods.HasFastBuildConfig(configurations))
                {
                    MasterBffGenerator.Generate(builder, solution, configurations, solutionFile, generatedFiles, skipFiles);
                }
            }
            else
            {
                switch (configurations[0].Target.GetFragment<DevEnv>())
                {
                    case DevEnv.make:
                        {
                            if (configurations[0].Platform == Platform.android)
                                MakeApplicationGenerator.Generate(builder, solution, configurations, solutionFile, generatedFiles, skipFiles);
                            else
                                MakefileGenerator.Generate(builder, solution, configurations, solutionFile, generatedFiles, skipFiles);
                            break;
                        }
                    case DevEnv.vs2010:
                    case DevEnv.vs2012:
                    case DevEnv.vs2013:
                    case DevEnv.vs2015:
                    case DevEnv.vs2017:
                    case DevEnv.vs2019:
                        {
                            if (UtilityMethods.HasFastBuildConfig(configurations))
                            {
                                MasterBffGenerator.Generate(builder, solution, configurations, solutionFile, generatedFiles, skipFiles);
                            }

                            SlnGenerator.Generate(builder, solution, configurations, solutionFile, generatedFiles, skipFiles);
                            break;
                        }
                    default:
                        {
                            throw new Error("Generate called with unknown DevEnv: " + configurations[0].Target.GetFragment<DevEnv>());
                        }
                }
            }
        }
    }
}
