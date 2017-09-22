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
