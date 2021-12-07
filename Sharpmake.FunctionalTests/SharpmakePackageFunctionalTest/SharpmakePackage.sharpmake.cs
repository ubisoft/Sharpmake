using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;

namespace SharpmakeGen.FunctionalTests
{
    public abstract class LibProjectBase : CSharpProject
    {
        public LibProjectBase()
            : base(typeof(Target))
        {
            RootPath = @"[project.SharpmakeCsPath]";
            SourceRootPath = @"[project.RootPath]\codebase\[project.Name]";
        }

        [Configure]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectFileName = "[project.Name]_[target.DevEnv]_[target.Platform]";
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";

            conf.Output = Configuration.OutputType.DotNetClassLibrary;

            conf.IntermediatePath = @"[conf.ProjectPath]\build\[conf.Name]\[project.Name]";
            conf.TargetPath = @"[conf.ProjectPath]\output\[conf.Name]";

            // .lib files must be with the .obj files when running in fastbuild distributed mode or we'll have missing symbols due to merging of the .pdb
            conf.TargetLibraryPath = "[conf.IntermediatePath]";
        }
    }
}