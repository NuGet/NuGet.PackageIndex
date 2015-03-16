using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes.CSharp.Utilities
{
    /// <summary>
    /// Code imported form Microsoft.CodeAnalysis since it is internal there and we need it
    /// </summary>
    internal static class SyntaxTriviaExtensions
    {
        public static bool IsDocComment(this SyntaxTrivia trivia)
        {
            return trivia.IsSingleLineDocComment() || trivia.IsMultiLineDocComment();
        }

        public static bool IsSingleLineDocComment(this SyntaxTrivia trivia)
        {
            return trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia;
        }

        public static bool IsMultiLineDocComment(this SyntaxTrivia trivia)
        {
            return trivia.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia;
        }

        public static bool IsElastic(this SyntaxTrivia trivia)
        {
            return trivia.HasAnnotation(SyntaxAnnotation.ElasticAnnotation);
        }
    }
}
