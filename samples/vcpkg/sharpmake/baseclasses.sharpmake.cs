// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace VCPKGSample
{
    /// <summary>
    /// This class is used to define the targets fragments used everywhere in this sample
    /// </summary>
    public class SampleTargets
    {
        public static Target[] Targets
        {
            get
            {
                return new Target[]{ new Target(
                    Platform.win64,
                    DevEnv.vs2019  | DevEnv.vs2022,
                    Optimization.Debug | Optimization.Release,
                    buildSystem: BuildSystem.MSBuild | BuildSystem.FastBuild)};
            }
        }
    }

    /// <summary>
    /// This is the base class for projects with the Sharpmake.Export attribute.
    ///
    /// </summary>
    /// <remarks>
    /// ConfigurePriority() attribute on configure methods is to force the order of the calls. Lower numbers have highest priority
    /// </remarks>
    public class ExportProject : Project
    {
        public ExportProject()
        {
            AddTargets(SampleTargets.Targets);
        }

        [Configure(), ConfigurePriority(1)]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
        }

        [Configure(Optimization.Debug), ConfigurePriority(2)]
        public virtual void ConfigureDebug(Configuration conf, Target target)
        {
        }

        [Configure(Optimization.Release), ConfigurePriority(3)]
        public virtual void ConfigureRelease(Configuration conf, Target target)
        {
        }
    }

    /// <summary>
    /// This is the base class for sharpmake project needing compilation.
    /// </summary>
    [Sharpmake.Generate]
    public class CommonProject : Project
    {
        public string TmpPath = @"[project.SharpmakeCsPath]\..\tmp";
        public string OutputPath = @"[project.SharpmakeCsPath]\..\bin";
        public string ExternPath = @"[project.SharpmakeCsPath]\..\extern";

        public CommonProject()
        {
            AddTargets(SampleTargets.Targets);

            SourceRootPath = @"[project.SharpmakeCsPath]\..\src\[project.Name]";
        }


        [Configure(), ConfigurePriority(1)]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]";
            conf.ProjectPath = @"[project.TmpPath]\projects\[project.Name]";
            conf.TargetPath = @"[project.OutputPath]\[target.Optimization]-[target.BuildSystem]";
            conf.IntermediatePath = @"[project.TmpPath]\[project.Name]\[target.Optimization]-[target.BuildSystem]";
            if ((target.BuildSystem & BuildSystem.FastBuild) != 0)
                conf.Name += "_FastBuild";

            conf.Options.Add(Sharpmake.Options.Vc.Compiler.CppLanguageStandard.CPP17);
            conf.Options.Add(Sharpmake.Options.Vc.Compiler.ConformanceMode.Enable);
            conf.AdditionalCompilerOptions.Add("/Zo");
            conf.LinkerPdbSuffix = string.Empty;

            conf.Options.Add(new Sharpmake.Options.Vc.Librarian.DisableSpecificWarnings(
                "4221" // This object file does not define any previously undefined public symbols, so it will not be used by any link operation that consumes this library
                ));

            conf.Options.Add(new Sharpmake.Options.Vc.Compiler.DisableSpecificWarnings(
                "4100", // unreferenced formal parameter
                "4324"  // structure was padded due to alignment specifier
                ));

            conf.Options.Add(Sharpmake.Options.Vc.Linker.GenerateMapFile.Disable);

            conf.Defines.Add("WIN32_LEAN_AND_MEAN");
        }

        [Configure(Optimization.Debug), ConfigurePriority(2)]
        public virtual void ConfigureDebug(Configuration conf, Target target)
        {
            conf.Options.Add(Sharpmake.Options.Vc.Compiler.Inline.Disable);
        }

        [Configure(Optimization.Release), ConfigurePriority(3)]
        public virtual void ConfigureRelease(Configuration conf, Target target)
        {
            conf.Options.Add(Sharpmake.Options.Vc.Compiler.Optimization.MaximizeSpeed);
            conf.Options.Add(Sharpmake.Options.Vc.General.WholeProgramOptimization.LinkTime);
            conf.Options.Add(Sharpmake.Options.Vc.Linker.LinkTimeCodeGeneration.UseLinkTimeCodeGeneration);
            conf.Options.Add(Sharpmake.Options.Vc.Linker.EnableCOMDATFolding.RemoveRedundantCOMDATs);
            conf.Options.Add(Sharpmake.Options.Vc.Linker.Reference.EliminateUnreferencedData);
        }

        [Configure(BuildSystem.FastBuild)]
        public void ConfigureFastBuild(Configuration conf, Target target)
        {
            conf.IsFastBuild = true;
            conf.FastBuildUnityPath = @"[project.TmpPath]\projects\[project.Name]\Unity";
        }
    }
}
