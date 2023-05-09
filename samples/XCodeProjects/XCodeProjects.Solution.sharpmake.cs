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
        }

        public override void ConfigureMacOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureMacOS(conf, target);
            conf.AddProject<CLIToolProject>(target);
            conf.AddProject<MetalNoStoryboardProject>(target);
        }

        public override void ConfigureIOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureIOS(conf, target);
            conf.AddProject<CLIToolProject>(target);
            conf.AddProject<MetalNoStoryboardProject>(target);
        }

        public override void ConfigureTVOS(Configuration conf, CommonTarget target)
        {
            base.ConfigureTVOS(conf, target);
            conf.AddProject<CLIToolProject>(target);
            conf.AddProject<MetalNoStoryboardProject>(target);
        }

        public override void ConfigureCatalyst(Configuration conf, CommonTarget target)
        {
            base.ConfigureCatalyst(conf, target);
            conf.AddProject<CLIToolProject>(target);
            conf.AddProject<MetalNoStoryboardProject>(target);
        }
    }
}
