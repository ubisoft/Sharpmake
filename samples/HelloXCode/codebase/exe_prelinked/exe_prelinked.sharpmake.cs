// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
