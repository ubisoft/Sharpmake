// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    public class DotNetDependency
    {
        public Project.Configuration Configuration { get; }
        public bool? ReferenceOutputAssembly { get; set; }
        public bool ReferenceSwappedWithOutputAssembly { get; set; } = false;
        public bool CopyLocal { get; set; } = true;

        public DotNetDependency(Project.Configuration configuration)
        {
            Configuration = configuration;
        }

        public override bool Equals(object other)
        {
            var otherDependency = other as DotNetDependency;
            return (otherDependency != null) && Configuration.Equals(otherDependency.Configuration);
        }

        public override int GetHashCode() => Configuration.GetHashCode();

        public override string ToString() => Configuration.ToString();
    }
}
