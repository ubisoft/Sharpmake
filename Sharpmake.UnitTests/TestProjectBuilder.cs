// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Sharpmake.UnitTests
{
    public class TestProjectBuilder
    {
        private readonly InitType _initType;
        private readonly string _namespace;
        private readonly ConfigureOrder _configureOrder;

        protected int WarningCount { get; private set; } = 0;
        protected int ErrorCount { get; private set; } = 0;

        protected TestProjectBuilder(
            InitType initType,
            string buildNamespace,
            ConfigureOrder configureOrder
        )
        {
            _initType = initType;
            _namespace = buildNamespace;
            _configureOrder = configureOrder;
        }

        public Builder Builder { get; private set; }

        protected enum InitType
        {
            Cpp,
            CSharp
        }

        [TearDown]
        public void TearDown()
        {
            ErrorCount = 0;
            WarningCount = 0;
        }

        protected void Init()
        {
            bool debugLog = true;
            bool multithreaded = false;
            bool writeFiles = false;
            bool dumpDependency = true;

            DependencyTracker.ResetSingleton();
            DependencyTracker.GraphWriteLegend = false;

            Builder = new Builder(
                new BuildContext.GenerateAll(debugLog, writeFiles),
                multithreaded,
                dumpDependency,
                false,
                false,
                false,
                false,
                true,
                GetGeneratorsManager,
                null
            );

            Builder.Arguments.ConfigureOrder = _configureOrder;

            // Force the test to load and register CommonPlatforms.dll as a Sharpmake extension
            // because sometimes you get the "no implementation of XX for platform YY."
            switch (_initType)
            {
                case InitType.Cpp:
                    {
                        // HACK: Explicitly reference something from CommonPlatforms to get
                        // visual studio to load the assembly
                        var platformWin64Type = typeof(Windows.Win64Platform);
                        PlatformRegistry.RegisterExtensionAssembly(platformWin64Type.Assembly);
                    }
                    break;
                case InitType.CSharp:
                    {
                        var platformDotNetType = typeof(DotNetPlatform);
                        PlatformRegistry.RegisterExtensionAssembly(platformDotNetType.Assembly);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_initType), _initType, null);
            }

            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            // Allow message log from builder.
            Builder.EventOutputError += (message, args) => { ++ErrorCount; Util.LogWrite(message, args); };
            Builder.EventOutputWarning += (message, args) => { ++WarningCount; Util.LogWrite(message, args); };
            Builder.EventOutputMessage += Util.LogWrite;
            Builder.EventOutputDebug += Util.LogWrite;
        }

        private void AddFakeFiles(IEnumerable<Type> sharpmakeProjects)
        {
            Util.FakePathPrefix = Directory.GetCurrentDirectory();

            var fakeFileExtensions = new List<string>();
            switch (_initType)
            {
                case InitType.Cpp:
                    fakeFileExtensions.Add("_source.cpp");
                    fakeFileExtensions.Add("_header.h");
                    break;
                case InitType.CSharp:
                    fakeFileExtensions.Add("_source.cs");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (var sharpmakeProject in sharpmakeProjects)
            {
                foreach (string fakeFileExtension in fakeFileExtensions)
                    Util.AddNewFakeFile(Util.PathMakeStandard(Path.Combine(sharpmakeProject.Name, sharpmakeProject.Name + fakeFileExtension)), 0);
            }
        }

        protected void GenerateAndBuildProjects()
        {
            IEnumerable<Type> sharpmakeProjects = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Namespace == _namespace);
            GenerateAndBuildProjects(sharpmakeProjects);
        }

        protected void GenerateAndBuildProjects(IEnumerable<Type> sharpmakeProjects)
        {
            // Create some random source files
            AddFakeFiles(sharpmakeProjects);

            foreach (var sharpmakeProject in sharpmakeProjects)
                Builder.Arguments.Generate(sharpmakeProject);

            Builder.BuildProjectAndSolution();

            var outputs = Builder.Generate();
            if (Builder.DumpDependencyGraph)
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
        public CSharpTestProjectBuilder(string buildNamespace, ConfigureOrder configureOrder = ConfigureOrder.New)
            : base(InitType.CSharp, buildNamespace, configureOrder) { }

        [OneTimeSetUp]
        public void CSharpInit()
        {
            Init();
            GenerateAndBuildProjects();
        }
    }

    public class CppTestProjectBuilder : TestProjectBuilder
    {
        public CppTestProjectBuilder(string buildNamespace, ConfigureOrder configureOrder = ConfigureOrder.New)
            : base(InitType.Cpp, buildNamespace, configureOrder) { }

        [OneTimeSetUp]
        public void CppInit()
        {
            Init();
            GenerateAndBuildProjects();
        }
    }
}
