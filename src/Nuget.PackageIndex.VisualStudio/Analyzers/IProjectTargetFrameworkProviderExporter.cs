// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;
using Nuget.PackageIndex.Client;

namespace Nuget.PackageIndex.VisualStudio.Analyzers
{
    /// <summary>
    /// Returns a list if target frameworks for given file path, by exporting all
    /// available IProjectTargetFrameworkProviders and looking for the one supporting
    /// given file's DTE project.
    /// </summary>
    internal interface IProjectTargetFrameworkProviderExporter
    {
        IEnumerable<TargetFrameworkMetadata> GetTargetFrameworks(string filePath);
    }
}
