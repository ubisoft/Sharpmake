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
using System.Collections.Concurrent;
using System.ComponentModel;

namespace SimpleNuGet.Impl
{
    internal class TypeConverterCache
    {
        #region Variables

        private readonly ConcurrentDictionary<Type, TypeConverter> _typeConverters = new ConcurrentDictionary<Type, TypeConverter>();

        public static TypeConverterCache Instance { get; } = new TypeConverterCache();

        #endregion

        #region Methods

        public TypeConverter GetConverter(Type type)
        {
            return _typeConverters.GetOrAdd(type, TypeDescriptor.GetConverter);
        }

        public bool TryConvertToString(Type destination, object value, out string convertedValue)
        {
            try
            {
                convertedValue = GetConverter(destination).ConvertToInvariantString(value);
                return true;
            }
            catch
            {
                convertedValue = null;
                return false;
            }
        }

        public bool TryConvertFromString(Type originalType, string value, out object convertedValue)
        {
            try
            {
                convertedValue = GetConverter(originalType).ConvertFromInvariantString(value);
                return true;
            }
            catch
            {
                convertedValue = null;
                return false;
            }
        }

        #endregion
    }
}