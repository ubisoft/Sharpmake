// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

namespace Sharpmake
{
    // Delegate template resolving to an external class (using an existing resolver)
    public interface IResolverHelper
    {
        string Resolve(Resolver resolver);
    }
}
