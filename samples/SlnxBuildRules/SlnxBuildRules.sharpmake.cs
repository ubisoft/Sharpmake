// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace SlnxBuildRules
{
    // Header-only library with Output=None. Pulled in as an OnlyBuildOrder dependency by
    // RulesExe, which populates GenericBuildDependencies and triggers a slnx <BuildDependency>.
    [Sharpmake.Generate]
    public class HeaderOnlyLib : Project
    {
        public HeaderOnlyLib()
        {
            Name = "HeaderOnlyLib";
            SourceRootPath = @"[project.SharpmakeCsPath]/headeronly";
            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2026,
                Optimization.Debug | Optimization.Release));

            IsFileNameToLower = false;
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]_[target.Optimization]_[target.DevEnv]";
            conf.Output = Configuration.OutputType.None;
            conf.IncludePaths.Add("[project.SourceRootPath]");
        }
    }

    [Sharpmake.Generate]
    public class RulesLib : Project
    {
        public RulesLib()
        {
            Name = "RulesLib";
            SourceRootPath = @"[project.SharpmakeCsPath]/lib";
            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2026,
                Optimization.Debug | Optimization.Release));

            IsFileNameToLower = false;
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]_[target.Optimization]_[target.DevEnv]";
            conf.Output = Configuration.OutputType.Lib;
        }
    }

    [Sharpmake.Generate]
    public class RulesExe : Project
    {
        public RulesExe()
        {
            Name = "RulesExe";
            SourceRootPath = @"[project.SharpmakeCsPath]/exe";
            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2026,
                Optimization.Debug | Optimization.Release));

            IsFileNameToLower = false;
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]_[target.Optimization]_[target.DevEnv]";
            conf.Output = Configuration.OutputType.Exe;

            // Build-order-only dependency on a None-output project -> slnx <BuildDependency>.
            conf.AddPrivateDependency<HeaderOnlyLib>(target, DependencySetting.OnlyBuildOrder);

            // NOTE: RulesLib is intentionally NOT a dependency of RulesExe. If it were, marking it
            // inactiveProject in the solution would be overridden by Build.YesThroughDependency and
            // the <Build Project="false" /> rule would not be emitted.
        }

        [Configure(Optimization.Release)]
        public void ConfigureRelease(Configuration conf, Target target)
        {
            // Remap the project config name so it differs from the solution config name ("Release")
            // -> slnx <BuildType Solution="Release|x64" Project="Retail" />.
            conf.Name = "Retail";
        }
    }

    [Sharpmake.Generate]
    public class SlnxBuildRulesSolution : Solution
    {
        public SlnxBuildRulesSolution()
        {
            Name = "SlnxBuildRules";
            Format = SolutionFormat.Xml; // opt into .slnx
            AddTargets(new Target(
                Platform.win64,
                DevEnv.vs2026,
                Optimization.Debug | Optimization.Release));

            IsFileNameToLower = false;
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<RulesExe>(target);
        }

        [Configure(Optimization.Debug)]
        public void ConfigureDebug(Configuration conf, Target target)
        {
            // Built normally in Debug.
            conf.AddProject<RulesLib>(target);
        }

        [Configure(Optimization.Release)]
        public void ConfigureRelease(Configuration conf, Target target)
        {
            // Excluded from build in Release only -> slnx <Build ... Project="false" /> in that scope.
            conf.AddProject<RulesLib>(target, inactiveProject: true);
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Arguments arguments)
        {
            arguments.Generate<HeaderOnlyLib>();
            arguments.Generate<RulesLib>();
            arguments.Generate<RulesExe>();
            arguments.Generate<SlnxBuildRulesSolution>();
        }
    }
}
