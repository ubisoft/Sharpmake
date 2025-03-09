// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using Sharpmake;

namespace XCodeProjects
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

            SourceRootPath = Path.Combine(@"[project.RootPath]", @"[project.Name]");
            AdditionalSourceRootPaths.Add(Globals.ExternalDirectory);

            SourceFilesExtensions.Add(".m", ".mm", ".metal", ".plist", ".storyboard", ".xcassets");

            // .storyboard .xcassets resources need to be marked as compilable for iOS.
            SourceFilesCompileExtensions.Add(".m", ".mm", ".metal", ".storyboard", ".xcassets");

            // Add the resource file extension
            ResourceFilesExtensions.Add(".storyboard", ".xcassets");

            //!!! CAUTION: THIS IS COUNTERINTUITIVE !!!
            // Add ".plist" to compilable or resource extensions BUT DO NOT BUILD IT
            SourceFilesCompileExtensions.Add(".plist");
            ResourceFilesExtensions.Add(".plist");
            SourceFilesBuildExcludeRegex.Add(".plist");
            // AND THEN add in each project with
            // conf.Options.Add(new Sharpmake.Options.XCode.Compiler.InfoPListFile(Path.Join(SourceRootPath, "Info.plist")));
        }

        [ConfigurePriority(ConfigurePriorities.All)]
        [Configure]
        public virtual void ConfigureAll(Configuration conf, CommonTarget target)
        {
            conf.PreferRelativePaths = true;
            conf.IncludePaths.Add(Globals.ExternalDirectory);
            conf.IncludePaths.Add(Globals.IncludesDirectories);

            conf.Options.Add(Options.XCode.Compiler.CLanguageStandard.C11);
            conf.Options.Add(Options.XCode.Compiler.CppLanguageStandard.CPP17);
            conf.Options.Add(Options.Vc.Compiler.CLanguageStandard.C11);
            conf.Options.Add(Options.Vc.Compiler.CppLanguageStandard.CPP17);
            conf.Options.Add(Options.Clang.Compiler.CLanguageStandard.C11);
            conf.Options.Add(Options.Clang.Compiler.CppLanguageStandard.Cpp17);

            conf.ProjectFileName = @"[project.Name]_[target.Platform]_[target.DevEnv]";
            conf.ProjectPath = Path.Combine(Globals.TmpDirectory, "projects", @"[project.Name]");
            conf.IsFastBuild = target.BuildSystem == BuildSystem.FastBuild;

            conf.IntermediatePath = Path.Combine(
                Globals.TmpDirectory,
                "obj",
                @"[target.DirectoryName]",
                @"[project.Name]"
            );
            conf.TargetPath = Path.Combine(Globals.OutputDirectory, @"[target.DirectoryName]");

            // Note: uncomment the following line if we port this sample to windows
            //conf.TargetLibraryPath = conf.IntermediatePath; // // .lib files must be with the .obj files when running in fastbuild distributed mode or we'll have missing symbols due to merging of the .pdb
            conf.TargetLibraryPath = Path.Combine(
                Globals.TmpDirectory,
                "lib",
                @"[target.DirectoryName]",
                @"[project.Name]"
            );

            // TODO: uncomment and fix this. Didn't find a way to have product with
            //       different names per configurations to work properly...
            //conf.TargetFileName += "_" + target.Optimization.ToString().ToLowerInvariant().First(); // suffix with lowered first letter of optim
            //if (conf.IsFastBuild)
            //conf.TargetFileName += "x";

            conf.Output = Configuration.OutputType.Lib; // defaults to creating static libs
        }

        ////////////////////////////////////////////////////////////////////////
        #region Platfoms
        [ConfigurePriority(ConfigurePriorities.Platform - 1)]
        [Configure(
            Platform.mac | Platform.ios | Platform.tvos | Platform.watchos | Platform.maccatalyst
        )]
        public virtual void ConfigureApple(Configuration conf, CommonTarget target)
        {
            conf.Options.Add(Options.Vc.SourceFile.PrecompiledHeader.NotUsingPrecompiledHeaders);
            conf.Options.Add(Options.XCode.Compiler.LibraryStandard.LibCxx);
            conf.Options.Add(Options.XCode.Compiler.DebugInformationFormat.DwarfWithDSym);
            conf.Options.Add(Options.XCode.Compiler.EnableBitcode.Disable);
            conf.Options.Add(Options.XCode.Compiler.GenerateInfoPlist.Enable);
            conf.Options.Add(
                new Options.XCode.Compiler.ProductBundleDisplayName(@"[project.Name]")
            );
            conf.AdditionalLinkerOptions.Add("-lm"); // math functions
        }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.mac)]
        public virtual void ConfigureMacOS(Configuration conf, CommonTarget target)
        {
            conf.LibraryPaths.Add(Path.Combine(Globals.LibrariesDirectory, "macOS"));
            conf.Options.Add(Options.XCode.Compiler.OnlyActiveArch.Enable);
            conf.XcodeSystemFrameworks.Add("AppKit");
        }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.ios)]
        public virtual void ConfigureIOS(Configuration conf, CommonTarget target)
        {
            conf.LibraryPaths.Add(Path.Combine(Globals.LibrariesDirectory, "iOS"));
            conf.Options.Add(Options.XCode.Compiler.TargetedDeviceFamily.IosAndIpad);
            conf.Options.Add(Options.XCode.Compiler.SupportsMacDesignedForIphoneIpad.Enable);
            conf.XcodeSystemFrameworks.Add("UIKit");
        }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.tvos)]
        public virtual void ConfigureTVOS(Configuration conf, CommonTarget target)
        {
            conf.LibraryPaths.Add(Path.Combine(Globals.LibrariesDirectory, "tvOS"));
            conf.Options.Add(Options.XCode.Compiler.TargetedDeviceFamily.Tvos);
            conf.XcodeSystemFrameworks.Add("UIKit");
        }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.watchos)]
        public virtual void ConfigureWatchOS(Configuration conf, CommonTarget target)
        {
            conf.LibraryPaths.Add(Path.Combine(Globals.LibrariesDirectory, "watchOS"));
            conf.Options.Add(Options.XCode.Compiler.TargetedDeviceFamily.Watchos);
            conf.XcodeSystemFrameworks.Add("UIKit");
        }

        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.maccatalyst)]
        public virtual void ConfigureCatalyst(Configuration conf, CommonTarget target)
        {
            conf.LibraryPaths.Add(Path.Combine(Globals.LibrariesDirectory, "iOS"));
            conf.Options.Add(Options.XCode.Compiler.TargetedDeviceFamily.MacCatalyst);
            conf.Options.Add(Options.XCode.Compiler.SupportsMacDesignedForIphoneIpad.Disable);
            conf.XcodeSystemFrameworks.Add("UIKit");
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
            conf.FastBuildUnityPath = Path.Combine(
                Globals.TmpDirectory,
                "unity",
                @"[project.Name]"
            );
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
            conf.SolutionFolder = Path.Combine("FastBuild", conf.SolutionFolder);
            conf.ProjectName += "_FastBuild";

            conf.Defines.Add("USES_FASTBUILD");
        }
        #endregion
        ////////////////////////////////////////////////////////////////////////
    }
}
