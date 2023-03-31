// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public static partial class Android
    {
        public sealed partial class AndroidAgdePlatform
        {
            public const string _linkerOptionsTemplate = @"
    .LinkerOptions          = '-o ""%2"" ""%1""'
                            // System options
                            // -------------------------
                            + ' [cmdLineOptions.ClangCompilerTarget]'
                            + ' [cmdLineOptions.GenerateSharedObject]'
                            + ' [cmdLineOptions.UseOfStl]'
                            // 
                            // Library Search Path
                            // ---------------------------
                            + ' [cmdLineOptions.AdditionalLibraryDirectories]'
                            // Libraries
                            // ---------------------------
                            + ' [cmdLineOptions.AdditionalDependencies]'
                            // Options
                            //--------
                            + ' [cmdLineOptions.ClangLinkType]'
                            + ' [cmdLineOptions.DebuggerSymbolInformation]'
                            + ' [cmdLineOptions.GenerateMapFile]'
                            + ' [cmdLineOptions.LinkIncremental]'
                            + ' [cmdLineOptions.FunctionBinding]'
                            + ' [cmdLineOptions.NoExecStackRequired]'
                            + ' [cmdLineOptions.Relocation]'
                            + ' [cmdLineOptions.UnresolvedSymbolReferences]'
                            + ' [cmdLineOptions.BuildId]'
                            // Additional linker options
                            //--------------------------
                            + ' [options.AdditionalLinkerOptions]'
";

            public const string _compilerExtraOptionsTemplate = @"
    .CompilerExtraOptions   = ''
            // System options
            // -------------------------
            + ' [cmdLineOptions.ClangDiagnosticsFormat]'
            + ' [cmdLineOptions.ClangCompilerTarget]'
            + ' [cmdLineOptions.LimitDebugInfo]'
            + ' [cmdLineOptions.ClangDebugInformationFormat]'
            + ' [cmdLineOptions.FloatABI]'
            + ' [cmdLineOptions.AddressSignificanceTable]'
            // General options
            // -------------------------
            + ' [cmdLineOptions.AdditionalIncludeDirectories]'
            + ' [cmdLineOptions.AdditionalUsingDirectories]'
            + ' [cmdLineOptions.PreprocessorDefinitions]'
            + ' [cmdLineOptions.UndefinePreprocessorDefinitions]'
            + ' [cmdLineOptions.UndefineAllPreprocessorDefinitions]'
            + ' [cmdLineOptions.WarningLevel]'
            + ' [cmdLineOptions.TreatWarningAsError]'
            + ' [cmdLineOptions.StackProtectionLevel]'
            + ' [cmdLineOptions.EnableDataLevelLinking]'
            + ' [cmdLineOptions.EnableFunctionLevelLinking]'
            + ' [cmdLineOptions.OmitFramePointers]'
            + ' [cmdLineOptions.LinkTimeOptimization]'
            + ' [cmdLineOptions.RuntimeTypeInfo]'
            + ' [cmdLineOptions.ExceptionHandling]'
            + ' [cmdLineOptions.UnwindTables]'
            + ' [cmdLineOptions.CppLanguageStd]'
            + ' [cmdLineOptions.CLanguageStd]'
            + ' [cmdLineOptions.ThumbMode]'
            + ' [cmdLineOptions.PositionIndependentCode]'
            // Additional compiler options
            //--------------------------
            + ' [options.AdditionalCompilerOptions]'
";

            public const string _compilerOptimizationOptionsTemplate =
                    @"
    // Optimizations options
    // ---------------------
    .CompilerOptimizations = ''
            + ' [cmdLineOptions.Optimization]'
            + ' [options.AdditionalCompilerOptimizeOptions]'
";
        }
    }
}
