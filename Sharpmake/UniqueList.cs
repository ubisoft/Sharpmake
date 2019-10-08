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
using System.Text;

namespace Sharpmake
{
    /// <summary>
    /// Same as Strings with configurable type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Size: {_hash.Count}")]
    public class UniqueList<T> : IEnumerable<T>
    {
        protected HashSet<T> _hash; // key is the string
        protected List<T> _values = new List<T>();  // Sorted keys are sorted on demand.
        private bool _readOnly = false;
        private bool _isDirty = false; // Does _values needs to be reconstructed ?
        private bool _isSorted = true; // Does _values is sorted ?
        private IComparer<T> _sortComparer = Comparer<T>.Default;

        public UniqueList()
            : this(EqualityComparer<T>.Default)
        {
        }

        public UniqueList(IEqualityComparer<T> comparer)
        {
            _hash = new HashSet<T>(comparer);
        }

        public UniqueList(IEqualityComparer<T> comparer, IEnumerable<T> other)
            : this(comparer)
        {
            AddRange(other);
        }

        public UniqueList(IEqualityComparer<T> comparer, params T[] values)
            : this(comparer)
        {
            AddRange(values);
        }

        public UniqueList(IEqualityComparer<T> comparer, UniqueList<T> other)
            : this(comparer)
        {
            _hash = new HashSet<T>(other._hash, comparer);
            _isDirty = true;
        }

        public void UpdateValue(T oldValue, T newValue)
        {
            if (!oldValue.Equals(newValue))
            {
                _hash.Remove(oldValue);
                _hash.Add(newValue);
                _isDirty = true;
            }
        }

        public void Add(T value1)
        {
            AddRange(new[] { value1 });
        }

        public void Add(T value1, T value2)
        {
            AddRange(new[] { value1, value2 });
        }

        public void Add(T value1, T value2, T value3)
        {
            AddRange(new[] { value1, value2, value3 });
        }

        public void Add(T value1, T value2, T value3, T value4)
        {
            AddRange(new[] { value1, value2, value3, value4 });
        }

        public void Add(T value1, T value2, T value3, T value4, T value5)
        {
            AddRange(new[] { value1, value2, value3, value4, value5 });
        }

        public void Add(params T[] values)
        {
            AddRange(values);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            ValidateReadOnly();
            _hash.UnionWith(collection);
            _isDirty = true;
        }

        public void IntersectWith(UniqueList<T> otherList)
        {
            var shortList = _hash.Count <= otherList._hash.Count ? this : otherList;
            var longList = _hash.Count > otherList._hash.Count ? this : otherList;
            var intersect = new HashSet<T>(_hash.Comparer);

            foreach (T key in shortList._hash)
            {
                if (longList._hash.Contains(key))
                {
                    // it is important to avoid using the .Add function on the UniqueList<T> rest because we want to preserve the original callerInfo in the dictionary
                    intersect.Add(key);
                }
            }
            _hash = intersect;
            _isDirty = true;
        }

        /// <param name="otherList">the other container to intersect with</param>
        /// <param name="rest">Contains elements in both containers that are did not intersect</param>
        public void IntersectWith(UniqueList<T> otherList, UniqueList<T> rest)
        {
            var shortList = _hash.Count <= otherList._hash.Count ? this : otherList;
            var longList = _hash.Count > otherList._hash.Count ? this : otherList;
            var intersect = new HashSet<T>(_hash.Comparer);

            foreach (T key in shortList._hash)
            {
                if (longList._hash.Contains(key))
                {
                    intersect.Add(key);
                }
                else
                {
                    rest.Add(key);
                }
            }

            foreach (T key in longList._hash)
            {
                if (!shortList._hash.Contains(key))
                {
                    rest.Add(key);
                }
            }


            _hash = intersect;
            _isDirty = true;
        }


        public int RemoveAll(Predicate<T> match)
        {
            ValidateReadOnly();
            int result = _hash.RemoveWhere(match);
            _isDirty |= result > 0;
            return result;
        }

        public void RemoveRange(IEnumerable<T> collection)
        {
            ValidateReadOnly();
            bool isDirty = false;
            foreach (T item in collection)
            {
                isDirty |= _hash.Remove(item);
            }
            _isDirty |= isDirty;
        }

        public void Remove(params T[] values)
        {
            RemoveRange(values);
        }

        public IComparer<T> SortComparer
        {
            get
            {
                return _sortComparer;
            }
            set
            {
                _sortComparer = value;
                _isDirty = true;
            }
        }

        private void UpdateValues()
        {
            lock (_values)
            {
                if (_isDirty)
                {
                    _values.Clear();
                    _values.AddRange(_hash);
                    _isSorted = false;
                    _isDirty = false;
                }
            }
            Debug.Assert(Count == _values.Count);
        }

        private void UpdateAndSortValues()
        {
            lock (_values)
            {
                if (_isDirty)
                {
                    _isSorted = false;
                    _values.Clear();
                    _values.AddRange(_hash);
                }

                if (!_isSorted)
                {
                    _values.Sort(_sortComparer);
                    _isSorted = true;
                }
                Debug.Assert(Count == _values.Count);
                _isDirty = false;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public List<T> Values
        {
            get
            {
                if (_isDirty)
                    UpdateValues();
                return _values;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public List<T> SortedValues
        {
            get
            {
                if (_isDirty || !_isSorted)
                    UpdateAndSortValues();
                return _values;
            }
        }

        public List<T> GetValuesWithCustomSort(Comparison<T> comparison)
        {
            List<T> items = new List<T>(Count);

            if (!_isDirty)
            {
                // If the sorted values is up to date, get the keys from there.
                Debug.Assert(Count == _values.Count);
                items.AddRange(_values);
            }
            else
            {
                items.AddRange(_hash);
            }

            // Sort the items
            items.Sort(comparison);
            return items;
        }

        #region IEnumerable

        public IEnumerator<T> GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region ICollection

        public void Clear()
        {
            ValidateReadOnly();
            _isDirty = true;
            _hash.Clear();
        }

        public bool Contains(T item)
        {
            return _hash.Contains(item);
        }


        public bool Remove(T item)
        {
            _isDirty = true;
            return _hash.Remove(item);
        }

        public int Count
        {
            get { return _hash.Count; }
        }

        internal void SetReadOnly(bool readOnly)
        {
            _readOnly = readOnly;
        }

        protected void ValidateReadOnly()
        {
            if (_readOnly)
            {
                throw new Error("Error: The list is readonly. Cannot modify the list during configuration.");
            }
        }

        public bool IsReadOnly
        {
            get { return _readOnly; }
        }

        #endregion

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(Count * 128);
            bool first = true;
            foreach (T value in _hash)
            {
                if (!first)
                    builder.Append(',');
                else
                    first = false;

                builder.Append(value.ToString());
            }

            return builder.ToString();
        }
    }
}
