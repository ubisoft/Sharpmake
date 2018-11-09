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

namespace Sharpmake
{
    /// <summary>
    /// Implementation of <see cref="IEqualityComparer{T}"/> that switches between case-sensitive
    /// to case-insensitive based on the operating system.
    /// </summary>
    /// <remarks>
    /// This class is a simple wrapper around either <see cref="StringComparer.Ordinal"/> (if the
    /// host operating system is Linux or Max OSX) or
    /// <see cref="StringComparer.OrdinalIgnoreCase"/> (on Windows operating systems.) You should
    /// use an instance of this class as the comparer when creating collections indexed on with
    /// file names.
    /// </remarks>
    public class FileSystemStringComparer : IComparer<string>, IEqualityComparer<string>
    {
        private static readonly bool s_hostOsIsCaseSensitive;

        static FileSystemStringComparer()
        {
            var operatingSystemFamily = Environment.OSVersion.Platform;
            s_hostOsIsCaseSensitive = (operatingSystemFamily == PlatformID.MacOSX || operatingSystemFamily == PlatformID.Unix);
        }

        private readonly object _comparer;         // Using System::Object as the type because this can be both IComparer or IEqualityComparer.

        /// <summary>
        /// Creates a new <see cref="FileSystemStringComparer"/> instance whose case sensitivity is
        /// the same as the case sensitivity of the host operating system's file system.
        /// </summary>
        public FileSystemStringComparer()
            : this(s_hostOsIsCaseSensitive)
        { }

        /// <summary>
        /// Creates a new <see cref="FileSystemStringComparer"/>.
        /// </summary>
        /// <param name="caseSensitive">The case-sensitivity mode to use in file name comparisons.</param>
        public FileSystemStringComparer(bool caseSensitive)
        {
            _comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        }

        public int Compare(string x, string y)
        {
            return ((IComparer<string>)_comparer).Compare(x, y);
        }

        public bool Equals(string x, string y)
        {
            return ((IEqualityComparer<string>)_comparer).Equals(x, y);
        }

        public int GetHashCode(string obj)
        {
            return ((IEqualityComparer<string>)_comparer).GetHashCode(obj);
        }
    }
}
