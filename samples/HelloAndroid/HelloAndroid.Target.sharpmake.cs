using Sharpmake;

namespace HelloAndroid
{
    public class AndroidTarget : Target
    {
        public Android.AndroidBuildTargets AndroidBuildTargets;

        public AndroidTarget() { }

        public AndroidTarget(
            Platform platform,
            DevEnv devEnv,
            Optimization optimization,
            Android.AndroidBuildTargets androidBuildTargets
        ) : base(platform, devEnv, optimization)
        {
            AndroidBuildTargets = androidBuildTargets;
        }

        public static Target[] GetDefaultTargets()
        {
            return new[]
            {
                new AndroidTarget(
                    Platform.android,
                    DevEnv.vs2017,
                    Optimization.Debug | Optimization.Release,
                    Android.AndroidBuildTargets.arm64_v8a | Android.AndroidBuildTargets.armeabi_v7a | Android.AndroidBuildTargets.x86 | Android.AndroidBuildTargets.x86_64),
            };
        }
    }
}

