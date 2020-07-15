// Copyright (c) 2017 Ubisoft Entertainment
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

using System.Collections.Generic;
using System.Reflection;

namespace Sharpmake
{
    public enum BuilderCompileErrorBehavior
    {
        ThrowException,
        ReturnNullAssembly,
    }

    public interface ILoadInfo
    {
        Assembly Assembly { get; }
        IAssemblyInfo AssemblyInfo { get; }
        IEnumerable<ISourceAttributeParser> Parsers { get; }
    }

    public interface IBuilderContext
    {
        ILoadInfo BuildAndLoadSharpmakeFiles(IEnumerable<ISourceAttributeParser> parsers, IEnumerable<IParsingFlowParser> flowParsers, params string[] files);
        ILoadInfo LoadExtension(string file);
        void AddDefine(string define);
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
        void AddDefine(string define);
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

        public static IAssemblyInfo CreateAssemblyInfoWithDebugProjectName(this IAssemblyInfo info, string debugProjectName)
        {
            return new DebugProjectNameAssemblyInfo(info, debugProjectName);
        }

        private class DebugProjectNameAssemblyInfo : IAssemblyInfo
        {
            private readonly IAssemblyInfo _info;
            public string DebugProjectName { get; }

            public DebugProjectNameAssemblyInfo(IAssemblyInfo info, string debugProjectName)
            {
                _info = info;
                DebugProjectName = debugProjectName;
            }

            public Assembly Assembly => _info.Assembly;
            public string Id => _info.Id;
            public IReadOnlyCollection<string> References => _info.References;
            public IReadOnlyCollection<string> SourceFiles => _info.SourceFiles;
            public IReadOnlyDictionary<string, IAssemblyInfo> SourceReferences => _info.SourceReferences;
            public bool UseDefaultReferences => _info.UseDefaultReferences;
        }
    }
}
