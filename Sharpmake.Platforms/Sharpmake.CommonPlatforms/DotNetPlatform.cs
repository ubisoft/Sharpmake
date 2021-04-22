// Copyright (c) 2017, 2021 Ubisoft Entertainment
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

namespace Sharpmake
{
    [PlatformImplementation(Platform.anycpu,
        typeof(IPlatformDescriptor),
        typeof(Project.Configuration.IConfigurationTasks))]
    public sealed class DotNetPlatform : BasePlatform, Project.Configuration.IConfigurationTasks
    {
        #region IPlatformDescriptor implementation
        public override string SimplePlatformString => "Any CPU";
        public override bool IsMicrosoftPlatform => true;
        public override bool IsPcPlatform => true;
        public override bool IsUsingClang => false;
        public override bool HasDotNetSupport => true;
        public override bool HasSharedLibrarySupport => true;
        #endregion

        #region IConfigurationTasks
        public override string SharedLibraryFileExtension => ".dll";
        public override string ProgramDatabaseFileExtension => ".pdb";
        public override string ExecutableFileExtension => ".exe";

        public string GetDefaultOutputExtension(Project.Configuration.OutputType outputType)
        {
            switch (outputType)
            {
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.DotNetConsoleApp:
                case Project.Configuration.OutputType.DotNetWindowsApp:
                    return ExecutableFileExtension;
                case Project.Configuration.OutputType.Lib:
                case Project.Configuration.OutputType.Dll:
                case Project.Configuration.OutputType.DotNetClassLibrary:
                    return SharedLibraryFileExtension;
                case Project.Configuration.OutputType.None:
                    return string.Empty;
                default:
                    throw new NotImplementedException("Please add extension for output type " + outputType);
            }
        }

        public string GetOutputFileNamePrefix(Project.Configuration.OutputType outputType)
        {
            return string.Empty;
        }

        public IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
        {
            yield break;
        }

        public void SetupDynamicLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
        }

        public void SetupStaticLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
        }
        #endregion
    }
}
