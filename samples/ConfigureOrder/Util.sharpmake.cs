using Sharpmake;

namespace ConfigureOrdering
{
    public class Util
    {
        private static Target s_defaultTarget;
        public static Target DefaultTarget
        {
            get
            {
                if (s_defaultTarget == null)
                    s_defaultTarget = new Target(
                                        Platform.win32,
                                        DevEnv.vs2017,
                                        Optimization.Release,
                                        OutputType.Lib,
                                        Blob.NoBlob,
                                        BuildSystem.MSBuild,
                                        DotNetFramework.v4_6_2);
                return s_defaultTarget;
            }
        }
    }
}
