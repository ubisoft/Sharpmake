// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Sharpmake.Generators
{
    public class CompilerSettings
    {
        public string CompilerName { get; private set; }
        public CompilerFamily FastBuildCompilerFamily { get; private set; }
        public Platform PlatformFlags { get; set; } // TODO: Remove the public setter.
        public Strings ExtraFiles { get; private set; }
        public string Executable { get; private set; }
        public string RootPath { get; private set; }
        public DevEnv DevEnv { get; set; }
        public IDictionary<string, Configuration> Configurations { get; private set; }

        public CompilerSettings(
            string compilerName,
            CompilerFamily compilerFamily,
            Platform platform,
            Strings extraFiles,
            string executable,
            string rootPath,
            DevEnv devEnv,
            IDictionary<string, Configuration> configurations
        )
        {
            CompilerName = compilerName;
            FastBuildCompilerFamily = compilerFamily;
            PlatformFlags = platform;
            ExtraFiles = extraFiles;
            Executable = executable;
            RootPath = rootPath;
            DevEnv = devEnv;
            Configurations = configurations;
        }

        public enum LinkerType
        {
            Auto,
            MSVC,
            GCC,
            SNCPS3,
            ClangOrbis,
            GreenHillsExlr,
            CodeWarriorLd
        }

        public class Configuration
        {
            public string BinPath { get; set; }
            public string LinkerPath { get; set; }
            public string ResourceCompiler { get; set; }
            public string EmbeddedResourceCompiler { get; set; }
            public string Compiler { get; set; }
            public string Librarian { get; set; }
            public string Linker { get; set; }
            public string PlatformLibPaths { get; set; }
            public string Masm { get; set; }
            public string Nasm { get; set; }
            public string Executable { get; set; }
            public string UsingOtherConfiguration { get; set; }
            public Platform Platform { get; private set; }
            public LinkerType FastBuildLinkerType { get; set; }

            public Configuration(
                Platform platform,
                string binPath = FileGeneratorUtilities.RemoveLineTag,
                string linkerPath = FileGeneratorUtilities.RemoveLineTag,
                string resourceCompiler = FileGeneratorUtilities.RemoveLineTag,
                string embeddedResourceCompiler = FileGeneratorUtilities.RemoveLineTag,
                string compiler = FileGeneratorUtilities.RemoveLineTag,
                string librarian = FileGeneratorUtilities.RemoveLineTag,
                string linker = FileGeneratorUtilities.RemoveLineTag,
                string executable = FileGeneratorUtilities.RemoveLineTag,
                string usingOtherConfiguration = FileGeneratorUtilities.RemoveLineTag,
                LinkerType fastBuildLinkerType = LinkerType.Auto
            )
            {
                BinPath = binPath;
                LinkerPath = linkerPath;
                ResourceCompiler = resourceCompiler;
                EmbeddedResourceCompiler = embeddedResourceCompiler;
                Compiler = compiler;
                Librarian = librarian;
                Linker = linker;
                Executable = executable;
                UsingOtherConfiguration = usingOtherConfiguration;
                Platform = platform;
                FastBuildLinkerType = fastBuildLinkerType;
            }
        }
    }
}
