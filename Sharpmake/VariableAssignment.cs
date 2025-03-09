// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;

namespace Sharpmake
{
    /// <summary>
    /// Simple class that wraps a variable name and it's assigned value.
    /// </summary>
    public class VariableAssignment
    {
        /// <summary>
        /// Gets the name of the variable or parameter to assign the value to.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Gets the value to assign.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Creates a new <see cref="VariableAssignment"/> instance.
        /// </summary>
        /// <param name="identifier">The name of the variable or parameter to assign.</param>
        /// <param name="value">The value to assign to <paramref name="identifier"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is `null`.</exception>
        public VariableAssignment(string identifier, object value)
        {
            if (identifier == null)
                throw new ArgumentNullException(nameof(identifier));

            Identifier = identifier;
            Value = value;
        }
    }
}
