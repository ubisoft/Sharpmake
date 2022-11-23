// Copyright (c) 2018-2022 Ubisoft Entertainment
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

using System;
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
        bool DebugScripts { get; }
    }

    public interface IAssemblerContext
    {
        void AddSourceFile(string file);
        [Obsolete("Use AddRuntimeReference() instead")]
        void AddReference(string file);
        void AddRuntimeReference(string file);
        [Obsolete("Use AddRuntimeReference() instead")]
        void AddReference(IAssemblyInfo info);
        void AddRuntimeReference(IAssemblyInfo info);
        void AddBuildReference(string file);
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
        [Obsolete("Use RuntimeReference instead")]
        IReadOnlyCollection<string> References { get; }
        IReadOnlyCollection<string> RuntimeReferences { get; }
        IReadOnlyCollection<string> BuildReferences { get; }
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

        [Obsolete("Use AddRuntimeReferences() instead")]
        public static void AddReferences(this IAssemblerContext context, IEnumerable<string> files) => AddRuntimeReferences(context, files);

        public static void AddRuntimeReferences(this IAssemblerContext context, IEnumerable<string> files)
        {
            foreach (string file in files)
                context.AddRuntimeReference(file);
        }

        public static void AddBuildReferences(this IAssemblerContext context, IEnumerable<string> files)
        {
            foreach (string file in files)
                context.AddBuildReference(file);
        }

        [Obsolete("Use AddRuntimeReferences() instead")]
        public static void AddReferences(this IAssemblerContext context, IEnumerable<IAssemblyInfo> infos) => AddRuntimeReferences(context, infos);

        public static void AddRuntimeReferences(this IAssemblerContext context, IEnumerable<IAssemblyInfo> infos)
        {
            foreach (var info in infos)
                context.AddRuntimeReference(info);
        }

        public static void AddSourceAttributeParsers(this IAssemblerContext context, IEnumerable<ISourceAttributeParser> parsers)
        {
            foreach (var parser in parsers)
                context.AddSourceAttributeParser(parser);
        }

        public static IAssemblyInfo BuildLoadAndAddReferenceToSharpmakeFilesAssembly(this IAssemblerContext context, params string[] files)
        {
            var assemblyInfo = context.BuildAndLoadSharpmakeFiles(files);
            context.AddRuntimeReference(assemblyInfo);
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
            [Obsolete("Use RuntimeReference instead")]
            public IReadOnlyCollection<string> References => RuntimeReferences;
            public IReadOnlyCollection<string> RuntimeReferences => _info.RuntimeReferences;
            public IReadOnlyCollection<string> BuildReferences => _info.BuildReferences;
            public IReadOnlyCollection<string> SourceFiles => _info.SourceFiles;
            public IReadOnlyDictionary<string, IAssemblyInfo> SourceReferences => _info.SourceReferences;
            public bool UseDefaultReferences => _info.UseDefaultReferences;
        }
    }
}
