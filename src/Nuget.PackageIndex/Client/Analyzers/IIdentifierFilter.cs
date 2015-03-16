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
