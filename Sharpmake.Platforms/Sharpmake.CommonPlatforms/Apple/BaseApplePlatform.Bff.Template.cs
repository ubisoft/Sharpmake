// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public abstract partial class BaseApplePlatform
    {
        private const string _linkerOptionsTemplate = @"
    .LinkerOptions          = '-o ""%2"" ""%1""[outputTypeArgument]'
                            // Library Search Path
                            // ---------------------------
                            + ' [cmdLineOptions.SysLibRoot]'
                            + ' [cmdLineOptions.AdditionalLibraryDirectories]'
                            // Libraries
                            // ---------------------------
                            + ' [cmdLineOptions.AdditionalDependencies]'
                            // SystemFrameworks, DeveloperFrameworks, UserFrameworks and FrameworkPaths
                            // -----------------------------------------------------------------------------
                            + ' [cmdLineOptions.SystemFrameworks]'
                            + ' [cmdLineOptions.DeveloperFrameworks]'
                            + ' [cmdLineOptions.UserFrameworks]'
                            + ' [cmdLineOptions.EmbeddedFrameworks]'
                            + ' [cmdLineOptions.LinkerSystemFrameworkPaths]'
                            + ' [cmdLineOptions.LinkerFrameworkPaths]'
                            // Options
                            //--------
                            + ' [cmdLineOptions.DeploymentTarget]'
                            + ' [cmdLineOptions.GenerateMapFile]'
                            + ' [cmdLineOptions.DeadCodeStripping]'
                            // Additional linker options
                            //--------------------------
                            + ' [options.AdditionalLinkerOptions]'
                            + ' [cmdLineOptions.DyLibInstallName]'
";

        private const string _compilerExtraOptionsGeneral = @"
    .CompilerExtraOptions   = ''
            // General options
            // -------------------------
            + ' [cmdLineOptions.GenerateDebuggingSymbols]'
            + ' [cmdLineOptions.AdditionalIncludeDirectories]'
            + ' [cmdLineOptions.AdditionalUsingDirectories]'
            + ' [cmdLineOptions.PreprocessorDefinitions]'
            + ' [cmdLineOptions.StdLib]'
            + ' [cmdLineOptions.SDKRoot]'
            + ' [cmdLineOptions.DeploymentTarget]'
            + ' [cmdLineOptions.CppLanguageStd]'
            + ' [cmdLineOptions.CLanguageStd]'
            + ' [cmdLineOptions.WarningReturnType]'
            + ' [cmdLineOptions.RuntimeTypeInfo]'
            + ' [cmdLineOptions.ClangEnableObjC_ARC]'
            + ' [cmdLineOptions.ClangEnableObjC_Weak]'
            + ' [cmdLineOptions.CppExceptions]'
            + ' [cmdLineOptions.ObjCExceptions]'
            + ' [cmdLineOptions.ObjCARCExceptions]'
            + ' [cmdLineOptions.DisableExceptions]'
            + ' [cmdLineOptions.PrivateInlines]'
";

        private const string _compilerExtraOptionsAdditional = @"
            // Additional compiler options
            //--------------------------
            + ' [options.AdditionalCompilerOptions]'
            // FrameworkPaths
            // ----------------------------------------------------------------------------
            + ' [cmdLineOptions.CompilerSystemFrameworkPaths]'
            + ' [cmdLineOptions.CompilerFrameworkPaths]'
";

        private const string _compilerOptimizationOptions =
                @"
    // Optimizations options
    // ---------------------
    .CompilerOptimizations = ''
            + ' [cmdLineOptions.OptimizationLevel]'
            + ' [options.AdditionalCompilerOptimizeOptions]'
";

        private const string _swiftCompilerExtraOptionsGeneral = @"
    .CompilerExtraOptions   = ''
            // General options
            // -------------------------
            + ' -parse-as-library'
            + ' -module-name [cmdLineOptions.SwiftModuleName]'
            + ' [cmdLineOptions.SwiftLanguageVersion]'
            + ' [cmdLineOptions.SwiftAdditionalIncludeDirectories]'
            + ' [cmdLineOptions.SwiftDeploymentTarget]'
            + ' -Xcc [cmdLineOptions.RuntimeTypeInfo]'
            + ' -Xcc [cmdLineOptions.CppExceptions]'
            + ' -Xcc [cmdLineOptions.ObjCExceptions]'
            + ' -Xcc [cmdLineOptions.ObjCARCExceptions]'
            + ' -Xcc [cmdLineOptions.DisableExceptions]'
";

        private const string _swiftCompilerExtraOptionsAdditional = @"
            // Additional compiler options
            //--------------------------
            + ' [cmdLineOptions.SwiftAdditionalCompilerOptions]'
";

        private const string _swiftCompilerOptimizationOptions =
                @"
    // Optimizations options
    // ---------------------
    .CompilerOptimizations = ''
            + ' [cmdLineOptions.SwiftOptimizationLevel]'
            + ' [cmdLineOptions.GenerateDebuggingSymbols]'
";
    }
}
