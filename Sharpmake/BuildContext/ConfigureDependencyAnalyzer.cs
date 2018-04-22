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
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sharpmake.BuildContext
{
    public struct ConfigureSignature
    {
        public readonly Type DeclaringType;
        public readonly ConfigurePriority Priority;
        public readonly string Signature;
        public readonly string DependencySignature;

        public ConfigureSignature(Type declaringType, ConfigurePriority priority, string signature, string dependencySignature)
        {
            DeclaringType = declaringType;
            Priority = priority;
            Signature = signature;
            DependencySignature = dependencySignature;
        }
    }

    internal class ConfigureDependencyAnalyzer
    {
        #region Test Cases type definition

        private abstract class BaseTestCase : BaseBuildContext
        {
            private IEnumerable<Type> _toGenerate;

            protected BaseTestCase(IEnumerable<Type> toGenerate)
            {
                _toGenerate = toGenerate;
            }

            public virtual IEnumerable<Type> TargetTypes
            {
                get { return _toGenerate; }
            }

            public abstract IEnumerable<BaseTestCase> GetNextStep(IDictionary<Type, GenerationOutput> outputs, out IEnumerable<ConfigureSignature> suspectedMethods);

            public override bool HaveToGenerate(Type type)
            {
                return _toGenerate.Contains(type);
            }

            public override bool WriteGeneratedFile(Type type, FileInfo path, MemoryStream generated)
            {
                return Util.IsFileDifferent(path, generated);
            }

            public override bool WriteLog
            {
                get { return false; }
            }
        }

        /// <summary>
        /// Tests a specific method to find it's closest dependency.
        /// Doesn't find all dependencies.
        /// </summary>
        private class MethodTestCase : BaseTestCase
        {
            private readonly Type _type;
            private readonly ConfigurePriority _priority;
            private IEnumerable<string> _methods;
            private readonly string _method;
            private int _max;
            private int _testIndex = 0;
            private int _min;

            /// <summary>
            /// Creates method test cases based on the information given
            /// </summary>
            /// <param name="type">Type of the project or solution to analyze</param>
            /// <param name="priority">Priority group of the methods</param>
            /// <param name="methods">Methods to analyze</param>
            /// <returns>The test cases to execute</returns>
            public static IEnumerable<BaseTestCase> CreateMethodTestCase(Type type, ConfigurePriority priority, IEnumerable<string> methods)
            {
                int i = 0;
                foreach (var method in methods.Skip(1))
                {
                    yield return new MethodTestCase(type, priority, method, methods, ++i);
                }
            }

            /// <summary>
            /// Creates method test cases based on the information given
            /// Should be call if you know that the methods contains an error.
            /// </summary>
            /// <param name="type">Type of the project or solution to analyze</param>
            /// <param name="priority">Priority group of the methods</param>
            /// <param name="methods">Methods to analyze</param>
            /// <param name="suspectedMethods">[out] The methods suspected before processing if any</param>
            /// <returns>The test cases to execute</returns>
            public static IEnumerable<BaseTestCase> CreateMethodTestCase(
                Type type,
                ConfigurePriority priority,
                IEnumerable<string> methods,
                out IEnumerable<ConfigureSignature> suspectedMethods)
            {
                if (methods.Count() > 2)
                {
                    suspectedMethods = Enumerable.Empty<ConfigureSignature>();
                    return CreateMethodTestCase(type, priority, methods);
                }
                else
                {
                    suspectedMethods = new[] { new ConfigureSignature(type, priority, methods.Last(), methods.First()) };
                    return Enumerable.Empty<BaseTestCase>();
                }
            }

            private MethodTestCase(Type type, ConfigurePriority priority, string method, IEnumerable<string> methods, int index)
                : base(new[] { type })
            {
                _type = type;
                _priority = priority;
                _method = method;
                _methods = methods;
                _max = index;
                _min = 0;
            }

            private MethodTestCase(Type type, ConfigurePriority priority, string method, IEnumerable<string> methods, int max, int min, int testIndex)
                : this(type, priority, method, methods, max)
            {
                _min = min;
                _testIndex = testIndex;
            }

            #region IAnalysisBuildContext implementation

            public override IEnumerable<BaseTestCase> GetNextStep(IDictionary<Type, GenerationOutput> outputs, out IEnumerable<ConfigureSignature> suspectedMethods)
            {
                var output = outputs[_type];
                bool generatedError = output.Generated.Count != 0 || output.Exception != null;
                int nextMax = (generatedError) ? _max : _testIndex;
                int nextMin = (generatedError) ? _testIndex : _min;
                int step = (nextMax - nextMin) / 2;

                if ((generatedError || _testIndex != 0) && step == 0)
                {
                    suspectedMethods = new[] { new ConfigureSignature(_type, _priority, _method, _methods.Skip(nextMin).First()) };
                }
                else
                {
                    suspectedMethods = Enumerable.Empty<ConfigureSignature>();
                }

                if (step != 0)
                {
                    return new[] { new MethodTestCase(_type, _priority, _method, _methods, nextMax, nextMin, nextMin + step) };
                }
                else
                {
                    return Enumerable.Empty<BaseTestCase>();
                }
            }

            #endregion

            #region IBuildContext implementation

            internal override IEnumerable<MethodInfo> OrderConfigure(Type type, ConfigurePriority priority, IEnumerable<MethodInfo> configures)
            {
                if (type.Equals(_type) && priority.Equals(_priority))
                {
                    var reordered = new List<MethodInfo>();

                    foreach (var configure in configures)
                    {
                        if (configure.ToString().Equals(_method))
                        {
                            reordered.Insert(_testIndex, configure);
                        }
                        else
                        {
                            reordered.Add(configure);
                        }
                    }

                    return reordered;
                }

                return configures;
            }
            #endregion


            public override string ToString()
            {
                return _type.Name + "." + _priority.Priority + "." + _method.Substring(5, _method.IndexOf('(') - 5) + " at " + _testIndex;
            }
        }

        /// <summary>
        /// Tests a specific priority if either is contains dependent methods or not.
        /// </summary>
        private class PriorityTestCase : BaseTestCase
        {
            private readonly Type _type;
            private readonly ConfigurePriority _priority;
            private readonly IEnumerable<string> _methods;

            /// <summary>
            /// Creates tests case for all given priority of needed.
            /// </summary>
            /// <param name="type">Source Type of the priorities and methods</param>
            /// <param name="priorities">Priorities to analyze</param>
            /// <returns>a list of test case to execute</returns>
            public static IEnumerable<BaseTestCase> CreatePriorityTestCase(Type type, IEnumerable<KeyValuePair<ConfigurePriority, IEnumerable<string>>> priorities)
            {
                return priorities.Select(priority => new PriorityTestCase(type, priority.Key, priority.Value));
            }

            /// <summary>
            /// Creates the tests case to analyze a list of priority.
            /// Must be used only if you are sure the type have dependent methods
            /// </summary>
            /// <param name="type">Type to analyze</param>
            /// <param name="priorities">Priorities to analyze</param>
            /// <param name="suspectedMethods">[out] the methods suspected before the first iteration</param>
            /// <returns>a list of test case to execute</returns>
            public static IEnumerable<BaseTestCase> CreatePriorityTestCase(
                Type type,
                IEnumerable<KeyValuePair<ConfigurePriority, IEnumerable<string>>> priorities,
                out IEnumerable<ConfigureSignature> suspectedMethods)
            {
                if (priorities.Count() > 1)
                {
                    // if there is more than one priority we can't suspect anything
                    suspectedMethods = Enumerable.Empty<ConfigureSignature>();
                    return priorities.Select(priority => new PriorityTestCase(type, priority.Key, priority.Value));
                }
                else
                {
                    var priority = priorities.First();
                    return MethodTestCase.CreateMethodTestCase(type, priority.Key, priority.Value, out suspectedMethods);
                }
            }

            private PriorityTestCase(Type type, ConfigurePriority priority, IEnumerable<string> methods)
                : base(new[] { type })
            {
                _type = type;
                _priority = priority;
                _methods = methods;
            }

            #region IAnalysisBuildContext implementation

            public override IEnumerable<BaseTestCase> GetNextStep(IDictionary<Type, GenerationOutput> outputs, out IEnumerable<ConfigureSignature> suspectedMethods)
            {
                var output = outputs[_type];
                if (output.Generated.Count != 0 || output.Exception != null)
                {
                    return MethodTestCase.CreateMethodTestCase(_type, _priority, _methods, out suspectedMethods);
                }
                else
                {
                    suspectedMethods = Enumerable.Empty<ConfigureSignature>();
                    return Enumerable.Empty<BaseTestCase>();
                }
            }

            #endregion

            #region IBuildContext implementation}

            internal override IEnumerable<MethodInfo> OrderConfigure(Type type, ConfigurePriority priority, IEnumerable<MethodInfo> configures)
            {
                if (type == _type && priority == _priority)
                {
                    return configures.Reverse();
                }

                return configures;
            }

            public override bool WriteGeneratedFile(Type type, FileInfo path, MemoryStream generated)
            {
                return Util.IsFileDifferent(path, generated);
            }

            #endregion

            public override string ToString()
            {
                return _type.Name + "." + _priority.Priority;
            }
        }

        /// <summary>
        /// Tests a specific type if either is contains dependent methods or not.
        /// </summary>
        private class TypeTestCase : BaseTestCase
        {
            private readonly Dictionary<Type, Dictionary<ConfigurePriority, IEnumerable<string>>> _toAnalyze = new Dictionary<Type, Dictionary<ConfigurePriority, IEnumerable<string>>>();

            internal TypeTestCase(ConcurrentDictionary<Type, ConcurrentDictionary<ConfigurePriority, IEnumerable<string>>> toAnalyze)
                : base(toAnalyze.Where(type => type.Value.Any(p => p.Value.Count() > 1)).Select(type => type.Key))
            {
                foreach (var type in toAnalyze)
                {
                    var prioritiesToKeep = new Dictionary<ConfigurePriority, IEnumerable<string>>();
                    foreach (var priority in type.Value.Where(p => p.Value.Count() > 1))
                    {
                        prioritiesToKeep.Add(priority.Key, priority.Value);
                    }

                    if (prioritiesToKeep.Count > 0)
                    {
                        _toAnalyze.Add(type.Key, prioritiesToKeep);
                    }
                }
            }

            #region IAnalysisBuildContext implementation

            public override IEnumerable<BaseTestCase> GetNextStep(IDictionary<Type, GenerationOutput> outputs, out IEnumerable<ConfigureSignature> suspectedMethods)
            {
                var newTests = new List<BaseTestCase>();
                var suspectedTotal = new List<ConfigureSignature>();
                IEnumerable<ConfigureSignature> typeSuspectedMethods;
                foreach (var type in _toAnalyze)
                {
                    var output = outputs[type.Key];
                    if (output.Generated.Count > 0 || output.Exception != null)
                    {
                        newTests.AddRange(PriorityTestCase.CreatePriorityTestCase(type.Key, type.Value, out typeSuspectedMethods));
                        suspectedTotal.AddRange(typeSuspectedMethods);
                    }
                }

                suspectedMethods = suspectedTotal;
                return newTests;
            }

            #endregion

            #region IBuildContext implementation

            internal override IEnumerable<MethodInfo> OrderConfigure(Type type, ConfigurePriority priority, IEnumerable<MethodInfo> configures)
            {
                return configures.Reverse();
            }

            #endregion

            public override string ToString()
            {
                return string.Join(Environment.NewLine, _toAnalyze.Select(t => t.Key.Name));
            }
        }

        /// <summary>
        /// First pass of the whole analysis. Builds and array of all the content to be analyzed
        /// </summary>
        private class AnalysisFirstPass : BaseTestCase
        {
            private ConcurrentDictionary<Type, ConcurrentDictionary<ConfigurePriority, IEnumerable<string>>> _typeDefinitions = new ConcurrentDictionary<Type, ConcurrentDictionary<ConfigurePriority, IEnumerable<string>>>();

            public AnalysisFirstPass() : base(Enumerable.Empty<Type>()) { }

            #region IAnalysisBuildContext implementation

            public override IEnumerable<BaseTestCase> GetNextStep(IDictionary<Type, GenerationOutput> outputs, out IEnumerable<ConfigureSignature> suspectedMethods)
            {
                suspectedMethods = Enumerable.Empty<ConfigureSignature>();
                var nonGeneratedTypes = _typeDefinitions.Where(type => !outputs.ContainsKey(type.Key));

                ConcurrentDictionary<ConfigurePriority, IEnumerable<string>> onRemoveDuty;
                foreach (var type in nonGeneratedTypes)
                {
                    _typeDefinitions.TryRemove(type.Key, out onRemoveDuty);
                }

                return new[] { new TypeTestCase(_typeDefinitions) };
            }

            public override IEnumerable<Type> TargetTypes
            {
                get { return _typeDefinitions.Keys; }
            }

            #endregion

            #region IBuildContext implementation

            public override bool HaveToGenerate(Type type)
            {
                return true;
            }

            internal override IEnumerable<MethodInfo> OrderConfigure(Type type, ConfigurePriority priority, IEnumerable<MethodInfo> configures)
            {
                var priorities = _typeDefinitions.GetOrAdd(type, new ConcurrentDictionary<ConfigurePriority, IEnumerable<string>>());
                priorities.TryAdd(priority, configures.Select(m => m.ToString()));

                return configures;
            }

            public override bool WriteGeneratedFile(Type type, FileInfo path, MemoryStream generated)
            {
                return Util.FileWriteIfDifferentInternal(path, generated);
            }

            public override string ToString()
            {
                return "First pass analyze";
            }

            #endregion
        }

        private class AnalyzerComposite : BaseTestCase, ICollection<BaseTestCase>
        {
            private readonly Collection<BaseTestCase> _analyzers = new Collection<BaseTestCase>();

            public AnalyzerComposite() : base(Enumerable.Empty<Type>()) { }

            #region IAnalysisBuildContext implementation

            public override IEnumerable<BaseTestCase> GetNextStep(IDictionary<Type, GenerationOutput> outputs, out IEnumerable<ConfigureSignature> suspectedMethods)
            {
                var nextSteps = new Collection<BaseTestCase>();
                var totalSuspected = new List<ConfigureSignature>();

                foreach (var analyzer in _analyzers.Where(analyzer => analyzer.TargetTypes.Any(type => outputs.Keys.Contains(type))))
                {
                    IEnumerable<ConfigureSignature> suspected;
                    foreach (var step in analyzer.GetNextStep(outputs, out suspected))
                    {
                        nextSteps.Add(step);
                    }
                    totalSuspected.AddRange(suspected);
                }

                suspectedMethods = totalSuspected;
                return nextSteps;
            }

            public override IEnumerable<Type> TargetTypes
            {
                get
                {
                    foreach (var analyzer in _analyzers)
                    {
                        foreach (var type in analyzer.TargetTypes)
                        {
                            yield return type;
                        }
                    }
                }
            }

            #endregion

            #region IBuildContext implementation


            public override bool HaveToGenerate(Type type)
            {
                return _analyzers.Any(analyzer => analyzer.HaveToGenerate(type));
            }

            internal override IEnumerable<MethodInfo> OrderConfigure(Type type, ConfigurePriority priority, IEnumerable<MethodInfo> configures)
            {
                foreach (var analyzer in _analyzers)
                {
                    if (analyzer.HaveToGenerate(type))
                    {
                        return analyzer.OrderConfigure(type, priority, configures);
                    }
                }

                return configures;
            }

            public override bool WriteGeneratedFile(Type type, FileInfo path, MemoryStream generated)
            {
                return _analyzers.Any(analyzer => analyzer.TargetTypes.Contains(type) && analyzer.WriteGeneratedFile(type, path, generated));
            }

            #endregion

            #region ICollection<IAnalysisBuildContext> implementation

            public void Add(BaseTestCase item)
            {
                _analyzers.Add(item);
            }

            public void Clear()
            {
                _analyzers.Clear();
            }

            public bool Contains(BaseTestCase item)
            {
                return _analyzers.Contains(item);
            }

            public void CopyTo(BaseTestCase[] array, int arrayIndex)
            {
                _analyzers.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return _analyzers.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public bool Remove(BaseTestCase item)
            {
                return _analyzers.Remove(item);
            }

            public IEnumerator<BaseTestCase> GetEnumerator()
            {
                return _analyzers.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            public override string ToString()
            {
                return string.Join(Environment.NewLine, _analyzers.Select(a => a.ToString()).OrderBy(s => s));
            }
        }

        #endregion

        private readonly Func<BaseBuildContext, IDictionary<Type, GenerationOutput>> _generationFactory;

        internal ConfigureDependencyAnalyzer(Func<BaseBuildContext, IDictionary<Type, GenerationOutput>> generationFactory)
        {
            _generationFactory = generationFactory;
        }

        public IEnumerable<ConfigureSignature> Analyze(TextWriter messageOutput, bool stopOnFirstError)
        {
            var allSuspectedMethods = new List<ConfigureSignature>();
            var currentTest = new AnalyzerComposite { new AnalysisFirstPass() };
            var tests = new List<AnalyzerComposite>() { currentTest };
            IDictionary<Type, GenerationOutput> result;
            do
            {
                result = _generationFactory(currentTest);
                IEnumerable<ConfigureSignature> suspectedMethods;
                foreach (var newAnalyzer in currentTest.GetNextStep(result, out suspectedMethods))
                {
                    AddAnalyzer(tests, newAnalyzer);
                }

                allSuspectedMethods.AddRange(suspectedMethods);

                if (messageOutput != null)
                {
                    messageOutput.WriteLine(BuildCurrentAnalysisMessage(currentTest, allSuspectedMethods));
                }

                tests.RemoveAt(0);
                currentTest = tests.FirstOrDefault();
            } while (currentTest != null && !(stopOnFirstError && result.Any(t => t.Value.HasChanged)));

            return allSuspectedMethods;
        }

        private static void AddAnalyzer(List<AnalyzerComposite> tests, BaseTestCase analyzer)
        {
            foreach (var composite in tests)
            {
                if (!analyzer.TargetTypes.Any(composite.TargetTypes.Contains))
                {
                    composite.Add(analyzer);
                    return;
                }
            }
            tests.Add(new AnalyzerComposite() { analyzer });
        }

        private static string BuildCurrentAnalysisMessage(BaseTestCase test, IEnumerable<ConfigureSignature> suspectedMethods)
        {
            var builder = new StringBuilder();

            if (test != null)
            {
                builder.AppendLine(test.ToString());
                builder.AppendFormat("{0} types tested, {1} suspected methods" + Environment.NewLine + Environment.NewLine, test.TargetTypes.Count(), suspectedMethods.Count());
            }
            else
            {
                builder.AppendLine("Nothing analyzed yet");
            }

            return builder.ToString();
        }
    }
}
