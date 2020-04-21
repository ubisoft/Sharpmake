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
