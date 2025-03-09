// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake.Generators;
using Sharpmake.Generators.Apple;
using Sharpmake.Generators.FastBuild;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake
{
    public static partial class Apple
    {
        [PlatformImplementation(Platform.ios,
            typeof(IPlatformDescriptor),
            typeof(IFastBuildCompilerSettings),
            typeof(IPlatformBff),
            typeof(IClangPlatformBff),
            typeof(IApplePlatformBff),
            typeof(IPlatformVcxproj),
            typeof(Project.Configuration.IConfigurationTasks))]
        public sealed partial class iOsPlatform : BaseApplePlatform
        {
            public override Platform SharpmakePlatform => Platform.ios;

            #region IPlatformDescriptor implementation
            public override string SimplePlatformString => "iOS";
            #endregion

            #region IPlatformBff implementation
            public override string BffPlatformDefine => "_IOS";

            public override string CConfigName(Configuration conf)
            {
                return ".iosConfig";
            }

            public override string CppConfigName(Configuration conf)
            {
                return ".iosppConfig";
            }

            public override string SwiftConfigName(Configuration conf)
            {
                return ".iosswiftConfig";
            }
            #endregion

            public override void SelectCompilerOptions(IGenerationContext context)
            {
                base.SelectCompilerOptions(context);

                var options = context.Options;
                var cmdLineOptions = context.CommandLineOptions;
                var conf = context.Configuration;

                // Sysroot
                options["SDKRoot"] = "iphoneos";
                cmdLineOptions["SDKRoot"] = $"-isysroot {ApplePlatform.Settings.IPhoneOSSDKPath}";

                // Target
                options["MacOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                options["TvOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                options["WatchOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;

                Options.XCode.Compiler.IPhoneOSDeploymentTarget iosDeploymentTarget = Options.GetObject<Options.XCode.Compiler.IPhoneOSDeploymentTarget>(conf);
                if (iosDeploymentTarget != null)
                {
                    options["IPhoneOSDeploymentTarget"] = iosDeploymentTarget.MinimumVersion;
                    string deploymentTarget = $"{GetDeploymentTargetPrefix(conf)}{iosDeploymentTarget.MinimumVersion}";
                    cmdLineOptions["DeploymentTarget"] = IsLinkerInvokedViaCompiler ? deploymentTarget : FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["SwiftDeploymentTarget"] = deploymentTarget;
                }
                else
                {
                    options["IPhoneOSDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["DeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                    cmdLineOptions["SwiftDeploymentTarget"] = FileGeneratorUtilities.RemoveLineTag;
                }

                context.SelectOptionWithFallback(
                    () => options["SupportsMaccatalyst"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.Compiler.SupportsMaccatalyst.Disable, () => options["SupportsMaccatalyst"] = "NO"),
                    Options.Option(Options.XCode.Compiler.SupportsMaccatalyst.Enable, () => options["SupportsMaccatalyst"] = "YES")
                );
                context.SelectOptionWithFallback(
                    () => options["SupportsMacDesignedForIphoneIpad"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.Compiler.SupportsMacDesignedForIphoneIpad.Disable, () => options["SupportsMacDesignedForIphoneIpad"] = "NO"),
                    Options.Option(Options.XCode.Compiler.SupportsMacDesignedForIphoneIpad.Enable, () => options["SupportsMacDesignedForIphoneIpad"] = "YES")
                );

                #region infoplist keys
                context.SelectOptionWithFallback(
                    () => options["LSRequiresIPhoneOS"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.LSRequiresIPhoneOS.Disable, () => options["LSRequiresIPhoneOS"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.LSRequiresIPhoneOS.Enable, () => options["LSRequiresIPhoneOS"] = "YES")
                );

                options["UIRequiredDeviceCapabilities"] = XCodeUtil.XCodeFormatList(Options.GetStrings<Options.XCode.InfoPlist.UIRequiredDeviceCapabilities>(conf), 4);
                options["UIMainStoryboardFile"] = Options.StringOption.Get<Options.XCode.InfoPlist.UIMainStoryboardFile>(conf);
                options["UILaunchStoryboardName"] = Options.StringOption.Get<Options.XCode.InfoPlist.UILaunchStoryboardName>(conf);
                options["CFBundleIconFile"] = Options.StringOption.Get<Options.XCode.InfoPlist.CFBundleIconFile>(conf);
                options["CFBundleIconFiles"] = XCodeUtil.XCodeFormatList(Options.GetStrings<Options.XCode.InfoPlist.CFBundleIconFiles>(conf), 4);
                options["CFBundleIconName"] = Options.StringOption.Get<Options.XCode.InfoPlist.CFBundleIconName>(conf);

                context.SelectOptionWithFallback(
                    () => options["UIPrerenderedIcon"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIPrerenderedIcon.Disable, () => options["UIPrerenderedIcon"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.UIPrerenderedIcon.Enable, () => options["UIPrerenderedIcon"] = "YES")
                );

                context.SelectOptionWithFallback(
                    () => options["UIInterfaceOrientation"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIInterfaceOrientation.UIInterfaceOrientationPortrait, () => options["UIInterfaceOrientation"] = "UIInterfaceOrientationPortrait"),
                    Options.Option(Options.XCode.InfoPlist.UIInterfaceOrientation.UIInterfaceOrientationPortraitUpsideDown, () => options["UIInterfaceOrientation"] = "UIInterfaceOrientationPortraitUpsideDown"),
                    Options.Option(Options.XCode.InfoPlist.UIInterfaceOrientation.UIInterfaceOrientationLandscapeLeft, () => options["UIInterfaceOrientation"] = "UIInterfaceOrientationLandscapeLeft"),
                    Options.Option(Options.XCode.InfoPlist.UIInterfaceOrientation.UIInterfaceOrientationLandscapeRight, () => options["UIInterfaceOrientation"] = "UIInterfaceOrientationLandscapeRight")
                );

                Options.XCode.InfoPlist.UIInterfaceOrientation_iPhone uiInterfaceOrientation_iPhone = Options.GetObject<Options.XCode.InfoPlist.UIInterfaceOrientation_iPhone>(conf);
                if (uiInterfaceOrientation_iPhone != null)
                {
                    options["UIInterfaceOrientation_iPhone"] = uiInterfaceOrientation_iPhone.ToString();
                }
                else
                {
                    options["UIInterfaceOrientation_iPhone"] = options["UIInterfaceOrientation"];
                }

                Options.XCode.InfoPlist.UIInterfaceOrientation_iPad uiInterfaceOrientation_iPad = Options.GetObject<Options.XCode.InfoPlist.UIInterfaceOrientation_iPad>(conf);
                if (uiInterfaceOrientation_iPad != null)
                {
                    options["UIInterfaceOrientation_iPad"] = uiInterfaceOrientation_iPad.ToString();
                }
                else
                {
                    options["UIInterfaceOrientation_iPad"] = options["UIInterfaceOrientation"];
                }

                Options.XCode.InfoPlist.UISupportedInterfaceOrientations uiSupportedInterfaceOrientations = Options.GetObject<Options.XCode.InfoPlist.UISupportedInterfaceOrientations>(conf);
                if (uiSupportedInterfaceOrientations != null)
                {
                    options["UISupportedInterfaceOrientations"] = uiSupportedInterfaceOrientations.ToString();
                }
                else
                {
                    options["UISupportedInterfaceOrientations"] = options["UIInterfaceOrientation"];
                }

                Options.XCode.InfoPlist.UISupportedInterfaceOrientations_iPhone uiSupportedInterfaceOrientations_iPhone = Options.GetObject<Options.XCode.InfoPlist.UISupportedInterfaceOrientations_iPhone>(conf);
                if (uiSupportedInterfaceOrientations_iPhone != null)
                {
                    options["UISupportedInterfaceOrientations_iPhone"] = uiSupportedInterfaceOrientations_iPhone.ToString();
                }
                else if (options["UISupportedInterfaceOrientations"] != FileGeneratorUtilities.RemoveLineTag)
                {
                    options["UISupportedInterfaceOrientations_iPhone"] = options["UISupportedInterfaceOrientations"];
                }
                else
                {
                    options["UISupportedInterfaceOrientations_iPhone"] = options["UIInterfaceOrientation_iPhone"];
                }

                Options.XCode.InfoPlist.UISupportedInterfaceOrientations_iPad uiSupportedInterfaceOrientations_iPad = Options.GetObject<Options.XCode.InfoPlist.UISupportedInterfaceOrientations_iPad>(conf);
                if (uiSupportedInterfaceOrientations_iPad != null)
                {
                    options["UISupportedInterfaceOrientations_iPad"] = uiSupportedInterfaceOrientations_iPad.ToString();
                }
                else if (options["UISupportedInterfaceOrientations"] != FileGeneratorUtilities.RemoveLineTag)
                {
                    options["UISupportedInterfaceOrientations_iPad"] = options["UISupportedInterfaceOrientations"];
                }
                else
                {
                    options["UISupportedInterfaceOrientations_iPad"] = options["UIInterfaceOrientation_iPad"];
                }

                context.SelectOptionWithFallback(
                    () => options["UIUserInterfaceStyle"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIUserInterfaceStyle.Automatic, () => options["UIUserInterfaceStyle"] = "Automatic"),
                    Options.Option(Options.XCode.InfoPlist.UIUserInterfaceStyle.Light, () => options["UIUserInterfaceStyle"] = "Light"),
                    Options.Option(Options.XCode.InfoPlist.UIUserInterfaceStyle.Dark, () => options["UIUserInterfaceStyle"] = "Dark")
                );

                context.SelectOptionWithFallback(
                    () => options["UIWhitePointAdaptivityStyle"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIWhitePointAdaptivityStyle.UIWhitePointAdaptivityStyleStandard, () => options["UIWhitePointAdaptivityStyle"] = "UIWhitePointAdaptivityStyleStandard"),
                    Options.Option(Options.XCode.InfoPlist.UIWhitePointAdaptivityStyle.UIWhitePointAdaptivityStyleReading, () => options["UIWhitePointAdaptivityStyle"] = "UIWhitePointAdaptivityStyleReading"),
                    Options.Option(Options.XCode.InfoPlist.UIWhitePointAdaptivityStyle.UIWhitePointAdaptivityStylePhoto, () => options["UIWhitePointAdaptivityStyle"] = "UIWhitePointAdaptivityStylePhoto"),
                    Options.Option(Options.XCode.InfoPlist.UIWhitePointAdaptivityStyle.UIWhitePointAdaptivityStyleVideo, () => options["UIWhitePointAdaptivityStyle"] = "UIWhitePointAdaptivityStyleVideo"),
                    Options.Option(Options.XCode.InfoPlist.UIWhitePointAdaptivityStyle.UIWhitePointAdaptivityStyleGame, () => options["UIWhitePointAdaptivityStyle"] = "UIWhitePointAdaptivityStyleGame")
                );

                context.SelectOptionWithFallback(
                    () => options["UIRequiresFullScreen"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIRequiresFullScreen.Disable, () => options["UIRequiresFullScreen"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.UIRequiresFullScreen.Enable, () => options["UIRequiresFullScreen"] = "YES")
                );

                context.SelectOptionWithFallback(
                    () => options["UIStatusBarHidden"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIStatusBarHidden.Disable, () => options["UIStatusBarHidden"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.UIStatusBarHidden.Enable, () => options["UIStatusBarHidden"] = "YES")
                );

                context.SelectOptionWithFallback(
                    () => options["UIViewControllerBasedStatusBarAppearance"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIViewControllerBasedStatusBarAppearance.Disable, () => options["UIViewControllerBasedStatusBarAppearance"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.UIViewControllerBasedStatusBarAppearance.Enable, () => options["UIViewControllerBasedStatusBarAppearance"] = "YES")
                );

                context.SelectOptionWithFallback(
                    () => options["UIStatusBarStyle"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIStatusBarStyle.UIStatusBarStyleDefault, () => options["UIStatusBarStyle"] = "UIStatusBarStyleDefault"),
                    Options.Option(Options.XCode.InfoPlist.UIStatusBarStyle.UIStatusBarStyleLightContent, () => options["UIStatusBarStyle"] = "UIStatusBarStyleLightContent"),
                    Options.Option(Options.XCode.InfoPlist.UIStatusBarStyle.UIStatusBarStyleDarkContent, () => options["UIStatusBarStyle"] = "UIStatusBarStyleDarkContent")
                );

                context.SelectOptionWithFallback(
                    () => options["UIApplicationSupportsIndirectInputEvents"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIApplicationSupportsIndirectInputEvents.Disable, () => options["UIApplicationSupportsIndirectInputEvents"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.UIApplicationSupportsIndirectInputEvents.Enable, () => options["UIApplicationSupportsIndirectInputEvents"] = "YES")
                );

                context.SelectOptionWithFallback(
                    () => options["UIRequiresPersistentWiFi"] = FileGeneratorUtilities.RemoveLineTag,
                    Options.Option(Options.XCode.InfoPlist.UIRequiresPersistentWiFi.Disable, () => options["UIRequiresPersistentWiFi"] = "NO"),
                    Options.Option(Options.XCode.InfoPlist.UIRequiresPersistentWiFi.Enable, () => options["UIRequiresPersistentWiFi"] = "YES")
                );
                #endregion //infoplist keys
            }

            public override void SelectLinkerOptions(IGenerationContext context)
            {
                base.SelectLinkerOptions(context);

                // Sysroot
                SelectCustomSysLibRoot(context, ApplePlatform.Settings.IPhoneOSSDKPath);
            }
        }
    }
}
