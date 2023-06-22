// Copyright (c) 2020, 2022 Ubisoft Entertainment
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
