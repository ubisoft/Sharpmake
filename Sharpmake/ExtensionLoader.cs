// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Sharpmake
{
    /// <summary>
    /// Helper for loading Sharpmake extensions.
    /// </summary>
    /// <remarks>
    /// Normally, this should be done using reflection-only load, but the problem is that you have
    /// to resolve the dependencies yourself. Turns out that it's simpler to actually let the CLR
    /// do a full load in a temporary <see cref="AppDomain"/> that we can trash later.
    /// </remarks>
    public class ExtensionLoader : IDisposable
    {
        public const string TempContextNamePrefix = "Temp extension check AssemblyLoadContext";
        public class ExtensionChecker : MarshalByRefObject
        {
            public bool IsSharpmakeExtension(string assemblyPath)
            {
                ExtensionLoader extensionLoader = new();
                bool result =  extensionLoader.LoadExtension(assemblyPath) != null;
                return result;
            }

            public static bool IsSharpmakeExtension(Assembly assembly)
            {
                if (assembly == null)
                    throw new ArgumentNullException(nameof(assembly));

                return assembly.GetCustomAttribute<SharpmakeExtensionAttribute>() != null;
            }
        }

        public static bool IsTempAssembly(Assembly assembly)
        {
            return AssemblyLoadContext.GetLoadContext(assembly)?.Name?.StartsWith(ExtensionLoader.TempContextNamePrefix) ?? false;
        }

        private static ConcurrentDictionary<string, byte> _nonExtensionAssemblyPaths = new();
        /// <summary>
        /// Releases the remote <see cref="AppDomain"/> if one was created.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        [Obsolete("This can't work on .net 6. Dead code", error: true)]
        public bool IsExtension(string assemblyPath)
        {
            return false;
        }

        [Obsolete("This can't work on .net 6. Dead code", error: true)]
        public Assembly LoadExtension(string assemblyPath, bool fastLoad)
        {
            return null;
        }

        /// <summary>
        /// Loads a Sharpmake extension assembly.
        /// </summary>
        /// <param name="assemblyPath">The path of the assembly that contains the Sharpmake extension.</param>
        /// <returns>The loaded extension's <see cref="Assembly"/> or null if it's not a Sharpmake extension.</returns>
        public Assembly LoadExtension(string assemblyPath)
        {
            if (assemblyPath == null)
                throw new ArgumentNullException(nameof(assemblyPath));

            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException("Cannot find the assembly DLL.", assemblyPath);
            
            if (_nonExtensionAssemblyPaths.Keys.Contains(assemblyPath))
                return null;

            // If we've already loaded the assembly in some loading context, check the attributes without loading it again
            foreach (AssemblyLoadContext alc in AssemblyLoadContext.All)
            {
                Assembly assembly = alc.Assemblies.FirstOrDefault(a => AssemblyHasSamePath(a, assemblyPath));
                if (assembly != null)
                {
                    if (ExtensionChecker.IsSharpmakeExtension(assembly))
                        return assembly;
                    
                    _nonExtensionAssemblyPaths.TryAdd(assemblyPath, 0);

                    return null;
                }
            }

            // Check the attributes via an unloadable context (reflexion only is not a thing anymore in .net 6)
            var contextName = $"{TempContextNamePrefix} - {Path.GetFileNameWithoutExtension(assemblyPath)}";
            AssemblyLoadContext tempLoadContext = new(contextName, true);

            bool isSharpmakeExtension;

            try
            {
                var tempAssembly = tempLoadContext.LoadFromAssemblyPath(assemblyPath);
                isSharpmakeExtension = ExtensionChecker.IsSharpmakeExtension(tempAssembly);
                // log unloading of contexts, only log info for Sharpmake extensions
                Action<string, object[]> logger = isSharpmakeExtension ? Builder.Instance.LogWriteLine : Builder.Instance.DebugWriteLine;
                tempLoadContext.Unloading += (context) => logger($"    [ExtensionLoader] Attempting to unload temporary context {context.Name}", new object[]{});
            }
            catch (BadImageFormatException)
            {
                // This is either a native C/C++ assembly, or there is a x86/x64 mismatch
                // that prevents it to load. Sharpmake platforms have no reason to not be
                // AnyCPU so just assume that it's not a Sharpmake extension.
                return null;
            }
            finally
            {
                // Unload the AssemblyLoadContext
                // NOTE: the unloading event is fired on calling unload for the context, the assembly might not be unloaded
                //  if there are references to it (make sure it is not used on temp assembly loading)
                tempLoadContext.Unload();
            }

            if (isSharpmakeExtension)
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

            _nonExtensionAssemblyPaths.TryAdd(assemblyPath, 0);

            return null;

            static bool AssemblyHasSamePath(Assembly assembly, string assemblyPath)
            {
                return assembly != null
                       && !string.IsNullOrEmpty(assemblyPath) && !assembly.IsDynamic &&
                       string.Equals(assembly.Location, assemblyPath, StringComparison.Ordinal);
            }
        }

        [Obsolete("This can't work on .net 6. Dead code", error: true)]
        public IEnumerable<Assembly> LoadExtensionsInDirectory(string directory)
        {
            return null;
        }
    }
}
