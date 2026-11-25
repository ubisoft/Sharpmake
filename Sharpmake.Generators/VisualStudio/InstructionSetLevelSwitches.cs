// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;

namespace Sharpmake.Generators.VisualStudio
{
    // The command line family of the compiler that builds a configuration: the same instruction set level
    // is asked with different switches by each of them.
    internal enum CompilerDriver
    {
        Msvc,     // cl.exe
        ClangCl,  // clang-cl.exe
        ClangGnu, // clang with gcc-style switches (Linux, Android, consoles)
    }

    internal static class InstructionSetLevelSwitches
    {
        private const int MinimumClangMajorForApx = 22;

        // Checks the toolchain that builds a configuration supports its instruction set level.
        // The MSVC minor version (14.51 or newer, which has /feature:APX) is not checked: the output must not depend
        // on the machine that generates it, so it is a documented requirement.
        // clangVersion is only evaluated when a clang-cl build needs it.
        public static void VerifyToolchain(ITarget target, Options.Vc.General.PlatformToolset toolset, bool usesClangCl, Func<string> clangVersion)
        {
            if (Util.GetInstructionSetLevel(target) != InstructionSetLevel.APX)
                return;

            if (toolset == Options.Vc.General.PlatformToolset.Default)
                return;

            if (toolset.IsLLVMToolchain())
            {
                // without clang-cl the Microsoft compiler builds the configuration, whose toolset is the default one
                if (!usesClangCl)
                    return;

                string version = clangVersion();
                if (!int.TryParse(version.Split('.')[0], out int major) || major < MinimumClangMajorForApx)
                {
                    throw new Error("Target \"{0}\": instruction set level APX needs clang {1} or newer with the {2} toolset, but clang {3} is used. Set ClangForWindows.Settings.ClangVersion or LLVMInstallDir to a newer one.",
                        target, MinimumClangMajorForApx, toolset, version);
                }
                return;
            }

            DevEnv? toolsetDevEnv = toolset.GetDefaultDevEnvForToolset();
            if (!toolsetDevEnv.HasValue || toolsetDevEnv.Value < DevEnv.vs2026)
            {
                throw new Error("Target \"{0}\": instruction set level APX needs the Default or v145 (Visual Studio 2026) platform toolset or newer, but {1} is used.",
                    target, toolset);
            }
        }

        // Returns the compiler switch of the instruction set level of the target, or null if there is none to add.
        // The architecture the level needs is checked when the target is validated (Util.VerifyProcessorArchitectureAndLevel).
        public static string GetCompilerSwitch(ITarget target, CompilerDriver driver)
        {
            if (Util.GetInstructionSetLevel(target) != InstructionSetLevel.APX)
                return null;

            switch (driver)
            {
                case CompilerDriver.Msvc: return "/feature:APX";
                // "/clang:" forwards the clang driver switch as is; checked with clang-cl 22.1.2 (defines __APX_F__, a plain -mapxf also works)
                case CompilerDriver.ClangCl: return "/clang:-mapxf";
                case CompilerDriver.ClangGnu: return "-mapxf";
                default: throw new Error("The APX feature is not supported by the compiler driver {0}.", driver);
            }
        }
    }
}
