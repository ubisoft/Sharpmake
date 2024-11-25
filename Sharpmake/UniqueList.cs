// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Sharpmake
{
    /// <summary>
    /// Same as Strings with configurable type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Size: {_hash.Count}")]
    public class UniqueList<T> : IEnumerable<T>
    {
        private HashSet<T> _hash; // key is the string
        private List<T> _values = new List<T>();  // Sorted keys are sorted on demand.

        [Flags]
        enum ModifierBits
        {
            DirtyBit = 1, // Does _values need to be reconstructed ?
            SortedBit = 2,  // Is _values sorted ?
            ReadOnlyBit = 4, // Is the container read only ?
        }

        // Note: Sadly we have to use Int64 because Interlocked.Read only accepts a 64 bits value
        private Int64 _modifierBits = (Int64) ModifierBits.SortedBit; 

        private bool IsDirty
        {
            get
            {
                Int64 modifierBits = Interlocked.Read(ref _modifierBits);
                return (modifierBits & (Int64)ModifierBits.DirtyBit) != 0;
            }
            set
            {
                if (value)
                {
                    Interlocked.Or(ref _modifierBits, (Int64)ModifierBits.DirtyBit);
                }
                else
                {
                    Interlocked.And(ref _modifierBits, ~(Int64)ModifierBits.DirtyBit);
                }
            }
        }

        private bool IsSorted
        {
            get
            {
                Int64 modifierBits = Interlocked.Read(ref _modifierBits);
                return (modifierBits & (Int64)ModifierBits.SortedBit) != 0;
            }
            set
            {
                if (value)
                {
                    Interlocked.Or(ref _modifierBits, (Int64)ModifierBits.SortedBit);
                }
                else
                {
                    Interlocked.And(ref _modifierBits, ~(Int64)ModifierBits.DirtyBit);
                }
            }
        }

        private IComparer<T> _sortComparer = Comparer<T>.Default;

        public UniqueList()
            : this(EqualityComparer<T>.Default)
        {
        }
        public UniqueList(IComparer<T> sortComparer)
            : this(EqualityComparer<T>.Default)
        {
            _sortComparer = sortComparer;
        }

        public UniqueList(IEqualityComparer<T> comparer)
        {
            _hash = new HashSet<T>(comparer);
        }
        public UniqueList(IEqualityComparer<T> comparer, IComparer<T> sortComparer)
            : this(comparer)
        {
            _sortComparer = sortComparer;
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
            IsDirty = other.Count > 0;
        }

        public void UpdateValue(T oldValue, T newValue)
        {
            if (!oldValue.Equals(newValue))
            {
                _hash.Remove(oldValue);
                _hash.Add(newValue);
                IsDirty = true;
            }
        }

        private void AddCore(T value)
        {
            _hash.Add(value);
        }

        private void GrowCapacity(int by)
        {
            _hash.EnsureCapacity(_hash.Count + by);
        }

        public void Add(T value1)
        {
            ValidateReadOnly();
            AddCore(value1);
            IsDirty = true;
        }

        public void Add(T value1, T value2)
        {
            ValidateReadOnly();
            AddCore(value1);
            AddCore(value2);
            IsDirty = true;
        }

        public void Add(T value1, T value2, T value3)
        {
            ValidateReadOnly();
            AddCore(value1);
            AddCore(value2);
            AddCore(value3);
            IsDirty = true;
        }

        public void Add(T value1, T value2, T value3, T value4)
        {
            ValidateReadOnly();
            AddCore(value1);
            AddCore(value2);
            AddCore(value3);
            AddCore(value4);
            IsDirty = true;
        }

        public void Add(T value1, T value2, T value3, T value4, T value5)
        {
            ValidateReadOnly();
            AddCore(value1);
            AddCore(value2);
            AddCore(value3);
            AddCore(value4);
            AddCore(value5);
            IsDirty = true;
        }

        public void Add(params T[] values)
        {
            if (values.Length > 0)
            {
                AddRange(values);
            }
        }

        public void AddRange(IEnumerable<T> collection)
        {
            ValidateReadOnly();
            int countBeforeUnion = _hash.Count;
            _hash.UnionWith(collection);
            if (_hash.Count != countBeforeUnion)
                IsDirty = true;
        }

        public void AddRange(UniqueList<T> other)
        {
            ValidateReadOnly();
            if (other.Count > 0)
            {
                GrowCapacity(other.Count);

                foreach (T value in other._hash)
                {
                    AddCore(value);
                }

                IsDirty = true;
            }
        }

        public void AddRange(IReadOnlyList<T> collection)
        {
            ValidateReadOnly();
            if (collection.Count > 0)
            {
                GrowCapacity(collection.Count);

                for (int i = 0; i < collection.Count; i++)
                {
                    AddCore(collection[i]);
                }

                IsDirty = true;
            }
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
            IsDirty = true;
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
            IsDirty = true;
        }


        public int RemoveAll(Predicate<T> match)
        {
            ValidateReadOnly();
            int result = _hash.RemoveWhere(match);
            IsDirty |= result > 0;
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
            IsDirty |= isDirty;
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
        }

        private void UpdateValues()
        {
            lock (_values)
            {
                if (IsDirty)
                {
                    if (_values.Count > 0)
                        _values.Clear();
                    if (_hash.Count > 0)
                        _values.AddRange(_hash);

                    // Clear boths bits in one operation
                    Interlocked.And(ref _modifierBits, ~((Int64)ModifierBits.DirtyBit | (Int64)ModifierBits.SortedBit));
                }
            }
            Debug.Assert(Count == _values.Count);
        }

        private void UpdateAndSortValues()
        {
            lock (_values)
            {
                // Directly check the modifiers bits to save one interlocked read
                Int64 modifiers = Interlocked.Read(ref _modifierBits);

                if ((modifiers & (Int64)ModifierBits.DirtyBit) != 0)
                {
                    modifiers &= ~(Int64)ModifierBits.SortedBit; // Clear sorted bits locally
                    if (_values.Count > 0)
                       _values.Clear();
                    if (_hash.Count > 0)
                       _values.AddRange(_hash);
                }

                if ((modifiers & (Int64)ModifierBits.SortedBit) == 0)
                {
                    if (_values.Count > 0)
                        _values.Sort(_sortComparer);
                    IsSorted = true;
                }
                Debug.Assert(Count == _values.Count);

                if ((modifiers & (Int64)ModifierBits.DirtyBit) != 0)
                    IsDirty = false; // Only clear the dirty bit if it was set
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public List<T> Values
        {
            get
            {
                if (IsDirty)
                    UpdateValues();
                return _values;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public List<T> SortedValues
        {
            get
            {
                // Directly check the modifiers bits to save one interlocked read
                Int64 modifiers = Interlocked.Read(ref _modifierBits);
                if ((modifiers & (Int64)ModifierBits.DirtyBit) != 0 ||
                    (modifiers & (Int64)ModifierBits.SortedBit) == 0)
                {
                    UpdateAndSortValues();
                }
                return _values;
            }
        }

        public List<T> GetValuesWithCustomSort(Comparison<T> comparison)
        {
            List<T> items = new List<T>(Count);

            if (!IsDirty)
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

        public List<T>.Enumerator GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
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
            if (_hash.Count > 0)
            {
                IsDirty = true;
                _hash.Clear();
            }
        }

        public bool Contains(T item)
        {
            return _hash.Contains(item);
        }


        public bool Remove(T item)
        {
            IsDirty = true;
            return _hash.Remove(item);
        }

        public int Count
        {
            get { return _hash.Count; }
        }

        internal void SetReadOnly(bool readOnly)
        {
            if (readOnly)
                Interlocked.Or(ref _modifierBits, (Int64)ModifierBits.ReadOnlyBit);
            else
                Interlocked.And(ref _modifierBits, ~(Int64)ModifierBits.ReadOnlyBit);
        }

        protected void ValidateReadOnly()
        {
            if (IsReadOnly)
            {
                throw new Error("Error: The list is readonly. Cannot modify the list during configuration.");
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return (Interlocked.Read(ref _modifierBits) & (Int64)ModifierBits.ReadOnlyBit) != 0;
            }
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

        #region Internals
        internal void SetDirty()
        {
            IsDirty = true;
        }
        #endregion
    }
}
