// Copyright (c) 2017 Ubisoft Entertainment
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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;


namespace Sharpmake
{
    /// <summary>
    /// Generates debug projects and solutions
    /// </summary>
    public static class DebugProjectGenerator
    {
        internal static string RootPath { get; private set; }
        internal static string[] MainSources { get; private set; }

        /// <summary>
        /// Generates debug projects and solutions
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="arguments"></param>
        public static void GenerateDebugSolution(string[] sources, Sharpmake.Arguments arguments)
        {
            FindAllSources(sources);
            arguments.Generate<DebugSolution>();
        }

        internal class ProjectContent
        {
            public string ProjectFolder;
            public readonly HashSet<string> ProjectFiles = new HashSet<string>();

            public readonly List<string> References = new List<string>();
            public readonly List<Type> ProjectReferences = new List<Type>();
        }
        internal static readonly Dictionary<Type, ProjectContent> DebugProjects = new Dictionary<Type, ProjectContent>();

        private static void FindAllSources(string[] sourcesArguments)
        {
            MainSources = sourcesArguments;
            RootPath = Path.GetDirectoryName(sourcesArguments[0]);

            Assembler assembler = new Assembler();
            List<string> allsources = assembler.GetSourceFiles(MainSources);

            ProjectContent project = new ProjectContent { ProjectFolder = RootPath };
            DebugProjects.Add(CreateProject("sharpmake_debug"), project);

            // Add sources
            foreach (var source in allsources)
            {
                project.ProjectFiles.Add(source);
            }

            // Add references
            var references = new HashSet<string>();
            if (assembler.UseDefaultReferences)
            {
                foreach (string defaultReference in Assembler.DefaultReferences)
                    references.Add(Assembler.GetAssemblyDllPath(defaultReference));
            }

            foreach (var assemblerRef in assembler.References)
                references.Add(assemblerRef);

            project.References.AddRange(references);

        }

        private static Type CreateProject(string typeSignature)
        {
            // define class type
            var assemblyName = new AssemblyName(typeSignature);
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DebugSharpmakeModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeSignature,
                TypeAttributes.Public | TypeAttributes.Class |
                TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
                TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
                typeof(DebugProject));

            // add attribute [Sharpmake.Generate]
            Type[] generateAttrParams = new Type[] { };
            ConstructorInfo generateAttrCtorInfo = typeof(Sharpmake.Generate).GetConstructor(generateAttrParams);
            CustomAttributeBuilder generateAttrBuilder = new CustomAttributeBuilder(generateAttrCtorInfo, new object[] { });
            typeBuilder.SetCustomAttribute(generateAttrBuilder);

            return typeBuilder.CreateType();
        }

        internal static Target GetTargets()
        {
            return new Target(Platform.anycpu, DevEnv.vs2015 | DevEnv.vs2017, Optimization.Debug,
                OutputType.Dll, Blob.NoBlob, BuildSystem.MSBuild, DotNetFramework.v4_5);
        }

        private static string s_sharpmakePackageName;
        private static string s_sharpmakePackageVersion;
        private static string s_sharpmakeDllPath;
        private static string s_sharpmakeGeneratorDllPath;
        private static string s_sharpmakeApplicationExePath;
        private static bool s_useLocalSharpmake = false;
        private static readonly Regex s_assemblyVersionRegex = new Regex(@"([^\s]+)(?:\s*\((.+)\))?", RegexOptions.Compiled);

        /// <summary>
        /// Add references to Sharpmake to given configuration.
        /// </summary>
        /// <param name="conf"></param>
        public static void AddSharpmakePackage(Project.Configuration conf)
        {
            if (s_sharpmakePackageName == null || s_sharpmakePackageVersion == null)
            {
                Assembly sharpmakeAssembly = Assembly.GetExecutingAssembly();
                string assemblyProductName = sharpmakeAssembly.GetName().Name;
                string assemblyProductVersion = FileVersionInfo.GetVersionInfo(sharpmakeAssembly.Location).ProductVersion;

                var match = s_assemblyVersionRegex.Match(assemblyProductVersion);
                if (match == null || match.Groups.Count < 3)
                    throw new AssemblyVersionException($"Sharpmake assembly version '{assemblyProductVersion}' is not valid.\nFormat should be '1.2.3.4 [(variationName)]'.");

                s_sharpmakePackageVersion = match.Groups[1].Value;
                string assemblyProductVariation = match.Groups[2].Value;
                s_sharpmakePackageName = $"{assemblyProductName}";
                if (!string.IsNullOrWhiteSpace(assemblyProductVariation))
                    s_sharpmakePackageName += $"-{assemblyProductVariation}";

                if (assemblyProductVariation == "LocalBuild")
                {
                    // debug solution generated from local build
                    s_useLocalSharpmake = true;

                    s_sharpmakeDllPath = sharpmakeAssembly.Location;
                    s_sharpmakeGeneratorDllPath = Assembly.Load("Sharpmake.Generators")?.Location;
                }

                s_sharpmakeApplicationExePath = Process.GetCurrentProcess().MainModule.FileName;

                if (Util.IsRunningInMono())
                {
                    // When running within Mono, s_sharpmakeApplicationExePath will at this point wrongly refer to the
                    // mono (or mono-sgen) executable. Fix it so that it points to Sharpmake.Application.exe.
                    s_sharpmakeApplicationExePath = $"{AppDomain.CurrentDomain.BaseDirectory}{AppDomain.CurrentDomain.FriendlyName}";
                }
            }

            conf.ReferencesByPath.Add(Assembler.DefaultReferences);

            if (s_useLocalSharpmake)
            {
                conf.ReferencesByPath.Add(s_sharpmakeDllPath);
                conf.ReferencesByPath.Add(s_sharpmakeGeneratorDllPath);
            }
            else
                conf.ReferencesByNuGetPackage.Add(s_sharpmakePackageName, s_sharpmakePackageVersion);
        }

        /// <summary>
        /// Set up debug configuration in user file
        /// </summary>
        /// <param name="conf"></param>
        public static void SetupProjectOptions(this Project.Configuration conf)
        {
            conf.CsprojUserFile = new Project.Configuration.CsprojUserFileSettings();
            conf.CsprojUserFile.StartAction = Project.Configuration.CsprojUserFileSettings.StartActionSetting.Program;
            string quote = Util.IsRunningInMono() ? @"\""" : @""""; // When running in Mono, we must escape "
            conf.CsprojUserFile.StartArguments = $@"/sources(@{quote}{string.Join(";", MainSources)}{quote})";
            conf.CsprojUserFile.StartProgram = s_sharpmakeApplicationExePath;
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

        [Configure()]
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
        public DebugProject()
            : base(typeof(Target))
        {
            // set paths
            RootPath = DebugProjectGenerator.DebugProjects[GetType()].ProjectFolder;
            SourceRootPath = RootPath;

            // add selected source files
            SourceFiles.AddRange(DebugProjectGenerator.DebugProjects[GetType()].ProjectFiles);

            // ensure that no file will be automagically added
            SourceFilesExtensions.Clear();
            ResourceFilesExtensions.Clear();
            PRIFilesExtensions.Clear();
            ResourceFiles.Clear();
            NoneExtensions.Clear();

            AddTargets(DebugProjectGenerator.GetTargets());
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectPath = RootPath;
            conf.ProjectFileName = "[project.Name].[target.DevEnv]";
            conf.Output = Configuration.OutputType.DotNetClassLibrary;

            DebugProjectGenerator.AddSharpmakePackage(conf);

            conf.Options.Add(Options.CSharp.LanguageVersion.CSharp5);

            conf.ReferencesByPath.AddRange(DebugProjectGenerator.DebugProjects[GetType()].References);
            foreach (var projectReference in DebugProjectGenerator.DebugProjects[GetType()].ProjectReferences)
            {
                conf.AddPrivateDependency(target, projectReference);
            }

            // set up custom configuration only to setup project
            if (string.CompareOrdinal(conf.ProjectPath.ToLower(), RootPath.ToLower()) == 0)
                conf.SetupProjectOptions();
        }
    }

    [Serializable]
    public class AssemblyVersionException : Exception
    {
        public AssemblyVersionException() : base() { }
        public AssemblyVersionException(string msg) : base(msg) { }

        protected AssemblyVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
