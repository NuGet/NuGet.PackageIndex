using System.Threading;
using System.Threading.Tasks;
using System.Composition;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Shell;
using Nuget.PackageIndex.Client.CodeFixes;
using Nuget.PackageIndex.VisualStudio.Analyzers;
using Nuget.PackageIndex.VisualStudio.CodeFixes.CSharp.Utilities;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes.CSharp
{
    /// <summary>
    /// CSharp code fix provider adding a missing package and using statement to the project,
    /// for given unknown type if this type information was found in the package index.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = ProviderName), Shared]
    public sealed class CSharpAddPackageCodeFixProvider : AddPackageCodeFixProviderBase
    {
        private const string ProviderName = "Add Missing Package"; // should it also be localized?

        [ImportingConstructor]
        public CSharpAddPackageCodeFixProvider([Import]SVsServiceProvider serviceProvider)
            : base(new PackageInstaller(serviceProvider), TargetFrameworkProvider.Instance)
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(CSharpAddPackageDiagnosticAnalyzer.DiagnosticId);
            }
        }

        protected override string ActionTitle
        {
            get
            {
                return Resources.AddPackageTitleFormat;
            }
        }

        protected override bool IgnoreCase
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// For now keep simple check if using directove can be added. Later we might add some checks here,
        /// for example if package source is unavailable etc 
        /// </summary>
        protected override bool CanAddImport(SyntaxNode node, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            return node.CanAddUsingDirectives(cancellationToken);
        }

        /// <summary>
        /// Adding using statement here
        /// </summary>
        protected override async Task<Document> AddImportAsync(SyntaxNode contextNode, string namespaceName, Document document, bool specialCaseSystem, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(namespaceName))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);

                var usings = root.DescendantNodes().Where(n => n is UsingDirectiveSyntax);
                var newNamespaceUsing = string.Format("using {0};", namespaceName);
                if (usings.Any(x => x.ToFullString().StartsWith(newNamespaceUsing)))
                {
                    return document;
                }

                var newUsingStatement = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName));
                var newRoot = ((CompilationUnitSyntax)root).AddUsingDirective(newUsingStatement, contextNode, specialCaseSystem);

                document = document.WithSyntaxRoot(newRoot);
            }

            return document;
        }
    }
}
