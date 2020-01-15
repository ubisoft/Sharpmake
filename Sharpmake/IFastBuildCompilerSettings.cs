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
    public enum CompilerFamily
    {
        Auto,
        MSVC,
        Clang,
        GCC,
        SNC,
        CodeWarriorWii,
        GreenHillsWiiU,
        CudaNVCC,
        QtRCC,
        VBCC,
        OrbisWavePsslc,
        ClangCl
    }

    public interface IFastBuildCompilerKey
    {
        DevEnv DevelopmentEnvironment { get; set; }
    }

    public class FastBuildCompilerKey : IFastBuildCompilerKey
    {
        public DevEnv DevelopmentEnvironment { get; set; }

        public FastBuildCompilerKey(DevEnv devEnv)
        {
            DevelopmentEnvironment = devEnv;
        }

        public override int GetHashCode()
        {
            return DevelopmentEnvironment.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return false;
            if (obj.GetType() != GetType())
                return false;

            return Equals((FastBuildCompilerKey)obj);
        }

        public bool Equals(FastBuildCompilerKey compilerFamilyKey)
        {
            return DevelopmentEnvironment.Equals(compilerFamilyKey.DevelopmentEnvironment);
        }
    }

    public interface IFastBuildCompilerSettings
    {
        IDictionary<DevEnv, string> BinPath { get; set; }
        IDictionary<IFastBuildCompilerKey, CompilerFamily> CompilerFamily { get; set; }
        IDictionary<DevEnv, string> LinkerPath { get; set; }
        IDictionary<DevEnv, string> LinkerExe { get; set; }
        IDictionary<DevEnv, string> LibrarianExe { get; set; }
        IDictionary<DevEnv, Strings> ExtraFiles { get; set; }
    }
}
