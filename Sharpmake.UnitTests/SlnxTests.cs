// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using NUnit.Framework;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.UnitTests
{
    public class SlnxTests
    {
        [Test]
        public void ThrowsWhenXmlRequestedOnNonVs2026()
        {
            var ex = Assert.Throws<Error>(() =>
                Slnx.ValidateDevEnv("MySolution", Solution.SolutionFormat.Xml, DevEnv.vs2022));
            Assert.That(ex.Message, Does.Contain("vs2026"));
            Assert.That(ex.Message, Does.Contain("MySolution"));
        }

        [Test]
        public void DoesNotThrowWhenXmlRequestedOnVs2026()
        {
            Assert.DoesNotThrow(() =>
                Slnx.ValidateDevEnv("MySolution", Solution.SolutionFormat.Xml, DevEnv.vs2026));
        }

        [Test]
        public void DoesNotThrowForLegacyOnAnyDevEnv()
        {
            Assert.DoesNotThrow(() =>
                Slnx.ValidateDevEnv("MySolution", Solution.SolutionFormat.Legacy, DevEnv.vs2022));
            Assert.DoesNotThrow(() =>
                Slnx.ValidateDevEnv("MySolution", Solution.SolutionFormat.Legacy, DevEnv.vs2026));
        }
    }
}
