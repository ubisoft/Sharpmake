// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sharpmake
{
    public class PlatformImplementationExtensionRegisteredEventArgs : EventArgs
    {
        public Assembly ExtensionAssembly { get; }
        public IReadOnlyList<Type> Interfaces { get; }

        internal PlatformImplementationExtensionRegisteredEventArgs(Assembly assembly, IEnumerable<Type> interfaces)
        {
            ExtensionAssembly = assembly;
            Interfaces = interfaces.ToArray();
        }
    }
}
