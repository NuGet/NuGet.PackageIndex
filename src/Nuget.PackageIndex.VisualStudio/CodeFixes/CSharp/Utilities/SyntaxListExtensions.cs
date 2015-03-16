using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes.CSharp.Utilities
{
    /// <summary>
    /// Code imported form Microsoft.CodeAnalysis since it is internal there and we need it
    /// </summary>
    internal static class SyntaxListExtensions
    {
        public static SyntaxList<T> ToSyntaxList<T>(this IEnumerable<T> sequence) where T : SyntaxNode
        {
            return SyntaxFactory.List(sequence);
        }
    }
}
