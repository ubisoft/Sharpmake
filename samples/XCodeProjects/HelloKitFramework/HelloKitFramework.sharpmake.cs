// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Generate]
    public class HelloKitFrameworkProject : CommonProject
    {
        public HelloKitFrameworkProject()
        {
            Name = @"HelloKit";

            AddTargets(CommonTarget.GetMacTargets());
            AddTargets(CommonTarget.GetIosTargets());
            AddTargets(CommonTarget.GetTvosTargets());
            AddTargets(CommonTarget.GetCatalystTargets());

            SourceRootPath = Util.GetCurrentSharpmakeFileInfo().DirectoryName;
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.AppleFramework;
            conf.AddPrivateDependency<FmtProject>(target);
            conf.Options.Add(
                new Sharpmake.Options.XCode.Compiler.InfoPListFile(
                    Path.Join(SourceRootPath, "Info-Framework.plist")
                )
            );
        }

        public override void ConfigureMacOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureMacOS(conf, target);
            conf.Options.Add(new Options.XCode.Compiler.ProductInstallPath(@"@executable_path/../Frameworks"));
        }

        public override void ConfigureIOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureIOS(conf, target);
            conf.Options.Add(new Options.XCode.Compiler.ProductInstallPath(@"@executable_path/Frameworks"));
        }

        public override void ConfigureTVOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureTVOS(conf, target);
            conf.Options.Add(new Options.XCode.Compiler.ProductInstallPath(@"@executable_path/Frameworks"));
        }

        public override void ConfigureCatalyst(Configuration conf, CommonTarget target)
        {
            base.ConfigureCatalyst(conf, target);
            conf.Options.Add(new Options.XCode.Compiler.ProductInstallPath(@"@executable_path/Frameworks"));
        }
    }
}
