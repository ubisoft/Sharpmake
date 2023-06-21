// Copyright (c) 2022-2023 Ubisoft Entertainment
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
