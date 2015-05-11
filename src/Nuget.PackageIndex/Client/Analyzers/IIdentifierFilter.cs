// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;

namespace Nuget.PackageIndex.Client.Analyzers
{
    /// <summary>
    /// A filter that verifies if diagnostic should be displayed for given identifier's node
    /// </summary>
    public interface IIdentifierFilter
    {
        bool ShouldDisplayDiagnostic(SyntaxNode node);
    }
}
