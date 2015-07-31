// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Package Index model representation, all models must implement it if they want to be 
    /// stored in the index.
    /// </summary>
    internal interface IPackageIndexModel
    {
        Document ToDocument();
        Query GetDefaultSearchQuery();
    }
}
