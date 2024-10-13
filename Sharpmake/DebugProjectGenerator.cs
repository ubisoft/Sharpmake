// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        internal static DevEnv DevEnv { get; private set; }
        internal static readonly DevEnv DefaultDevEnv = DevEnv.vs2022;

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

                if (Util.IsRunningInMono() || Util.GetExecutingPlatform() == Platform.mac)
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
            GenerateDebugSolution(sources, null, arguments, startArguments, DefaultDevEnv);
        }

        /// <summary>
        /// Generates debug projects and solutions
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="arguments"></param>
        /// <param name="startArguments"></param>
        /// <param name="defines"></param>
        [Obsolete("Defines should be inserted in the Sharpmake.Arguments parameter thus rendering this function useless ", error: true)]
        public static void GenerateDebugSolution(string[] sources, Arguments arguments, string startArguments, string[] defines)
        {
            GenerateDebugSolution(sources, null, arguments, startArguments, DefaultDevEnv);
        }

        /// <summary>
        /// Generates debug projects and solutions
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="solutionPath"></param>
        /// <param name="arguments"></param>
        /// <param name="startArguments"></param>
        internal static void GenerateDebugSolution(string[] sources, string solutionPath, Arguments arguments, string startArguments)
        {
            GenerateDebugSolution(sources, solutionPath, arguments, startArguments, DefaultDevEnv);
        }

        /// <summary>
        /// Generates debug projects and solutions
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="solutionPath"></param>
        /// <param name="arguments"></param>
        /// <param name="startArguments"></param>
        /// <param name="devEnv"></param>
        internal static void GenerateDebugSolution(string[] sources, string solutionPath, Arguments arguments, string startArguments, DevEnv devEnv)
        {
            DevEnv = devEnv;
            FindAllSources(sources, solutionPath, arguments, startArguments);
            arguments.Generate<DebugSolution>();
        }

        internal class ProjectContent
        {
            public string DisplayName;

            public string ProjectFolder;
            public readonly HashSet<string> ProjectFiles = new HashSet<string>();
            public readonly HashSet<string> ProjectNoneFiles = new HashSet<string>();

            public readonly List<string> References = new List<string>();
            public readonly List<Type> ProjectReferences = new List<Type>();

            public readonly List<string> Defines = new List<string>();

            public bool IsSetupProject;

            public string StartArguments;
        }
        internal static readonly Dictionary<Type, ProjectContent> DebugProjects = new Dictionary<Type, ProjectContent>();

        private static void FindAllSources(string[] sourcesArguments, string solutionPath, Sharpmake.Arguments sharpmakeArguments, string startArguments)
        {
            MainSources = sourcesArguments;
            if (!string.IsNullOrEmpty(solutionPath))
            {
                RootPath = solutionPath;
                if (!Path.IsPathRooted(RootPath))
                {
                    RootPath = Path.Combine(Directory.GetCurrentDirectory(), RootPath);
                }
                Directory.CreateDirectory(RootPath);
            }
            else
            {
                RootPath = Path.GetDirectoryName(sourcesArguments[0]);
            }

            Assembler assembler = new Assembler(sharpmakeArguments.Builder.Defines);
            assembler.AttributeParsers.Add(new DebugProjectNameAttributeParser());
            IAssemblyInfo assemblyInfo = assembler.LoadUncompiledAssemblyInfo(Builder.Instance.CreateContext(BuilderCompileErrorBehavior.ReturnNullAssembly), MainSources);

            GenerateDebugProject(assemblyInfo, true, startArguments, new Dictionary<string, Type>(), sharpmakeArguments.Builder.Defines.ToArray());
        }

        private static Type GenerateDebugProject(IAssemblyInfo assemblyInfo, bool isSetupProject, string startArguments, IDictionary<string, Type> visited, string[] defines)
        {
            string displayName = assemblyInfo.DebugProjectName;
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = isSetupProject ? "sharpmake_debug" : $"sharpmake_package_{assemblyInfo.Id.GetDeterministicHashCode():X8}";
            }

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
            
            // Add files in project that aren't meant to be compiled
            project.ProjectNoneFiles.UnionWith(assemblyInfo.NoneFiles);


            // Add references
            var references = new HashSet<string>();
            foreach (var assemblerRef in assemblyInfo.RuntimeReferences)
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
                DevEnv,
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
            conf.CsprojUserFile.StartArguments = $@"/sources(@{quote}{string.Join($"{quote},@{quote}", MainSources)}{quote}) {startArguments}";
            conf.CsprojUserFile.StartProgram = DebugProjectExtension.GetSharpmakeExecutableFullPath();
            conf.CsprojUserFile.WorkingDirectory = Directory.GetCurrentDirectory();
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

            PreserveLinkFolderPaths = true;

            // set paths
            RootPath = Util.FindCommonRootPath(_projectInfo.ProjectFiles.Select(f => Path.GetDirectoryName(f)).Distinct()) ?? _projectInfo.ProjectFolder;
            SourceRootPath = RootPath;

            // add selected source files
            SourceFiles.AddRange(_projectInfo.ProjectFiles);
            NoneFiles.AddRange(_projectInfo.ProjectNoneFiles);
            
            // ensure that no file will be automagically added
            SourceFilesExtensions.Clear();
            ResourceFilesExtensions.Clear();
            PRIFilesExtensions.Clear();
            ResourceFiles.Clear();
            NoneExtensions.Clear();
            VsctExtension.Clear();

            // nor removed
            SourceFilesExcludeRegex.Clear();

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
            conf.ProjectPath = _projectInfo.ProjectFolder;
            conf.ProjectFileName = "[project.Name].[target.DevEnv]";
            conf.Output = Configuration.OutputType.DotNetClassLibrary;

            conf.DefaultOption = target.Optimization == Optimization.Debug ? Options.DefaultTarget.Debug : Options.DefaultTarget.Release;

            conf.Options.Add(Assembler.SharpmakeScriptsCSharpVersion);

            // Suppress assembly redirect warnings: https://github.com/dotnet/roslyn/issues/19640
            // Also suppress NuGet downgrade warnings, as this is not MsBuild that drive how Sharpmake load its assemblies.
            conf.Options.Add(
                new Options.CSharp.SuppressWarning(
                    "CS1701",
                    "CS1702",
                    "NU1605"
                )
            );

            conf.Defines.Add(_projectInfo.Defines.ToArray());

            foreach (var projectReference in _projectInfo.ProjectReferences)
            {
                conf.AddPrivateDependency(target, projectReference);
            }

            DebugProjectGenerator.DebugProjectExtension.AddReferences(conf, _projectInfo.References);
            DebugProjectGenerator.DebugProjectExtension.AddSharpmakePackage(conf);

            // set up custom configuration only to setup project
            if (_projectInfo.IsSetupProject)
            {
                conf.SetupProjectOptions(_projectInfo.StartArguments);
            }
        }

        /// <summary>
        /// Get the link folder for a file considering that the path is relative to the debug project folder but that we want
        /// the link to represent the path relative to the SourceRootPath. 
        /// </summary>
        public override string GetLinkFolder(string file)
        {
            string absolutePath = Path.IsPathFullyQualified(file) ? file : Path.GetFullPath(Path.Combine(DebugProjectGenerator.RootPath, file));
            
            string relativePath = Util.PathGetRelative(SourceRootPath, Path.GetDirectoryName(absolutePath));
            
            // Remove the root, if it exists.
            // This will only happen if file is rooted *and* doesn't share the same root as SourceRootPath.
            if (Path.IsPathRooted(relativePath))
            {
                relativePath = relativePath.Substring(Path.GetPathRoot(relativePath).Length);
            }
            
            // If the relative path is elsewhere, we leave the file in the root.
            if (relativePath.Contains(".."))
            {
                return string.Empty;
            }
            
            return relativePath;
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
