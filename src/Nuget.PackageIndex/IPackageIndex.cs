using System.Collections.Generic;
using NuGet;
using Nuget.PackageIndex.Engine;
using Nuget.PackageIndex.Models;
using System;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Represents package index and exposes common operations for all index types (local, remote)
    /// </summary>
    public interface IPackageIndex : IDisposable
    {
        IList<PackageIndexError> AddPackage(ZipPackage package, bool force = false);
        IList<PackageIndexError> RemovePackage(string packageName);
        IList<PackageIndexError> Clean();
        IList<TypeModel> GetTypes(string typeName);
        IList<PackageModel> GetPackages(string packageName);
    }
}
