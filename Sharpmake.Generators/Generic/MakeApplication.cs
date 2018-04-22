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
using System.Linq;

namespace Sharpmake.Generators.Generic
{
    public partial class MakeApplication : ISolutionGenerator
    {
        private Builder _builder;
        private string _solutionExtension = ".mk";

        public void Generate(Builder builder, Solution solution, List<Solution.Configuration> configurations, string solutionFile, List<string> generatedFiles, List<string> skipFiles)
        {
            _builder = builder;

            FileInfo fileInfo = new FileInfo(solutionFile);
            string solutionPath = fileInfo.Directory.FullName;
            string projectMainMakefileFileName = "Android";

            bool updated;
            string projectMainFileResult = GenerateProjectMainMakefile(solution, configurations, solutionPath, projectMainMakefileFileName, out updated);
            if (updated)
                generatedFiles.Add(projectMainFileResult);
            else
                skipFiles.Add(projectMainFileResult);

            string solutionFileName = "Application";
            string applicationFileResult = GenerateApplicationMakefile(solution, configurations, solutionPath, solutionFileName, out updated);
            if (updated)
                generatedFiles.Add(applicationFileResult);
            else
                skipFiles.Add(applicationFileResult);

            _builder = null;
        }

        private string GenerateApplicationMakefile(Solution solution, List<Solution.Configuration> configurations, string solutionPath, string solutionFile, out bool updated)
        {
            // Create the target folder.
            string solutionFolder = Util.GetCapitalizedPath(solutionPath);
            Directory.CreateDirectory(solutionFolder);

            // Main solution file. 
            string solutionFileContentsPath = solutionFolder + Path.DirectorySeparatorChar + solutionFile + _solutionExtension;
            FileInfo solutionFileContentsInfo = new FileInfo(solutionFileContentsPath);

            // Write the makefile in a file in memory as to not overwrite if no changes detected.
            var fileGenerator = new FileGenerator();
            using (fileGenerator.Declare("item", new ApplicationSettings(configurations)))
            {
                fileGenerator.Write(Template.ApplicationContent);
            }

            // Write the solution file
            updated = _builder.Context.WriteGeneratedFile(solution.GetType(), solutionFileContentsInfo, fileGenerator.ToMemoryStream());

            return solutionFileContentsInfo.FullName;
        }

        private string GenerateProjectMainMakefile(Solution solution, List<Solution.Configuration> configurations, string solutionPath, string solutionFile, out bool updated)
        {
            // Create the target folder.
            string solutionFolder = Util.GetCapitalizedPath(solutionPath);
            Directory.CreateDirectory(solutionFolder);

            // Main solution file. 
            string solutionFileContentsPath = solutionFolder + Path.DirectorySeparatorChar + solutionFile + _solutionExtension;
            FileInfo solutionFileContentsInfo = new FileInfo(solutionFileContentsPath);

            // Write the makefile in a file in memory as to not overwrite if no changes detected.
            var fileGenerator = new FileGenerator();
            fileGenerator.Write(Template.MainProjectContent);

            // Write the solution file
            updated = _builder.Context.WriteGeneratedFile(solution.GetType(), solutionFileContentsInfo, fileGenerator.ToMemoryStream());

            return solutionFileContentsInfo.FullName;
        }

        private class ApplicationSettings
        {
            private const string DefaultAppPlatform = "android-10";
            private const string DefaultAbi = "armeabi-v7a";
            private const string DefaultToolchainVersion = "4.8";

            private string _appPlatform;
            private string _abi;
            private string _stl;
            private string _cFlagsDebug;
            private string _cFlagsRelease;
            private readonly string _toolchainVersion;

            public ApplicationSettings(List<Solution.Configuration> configurations)
            {
                Solution.Configuration configurationDebug = configurations.FirstOrDefault(conf => conf.Target.GetFragment<Optimization>() == Optimization.Debug);
                Solution.Configuration configurationRelease = configurations.FirstOrDefault(conf => conf.Target.GetFragment<Optimization>() == Optimization.Release);
                Solution.Configuration configurationFinal = configurations.FirstOrDefault(conf => conf.Target.GetFragment<Optimization>() == Optimization.Retail);

                if (configurationDebug == null || configurationRelease == null || configurationFinal == null)
                    throw new Error("Android makefiles require a debug, release and final configuration. ");

                _appPlatform = Options.GetString<Options.AndroidMakefile.AppPlatform>(configurationDebug);
                if (String.IsNullOrEmpty(_appPlatform))
                    _appPlatform = DefaultAppPlatform;

                Strings abis = Options.GetStrings<Options.AndroidMakefile.SupportedABIs>(configurationDebug);
                _abi = abis.DefaultIfEmpty(DefaultAbi).Aggregate((first, next) => first + " " + next);

                Options.SelectOption(configurationDebug,
                    Options.Option(Options.AndroidMakefile.StandardLibrary.System, () => _stl = "system"),
                    Options.Option(Options.AndroidMakefile.StandardLibrary.GAbiPP_Static, () => _stl = "gabi++_static"),
                    Options.Option(Options.AndroidMakefile.StandardLibrary.GAbiPP_Shared, () => _stl = "gabi++_shared"),
                    Options.Option(Options.AndroidMakefile.StandardLibrary.StlPort_Static, () => _stl = "stlport_static"),
                    Options.Option(Options.AndroidMakefile.StandardLibrary.StlPort_Shared, () => _stl = "stlport_shared"),
                    Options.Option(Options.AndroidMakefile.StandardLibrary.GnuStl_Static, () => _stl = "gnustl_static"),
                    Options.Option(Options.AndroidMakefile.StandardLibrary.GnuStl_Shared, () => _stl = "gnustl_shared")
                );

                Strings compilerFlagsDebug = Options.GetStrings<Options.AndroidMakefile.CompilerFlags>(configurationDebug);
                Strings compilerFlagsRelease = Options.GetStrings<Options.AndroidMakefile.CompilerFlags>(configurationRelease);

                _cFlagsDebug = compilerFlagsDebug.DefaultIfEmpty("").Aggregate((first, next) => first + " " + next);
                _cFlagsRelease = compilerFlagsRelease.DefaultIfEmpty("").Aggregate((first, next) => first + " " + next);

                _toolchainVersion = Options.GetString<Options.AndroidMakefile.ToolchainVersion>(configurationDebug);
                if (String.IsNullOrEmpty(_toolchainVersion))
                    _toolchainVersion = DefaultToolchainVersion;
            }

            public string AppPlatform { get { return _appPlatform; } }
            public string Abi { get { return _abi; } }
            public string Stl { get { return _stl; } }
            public string CFlagsDebug { get { return _cFlagsDebug; } }
            public string CFlagsRelease { get { return _cFlagsRelease; } }
            public string ToolchainVersion { get { return _toolchainVersion; } }
        }
    }
}
