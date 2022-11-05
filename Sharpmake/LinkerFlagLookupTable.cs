using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharpmake
{
    enum LinkerFlag
    {
        IncludePath,
        IncludeFile
    }
    class LinkerFlagLookupTable
    {
        private static Dictionary<LinkerFlag, string> MsvcCompilerFlags = new Dictionary<LinkerFlag, string>();
        private static Dictionary<LinkerFlag, string> ClangCompilerFlags = new Dictionary<LinkerFlag, string>();
        private static Dictionary<LinkerFlag, string> GccCompilerFlags = new Dictionary<LinkerFlag, string>();

        public static void Init()
        {
            CreateMsvcLookupTable();
            CreateClangLookupTable();
            CreateGccLookupTable();
        }

        public static string Get(Compiler compiler, LinkerFlag flag)
        {
            switch (compiler)
            {
                case Compiler.MSVC:
                    return MsvcCompilerFlags.GetValueOrAdd(flag, "");
                case Compiler.Clang:
                    return ClangCompilerFlags.GetValueOrAdd(flag, "");
                case Compiler.GCC:
                    return GccCompilerFlags.GetValueOrAdd(flag, "");
                default:
                    throw new Error("Unknown compiler used for linker flag lookup");
            }
        }

        private static void CreateMsvcLookupTable()
        {
            MsvcCompilerFlags.Add(LinkerFlag.IncludePath, "/LIBPATH:");
            MsvcCompilerFlags.Add(LinkerFlag.IncludeFile, "");
        }
        private static void CreateClangLookupTable()
        {
            ClangCompilerFlags.Add(LinkerFlag.IncludePath, "-L");
            ClangCompilerFlags.Add(LinkerFlag.IncludeFile, "-l");
        }
        private static void CreateGccLookupTable()
        {
            GccCompilerFlags.Add(LinkerFlag.IncludePath, "-L");
            GccCompilerFlags.Add(LinkerFlag.IncludeFile, "-l");
        }
    }
}
