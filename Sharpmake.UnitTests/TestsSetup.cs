using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    [SetUpFixture]
    public class TestsSetup
    {
        [OneTimeSetUp]
        public static void Initialize()
        {
            PlatformRegistry.RegisterExtensionAssembly(typeof(Windows.Win32Platform).Assembly);
        }
    }
}
