// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public static partial class Apple
    {
        [PlatformImplementation(Platform.maccatalyst,
            typeof(IPlatformDescriptor),
            typeof(IFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IClangPlatformBff),
            typeof(IPlatformVcxproj),
            typeof(Project.Configuration.IConfigurationTasks))]
        public sealed partial class MacCatalystPlatform : BaseApplePlatform
        {
            public override Platform SharpmakePlatform => Platform.maccatalyst;

            #region IPlatformDescriptor implementation.
            public override string SimplePlatformString => "MacCatalyst";
            #endregion

            public override string BffPlatformDefine => "_MACCATALYST";

            public override string CConfigName(Configuration conf)
            {
                return ".maccatalystConfig";
            }

            public override string CppConfigName(Configuration conf)
            {
                return ".maccatalystppConfig";
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
                options["SDKRoot"] = "iphoneos";
                cmdLineOptions["SDKRoot"] = $"-isysroot {XCodeDeveloperFolder}/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk";
                Options.XCode.Compiler.SDKRoot customSdkRoot = Options.GetObject<Options.XCode.Compiler.SDKRoot>(conf);
                if (customSdkRoot != null)
                {
                    options["SDKRoot"] = customSdkRoot.Value;
                    cmdLineOptions["SDKRoot"] = $"-isysroot {customSdkRoot.Value}";
                }

                // Target
                options["MacOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                options["TvOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                options["WatchOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;

                Options.XCode.Compiler.IPhoneOSDeploymentTarget iosDeploymentTarget = Options.GetObject<Options.XCode.Compiler.IPhoneOSDeploymentTarget>(conf);
                if (iosDeploymentTarget != null)
                {
                    options["IPhoneOSDeploymentTarget"] = iosDeploymentTarget.MinimumVersion;
                    cmdLineOptions["IPhoneOSDeploymentTarget"] = $"-target arm64-apple-ios{iosDeploymentTarget.MinimumVersion}";
                }
                else
                {
                    options["IPhoneOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["IPhoneOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                }

                 context.SelectOptionWithFallback(
                    () => options["SupportsMaccatalyst"] = "YES",
                    Options.Option(Options.XCode.Compiler.SupportsMaccatalyst.Disable, () => options["SupportsMaccatalyst"] = "NO")
                );
                context.SelectOptionWithFallback(
                    () => options["SupportsMacDesignedForIphoneIpad"] = "YES",
                    Options.Option(Options.XCode.Compiler.SupportsMacDesignedForIphoneIpad.Disable, () => options["SupportsMacDesignedForIphoneIpad"] = "NO")
                );
            }

            public override void SelectLinkerOptions(IGenerationContext context)
            {
                base.SelectLinkerOptions(context);

                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                // Sysroot
                cmdLineOptions["SysLibRoot"] = $"-syslibroot {XCodeDeveloperFolder}/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk";
                Options.XCode.Compiler.SDKRoot customSdkRoot = Options.GetObject<Options.XCode.Compiler.SDKRoot>(conf);
                if (customSdkRoot != null)
                    cmdLineOptions["SysLibRoot"] = $"-isysroot {customSdkRoot.Value}";
            }
        }
    }
}
