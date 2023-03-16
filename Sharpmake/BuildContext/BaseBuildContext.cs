// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Sharpmake.BuildContext
{
    public abstract class BaseBuildContext
    {
        /// <summary>
        /// Creates and enumerable of Configure(...) method info based on the given type.
        /// Useful to have different sorts to support old Sharpmake versions or to analyze
        /// dependencies between Configure functions.
        /// </summary>
        /// <param name="type">type on which to get the configure methods</param>
        /// <returns>an ordered enumeration of method info</returns>
        public IEnumerable<MethodInfo> CreateConfigureCollection(Type type)
        {
            return ConfigureCollection.Create(type, ConfigureOrder, OrderConfigure);
        }

        public ConfigureOrder ConfigureOrder = ConfigureOrder.New;

        internal virtual IEnumerable<MethodInfo> OrderConfigure(Type type, ConfigurePriority priority, IEnumerable<MethodInfo> methods)
        {
            return methods;
        }

        public virtual bool HaveToGenerate(Type type) { return true; }

        public abstract bool WriteGeneratedFile(Type type, FileInfo path, MemoryStream generated);
        public abstract bool WriteGeneratedFile(Type type, FileInfo path, IFileGenerator generator);

        public abstract bool WriteLog { get; }
    }
}
