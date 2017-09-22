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
namespace Sharpmake
{
    public static partial class Linux
    {
        [PlatformImplementation(Platform.linux,
            typeof(IPlatformDescriptor),
            typeof(Project.Configuration.IConfigurationTasks))]
        public class LinuxPlatform : BasePlatform, Project.Configuration.IConfigurationTasks
        {
            #region IPlatformDescriptor implementation
            public override string SimplePlatformString => "Linux";
            public override bool IsMicrosoftPlatform => false; // No way!
            public override bool IsPcPlatform => true;
            public override bool IsUsingClang => false; // Maybe now? Traditionally GCC but only the GNU project is backing it now.
            public override bool HasDotNetSupport => false; // Technically false with .NET Core and Mono.
            public override bool HasSharedLibrarySupport => false; // Was not specified in sharpmake (probably because it was not implemented), but this is obviously wrong.
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
                        return string.Empty;
                    case Project.Configuration.OutputType.Dll:
                        return "so";
                    default:
                        return "a";
                }
            }
            #endregion

            #region IPlatformVcxproj implementation
            public override string ProgramDatabaseFileExtension => string.Empty;
            public override string SharedLibraryFileExtension => "so";
            public override string ExecutableFileExtension => string.Empty;
            #endregion
        }
    }
}
