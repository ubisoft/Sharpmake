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
using System.Text.RegularExpressions;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.Generators.JsonCompilationDatabase
{
    /// <summary>
    /// Compile command format.
    /// - Command: Compilation command specified as a shell command
    /// - Arguments: Compilation command specified as a list of arguments
    /// </summary>
    public enum CompileCommandFormat
    {
        Command,
        Arguments
    }

    public class JsonCompilationDatabase
    {
        public const string FileName = "compile_commands.json";

        public event Action<IGenerationContext, CompileCommand> CompileCommandGenerated;

        public void Generate(Builder builder, string path, IEnumerable<Project.Configuration> projectConfigurations, CompileCommandFormat format, List<string> generatedFiles, List<string> skipFiles)
        {
            var database = new List<IDictionary<string, object>>();

            foreach (var configuration in projectConfigurations)
            {
                database.AddRange(GetEntries(builder, configuration, format));
            }

            if (database.Count > 0)
                WriteGeneratedFile(builder, path, database, generatedFiles, skipFiles);
        }

        private void WriteGeneratedFile(Builder builder, string path, IEnumerable<IDictionary<string, object>> database, List<string> generatedFiles, List<string> skipFiles)
        {
            var file = new FileInfo(Path.Combine(path, FileName));

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            using (var serializer = new Util.JsonSerializer(writer) { IsOutputFormatted = true })
            {
                serializer.Serialize(database);
                serializer.Flush();

                if (builder.Context.WriteGeneratedFile(null, file, stream))
                {
                    generatedFiles.Add(Path.Combine(file.DirectoryName, file.Name));
                }
                else
                {
                    skipFiles.Add(Path.Combine(file.DirectoryName, file.Name));
                }
            }
        }

        private IEnumerable<IDictionary<string, object>> GetEntries(Builder builder, Project.Configuration projectConfiguration, CompileCommandFormat format)
        {
            var context = new CompileCommandGenerationContext(builder, projectConfiguration.Project, projectConfiguration);
            var resolverParams = new[] {
                    new VariableAssignment("project", context.Project),
                    new VariableAssignment("target", context.Configuration.Target),
                    new VariableAssignment("conf", context.Configuration)
            };
            context.EnvironmentVariableResolver = PlatformRegistry.Get<IPlatformDescriptor>(projectConfiguration.Platform).GetPlatformEnvironmentResolver(resolverParams);

            var factory = new CompileCommandFactory(context);

            var database = projectConfiguration.Project.GetSourceFilesForConfigurations(new[] { projectConfiguration })
                .Except(projectConfiguration.ResolvedSourceFilesBuildExclude)
                .Where(f => projectConfiguration.Project.SourceFilesCPPExtensions.Contains(Path.GetExtension(f)))
                .Select(factory.CreateCompileCommand);

            foreach (var cc in database)
            {
                CompileCommandGenerated?.Invoke(context, cc);

                if (format == CompileCommandFormat.Arguments)
                {
                    yield return new Dictionary<string, object>
                    {
                        { "directory", cc.Directory },
                        { "arguments", cc.Arguments },
                        { "file", cc.File },
                    };
                }
                else
                {
                    yield return new Dictionary<string, object>
                    {
                        { "directory", cc.Directory },
                        { "command", cc.Command },
                        { "file", cc.File },
                    };
                }
            }
        }
    }

    public class CompileCommand
    {
        public string File { get; set; }
        public string Directory { get; set; }
        public string Command { get; set; }
        public List<string> Arguments { get; set; }
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
            "ManifestInputs"
        };

        private enum CompilerFlags
        {
            OutputFile,
            UsePrecomp,
            CreatePrecomp,
            PrecompPath,
            IncludePath,
        }

        private static readonly IDictionary<CompilerFlags, string> s_clangFlags = new Dictionary<CompilerFlags, string>
        {
            { CompilerFlags.OutputFile, "-o {0}" },
            { CompilerFlags.UsePrecomp, "-include-pch {0}" },
            { CompilerFlags.CreatePrecomp, "-x c++-header {0}" },
            { CompilerFlags.IncludePath, "-I" },
        };

        private static readonly IDictionary<CompilerFlags, string> s_vcFlags = new Dictionary<CompilerFlags, string>
        {
            { CompilerFlags.OutputFile, "/Fo\"{0}\"" },
            { CompilerFlags.UsePrecomp, "/Yu\"{0}\" /FI\"{0}\"" },
            { CompilerFlags.CreatePrecomp, "/Yc\"{0}\" /FI\"{0}\"" },
            { CompilerFlags.PrecompPath, "/Fp\"{0}\"" },
            { CompilerFlags.IncludePath, "/I" },
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
        private List<string> _arguments = new List<string>();

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

            _arguments.Add(isClang ? "-c" : "/c");

            // Precomp arguments flags are actually written by the bff generator (see bff.template.cs)
            // Therefore, the CommandLineOptions entries only contain the pch name and file.
            if (_config.PrecompSource != null)
            {
                _precompFile = Path.Combine(_projectDirectory, context.Options[PrecompFileKey]);

                string name;
                if (_flags.ContainsKey(CompilerFlags.PrecompPath))
                {
                    _arguments.Add(string.Format(_flags[CompilerFlags.PrecompPath], _precompFile));
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

            var validOptions = context.CommandLineOptions
                .Where(IsValidOption)
                .ToDictionary(kvp => kvp.Key, FlattenMultilineArgument);

            var platformDefineSwitch = isClang ? "-D" : "/D";
            if (isMicrosoft)
            {
                // Required to avoid errors in VC headers.
                var value = validOptions.ContainsKey("ExceptionHandling") ? 1 : 0;
                _arguments.Add($"{platformDefineSwitch}_HAS_EXCEPTIONS={value}");
            }

            _arguments.AddRange(validOptions.Values.SelectMany(x => x));

            SelectPreprocessorDefinitions(context, platformDefineSwitch);
            FillIncludeDirectoriesOptions(context);
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

        private void SelectPreprocessorDefinitions(CompileCommandGenerationContext context, string platformDefineSwitch)
        {
            var defines = new Strings();
            defines.AddRange(context.Options.ExplicitDefines);
            defines.AddRange(context.Configuration.Defines);

            foreach (string define in defines.SortedValues)
            {
                if (!string.IsNullOrWhiteSpace(define))
                    _arguments.Add(string.Format(@"{0}{1}{2}{1}", platformDefineSwitch, Util.DoubleQuotes, define.Replace(Util.DoubleQuotes, Util.EscapedDoubleQuotes)));
            }
        }

        private void FillIncludeDirectoriesOptions(CompileCommandGenerationContext context)
        {
            // TODO: really not ideal, refactor and move the properties we need from it someplace else
            var platformVcxproj = PlatformRegistry.Query<IPlatformVcxproj>(context.Configuration.Platform);

            var includePaths = new OrderableStrings(platformVcxproj.GetIncludePaths(context));
            var resourceIncludePaths = new OrderableStrings(platformVcxproj.GetResourceIncludePaths(context));
            context.CommandLineOptions["AdditionalIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalResourceIncludeDirectories"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["AdditionalUsingDirectories"] = FileGeneratorUtilities.RemoveLineTag;

            var platformDescriptor = PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform);

            string defaultCmdLineIncludePrefix = _flags[CompilerFlags.IncludePath];

            // Fill include dirs
            var dirs = new List<string>();

            var platformIncludePaths = platformVcxproj.GetPlatformIncludePathsWithPrefix(context);
            var platformIncludePathsPrefixed = platformIncludePaths.Select(p => CmdLineConvertIncludePathsFunc(context, p.Path, p.CmdLinePrefix)).ToList();
            dirs.AddRange(platformIncludePathsPrefixed);

            // TODO: move back up, just below the creation of the dirs list
            dirs.AddRange(includePaths.Select(p => CmdLineConvertIncludePathsFunc(context, p, defaultCmdLineIncludePrefix)));

            if (dirs.Any())
            {
                context.CommandLineOptions["AdditionalIncludeDirectories"] = string.Join(" ", dirs);
                _arguments.AddRange(dirs);
            }

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
            {
                context.CommandLineOptions["AdditionalResourceIncludeDirectories"] = string.Join(" ", resourceDirs);
                _arguments.AddRange(resourceDirs);
            }

            // Fill using dirs
            Strings additionalUsingDirectories = Options.GetStrings<Options.Vc.Compiler.AdditionalUsingDirectories>(context.Configuration);
            additionalUsingDirectories.AddRange(context.Configuration.AdditionalUsingDirectories);
            additionalUsingDirectories.AddRange(platformVcxproj.GetCxUsingPath(context));
            if (additionalUsingDirectories.Any())
            {
                var cmdAdditionalUsingDirectories = additionalUsingDirectories.Select(p => CmdLineConvertIncludePathsFunc(context, p, "/AI"));
                context.CommandLineOptions["AdditionalUsingDirectories"] = string.Join(" ", cmdAdditionalUsingDirectories);
                _arguments.AddRange(cmdAdditionalUsingDirectories);
            }
        }

        // ProjectOptionsGenerator will generate format some arguments
        // (includes, defines, manifests...) specifically for the bff template.
        // The string is split on multiple lines and concatenated ('foo'\r\n + 'bar')
        private IList<string> FlattenMultilineArgument(KeyValuePair<string, string> kvp)
        {
            if (!s_multilineArgumentKeys.Contains(kvp.Key))
            {
                var parts = kvp.Value.Split(' ');
                return parts.ToList();
            }
            else
            {
                var parts = kvp.Value.Split('\r', '\n')
                    .Select(s => s.Trim(' ', '\t', '\'', '+'));

                return parts.Where(s => s.Count() > 0).ToList();
            }
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
                string fileExtension = Path.GetExtension(inputFile);
                bool isDontUsePrecomp = _config.PrecompSourceExclude.Contains(inputFile) ||
                                        _config.PrecompSourceExcludeFolders.Any(folder => inputFile.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) ||
                                        _config.PrecompSourceExcludeExtension.Contains(fileExtension) ||
                                        string.Compare(fileExtension, ".c", StringComparison.OrdinalIgnoreCase) == 0;

                if (isDontUsePrecomp == false)
                {
                    precompArgument = _usePrecompArgument;
                }
                else
                {
                    precompArgument = null;
                }

                outputFile = Path.ChangeExtension(Path.Combine(_outputDirectory, Path.GetFileName(inputFile)), _outputExtension);
            }

            args.Add(inputFile);
            args.Add(string.Format(_flags[CompilerFlags.OutputFile], outputFile));

            if (!string.IsNullOrEmpty(precompArgument))
                args.Add(precompArgument);

            var command = string.Join(" ", args) + " " + string.Join(" ", _arguments);

            args.AddRange(_arguments);

            // Remove unescaped double quote from arguments list (but keep them for the full command line).
            // This is in fact what will do the shell when it will parse the full command line and give the argv/argc to the program.
            var match_unescaped_double_quote = new Regex(@"(?<!\\)((\\{2})*)""");

            return new CompileCommand
            {
                File = inputFile,
                Directory = _projectDirectory,
                Command = command,
                Arguments = args.Select(arg => match_unescaped_double_quote.Replace(arg, "$1")).ToList()
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
