﻿using System.Collections.Generic;
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
        IEnumerable<string> GetPackages(bool newOnly, CancellationToken cancellationToken = default(CancellationToken));
        Task<LocalPackageIndexBuilderResult> BuildAsync(bool newOnly = false, CancellationToken cancellationToken = default(CancellationToken));
        LocalPackageIndexBuilderResult Clean();
        LocalPackageIndexBuilderResult Rebuild();
        LocalPackageIndexBuilderResult AddPackage(string nupkgFilePath, bool force = false);
        LocalPackageIndexBuilderResult RemovePackage(string packageName, bool force = false);
    }
}
