// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sharpmake
{
    // Cache of Regex.Match query results
    internal class RegexMatchCache
    {
        private readonly ConcurrentDictionary<Key, Match> _data;

        /// <param name="capacity">Initial cache capacity. Should not be divisible by a small prime number. Justification here: https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.-ctor?view=netcore-3.1. /// </param>
        public RegexMatchCache(int capacity = 0)
        {
            _data = new ConcurrentDictionary<Key, Match>(System.Environment.ProcessorCount, capacity, new KeyEqualityComparer());
        }

        public bool TryGet(string regex, string toEvaluate, out Match match)
        {
            return _data.TryGetValue(new Key(regex, toEvaluate), out match);
        }

        public void Add(string regex, string toEvaluate, Match match)
        {
            _data[new Key(regex, toEvaluate)] = match;
        }

        public int Count => _data.Count;

        private struct Key
        {
            public readonly string RegexStr;
            public readonly string ToEvaluate;

            public Key(string regexStr, string toEvaluate)
            {
                RegexStr = regexStr;
                ToEvaluate = toEvaluate;
            }

            public int HashCode => ComputeHashCode(RegexStr, ToEvaluate);

            private static int ComputeHashCode(string regexStr, string toEvaluate)
            {
                uint seed = 0u;
                unchecked
                {
                    HashCombine(ref seed, (uint)regexStr.GetHashCode());
                    HashCombine(ref seed, (uint)toEvaluate.GetHashCode());
                    return (int)seed;
                }
            }

            // Ref: Boost lib, container_hash/hash.hpp
            private static void HashCombine(ref uint h1, uint k1)
            {
                const uint c1 = 0xcc9e2d51;
                const uint c2 = 0x1b873593;

                k1 *= c1;
                k1 = Util.Rotl32(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1 = Util.Rotl32(h1, 13);
                h1 = h1 * 5 + 0xe6546b64;
            }
        }

        private class KeyEqualityComparer : EqualityComparer<Key>
        {
            public override bool Equals(Key x, Key y)
            {
                return x.RegexStr.Equals(y.RegexStr, System.StringComparison.Ordinal) &&
                    x.ToEvaluate.Equals(y.ToEvaluate, System.StringComparison.Ordinal);
            }

            public override int GetHashCode(Key obj)
            {
                return obj.HashCode;
            }
        }
    }

    public static class GlobalRegexMatchCache
    {
        private static RegexMatchCache s_instance;
        internal static RegexMatchCache Instance => s_instance;

        /// <param name="capacity">Initial cache capacity. Should not be divisible by a small prime number. Justification here: https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.-ctor?view=netcore-3.1. /// </param>
        public static void Init(int capacity)
        {
            s_instance = new RegexMatchCache(capacity);
        }

        public static void UnInit()
        {
            s_instance = null;
        }

        /// <summary>
        /// Returns size of cache ( i.e. the number of elements in the cache ).
        /// </summary>
        public static int Count => s_instance != null ? s_instance.Count : 0;
    }

    // CachedRegex is used where the same strings are likely to evaluate
    // multiple times.
    internal class CachedRegex
    {
        private readonly Regex _regex;

        public CachedRegex(Regex regex)
        {
            _regex = regex;
        }

        public Match Match(string toEvaluate)
        {
            Match match;
            var regex = _regex;
            var regexMatchCache = GlobalRegexMatchCache.Instance;
            if (regexMatchCache == null)
            {
                match = regex.Match(toEvaluate);
            }
            else
            {
                var regexStr = regex.ToString();
                if (regexMatchCache.TryGet(regexStr, toEvaluate, out match))
                {
                    return match;
                }

                match = regex.Match(toEvaluate);
                regexMatchCache.Add(regexStr, toEvaluate, match);
            }
            return match;
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

        private static string s_escapedWinSeparator = Regex.Escape(Util.WindowsSeparator.ToString());

        private static CachedRegex CreateCachedRegex(string expression)
        {
            if (Util.UsesUnixSeparator)
            {
                if (expression.Contains(@"\\", System.StringComparison.Ordinal))
                {
                    expression = expression.Replace(@"\\", Regex.Escape(Util.UnixSeparator.ToString()));
                }
            }
            else
            {
                if (expression.Contains('/', System.StringComparison.Ordinal))
                {
                    string oldExpression = expression;
                    // Ignore valid patterns that contain forward /
                    string refExpression = oldExpression.Replace(@"[/\\]", s_escapedWinSeparator)
                                                        .Replace(@"[\\/]", s_escapedWinSeparator);

                    // Handle the case where there are character escapes:
                    //   \/ is equivalent to /, but only if \ is not itself escaped.
                    expression = refExpression.Replace(s_escapedWinSeparator, @"/")  // First get the double backslashes out of the way (they will be converted back). Now the only backslashes left are not escaped.
                                              .Replace(@"\/", @"/")
                                              .Replace(@"/", s_escapedWinSeparator);

                    if (!string.Equals(refExpression, expression, System.StringComparison.Ordinal))
                        Util.LogWrite($"Warning: Converting regex to native separators, to avoid breaking cross-compilation on Windows: {oldExpression} changed to {expression}");
                }
            }

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
