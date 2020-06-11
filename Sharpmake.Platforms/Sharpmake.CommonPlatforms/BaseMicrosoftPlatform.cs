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
using System.IO;
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
        public override bool HasDotNetSupport => true;
        public override bool HasSharedLibrarySupport => true;
        #endregion

        #region Project.Configuration.IConfigurationTasks implementation
        public void SetupDynamicLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
        }

        public void SetupStaticLibraryPaths(Project.Configuration configuration, DependencySetting dependencySetting, Project.Configuration dependency)
        {
            DefaultPlatform.SetupLibraryPaths(configuration, dependencySetting, dependency);
        }

        public string GetDefaultOutputExtension(Project.Configuration.OutputType outputType)
        {
            switch (outputType)
            {
                case Project.Configuration.OutputType.Exe:
                case Project.Configuration.OutputType.DotNetConsoleApp:
                case Project.Configuration.OutputType.DotNetWindowsApp:
                    return "exe";
                case Project.Configuration.OutputType.Lib:
                    return "lib";
                case Project.Configuration.OutputType.Dll:
                case Project.Configuration.OutputType.DotNetClassLibrary:
                    return "dll";
                case Project.Configuration.OutputType.None:
                    return string.Empty;
                default:
                    return outputType.ToString().ToLower();
            }
        }

        public virtual IEnumerable<string> GetPlatformLibraryPaths(Project.Configuration configuration)
        {
            var dirs = new List<string>();
            var dotnet = Util.IsDotNet(configuration) ? configuration.Target.GetFragment<DotNetFramework>() : default(DotNetFramework?);

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
               (useLldLink == Options.Vc.LLVM.UseLldLink.Default && platformToolset == Options.Vc.General.PlatformToolset.LLVM))
            {
                linkerPathOverride = Path.Combine(ClangForWindows.Settings.LLVMInstallDir, "bin");
                linkerExeOverride = "lld-link.exe";
                librarianExeOverride = "llvm-lib.exe";
            }
        }
        #endregion

        #region IPlatformVcxproj implementation
        public override string ExecutableFileExtension => "exe";
        public override string SharedLibraryFileExtension => "lib";
        public override string ProgramDatabaseFileExtension => "pdb";
        public override string StaticLibraryFileExtension => "lib";
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

        public override void GenerateProjectMasmVcxproj(IVcxprojGenerationContext context, IFileGenerator generator)
        {
            generator.Write(_projectConfigurationsMasmTemplate);
        }
    }
}
