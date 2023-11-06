// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    public class ResolverTest
    {
        [Resolver.Resolvable]
        private class FieldClass
        {
            public bool Bool;
            public int Int;
            public string String;
            public readonly string ReadOnlyString = "[p1.String]";
            public FieldClass RecursionObject;
            public readonly object NullObject = null;


            public override string ToString()
            {
                return "FieldClass";
            }
        }

        [Resolver.Resolvable]
        private class PropertyClass
        {
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public string Value3 { get; set; }

            public override string ToString()
            {
                return "PropertyClass";
            }
        }


        [Resolver.Resolvable]
        private class ListClass
        {
            public string Value { get; set; }
            public Strings FieldListString = new Strings();

            private Strings _propertyListString = new Strings();
            public Strings PropertyListString
            {
                get { return _propertyListString; }
            }

            public override string ToString()
            {
                return "ListClass";
            }
        }

        [Resolver.Resolvable]
        private class ObjectClass
        {
            public object ChildObject;
            public string StringValue1;
            public string StringValue2;
        }

        [Resolver.Resolvable]
        private class DictionaryClass
        {
            public Dictionary<string, string> Dictionary = new Dictionary<string, string>();
        }

        [Test]
        public void CanResolveAFieldClass()
        {
            Resolver resolver = new Resolver();
            FieldClass obj = new FieldClass();
            resolver.Resolve(obj);
        }

        [Test]
        public void CanResolveWithRecursionObject()
        {
            Resolver resolver = new Resolver();
            FieldClass obj = new FieldClass();
            obj.RecursionObject = obj;
            resolver.Resolve(obj);
        }

        [Test]
        public void CanResolveFieldsWithDifferentTypes()
        {
            var obj = new FieldClass { Bool = false, Int = 12345678 };

            var resolver = new Resolver();
            resolver.SetParameter("p1", obj);

            obj.String = "[p1.Bool] - [p1.Int]";
            resolver.Resolve(obj);
            Assert.That(obj.String, Is.EqualTo("False - 12345678"));

            // Must pass a new object. Resolver doesn't resolve same objects twice.
            obj = new FieldClass();
            obj.String = "[p1]";
            resolver.Resolve(obj);
            Assert.That(obj.String, Is.EqualTo("FieldClass"));

            obj = new FieldClass();
            obj.String = "[p1.NullObject]";
            Assert.Throws<Error>(() => resolver.Resolve(obj));
        }

        [Test]
        public void DoesNotResolveAReadOnlyField()
        {
            var obj = new FieldClass();
            obj.String = "String";

            var resolver = new Resolver();
            resolver.SetParameter("p1", obj);
            resolver.Resolve(obj);

            Assert.That(obj.ReadOnlyString, Is.EqualTo("[p1.String]"));
        }

        [Test]
        public void ReplacesValuesOnDependentFields()
        {
            var obj = new PropertyClass();
            obj.Value1 = "* [obj] *";
            obj.Value2 = "** [obj.Value1] **";
            obj.Value3 = "*** [obj.Value2] ***";

            var resolver = new Resolver();
            resolver.SetParameter("obj", obj);
            resolver.Resolve(obj);

            Assert.That(obj.Value1, Is.EqualTo("* PropertyClass *"));
            Assert.That(obj.Value2, Is.EqualTo("** * PropertyClass * **"));
            Assert.That(obj.Value3, Is.EqualTo("*** ** * PropertyClass * ** ***"));
        }

        // Property Test
        [Test]
        public void CanResolveInsideContainers()
        {
            var obj = new ListClass { Value = "'come get some'" };

            obj.FieldListString.Add("* [obj.Value] *");
            obj.FieldListString.Add("- [obj.Value] - [obj.Value] -");
            obj.FieldListString.Add("+ [obj.Value] + [obj.Value] + [obj.Value] +");

            obj.PropertyListString.Add("* [obj.Value] *");
            obj.PropertyListString.Add("- [obj.Value] - [obj.Value] -");
            obj.PropertyListString.Add("+ [obj.Value] + [obj.Value] + [obj.Value] +");

            var resolver = new Resolver();
            resolver.SetParameter("obj", obj);
            resolver.Resolve(obj);

            Assert.That(obj.FieldListString.Values[0], Is.EqualTo("* 'come get some' *"));
            Assert.That(obj.FieldListString.Values[1], Is.EqualTo("- 'come get some' - 'come get some' -"));
            Assert.That(obj.FieldListString.Values[2], Is.EqualTo("+ 'come get some' + 'come get some' + 'come get some' +"));

            Assert.That(obj.PropertyListString.Values[0], Is.EqualTo("* 'come get some' *"));
            Assert.That(obj.PropertyListString.Values[1], Is.EqualTo("- 'come get some' - 'come get some' -"));
            Assert.That(obj.PropertyListString.Values[2], Is.EqualTo("+ 'come get some' + 'come get some' + 'come get some' +"));
        }

        [Test]
        public void CanResolveAcrossDifferentParameters()
        {
            var root = new ObjectClass();
            var child = new ObjectClass();
            var grandChild = new ObjectClass();

            root.StringValue1 = "RootStringValue1";
            root.StringValue2 = "[child.StringValue1]";

            child.StringValue1 = "ChildStringValue1";
            child.StringValue2 = "[root.StringValue1]";

            grandChild.StringValue1 = "[root.StringValue1] [child.StringValue1]";
            grandChild.StringValue2 = "[root.ChildObject.StringValue1] [root.ChildObject.StringValue2]";

            root.ChildObject = child;
            child.ChildObject = grandChild;

            var resolver = new Resolver();
            resolver.SetParameter("root", root);
            resolver.SetParameter("child", child);

            resolver.Resolve(root);

            Assert.That(root.StringValue1, Is.EqualTo("RootStringValue1"));
            Assert.That(root.StringValue2, Is.EqualTo("ChildStringValue1"));

            Assert.That(child.StringValue1, Is.EqualTo("ChildStringValue1"));
            Assert.That(child.StringValue2, Is.EqualTo("RootStringValue1"));

            Assert.That(grandChild.StringValue1, Is.EqualTo("RootStringValue1 ChildStringValue1"));
            Assert.That(grandChild.StringValue2, Is.EqualTo("ChildStringValue1 RootStringValue1"));
        }

        // Map Test
        [Test]
        public void CanAccessDictionnaryKeysAsProperties()
        {
            var dict = new DictionaryClass();
            dict.Dictionary["element"] = "yeah";

            var dictionary = new Dictionary<string, int>();
            dictionary["one"] = 1;
            dictionary["two"] = 2;
            dictionary["three"] = 3;
            dictionary["four"] = 4;

            var resolver = new Resolver();
            resolver.SetParameter("i", dictionary);
            resolver.SetParameter("dict", dict);

            var result = resolver.Resolve("-> [dict.Dictionary.element] [i.one] [i.two] [i.three] [i.four] <-");

            Assert.That(result, Is.EqualTo("-> yeah 1 2 3 4 <-"));
        }

        private class Test9Foo
        {
            public int Value = 1;
        }

        [Test]
        public void CanResolveASimpleString()
        {
            var foo = new Test9Foo();
            const string str = "foo.Value = [foo.Value]";

            var resolver = new Resolver();
            resolver.SetParameter("foo", foo);

            Assert.That(resolver.Resolve(str), Is.EqualTo("foo.Value = 1"));
        }


        [Test]
        public void CanResolveStrings()
        {
            var resolver = new Resolver();

            var list = new Strings { Separator = "\r\n" };
            list.Add("a");
            list.Add("c");
            list.Add("b");

            resolver.SetParameter("list", list);

            // Please note that the list is sorted before being printed
            Assert.That(resolver.Resolve("[list]"), Is.EqualTo("a\r\nb\r\nc"));

            var list2 = new Strings();
            list2.Separator = ";";
            list2.Add("animal");
            list2.Add("copper");
            list2.Add("brute");

            // We replace previous list
            resolver.SetParameter("list", list2);

            Assert.That(resolver.Resolve("[list]"), Is.EqualTo("animal;brute;copper"));
        }

        [Test]
        public void CanResolveStringsToLower()
        {
            var obj = new PropertyClass();
            obj.Value1 = "[obj]";
            obj.Value2 = "[lower:obj]";
            obj.Value3 = "[lower:obj.Value1]";

            var resolver = new Resolver();
            resolver.SetParameter("obj", obj);
            resolver.Resolve(obj);

            Assert.That(obj.Value1, Is.EqualTo("PropertyClass"));
            Assert.That(obj.Value2, Is.EqualTo("propertyclass"));
            Assert.That(obj.Value3, Is.EqualTo("propertyclass"));
        }

        [Test]
        public void CanResolveStringsEscape()
        {
            var obj = new PropertyClass();
            obj.Value1 = "<stuff>";

            string someTemplate = @"<Field>[EscapeXML:obj.Value1]</Field>";

            var resolver = new Resolver();
            resolver.SetParameter("obj", obj);
            string result = resolver.Resolve(someTemplate);

            Assert.AreEqual("<Field>&lt;stuff&gt;</Field>", result);
        }
    }
}

