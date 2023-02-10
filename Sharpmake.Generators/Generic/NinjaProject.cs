using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Sharpmake.Generators.Generic
{
    public partial class NinjaProject : IProjectGenerator
    {
        private class GenerationContext : IGenerationContext
        {
            private Dictionary<Project.Configuration, Options.ExplicitOptions> _projectConfigurationOptions;
            private IDictionary<string, string> _cmdLineOptions;
            private IDictionary<string, string> _linkerCmdLineOptions;
            private Resolver _envVarResolver;

            public Builder Builder { get; }
            public string ProjectPath { get; }
            public string ProjectDirectory { get; }
            public string ProjectFileName { get; }
            public string ProjectDirectoryCapitalized { get; }
            public string ProjectSourceCapitalized { get; }
            public bool PlainOutput { get { return true; } }
            public Project Project { get; }

            public Compiler Compiler { get; }

            public Project.Configuration Configuration { get; set; }

            public IReadOnlyDictionary<Project.Configuration, Options.ExplicitOptions> ProjectConfigurationOptions => _projectConfigurationOptions;

            public void SetProjectConfigurationOptions(Dictionary<Project.Configuration, Options.ExplicitOptions> projectConfigurationOptions)
            {
                _projectConfigurationOptions = projectConfigurationOptions;
            }

            public DevEnv DevelopmentEnvironment => Configuration.Target.GetFragment<DevEnv>();
            public DevEnvRange DevelopmentEnvironmentsRange { get; }
            public Options.ExplicitOptions Options
            {
                get
                {
                    Debug.Assert(_projectConfigurationOptions.ContainsKey(Configuration));
                    return _projectConfigurationOptions[Configuration];
                }
            }
            public IDictionary<string, string> CommandLineOptions
            {
                get
                {
                    Debug.Assert(_cmdLineOptions != null);
                    return _cmdLineOptions;
                }
                set
                {
                    _cmdLineOptions = value;
                }
            }
            public IDictionary<string, string> LinkerCommandLineOptions
            {
                get
                {
                    Debug.Assert(_linkerCmdLineOptions != null);
                    return _linkerCmdLineOptions;
                }
                set
                {
                    _linkerCmdLineOptions = value;
                }
            }
            public Resolver EnvironmentVariableResolver
            {
                get
                {
                    Debug.Assert(_envVarResolver != null);
                    return _envVarResolver;
                }
                set
                {
                    _envVarResolver = value;
                }
            }

            public FastBuildMakeCommandGenerator FastBuildMakeCommandGenerator { get; }

            public GenerationContext(Builder builder, string projectPath, Project project, Project.Configuration configuration)
            {
                Builder = builder;

                FileInfo fileInfo = new FileInfo(projectPath);
                ProjectPath = fileInfo.FullName;
                ProjectDirectory = Path.GetDirectoryName(ProjectPath);
                ProjectFileName = Path.GetFileName(ProjectPath);
                Project = project;

                ProjectDirectoryCapitalized = Util.GetCapitalizedPath(ProjectDirectory);
                ProjectSourceCapitalized = Util.GetCapitalizedPath(Project.SourceRootPath);

                Configuration = configuration;
                Compiler = configuration.Target.GetFragment<Compiler>();
            }

            public void Reset()
            {
                CommandLineOptions = null;
                Configuration = null;
                EnvironmentVariableResolver = null;
            }

            public void SelectOption(params Options.OptionAction[] options)
            {
                Sharpmake.Options.SelectOption(Configuration, options);
            }

            public void SelectOptionWithFallback(Action fallbackAction, params Options.OptionAction[] options)
            {
                Sharpmake.Options.SelectOptionWithFallback(Configuration, fallbackAction, options);
            }
        }

        private class CompileStatement
        {
            private string Name;
            private string Input;
            private GenerationContext Context;

            public Strings Defines { get; set; }
            public string DepPath { get; set; }
            public Strings ImplicitCompilerFlags { get; set; }
            public Strings CompilerFlags { get; set; }
            public OrderableStrings Includes { get; set; }
            public OrderableStrings SystemIncludes { get; set; }
            public string TargetFilePath { get; set; }

            public CompileStatement(string name, string input, GenerationContext context)
            {
                Name = name;
                Input = input;
                Context = context;
            }

            public override string ToString()
            {
                var fileGenerator = new FileGenerator();

                string defines = MergeMultipleFlagsToString(Defines, CompilerFlagLookupTable.Get(Context.Compiler, CompilerFlag.Define));
                string implicitCompilerFlags = MergeMultipleFlagsToString(ImplicitCompilerFlags);
                string compilerFlags = MergeMultipleFlagsToString(CompilerFlags);
                string includes = MergeMultipleFlagsToString(Includes, CompilerFlagLookupTable.Get(Context.Compiler, CompilerFlag.Include));
                string systemIncludes = MergeMultipleFlagsToString(SystemIncludes, CompilerFlagLookupTable.Get(Context.Compiler, CompilerFlag.SystemInclude));

                fileGenerator.WriteLine($"{Template.BuildBegin}{Name}: {Template.RuleStatement.CompileCppFile(Context)} {Input}");

                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.Defines(Context)}", defines);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.DepFile(Context)}", $"{DepPath}.d");
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.CompilerImplicitFlags(Context)}", implicitCompilerFlags);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.SystemIncludes(Context)}", systemIncludes);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.CompilerFlags(Context)}", compilerFlags);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.Includes(Context)}", includes);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.TargetPdb(Context)}", TargetFilePath);

                return fileGenerator.ToString();
            }
        }

        private class LinkStatement
        {
            public Strings ObjFilePaths { get; set; }
            public Strings ImplicitLinkerFlags { get; set; }
            public Strings Flags { get; set; }
            public Strings ImplicitLinkerPaths { get; set; }
            public Strings ImplicitLinkerLibs { get; set; }
            public Strings LinkerPaths { get; set; }
            public Strings LinkerLibs { get; set; }
            public string PreBuild { get; set; }
            public string PostBuild { get; set; }
            public string TargetPdb { get; set; }

            private GenerationContext Context;
            private string OutputPath;

            public LinkStatement(GenerationContext context, string outputPath)
            {
                Context = context;
                OutputPath = outputPath;

                PreBuild = "cd .";
                PostBuild = "cd .";
            }

            public override string ToString()
            {
                var fileGenerator = new FileGenerator();

                string objPaths = MergeMultipleFlagsToString(ObjFilePaths);
                string implicitLinkerFlags = MergeMultipleFlagsToString(ImplicitLinkerFlags);
                string implicitLinkerPaths = MergeMultipleFlagsToString(ImplicitLinkerPaths, LinkerFlagLookupTable.Get(Context.Compiler, LinkerFlag.IncludePath));
                string implicitLinkerLibs = MergeMultipleFlagsToString(ImplicitLinkerLibs, LinkerFlagLookupTable.Get(Context.Compiler, LinkerFlag.IncludeFile));
                string linkerFlags = MergeMultipleFlagsToString(Flags);
                string libraryPaths = MergeMultipleFlagsToString(LinkerPaths, LinkerFlagLookupTable.Get(Context.Compiler, LinkerFlag.IncludePath));
                string libraryFiles = MergeMultipleFlagsToString(LinkerLibs, LinkerFlagLookupTable.Get(Context.Compiler, LinkerFlag.IncludeFile));

                fileGenerator.WriteLine($"{Template.BuildBegin}{CreateNinjaFilePath(FullOutputPath(Context))}: {Template.RuleStatement.LinkToUse(Context)} {objPaths}");

                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.LinkerImplicitFlags(Context)}", implicitLinkerFlags);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.ImplicitLinkerPaths(Context)}", implicitLinkerPaths);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.ImplicitLinkerLibraries(Context)}", implicitLinkerLibs);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.LinkerFlags(Context)}", linkerFlags);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.LinkerPaths(Context)}", libraryPaths);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.LinkerLibraries(Context)}", libraryFiles);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.TargetFile(Context)}", OutputPath);
                WriteIfNotEmpty(fileGenerator, $"  {Template.BuildStatement.TargetPdb(Context)}", TargetPdb);
                WriteIfNotEmptyOr(fileGenerator, $"  {Template.BuildStatement.PreBuild(Context)}", PreBuild, "cd .");
                WriteIfNotEmptyOr(fileGenerator, $"  {Template.BuildStatement.PostBuild(Context)}", PostBuild, "cd .");
                ;

                return fileGenerator.ToString();
            }
        }

        private static readonly string ProjectExtension = ".ninja";

        private static string MergeMultipleFlagsToString(Strings options, string perOptionPrefix = "")
        {
            string result = "";
            foreach (var option in options)
            {
                if (option == "REMOVE_LINE_TAG")
                {
                    continue;
                }

                result += $"{perOptionPrefix}{option} ";
            }
            return result;
        }
        private static string MergeMultipleFlagsToString(OrderableStrings options, string perOptionPrefix = "")
        {
            string result = "";
            foreach (var option in options)
            {
                if (option == "REMOVE_LINE_TAG")
                {
                    continue;
                }

                result += $"{perOptionPrefix}\"{option}\" ";
            }
            return result;
        }
        public void Generate(
        Builder builder,
        Project project,
        List<Project.Configuration> configurations,
        string projectFilePath,
        List<string> generatedFiles,
        List<string> skipFiles)
        {
            // The first pass writes ninja files per configuration
            Strings filesToCompile = GetFilesToCompile(project);

            foreach (var config in configurations)
            {
                GenerationContext context = new GenerationContext(builder, projectFilePath, project, config);

                if (config.Output == Project.Configuration.OutputType.Dll && context.Compiler == Compiler.GCC)
                {
                    throw new Error("Shared library for GCC is currently not supported");
                }

                WritePerConfigFile(context, filesToCompile, generatedFiles, skipFiles);
                WriteCompilerDatabaseFile(context);
            }

            // the second pass uses these files to create a project file where the files can be build
            WriteProjectFile(builder, projectFilePath, project, configurations, generatedFiles, skipFiles);
        }

        public void Generate(
            Builder builder,
            Solution solution,
            List<Solution.Configuration> configurations,
            string solutionFile,
            List<string> generatedFiles,
            List<string> skipFiles)
        {
            FileGenerator fileGenerator = new FileGenerator();

            GenerateHeader(fileGenerator);

            fileGenerator.WriteLine($"# Solution for {solution.Name}");

            List<Project> projectsToInclude = new List<Project>();
            foreach (var config in configurations)
            {
                foreach (var projectInfo in config.IncludedProjectInfos)
                {
                    if (projectsToInclude.FindIndex(x => x == projectInfo.Project) == -1)
                    {
                        projectsToInclude.Add(projectInfo.Project);
                    }
                }
            }

            foreach (var project in projectsToInclude)
            {
                string fullProjectPath = FullProjectPath(project);
                fileGenerator.WriteLine($"include {CreateNinjaFilePath(fullProjectPath)}");
            }

            fileGenerator.RemoveTaggedLines();
            MemoryStream memoryStream = fileGenerator.ToMemoryStream();
            FileInfo solutionFileInfo = new FileInfo($"{solutionFile}{Util.GetSolutionExtension(DevEnv.ninja)}");

            if (builder.Context.WriteGeneratedFile(solution.GetType(), solutionFileInfo, memoryStream))
            {
                generatedFiles.Add(solutionFileInfo.FullName);
            }
            else
            {
                skipFiles.Add(solutionFileInfo.FullName);
            }
        }

        private void WriteProjectFile(Builder builder, string projectFilePath, Project project, List<Project.Configuration> configurations, List<string> generatedFiles, List<string> skipFiles)
        {
            List<string> filePaths = new List<string>();
            List<string> dependencyFilePaths = new List<string>();

            foreach (var config in configurations)
            {
                GenerationContext context = new GenerationContext(builder, projectFilePath, project, config);

                foreach (var dependency in config.ResolvedDependencies)
                {
                    string dependencyFilePath = FullProjectPath(dependency.Project);

                    if (!dependencyFilePaths.Contains(dependencyFilePath))
                    {
                        dependencyFilePaths.Add(dependencyFilePath);
                    }
                }

                filePaths.Add(GetPerConfigFilePath(context));
            }

            var fileGenerator = new FileGenerator();

            GenerateHeader(fileGenerator);

            fileGenerator.WriteLine($"# Dependencies");
            foreach (var path in dependencyFilePaths)
            {
                fileGenerator.WriteLine($"include {CreateNinjaFilePath(path)}");
            }

            fileGenerator.WriteLine($"");
            fileGenerator.WriteLine($"# Ninja files");
            foreach (var path in filePaths)
            {
                fileGenerator.WriteLine($"include {CreateNinjaFilePath(path)}");
            }
            fileGenerator.WriteLine($"");

            string fullProjectPath = FullProjectPath(project);

            if (SaveFileGeneratorToDisk(fileGenerator, builder, project, $"{fullProjectPath}"))
            {
                generatedFiles.Add(fullProjectPath);
            }
            else
            {
                skipFiles.Add(fullProjectPath);
            }
        }

        private string FullProjectPath(Project project)
        {
            foreach (var config in project.Configurations)
            {
                if (config.Target.GetFragment<DevEnv>() == DevEnv.ninja)
                {
                    string projectPath = Path.Combine(config.ProjectPath, project.Name);
                    return $"{projectPath}{ProjectExtension}";
                }
            }

            throw new Error("Failed to find project path");
        }

        private void WritePerConfigFile(GenerationContext context, Strings filesToCompile, List<string> generatedFiles, List<string> skipFiles)
        {
            Strings objFilePaths = GetObjPaths(context);

            ResolvePdbPaths(context);
            GenerateConfOptions(context);

            List<CompileStatement> compileStatements = GenerateCompileStatements(context, filesToCompile, objFilePaths);
            List<LinkStatement> linkStatements = GenerateLinking(context, objFilePaths);

            var fileGenerator = new FileGenerator();

            GenerateHeader(fileGenerator);
            GenerateRules(fileGenerator, context);

            fileGenerator.RemoveTaggedLines();

            foreach (var compileStatement in compileStatements)
            {
                fileGenerator.WriteLine(compileStatement.ToString());
            }

            foreach (var linkStatement in linkStatements)
            {
                fileGenerator.WriteLine(linkStatement.ToString());
            }

            GenerateProjectBuilds(fileGenerator, context);

            string filePath = GetPerConfigFilePath(context);

            if (SaveFileGeneratorToDisk(fileGenerator, context, filePath))
            {
                generatedFiles.Add(filePath);
            }
            else
            {
                skipFiles.Add(filePath);
            }
        }

        private void WriteCompilerDatabaseFile(GenerationContext context)
        {
            string outputFolder = Directory.GetParent(GetCompilerDBPath(context)).FullName;
            string outputPath = GetCompilerDBPath(context);
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            else if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            

            string ninjaFilePath = KitsRootPaths.GetNinjaPath();
            string command = $"-f {GetPerConfigFilePath(context)} {Template.CompDBBuildStatement(context)} --quiet >> {outputPath}";

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C {ninjaFilePath} {command}";
            process.StartInfo = startInfo;
            process.Start();
        }

        private string GetPerConfigFileName(GenerationContext context)
        {
            return $"{context.Project.Name}.{context.Configuration.Name}.{context.Compiler}{ProjectExtension}";
        }

        private string GetCompilerDBPath(GenerationContext context)
        {
            return $"{Path.Combine(context.Configuration.ProjectPath, "clang_tools", $"{Template.PerConfigFolderFormat(context)}", "compile_commands.json")}";
        }

        private string GetPerConfigFilePath(GenerationContext context)
        {
            return Path.Combine(context.Configuration.ProjectPath, "ninja", GetPerConfigFileName(context));
        }

        private static void WriteIfNotEmpty(FileGenerator fileGenerator, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                fileGenerator.WriteLine($"{key} = {value}");
            }
        }
        private static void WriteIfNotEmptyOr(FileGenerator fileGenerator, string key, string value, string orValue)
        {
            if (!string.IsNullOrEmpty(value))
            {
                fileGenerator.WriteLine($"{key} = {value}");
            }
            else
            {
                fileGenerator.WriteLine($"{key} = {orValue}");
            }
        }

        private static string FullOutputPath(GenerationContext context)
        {
            string fullFileName = $"{context.Configuration.TargetFileFullName}_{context.Configuration.Name}_{context.Compiler}{context.Configuration.TargetFileFullExtension}";
            return CreateNinjaFilePath($"{Path.Combine(context.Configuration.TargetPath, fullFileName)}");
        }

        private void ResolvePdbPaths(GenerationContext context)
        {
            // Relative pdb filepaths is not supported for ninja generation
            if (context.Configuration.UseRelativePdbPath == true)
            {
                Util.LogWrite("Warning: Configuration.UseRelativePdbPath is not supported for ninja generation");
                context.Configuration.UseRelativePdbPath = false;
            }

            // Resolve pdb filepath so it's sorted per compiler
            context.Configuration.CompilerPdbSuffix = $"{context.Compiler}{context.Configuration.CompilerPdbSuffix}";
            context.Configuration.LinkerPdbSuffix = $"{context.Compiler}{context.Configuration.LinkerPdbSuffix}";

            // Not all compilers generate the directories to pdb files
            CreatePdbPath(context);
        }

        private bool SaveFileGeneratorToDisk(FileGenerator fileGenerator, GenerationContext context, string filePath)
        {
            return SaveFileGeneratorToDisk(fileGenerator, context.Builder, context.Project, filePath);
        }

        private bool SaveFileGeneratorToDisk(FileGenerator fileGenerator, Builder builder, Project project, string filePath)
        {
            MemoryStream memoryStream = fileGenerator.ToMemoryStream();
            FileInfo projectFileInfo = new FileInfo(filePath);
            return builder.Context.WriteGeneratedFile(project.GetType(), projectFileInfo, memoryStream);
        }

        private Strings GetFilesToCompile(Project project)
        {
            Strings filesToCompile = new Strings();

            foreach (var sourceFile in project.SourceFiles)
            {
                string extension = Path.GetExtension(sourceFile);
                if (project.SourceFilesCompileExtensions.Contains(extension))
                {
                    filesToCompile.Add(sourceFile);
                }
            }

            return filesToCompile;
        }
        Strings GetObjPaths(GenerationContext context)
        {
            Strings objFilePaths = new Strings();

            foreach (var sourceFile in context.Project.SourceFiles)
            {
                string extension = Path.GetExtension(sourceFile);
                if (context.Project.SourceFilesCompileExtensions.Contains(extension))
                {
                    string fileStem = Path.GetFileNameWithoutExtension(sourceFile);

                    string outputExtension = context.Configuration.Target.GetFragment<Compiler>() == Compiler.MSVC ? ".obj" : ".o";

                    string objPath = $"{Path.Combine(context.Configuration.IntermediatePath, fileStem)}{outputExtension}";
                    objFilePaths.Add(CreateNinjaFilePath(objPath));
                }
            }

            return objFilePaths;
        }

        private void CreatePdbPath(GenerationContext context)
        {
            if (!Directory.Exists(Path.GetDirectoryName(context.Configuration.LinkerPdbFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(context.Configuration.LinkerPdbFilePath));
            }
        }

        private string GetCompilerPath(GenerationContext context)
        {
            return KitsRootPaths.GetCompilerSettings(context.Compiler).BinPath;
        }

        private string GetLinkerPath(GenerationContext context)
        {
            return context.Configuration.Output == Project.Configuration.OutputType.Lib
                ? KitsRootPaths.GetCompilerSettings(context.Compiler).ArchiverPath
                : KitsRootPaths.GetCompilerSettings(context.Compiler).LinkerPath;
        }

        private void GenerateHeader(FileGenerator fileGenerator)
        {
            fileGenerator.WriteLine($"# !! Sharpmake generated file !!");
            fileGenerator.WriteLine($"# All edits will be overwritten on the next sharpmake run");
            fileGenerator.WriteLine($"#");
            fileGenerator.WriteLine($"# Make sure we have the right version of Ninja");
            fileGenerator.WriteLine($"ninja_required_version = 1.1");
            fileGenerator.WriteLine($"builddir = .ninja");
            fileGenerator.WriteLine($"");
        }

        private void GenerateRules(FileGenerator fileGenerator, GenerationContext context)
        {
            // Compilation
            fileGenerator.WriteLine($"# Rules to specify how to do things");
            fileGenerator.WriteLine($"");
            fileGenerator.WriteLine($"# Rule for compiling C++ files using {context.Compiler}");
            fileGenerator.WriteLine($"{Template.RuleBegin} {Template.RuleStatement.CompileCppFile(context)}");
            fileGenerator.WriteLine($"  depfile = ${Template.BuildStatement.DepFile(context)}");
            fileGenerator.WriteLine($"  deps = gcc");
            fileGenerator.WriteLine($"{Template.CommandBegin}{GetCompilerPath(context)} ${Template.BuildStatement.Defines(context)} ${Template.BuildStatement.SystemIncludes(context)} ${Template.BuildStatement.Includes(context)} ${Template.BuildStatement.CompilerFlags(context)} ${Template.BuildStatement.CompilerImplicitFlags(context)} {Template.Input}");
            fileGenerator.WriteLine($"{Template.DescriptionBegin} Building C++ object $out");
            fileGenerator.WriteLine($"");

            // Linking

            string outputType = context.Configuration.Output == Project.Configuration.OutputType.Exe
                ? "executable"
                : "archive";

            fileGenerator.WriteLine($"# Rule for linking C++ objects");
            fileGenerator.WriteLine($"{Template.RuleBegin}{Template.RuleStatement.LinkToUse(context)}");
            fileGenerator.WriteLine($"{Template.CommandBegin}cmd.exe /C \"${Template.BuildStatement.PreBuild(context)} && {GetLinkerPath(context)} ${Template.BuildStatement.LinkerImplicitFlags(context)} ${Template.BuildStatement.LinkerFlags(context)} ${Template.BuildStatement.ImplicitLinkerPaths(context)} ${Template.BuildStatement.ImplicitLinkerLibraries(context)} ${Template.BuildStatement.LinkerLibraries(context)} $in && ${Template.BuildStatement.PostBuild(context)}\"");
            fileGenerator.WriteLine($"{Template.DescriptionBegin}Linking C++ {outputType} ${Template.BuildStatement.TargetFile(context)}");
            fileGenerator.WriteLine($"  restat = $RESTAT");
            fileGenerator.WriteLine($"");

            // Cleaning
            fileGenerator.WriteLine($"# Rule to clean all built files");
            fileGenerator.WriteLine($"{Template.RuleBegin}{Template.RuleStatement.Clean(context)}");
            fileGenerator.WriteLine($"{Template.CommandBegin}{KitsRootPaths.GetNinjaPath()} -f {GetPerConfigFilePath(context)} -t clean");
            fileGenerator.WriteLine($"{Template.DescriptionBegin}Cleaning all build files");
            fileGenerator.WriteLine($"");

            // Compiler DB
            fileGenerator.WriteLine($"# Rule to generate compiler db");
            fileGenerator.WriteLine($"{Template.RuleBegin}{Template.RuleStatement.CompilerDB(context)}");
            fileGenerator.WriteLine($"{Template.CommandBegin}{KitsRootPaths.GetNinjaPath()} -f {GetPerConfigFilePath(context)} -t compdb {Template.RuleStatement.CompileCppFile(context)}");
            fileGenerator.WriteLine($"");
        }

        private List<CompileStatement> GenerateCompileStatements(GenerationContext context, Strings filesToCompile, Strings objPaths)
        {
            List<CompileStatement> statements = new List<CompileStatement>();

            for (int i = 0; i < filesToCompile.Count; ++i)
            {
                string fileToCompile = filesToCompile.ElementAt(i);
                string objPath = objPaths.ElementAt(i);
                string ninjaFilePath = CreateNinjaFilePath(fileToCompile);

                var compileStatement = new CompileStatement(objPath, ninjaFilePath, context);
                compileStatement.Defines = context.Configuration.Defines;
                compileStatement.DepPath = objPath;
                compileStatement.ImplicitCompilerFlags = GetImplicitCompilerFlags(context, objPath);
                compileStatement.CompilerFlags = GetCompilerFlags(context);
                compileStatement.Includes = context.Configuration.IncludePaths;
                compileStatement.SystemIncludes = context.Configuration.IncludeSystemPaths;
                compileStatement.TargetFilePath = context.Configuration.LinkerPdbFilePath;

                statements.Add(compileStatement);
            }

            return statements;
        }

        private List<LinkStatement> GenerateLinking(GenerationContext context, Strings objFilePaths)
        {
            List<LinkStatement> statements = new List<LinkStatement>();

            string outputPath = FullOutputPath(context);

            var linkStatement = new LinkStatement(context, outputPath);

            linkStatement.ObjFilePaths = objFilePaths;
            linkStatement.ImplicitLinkerFlags = GetImplicitLinkerFlags(context, outputPath);
            linkStatement.Flags = GetLinkerFlags(context);
            linkStatement.ImplicitLinkerPaths = GetImplicitLinkPaths(context);
            linkStatement.LinkerPaths = GetLinkerPaths(context);
            linkStatement.ImplicitLinkerLibs = GetImplicitLinkLibraries(context);
            linkStatement.LinkerLibs = GetLinkLibraries(context);
            linkStatement.PreBuild = GetPreBuildCommands(context);
            linkStatement.PostBuild = GetPostBuildCommands(context);
            linkStatement.TargetPdb = context.Configuration.LinkerPdbFilePath;

            statements.Add(linkStatement);

            return statements;
        }

        private void GenerateProjectBuilds(FileGenerator fileGenerator, GenerationContext context)
        {
            //build app.exe: phony d$:\testing\ninjasharpmake\.rex\build\ninja\app\debug\bin\app.exe
            string phony_name = $"{ context.Configuration.Name }_{ context.Compiler}_{ context.Configuration.TargetFileFullName}".ToLower();
            fileGenerator.WriteLine($"{Template.BuildBegin}{phony_name}: phony {FullOutputPath(context)}");
            fileGenerator.WriteLine($"{Template.BuildBegin}{Template.CleanBuildStatement(context)}: {Template.RuleStatement.Clean(context)}");
            fileGenerator.WriteLine($"{Template.BuildBegin}{Template.CompDBBuildStatement(context)}: {Template.RuleStatement.CompilerDB(context)}");
            fileGenerator.WriteLine($"");

            fileGenerator.WriteLine($"default {phony_name}");
        }

        private static string CreateNinjaFilePath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                return path;
            }

            // filepaths are absolute and Ninja doesn't support a ':' in a path
            // We need to prepend '$' to the ':' to make sure Ninja parses it correctly
            string driveLetter = path.Substring(0, 1);
            string filePathWithoutDriveLetter = path.Substring(1);
            return $"{driveLetter}${filePathWithoutDriveLetter}";

        }

        // subtract all compiler options from the config and translate them to compiler specific flags
        private Strings GetImplicitCompilerFlags(GenerationContext context, string ninjaObjPath)
        {
            Strings flags = new Strings();
            switch (context.Configuration.Target.GetFragment<Compiler>())
            {
                case Compiler.MSVC:
                    flags.Add("/nologo"); // supress copyright banner in compiler
                    flags.Add("/TP"); // treat all files on command line as C++ files
                    flags.Add(" /c"); // don't auto link
                    flags.Add($" /Fo\"{ninjaObjPath}\""); // obj output path
                    flags.Add($" /FS"); // force async pdb generation
                    break;
                case Compiler.Clang:
                    flags.Add(" -c"); // don't auto link
                    flags.Add($" -o\"{ninjaObjPath}\""); // obj output path
                    break;
                case Compiler.GCC:
                    flags.Add(" -D_M_X64"); // used in corecrt_stdio_config.h
                    flags.Add($" -o\"{ninjaObjPath}\""); // obj output path
                    flags.Add(" -c"); // don't auto link
                    break;
                default:
                    throw new Error("Unknown Compiler used for implicit compiler flags");
            }

            if (context.Configuration.Output == Project.Configuration.OutputType.Dll)
            {
                if (PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform).HasSharedLibrarySupport)
                {
                    flags.Add($" {CompilerFlagLookupTable.Get(context.Compiler, CompilerFlag.Define)}_WINDLL");
                }
            }

            int index = context.Configuration.Options.FindIndex(x => x.GetType() == typeof(Options.Vc.Compiler.CppLanguageStandard));
            if (index != -1)
            {
                object option = context.Configuration.Options[index];
                Options.Vc.Compiler.CppLanguageStandard cppStandard = (Options.Vc.Compiler.CppLanguageStandard)option;

                //switch (cppStandard)
                //{
                //    case Options.Vc.Compiler.CppLanguageStandard.CPP14:
                //        flags.Add($" {CompilerFlagLookupTable.Get(context.Compiler, CompilerFlag.Define)}_MSVC_LANG=201402L");
                //        break;
                //    case Options.Vc.Compiler.CppLanguageStandard.CPP17:
                //        flags.Add($" {CompilerFlagLookupTable.Get(context.Compiler, CompilerFlag.Define)}_MSVC_LANG=201703L");
                //        break;
                //    case Options.Vc.Compiler.CppLanguageStandard.CPP20:
                //        flags.Add($" {CompilerFlagLookupTable.Get(context.Compiler, CompilerFlag.Define)}_MSVC_LANG=202002L");
                //        break;
                //    case Options.Vc.Compiler.CppLanguageStandard.Latest:
                //        flags.Add($" {CompilerFlagLookupTable.Get(context.Compiler, CompilerFlag.Define)}_MSVC_LANG=202004L");
                //        break;
                //    default:
                //        flags.Add($" {CompilerFlagLookupTable.Get(context.Compiler, CompilerFlag.Define)}_MSVC_LANG=201402L");
                //        break;
                //}
            }

            return flags;
        }
        private Strings GetImplicitLinkerFlags(GenerationContext context, string outputPath)
        {
            Strings flags = new Strings();
            switch (context.Configuration.Target.GetFragment<Compiler>())
            {
                case Compiler.MSVC:
                    flags.Add("/nologo"); // supress copyright banner in compiler
                    flags.Add($" /OUT:{outputPath}"); // Output file
                    if (context.Configuration.Output == Project.Configuration.OutputType.Dll)
                    {
                        flags.Add(" /dll");
                    }
                    break;
                case Compiler.Clang:
                    if (context.Configuration.Output != Project.Configuration.OutputType.Lib)
                    {
                        flags.Add(" -fuse-ld=lld-link"); // use the llvm lld linker
                        flags.Add(" -nostartfiles"); // Do not use the standard system startup files when linking
                        flags.Add(" -nostdlib"); // Do not use the standard system startup files or libraries when linking
                        flags.Add($" -o {outputPath}"); // Output file
                        if (context.Configuration.Output == Project.Configuration.OutputType.Dll)
                        {
                            flags.Add(" -shared");
                        }
                    }
                    else
                    {
                        flags.Add(" qc");
                        flags.Add($" {outputPath}"); // Output file
                    }
                    break;
                case Compiler.GCC:
                    //flags += " -fuse-ld=lld"; // use the llvm lld linker
                    //flags.Add(" -nostdlib"); // Do not use the standard system startup files or libraries when linking
                    if (context.Configuration.Output != Project.Configuration.OutputType.Exe)
                    {
                        flags.Add($"qc {outputPath}"); // Output file
                    }
                    else
                    {
                        flags.Add($"-o {outputPath}"); // Output file
                    }
                    if (context.Configuration.Output == Project.Configuration.OutputType.Dll)
                    {
                        flags.Add(" -shared");
                    }
                    break;
                default:
                    throw new Error("Unknown Compiler used for implicit linker flags");
            }

            return flags;
        }

        private Strings GetImplicitLinkPaths(GenerationContext context)
        {
            Strings linkPath = new Strings();

            if (context.Configuration.Output != Project.Configuration.OutputType.Lib)
            {

                switch (context.Configuration.Target.GetFragment<Compiler>())
                {
                    case Compiler.MSVC:
                        linkPath.Add("\"D:/Tools/MSVC/install/14.29.30133/lib/x64\"");
                        linkPath.Add("\"D:/Tools/MSVC/install/14.29.30133/atlmfc/lib/x64\"");
                        linkPath.Add("\"D:/Tools/Windows SDK/10.0.19041.0/lib/ucrt/x64\"");
                        linkPath.Add("\"D:/Tools/Windows SDK/10.0.19041.0/lib/um/x64\"");
                        break;
                    case Compiler.Clang:
                        linkPath.Add("\"D:/Tools/MSVC/install/14.29.30133/lib/x64\"");
                        linkPath.Add("\"D:/Tools/MSVC/install/14.29.30133/atlmfc/lib/x64\"");
                        linkPath.Add("\"D:/Tools/Windows SDK/10.0.19041.0/lib/ucrt/x64\"");
                        linkPath.Add("\"D:/Tools/Windows SDK/10.0.19041.0/lib/um/x64\"");
                        break;
                    case Compiler.GCC:
                        //linkPath.Add("\"D:/Tools/MSVC/install/14.29.30133/lib/x64\"");
                        //linkPath.Add("\"D:/Tools/MSVC/install/14.29.30133/atlmfc/lib/x64\"");
                        //linkPath.Add("\"D:/Tools/Windows SDK/10.0.19041.0/lib/ucrt/x64\"");
                        //linkPath.Add("\"D:/Tools/Windows SDK/10.0.19041.0/lib/um/x64\"");
                        break;
                }
            }

            return linkPath;
        }

        private Strings GetImplicitLinkLibraries(GenerationContext context)
        {
            Strings linkLibraries = new Strings();

            if (context.Configuration.Output == Project.Configuration.OutputType.Lib)
                return linkLibraries;

            switch (context.Configuration.Target.GetFragment<Compiler>())
            {
                case Compiler.MSVC:
                    linkLibraries.Add("kernel32.lib");
                    linkLibraries.Add("user32.lib");
                    linkLibraries.Add("gdi32.lib");
                    linkLibraries.Add("winspool.lib");
                    linkLibraries.Add("shell32.lib");
                    linkLibraries.Add("ole32.lib");
                    linkLibraries.Add("oleaut32.lib");
                    linkLibraries.Add("uuid.lib");
                    linkLibraries.Add("comdlg32.lib");
                    linkLibraries.Add("advapi32.lib");
                    linkLibraries.Add("oldnames.lib");
                    break;
                case Compiler.Clang:
                    linkLibraries.Add("kernel32");
                    linkLibraries.Add("user32");
                    linkLibraries.Add("gdi32");
                    linkLibraries.Add("winspool");
                    linkLibraries.Add("shell32");
                    linkLibraries.Add("ole32");
                    linkLibraries.Add("oleaut32");
                    linkLibraries.Add("uuid");
                    linkLibraries.Add("comdlg32");
                    linkLibraries.Add("advapi32");
                    linkLibraries.Add("oldnames");
                    linkLibraries.Add("libcmt.lib");
                    break;
                case Compiler.GCC:
                    //linkLibraries.Add("kernel32");
                    //linkLibraries.Add("user32");
                    //linkLibraries.Add("gdi32");
                    //linkLibraries.Add("winspool");
                    //linkLibraries.Add("shell32");
                    //linkLibraries.Add("ole32");
                    //linkLibraries.Add("oleaut32");
                    //linkLibraries.Add("uuid");
                    //linkLibraries.Add("comdlg32");
                    //linkLibraries.Add("advapi32");
                    //linkLibraries.Add("oldnames");
                    break;
            }

            return linkLibraries;
        }

        private Strings GetCompilerFlags(GenerationContext context)
        {
            return new Strings(context.CommandLineOptions.Values);
        }

        private Strings GetLinkerPaths(GenerationContext context)
        {
            return new Strings(context.Configuration.LibraryPaths);
        }
        private Strings GetLinkLibraries(GenerationContext context)
        {
            return new Strings(context.Configuration.LibraryFiles);
        }

        private string GetPreBuildCommands(GenerationContext context)
        {
            string preBuildCommand = "";
            string suffix = " && ";

            foreach (var command in context.Configuration.EventPreBuild)
            {
                preBuildCommand += command;
                preBuildCommand += suffix;
            }

            // remove trailing && if possible
            if (preBuildCommand.EndsWith(suffix))
            {
                preBuildCommand = preBuildCommand.Substring(0, preBuildCommand.Length - suffix.Length);
            }

            return preBuildCommand;
        }

        private string GetPostBuildCommands(GenerationContext context)
        {
            string postBuildCommand = "";
            string suffix = " && ";

            foreach (var command in context.Configuration.EventPostBuild)
            {
                postBuildCommand += command;
                postBuildCommand += suffix;
            }

            // remove trailing && if possible
            if (postBuildCommand.EndsWith(suffix))
            {
                postBuildCommand = postBuildCommand.Substring(0, postBuildCommand.Length - suffix.Length);
            }

            return postBuildCommand;
        }

        private Strings GetLinkerFlags(GenerationContext context)
        {
            Strings flags = new Strings(context.LinkerCommandLineOptions.Values);

            // If we're making an archive, not all linker flags are supported
            switch (context.Compiler)
            {
                case Compiler.MSVC:
                    return FilterMsvcLinkerFlags(flags, context);
                case Compiler.Clang:
                    return FilterClangLinkerFlags(flags, context);
                case Compiler.GCC:
                    return FilterGccLinkerFlags(flags, context);
                default:
                    throw new Error($"Not linker flag filtering implemented for compiler {context.Compiler}");
            }
        }

        private Strings FilterMsvcLinkerFlags(Strings flags, GenerationContext context)
        {
            switch (context.Configuration.Output)
            {
                case Project.Configuration.OutputType.Exe:
                    break;
                case Project.Configuration.OutputType.Lib:
                    RemoveIfContains(flags, "/INCREMENTAL");
                    RemoveIfContains(flags, "/DYNAMICBASE");
                    RemoveIfContains(flags, "/DEBUG");
                    RemoveIfContains(flags, "/PDB");
                    RemoveIfContains(flags, "/LARGEADDRESSAWARE");
                    RemoveIfContains(flags, "/OPT:REF");
                    RemoveIfContains(flags, "/OPT:ICF");
                    RemoveIfContains(flags, "/OPT:NOREF");
                    RemoveIfContains(flags, "/OPT:NOICF");
                    RemoveIfContains(flags, "/FUNCTIONPADMIN");
                    break;
                case Project.Configuration.OutputType.Dll:
                default:
                    break;
            }

            return flags;
        }
        private Strings FilterClangLinkerFlags(Strings flags, GenerationContext context)
        {
            switch (context.Configuration.Output)
            {
                case Project.Configuration.OutputType.Exe:
                    break;
                case Project.Configuration.OutputType.Lib:
                    break;
                case Project.Configuration.OutputType.Dll:
                    break;
                default:
                    break;
            }

            return flags;
        }
        private Strings FilterGccLinkerFlags(Strings flags, GenerationContext context)
        {
            switch (context.Configuration.Output)
            {
                case Project.Configuration.OutputType.Exe:
                    break;
                case Project.Configuration.OutputType.Lib:
                    break;
                case Project.Configuration.OutputType.Dll:
                    break;
                default:
                    break;
            }

            return flags;
        }

        private void RemoveIfContains(Strings flags, string value)
        {
            flags.RemoveAll(x => x.StartsWith(value));
        }

        private void GenerateConfOptions(GenerationContext context)
        {
            // generate all configuration options once...
            var projectOptionsGen = new GenericProjectOptionsGenerator();
            var projectConfigurationOptions = new Dictionary<Project.Configuration, Options.ExplicitOptions>();
            context.SetProjectConfigurationOptions(projectConfigurationOptions);

            // set generator information
            var configurationTasks = PlatformRegistry.Get<Project.Configuration.IConfigurationTasks>(context.Configuration.Platform);
            context.Configuration.GeneratorSetOutputFullExtensions(
                configurationTasks.GetDefaultOutputFullExtension(Project.Configuration.OutputType.Exe),
                configurationTasks.GetDefaultOutputFullExtension(Project.Configuration.OutputType.Exe),
                configurationTasks.GetDefaultOutputFullExtension(Project.Configuration.OutputType.Dll),
                ".pdb");

            projectConfigurationOptions.Add(context.Configuration, new Options.ExplicitOptions());
            context.CommandLineOptions = new GenericProjectOptionsGenerator.GenericCmdLineOptions();
            context.LinkerCommandLineOptions = new GenericProjectOptionsGenerator.GenericCmdLineOptions();

            projectOptionsGen.GenerateOptions(context);
        }
    }
}
