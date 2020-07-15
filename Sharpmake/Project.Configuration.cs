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
using System.Runtime.CompilerServices;

namespace Sharpmake
{
    /// <summary>
    /// Options to specify the properties of the dependencies between projects. This is used with
    /// <see cref="Project.Configuration.AddPublicDependency"/> and
    /// <see cref="Project.Configuration.AddPrivateDependency"/>.
    /// </summary>
    [Flags]
    public enum DependencySetting
    {
        /// <summary>
        /// The dependent project must be built after the dependency.
        ///  Otherwise the two files have no dependencies.
        /// </summary>
        OnlyBuildOrder = 0,

        /// <summary>
        /// The dependent project inherits the library files of the dependency.
        /// Valid only when the project is a C or a C++ project.
        /// </summary>
        LibraryFiles = 1 << 1,

        /// <summary>
        /// The dependent project inherits the library paths of the dependency.
        /// Valid only when the project is a C or a C++ project.
        /// </summary>
        LibraryPaths = 1 << 2,

        /// <summary>
        /// The dependent project inherits the include paths of the dependency.
        /// Valid only when the project is a C or a C++ project.
        /// </summary>
        IncludePaths = 1 << 3,

        /// <summary>
        /// The dependent project inherits the defined symbols of the dependency.
        /// Valid only when the project is a C or a C++ project.
        /// </summary>
        Defines = 1 << 4,

        /// <summary>
        /// The dependent project inherits the `using` paths of the dependency.
        /// Valid only if the project is a C# project and uses Microsoft C++/CX extensions .
        /// </summary>
        AdditionalUsingDirectories = 1 << 5,
        ForceUsingAssembly = 1 << 6,

        /// <summary>
        /// Specifies that the dependent project inherits the dependency's library files, library
        /// paths, include paths and defined symbols.
        /// </summary>
        Default = LibraryFiles |
                  LibraryPaths |
                  IncludePaths |
                  Defines,

        /// <summary>
        /// Specifies that the dependent project inherits the dependency's include paths and
        /// defined symbols, but not it's library files or library paths. Use this for header-only
        /// C++ libraries.
        /// </summary>
        DefaultWithoutLinking = IncludePaths |
                                Defines,

        DefaultForceUsing = ForceUsingAssembly
                              | IncludePaths
                              | Defines,


        ////////////////////////////////////////////////////////////////////////
        // OLD AND DEPRECATED FLAGS
        [Obsolete("Please use OnlyBuildOrder instead.", error: false)]
        OnlyDependencyInSolution = -1,

        [Obsolete("Please use OnlyBuildOrder instead.", error: false)]
        ForcedDependencyInSolution = -1,

        [Obsolete("Please replace by OnlyBuildOrder if that's what you wanted, otherwise remove it, it isn't needed.", error: false)]
        ProjectReference = -1,

        [Obsolete("Please replace by LibraryFiles.", error: false)]
        InheritLibraryFiles = -1,

        [Obsolete("Please replace by LibraryPaths.", error: false)]
        InheritLibraryPaths = -1,

        [Obsolete("Please replace by IncludePaths.", error: false)]
        InheritIncludePaths = -1,

        [Obsolete("Please replace by Defines.", error: false)]
        InheritDefines = -1,

        [Obsolete("Please remove this.", error: false)]
        InheritDependencies = -1,

        [Obsolete("Please replace by LibraryFiles if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesLibraryFiles = -1,

        [Obsolete("Please replace by LibraryPaths if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesLibraryPaths = -1,

        [Obsolete("Please replace by IncludePaths if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesIncludePaths = -1,

        [Obsolete("Please replace by Defines if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesDefines = -1,

        [Obsolete("Please replace by OnlyBuildOrder if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesNothing = -1,
        [Obsolete("Please replace by OnlyBuildOrder if needed, sharpmake controls the dependency inheritance.", error: false)]
        InheritFromDependenciesDependencies = -1,
    }

    /// <summary>
    /// Visibility types for inter-project dependency relationships. This setting is
    /// usually only meaningful in cases where a library depends on another library because
    /// one of its executables has an end-point in the other's dependency graph.
    /// </summary>
    public enum DependencyType
    {
        /// <summary>
        /// Specifies that the dependency relationship is private. The dependent project will not
        /// expose the dependency's exported properties, such as it's include paths.
        /// </summary>
        /// <remarks>
        /// A library that has a private dependency relationship with another library will use that
        /// library internally when compiled but will not expose the private dependency's
        /// exported properties (library paths, include paths, etc.) when other projects link to
        /// it. For example, if library B has a private dependency on C and A wants to link to B,
        /// A will not inherit any of C's include paths, library paths, etc.
        /// </remarks>
        Private,

        /// <summary>
        /// Specifies that the dependency relationship is public. The dependent project will expose
        /// the dependency's exported properties as it's own.
        /// </summary>
        /// <remarks>
        /// A library that has a public dependency relationship with another library will expose
        /// that dependency's include paths, library paths, etc. to any project that has a public
        /// dependency on it. For example, if library B has a public dependency on C and A wants to
        /// link to B, A will inherit all of C's include paths, library paths, etc.
        /// </remarks>
        Public
    }

    public partial class Project
    {
        /// <summary>
        /// Holds the properties of an individual project's configuration. This holds all the
        /// properties and settings needed to generate the configuration.
        /// </summary>
        /// <remarks>
        /// This class is at the core of Sharpmake's generation engine. Methods marked with
        /// <see cref="Generate"/> are passed an instance of this class and set its properties
        /// accordingly in order to generate the configuration. Please refer to the (Sharpmake
        /// documentation and tutorials)[https://github.com/ubisoftinc/Sharpmake/wiki] for a
        /// full explanation.
        /// <para>
        /// Unless specified otherwise, all string properties can contain tokens that are resolved
        /// after the configuration phase, during the generation. Those tokens are inserted
        /// using square brackets. For example, you can write the following to refer to the
        /// project's */src/code.cpp* file in it's root.
        /// <c>
        ///     conf.Property = "[project.SourceRootPath]/src/code.cpp";
        /// </c>
        /// This is very useful for paths because they often need to combine path elements, and
        /// this is much less verbose than <c>Path.Combine</c>. This is also useful because Sharpmake
        /// currently doesn't support string interpolation (it uses Roslyn to compile the scripts).
        /// Note, however, these tokens don't understand the scope in which they're used and
        /// only support the following source objects:
        ///     * `project`
        ///     * `solution`
        ///     * `conf`
        /// </para>
        /// <note>
        /// There is one important caveat for C++ projects in relation to exceptions. Because
        /// Sharpmake was originally designed as an internal tool to build engines for interactive
        /// games at Ubisoft, **C++ exceptions are disabled by default**. If your project uses
        /// exceptions, they currently must be manually re-enabled by adding the correct exception
        /// setting. For Visual Studio projects, add the correct value of
        /// <see cref="Options.Vc.Compiler.Exceptions"/> to the
        /// <see cref="Sharpmake.Configuration.Options"/> property, as in the example below.
        /// <code language=cs>
        ///     conf.Options.Add(Sharpmake.Options.Vc.Compiler.Exceptions.Enable);
        /// </code>
        /// </note>
        /// <note>
        /// In addition, you can selectively enable and disable exceptions on source files on a
        /// file-by-file basis using <see cref="SourceFilesExceptionsEnabled"/>,
        /// <see cref="SourceFilesExceptionsEnabledWithExternC"/> or
        /// <see cref="SourceFilesExceptionsEnabledWithSEH"/>.
        /// </note>
        /// <note>
        /// Finally, source files compiled in a context that requires C++ exceptions
        /// (such as source files compiled with the WinRT extensions)
        /// are implicitly added to <see cref="SourceFilesExceptionsEnabled"/>.
        /// </note>
        /// </remarks>
        [Resolver.Resolvable]
        public class Configuration : Sharpmake.Configuration
        {
            /// <summary>
            /// Interface for classes that implement platform-specific tasks for generating
            /// configurations. An implementation of this interface is required when generating
            /// for a platform.
            /// </summary>
            /// <remarks>
            /// Implementations can assume that they will only be called by Sharpmake, and that the
            /// arguments are sane (ex: <see cref="SetupStaticLibraryPaths"/> is passed valid (non-null)
            /// configurations).
            /// </remarks>
            public interface IConfigurationTasks
            {
                /// <summary>
                /// Sets up the library paths when adding a dependency on a dynamic library.
                /// </summary>
                /// <param name="configuration">The <see cref="Configuration"/> instance on which
                ///        to set the paths.</param>
                /// <param name="dependencySetting">The <see cref="DependencySetting"/> bitflags
                ///        that specify the properties of the dependency relationship.</param>
                /// <param name="dependency">The <see cref="Configuration"/> instance of the dependency.</param>
                void SetupDynamicLibraryPaths(Configuration configuration, DependencySetting dependencySetting, Configuration dependency);

                /// <summary>
                /// Sets up the library paths when adding a dependency on a static library.
                /// </summary>
                /// <param name="configuration">The <see cref="Configuration"/> instance on which to 
                ///        set the paths.</param>
                /// <param name="dependencySetting">The <see cref="DependencySetting"/> bitflags
                ///        that specify the properties of the dependency relationship.</param>
                /// <param name="dependency">The <see cref="Configuration"/> instance of the dependency.</param>
                void SetupStaticLibraryPaths(Configuration configuration, DependencySetting dependencySetting, Configuration dependency);

                /// <summary>
                /// Gets the default file extension for a given output type.
                /// </summary>
                /// <param name="outputType">The <see cref="OutputType"/> whose default file extension we are seeking.</param>
                /// <returns>A string, containing the file extension (not including the dot (.) prefix).</returns>
                string GetDefaultOutputExtension(OutputType outputType);

                /// <summary>
                /// Gets the library paths native to the specified configuration's platform.
                /// </summary>
                /// <param name="configuration">The <see cref="Configuration"/> to get the paths for.</param>
                /// <returns>A list of library paths for the specified configuration and platform.</returns>
                IEnumerable<string> GetPlatformLibraryPaths(Configuration configuration);
            }

            private static int s_count = 0;

            /// <summary>
            /// Gets the number of generated <see cref="Configuration"/> instances.
            /// </summary>
            public static int Count => s_count;

            private const string RemoveLineTag = "REMOVE_LINE_TAG";

            private enum LinkState
            {
                NotLinked,
                Linking,
                Linked
            }
            private LinkState _linkState = LinkState.NotLinked;

            public Configuration()
            {
                PrecompSourceExcludeExtension.Add(".asm");
            }

            /// <summary>
            /// Maps the .NET <see cref="OutputType"/> into its native counterpart.
            /// </summary>
            /// <param name="type">Specifies the <see cref="OutputType"/> to map.</param>
            /// <returns> Returns the mapped <see cref="OutputType"/> value.</returns>
            /// <remarks>
            /// This method maps values of <see cref="OutputType"/> in the following way:
            ///     * <see cref="OutputType.DotNetConsoleApp"/> and <see cref="OutputType.DotNetWindowsApp"/> are mapped to <see cref="OutputType.Exe"/>.
            ///     * <see cref="OutputType.DotNetClassLibrary"/> is mapped to <see cref="OutputType.Dll"/>.
            ///     * Other values are mapped to themselves.
            /// </remarks>
            public static OutputType SimpleOutputType(OutputType type)
            {
                switch (type)
                {
                    case OutputType.DotNetConsoleApp:
                    case OutputType.DotNetWindowsApp:
                        return OutputType.Exe;
                    case OutputType.DotNetClassLibrary:
                        return OutputType.Dll;
                    default:
                        return type;
                }
            }

            /// <summary>
            /// Output types for the <see cref="Configuration"/>.
            /// </summary>
            public enum OutputType
            {
                /// <summary>
                /// Output is an executable/>.
                /// </summary>
                Exe,

                /// <summary>
                /// Output is a static library/>.
                /// </summary>
                Lib,

                /// <summary>
                /// Output is a DLL(Dynamic Link library)/>.
                /// </summary>
                Dll,

                /// <summary>
                /// The project does not produce any code. It is either a header-only library, or a
                /// utility project that is used as part of the build system but does not produce
                /// any code.
                /// </summary>
                Utility,

                /// <summary>
                /// The output is an executable .NET program that opens a console window on
                /// startup. The extension is always <c>.exe</c>.
                /// </summary>
                DotNetConsoleApp,

                /// <summary>
                /// The output is a .NET class library that can be added as a reference. The
                /// extension is always <c>.dll</c>.
                /// </summary>
                DotNetClassLibrary,

                /// <summary>
                /// The output is an executable .NET program that does not display a console window
                /// on startup. The extension is always <c>.exe</c>.
                /// </summary>
                DotNetWindowsApp,

                /// <summary>
                /// The output is an iOS app.
                /// </summary>
                IosApp,

                /// <summary>
                /// The output is an iOS test bundle.
                /// </summary>
                IosTestBundle,

                /// <summary>
                /// Specifies no output. Do not use this.
                /// </summary>
                None,
            }

            /// <summary>
            /// Methods to list source files.
            /// </summary>
            /// <remarks>
            /// This is only used for FASTBuild generation.
            /// </remarks>
            public enum InputFileStrategy
            {
                /// <summary>
                /// Explicitly refer to files in FASTBuild configuration files using file lists.
                /// </summary>
                Include = 0x01,

                /// <summary>
                /// Implicitly refer to files in FASTBuild configuration files using paths and
                /// exclusion file lists.
                /// </summary>
                Exclude = 0x02
            }

            /// <summary>
            /// FASTBuild deoptimization strategies for writable files.
            /// </summary>
            public enum DeoptimizationWritableFiles
            {
                /// <summary>
                /// No deoptimization. This is the default.
                /// </summary>
                NoDeoptimization = 0x01, // default

                /// <summary>
                /// Deoptimize all files with a writable flag on the file system.
                /// </summary>
                /// <remarks>
                /// This is useful when using Perforce, since files that have not been modified are
                /// typically read-only. That is, this option enables automatic deoptimization of modified files.
                /// </remarks>
                DeoptimizeWritableFiles = 0x02,

                /// <summary>
                /// When the <c>FASTBUILD_DEOPTIMIZE_OBJECT</c> token is specified,
                /// deoptimize files with writable status. 
                /// </summary>
                /// <remarks>
                /// This is useful when using Perforce, since files that have not been modified are
                /// typically read-only. That is, this enables automatic deoptimization of modified files.
                /// </remarks>
                DeoptimizeWritableFilesWithToken = 0x04

                //
                // Probably want to support deoptimiztion for other SSCs, ie: files that are changed or staged on Git.
                //
            }

            /// <summary>
            /// When the output is an executable program, this lists the levels of privileges that
            /// it can require upon execution, using Windows' User Account Control (UAC.)
            /// </summary>
            public enum UACExecutionLevel
            {
                /// <summary>
                /// Use the same privileges as the process that created the program.
                /// </summary>
                asInvoker,

                /// <summary>
                /// Use the highest privileges available to the current user.
                /// </summary>
                highestAvailable,

                /// <summary>
                /// Always run with administrator privileges. This will usually open a UAC dialog
                /// box for the user.
                /// </summary>
                requireAdministrator
            }

            public Strings PathExcludeBuild = new Strings();

            private OutputType _output = OutputType.Exe; // None is default if Export

            /// <summary>
            /// Gets or sets the output type of the current configuration, exe, lib or dll.
            /// </summary>
            public OutputType Output
            {
                get { return _output; }
                set
                {
                    if (!Project.IsValidConfigurationOutputType(value))
                        throw new Error("The specified configuration output type \"{0}\" is not valid for the project \"{1}\".", value, Project.GetType().ToNiceTypeName());
                    _output = value;
                }
            }

            /// <summary>
            /// Gets or sets the project's output extension (ie: .dll, .self, .exe, .dlu).
            /// </summary>
            public string OutputExtension = "";

            /// <summary>
            /// Gets or sets whether to copy output files to the output directory.
            /// </summary>
            /// <remarks>
            /// This setting is provided for libraries, because they are usually intermediate
            /// artifacts during the compilation process and do not need to be in the final output
            /// directory unless it's necessary. 
            /// <para>
            /// The default is <c>false</c>. Setting this to <c>true</c> will force the generators
            /// to copy the library artifacts.
            /// </para>
            /// <para>
            /// If <see cref="Output"/> is set to a value that corresponds to an executable program
            /// (ie: <see cref="OutputType.Exe"/>), the generators disregard this property and
            /// always copy the results.
            /// </para>
            /// </remarks>
            public bool ExecuteTargetCopy = false;

            /// <summary>
            /// Gets or sets whether dependent projects will copy their debugging database to the
            /// target path of their dependency projects. The default value is <c>false</c>.
            /// </summary>
            public bool CopyCompilerPdbToDependentTargets = false;

            // Xcopy parameters
            // /d           Copy file only if the source time is newer than the destination time.
            // /F           Displays full source and destination file names while copying.
            // /R           Overwrites read-only files.
            // /H           Copies hidden and system files.
            // /V           Verifies the size of each new file.
            // /Y           Suppresses prompting to confirm whether you want to overwrite an existing destination file or not.
            /// <summary>
            /// Command to execute <see cref="TargetCopyFiles"/>.
            /// </summary>
            /// <param name="relativeSourcePath">The relative path to the files.</param>
            /// <param name="relativeTargetPath">The relative path to the target directory.</param>
            /// <param name="workingPath">The path to the working directory.</param>
            /// <returns>The mapped <see cref="OutputType"/> value as a string.</returns>
            public delegate string TargetCopyCommandCreator(string relativeSourcePath, string relativeTargetPath, string workingPath);

            public TargetCopyCommandCreator CreateTargetCopyCommand =
                (source, target, workingPath) => string.Format(@"xcopy /d /F /R /H /V /Y ""{0}"" ""{1}"" >nul", source, target);

            /// <summary>
            /// Setting this boolean to true forces Sharpmake to fill in the AD fields in the current static
            /// library project.
            /// </summary>
            /// <remarks>
            /// Since Sharpmake handles all dependencies, using an <c>AdditionalDependencies</c> field in
            /// your project is typically useless for static libraries. However, when dependents aren't
            /// generated by Sharpmake, (that is, when a .sln contains Sharpmake generated projects as static
            /// libraries as well as manually maintained dependent projects) this feature can be useful.
            /// <para>
            /// The default is <c>false</c>. Set this boolean to <c>true</c> to make Sharpmake fill in the fields
            /// for the current static library project.
            /// </para>
            /// </remarks>
            public bool ExportAdditionalLibrariesEvenForStaticLib = false;

            /// <summary>
            /// Gets or sets the name of the project, as viewed by the configuration.
            /// </summary>
            /// <remarks>
            /// Under normal circumstances, you should not need to edit this property. The name of
            /// the project is set in <see cref="Name"/> and this is the default value.
            /// </remarks>
            public string ProjectName = "[project.Name]";

            /// <summary>
            /// Gets or sets the file name for the generated project, without any file extension.
            /// (ex: `"MyProject"`)
            /// </summary>
            public string ProjectFileName = "[project.Name]";

            /// <summary>
            /// Gets or sets the directory in which the project will be generated.
            /// </summary>
            /// <remarks>
            /// By default, this is set to the same directory that this Sharpmake script is running in.
            /// </remarks>
            public string ProjectPath = "[project.SharpmakeCsPath]";

            /// <summary>
            /// Gets or sets the name of the generated .NET assembly.
            /// </summary>
            /// <remarks>
            /// Ignored in projects that are not built on the .NET framework.
            /// </remarks>
            public string AssemblyName = "[project.AssemblyName]";

            /// <summary>
            /// Gets the full path of the project file, including the directory and the
            /// file name. This doesn't include the file extension which depends on
            /// the generator.
            /// </summary>
            public string ProjectFullFileName { get { return Path.Combine(ProjectPath, ProjectFileName); } }

            /// <summary>
            /// Gets or sets the solution folder that will hold the Visual Studio solution for this project.
            /// </summary>
            /// <remarks>
            /// Ignored unless building a Visual Studio project.
            /// <para>
            /// To place the project in a sub-directory, use a `/` as a directory separator.
            /// </para>
            /// </remarks>
            public string SolutionFolder = "";

            /// <summary>
            /// Gets or sets the suffix to use in <see cref="LinkerPdbSuffix"/>.
            /// If unset, the pdb file names will be the target name with a suffix and the .pdb extension.
            /// </summary>
            /// <remarks>
            /// Always put a separate pdb for the compiler in the intermediate path to avoid
            /// conflicts with the one from the linker.
            /// This helps the following things:
            /// 1. Makes the linker go faster
            /// 2. Avoid pdbs for dlls and .exe(s) growing and growing at each link
            /// 3. Makes incremental linking work better.
            /// </remarks>
            public string LinkerPdbSuffix = string.Empty;

            /// <summary>
            /// Gets or sets the directory and file name of the Visual Studio *linker* PDB file,
            /// including the file extension.
            /// </summary>
            /// <remarks>
            /// Used only when generating a Visual Studio project.
            /// <para>
            /// The default value is:
            /// <c>[conf.TargetPath]/[conf.TargetFileFullName][conf.LinkerPdbSuffix].pdb</c>.
            /// </para>
            /// <para>
            /// Always put a separate PDB for the compiler in the intermediate path to avoid
            /// conflicts with the one from the linker.
            /// </para>
            /// </remarks>
            public string LinkerPdbFilePath = "[conf.TargetPath]" + Path.DirectorySeparatorChar + "[conf.TargetFileFullName][conf.LinkerPdbSuffix].pdb";

            /// <summary>
            /// Gets or sets the suffix to use in <see cref="CompilerPdbFilePath"/>.
            /// </summary>
            /// <remarks>
            /// Provided only as a convenience as it is only used in the default
            /// value of <see cref="CompilerPdbFilePath"/> to assign a suffix to the PDB. If you
            /// change <see cref="CompilerPdbFilePath"/> so that it doesn't use this property,
            /// then it isn't used.
            /// </remarks>
            public string CompilerPdbSuffix = "_compiler";

            /// <summary>
            /// Gets or sets the directory and file name of the Visual Studio <i>compiler</i> PDB file,
            /// including the file extension.
            /// </summary>
            /// <remarks>
            /// Used only when generating a Visual Studio project.
            /// <para>
            /// The default value is
            /// <c>[conf.IntermediatePath]/[conf.TargetFileFullName][conf.CompilerPdbSuffix].pdb</c>.
            /// </para>
            /// <para>
            /// The default file name in <see cref="CompilerPdbFilePath"/> in Sharpmake does not
            /// match its default file name in Visual Studio for compiler PDB, which is <c>VCx0.pdb</c>.
            /// See <externalLink>
            /// <linkText> /Fd (Program Database File Name)</linkText>
            /// <linkUri>https://msdn.microsoft.com/en-us/library/9wst99a9.aspx</linkUri>
            /// </externalLink>.
            /// If you mean to use Visual Studio's default value, you must set this property to <c>null</c>.
            /// </para>
            /// <para>
            /// Always put a separate PDB for the compiler in the intermediate path to avoid
            /// conflicts with the one from the linker.
            /// </para>
            /// </remarks>
            public string CompilerPdbFilePath = "[conf.IntermediatePath]" + Path.DirectorySeparatorChar + "[conf.TargetFileFullName][conf.CompilerPdbSuffix].pdb";

            /// <summary>
            /// Gets or sets whether <see cref="CompilerPdbFilePath"/> and
            /// <see cref="LinkerPdbFilePath"/> are relative.
            /// </summary>
            public bool UseRelativePdbPath = true;

            /// <summary>
            /// Gets or sets the suffix of the manifests when building a project that uses
            /// Microsoft's C++/CX with the build option *Embed Manifest*.
            /// </summary>
            public string ManifestFileSuffix = ".intermediate.manifest";

            /// <summary>
            /// Prefix for compiled embedded resource files
            /// </summary>
            public string EmbeddedResourceOutputPrefix = string.Empty;

            /// <summary>
            /// Gets or sets the directory where the compiler will place the intermediate files.
            /// </summary>
            /// <remarks>
            /// This corresponds to the <i>Intermediate</i> directory in the Visual Studio project
            /// configuration.
            /// <para>
            /// The default value is <c>[conf.ProjectPath]/obj/[target.Platform]</c>.
            /// </para>
            /// </remarks>
            public string IntermediatePath = "[conf.ProjectPath]" + Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar + "[target.Platform]" + Path.DirectorySeparatorChar + "[target.Name]";

            /// <summary>
            /// Base Intermediate devEnv directory. Only used in csproj
            /// </summary>
            public string BaseIntermediateOutputPath = string.Empty;

            /// <summary>
            /// Gets the list of defined symbols to use when compiling the project.
            /// </summary>
            /// <remarks>
            /// Generators are allowed to add new symbols to this list when needed. For example,
            /// you don't need to explicitly add <c>_WIN32</c> to the list when building for Windows.
            /// <para>
            /// These symbols are defined during the compilation, not when the project is used as a library.
            /// The symbols that need to be defined when this project is being consumed as a
            /// library, must be added to <seealso cref="ExportDefines"/> instead.
            /// </para>
            /// </remarks>
            public Strings Defines = new Strings();

            /// <summary>
            /// Gets the list of symbols that are exported when the project is being used as a
            /// library.
            /// </summary>
            /// <remarks>
            /// Not used if the project is not a library.
            /// <para>
            /// The symbols defined in this list are not defined when building the library. You
            /// must define them in <seealso cref="Defines"/>.
            /// </para>
            /// </remarks>
            public Strings ExportDefines = new Strings();

            /// <summary>
            /// Excludes the specified files from the build. Removes the files in this list from
            /// project.SourceFiles and matches project.SourceFilesRegex.
            /// </summary>
            public Strings SourceFilesBuildExclude = new Strings();

            /// <summary>
            /// Gets a list of regular expressions that are used to filter matching source files
            /// out of the build.
            /// </summary>
            public Strings SourceFilesBuildExcludeRegex = new Strings();

            /// <summary>
            /// Gets a list of regular expressions that are used to filter matching source files
            /// into the build.
            /// </summary>
            public Strings SourceFilesFiltersRegex = new Strings();

            /// <summary>
            /// Source files that match this regex will be compiled as C Files.
            /// </summary>
            public Strings SourceFilesCompileAsCRegex = new Strings();

            /// <summary>
            /// Source files that match this regex will be compiled as CPP Files.
            /// </summary>
            public Strings SourceFilesCompileAsCPPRegex = new Strings();

            /// <summary>
            /// Source files that match this regex will be compiled as CLR Files.
            /// </summary>
            public Strings SourceFilesCompileAsCLRRegex = new Strings();

            /// <summary>
            /// Source files that match this regex will be excluded from the CLR Files list.
            /// Used on C++ projects rather than C++/CLI projects.
            /// </summary>
            public Strings SourceFilesCompileAsCLRExcludeRegex = new Strings();

            /// <summary>
            /// Source files that match this regex will be explicitly not compiled as CLR files.
            /// Used on C++/CLI projects to force certain files to be compiled without the <c>/clr</c> switch.
            /// </summary>
            public Strings SourceFilesCompileAsNonCLRRegex = new Strings();

            /// <summary>
            /// Gets a list of include paths for compiling C and C++ projects.
            /// </summary>
            /// <remarks>
            /// If the project is a library, the include paths are imported in dependent
            /// projects. Use <see cref="IncludePrivatePaths"/> if you need to use include paths
            /// that are only used to compile the library.
            /// </remarks>
            public OrderableStrings IncludePaths = new OrderableStrings();

            public OrderableStrings DependenciesIncludePaths = new OrderableStrings();

            /// <summary>
            /// Gets a list of include paths for compiling C and C++ libraries that are not
            /// shared with dependent projects.
            /// </summary>
            public OrderableStrings IncludePrivatePaths = new OrderableStrings();

            /// <summary>
            /// Gets a list of system include paths for compiling C and C++ projects. When possible, these paths are searched first when #include <> is used.
            /// </summary>
            public OrderableStrings IncludeSystemPaths = new OrderableStrings();

            public OrderableStrings DependenciesIncludeSystemPaths { get; private set; } = new OrderableStrings();

            #region Resource Includes
            /// <summary>
            /// Include paths for resource compilation.
            /// These paths will propagate via the IncludePaths DependencySetting, use ResourceIncludePrivatePaths if you want to avoid this
            /// </summary>
            public OrderableStrings ResourceIncludePaths = new OrderableStrings();

            /// <summary>
            /// Include paths for resource compilation.
            /// These paths are received from dependencies via the IncludePaths DependencySetting.
            /// </summary>
            public IEnumerable<string> DependenciesResourceIncludePaths => _dependenciesResourceIncludePaths;
            protected OrderableStrings _dependenciesResourceIncludePaths = new OrderableStrings();

            /// <summary>
            /// Include paths for resource compilation.
            /// These paths will never propagate.
            /// </summary>
            public OrderableStrings ResourceIncludePrivatePaths = new OrderableStrings();
            #endregion

            /// <summary>
            /// Gets a list of compiler options to send when calling the compiler.
            /// </summary>
            /// <remarks>
            /// Generators are allowed to transform the textual representation of the options added
            /// here so that they work with the shell of the operating system or with the makefile
            /// format.
            /// <list type="bullet">
            /// <item>The values in this list are simply concatenated, separated with spaces, sanitized
            /// for the shell, and then appended directly to the command that calls the compiler.
            /// </item>
            /// <item>
            /// They are not translated from one compiler to the other. When you
            /// use this property, you need to know which C++ compiler you're using.
            /// </item>
            /// </list>
            /// <para>
            /// This property is for the compiler. Its counterpart for the linker is
            /// <see cref="AdditionalLinkerOptions"/>.
            /// </para>
            /// </remarks>
            public OrderableStrings AdditionalCompilerOptions = new OrderableStrings();

            /// <summary>
            /// Gets a list of file extensions that are added to a Visual Studio project with the
            /// <b>None</b> build action.
            /// </summary>
            /// <remarks>
            /// Used only by the Visual Studio generators.
            /// </remarks>
            public Strings AdditionalNone = new Strings();

            /// <summary>
            /// Adds commands for VS debugger
            /// </summary>
            /// <remarks>
            /// Used only by the Visual Studio generators.
            /// </remarks>
            public string AdditionalDebuggerCommands = RemoveLineTag;

            /// <summary>
            /// Gets or sets the name of the source file for the precompiled header in C and C++
            /// projects, ie: <c>stdafx.cpp</c>. This property must be <c>null</c> for projects that don't
            /// have a precompiled header.
            /// </summary>
            /// <remarks>
            /// Both <see cref="PrecompHeader"/> and <see cref="PrecompSource"/> must be <c>null</c> if
            /// the project doesn't have precompiled headers.
            /// <para>
            /// Sharpmake assumes that a relative path here is relative to <see cref="Project.SourceRootPath"/>.
            /// If that isn't correct, you must use an absolute path.
            /// </para>
            /// </remarks>
            public string PrecompSource = null;

            /// <summary>
            /// Gets or sets the name of the precompiled header in C and C++ projects,
            /// ie: <c>stdafx.h</c>. This property must be <c>null</c> for projects that do not have a
            /// precompiled header.
            /// </summary>
            /// <remarks>
            /// Both <see cref="PrecompHeader"/> and <see cref="PrecompSource"/> must be <c>null</c> if
            /// the project doesn't have precompiled headers.
            /// <para>
            /// Sharpmake assumes that any relative path entered here is relative to
            /// <see cref="Project.SourceRootPath"/>. If that isn't correct, you must use an absolute path.
            /// </para>
            /// <note>
            /// The source files must manually include this header or you will have
            /// compiler errors. Sharpmake merely tells the compiler to expect a precompiled
            /// header. The compiler doesn't implicitly include the header.
            /// </note>
            /// </remarks>
            public string PrecompHeader = null;

            /// <summary>
            /// Gets or sets the output directory for the precompiled header's binary file in C and C++
            /// projects.
            /// </summary>
            /// <remarks>
            /// If this property is set to <c>null</c>, Sharpmake will simply write the binary file to
            /// <see cref="IntermediatePath"/>, the same as the object file.
            /// <para>
            /// If defined, precompiled headers are written to this directory instead of the intermediate directory.
            /// </para>
            /// </remarks>
            public string PrecompHeaderOutputFolder = null;

            /// <summary>
            /// Gets a list of files that don't use the precompiled headers.
            /// </summary>
            public Strings PrecompSourceExclude = new Strings();

            /// <summary>
            /// Gets a list of file extensions that don't use the precompiled headers.
            /// </summary>
            public Strings PrecompSourceExcludeExtension = new Strings();

            /// <summary>
            /// Gets the list of directories that contain source files that don't use the
            /// precompiled headers.
            /// </summary>
            public Strings PrecompSourceExcludeFolders = new Strings();

            /// <summary>
            /// List of headers passed to the preprocessor to be parsed.
            /// </summary>
            public Strings ForcedIncludes = new Strings();

            /// <summary>
            /// List of files that are built to consume WinRT Extensions.
            /// </summary>
            public Strings ConsumeWinRTExtensions = new Strings();

            /// <summary>
            /// Regex-based list of files that are built to consume WinRT Extensions.
            /// </summary>
            public Strings SourceFilesCompileAsWinRTRegex = new Strings();

            /// <summary>
            /// List of files that are excluded from being built to consume WinRT Extensions.
            /// </summary>
            public Strings ExcludeWinRTExtensions = new Strings();

            /// <summary>
            /// Regex-based list of files that are excluded from being built to consume WinRT Extensions.
            /// </summary>
            public Strings SourceFilesExcludeAsWinRTRegex = new Strings();

            /// <summary>
            /// Gets a list of files that must be compiled using the compiler's default exception settings
            /// and with exceptions enabled.
            /// </summary>
            /// <remarks>
            /// If the source file is compiled with WinRT extensions, it is implicitly added to
            /// this list.
            /// </remarks>
            public Strings SourceFilesExceptionsEnabled = new Strings();

            /// <summary>
            /// Gets a list of files that must be compiled with <c>extern C</c> exceptions enabled.
            /// </summary>
            public Strings SourceFilesExceptionsEnabledWithExternC = new Strings();

            /// <summary>
            /// Gets a list of files that must be compiled with SEH exceptions enabled.
            /// </summary>
            public Strings SourceFilesExceptionsEnabledWithSEH = new Strings();

            // The .ruleset file to use for code analysis
            public string CodeAnalysisRuleSetFilePath = RemoveLineTag;

            /// <summary>
            /// Enables (true) or disables (false) a dump of the dependency graph for this configuration.
            /// </summary>
            public bool DumpDependencyGraph = false;

            /// <summary>
            /// Adds a C or C++ source file with a specific exception setting.
            /// </summary>
            /// <param name="filename">The path of the source file.</param>
            /// <param name="exceptionSetting">The C++ exception setting.</param>
            /// <exception cref="ArgumentNullException"><paramref name="filename"/> is <c>null</c>.</exception>
            /// <exception cref="ArgumentException"><paramref name="exceptionSetting"/> is not a known value.</exception>
            /// <exception cref="Error"><paramref name="filename"/> has already been added with a different exception mode.</exception>
            /// <remarks>
            /// This is a utility method for selecting either
            /// <see cref="SourceFilesExceptionsEnabled"/>,
            /// <see cref="SourceFilesExceptionsEnabledWithExternC"/> or
            /// <see cref="SourceFilesExceptionsEnabledWithSEH"/> and for making sure that the file has
            /// not already been included with another exception setting.
            /// </remarks>
            public void AddSourceFileWithExceptionSetting(string filename, Options.Vc.Compiler.Exceptions exceptionSetting)
            {
                switch (exceptionSetting)
                {
                    case Sharpmake.Options.Vc.Compiler.Exceptions.Enable:
                        {
                            if (SourceFilesExceptionsEnabledWithExternC.Contains(filename) ||
                               SourceFilesExceptionsEnabledWithSEH.Contains(filename))
                                throw new Error("Conflicting exception settings for file " + filename);

                            SourceFilesExceptionsEnabled.Add(filename);
                        }
                        break;
                    case Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithExternC:
                        {
                            if (SourceFilesExceptionsEnabled.Contains(filename) ||
                               SourceFilesExceptionsEnabledWithSEH.Contains(filename))
                                throw new Error("Conflicting exception settings for file " + filename);

                            SourceFilesExceptionsEnabledWithExternC.Add(filename);
                        }
                        break;
                    case Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithSEH:
                        {
                            if (SourceFilesExceptionsEnabled.Contains(filename) ||
                               SourceFilesExceptionsEnabledWithExternC.Contains(filename))
                                throw new Error("Conflicting exception settings for file " + filename);

                            SourceFilesExceptionsEnabledWithSEH.Add(filename);
                        }
                        break;
                    default: throw new NotImplementedException("Exception setting for file " + filename + " not recognized");
                }
            }

            /// <summary>
            /// Gets which exception setting has been set for a given file in a C or C++ project.
            /// </summary>
            /// <param name="filename">The path of the file to examine.</param>
            /// <returns>A value from the <see cref="Options.Vc.Compiler.Exceptions"/> enumerated type that specifies which exception mode is used for the specified file.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="filename"/> is <c>null</c>.</exception>
            public Options.Vc.Compiler.Exceptions GetExceptionSettingForFile(string filename)
            {
                // If consuming WinRT, file must be compiled with exceptions enabled
                if (ConsumeWinRTExtensions.Contains(filename) || ResolvedSourceFilesWithCompileAsWinRTOption.Contains(filename))
                {
                    return Sharpmake.Options.Vc.Compiler.Exceptions.Enable;
                }

                if (SourceFilesExceptionsEnabled.Contains(filename))
                    return Sharpmake.Options.Vc.Compiler.Exceptions.Enable;
                if (SourceFilesExceptionsEnabledWithExternC.Contains(filename))
                    return Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithExternC;
                if (SourceFilesExceptionsEnabledWithSEH.Contains(filename))
                    return Sharpmake.Options.Vc.Compiler.Exceptions.EnableWithSEH;

                return Sharpmake.Options.Vc.Compiler.Exceptions.Disable;
            }

            /// <summary>
            /// Gets a list of the search directories for static libraries.
            /// </summary>
            public OrderableStrings LibraryPaths = new OrderableStrings();

            public OrderableStrings DependenciesOtherLibraryPaths = new OrderableStrings();
            public OrderableStrings DependenciesBuiltTargetsLibraryPaths = new OrderableStrings();

            public OrderableStrings DependenciesLibraryPaths
            {
                get
                {
                    var allLibraryPaths = new OrderableStrings(DependenciesBuiltTargetsLibraryPaths);
                    allLibraryPaths.AddRange(DependenciesOtherLibraryPaths);
                    return allLibraryPaths;
                }
            }

            public void AddDependencyBuiltTargetLibraryPath(string libraryPath, int orderNumber)
            {
                if (_linkState != LinkState.Linking)
                    throw new Error($"Cannot add built target lib '{libraryPath}' outside of the link process of the Project.Configuration");
                DependenciesBuiltTargetsLibraryPaths.Add(libraryPath, orderNumber);
            }

            /// <summary>
            /// Gets a list of the static libraries to link to.
            /// </summary>
            /// <remarks>
            /// This should only be used for third party libraries that are not part of the compiled
            /// source code. Libraries that are part of the compiled source code should be included
            /// by calling either
            /// <see cref="AddPublicDependency{TPROJECT}(ITarget, DependencySetting, string, int)"/>
            /// or <see cref="AddPrivateDependency{TPROJECT}(ITarget, DependencySetting, string, int)"/>.
            /// This makes things much easier because Sharpmake will automatically take care
            /// of setting the library paths, library files, include paths, and build order
            /// according to the dependency graph.
            /// <para>
            /// Unless your library name contains a <c>.</c>(dot) in its file name, you don't need to add
            /// the file extension of any library you add here. If you do, Sharpmake will
            /// automatically remove it.
            /// </para>
            /// </remarks>
            public OrderableStrings LibraryFiles = new OrderableStrings();

            /// <summary>
            /// Gets a list of "using" directories for compiling WinRT C++ extensions.
            /// </summary>
            /// <remarks>
            /// As WinRT is a Microsoft extension, this property is only used by the Visual Studio
            /// generators.
            /// </remarks>
            public OrderableStrings AdditionalUsingDirectories = new OrderableStrings();

            public OrderableStrings DependenciesOtherLibraryFiles = new OrderableStrings();
            public OrderableStrings DependenciesBuiltTargetsLibraryFiles = new OrderableStrings();

            public OrderableStrings DependenciesLibraryFiles
            {
                get
                {
                    var allLibraryFiles = new OrderableStrings(DependenciesBuiltTargetsLibraryFiles);
                    allLibraryFiles.AddRange(DependenciesOtherLibraryFiles);
                    return allLibraryFiles;
                }
            }

            public void AddDependencyBuiltTargetLibraryFile(string libraryFile, int orderNumber)
            {
                if (_linkState != LinkState.Linking)
                    throw new Error($"Cannot add built target lib '{libraryFile}' outside of the link process of the Project.Configuration");
                DependenciesBuiltTargetsLibraryFiles.Add(libraryFile, orderNumber);
            }

            public OrderableStrings DependenciesForceUsingFiles = new OrderableStrings();
            public UniqueList<Configuration> ConfigurationDependencies = new UniqueList<Configuration>();
            public UniqueList<Configuration> ForceUsingDependencies = new UniqueList<Configuration>();
            public UniqueList<Configuration> GenericBuildDependencies = new UniqueList<Configuration>();
            internal UniqueList<Configuration> BuildOrderDependencies = new UniqueList<Configuration>();

            /// <summary>
            /// Gets the list of public dependencies for .NET projects.
            /// </summary>
            /// <remarks>
            /// You should use
            /// <see cref="AddPublicDependency{TPROJECT}(ITarget, DependencySetting, string, int)"/>
            /// instead of adding elements directly into this list.
            /// </remarks>
            public List<DotNetDependency> DotNetPublicDependencies = new List<DotNetDependency>();

            /// <summary>
            /// Gets the list of private dependencies for .NET projects.
            /// </summary>
            /// <remarks>
            /// You should use
            /// <see cref="AddPrivateDependency{TPROJECT}(ITarget, DependencySetting, string, int)"/>
            /// instead of adding elements directly to this list.
            /// </remarks>
            public List<DotNetDependency> DotNetPrivateDependencies = new List<DotNetDependency>();

            /// <summary>
            /// Options passed to the librarian / archiver
            /// </summary>
            public OrderableStrings AdditionalLibrarianOptions = new OrderableStrings();

            /// <summary>
            /// Gets a list of linker options to send when calling the compiler.
            /// </summary>
            /// <remarks>
            /// Generators are allowed to transform the textual representation of the options added
            /// here so that they work with the operating system's shell or with the makefile
            /// format.
            /// <para>
            /// The values in this list are simply concatenated, separated with spaces, sanitized
            /// for the shell, and then appended directly to the command that calls the linker.
            /// </para>
            /// <para>
            /// The options added here are not translated from one linker to the other. When you
            /// use this property, you need to know which C++ compiler you're using.
            /// </para>
            /// <para>
            /// This property is for the linker. Its counterpart for the compiler is
            /// <see cref="AdditionalCompilerOptions"/>.
            /// </para>
            /// </remarks>
            public OrderableStrings AdditionalLinkerOptions = new OrderableStrings();

            public OrderableStrings AdditionalNSOFiles = new OrderableStrings();
            public string MetadataSource = null;

            public Type ExportSymbolThroughProject = null;

            /// <summary>
            /// Target path, where the output files will be compiled, ex: exe, dll, self, xex
            /// </summary>
            public string TargetPath = Path.Combine("[conf.ProjectPath]", "output", "[target.Platform]", "[conf.Name]");

            public string TargetLibraryPath = "[conf.TargetPath]";

            public bool ExportDllSymbols = true;

            /// <summary>
            /// Gets or sets whether a .NET class library generates an import library instead of a
            /// managed assembly (DLL).
            /// </summary>
            /// <remark>
            /// This property has no effect unless <see cref="Configuration.OutputType"/> is set to
            /// <see cref="OutputType.DotNetClassLibrary"/>.
            /// </remark>
            public bool CppCliExportsNativeLib = true;

            /// <summary>
            /// Gets or sets whether to skip generating a Visual Studio filter file for this project.
            /// </summary>
            public bool SkipFilterGeneration = false;

            /// <summary>
            /// Gets or sets the path of the module definition file to be passed to the linker.
            /// </summary>
            /// <remarks>
            /// This is only used by the Visual Studio generators.
            /// </remarks>
            public string ModuleDefinitionFile = null;

            /// <summary>
            /// Gets or sets the path where the blob files will be generated.
            /// </summary>
            /// <remarks>
            /// <note>
            /// FASTBuild supports blobbing via it's "unity" files and the FASTBuild
            /// generators use <see cref="FastBuildUnityPath"/> to determine where to put the unity files.
            /// </note>
            /// </remarks>
            private string _blobPath = null;
            public string BlobPath
            {
                get { return _blobPath ?? Project.BlobPath; }
                set { _blobPath = value; }
            }

            /// <summary>
            /// How many static blob files would this configuration generate
            /// </summary>
            internal int GeneratableBlobCount = 0;

            private string _fastBuildUnityPath = null;

            /// <summary>
            /// Gets or sets the path of the unity files generated by the FASTBuild build system.
            /// </summary>
            /// <remarks>
            /// This property is only used when generating FASTBuild makefiles. When using the
            /// usual compiler, use <see cref="BlobPath"/> to set the location of the blob files.
            /// </remarks>
            public string FastBuildUnityPath
            {
                get { return _fastBuildUnityPath ?? _blobPath ?? Project.FastBuildUnityPath; }
                set { _fastBuildUnityPath = value; }
            }

            /// <summary>
            /// If specified, overrides <c>Project.DefaultBlobWorkFileHeader</c>.
            /// </summary>
            public string BlobWorkFileHeader = null;

            /// <summary>
            /// If specified, overrides <c>Project.DefaultBlobWorkFileFooter</c>.
            /// </summary>
            public string BlobWorkFileFooter = null;

            /// <summary>
            /// If specified, overrides Project.BlobSize .
            /// </summary>
            public int BlobSize = 0;

            /// <summary>
            /// Gets or sets the number of "unity" files to generate when using FASTBuild.
            /// </summary>
            public int FastBuildUnityCount { get; set; }

            /// <summary>
            /// Gets or sets whether to include blobs in the project.
            /// </summary>
            public bool IncludeBlobbedSourceFiles = true;

            // Build writable files individually
            public bool FastBuildUnityInputIsolateWritableFiles = true;

            // Disable isolation when many files are writable
            public int FastBuildUnityInputIsolateWritableFilesLimit = 10;

            /// <summary>
            /// Custom Actions to do before invoking FastBuildExecutable.
            /// </summary>
            public string FastBuildCustomActionsBeforeBuildCommand = RemoveLineTag;

            /// <summary>
            /// Gets or sets the name of the FASTBuild BFF file.
            /// </summary>
            public string BffFileName = "[conf.ProjectFileName]";

            /// <summary>
            /// Gets the full file path of the FASTBuild BFF file. This includes the directory and
            /// file name.
            /// </summary>
            public string BffFullFileName => Path.Combine(ProjectPath, BffFileName);

            /// <summary>
            /// Gets or sets whether to generate a FASTBuild (.bff) file when using FASTBuild.
            /// </summary>
            /// <remarks>
            /// For projects merging multiple targets, sometimes what is wanted is to not generate FastBuild
            ///  .bff files but, instead, include any existing .bff files from the appropriate targets.
            /// </remarks>
            public bool DoNotGenerateFastBuild = false;

            // container for executable
            /// <summary>
            /// Represents a build step that invokes an executable on the file system.
            /// </summary>
            [Resolver.Resolvable]
            public class BuildStepExecutable : BuildStepBase
            {
                /// <summary>
                /// Creates a new <see cref="BuildStepExecutable"/> instance.
                /// </summary>
                /// <param name="executableFile">The executable file.</param>
                /// <param name="executableInputFileArgumentOption">The command line option that specifies the input file.</param>
                /// <param name="executableOutputFileArgumentOption">The command line option that specifies the output file.</param>
                /// <param name="executableOtherArguments">Any other command line arguments to pass to the executable.</param>
                /// <param name="executableWorkingDirectory">The working directory of the executable.</param>
                /// <param name="isNameSpecific">???</param>
                /// <param name="useStdOutAsOutput">If `true`, the output is to *stdout*.</param>
                public BuildStepExecutable(
                    string executableFile,
                    string executableInputFileArgumentOption,
                    string executableOutputFileArgumentOption,
                    string executableOtherArguments,
                    string executableWorkingDirectory,
                    bool isNameSpecific,
                    bool useStdOutAsOutput)
                    : this(executableFile,
                          executableInputFileArgumentOption,
                          executableOutputFileArgumentOption,
                          executableOtherArguments,
                          executableWorkingDirectory,
                          isNameSpecific,
                          useStdOutAsOutput,
                          false)
                {
                }

                public BuildStepExecutable(
                    string executableFile,
                    string executableInputFileArgumentOption,
                    string executableOutputFileArgumentOption,
                    string executableOtherArguments,
                    string executableWorkingDirectory = "",
                    bool isNameSpecific = false,
                    bool useStdOutAsOutput = false,
                    bool alwaysShowOutput = false)

                {
                    ExecutableFile = executableFile;
                    ExecutableInputFileArgumentOption = executableInputFileArgumentOption;
                    ExecutableOutputFileArgumentOption = executableOutputFileArgumentOption;
                    ExecutableOtherArguments = executableOtherArguments;
                    ExecutableWorkingDirectory = executableWorkingDirectory;
                    IsNameSpecific = isNameSpecific;
                    FastBuildUseStdOutAsOutput = useStdOutAsOutput;
                    FastBuildAlwaysShowOutput = alwaysShowOutput;
                }

                /// <summary>
                /// Gets or sets the name of the executable file.
                /// </summary>
                public string ExecutableFile = "";

                /// <summary>
                /// Gets or sets the command line option that specifies the input file.
                /// </summary>
                public string ExecutableInputFileArgumentOption = "";

                /// <summary>
                /// Gets or sets the command line option that specifies the output file.
                /// </summary>
                public string ExecutableOutputFileArgumentOption = "";

                /// <summary>
                /// Gets or sets any other command line option to pass to the executable.
                /// </summary>
                public string ExecutableOtherArguments = "";

                /// <summary>
                /// Gets or sets the working directory to use when calling the executable.
                /// </summary>
                public string ExecutableWorkingDirectory = "";

                /// <summary>
                /// Sets multiple files as executable input. Only supported by Bff generator.
                /// </summary>
                public Strings FastBuildExecutableInputFiles = new Strings();

                /// <summary>
                /// Gets or sets whether the output is to *stdout*.
                /// </summary>
                public bool FastBuildUseStdOutAsOutput = false;

                public bool FastBuildAlwaysShowOutput = false;

                internal override void Resolve(Resolver resolver)
                {
                    base.Resolve(resolver);

                    if (!string.IsNullOrEmpty(ExecutableInputFileArgumentOption) &&
                        FastBuildExecutableInputFiles.Count > 0)
                    {
                        throw new Error("BuildStepExecutable has both ExecutableInputFileArgumentOption and FastBuildExecutableInputFiles defined. " +
                            "These options are mutually exclusive.\n" +
                            "Executable: {0}\n" +
                            "ExecutableInputFileArgumentOption: {1}\n" +
                            "FastBuildExecutableInputFiles: {2}",
                            ExecutableFile,
                            ExecutableInputFileArgumentOption,
                            FastBuildExecutableInputFiles);
                    }
                }
            }

            [Resolver.Resolvable]
            public class BuildStepTest : BuildStepBase
            {
                public BuildStepTest(
                    string executableFile,
                    string executableArguments,
                    string outputFile,
                    string executableWorkingDirectory = "",
                    int timeOutInSecond = 0,
                    bool alwaysShowOutput = true)

                {
                    TestExecutable = executableFile;
                    TestArguments = executableArguments;
                    TestOutput = outputFile;
                    TestWorkingDir = executableWorkingDirectory;
                    TestTimeOutInSecond = timeOutInSecond;
                    TestAlwaysShowOutput = alwaysShowOutput;
                }

                public string TestExecutable = "";
                public string TestOutput = "";
                public string TestArguments = "";
                public string TestWorkingDir = "";
                public int TestTimeOutInSecond = 0;
                public bool TestAlwaysShowOutput = false;
            }

            public class FileCustomBuild
            {
                public FileCustomBuild(string description = "Copy files...")
                {
                    Description = description;
                }

                public string Description;
                public Strings CommandLines = new Strings();
                public Strings Inputs = new Strings();
                public Strings Outputs = new Strings();
                public bool LinkObjects = false;
            }

            // container for copy
            [Resolver.Resolvable]
            public class BuildStepCopy : BuildStepBase
            {
                public BuildStepCopy(BuildStepCopy buildStepCopy)
                {
                    SourcePath = buildStepCopy.SourcePath;
                    DestinationPath = buildStepCopy.DestinationPath;

                    IsFileCopy = buildStepCopy.IsFileCopy;
                    IsRecurse = buildStepCopy.IsRecurse;
                    IsNameSpecific = buildStepCopy.IsNameSpecific;
                    CopyPattern = buildStepCopy.CopyPattern;
                }

                public BuildStepCopy(string sourcePath, string destinationPath, bool isNameSpecific = false, string copyPattern = "*", bool fileCopy = true)
                {
                    SourcePath = sourcePath;
                    DestinationPath = destinationPath;

                    IsFileCopy = fileCopy;
                    IsRecurse = true;
                    IsNameSpecific = isNameSpecific;
                    CopyPattern = copyPattern;
                }
                public string SourcePath = "";
                public string DestinationPath = "";

                public bool IsFileCopy { get; set; }
                public bool IsRecurse { get; set; }
                public string CopyPattern { get; set; }

                public virtual string GetCopyCommand(string workingPath, EnvironmentVariableResolver resolver)
                {
                    string sourceRelativePath = Util.PathGetRelative(workingPath, resolver.Resolve(SourcePath));
                    string destinationRelativePath = Util.PathGetRelative(workingPath, resolver.Resolve(DestinationPath));

                    return string.Join(" ",
                        "robocopy.exe",

                        // file selection options
                        "/xo",  // /XO :: eXclude Older files.

                        // logging options
                        "/ns",  // /NS :: No Size - don't log file sizes.
                        "/nc",  // /NC :: No Class - don't log file classes.
                        "/np",  // /NP :: No Progress - don't display percentage copied.
                        "/njh", // /NJH :: No Job Header.
                        "/njs", // /NJS :: No Job Summary.
                        "/ndl", // /NDL :: No Directory List - don't log directory names.
                        "/nfl", // /NFL :: No File List - don't log file names.

                        // parameters
                        "\"" + sourceRelativePath + "\"",
                        "\"" + destinationRelativePath + "\"",
                        "\"" + CopyPattern + "\"",

                        "> nul", // direct all remaining stdout to nul

                        // Error handling: any value greater than 7 indicates that there was at least one failure during the copy operation.
                        // The type nul is used to clear the errorlevel to 0
                        // see https://ss64.com/nt/robocopy-exit.html for more info
                        "& if %ERRORLEVEL% GEQ 8 (echo Copy failed & exit 1) else (type nul>nul)"
                    );
                }
            }

            public abstract class BuildStepBase : IComparable
            {
                public bool IsNameSpecific { get; set; }

                public bool IsResolved { get; private set; } = false;

                // Override this to control the order of BuildStep execution in Build Events
                public virtual int CompareTo(object obj)
                {
                    if (obj == null)
                        return 1;

                    return 0;
                }

                internal virtual void Resolve(Resolver resolver)
                {
                    if (IsResolved)
                        return;

                    resolver.Resolve(this);

                    IsResolved = true;
                }
            }

            /// <summary>
            /// Settings for NMake projects with custom execution
            /// </summary>
            public class NMakeBuildSettings
            {
                public string BuildCommand = RemoveLineTag;
                public string RebuildCommand = RemoveLineTag;
                public string CleanCommand = RemoveLineTag;
                public string OutputFile = RemoveLineTag;

                public bool IsResolved { get; private set; } = false;

                internal void Resolve(Resolver resolver)
                {
                    if (IsResolved)
                        return;

                    BuildCommand = resolver.Resolve(BuildCommand);
                    RebuildCommand = resolver.Resolve(RebuildCommand);
                    CleanCommand = resolver.Resolve(CleanCommand);
                    OutputFile = resolver.Resolve(OutputFile);

                    IsResolved = true;
                }
            }
            public NMakeBuildSettings CustomBuildSettings = null;


            /// <summary>
            /// If specified, every obj will be output to intermediate directories corresponding to the source hierarchy.
            /// </summary>
            /// <remarks>
            /// <note type="warning">
            /// This will slow down your project's compile time!
            /// <externalLink>
            /// <linkText>See a discussion of this in StackOverflow</linkText>
            /// <linkUri>http://stackoverflow.com/a/1999344</linkUri>
            /// </externalLink>.
            /// </note>
            /// </remarks>
            public Func<string, string> ObjectFileName = null;

            /// <summary>
            /// Gets or sets the name of the current configuration.
            /// </summary>
            /// <remarks>
            /// In Visual Studio, the name of the configuration is displayed in the drop-down list.
            /// </remarks>
            public string Name = "[target.ProjectConfigurationName]";

            /// <summary>
            /// Gets or sets the base file name of the target.
            /// </summary>
            /// <remarks>
            /// Despite the name of the property, this is actually the base name. You can prepend
            /// and append suffixes using <see cref="TargetFilePrefix"/> and
            /// <see cref="TargetFileSuffix"/>.
            /// <para>
            /// The default value is the name of the project.
            /// </para>
            /// </remarks>
            public string TargetFileName = "[project.Name]";                // "system"

            /// <summary>
            /// Gets or sets the suffix to append to the target name.
            /// </summary>
            public string TargetFileSuffix = "";                            // "_rt"

            /// <summary>
            /// Gets or sets the prefix to prepend to the target name.
            /// </summary>
            public string TargetFilePrefix = "";

            /// <summary>
            /// Gets or sets the full file name of the target, without the path but with the suffix
            /// and the prefix.
            /// </summary>
            public string TargetFileFullName = "[conf.TargetFilePrefix][conf.TargetFileName][conf.TargetFileSuffix]";

            /// <summary>
            /// Gets or sets the ordering index of the target when added as a library to another
            /// project.
            /// </summary>
            public int TargetFileOrderNumber = 0;

            /// <summary>
            /// Gets or sets the ordering index of the library paths when added as a library to
            /// another project.
            /// </summary>
            public int TargetLibraryPathOrderNumber = 0;

            /// <summary>
            /// Gets or sets the list of files to copy to the output directory.
            /// </summary>
            public Strings TargetCopyFiles = new Strings();

            /// <summary>
            /// Gets or sets the list of files that the target depends on.
            /// </summary>
            public Strings TargetDependsFiles = new Strings();

            /// <summary>
            /// Gets or sets whether this configuration is included in or excluded from the build.
            /// </summary>
            public bool IsExcludedFromBuild = false;

            /// <summary>
            /// Gets or sets a custom <see cref="FileCustomBuild"/> that is used to copy
            /// dependencies after a build.
            /// </summary>
            /// <remarks>
            /// This can be used to add a custom build tool on a dummy file to copy the
            /// dependencies' DLLs and PDBs. Works better than a PostBuildStep.
            /// </remarks>
            public FileCustomBuild CopyDependenciesBuildStep = null;

            /// <summary>
            /// Gets or sets a list of shell commands to add as a prebuild script.
            /// </summary>
            public Strings EventPreBuild = new Strings();

            /// <summary>
            /// Gets or sets the name of the prebuild script (that is written to the build
            /// output).
            /// </summary>
            public string EventPreBuildDescription = "";

            /// <summary>
            /// Gets or sets whether the prebuild is excluded from the build.
            /// </summary>
            public bool EventPreBuildExcludedFromBuild = false;

            /// <summary>
            /// Gets or sets a list of <see cref="BuildStepBase"/> instances that call executables
            /// at prebuild.
            /// </summary>
            public UniqueList<BuildStepBase> EventPreBuildExe = new UniqueList<BuildStepBase>();

            /// <summary>
            /// Gets or sets a list of <see cref="BuildStepBase"/> instances that call executables
            /// at prebuild.
            /// </summary>
            public UniqueList<BuildStepBase> EventCustomPreBuildExe = new UniqueList<BuildStepBase>();

            public Dictionary<string, BuildStepBase> EventPreBuildExecute = new Dictionary<string, BuildStepBase>();

            public Dictionary<string, BuildStepBase> EventCustomPrebuildExecute = new Dictionary<string, BuildStepBase>();

            /// <summary>
            /// Gets or sets a list of shell commands to execute before linking a C or C++ project.
            /// </summary>
            public Strings EventPreLink = new Strings();

            /// <summary>
            /// Gets or sets a description to write to the build output before linking a C or C++
            /// project.
            /// </summary>
            public string EventPreLinkDescription = "";

            /// <summary>
            /// Gets or sets whether the pre-link is excluded from the build.
            /// </summary>
            public bool EventPreLinkExcludedFromBuild = false;

            /// <summary>
            /// Gets or sets a list of shell commands to execute after linking to a C or C++
            /// project.
            /// </summary>
            public Strings EventPrePostLink = new Strings();

            /// <summary>
            /// Gets or sets a description to write to the build output after linking to a C or C++
            /// project.
            /// </summary>
            public string EventPrePostLinkDescription = "";

            /// <summary>
            /// Gets or sets whether the post-link is excluded from the build.
            /// </summary>
            public bool EventPrePostLinkExcludedFromBuild = false;

            /// <summary>
            /// Gets or sets a list of shell commands to execute after building the project.
            /// </summary>
            public List<string> EventPostBuild = new List<string>();

            /// <summary>
            /// Gets or sets a description to write to the build output after building the
            /// project.
            /// </summary>
            public string EventPostBuildDescription = "";

            /// <summary>
            /// Gets or sets whether the post-build is excluded from the build.
            /// </summary>
            public bool EventPostBuildExcludedFromBuild = false;

            public UniqueList<BuildStepBase> EventPostBuildExe = new UniqueList<BuildStepBase>();
            public UniqueList<BuildStepBase> EventCustomPostBuildExe = new UniqueList<BuildStepBase>();
            public Dictionary<string, BuildStepBase> EventPostBuildExecute = new Dictionary<string, BuildStepBase>();
            public Dictionary<string, BuildStepBase> EventCustomPostBuildExecute = new Dictionary<string, BuildStepBase>();
            public HashSet<KeyValuePair<string, string>> EventPostBuildCopies = new HashSet<KeyValuePair<string, string>>(); // <path to file, destination directory>
            public BuildStepExecutable PostBuildStampExe = null;
            public BuildStepTest PostBuildStepTest = null;

            public List<string> CustomBuildStep = new List<string>();
            public string CustomBuildStepDescription = "";
            public Strings CustomBuildStepOutputs = new Strings();
            public Strings CustomBuildStepInputs = new Strings();
            public string CustomBuildStepBeforeTargets = "";
            public string CustomBuildStepAfterTargets = "";
            public string CustomBuildStepTreatOutputAsContent = ""; // should be a bool

            /// <summary>
            /// This is all the data specific to a custom build step.
            /// The ones stored in the project configuration use absolute paths
            /// but we need relative paths when we're ready to export a specific
            /// project file.
            /// </summary>
            public class CustomFileBuildStepData
            {
                /// <summary>
                /// This lets us filter which type of project files should have this custom build step.
                /// This is specifically used to deal with the limitations of different build systems.
                /// </summary>
                /// <remarks>
                /// Visual studio only supports one build action per file, so if you need both compilation and
                /// some other build steps such as QT or Documentation generation on the same file, you need to put the rule
                /// on a different input file that also depends on the real input file.
                /// <para>
                /// FASTBuild is key based, not file based. So it can have two different operations on the same file.
                /// If you need support for FASTBuild, you can make two different custom build rules with one specific to BFF 
                /// and the other excluding BFF.
                /// </para>
                /// </remarks>
                public enum ProjectFilter
                {
                    /// <summary>
                    /// The custom build step is used for both project file and FASTBuild generation.
                    /// </summary>
                    AllProjects,
                    /// <summary>
                    /// The custom build step excludes BFF.
                    /// </summary>
                    ExcludeBFF,
                    /// <summary>
                    /// The custom build step is specific to BFF 
                    /// </summary>
                    BFFOnly
                };

                /// <summary>
                /// File custom builds are bound to a specific existing file. They run when the file is changed.
                /// </summary>
                public string KeyInput = "";
                /// <summary>
                /// This is the executable for the custom build step.
                /// </summary>
                public string Executable = "";
                /// <summary>
                /// These are the arguments to pass to the executable.
                /// </summary>
                /// <remarks>
                /// We support [input] and [output] tags in the executable arguments that will auto-resolve to the relative
                /// paths to <see cref="KeyInput"/> and <see cref="Output"/>.
                /// </remarks>
                public string ExecutableArguments = "";
                /// <summary>
                /// This is what will appear in the project file under "description". It's also the key used
                /// for FASTBuild, so it should be unique per build step if you want to use FASTBuild.
                /// </summary>
                public string Description = "";
                /// <summary>
                /// For FASTBuild compatibility, we can only have one input and one output per custom command.
                /// This is what we tell the build system we're going to produce.
                /// </summary>
                public string Output = "";
                /// <summary>
                /// Not supported by FASTBuild.
                /// Additional files that will cause a re-run of this custom build step can be be specified here.
                /// </summary>
                public Strings AdditionalInputs = new Strings();
                /// <summary>
                /// Specifies whether this step should run in builds for project files or FASTBuild or both.
                /// </summary>
                public ProjectFilter Filter = ProjectFilter.AllProjects;
            }

            public class CustomFileBuildStep : CustomFileBuildStepData
            {
                // Initial resolve pass, in-place.
                public virtual void Resolve(Resolver resolver)
                {
                    KeyInput = resolver.Resolve(KeyInput);
                    Executable = resolver.Resolve(Executable);
                    Description = resolver.Resolve(Description);
                    Output = resolver.Resolve(Output);
                    foreach (var input in AdditionalInputs.Values)
                    {
                        AdditionalInputs.UpdateValue(input, resolver.Resolve(input));
                    }

                    // We don't resolve arguments yet as we need the relative directly first.
                }

                // Pre-save make-relative pass, to set all fields relative to project path.
                // This WILL get called multiple times, so it needs to write to different fields than
                // the original input.
                public virtual CustomFileBuildStepData MakePathRelative(Resolver resolver, Func<string, bool, string> MakeRelativeTool)
                {
                    var relativeData = new CustomFileBuildStepData();
                    relativeData.KeyInput = MakeRelativeTool(KeyInput, true);
                    relativeData.Executable = MakeRelativeTool(Executable, true);
                    relativeData.Output = MakeRelativeTool(Output, true);
                    using (resolver.NewScopedParameter("input", relativeData.KeyInput))
                    using (resolver.NewScopedParameter("output", relativeData.Output))
                    {
                        relativeData.ExecutableArguments = resolver.Resolve(ExecutableArguments);
                    }
                    relativeData.Description = Description;
                    foreach (var input in AdditionalInputs.Values)
                    {
                        relativeData.AdditionalInputs.Add(MakeRelativeTool(input, true));
                    }
                    relativeData.Filter = Filter;
                    return relativeData;
                }
            };
            /// <summary>
            /// Specifies a list of custom build steps that will be executed when this configuration is active.
            /// </summary>
            public List<CustomFileBuildStep> CustomFileBuildSteps = new List<CustomFileBuildStep>();

            public string EventCustomBuildDescription = "";
            public Strings EventCustomBuild = new Strings();
            public string EventCustomBuildOutputs = "";

            public string LayoutDir = "";
            public string PullMappingFile = "";
            public string PullTemporaryFolder = "";
            public Strings AdditionalManifestFiles = new Strings();
            public string LayoutExtensionFilter = "";

            // Only used by csproj
            /// <summary>
            /// Gets or sets the working directory when a C# project is started from Visual Studio.
            /// </summary>
            public string StartWorkingDirectory = string.Empty;

            /// <summary>
            /// Defines where the compiler will generate an XML documentation file at compile time.
            /// </summary>
            ///
            /// The compiler generated XML file can be distributed alongside your .NET assembly so that
            /// Visual Studio and other IDEs can use IntelliSense to show quick information about types
            /// or members.
            /// Additionally, the XML file can be run through tools like DocFX and Sandcastle
            /// to generate API reference websites
            ///
            /// The following will output an XML file in the target directory with the same root filename as the assembly
            ///
            ///     conf.XmlDocumentationFile = @"[conf.TargetPath]\[project.AssemblyName].xml";
            ///
            /// <remarks>C# only</remarks>
            public string XmlDocumentationFile = "";

            public FileCustomBuild CustomBuildForAllSources = null;
            public FileCustomBuild CustomBuildForAllIncludes = null;

            /// <summary>
            /// Gets the <see cref="Project"/> that this <see cref="Project.Configuration"/>
            /// belongs to.
            /// </summary>
            /// <remarks>
            /// If this is a C# project, <see cref="Project"/> can be safely cast to
            /// <see cref="CSharpProject"/>.
            /// </remarks>
            public Project Project { get { return Owner as Project; } }

            /// <summary>
            /// Gets or sets whether this project is deployed.
            /// </summary>
            /// <remarks>
            /// This property only applies to Visual Studio projects.
            /// </remarks>
            public bool DeployProject = false;

            /// <summary>
            /// Gets or sets whether blobbing is enabled for this configuration.
            /// </summary>
            /// <remarks>
            /// Blobbing is only used for C and C++ projects. FASTBuild uses it's own blobbing
            /// strategy (called unity files), which is enabled by setting FASTBuild properties.
            /// </remarks>
            public bool IsBlobbed = false;

            /// <summary>
            /// Gets or sets the defined symbol that tells a C++ project that it is being built
            /// using a blobbing strategy.
            /// </summary>
            /// <remarks>
            /// Blobbing is only used for C and C++ projects. FASTBuild uses it's own blobbing
            /// strategy (called unity files), which is enabled by setting FASTBuild properties.
            /// </remarks>
            public string BlobFileDefine = "";

            /// <summary>
            /// Gets or sets the Windows UAC permissions required to run the program.
            /// </summary>
            public UACExecutionLevel ApplicationPermissions = UACExecutionLevel.asInvoker;

            /// <summary>
            /// Gets or sets the defined symbol that can be used from C and C++ projects to detect
            /// that a Windows resource file (.rc) is being used.
            /// </summary>
            public string ResourceFileDefine = "";

            /// <summary>
            /// Gets the file extension for executables.
            /// </summary>
            public string ExecutableExtension { get; private set; }

            /// <summary>
            /// Gets the file extension for compressed executables, such as bundles, game packages
            /// for consoles, etc.
            /// </summary>
            public string CompressedExecutableExtension { get; private set; }

            /// <summary>
            /// Gets the file extension for shared libraries.
            /// </summary>
            // TODO: Deprecate this and create a SharedLibraryExtension property instead.
            public string DllExtension { get; private set; }

            /// <summary>
            /// Gets the file extension for program debug databases.
            /// </summary>
            public string ProgramDatabaseExtension { get; private set; }

            private string _customTargetFileExtension = null;
            public string TargetFileExtension
            {
                get
                {
                    return !string.IsNullOrEmpty(_customTargetFileExtension) ? _customTargetFileExtension :
                                  (Output == OutputType.Dll || Output == OutputType.DotNetClassLibrary ? DllExtension : CompressedExecutableExtension);
                }
                set { _customTargetFileExtension = value; }
            }


            // FastBuild configuration
            /// <summary>
            /// Gets or sets whether FASTBuild will be used to build the project.
            /// </summary>
            public bool IsFastBuild = false;

            /// <summary>
            /// List of the MasterBff files this project appears in.
            /// This is populated from the solution generator
            /// </summary>
            [Resolver.SkipResolveOnMember]
            public IEnumerable<string> FastBuildMasterBffList { get { return _fastBuildMasterBffList; } }

            internal void AddMasterBff(string masterBff)
            {
                lock (_fastBuildMasterBffListLock)
                    _fastBuildMasterBffList.Add(masterBff + FastBuildSettings.FastBuildConfigFileExtension); // for some reason we don't get the extension...
            }

            [Resolver.SkipResolveOnMember]
            private readonly Strings _fastBuildMasterBffList = new Strings();
            private readonly object _fastBuildMasterBffListLock = new object();

            [Obsolete("Sharpmake will determine the projects to build.")]
            public bool IsMainProject = false;

            /// <summary>
            /// Gets or sets whether FASTBuild blobs (unities) will be used in the build.
            /// </summary>
            public bool FastBuildBlobbed = true;

            [Obsolete("Use FastBuildDistribution instead.")]
            public bool FastBuildDisableDistribution = false;

            /// <summary>
            /// Gets or sets whether FASTBuild tasks will be distributed on the network.
            /// </summary>
            // Is that it? (brousseau)
            public bool FastBuildDistribution = true;

            /// <summary>
            /// Gets or sets whether FASTBuild will use cached results to accelerate the build.
            /// </summary>
            /// <remarks>
            /// If caching is allowed, FASTBuild will use the value specified in
            /// <see cref="FastBuildSettings.CacheType"/>.
            /// </remarks>
            public bool FastBuildCacheAllowed = true;

            /// <summary>
            /// Gets or sets the strategy to use to select files that are blobbed.
            /// </summary>
            public InputFileStrategy FastBuildBlobbingStrategy = InputFileStrategy.Exclude;

            /// <summary>
            /// Gets or sets the strategy to use to select files that are not blobbed.
            /// </summary>
            public InputFileStrategy FastBuildNoBlobStrategy = InputFileStrategy.Include;

            /// <summary>
            /// Gets or sets the generic criteria by which files are deoptimized (compiled individually)
            /// by FASTBuild.
            /// </summary>
            public DeoptimizationWritableFiles FastBuildDeoptimization = DeoptimizationWritableFiles.NoDeoptimization;

            /// <summary>
            /// Gets or sets custom command line arguments to pass to FASTBuild when building the
            /// project with this configuration.
            /// </summary>
            public string FastBuildCustomArgs = string.Empty;

            // If true, remove the source files from a FastBuild project's associated vcxproj file.
            public bool StripFastBuildSourceFiles = true;

            private Dictionary<KeyValuePair<Type, ITarget>, DependencySetting> _dependenciesSetting = new Dictionary<KeyValuePair<Type, ITarget>, DependencySetting>();

            // These dependencies will not be propagated to other projects that depend on us
            internal IDictionary<Type, ITarget> UnResolvedPrivateDependencies { get; } = new Dictionary<Type, ITarget>();
            // These dependencies will be propagated to other dependent projects, but not across dll dependencies.
            internal IDictionary<Type, ITarget> UnResolvedProtectedDependencies { get; } = new Dictionary<Type, ITarget>();
            // These dependencies are always propagated to other dependent projects.
            internal Dictionary<Type, ITarget> UnResolvedPublicDependencies { get; } = new Dictionary<Type, ITarget>();

            private Strings _resolvedTargetCopyFiles = new Strings();

            /// <summary>
            /// Gets the list of resolved files to copy.
            /// </summary>
            public IEnumerable<string> ResolvedTargetCopyFiles => _resolvedTargetCopyFiles;

            private Strings _resolvedTargetDependsFiles = new Strings();

            /// <summary>
            /// Gets the list of resolved dependency files.
            /// </summary>
            public IEnumerable<string> ResolvedTargetDependsFiles => _resolvedTargetDependsFiles;

            private UniqueList<BuildStepBase> _resolvedEventPreBuildExe = new UniqueList<BuildStepBase>();

            /// <summary>
            /// Gets the list of resolved pre-build executables.
            /// </summary>
            public IEnumerable<BuildStepBase> ResolvedEventPreBuildExe => _resolvedEventPreBuildExe.SortedValues;

            private UniqueList<BuildStepBase> _resolvedEventPostBuildExe = new UniqueList<BuildStepBase>();

            /// <summary>
            /// Gets the list of resolved post-build executables.
            /// </summary>
            public IEnumerable<BuildStepBase> ResolvedEventPostBuildExe => _resolvedEventPostBuildExe.SortedValues;

            private UniqueList<BuildStepBase> _resolvedEventCustomPreBuildExe = new UniqueList<BuildStepBase>();
            public IEnumerable<BuildStepBase> ResolvedEventCustomPreBuildExe => _resolvedEventCustomPreBuildExe.SortedValues;

            private UniqueList<BuildStepBase> _resolvedEventCustomPostBuildExe = new UniqueList<BuildStepBase>();
            public IEnumerable<BuildStepBase> ResolvedEventCustomPostBuildExe => _resolvedEventCustomPostBuildExe.SortedValues;

            private UniqueList<BuildStepBase> _resolvedExecFiles = new UniqueList<BuildStepBase>();
            public IEnumerable<BuildStepBase> ResolvedExecFiles => _resolvedExecFiles;

            private UniqueList<BuildStepBase> _resolvedExecDependsFiles = new UniqueList<BuildStepBase>();
            public IEnumerable<BuildStepBase> ResolvedExecDependsFiles => _resolvedExecDependsFiles;


            private string _ProjectGuid = null;

            /// <summary>
            /// Gets or sets the GUID of the Visual Studio project.
            /// </summary>
            /// <remarks>
            /// This is only relevant to Visual Studio generators.
            /// <para>
            /// This property coerces any value set to it to use an uppercase
            /// `00000000-0000-0000-0000-000000000000` format for the GUID.
            /// </para>
            /// </remarks>
            public string ProjectGuid
            {
                get { return _ProjectGuid; }
                set
                {
                    if (_ProjectGuid != value)
                    {
                        // Makes sure that the GUID is formatted correctly.
                        var parsedGuid = Guid.Parse(value);
                        _ProjectGuid = parsedGuid.ToString("D").ToUpperInvariant();
                    }
                }
            }

            /// <summary>
            /// Gets or sets the full file name of the project.
            /// </summary>
            public string ProjectFullFileNameWithExtension = null;

            public void GeneratorSetGeneratedInformation(string executableExtension, string compressedExecutableExtension, string dllExtension, string programDatabaseExtension)
            {
                ExecutableExtension = executableExtension;
                CompressedExecutableExtension = compressedExecutableExtension;
                DllExtension = dllExtension;
                ProgramDatabaseExtension = programDatabaseExtension;
            }

            public Strings ResolvedSourceFilesBuildExclude = new Strings();

            public Strings ResolvedSourceFilesBlobExclude = new Strings();

            public Strings ResolvedSourceFilesGenerateXmlDocumentationExclude = new Strings();

            public Strings ResolvedSourceFilesWithCompileAsCOption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsCPPOption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsCLROption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsNonCLROption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsWinRTOption = new Strings();
            public Strings ResolvedSourceFilesWithExcludeAsWinRTOption = new Strings();

            public bool NeedsAppxManifestFile = false;
            public string AppxManifestFilePath = "[conf.TargetPath]/[project.Name].appxmanifest";

            public Strings PRIFilesExtensions = new Strings();

            /// <summary>
            /// Generate relative paths in places where it would be otherwise beneficial to use absolute paths.
            /// </summary>
            public bool PreferRelativePaths = true;

            internal override void Construct(object owner, ITarget target)
            {
                base.Construct(owner, target);

                System.Threading.Interlocked.Increment(ref s_count);

                if (target.TestFragment(Optimization.Release) || target.TestFragment(Optimization.Retail))
                    DefaultOption = Sharpmake.Options.DefaultTarget.Release;
                Project project = (Project)owner;

                // Change Output default for Export
                if (project.SharpmakeProjectType == ProjectTypeAttribute.Export)
                    Output = OutputType.None;
            }

            internal void Resolve(Resolver resolver)
            {
                if (PrecompHeader == null && PrecompSource != null)
                    throw new Error("Incoherent settings for {0} : PrecompHeader is null but PrecompSource is not", ToString());
                // TODO : Is it OK to comment this or is it a hack ?
                //if (PrecompHeader != null && PrecompSource == null)
                //    throw new Error("Incoherent settings for {0} : PrecompSource is null but PrecompHeader is not", ToString());

                resolver.SetParameter("conf", this);
                resolver.SetParameter("target", Target);
                resolver.Resolve(this);

                Util.ResolvePath(Project.SharpmakeCsPath, ref ProjectPath);
                if (DebugBreaks.ShouldBreakOnProjectPath(DebugBreaks.Context.Resolving, Path.Combine(ProjectPath, ProjectFileName) + (Project is CSharpProject ? ".csproj" : ".vcxproj"), this))
                    System.Diagnostics.Debugger.Break();
                Util.ResolvePath(Project.SharpmakeCsPath, ref IntermediatePath);
                Util.ResolvePath(Project.SharpmakeCsPath, ref LibraryPaths);
                Util.ResolvePathAndFixCase(Project.SharpmakeCsPath, ref TargetCopyFiles);
                Util.ResolvePath(Project.SharpmakeCsPath, ref TargetDependsFiles);
                Util.ResolvePath(Project.SharpmakeCsPath, ref TargetPath);
                Util.ResolvePath(Project.SharpmakeCsPath, ref TargetLibraryPath);
                Util.ResolvePath(Project.SharpmakeCsPath, ref AdditionalUsingDirectories);
                if (_blobPath != null)
                    Util.ResolvePath(Project.SharpmakeCsPath, ref _blobPath);

                // workaround for export projects: they do not generate pdb, so no need to resolve their paths
                if (Project.SharpmakeProjectType != ProjectTypeAttribute.Export)
                {
                    // Reset to the default if the script set it to an empty string.
                    if (!string.IsNullOrEmpty(LinkerPdbFilePath))
                        Util.ResolvePath(Project.SharpmakeCsPath, ref LinkerPdbFilePath);
                    if (!string.IsNullOrEmpty(CompilerPdbFilePath))
                        Util.ResolvePath(Project.SharpmakeCsPath, ref CompilerPdbFilePath);
                }
                if (PrecompHeaderOutputFolder != null)
                    Util.ResolvePath(Project.SharpmakeCsPath, ref PrecompHeaderOutputFolder);

                Util.ResolvePath(Project.SourceRootPath, ref SourceFilesBuildExclude);
                Util.ResolvePath(Project.SourceRootPath, ref IncludePaths);
                Util.ResolvePath(Project.SourceRootPath, ref IncludePrivatePaths);
                Util.ResolvePath(Project.SourceRootPath, ref PrecompSourceExclude);
                Util.ResolvePath(Project.SourceRootPath, ref PrecompSourceExcludeFolders);
                Util.ResolvePath(Project.SourceRootPath, ref ConsumeWinRTExtensions);
                Util.ResolvePath(Project.SourceRootPath, ref ExcludeWinRTExtensions);
                Util.ResolvePath(Project.SourceRootPath, ref ResourceIncludePaths);
                Util.ResolvePath(Project.SourceRootPath, ref ResourceIncludePrivatePaths);
                Util.ResolvePath(Project.SourceRootPath, ref SourceFilesExceptionsEnabled);
                Util.ResolvePath(Project.SourceRootPath, ref SourceFilesExceptionsEnabledWithExternC);
                Util.ResolvePath(Project.SourceRootPath, ref SourceFilesExceptionsEnabledWithSEH);
                Util.ResolvePath(Project.SourceRootPath, ref AdditionalManifestFiles);

                if (ModuleDefinitionFile != null)
                {
                    Util.ResolvePath(Project.SourceRootPath, ref ModuleDefinitionFile);
                }

                if (Project.IsFileNameToLower)
                {
                    ProjectFileName = ProjectFileName.ToLowerInvariant();
                    BffFileName = BffFileName.ToLowerInvariant();
                }

                if (Project.IsTargetFileNameToLower)
                {
                    TargetFileName = TargetFileName.ToLowerInvariant();
                    TargetFileFullName = TargetFileFullName.ToLowerInvariant();
                    TargetFileSuffix = TargetFileSuffix.ToLowerInvariant();
                    TargetFilePrefix = TargetFilePrefix.ToLowerInvariant();
                }

                _resolvedTargetDependsFiles.AddRange(TargetDependsFiles);
                _resolvedTargetCopyFiles.AddRange(TargetCopyFiles);

                foreach (var tuple in new[] {
                    Tuple.Create(EventPreBuildExe,        _resolvedEventPreBuildExe),
                    Tuple.Create(EventPostBuildExe,       _resolvedEventPostBuildExe),
                    Tuple.Create(EventCustomPreBuildExe,  _resolvedEventCustomPreBuildExe),
                    Tuple.Create(EventCustomPostBuildExe, _resolvedEventCustomPostBuildExe),
                })
                {
                    UniqueList<BuildStepBase> eventsToResolve = tuple.Item1;
                    UniqueList<BuildStepBase> resolvedEvents = tuple.Item2;

                    foreach (BuildStepBase eventToResolve in eventsToResolve)
                        eventToResolve.Resolve(resolver);

                    resolvedEvents.AddRange(eventsToResolve);
                }

                foreach (var customFileBuildStep in CustomFileBuildSteps)
                {
                    customFileBuildStep.Resolve(resolver);
                    Util.ResolvePath(Project.SourceRootPath, ref customFileBuildStep.KeyInput);
                    Util.ResolvePath(Project.SourceRootPath, ref customFileBuildStep.Executable);
                    Util.ResolvePath(Project.SourceRootPath, ref customFileBuildStep.Output);
                    Util.ResolvePath(Project.SourceRootPath, ref customFileBuildStep.AdditionalInputs);
                }

                if (CustomBuildSettings != null)
                {
                    CustomBuildSettings.Resolve(resolver);
                    Util.ResolvePath(Project.SourceRootPath, ref CustomBuildSettings.OutputFile);
                }

                foreach (var option in Options)
                {
                    var pathOption = option as Options.PathOption;
                    if (pathOption != null)
                    {
                        pathOption.Path = resolver.Resolve(pathOption.Path);
                        Util.ResolvePath(Project.SourceRootPath, ref pathOption.Path);
                    }
                }

                foreach (var eventDictionary in new[]{
                    EventPreBuildExecute,
                    EventCustomPrebuildExecute,
                    EventPostBuildExecute,
                    EventCustomPostBuildExecute
                })
                {
                    foreach (KeyValuePair<string, BuildStepBase> eventPair in eventDictionary)
                        eventPair.Value.Resolve(resolver);
                }

                if (PostBuildStampExe != null)
                    PostBuildStampExe.Resolve(resolver);

                if (PostBuildStepTest != null)
                    PostBuildStepTest.Resolve(resolver);

                string dependencyExtension = Util.GetProjectFileExtension(this);
                ProjectFullFileNameWithExtension = ProjectFullFileName + dependencyExtension;

                if (string.IsNullOrEmpty(ProjectGuid) && Project.SharpmakeProjectType != ProjectTypeAttribute.Compile)
                    ProjectGuid = Util.BuildGuid(ProjectFullFileNameWithExtension, Project.GuidReferencePath);

                if (PrecompHeader != null)
                    PrecompHeader = Util.SimplifyPath(PrecompHeader);
                if (PrecompSource != null)
                    PrecompSource = Util.SimplifyPath(PrecompSource);

                resolver.RemoveParameter("conf");
                resolver.RemoveParameter("target");
            }

            private void SetDependency(
                Type projectType,
                ITarget target,
                DependencySetting value
            )
            {
                KeyValuePair<Type, ITarget> pair = new KeyValuePair<Type, ITarget>(projectType, target);
                DependencySetting previousValue;

                if (value < 0) //LCTODO remove when the deprecated dependency settings are removed
                    value = DependencySetting.OnlyBuildOrder;

                if (_dependenciesSetting.TryGetValue(pair, out previousValue) && value != previousValue)
                {
                    _dependenciesSetting[pair] = value | previousValue;
                }
                else
                {
                    _dependenciesSetting[pair] = value;
                }
            }

            // These dependencies are always propagated to other dependent projects.
            public void AddPublicDependency<TPROJECT>(
                ITarget target,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPublicDependency(target, typeof(TPROJECT), dependencySetting, sourceFilePath, sourceLineNumber);
            }

            public virtual void AddPublicDependency(
                ITarget target,
                Type projectType,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                if (target == null)
                    return;
                if (HaveDependency(projectType))
                    throw new Error(Util.FormatCallerInfo(sourceFilePath, sourceLineNumber)
                                    + "error: Project configuration {0} already contains dependency to {1} for target {2}",
                                    Owner.GetType().ToNiceTypeName(),
                                    projectType.ToNiceTypeName(),
                                    target.ToString());
                UnResolvedPublicDependencies.Add(projectType, target);
                SetDependency(projectType, target, dependencySetting);
            }

            // These dependencies will be propagated to other dependent projects, but not across dll dependencies.
            [Obsolete("Protected dependencies are deprecated, please use public/private instead.", error: false)]
            public void AddProtectedDependency<TPROJECT>(
                ITarget target,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPublicDependency(target, typeof(TPROJECT), dependencySetting, sourceFilePath, sourceLineNumber);
            }

            [Obsolete("Protected dependencies are deprecated, please use public/private instead.", error: false)]
            public void AddProtectedDependency(
                ITarget target,
                Type projectType,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPublicDependency(target, projectType, dependencySetting, sourceFilePath, sourceLineNumber);
            }

            // These dependencies will never be propagated to other projects that depend on us
            public void AddPrivateDependency<TPROJECT>(
                ITarget target,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPrivateDependency(target, typeof(TPROJECT), dependencySetting, sourceFilePath, sourceLineNumber);
            }

            public virtual void AddPrivateDependency(
                ITarget target,
                Type projectType,
                DependencySetting dependencySetting = DependencySetting.Default,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                if (target == null)
                    return;
                if (HaveDependency(projectType))
                    throw new Error(Util.FormatCallerInfo(sourceFilePath, sourceLineNumber)
                                    + "error: Project configuration {0} already contains dependency to {1} for target {2}",
                                    Owner.GetType().ToNiceTypeName(),
                                    projectType.ToNiceTypeName(),
                                    target.ToString());

                UnResolvedPrivateDependencies.Add(projectType, target);
                SetDependency(projectType, target, dependencySetting);
            }

            // These dependencies will only be added to solutions for build ordering
            [Obsolete("Solution only dependencies are deprecated, please use Private with OnlyBuildOrder flag instead.", error: false)]
            public void AddSolutionOnlyDependency<TPROJECT>(
                ITarget target,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPrivateDependency(target, typeof(TPROJECT), DependencySetting.OnlyBuildOrder, sourceFilePath, sourceLineNumber);
            }
            [Obsolete("Solution only dependencies are deprecated, please use Private with OnlyBuildOrder flag instead.", error: false)]
            public void AddSolutionOnlyDependency(
                ITarget target,
                Type projectType,
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                AddPrivateDependency(target, projectType, DependencySetting.OnlyBuildOrder, sourceFilePath, sourceLineNumber);
            }

            public bool HaveDependency<TPROJECT>()
            {
                return HaveDependency(typeof(TPROJECT));
            }
            public bool HaveDependency(Type projectType)
            {
                return UnResolvedPrivateDependencies.ContainsKey(projectType) || UnResolvedProtectedDependencies.ContainsKey(projectType) || UnResolvedPublicDependencies.ContainsKey(projectType);
            }

            /// <summary>
            /// Gets the dependency settings configuration for the given project type of this configuration.
            /// </summary>
            /// <param name="projectType"> The project type.</param>
            /// <returns>The specified project's dependency settings with related flags activated.
            /// </returns>
            public DependencySetting GetDependencySetting(Type projectType)
            {
                DependencySetting dependencyInheritance = DependencySetting.OnlyBuildOrder;
                foreach (var dependency in _dependenciesSetting)
                {
                    if (dependency.Key.Key == projectType)
                        dependencyInheritance |= dependency.Value;
                }
                return dependencyInheritance;
            }

            private Configuration GetDependencyConfiguration(Builder builder, Configuration visitedConfiguration, KeyValuePair<Type, ITarget> pair)
            {
                Project dependencyProject = builder.GetProject(pair.Key);
                if (dependencyProject == null)
                    throw new Error("resolving dependencies for {0}: cannot find project dependency of type {1} induced by {2}",
                        Owner.GetType().ToNiceTypeName(), pair.Key.ToNiceTypeName(), visitedConfiguration.ToString());

                Configuration dependencyConf = dependencyProject.GetConfiguration(pair.Value);
                if (dependencyConf == null)
                {
                    var messageBuilder = new System.Text.StringBuilder();

                    messageBuilder.AppendFormat(
                            "resolving dependencies for {0}: cannot find dependency project configuration {1} in project {2} induced by {3}",
                            Owner.GetType().ToNiceTypeName(),
                            pair.Value,
                            pair.Key.ToNiceTypeName(),
                            visitedConfiguration.ToString()
                    );
                    if (pair.Value.GetType() == dependencyProject.Targets.TargetType)
                    {
                        messageBuilder.AppendFormat(
                            ".  The target type is correct.  The error can be caused by missing calls to AddTargets or unwanted calls to AddFragmentMask in the constructor of {0}.",
                            dependencyProject.GetType().ToNiceTypeName()
                        );
                    }
                    else
                    {
                        messageBuilder.AppendFormat(
                            ".  Are you passing the appropriate target type in AddDependency<{0}>(...)?  It should be type {1}.",
                            dependencyProject.GetType().ToNiceTypeName(),
                            dependencyProject.Targets.TargetType.ToNiceTypeName()
                        );
                    }
                    messageBuilder.AppendLine();

                    if (dependencyProject.Configurations.Any())
                    {
                        messageBuilder.AppendLine("Project configurations are:");
                        int i = 0;
                        foreach (var conf in dependencyProject.Configurations)
                            messageBuilder.AppendLine(++i + "/" + dependencyProject.Configurations.Count + " " + conf.ToString());
                    }
                    else
                    {
                        messageBuilder.AppendLine("The project does not contain any configurations!");
                    }

                    Trace.WriteLine(messageBuilder.ToString());
                    Debugger.Break();

                    throw new Error(messageBuilder.ToString());
                }

                if (!dependencyConf.Target.IsEqualTo(pair.Value))
                    throw new Error(
                        "resolving dependencies for {0}: project {1} cannot depends other project on many target: {2} {3} (induced by {4})",
                        Owner.GetType().ToNiceTypeName(), dependencyProject.GetType().ToNiceTypeName(), dependencyConf.Target, pair.Value, visitedConfiguration.ToString());

                return dependencyConf;
            }

            private void GetRecursiveDependencies(
                HashSet<Configuration> resolved,
                HashSet<Configuration> unresolved
            )
            {
                foreach (Configuration c in ResolvedDependencies)
                {
                    if (resolved.Contains(c))
                        continue;

                    if (!unresolved.Add(c))
                        throw new Error($"Cyclic dependency detected while following dependency chain of configuration: {this}");

                    c.GetRecursiveDependencies(resolved, unresolved);

                    resolved.Add(c);
                    unresolved.Remove(c);
                }
            }

            internal List<Configuration> GetRecursiveDependencies()
            {
                var result = new HashSet<Configuration>();
                GetRecursiveDependencies(result, new HashSet<Configuration>());

                return result.ToList();
            }

            public DotNetReferenceCollection DotNetReferences = new DotNetReferenceCollection();

            public Strings ProjectReferencesByPath = new Strings();
            public Strings ReferencesByName = new Strings();
            public Strings ReferencesByNameExternal = new Strings();
            public Strings ReferencesByPath = new Strings();
            public string ConditionalReferencesByPathCondition = string.Empty;
            public Strings ConditionalReferencesByPath = new Strings();
            public Strings ForceUsingFiles = new Strings();

            public Strings CustomPropsFiles = new Strings();  // vs2010+ .props files
            public Strings CustomTargetsFiles = new Strings();  // vs2010+ .targets files

            // NuGet packages (only C# for now)
            public PackageReferences ReferencesByNuGetPackage = new PackageReferences();

            public bool? ReferenceOutputAssembly = null;

            private List<Configuration> _resolvedDependencies;
            public IEnumerable<Configuration> ResolvedDependencies => _resolvedDependencies;

            private List<Configuration> _resolvedPrivateDependencies;
            public IEnumerable<Configuration> ResolvedPrivateDependencies => _resolvedPrivateDependencies;

            private List<Configuration> _resolvedPublicDependencies;
            public IEnumerable<Configuration> ResolvedPublicDependencies => _resolvedPublicDependencies;

            private static int SortConfigurationForLink(Configuration l, Configuration r)
            {
                if (l.Project.DependenciesOrder != r.Project.DependenciesOrder)
                    return l.Project.DependenciesOrder.CompareTo(r.Project.DependenciesOrder);

                return string.Compare(l.Project.FullClassName, r.Project.FullClassName, StringComparison.Ordinal);
            }

            internal class DependencyNode
            {
                internal DependencyNode(Configuration inConfiguration, DependencySetting inDependencySetting)
                {
                    _configuration = inConfiguration;
                    _dependencySetting = inDependencySetting;
                }

                internal Configuration _configuration;
                internal DependencySetting _dependencySetting;
                internal Dictionary<DependencyNode, DependencyType> _childNodes = new Dictionary<DependencyNode, DependencyType>();
            }

            public class VcxprojUserFileSettings
            {
                public string LocalDebuggerCommand = RemoveLineTag;
                public string LocalDebuggerCommandArguments = RemoveLineTag;
                public string LocalDebuggerEnvironment = RemoveLineTag;
                public string LocalDebuggerWorkingDirectory = RemoveLineTag;
                public string RemoteDebuggerCommand = RemoveLineTag;
                public string RemoteDebuggerCommandArguments = RemoveLineTag;
                public string RemoteDebuggingMode = RemoveLineTag;
                public string RemoteDebuggerWorkingDirectory = RemoveLineTag;
                public bool OverwriteExistingFile = true;
            }

            public VcxprojUserFileSettings VcxprojUserFile = null;

            public class CsprojUserFileSettings
            {
                public enum StartActionSetting
                {
                    Project,
                    Program,
                    URL
                }

                public StartActionSetting StartAction = StartActionSetting.Project;
                public string StartProgram = RemoveLineTag;
                public string StartURL = RemoveLineTag;
                public string StartArguments = RemoveLineTag;
                public string WorkingDirectory = RemoveLineTag;
                public bool OverwriteExistingFile = true;
                public bool EnableUnmanagedDebug = false;
            }
            public CsprojUserFileSettings CsprojUserFile = null;

            internal class PropagationSettings
            {
                internal PropagationSettings(DependencySetting inDependencySetting, bool inIsImmediate, bool inHasPublicPathToRoot, bool inHasPublicPathToImmediate, bool inGoesThroughDLL)
                {
                    _dependencySetting = inDependencySetting;
                    _isImmediate = inIsImmediate;
                    _hasPublicPathToRoot = inHasPublicPathToRoot;
                    _hasPublicPathToImmediate = inHasPublicPathToImmediate;
                    _goesThroughDLL = inGoesThroughDLL;
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj)) return false;
                    if (ReferenceEquals(this, obj)) return true;
                    if (obj.GetType() != GetType()) return false;

                    PropagationSettings other = (PropagationSettings)obj;

                    return _dependencySetting == other._dependencySetting &&
                           _isImmediate == other._isImmediate &&
                           _hasPublicPathToRoot == other._hasPublicPathToRoot &&
                           _hasPublicPathToImmediate == other._hasPublicPathToImmediate &&
                           _goesThroughDLL == other._goesThroughDLL;
                }

                public override int GetHashCode()
                {
                    unchecked // Overflow is fine, just wrap
                    {
                        int hash = 17;
                        hash = hash * 23 + _dependencySetting.GetHashCode();
                        hash = hash * 23 + _isImmediate.GetHashCode();
                        hash = hash * 23 + _hasPublicPathToRoot.GetHashCode();
                        hash = hash * 23 + _hasPublicPathToImmediate.GetHashCode();
                        hash = hash * 23 + _goesThroughDLL.GetHashCode();
                        return hash;
                    }
                }

                internal readonly DependencySetting _dependencySetting;
                internal readonly bool _isImmediate;
                internal readonly bool _hasPublicPathToRoot;
                internal readonly bool _hasPublicPathToImmediate;
                internal readonly bool _goesThroughDLL;
            }

            internal void Link(Builder builder)
            {
                Trace.Assert(_linkState == LinkState.NotLinked);
                _linkState = LinkState.Linking;

                if (builder.DumpDependencyGraph && !Project.IsFastBuildAll)
                {
                    DependencyTracker.Instance.AddDependency(DependencyType.Public, Project, this, UnResolvedPublicDependencies, _dependenciesSetting);
                    DependencyTracker.Instance.AddDependency(DependencyType.Private, Project, this, UnResolvedPrivateDependencies, _dependenciesSetting);
                }

                // Check if we need to add dependencies on libs that we compile (in the current solution)
                bool explicitDependenciesGlobal = true;
                if (IsFastBuild)
                {
                    explicitDependenciesGlobal = Sharpmake.Options.GetObject<Options.Vc.Linker.UseLibraryDependencyInputs>(this) != Sharpmake.Options.Vc.Linker.UseLibraryDependencyInputs.Enable;
                }
                else
                {
                    explicitDependenciesGlobal = Sharpmake.Options.GetObject<Options.Vc.Linker.LinkLibraryDependencies>(this) != Sharpmake.Options.Vc.Linker.LinkLibraryDependencies.Enable;
                }

                // create a tree of dependency from this configuration
                DependencyNode rootNode = BuildDependencyNodeTree(builder, this);

                HashSet<Configuration> resolvedPublicDependencies = new HashSet<Configuration>();
                HashSet<Configuration> resolvedPrivateDependencies = new HashSet<Configuration>();

                var resolvedDotNetPublicDependencies = new HashSet<DotNetDependency>();
                var resolvedDotNetPrivateDependencies = new HashSet<DotNetDependency>();

                var visitedNodes = new Dictionary<DependencyNode, List<PropagationSettings>>();
                var visitingNodes = new Stack<Tuple<DependencyNode, PropagationSettings>>();
                visitingNodes.Push(Tuple.Create(rootNode, new PropagationSettings(DependencySetting.Default, true, true, true, false)));

                while (visitingNodes.Count > 0)
                {
                    var visitedTuple = visitingNodes.Pop();

                    var visitedNode = visitedTuple.Item1;
                    var propagationSetting = visitedTuple.Item2;

                    bool nodeAlreadyVisited = visitedNodes.ContainsKey(visitedNode);
                    if (nodeAlreadyVisited && visitedNodes[visitedNode].Contains(propagationSetting))
                        continue;

                    if (!nodeAlreadyVisited)
                        visitedNodes[visitedNode] = new List<PropagationSettings>();
                    visitedNodes[visitedNode].Add(propagationSetting);

                    Configuration dependency = visitedNode._configuration;

                    bool isRoot = visitedNode == rootNode;
                    bool isImmediate = propagationSetting._isImmediate;
                    bool hasPublicPathToRoot = propagationSetting._hasPublicPathToRoot;
                    bool hasPublicPathToImmediate = propagationSetting._hasPublicPathToImmediate;
                    bool goesThroughDLL = propagationSetting._goesThroughDLL;

                    foreach (var childNode in visitedNode._childNodes)
                    {
                        var childTuple = Tuple.Create(
                            childNode.Key,
                            new PropagationSettings(
                                isRoot ? childNode.Key._dependencySetting : (propagationSetting._dependencySetting & childNode.Key._dependencySetting), // propagate the parent setting by masking it
                                isRoot, // only children of root are immediate
                                (isRoot || hasPublicPathToRoot) && childNode.Value == DependencyType.Public,
                                (isImmediate || hasPublicPathToImmediate) && childNode.Value == DependencyType.Public,
                                !isRoot && (goesThroughDLL || visitedNode._configuration.Output == OutputType.Dll)
                            )
                        );

                        visitingNodes.Push(childTuple);
                    }

                    if (isRoot)
                        continue;

                    if (hasPublicPathToRoot)
                    {
                        resolvedPrivateDependencies.Remove(dependency);
                        resolvedPublicDependencies.Add(dependency);
                    }
                    else if (!resolvedPublicDependencies.Contains(dependency))
                    {
                        resolvedPrivateDependencies.Add(dependency);
                    }

                    bool isExport = dependency.Project.SharpmakeProjectType == ProjectTypeAttribute.Export;
                    bool compile = dependency.Project.SharpmakeProjectType == ProjectTypeAttribute.Generate ||
                                   dependency.Project.SharpmakeProjectType == ProjectTypeAttribute.Compile;

                    var dependencySetting = propagationSetting._dependencySetting;
                    if (dependencySetting != DependencySetting.OnlyBuildOrder)
                    {
                        _resolvedEventPreBuildExe.AddRange(dependency.EventPreBuildExe);
                        _resolvedEventPostBuildExe.AddRange(dependency.EventPostBuildExe);
                        _resolvedEventCustomPreBuildExe.AddRange(dependency.EventCustomPreBuildExe);
                        _resolvedEventCustomPostBuildExe.AddRange(dependency.EventCustomPostBuildExe);
                        _resolvedTargetCopyFiles.AddRange(dependency.TargetCopyFiles);
                        _resolvedTargetDependsFiles.AddRange(dependency.TargetDependsFiles);
                        _resolvedExecDependsFiles.AddRange(dependency.EventPreBuildExe);
                        _resolvedExecDependsFiles.AddRange(dependency.EventPostBuildExe);
                    }
                    else if (Output == OutputType.None && isExport == false)
                    {
                        GenericBuildDependencies.Add(dependency);
                    }

                    if (dependency.Output == OutputType.Lib || dependency.Output == OutputType.Dll || dependency.Output == OutputType.None)
                    {
                        bool wantIncludePaths = isImmediate || hasPublicPathToImmediate;
                        if (wantIncludePaths && dependencySetting.HasFlag(DependencySetting.IncludePaths))
                        {
                            DependenciesIncludePaths.AddRange(dependency.IncludePaths);
                            DependenciesIncludeSystemPaths.AddRange(dependency.IncludeSystemPaths);
                            _dependenciesResourceIncludePaths.AddRange(dependency.ResourceIncludePaths);

                            // Is there a case where we want the defines but *not* the include paths?
                            if (dependencySetting.HasFlag(DependencySetting.Defines))
                                Defines.AddRange(dependency.ExportDefines);
                        }
                    }

                    switch (dependency.Output)
                    {
                        case OutputType.None:
                        case OutputType.Lib:
                            {
                                bool dependencyOutputLib = dependency.Output == OutputType.Lib;

                                if (dependencyOutputLib && !goesThroughDLL &&
                                    (Output == OutputType.Lib ||
                                     dependency.ExportSymbolThroughProject == null ||
                                     dependency.ExportSymbolThroughProject == Project.GetType())
                                )
                                {
                                    if (explicitDependenciesGlobal || !compile)
                                        PlatformRegistry.Get<IConfigurationTasks>(dependency.Platform).SetupStaticLibraryPaths(this, dependencySetting, dependency);
                                    if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                                        ConfigurationDependencies.Add(dependency);
                                    if (dependencySetting == DependencySetting.OnlyBuildOrder)
                                        BuildOrderDependencies.Add(dependency);
                                }

                                if (!goesThroughDLL)
                                {
                                    if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                                        DependenciesOtherLibraryPaths.AddRange(dependency.LibraryPaths);

                                    if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                                        DependenciesOtherLibraryFiles.AddRange(dependency.LibraryFiles);

                                    if (dependencySetting.HasFlag(DependencySetting.ForceUsingAssembly))
                                        DependenciesForceUsingFiles.AddRange(dependency.ForceUsingFiles);
                                }

                                // If our no-output project is just a build-order dependency, update the build order accordingly
                                if (!dependencyOutputLib && isImmediate && dependencySetting == DependencySetting.OnlyBuildOrder && !isExport)
                                    GenericBuildDependencies.Add(dependency);
                            }
                            break;
                        case OutputType.Dll:
                            {
                                var configTasks = PlatformRegistry.Get<IConfigurationTasks>(dependency.Platform);

                                if (dependency.ExportDllSymbols && (isImmediate || hasPublicPathToRoot || !goesThroughDLL))
                                {
                                    if (explicitDependenciesGlobal || !compile || (IsFastBuild && Util.IsDotNet(dependency)))
                                        configTasks.SetupDynamicLibraryPaths(this, dependencySetting, dependency);
                                    if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                                        ConfigurationDependencies.Add(dependency);
                                    if (dependencySetting.HasFlag(DependencySetting.ForceUsingAssembly))
                                        ForceUsingDependencies.Add(dependency);
                                    if (dependencySetting == DependencySetting.OnlyBuildOrder)
                                        BuildOrderDependencies.Add(dependency);

                                    // check if that case is valid: dll with additional libs
                                    if (isExport && !goesThroughDLL)
                                    {
                                        if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                                            DependenciesOtherLibraryPaths.AddRange(dependency.LibraryPaths);

                                        if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                                            DependenciesOtherLibraryFiles.AddRange(dependency.LibraryFiles);
                                    }
                                }

                                if (dependencySetting.HasFlag(DependencySetting.AdditionalUsingDirectories) ||
                                    dependencySetting.HasFlag(DependencySetting.ForceUsingAssembly))
                                    AdditionalUsingDirectories.Add(dependency.TargetPath);

                                string platformDllExtension = "." + dependency.OutputExtension;
                                string dependencyDllFullPath = Path.Combine(dependency.TargetPath, dependency.TargetFileFullName + platformDllExtension);
                                if ((Output == OutputType.Exe || ExecuteTargetCopy)
                                    && dependencySetting.HasFlag(DependencySetting.LibraryFiles)
                                    && dependency.TargetPath != TargetPath)
                                {
                                    // If using OnlyBuildOrder, ExecuteTargetCopy must be set to enable the copy.
                                    if (dependencySetting != DependencySetting.OnlyBuildOrder || ExecuteTargetCopy)
                                    {
                                        _resolvedTargetCopyFiles.Add(dependencyDllFullPath);
                                        // Add PDBs only if they exist and the dependency is not an [export] project
                                        if (!isExport && Sharpmake.Options.GetObject<Options.Vc.Linker.GenerateDebugInformation>(dependency) != Sharpmake.Options.Vc.Linker.GenerateDebugInformation.Disable)
                                        {
                                            _resolvedTargetCopyFiles.Add(dependency.LinkerPdbFilePath);

                                            if (dependency.CopyCompilerPdbToDependentTargets)
                                                _resolvedTargetCopyFiles.Add(dependency.CompilerPdbFilePath);
                                        }
                                    }
                                    _resolvedEventPreBuildExe.AddRange(dependency.EventPreBuildExe);
                                    _resolvedEventPostBuildExe.AddRange(dependency.EventPostBuildExe);
                                    _resolvedEventCustomPreBuildExe.AddRange(dependency.EventCustomPreBuildExe);
                                    _resolvedEventCustomPostBuildExe.AddRange(dependency.EventCustomPostBuildExe);
                                }
                                _resolvedTargetDependsFiles.Add(dependencyDllFullPath);

                                // If this is not a .Net project, no .Net dependencies are needed
                                if (Util.IsDotNet(this))
                                {
                                    // If the dependency is not a .Net project, it will not function properly when referenced by a .Net compilation process
                                    if (Util.IsDotNet(dependency))
                                    {
                                        if (hasPublicPathToRoot)
                                            resolvedDotNetPublicDependencies.Add(new DotNetDependency(dependency));
                                        else if (isImmediate)
                                            resolvedDotNetPrivateDependencies.Add(new DotNetDependency(dependency));
                                    }
                                    // If the dependency is not a .Net project, it need anyway to be compiled before this one, so we add it as post dependency in the solution
                                    else if (isImmediate && !isExport)
                                    {
                                        GenericBuildDependencies.Add(dependency);
                                    }
                                }
                            }
                            break;
                        case OutputType.IosApp:
                        case OutputType.Exe:
                            {
                                if (Output != OutputType.Utility && Output != OutputType.Exe && Output != OutputType.None)
                                    throw new Error("Project {0} cannot depend on OutputType {1} {2}", this, Output, dependency);

                                if (hasPublicPathToRoot)
                                    resolvedDotNetPublicDependencies.Add(new DotNetDependency(dependency));
                                else if (isImmediate)
                                    resolvedDotNetPrivateDependencies.Add(new DotNetDependency(dependency));

                                if (dependencySetting == DependencySetting.OnlyBuildOrder)
                                    BuildOrderDependencies.Add(dependency);
                                else
                                    ConfigurationDependencies.Add(dependency);
                            }
                            break;
                        case OutputType.Utility: throw new NotImplementedException(dependency.Project.Name + " " + dependency.Output);
                        case OutputType.DotNetConsoleApp:
                        case OutputType.DotNetClassLibrary:
                        case OutputType.DotNetWindowsApp:
                            {
                                if (dependencySetting.HasFlag(DependencySetting.AdditionalUsingDirectories) ||
                                    dependencySetting.HasFlag(DependencySetting.ForceUsingAssembly))
                                    AdditionalUsingDirectories.Add(dependency.TargetPath);

                                bool? referenceOutputAssembly = ReferenceOutputAssembly;
                                if (dependencySetting == DependencySetting.OnlyBuildOrder)
                                    referenceOutputAssembly = false;
                                if (dependencySetting.HasFlag(DependencySetting.ForceUsingAssembly))
                                    ForceUsingDependencies.Add(dependency);

                                var dotNetDependency = new DotNetDependency(dependency)
                                {
                                    ReferenceOutputAssembly = referenceOutputAssembly
                                };

                                if (!resolvedDotNetPublicDependencies.Contains(dotNetDependency))
                                {
                                    if (hasPublicPathToRoot)
                                    {
                                        resolvedDotNetPrivateDependencies.Remove(dotNetDependency);
                                        resolvedDotNetPublicDependencies.Add(dotNetDependency);
                                    }
                                    else if ((isImmediate || hasPublicPathToImmediate))
                                    {
                                        resolvedDotNetPrivateDependencies.Add(dotNetDependency);
                                    }
                                }
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (resolvedPublicDependencies.Overlaps(resolvedPrivateDependencies) || resolvedDotNetPublicDependencies.Overlaps(resolvedDotNetPrivateDependencies))
                    throw new InternalError("Something goes wrong in Project.Configuration.ResolveDependencies(): same dependency resolved in public and private lists");

                // Will include to the project:
                //  - lib,dll: include paths
                //  - lib,dll: library paths and files
                //  - dll: copy dll to the output executable directory
                _resolvedPublicDependencies = resolvedPublicDependencies.ToList();

                // Will include to the project to act as a project bridge:
                //  - lib: add Library paths and files to be able to link the executable
                //  - dll: Copy dll to the output path
                _resolvedPrivateDependencies = resolvedPrivateDependencies.ToList();

                DotNetPublicDependencies = resolvedDotNetPublicDependencies.ToList();
                DotNetPrivateDependencies = resolvedDotNetPrivateDependencies.ToList();

                // sort base on DependenciesOrder
                _resolvedPublicDependencies.Sort(SortConfigurationForLink);
                _resolvedPrivateDependencies.Sort(SortConfigurationForLink);

                _resolvedDependencies = new List<Configuration>();
                _resolvedDependencies.AddRange(_resolvedPublicDependencies);
                _resolvedDependencies.AddRange(_resolvedPrivateDependencies);

                _linkState = LinkState.Linked;
            }

            static private DependencyNode BuildDependencyNodeTree(Builder builder, Configuration conf)
            {
                DependencyNode rootNode = new DependencyNode(conf, DependencySetting.Default);

                Dictionary<Configuration, DependencyNode> visited = new Dictionary<Configuration, DependencyNode>();

                Stack<DependencyNode> visiting = new Stack<DependencyNode>();
                visiting.Push(rootNode);
                while (visiting.Count > 0)
                {
                    DependencyNode visitedNode = visiting.Pop();
                    Configuration visitedConfiguration = visitedNode._configuration;

                    // if we already know that configuration, just reattach its children to the current node
                    DependencyNode alreadyExisting = null;
                    if (visited.TryGetValue(visitedConfiguration, out alreadyExisting))
                    {
                        foreach (var child in alreadyExisting._childNodes)
                        {
                            System.Diagnostics.Debug.Assert(!visitedNode._childNodes.ContainsKey(child.Key));
                            visitedNode._childNodes.Add(child.Key, child.Value);
                        }
                        continue;
                    }

                    visited.Add(visitedConfiguration, visitedNode);

                    var unresolvedDependencies = new[] { visitedConfiguration.UnResolvedPublicDependencies, visitedConfiguration.UnResolvedPrivateDependencies };
                    foreach (Dictionary<Type, ITarget> dependencies in unresolvedDependencies)
                    {
                        if (dependencies.Count == 0)
                            continue;

                        bool isPrivateDependency = dependencies == visitedConfiguration.UnResolvedPrivateDependencies;
                        DependencyType dependencyType = isPrivateDependency ? DependencyType.Private : DependencyType.Public;

                        foreach (KeyValuePair<Type, ITarget> pair in dependencies)
                        {
                            Configuration dependencyConf = conf.GetDependencyConfiguration(builder, visitedConfiguration, pair);

                            // Get the dependency settings from the owner of the dependency.
                            DependencySetting dependencySetting;
                            if (!visitedConfiguration._dependenciesSetting.TryGetValue(pair, out dependencySetting))
                                dependencySetting = DependencySetting.Default;

                            DependencyNode childNode = new DependencyNode(dependencyConf, dependencySetting);
                            System.Diagnostics.Debug.Assert(!visitedNode._childNodes.ContainsKey(childNode));
                            visitedNode._childNodes.Add(childNode, dependencyType);

                            visiting.Push(childNode);
                        }
                    }
                }

                return rootNode;
            }

            internal void SetDefaultOutputExtension()
            {
                if (string.IsNullOrEmpty(OutputExtension))
                    OutputExtension = PlatformRegistry.Get<IConfigurationTasks>(Platform).GetDefaultOutputExtension(Output);
            }

            #region Deprecated
            [Obsolete("This delegate was used only by " + nameof(FastBuildFileIncludeCondition) + " which had no effect. It will be removed.")]
            public delegate bool FastBuildFileIncludeConditionDelegate(Project.Configuration conf);
            [Obsolete("This property could be set but was never used by Sharpmake. It will be removed.")]
            public FastBuildFileIncludeConditionDelegate FastBuildFileIncludeCondition = null;
            #endregion
        }
    }
}
