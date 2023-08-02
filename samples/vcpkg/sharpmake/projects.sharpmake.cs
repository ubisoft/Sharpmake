// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace VCPKGSample
{
    [Sharpmake.Generate]
    public class VCPKGSample : CommonProject
    {
        public VCPKGSample()
        {
            Name = "VCPKGSample";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.Exe;
            conf.Options.Add(Options.Vc.Compiler.Exceptions.Enable);

            conf.AddPrivateDependency<Extern.Curl>(target);
            conf.AddPrivateDependency<Extern.RapidJSON>(target);
        }
    }

    [Sharpmake.Generate]
    public class VCPKGSampleSolution : Sharpmake.Solution
    {
        public VCPKGSampleSolution()
        {
            Name = "VCPKGSample";

            AddTargets(SampleTargets.Targets);
            ExtraItems.Add("Markdown", new Strings()
            {
                @"[solution.SharpmakeCsPath]/../README.md",
            });
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionFileName = "[solution.Name]_[target.DevEnv]";
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\..\tmp\projects";
            if ((target.BuildSystem & BuildSystem.FastBuild) != 0)
                conf.Name += "_FastBuild";
            conf.AddProject<VCPKGSample>(target);
        }
    }
}
