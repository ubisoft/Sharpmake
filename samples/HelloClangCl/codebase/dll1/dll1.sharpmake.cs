// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.IO;

namespace HelloClangCl
{
    [Sharpmake.Generate]
    public class Dll1Project : CommonProject
    {
        public Dll1Project()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "dll1";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "SharedLibs";

            conf.PrecompHeader = "precomp.h";
            conf.PrecompSource = "precomp.cpp";

            conf.Output = Configuration.OutputType.Dll;

            conf.Defines.Add("UTIL_DLL_EXPORT");
            conf.ExportDefines.Add("UTIL_DLL_IMPORT");

            conf.IncludePaths.Add(SourceRootPath);

            string outputFileName = @"[conf.TargetPath]\[conf.TargetFileFullName].step_output.txt";
            conf.EventPostBuildExecute.Add(
                "sentinel_[conf.Name]",
                new Configuration.BuildStepExecutable(
                    Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                    @"[conf.TargetPath]\[conf.TargetFileFullNameWithExtension]",
                    outputFileName,
                    $@"/c ""echo dll linked > {outputFileName}""",
                    "[conf.TargetPath]"
                )
            );
            conf.EventPostBuild.Add($@"echo dll linked > {outputFileName}");

            conf.AddPrivateDependency<StaticLib1Project>(target);
        }
    }
}
