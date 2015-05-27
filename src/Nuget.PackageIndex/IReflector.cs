// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Nuget.PackageIndex.Models;
using System.Collections.Generic;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Retrieves types and methods information from a given assembly.
    /// When called for multiple assemblies in a package merges unique types 
    ///and other collected metadata.
    /// </summary>
    internal interface IReflector
    {
        IEnumerable<TypeModel> Types { get; }
        IEnumerable<NamespaceModel> Namespaces { get; }
        IEnumerable<ExtensionModel> Extensions { get; }

        void ProcessAssembly(string assemblyPath);        
    }
}
