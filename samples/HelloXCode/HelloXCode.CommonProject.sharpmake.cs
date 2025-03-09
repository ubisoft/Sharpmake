// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using Sharpmake;

namespace HelloXCode
{
    public static class ConfigurePriorities
    {
        public const int All = -75;
        public const int Platform = -50;
        public const int Optimization = -25;
        /*     SHARPMAKE DEFAULT IS 0     */
        public const int Blobbing = 10;
        public const int BuildSystem = 30;
    }

    public abstract class CommonProject : Sharpmake.Project
    {
        protected CommonProject()
            : base(typeof(CommonTarget))
        {
            RootPath = Globals.RootDirectory;
            IsFileNameToLower = false;
            IsTargetFileNameToLower = false;

            SourceFilesExtensions.Add(".sc");

            SourceRootPath = @"[project.RootPath]\[project.Name]";
            AdditionalSourceRootPaths.Add(Globals.ExternalDirectory);
        }

        [ConfigurePriority(ConfigurePriorities.All)]
        [Configure]
        public virtual void ConfigureAll(Configuration conf, CommonTarget target)
        {
            conf.IncludePaths.Add(Globals.ExternalDirectory);

            conf.ProjectFileName = "[project.Name]_[target.Platform]";
            if (target.DevEnv != DevEnv.xcode)
                conf.ProjectFileName += "_[target.DevEnv]";
            conf.ProjectPath = Path.Combine(Globals.TmpDirectory, @"projects\[project.Name]");
            conf.IsFastBuild = target.BuildSystem == BuildSystem.FastBuild;

            conf.IntermediatePath = Path.Combine(Globals.TmpDirectory, @"obj\[target.DirectoryName]\[project.Name]");
            conf.TargetPath = Path.Combine(Globals.OutputDirectory, "[target.DirectoryName]");

            // Note: uncomment the following line if we port this sample to windows
            //conf.TargetLibraryPath = conf.IntermediatePath; // // .lib files must be with the .obj files when running in fastbuild distributed mode or we'll have missing symbols due to merging of the .pdb
            conf.TargetLibraryPath = Path.Combine(Globals.TmpDirectory, @"lib\[target.DirectoryName]\[project.Name]");

            // TODO: uncomment and fix this. Didn't find a way to have product with
            //       different names per configurations to work properly...
            //conf.TargetFileName += "_" + target.Optimization.ToString().ToLowerInvariant().First(); // suffix with lowered first letter of optim
            //if (conf.IsFastBuild)
            //conf.TargetFileName += "x";

            conf.Output = Configuration.OutputType.Lib; // defaults to creating static libs
            conf.Options.Add(Options.XCode.Editor.Indent.Spaces);

            if (target.Optimization == Optimization.Release)
            {
                conf.Options.Add(Sharpmake.Options.XCode.Compiler.DebugInformationFormat.DwarfWithDSym);
                conf.Options.Add(Sharpmake.Options.XCode.Linker.StripLinkedProduct.Enable);
                conf.Options.Add(Sharpmake.Options.XCode.Linker.StripStyle.DebuggingSymbolsOnly);
                conf.Options.Add(Sharpmake.Options.XCode.Linker.StripSwiftSymbols.Enable);
            }
            else
                conf.Options.Add(Sharpmake.Options.XCode.Compiler.DebugInformationFormat.Dwarf);
        }

        ////////////////////////////////////////////////////////////////////////
        #region Platfoms
        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.mac)]
        public virtual void ConfigureMac(Configuration conf, CommonTarget target)
        {
            conf.Options.Add(Options.XCode.Compiler.OnlyActiveArch.Enable);
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
        #region Compilers and toolchains
        [ConfigurePriority(ConfigurePriorities.BuildSystem)]
        [Configure(BuildSystem.FastBuild)]
        public virtual void ConfigureFastBuild(Configuration conf, CommonTarget target)
        {
            conf.SolutionFolder = "FastBuild/" + conf.SolutionFolder;
            conf.ProjectName += "_FastBuild";

            conf.Defines.Add("USES_FASTBUILD");
        }
        #endregion
        ////////////////////////////////////////////////////////////////////////
    }
}
