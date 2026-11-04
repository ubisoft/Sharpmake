// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Sharpmake;

[module: Sharpmake.Include("Module.sharpmake.cs")]
[module: Sharpmake.Include("codebase/Engine/Plugins/Core/Core.sharpmake.cs")]
[module: Sharpmake.Include("codebase/Engine/Plugins/Foo/Foo.sharpmake.cs")]
[module: Sharpmake.Include("codebase/Engine/Plugins/Renderer/Renderer.sharpmake.cs")]

namespace CustomModules
{
    public static class Common
    {
        public static void Configure(Project.Configuration conf, Project project, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]_[target.Optimization]_[target.DevEnv]";
        }
    }

    // Embeds the Core, Foo and Renderer modules. It is the only project that compiles them:
    // projects that need them depend on it and inherit their include paths and public defines.
    [Sharpmake.Generate]
    public class EngineProject : ModuleProject
    {
        public EngineProject()
        {
            Name = "Engine";
            AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug, OutputType.Lib));
            SourceRootPath = "[project.SharpmakeCsPath]/codebase/Engine/Source";

            IsFileNameToLower = false;

            AddModule<FooModule>();
            AddModule<RendererModule>();
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            Common.Configure(conf, this, target);

            conf.Output = Configuration.OutputType.Lib;
            conf.IncludePaths.Add("[project.SourceRootPath]");
        }
    }

    // Compiles no module: it gets the include paths and public defines of Core, Foo and Renderer
    // by depending on EngineProject.
    [Sharpmake.Generate]
    public class GameplayProject : Project
    {
        public GameplayProject()
        {
            Name = "Gameplay";
            AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug, OutputType.Lib));
            SourceRootPath = "[project.SharpmakeCsPath]/codebase/Gameplay/Source";

            IsFileNameToLower = false;
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            Common.Configure(conf, this, target);

            conf.Output = Configuration.OutputType.Lib;
            conf.IncludePaths.Add("[project.SourceRootPath]");
            conf.AddPublicDependency<EngineProject>(target);
        }
    }

    [Sharpmake.Generate]
    public class GameProject : Project
    {
        public GameProject()
        {
            Name = "Game";
            AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug));
            SourceRootPath = "[project.SharpmakeCsPath]/codebase/Game/Source";

            IsFileNameToLower = false;
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            Common.Configure(conf, this, target);

            conf.AddPublicDependency<GameplayProject>(target);
        }
    }

    [Sharpmake.Generate]
    public class ModulesSolution : Sharpmake.Solution
    {
        public ModulesSolution()
        {
            Name = "CustomModules";
            AddTargets(new Target(Platform.win64, DevEnv.vs2022, Optimization.Debug));

            IsFileNameToLower = false;
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]_[target.Platform]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<GameProject>(target);
        }
    }

    // Sample-only check that the module setup is correct (a source compiled into two projects
    // breaks the one definition rule). Not meant for production: it regex-parses vcxproj files.
    public static class SamplePostgenerationVerifications
    {
        private static readonly Regex s_compileRegex = new Regex("<ClCompile Include=\"([^\"]+)\"", RegexOptions.Compiled);

        public static void Register(Builder builder)
        {
            builder.EventPostGeneration += CheckSourcesCompiledOnce;
        }

        private static void CheckSourcesCompiledOnce(List<Project> projects, List<Solution> solutions)
        {
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string vcxproj in GetVcxprojFiles(projects))
            {
                string projectName = Path.GetFileNameWithoutExtension(vcxproj);
                string projectDir = Path.GetDirectoryName(vcxproj);
                foreach (Match match in s_compileRegex.Matches(File.ReadAllText(vcxproj)))
                {
                    string source = Path.GetFullPath(Path.Combine(projectDir, match.Groups[1].Value));
                    string owner;
                    if (owners.TryGetValue(source, out owner) && owner != projectName)
                        throw new Error("{0} is compiled in both projects {1} and {2}: a module must be embedded in a single project.", source, owner, projectName);

                    owners[source] = projectName;
                }
            }
        }

        private static IEnumerable<string> GetVcxprojFiles(List<Project> projects)
        {
            var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Project project in projects)
            {
                foreach (Project.Configuration conf in project.Configurations)
                {
                    string file = conf.ProjectFullFileName + ".vcxproj";
                    if (File.Exists(file))
                        files.Add(file);
                }
            }

            return files;
        }
    }

    public static class Main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            KitsRootPaths.SetUseKitsRootForDevEnv(DevEnv.vs2022, KitsRootEnum.KitsRoot10, Options.Vc.General.WindowsTargetPlatformVersion.v10_0_26100_0);

            SamplePostgenerationVerifications.Register(arguments.Builder);
            arguments.Generate<ModulesSolution>();
        }
    }
}
