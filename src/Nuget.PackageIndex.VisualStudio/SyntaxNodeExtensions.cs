// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// Code imported form Microsoft.CodeAnalysis since it is internal there and we need it
    /// </summary>
    public static class SyntaxNodeExtensions
    {
        public static TNode GetAncestor<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            if (node == null)
            {
                return default(TNode);
            }

            return node.GetAncestors<TNode>().FirstOrDefault();
        }

        public static IEnumerable<TNode> GetAncestors<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is TNode)
                {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static IEnumerable<TNode> GetAncestorsOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node;
            while (current != null)
            {
                if (current is TNode)
                {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static TNode GetAncestorOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            if (node == null)
            {
                return default(TNode);
            }

            return node.GetAncestorsOrThis<TNode>().FirstOrDefault();
        }
    }
}
