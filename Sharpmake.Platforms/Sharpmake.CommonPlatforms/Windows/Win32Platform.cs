// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using Sharpmake.Generators;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public static partial class Windows
    {
        [PlatformImplementation(Platform.win32,
            typeof(IPlatformDescriptor),
            typeof(Project.Configuration.IConfigurationTasks),
            typeof(IFastBuildCompilerSettings),
            typeof(IWindowsFastBuildCompilerSettings),
            typeof(IPlatformVcxproj))]
        public sealed class Win32Platform : BaseWindowsPlatform
        {
            #region IPlatformDescriptor implementation
            public override string SimplePlatformString => "Win32";
            public override string GetToolchainPlatformString(ITarget target) => "Win32";
            #endregion

            #region IPlatformVcxproj implementation
            public override IEnumerable<string> GetImplicitlyDefinedSymbols(IGenerationContext context)
            {
                var defines = new List<string>();
                defines.AddRange(base.GetImplicitlyDefinedSymbols(context));
                defines.Add("WIN32");

                return defines;
            }

            public override void SetupPlatformTargetOptions(IGenerationContext context)
            {
                context.Options["TargetMachine"] = "MachineX86";
                context.CommandLineOptions["TargetMachine"] = "/MACHINE:X86";
                context.CommandLineOptions["NasmCompilerFormat"] = "-fwin32";
            }

            public override void SelectPlatformAdditionalDependenciesOptions(IGenerationContext context)
            {
                base.SelectPlatformAdditionalDependenciesOptions(context);
                context.Options["AdditionalDependencies"] += ";%(AdditionalDependencies)";
            }
            #endregion
        }
    }
}
