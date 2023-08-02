// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Generate]
    public class HelloKitConsumerProject : CommonProject
    {
        public HelloKitConsumerProject()
        {
            Name = @"HelloKitConsumer";

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
            conf.AddPublicDependency<FmtProject>(target);
            conf.AddPublicDependency<HelloKitFrameworkProject>(
                target,
                DependencySetting.OnlyBuildOrder
            );

            conf.XcodeFrameworkPaths.Add(Globals.OutputDirectory, @"[conf.TargetPath]");

            conf.XcodeEmbeddedFrameworks.Add(Path.Combine(@"[conf.TargetPath]", "HelloKit.framework"));

            //adding `LD_RUNPATH_SEARCH_PATHS = "@executable_path/Frameworks @rpath/HelloKit.framework/Versions/A/HelloKit";`
            conf.Options.Add(new Options.XCode.Compiler.LdRunPaths(@"@executable_path/Frameworks @rpath/HelloKit.framework/Versions/A/HelloKit"));
        }
    }
}
