// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Nuget.PackageIndex.Models;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Represents package index and exposes common operations for all index types (local, remote)
    /// </summary>
    public interface IPackageIndex
    {
        IList<TypeInfo> GetTypes(string typeName);
        IList<PackageInfo> GetPackages(string packageName);
        IList<NamespaceInfo> GetNamespaces(string ns);
        IList<ExtensionInfo> GetExtensions(string extension);
    }
}
