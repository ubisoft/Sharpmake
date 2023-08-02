// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Generate]
    public class GetBrightnessProject : CommonProject
    {
        public GetBrightnessProject()
        {
            Name = @"GetBrightness";

            AddTargets(CommonTarget.GetMacTargets());
            AddTargets(CommonTarget.GetIosTargets());
            AddTargets(CommonTarget.GetTvosTargets());
            AddTargets(CommonTarget.GetCatalystTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.Exe;
            conf.AddPublicDependency<FmtProject>(target);
            conf.AddPublicDependency<BrightnessControlProject>(target, DependencySetting.Default);
            //! invoking a configuration callback
            BrightnessControlProject.ApplyClientConfiguration(conf, target);
        }
    }
}
