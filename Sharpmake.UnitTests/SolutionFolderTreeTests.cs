// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using NUnit.Framework;
using Sharpmake.Generators.VisualStudio;

namespace Sharpmake.UnitTests
{
    [TestFixture]
    public class SolutionFolderTreeTests
    {
        [Test]
        public void NullPathReturnsNull()
        {
            var tree = new SolutionFolderTree();

            Assert.That(tree.GetSolutionFolder(null), Is.Null);
            Assert.That(tree.AllFolders, Is.Empty);
        }

        [TestCase("")]
        [TestCase("/")]
        [TestCase("\\\\")]
        public void PathWithoutNamesReturnsNull(string path)
        {
            var tree = new SolutionFolderTree();

            Assert.That(tree.GetSolutionFolder(path), Is.Null);
            Assert.That(tree.AllFolders, Is.Empty);
        }

        [Test]
        public void NestedPathCreatesParentChain()
        {
            var tree = new SolutionFolderTree();

            SolutionFolder leaf = tree.GetSolutionFolder("Engine/Core");

            Assert.That(leaf.Name, Is.EqualTo("Core"));
            Assert.That(leaf.Parent, Is.Not.Null);
            Assert.That(leaf.Parent.Name, Is.EqualTo("Engine"));
            Assert.That(leaf.Parent.Parent, Is.Null);
            Assert.That(leaf.Parent.Childs, Is.EqualTo(new[] { leaf }));
            Assert.That(leaf.Path, Is.EqualTo("Engine" + Path.DirectorySeparatorChar + "Core"));
            Assert.That(tree.RootFolders, Is.EqualTo(new[] { leaf.Parent }));
            Assert.That(tree.AllFolders, Is.EquivalentTo(new[] { leaf.Parent, leaf }));
        }

        [TestCase("Engine/Core")]
        [TestCase("Engine\\Core")]
        [TestCase("/Engine//Core/")]
        public void SamePathReturnsSameFolder(string path)
        {
            var tree = new SolutionFolderTree();
            SolutionFolder first = tree.GetSolutionFolder("Engine/Core");

            SolutionFolder second = tree.GetSolutionFolder(path);

            Assert.That(second, Is.SameAs(first));
            Assert.That(tree.RootFolders, Has.Count.EqualTo(1));
            Assert.That(tree.AllFolders, Has.Count.EqualTo(2));
        }

        [Test]
        public void SiblingFoldersShareParent()
        {
            var tree = new SolutionFolderTree();

            SolutionFolder core = tree.GetSolutionFolder("Engine/Core");
            SolutionFolder render = tree.GetSolutionFolder("Engine/Render");

            Assert.That(render.Parent, Is.SameAs(core.Parent));
            Assert.That(core.Parent.Childs, Is.EqualTo(new[] { core, render }));
            Assert.That(tree.AllFolders, Has.Count.EqualTo(3));
        }

        [Test]
        public void SameNameUnderDifferentParentsIsDistinct()
        {
            var tree = new SolutionFolderTree();

            SolutionFolder engineTests = tree.GetSolutionFolder("Engine/Tests");
            SolutionFolder toolsTests = tree.GetSolutionFolder("Tools/Tests");

            Assert.That(toolsTests, Is.Not.SameAs(engineTests));
            Assert.That(toolsTests.Guid, Is.Not.EqualTo(engineTests.Guid));
            Assert.That(tree.RootFolders, Has.Count.EqualTo(2));
            Assert.That(tree.AllFolders, Has.Count.EqualTo(4));
        }

        [Test]
        public void FolderNamesAreCaseSensitive()
        {
            var tree = new SolutionFolderTree();

            SolutionFolder lower = tree.GetSolutionFolder("engine");
            SolutionFolder upper = tree.GetSolutionFolder("Engine");

            Assert.That(upper, Is.Not.SameAs(lower));
            Assert.That(tree.RootFolders, Has.Count.EqualTo(2));
        }

        [Test]
        public void GuidIsBuiltFromPathAndStableAcrossTrees()
        {
            SolutionFolder first = new SolutionFolderTree().GetSolutionFolder("Engine/Core");
            SolutionFolder second = new SolutionFolderTree().GetSolutionFolder("Engine/Core");

            Assert.That(first.Guid, Is.EqualTo(Util.BuildGuid(first.Path)));
            Assert.That(first.Parent.Guid, Is.EqualTo(Util.BuildGuid("Engine")));
            Assert.That(second.Guid, Is.EqualTo(first.Guid));
        }

        [Test]
        public void SortStablyOrdersByNameIgnoringCase()
        {
            var tree = new SolutionFolderTree();
            tree.GetSolutionFolder("charlie");
            tree.GetSolutionFolder("alpha");
            tree.GetSolutionFolder("Bravo");

            tree.SortStably();

            // An ordinal sort would put "Bravo" first.
            Assert.That(tree.AllFolders.Select(f => f.Name), Is.EqualTo(new[] { "alpha", "Bravo", "charlie" }));
        }

        [Test]
        public void SortStablyBreaksNameTiesByGuid()
        {
            var tree = new SolutionFolderTree();
            SolutionFolder first = tree.GetSolutionFolder("Engine/Tests");
            SolutionFolder second = tree.GetSolutionFolder("Tools/Tests");
            SolutionFolder third = tree.GetSolutionFolder("Samples/Tests");

            tree.SortStably();

            var expectedTests = new[] { first, second, third }.OrderBy(f => f.Guid).ToArray();
            Assert.That(tree.AllFolders.Where(f => f.Name == "Tests"), Is.EqualTo(expectedTests));
            Assert.That(tree.AllFolders.Select(f => f.Name), Is.EqualTo(new[] { "Engine", "Samples", "Tests", "Tests", "Tests", "Tools" }));
        }

        [Test]
        public void SortStablyDoesNotReorderRootFolders()
        {
            var tree = new SolutionFolderTree();
            SolutionFolder zulu = tree.GetSolutionFolder("Zulu");
            SolutionFolder alpha = tree.GetSolutionFolder("Alpha");

            tree.SortStably();

            Assert.That(tree.RootFolders, Is.EqualTo(new[] { zulu, alpha }));
        }

        [Test]
        public void ClearRemovesAllFolders()
        {
            var tree = new SolutionFolderTree();
            SolutionFolder before = tree.GetSolutionFolder("Engine/Core");

            tree.Clear();

            Assert.That(tree.RootFolders, Is.Empty);
            Assert.That(tree.AllFolders, Is.Empty);
            Assert.That(tree.GetSolutionFolder("Engine/Core"), Is.Not.SameAs(before));
        }
    }
}
