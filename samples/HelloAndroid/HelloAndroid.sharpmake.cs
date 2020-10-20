using Sharpmake;

[module: Sharpmake.Include("HelloAndroid.Projects.sharpmake.cs")]
[module: Sharpmake.Include("HelloAndroid.Solution.sharpmake.cs")]
[module: Sharpmake.Include("HelloAndroid.Target.sharpmake.cs")]

[module: Sharpmake.DebugProjectName("Sharpmake.HelloAndroid")]

namespace HelloAndroid
{
    public static class StartupClass
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<HelloAndroidSolution>();
        }
    }
}

