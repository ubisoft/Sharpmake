// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sharpmake;

namespace CLR_SharpmakeTest
{
    public class CommonProject : Project
    {
        public CommonProject()
        {
            RootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            SourceRootPath = @"[project.RootPath]";
            AddTargets(Common.CommonTarget);
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects\";
            conf.ProjectFileName = @"[project.Name].[target.DevEnv].[target.Framework]";
            conf.IntermediatePath = @"[conf.ProjectPath]\temp\[project.Name]\[target.DevEnv]\[target.Framework]\[conf.Name]";

            conf.Output = Configuration.OutputType.DotNetClassLibrary;
            if (target.Optimization == Optimization.Debug)
                conf.Options.Add(Options.Vc.Compiler.RuntimeLibrary.MultiThreadedDebugDLL);
            else
                conf.Options.Add(Options.Vc.Compiler.RuntimeLibrary.MultiThreadedDLL);
        }
    }

    public class CommonCSharpProject : CSharpProject
    {
        public CommonCSharpProject()
        {
            RootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
            SourceRootPath = @"[project.RootPath]";

            AddTargets(Common.CommonTarget);
        }

        [Configure()]
        public virtual void ConfigureAll(Configuration conf, Target target)
        {
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.ProjectFileName = @"[project.Name].[target.DevEnv].[target.Framework]";
            conf.TargetPath = @"[conf.ProjectPath]\output\[target.DevEnv]\[target.Framework]\[conf.Name]";
            conf.IntermediatePath = @"[conf.ProjectPath]\temp\[project.Name]\[target.DevEnv]\[target.Framework]\[conf.Name]";
            conf.Output = Configuration.OutputType.DotNetClassLibrary;
            if (target.Optimization == Optimization.Debug)
                conf.Options.Add(Options.Vc.Compiler.RuntimeLibrary.MultiThreadedDebugDLL);
            else
                conf.Options.Add(Options.Vc.Compiler.RuntimeLibrary.MultiThreadedDLL);

            conf.Options.Add(Sharpmake.Options.CSharp.TreatWarningsAsErrors.Enabled);
        }
    }

    [Sharpmake.Generate]
    public class TestCSharpConsole : CommonCSharpProject
    {
        public TestCSharpConsole() { }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.DotNetConsoleApp;
            conf.AddPrivateDependency<CLR_CPP_Proj>(target);
        }
    }

    [Sharpmake.Generate]
    public class OtherCSharpProj : CommonCSharpProject
    {
        public OtherCSharpProj() { }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.DotNetClassLibrary;
        }
    }

    [Sharpmake.Generate]
    public class CSharpProjBuildOrderDependency : CommonCSharpProject
    {
        public CSharpProjBuildOrderDependency() { }
    }

    [Sharpmake.Generate]
    public class CLR_CPP_Proj : CommonProject
    {
        public CLR_CPP_Proj()
        {
            Name = "CLRCPPProj";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);

            conf.ReferencesByName.Add(
                "System",
                "System.Data",
                "System.Xml"
            );

            conf.AddPrivateDependency<OtherCSharpProj>(target, DependencySetting.DefaultWithoutCopy);
            conf.AddPrivateDependency<CSharpProjBuildOrderDependency>(target, DependencySetting.OnlyBuildOrder);
            conf.AddPrivateDependency<TheEmptyCPPProject>(target);

            // Force full pdb otherwise we get this message: /DEBUG:FASTLINK is not supported when managed code is present; restarting link with /DEBUG:FULL
            conf.Options.Add(Options.Vc.Linker.GenerateFullProgramDatabaseFile.Enable);

            // Force RTTI to be enabled
            conf.Options.Add(Sharpmake.Options.Vc.Compiler.RTTI.Enable);

            conf.Options.Add(Sharpmake.Options.Vc.General.CommonLanguageRuntimeSupport.ClrSupport);
        }
    }

    [Sharpmake.Generate]
    public class TheEmptyCPPProject : CommonProject
    {
        public TheEmptyCPPProject()
        {
            Name = "theEmptyCPPProject";
        }

        public override void ConfigureAll(Configuration conf, Target target)
        {
            base.ConfigureAll(conf, target);
            conf.Output = Configuration.OutputType.Lib;
            conf.Options.Add(Options.Vc.Compiler.Exceptions.EnableWithSEH);
        }
    }
}
