// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public abstract partial class BaseMicrosoftPlatform
        : BasePlatform
        , Project.Configuration.IConfigurationTasks
        , IMicrosoftPlatformBff
        , IPlatformVcxproj
    {
        #region IPlatformDescriptor implementation
        public override bool IsMicrosoftPlatform => true;
        public override bool IsUsingClang => false;
        public override bool IsLinkerInvokedViaCompiler { get; set; } = false;
        public override bool HasDotNetSupport => true;
        public override bool HasSharedLibrarySupport => true;
        #endregion

        #region Project.Configuration.IConfigurationTasks implementation
        public void SetupDynamicLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            if (dependency.Project.SharpmakeProjectType != Project.ProjectTypeAttribute.Export &&
                !(configuration.IsFastBuild && !dependency.IsFastBuild))
            {
                if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                    configuration.AddDependencyBuiltTargetLibraryPath(dependency.TargetLibraryPath, dependency.TargetLibraryPathOrderNumber);
                if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                    configuration.AddDependencyBuiltTargetLibraryFile(dependency.TargetFileFullName + StaticLibraryFileFullExtension, dependency.TargetFileOrderNumber);
            }
            else
            {
                if (dependencySetting.HasFlag(DependencySetting.LibraryPaths))
                    configuration.DependenciesOtherLibraryPaths.Add(dependency.TargetLibraryPath, dependency.TargetLibraryPathOrderNumber);
                if (dependencySetting.HasFlag(DependencySetting.LibraryFiles))
                    configuration.DependenciesOtherLibraryFiles.Add(dependency.TargetFileFullName + StaticLibraryFileFullExtension, dependency.TargetFileOrderNumber);
            }
        }

        public void SetupStaticLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
        }

        // The below method was replaced by GetDefaultOutputFullExtension
        // string GetDefaultOutputExtension(OutputType outputType);

        public string GetDefaultOutputFullExtension(Project.Configuration.OutputType outputType)
        {
            switch (outputType)
            {
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.DotNetConsoleApp:
                case Project.Configuration.OutputType.DotNetWindowsApp:
                    return ExecutableFileFullExtension;
                case Project.Configuration.OutputType.Lib:
                    return StaticLibraryFileFullExtension;
                case Project.Configuration.OutputType.Dll:
                case Project.Configuration.OutputType.DotNetClassLibrary:
                    return ".dll";
                case Project.Configuration.OutputType.None:
                case Project.Configuration.OutputType.Utility:
                    return string.Empty;
                default:
                    throw new NotImplementedException("Please add extension for output type " + outputType);
            }
        }

        public string GetOutputFileNamePrefix(Project.Configuration.OutputType outputType)
        {
            return string.Empty;
        }

        public virtual IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
        {
            var dirs = new List<string>();
            var hasDotNetDependency = Util.IsDotNet(configuration) || configuration.ResolvedSourceFilesWithCompileAsCLROption.Count > 0;
            var dotnet = hasDotNetDependency ? configuration.Target.GetFragment<DotNetFramework>() : default(DotNetFramework?);

            var platformToolset = Options.GetObject<Options.Vc.General.PlatformToolset>(configuration);
            if (platformToolset.IsLLVMToolchain())
            {
                Options.Vc.General.PlatformToolset overridenPlatformToolset = Options.Vc.General.PlatformToolset.Default;
                if (Options.WithArgOption<Options.Vc.General.PlatformToolset>.Get<Options.Clang.Compiler.LLVMVcPlatformToolset>(configuration, ref overridenPlatformToolset))
                    platformToolset = overridenPlatformToolset;
            }

            var devEnv = platformToolset.GetDefaultDevEnvForToolset() ?? configuration.Target.GetFragment<DevEnv>();

            string platformDirsStr = devEnv.GetWindowsLibraryPath(configuration.Target.GetPlatform(), dotnet);
            dirs.AddRange(EnumerateSemiColonSeparatedString(platformDirsStr));

            return dirs;
        }
        #endregion

        #region IMicrosoftPlatformBff implementation
        public virtual bool SupportsResourceFiles => false;

        protected void GetLinkerExecutableInfo(Project.Configuration conf, out string linkerPathOverride, out string linkerExeOverride, out string librarianExeOverride)
        {
            linkerPathOverride = null;
            linkerExeOverride = null;
            librarianExeOverride = null;

            var platformToolset = Options.GetObject<Options.Vc.General.PlatformToolset>(conf);
            var useLldLink = Options.GetObject<Options.Vc.LLVM.UseLldLink>(conf);
            if (useLldLink == Options.Vc.LLVM.UseLldLink.Enable ||
               (useLldLink == Options.Vc.LLVM.UseLldLink.Default && platformToolset.IsLLVMToolchain()))
            {
                linkerPathOverride = platformToolset == Options.Vc.General.PlatformToolset.ClangCL ? ClangForWindows.GetWindowsClangExecutablePath(conf.Target.GetFragment<DevEnv>()) : ClangForWindows.GetWindowsClangExecutablePath();
                linkerExeOverride = "lld-link.exe";
                librarianExeOverride = "llvm-lib.exe";
            }
        }
        #endregion

        #region IPlatformVcxproj implementation
        public override string ExecutableFileFullExtension => ".exe";
        public override string SharedLibraryFileFullExtension => ".lib";
        public override string ProgramDatabaseFileFullExtension => ".pdb";
        public override string StaticLibraryFileFullExtension => ".lib";
        #endregion

        public enum RuntimeLibrary
        {
            Static,
            Dynamic
        }

        public static IEnumerable<string> GetUCRTLibs(RuntimeLibrary runtime, bool debugVersion)
        {
            // cf. https://blogs.msdn.microsoft.com/vcblog/2015/03/03/introducing-the-universal-crt
            string suffix = debugVersion ? "d" : "";
            if (runtime == RuntimeLibrary.Static)
            {
                yield return "libcmt" + suffix;
                yield return "libvcruntime" + suffix;
                yield return "libucrt" + suffix;
            }
            else if (runtime == RuntimeLibrary.Dynamic)
            {
                yield return "msvcrt" + suffix;
                yield return "vcruntime" + suffix; // associated DLL is vcruntime<version><suffix>.dll
                yield return "ucrt" + suffix; // associated DLL is ucrtbase<suffix>.dll
            }
            else
                throw new NotImplementedException("Unsupported runtime library value " + runtime);
        }

        protected override IEnumerable<string> GetAssemblyIncludePathsImpl(IGenerationContext context)
        {
            var assemblyIncludePaths = new OrderableStrings();
            assemblyIncludePaths.AddRange(context.Configuration.AssemblyIncludePaths);

            if (assemblyIncludePaths.Count() > 10)
                throw new Error("{0} project configuration contains more than 10 Assembly include paths, in total: {1}", context.Configuration, assemblyIncludePaths.Count());

            return assemblyIncludePaths;
        }

        public override void GenerateProjectMasmVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            generator.Write(_projectConfigurationsMasmTemplate);
        }
    }
}
