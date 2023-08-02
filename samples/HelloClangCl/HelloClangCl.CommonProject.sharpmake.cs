// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using Sharpmake;

namespace HelloClangCl
{
    public static class ConfigurePriorities
    {
        public const int All = -75;
        public const int Platform = -50;
        public const int Optimization = -25;
        /*     SHARPMAKE DEFAULT IS 0     */
        public const int Blobbing = 25;
        public const int BuildSystem = 50;
        public const int Compiler = 75;
    }

    public abstract class CommonProject : Sharpmake.Project
    {
        protected CommonProject()
            : base(typeof(CommonTarget))
        {
            RootPath = Globals.RootDirectory;
            IsFileNameToLower = false;
            IsTargetFileNameToLower = false;

            SourceRootPath = @"[project.RootPath]\[project.Name]";
        }

        [ConfigurePriority(ConfigurePriorities.All)]
        [Configure]
        public virtual void ConfigureAll(Configuration conf, CommonTarget target)
        {
            conf.ProjectFileName = "[project.Name]_[target.Platform]";
            if (target.DevEnv != DevEnv.xcode)
                conf.ProjectFileName += "_[target.DevEnv]";
            conf.ProjectPath = Path.Combine(Globals.TmpDirectory, @"projects\[project.Name]");
            conf.IsFastBuild = target.BuildSystem == BuildSystem.FastBuild;

            conf.IntermediatePath = Path.Combine(Globals.TmpDirectory, @"obj\[target.DirectoryName]\[project.Name]");
            conf.TargetPath = Path.Combine(Globals.OutputDirectory, "[target.DirectoryName]");

            // Note: uncomment the following line if we port this sample to windows
            //conf.TargetLibraryPath = conf.IntermediatePath; // .lib files must be with the .obj files when running in fastbuild distributed mode or we'll have missing symbols due to merging of the .pdb
            conf.TargetLibraryPath = Path.Combine(Globals.TmpDirectory, @"lib\[target.DirectoryName]\[project.Name]");

            conf.Output = Configuration.OutputType.Lib; // defaults to creating static libs

            conf.Options.Add(Options.Vc.Compiler.CppLanguageStandard.CPP17);
        }

        ////////////////////////////////////////////////////////////////////////
        #region Platfoms
        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.win64)]
        public virtual void ConfigureWin64(Configuration conf, CommonTarget target)
        {
            conf.Defines.Add("_HAS_EXCEPTIONS=0");
        }
        #endregion
        ////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////
        #region Optimizations
        [ConfigurePriority(ConfigurePriorities.Optimization)]
        [Configure(Optimization.Debug)]
        public virtual void ConfigureDebug(Configuration conf, CommonTarget target)
        {
            conf.DefaultOption = Options.DefaultTarget.Debug;
        }

        [ConfigurePriority(ConfigurePriorities.Optimization)]
        [Configure(Optimization.Release)]
        public virtual void ConfigureRelease(Configuration conf, CommonTarget target)
        {
            conf.DefaultOption = Options.DefaultTarget.Release;
        }
        #endregion
        ////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////
        #region Blobs and unitys
        [Configure(Blob.FastBuildUnitys)]
        [ConfigurePriority(ConfigurePriorities.Blobbing)]
        public virtual void FastBuildUnitys(Configuration conf, CommonTarget target)
        {
            conf.FastBuildBlobbed = true;
            conf.FastBuildUnityPath = Path.Combine(Globals.TmpDirectory, @"unity\[project.Name]");
            conf.IncludeBlobbedSourceFiles = false;
            conf.IsBlobbed = false;
        }

        [Configure(Blob.NoBlob)]
        [ConfigurePriority(ConfigurePriorities.Blobbing)]
        public virtual void BlobNoBlob(Configuration conf, CommonTarget target)
        {
            conf.FastBuildBlobbed = false;
            conf.IsBlobbed = false;

            if (conf.IsFastBuild)
                conf.ProjectName += "_NoBlob";
        }
        #endregion
        ////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////
        #region Build system
        [ConfigurePriority(ConfigurePriorities.BuildSystem)]
        [Configure(BuildSystem.MSBuild)]
        public virtual void ConfigureMSBuild(Configuration conf, CommonTarget target)
        {
            // starting with vs2019 16.10, need this to fix warning: argument unused during compilation: '/MP'
            conf.Options.Add(Options.Vc.Compiler.MultiProcessorCompilation.Disable);
        }

        [ConfigurePriority(ConfigurePriorities.BuildSystem)]
        [Configure(BuildSystem.FastBuild)]
        public virtual void ConfigureFastBuild(Configuration conf, CommonTarget target)
        {
            conf.SolutionFolder = "FastBuild/" + conf.SolutionFolder;
            conf.ProjectName += "_FastBuild";
            conf.ProjectFileName += "_FastBuild";

            conf.Defines.Add("USES_FASTBUILD");

            // Force writing to pdb from different cl.exe process to go through the pdb server
            if (target.Compiler == Compiler.MSVC)
                conf.AdditionalCompilerOptions.Add("/FS");
        }
        #endregion
        ////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////
        #region Compilers and toolchains
        [ConfigurePriority(ConfigurePriorities.Compiler)]
        [Configure(Compiler.MSVC)]
        public virtual void ConfigureMSVC(Configuration conf, CommonTarget target)
        {
            // no need to specify the PlatformToolset here since we want the default

            conf.Options.Add(Options.Vc.General.TreatWarningsAsErrors.Enable);
        }

        [ConfigurePriority(ConfigurePriorities.Compiler)]
        [Configure(Compiler.ClangCl)]
        public virtual void ConfigureClangCl(Configuration conf, CommonTarget target)
        {
            conf.Options.Add(Options.Vc.General.PlatformToolset.ClangCL);

            conf.Options.Add(Options.Vc.General.TreatWarningsAsErrors.Enable);

            conf.AdditionalCompilerOptions.Add(
                "-Wno-#pragma-messages",
                "-Wno-c++98-compat",
                "-Wno-microsoft-include"
            );
        }
        #endregion
        ////////////////////////////////////////////////////////////////////////
    }
}
