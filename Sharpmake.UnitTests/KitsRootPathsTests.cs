// Copyright (c) 2018-2020 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    internal class KitsRootPathsTests
    {
        [Test]
        public void Test_NETFXKitsDir_throws_for_old_frameworks()
        {
            Assert.Throws<NotImplementedException>(() => KitsRootPaths.GetNETFXKitsDir(DotNetFramework.v3_5));
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
