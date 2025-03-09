// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake.Generators;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public static partial class Apple
    {
        [PlatformImplementation(Platform.tvos,
            typeof(IPlatformDescriptor),
            typeof(IFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IClangPlatformBff),
            typeof(IApplePlatformBff),
            typeof(IPlatformVcxproj),
            typeof(Project.Configuration.IConfigurationTasks))]
        public sealed partial class tvOsPlatform : BaseApplePlatform
        {
            public override Platform SharpmakePlatform => Platform.tvos;

            #region IPlatformDescriptor implementation
            public override string SimplePlatformString => "tvOS";
            #endregion

            #region IPlatformBff implementation
            public override string BffPlatformDefine => "_TVOS";

            public override string CConfigName(Configuration conf)
            {
                return ".tvosConfig";
            }

            public override string CppConfigName(Configuration conf)
            {
                return ".tvosppConfig";
            }

            public override string SwiftConfigName(Configuration conf)
            {
                return ".tvosswiftConfig";
            }
            #endregion

            public override void SelectCompilerOptions(IGenerationContext context)
            {
                base.SelectCompilerOptions(context);

                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                // Sysroot
                options["SDKRoot"] = "appletvos";
                cmdLineOptions["SDKRoot"] = $"-isysroot {ApplePlatform.Settings.TVOSSDKPath}";

                // Target
                options["MacOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                options["IPhoneOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                options["WatchOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;

                Options.XCode.Compiler.TvOSDeploymentTarget tvosDeploymentTarget = Options.GetObject<Options.XCode.Compiler.TvOSDeploymentTarget>(conf);
                if (tvosDeploymentTarget != null)
                {
                    options["TvOSDeploymentTarget"] = tvosDeploymentTarget.MinimumVersion;
                    string deploymentTarget = $"{GetDeploymentTargetPrefix(conf)}{tvosDeploymentTarget.MinimumVersion}";
                    cmdLineOptions["DeploymentTarget"] = IsLinkerInvokedViaCompiler ? deploymentTarget : FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["SwiftDeploymentTarget"] = deploymentTarget;
                }
                else
                {
                    options["TvOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["DeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["SwiftDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                }

                options["SupportsMaccatalyst"] = FileGeneratorUtilities.RemoveLineTag;
                options["SupportsMacDesignedForIphoneIpad"] = FileGeneratorUtilities.RemoveLineTag;

                #region infoplist keys
                context.SelectOptionWithFallback(
                    () => options["UIAppSupportsHDR"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIAppSupportsHDR.Disable, () => options["UIAppSupportsHDR"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.UIAppSupportsHDR.Enable, () => options["UIAppSupportsHDR"] = "YES")
                );
                #endregion // infoplist keys
            }

            public override void SelectLinkerOptions(IGenerationContext context)
            {
                base.SelectLinkerOptions(context);

                // Sysroot
                SelectCustomSysLibRoot(context, ApplePlatform.Settings.TVOSSDKPath);
            }
        }
    }
}
