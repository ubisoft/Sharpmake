// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Linq;

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

        public bool HasSameFlags(Configure other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (Flags == null && other.Flags == null)
                return true;

            var groupedFlags = Flags.ToLookup(f => f.GetType(), f => f);
            var groupedOtherFlags = other.Flags.ToLookup(f => f.GetType(), f => f);

            // don't even bother to accumulate the values in case a flag type is not present in the other array
            foreach (var groupedFlag in groupedFlags)
            {
                if (!groupedOtherFlags.Contains(groupedFlag.Key))
                    return false;
            }

            // if we're here, we know all of the types in the first array are in the second, so iterate and merge the values
            foreach (var groupedFlag in groupedFlags)
            {
                int accumulate = 0;
                foreach (var flag in groupedFlag)
                    accumulate |= (int)flag;

                int otherAccumulate = 0;
                foreach (var flag in groupedOtherFlags[groupedFlag.Key])
                    otherAccumulate |= (int)flag;

                if (accumulate != otherAccumulate)
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            if (Flags == null)
                return "null";

            return string.Join(" ", Flags);
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
    /// Marks elements of a fragment enum that should not be considered individual fragments.
    /// </summary>
    /// <remarks>
    /// When an enumeration is marked with <see cref="Fragment"/>, Sharpmake normally ensures that
    /// each element sets 1 and only 1 bit. However, it is often useful in a bit enum to combine
    /// multiple bits together to create sets that go well together. To prevent Sharpmake from
    /// considering those errors, you must decorate these enum members with
    /// <see cref="CompositeFragmentAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class CompositeFragmentAttribute : Attribute
    {
    }

    /// <summary>
    /// Allows a field with a duplicate value in a fragment enum to be ignored.
    /// </summary>
    /// <remarks>
    /// When an enumeration is marked with <see cref="Fragment"/>, Sharpmake normally ensures that
    /// each element has a unique value. However, it is often useful in a bit enum to have special
    /// values that represent sections or limits. To prevent Sharpmake from considering those
    /// errors, you must decorate these enum members with
    /// <see cref="IgnoreDuplicateFragmentValueAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class IgnoreDuplicateFragmentValueAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Enum)]
    public class TolerateDoubleAttribute : Attribute
    {
    }

    public enum IncludeType
    {
        Relative,                   // Default, search the included file from the directory of the file doing the inclusion
        FarthestMatchInParentPath,  // Search the included file from the directory of the file doing the inclusion and go back in directory structure until a match is found, use the farthest match
        NearestMatchInParentPath,   // Search the included file from the directory of the file doing the inclusion and go back in directory structure until a match is found, use the nearest match
    };

    [System.AttributeUsage(AttributeTargets.Module, AllowMultiple = true)]
    public class Include : Attribute
    {
        public Include(string fileName) { }
        public Include(string fileName, IncludeType type) { }
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
