using Nuget.PackageIndex.Engine;
using NuGet;
using System;
using System.Collections.Generic;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Represents a local package index
    /// </summary>
    public interface ILocalPackageIndex : IPackageIndex
    {
        bool IndexExists { get; }
        DateTime LastWriteTime { get; }
        IList<PackageIndexError> AddPackage(ZipPackage package, bool force = false);
        IList<PackageIndexError> RemovePackage(string packageName);
        IList<PackageIndexError> Clean();
    }
}
