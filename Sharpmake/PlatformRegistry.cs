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
using System.Text;

namespace Sharpmake
{
    /// <summary>
    /// Global registry that maps platform-specific implementations of interfaces with a concrete
    /// implementation. This facility allows to add and remove platform-specific generators without
    /// having to break any code.
    /// </summary>
    /// <remarks>
    /// This class searches for implementations using .NET reflection when the
    /// type is loaded by looking for types marked with <see cref="PlatformImplementationAttribute"/>
    /// in assemblies marked with <see cref="SharpmakeExtensionAttribute"/>. It may
    /// also store default implementations to fall-back to when it does not find any implementation
    /// for a requested platform and interface. Default implementations must be marked with
    /// <see cref="DefaultPlatformImplementationAttribute"/>.
    /// </remarks>
    public static class PlatformRegistry
    {
        [DebuggerDisplay("{Platform}; {InterfaceType}")]
        private struct PlatformImplementation
        {
            public Platform Platform { get; }
            public Type InterfaceType { get; }

            public PlatformImplementation(Platform platform, Type interfaceType)
            {
                Platform = platform;
                InterfaceType = interfaceType;
            }
        }

        private class PlatformImplementationDescriptor
        {
            public PlatformImplementation Implementation { get; }
            public Type ConcreteType { get; }

            public PlatformImplementationDescriptor(PlatformImplementation impl, Type type)
            {
                Implementation = impl;
                ConcreteType = type;
            }
        }

        private class PlatformImplementationComparer : IEqualityComparer<PlatformImplementation>
        {
            public bool Equals(PlatformImplementation x, PlatformImplementation y)
            {
                return x.Platform == y.Platform && x.InterfaceType == y.InterfaceType;
            }

            public int GetHashCode(PlatformImplementation obj)
            {
                return obj.Platform.GetHashCode() ^ obj.InterfaceType.GetHashCode();
            }
        }

        private static readonly IDictionary<PlatformImplementation, object> s_implementations = new Dictionary<PlatformImplementation, object>(new PlatformImplementationComparer());
        private static readonly IDictionary<Type, object> s_defaultImplementations = new Dictionary<Type, object>();
        private static readonly ISet<object> s_implementationInstances = new HashSet<object>();
        private static readonly ISet<Assembly> s_parsedAssemblies = new HashSet<Assembly>();

        static PlatformRegistry()
        {
            // Set an assembly resolver that can link to the loaded extensions.
            AppDomain.CurrentDomain.AssemblyResolve += AppDomain_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyLoad += AppDomain_AssemblyLoad;
            RegisterPlatformsFromLoadedAssemblies();
        }

        public static void RegisterPlatformsFromLoadedAssemblies()
        {
            // Query all loaded assemblies for types that are platform-specific implementations.
            // Restrict to assemblies decorated with attribute ContainsPlatformImplementations so
            // it doesn't search uselessly through the standard .NET class libraries.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                RegisterExtensionAssembly(assembly);
        }

        /// <summary>
        /// Occurs when an extension assembly containing platform implementations is loaded.
        /// </summary>
        public static event EventHandler<PlatformImplementationExtensionRegisteredEventArgs> PlatformImplementationExtensionRegistered;

        /// <summary>
        /// Scans a Sharpmake extension assembly for the platform implementations, and then
        /// register those implementations.
        /// </summary>
        /// <param name="extensionAssembly">The <see cref="Assembly"/> to scan.</param>
        /// <exception cref="ArgumentNullException"><paramref name="extensionAssembly"/> is `null`.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="extensionAssembly"/> was loaded in reflection-only.</exception>
        /// <exception cref="NotSupportedException"><paramref name="extensionAssembly"/> is a dynamically compiled assembly.</exception>
        public static void RegisterExtensionAssembly(Assembly extensionAssembly)
        {
            if (extensionAssembly == null)
                throw new ArgumentNullException(nameof(extensionAssembly));

            if (extensionAssembly.ReflectionOnly)
                return;

            // Don't support loading dynamically compiled assemblies
            if (extensionAssembly.IsDynamic)
                return;

            // Ignores if the assembly does not declare itself as a Sharpmake extension.
            if (!ExtensionLoader.ExtensionChecker.IsSharpmakeExtension(extensionAssembly))
                return;

            // Don't support loading dynamically compiled assemblies because we need the location
            // to verify that we are not loading the same dll twice.
            if (extensionAssembly.Location == null)
                throw new NotSupportedException("Assembly does not have any location : not supported.");

            // Has that assembly already been checked for platform stuff?
            if (s_parsedAssemblies.Any(assembly => assembly.Location == extensionAssembly.Location))
                return;

            // Go through all the types declared in the Sharpmake extension assembly and look
            // up for platform implementations.
            var typeInfos = from type in extensionAssembly.GetTypes()
                            let attributes = type.GetCustomAttributes<PlatformImplementationAttribute>()
                            where attributes.Any()
                            from attribute in attributes
                            from platform in EnumeratePlatformBits(attribute.Platforms)
                            from ifaceType in attribute.InterfaceTypes
                            select new PlatformImplementationDescriptor(new PlatformImplementation(platform, ifaceType), type);

            var registeredTypes = new List<Type>();

            if (typeInfos.Any())
            {
                lock (s_implementations)
                {
                    // Make sure that our platform implementations are unique.
                    EnsureUniquePlatformImplementations(typeInfos);

                    foreach (var type in typeInfos)
                    {
                        Type ifaceType = type.Implementation.InterfaceType;
                        registeredTypes.Add(ifaceType);
                        RegisterImplementationImplNoLock(type.Implementation.Platform, ifaceType, GetImplementationInstance(type.ConcreteType));
                    }
                }
            }

            // Go through all types again and this time get the default implementations.
            var defaultTypes = from type in extensionAssembly.GetTypes()
                               let attributes = type.GetCustomAttributes<DefaultPlatformImplementationAttribute>()
                               where attributes.Any()
                               from attribute in attributes
                               from ifaceType in attribute.InterfaceTypes
                               select new
                               {
                                   ImplementationType = type,
                                   InterfaceType = ifaceType
                               };

            if (defaultTypes.Any())
            {
                lock (s_defaultImplementations)
                {
                    foreach (var type in defaultTypes)
                    {
                        // TODO: Check if the attribute is given to different types and throw an
                        //       error if it does, just like for the platform implementations do
                        //       by calling EnsureUniquePlatformImplementations(typeInfo). That's
                        //       assuming that we don't scrap the concept of a default
                        //       implementation though.

                        registeredTypes.Add(type.InterfaceType);
                        if (!s_defaultImplementations.ContainsKey(type.InterfaceType))
                            s_defaultImplementations.Add(type.InterfaceType, GetImplementationInstance(type.ImplementationType));
                    }
                }
            }

            s_parsedAssemblies.Add(extensionAssembly);

            if (typeInfos.Any() || defaultTypes.Any())
                PlatformImplementationExtensionRegistered?.Invoke(null, new PlatformImplementationExtensionRegisteredEventArgs(extensionAssembly, registeredTypes));
        }

        /// <summary>
        /// Registers a platform implementation given an implementation class.
        /// </summary>
        /// <param name="implType">The <see cref="Type"/> of the implementation class.</param>
        /// <exception cref="ArgumentNullException"><paramref name="implType"/> is `null`.</exception>
        /// <exception cref="ArgumentException"><paramref name="implType"/> does not have a default constructor, or is an abstract class or an interface.</exception>
        /// <remarks>
        /// This method will search through the <see cref="PlatformImplementationAttribute"/> on
        /// <paramref name="implType"/> to find what platform/interface pairs to register.
        /// </remarks>
        public static void RegisterImplementation(Type implType)
        {
            if (implType == null)
                throw new ArgumentNullException(nameof(implType));
            if (implType.IsAbstract || implType.IsInterface)
                throw new ArgumentException($"{implType} is an abstract class or an interface.");

            try
            {
                object implementation = GetImplementationInstance(implType);
                var attributes = implType.GetCustomAttributes<PlatformImplementationAttribute>();

                foreach (var att in attributes)
                {
                    foreach (var ifaceType in att.InterfaceTypes)
                    {
                        foreach (var platform in EnumeratePlatformBits(att.Platforms))
                            RegisterImplementation(platform, ifaceType, implementation);
                    }
                }
            }
            catch (MissingMethodException ex)
            {
                throw new ArgumentException($"There is no default constructor in type {implType.Name}, or it is not accessible.", ex);
            }
        }

        /// <summary>
        /// Registers a platform and an interface to a given implementation class.
        /// </summary>
        /// <param name="platform">The <see cref="Platform"/> to register to.</param>
        /// <param name="ifaceType">The interface to register to.</param>
        /// <param name="implType">The <see cref="Type"/> of the implementing class. See remarks.</param>
        /// <exception cref="ArgumentNullException"><paramref name="ifaceType"/> or <paramref name="implType"/> are `null`.</exception>
        /// <exception cref="ArgumentException"><paramref name="implType"/> does not implement <paramref name="ifaceType"/>, or <paramref name="implType"/> does not have a default constructor, or <paramref name="implType"/> is an abstract class or an interface.</exception>
        /// <remarks>
        /// <para>
        /// This method ignores the attributes on <paramref name="implType"/> and registers it for
        /// <paramref name="platform"/> and <paramref name="ifaceType"/>.
        /// </para>
        /// <para>
        /// <paramref name="implType"/> must have a default constructor because this method will
        /// attempt to create an instance.
        /// </para>
        /// </remarks>
        public static void RegisterImplementation(Platform platform, Type ifaceType, Type implType)
        {
            if (ifaceType == null)
                throw new ArgumentNullException(nameof(ifaceType));
            if (implType == null)
                throw new ArgumentNullException(nameof(implType));
            if (!ifaceType.IsAssignableFrom(implType))
                throw new ArgumentException($"{implType.Name} does not inherit {ifaceType.Name}.");
            if (implType.IsAbstract || implType.IsInterface)
                throw new ArgumentException($"{implType} is an abstract class or an interface.");

            try
            {
                object implementation = GetImplementationInstance(implType);
                RegisterImplementationImpl(platform, ifaceType, implementation);
            }
            catch (MissingMethodException ex)
            {
                throw new ArgumentException($"There is no default constructor in type {implType.Name}, or it is not accessible.", ex);
            }
        }

        /// <summary>
        /// Registers a platform and an interface to an object that implements the interface.
        /// </summary>
        /// <param name="platform">The <see cref="Platform"/> to register to.</param>
        /// <param name="ifaceType">The interface to register to.</param>
        /// <param name="implementation">An <see cref="Object"/> that implements <paramref name="ifaceType"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="ifaceType"/> or <paramref name="implementation"/> are `null`.</exception>
        /// <exception cref="ArgumentException"><paramref name="implementation"/> does not implement <paramref name="ifaceType"/>.</exception>
        /// <remarks>
        /// This method ignores the attributes on <paramref name="implementation"/>'s
        /// <see cref="Type"/> and registers it for <paramref name="platform"/> and
        /// <paramref name="ifaceType"/>.
        /// </remarks>
        public static void RegisterImplementation(Platform platform, Type ifaceType, object implementation)
        {
            if (ifaceType == null)
                throw new ArgumentNullException(nameof(ifaceType));
            if (implementation == null)
                throw new ArgumentNullException(nameof(implementation));

            Type implType = implementation.GetType();
            if (!ifaceType.IsAssignableFrom(implType))
                throw new ArgumentException($"{implType.Name} does not inherit {ifaceType.Name}.");

            RegisterImplementationImpl(platform, ifaceType, implementation);
        }

        /// <summary>
        /// Registers a platform implementation given an implementation class.
        /// </summary>
        /// <typeparam name="TImplementation">The type of the implementation class.</typeparam>
        /// <remarks>
        /// This method will search through the <see cref="PlatformImplementationAttribute"/> on
        /// <typeparamref name="TImplementation"/> to find what platform/interface pairs to register.
        /// </remarks>
        public static void RegisterImplementation<TImplementation>()
            where TImplementation : class, new()
        {
            var implType = typeof(TImplementation);
            RegisterImplementation(implType);
        }

        /// <summary>
        /// Registers a platform and an interface to a given implementation class.
        /// </summary>
        /// <typeparam name="TInterface">The interface to register to.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementing class.</typeparam>
        /// <param name="platform">The <see cref="Platform"/> to register to.</param>
        /// <remarks>
        /// This method ignores the attributes on <typeparamref name="TImplementation"/> and
        /// registers it for <paramref name="platform"/> and <typeparamref name="TInterface"/>.
        /// </remarks>
        public static void RegisterImplementation<TInterface, TImplementation>(Platform platform)
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            Type ifaceType = typeof(TInterface);
            Type implType = typeof(TImplementation);
            RegisterImplementation(platform, ifaceType, implType);
        }

        /// <summary>
        /// Registers a platform and an interface to an object that implements the interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface to register to.</typeparam>
        /// <param name="platform">The <see cref="Platform"/> to register to.</param>
        /// <param name="implementation">A <typeparamref name="TInterface"/> that provides the implementation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="implementation"/> is `null`.</exception>
        /// <remarks>
        /// This method ignores the attributes on <paramref name="implementation"/>'s
        /// <see cref="Type"/> and registers it for <paramref name="platform"/> and
        /// <typeparamref name="TInterface"/>.
        /// </remarks>
        public static void RegisterImplementation<TInterface>(Platform platform, TInterface implementation)
            where TInterface : class
        {
            if (implementation == null)
                throw new ArgumentNullException(nameof(platform));

            Type ifaceType = typeof(TInterface);
            RegisterImplementation(platform, ifaceType, implementation);
        }

        /// <summary>
        /// Checks if the registry contains an implementation of a given interface for a given
        /// platform.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface needed.</typeparam>
        /// <param name="platform">The platform for which the interface must be implemented.</param>
        /// <returns>`true` if the interface is there, `false` otherwise.</returns>
        public static bool Has<TInterface>(Platform platform)
            where TInterface : class
        {
            Type ifaceType = typeof(TInterface);
            var platformImpl = new PlatformImplementation(platform, ifaceType);
            lock (s_implementations)
            {
                if (s_implementations.ContainsKey(platformImpl))
                    return true;
            }

            return s_defaultImplementations.ContainsKey(ifaceType);
        }

        /// <summary>
        /// Gets the default implementation of a given interface. This is what is returned if an
        /// interface is not implemented for any platform.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface to get.</typeparam>
        /// <returns>The default interface, as a <typeparamref name="TInterface"/> instance.</returns>
        /// <exception cref="PlatformNotSupportedException">There is no default implementation of <typeparamref name="TInterface"/>.</exception>
        public static TInterface GetDefault<TInterface>()
            where TInterface : class
        {
            TInterface iface = QueryDefault<TInterface>();
            if (iface == null)
                throw new PlatformNotSupportedException(typeof(TInterface));

            return iface;
        }

        /// <summary>
        /// Gets the default implementation of a given interface if it has one. Does not throw if
        /// there is no default implementation.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface to get.</typeparam>
        /// <returns>The default interface, as a <typeparamref name="TInterface"/> instance, if such an interface exists. Returns `null` if there is no default interface.</returns>
        public static TInterface QueryDefault<TInterface>()
            where TInterface : class
        {
            Type ifaceType = typeof(TInterface);
            object implObj;
            if (s_defaultImplementations.TryGetValue(ifaceType, out implObj))
                return (TInterface)implObj;
            else
                return null;
        }

        /// <summary>
        /// Gets the implementation of a given interface for a given platform. If no implementation
        /// was defined for that platform, returns the default implementation instead, if one was
        /// defined.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface to get.</typeparam>
        /// <param name="platform">The platform whose implementation is requested.</param>
        /// <returns>The implementation of <typeparamref name="TInterface"/> for a given platform.</returns>
        /// <exception cref="PlatformNotSupportedException">There is neither an implementation nor a default implementation of <typeparamref name="TInterface"/> for that platform.</exception>
        public static TInterface Get<TInterface>(Platform platform)
            where TInterface : class
        {
            TInterface iface = Query<TInterface>(platform);
            if (iface == null)
                throw new PlatformNotSupportedException(platform, typeof(TInterface));

            return iface;
        }

        /// <summary>
        /// Gets the implementation of a given interface for a given platform. If no implementation
        /// was defined for that platform, returns the default implementation instead, if one was
        /// defined. Does not throw an exception if no platform implementation is found.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface to get.</typeparam>
        /// <param name="platform">The platform whose implementation is requested.</param>
        /// <returns>The implementation of <typeparamref name="TInterface"/> for a given platform, or `null` if no such implementation or default implementation exists for that platform.</returns>
        public static TInterface Query<TInterface>(Platform platform)
            where TInterface : class
        {
            Type ifaceType = typeof(TInterface);
            var platformImpl = new PlatformImplementation(platform, ifaceType);
            object implObj = null;
            lock (s_implementations)
            {
                if (s_implementations.TryGetValue(platformImpl, out implObj))
                    return (TInterface)implObj;
            }

            if (s_defaultImplementations.TryGetValue(ifaceType, out implObj))
                return (TInterface)implObj;
            else
                return null;
        }

        /// <summary>
        /// Gets the list of platforms for which a given interface is available.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface to check availability for.</typeparam>
        /// <returns>The collection of available platforms.</returns>
        public static IEnumerable<Platform> GetAvailablePlatforms<TInterface>()
            where TInterface : class
        {
            Type ifaceType = typeof(TInterface);
            lock (s_implementations)
            {
                return (from impl in s_implementations.Keys
                        where impl.InterfaceType == ifaceType
                        select impl.Platform).ToArray();
            }
        }

        private static void EnsureUniquePlatformImplementations(IEnumerable<PlatformImplementationDescriptor> typeInfos)
        {
            var comparer = new PlatformImplementationComparer();
            var checkedTypes = new Dictionary<PlatformImplementation, PlatformImplementationDescriptor>(comparer);
            var duplicates = new Dictionary<PlatformImplementation, List<Type>>(comparer);
            foreach (var typeInfo in typeInfos)
            {
                // If it's a duplicate of something we already have registered and the
                // implementation has the exact type we desire, ignore.
                if (s_implementations.ContainsKey(typeInfo.Implementation))
                {
                    if (s_implementations[typeInfo.Implementation].GetType() == typeInfo.ConcreteType)
                        continue;
                }

                // Checks for duplicates in the list of implementations to add.
                if (checkedTypes.ContainsKey(typeInfo.Implementation))
                {
                    List<Type> duplicateTypes;
                    if (!duplicates.TryGetValue(typeInfo.Implementation, out duplicateTypes))
                    {
                        duplicateTypes = new List<Type>();
                        duplicateTypes.Add(checkedTypes[typeInfo.Implementation].ConcreteType);
                        duplicates.Add(typeInfo.Implementation, duplicateTypes);
                    }

                    duplicateTypes.Add(typeInfo.ConcreteType);
                }
            }

            if (duplicates.Any())
            {
                var errorMessageBuilder = new StringBuilder();
                foreach (var duplicate in duplicates)
                {
                    string platform = duplicate.Key.Platform.ToString();
                    string iface = duplicate.Key.InterfaceType.Name;
                    errorMessageBuilder.AppendLine($"Duplicate platform detected: Platform {platform} as interface {iface}");
                    foreach (var type in duplicate.Value)
                        errorMessageBuilder.AppendLine($"\tHas implementation in {type.Name} ({type.Assembly.FullName})");
                    errorMessageBuilder.AppendLine("---");
                }
                throw new DuplicatePlatformImplementationException(errorMessageBuilder.ToString());
            }
        }

        private static object GetImplementationInstance(Type implType)
        {
            lock (s_implementationInstances)
            {
                object instance = s_implementationInstances.SingleOrDefault(obj => obj.GetType().AssemblyQualifiedName == implType.AssemblyQualifiedName);
                if (instance == null)
                {
                    try
                    {
                        instance = Activator.CreateInstance(implType);
                        s_implementationInstances.Add(instance);
                    }
                    catch (Exception ex)
                    {
                        throw new PlatformImplementationCreationException(implType, ex);
                    }
                }

                return instance;
            }
        }

        private static void RegisterImplementationImpl(Platform platform, Type ifaceType, object implementation)
        {
            lock (s_implementations)
            {
                RegisterImplementationImplNoLock(platform, ifaceType, implementation);
            }
        }

        private static void RegisterImplementationImplNoLock(Platform platform, Type ifaceType, object implementation)
        {
            var platformImpl = new PlatformImplementation(platform, ifaceType);
            object prevImpl = null;
            if (s_implementations.TryGetValue(platformImpl, out prevImpl))
            {
                if (object.ReferenceEquals(prevImpl, implementation))
                    return;

                throw new InvalidOperationException($"There is already an implementation of interface {ifaceType} for platform {platform}. Cannot register {implementation.GetType().AssemblyQualifiedName}");
            }
            else
            {
                s_implementations.Add(platformImpl, implementation);
            }
        }

        private static Assembly AppDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var extAssemblies = s_implementationInstances.GroupBy(i => i.GetType().Assembly).Select(g => g.Key);
            foreach (var ext in extAssemblies)
            {
                if (args.Name.IndexOf(ext.GetName().Name, StringComparison.OrdinalIgnoreCase) != -1)
                    return ext;
            }

            return null;
        }

        private static void AppDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            RegisterExtensionAssembly(args.LoadedAssembly);
        }

        private static IEnumerable<Platform> EnumeratePlatformBits(Platform platforms)
        {
            uint bitField = (uint)platforms;
            int bitIndex = 0;
            while (bitField != 0)
            {
                if (0 != (bitField & 1))
                    yield return (Platform)(1 << bitIndex);
                bitIndex++;
                bitField >>= 1;
            }
        }
    }
}
