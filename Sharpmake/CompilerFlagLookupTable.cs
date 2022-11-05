using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpmake
{
    enum CompilerFlag
    {
        Define,
        Include,
        SystemInclude
    }

    class CompilerFlagLookupTable
    {
        private static Dictionary<CompilerFlag, string> MsvcCompilerFlags = new Dictionary<CompilerFlag, string>();
        private static Dictionary<CompilerFlag, string> ClangCompilerFlags = new Dictionary<CompilerFlag, string>();
        private static Dictionary<CompilerFlag, string> GccCompilerFlags = new Dictionary<CompilerFlag, string>();

        public static void Init()
        {
            CreateMsvcLookupTable();
            CreateClangLookupTable();
            CreateGccLookupTable();
        }

        public static string Get(Compiler compiler, CompilerFlag flag)
        {
            switch (compiler)
            {
                case Compiler.MSVC: return MsvcCompilerFlags.GetValueOrAdd(flag, "");
                case Compiler.Clang: return ClangCompilerFlags.GetValueOrAdd(flag, "");
                case Compiler.GCC: return GccCompilerFlags.GetValueOrAdd(flag, "");
                default: throw new Error("Unknown compiler used for compiler flag lookup");
            }
        }

        private static void CreateMsvcLookupTable()
        {
            MsvcCompilerFlags.Add(CompilerFlag.Define, "/D");
            MsvcCompilerFlags.Add(CompilerFlag.Include, "/I");
            MsvcCompilerFlags.Add(CompilerFlag.SystemInclude, "/I");
        }
        private static void CreateClangLookupTable()
        {
            ClangCompilerFlags.Add(CompilerFlag.Define, "-D");
            ClangCompilerFlags.Add(CompilerFlag.Include, "-I");
            ClangCompilerFlags.Add(CompilerFlag.SystemInclude, "-isystem");
        }
        private static void CreateGccLookupTable()
        {
            GccCompilerFlags.Add(CompilerFlag.Define, "-D");
            GccCompilerFlags.Add(CompilerFlag.Include, "-I");
            GccCompilerFlags.Add(CompilerFlag.SystemInclude, "-isystem");
        }
    }
}
