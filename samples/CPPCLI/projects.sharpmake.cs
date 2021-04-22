// Copyright (c) 2017, 2019, 2021 Ubisoft Entertainment
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

            conf.AddPrivateDependency<OtherCSharpProj>(target, DependencySetting.OnlyBuildOrder);
            conf.AddPrivateDependency<TheEmptyCPPProject>(target);

            // Force full pdb otherwise we get this message: /DEBUG:FASTLINK is not supported when managed code is present; restarting link with /DEBUG:FULL
            conf.Options.Add(Options.Vc.Linker.GenerateFullProgramDatabaseFile.Enable);
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
