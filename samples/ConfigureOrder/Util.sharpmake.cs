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
                                        DevEnv.vs2013,
                                        Optimization.Release,
                                        OutputType.Lib,
                                        Blob.NoBlob,
                                        BuildSystem.MSBuild,
                                        DotNetFramework.v4_5);
                return s_defaultTarget;
            }
        }
    }
}
