// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Package Index model representation, all models must implement it if they want to be 
    /// stored in the index.
    /// </summary>
    public interface IPackageIndexModelInfo
    {
        List<string> TargetFrameworks { get;  }
        string PackageName { get; }
        string PackageVersion { get; }
        string GetFriendlyEntityName();
        string GetFriendlyPackageName();
        string GetNamespace();
    }
}
