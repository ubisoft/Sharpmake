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

using Sharpmake;
using System;

// This sample shows how to order the configure call within a project or a solution.

namespace ConfigureOrdering
{
    #region Configure method enumeration

    [Flags]
    public enum ConfigureMethod
    {
        None = 0,
        FooBar = 1,
        Foo = 2,
        Bar = 4
    }
    #endregion

    [Sharpmake.Generate]
    public class FooBarProject : Project
    {
        public FooBarProject()
        {
            AddTargets(Util.DefaultTarget);
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
        }

        /// <summary>
        /// Used to keep a track of executed configure methods
        /// </summary>
        protected ConfigureMethod executedMethodFlags = new ConfigureMethod();

        [Configure()]
        public virtual void Bar(Configuration conf, Target target)
        {
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]\[target.Name]";

            if (executedMethodFlags.Equals(ConfigureMethod.None))
            {
                conf.Defines.Add("FIRST_FooBarProject_Bar");
            }

            executedMethodFlags |= ConfigureMethod.Bar;
        }

        [Configure()]
        public virtual void Foo(Configuration conf, Target target)
        {
            if (executedMethodFlags.Equals(ConfigureMethod.Bar))
            {
                conf.Defines.Add("SECOND_FooBarProject_Foo");
            }

            executedMethodFlags |= ConfigureMethod.Foo;
        }
    }

    [Sharpmake.Generate]
    public class ParentProject : Project
    {
        public ParentProject()
        {
            AddTargets(Util.DefaultTarget);
            SourceRootPath = @"[project.SharpmakeCsPath]\codebase\[project.Name]";
        }

        protected ConfigureMethod executedMethodFlags = new ConfigureMethod();

        [Configure()]
        public virtual void Bar(Configuration conf, Target target)
        {
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]\[target.Name]";

            if (executedMethodFlags.Equals(ConfigureMethod.None))
            {
                conf.Defines.Add("FIRST_ParentProject_Bar");
            }

            executedMethodFlags |= ConfigureMethod.Bar;
        }

        [Configure()]
        public virtual void Foo(Configuration conf, Target target)
        {
            if (executedMethodFlags.Equals(ConfigureMethod.Bar))
            {
                conf.Defines.Add("SECOND_ParentProject_Foo");
            }

            executedMethodFlags |= ConfigureMethod.Foo;
        }
    }

    [Sharpmake.Generate]
    public class ChildProject : ParentProject
    {
        [Configure()]
        public virtual void FooBar(Configuration conf, Target target)
        {
            if (executedMethodFlags.Equals(ConfigureMethod.Foo))
            {
                conf.Defines.Add("SECOND_ChildProject_FooBar");
            }

            executedMethodFlags |= ConfigureMethod.FooBar;
        }

        [ConfigurePriority(2)]
        public override void Bar(Configuration conf, Target target)
        {
            if (executedMethodFlags.Equals(ConfigureMethod.Foo | ConfigureMethod.FooBar))
            {
                conf.Defines.Add("THIRD_ChildProject_Bar");
            }

            executedMethodFlags |= ConfigureMethod.Bar;
        }

        public override void Foo(Configuration conf, Target target)
        {
            conf.ProjectPath = @"[project.SharpmakeCsPath]\projects";
            conf.IntermediatePath = @"[conf.ProjectPath]\obj\[project.Name]\[target.Platform]\[target.Name]";

            if (executedMethodFlags.Equals(ConfigureMethod.None))
            {
                conf.Defines.Add("FIRST_ChildProject_Foo");
            }

            executedMethodFlags |= ConfigureMethod.Foo;
        }
    }

    #region Solution definition
    [Sharpmake.Generate]
    public class ConfigureOrderingSolution : Sharpmake.Solution
    {
        public ConfigureOrderingSolution()
        {
            AddTargets(Util.DefaultTarget);
        }

        [Configure()]
        public void ConfigureAll(Configuration conf, Target target)
        {
            conf.SolutionPath = @"[solution.SharpmakeCsPath]\projects";
            conf.AddProject<FooBarProject>(target);
            conf.AddProject<ParentProject>(target);
            conf.AddProject<ChildProject>(target);
        }
    }
    #endregion
}
