// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Sharpmake
{
    /// <summary>
    /// Marks a concrete class as an implementation of given interfaces for given platforms. This
    /// class must have a default constructor. Obviously, it also needs to actually implement the
    /// interface it pretends to!
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PlatformImplementationAttribute : Attribute
    {
        /// <summary>
        /// Gets the list of platforms that the object implements.
        /// </summary>
        /// <remarks>
        /// As <see cref="Platform"/> is a bitfield, it is possible to specify multiple supported
        /// platforms.
        /// </remarks>
        public Platform Platforms { get; }

        /// <summary>
        /// Gets a collection of the <see cref="Type"/> of the interfaces that the object
        /// implements and exposes.
        /// </summary>
        public IReadOnlyCollection<Type> InterfaceTypes { get; }

        /// <summary>
        /// Creates a new <see cref="PlatformImplementationAttribute"/> instance.
        /// </summary>
        /// <param name="platform">The implemented platform.</param>
        /// <param name="ifaceTypes">An array that lists the <see cref="Type"/> of the interfaces that the object implements.</param>
        public PlatformImplementationAttribute(Platform platform, params Type[] ifaceTypes)
        {
            Platforms = platform;
            InterfaceTypes = ifaceTypes;
        }
    }

    /// <summary>
    /// Marks a concrete class as a default implementation of given interfaces. This class must
    /// have a default constructor. Obviously, it also needs to actually implement the interface it
    /// pretends to!
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DefaultPlatformImplementationAttribute : Attribute
    {
        //
        // Default implementations may not be super useful since you can just use base classes.
        // Consider if we really need this, so we can remove it before we open-source Sharpmake.
        //

        /// <summary>
        /// Gets the <see cref="Type"/> of the implemented interface.
        /// </summary>
        public Type[] InterfaceTypes { get; }

        /// <summary>
        /// Creates a new <see cref="PlatformImplementationAttribute"/> instance.
        /// </summary>
        /// <param name="ifaceTypes">An array that lists the <see cref="Type"/> of the interfaces that the object implements.</param>
        public DefaultPlatformImplementationAttribute(params Type[] ifaceTypes)
        {
            InterfaceTypes = ifaceTypes;
        }
    }
}
