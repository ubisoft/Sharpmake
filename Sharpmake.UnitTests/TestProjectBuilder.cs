// Copyright (c) 2017 Ubisoft Entertainment
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
using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Sharpmake.UnitTests
{
    public class TestProjectBuilder
    {
        private readonly string _namespace;

        protected TestProjectBuilder(string buildNamespace)
        {
            _namespace = buildNamespace;
        }

        public Builder Builder { get; private set; }

        protected enum InitType
        {
            Cpp,
            CSharp
        }

        protected void Init(InitType initType)
        {
            bool debugLog = true;
            bool multithreaded = false;
            bool writeFiles = false;
            bool dumpDependency = true;

            DependencyTracker.GraphWriteLegend = false;

            Builder = new Builder(
                new Sharpmake.BuildContext.GenerateAll(debugLog, writeFiles),
                multithreaded,
                dumpDependency,
                false,
                false,
                false,
                false,
                GetGeneratorsManager,
                null
            );

            var fakeFileExtensions = new List<string>();

            // Force the test to load and register CommonPlatforms.dll as a Sharpmake extension
            // because sometimes you get the "no implementation of XX for platform YY."
            switch (initType)
            {
                case InitType.Cpp:
                    {
                        // HACK: Explicitely reference something from CommonPlatforms to get
                        // visual studio to load the assembly
                        var platformWin64Type = typeof(Windows.Win64Platform);
                        PlatformRegistry.RegisterExtensionAssembly(platformWin64Type.Assembly);

                        fakeFileExtensions.Add("_source.cpp");
                        fakeFileExtensions.Add("_header.h");
                    }
                    break;
                case InitType.CSharp:
                    {
                        var platformDotNetType = typeof(DotNetPlatform);
                        PlatformRegistry.RegisterExtensionAssembly(platformDotNetType.Assembly);

                        fakeFileExtensions.Add("_source.cs");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(initType), initType, null);
            }

            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            // Allow message log from builder.
            Builder.OutputDelegate log = (msg, args) =>
            {
                Console.Write(msg, args);
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Trace.Write(string.Format(msg, args));
            };
            Builder.EventOutputError += log;
            Builder.EventOutputWarning += log;
            Builder.EventOutputMessage += log;
            Builder.EventOutputDebug += log;

            ////////////////////////////////////////////////////////////////////
            // Register projects to generate here
            var sharpmakeProjects = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Namespace == _namespace);

            // Also create some random source files
            Util.FakePathPrefix = Directory.GetCurrentDirectory();

            foreach (var sharpmakeProject in sharpmakeProjects)
            {
                foreach (string fakeFileExtension in fakeFileExtensions)
                    Util.AddNewFakeFile(Util.PathMakeStandard(Path.Combine(sharpmakeProject.Name, sharpmakeProject.Name + fakeFileExtension)), 0);
            }

            foreach (var sharpmakeProject in sharpmakeProjects)
            {
                Builder.Arguments.Generate(sharpmakeProject);
            }
            ////////////////////////////////////////////////////////////////////

            Builder.BuildProjectAndSolution();

            var outputs = Builder.Generate();
            if (dumpDependency)
                DependencyTracker.Instance.DumpGraphs(outputs);
        }

        [OneTimeTearDown]
        public void Shutdown()
        {
            Builder.Dispose();
        }

        public static IGeneratorManager GetGeneratorsManager()
        {
            return new Sharpmake.Generators.GeneratorManager();
        }

        public Project GetProject<T>()
        {
            return Builder._projects[typeof(T)];
        }
    }


    public class CSharpTestProjectBuilder : TestProjectBuilder
    {
        public CSharpTestProjectBuilder(string buildNamespace)
            : base(buildNamespace) { }

        [OneTimeSetUp]
        public void CSharpInit()
        {
            Init(InitType.CSharp);
        }
    }

    public class CppTestProjectBuilder : TestProjectBuilder
    {
        public CppTestProjectBuilder(string buildNamespace)
            : base(buildNamespace) { }

        [OneTimeSetUp]
        public void CppInit()
        {
            Init(InitType.Cpp);
        }
    }

}
