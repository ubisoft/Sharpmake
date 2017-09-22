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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sharpmake
{
    // CachedRegex is used where the same strings are likely to evaluate
    // multiple times.
    internal class CachedRegex
    {
        private readonly  Regex _regex;
        public CachedRegex(Regex regex) { _regex = regex; }

        public Match Match(string toEvaluate)
        {
            return _regex.Match(toEvaluate);
        }
    }

    // The RegexCache is used where the same regex expression is likely to be compiled
    // multiple times.  It comes also with the advantages of CachedRegex.
    internal static class RegexCache
    {
        private static ConcurrentDictionary<string, CachedRegex> s_cachedRegexes = new ConcurrentDictionary<string, CachedRegex>();

        public static CachedRegex GetRegex(string expression)
        {
            return s_cachedRegexes.GetOrAdd(expression, CreateCachedRegex);
        }

        private static CachedRegex CreateCachedRegex(string expression)
        {
            if (Util.UsesUnixSeparator && expression.IndexOf(@"\\", System.StringComparison.Ordinal) >= 0)
                expression = expression.Replace(@"\\", Regex.Escape(Util.UnixSeparator.ToString()));

            return new CachedRegex(
                new Regex(
                    expression,
                    RegexOptions.Compiled |
                    RegexOptions.Singleline |
                    RegexOptions.CultureInvariant |
                    RegexOptions.IgnoreCase
                )
            );
        }

        public static IEnumerable<CachedRegex> GetCachedRegexes(Strings regexes)
        {
            return regexes.Select(expression => GetRegex(expression));
        }
    }
}
