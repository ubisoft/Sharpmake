using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sharpmake
{
    public enum BuilderCompileErrorBehavior
    {
        ThrowException,
        ReturnNullAssembly,
    }

    public interface ILoadInfo
    {
        IAssemblyInfo AssemblyInfo { get; }
        IEnumerable<ISourceAttributeParser> Parsers { get; }
    }

    public interface IBuilderContext
    {
        ILoadInfo BuildAndLoadSharpmakeFiles(IEnumerable<ISourceAttributeParser> parsers, params string[] files);
        BuilderCompileErrorBehavior CompileErrorBehavior { get; }
    }

    public interface IAssemblerContext
    {
        void AddSourceFile(string file);
        void AddReference(string file);
        void AddReference(IAssemblyInfo info);
        void AddSourceAttributeParser(ISourceAttributeParser parser);
        IAssemblyInfo BuildAndLoadSharpmakeFiles(params string[] files);
        void SetDebugProjectName(string name);
    }

    public interface IAssemblyInfo
    {
        string Id { get; }
        string DebugProjectName { get; }
        Assembly Assembly { get; }
        IReadOnlyCollection<string> SourceFiles { get; }
        IReadOnlyCollection<string> References { get; }
        IReadOnlyDictionary<string, IAssemblyInfo> SourceReferences { get; }
        bool UseDefaultReferences { get; }
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

        public static void AddReferences(this IAssemblerContext context, IEnumerable<IAssemblyInfo> infos)
        {
            foreach (var info in infos)
                context.AddReference(info);
        }

        public static void AddSourceAttributeParsers(this IAssemblerContext context, IEnumerable<ISourceAttributeParser> parsers)
        {
            foreach (var parser in parsers)
                context.AddSourceAttributeParser(parser);
        }

        public static IAssemblyInfo BuildLoadAndAddReferenceToSharpmakeFilesAssembly(this IAssemblerContext context, params string[] files)
        {
            var assemblyInfo = context.BuildAndLoadSharpmakeFiles(files);
            context.AddReference(assemblyInfo);
            return assemblyInfo;
        }
    }
}
