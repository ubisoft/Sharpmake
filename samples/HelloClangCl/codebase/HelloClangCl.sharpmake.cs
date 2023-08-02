// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace HelloClangCl
{
    [Sharpmake.Generate]
    public class HelloClangClSolution : CommonSolution
    {
        public HelloClangClSolution()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "HelloClangCl";
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
