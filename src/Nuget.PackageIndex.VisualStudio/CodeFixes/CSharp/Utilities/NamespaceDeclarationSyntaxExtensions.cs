// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nuget.PackageIndex.Client;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes.CSharp.Utilities
{
    /// <summary>
    /// Code imported form Microsoft.CodeAnalysis since it is internal there and we need it
    /// </summary>
    internal static class NamespaceDeclarationSyntaxExtensions
    {
        public static NamespaceDeclarationSyntax AddUsingDirectives(
            this NamespaceDeclarationSyntax namespaceDeclaration,
            IList<UsingDirectiveSyntax> usingDirectives,
            bool placeSystemNamespaceFirst,
            params SyntaxAnnotation[] annotations)
        {
            if (!usingDirectives.Any())
            {
                return namespaceDeclaration;
            }

            var specialCaseSystem = placeSystemNamespaceFirst;
            var comparer = specialCaseSystem
                ? UsingsAndExternAliasesDirectiveComparer.SystemFirstInstance
                : UsingsAndExternAliasesDirectiveComparer.NormalInstance;

            var usings = new List<UsingDirectiveSyntax>();
            usings.AddRange(namespaceDeclaration.Usings);
            usings.AddRange(usingDirectives);

            if (namespaceDeclaration.Usings.IsSorted(comparer))
            {
                usings.Sort(comparer);
            }

            usings = usings.Select(u => u.WithAdditionalAnnotations(annotations)).ToList();
            var newNamespace = namespaceDeclaration.WithUsings(usings.ToSyntaxList());

            return newNamespace;
        }
    }
}
