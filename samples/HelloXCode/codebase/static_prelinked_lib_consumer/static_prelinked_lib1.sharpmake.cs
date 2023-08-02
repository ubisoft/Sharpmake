// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Sharpmake;

namespace HelloXCode
{
    /// <summary>
    /// This project tests the XCode's Pre-Linked libraries feature.
    /// Project includes (but does not link) the sub library (consumed) that is then pre-linked by XCode
    /// the new PrelinkLibraries options. The EXE using this library will then be able to link the consumed library even though
    /// it is not in actually used in EXE.
    /// </summary>
    [Sharpmake.Generate]
    public class StaticPrelinkedLibConsumerProject : CommonProject
    {
        public StaticPrelinkedLibConsumerProject()
        {
            AddTargets(CommonTarget.GetDefaultTargets());
            Name = "static_prelinked_lib_consumer";
        }

        public override void ConfigureAll(Configuration conf, CommonTarget target)
        {
            base.ConfigureAll(conf, target);

            conf.SolutionFolder = "StaticLibs";

            conf.IncludePaths.Add(SourceRootPath);

            //  Important! do not link the Consumed library - let it be pre-linked by XCode
            conf.AddPrivateDependency<StaticPrelinkedLibConsumed>(target, DependencySetting.DefaultWithoutLinking);

            //  Custom build step - to generate the sub library
            var platform = target.GetPlatform().HasFlag(Platform.mac) ? "mac" : "ios";
            var projPath = Path.Combine(Globals.TmpDirectory, "projects/static_prelinked_lib_consumed");
            var configuration = target.Optimization.ToString().ToLowerInvariant();
            conf.EventPreBuild.Add($"xcodebuild build -scheme static_prelinked_lib_consumed_{platform} -project {projPath}/static_prelinked_lib_consumed_{platform}.xcodeproj -configuration {configuration}");

            //  Test pre-linked libraries
            var libraryToPrelink = Path.Combine(conf.TargetLibraryPath, "..", "static_prelinked_lib_consumed", "libstatic_prelinked_lib_consumed.a");

            conf.Options.Add(Options.XCode.Linker.PerformSingleObjectPrelink.Enable);
            conf.Options.Add(new Options.XCode.Linker.PrelinkLibraries(libraryToPrelink));
        }
    }
}
