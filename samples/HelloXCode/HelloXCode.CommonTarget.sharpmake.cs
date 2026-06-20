// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sharpmake;

namespace HelloXCode
{
    [Fragment, Flags]
    public enum Optimization
    {
        Debug = 1 << 0,
        Release = 1 << 1
    }

    [Fragment, Flags]
    public enum BuildSystem
    {
        Default = 1 << 0,
        FastBuild = 1 << 1,
    }

    [DebuggerDisplay("\"{Platform}_{DevEnv}\" {Name}")]
    public class CommonTarget : Sharpmake.ITarget
    {
        public Platform Platform;
        public DevEnv DevEnv;
        public Optimization Optimization;
        public Blob Blob;
        public BuildSystem BuildSystem;

        public CommonTarget() { }

        public CommonTarget(
            Platform platform,
            DevEnv devEnv,
            Optimization optimization,
            Blob blob,
            BuildSystem buildSystem
        )
        {
            Platform = platform;
            DevEnv = devEnv;
            Optimization = optimization;
            Blob = blob;
            BuildSystem = buildSystem;
        }

        public override string Name
        {
            get
            {
                var nameParts = new List<string>();

                nameParts.Add(Optimization.ToString().ToLowerInvariant());
                if (BuildSystem == BuildSystem.FastBuild)
                    nameParts.Add(BuildSystem.ToString().ToLowerInvariant());
                
                if (Blob == Blob.Blob)
                    nameParts.Add(Blob.ToString().ToLowerInvariant());

                return string.Join("_", nameParts);
            }
        }
        
        public string SolutionPlatformName
        {
            get
            {
                var nameParts = new List<string>();

                if (BuildSystem == BuildSystem.FastBuild)
                    nameParts.Add(BuildSystem.ToString().ToLowerInvariant());
                
                if (Blob == Blob.Blob)
                    nameParts.Add(Blob.ToString().ToLowerInvariant());
                
                nameParts.Add(DevEnv.ToString().ToLowerInvariant());

                return string.Join("_", nameParts);
            }
        }

        /// <summary>
        /// returns a string usable as a directory name, to use for instance for the intermediate path
        /// </summary>
        public string DirectoryName
        {
            get
            {
                var dirNameParts = new List<string>();

                dirNameParts.Add(Platform.ToString());
                dirNameParts.Add(Optimization.ToString());

                if (BuildSystem == BuildSystem.FastBuild)
                    dirNameParts.Add(BuildSystem.ToString());

                dirNameParts.Add(DevEnv.ToString());

                return string.Join("_", dirNameParts).ToLowerInvariant();
            }
        }

        public override Platform GetPlatform()
        {
            return Platform;
        }

        public static CommonTarget[] GetDefaultTargets()
        {
            var result = new List<CommonTarget>();
            result.AddRange(GetMacTargets());
            return result.ToArray();
        }

        public static CommonTarget[] GetMacTargets()
        {
            var macosTarget = new CommonTarget(
                Platform.mac,
                DevEnv.xcode | DevEnv.vs2026,
                Optimization.Debug | Optimization.Release,
                Blob.NoBlob,
                BuildSystem.Default
            );

            // make a blob version of the target
            var macosBlobTarget = (CommonTarget)macosTarget.Clone(
                Blob.Blob
            );

            // make a fastbuild version of the target
            var macosFastBuildTarget = (CommonTarget)macosTarget.Clone(
                Blob.FastBuildUnitys,
                BuildSystem.FastBuild,
                DevEnv.vs2026
            );

            var iosTarget = new CommonTarget(
                Platform.ios,
                DevEnv.xcode | DevEnv.vs2026,
                Optimization.Debug | Optimization.Release,
                Blob.NoBlob,
                BuildSystem.Default
            );

            // make a blob version of the target
            var iosBlobTarget = (CommonTarget)iosTarget.Clone(
                Blob.Blob
            );

            // make a fastbuild version of the target
            var iosFastBuildTarget = (CommonTarget)iosTarget.Clone(
                Blob.FastBuildUnitys,
                BuildSystem.FastBuild,
                DevEnv.vs2026
            );

            var tvosTarget = new CommonTarget(
                Platform.tvos,
                DevEnv.xcode | DevEnv.vs2026,
                Optimization.Debug | Optimization.Release,
                Blob.NoBlob,
                BuildSystem.Default
            );

            // make a fastbuild version of the target
            var tvosBlobTarget = (CommonTarget)tvosTarget.Clone(
                Blob.Blob
            );

            // make a fastbuild version of the target
            var tvosFastBuildTarget = (CommonTarget)tvosTarget.Clone(
                Blob.FastBuildUnitys,
                BuildSystem.FastBuild,
                DevEnv.vs2026
            );

            var watchosTarget = new CommonTarget(
                Platform.watchos,
                DevEnv.xcode | DevEnv.vs2026,
                Optimization.Debug | Optimization.Release,
                Blob.NoBlob,
                BuildSystem.Default
            );

            // make a blob version of the target
            var watchosBlobTarget = (CommonTarget)watchosTarget.Clone(
                Blob.Blob
            );

            // make a fastbuild version of the target
            var watchosFastBuildTarget = (CommonTarget)watchosTarget.Clone(
                Blob.FastBuildUnitys,
                BuildSystem.FastBuild,
                DevEnv.vs2026
            );

            var catalystTarget = new CommonTarget(
                Platform.maccatalyst,
                DevEnv.xcode | DevEnv.vs2026,
                Optimization.Debug | Optimization.Release,
                Blob.NoBlob,
                BuildSystem.Default
            );

            // make a blob version of the target
            var catalystBlobTarget = (CommonTarget)catalystTarget.Clone(
                Blob.Blob
            );

            // make a FastBuild version of the target
            var catalystFastBuildTarget = (CommonTarget)catalystTarget.Clone(
                Blob.FastBuildUnitys,
                BuildSystem.FastBuild,
                DevEnv.vs2026
            );

            return new[] {
                macosTarget, macosBlobTarget, macosFastBuildTarget,
                iosTarget, iosBlobTarget, iosFastBuildTarget,
                tvosTarget, tvosBlobTarget, tvosFastBuildTarget,
                watchosTarget, watchosBlobTarget, watchosFastBuildTarget,
                catalystTarget, catalystBlobTarget, catalystFastBuildTarget,
            };
        }
    }
}
