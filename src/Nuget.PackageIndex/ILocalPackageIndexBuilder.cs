// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Provides API for local index manipulation. Knows how to find packages on local machine.
    /// </summary>
    public interface ILocalPackageIndexBuilder
    {
        ILocalPackageIndex Index { get; }
        IList<string> GetPackageDirectories();
        Task<LocalPackageIndexBuilderResult> BuildAsync(bool shouldClean, bool newOnly, CancellationToken cancellationToken);
        LocalPackageIndexBuilderResult Clean();
        LocalPackageIndexBuilderResult Rebuild();
        LocalPackageIndexBuilderResult AddPackage(string nupkgFilePath, bool force);
        LocalPackageIndexBuilderResult RemovePackage(string packageName, bool force);
    }
}
