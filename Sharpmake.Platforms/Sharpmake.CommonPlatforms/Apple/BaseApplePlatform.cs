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
using System.IO;
using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;

namespace Sharpmake
{
    public abstract partial class BaseApplePlatform
        : IPlatformDescriptor, Project.Configuration.IConfigurationTasks
    {
        #region IPlatformDescriptor
        public abstract string SimplePlatformString { get; }
        public bool IsMicrosoftPlatform => false;
        public bool IsUsingClang => true;
        public bool HasDotNetSupport => false; // maybe? (.NET Core)
        public bool HasSharedLibrarySupport => true;
        public bool HasPrecompiledHeaderSupport => true;

        public bool IsPcPlatform => false; // LCTODO: ditch since it's obsolete

        public EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] variables)
        {
            return new EnvironmentVariableResolver(variables);
        }

        public string GetPlatformString(ITarget target) => SimplePlatformString;
        #endregion

        #region IConfigurationTasks
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

        public IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
        {
            yield break;
        }

        public void SetupDynamicLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            // There's no implib on apple platforms, the dynlib does both
            if (dependency.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Export &&
                !(configuration.IsFastBuild && !dependency.IsFastBuild))
            {
                if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                    configuration.AddDependencyBuiltTargetLibraryPath(dependency.TargetPath, dependency.TargetLibraryPathOrderNumber);
                if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                    configuration.AddDependencyBuiltTargetLibraryFile(dependency.TargetFileFullName, dependency.TargetFileOrderNumber);
            }
            else
            {
                if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                    configuration.DependenciesOtherLibraryPaths.Add(dependency.TargetPath, dependency.TargetLibraryPathOrderNumber);
                if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                    configuration.DependenciesOtherLibraryFiles.Add(dependency.TargetFileFullName, dependency.TargetFileOrderNumber);
            }
        }

        public void SetupStaticLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
        }
        #endregion
    }
}
