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

        private static IEnumerable<MethodInfo> GetConfigureMethods(Type type, ConfigureOrder baseOrder)
        {
            Dictionary<string, MethodInfo> configureMethodDictionary = new Dictionary<string, MethodInfo>();
            List<MethodInfo> configureMethodInfos = new List<MethodInfo>();
            Type currentType = type;
            while (currentType != typeof(object))
            {
                MethodInfo[] methodInfos = currentType.GetMethods();
                foreach (MethodInfo methodInfo in methodInfos)
                {
                    bool defineConfigure = methodInfo.IsDefined(typeof(Configure), true);

                    if (!methodInfo.IsAbstract &&
                        !methodInfo.IsConstructor &&
                        !methodInfo.IsGenericMethod &&
                        defineConfigure)
                    {
                        string signature = methodInfo.ToString();
                        if (!configureMethodDictionary.ContainsKey(signature))
                        {
                            configureMethodDictionary.Add(signature, methodInfo);
                            configureMethodInfos.Add(methodInfo);
                        }
                    }
                }
                currentType = currentType.BaseType;
            }

            List<MethodInfo> filterMethods = new List<MethodInfo>();
            Dictionary<Type, List<MethodInfo>> typeMethodInfo = new Dictionary<Type, List<MethodInfo>>();
            currentType = type;
            while (currentType != typeof(object))
            {
                typeMethodInfo.Add(currentType, new List<MethodInfo>());
                currentType = currentType.BaseType;
            }

            foreach (MethodInfo method in configureMethodInfos)
            {
                // Get the first declaring type
                Type rootDeclaringType = method.DeclaringType;
                while (rootDeclaringType.BaseType.GetMethod(method.Name) != null)
                    rootDeclaringType = rootDeclaringType.BaseType;
                typeMethodInfo[rootDeclaringType].Add(method);
            }

            currentType = type;
            while (currentType != typeof(object))
            {
                var typeConfigure = typeMethodInfo[currentType].AsEnumerable();

                if (baseOrder == ConfigureOrder.New)
                    typeConfigure = typeConfigure.OrderBy(configure => configure.MetadataToken);

                filterMethods.InsertRange(0, typeConfigure);
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
