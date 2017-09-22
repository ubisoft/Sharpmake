using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[module: Sharpmake.Include("ConfigureOrdering.sharpmake.cs")]
[module: Sharpmake.Include("Util.sharpmake.cs")]

namespace ConfigureOrdering
{
    internal class main
    {
        [Sharpmake.Main]
        public static void SharpmakeMain(Sharpmake.Arguments arguments)
        {
            arguments.Generate<ConfigureOrderingSolution>();
        }
    }
}
