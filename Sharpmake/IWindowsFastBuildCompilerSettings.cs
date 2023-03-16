// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Sharpmake
{
    public class FastBuildWindowsCompilerFamilyKey : IFastBuildCompilerKey
    {
        public DevEnv DevelopmentEnvironment { get; set; }
        public Options.Vc.General.PlatformToolset PlatformToolset { get; set; }

        public FastBuildWindowsCompilerFamilyKey(DevEnv devEnv, Options.Vc.General.PlatformToolset platformToolset)
        {
            DevelopmentEnvironment = devEnv;
            PlatformToolset = platformToolset;
        }

        public override int GetHashCode()
        {
            int hash = 3;

            hash = hash * 5 + DevelopmentEnvironment.GetHashCode();
            hash = hash * 5 + PlatformToolset.GetHashCode();

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return false;
            if (obj.GetType() != GetType())
                return false;

            return Equals((FastBuildWindowsCompilerFamilyKey)obj);
        }

        public bool Equals(FastBuildWindowsCompilerFamilyKey compilerFamilyKey)
        {
            return DevelopmentEnvironment.Equals(compilerFamilyKey.DevelopmentEnvironment) &&
                   PlatformToolset.Equals(compilerFamilyKey.PlatformToolset);
        }
    }

    public interface IWindowsFastBuildCompilerSettings : IFastBuildCompilerSettings
    {
        // TODO: It looks like this belongs to Sharpmake.Generators.
        IDictionary<DevEnv, string> ResCompiler { get; set; }
        IDictionary<DevEnv, string> ResxCompiler { get; set; }
    }
}
