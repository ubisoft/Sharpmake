using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;

[module: Sharpmake.Package(@".\SharpmakePackage.sharpmake.cs")]


namespace SharpmakeGen.FunctionalTests
{
    [Generate]
    public class SimpleProject : LibProjectBase
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<SimpleProject>();
        }
    }
}