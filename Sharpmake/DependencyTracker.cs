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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sharpmake
{
    internal class DependencyTracker
    {
        public static DependencyTracker Instance { get; private set; } = new DependencyTracker();

        public static bool ShowDependenciesFromExtern = true;
        public static bool ShowDependenciesToExtern = true;

        public static bool GraphWriteLegend = true;

        public const string ColorExe = "lightcyan";
        public const string ColorDll = "pink";
        public const string ColorLib = "lemonchiffon";
        public const string ColorExternExe = ColorExe + "3";
        public const string ColorExternDll = ColorDll + "3";
        public const string ColorExternLib = ColorLib + "3";
        public const string ColorUnkownOutputType = "white";

        public const string ColorPublicDep = "green";
        public const string ColorPrivateDep = "red";

        public const string ShapeProject = "egg";
        public const string ShapeExtern = "diamond";
        public const string ShapeUnknown = "octagon";

        public const int WidthExternDep = 1;
        public const int WidthDep = 2;

        public DependencyTracker()
        {
            _projects = new Dictionary<string, TrackedProject>();
        }

        public static void ResetSingleton()
        {
            Instance = new DependencyTracker();
        }

        public void UpdateConfiguration(Project project, Project.Configuration config)
        {
            lock (this)
            {
                foreach (KeyValuePair<string, TrackedProject> p in _projects)
                {
                    if (p.Key == project.ToString())
                    {
                        p.Value.AddConfig(config);
                        return;
                    }
                }

                _projects.Add(project.ToString(), new TrackedProject(project, config));
            }
        }

        public void AddDependency(
            DependencyType dependencyType,
            Project projectFrom,
            Project.Configuration configFrom,
            IEnumerable<KeyValuePair<Type, ITarget>> dependencies,
            IDictionary<KeyValuePair<Type, ITarget>, DependencySetting> dependenciesSetting
        )
        {
            lock (this)
            {
                foreach (KeyValuePair<Type, ITarget> pair in dependencies)
                {
                    TrackedConfiguration confFrom = FindConfiguration(projectFrom, configFrom);
                    TrackedConfiguration confTo = FindConfiguration(pair.Key, pair.Value);

                    DependencySetting dependencySetting;
                    if (!dependenciesSetting.TryGetValue(pair, out dependencySetting))
                        dependencySetting = DependencySetting.Default;

                    confFrom.AddDependency(confTo, dependencyType, dependencySetting);
                }
            }
        }

        public void DumpGraphs(IDictionary<Type, GenerationOutput> outputs)
        {
            lock (this)
            {
                var dependencyOutput = outputs.GetValueOrAdd(typeof(DependencyTracker), new GenerationOutput());
                foreach (KeyValuePair<string, TrackedProject> pair in _projects)
                {
                    TrackedProject p = pair.Value;
                    foreach (KeyValuePair<string, TrackedConfiguration> confPair in p.Configurations)
                    {
                        TrackedConfiguration c = confPair.Value;
                        if (!c.DumpDependencyGraph())
                            continue;

                        string fileName = @"Dependencies ("
                            + c.GetDisplayedName(false)
                            + ", "
                            + c.GetConfigName()
                            + (DependencyTracker.ShowDependenciesFromExtern ? "" : " [excl from Extern]")
                            + (DependencyTracker.ShowDependenciesToExtern ? "" : " [excl to Extern]")
                        + ").gv";
                        // Open the stream and read it back.

                        MemoryStream memoryStream = new MemoryStream();
                        StreamWriter writer = new StreamWriter(memoryStream);

                        writer.WriteLine("digraph g");
                        writer.WriteLine("{");

                        writer.WriteLine("graph [rankdir = \"TD\" bgcolor = \"lightblue:black\" style=\"filled\" gradientangle = 270 splines=true];");

                        WriteGraph(writer, c, "extStruct", false, false, () =>
                            {
                                foreach (KeyValuePair<string, TrackedProject> proj in _projects)
                                    proj.Value.ResetVisit();
                            }
                        );

                        if (GraphWriteLegend)
                            WriteLegend(writer);

                        writer.WriteLine("}");
                        writer.Flush();

                        var outputFileInfo = new FileInfo(fileName);
                        bool written = Builder.Instance.Context.WriteGeneratedFile(typeof(DependencyTracker), outputFileInfo, memoryStream);
                        if (written)
                            dependencyOutput.Generated.Add(outputFileInfo.FullName);
                        else
                            dependencyOutput.Skipped.Add(outputFileInfo.FullName);
                    }
                }
            }
        }

        private TrackedConfiguration FindConfiguration(Project project, Project.Configuration config)
        {
            TrackedProject p = _projects[project.ToString()];
            return p.FindConfiguration(config);
        }

        private TrackedConfiguration FindConfiguration(Type project, ITarget target)
        {
            TrackedProject p = _projects[project.ToString()];
            return p.FindConfiguration(target);
        }

        private delegate void ResetVisitDelegate();

        private void WriteGraph(StreamWriter stream, TrackedConfiguration root, string externStructName, bool verboseEdges, bool verboseNodes, ResetVisitDelegate ResetVisit)
        {
            try
            {
                ResetVisit();
                root.WriteNodeDescription(stream, false, verboseNodes, "");

                ResetVisit();
                root.WriteNodeDescription(stream, true, verboseNodes, "");

                stream.WriteLine();

                ResetVisit();
                root.WriteDependencies(stream, verboseEdges, "");
                stream.WriteLine();
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e);
            }
        }

        private void WriteLegend(StreamWriter stream)
        {
            stream.WriteLine("subgraph cluster_l{");
            stream.WriteLine("\tgraph[style = \"rounded, filled\", fillcolor = lightyellow];");

            List<TrackedProject> legendProjects = new List<TrackedProject>();
            List<TrackedProject> legendExtProjects = new List<TrackedProject>();
            legendProjects.Add(new TrackedProject("Executable", false, Project.Configuration.OutputType.Exe));
            legendProjects.Add(new TrackedProject("Static_Library1", false, Project.Configuration.OutputType.Lib));
            legendProjects.Add(new TrackedProject("Dll", false, Project.Configuration.OutputType.Dll));
            legendProjects.Add(new TrackedProject("Static_Library2", false, Project.Configuration.OutputType.Lib));
            legendExtProjects.Add(new TrackedProject("Extern_Library1", true, Project.Configuration.OutputType.Lib));
            legendExtProjects.Add(new TrackedProject("Extern_Dll", true, Project.Configuration.OutputType.Dll));
            legendExtProjects.Add(new TrackedProject("Extern_Library2", true, Project.Configuration.OutputType.Lib));
            legendProjects[0].Configurations.First().Value.AddDependency(legendProjects[1].Configurations.First().Value, DependencyType.Public);
            legendProjects[1].Configurations.First().Value.AddDependency(legendProjects[2].Configurations.First().Value, DependencyType.Public);
            legendProjects[2].Configurations.First().Value.AddDependency(legendProjects[3].Configurations.First().Value, DependencyType.Private);
            legendProjects[1].Configurations.First().Value.AddDependency(legendExtProjects[0].Configurations.First().Value, DependencyType.Public);
            legendProjects[2].Configurations.First().Value.AddDependency(legendExtProjects[2].Configurations.First().Value, DependencyType.Public);
            legendProjects[3].Configurations.First().Value.AddDependency(legendExtProjects[1].Configurations.First().Value, DependencyType.Private);

            WriteGraph(stream, legendProjects[0].Configurations.First().Value, "ExternLegend", true, false,
            () =>
            {
                foreach (TrackedProject p in legendProjects)
                    p.ResetVisit();
                foreach (TrackedProject p in legendExtProjects)
                    p.ResetVisit();
            }
            );

            stream.WriteLine("\t}");
        }

        private readonly Dictionary<string, TrackedProject> _projects;
    }
}
