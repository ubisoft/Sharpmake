// Copyright (c) 2022 Ubisoft Entertainment
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

namespace HelloXCode
{
    /// <summary>
    /// This project tests the XCode's Pre-Linked libraries feature.
    /// </summary>
    [Sharpmake.Generate]
    public class ExePrelinkedProject : CommonProject
    {
        public ExePrelinkedProject()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "exe_prelinked";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.Output = Configuration.OutputType.Exe;

            conf.AddPrivateDependency<StaticPrelinkedLibConsumerProject>(target);

            conf.Defines.Add("CREATION_DATE=\"January 2022\"");
        }
    }
}
