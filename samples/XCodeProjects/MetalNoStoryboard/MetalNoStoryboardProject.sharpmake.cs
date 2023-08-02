// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Generate]
    public class MetalNoStoryboardProject : CommonProject
    {
        public MetalNoStoryboardProject()
        {
            Name = @"MetalNoStoryboard";

            AddTargets(CommonTarget.GetMacTargets());
            AddTargets(CommonTarget.GetIosTargets());
            AddTargets(CommonTarget.GetTvosTargets());
            AddTargets(CommonTarget.GetCatalystTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.AppleApp;
            conf.XcodeSystemFrameworks.Add("Metal", "MetalKit", "GameController");

            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleVersion(@"4.2"));
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleShortVersion(@"42"));

            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.CFBundleSpokenName(
                    @"Metal Without Storyboard Sample"
                )
            );
            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.CFBundleDevelopmentRegion(@"en")
            );
            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.CFBundleLocalizations(@"en", @"de")
            );

            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.NSHumanReadableCopyright(@"CC0")
            );
            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.UIRequiredDeviceCapabilities(
                    @"arm64",
                    @"metal",
                    @"camera",
                    @"wifi"
                )
            );

            conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.NSHighResolutionCapable.Enable);
            conf.Options.Add(
                Sharpmake.Options.XCode.InfoPlist.CFBundleAllowMixedLocalizations.Enable
            );
            conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.LSRequiresNativeExecution.Enable);
            conf.Options.Add(
                Sharpmake.Options.XCode.InfoPlist.LSMultipleInstancesProhibited.Enable
            );
            conf.Options.Add(
                Sharpmake.Options.XCode.InfoPlist.NSSupportsAutomaticGraphicsSwitching.Enable
            );
            conf.Options.Add(
                Sharpmake.Options.XCode.InfoPlist.NSPrefersDisplaySafeAreaCompatibilityMode.Enable
            );
            conf.Options.Add(
                Sharpmake.Options.XCode.InfoPlist.UISupportsTrueScreenSizeOnMac.Enable
            );
            conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.LSRequiresIPhoneOS.Enable);
            conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UIPrerenderedIcon.Enable);
            conf.Options.Add(
                Sharpmake
                    .Options
                    .XCode
                    .InfoPlist
                    .UIInterfaceOrientation
                    .UIInterfaceOrientationLandscapeRight
            );
            //conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UIInterfaceOrientation_iPhone.UIInterfaceOrientationLandscapeRight);
            //conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UIInterfaceOrientation_iPad.UIInterfaceOrientationLandscapeLeft);
            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.UISupportedInterfaceOrientations(
                    Sharpmake
                        .Options
                        .XCode
                        .InfoPlist
                        .UIInterfaceOrientation
                        .UIInterfaceOrientationLandscapeLeft,
                    Sharpmake
                        .Options
                        .XCode
                        .InfoPlist
                        .UIInterfaceOrientation
                        .UIInterfaceOrientationLandscapeRight
                )
            );
            //conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UISupportedInterfaceOrientations_iPhone.UIInterfaceOrientationLandscapeRight);
            //conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UISupportedInterfaceOrientations_iPad.UIInterfaceOrientationLandscapeLeft);
            conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UIUserInterfaceStyle.Automatic);
            conf.Options.Add(
                Sharpmake
                    .Options
                    .XCode
                    .InfoPlist
                    .UIWhitePointAdaptivityStyle
                    .UIWhitePointAdaptivityStyleGame
            );
            conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UIRequiresFullScreen.Enable);
            conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UIStatusBarHidden.Enable);
            conf.Options.Add(
                Sharpmake.Options.XCode.InfoPlist.UIViewControllerBasedStatusBarAppearance.Enable
            );
            conf.Options.Add(
                Sharpmake.Options.XCode.InfoPlist.UIStatusBarStyle.UIStatusBarStyleDarkContent
            );
            conf.Options.Add(
                Sharpmake.Options.XCode.InfoPlist.UIApplicationSupportsIndirectInputEvents.Enable
            );
            conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UIRequiresPersistentWiFi.Enable);
            conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UIAppSupportsHDR.Enable);

            conf.TargetCopyFilesPath = Path.Join(@"./");
            conf.TargetCopyFiles.Add(@"foobar.dat");
            conf.TargetCopyFilesToSubDirectory.Add(
                new KeyValuePair<string, string>(@"foobar2.dat", @"huba")
            );
        }

        public override void ConfigureMacOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureMacOS(conf, target);
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.InfoPListFile(
                    Path.Join(SourceRootPath, "Info.plist")
                )
            );
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(
                    @"com.ubisoft.sharpmake.sample.metalnostoryboard.macos"
                )
            );
        }

        public override void ConfigureIOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureIOS(conf, target);
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.InfoPListFile(
                    Path.Join(SourceRootPath, "Info-ios.plist")
                )
            );
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(
                    @"com.ubisoft.sharpmake.sample.metalnostoryboard.ios"
                )
            );
        }

        public override void ConfigureTVOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureTVOS(conf, target);
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.InfoPListFile(
                    Path.Join(SourceRootPath, "Info-tvos.plist")
                )
            );
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(
                    @"com.ubisoft.sharpmake.sample.metalnostoryboard.tvos"
                )
            );
        }

        public override void ConfigureCatalyst(Configuration conf, CommonTarget target)
        {
            base.ConfigureCatalyst(conf, target);
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.InfoPListFile(
                    Path.Join(SourceRootPath, "Info-catalyst.plist")
                )
            );
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(
                    @"com.ubisoft.sharpmake.sample.metalnostoryboard.catalyst"
                )
            );
        }
    }
}
