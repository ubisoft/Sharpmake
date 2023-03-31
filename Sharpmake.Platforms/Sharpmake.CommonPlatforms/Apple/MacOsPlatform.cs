// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
                cmdLineOptions["SDKRoot"] = $"-isysroot {XCodeDeveloperFolder}/Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk";
                Options.XCode.Compiler.SDKRoot customSdkRoot = Options.GetObject<Options.XCode.Compiler.SDKRoot>(conf);
                if (customSdkRoot != null)
                {
                    options["SDKRoot"] = customSdkRoot.Value;
                    cmdLineOptions["SDKRoot"] = $"-isysroot {customSdkRoot.Value}";
                }

                // Target
                options["IPhoneOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                options["TvOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                options["WatchOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;

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

                options["SupportsMaccatalyst"] = FileGeneratorUtilities.RemoveLineTag;
                options["SupportsMacDesignedForIphoneIpad"] = FileGeneratorUtilities.RemoveLineTag;
            }

            public override void SelectLinkerOptions(IGenerationContext context)
            {
                base.SelectLinkerOptions(context);

                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                // Sysroot
                cmdLineOptions["SysLibRoot"] = $"-syslibroot {XCodeDeveloperFolder}/Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk";
                Options.XCode.Compiler.SDKRoot customSdkRoot = Options.GetObject<Options.XCode.Compiler.SDKRoot>(conf);
                if (customSdkRoot != null)
                    cmdLineOptions["SysLibRoot"] = $"-isysroot {customSdkRoot.Value}";
            }
        }
    }
}
