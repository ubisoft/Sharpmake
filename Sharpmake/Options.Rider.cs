// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public static partial class Options
    {
        public static class Rider
        {
            /// <summary>
            /// Specify vcxproj for MSBuild 
            /// </summary>
            public class MsBuildOverrideProjectFile : PathOption
            {
                public MsBuildOverrideProjectFile(string path) : base(path) { }
            }

            public class MsBuildOverrideConfigurationName : StringOption
            {
                public static readonly string Default = null;
                public MsBuildOverrideConfigurationName(string path) : base(path) {}
            }
            
            public class MsBuildOverridePlatformName : StringOption
            {
                public static readonly string Default = null;
                public MsBuildOverridePlatformName(string path) : base(path) {}
            }
        }
    }
}


