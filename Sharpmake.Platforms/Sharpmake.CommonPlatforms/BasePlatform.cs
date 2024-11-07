// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public abstract partial class BasePlatform : IPlatformDescriptor, IPlatformBff, IPlatformVcxproj
    {
        #region IPlatformDescriptor
        public abstract string SimplePlatformString { get; }
        public abstract string GetToolchainPlatformString(ITarget target);
        public abstract bool IsMicrosoftPlatform { get; }
        public abstract bool IsPcPlatform { get; }
        public abstract bool IsUsingClang { get; }
        public abstract bool IsLinkerInvokedViaCompiler { get; set; }
        public abstract bool HasDotNetSupport { get; }
        public abstract bool HasSharedLibrarySupport { get; }
        public virtual bool HasPrecompiledHeaderSupport => true;

        public virtual EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] parameters)
        {
            //
            // TODO: EnvironmentVariableResolver is not an actual environment variable resolver,
            //       and doesn't care about environment variables, just those passed to it in the
            //       argument. This causes it to attempt to resolve environment variable that were
            //       not passed to it, and throw an exception for it.
            //

            //return new EnvironmentVariableResolver(assignments);
            return null;
        }
        #endregion

        #region IPlatformBff implementation
        protected const string RemoveLineTag = FileGeneratorUtilities.RemoveLineTag;

        public virtual string BffPlatformDefine => null;

        public virtual string CConfigName(Configuration conf)
        {
            return string.Empty;
        }

        public virtual string CppConfigName(Configuration conf)
        {
            return string.Empty;
        }

        public virtual void SelectPreprocessorDefinitionsBff(IBffGenerationContext context)
        {
            var platformDescriptor = PlatformRegistry.Get<IPlatformDescriptor>(context.Configuration.Platform);
            string platformDefineSwitch = platformDescriptor.IsUsingClang ? "-D" : "/D";

            var defines = new Strings();
            defines.AddRange(context.Options.ExplicitDefines);
            defines.AddRange(context.Configuration.Defines);

            if (defines.Count > 0)
            {
                var fastBuildDefines = new List<string>();

                foreach (string define in defines.SortedValues)
                {
                    if (!string.IsNullOrWhiteSpace(define))
                        fastBuildDefines.Add(string.Format(@"{0}{1}{2}{1}", platformDefineSwitch, Util.DoubleQuotes, define.Replace(Util.DoubleQuotes, Util.EscapedDoubleQuotes)));
                }
                context.CommandLineOptions["PreprocessorDefinitions"] = string.Join($"'{Environment.NewLine}            + ' ", fastBuildDefines);
            }
            else
            {
                context.CommandLineOptions["PreprocessorDefinitions"] = FileGeneratorUtilities.RemoveLineTag;
            }

            Strings resourceDefines = Options.GetStrings<Options.Vc.ResourceCompiler.PreprocessorDefinitions>(context.Configuration);
            if (resourceDefines.Any())
            {
                var fastBuildDefines = new List<string>();

                foreach (string resourceDefine in resourceDefines.SortedValues)
                {
                    if (!string.IsNullOrWhiteSpace(resourceDefine))
                        fastBuildDefines.Add(string.Format(@"""{0}{1}""", platformDefineSwitch, resourceDefine.Replace(Util.DoubleQuotes, Util.EscapedDoubleQuotes)));
                }
                context.CommandLineOptions["ResourcePreprocessorDefinitions"] = string.Join($"'{Environment.NewLine}                                    + ' ", fastBuildDefines);
            }
            else
            {
                context.CommandLineOptions["ResourcePreprocessorDefinitions"] = FileGeneratorUtilities.RemoveLineTag;
            }
        }
        public virtual void SelectAdditionalCompilerOptionsBff(IBffGenerationContext context)
        {
        }

        public virtual void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration configuration, string fastBuildOutputFile)
        {
            SetupExtraLinkerSettings(fileGenerator, configuration.Output);
        }

        private void SetupExtraLinkerSettings(IFileGenerator fileGenerator, Project.Configuration.OutputType outputType)
        {
            using (fileGenerator.Resolver.NewScopedParameter("dllOption", outputType == Project.Configuration.OutputType.Dll ? " /DLL" : ""))
            {
                fileGenerator.Write(Bff.Template.ConfigurationFile.LinkerOptions);
            }
        }

        public virtual IEnumerable<Project.Configuration.BuildStepBase> GetExtraPostBuildEvents(Project.Configuration configuration, string fastBuildOutputFile)
        {
            return Enumerable.Empty<Project.Configuration.BuildStepBase>();
        }

        public virtual IEnumerable<Project.Configuration.BuildStepExecutable> GetExtraStampEvents(Project.Configuration configuration, string fastBuildOutputFile)
        {
            return Enumerable.Empty<Project.Configuration.BuildStepExecutable>();
        }

        public virtual string GetOutputFilename(Project.Configuration.OutputType outputType, string fastBuildOutputFile) => fastBuildOutputFile;

        public virtual void AddCompilerSettings(IDictionary<string, CompilerSettings> masterCompilerSettings, Project.Configuration conf)
        {
        }
        #endregion

        #region IPlatformVcxproj implementation
        public abstract string ExecutableFileFullExtension { get; }
        public virtual string PackageFileFullExtension => ExecutableFileFullExtension;
        public abstract string SharedLibraryFileFullExtension { get; }
        public abstract string ProgramDatabaseFileFullExtension { get; }
        public virtual string StaticLibraryFileFullExtension => ".lib";
        public virtual string StaticOutputLibraryFileFullExtension => StaticLibraryFileFullExtension;
        public virtual bool ExcludesPrecompiledHeadersFromBuild => false;
        public virtual bool HasUserAccountControlSupport => false;
        public virtual bool HasEditAndContinueDebuggingSupport => false;

        public virtual void SetupDeleteExtensionsOnCleanOptions(IGenerationContext context)
        {
        }

        public virtual IEnumerable<string> GetImplicitlyDefinedSymbols(IGenerationContext context)
        {
            yield break;
        }

        public virtual IEnumerable<string> GetLibraryPaths(IGenerationContext context)
        {
            yield break;
        }

        public virtual IEnumerable<string> GetLibraryFiles(IGenerationContext context)
        {
            yield break;
        }

        public virtual IEnumerable<string> GetPlatformLibraryFiles(IGenerationContext context)
        {
            yield break;
        }

        public IEnumerable<string> GetIncludePaths(IGenerationContext context)
        {
            return GetIncludePathsImpl(context);
        }

        public IEnumerable<string> GetPlatformIncludePaths(IGenerationContext context)
        {
            return GetPlatformIncludePathsWithPrefixImpl(context).Select(x => x.Path);
        }

        public IEnumerable<IncludeWithPrefix> GetPlatformIncludePathsWithPrefix(IGenerationContext context)
        {
            return GetPlatformIncludePathsWithPrefixImpl(context);
        }

        public IEnumerable<string> GetResourceIncludePaths(IGenerationContext context)
        {
            return GetResourceIncludePathsImpl(context);
        }

        public IEnumerable<string> GetAssemblyIncludePaths(IGenerationContext context)
        {
            return GetAssemblyIncludePathsImpl(context);
        }

        public virtual IEnumerable<string> GetCxUsingPath(IGenerationContext context)
        {
            yield break;
        }

        public virtual IEnumerable<VariableAssignment> GetEnvironmentVariables(IGenerationContext context)
        {
            yield break;
        }

        public virtual void SetupSdkOptions(IGenerationContext context)
        {
        }

        public virtual void SetupPlatformToolsetOptions(IGenerationContext context)
        {
        }

        public virtual void SetupPlatformTargetOptions(IGenerationContext context)
        {
            context.Options["TargetMachine"] = FileGeneratorUtilities.RemoveLineTag;
            context.CommandLineOptions["TargetMachine"] = FileGeneratorUtilities.RemoveLineTag;
        }

        public virtual void SelectCompilerOptions(IGenerationContext context)
        {
        }

        protected void FixupPrecompiledHeaderOptions(IGenerationContext context)
        {
            var options = context.Options;
            var cmdLineOptions = context.CommandLineOptions;
            var conf = context.Configuration;

            if (options["UsePrecompiledHeader"] == "NotUsing")
            {
                options["UsePrecompiledHeader"] = FileGeneratorUtilities.RemoveLineTag;
            }
            else
            {
                Strings pathsToConsider = new Strings(context.ProjectSourceCapitalized);
                pathsToConsider.AddRange(context.Project.AdditionalSourceRootPaths);
                pathsToConsider.AddRange(GetIncludePaths(context));

                string pchFileSourceRelative = context.Options["PrecompiledHeaderThrough"];

                string pchFileVcxprojRelative = null;
                bool foundPchInclude = false;

                foreach (var includePath in pathsToConsider)
                {
                    var pchFile = Util.PathGetAbsolute(includePath, pchFileSourceRelative);
                    if (conf.Project.ResolvedSourceFiles.Contains(pchFile))
                    {
                        pchFileVcxprojRelative = Util.PathGetRelative(context.ProjectDirectory, pchFile, true);
                        foundPchInclude = true;
                        break;
                    }
                }

                if (!foundPchInclude)
                {
                    foreach (var includePath in pathsToConsider)
                    {
                        var pchFile = Util.PathGetAbsolute(includePath, pchFileSourceRelative);
                        if (Util.FileExists(pchFile))
                        {
                            pchFileVcxprojRelative = Util.PathGetRelative(context.ProjectDirectory, pchFile, true);
                            foundPchInclude = true;
                            break;
                        }
                    }
                }

                if (!foundPchInclude)
                    throw new Error($"Sharpmake couldn't locate the PCH '{pchFileSourceRelative}' in {conf}");

                context.Options["PrecompiledHeaderThrough"] = pchFileVcxprojRelative;
            }
        }

        public virtual void SelectPrecompiledHeaderOptions(IGenerationContext context)
        {
        }

        public virtual void SelectLinkerOptions(IGenerationContext context)
        {
        }

        public virtual void SelectPlatformAdditionalDependenciesOptions(IGenerationContext context)
        {
        }

        public virtual void SelectApplicationFormatOptions(IGenerationContext context)
        {
        }

        public virtual void SelectBuildType(IGenerationContext context)
        {
        }

        public virtual void SelectPreprocessorDefinitionsVcxproj(IVcxprojGenerationContext context)
        {
            // concat defines, don't add options.Defines since they are automatically added by VS
            var defines = new Strings();
            defines.AddRange(context.Options.ExplicitDefines);
            defines.AddRange(context.Configuration.Defines);

            context.Options["PreprocessorDefinitions"] = defines.JoinStrings(";");
        }

        public virtual bool HasPrecomp(IGenerationContext context)
        {
            Project.Configuration conf = context.Configuration;
            return !string.IsNullOrEmpty(conf.PrecompSource) && !string.IsNullOrEmpty(conf.PrecompHeader);
        }

        public virtual void GenerateSdkVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
        }

        public virtual void GenerateMakefileConfigurationVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
        }

        public virtual void GenerateProjectCompileVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            generator.Write(_projectConfigurationsCompileTemplate);
        }

        public virtual void GenerateProjectLinkVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            var simpleOutput = Project.Configuration.SimpleOutputType(context.Configuration.Output);
            switch (simpleOutput)
            {
                case Project.Configuration.OutputType.Lib:
                    generator.Write(GetProjectStaticLinkVcxprojTemplate());
                    break;
                case Project.Configuration.OutputType.Dll:
                    generator.Write(GetProjectLinkSharedVcxprojTemplate());
                    break;
                case Project.Configuration.OutputType.Exe:
                    generator.Write(GetProjectLinkExecutableVcxprojTemplate());
                    break;
            }
        }

        public virtual void GenerateProjectMasmVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
        }

        public virtual void GenerateProjectNasmVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            // Fill Assembly include dirs
            var preIncludedFiles = new List<string>();
            preIncludedFiles.AddRange(context.Project.NasmPreIncludedFiles.AsEnumerable<string>());

            string preIncludedFilesJoined = string.Join(';', preIncludedFiles);

            using (generator.Declare("ExePath", context.Project.NasmExePath))
            using (generator.Declare("PreIncludedFiles", preIncludedFilesJoined))
            {
                generator.Write(_projectConfigurationsNasmTemplate);
            }
        }

        public virtual void GenerateUserConfigurationFile(Project.Configuration conf, IFileGenerator generator)
        {
            generator.Write(_userFileConfigurationGeneralTemplate);
        }

        public virtual void GenerateRunFromPcDeployment(IVcxprojGenerationContext context, IFileGenerator generator)
        {
        }

        protected virtual void WriteWindowsKitsOverrides(IVcxprojGenerationContext context, IFileGenerator fileGenerator)
        {
            KitsRootEnum? kitsRootWritten = null;
            for (DevEnv devEnv = context.DevelopmentEnvironmentsRange.MinDevEnv; devEnv <= context.DevelopmentEnvironmentsRange.MaxDevEnv; devEnv = (DevEnv)((int)devEnv << 1))
            {
                // there's no need to write the properties with older versions of vs, as we override
                // completely the VC++ directories entries in the vcxproj
                if (devEnv < DevEnv.vs2015)
                    continue;

                KitsRootEnum kitsRootVersion = KitsRootPaths.GetUseKitsRootForDevEnv(devEnv);
                if (kitsRootWritten == null)
                    kitsRootWritten = kitsRootVersion;
                else if (kitsRootWritten != kitsRootVersion)
                    throw new Error($"Different values of kitsRoot in the same vcxproj {context.ProjectFileName}");
                else
                    continue;

                string windowsSdkDirKey = FileGeneratorUtilities.RemoveLineTag;
                string windowsSdkDirValue = FileGeneratorUtilities.RemoveLineTag;

                string UniversalCRTSdkDir_10 = FileGeneratorUtilities.RemoveLineTag;
                string UCRTContentRoot = FileGeneratorUtilities.RemoveLineTag;

                string targetPlatformVersionString = FileGeneratorUtilities.RemoveLineTag;
                if (kitsRootVersion != KitsRootEnum.KitsRoot81) // 8.1 is the default value for vs2015 and vs2017, so only specify a different platformVersion if we need to
                    targetPlatformVersionString = KitsRootPaths.GetWindowsTargetPlatformVersionForDevEnv(devEnv).ToVersionString();

                if (devEnv.OverridenWindowsPath())
                {
                    windowsSdkDirValue = Util.EnsureTrailingSeparator(KitsRootPaths.GetRoot(kitsRootVersion));
                    switch (kitsRootVersion)
                    {
                        case KitsRootEnum.KitsRoot:
                            windowsSdkDirKey = "WindowsSdkDir_80";
                            break;
                        case KitsRootEnum.KitsRoot81:
                            windowsSdkDirKey = "WindowsSdkDir_81";
                            break;
                        case KitsRootEnum.KitsRoot10:
                            {
                                windowsSdkDirKey = "WindowsSdkDir_10";
                                UniversalCRTSdkDir_10 = windowsSdkDirValue;

                                // this variable is found in Windows Kits\10\DesignTime\CommonConfiguration\Neutral\uCRT.props
                                // it is always read from the registry unless overridden, so we need to explicitly set it
                                UCRTContentRoot = windowsSdkDirValue;
                            }
                            break;
                        default:
                            throw new NotImplementedException($"Unsupported kitsRoot '{kitsRootVersion}'");
                    }
                }

                using (fileGenerator.Declare("windowsSdkDirKey", windowsSdkDirKey))
                using (fileGenerator.Declare("windowsSdkDirValue", windowsSdkDirValue))
                using (fileGenerator.Declare("UniversalCRTSdkDir_10", UniversalCRTSdkDir_10))
                using (fileGenerator.Declare("UCRTContentRoot", UCRTContentRoot))
                using (fileGenerator.Declare("targetPlatformVersion", targetPlatformVersionString))
                {
                    fileGenerator.Write(_windowsSDKOverridesBegin);

                    // vs2015 specific, we need to set the UniversalCRTSdkDir to $(UniversalCRTSdkDir_10) because it is not done in the .props
                    if (devEnv == DevEnv.vs2015 && !string.Equals(UniversalCRTSdkDir_10, FileGeneratorUtilities.RemoveLineTag, StringComparison.Ordinal))
                    {
                        using (fileGenerator.Declare("custompropertyname", "UniversalCRTSdkDir"))
                        using (fileGenerator.Declare("custompropertyvalue", "$(UniversalCRTSdkDir_10)"))
                        {
                            fileGenerator.Write(fileGenerator.Resolver.Resolve(Vcxproj.Template.Project.CustomProperty));
                        }
                    }
                    fileGenerator.Write(_windowsSDKOverridesEnd);
                }
            }
        }

        public virtual void GenerateProjectPlatformSdkDirectoryDescription(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            bool hasNonFastBuildConfig = context.ProjectConfigurations.Any(c => !c.IsFastBuild);
            if (hasNonFastBuildConfig)
                WriteWindowsKitsOverrides(context, generator);
        }

        public virtual void GeneratePostDefaultPropsImport(IVcxprojGenerationContext context, IFileGenerator generator)
        {
        }

        public virtual void GenerateProjectConfigurationGeneral(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            generator.Write(_projectConfigurationsGeneral);
        }

        public virtual void GenerateProjectConfigurationGeneral2(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            generator.Write(_projectConfigurationsGeneral2);
        }

        public virtual void GenerateProjectConfigurationFastBuildMakeFile(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            generator.Write(_projectConfigurationsFastBuildMakefile);
        }
        public virtual void GenerateProjectConfigurationCustomMakeFile(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            generator.Write(_projectConfigurationsCustomMakefile);
        }

        public virtual void GenerateProjectPlatformImportSheet(IVcxprojGenerationContext context, IFileGenerator generator)
        {
        }

        public virtual void GeneratePlatformResourceFileList(IVcxprojGenerationContext context, IFileGenerator generator, Strings alreadyWrittenPriFiles, IList<Vcxproj.ProjectFile> resourceFiles, IList<Vcxproj.ProjectFile> imageResourceFiles)
        {
        }

        public virtual void GeneratePlatformReferences(IVcxprojGenerationContext context, IFileGenerator generator)
        {
        }

        public virtual void GeneratePlatformSpecificProjectDescription(IVcxprojGenerationContext context, IFileGenerator generator)
        {
        }

        public virtual IEnumerable<Tuple<string, List<Vcxproj.ProjectFile>>> GetPlatformFileLists(IVcxprojGenerationContext context)
        {
            yield break;
        }

        public virtual void SetupPlatformLibraryOptions(out string platformLibExtension, out string platformOutputLibExtension, out string platformPrefixExtension, out string platformLibPrefix)
        {
            platformLibExtension = ".lib";
            platformOutputLibExtension = ".lib";
            platformPrefixExtension = string.Empty;
            platformLibPrefix = string.Empty;
        }

        protected virtual string GetProjectLinkExecutableVcxprojTemplate()
        {
            return GetProjectLinkSharedVcxprojTemplate();
        }

        protected virtual string GetProjectLinkSharedVcxprojTemplate()
        {
            return _projectConfigurationsLinkTemplate;
        }

        protected virtual string GetProjectStaticLinkVcxprojTemplate()
        {
            return _projectConfigurationsStaticLinkTemplate;
        }

        protected IEnumerable<string> EnumerateSemiColonSeparatedString(string str)
        {
            string[] dirs = str.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var dir in dirs)
                yield return dir;
        }

        protected virtual IEnumerable<string> GetIncludePathsImpl(IGenerationContext context)
        {
            var includePaths = new OrderableStrings();
            includePaths.AddRange(context.Configuration.IncludePrivatePaths);
            includePaths.AddRange(context.Configuration.IncludePaths);
            includePaths.AddRange(context.Configuration.DependenciesIncludePaths);
            includePaths.AddRange(context.Configuration.IncludeSystemPaths);
            includePaths.AddRange(context.Configuration.DependenciesIncludeSystemPaths);

            includePaths.Sort();
            return includePaths;
        }

        protected virtual IEnumerable<IncludeWithPrefix> GetPlatformIncludePathsWithPrefixImpl(IGenerationContext context)
        {
            yield break;
        }

        protected virtual IEnumerable<string> GetResourceIncludePathsImpl(IGenerationContext context)
        {
            var resourceIncludePaths = new OrderableStrings();
            resourceIncludePaths.AddRange(context.Configuration.ResourceIncludePrivatePaths);
            resourceIncludePaths.AddRange(context.Configuration.ResourceIncludePaths);
            resourceIncludePaths.AddRange(context.Configuration.DependenciesResourceIncludePaths);

            return resourceIncludePaths;
        }

        protected virtual IEnumerable<string> GetAssemblyIncludePathsImpl(IGenerationContext context)
        {
            yield break;
        }

        #endregion
    }
}
