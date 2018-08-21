using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sharpmake
{
    public interface IBuilderContext
    {
    }

    public interface IAssemblerContext
    {
        void AddSourceFile(string file);
        void AddReference(string file);
    }

    public static class AssemblerContextHelpers
    {
        public static void AddSourceFiles(this IAssemblerContext context, IEnumerable<string> files)
        {
            foreach (string file in files)
                context.AddSourceFile(file);
        }

        public static void AddReferences(this IAssemblerContext context, IEnumerable<string> files)
        {
            foreach (string file in files)
                context.AddReference(file);
        }
    }
}
