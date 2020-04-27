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

namespace Sharpmake.Generators
{
    public interface IGenerationContext
    {
        Builder Builder { get; }
        Project Project { get; }
        Project.Configuration Configuration { get; }
        string ProjectDirectory { get; }
        DevEnv DevelopmentEnvironment { get; }

        Options.ExplicitOptions Options { get; }
        IDictionary<string, string> CommandLineOptions { get; }

        string ProjectDirectoryCapitalized { get; }
        string ProjectSourceCapitalized { get; }

        // If the output should not contain any variables (they should be resolved)
        bool PlainOutput { get; }

        void SelectOption(params Options.OptionAction[] options);
        void SelectOptionWithFallback(Action fallbackAction, params Options.OptionAction[] options);
    }
}
