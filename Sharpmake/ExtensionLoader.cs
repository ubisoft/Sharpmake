// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        public class ExtensionChecker : MarshalByRefObject
        {
            private readonly Dictionary<string, bool> _loadedAssemblies = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            public bool IsSharpmakeExtension(string assemblyPath)
            {
                if (assemblyPath == null)
                    throw new ArgumentNullException(nameof(assemblyPath));
                if (!File.Exists(assemblyPath))
                    throw new FileNotFoundException("Cannot find the assembly DLL.", assemblyPath);

                lock (_loadedAssemblies)
                {
                    // If the assembly has already been loaded, check if it is.
                    bool result;
                    if (_loadedAssemblies.TryGetValue(assemblyPath, out result))
                        return result;

                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(assemblyPath);
                        result = IsSharpmakeExtension(assembly);
                    }
                    catch (BadImageFormatException)
                    {
                        // This is either a native C/C++ assembly, or there is a x86/x64 mismatch
                        // that prevents it to load. Sharpmake platforms have no reason to not be
                        // AnyCPU so just assume that it's not a Sharpmake extension.
                        result = false;
                    }
                    catch (Exception ex)
                    {
                        throw new Error("An unexpected error has occurred while loading a potential Sharpmake extension assembly: {0}", ex.Message);
                    }

                    _loadedAssemblies.Add(assemblyPath, result);
                    return result;
                }
            }

            public static bool IsSharpmakeExtension(Assembly assembly)
            {
                if (assembly == null)
                    throw new ArgumentNullException(nameof(assembly));

                return assembly.GetCustomAttribute<SharpmakeExtensionAttribute>() != null;
            }
        }

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
        /// <returns>The loaded extension's <see cref="Assembly"/>.</returns>
        public Assembly LoadExtension(string assemblyPath)
        {
            if (assemblyPath == null)
                throw new ArgumentNullException(nameof(assemblyPath));

            try
            {
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                if (!ExtensionChecker.IsSharpmakeExtension(assembly))
                    return null;
                return assembly;
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        [Obsolete("This can't work on .net 6. Dead code", error: true)]
        public IEnumerable<Assembly> LoadExtensionsInDirectory(string directory)
        {
            return null;
        }
    }
}
