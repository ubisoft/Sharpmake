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
using System.Linq;
using System.Text;

namespace Sharpmake
{
    public class Strings : UniqueList<string>
    {
        public Strings(IEqualityComparer<string> hashComparer, IComparer<string> sortComparer)
            : base(hashComparer)
        {
            SortComparer = sortComparer;
        }
        public Strings()
            : base(StringComparer.OrdinalIgnoreCase)
        {
            SortComparer = StringComparer.OrdinalIgnoreCase;
        }

        public Strings(IEnumerable<string> other) : base(StringComparer.OrdinalIgnoreCase, other) { }
        public Strings(IEqualityComparer<string> comparer, IEnumerable<string> other) : base(comparer, other) { }

        public Strings(UniqueList<string> other) : base(StringComparer.OrdinalIgnoreCase, other) { }
        public Strings(IEqualityComparer<string> comparer, UniqueList<string> other) : base(comparer, other) { }

        public Strings(params string[] values) : base(StringComparer.OrdinalIgnoreCase, values) { }
        public Strings(IEqualityComparer<string> comparer, params string[] values) : base(comparer, values) { }

        public string Separator { get; set; } = ",";

        public string JoinStrings(string separator, bool escapeXml = false)
        {
            return JoinStrings(separator, "", "", escapeXml);
        }

        public string JoinStrings(string separator, string prefix, bool escapeXml = false)
        {
            return JoinStrings(separator, prefix, "", escapeXml);
        }

        public string JoinStrings(string separator, string prefix, string suffix, bool escapeXml = false)
        {
            return Util.JoinStrings(this.SortedValues, separator, prefix, suffix, escapeXml);
        }

        public void InsertPrefix(string prefix)
        {
            foreach (string value in Values)
            {
                UpdateValue(value, prefix + value);
            }
        }

        public void InsertSuffix(string suffix)
        {
            InsertSuffix(suffix, false);
        }

        public void InsertSuffix(string suffix, bool addOnlyIfAbsent)
        {
            foreach (string value in Values)
            {
                if (addOnlyIfAbsent && value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    continue;
                UpdateValue(value, value + suffix);
            }
        }

        public void InsertPrefixSuffix(string prefix, string suffix)
        {
            foreach (string value in Values)
            {
                UpdateValue(value, prefix + value + suffix);
            }
        }

        public void ToLower()
        {
            foreach (string value in Values)
            {
                UpdateValue(value, value.ToLower());
            }
        }

        public override string ToString()
        {
            return JoinStrings(Separator);
        }
    }

    public static class StringsExtension
    {
        public static void ForEach(this IEnumerable<Strings> strings, Action<Strings> action)
        {
            foreach (var elt in strings)
            {
                if (elt == null)
                    throw new NullReferenceException();

                action(elt);
            }
        }
    }

    /// <summary>
    /// Same as Strings but support to specify additionally an order number for every entry.
    /// Every entry has by default the value 0.  Putting negative values will result in the entries
    /// to be first after a sort, putting positive will result in the entries to be last after a
    /// sort.  Order numbers are kept when copying container to another OrderableStrings.
    /// It is forbidden to specify 2 different non-zero order numbers for the exact same string
    /// in 2 merged together OrderableStrings.
    /// </summary>
    public class OrderableStrings : IList<string>  // IList<string> for resolver
    {
        private HashSet<string> _hashSet = new HashSet<string>();

        private struct StringEntry : IComparable<StringEntry>
        {
            public StringEntry(string stringValue)
            {
                StringValue = stringValue;
                OrderNumber = 0;
            }
            public StringEntry(string stringValue, int orderNumber)
            {
                StringValue = stringValue;
                OrderNumber = orderNumber;
            }
            public string StringValue;
            public int OrderNumber;

            public override string ToString()
            {
                return StringValue;
            }

            #region IComparable Members

            public int CompareTo(StringEntry obj)
            {
                if (OrderNumber != obj.OrderNumber)
                {
                    if (OrderNumber != 0 && obj.OrderNumber != 0 && StringValue == obj.StringValue)
                        throw new Error("Cannot specify to different non-zero order values for same value \"" + StringValue + "\"");

                    return OrderNumber.CompareTo(obj.OrderNumber);
                }
                return string.Compare(StringValue, obj.StringValue, StringComparison.InvariantCultureIgnoreCase);
            }

            #endregion
        }

        private readonly List<StringEntry> _list = new List<StringEntry>();

        public OrderableStrings()
        {
        }

        public OrderableStrings(IEnumerable<string> strings)
        {
            AddRange(strings);
        }

        public OrderableStrings(OrderableStrings other)
        {
            _list.AddRange(other._list);
            _hashSet.UnionWith(other._hashSet);
        }

        public string JoinStrings(string separator)
        {
            return JoinStrings(separator, "", "");
        }

        public string JoinStrings(string separator, string prefix)
        {
            return JoinStrings(separator, prefix, "");
        }

        public string JoinStrings(string separator, string prefix, string suffix)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < _list.Count; ++i)
            {
                if (i != 0)
                    builder.Append(separator);
                builder.Append(prefix + _list[i] + suffix);
            }
            return builder.ToString();
        }

        public void InsertPrefix(string prefix)
        {
            for (int i = 0; i < _list.Count; ++i)
                _list[i] = new StringEntry(prefix + _list[i].StringValue, _list[i].OrderNumber);
            _hashSet.Clear();
            _hashSet.UnionWith(from i in _list select i.StringValue);
        }

        public void InsertSuffix(string suffix)
        {
            InsertSuffix(suffix, false);
        }

        public void InsertSuffix(string suffix, bool addOnlyIfAbsent)
        {
            for (int i = 0; i < _list.Count; ++i)
            {
                if (addOnlyIfAbsent && _list[i].StringValue.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    continue;
                _list[i] = new StringEntry(_list[i] + suffix, _list[i].OrderNumber);
            }
            _hashSet.Clear();
            _hashSet.UnionWith(from i in _list select i.StringValue);
        }

        public void InsertPrefixSuffix(string prefix, string suffix)
        {
            for (int i = 0; i < _list.Count; ++i)
                _list[i] = new StringEntry(prefix + _list[i].StringValue + suffix, _list[i].OrderNumber);
            _hashSet.Clear();
            _hashSet.UnionWith(from i in _list select i.StringValue);
        }

        public void Add(params string[] values)
        {
            foreach (string value in values)
            {
                Add(value);
            }
        }

        public void Add(string item)
        {
            if (_hashSet.Add(item))
                _list.Add(new StringEntry(item));
        }

        public void Add(string item, int orderNumber)
        {
            if (_hashSet.Add(item))
                _list.Add(new StringEntry(item, orderNumber));
            else if (orderNumber != 0)
            {
                // Make sure to have same number
                for (int i = 0; i < _list.Count; ++i)
                {
                    if (_list[i].StringValue == item)
                    {
                        if (_list[i].OrderNumber == 0)
                            _list[i] = new StringEntry(item, orderNumber);
                        else if (_list[i].OrderNumber != orderNumber)
                        {
                            throw new Error(
                                "Cannot specify 2 different non-zero order number for \"" +
                                item + "\": " + _list[i].OrderNumber + " and " + orderNumber);
                        }
                    }
                }
            }
        }

        public void AddRange(IEnumerable<string> collection)
        {
            foreach (var item in collection)
                Add(item);
        }

        public void AddRange(OrderableStrings collection)
        {
            List<StringEntry> existingEntriesToAdd = null;
            foreach (var entry in collection._list)
            {
                if (_hashSet.Add(entry.StringValue))
                    _list.Add(entry);
                else if (entry.OrderNumber != 0)  // make sure to have orderNumber
                {
                    if (existingEntriesToAdd == null)
                        existingEntriesToAdd = new List<StringEntry>();
                    existingEntriesToAdd.Add(entry);
                }
            }
            if (existingEntriesToAdd != null)
            {
                Dictionary<string, int> dict = GetStringToOrderNumberDictionary(existingEntriesToAdd);
                for (int i = 0; i < _list.Count; ++i)
                {
                    int orderNumber;
                    if (dict.TryGetValue(_list[i].StringValue, out orderNumber))
                    {
                        if (_list[i].OrderNumber == 0)
                            _list[i] = new StringEntry(_list[i].StringValue, orderNumber);
                        else if (_list[i].OrderNumber != orderNumber)
                        {
                            throw new Error(
                                "Cannot specify 2 different non-zero order number for \"" +
                                _list[i].StringValue + "\": " + _list[i].OrderNumber + " and " + orderNumber);
                        }
                    }
                }
            }
        }

        private static Dictionary<string, int> GetStringToOrderNumberDictionary(List<StringEntry> entries)
        {
            var dict = new Dictionary<string, int>();
            foreach (var entry in entries)
            {
                dict[entry.StringValue] = entry.OrderNumber;
            }
            return dict;
        }

        public void IntersectWith(IEnumerable<string> collection)
        {
            _list.Clear();
            foreach (string item in collection)
            {
                if (_hashSet.Contains(item))
                    _list.Add(new StringEntry(item));
            }
            _hashSet.Clear();
            _hashSet.UnionWith(from i in _list select i.StringValue);
        }

        /// <param name="collection">The collection to intersect with</param>
        /// <param name="rest">Contains elements in both containers that are did not intersect</param>
        public void IntersectWith(IEnumerable<string> collection, Strings rest)
        {
            var values = new Dictionary<string, int>();
            foreach (var entry in _list)
                values[entry.StringValue] = entry.OrderNumber;
            _list.Clear();
            foreach (string item in collection)
            {
                if (_hashSet.Contains(item))
                    _list.Add(new StringEntry(item, values[item]));
                else
                    rest.Add(item);
            }
            var previousHashSet = _hashSet;
            _hashSet = new HashSet<string>();
            _hashSet.UnionWith(from i in _list select i.StringValue);
            previousHashSet.ExceptWith(from i in _list select i.StringValue);
            foreach (string item in previousHashSet)
            {
                rest.Add(item);
            }
        }

        public bool Remove(string item)
        {
            if (_hashSet.Remove(item))
            {
                if (!_list.Remove(new StringEntry(item)))
                {
                    for (int i = 0; i < _list.Count; ++i)
                    {
                        if (_list[i].StringValue == item)
                        {
                            _list.RemoveAt(i);
                            break;
                        }
                    }
                }
                return true;
            }
            return false;
        }
        public void RemoveRange(IEnumerable<string> collection)
        {
            foreach (string item in collection)
            {
                if (_hashSet.Remove(item))
                {
                    if (!_list.Remove(new StringEntry(item)))
                    {
                        // Idiot to loop, but this is a ridiculous edge case
                        for (int i = 0; i < _list.Count; ++i)
                        {
                            if (_list[i].StringValue == item)
                            {
                                _list.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void ToLower()
        {
            for (int i = 0; i < _list.Count; ++i)
                _list[i] = new StringEntry(_list[i].StringValue.ToLower(), _list[i].OrderNumber);
            _hashSet.Clear();
            _hashSet.UnionWith(from i in _list select i.StringValue);
        }

        public void Sort()
        {
            _list.Sort();
        }

        private int StableSortCompare(StringEntry e1, StringEntry e2)
        {
            if (e1.OrderNumber != e2.OrderNumber)
            {
                if (e1.OrderNumber < e2.OrderNumber)
                    return -1;
                else
                    return 1;
            }
            return 0;
        }

        public void StableSort()
        {
            if (_list.Any(entry => entry.OrderNumber != 0))
            {
                int count = _list.Count;
                for (int j = 1; j < count; j++)
                {
                    StringEntry key = _list[j];

                    int i = j - 1;
                    for (; i >= 0 && StableSortCompare(_list[i], key) > 0; i--)
                    {
                        _list[i + 1] = _list[i];
                    }
                    _list[i + 1] = key;
                }
            }
        }

        public bool Contains(string value)
        {
            return _hashSet.Contains(value);
        }

        public override string ToString()
        {
            return JoinStrings(",");
        }

        #region ICollection<string> Members

        public void Clear()
        {
            _hashSet.Clear();
            _list.Clear();
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            var list = new List<string>(from i in _list select i.StringValue);
            list.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region IEnumerable<string> Members

        public IEnumerator<string> GetEnumerator()
        {
            return (from i in _list select i.StringValue).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        #endregion

        #region IList<string> Members

        public int IndexOf(string item)
        {
            return _list.IndexOf(new StringEntry(item, 0));
        }

        public void Insert(int index, string value)
        {
            if (_hashSet.Add(value))
                _list.Insert(index, new StringEntry(value, 0));
        }

        public void RemoveAt(int index)
        {
            _hashSet.Remove(_list[index].StringValue);
            _list.RemoveAt(index);
        }

        public string this[int index]
        {
            get
            {
                return _list[index].StringValue;
            }
            set
            {
                if (_list[index].StringValue != value)
                {
                    // Assuming this is called to update same value
                    int orderNumber = _list[index].OrderNumber;

                    Debug.Assert(!_hashSet.Contains(value));
                    _hashSet.Remove(_list[index].StringValue);
                    _hashSet.Add(value);
                    _list[index] = new StringEntry(value, orderNumber);
                }
            }
        }

        public int GetOrderNumber(int index)
        {
            return _list[index].OrderNumber;
        }

        // When doing a for loop to change all values, like resolving them, it's
        // possible to end up with duplicates after resolve.  This function can
        // be called to set value at index, remove it if already there and return
        // index ready to be incremented.
        public int SetOrRemoveAtIndex(int index, string value)
        {
            if (_list[index].StringValue != value)
            {
                // Assuming this is called to update same value
                int orderNumber = _list[index].OrderNumber;

                if (_hashSet.Contains(value))
                {
                    // Already there, remove
                    _hashSet.Remove(_list[index].StringValue);
                    _list.RemoveAt(index);
                    return index - 1;
                }
                else
                {
                    _hashSet.Remove(_list[index].StringValue);
                    _hashSet.Add(value);
                    _list[index] = new StringEntry(value, orderNumber);
                }
            }
            return index;
        }
        #endregion
    }
}

