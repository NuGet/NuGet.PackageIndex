using System;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Represents a local package index
    /// </summary>
    public interface ILocalPackageIndex : IPackageIndex
    {
        bool IndexExists { get; }
        DateTime LastWriteTime { get; }
    }
}
