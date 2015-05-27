// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Threading;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Abstraction for different package discovery mechanizms
    /// </summary>
    internal interface ILocalPackageLoader
    {
        IEnumerable<IPackageMetadata> DiscoverPackages(IEnumerable<string> sourcePaths, bool newOnly, DateTime lastCheckTime, CancellationToken cancellationToken);
        IPackageMetadata GetPackageMetadataFromPath(string packagePath);
    }
}
