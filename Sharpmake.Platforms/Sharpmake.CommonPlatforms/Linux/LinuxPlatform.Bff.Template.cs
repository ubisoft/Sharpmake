// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public static partial class Linux
    {
        public sealed partial class LinuxPlatform
        {
            public const string _linkerOptionsTemplate = @"
    .LinkerOptions          = '-o ""%2""'
                            + ' [cmdLineOptions.WholeArchiveBegin]'
                            + ' ""%1""'
                            + ' [cmdLineOptions.WholeArchiveEnd]'
                            + ' [sharedOption]'
                            + ' [cmdLineOptions.BffSysRoot]'
                            // Library Search Path
                            // ---------------------------
                            + ' [cmdLineOptions.AdditionalLibraryDirectories]'
                            // Libraries
                            // ---------------------------
                            + ' [cmdLineOptions.AdditionalDependencies]'
                            // Options
                            //--------
                            + ' [cmdLineOptions.StdLib]'
                            + ' [cmdLineOptions.EditAndContinue]'
                            + ' [cmdLineOptions.InfoStripping]'
                            + ' [cmdLineOptions.DataStripping]'
                            + ' [cmdLineOptions.DuplicateStripping]'
                            + ' [cmdLineOptions.Addressing]'
                            + ' [cmdLineOptions.GenerateMapFile]'
                            // Additional linker options
                            //--------------------------
                            + ' [options.AdditionalLinkerOptions]'
";

            public const string _compilerExtraOptions = @"
    .CompilerExtraOptions   = ''
            // General options
            // -------------------------
            + ' [cmdLineOptions.StdLib]'
            + ' [cmdLineOptions.DebugInformationFormat]'
            + ' [cmdLineOptions.AdditionalIncludeDirectories]'
            + ' [cmdLineOptions.AdditionalUsingDirectories]'
            + ' [cmdLineOptions.PreprocessorDefinitions]'
            + ' [cmdLineOptions.UndefinePreprocessorDefinitions]'
            + ' [cmdLineOptions.UndefineAllPreprocessorDefinitions]'
            + ' [cmdLineOptions.Warnings]'
            + ' [cmdLineOptions.ExtraWarnings]'
            + ' [cmdLineOptions.WarningsAsErrors]'
            + ' [cmdLineOptions.PositionIndependentCode]'
            + ' [cmdLineOptions.FastMath]'
            + ' [cmdLineOptions.NoStrictAliasing]'
            + ' [cmdLineOptions.UnrollLoops]'
            + ' [cmdLineOptions.LinkTimeOptimization]'
            + ' [cmdLineOptions.AnsiCompliance]'
            + ' [cmdLineOptions.RuntimeTypeInfo]'
            + ' [cmdLineOptions.CharUnsigned]'
            + ' [cmdLineOptions.MsExtensions]'
            + ' [cmdLineOptions.CppLanguageStd]'
            + ' [cmdLineOptions.CLanguageStd]'
            + ' [cmdLineOptions.BffSysRoot]'
            // Additional compiler options
            //--------------------------
            + ' [options.AdditionalCompilerOptions]'
";

            public const string _compilerOptimizationOptions =
                @"
    // Optimizations options
    // ---------------------
    .CompilerOptimizations = ''
            + ' [cmdLineOptions.OptimizationLevel]'
            + ' [options.AdditionalCompilerOptimizeOptions]'
";
        }
    }
}
