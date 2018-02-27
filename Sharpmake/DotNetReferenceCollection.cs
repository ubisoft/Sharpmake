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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sharpmake
{
    [DebuggerDisplay("{Include}")]
    public class DotNetReference : IComparable<DotNetReference>
    {
        public enum ReferenceType
        {
            Project,
            External,
            DotNet,
            DotNetExtensions
        }

        public ReferenceType Type { get; }
        public string Include { get; }
        public string LinkFolder { get; set; }
        public string HintPath { get; set; }
        public bool? SpecificVersion { get; set; }
        public bool? Private { get; set; }
        public bool? EmbedInteropTypes { get; set; }

        public DotNetReference(string include, ReferenceType type)
        {
            Include = include;
            Type = type;
        }

        public int CompareTo(DotNetReference other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var includeComparison = string.Compare(Include, other.Include, StringComparison.Ordinal);
            if (includeComparison != 0) return includeComparison;
            var linkFolderComparison = string.Compare(LinkFolder, other.LinkFolder, StringComparison.Ordinal);
            if (linkFolderComparison != 0) return linkFolderComparison;
            var specificVersionComparison = Nullable.Compare(SpecificVersion, other.SpecificVersion);
            if (specificVersionComparison != 0) return specificVersionComparison;
            var hintPathComparison = string.Compare(HintPath, other.HintPath, StringComparison.Ordinal);
            if (hintPathComparison != 0) return hintPathComparison;
            var privateComparison = Nullable.Compare(Private, other.Private);
            if (privateComparison != 0) return privateComparison;
            return Nullable.Compare(EmbedInteropTypes, other.EmbedInteropTypes);
        }
    }

    public class DotNetReferenceCollection : ICollection<DotNetReference>
    {
        private readonly UniqueList<DotNetReference> _references = new UniqueList<DotNetReference>();

        public int Count => _references.Count;
        public bool IsReadOnly { get; } = false;

        public IEnumerator<DotNetReference> GetEnumerator()
        {
            return _references.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IReadOnlyList<DotNetReference> SortedValues => _references.SortedValues;

        public void Add(DotNetReference reference)
        {
            _references.Add(reference);
        }

        public bool Remove(DotNetReference item)
        {
            return _references.Remove(item);
        }

        public void Clear()
        {
            _references.Clear();
        }

        public bool Contains(DotNetReference item)
        {
            return _references.Contains(item);
        }

        public void CopyTo(DotNetReference[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (arrayIndex + _references.Count >= array.Length)
                throw new ArgumentException(String.Format("Array too small : {0} expect minimum {1}", array.Length, arrayIndex + _references.Count), nameof(array));

            int i = 0;
            foreach (DotNetReference dotNetReference in _references)
            {
                array[arrayIndex + i] = dotNetReference;
                i++;
            }
        }
    }
}
