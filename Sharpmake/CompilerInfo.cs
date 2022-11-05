using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpmake
{
    [Fragment, Flags]
    public enum Compiler
    {
        Auto = (1 << 0), // Determine Compiler from DevEnv (eg. Visual Studio -> MSVC)
        MSVC = (1 << 1), // Microsoft and compatible compilers
        Clang = (1 << 2), // Clang and compatible compilers
        GCC = (1 << 3), // GCC and compatible compilers
    }

    public class CompilerInfo
    {
        public CompilerInfo(Compiler compiler, string binPath, string linkerPath)
        {
            Compiler = compiler;
            BinPath = binPath;
            LinkerPath = linkerPath;
        }

        public Compiler Compiler { get; set; }
        public string BinPath { get; set; }
        public string LinkerPath { get; set; }
    }
}
