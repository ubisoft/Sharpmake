// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
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
