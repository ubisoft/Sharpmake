using System;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    class KitsRootPathsTests
    {
        [Test]
        public void Test_NETFXKitsDir_throws_for_old_frameworks()
        {
            Assert.Throws<NotImplementedException>(() => KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v3));
            Assert.Throws<NotImplementedException>(() => KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v4_0));
            Assert.Throws<NotImplementedException>(() => KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v4_5));
        }

        [Test]
        public void Test_NETFXKitsDir_is_not_null_for_new_frameworks()
        {
            Assert.IsNotNull(KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v4_6));
            Assert.IsNotNull(KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v4_6_1));
            Assert.IsNotNull(KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v4_6_2));
        }
    }
}
