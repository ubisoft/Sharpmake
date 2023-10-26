// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;
using static Sharpmake.Options.XCode.Compiler;

namespace Sharpmake
{
    public static partial class Apple
    {
        [PlatformImplementation(Platform.watchos,
            typeof(IPlatformDescriptor),
            typeof(IFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IClangPlatformBff),
            typeof(IPlatformVcxproj),
            typeof(Project.Configuration.IConfigurationTasks))]
        public sealed partial class watchOsPlatform : BaseApplePlatform
        {
            public override Platform SharpmakePlatform => Platform.watchos;

            #region IPlatformDescriptor implementation.
            public override string SimplePlatformString => "watchOS";
            #endregion

            public override string BffPlatformDefine => "_WATCHOS";

            public override string CConfigName(Configuration conf)
            {
                return ".watchosConfig";
            }

            public override string CppConfigName(Configuration conf)
            {
                return ".watchosppConfig";
            }

            protected override void WriteCompilerExtraOptionsGeneral(IFileGenerator generator)
            {
                base.WriteCompilerExtraOptionsGeneral(generator);
            }

            public override void SelectCompilerOptions(IGenerationContext context)
            {
                base.SelectCompilerOptions(context);

                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                // Sysroot
                options["SDKRoot"] = "watchos";
                cmdLineOptions["SDKRoot"] = $"-isysroot {XCodeDeveloperFolder}/Platforms/watchOS.platform/Developer/SDKs/watchOS.sdk";
                Options.XCode.Compiler.SDKRoot customSdkRoot = Options.GetObject<Options.XCode.Compiler.SDKRoot>(conf);
                if (customSdkRoot != null)
                {
                    // Xcode doesn't accept the customized sdk path as SDKRoot
                    //options["SDKRoot"] = customSdkRoot.Value;
                    cmdLineOptions["SDKRoot"] = $"-isysroot {customSdkRoot.Value}";
                }

                // Target
                options["MacOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                options["IPhoneOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                options["TvOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;

                Options.XCode.Compiler.WatchOSDeploymentTarget watchosDeploymentTarget = Options.GetObject<Options.XCode.Compiler.WatchOSDeploymentTarget>(conf);
                if (watchosDeploymentTarget != null)
                {
                    options["WatchOSDeploymentTarget"] = watchosDeploymentTarget.MinimumVersion;
                    cmdLineOptions["DeploymentTarget"] = IsLinkerInvokedViaCompiler ? $"{GetDeploymentTargetPrefix(conf)}{watchosDeploymentTarget.MinimumVersion}" : FileGeneratorUtilities.RemoveLineTag;
                }
                else
                {
                    options["WatchOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["DeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                }

                options["SupportsMaccatalyst"] = FileGeneratorUtilities.RemoveLineTag;
                options["SupportsMacDesignedForIphoneIpad"] = FileGeneratorUtilities.RemoveLineTag;
            }

            public override void SelectLinkerOptions(IGenerationContext context)
            {
                base.SelectLinkerOptions(context);

                // Sysroot
                var defaultSdkRoot = $"{XCodeDeveloperFolder}/Platforms/watchOS.platform/Developer/SDKs/watchOS.sdk";
                SelectCustomSysLibRoot(context, defaultSdkRoot);
            }
        }
    }
}
