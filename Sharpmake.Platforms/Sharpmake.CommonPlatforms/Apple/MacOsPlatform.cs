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

using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public static partial class Apple
    {
        [PlatformImplementation(Platform.mac,
            typeof(IPlatformDescriptor),
            typeof(IFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IClangPlatformBff),
            typeof(IPlatformVcxproj),
            typeof(Project.Configuration.IConfigurationTasks))]
        public sealed class MacOsPlatform : BaseApplePlatform
        {
            public override Platform SharpmakePlatform => Platform.mac;

            public override string SimplePlatformString => "Mac";

            public override string BffPlatformDefine => "APPLE_OSX";

            public override string CConfigName(Configuration conf)
            {
                return ".osxConfig";
            }

            public override string CppConfigName(Configuration conf)
            {
                return ".osxppConfig";
            }

            public override void SelectCompilerOptions(IGenerationContext context)
            {
                base.SelectCompilerOptions(context);

                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                // Sysroot
                options["SDKRoot"] = "macosx";
                cmdLineOptions["SDKRoot"] = $"-isysroot {XCodeDeveloperFolder}/Platforms/MacOSX.platform/Developer/SDKs/MacOSX10.15.sdk";
                Options.XCode.Compiler.SDKRoot customSdkRoot = Options.GetObject<Options.XCode.Compiler.SDKRoot>(conf);
                if (customSdkRoot != null)
                {
                    options["SDKRoot"] = customSdkRoot.Value;
                    cmdLineOptions["SDKRoot"] = $"-isysroot {customSdkRoot.Value}";
                }

                // Target
                options["IPhoneOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;

                var macOsDeploymentTarget = Options.GetObject<Options.XCode.Compiler.MacOSDeploymentTarget>(conf);
                if (macOsDeploymentTarget != null)
                {
                    options["MacOSDeploymentTarget"] = macOsDeploymentTarget.MinimumVersion;
                    cmdLineOptions["MacOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag; // TODO: find what to write here
                }
                else
                {
                    options["MacOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["MacOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                }
            }
        }
    }
}
