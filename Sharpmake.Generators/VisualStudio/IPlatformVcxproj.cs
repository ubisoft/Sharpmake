// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Sharpmake.Generators.VisualStudio
{
    public class IncludeWithPrefix
    {
        public string CmdLinePrefix { get; }
        public string Path { get; }

        public IncludeWithPrefix(string cmdLinePrefix, string includePath)
        {
            CmdLinePrefix = cmdLinePrefix;
            Path = includePath;
        }
    }

    public interface IPlatformVcxproj
    {
        // ExecutableFileExtension
        // PackageFileExtension
        // SharedLibraryFileExtension
        // ProgramDatabaseFileExtension
        // StaticLibraryFileExtension
        // StaticOutputLibraryFileExtension

        // the above properties have been replaced by their "Full" equivalents below
        // because most required sharpmake to add a leading ".", which was an issue on some platforms

        string ExecutableFileFullExtension { get; }
        string PackageFileFullExtension { get; }
        string SharedLibraryFileFullExtension { get; }
        string ProgramDatabaseFileFullExtension { get; }
        string StaticLibraryFileFullExtension { get; }
        string StaticOutputLibraryFileFullExtension { get; }

        bool ExcludesPrecompiledHeadersFromBuild { get; }
        bool HasUserAccountControlSupport { get; }
        bool HasEditAndContinueDebuggingSupport { get; }

        IEnumerable<string> GetImplicitlyDefinedSymbols(IGenerationContext context);

        IEnumerable<string> GetLibraryPaths(IGenerationContext context);

        IEnumerable<string> GetLibraryFiles(IGenerationContext context);
        IEnumerable<string> GetPlatformLibraryFiles(IGenerationContext context);

        // IncludePaths should contain only the project's own includes, and PlatformIncludePaths
        // are the platform's include paths.
        IEnumerable<string> GetIncludePaths(IGenerationContext context);
        IEnumerable<string> GetPlatformIncludePaths(IGenerationContext context);
        IEnumerable<IncludeWithPrefix> GetPlatformIncludePathsWithPrefix(IGenerationContext context);
        IEnumerable<string> GetResourceIncludePaths(IGenerationContext context);
        IEnumerable<string> GetAssemblyIncludePaths(IGenerationContext context);

        IEnumerable<string> GetCxUsingPath(IGenerationContext context);

        IEnumerable<VariableAssignment> GetEnvironmentVariables(IGenerationContext context);

        // GetOutputFileNamePrefix is now in IConfigurationTasks

        void SetupDeleteExtensionsOnCleanOptions(IGenerationContext context);
        void SetupSdkOptions(IGenerationContext context);
        void SetupPlatformToolsetOptions(IGenerationContext context);
        void SetupPlatformTargetOptions(IGenerationContext context);
        void SelectCompilerOptions(IGenerationContext context);
        void SelectPrecompiledHeaderOptions(IGenerationContext context);
        void SelectLinkerOptions(IGenerationContext context);
        void SelectPlatformAdditionalDependenciesOptions(IGenerationContext context);
        void SelectApplicationFormatOptions(IGenerationContext context);
        void SelectBuildType(IGenerationContext context);

        void SelectPreprocessorDefinitionsVcxproj(IVcxprojGenerationContext context);

        bool HasPrecomp(IGenerationContext context);

        void GenerateSdkVcxproj(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateMakefileConfigurationVcxproj(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateProjectCompileVcxproj(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateProjectLinkVcxproj(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateProjectMasmVcxproj(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateProjectNasmVcxproj(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateUserConfigurationFile(Project.Configuration conf, IFileGenerator generator); // Should take IVcxprojGenerationContext but this is called by BaseUserFile which should not know that interface.
        void GenerateRunFromPcDeployment(IVcxprojGenerationContext context, IFileGenerator generator);

        void GeneratePlatformSpecificProjectDescription(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateProjectPlatformSdkDirectoryDescription(IVcxprojGenerationContext context, IFileGenerator generator);
        void GeneratePostDefaultPropsImport(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateProjectConfigurationGeneral(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateProjectConfigurationGeneral2(IVcxprojGenerationContext context, IFileGenerator generator); // TODO: Merge with the above function and edit the reference projects.
        void GenerateProjectConfigurationFastBuildMakeFile(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateProjectConfigurationCustomMakeFile(IVcxprojGenerationContext context, IFileGenerator generator);
        void GenerateProjectPlatformImportSheet(IVcxprojGenerationContext context, IFileGenerator generator);
        void GeneratePlatformResourceFileList(IVcxprojGenerationContext context, IFileGenerator generator, Strings alreadyWrittenPriFiles, IList<Vcxproj.ProjectFile> resourceFiles, IList<Vcxproj.ProjectFile> imageResourceFiles);
        void GeneratePlatformReferences(IVcxprojGenerationContext context, IFileGenerator generator);

        // type -> files
        IEnumerable<Tuple<string, List<Vcxproj.ProjectFile>>> GetPlatformFileLists(IVcxprojGenerationContext context);

        // TODO: Refactor this.
        void SetupPlatformLibraryOptions(out string platformLibExtension, out string platformOutputLibExtension, out string platformPrefixExtension, out string platformLibPrefix);
    }
}
