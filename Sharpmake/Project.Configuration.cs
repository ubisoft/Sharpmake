// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections;
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
        /// The dependent project will reference the target assembly file instead of using a project reference.
        /// Valid only for C# projects. Note that these assemblies are expected to be found in the project's output
        /// directory and thus must be built otherwise.
        /// </summary>
        DependOnAssemblyOutput = 1 << 7,

        /// <summary>
        /// The dependent project won't have a ProjectReference added.
        /// Valid only when the project is a C or a C++ project.
        /// </summary>
        NoProjectReference = 1 << 8,

        /// <summary>
        /// Indicates if the reference dll should be copied to the output folder. Represents the Private option of project reference.
        /// Valid only for C# projects.
        /// </summary>
        /// <remarks>
        /// Private: Specifies whether the reference should be copied to the output folder. 
        /// This attribute matches the Copy Local property of the reference that's in the Visual Studio IDE.
        /// </remarks>
        CopyLocal = 1 << 9,

        /// <summary>
        /// Specifies that the dependent project inherits the dependency's library files, library
        /// paths, include paths and defined symbols.
        /// </summary>
        Default = LibraryFiles |
                  LibraryPaths |
                  IncludePaths |
                  Defines |
                  CopyLocal,

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

        /// <summary>
        /// Indicates that the dependency use the default setting while not copying the dll 
        /// to the current project output path (Private = false). Only use with C# dependency.
        /// </summary>
        DefaultWithoutCopy = Default & ~CopyLocal
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

                // The below method was replaced by GetDefaultOutputFullExtension
                // string GetDefaultOutputExtension(OutputType outputType);

                /// <summary>
                /// Gets the default file extension for a given output type.
                /// </summary>
                /// <param name="outputType">The <see cref="OutputType"/> whose default file extension we are seeking.</param>
                /// <returns>A string, containing the file extension (could be empty on some platforms, like exe on linux).</returns>
                string GetDefaultOutputFullExtension(OutputType outputType);

                /// <summary>
                /// Gets the default file prefix for a given output type.
                /// </summary>
                /// <param name="outputType">The <see cref="OutputType"/> whose default file prefix we are seeking.</param>
                /// <returns>A string, containing the file prefix (for instance lib on linux).</returns>
                string GetOutputFileNamePrefix(Project.Configuration.OutputType outputType);

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
                /// The output is an Apple (macOS|iOS|tvOS|watchOS) app.
                /// </summary>
                AppleApp,

                /// <summary>
                /// The output is an Apple (macOS|iOS|tvOS|watchOS) framework (i.e. a Bundle of DLL (dylib) and Headers).
                /// </summary>
                AppleFramework,

                /// <summary>
                /// The output is an Apple (macOS|iOS|tvOS|watchOS) Bundle (i.e. kind of equivalent to DLL (dylib)).
                /// </summary>
                AppleBundle,

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
                /// UAC Execution Level: as invoker.
                /// </summary>
                /// <remarks>
                /// Use the same privileges as the process that created the program.
                /// </remarks>
                asInvoker,

                /// <summary>
                /// UAC Execution Level: highest available.
                /// </summary>
                /// <remarks>
                /// Use the highest privileges available to the current user.
                /// </remarks>
                highestAvailable,

                /// <summary>
                /// UAC Execution Level: require administrator.
                /// </summary>
                /// <remarks>
                /// Always run with administrator privileges. This will usually open a UAC dialog
                /// box for the user.
                /// </remarks>
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
            [Obsolete("Use " + nameof(TargetFileFullExtension) + " instead", error: true)]
            public string OutputExtension = null;

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
            /// Gets or sets whether the configuration output dll file will be copied in the target path of the projects depending on it.
            /// </summary>
            /// <remarks>
            /// This setting only apply with <see cref="OutputType.Dll"/>
            /// This setting is usefull for dlls that are dynamically loaded:
            /// The dll do not need to be put along the executable.
            /// <para>
            /// The default is <c>true</c>. Setting this to <c>false</c> will prevent the generators
            /// to copy the library artifact in the exe directory.
            /// </para>
            /// </remarks>
            public bool AllowOutputDllCopy = true;

            /// <summary>
            /// Controls whether the .pdb files of [Export] projects will be copied to dependents.
            /// The default value is <c>false</c>.
            /// </summary>
            public bool AllowExportProjectsToCopyPdbToDependentTargets = false;

            /// <summary>
            /// Gets or sets whether dependent projects will copy their dll debugging database to the
            /// target path of their dependency projects. The default value is <c>true</c>.
            /// </summary>
            public bool CopyLinkerPdbToDependentTargets = true;

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
            /// Setting this boolean to true forces Sharpmake to bypass the additional dependencies prefix added normally
            /// on libraries (-l{...}, or lib{...}.a).
            /// </summary>
            /// <remarks>
            /// Since Sharpmake handles all dependencies, using an <c>AdditionalDependencies</c> field in
            /// your project as librairies, it is impossible to add other dependencies like externally built objects
            /// (i.e. *.asm files build externally as *.o). When this is the case, bypassing the prefixing can allow us
            /// more flexibility on our build pipeline.
            /// <para>
            /// The default is <c>false</c>. Set this boolean to <c>true</c> to make Sharpmake skip the name mangling of
            /// the additional dependencies.
            /// </para>
            /// </remarks>
            public bool BypassAdditionalDependenciesPrefix = false;

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
            /// Set the solution folder associated with a solution name
            /// </summary>
            /// <remarks>
            /// Ignored unless generating for Visual Studio
            /// This property allows to get the same project being in different folder dependeng on the solution name. Ex. In Engine.sln the project Physic is at root, while in Tools.sln it is in a Engine/ directory
            /// Use the property SolutionFolder if not found inside the dictionary.
            /// <para>
            /// To place the project in a sub-directory, use a `/` as a directory separator.
            /// </para>
            /// </remarks>
            public void AddSolutionFolder(string solutionName, string solutionFolder)
            {
                _solutionFolders[solutionName] = solutionFolder;
            }

            /// <summary>
            /// Gets the solution folder associated with a solution name.
            /// </summary>
            public string GetSolutionFolder(string solutionName)
            {
                string specificSolutionFolder = null;
                if (_solutionFolders.TryGetValue(solutionName, out specificSolutionFolder) == false)
                    return SolutionFolder;
                return specificSolutionFolder;
            }

            private Dictionary<string, string> _solutionFolders = new Dictionary<string, string>();

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
            /// Source files that match this regex will be compiled as ObjC Files.
            /// </summary>
            public Strings SourceFilesCompileAsObjCRegex = new Strings();

            /// <summary>
            /// Source files that match this regex will be compiled as ObjCPP Files.
            /// </summary>
            public Strings SourceFilesCompileAsObjCPPRegex = new Strings();

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
            /// Include paths for Assembly compilation.
            /// </summary>
            /// <remarks>
            /// The maximum number of these paths is 10.
            /// </remarks>
            public OrderableStrings AssemblyIncludePaths = new OrderableStrings();

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
            /// Get a list of compiler optimization options to send when calling the compiler. It is necessary to properly implement the 
            /// fastbuild .CompilerOptionsDeoptimized
            /// </summary>
            /// <remarks>
            /// <para>
            /// This property is for the compiler. It is similar to 
            /// <see cref="AdditionalCompilerOptions"/> but only for optimizations options not exposed by Sharpmake.
            /// </para>
            /// </remarks>
            public OrderableStrings AdditionalCompilerOptimizeOptions = new OrderableStrings();

            /// <summary>
            /// Compiler-specific options to pass when invoking the compiler to create PCHs.
            /// </summary>
            /// <remarks>
            /// Currently only respected by the BFF generator.
            /// </remarks>
            public OrderableStrings AdditionalCompilerOptionsOnPCHCreate = new OrderableStrings();

            /// <summary>
            /// Compiler-specific options to pass when invoking the compiler telling it to use PCHs.
            /// </summary>
            /// <remarks>
            /// Currently only respected by the BFF generator.
            /// </remarks>
            public OrderableStrings AdditionalCompilerOptionsOnPCHUse = new OrderableStrings();

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
            /// Sharpmake assumes that a relative path here is relative to <see cref="SourceRootPath"/>.
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
            /// <see cref="SourceRootPath"/>. If that isn't correct, you must use an absolute path.
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
            /// Gets or sets the name for the precompiled header's binary file in C and C++ projects,
            /// e.g. <c>pch.pch</c>.
            /// </summary>
            /// <remarks>
            /// If this property is set to <c>null</c>, Sharpmake will simply use the project's name.
            /// To modify the output directory of this file, use <see cref="PrecompHeaderOutputFolder"/>.
            /// </remarks>
            public string PrecompHeaderOutputFile = null;

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

            public List<ForcedIncludesFilter> ForcedIncludesFilters = new List<ForcedIncludesFilter>();

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
                    default:
                        throw new NotImplementedException("Exception setting for file " + filename + " not recognized");
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
            /// Gets a list of the System Frameworks to link to for Xcode project.
            /// </summary>
            private OrderableStrings _XcodeSystemFrameworks = null;
            public OrderableStrings XcodeSystemFrameworks
            {
                get
                {
                    if (_XcodeSystemFrameworks == null)
                    {
                        _XcodeSystemFrameworks = new OrderableStrings();
                    }
                    return _XcodeSystemFrameworks;
                }
                private set { _XcodeSystemFrameworks = value; }
            }

            private OrderableStrings _XcodeDependenciesSystemFrameworks = null;
            public OrderableStrings XcodeDependenciesSystemFrameworks
            {
                get
                {
                    if (_XcodeDependenciesSystemFrameworks == null)
                    {
                        _XcodeDependenciesSystemFrameworks = new OrderableStrings();
                    }
                    return _XcodeDependenciesSystemFrameworks;
                }
            }

            /// <summary>
            /// Gets a list of the Developer Frameworks to link to for Xcode project.
            /// </summary>
            private OrderableStrings _XcodeDeveloperFrameworks = null;
            public OrderableStrings XcodeDeveloperFrameworks
            {
                get
                {
                    if (_XcodeDeveloperFrameworks == null)
                    {
                        _XcodeDeveloperFrameworks = new OrderableStrings();
                    }
                    return _XcodeDeveloperFrameworks;
                }
                private set { _XcodeDeveloperFrameworks = value; }
            }

            private OrderableStrings _XcodeDependenciesDeveloperFrameworks = null;
            public OrderableStrings XcodeDependenciesDeveloperFrameworks
            {
                get
                {
                    if (_XcodeDependenciesDeveloperFrameworks == null)
                    {
                        _XcodeDependenciesDeveloperFrameworks = new OrderableStrings();
                    }
                    return _XcodeDependenciesDeveloperFrameworks;
                }
            }

            /// <summary>
            /// Gets a list of the User Frameworks to link to for Xcode project.
            /// </summary>
            private OrderableStrings _XcodeUserFrameworks = null;
            public OrderableStrings XcodeUserFrameworks
            {
                get
                {
                    if (_XcodeUserFrameworks == null)
                    {
                        _XcodeUserFrameworks = new OrderableStrings();
                    }
                    return _XcodeUserFrameworks;
                }
                private set { _XcodeUserFrameworks = value; }
            }

            private OrderableStrings _XcodeDependenciesUserFrameworks = null;
            public OrderableStrings XcodeDependenciesUserFrameworks
            {
                get
                {
                    if (_XcodeDependenciesUserFrameworks == null)
                    {
                        _XcodeDependenciesUserFrameworks = new OrderableStrings();
                    }
                    return _XcodeDependenciesUserFrameworks;
                }
            }

            /// <summary>
            /// Gets a list of the Frameworks to link to and to embed in application for Xcode project.
            /// </summary>
            private OrderableStrings _XcodeEmbeddedFrameworks = null;
            public OrderableStrings XcodeEmbeddedFrameworks
            {
                get
                {
                    if (_XcodeEmbeddedFrameworks == null)
                    {
                        _XcodeEmbeddedFrameworks = new OrderableStrings();
                    }
                    return _XcodeEmbeddedFrameworks;
                }
                private set { _XcodeEmbeddedFrameworks = value; }
            }

            private OrderableStrings _XcodeDependenciesEmbeddedFrameworks = null;
            public OrderableStrings XcodeDependenciesEmbeddedFrameworks
            {
                get
                {
                    if (_XcodeDependenciesEmbeddedFrameworks == null)
                    {
                        _XcodeDependenciesEmbeddedFrameworks = new OrderableStrings();
                    }
                    return _XcodeDependenciesEmbeddedFrameworks;
                }
            }

            /// <summary>
            /// Gets a list of the System Framework paths to link to for Xcode project.
            /// </summary>
            private OrderableStrings _XcodeSystemFrameworkPaths = null;
            public OrderableStrings XcodeSystemFrameworkPaths
            {
                get
                {
                    if (_XcodeSystemFrameworkPaths == null)
                    {
                        _XcodeSystemFrameworkPaths = new OrderableStrings();
                    }
                    return _XcodeSystemFrameworkPaths;
                }
                private set { _XcodeSystemFrameworkPaths = value; }
            }

            private OrderableStrings _XcodeDependenciesSystemFrameworkPaths = null;
            public OrderableStrings XcodeDependenciesSystemFrameworkPaths
            {
                get
                {
                    if (_XcodeDependenciesSystemFrameworkPaths == null)
                    {
                        _XcodeDependenciesSystemFrameworkPaths = new OrderableStrings();
                    }
                    return _XcodeDependenciesSystemFrameworkPaths;
                }
            }

            /// <summary>
            /// Gets a list of the Framework paths to link to for Xcode project.
            /// </summary>
            private OrderableStrings _XcodeFrameworkPaths = null;
            public OrderableStrings XcodeFrameworkPaths
            {
                get
                {
                    if (_XcodeFrameworkPaths == null)
                    {
                        _XcodeFrameworkPaths = new OrderableStrings();
                    }
                    return _XcodeFrameworkPaths;
                }
                private set { _XcodeFrameworkPaths = value; }
            }

            private OrderableStrings _XcodeDependenciesFrameworkPaths = null;
            public OrderableStrings XcodeDependenciesFrameworkPaths
            {
                get
                {
                    if (_XcodeDependenciesFrameworkPaths == null)
                    {
                        _XcodeDependenciesFrameworkPaths = new OrderableStrings();
                    }
                    return _XcodeDependenciesFrameworkPaths;
                }
            }

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
            /// Allow/prevent writing VC overrides to the vcxproj files for this conf
            /// Note that the setting must have the same value for all conf in the same vcxproj file
            /// </summary>
            /// <remarks>
            /// This is only used by the Visual Studio Vcxproj generator
            /// </remarks>
            public bool WriteVcOverrides = true;

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

            public string FastBuildInputFilesRootPath = null;

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
            /// Gets or sets the path of the list with files to isolate,
            /// e.g.: @"temp\IsolateFileList.txt".
            /// </summary>
            /// <remarks>
            /// <note>
            /// Files in this list will be excluded from the FASTBuild unity build.
            /// Their path must be relative to the FASTBuild working directory.
            /// This is usually the location of the MasterBff file.
            /// </note>
            /// </remarks>
            public string FastBuildUnityInputIsolateListFile = null;

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
            /// Whether to use relative paths in FASTBuild-generated Unity files.
            /// </summary>
            public bool FastBuildUnityUseRelativePaths = false;

            /// <summary>
            /// Give a same value to configurations with same fastbuild unity settings that you want to keep together. If you want to have developer
            /// machines have fastbuild cache hits but the section settings are not exactly the same for some targets you will need this field to
            /// to have the same unity sections than on your build machines(typically at Ubisoft, only have the build machines have the cache in read-write mode).
            /// </summary>
            /// <remarks>
            /// With this field we can force some extra fastbuild unity sections even for sections with identical settings.
            /// Sharpmake will take into account the bucket number when creating its internal unity objects and this will let us create artifical delimitation 
            /// of setting sections.
            /// This field should be used only if FragmentHashUnityResolver is used.
            /// </remarks>
            /// <example>
            /// You have two targets: Debug and Release. You want to enable deoptimization on debug target on developer machine but never on build machines
            /// In a Configure method you do this:
            /// if (IsBuildMachine() || target.Optimization > Optimization.Debug )
            /// {
            ///     conf.FastBuildUnityInputIsolateWritableFiles = false;
            ///     conf.FastBuildDeoptimization = Configuration.DeoptimizationWritableFiles.NoDeoptimization;
            ///}
            ///else
            ///{
            ///    conf.FastBuildUnityInputIsolateWritableFilesLimit = 50;
            ///    conf.FastBuildDeoptimization = true;
            ///}
            /// Without this change and the code below we have different unity sections on build machine and developper machines, causing fastbuild cache misses.
            /// We now do this in a Configure method:
            /// conf.FastBuildUnitySectionBucket = (byte)(target.Optimization > Optimization.Debug ? 1 : 0);
            /// By doing this we force two separate unity sections on build machines(same as on developer machine).
            /// 
            /// Important: This assumes that you configured Sharpmake to use FragmentHashUnityResolver as the unity resolver.
            /// </example>
            public Byte FastBuildUnitySectionBucket = 0;

            /// <summary>
            /// List of version detection ways to set _MSC_VER and _MSC_FULL_VER preprocessor values when using ClangCl
            /// </summary>
            /// <remarks>
            /// This is only used for FASTBuild generation when using ClangCl.
            /// </remarks>
            public enum FastBuildClangMscVersionDetectionType
            {
                /// <summary>
                /// Sets the -fmsc-version compiler tag (ClangCl specific) in the command-line options in the FASTBuild (.bff) file with the "major" version, e.g. Any VS2022 (17.x) is 1930
                /// </summary>
                /// <remarks>
                /// This sets the _MSC_VER preprocessor flag to the "major" version, e.g. for any VS2022, _MSC_VER is set to 1930
                /// This sets the _MSC_FULL_VER preprocessor flag to the "major" version with additional zeros, e.g. for any VS2022, _MSC_FULL_VER is set to 193000000
                /// </remarks>
                MajorVersion,

                /// <summary>
                /// Sets the -fms-compatibility-version compiler tag (ClangCl specific) in the command-line options in the FASTBuild (.bff) file with the complete version, e.g. VS2022 17.4.0 is 19.34.31933.
                /// </summary>
                /// <remarks>
                /// This sets the _MSC_VER preprocessor flag to the most 4-digits precise version, e.g. for VS2022 17.4.0, _MSC_VER is set to 1934
                /// This sets the _MSC_FULL_VER preprocessor flag to the complete version, e.g. for VS2022 17.4.0, _MSC_FULL_VER is set to 193431933
                /// If the full version cannot be detected, fallback to the behavior of MajorVersion
                /// This option is not compatible with a non-empty value in Options.Clang.Compiler.MscVersion
                /// </remarks>
                FullVersion, // Replaces MajorVersion as the default value since it uses more accurate compiler versions (ClangCl compatibility is improved) and fallbacks to MajorVersion in case the more accurate versions couldn't be found

                /// <summary>
                /// Does not set any version.
                /// </summary>
                Disabled
            }

            /// <summary>
            /// (Only for FastBuild with ClangCl) Sets how to detect the Microsoft compiler version to fill the _MSC_VER and _MSC_FULL_VER preprocessor values.
            /// </summary>
            public FastBuildClangMscVersionDetectionType FastBuildClangMscVersionDetectionInfo = FastBuildClangMscVersionDetectionType.FullVersion;

            private string _fastBuildLinkConcurrencyGroup = null;
            /// <summary>
            /// Optional fastbuild concurrency group name. Concurrency groups are used to limit the number of parallel processes using the same concurrency group.
            /// It can be used for example to limit the number of LTO link process to 1.
            /// </summary>
            public string FastBuildLinkConcurrencyGroup
            {
                get
                {
                    return _fastBuildLinkConcurrencyGroup;
                }
                set
                {
                    if (!FastBuildSettings.EnableConcurrencyGroups)
                        throw new Error("Can't set FastBuildLinkConcurrencyGroup as FastBuildSettings.EnableConcurrencyGroups is false");
                    if (!FastBuildSettings.ConcurrencyGroups.ContainsKey(value))
                        throw new Error($"Can't set FastBuildLinkConcurrencyGroup to {value} as it is not defined in FastBuildSettings.ConcurrencyGroups");

                    _fastBuildLinkConcurrencyGroup = value;
                }
            }

            private Strings _intellisenseAdditionalDefines;

            /// <summary>
            /// This property is used to have a list of defines that are not used in the build 
            /// but are used for intellisense in Visual Studio.
            /// This is only used for fastbuild project(implemented using nmake project)
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public Strings IntellisenseAdditionalDefines
            {
                get
                {
                    return GetDynamicPropertyField(ref _intellisenseAdditionalDefines, () => new Strings());
                }
            }

            private Strings _intellisenseAdditionalCommandLineOptions;
            /// <summary>
            /// This property is used to have a list of additional command line options that are not used in the build 
            /// but are used for intellisense in Visual Studio.
            /// This is only used for fastbuild project(implemented using nmake project)
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public Strings IntellisenseAdditionalCommandLineOptions
            {
                get
                {
                    return GetDynamicPropertyField(ref _intellisenseAdditionalCommandLineOptions, () => new Strings());
                }
            }

            /// <summary>
            /// Gets or sets whether to generate a FASTBuild (.bff) file when using FASTBuild.
            /// </summary>
            /// <remarks>
            /// For projects merging multiple targets, sometimes what is wanted is to not generate FastBuild
            ///  .bff files but, instead, include any existing .bff files from the appropriate targets.
            /// </remarks>
            public bool DoNotGenerateFastBuild = false;

            // Jumbo builds support for msbuild
            public int MaxFilesPerJumboFile = 0;
            public int MinFilesPerJumboFile = 2;
            public int MinJumboFiles = 1;

            internal HashSet<Configuration> ConfigurationsSwappedToDll { get; set; }
            
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
                    bool alwaysShowOutput = false,
                    bool executeAlways = false)

                {
                    ExecutableFile = executableFile;
                    ExecutableInputFileArgumentOption = executableInputFileArgumentOption;
                    ExecutableOutputFileArgumentOption = executableOutputFileArgumentOption;
                    ExecutableOtherArguments = executableOtherArguments;
                    ExecutableWorkingDirectory = executableWorkingDirectory;
                    IsNameSpecific = isNameSpecific;
                    FastBuildUseStdOutAsOutput = useStdOutAsOutput;
                    FastBuildAlwaysShowOutput = alwaysShowOutput;
                    FastBuildExecAlways = executeAlways;
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

                /// <summary>
                /// Gets or sets whether the step should be executed every time.
                /// </summary>
                public bool FastBuildExecAlways = false;

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
                    Mirror = buildStepCopy.Mirror;
                }

                public BuildStepCopy(string sourcePath, string destinationPath, bool isNameSpecific = false, string copyPattern = "*", bool fileCopy = true, bool mirror = false)
                {
                    SourcePath = sourcePath;
                    DestinationPath = destinationPath;

                    IsFileCopy = fileCopy;
                    IsRecurse = true;
                    IsNameSpecific = isNameSpecific;
                    CopyPattern = copyPattern;
                    Mirror = mirror;
                }

                public string SourcePath = "";
                public string DestinationPath = "";

                public bool IsFileCopy { get; set; }
                public bool IsRecurse { get; set; }
                public bool Mirror { get; set; }
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
                        Mirror ? "/mir" : string.Empty, // /MIR :: Mirrors a directory tree (equivalent to /e plus /purge).

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
                public string CompileFileCommand = RemoveLineTag;
                public string OutputFile = RemoveLineTag;
                public string AdditionalOptions = "";

                /// <summary>
                /// Automatically add hidden arguments to AdditionalOptions so that the IntelliSense match with the project parameters
                /// </summary>
                public bool AutoConfigure = true;

                public bool IsResolved { get; private set; } = false;

                internal void Resolve(Resolver resolver)
                {
                    if (IsResolved)
                        return;

                    BuildCommand = resolver.Resolve(BuildCommand);
                    RebuildCommand = resolver.Resolve(RebuildCommand);
                    CleanCommand = resolver.Resolve(CleanCommand);
                    OutputFile = resolver.Resolve(OutputFile);
                    AdditionalOptions = resolver.Resolve(AdditionalOptions);

                    IsResolved = true;
                }
            }
            public NMakeBuildSettings CustomBuildSettings = null;


            /// <summary>
            /// Specifies a function with a relative source file path as input and an object file path as output.
            /// </summary>
            /// <remarks>
            /// <note type="warning">
            /// This will slow down your project's compile time! Overwrite the object file output path
            /// only for the files that absolutely require it. Let the function return null or empty string
            /// to skip the overwrite for the given source file.
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
            /// Gets or set the platform dependent file prefix (for instance "lib" for libraries on linux).
            /// If left null, sharpmake will set it to the default for the platform
            /// according to the output type when the conf is resolved.
            /// </summary>
            public string TargetFilePlatformPrefix = null;

            /// <summary>
            /// Gets the full file name of the target, without the path but with the prefix and suffix, and without the extension
            /// </summary>
            public string TargetFileFullName { get; set; } = "[conf.TargetFilePlatformPrefix][conf.TargetFilePrefix][conf.TargetFileName][conf.TargetFileSuffix]";

            /// <summary>
            /// Gets or sets the project's full extension (ie: .dll, .self, .exe, .dlu).
            /// Set to an empty string you don't want any.
            /// If left null, sharpmake will set it to the default for the platform according to the output type, when the conf is resolved.
            /// </summary>
            public string TargetFileFullExtension = null;

            /// <summary>
            /// Gets the full file name of the target, without the path but with the suffix, and the extension
            /// and the prefix.
            /// </summary>
            public string TargetFileFullNameWithExtension = "[conf.TargetFileFullName][conf.TargetFileFullExtension]";

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
            /// Target copy files path, where the TargetCopyFiles files will be copied
            /// </summary>
            public string TargetCopyFilesPath = "[conf.TargetPath]";

            /// <summary>
            /// Gets or sets the list of files to copy to a sub-directory of the output directory.
            /// </summary>
            public HashSet<KeyValuePair<string, string>> TargetCopyFilesToSubDirectory = new HashSet<KeyValuePair<string, string>>();

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
            public List<BuildStepExecutable> PostBuildStampExes = new List<BuildStepExecutable>();

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
                /// Use this to indicate the executable is in the system Path
                /// </summary>
                public bool UseExecutableFromSystemPath = false;
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
                /// Optional string to hint to the build system at what to treat the output from the build command as
                /// </summary>
                public string OutputItemType = "";

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
                    if (!UseExecutableFromSystemPath)
                        relativeData.Executable = MakeRelativeTool(Executable, true);
                    else
                        relativeData.Executable = Executable;
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
            /// Gets the <see cref="Project"/> that this <see cref="Configuration"/>
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
            [Obsolete("DeployProject is obsolete, use DeployProjectType instead (DeployType.OnlyIfBuild for the case where DeployProject == true)", false)]
            public bool DeployProject
            {
                get => DeployProjectType != DeployType.NoDeploy;
                set => DeployProjectType = value ? DeployType.OnlyIfBuild : DeployType.NoDeploy;
            }

            /// <summary>
            /// Gets or sets whether this project is deployed and in which cases.
            /// </summary>
            /// <remarks>
            /// This property only applies to Visual Studio projects.
            /// </remarks>
            public DeployType DeployProjectType {get; set; } = DeployType.NoDeploy;

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
            public string ExecutableFullExtension { get; private set; }
            [Obsolete("Use " + nameof(ExecutableFullExtension) + " instead", error: true)]
            public string ExecutableExtension;

            /// <summary>
            /// Gets the file extension for compressed executables, such as bundles, game packages
            /// for consoles, etc.
            /// </summary>
            public string CompressedExecutableFullExtension { get; private set; }
            [Obsolete("Use " + nameof(CompressedExecutableFullExtension) + " instead", error: true)]
            public string CompressedExecutableExtension;

            /// <summary>
            /// Gets the file extension for shared libraries.
            /// </summary>
            // TODO: Deprecate this and create a SharedLibraryExtension property instead.
            public string DllFullExtension { get; private set; }
            [Obsolete("Use " + nameof(DllFullExtension) + " instead", error: true)]
            public string DllExtension;

            /// <summary>
            /// Gets the file extension for program debug databases.
            /// </summary>
            public string ProgramDatabaseFullExtension { get; private set; }
            [Obsolete("Use " + nameof(ProgramDatabaseFullExtension) + " instead", error: true)]
            public string ProgramDatabaseExtension;

            [Obsolete("Use " + nameof(TargetFileFullExtension) + " instead", error: true)]
            public string TargetFileExtension
            {
                get
                {
                    return null;
                }
                set { }
            }

            /// <summary>
            /// Mark the configuration to be the default build configuration for XCode project
            /// </summary>
            public bool UseAsDefaultForXCode = false;

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

            /// <summary>
            /// Gets or sets whether FASTBuild blobs (unities) will be used in the build.
            /// </summary>
            public bool FastBuildBlobbed = true;

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

            /// <summary>
            /// Override this delegate with a method returning a bool letting sharpmake know if it needs to add the
            /// project containing this FastBuild conf to the solution.
            /// By default, sharpmake will only add it if the Output is executable, or if <see cref="VcxprojUserFile"/>
            /// is not null.
            /// </summary>
            public Func<bool> AddFastBuildProjectToSolutionCallback
            {
                get
                {
                    return _addFastBuildProjectToSolutionCallback ?? DefaultAddFastBuildProjectToSolution;
                }
                set
                {
                    _addFastBuildProjectToSolutionCallback = value;
                }
            }
            private Func<bool> _addFastBuildProjectToSolutionCallback = null;

            /// <summary>
            /// Default method returning whether sharpmake will add the project containing this FastBuild conf to the solution
            /// </summary>
            public bool DefaultAddFastBuildProjectToSolution()
            {
                if (!IsFastBuild)
                    return true;

                if (Project.IsFastBuildAll)
                    return true;

                if (!DoNotGenerateFastBuild)
                {
                    if (Output == OutputType.Exe)
                        return true;

                    if (VcxprojUserFile != null)
                        return true;
                }

                return false;
            }

            private Dictionary<ValueTuple<Type, ITarget>, DependencySetting> _dependenciesSetting = new Dictionary<ValueTuple<Type, ITarget>, DependencySetting>();

            // These dependencies will not be propagated to other projects that depend on us
            internal IDictionary<Type, ITarget> UnResolvedPrivateDependencies { get; } = new Dictionary<Type, ITarget>();
            // These dependencies are always propagated to other dependent projects.
            internal Dictionary<Type, ITarget> UnResolvedPublicDependencies { get; } = new Dictionary<Type, ITarget>();

            private Strings _resolvedTargetCopyFiles = new Strings();

            /// <summary>
            /// Gets the list of resolved files to copy.
            /// </summary>
            public IEnumerable<string> ResolvedTargetCopyFiles => _resolvedTargetCopyFiles;

            private HashSet<KeyValuePair<string, string>> _resolvedTargetCopyFilesToSubDirectory = new HashSet<KeyValuePair<string, string>>();

            /// <summary>
            /// Gets the list of resolved files to copy to a sub directory of the target directory.
            /// </summary>
            public IEnumerable<KeyValuePair<string, string>> ResolvedTargetCopyFilesToSubDirectory => _resolvedTargetCopyFilesToSubDirectory;


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

            public void GeneratorSetOutputFullExtensions(string executableExtension, string compressedExecutableExtension, string dllExtension, string programDatabaseExtension)
            {
                ExecutableFullExtension = executableExtension;
                CompressedExecutableFullExtension = compressedExecutableExtension;
                DllFullExtension = dllExtension;
                ProgramDatabaseFullExtension = programDatabaseExtension;
            }

            [Obsolete("Use " + nameof(GeneratorSetOutputFullExtensions) + " with full extensions", error: true)]
            public void GeneratorSetGeneratedInformation(string executableExtension, string compressedExecutableExtension, string dllExtension, string programDatabaseExtension)
            {
            }

            public Strings ResolvedSourceFilesBuildExclude = new Strings();

            private Strings _XcodeUnitTestSourceFilesBuildExclude = null;
            public Strings XcodeUnitTestSourceFilesBuildExclude
            {
                get
                {
                    if (_XcodeUnitTestSourceFilesBuildExclude == null)
                    {
                        _XcodeUnitTestSourceFilesBuildExclude = new Strings();
                    }
                    return _XcodeUnitTestSourceFilesBuildExclude;
                }
                private set { _XcodeUnitTestSourceFilesBuildExclude = value; }
            }
            private Strings _XcodeResolvedUnitTestSourceFilesBuildExclude = null;
            public Strings XcodeResolvedUnitTestSourceFilesBuildExclude
            {
                get
                {
                    if (_XcodeResolvedUnitTestSourceFilesBuildExclude == null)
                    {
                        _XcodeResolvedUnitTestSourceFilesBuildExclude = new Strings();
                    }
                    return _XcodeResolvedUnitTestSourceFilesBuildExclude;
                }
                private set { _XcodeResolvedUnitTestSourceFilesBuildExclude = value; }
            }

            /// <summary>
            /// This property is used to override default behavior for XCode executable projects 
            /// configuration compiled using fastbuild.
            /// If true(default), it will use a native XCode project to execute fastbuild
            /// If false, it will use a makefile project to execute fastbuild.
            /// </summary>
            /// <remarks>
            /// When using the default value, the project will not contain source files. The reason is we can't have source files 
            /// in the project as otherwise xcode will compile them itself and it will then try to relink the executable with those.
            /// This will create unresolved errors.
            /// When a native project is used Xcode will handle signing, package creation. These steps must be implemented by yourself if you
            /// decide to use a makefile project. However only a makefile project can have source files.
            /// </remarks>
            public bool XcodeUseNativeProjectForFastBuildApp { get; set; } = true;

            public Strings ResolvedSourceFilesBlobExclude = new Strings();

            public Strings ResolvedSourceFilesGenerateXmlDocumentationExclude = new Strings();

            public Strings ResolvedSourceFilesWithCompileAsCOption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsCPPOption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsObjCOption = new Strings();
            public Strings ResolvedSourceFilesWithCompileAsObjCPPOption = new Strings();
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

            /// <summary>
            /// Configuration OS version if not defined as Target fragment.
            /// </summary>
            /// <remarks>This allow adding OS version to specific DotNetFramework during configuration without altering Target's matching system</remarks>
            public DotNetOS DotNetOSVersion = DotNetOS.Default;

            /// <summary>
            /// Optional OS version at the end of the TargetFramework, for example, net5.0-ios13.0.
            /// </summary>
            /// <remarks>C# only, will throw if the target doesn't have a non-default DotNetOS fragment</remarks>
            public string DotNetOSVersionSuffix = string.Empty;

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

            private Resolver.ResolveStates _resolveState = Resolver.ResolveStates.NotResolved;

            /// <summary>
            ///  This helper function is used to implement properties that can only be allocated before resolving takes place. This will
            ///  be useful to reduce the number of allocations as we can now have a bunch of null container fields for unused properties.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="propertyField">The internal property field associated with the property</param>
            /// <param name="creator">Simple functor used to allocate the field when the field is accessed(only when we are in configure phase, before resolve)</param>
            /// <returns>T: The backing field or null</returns>
            private T GetDynamicPropertyField<T>(ref T propertyField, Func<T> creator)
            {
                if (propertyField == null && _resolveState == Resolver.ResolveStates.NotResolved)
                    propertyField = creator();
                return propertyField;
            }

            internal void Resolve(Resolver resolver)
            {
                if (_resolveState != Resolver.ResolveStates.NotResolved)
                    throw new Error("Can't resolve twice!");
                _resolveState = Resolver.ResolveStates.InProgress;

                if (PrecompHeader == null && PrecompSource != null)
                    throw new Error("Incoherent settings for {0} : PrecompHeader is null but PrecompSource is not", ToString());
                // TODO : Is it OK to comment this or is it a hack ?
                //if (PrecompHeader != null && PrecompSource == null)
                //    throw new Error("Incoherent settings for {0} : PrecompSource is null but PrecompHeader is not", ToString());

                SetPlatformDependentProperties();

                resolver.SetParameter("conf", this);
                resolver.SetParameter("target", Target);
                resolver.Resolve(this);

                Util.ResolvePath(Project.SharpmakeCsPath, ref ProjectPath);
                if (DebugBreaks.ShouldBreakOnProjectPath(DebugBreaks.Context.Resolving, Path.Combine(ProjectPath, ProjectFileName) + (Project is CSharpProject ? ".csproj" : ".vcxproj"), this))
                    System.Diagnostics.Debugger.Break();
                Util.ResolvePath(Project.SharpmakeCsPath, ref IntermediatePath);
                if (!string.IsNullOrEmpty(BaseIntermediateOutputPath))
                    Util.ResolvePath(Project.SharpmakeCsPath, ref BaseIntermediateOutputPath);
                Util.ResolvePath(Project.SharpmakeCsPath, ref LibraryPaths);
                Util.ResolvePathAndFixCase(Project.SharpmakeCsPath, ref TargetCopyFiles);
                Util.ResolvePath(Project.SharpmakeCsPath, ref TargetCopyFilesPath);
                Util.ResolvePathAndFixCase(Project.SharpmakeCsPath, Util.KeyValuePairResolveType.ResolveAll, ref EventPostBuildCopies);
                Util.ResolvePathAndFixCase(Project.SharpmakeCsPath, Util.KeyValuePairResolveType.ResolveKey, ref TargetCopyFilesToSubDirectory);
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
                Util.ResolvePath(Project.SourceRootPath, ref IncludeSystemPaths);
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
                if (!string.IsNullOrEmpty(XmlDocumentationFile))
                    Util.ResolvePath(Project.SourceRootPath, ref XmlDocumentationFile);

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
                    TargetFilePlatformPrefix = TargetFilePlatformPrefix.ToLowerInvariant();
                    TargetFilePrefix = TargetFilePrefix.ToLowerInvariant();
                    TargetFileName = TargetFileName.ToLowerInvariant();
                    TargetFileSuffix = TargetFileSuffix.ToLowerInvariant();
                    TargetFileFullName = TargetFileFullName.ToLowerInvariant();
                    TargetFileFullNameWithExtension = TargetFileFullNameWithExtension.ToLowerInvariant();
                    TargetFileFullExtension = TargetFileFullExtension.ToLowerInvariant();
                }

                _resolvedTargetDependsFiles.AddRange(TargetDependsFiles);
                _resolvedTargetCopyFiles.AddRange(TargetCopyFiles);

                foreach (var keyValuePair in TargetCopyFilesToSubDirectory)
                {
                    _resolvedTargetCopyFilesToSubDirectory.Add(keyValuePair);
                }

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

                    if(!customFileBuildStep.UseExecutableFromSystemPath)
                    {
                        Util.ResolvePath(Project.SourceRootPath, ref customFileBuildStep.Executable);
                    }

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
                        if (pathOption.Path != null)
                        {
                            Util.ResolvePath(Project.SourceRootPath, ref pathOption.Path);
                        }
                    }
                }

                foreach (var filter in ForcedIncludesFilters)
                {
                    Util.ResolvePath(Project.SourceRootPath, ref filter.ExcludeFiles);
                    Util.ResolvePath(Project.SourceRootPath, ref filter.FilterFiles);
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

                if (PostBuildStampExe != null && PostBuildStampExes.Any())
                    throw new Error("Incoherent settings for {0} : both PostBuildStampExe and PostBuildStampExes have values, they are mutually exclusive.", ToString());

                foreach (var stampExe in PostBuildStampExes)
                    stampExe.Resolve(resolver);

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

                ProjectReferencesByPath.Resolve(Project.SourceRootPath, resolver);

                resolver.RemoveParameter("conf");
                resolver.RemoveParameter("target");

                _resolveState = Resolver.ResolveStates.Resolved;
            }

            private void SetDependency(
                Type projectType,
                ITarget target,
                DependencySetting value
            )
            {
                ValueTuple<Type, ITarget> pair = new ValueTuple<Type, ITarget>(projectType, target);
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

            public bool HaveDependency<TPROJECT>()
            {
                return HaveDependency(typeof(TPROJECT));
            }

            public bool HaveDependency(Type projectType)
            {
                return UnResolvedPrivateDependencies.ContainsKey(projectType) || UnResolvedPublicDependencies.ContainsKey(projectType);
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
                    if (dependency.Key.Item1 == projectType)
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

            // For source compatibility, ProjectReferencesByPath is still an IEnumerable<string>
            public class ProjectReferencesByPathContainer : IEnumerable<string>
            {
                public class Info
                {
                    public string projectFilePath { get; internal set; }
                    public Guid projectGuid { get; internal set; }
                    public RefOptions refOptions { get; internal set; }
                    public Guid projectTypeGuid { get; internal set; }
                };

                [Flags]
                public enum RefOptions
                {
                    ReferenceOutputAssembly = 1 << 0,
                    CopyLocalSatelliteAssemblies = 1 << 1,
                    LinkLibraryDependencies = 1 << 2,
                    UseLibraryDependencyInputs = 1 << 3,
                    // VC default option
                    Default = ReferenceOutputAssembly | LinkLibraryDependencies,
                };

                /// <summary>
                /// Adds a new ProjectReferencesByPath path, with optionally the guid.
                /// Adding the guid allows to set the reference without opening the project.
                /// </summary>
                /// <param name="projectFilePath">The project file path</param>
                /// <param name="projectGuid">An optional project guid</param>
                /// <param name="refOptions">Reference options</param>
                /// <param name="projectTypeGuid">An optional project type guid, one member of ProjectTypeGuids. Deduced from file extension if not provided.</param>
                public void Add(string projectFilePath, Guid projectGuid = new Guid(), RefOptions refOptions = RefOptions.Default, Guid projectTypeGuid = new Guid())
                {
                    _projectsInfos.Add(new Info()
                    {
                        projectFilePath = projectFilePath,
                        projectGuid = projectGuid,
                        refOptions = refOptions,
                        projectTypeGuid = projectTypeGuid
                    });
                }

                public void AddRange(IEnumerable<string> projectFilePaths)
                {
                    foreach (var projectFilePath in projectFilePaths)
                    {
                        Add(projectFilePath);
                    }
                }

                public int Count => ProjectsInfos.Count();

                public IEnumerator<string> GetEnumerator()
                {
                    return ProjectsInfos.Select(pi => pi.projectFilePath).GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }

                public void Clear()
                {
                    _projectsInfos.Clear();
                }


                internal void Resolve(string sourceRootPath, Resolver resolver)
                {
                    if (IsResolved)
                        return;

                    if (_projectsInfos.Any())
                    {
                        for (int i = 0; i < _projectsInfos.Count; i++)
                        {
                            Info projectInfo = _projectsInfos[i];
                            string path = resolver.Resolve(projectInfo.projectFilePath);
                            Util.ResolvePath(sourceRootPath, ref path);
                            projectInfo.projectFilePath = path;
                        }
                    }

                    IsResolved = true;
                }

                public bool IsResolved { get; private set; } = false;
                public IEnumerable<Info> ProjectsInfos => _projectsInfos;
                private List<Info> _projectsInfos = new List<Info>();
            }

            public ProjectReferencesByPathContainer ProjectReferencesByPath = new ProjectReferencesByPathContainer();
            public Strings ReferencesByName = new Strings();
            public Strings ReferencesByNameExternal = new Strings();
            public Strings ReferencesByPath = new Strings();
            public string ConditionalReferencesByPathCondition = string.Empty;
            public Strings ConditionalReferencesByPath = new Strings();
            public Strings ForceUsingFiles = new Strings();

            public Strings CustomPropsFiles = new Strings();  // vs2010+ .props files
            /// <summary>
            /// CustomProperties for configuration level. Supported only in msbuild based targets(C++/C#)
            /// </summary>
            public Dictionary<string, string> CustomProperties = new Dictionary<string, string>();
            public Strings CustomTargetsFiles = new Strings();  // vs2010+ .targets files

            // NuGet packages (C# and visual studio c++ for now)
            public PackageReferences ReferencesByNuGetPackage = new PackageReferences();

            // Framework references in C#, see: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/target-aspnetcore?view=aspnetcore-5.0&tabs=visual-studio
            public Strings FrameworkReferences = new Strings();

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

            private static int SortDotNetDependencyForLink(DotNetDependency l, DotNetDependency r)
            {
                return SortConfigurationForLink(l.Configuration, r.Configuration);
            }
            private static int SortDependencyForLink((DependencyNode, DependencyType) l, (DependencyNode, DependencyType) r)
            {
                int cmpType = l.Item2.CompareTo(r.Item2);
                if (cmpType != 0)
                    return -cmpType; // reverse order (public first, private last)
                return SortConfigurationForLink(l.Item1._configuration, r.Item1._configuration);
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
                internal List<(DependencyNode, DependencyType)> _childNodes = new List<(DependencyNode, DependencyType)>();
            }

            public class VcxprojUserFileSettings
            {
                public string LocalDebuggerCommand = RemoveLineTag;
                public string LocalDebuggerCommandArguments = RemoveLineTag;
                public string LocalDebuggerEnvironment = RemoveLineTag;
                public string LocalDebuggerWorkingDirectory = RemoveLineTag;
                public bool LocalDebuggerAttach = false;
                public string PreLaunchCommand = RemoveLineTag;
                public string RemoteDebuggerCommand = RemoveLineTag;
                public string RemoteDebuggerCommandArguments = RemoveLineTag;
                public string RemoteDebuggingMode = RemoveLineTag;
                public string RemoteDebuggerWorkingDirectory = RemoveLineTag;
                public bool OverwriteExistingFile = true;
                public string LocalDebuggerAttachString => LocalDebuggerAttach ? "true" : RemoveLineTag;
            }

            public VcxprojUserFileSettings VcxprojUserFile = null;

            [Resolver.Resolvable]
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


            internal struct PropagationSettings : IEquatable<PropagationSettings>
            {
                public PropagationSettings(
                    DependencySetting dependencySetting,
                    bool isImmediate,
                    bool hasPublicPathToRoot,
                    bool hasPublicPathToImmediate,
                    bool goesThroughDLL,
                    bool isDotnetReferenceSwappedWithOutputAssembly,
                    bool hasProjectReference)
                {
                    DependencySetting = dependencySetting;
                    IsImmediate = isImmediate;
                    HasPublicPathToRoot = hasPublicPathToRoot;
                    HasPublicPathToImmediate = hasPublicPathToImmediate;
                    GoesThroughDLL = goesThroughDLL;
                    IsDotnetReferenceSwappedWithOutputAssembly = isDotnetReferenceSwappedWithOutputAssembly;
                    HasProjectReference = hasProjectReference;
                }

                public DependencySetting DependencySetting { get; }
                public bool IsImmediate { get; }
                public bool HasPublicPathToRoot { get; }
                public bool HasPublicPathToImmediate { get; }
                public bool GoesThroughDLL { get; }
                public bool IsDotnetReferenceSwappedWithOutputAssembly { get; }
                public bool HasProjectReference { get; }

                public override bool Equals(object obj)
                {
                    return obj is PropagationSettings other && Equals(other);
                }

                public bool Equals(PropagationSettings other)
                {
                    return DependencySetting == other.DependencySetting &&
                           IsImmediate == other.IsImmediate &&
                           HasPublicPathToRoot == other.HasPublicPathToRoot &&
                           HasPublicPathToImmediate == other.HasPublicPathToImmediate &&
                           GoesThroughDLL == other.GoesThroughDLL &&
                           IsDotnetReferenceSwappedWithOutputAssembly == other.IsDotnetReferenceSwappedWithOutputAssembly &&
                           HasProjectReference == other.HasProjectReference;
                }

                public override int GetHashCode()
                {
                    unchecked // Overflow is fine, just wrap
                    {
                        int hash = 17;
                        hash = hash * 23 + DependencySetting.GetHashCode();
                        hash = hash * 23 + IsImmediate.GetHashCode();
                        hash = hash * 23 + HasPublicPathToRoot.GetHashCode();
                        hash = hash * 23 + HasPublicPathToImmediate.GetHashCode();
                        hash = hash * 23 + GoesThroughDLL.GetHashCode();
                        hash = hash * 23 + IsDotnetReferenceSwappedWithOutputAssembly.GetHashCode();
                        hash = hash * 23 + HasProjectReference.GetHashCode();
                        return hash;
                    }
                }
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
                
                // Keep track of all configurations that have been swapped to dll as we don't want to include them in the final solution
                HashSet<Configuration> configurationsSwappedToDll = null;

                // We also keep track of configurations that have been added explicitly without swapping, to make sure the project is still included in the solution
                HashSet<Configuration> configurationsStillUsedAsNotSwappedToDll = null;

                var visitedNodes = new Dictionary<DependencyNode, List<PropagationSettings>>();
                var visitingNodes = new Stack<Tuple<DependencyNode, PropagationSettings>>();
                visitingNodes.Push(
                    Tuple.Create(
                        rootNode,
                        new PropagationSettings(
                            DependencySetting.Default,
                            isImmediate: true,
                            hasPublicPathToRoot: true,
                            hasPublicPathToImmediate: true,
                            goesThroughDLL: false,
                            isDotnetReferenceSwappedWithOutputAssembly: false,
                            hasProjectReference: true)));

                (IConfigurationTasks, Platform)? lastPlatformConfigurationTasks = null;

                IConfigurationTasks GetConfigurationTasks(Platform platform)
                {
                    if (lastPlatformConfigurationTasks.HasValue && lastPlatformConfigurationTasks.Value.Item2 == platform)
                    {
                        return lastPlatformConfigurationTasks.Value.Item1;
                    }

                    var tasks = PlatformRegistry.Get<IConfigurationTasks>(platform);

                    lastPlatformConfigurationTasks = (tasks, platform);

                    return tasks;
                }

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
                    bool isImmediate = propagationSetting.IsImmediate;
                    bool hasPublicPathToRoot = propagationSetting.HasPublicPathToRoot;
                    bool hasPublicPathToImmediate = propagationSetting.HasPublicPathToImmediate;
                    bool goesThroughDLL = propagationSetting.GoesThroughDLL;
                    bool isDotnetReferenceSwappedWithOutputAssembly = propagationSetting.IsDotnetReferenceSwappedWithOutputAssembly || visitedNode._dependencySetting.HasFlag(DependencySetting.DependOnAssemblyOutput);
                    bool hasProjectReference = propagationSetting.HasProjectReference && !visitedNode._dependencySetting.HasFlag(DependencySetting.NoProjectReference);

                    foreach (var childNode in visitedNode._childNodes)
                    {
                        var childTuple = Tuple.Create(
                            childNode.Item1,
                            new PropagationSettings(
                                isRoot ? childNode.Item1._dependencySetting : (propagationSetting.DependencySetting & childNode.Item1._dependencySetting), // propagate the parent setting by masking it
                                isRoot, // only children of root are immediate
                                (isRoot || hasPublicPathToRoot) && childNode.Item2 == DependencyType.Public,
                                (isImmediate || hasPublicPathToImmediate) && childNode.Item2 == DependencyType.Public,
                                !isRoot && (goesThroughDLL || visitedNode._configuration.Output == OutputType.Dll),
                                isDotnetReferenceSwappedWithOutputAssembly || visitedNode._dependencySetting.HasFlag(DependencySetting.DependOnAssemblyOutput),
                                hasProjectReference && !visitedNode._dependencySetting.HasFlag(DependencySetting.NoProjectReference)
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

                    var dependencySetting = propagationSetting.DependencySetting;
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

                        foreach (var keyValuePair in dependency.TargetCopyFilesToSubDirectory)
                        {
                            _resolvedTargetCopyFilesToSubDirectory.Add(keyValuePair);
                        }
                    }
                    else if (Output == OutputType.None && isExport == false)
                    {
                        GenericBuildDependencies.Add(dependency);
                    }

                    if (dependency.Output == OutputType.Lib
                        || dependency.Output == OutputType.Dll
                        || dependency.Output == OutputType.Utility
                        || dependency.Output == OutputType.None)
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
                                        GetConfigurationTasks(dependency.Platform).SetupStaticLibraryPaths(this, dependencySetting, dependency);
                                    if (dependencySetting.HasFlag(DependencySetting.LibraryFiles) && hasProjectReference)
                                        ConfigurationDependencies.Add(dependency);
                                    if (dependencySetting == DependencySetting.OnlyBuildOrder && hasProjectReference)
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

                                if (dependency.Platform.Equals(Platform.mac) ||
                                    dependency.Platform.Equals(Platform.ios) ||
                                    dependency.Platform.Equals(Platform.tvos) ||
                                    dependency.Platform.Equals(Platform.watchos) ||
                                    dependency.Platform.Equals(Platform.maccatalyst)
                                )
                                {
                                    if (dependency.XcodeSystemFrameworks.Count > 0)
                                        XcodeDependenciesSystemFrameworks.AddRange(dependency.XcodeSystemFrameworks);

                                    if (dependency.XcodeDeveloperFrameworks.Count > 0)
                                        XcodeDependenciesDeveloperFrameworks.AddRange(dependency.XcodeDeveloperFrameworks);

                                    if (dependency.XcodeUserFrameworks.Count > 0)
                                        XcodeDependenciesUserFrameworks.AddRange(dependency.XcodeUserFrameworks);

                                    if (dependency.XcodeEmbeddedFrameworks.Count > 0)
                                        XcodeDependenciesEmbeddedFrameworks.AddRange(dependency.XcodeEmbeddedFrameworks);

                                    if (dependency.XcodeSystemFrameworkPaths.Count > 0)
                                        XcodeDependenciesSystemFrameworkPaths.AddRange(dependency.XcodeSystemFrameworkPaths);

                                    if (dependency.XcodeFrameworkPaths.Count > 0)
                                        XcodeDependenciesFrameworkPaths.AddRange(dependency.XcodeFrameworkPaths);
                                }

                                // If our no-output project is just a build-order dependency, update the build order accordingly
                                if (!dependencyOutputLib && isImmediate && dependencySetting == DependencySetting.OnlyBuildOrder && !isExport)
                                    GenericBuildDependencies.Add(dependency);
                            }
                            break;
                        case OutputType.Dll:
                        case OutputType.AppleFramework:
                        case OutputType.AppleBundle:
                            {
                                var configTasks = GetConfigurationTasks(dependency.Platform);

                                if (dependency.ExportDllSymbols && (isImmediate || hasPublicPathToRoot || !goesThroughDLL))
                                {
                                    if (explicitDependenciesGlobal || !compile || (IsFastBuild && Util.IsDotNet(dependency)))
                                        configTasks.SetupDynamicLibraryPaths(this, dependencySetting, dependency);
                                    if (dependencySetting.HasFlag(DependencySetting.LibraryFiles) && hasProjectReference)
                                        ConfigurationDependencies.Add(dependency);
                                    if (dependencySetting.HasFlag(DependencySetting.ForceUsingAssembly))
                                        ForceUsingDependencies.Add(dependency);
                                    if (dependencySetting == DependencySetting.OnlyBuildOrder && hasProjectReference)
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

                                if (!dependency.ExportDllSymbols && (isImmediate || hasPublicPathToRoot || !goesThroughDLL))
                                {
                                    if (dependencySetting.HasFlag(DependencySetting.LibraryFiles) ||
                                        dependencySetting.HasFlag(DependencySetting.ForceUsingAssembly) ||
                                        dependencySetting == DependencySetting.OnlyBuildOrder)
                                        BuildOrderDependencies.Add(dependency);
                                }

                                if (dependencySetting.HasFlag(DependencySetting.AdditionalUsingDirectories) ||
                                    dependencySetting.HasFlag(DependencySetting.ForceUsingAssembly))
                                    AdditionalUsingDirectories.Add(dependency.TargetPath);

                                string dependencyDllFullPath = Path.Combine(dependency.TargetPath, dependency.TargetFileFullNameWithExtension);
                                if ((Output == OutputType.Exe || ExecuteTargetCopy)
                                    && dependency.AllowOutputDllCopy
                                    && dependencySetting.HasFlag(DependencySetting.LibraryFiles)
                                    && dependency.TargetPath != TargetPath)
                                {
                                    // If using OnlyBuildOrder, ExecuteTargetCopy must be set to enable the copy.
                                    if (dependencySetting != DependencySetting.OnlyBuildOrder || ExecuteTargetCopy)
                                    {
                                        _resolvedTargetCopyFiles.Add(dependencyDllFullPath);
                                        // Add PDBs only if they exist and the dependency is not an [export] project
                                        if ((!isExport || dependency.AllowExportProjectsToCopyPdbToDependentTargets) &&
                                            Sharpmake.Options.GetObject<Options.Vc.Linker.GenerateDebugInformation>(dependency) != Sharpmake.Options.Vc.Linker.GenerateDebugInformation.Disable)
                                        {
                                            if (dependency.CopyLinkerPdbToDependentTargets)
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
                        case OutputType.AppleApp:
                        case OutputType.Exe:
                            {
                                if (hasPublicPathToRoot)
                                    resolvedDotNetPublicDependencies.Add(new DotNetDependency(dependency));
                                else if (isImmediate)
                                    resolvedDotNetPrivateDependencies.Add(new DotNetDependency(dependency));

                                if (hasProjectReference)
                                {
                                    if (dependencySetting == DependencySetting.OnlyBuildOrder)
                                        BuildOrderDependencies.Add(dependency);
                                    else
                                        ConfigurationDependencies.Add(dependency);
                                }
                            }
                            break;
                        case OutputType.Utility:
                            {
                                // As visual studio do not handle reference being different between configuration,
                                // We use the "utility" output to mark a configuration as exclude from build.
                                if (!goesThroughDLL &&
                                        (Output == OutputType.Lib ||
                                         dependency.ExportSymbolThroughProject == null ||
                                         dependency.ExportSymbolThroughProject == Project.GetType()) &&
                                         dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                                {
                                    ConfigurationDependencies.Add(dependency);
                                }
                            }
                            break;
                        case OutputType.DotNetConsoleApp:
                        case OutputType.DotNetClassLibrary:
                        case OutputType.DotNetWindowsApp:
                            {
                                if (dependencySetting.HasFlag(DependencySetting.AdditionalUsingDirectories) ||
                                    dependencySetting.HasFlag(DependencySetting.ForceUsingAssembly))
                                    AdditionalUsingDirectories.Add(dependency.TargetPath);

                                bool? referenceOutputAssembly = ReferenceOutputAssembly;
                                if (isImmediate && dependencySetting == DependencySetting.OnlyBuildOrder)
                                    referenceOutputAssembly = false;
                                if (dependencySetting.HasFlag(DependencySetting.ForceUsingAssembly))
                                    ForceUsingDependencies.Add(dependency);

                                var dotNetDependency = new DotNetDependency(dependency)
                                {
                                    ReferenceOutputAssembly = referenceOutputAssembly,
                                    ReferenceSwappedWithOutputAssembly = isDotnetReferenceSwappedWithOutputAssembly,
                                    CopyLocal = visitedNode._dependencySetting.HasFlag(DependencySetting.CopyLocal)
                                };
                                
                                if (isDotnetReferenceSwappedWithOutputAssembly)
                                {
                                    configurationsSwappedToDll ??= new HashSet<Configuration>();
                                    configurationsSwappedToDll.Add(dotNetDependency.Configuration);
                                }
                                else
                                {
                                    configurationsStillUsedAsNotSwappedToDll ??= new HashSet<Configuration>();
                                    configurationsStillUsedAsNotSwappedToDll.Add(dotNetDependency.Configuration);
                                }

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

                DotNetPublicDependencies.Sort(SortDotNetDependencyForLink);
                DotNetPrivateDependencies.Sort(SortDotNetDependencyForLink);

                if (configurationsSwappedToDll is not null)
                {
                    ConfigurationsSwappedToDll = configurationsSwappedToDll;

                    // Remove configurations that have been explicitly used as not swapped to dll
                    if (configurationsStillUsedAsNotSwappedToDll is not null)
                        ConfigurationsSwappedToDll.ExceptWith(configurationsStillUsedAsNotSwappedToDll);
                }
                
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

                Dictionary<Configuration, DependencyNode> visited = new Dictionary<Configuration, DependencyNode>(64);

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
#if DEBUG
                        foreach (var child in alreadyExisting._childNodes)
                        {
                            Debug.Assert(visitedNode._childNodes.All(c => c.Item1 != child.Item1));
                        }
#endif

                        visitedNode._childNodes.AddRange(alreadyExisting._childNodes);

                        continue;
                    }

                    visited.Add(visitedConfiguration, visitedNode);

                    var unresolvedDependencies = new[] { visitedConfiguration.UnResolvedPublicDependencies, visitedConfiguration.UnResolvedPrivateDependencies };

                    int total = 0;

                    foreach (Dictionary<Type, ITarget> dependencies in unresolvedDependencies)
                    {
                        total += dependencies.Count;
                    }

                    visitedNode._childNodes.Capacity += total;

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
                            var key = new ValueTuple<Type, ITarget>(pair.Key, pair.Value);

                            if (!visitedConfiguration._dependenciesSetting.TryGetValue(key, out dependencySetting))
                                dependencySetting = DependencySetting.Default;

                            DependencyNode childNode = new DependencyNode(dependencyConf, dependencySetting);
#if DEBUG
                            Debug.Assert(visitedNode._childNodes.All(c => c.Item1 != childNode));
#endif
                            visitedNode._childNodes.Add((childNode, dependencyType));

                            visiting.Push(childNode);
                        }
                    }
                    visitedNode._childNodes.Sort(SortDependencyForLink);
                }

                return rootNode;
            }

            internal void SetPlatformDependentProperties()
            {
                var configTasks = PlatformRegistry.Get<IConfigurationTasks>(Platform);

                DllFullExtension = configTasks.GetDefaultOutputFullExtension(OutputType.Dll);
                ExecutableFullExtension = configTasks.GetDefaultOutputFullExtension(OutputType.Exe);

                if (TargetFilePlatformPrefix == null)
                    TargetFilePlatformPrefix = configTasks.GetOutputFileNamePrefix(Output);
                if (TargetFileFullExtension == null)
                    TargetFileFullExtension = configTasks.GetDefaultOutputFullExtension(Output);
                if (Project is CSharpProject)
                    TargetFileName = Project.AssemblyName;
            }
        }
    }
}
