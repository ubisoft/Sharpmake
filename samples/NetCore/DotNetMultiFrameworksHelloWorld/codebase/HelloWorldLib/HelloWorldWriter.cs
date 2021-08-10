using System;
using System.Reflection;

namespace HelloWorld
{
    public static class HelloWorldWriter
    {
        private static readonly string s_framework = Assembly.GetEntryAssembly()?.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName;
        private static readonly string s_frameworkDisplayName = Assembly.GetEntryAssembly()?.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkDisplayName;
        private static string CompiledWithFramework() => !string.IsNullOrEmpty(s_frameworkDisplayName) ? s_frameworkDisplayName : !string.IsNullOrEmpty(s_framework) ? s_framework : "Unknown";

        public static void WriteHelloWorldLine()
        {
            Console.WriteLine($"Hello World - {CompiledWithFramework()}!");
        }
    }
}
