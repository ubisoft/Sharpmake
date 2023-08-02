// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace XCodeProjects
{
    [Sharpmake.Generate]
    public class XCodeProjectsSolution : CommonSolution
    {
        public XCodeProjectsSolution()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "XCodeProjects";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);
            conf.AddProject<CLIToolProject>(target);
            conf.AddProject<GUIToolProject>(target);
            conf.AddProject<MetalNoStoryboardProject>(target);
            conf.AddProject<MetalWithStoryboardProject>(target);

            conf.AddProject<FmtProject>(target);
            conf.AddProject<StoreKitProject>(target);
            conf.AddProject<BrightnessControlProject>(target);

            conf.AddProject<GetBrightnessProject>(target);
            conf.AddProject<SetBrightnessProject>(target);
            conf.AddProject<HasDebuggerProject>(target);
            conf.AddProject<SysInfoProject>(target);
            conf.AddProject<GotoVSCodeProject>(target);
            conf.AddProject<GotoXCodeProject>(target);
            conf.AddProject<ToPasteboardProject>(target);
            conf.AddProject<FromPasteboardProject>(target);
            conf.AddProject<ShellExecProject>(target);
            conf.AddProject<ShowInFinderProject>(target);
            conf.AddProject<OpenSettingsProject>(target);
            conf.AddProject<OpenAppStoreProject>(target);
            conf.AddProject<ReadAppDataProject>(target);

            conf.AddProject<SampleBundleProject>(target);
            conf.AddProject<HelloKitFrameworkProject>(target);
            conf.AddProject<HelloKitConsumerProject>(target);
        }

        public override void ConfigureMacOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureMacOS(conf, target);
        }

        public override void ConfigureIOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureIOS(conf, target);
        }

        public override void ConfigureTVOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureTVOS(conf, target);
        }

        public override void ConfigureCatalyst(Configuration conf, CommonTarget target)
        {
            base.ConfigureCatalyst(conf, target);
        }
    }
}
