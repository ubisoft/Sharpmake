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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators.JsonCompilationDatabase
{
    public class JsonCompilationDatabase : IProjectGenerator, ISolutionGenerator
    {
        public const string FileName = "compile_commands.json";

        public event Action<IGenerationContext, CompileCommand> CompileCommandGenerated;

        public void Generate(Builder builder, Solution solution, List<Solution.Configuration> configurations, string solutionFile, List<string> generatedFiles, List<string> skipFiles)
        {
            if (configurations.Count > 1)
            {
                builder.LogWarningLine("CompilationDatabase: Ignoring {0} configurations.", configurations.Count - 1);
            }

            var sConfig = configurations.First();

            var projects = sConfig.IncludedProjectInfos.Select(pi => pi.Project);

            var database = new List<IDictionary<string, string>>();

            foreach (var project in projects)
            {
                var config = project.Configurations.FirstOrDefault(c => c.Target.IsEqualTo(sConfig.Target));
                if (config == null)
                {
                    continue;
                }
                database.AddRange(GetProjectEntries(builder, project, config));
            }

            WriteGeneratedFile(builder, solution.GetType(), solutionFile, database, generatedFiles, skipFiles);
        }

        public void Generate(Builder builder, Project project, List<Project.Configuration> configurations, string projectFile, List<string> generatedFiles, List<string> skipFiles)
        {
            if (configurations.Count > 1)
            {
                builder.LogWarningLine("CompilationDatabase: Ignoring {0} configurations.", configurations.Count - 1);
            }

            var config = configurations.First();

            var database = GetProjectEntries(builder, project, config);

            WriteGeneratedFile(builder, project.GetType(), projectFile, database, generatedFiles, skipFiles);
        }

        private void WriteGeneratedFile(Builder builder, Type type, string path, IEnumerable<IDictionary<string, string>> database, List<string> generatedFiles, List<string> skipFiles)
        {
            var file = new FileInfo(Path.Combine(Path.GetDirectoryName(path), FileName));

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            using (var serializer = new Util.JsonSerializer(writer) { IsOutputFormatted = true })
            {
                serializer.Serialize(database);
                serializer.Flush();

                if (builder.Context.WriteGeneratedFile(type, file, stream))
                {
                    generatedFiles.Add(path);
                }
                else
                {
                    skipFiles.Add(path);
                }
            }
        }

        private IEnumerable<IDictionary<string, string>> GetProjectEntries(Builder builder, Project project, Project.Configuration config)
        {
            var context = new CompileCommandGenerationContext(builder, project, config);
            var resolverParams = new[] {
                    new VariableAssignment("project", context.Project),
                    new VariableAssignment("target", context.Configuration.Target),
                    new VariableAssignment("conf", context.Configuration)
            };
            context.EnvironmentVariableResolver = PlatformRegistry.Get<IPlatformDescriptor>(config.Platform).GetPlatformEnvironmentResolver(resolverParams);

            var factory = new CompileCommandFactory(context);

            var database = project.GetSourceFilesForConfigurations(new[] { config })
                .Except(config.ResolvedSourceFilesBuildExclude)
                .Where(f => project.SourceFilesCPPExtensions.Contains(Path.GetExtension(f)))
                .Select(factory.CreateCompileCommand);

            foreach (var cc in database)
            {
                CompileCommandGenerated?.Invoke(context, cc);
                yield return new Dictionary<string, string>
                {
                    { "directory", cc.Directory },
                    { "command", cc.Command },
                    { "file", cc.File },
                };
            }
        }
    }

    public class CompileCommand
    {
        public string File { get; set; }
        public string Directory { get; set; }
        public string Command { get; set; }
    }

    internal class CompileCommandFactory
    {
        private const string PrecompNameKey = "PrecompiledHeaderThrough";
        private const string PrecompFileKey = "PrecompiledHeaderFile";
        private const string AdditionalOptionsKey = "AdditionalCompilerOptions";

        private static readonly string[] s_ignoredOptions = new[] {
            PrecompFileKey,
            PrecompNameKey,
            "IntermediateDirectory",
            "AdditionalDependencies",
            "AdditionalResourceIncludeDirectories",
            "TreatWarningAsError"
        };

        private static readonly string[] s_multilineArgumentKeys = new[] {
            "AdditionalLibraryDirectories",
            "PreprocessorDefinitions",
            "ManifestInputs"
        };

        private enum CompilerFlags
        {
            OutputFile,
            UsePrecomp,
            CreatePrecomp,
            PrecompPath,
        }

        private static readonly IDictionary<CompilerFlags, string> s_clangFlags = new Dictionary<CompilerFlags, string>
        {
            { CompilerFlags.OutputFile, "-o {0}" },
            { CompilerFlags.UsePrecomp, "-include-pch {0}" },
            { CompilerFlags.CreatePrecomp, "-x c++-header {0}" },
        };

        private static readonly IDictionary<CompilerFlags, string> s_vcFlags = new Dictionary<CompilerFlags, string>
        {
            { CompilerFlags.OutputFile, "/Fo\"{0}\"" },
            { CompilerFlags.UsePrecomp, "/Yu\"{0}\" /FI\"{0}\"" },
            { CompilerFlags.CreatePrecomp, "/Yc\"{0}\" /FI\"{0}\"" },
            { CompilerFlags.PrecompPath, "/Fp\"{0}\"" },
        };

        private static readonly ProjectOptionsGenerator s_optionGenerator = new ProjectOptionsGenerator();

        private IDictionary<CompilerFlags, string> _flags;

        private Project.Configuration _config;
        private string _compiler;
        private string _projectDirectory;
        private string _outputDirectory;
        private string _outputExtension;
        private string _precompFile;
        private string _usePrecompArgument;
        private string _createPrecompArgument;
        private string _arguments;

        public CompileCommandFactory(CompileCommandGenerationContext context)
        {
            var isClang = context.Configuration.Platform.IsUsingClang();
            var isMicrosoft = context.Configuration.Platform.IsMicrosoft();

            _compiler = isClang ? "clang.exe" : "clang-cl.exe";
            _config = context.Configuration;
            _outputExtension = isMicrosoft ? ".obj" : ".o";
            _outputDirectory = _config.IntermediatePath;
            _projectDirectory = context.ProjectDirectoryCapitalized;
            _flags = isClang ? s_clangFlags : s_vcFlags;

            s_optionGenerator.GenerateOptions(context, ProjectOptionGenerationLevel.Compiler);
            var argsList = new List<string>();

            argsList.Add(isClang ? "-c" : "/c");

            // Precomp arguments flags are actually written by the bff generator (see bff.template.cs)
            // Therefore, the CommandLineOptions entries only contain the pch name and file.
            if (_config.PrecompSource != null)
            {
                _precompFile = Path.Combine(_projectDirectory, context.Options[PrecompFileKey]);

                string name;
                if (_flags.ContainsKey(CompilerFlags.PrecompPath))
                {
                    argsList.Add(string.Format(_flags[CompilerFlags.PrecompPath], _precompFile));
                    name = context.Options[PrecompNameKey];
                }
                else
                {
                    name = _precompFile;
                }

                _createPrecompArgument = string.Format(_flags[CompilerFlags.CreatePrecomp], name);
                _usePrecompArgument = string.Format(_flags[CompilerFlags.UsePrecomp], name);
            }

            // AdditionalCompilerOptions are referenced from Options in the bff template.
            context.CommandLineOptions.Add(AdditionalOptionsKey, context.Options[AdditionalOptionsKey]);

            FillIncludeDirectoriesOptions(context);

            var validOptions = context.CommandLineOptions
                .Where(IsValidOption)
                .ToDictionary(kvp => kvp.Key, FlattenMultilineArgument);

            if (isMicrosoft)
            {
                // Required to avoid errors in VC headers.
                var flag = isClang ? "-D" : "/D";
                var value = validOptions.ContainsKey("ExceptionHandling") ? 1 : 0;
                argsList.Add($"{flag}_HAS_EXCEPTIONS={value}");
            }

            argsList.AddRange(validOptions.Values);

            _arguments = string.Join(" ", argsList);
        }

        private bool IsValidOption(KeyValuePair<string, string> option)
        {
            return !option.Value.Equals(FileGeneratorUtilities.RemoveLineTag) && !s_ignoredOptions.Contains(option.Key);
        }

        internal static string CmdLineConvertIncludePathsFunc(CompileCommandGenerationContext context, string include, string prefix)
        {
            // if the include is below the global root, we compute the relative path,
            // otherwise it's probably a system include for which we keep the full path
            string resolvedInclude = context.EnvironmentVariableResolver.Resolve(include);
            if (resolvedInclude.StartsWith(context.Project.RootPath, StringComparison.OrdinalIgnoreCase))
                resolvedInclude = Util.PathGetRelative(context.ProjectDirectory, resolvedInclude, true);
            return $@"{prefix}""{resolvedInclude}""";
        }

        private static void FillIncludeDirectoriesOptions(CompileCommandGenerationContext context)
        {
            // TODO: really not ideal, refactor and move the properties we need from it someplace else
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);

            var includePaths = new OrderableStrings(platformVcxproj.GetIncludePaths(context));
            var resourceIncludePaths = new OrderableStrings(platformVcxproj.GetResourceIncludePaths(context));
            context.CommandLineOptions["AdditionalIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalResourceIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalUsingDirectories"] = FileGeneratorUtilities.RemoveLineTag;

            var platformDescriptor = PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform);

            string defaultCmdLineIncludePrefix = platformDescriptor.IsUsingClang ? "-I" : "/I";

            // Fill include dirs
            var dirs = new List<string>();

            var platformIncludePaths = platformVcxproj.GetPlatformIncludePathsWithPrefix(context);
            var platformIncludePathsPrefixed = platformIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, p.Path, p.CmdLinePrefix)).ToList();
            dirs.AddRange(platformIncludePathsPrefixed);

            // TODO: move back up, just below the creation of the dirs list
            dirs.AddRange(includePaths.Select(p => CmdLineConvertIncludePathsFunc(context, p, defaultCmdLineIncludePrefix)));

            if (dirs.Any())
                context.CommandLineOptions["AdditionalIncludeDirectories"] = string.Join(" ", dirs);

            // Fill resource include dirs
            var resourceDirs = new List<string>();
            resourceDirs.AddRange(resourceIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, p, defaultCmdLineIncludePrefix)));

            if (Options.GetObject<Options.Vc.General.PlatformToolset>(context.Configuration).IsLLVMToolchain() &&
                Options.GetObject<Options.Vc.LLVM.UseClangCl>(context.Configuration) == Options.Vc.LLVM.UseClangCl.Enable)
            {
                // with LLVM as toolchain, we are still using the default resource compiler, so we need the default include prefix
                // TODO: this is not great, ideally we would need the prefix to be per "compiler", and a platform can have many
                var platformIncludePathsDefaultPrefix = platformIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, p.Path, defaultCmdLineIncludePrefix));
                resourceDirs.AddRange(platformIncludePathsDefaultPrefix);
            }
            else
            {
                resourceDirs.AddRange(platformIncludePathsPrefixed);
            }

            if (resourceDirs.Any())
                context.CommandLineOptions["AdditionalResourceIncludeDirectories"] = string.Join(" ", resourceDirs);

            // Fill using dirs
            Strings additionalUsingDirectories = Options.GetStrings<Options.Vc.Compiler.AdditionalUsingDirectories>(context.Configuration);
            additionalUsingDirectories.AddRange(context.Configuration.AdditionalUsingDirectories);
            additionalUsingDirectories.AddRange(platformVcxproj.GetCxUsingPath(context));
            if (additionalUsingDirectories.Any())
            {
                var cmdAdditionalUsingDirectories = additionalUsingDirectories.Select(p => CmdLineConvertIncludePathsFunc(context, p, "/AI"));
                context.CommandLineOptions["AdditionalUsingDirectories"] = string.Join(" ", cmdAdditionalUsingDirectories);
            }
        }

        // ProjectOptionsGenerator will generate format some arguments
        // (includes, defines, manifests...) specifically for the bff template.
        // The string is split on multiple lines and concatenated ('foo'\r\n + 'bar')
        private string FlattenMultilineArgument(KeyValuePair<string, string> kvp)
        {
            if (!s_multilineArgumentKeys.Contains(kvp.Key))
                return kvp.Value;

            var parts = kvp.Value.Split('\r', '\n')
                .Select(s => s.Trim(' ', '\t', '\'', '+'));

            return string.Join(" ", parts);
        }

        // TODO: Consider sub-configurations (file specific)

        public CompileCommand CreateCompileCommand(string inputFile)
        {
            string outputFile;
            string precompArgument;
            var args = new List<string>();

            args.Add(_compiler);

            if (_config.PrecompSource != null && inputFile.EndsWith(_config.PrecompSource, StringComparison.OrdinalIgnoreCase))
            {
                precompArgument = _createPrecompArgument;
                outputFile = _precompFile;

                if (_flags.ContainsKey(CompilerFlags.PrecompPath))
                    outputFile += _outputExtension;
            }
            else
            {
                precompArgument = _usePrecompArgument;
                outputFile = Path.ChangeExtension(Path.Combine(_outputDirectory, Path.GetFileName(inputFile)), _outputExtension);
            }

            args.Add(inputFile);
            args.Add(string.Format(_flags[CompilerFlags.OutputFile], outputFile));

            if (!string.IsNullOrEmpty(precompArgument))
                args.Add(precompArgument);

            var command = string.Join(" ", args) + " " + _arguments;

            return new CompileCommand
            {
                File = inputFile,
                Directory = _projectDirectory,
                Command = command,
            };
        }
    }

    internal class CompileCommandGenerationContext : IGenerationContext
    {
        private Resolver _envVarResolver;

        public Builder Builder { get; private set; }

        public Project Project { get; private set; }

        public Project.Configuration Configuration { get; set; }

        public string ProjectDirectory { get; private set; }

        public Options.ExplicitOptions Options { get; set; }

        public IDictionary<string, string> CommandLineOptions { get; set; }

        public DevEnv DevelopmentEnvironment { get { return Configuration.Compiler; } }

        public string ProjectDirectoryCapitalized { get; private set; }

        public string ProjectSourceCapitalized { get; private set; }

        public bool PlainOutput { get { return true; } }

        public Resolver EnvironmentVariableResolver
        {
            get
            {
                System.Diagnostics.Debug.Assert(_envVarResolver != null);
                return _envVarResolver;
            }
            set
            {
                _envVarResolver = value;
            }
        }

        public CompileCommandGenerationContext(Builder builder, Project project, Project.Configuration config)
        {
            Builder = builder;
            Project = project;
            ProjectDirectory = config.ProjectPath;
            ProjectDirectoryCapitalized = Util.GetCapitalizedPath(ProjectDirectory);
            ProjectSourceCapitalized = Util.GetCapitalizedPath(project.SourceRootPath);
            Configuration = config;
            Options = new Options.ExplicitOptions();
            CommandLineOptions = new Dictionary<string, string>();
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
}
