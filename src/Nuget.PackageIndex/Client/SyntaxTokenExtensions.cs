using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Nuget.PackageIndex.Client
{
    /// <summary>
    /// Code imported form Microsoft.CodeAnalysis since it is internal there and we need it
    /// </summary>
    internal static class SyntaxTokenExtensions
    {
        public static IEnumerable<T> GetAncestors<T>(this SyntaxToken token)
            where T : SyntaxNode
        {
            return token.Parent != null
                ? token.Parent.AncestorsAndSelf().OfType<T>()
                : new List<T>();
        }
    }
}
