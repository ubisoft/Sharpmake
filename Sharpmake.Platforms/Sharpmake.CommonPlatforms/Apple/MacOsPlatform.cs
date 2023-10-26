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
                    // Xcode doesn't accept the customized sdk path as SDKRoot
                    //options["SDKRoot"] = customSdkRoot.Value;
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
                    cmdLineOptions["DeploymentTarget"] = IsLinkerInvokedViaCompiler ? $"{GetDeploymentTargetPrefix(conf)}{macOsDeploymentTarget.MinimumVersion}" : FileGeneratorUtilities.RemoveLineTag;
                }
                else
                {
                    options["MacOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["DeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                }

                options["SupportsMaccatalyst"] = FileGeneratorUtilities.RemoveLineTag;
                options["SupportsMacDesignedForIphoneIpad"] = FileGeneratorUtilities.RemoveLineTag;

                #region infoplist keys
                // MacOS specific flags
                options["NSHumanReadableCopyright"] = Options.StringOption.Get<Options.XCode.InfoPlist.NSHumanReadableCopyright>(conf);
                options["NSMainStoryboardFile"] = Options.StringOption.Get<Options.XCode.InfoPlist.NSMainStoryboardFile>(conf);
                options["NSMainNibFile"] = Options.StringOption.Get<Options.XCode.InfoPlist.NSMainNibFile>(conf);
                options["NSPrefPaneIconFile"] = Options.StringOption.Get<Options.XCode.InfoPlist.NSPrefPaneIconFile>(conf);
                options["NSPrefPaneIconLabel"] = Options.StringOption.Get<Options.XCode.InfoPlist.NSPrefPaneIconLabel>(conf);
                options["NSPrincipalClass"] = Options.StringOption.Get<Options.XCode.InfoPlist.NSPrincipalClass>(conf);

                context.SelectOptionWithFallback(
                    () => options["LSRequiresNativeExecution"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.LSRequiresNativeExecution.Disable, () => options["LSRequiresNativeExecution"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.LSRequiresNativeExecution.Enable, () => options["LSRequiresNativeExecution"] = "YES")
                );

                context.SelectOptionWithFallback(
                    () => options["LSMultipleInstancesProhibited"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.LSMultipleInstancesProhibited.Disable, () => options["LSMultipleInstancesProhibited"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.LSMultipleInstancesProhibited.Enable, () => options["LSMultipleInstancesProhibited"] = "YES")
                );

                context.SelectOptionWithFallback(
                    () => options["NSSupportsAutomaticGraphicsSwitching"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.NSSupportsAutomaticGraphicsSwitching.Disable, () => options["NSSupportsAutomaticGraphicsSwitching"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.NSSupportsAutomaticGraphicsSwitching.Enable, () => options["NSSupportsAutomaticGraphicsSwitching"] = "YES")
                );

                context.SelectOptionWithFallback(
                    () => options["NSPrefersDisplaySafeAreaCompatibilityMode"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.NSPrefersDisplaySafeAreaCompatibilityMode.Disable, () => options["NSPrefersDisplaySafeAreaCompatibilityMode"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.NSPrefersDisplaySafeAreaCompatibilityMode.Enable, () => options["NSPrefersDisplaySafeAreaCompatibilityMode"] = "YES")
                );

                context.SelectOptionWithFallback(
                    () => options["UISupportsTrueScreenSizeOnMac"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UISupportsTrueScreenSizeOnMac.Disable, () => options["UISupportsTrueScreenSizeOnMac"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.UISupportsTrueScreenSizeOnMac.Enable, () => options["UISupportsTrueScreenSizeOnMac"] = "YES")
                );
                #endregion // infoplist keys
            }

            public override void SelectLinkerOptions(IGenerationContext context)
            {
                base.SelectLinkerOptions(context);

                // Sysroot
                var defaultSdkRoot = $"{XCodeDeveloperFolder}/Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk";
                SelectCustomSysLibRoot(context, defaultSdkRoot);
            }
        }
    }
}
