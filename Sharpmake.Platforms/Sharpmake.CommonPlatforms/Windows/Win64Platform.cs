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
    public static partial class Windows
    {
        [PlatformImplementation(Platform.win64,
            typeof(IPlatformDescriptor),
            typeof(IFastBuildCompilerSettings),
            typeof(IWindowsFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IMicrosoftPlatformBff),
            typeof(IPlatformVcxproj))]
        public sealed class Win64Platform : BaseWindowsPlatform
        {
            #region IPlatformDescriptor implementation
            public override string SimplePlatformString => "x64";

            public override EnvironmentVariableResolver GetPlatformEnvironmentResolver(params VariableAssignment[] assignments)
            {
                return new Win64EnvironmentVariableResolver(assignments);
            }
            #endregion

            #region IMicrosoftPlatformBff implementation
            public override string BffPlatformDefine => "WIN64";
            public override string CConfigName => ".win64Config";
            public override bool SupportsResourceFiles => true;

            public override void AddCompilerSettings(
                IDictionary<string, CompilerSettings> masterCompilerSettings,
                string compilerName,
                string rootPath,
                DevEnv devEnv,
                string projectRootPath
            )
            {
                var fastBuildCompilerSettings = PlatformRegistry.Get<IWindowsFastBuildCompilerSettings>(Platform.win64);
                if (!fastBuildCompilerSettings.BinPath.ContainsKey(devEnv))
                    fastBuildCompilerSettings.BinPath.Add(devEnv, devEnv.GetVisualStudioBinPath(Platform.win64));
                if (!fastBuildCompilerSettings.LinkerPath.ContainsKey(devEnv))
                    fastBuildCompilerSettings.LinkerPath.Add(devEnv, fastBuildCompilerSettings.BinPath[devEnv]);
                if (!fastBuildCompilerSettings.ResCompiler.ContainsKey(devEnv))
                    fastBuildCompilerSettings.ResCompiler.Add(devEnv, devEnv.GetWindowsResourceCompiler(Platform.win64));

                CompilerSettings compilerSettings = GetMasterCompilerSettings(masterCompilerSettings, compilerName, rootPath, devEnv, projectRootPath, false);
                compilerSettings.PlatformFlags |= Platform.win64;
                SetConfiguration(compilerSettings.Configurations, string.Empty, projectRootPath, devEnv, false);
            }

            public override CompilerSettings GetMasterCompilerSettings(
                IDictionary<string, CompilerSettings> masterCompilerSettings,
                string compilerName,
                string rootPath,
                DevEnv devEnv,
                string projectRootPath,
                bool useCCompiler
            )
            {
                CompilerSettings compilerSettings;

                if (masterCompilerSettings.ContainsKey(compilerName))
                {
                    compilerSettings = masterCompilerSettings[compilerName];
                }
                else
                {
                    string pathToCompiler = devEnv.GetVisualStudioBinPath(Platform.win64).ReplaceHeadPath(rootPath, "$RootPath$");

                    Strings extraFiles = new Strings();

                    extraFiles.Add(
                        Path.Combine(pathToCompiler, "c1.dll"),
                        Path.Combine(pathToCompiler, "c1xx.dll"),
                        Path.Combine(pathToCompiler, "c2.dll"),
                        Path.Combine(pathToCompiler, "mspdbcore.dll"),
                        Path.Combine(pathToCompiler, "mspdbsrv.exe"),
                        Path.Combine(pathToCompiler, @"1033\clui.dll")
                    );

                    switch (devEnv)
                    {
                        case DevEnv.vs2012:
                            {
                                extraFiles.Add(
                                    Path.Combine(pathToCompiler, "c1ast.dll"),
                                    Path.Combine(pathToCompiler, "c1xxast.dll"),
                                    Path.Combine(pathToCompiler, "mspft110.dll"),
                                    Path.Combine(pathToCompiler, "msobj110.dll"),
                                    Path.Combine(pathToCompiler, "mspdb110.dll"),
                                    @"$RootPath$\redist\x64\Microsoft.VC110.CRT\msvcp110.dll",
                                    @"$RootPath$\redist\x64\Microsoft.VC110.CRT\msvcr110.dll",
                                    @"$RootPath$\redist\x64\Microsoft.VC110.CRT\vccorlib110.dll"
                                );
                            }
                            break;
                        case DevEnv.vs2013:
                            {
                                extraFiles.Add(
                                    Path.Combine(pathToCompiler, "c1ast.dll"),
                                    Path.Combine(pathToCompiler, "c1xxast.dll"),
                                    Path.Combine(pathToCompiler, "mspft120.dll"),
                                    Path.Combine(pathToCompiler, "msobj120.dll"),
                                    Path.Combine(pathToCompiler, "mspdb120.dll"),
                                    @"$RootPath$\redist\x64\Microsoft.VC120.CRT\msvcp120.dll",
                                    @"$RootPath$\redist\x64\Microsoft.VC120.CRT\msvcr120.dll",
                                    @"$RootPath$\redist\x64\Microsoft.VC120.CRT\vccorlib120.dll"
                                );
                            }
                            break;
                        case DevEnv.vs2015:
                        case DevEnv.vs2017:
                            {
                                string systemDllPath = FastBuildSettings.SystemDllRoot;
                                if (systemDllPath == null)
                                    systemDllPath = KitsRootPaths.GetRoot(KitsRootEnum.KitsRoot10) + @"Redist\ucrt\DLLs\x64\";

                                if (!Path.IsPathRooted(systemDllPath))
                                    systemDllPath = Util.SimplifyPath(Path.Combine(projectRootPath, systemDllPath));

                                extraFiles.Add(
                                    Path.Combine(pathToCompiler, "msobj140.dll"),
                                    Path.Combine(pathToCompiler, "mspft140.dll"),
                                    Path.Combine(pathToCompiler, "mspdb140.dll")
                                );

                                if (devEnv == DevEnv.vs2015)
                                {
                                    extraFiles.Add(

                                        Path.Combine(pathToCompiler, "vcvars64.bat"),
                                        @"$RootPath$\redist\x64\Microsoft.VC140.CRT\concrt140.dll",
                                        @"$RootPath$\redist\x64\Microsoft.VC140.CRT\msvcp140.dll",
                                        @"$RootPath$\redist\x64\Microsoft.VC140.CRT\vccorlib140.dll",
                                        @"$RootPath$\redist\x64\Microsoft.VC140.CRT\vcruntime140.dll",
                                        Path.Combine(systemDllPath, "ucrtbase.dll")
                                    );
                                }
                                else
                                {
                                    extraFiles.Add(
                                        Path.Combine(pathToCompiler, "mspdbcore.dll"),
                                        Path.Combine(pathToCompiler, "msvcdis140.dll"),
                                        Path.Combine(pathToCompiler, "msvcp140.dll"),
                                        Path.Combine(pathToCompiler, "pgodb140.dll"),
                                        Path.Combine(pathToCompiler, "vcruntime140.dll"),
                                        @"$RootPath$\Auxiliary\Build\vcvars64.bat"
                                    );
                                }

                                try
                                {
                                    foreach (string p in Directory.EnumerateFiles(systemDllPath, "api-ms-win-*.dll"))
                                        extraFiles.Add(p);
                                }
                                catch { }
                            }
                            break;
                        default:
                            throw new NotImplementedException("This devEnv (" + devEnv + ") is not supported!");
                    }

                    string executable = Path.Combine(pathToCompiler, "cl.exe");

                    compilerSettings = new CompilerSettings(compilerName, Platform.win64, extraFiles, executable, rootPath, devEnv, new Dictionary<string, CompilerSettings.Configuration>());
                    masterCompilerSettings.Add(compilerName, compilerSettings);
                }

                return compilerSettings;
            }

            public override void SetConfiguration(IDictionary<string, CompilerSettings.Configuration> configurations, string compilerName, string projectRootPath, DevEnv devEnv, bool useCCompiler)
            {
                string configName = ".win64Config";

                if (!configurations.ContainsKey(configName))
                {
                    var fastBuildCompilerSettings = PlatformRegistry.Get<IWindowsFastBuildCompilerSettings>(Platform.win64);
                    string binPath = fastBuildCompilerSettings.BinPath[devEnv];
                    string linkerPath = fastBuildCompilerSettings.LinkerPath[devEnv];
                    string resCompiler = fastBuildCompilerSettings.ResCompiler[devEnv];
                    configurations.Add(configName,
                        new CompilerSettings.Configuration(
                            Platform.win64,
                            binPath: Util.GetCapitalizedPath(Util.PathGetAbsolute(projectRootPath, binPath)),
                            linkerPath: Util.GetCapitalizedPath(Util.PathGetAbsolute(projectRootPath, linkerPath)),
                            resourceCompiler: Util.GetCapitalizedPath(Util.PathGetAbsolute(projectRootPath, resCompiler)),
                            librarian: @"$LinkerPath$\lib.exe",
                            linker: @"$LinkerPath$\link.exe"
                        )
                    );

                    configurations.Add(".win64ConfigMasm", new CompilerSettings.Configuration(Platform.win64, compiler: @"$BinPath$\ml64.exe", usingOtherConfiguration: @".win64Config"));
                }
            }
            #endregion

            #region IPlatformVcxproj implementation
            public override bool HasEditAndContinueDebuggingSupport => true;
            public override IEnumerable<string> GetImplicitlyDefinedSymbols(IGenerationContext context)
            {
                var defines = new List<string>();
                defines.AddRange(base.GetImplicitlyDefinedSymbols(context));
                defines.Add("WIN64");

                return defines;
            }

            public override void SetupPlatformTargetOptions(IGenerationContext context)
            {
                context.Options["TargetMachine"] = "MachineX64";
                context.CommandLineOptions["TargetMachine"] = "/MACHINE:X64";
            }

            protected override IEnumerable<string> GetPlatformIncludePathsImpl(IGenerationContext context)
            {
                return EnumerateSemiColonSeparatedString(context.DevelopmentEnvironment.GetWindowsIncludePath());
            }
            #endregion
        }
    }
}
