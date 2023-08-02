// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sharpmake;

[module: Sharpmake.Reference("Sharpmake.CommonPlatforms.dll")]

namespace HelloAndroid
{
    using AndroidBuildTargets = Sharpmake.Android.AndroidBuildTargets;

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
        public AndroidBuildTargets AndroidBuildTargets = AndroidBuildTargets.arm64_v8a | AndroidBuildTargets.x86_64;
        public Android.AndroidBuildType AndroidBuildType = Android.AndroidBuildType.Gradle;

        public CommonTarget() { }

        public CommonTarget(
            Platform platform,
            DevEnv devEnv,
            Optimization optimization,
            Blob blob,
            BuildSystem buildSystem,
            Android.AndroidBuildType androidBuildType
        )
        {
            Platform = platform;
            DevEnv = devEnv;
            Optimization = optimization;
            Blob = blob;
            BuildSystem = buildSystem;
            AndroidBuildType = androidBuildType;
        }

        public override string Name
        {
            get
            {
                var nameParts = new List<string>();

                nameParts.Add(Optimization.ToString());
                if (Blob == Blob.NoBlob)
                {
                    nameParts.Add(Blob.ToString());
                }
                if (BuildSystem == BuildSystem.FastBuild)
                {
                    nameParts.Add(BuildSystem.ToString());
                }

                //using underscore to join different name parts because gradle is not able to parse
                //the names properly if we use space to join them
                return string.Join("_", nameParts);
            }
        }

        public string SolutionPlatformName
        {
            get
            {
                return this.AndroidBuildTargets.ToString();
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

                return string.Join("_", dirNameParts);
            }
        }

        public override Sharpmake.Optimization GetOptimization()
        {
            switch (Optimization)
            {
                case Optimization.Debug:
                    return Sharpmake.Optimization.Debug;
                case Optimization.Release:
                    return Sharpmake.Optimization.Release;
                default:
                    throw new NotSupportedException("Optimization value " + Optimization.ToString());
            }
        }

        public override Platform GetPlatform()
        {
            return Platform;
        }

        public static CommonTarget[] GetDefaultTargets()
        {
            var result = new List<CommonTarget>();
            result.AddRange(GetAndroidTargets());
            return result.ToArray();
        }

        public static CommonTarget[] GetAndroidTargets()
        {
            var defaultTarget = new CommonTarget(
                Platform.android,
                DevEnv.vs2019,
                Optimization.Debug | Optimization.Release,
                Blob.NoBlob,
                BuildSystem.Default,
                Android.AndroidBuildType.Gradle
            );

            // make a fastbuild version of the target
            var fastBuildTarget = (CommonTarget)defaultTarget.Clone(
                Blob.FastBuildUnitys,
                BuildSystem.FastBuild
            );

            return new[] { defaultTarget };
        }
    }
}
