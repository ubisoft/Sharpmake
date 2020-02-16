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
using System.Runtime.Serialization;

namespace Sharpmake
{
    /// <summary>
    /// Exception thrown by <see cref="PlatformRegistry.Get{TInterface}(Platform)"/> when
    /// requesting an interface for an interface implementation that is not implemented for the
    /// requested platform and has no default implementation either.
    /// </summary>
    [Serializable]
    public class PlatformNotSupportedException : Exception
    {
        internal PlatformNotSupportedException(Type implType)
            : base($"No default implementation of {implType.Name} is provided.")
        { }

        internal PlatformNotSupportedException(Platform platform, Type implType)
            : base($"No implementation of {implType.Name} for {platform} is provided.")
        { }

        protected PlatformNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    /// <summary>
    /// Exception thrown when <see cref="PlatformRegistry"/> finds two or more implementation of an
    /// interface for a given platform.
    /// </summary>
    [Serializable]
    public class DuplicatePlatformImplementationException : Exception
    {
        internal DuplicatePlatformImplementationException(string message)
            : base(message)
        { }

        protected DuplicatePlatformImplementationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    /// <summary>
    /// Exception thrown when <see cref="PlatformRegistry"/> is unable to instantiate an interface
    /// implementation object. This is usually because the type has no constructor, or the
    /// constructor threw an exception.
    /// </summary>
    [Serializable]
    public class PlatformImplementationCreationException : Exception
    {
        internal PlatformImplementationCreationException(Type type, Exception innerException)
            : base($"Failed to instantiate platform implementation type {type.Name}. Check the inner exception for details.", innerException)
        { }

        protected PlatformImplementationCreationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
