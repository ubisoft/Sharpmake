// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
