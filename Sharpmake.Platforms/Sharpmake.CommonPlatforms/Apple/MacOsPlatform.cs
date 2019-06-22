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

namespace Sharpmake
{
    public static class Apple
    {
        [PlatformImplementation(Platform.mac,
            typeof(IPlatformDescriptor),
            typeof(Project.Configuration.IConfigurationTasks))]
        public sealed class MacOsPlatform : IPlatformDescriptor, Project.Configuration.IConfigurationTasks
        {
            #region IPlatformDescriptor implementation.
            public string SimplePlatformString => "Mac";
            public bool IsMicrosoftPlatform => false;
            public bool IsPcPlatform => true;
            public bool IsUsingClang => true;
            public bool HasDotNetSupport => false; // maybe? (.NET Core)
            public bool HasSharedLibrarySupport => true;
            public bool HasPrecompiledHeaderSupport => true;

            public EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] variables)
            {
                return new EnvironmentVariableResolver(variables);
            }
            #endregion

            #region Project.Configuration.IConfigurationTasks implementation.
            public void SetupDynamicLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
            {
                DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
            }

            public void SetupStaticLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
            {
                DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
            }

            public string GetDefaultOutputExtension(Project.Configuration.OutputType outputType)
            {
                switch (outputType)
                {
                    // Using the Unix extensions since Darwin is a Unix implementation and the
                    // executables Mac users interact with are actually bundles. If this causes
                    // issues, see if using .app for executables and .dylib/.framework for
                    // libraries work better. iOS is Darwin/Cocoa so assuming that the same goes
                    // for it.
                    case Project.Configuration.OutputType.Exe:
                    case Project.Configuration.OutputType.IosApp:
                    case Project.Configuration.OutputType.IosTestBundle:
                        return string.Empty;
                    case Project.Configuration.OutputType.Lib:
                        return "a";
                    case Project.Configuration.OutputType.Dll:
                        return "so";

                    // .NET remains the same on all platforms. (Mono loads .exe and .dll regardless
                    // of platforms, and I assume the same about .NET Core.)
                    case Project.Configuration.OutputType.DotNetConsoleApp:
                    case Project.Configuration.OutputType.DotNetWindowsApp:
                        return "exe";
                    case Project.Configuration.OutputType.DotNetClassLibrary:
                        return "dll";

                    case Project.Configuration.OutputType.None:
                        return string.Empty;
                    default:
                        return outputType.ToString().ToLower();
                }
            }

            public string GetLibraryOutputPrefix()
            {
                return string.Empty;
            }

            public IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
            {
                yield break;
            }
            #endregion
        }
    }
}
