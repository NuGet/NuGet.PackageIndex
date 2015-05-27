// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Abstraction for reflectors, that creates a specific type of reflector
    /// </summary>
    internal interface IReflectorFactory
    {
        IReflector Create(string packageId, string packageVersion, IEnumerable<string> packageTargetFrameworks);
    }
}
