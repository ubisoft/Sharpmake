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

namespace SimpleNuGet.Impl
{
    /// <summary>
    /// Sorts text in a way Test9 comes before Test10
    /// </summary>
    internal class NumericString : IComparable
    {
        #region Variables

        private readonly string _value;
        private readonly IComparable[] _tokens;

        #endregion

        #region Constructor

        public NumericString(string value)
        {
            _value = value;
            _tokens = Tokenize(_value);
        }

        #endregion

        #region Properties

        public static NumericString Empty
        {
            get { return new NumericString(null); }
        }

        #endregion

        #region IComparable

        private static IComparable[] Tokenize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new IComparable[0];
            }

            List<IComparable> tokens = new List<IComparable>();
            int tokenStartIndex = 0;
            bool tokenIsNumeric = char.IsDigit(value[0]);

            for (int i = 1; i < value.Length; i++)
            {
                bool currentCharIsNumeric = char.IsDigit(value[i]);
                if (currentCharIsNumeric != tokenIsNumeric)
                {
                    tokens.Add(CreateToken(tokenStartIndex, i - 1, value, tokenIsNumeric));
                    tokenIsNumeric = currentCharIsNumeric;
                    tokenStartIndex = i;
                }
            }

            tokens.Add(CreateToken(tokenStartIndex, value.Length - 1, value, tokenIsNumeric));
            return tokens.ToArray();
        }

        private static IComparable CreateToken(int start, int end, string value, bool numeric)
        {
            string token = value.Substring(start, end - start + 1);
            if (numeric)
            {
                long tokenValue;
                if (long.TryParse(token, out tokenValue))
                {
                    return new NumberToken(tokenValue);
                }
            }
            return new StringToken(token);
        }

        public int CompareTo(object obj)
        {
            if (obj is string)
            {
                return CompareTo(new NumericString((string)obj));
            }
            return CompareTo(obj as NumericString);
        }

        public int CompareTo(NumericString other)
        {
            if (other == null)
            {
                return 1;
            }

            for (int i = 0; i < _tokens.Length && i < other._tokens.Length; i++)
            {
                int comparison = _tokens[i].CompareTo(other._tokens[i]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return _tokens.Length.CompareTo(other._tokens.Length);
        }

        public override string ToString()
        {
            return _value ?? string.Empty;
        }

        #endregion

        #region Nested

        private struct StringToken : IComparable
        {
            private readonly string _token;

            public StringToken(string token)
            {
                _token = token;
            }

            public int CompareTo(object obj)
            {
                if (obj is NumberToken)
                {
                    return 1;
                }

                return _token.CompareTo(((StringToken)obj)._token);
            }
        }

        private struct NumberToken : IComparable
        {
            private readonly long _token;

            public NumberToken(long token)
            {
                _token = token;
            }

            public int CompareTo(object obj)
            {
                if (obj is StringToken)
                {
                    return -1;
                }

                return _token.CompareTo(((NumberToken)obj)._token);
            }
        }

        #endregion
    }
}