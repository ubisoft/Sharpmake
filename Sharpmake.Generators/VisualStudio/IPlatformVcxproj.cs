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
        string ExecutableFileExtension { get; }
        string PackageFileExtension { get; }
        string SharedLibraryFileExtension { get; }
        string ProgramDatabaseFileExtension { get; }
        string StaticLibraryFileExtension { get; }
        string StaticOutputLibraryFileExtension { get; }
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

        IEnumerable<string> GetCxUsingPath(IGenerationContext context);

        IEnumerable<VariableAssignment> GetEnvironmentVariables(IGenerationContext context);

        string GetOutputFileNamePrefix(IGenerationContext context, Project.Configuration.OutputType outputType);

        void SetupDeleteExtensionsOnCleanOptions(IGenerationContext context);
        void SetupSdkOptions(IGenerationContext context);
        void SetupPlatformToolsetOptions(IGenerationContext context);
        void SetupPlatformTargetOptions(IGenerationContext context);
        void SelectCompilerOptions(IGenerationContext context);
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
        void SetupPlatformLibraryOptions(ref string platformLibExtension, ref string platformOutputLibExtension, ref string platformPrefixExtension);
    }
}
