// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Provides an abstracted representation of package to be used in public types
    /// </summary>
    public interface IPackageMetadata
    {
        string Id { get; }
        string Version{ get; }
        IEnumerable<string> TargetFrameworks { get; set; }
        string LocalPath { get; }
        IEnumerable<AssemblyMetadata> Assemblies { get; set; }
        void Load();
    }
}
