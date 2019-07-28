using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

#if (VISUAL_STUDIO_EXTENSION_ENABLED)
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.Setup.Configuration;
#endif

namespace Sharpmake
{
    public static class VisualStudioExtension
    {
#if (VISUAL_STUDIO_EXTENSION_ENABLED)
        public class VsInstallation
        {
            public VsInstallation(ISetupInstance2 setupInstance)
            {
                Version = new Version(setupInstance.GetInstallationVersion());

                var catalog = setupInstance as ISetupInstanceCatalog;
                IsPrerelease = catalog?.IsPrerelease() ?? false;

                InstallationPath = setupInstance.GetInstallationPath();

                if ((setupInstance.GetState() & InstanceState.Registered) == InstanceState.Registered)
                {
                    ProductID = setupInstance.GetProduct().GetId();
                    Components = (from package in setupInstance.GetPackages()
                                  where string.Equals(package.GetType(), "Component", StringComparison.OrdinalIgnoreCase)
                                  select package.GetId()).ToArray();
                    Workloads = (from package in setupInstance.GetPackages()
                                 where string.Equals(package.GetType(), "Workload", StringComparison.OrdinalIgnoreCase)
                                 select package.GetId()).ToArray();
                }
            }

            public Version Version { get; }

            public string InstallationPath { get; }

            public bool IsPrerelease { get; }

            /// <summary>
            /// The full list of products can be found here: https://docs.microsoft.com/en-us/visualstudio/install/workload-and-component-ids
            /// </summary>
            public string ProductID { get; } = null;

            /// <summary>
            /// This can be used to check and limit by specific installed workloads.
            /// 
            /// What is a Workload?
            /// In the VS installer, a 'Workload' is a section that you see in the UI such as 'Desktop development with C++' or '.NET desktop development'.
            /// 
            /// The full list of products is here: https://docs.microsoft.com/en-us/visualstudio/install/workload-and-component-ids
            /// 
            /// For each product, clicking it will bring up a page of all of the possible Workloads.
            /// For example: https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-professional
            /// </summary>
            public string[] Workloads { get; } = new string[] { };

            /// <summary>
            /// This can be used to check and limit by specific installed components.
            /// What is a Component?
            /// In the Visual Studio Installer, the 'Components' are individual components associated with each Workload (and some just on the side), 
            /// that you can see in the Summary on the right.
            /// Each workflow contains a number of mandatory components, but also a list of optional ones.
            /// An example would be: 'NuGet package manager' or 'C++/CLI support'.
            /// 
            /// The full list of products is here: https://docs.microsoft.com/en-us/visualstudio/install/workload-and-component-ids
            /// 
            /// For each product, clicking it will bring up a page of all of the possible Workloads.
            /// For example: https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-professional
            /// </summary>
            public string[] Components { get; } = new string[] { };
        }

        public static List<VsInstallation> s_VisualStudioInstallations { get; private set; } = null;

        private static object s_vsInstallScanLock = new object();
        public static List<VsInstallation> GetVisualStudioInstalledVersions()
        {
            lock (s_vsInstallScanLock)
            {
                if (s_VisualStudioInstallations != null)
                    return s_VisualStudioInstallations;

                var installations = new List<VsInstallation>();

                try
                {
                    var query = (ISetupConfiguration2)new SetupConfiguration();
                    var e = query.EnumAllInstances();

                    int fetched;
                    var instances = new ISetupInstance[1];
                    do
                    {
                        e.Next(1, instances, out fetched);
                        if (fetched > 0)
                        {
                            var setupInstance2 = (ISetupInstance2)instances[0];
                            if ((setupInstance2.GetState() & InstanceState.Local) == InstanceState.Local)
                            {
                                installations.Add(new VsInstallation(setupInstance2));
                            }
                        }
                    } while (fetched > 0);
                }
                catch (COMException)
                {
                    // Ignore
                }

                s_VisualStudioInstallations = installations;
                return s_VisualStudioInstallations;
            }
        }

        /// <summary>
        /// The supported visual studio products, in order by priority in which Sharpmake will choose them.
        /// We want to block products like the standalone Team Explorer, which is in the Visual Studio
        /// family yet isn't a variant of Visual Studio proper.
        /// 
        /// The list of Product IDs can be found here: https://docs.microsoft.com/en-us/visualstudio/install/workload-and-component-ids
        /// </summary>
        public static readonly string[] s_supportedVisualStudioProducts = new[]
        {
            "Microsoft.VisualStudio.Product.Enterprise",
            "Microsoft.VisualStudio.Product.Professional",
            "Microsoft.VisualStudio.Product.Community",
            "Microsoft.VisualStudio.Product.BuildTools"
        };

        public static int IndexOf<T>(this T[] source, T value, StringComparison stringComparison)
        {
            if (typeof(T) == typeof(string))
                return Array.FindIndex(source, m => m.ToString().Equals(value.ToString(), stringComparison));
            else
                return Array.IndexOf(source, value);
        }

        private class VisualStudioVersionSorter : IComparer<VsInstallation>
        {
            public int Compare(VsInstallation x, VsInstallation y)
            {
                // Order by Product ID priority (the order they appear in s_supportedVisualStudioProducts).
                int xProductIndex = s_supportedVisualStudioProducts.IndexOf(x.ProductID, StringComparison.OrdinalIgnoreCase);
                int yProductIndex = s_supportedVisualStudioProducts.IndexOf(y.ProductID, StringComparison.OrdinalIgnoreCase);

                int versionComparison = xProductIndex.CompareTo(yProductIndex);
                if (versionComparison != 0)
                    return versionComparison;

                // If they have the same Product ID, then compare their versions and return the highest one.
                return y.Version.CompareTo(x.Version); // Swap x and y so that the comparison is inversed (higher values first).
            }
        }

        public static List<VsInstallation> GetVisualStudioInstallationsFromQuery(int visualMajorVersion, bool allowPrereleaseVersions = false,
            string[] requiredComponents = null, string[] requiredWorkloads = null)
        {
            // Fetch all installed products
            var installedVersions = GetVisualStudioInstalledVersions();

            // Limit to our major version + the supported products, and order by priority.
            var candidates = installedVersions.Where(i =>
                    i.Version.Major == visualMajorVersion
                    && s_supportedVisualStudioProducts.Contains(i.ProductID, StringComparer.OrdinalIgnoreCase)
                    && (requiredComponents == null || !requiredComponents.Except(i.Components).Any())
                    && (requiredWorkloads == null || !requiredWorkloads.Except(i.Workloads).Any()))
                .OrderBy(x => x, new VisualStudioVersionSorter()).ToList();

            return candidates;
        }

        public static string GetVisualStudioInstallPathFromQuery(int visualMajorVersion, bool allowPrereleaseVersions = false,
            string[] requiredComponents = null, string[] requiredWorkloads = null)
        {
            var vsInstallations = GetVisualStudioInstallationsFromQuery(visualMajorVersion, allowPrereleaseVersions, requiredComponents, requiredWorkloads);
            VsInstallation priorityInstallation = vsInstallations.FirstOrDefault(i => allowPrereleaseVersions || !i.IsPrerelease);
            return priorityInstallation != null ? priorityInstallation.InstallationPath : null;
        }

        public static IEnumerable<string> EnumeratePathToDotNetFramework()
        {
            for (int i = (int)TargetDotNetFrameworkVersion.VersionLatest; i >= 0; --i)
            {
                string frameworkDirectory = ToolLocationHelper.GetPathToDotNetFramework((TargetDotNetFrameworkVersion)i);
                if (frameworkDirectory != null)
                    yield return frameworkDirectory;
            }
        }

        public static void BreakIntoDebugger()
        {
            System.Windows.Forms.MessageBox.Show("Debugger requested. Please attach a debugger and press OK");
        }
#else
        public static string GetVisualStudioInstallPathFromQuery(int visualMajorVersion, bool allowPrereleaseVersions = false,
            string[] requiredComponents = null, string[] requiredWorkloads = null)
        {
            return string.Empty;
        }

        static string[] _FrameworkPaths =
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(System.IO.Path.PathSeparator)
            .Select(System.IO.Path.GetDirectoryName)
            .Distinct()
            .ToArray();

        public static IEnumerable<string> EnumeratePathToDotNetFramework()
        {
            return _FrameworkPaths;
        }

        public static void BreakIntoDebugger()
        {
            while (!System.Diagnostics.Debugger.IsAttached)
            {
                Console.Write("Waiting for debugger to attach...." + Environment.NewLine);
                System.Threading.Thread.Sleep(1000);
            }
        }
#endif
    }
}
