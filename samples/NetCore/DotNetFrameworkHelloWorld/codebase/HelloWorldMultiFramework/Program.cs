using System;
using System.Reflection;

namespace HelloWorld
{
    class Program
    {
        private static readonly string s_framework = Assembly.GetEntryAssembly()?.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName;
        private static readonly string s_frameworkDisplayName = Assembly.GetEntryAssembly()?.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkDisplayName;
        private static string CompiledWithFramework() => !string.IsNullOrEmpty(s_frameworkDisplayName) ? s_frameworkDisplayName : !string.IsNullOrEmpty(s_framework) ? s_framework : "Unknown";

        static void Main(string[] args)
        {
            Console.WriteLine($"Hello World - {CompiledWithFramework()}!");
        }
    }
}
