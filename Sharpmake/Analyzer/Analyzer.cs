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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Sharpmake.BuildContext;

namespace Sharpmake.Analyzer
{
    /// <summary>
    /// Contains methods for specific analysis on a sharpmake DLL
    /// </summary>
    public static class Analyzer
    {
        /// <summary>
        /// Analyzes configure methods to find all the Configure() dependent on the call order within the same priority 
        /// </summary>
        /// <param name="builderFactory">a Functor creator of builder</param>
        /// <param name="stopOnFirstError"></param>
        /// <returns>a collection of the method dependent on any other methods</returns>
        public static IEnumerable<ConfigureMethodInfo> AnalyzeConfigure(Func<BaseBuildContext, Builder> builderFactory, bool stopOnFirstError)
        {
            var stopwatch = Stopwatch.StartNew();
            var analyzer = new ConfigureDependencyAnalyzer(
                context =>
                {
                    using (Builder builder = builderFactory(context))
                    {
                        return builder.Generate();
                    }
                });

            var suspectedMethods = analyzer.Analyze(Console.Out, stopOnFirstError);

            Console.WriteLine();
            Console.WriteLine(stopwatch.Elapsed);
            return CreateReport(suspectedMethods).Where(r => !r.Dependencies.Any());
        }

        private static IEnumerable<ConfigureMethodInfo> CreateReport(IEnumerable<ConfigureSignature> toOrder)
        {
            var cache = new Dictionary<Tuple<int, Type>, ConfigureMethodInfo>();
            foreach (var configure in toOrder)
            {
                var type = configure.DeclaringType;
                var priority = configure.Priority;

                var dependency = CacheIn(cache, Tuple.Create(type, priority, configure.DependencySignature));
                var dependent = CacheIn(cache, Tuple.Create(type, priority, configure.Signature));

                dependency.AddDependent(dependent);
            }

            return cache.Values;
        }

        private static ConfigureMethodInfo CacheIn(IDictionary<Tuple<int, Type>, ConfigureMethodInfo> cache, Tuple<Type, ConfigurePriority, string> configureInfo)
        {
            var configure = new ConfigureMethodInfo(configureInfo);
            var configureKey = Tuple.Create(configure.Method.MetadataToken, configure.Type);
            return cache.GetValueOrAdd(configureKey, configure);
        }
    }

    /// <summary>
    /// Typical wrapper for a Configure MethodInfo
    /// Allows to keep a track of the dependency and the dependents of a configure method
    /// </summary>
    public class ConfigureMethodInfo
    {
        public readonly Type Type;
        public readonly MethodInfo Method;
        public readonly ConfigurePriority Priority;

        public IEnumerable<ConfigureMethodInfo> Origin { get { return _origin; } }
        private IList<ConfigureMethodInfo> _origin = new List<ConfigureMethodInfo>();

        public readonly IEnumerable<Type> MethodAncestorTypes;
        private readonly IDictionary<int, IList<Type>> _metadataTypeMap;

        public IEnumerable<ConfigureMethodInfo> Dependencies { get { return _dependencies; } }
        private readonly ISet<ConfigureMethodInfo> _dependencies = new HashSet<ConfigureMethodInfo>();

        public IEnumerable<ConfigureMethodInfo> Dependents { get { return _dependents; } }
        private readonly ISet<ConfigureMethodInfo> _dependents = new HashSet<ConfigureMethodInfo>();

        public ConfigureMethodInfo(Tuple<Type, ConfigurePriority, string> method)
        {
            var configures = ConfigureCollection.Create(method.Item1);
            Method = configures.Single(m => m.ToString() == method.Item3);
            _metadataTypeMap = GetAncestorsWithMethod(method.Item1, Method.ToString());
            MethodAncestorTypes = _metadataTypeMap.SelectMany(types => types.Value);
            Type = _metadataTypeMap[Method.MetadataToken].Last();
            Priority = method.Item2;
        }

        private static IDictionary<int, IList<Type>> GetAncestorsWithMethod(Type type, string signature)
        {
            var metaType = new Dictionary<int, IList<Type>>();
            MethodInfo method;
            while ((method = ConfigureCollection.Create(type).SingleOrDefault(m => m.ToString() == signature)) != null)
            {
                metaType.AddOrUpdateValue(method.MetadataToken, new List<Type> { type },
                    (key, oldT, newT) =>
                    {
                        foreach (var t in newT)
                            oldT.Add(t);

                        return oldT;
                    });

                type = type.BaseType;
            }

            return metaType;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ConfigureMethodInfo;
            return other != null && Method.MetadataToken == other.Method.MetadataToken;
        }

        internal void AddDependent(ConfigureMethodInfo dependent)
        {
            _dependents.Add(dependent);
            dependent.AddDependency(this);
        }

        private void AddDependency(ConfigureMethodInfo dependency)
        {
            _dependencies.Add(dependency);
        }

        public static IDictionary<ConfigureMethodInfo, int> CalculateWeights(IEnumerable<ConfigureMethodInfo> methods)
        {
            var cache = new Dictionary<ConfigureMethodInfo, int>();
            foreach (var method in methods)
            {
                CalculateWeight(method, cache);
            }
            return cache;
        }

        private static int CalculateWeight(ConfigureMethodInfo method, IDictionary<ConfigureMethodInfo, int> weightCache)
        {
            int weight;
            if (!weightCache.TryGetValue(method, out weight))
            {
                weight = method.Dependents.Count() + method.Method.GetPriority().Priority * -1;
                foreach (var dependent in method.Dependents)
                {
                    weight += CalculateWeight(dependent, weightCache);
                }
                weightCache[method] = weight;
            }
            return weight;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", Type.FullName, Method.Name);
        }
    }
}
