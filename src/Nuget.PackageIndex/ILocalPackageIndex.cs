// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Nuget.PackageIndex.Engine;
using Nuget.PackageIndex.Models;

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
        string Location { get; }
        IIndexSettings Settings { get; }
        IList<PackageIndexError> AddPackage(IPackageMetadata package, bool force);
        IList<PackageIndexError> RemovePackage(string packageName);
        IList<PackageInfo> GetPackages();
        IList<TypeInfo> GetTypes();
        IList<NamespaceInfo> GetNamespaces();
        IList<ExtensionInfo> GetExtensions();
        IList<PackageIndexError> Clean();
        void WarmUp();
        void CoolDown();
    }
}
