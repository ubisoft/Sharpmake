// Copyright (c) 2020-2021 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.IO;
using System.Linq;
using Sharpmake;

namespace HelloIOS
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
            AddTargets(CommonTarget.GetDefaultTargets());
            
            RootPath = Globals.RootDirectory;
            IsFileNameToLower = false;
            IsTargetFileNameToLower = false;

            SourceFilesExtensions.Add(".m", ".mm"); // Objective-C, and Objective-C++ for iOS
            SourceFilesCompileExtensions.Add(".m", ".mm");

            SourceRootPath = @"[project.RootPath]/[project.Name]";
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
            //conf.TargetLibraryPath = conf.IntermediatePath; // // .lib files must be with the .obj files when running in fastbuild distributed mode or we'll have missing symbols due to merging of the .pdb
            conf.TargetLibraryPath = Path.Combine(Globals.TmpDirectory, @"lib\[target.DirectoryName]\[project.Name]");

            // TODO: uncomment and fix this. Didn't find a way to have product with
            //       different names per configurations to work properly...
            //conf.TargetFileName += "_" + target.Optimization.ToString().ToLowerInvariant().First(); // suffix with lowered first letter of optim
            if (conf.IsFastBuild)
                conf.TargetFileName += "x";

            conf.Output = Configuration.OutputType.Lib; // defaults to creating static libs
        }

        ////////////////////////////////////////////////////////////////////////
        #region Platfoms
        [ConfigurePriority(ConfigurePriorities.Platform)]
        [Configure(Platform.ios)]
        public virtual void ConfigureIos(Configuration conf, CommonTarget target)
        {
            if (conf.IsFastBuild)
            {
                conf.Options.Add(new Sharpmake.Options.XCode.Compiler.Archs("arm64"));
                conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ValidArchs("arm64"));
                conf.Options.Add(new Options.XCode.Compiler.IPhoneOSDeploymentTarget("13.0"));
            }
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
            conf.ProjectFileName += "_FastBuild";

            conf.Defines.Add("USES_FASTBUILD");
        }
        #endregion
        ////////////////////////////////////////////////////////////////////////
    }
}
