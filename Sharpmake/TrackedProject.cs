// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Sharpmake
{
    public class TrackedProject : IComparable<TrackedProject>
    {
        private string GetKeyFromConfiguration(Project.Configuration config)
        {
            if (_project != null)
                return config.Target.GetTargetString();

            return ProjectString;
        }

        public TrackedProject(string projName, bool isExtern, Project.Configuration.OutputType configOutputType)
        {
            ProjectString = projName;
            _isExtern = isExtern;
            _project = null;
            Configurations.Add(ProjectString, new TrackedConfiguration(this, configOutputType));
        }

        public TrackedProject(Project proj, Project.Configuration config)
        {
            ProjectString = proj.ToString();
            _project = proj;
            _isExtern = false;
            Configurations.Add(GetKeyFromConfiguration(config), new TrackedConfiguration(this, config));
        }

        public void ResetVisit()
        {
            foreach (KeyValuePair<string, TrackedConfiguration> c in Configurations)
                c.Value.ResetVisit();
        }

        public int CompareTo(TrackedProject other)
        {
            return string.Compare(ProjectString, other.ProjectString, StringComparison.Ordinal);
        }

        public TrackedConfiguration FindConfiguration(Project.Configuration config)
        {
            return Configurations[GetKeyFromConfiguration(config)];
        }

        public TrackedConfiguration FindConfiguration(ITarget target)
        {
            return Configurations[target.GetTargetString()];
        }

        public bool IsExtern()
        {
            if (_project != null)
                return _project.SourceRootPath.ToLower().Contains("extern") || _project.SharpmakeCsFileName.ToLower().Contains("extern");

            return _isExtern;
        }

        public void AddConfig(Project.Configuration config)
        {
            string key = GetKeyFromConfiguration(config);
            Configurations.Add(key, new TrackedConfiguration(this, config));
        }


        public Dictionary<string, TrackedConfiguration> Configurations { get; } = new Dictionary<string, TrackedConfiguration>();
        private readonly Project _project;
        public string ProjectString { get; }
        private readonly bool _isExtern;
    }
}
