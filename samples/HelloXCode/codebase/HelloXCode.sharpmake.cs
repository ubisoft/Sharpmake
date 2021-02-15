// Copyright (c) 2020 Ubisoft Entertainment
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
    [Sharpmake.Generate]
    public class HelloXCodeSolution : CommonSolution
    {
        public HelloXCodeSolution()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "HelloXCode";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.AddProject<ExeProject>(target);
            conf.AddProject<Dll1Project>(target);
            conf.AddProject<StaticLib1Project>(target);
            conf.AddProject<StaticLib2Project>(target);
        }
    }
}
