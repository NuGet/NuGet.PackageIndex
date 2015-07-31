// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Nuget.PackageIndex.Client.Analyzers;
using Nuget.PackageIndex.Models;

namespace Nuget.PackageIndex.Client
{
    /// <summary>
    /// Represents a language agnostic analyzer that can get suggested packages
    /// for given SyntaxNode.
    /// </summary>
    public interface IAddPackageAnalyzer
    {
        ISyntaxHelper SyntaxHelper { get; }
        IList<IPackageIndexModelInfo> GetSuggestions(SyntaxNode node, IEnumerable<ProjectMetadata> projects);
    }
}

