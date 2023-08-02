// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Export]
    public class StoreKitProject : CommonProject
    {
        public StoreKitProject()
        {
            Name = @"StoreKit";

            AddTargets(CommonTarget.GetMacTargets());
            AddTargets(CommonTarget.GetIosTargets());
            AddTargets(CommonTarget.GetTvosTargets());
            AddTargets(CommonTarget.GetCatalystTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);
            conf.ExportDefines.Add("USE_STOREKIT=1");
            conf.XcodeSystemFrameworks.Add("StoreKit");
            conf.Output = Configuration.OutputType.None;
        }
    }
}
