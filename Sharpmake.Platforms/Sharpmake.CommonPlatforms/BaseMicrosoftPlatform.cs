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
using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public abstract class BaseMicrosoftPlatform
        : BasePlatform
        , Project.Configuration.IConfigurationTasks
        , IMicrosoftPlatformBff
        , IPlatformVcxproj
    {
        #region IPlatformDescriptor implementation
        public override bool IsMicrosoftPlatform => true;
        public override bool IsUsingClang => false;
        public override bool HasDotNetSupport => true;
        public override bool HasSharedLibrarySupport => true;
        #endregion

        #region Project.Configuration.IConfigurationTasks implementation
        public void SetupLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
        }

        public string GetDefaultOutputExtension(Project.Configuration.OutputType outputType)
        {
            switch (outputType)
            {
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.DotNetConsoleApp:
                case Project.Configuration.OutputType.DotNetWindowsApp:
                    return "exe";
                case Project.Configuration.OutputType.Lib:
                    return "lib";
                case Project.Configuration.OutputType.Dll:
                case Project.Configuration.OutputType.DotNetClassLibrary:
                    return "dll";
                case Project.Configuration.OutputType.None:
                    return string.Empty;
                default:
                    return outputType.ToString().ToLower();
            }
        }

        public virtual IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
        {
            var dirs = new List<string>();
            var dotnet = Util.IsDotNet(configuration) ? configuration.Target.GetFragment<DotNetFramework>() : default(DotNetFramework?);
            string platformDirsStr = configuration.Target.GetFragment<DevEnv>().GetWindowsLibraryPath(configuration.Target.GetPlatform(), dotnet);
            dirs.AddRange(EnumerateSemiColonSeparatedString(platformDirsStr));

            return dirs;
        }
        #endregion

        #region IMicrosoftPlatformBff implementation
        public virtual bool SupportsResourceFiles => false;
        #endregion

        #region IPlatformVcxproj implementation
        public override string ExecutableFileExtension => "exe";
        public override string SharedLibraryFileExtension => "dll";
        public override string ProgramDatabaseFileExtension => "pdb";
        public override string StaticLibraryFileExtension => "lib";
        #endregion
    }
}
