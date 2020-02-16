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
using System.IO;
using System.Reflection;
using System.Security.Policy;

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
        private readonly object _mutex = new object();
        private AppDomain _remoteDomain;
        private ExtensionChecker _validator;

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
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets whether an assembly is a Sharpmake extension.
        /// </summary>
        /// <param name="assemblyPath">The path of the assembly to check whether it's an extension.</param>
        /// <returns>`true` if it is an extension, `false` otherwise.</returns>
        /// <remarks>
        /// This method will instantiate a remote <see cref="AppDomain"/> if none was created.
        /// </remarks>
        public bool IsExtension(string assemblyPath)
        {
            if (assemblyPath == null)
                throw new ArgumentNullException(nameof(assemblyPath));

            // If it's this assembly, ignore of course.
            if (StringComparer.OrdinalIgnoreCase.Equals(typeof(Util).Assembly.Location, assemblyPath))
                return false;

            CreateRemoteExtensionCheckerIfNeeded();

            return _validator.IsSharpmakeExtension(assemblyPath);
        }

        /// <summary>
        /// Loads a Sharpmake extension assembly.
        /// </summary>
        /// <param name="assemblyPath">The path of the assembly that contains the Sharpmake extension.</param>
        /// <param name="fastLoad">Whether this method should load the assembly remotely first. See remarks.</param>
        /// <returns>The loaded extension's <see cref="Assembly"/>.</returns>
        /// <remarks>
        /// Because loading an extension in a remote assembly for validation is expensive, this
        /// method provides the <paramref name="fastLoad"/> argument which, when `false`, will load
        /// the extension in the current <see cref="AppDomain"/> instead of doing so in a remote
        /// <see cref="AppDomain"/>, testing whether it contains
        /// <see cref="SharpmakeExtensionAttribute"/>, and then loading it again in
        /// <see cref="AppDomain.CurrentDomain"/>. However, because it is impossible to unload a
        /// loaded assembly from the CLR, if this method fail you have essentially polluted the
        /// process' address space with an assembly that you may not need.
        /// </remarks>
        public Assembly LoadExtension(string assemblyPath, bool fastLoad)
        {
            if (assemblyPath == null)
                throw new ArgumentNullException(nameof(assemblyPath));

            if (fastLoad)
            {
                if (!IsExtension(assemblyPath))
                    return null;
            }

            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            if (!fastLoad)
            {
                if (!ExtensionChecker.IsSharpmakeExtension(assembly))
                    return null;
            }

            return assembly;
        }

        /// <summary>
        /// Loads all Sharpmake extensions in a directory.
        /// </summary>
        /// <param name="directory">The path to the directory to scan for assemblies.</param>
        /// <returns>A <see cref="IEnumerable{T}"/> that contains the loaded <see cref="Assembly"/>.</returns>
        public IEnumerable<Assembly> LoadExtensionsInDirectory(string directory)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Directory {directory} does not exist.");

            CreateRemoteExtensionCheckerIfNeeded();

            var assemblies = new List<Assembly>();
            IEnumerable<string> dlls = Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var dll in dlls)
            {
                if (IsExtension(dll))
                {
                    try
                    {
                        Assembly assembly = LoadExtension(dll, true);
                        if (assembly != null)
                            assemblies.Add(assembly);
                    }
                    catch (Exception ex)
                    {
                        Util.LogWrite("Failure to load assembly {0}. This may cause runtime errors.\nDetails: {1}", Path.GetFileName(dll), ex.Message);
                    }
                }
            }

            return assemblies;
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_remoteDomain != null)
                {
                    AppDomain.Unload(_remoteDomain);
                    _remoteDomain = null;
                    _validator = null;
                }
            }
        }

        private void CreateRemoteExtensionCheckerIfNeeded()
        {
            lock (_mutex)
            {
                if (_validator == null)
                {
                    _remoteDomain = AppDomain.CreateDomain("ExtensionHelperDomain", new Evidence(AppDomain.CurrentDomain.Evidence));
                    _validator = _remoteDomain.CreateInstanceAndUnwrap(typeof(ExtensionChecker).Assembly.FullName, typeof(ExtensionChecker).FullName) as ExtensionChecker;
                }
            }
        }
    }
}
