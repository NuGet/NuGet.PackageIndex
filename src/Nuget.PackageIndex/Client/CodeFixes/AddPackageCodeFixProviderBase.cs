using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Nuget.PackageIndex.Client.CodeFixes
{
    /// <summary>
    /// This is a language agnostic base class that can add a missing package
    /// for given unknown type.
    /// TODO: Add ILogger here for telemetry purposes
    /// </summary>
    public abstract partial class AddPackageCodeFixProviderBase : CodeFixProvider
    {
        private readonly IPackageInstaller _packageInstaller;
        private readonly IPackageSearcher _packageSearcher;

        public AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller)
            : this(packageInstaller, new PackageSearcher())
        {
        }

        internal AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller, IPackageSearcher packageSearcher)
        {
            _packageInstaller = packageInstaller;
            _packageSearcher = packageSearcher;
        }

#if RC
        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
#else
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
#endif
        {
            var document = context.Document;
            var span = context.Span;
            var diagnostics = context.Diagnostics;
            var cancellationToken = context.CancellationToken;
            var project = document.Project;
            var diagnostic = diagnostics.First();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(span.Start, findInsideTrivia: true);
            var ancestors = token.GetAncestors<SyntaxNode>();
            if (!ancestors.Any())
            {
                return;
            }

            var node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root);
            if (node == null)
            {
                return;
            }

            var placeSystemNamespaceFirst = true;            

            if (!cancellationToken.IsCancellationRequested)
            {
                if (CanAddImport(node, cancellationToken))
                {
                    var typeName = node.ToString();
                    var packagesWithGivenType =_packageSearcher.Search(typeName);
                    foreach(var typeInfo in packagesWithGivenType)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var namespaceName = typeInfo.FullName.Contains(".") ? Path.GetFileNameWithoutExtension(typeInfo.FullName) : null;
                        if (!string.IsNullOrEmpty(namespaceName))
                        {
                            var action = new AddPackageCodeAction(_packageInstaller, typeInfo, ActionTitle, 
                                            (c) => AddImportAsync(node, namespaceName, document, placeSystemNamespaceFirst, cancellationToken));
                            context.RegisterFix(action, diagnostic);
                        }
                    }
                }
            }
        }

        #region Abstract methods and properties

        // note this format should have only one place holder {0}
        protected abstract string ActionTitle { get; }
        protected abstract bool IgnoreCase { get; }
        protected abstract bool CanAddImport(SyntaxNode node, CancellationToken cancellationToken);
        protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, string namespaceName, Document documemt, bool specialCaseSystem, CancellationToken cancellationToken);

        #endregion
    }
}

