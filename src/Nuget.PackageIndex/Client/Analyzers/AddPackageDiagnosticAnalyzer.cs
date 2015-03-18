using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Nuget.PackageIndex.Models;
using Nuget.PackageIndex.Logging;

namespace Nuget.PackageIndex.Client.Analyzers
{
    /// <summary>
    /// A language agnostic base class for diagnostics that want to query package index for
    /// unknown identifiers  to see if there packages where missing type(unknown identifier) live.
    /// </summary>
    /// <typeparam name="TLanguageKindEnum">Specifies syntax language</typeparam>
    /// <typeparam name="TSimpleNameSyntax">Specifies simple name syntax</typeparam>
    /// <typeparam name="TQualifiedNameSyntax">Specifies qualified name syntax</typeparam>
    /// <typeparam name="TIdentifierNameSyntax">Specifies identifier name syntax</typeparam>
    public abstract class AddPackageDiagnosticAnalyzer<TLanguageKindEnum, TSimpleNameSyntax, TQualifiedNameSyntax, TIdentifierNameSyntax> 
        : UnknownIdentifierDiagnosticAnalyzerBase<TLanguageKindEnum, TSimpleNameSyntax, TQualifiedNameSyntax, TIdentifierNameSyntax>
        where TLanguageKindEnum : struct
        where TSimpleNameSyntax : SyntaxNode
        where TQualifiedNameSyntax : SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
    {
        private readonly IPackageSearcher _packageSearcher;

        public AddPackageDiagnosticAnalyzer(IEnumerable<IIdentifierFilter> identifierFilters)
            : this(new PackageSearcher(new LogFactory(LogLevel.Quiet)), identifierFilters)
        {
        }

        public AddPackageDiagnosticAnalyzer(IEnumerable<IIdentifierFilter> identifierFilters, IProjectFilter projectFilter, ILog logger)
            : this(new PackageSearcher(logger), identifierFilters)
        {
        }

        internal AddPackageDiagnosticAnalyzer(IPackageSearcher packageSearcher, IEnumerable<IIdentifierFilter> identifierFilters)
            : base(identifierFilters)
        {
            _packageSearcher = packageSearcher;
        }

        /// <summary>
        /// Does actual query to Packag Index to find packages where a given type is defined.
        /// Note: At diagnostic level there no way to know in which document, project or workspace 
        /// we are for a given SyntaxNode (or it's SyntaxNodeAnalysisContext) since diagnostics 
        /// work at csc.exe level (in command line for example) and there no container objects defined
        /// at that time. 
        /// Thus we will display information about all packages returned form index to suggest user where
        /// type can be located, however when user clicks Ctrl+. we would display only code fixes for packages
        /// that satisfy current project target frameworks list.
        /// </summary>
        protected override IEnumerable<string> AnalyzeNode(TIdentifierNameSyntax node)
        {
            var typeName = node.ToString();
            var packagesWithGivenType = _packageSearcher.Search(typeName);
            if (!packagesWithGivenType.Any())
            {
                return null;
            }

            return GetFriendlyPackagesString(packagesWithGivenType);
        }

        private IEnumerable<string> GetFriendlyPackagesString(IEnumerable<TypeModel> types)
        {
            foreach (var t in types)
            {
                yield return string.Format(FriendlyMessageFormat, string.Format("{0}, {1} {2}", t.FullName, t.PackageName, t.PackageVersion));
            }
        }

        #region Abstract methods and properties

        // must have 1 format placeholder {0}
        protected abstract string FriendlyMessageFormat { get; }

        #endregion
    }
}


