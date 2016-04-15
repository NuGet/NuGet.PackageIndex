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
        IEnumerable<IPackageMetadata> DiscoverPackages(IEnumerable<string> sourcePaths, 
                                                      HashSet<string> indexedPackages, 
                                                      bool newOnly, 
                                                      DateTime lastIndexModifiedTime, 
                                                      CancellationToken cancellationToken,
                                                      Func<string, bool> shouldIncludeFunc);
        void LoadPackage(IPackageMetadata packageMetadata);
        IPackageMetadata GetPackageMetadataFromPath(string packagePath, Func<string, bool> shouldIncludeFunc);
    }
}
