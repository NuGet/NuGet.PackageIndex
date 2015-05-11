// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
        bool IsLocked { get; }
        bool IndexExists { get; }
        DateTime LastWriteTime { get; }
        IList<PackageIndexError> AddPackage(ZipPackage package, bool force = false);
        IList<PackageIndexError> RemovePackage(string packageName);
        IList<PackageIndexError> Clean();
    }
}
