// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;

namespace Sharpmake
{
    /// <summary>
    /// Marks a .NET assembly as a host of Sharpmake extension types (platform implementations for Sharpmake generators, builder...).
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SharpmakeExtensionAttribute : Attribute { }
}
