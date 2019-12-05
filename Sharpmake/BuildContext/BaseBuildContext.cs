// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
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

        public abstract bool WriteLog { get; }
    }
}
