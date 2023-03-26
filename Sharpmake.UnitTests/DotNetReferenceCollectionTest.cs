// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    internal class DotNetReferenceTest
    {
        private List<DotNetReference> _testReferences;

        [SetUp]
        public void Setup()
        {
            _testReferences = new List<DotNetReference>()
            {
                new DotNetReference("MyLib1", DotNetReference.ReferenceType.DotNet),
                new DotNetReference("MyLib2", DotNetReference.ReferenceType.DotNetExtensions),
                new DotNetReference("MyLib3", DotNetReference.ReferenceType.Project),
            };
        }

        [Test]
        public void DefaultConstructor()
        {
            Assert.That(_testReferences[0].Include, Is.EqualTo("MyLib1"));
            Assert.That(_testReferences[0].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNet));
            Assert.That(_testReferences[0].HintPath, Is.EqualTo(null));
            Assert.That(_testReferences[0].LinkFolder, Is.EqualTo(null));
            Assert.That(_testReferences[0].Private, Is.EqualTo(null));
            Assert.That(_testReferences[0].SpecificVersion, Is.EqualTo(null));
            Assert.That(_testReferences[0].EmbedInteropTypes, Is.EqualTo(null));
        }

        [Test]
        public void SetHintPath()
        {
            Assert.That(_testReferences[1].Include, Is.EqualTo("MyLib2"));
            Assert.That(_testReferences[1].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNetExtensions));
            Assert.That(_testReferences[1].HintPath, Is.EqualTo(null));
            Assert.That(_testReferences[1].LinkFolder, Is.EqualTo(null));
            Assert.That(_testReferences[1].Private, Is.EqualTo(null));
            Assert.That(_testReferences[1].SpecificVersion, Is.EqualTo(null));
            Assert.That(_testReferences[1].EmbedInteropTypes, Is.EqualTo(null));

            _testReferences[1].HintPath = @"C:\temp\toto.dll";

            Assert.That(_testReferences[1].Include, Is.EqualTo("MyLib2"));
            Assert.That(_testReferences[1].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNetExtensions));
            Assert.That(_testReferences[1].HintPath, Is.EqualTo(@"C:\temp\toto.dll"));
            Assert.That(_testReferences[1].LinkFolder, Is.EqualTo(null));
            Assert.That(_testReferences[1].Private, Is.EqualTo(null));
            Assert.That(_testReferences[1].SpecificVersion, Is.EqualTo(null));
            Assert.That(_testReferences[1].EmbedInteropTypes, Is.EqualTo(null));
        }

        [Test]
        public void SetLinkFolder()
        {
            Assert.That(_testReferences[1].Include, Is.EqualTo("MyLib2"));
            Assert.That(_testReferences[1].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNetExtensions));
            Assert.That(_testReferences[1].HintPath, Is.EqualTo(null));
            Assert.That(_testReferences[1].LinkFolder, Is.EqualTo(null));
            Assert.That(_testReferences[1].Private, Is.EqualTo(null));
            Assert.That(_testReferences[1].SpecificVersion, Is.EqualTo(null));
            Assert.That(_testReferences[1].EmbedInteropTypes, Is.EqualTo(null));

            _testReferences[1].LinkFolder = @"MyFolder";

            Assert.That(_testReferences[1].Include, Is.EqualTo("MyLib2"));
            Assert.That(_testReferences[1].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNetExtensions));
            Assert.That(_testReferences[1].HintPath, Is.EqualTo(null));
            Assert.That(_testReferences[1].LinkFolder, Is.EqualTo(@"MyFolder"));
            Assert.That(_testReferences[1].Private, Is.EqualTo(null));
            Assert.That(_testReferences[1].SpecificVersion, Is.EqualTo(null));
            Assert.That(_testReferences[1].EmbedInteropTypes, Is.EqualTo(null));
        }

        [Test]
        public void SetPrivate()
        {
            Assert.That(_testReferences[1].Include, Is.EqualTo("MyLib2"));
            Assert.That(_testReferences[1].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNetExtensions));
            Assert.That(_testReferences[1].HintPath, Is.EqualTo(null));
            Assert.That(_testReferences[1].LinkFolder, Is.EqualTo(null));
            Assert.That(_testReferences[1].Private, Is.EqualTo(null));
            Assert.That(_testReferences[1].SpecificVersion, Is.EqualTo(null));
            Assert.That(_testReferences[1].EmbedInteropTypes, Is.EqualTo(null));

            _testReferences[1].Private = false;

            Assert.That(_testReferences[1].Include, Is.EqualTo("MyLib2"));
            Assert.That(_testReferences[1].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNetExtensions));
            Assert.That(_testReferences[1].HintPath, Is.EqualTo(null));
            Assert.That(_testReferences[1].LinkFolder, Is.EqualTo(null));
            Assert.That(_testReferences[1].Private, Is.EqualTo(false));
            Assert.That(_testReferences[1].SpecificVersion, Is.EqualTo(null));
            Assert.That(_testReferences[1].EmbedInteropTypes, Is.EqualTo(null));
        }

        [Test]
        public void SetSpecificVersion()
        {
            Assert.That(_testReferences[1].Include, Is.EqualTo("MyLib2"));
            Assert.That(_testReferences[1].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNetExtensions));
            Assert.That(_testReferences[1].HintPath, Is.EqualTo(null));
            Assert.That(_testReferences[1].LinkFolder, Is.EqualTo(null));
            Assert.That(_testReferences[1].Private, Is.EqualTo(null));
            Assert.That(_testReferences[1].SpecificVersion, Is.EqualTo(null));
            Assert.That(_testReferences[1].EmbedInteropTypes, Is.EqualTo(null));

            _testReferences[1].SpecificVersion = true;

            Assert.That(_testReferences[1].Include, Is.EqualTo("MyLib2"));
            Assert.That(_testReferences[1].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNetExtensions));
            Assert.That(_testReferences[1].HintPath, Is.EqualTo(null));
            Assert.That(_testReferences[1].LinkFolder, Is.EqualTo(null));
            Assert.That(_testReferences[1].Private, Is.EqualTo(null));
            Assert.That(_testReferences[1].SpecificVersion, Is.EqualTo(true));
            Assert.That(_testReferences[1].EmbedInteropTypes, Is.EqualTo(null));
        }

        [Test]
        public void SetEmbedInteropTypes()
        {
            Assert.That(_testReferences[1].Include, Is.EqualTo("MyLib2"));
            Assert.That(_testReferences[1].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNetExtensions));
            Assert.That(_testReferences[1].HintPath, Is.EqualTo(null));
            Assert.That(_testReferences[1].LinkFolder, Is.EqualTo(null));
            Assert.That(_testReferences[1].Private, Is.EqualTo(null));
            Assert.That(_testReferences[1].SpecificVersion, Is.EqualTo(null));
            Assert.That(_testReferences[1].EmbedInteropTypes, Is.EqualTo(null));

            _testReferences[1].EmbedInteropTypes = true;

            Assert.That(_testReferences[1].Include, Is.EqualTo("MyLib2"));
            Assert.That(_testReferences[1].Type, Is.EqualTo(DotNetReference.ReferenceType.DotNetExtensions));
            Assert.That(_testReferences[1].HintPath, Is.EqualTo(null));
            Assert.That(_testReferences[1].LinkFolder, Is.EqualTo(null));
            Assert.That(_testReferences[1].Private, Is.EqualTo(null));
            Assert.That(_testReferences[1].SpecificVersion, Is.EqualTo(null));
            Assert.That(_testReferences[1].EmbedInteropTypes, Is.EqualTo(true));
        }

        [Test]
        public void CompareToEquals()
        {
            Assert.That(_testReferences[0].CompareTo(_testReferences[0]), Is.EqualTo(0));
            Assert.That(_testReferences[1].CompareTo(_testReferences[1]), Is.EqualTo(0));
            Assert.That(_testReferences[2].CompareTo(_testReferences[2]), Is.EqualTo(0));
        }

        [Test]
        public void CompareToNotEquals()
        {
            Assert.That(_testReferences[0].CompareTo(_testReferences[1]), Is.EqualTo(-1));
            Assert.That(_testReferences[1].CompareTo(_testReferences[2]), Is.EqualTo(-1));
            Assert.That(_testReferences[2].CompareTo(_testReferences[0]), Is.EqualTo(2));
        }
    }

    internal class DotNetReferenceCollectionTest
    {
        private readonly DotNetReference[] _testReferences = {
            new DotNetReference("MyLib1", DotNetReference.ReferenceType.DotNet),
            new DotNetReference("MyLib2", DotNetReference.ReferenceType.DotNet),
            new DotNetReference("MyLib3", DotNetReference.ReferenceType.DotNet),
        };

        [Test]
        public void Constructor()
        {
            DotNetReferenceCollection collection = new DotNetReferenceCollection { _testReferences[0] };

            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.True(collection.Contains(_testReferences[0]));
        }

        [Test]
        public void Add()
        {
            DotNetReferenceCollection collection = new DotNetReferenceCollection();
            collection.Add(_testReferences[1]);

            Assert.That(collection.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add2()
        {
            DotNetReferenceCollection collection = new DotNetReferenceCollection();

            Assert.That(collection.Count, Is.EqualTo(0));
            collection.Add(_testReferences[1]);
            collection.Add(_testReferences[0]);
            Assert.That(collection.Count, Is.EqualTo(2));
            Assert.True(collection.Contains(_testReferences[1]));
            Assert.True(collection.Contains(_testReferences[0]));
        }

        [Test]
        public void AddSame2()
        {
            DotNetReferenceCollection collection = new DotNetReferenceCollection();

            Assert.That(collection.Count, Is.EqualTo(0));
            collection.Add(_testReferences[0]);
            collection.Add(_testReferences[0]);
            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.True(collection.Contains(_testReferences[0]));
        }

        [Test]
        public void Remove()
        {
            DotNetReferenceCollection collection = new DotNetReferenceCollection { _testReferences[0] };

            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.True(collection.Remove(_testReferences[0]));
            Assert.That(collection.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveNotExists()
        {
            DotNetReferenceCollection collection = new DotNetReferenceCollection { _testReferences[0] };

            Assert.That(collection.Count, Is.EqualTo(1));
            Assert.False(collection.Remove(_testReferences[2]));
            Assert.That(collection.Count, Is.EqualTo(1));
        }

        [Test]
        public void Clear()
        {
            DotNetReferenceCollection collection = new DotNetReferenceCollection { _testReferences[0], _testReferences[1], _testReferences[2] };

            Assert.That(collection.Count, Is.EqualTo(3));
            collection.Clear();
            Assert.That(collection.Count, Is.EqualTo(0));
        }
    }
}
