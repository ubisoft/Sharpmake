// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Generate]
    public class BrightnessControlProject : CommonProject
    {
        public BrightnessControlProject()
        {
            Name = @"BrightnessControl";

            AddTargets(CommonTarget.GetMacTargets());
            AddTargets(CommonTarget.GetIosTargets());
            AddTargets(CommonTarget.GetTvosTargets());
            AddTargets(CommonTarget.GetCatalystTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.Lib;
            conf.IncludePaths.Add(SourceRootPath);
        }

        public override void ConfigureMacOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureMacOS(conf, target);

            conf.XcodeSystemFrameworks.Add("IOKit", "CoreDisplay", "DisplayServices");

            conf.XcodeFrameworkPaths.Add("/System/Library/PrivateFrameworks");

            conf.AdditionalLinkerOptions.Add(
                "-Wl,-U,_CoreDisplay_Display_SetUserBrightness",
                "-Wl,-U,_CoreDisplay_Display_GetUserBrightness",
                "-Wl,-U,_DisplayServicesCanChangeBrightness",
                "-Wl,-U,_DisplayServicesBrightnessChanged",
                "-Wl,-U,_DisplayServicesGetBrightness",
                "-Wl,-U,_DisplayServicesSetBrightness"
            );
        }

        public static void ApplyClientConfiguration(Configuration conf, CommonTarget target)
        {
            conf.XcodeFrameworkPaths.Add("/System/Library/PrivateFrameworks");

            conf.AdditionalLinkerOptions.Add(
                "-Wl,-U,_CoreDisplay_Display_SetUserBrightness",
                "-Wl,-U,_CoreDisplay_Display_GetUserBrightness",
                "-Wl,-U,_DisplayServicesCanChangeBrightness",
                "-Wl,-U,_DisplayServicesBrightnessChanged",
                "-Wl,-U,_DisplayServicesGetBrightness",
                "-Wl,-U,_DisplayServicesSetBrightness"
            );
        }
    }
}
