// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Nuget.PackageIndex.Models;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Wraps a search logic for given type instead of just using an instance of IPackageIndex.
    /// This is needed since there could be multiple versions of indexes and we need one place 
    /// for the logic that knows which version to use.
    /// </summary>
    public interface IPackageSearcher
    {
        IEnumerable<NamespaceInfo> SearchNamespace(string namespaceName);
        IEnumerable<ExtensionInfo> SearchExtension(string extensionName);
        IEnumerable<TypeInfo> SearchType(string typeName);
    }
}
