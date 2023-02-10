// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Sharpmake
{
    public enum CompilerFamily
    {
        Auto, // Auto detect compiler based on executable path
        MSVC, // Microsoft and compatible compilers
        Clang, // Clang and compatible compilers
        GCC, // GCC and compatible compilers
        SNC, // SNC and compatible compilers
        CodeWarriorWii, // CodeWarrior compiler for the Wii
        GreenHillsWiiU, // GreenHills compiler for the Wii U
        CudaNVCC, // NVIDIA's CUDA compiler
        QtRCC, // Qt's resource compiler
        VBCC, // vbcc compiler
        OrbisWavePsslc, // orbis wave psslc shader compiler
        ClangCl, // Clang in MSVC cl-compatible mode
        CSharp, // C# compiler
        Custom, // Any custom compiler
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
        IDictionary<DevEnv, bool> LinkerInvokedViaCompiler { get; set; }
        IDictionary<DevEnv, string> LibrarianExe { get; set; }
        IDictionary<DevEnv, Strings> ExtraFiles { get; set; }
    }
}
