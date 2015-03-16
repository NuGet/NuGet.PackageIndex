using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nuget.PackageIndex.Client;
using Nuget.PackageIndex.Client.Analyzers;

namespace Nuget.PackageIndex.VisualStudio.Analyzers.IdentifierFilters
{
    /// <summary>
    /// A filter that verifies unknown identifier is a type rewrite in using statement:
    ///     using Task=System.Threading.Tasks.Task;
    /// in this case Task identifier would generate a diagnostic with unknown identifier,
    /// but it is not the code and is valid case, thus diagnostic should not be displayed.
    /// </summary>
    public class UsingIdentifierFilter : IIdentifierFilter
    {
        public bool ShouldDisplayDiagnostic(SyntaxNode node)
        {
            return node.GetAncestor<UsingDirectiveSyntax>() == null;
        }
    }
}
