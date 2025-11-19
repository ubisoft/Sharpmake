// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    internal class KitsRootPathsTests
    {
        [Test]
        public void Test_NETFXKitsDir_is_not_null_for_new_frameworks()
        {
            Assert.IsNotNull(KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v4_7));
            Assert.IsNotNull(KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v4_7_1));
            Assert.IsNotNull(KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v4_7_2));
            Assert.IsNotNull(KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v4_8));
        }
    }
}
