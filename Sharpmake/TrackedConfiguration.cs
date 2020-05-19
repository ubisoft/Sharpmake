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

namespace Sharpmake
{
    public class TrackedConfiguration : IComparable<TrackedConfiguration>
    {
        public TrackedConfiguration(TrackedProject owningProject, Project.Configuration.OutputType configOutputType)
        {
            _nodeVisited = false;
            _owningProject = owningProject;
            _dependenciesTo = new SortedDictionary<TrackedConfiguration, Tuple<DependencyType, DependencySetting>>();
            _config = null;
            _configOutputType = configOutputType;
            string cfgString = owningProject.ToString();
            _configTypeName = cfgString.Substring(cfgString.IndexOf(':') + 1);
        }

        public TrackedConfiguration(TrackedProject owningProject, Project.Configuration config)
        {
            _nodeVisited = false;
            _owningProject = owningProject;
            _dependenciesTo = new SortedDictionary<TrackedConfiguration, Tuple<DependencyType, DependencySetting>>();
            _config = config;
            _configOutputType = _config.Output;
            string cfgString = config.Owner != null ? config.ToString() : owningProject.ToString();
            _configTypeName = cfgString.Substring(cfgString.IndexOf(':') + 1);
        }

        public int CompareTo(TrackedConfiguration other)
        {
            if (_config?.Owner == null)
                return string.Compare(GetDisplayedName(false), other.GetDisplayedName(false), StringComparison.Ordinal);

            return string.Compare(_config.ToString(), other._config.ToString(), StringComparison.Ordinal);
        }

        public void AddDependency(
            TrackedConfiguration projectTo,
            DependencyType depType,
            DependencySetting setting = DependencySetting.Default)
        {
            if (!_dependenciesTo.ContainsKey(projectTo))
                _dependenciesTo.Add(projectTo, Tuple.Create(depType, setting));
        }

        public bool IsExtern()
        {
            return _owningProject.IsExtern();
        }

        public string GetNodeIdForReference()
        {
            return GetUniqueId();
        }

        public string GetUniqueId()
        {
            if (_config?.Owner != null)
                return (_config.ToString()).Replace('.', '_').Replace(' ', '_').Replace(':', '_');

            return _owningProject.ProjectString;
        }

        public string GetDisplayedName(bool verbose)
        {
            if (!verbose || _config == null)
                return _owningProject.ProjectString;

            return
                _owningProject.ProjectString
                + "<BR/>"
                + "<FONT POINT-SIZE=\"8\">"
                + GetConfigName()
                + "</FONT>";
        }

        public string GetConfigName()
        {
            return _config?.Target?.GetTargetString() ?? _configOutputType.ToString();
        }

        public bool IsNodeVisited()
        {
            return _nodeVisited;
        }

        public void ResetVisit()
        {
            _nodeVisited = false;
        }

        public void WriteNodeDescription(System.IO.StreamWriter stream, bool writeExternNodes, bool verbose, string prefix)
        {
            bool isExternProject = IsExtern();
            _nodeVisited = true;
            bool writeDescription = (isExternProject == writeExternNodes);
            if (writeDescription)
            {
                stream.Write("\t" + GetUniqueId() + "[label=<" + GetDisplayedName(verbose) + ">");

                string shape;
                if (isExternProject)
                    shape = DependencyTracker.ShapeExtern;
                else
                {
                    switch (_configOutputType)
                    {
                        case Project.Configuration.OutputType.Exe:
                        case Project.Configuration.OutputType.Dll:
                        case Project.Configuration.OutputType.Lib:
                            shape = DependencyTracker.ShapeProject;
                            break;
                        default:
                            shape = DependencyTracker.ShapeUnknown;
                            break;
                    }
                }

                string color;
                Project.Configuration.OutputType outputType = Project.Configuration.OutputType.None;

                if (_configOutputType != Project.Configuration.OutputType.None)
                {
                    outputType = _configOutputType;
                }
                else if (_config != null)
                {
                    // output type might be defined in inherited target class (this is the case for some extern project). Try to find it through reflection.
                    try
                    {
                        System.Type type = _config.Target.GetType();
                        System.Reflection.FieldInfo field = type.GetField("OutputType");
                        object fieldValue = field.GetValue(_config.Target);
                        switch (fieldValue.ToString())
                        {
                            case "Dll":
                                outputType = Project.Configuration.OutputType.Dll;
                                break;
                            case "Lib":
                                outputType = Project.Configuration.OutputType.Lib;
                                break;
                            case "Exe":
                                outputType = Project.Configuration.OutputType.Exe;
                                break;
                            default:
                                outputType = Project.Configuration.OutputType.None;
                                break;
                        }
                    }
                    catch { }
                }

                switch (outputType)
                {
                    case Project.Configuration.OutputType.Exe:
                        color = isExternProject ? DependencyTracker.ColorExternExe : DependencyTracker.ColorExe;
                        break;
                    case Project.Configuration.OutputType.Dll:
                        color = isExternProject ? DependencyTracker.ColorExternDll : DependencyTracker.ColorDll;
                        break;
                    case Project.Configuration.OutputType.Lib:
                        color = isExternProject ? DependencyTracker.ColorExternLib : DependencyTracker.ColorLib;
                        break;
                    default:
                        color = DependencyTracker.ColorUnkownOutputType;
                        break;
                }

                stream.WriteLine(" shape = " + shape + ", style=filled, fillcolor = " + color + ", fontsize=8, fontname=\"sans-serif\"]");
            }

            foreach (var dep in _dependenciesTo)
            {
                if (!dep.Key.IsNodeVisited())
                    dep.Key.WriteNodeDescription(stream, writeExternNodes, verbose, prefix + " ");
            }
        }

        public void WriteDependencies(System.IO.StreamWriter stream, bool verbose, string prefix)
        {
            _nodeVisited = true;
            foreach (var dep in _dependenciesTo)
            {
                if (IsExtern() && !DependencyTracker.ShowDependenciesFromExtern)
                    continue;
                if (dep.Key.IsExtern() && !DependencyTracker.ShowDependenciesToExtern)
                    continue;
                stream.Write("\t" + GetNodeIdForReference() + " -> " + dep.Key.GetNodeIdForReference());

                string color = "";
                string label = "";
                string width = "";
                if (dep.Key.IsExtern())
                    width = "penwidth=" + DependencyTracker.WidthExternDep;
                else
                    width = "penwidth=" + DependencyTracker.WidthDep;

                // http://www.graphviz.org/doc/info/colors.html
                switch (dep.Value.Item1)
                {
                    case DependencyType.Public:
                        color = "color=" + DependencyTracker.ColorPublicDep;
                        label = "label=\"Public\"";
                        break;
                    case DependencyType.Private:
                        color = "color=" + DependencyTracker.ColorPrivateDep;
                        label = "label=\"Private\"";
                        break;
                }

                string setting = string.Empty;
                if (dep.Value.Item2 != DependencySetting.Default)
                    setting = ", label=\"  " + dep.Value.Item2.ToString() + "\", fontsize=8, fontcolor=\"white\", fontname=\"sans-serif\"";

                if (verbose)
                    stream.WriteLine("[" + color + "," + width + ", " + label + "]");
                else
                    stream.WriteLine("[" + color + "," + width + setting + "]");
            }

            foreach (var dep in _dependenciesTo)
            {
                if (!dep.Key.IsNodeVisited())
                    dep.Key.WriteDependencies(stream, verbose, prefix + " ");
            }
        }

        public bool DumpDependencyGraph()
        {
            return _config.DumpDependencyGraph;
        }

        private bool _nodeVisited;
        private Project.Configuration _config;
        private Project.Configuration.OutputType _configOutputType;
        private TrackedProject _owningProject;
        private SortedDictionary<TrackedConfiguration, Tuple<DependencyType, DependencySetting>> _dependenciesTo;
        private string _configTypeName;
    }
}


