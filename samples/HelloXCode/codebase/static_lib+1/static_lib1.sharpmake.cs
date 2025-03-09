// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using Sharpmake;

namespace HelloXCode
{
    [Sharpmake.Generate]
    public class StaticLib1Project : CommonProject
    {
        public StaticLib1Project()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "static_lib+1";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            // intentionally in a subfolder
            conf.PrecompHeader = @"src\pch.h";
            conf.PrecompSource = @"src\pch.cpp";

            conf.IncludePaths.Add(SourceRootPath);

            conf.SolutionFolder = "StaticLibs";

            switch (target.Optimization)
            {
                case Optimization.Debug:
                    conf.SourceFilesBuildExclude.Add(@"src\ensure_release.cpp");
                    break;
                case Optimization.Release:
                    // use a different method to exclude ensure_debug.cpp
                    conf.SourceFilesBuildExcludeRegex.Add(Util.RegexPathCombine("src", @"ensure_d.*\.cpp$"));
                    break;
                default:
                    throw new Error("Unexpected optimization " + target.Optimization);
            }

            conf.XcodeSystemFrameworks.Add("CoreFoundation");
        }
    }
}
