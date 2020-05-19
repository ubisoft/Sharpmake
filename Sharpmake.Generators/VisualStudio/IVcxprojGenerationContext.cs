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
