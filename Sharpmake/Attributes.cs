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

namespace Sharpmake
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EntryPoint : Attribute
    { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class Main : EntryPoint
    { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class Generate : Attribute
    { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class Compile : Attribute
    { }

    // Only define settings for other project that depends on the current project ( include, lib, define, ... )
    // Often used for 3rd party library
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class Export : Attribute
    { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class Configure : Attribute
    {
        public object[] Flags { get; }

        public Configure()
        {
            Flags = null;
        }

        public Configure(params object[] flags)
        {
            Flags = flags;
        }
    }

    /// <summary>
    /// This method attribute is used to specify the execution order of Configure(...) within
    /// a project or a solution generation
    /// 
    /// If this attribute is not set a default value a 0 is used
    /// 
    /// The configure methods using this property are sorted ascendingly.
    /// ex: ... -1 before 0 before 1 ...
    /// 
    /// See Configure method attribute and Configurable class for more details
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ConfigurePriority : Attribute, IComparable<ConfigurePriority>
    {
        public static readonly ConfigurePriority DefaultPriority = new ConfigurePriority(0);

        #region Priority Property
        /// <summary>
        /// Priority level of the configure method
        /// </summary>
        public int Priority { get; }
        #endregion

        /// <summary>
        /// Priority attribute of a configure method
        /// </summary>
        /// <param name="priority">Priority level of the configure method</param>
        public ConfigurePriority(int priority)
        {
            Priority = priority;
        }

        public int CompareTo(ConfigurePriority other)
        {
            return Priority.CompareTo(other.Priority);
        }

        public override string ToString()
        {
            return Priority.ToString();
        }
    }


    [AttributeUsage(AttributeTargets.Enum)]
    public class Fragment : Attribute
    {
    }

    /// <summary>
    /// Marks elements of fragments that should not be considered individual fragments.
    /// </summary>
    /// <remarks>
    /// When an enumeration is marked with <see cref="Fragment"/>, Sharpmake normally ensure that
    /// each element sets 1 and only 1 bit. However, it is often useful in a bit enum to combine
    /// multiple bits together to create sets that go well together. To prevent Sharpmake from
    /// considering those errors, you must decorate these enum members with
    /// <see cref="CompositeFragmentAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class CompositeFragmentAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Enum)]
    public class TolerateDoubleAttribute : Attribute
    {
    }

    [System.AttributeUsage(AttributeTargets.Module, AllowMultiple = true)]
    public class Include : Attribute
    {
        public Include(string fileName) { }
    }

    [System.AttributeUsage(AttributeTargets.Module, AllowMultiple = true)]
    public class Reference : Attribute
    {
        public string FileName { get; }
        public Reference(string fileName)
        {
            FileName = fileName;
        }
    }

    [System.AttributeUsage(AttributeTargets.Module, AllowMultiple = true)]
    public class Package : Attribute
    {
        public string FileName { get; }
        public Package(string fileName)
        {
            FileName = fileName;
        }
    }

    [System.AttributeUsage(AttributeTargets.Module, AllowMultiple = false)]
    public class DebugProjectName : Attribute
    {
        public string Name { get; }
        public DebugProjectName(string name)
        {
            Name = name;
        }
    }
}
