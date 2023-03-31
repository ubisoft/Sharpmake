// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

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
