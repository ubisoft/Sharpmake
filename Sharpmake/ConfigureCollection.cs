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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Sharpmake
{
    [DefaultValue(New)]
    public enum ConfigureOrder
    {
        /// <summary>
        /// Orders the configure methods by the first definition of the configure.
        /// With default order given by reflection:
        /// http://pierrerebours.com/blog/order-returned-getmethods
        /// The order can't be predicted.
        /// </summary>
        Old,

        /// <summary>
        /// Orders the configure methods by the first definition of the configure and the MetadataToken of the method.
        /// e.g. 
        /// class A     { virtual  void foo() { }   void bar() { } }
        /// class B : A { override void foo() { }   void buz() { } }
        /// Assuming foo(), bar() and buz() are marked as configure methods,
        /// if A is generated, foo() will be called then bar() will be called
        /// if B is generated, the call sequence will be foo(), bar(), buz()
        /// </summary>
        New
    }

    internal class ConfigureCollection : IEnumerable<MethodInfo>
    {
        private static readonly ConcurrentDictionary<Tuple<MethodInfo, bool>, Configure> s_cachedMethodInfoToConfigureAttributes = new ConcurrentDictionary<Tuple<MethodInfo, bool>, Configure>();

        internal static Configure GetConfigureAttribute(MethodInfo configure, bool inherit)
        {
            return s_cachedMethodInfoToConfigureAttributes.GetOrAdd(Tuple.Create(configure, inherit), configure.GetCustomAttribute(typeof(Configure), inherit) as Configure);
        }

        private readonly IEnumerable<MethodInfo> _orderedConfigureCollection;

        internal static ConfigureCollection Create(Type type,
            ConfigureOrder configureBaseOrder = ConfigureOrder.New,
            Func<Type, ConfigurePriority, IEnumerable<MethodInfo>, IEnumerable<MethodInfo>> orderProvider = null)
        {
            var configures = GetConfigureMethods(type, configureBaseOrder);
            return new ConfigureCollection(type, configures, orderProvider ?? ((t, p, methods) => methods));
        }

        private ConfigureCollection(
            Type type,
            IEnumerable<MethodInfo> configures,
            Func<Type, ConfigurePriority, IEnumerable<MethodInfo>, IEnumerable<MethodInfo>> orderProvider = null)
        {
            var orderedConfigureDictionary = new SortedDictionary<ConfigurePriority, List<MethodInfo>>();

            foreach (var configure in configures)
            {
                var priorityConfigureCollection = orderedConfigureDictionary.GetValueOrAdd(configure.GetPriority(), new List<MethodInfo>());
                priorityConfigureCollection.Add(configure);
            }

            _orderedConfigureCollection = orderedConfigureDictionary.SelectMany(priority => orderProvider(type, priority.Key, priority.Value));
        }

        // will return a dictionary where the key will be the signature, and the value the list of
        // method infos that share it, ordered with the most derived first and the base class last
        private static Dictionary<string, List<MethodInfo>> GetMethodInfoBySignature(Type type)
        {
            var configureMethodsInfo = new Dictionary<string, List<MethodInfo>>();

            var allConfigureFullSignatures = new HashSet<string>();

            Type currentType = type;
            while (currentType != typeof(object))
            {
                MethodInfo[] methodInfos = currentType.GetMethods();
                foreach (MethodInfo methodInfo in methodInfos)
                {
                    if (!methodInfo.IsAbstract &&
                        !methodInfo.IsConstructor &&
                        !methodInfo.IsGenericMethod &&
                        methodInfo.IsDefined(typeof(Configure), true))
                    {
                        string signature = methodInfo.ToString();
                        string fullSignature = methodInfo.DeclaringType.FullName + signature;

                        // full signature could be found more than once when parsing inheritance chain
                        if (allConfigureFullSignatures.Add(fullSignature))
                        {
                            List<MethodInfo> configureMethods;
                            if (configureMethodsInfo.TryGetValue(signature, out configureMethods))
                                configureMethods.Add(methodInfo);
                            else
                                configureMethodsInfo.Add(signature, new List<MethodInfo> { methodInfo });
                        }
                    }
                }
                currentType = currentType.BaseType;
            }

            return configureMethodsInfo;
        }

        private static void VerifyAttributesConsistency(MethodInfo methodInfo, MethodInfo baseMethodInfo, string methodSignature)
        {
            // do the check here if a previous method was found
            var configureAttribute = GetConfigureAttribute(methodInfo, inherit: false);
            var baseConfigureAttribute = GetConfigureAttribute(baseMethodInfo, inherit: false);

            // if the derived class configure has attributes, we only allow them if they are identical to the ones from the base class
            if (configureAttribute != null)
            {
                if (!configureAttribute.HasSameFlags(baseConfigureAttribute))
                {
                    throw new Error(
                        "Attributes mismatch for signature {0}!\nType {1} has {2}\nBase {3} has {4}",
                        methodSignature,
                        methodInfo.DeclaringType.FullName,
                        configureAttribute,
                        baseMethodInfo.DeclaringType.FullName,
                        baseConfigureAttribute == null ? "*no attributes*" : baseConfigureAttribute.ToString()
                    );
                }
                else
                {
                    Builder.Instance.LogWarningLine(
                        "Warning: Please remove attributes on {0} overriding {1}, they are useless.",
                        methodInfo.DeclaringType.FullName + "." + methodInfo.Name,
                        baseMethodInfo.DeclaringType.FullName + "." + baseMethodInfo.Name
                    );
                }
            }
            else
            {
                // if it didn't, do nothing
            }
        }

        private static void ConfigureConsistencyCheck(Dictionary<string, List<MethodInfo>> configureMethodsInfo)
        {
            foreach (var configureMethodInfo in configureMethodsInfo)
            {
                // if there is only one configure method with that signature, there's nothing to check
                if (configureMethodInfo.Value.Count <= 1)
                    continue;

                string methodSignature = configureMethodInfo.Key;
                MethodInfo baseMethodInfo = null;
                foreach (MethodInfo methodInfo in configureMethodInfo.Value.AsEnumerable().Reverse()) // start from the end, meaning the base class
                {
                    if (baseMethodInfo == null)
                        baseMethodInfo = methodInfo;
                    else
                        VerifyAttributesConsistency(methodInfo, baseMethodInfo, methodSignature);
                }
            }
        }

        private static IEnumerable<MethodInfo> GetConfigureMethods(Type type, ConfigureOrder baseOrder)
        {
            var typeMethodInfo = new Dictionary<Type, List<MethodInfo>>();

            var configureMethodsInfo = GetMethodInfoBySignature(type);
            ConfigureConsistencyCheck(configureMethodsInfo);

            foreach (var configureMethodInfo in configureMethodsInfo)
            {
                // Get the base declaring type (last in the array)
                Type rootDeclaringType = configureMethodInfo.Value.Last().DeclaringType;
                typeMethodInfo.GetValueOrAdd(rootDeclaringType, new List<MethodInfo>()).Add(configureMethodInfo.Value.First());
            }

            var filterMethods = new List<MethodInfo>();
            Type currentType = type;
            while (currentType != typeof(object))
            {
                List<MethodInfo> methodInfoList;
                if (typeMethodInfo.TryGetValue(currentType, out methodInfoList))
                {
                    var typeConfigure = methodInfoList.AsEnumerable();
                    if (baseOrder == ConfigureOrder.New)
                        typeConfigure = typeConfigure.OrderBy(configure => configure.MetadataToken);

                    filterMethods.InsertRange(0, typeConfigure);
                }

                currentType = currentType.BaseType;
            }

            return filterMethods;
        }

        public IEnumerator<MethodInfo> GetEnumerator()
        {
            return _orderedConfigureCollection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class MethodInfoExtension
    {
        public static ConfigurePriority GetPriority(this MethodInfo method)
        {
            return method.GetPriority(ConfigurePriority.DefaultPriority);
        }

        public static ConfigurePriority GetPriority(this MethodInfo method, ConfigurePriority defaultPriority)
        {
            object[] configureAttributes = method.GetCustomAttributes(typeof(ConfigurePriority), true);
            return (ConfigurePriority)configureAttributes.FirstOrDefault() ?? defaultPriority;
        }
    }
}
