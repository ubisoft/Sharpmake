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
using System.IO;
using System.Linq;
using System.Reflection;
using Sharpmake;

namespace SharpmakeUnitTests
{
    public class CSharpTestProjectBuilder
    {
        private readonly string _namespace;

        public CSharpTestProjectBuilder(string buildNamespace)
        {
            _namespace = buildNamespace;
        }

        public Builder Builder { get; private set; }

        [OneTimeSetUp]
        public void Init()
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

            // Force the test to load and register CommonPlatforms.dll as a Sharpmake extension
            // because sometimes you get the "no implementation of XX for platform YY."
            var platformDotNetType = typeof(DotNetPlatform);
            PlatformRegistry.RegisterExtensionAssembly(platformDotNetType.Assembly);

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
            var sharpmakeProjects = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && t.Namespace == _namespace);

            // Also create some random source files
            Util.FakePathPrefix = Directory.GetCurrentDirectory();
            foreach (var sharpmakeProject in sharpmakeProjects)
            {
                Util.AddNewFakeFile(Util.PathMakeStandard(Path.Combine(sharpmakeProject.Name, sharpmakeProject.Name + "_source.cs")), 0);
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
}
