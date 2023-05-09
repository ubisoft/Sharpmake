// Copyright (c) 2022-2023 Ubisoft Entertainment
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sharpmake;

namespace XCodeProjects
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

        public CommonTarget(CommonTarget reference)
        {
            Platform = reference.Platform;
            DevEnv = reference.DevEnv;
            Optimization = reference.Optimization;
            Blob = reference.Blob;
            BuildSystem = reference.BuildSystem;
        }

        public override string Name
        {
            get => Optimization.ToString().ToLowerInvariant();
        }

        public string SolutionPlatformName
        {
            get { return $"{BuildSystem.ToString()}_{Blob.ToString()}".ToLowerInvariant(); }
        }

        /// <summary>
        /// returns a string usable as a directory name, to use for instance for the intermediate path
        /// </summary>
        public string DirectoryName
        {
            get
            {
                return $"{Platform.ToString()}_{Optimization.ToString()}_{BuildSystem.ToString()}".ToLowerInvariant();
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
            result.AddRange(GetIosTargets());
            result.AddRange(GetTvosTargets());
            result.AddRange(GetCatalystTargets());
            return result.ToArray();
        }

        public static CommonTarget[] GetMacTargets()
        {
            var macosTarget = new CommonTarget()
            {
                Platform = Platform.mac,
                DevEnv = DevEnv.xcode,
                Optimization = Optimization.Debug | Optimization.Release,
                Blob = Blob.NoBlob,
                BuildSystem = BuildSystem.Default
            };

            // make a blob version of the target
            var macosBlobTarget = new CommonTarget(macosTarget) { Blob = Blob.Blob, };

            // make a fastbuild version of the target
            var macosFastBuildTarget = new CommonTarget(macosTarget)
            {
                Blob = Blob.FastBuildUnitys,
                BuildSystem = BuildSystem.FastBuild,
            };

            return new CommonTarget[] { macosTarget, macosBlobTarget, macosFastBuildTarget, };
        }

        public static CommonTarget[] GetIosTargets()
        {
            var iosTarget = new CommonTarget()
            {
                Platform = Platform.ios,
                DevEnv = DevEnv.xcode,
                Optimization = Optimization.Debug | Optimization.Release,
                Blob = Blob.NoBlob,
                BuildSystem = BuildSystem.Default
            };

            // make a blob version of the target
            var iosBlobTarget = new CommonTarget(iosTarget) { Blob = Blob.Blob, };

            // make a fastbuild version of the target
            var iosFastBuildTarget = new CommonTarget(iosTarget)
            {
                Blob = Blob.FastBuildUnitys,
                BuildSystem = BuildSystem.FastBuild,
            };

            return new CommonTarget[] { iosTarget, iosBlobTarget, iosFastBuildTarget, };
        }

        public static CommonTarget[] GetTvosTargets()
        {
            var tvosTarget = new CommonTarget()
            {
                Platform = Platform.tvos,
                DevEnv = DevEnv.xcode,
                Optimization = Optimization.Debug | Optimization.Release,
                Blob = Blob.NoBlob,
                BuildSystem = BuildSystem.Default
            };

            // make a blob version of the target
            var tvosBlobTarget = new CommonTarget(tvosTarget) { Blob = Blob.Blob, };

            // make a fastbuild version of the target
            var tvosFastBuildTarget = new CommonTarget(tvosTarget)
            {
                Blob = Blob.FastBuildUnitys,
                BuildSystem = BuildSystem.FastBuild,
            };

            return new CommonTarget[] { tvosTarget, tvosBlobTarget, tvosFastBuildTarget, };
        }

        public static CommonTarget[] GetWatchosTargets()
        {
            var watchosTarget = new CommonTarget()
            {
                Platform = Platform.watchos,
                DevEnv = DevEnv.xcode,
                Optimization = Optimization.Debug | Optimization.Release,
                Blob = Blob.NoBlob,
                BuildSystem = BuildSystem.Default
            };

            // make a blob version of the target
            var watchosBlobTarget = new CommonTarget(watchosTarget) { Blob = Blob.Blob, };

            // make a fastbuild version of the target
            var watchosFastBuildTarget = new CommonTarget(watchosTarget)
            {
                Blob = Blob.FastBuildUnitys,
                BuildSystem = BuildSystem.FastBuild,
            };

            return new CommonTarget[] { watchosTarget, watchosBlobTarget, watchosFastBuildTarget, };
        }

        public static CommonTarget[] GetCatalystTargets()
        {
            var catTarget = new CommonTarget()
            {
                Platform = Platform.maccatalyst,
                DevEnv = DevEnv.xcode,
                Optimization = Optimization.Debug | Optimization.Release,
                Blob = Blob.NoBlob,
                BuildSystem = BuildSystem.Default
            };

            // make a blob version of the target
            var catBlobTarget = new CommonTarget(catTarget) { Blob = Blob.Blob, };

            // make a fastbuild version of the target
            var catFastBuildTarget = new CommonTarget(catTarget)
            {
                Blob = Blob.FastBuildUnitys,
                BuildSystem = BuildSystem.FastBuild,
            };

            return new CommonTarget[] { catTarget, catBlobTarget, catFastBuildTarget, };
        }
    }
}
