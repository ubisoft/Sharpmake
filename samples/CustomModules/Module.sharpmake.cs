// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Sharpmake;

namespace CustomModules
{
    // A module is not a project: it contributes its sources, include paths and defines
    // to every ModuleProject that consumes it through AddModule<T>().
    public abstract class Module
    {
        public string SharpmakeCsPath { get; }

        // Directory scanned for source files. Defaults to the folder of the module's sharpmake script.
        public virtual string SourcePath => SharpmakeCsPath;

        // Defines set in the embedding project and exported to the projects that depend on it.
        public virtual IEnumerable<string> PublicDefines => Array.Empty<string>();

        internal readonly List<Type> Dependencies = new List<Type>();

        // Derived modules must declare a constructor so Sharpmake can find their script from the call stack.
        protected Module()
        {
            string file;
            string baseFile;
            if (!Util.GetStackSourceFileTopMostTypeOf(GetType(), out file) ||
                !Util.GetStackSourceFileTopMostTypeOf(typeof(Module), out baseFile) ||
                file == baseFile)
                throw new Error("Cannot locate cs source for module {0}: it must declare a constructor.", GetType().Name);

            SharpmakeCsPath = Util.PathMakeStandard(new FileInfo(file).DirectoryName);
        }

        // Dependencies are transitive: whoever consumes this module also gets T.
        protected void AddDependency<T>() where T : Module, new()
        {
            Dependencies.Add(typeof(T));
        }

        // Called for every configuration of the embedding project, for settings that must stay private to it.
        public virtual void Configure(Project.Configuration conf, Target target)
        {
        }
    }

    // Sharpmake locks a project's source lists while it configures, so modules must be
    // attached in the project constructor.
    public abstract class ModuleProject : Project
    {
        // Dependencies come before the modules that use them.
        private readonly List<Module> _modules = new List<Module>();
        private readonly HashSet<Type> _moduleTypes = new HashSet<Type>();

        protected ModuleProject()
        {
            // Show each module's sharpmake script next to its sources.
            NoneExtensions.Add(".sharpmake.cs");
        }

        protected void AddModule<T>() where T : Module, new()
        {
            AddModule(typeof(T));
        }

        private void AddModule(Type moduleType)
        {
            if (!_moduleTypes.Add(moduleType))
                return;

            Module module = (Module)Activator.CreateInstance(moduleType);
            foreach (Type dependency in module.Dependencies)
                AddModule(dependency);

            _modules.Add(module);
            AdditionalSourceRootPaths.Add(module.SourcePath);
        }

        [Configure]
        public void ConfigureModules(Configuration conf, Target target)
        {
            foreach (Module module in _modules)
            {
                conf.IncludePaths.Add(module.SourcePath);
                foreach (string define in module.PublicDefines)
                {
                    conf.Defines.Add(define);
                    conf.ExportDefines.Add(define);
                }

                module.Configure(conf, target);
            }
        }
    }
}
