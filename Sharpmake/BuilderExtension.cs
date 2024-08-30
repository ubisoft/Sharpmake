// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Sharpmake
{
    /// <summary>
    /// Put this attribute on a static method that match the name and signature of an Event in the Builder class, and it will be called automatically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class BuilderEventAttribute : Attribute { }

    public class BuilderExtension
    {
        private readonly Builder _builder;

        public BuilderExtension(Builder builder)
        {
            _builder = builder;

            // Set an assembly event that can register generator's extension types on assembly load
            AppDomain.CurrentDomain.AssemblyLoad += AppDomain_AssemblyLoad;

            // Check all already loaded assemblies for generator's extension types
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                RegisterExtensionAssembly(assembly);
        }

        private void AppDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            RegisterExtensionAssembly(args.LoadedAssembly);
        }

        /// <summary>
        /// Scans a Sharpmake extension assembly for builder's event extension, and then hook them to the builder's events.
        /// </summary>
        /// <param name="extensionAssembly">The <see cref="Assembly"/> to scan.</param>
        private void RegisterExtensionAssembly(Assembly extensionAssembly)
        {
            if (ExtensionLoader.IsTempAssembly(extensionAssembly))
                return;

            // Ignores if the assembly does not declare itself as a Sharpmake extension.
            if (!ExtensionLoader.ExtensionChecker.IsSharpmakeExtension(extensionAssembly))
                return;

            _builder.ExecuteEntryPointInAssemblies<EntryPoint>(extensionAssembly);

            foreach (Type classType in extensionAssembly.GetTypes().Where(t => t.IsVisible))
            {
                foreach (MethodInfo methodInfo in classType.GetMethods().Where(m => m.GetCustomAttributes<BuilderEventAttribute>().Any()))
                {
                    if (!methodInfo.IsStatic)
                        throw new Exception($"Method {methodInfo.Name} from assembly {extensionAssembly.FullName} must be static");

                    EventInfo ev = _builder.GetType().GetEvents().FirstOrDefault(e => e.Name == methodInfo.Name);
                    if (ev == null)
                        throw new Exception($"Can't find {methodInfo.Name} event in Builder class");

                    Delegate handler = Delegate.CreateDelegate(ev.EventHandlerType, null, methodInfo);
                    ev.AddEventHandler(_builder, handler);
                }
            }
        }
    }
}
