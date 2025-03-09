// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Generate]
    public class MetalWithStoryboardProject : CommonProject
    {
        public MetalWithStoryboardProject()
        {
            Name = @"MetalWithStoryboard";

            AddTargets(CommonTarget.GetMacTargets());
            AddTargets(CommonTarget.GetIosTargets());
            AddTargets(CommonTarget.GetTvosTargets());
            AddTargets(CommonTarget.GetCatalystTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;

            SourceFiles.Add(
                Path.Combine(@"[project.SourceRootPath]", "Base-appkit.lproj", "Main.storyboard")
            );
            SourceFiles.Add(
                Path.Combine(
                    @"[project.SourceRootPath]",
                    "Base-uikit.lproj",
                    "LaunchScreen.storyboard"
                )
            );
            SourceFiles.Add(
                Path.Combine(@"[project.SourceRootPath]", "Base-uikit.lproj", "Main.storyboard")
            );
            ResourceFiles.Add(Path.Combine(@"[project.SourceRootPath]", "Assets.xcassets"));
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.AppleApp;
            conf.XcodeSystemFrameworks.Add("Metal", "MetalKit", "ModelIO");

            conf.Options.Add(Sharpmake.Options.XCode.Compiler.GenerateInfoPlist.Enable);
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleVersion(@"1.0"));
            conf.Options.Add(new Sharpmake.Options.XCode.Compiler.ProductBundleShortVersion(@"1"));

            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.AssetCatalogCompilerAppIconName(@"AppIcon")
            );
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.AssetCatalogCompilerGlobalAccentColorName(
                    @"AccentColor"
                )
            );
            conf.Options.Add(
                Sharpmake.Options.XCode.Compiler.AssetCatalogCompilerOptimization.Time
            );
            conf.Options.Add(
                Sharpmake.Options.XCode.Compiler.AssetCatalogCompilerStandaloneIconBehavior.All
            );
            conf.Options.Add(Sharpmake.Options.XCode.Compiler.AssetCatalogNotices.Enable);
            conf.Options.Add(Sharpmake.Options.XCode.Compiler.AssetCatalogWarnings.Enable);

            conf.Options.Add(new Sharpmake.Options.XCode.InfoPlist.NSMainStoryboardFile(@"Main"));
            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.NSPrincipalClass(@"NSApplication")
            );
            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.NSHumanReadableCopyright(@"(C) 2023 Ubisoft")
            );

            conf.Options.Add(
                Sharpmake.Options.XCode.InfoPlist.UIApplicationSupportsIndirectInputEvents.Enable
            );
            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.UILaunchStoryboardName(@"LaunchScreen")
            );
            conf.Options.Add(new Sharpmake.Options.XCode.InfoPlist.UIMainStoryboardFile(@"Main"));
            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.UIRequiredDeviceCapabilities(
                    @"metal",
                    @"arm64"
                )
            );
            conf.Options.Add(Sharpmake.Options.XCode.InfoPlist.UIStatusBarHidden.Enable);

            conf.Options.Add(Sharpmake.Options.XCode.Compiler.SwiftEmitLocStrings.Enable);

            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.UISupportedInterfaceOrientations_iPhone(
                    Sharpmake
                        .Options
                        .XCode
                        .InfoPlist
                        .UIInterfaceOrientation
                        .UIInterfaceOrientationPortrait,
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
            conf.Options.Add(
                new Sharpmake.Options.XCode.InfoPlist.UISupportedInterfaceOrientations_iPhone(
                    Sharpmake
                        .Options
                        .XCode
                        .InfoPlist
                        .UIInterfaceOrientation
                        .UIInterfaceOrientationPortrait,
                    Sharpmake
                        .Options
                        .XCode
                        .InfoPlist
                        .UIInterfaceOrientation
                        .UIInterfaceOrientationPortraitUpsideDown,
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
        }

        public override void ConfigureMacOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureMacOS(conf, target);
            // conf.Options.Add(new Sharpmake.Options.XCode.Compiler.InfoPListFile(Path.Join(SourceRootPath, "Info.plist")));
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(
                    @"com.ubisoft.sharpmake.sample.metalwithstoryboard.macos"
                )
            );
            conf.SourceFilesBuildExcludeRegex.Add(@"Base\-uikit\.lproj");
        }

        public override void ConfigureIOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureIOS(conf, target);
            // conf.Options.Add(new Sharpmake.Options.XCode.Compiler.InfoPListFile(Path.Join(SourceRootPath, "Info-ios.plist")));
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(
                    @"com.ubisoft.sharpmake.sample.metalwithstoryboard.ios"
                )
            );
            conf.SourceFilesBuildExcludeRegex.Add(@"Base\-appkit\.lproj");
        }

        public override void ConfigureTVOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureTVOS(conf, target);
            // conf.Options.Add(new Sharpmake.Options.XCode.Compiler.InfoPListFile(Path.Join(SourceRootPath, "Info-tvos.plist")));
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(
                    @"com.ubisoft.sharpmake.sample.metalwithstoryboard.tvos"
                )
            );
            conf.SourceFilesBuildExcludeRegex.Add(@"Base\-appkit\.lproj");
        }

        public override void ConfigureCatalyst(Configuration conf, CommonTarget target)
        {
            base.ConfigureCatalyst(conf, target);
            // conf.Options.Add(new Sharpmake.Options.XCode.Compiler.InfoPListFile(Path.Join(SourceRootPath, "Info-catalyst.plist")));
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.ProductBundleIdentifier(
                    @"com.ubisoft.sharpmake.sample.metalwithstoryboard.catalyst"
                )
            );
            conf.SourceFilesBuildExcludeRegex.Add(@"Base\-appkit\.lproj");
        }
    }
}
