// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes.CSharp.Utilities
{
    /// <summary>
    /// Code imported form Microsoft.CodeAnalysis since it is internal there and we need it
    /// </summary>
    internal static class SyntaxNodeExtensions
    {
        public static NamespaceDeclarationSyntax GetInnermostNamespaceDeclarationWithUsings(this SyntaxNode contextNode)
        {
            var usingDirectiveAncsestor = contextNode.GetAncestor<UsingDirectiveSyntax>();
            if (usingDirectiveAncsestor == null)
            {
                return contextNode.GetAncestorsOrThis<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
            }
            else
            {
                // We are inside a using directive. In this case, we should find and return the first 'parent' namespace with usings.
                var containingNamespace = usingDirectiveAncsestor.GetAncestor<NamespaceDeclarationSyntax>();
                if (containingNamespace == null)
                {
                    // We are inside a top level using directive (i.e. one that's directly in the compilation unit).
                    return null;
                }
                else
                {
                    return containingNamespace.GetAncestors<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
                }
            }
        }

        private static TextSpan GetUsingsSpan(CompilationUnitSyntax root, NamespaceDeclarationSyntax namespaceDeclaration)
        {
            if (namespaceDeclaration != null)
            {
                var usings = namespaceDeclaration.Usings;
                var start = usings.First().SpanStart;
                var end = usings.Last().Span.End;
                return TextSpan.FromBounds(start, end);
            }
            else
            {
                var rootUsings = root.Usings;
                if (rootUsings.Any())
                {
                    var start = rootUsings.First().SpanStart;
                    var end = rootUsings.Last().Span.End;
                    return TextSpan.FromBounds(start, end);
                }
                else
                {
                    var start = 0;
                    var end = root.Members.Any()
                        ? root.Members.First().GetFirstToken().Span.End
                        : root.Span.End;
                    return TextSpan.FromBounds(start, end);
                }
            }
        }

        public static bool OverlapsHiddenPosition(this SyntaxTree tree, TextSpan span, CancellationToken cancellationToken)
        {
            if (tree == null)
            {
                return false;
            }

            var text = tree.GetText(cancellationToken);

            return text.OverlapsHiddenPosition(span, (position, cancellationToken2) =>
            {
                // implements the ASP.Net IsHidden rule
                var lineVisibility = tree.GetLineVisibility(position, cancellationToken2);
                return lineVisibility == LineVisibility.Hidden || lineVisibility == LineVisibility.BeforeFirstLineDirective;
            },
                cancellationToken);
        }
    }
}
