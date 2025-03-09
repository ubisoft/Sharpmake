// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    public class CommandLineTest
    {
        [Test, Sequential]
        public void ParsesOneParameter([Values("/toto",
                                               "  \t   \t/\t   \ttoto\t  ",
                                               "/toto()")] string commandLine)
        {
            CommandLine.Parameter[] p = CommandLine.Parse(commandLine);
            Assert.That(p, Has.Length.EqualTo(1));
            Assert.That(p[0].Name, Is.EqualTo("toto"));
            Assert.That(p[0].Args, Is.Empty);
        }

        [Test]
        public void ParsesOneParameterWithVoidArgument()
        {
            CommandLine.Parameter[] p = CommandLine.Parse("/toto\t   \t\t(  \t\t ) \t\t \t");
            Assert.That(p, Has.Length.EqualTo(1));
            Assert.That(p[0].Name, Is.EqualTo("toto"));
            Assert.That(p[0].Args, Is.Empty);
            Assert.That(p[0].ArgsCount, Is.EqualTo(0));
        }

        [Test, Sequential]
        public void ParsesTwoParameters([Values("/toto /tata", " /  toto   /  tata  ", " /  toto (    )  /  tata (   )  ")] string commandLine)
        {
            CommandLine.Parameter[] p = CommandLine.Parse(commandLine);
            Assert.That(p, Has.Length.EqualTo(2));

            Assert.That(p[0].Name, Is.EqualTo("toto"));
            Assert.That(p[0].Args, Is.Empty);
            Assert.That(p[0].ArgsCount, Is.EqualTo(0));

            Assert.That(p[1].Name, Is.EqualTo("tata"));
            Assert.That(p[1].Args, Is.Empty);
            Assert.That(p[1].ArgsCount, Is.EqualTo(0));
        }

        [Test]
        public void ParsesOneParameterWithArgument()
        {
            CommandLine.Parameter[] p = CommandLine.Parse("/toto(1)");

            Assert.That(p, Has.Length.EqualTo(1));

            Assert.That(p[0].Name, Is.EqualTo("toto"));
            Assert.That(p[0].Args, Is.EqualTo("1"));
            Assert.That(p[0].ArgsCount, Is.EqualTo(1));
        }

        [Test]
        public void ParsesOneParameterWithComplexeArgument()
        {
            CommandLine.Parameter[] p = CommandLine.Parse("/toto(project.ToString())");

            Assert.That(p, Has.Length.EqualTo(1));

            Assert.That(p[0].Name, Is.EqualTo("toto"));
            Assert.That(p[0].Args, Is.EqualTo("project.ToString()"));
            Assert.That(p[0].ArgsCount, Is.EqualTo(1));
        }

        [Test]
        public void ParsesTwoParametersWithComplexeArguments()
        {
            CommandLine.Parameter[] p = CommandLine.Parse("/toto(project.ToString())/tata(project2.ToString())");

            Assert.That(p, Has.Length.EqualTo(2));

            Assert.That(p[0].Name, Is.EqualTo("toto"));
            Assert.That(p[0].Args, Is.EqualTo("project.ToString()"));
            Assert.That(p[0].ArgsCount, Is.EqualTo(1));

            Assert.That(p[1].Name, Is.EqualTo("tata"));
            Assert.That(p[1].Args, Is.EqualTo("project2.ToString()"));
            Assert.That(p[1].ArgsCount, Is.EqualTo(1));
        }

        public class Foo
        {
            public string ReceivedString = "";
            public int ReceivedInt;

            [CommandLine.Option("Test")]
            public void CommandLineTest(int p0)
            {
                ReceivedInt = p0;
            }

            [CommandLine.Option("Test")]
            public void CommandLineTest(string p0)
            {
                ReceivedString = p0;
            }
        }

        [Test]
        public void CanCallMethodsWithOptionAttribute()
        {
            var foo = new Foo();

            Assert.That(foo.ReceivedInt, Is.EqualTo(0));
            Assert.That(foo.ReceivedString, Is.Empty);

            CommandLine.ExecuteOnObject(foo, "/Test(\"123123\")");
            CommandLine.ExecuteOnObject(foo, "/Test(123123)");

            Assert.That(foo.ReceivedInt, Is.EqualTo(123123));
            Assert.That(foo.ReceivedString, Is.EqualTo("123123"));
        }
    }
}

