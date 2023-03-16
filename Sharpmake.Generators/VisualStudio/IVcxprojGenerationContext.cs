// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Sharpmake.Generators.VisualStudio
{
    public interface IVcxprojGenerationContext : IGenerationContext
    {
        string ProjectPath { get; }
        string ProjectFileName { get; }
        IReadOnlyList<Project.Configuration> ProjectConfigurations { get; }
        IReadOnlyDictionary<Project.Configuration, Options.ExplicitOptions> ProjectConfigurationOptions { get; }
        DevEnvRange DevelopmentEnvironmentsRange { get; }
        IReadOnlyDictionary<Platform, IPlatformVcxproj> PresentPlatforms { get; }

        Resolver EnvironmentVariableResolver { get; }
    }
}
