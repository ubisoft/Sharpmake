// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
        public override bool IsLinkerInvokedViaCompiler { get; set; } = false;
        public override bool HasDotNetSupport => true;
        public override bool HasSharedLibrarySupport => true;
        #endregion

        #region IConfigurationTasks
        public override string SharedLibraryFileFullExtension => ".dll";
        public override string ProgramDatabaseFileFullExtension => ".pdb";
        public override string ExecutableFileFullExtension => ".exe";

        public string GetDefaultOutputFullExtension(Project.Configuration.OutputType outputType)
        {
            switch (outputType)
            {
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.DotNetConsoleApp:
                case Project.Configuration.OutputType.DotNetWindowsApp:
                    return ExecutableFileFullExtension;
                case Project.Configuration.OutputType.Lib:
                case Project.Configuration.OutputType.Dll:
                case Project.Configuration.OutputType.DotNetClassLibrary:
                    return SharedLibraryFileFullExtension;
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
