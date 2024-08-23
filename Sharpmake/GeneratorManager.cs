// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Sharpmake
{
    public interface IProjectGenerator
    {
        void Generate(Builder builder,
                      Project project,
                      List<Project.Configuration> configurations,
                      string projectFile,
                      List<string> generatedFiles,
                      List<string> skipFiles);
    }

    public interface ISolutionGenerator
    {
        void Generate(Builder builder,
                      Solution solution,
                      List<Solution.Configuration> configurations,
                      string solutionFile,
                      List<string> generatedFiles,
                      List<string> skipFiles);
    }

    public interface IGeneratorManager : IProjectGenerator, ISolutionGenerator
    {
        void InitializeBuilder(Builder builder);

        void BeforeGenerate();
    }
}
