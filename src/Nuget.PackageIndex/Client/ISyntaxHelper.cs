// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Nuget.PackageIndex.Client.Analyzers
{
    /// <summary>
    /// Language specific helper that can determine if syntax node is one of supported types
    /// </summary>
    public interface ISyntaxHelper
    {
        bool IsImport(SyntaxNode node, out string importFullName);
        bool IsExtension(SyntaxNode node);
        bool IsType(SyntaxNode node);
        bool IsSupported(SyntaxNode node);
        bool IsAttribute(SyntaxNode node);
        string[] SupportedDiagnostics { get; }
    }
}
