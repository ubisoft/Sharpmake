// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

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
                generator.Write(_compilerExtraOptionsGeneral);
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
                    options["SDKRoot"] = customSdkRoot.Value;
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
                    cmdLineOptions["WatchOSDeploymentTarget"] = $"-target arm64-apple-watchos{watchosDeploymentTarget.MinimumVersion}";
                }
                else
                {
                    options["WatchOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["WatchOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
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
                cmdLineOptions["SysLibRoot"] = $"-syslibroot {XCodeDeveloperFolder}/Platforms/watchOS.platform/Developer/SDKs/watchOS.sdk";
                Options.XCode.Compiler.SDKRoot customSdkRoot = Options.GetObject<Options.XCode.Compiler.SDKRoot>(conf);
                if (customSdkRoot != null)
                    cmdLineOptions["SysLibRoot"] = $"-isysroot {customSdkRoot.Value}";
            }
        }
    }
}
