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
using System.Linq;

namespace Sharpmake
{
    /// <summary>
    /// Provides a static utility object of methods and properties to interact
    /// with enumerated types.
    /// </summary>
    public static class EnumExtensions
    {
        public static bool HasAnyFlag(this Enum enum1, Enum enum2)
        {
            return (Convert.ToUInt32(enum1) & Convert.ToUInt32(enum2)) != 0;
        }

        /// <summary>
        /// Sets a flag bit to 0 or 1
        /// </summary>
        public static TEnum SetFlag<TEnum>(this TEnum self, TEnum other, bool setBit)
            => setBit ? AddFlag(self, other) : RemoveFlag(self, other);

        /// <summary>
        /// Adds a flag to an enum
        /// </summary>
        public static TEnum AddFlag<TEnum>(this TEnum self, TEnum other)
        {
            return (TEnum)Enum.ToObject(self.GetType(), Convert.ToUInt32(self) | Convert.ToUInt32(other));
        }

        /// <summary>
        /// Removes a flag from an enum
        /// </summary>
        public static TEnum RemoveFlag<TEnum>(this TEnum self, TEnum other)
        {
            return (TEnum)Enum.ToObject(self.GetType(), Convert.ToUInt32(self) & ~Convert.ToUInt32(other));
        }

        /// <summary>
        /// Toggles a flag in an enum
        /// </summary>
        public static TEnum ToggleFlag<TEnum>(this TEnum enum1, TEnum enum2)
        {
            return (TEnum)Enum.ToObject(enum1.GetType(), Convert.ToUInt32(enum1) ^ Convert.ToUInt32(enum2));
        }

        /// <summary>
        /// Enumerates all flags set in the enum
        /// </summary>
        public static IEnumerable<TEnum> EnumerateFlags<TEnum>(this Enum input)
        {
            return Enum
                .GetValues(input.GetType())
                .Cast<Enum>()
                .Where(input.HasFlag)
                .EnumCast<TEnum>();
        }

        /// <summary>
        /// Aggregates an enumerable of enum flags into one
        /// </summary>
        public static TEnum ToFlags<TEnum>(this IEnumerable<TEnum> enums, TEnum defaultValue = default(TEnum)) => enums.Aggregate(defaultValue, AddFlag);

        /// <summary>
        /// Casts an generic Enum to a specific one
        /// </summary>
        public static TEnum EnumCast<TEnum>(this Enum self)
        {
            if (self.GetType() != typeof(TEnum))
                throw new InvalidCastException("Enums are not of the same type");
            return (TEnum)(object)self;
        }

        /// <summary>
        /// Casts an generic Enum to a specific one
        /// </summary>
        public static IEnumerable<TEnum> EnumCast<TEnum>(this IEnumerable<Enum> self) => self.Select(e => e.EnumCast<TEnum>());
    }

    public static class EnumUtils
    {
        /// <summary>
        /// Tries parsing a string as an enum type
        /// </summary>
        public static bool TryParse<TEnum>(string strEnumValue, out TEnum outValue)
        {
            if (string.IsNullOrEmpty(strEnumValue) || !Enum.IsDefined(typeof(TEnum), strEnumValue))
            {
                outValue = default(TEnum);
                return false;
            }

            outValue = (TEnum)Enum.Parse(typeof(TEnum), strEnumValue);

            return true;
        }

        /// <summary>
        /// Parses a string as an enum type. Returns the default value if it fails.
        /// </summary>
        public static TEnum ParseOrDefault<TEnum>(string strEnumValue, TEnum defaultValue = default(TEnum))
        {
            TEnum result;
            return TryParse(strEnumValue, out result) ? result : defaultValue;
        }

        /// <summary>
        /// Enumerates the values of an enum
        /// </summary>
        public static IEnumerable<TEnum> EnumerateValues<TEnum>() => Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
    }
}
