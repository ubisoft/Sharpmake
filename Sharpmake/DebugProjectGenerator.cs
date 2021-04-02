// Copyright (c) 2017-2021 Ubisoft Entertainment
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace Sharpmake
{
    /// <summary>
    /// Generates debug projects and solutions
    /// </summary>
    public static class DebugProjectGenerator
    {
        internal static string RootPath { get; private set; }
        internal static string[] MainSources { get; private set; }

        public interface IDebugProjectExtension
        {
            void AddSharpmakePackage(Project.Configuration config);
            void AddReferences(Project.Configuration config, IEnumerable<string> additionalReferences = null);
            string GetSharpmakeExecutableFullPath();
        }

        public class DefaultDebugProjectExtension : IDebugProjectExtension
        {
            public virtual void AddSharpmakePackage(Project.Configuration conf)
            {
                if (!ShouldUseLocalSharpmakeDll())
                {
                    return;
                }

                string sharpmakeDllPath;
                string sharpmakeGeneratorDllPath;
                GetSharpmakeLocalDlls(out sharpmakeDllPath, out sharpmakeGeneratorDllPath);

                conf.ReferencesByPath.Add(sharpmakeDllPath);
                conf.ReferencesByPath.Add(sharpmakeGeneratorDllPath);
            }

            protected static void GetSharpmakeLocalDlls(out string sharpmakeDllPath, out string sharpmakeGeneratorDllPath)
            {
                sharpmakeDllPath = Assembly.GetExecutingAssembly().Location;
                sharpmakeGeneratorDllPath = Assembly.Load("Sharpmake.Generators")?.Location;
            }

            public virtual void AddReferences(Project.Configuration conf, IEnumerable<string> additionalReferences = null)
            {
                conf.ReferencesByPath.Add(Assembler.DefaultReferences);
                if (additionalReferences != null)
                {
                    conf.ReferencesByPath.AddRange(additionalReferences);
                }
            }

            public virtual bool ShouldUseLocalSharpmakeDll()
            {
                return true;
            }

            public virtual string GetSharpmakeExecutableFullPath()
            {
                string sharpmakeApplicationExePath = Process.GetCurrentProcess().MainModule.FileName;

                if (Util.IsRunningInMono())
                {
                    // When running within Mono, sharpmakeApplicationExePath will at this point wrongly refer to the
                    // mono (or mono-sgen) executable. Fix it so that it points to Sharpmake.Application.exe.
                    sharpmakeApplicationExePath = $"{AppDomain.CurrentDomain.BaseDirectory}{AppDomain.CurrentDomain.FriendlyName}";
                }
                return sharpmakeApplicationExePath;
            }
        }

        public static IDebugProjectExtension DebugProjectExtension { get; set; } = new DefaultDebugProjectExtension();

        /// <summary>
        /// Generates debug projects and solutions
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="arguments"></param>
        /// <param name="startArguments"></param>
        public static void GenerateDebugSolution(string[] sources, Arguments arguments, string startArguments)
        {
            GenerateDebugSolution(sources, arguments, startArguments, null);
        }

        /// <summary>
        /// Generates debug projects and solutions
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="arguments"></param>
        /// <param name="startArguments"></param>
        /// <param name="defines"></param>
        public static void GenerateDebugSolution(string[] sources, Arguments arguments, string startArguments, string[] defines)
        {
            FindAllSources(sources, arguments, startArguments, defines);
            arguments.Generate<DebugSolution>();
        }

        internal class ProjectContent
        {
            public string DisplayName;

            public string ProjectFolder;
            public readonly HashSet<string> ProjectFiles = new HashSet<string>();

            public readonly List<string> References = new List<string>();
            public readonly List<Type> ProjectReferences = new List<Type>();

            public readonly List<string> Defines = new List<string>();

            public bool IsSetupProject;

            public string StartArguments;
        }
        internal static readonly Dictionary<Type, ProjectContent> DebugProjects = new Dictionary<Type, ProjectContent>();

        private static void FindAllSources(string[] sourcesArguments, Sharpmake.Arguments sharpmakeArguments, string startArguments, string[] defines)
        {
            MainSources = sourcesArguments;
            RootPath = Path.GetDirectoryName(sourcesArguments[0]);

            Assembler assembler = new Assembler(sharpmakeArguments.Builder.Defines);
            assembler.AttributeParsers.Add(new DebugProjectNameAttributeParser());
            IAssemblyInfo assemblyInfo = assembler.LoadUncompiledAssemblyInfo(Builder.Instance.CreateContext(BuilderCompileErrorBehavior.ReturnNullAssembly), MainSources);

            GenerateDebugProject(assemblyInfo, true, startArguments, new Dictionary<string, Type>(), defines);
        }

        private static Type GenerateDebugProject(IAssemblyInfo assemblyInfo, bool isSetupProject, string startArguments, IDictionary<string, Type> visited, string[] defines)
        {
            string displayName = assemblyInfo.DebugProjectName;
            if (string.IsNullOrEmpty(displayName))
                displayName = isSetupProject ? "sharpmake_debug" : $"sharpmake_package_{assemblyInfo.Id.GetHashCode():X8}";

            Type generatedProject;
            if (visited.TryGetValue(assemblyInfo.Id, out generatedProject))
            {
                if (generatedProject == null)
                    throw new Error($"Circular sharpmake package dependency on {displayName}");
                return generatedProject;
            }

            visited[assemblyInfo.Id] = null;

            ProjectContent project = new ProjectContent
            {
                ProjectFolder = RootPath,
                IsSetupProject = isSetupProject,
                DisplayName = displayName,
                StartArguments = startArguments
            };
            generatedProject = CreateProject(displayName);
            DebugProjects.Add(generatedProject, project);

            // Add sources
            foreach (var source in assemblyInfo.SourceFiles)
            {
                project.ProjectFiles.Add(source);
            }

            // Add references
            var references = new HashSet<string>();
            if (assemblyInfo.UseDefaultReferences)
            {
                foreach (string defaultReference in Assembler.DefaultReferences)
                    references.Add(Assembler.GetAssemblyDllPath(defaultReference));
            }

            foreach (var assemblerRef in assemblyInfo.References)
            {
                if (!assemblyInfo.SourceReferences.ContainsKey(assemblerRef))
                {
                    references.Add(assemblerRef);
                }
            }

            project.References.AddRange(references);

            foreach (var refInfo in assemblyInfo.SourceReferences.Values)
            {
                project.ProjectReferences.Add(GenerateDebugProject(refInfo, false, string.Empty, visited, defines));
            }

            if (defines != null)
            {
                project.Defines.AddRange(defines);
            }

            visited[assemblyInfo.Id] = generatedProject;

            return generatedProject;
        }

        private static Type CreateProject(string typeSignature)
        {
            // define class type
            var assemblyName = new AssemblyName(typeSignature);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DebugSharpmakeModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeSignature,
                TypeAttributes.Public | TypeAttributes.Class |
                TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
                TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
                typeof(DebugProject));

            // add attribute [Sharpmake.Generate]
            Type[] generateAttrParams = { };
            ConstructorInfo generateAttrCtorInfo = typeof(Sharpmake.Generate).GetConstructor(generateAttrParams);
            CustomAttributeBuilder generateAttrBuilder = new CustomAttributeBuilder(generateAttrCtorInfo, new object[] { });
            typeBuilder.SetCustomAttribute(generateAttrBuilder);

            return typeBuilder.CreateType();
        }

        internal static Target GetTargets()
        {
            return new Target(
                Platform.anycpu,
                DevEnv.vs2019,
                Optimization.Debug | Optimization.Release,
                OutputType.Dll,
                Blob.NoBlob,
                BuildSystem.MSBuild,
                Assembler.SharpmakeDotNetFramework
            );
        }

        /// <summary>
        /// Set up debug configuration in user file
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="startArguments"></param>
        public static void SetupProjectOptions(this Project.Configuration conf, string startArguments)
        {
            conf.CsprojUserFile = new Project.Configuration.CsprojUserFileSettings();
            conf.CsprojUserFile.StartAction = Project.Configuration.CsprojUserFileSettings.StartActionSetting.Program;

            string quote = "\'"; // Use single quote that is cross platform safe
            conf.CsprojUserFile.StartArguments = $@"/sources(@{quote}{string.Join(";", MainSources)}{quote}) {startArguments}";
            conf.CsprojUserFile.StartProgram = DebugProjectExtension.GetSharpmakeExecutableFullPath();
        }
    }

    [Sharpmake.Generate]
    public class DebugSolution : Solution
    {
        public DebugSolution()
            : base(typeof(Target))
        {
            Name = "Sharpmake_DebugSolution";

            AddTargets(DebugProjectGenerator.GetTargets());
        }

        [Configure]
        public virtual void Configure(Configuration conf, Target target)
        {
            conf.SolutionPath = DebugProjectGenerator.RootPath;
            conf.SolutionFileName = "[solution.Name].[target.DevEnv]";

            foreach (var project in DebugProjectGenerator.DebugProjects)
                conf.AddProject(project.Key, target);
        }
    }

    [Sharpmake.Generate]
    public class DebugProject : CSharpProject
    {
        private readonly DebugProjectGenerator.ProjectContent _projectInfo;

        public DebugProject()
            : base(typeof(Target), typeof(Configuration), isInternal: true)
        {
            _projectInfo = DebugProjectGenerator.DebugProjects[GetType()];

            // set paths
            RootPath = _projectInfo.ProjectFolder;
            SourceRootPath = RootPath;

            // add selected source files
            SourceFiles.AddRange(_projectInfo.ProjectFiles);

            // ensure that no file will be automagically added
            SourceFilesExtensions.Clear();
            ResourceFilesExtensions.Clear();
            PRIFilesExtensions.Clear();
            ResourceFiles.Clear();
            NoneExtensions.Clear();
            VsctExtension.Clear();

            Name = _projectInfo.DisplayName;

            // Use the new csproj style
            ProjectSchema = CSharpProjectSchema.NetCore;

            // prevents output dir to have a framework subfolder
            CustomProperties.Add("AppendTargetFrameworkToOutputPath", "false");

            // we need to disable determinism while because we are using wildcards in assembly versions
            // error CS8357: The specified version string contains wildcards, which are not compatible with determinism
            CustomProperties.Add("Deterministic", "false");

            AddTargets(DebugProjectGenerator.GetTargets());
        }

        [Configure]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectPath = RootPath;
            conf.ProjectFileName = "[project.Name].[target.DevEnv]";
            conf.Output = Configuration.OutputType.DotNetClassLibrary;

            conf.DefaultOption = target.Optimization == Optimization.Debug ? Options.DefaultTarget.Debug : Options.DefaultTarget.Release;

            conf.Options.Add(Assembler.SharpmakeScriptsCSharpVersion);

            conf.Defines.Add(_projectInfo.Defines.ToArray());

            foreach (var projectReference in _projectInfo.ProjectReferences)
            {
                conf.AddPrivateDependency(target, projectReference);
            }

            DebugProjectGenerator.DebugProjectExtension.AddReferences(conf, _projectInfo.References);
            DebugProjectGenerator.DebugProjectExtension.AddSharpmakePackage(conf);

            // set up custom configuration only to setup project
            if (_projectInfo.IsSetupProject &&
                FileSystemStringComparer.Default.Equals(conf.ProjectPath, RootPath))
            {
                conf.SetupProjectOptions(_projectInfo.StartArguments);
            }
        }
    }

    [Serializable]
    public class AssemblyVersionException : Exception
    {
        public AssemblyVersionException() : base() { }
        public AssemblyVersionException(string msg) : base(msg) { }

        protected AssemblyVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
