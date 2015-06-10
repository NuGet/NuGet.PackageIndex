// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Provides an abstracted representation of package to be used in public types
    /// </summary>
    internal class PackageMetadata : IPackageMetadata
    {
        public string Id { get; set; }
        public string Version{ get; set; }
        public IEnumerable<string> TargetFrameworks { get; set; }
        public string LocalPath { get; set; }
        public IEnumerable<AssemblyMetadata> Assemblies { get; set; }
    }
}
