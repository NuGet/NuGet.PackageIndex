// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nuget.PackageIndex.Client.Analyzers;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// CSharp Language specific helper that can determine if syntax node is one of supported types
    /// </summary>
    public class CSharpSyntaxHelper : ISyntaxHelper
    {
        public bool IsImport(SyntaxNode node, out string importFullName)
        {
            importFullName = string.Empty;

            if (node.GetAncestor<IfDirectiveTriviaSyntax>() != null
                || node.GetAncestor<ElifDirectiveTriviaSyntax>() != null)
            {
                return false;
            }

            var usingDirective = node.GetAncestor<UsingDirectiveSyntax>();
            importFullName = usingDirective == null ? null : usingDirective.Name.ToString();
            return usingDirective != null;
        }

        public bool IsExtension(SyntaxNode node)
        {
            return (node.Parent is MemberAccessExpressionSyntax)
                && node.GetAncestor<UsingDirectiveSyntax>() == null
                && !node.Parent.ChildNodes().First().Equals(node);
        }

        public bool IsType(SyntaxNode node)
        {
            if (node.GetAncestor<UsingDirectiveSyntax>() != null
                || node.Parent is NameMemberCrefSyntax
                || node.Parent is XmlCrefAttributeSyntax
                || node.Parent is XmlNameAttributeSyntax
                || node.Parent is MemberBindingExpressionSyntax
                || node.Parent is ConditionalAccessExpressionSyntax)
            {
                return false;
            }

            if (node.Parent is MemberAccessExpressionSyntax)
            {
                if (!node.Parent.ChildNodes().First().Equals(node))
                {                    
                    return false;
                }
            }

            return true;
        }

        public bool IsSupported(SyntaxNode node)
        {
            if (node is IdentifierNameSyntax)
            {
                var asIdentifierNode = node as IdentifierNameSyntax;
                if (asIdentifierNode.IsVar 
                    || asIdentifierNode.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NameOfKeyword))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
