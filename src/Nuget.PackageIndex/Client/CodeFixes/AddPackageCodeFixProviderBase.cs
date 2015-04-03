using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Nuget.PackageIndex.Logging;

namespace Nuget.PackageIndex.Client.CodeFixes
{
    /// <summary>
    /// This is a language agnostic base class that can add a missing package
    /// for given unknown type.
    /// TODO: Add ILogger here for telemetry purposes
    /// </summary>
    public abstract partial class AddPackageCodeFixProviderBase : CodeFixProvider
    {
        // we want to limit number of suggsted packages, to avoid rare cases when user had 
        // too many packages with the same type (since it would create super long list of suggestions,
        // which would be not that usable anyway).
        internal const int MaxPackageSuggestions = 5;

        private readonly IPackageInstaller _packageInstaller;
        private readonly IPackageSearcher _packageSearcher;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        public AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller, ITargetFrameworkProvider targetFrameworkProvider)
            : this(packageInstaller, new PackageSearcher(new LogFactory(LogLevel.Quiet)), targetFrameworkProvider)
        {
        }

        public AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller, ILog logger, ITargetFrameworkProvider targetFrameworkProvider)
            : this(packageInstaller, new PackageSearcher(logger), targetFrameworkProvider)
        {
        }

        internal AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller, IPackageSearcher packageSearcher, ITargetFrameworkProvider targetFrameworkProvider)
        {
            _packageInstaller = packageInstaller;
            _packageSearcher = packageSearcher;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
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
                    var projectTargetFrameworks = _targetFrameworkProvider.GetTargetFrameworks(document.FilePath);
                    // Note: allowHigherVersions=false here since we don't want to provide code fix that adds another 
                    // dependency for the same package but different version, user should upgrade it on his own when
                    // see Diagnostic suggestion
                    var packagesWithGivenType = TargetFrameworkHelper.GetSupportedPackages(_packageSearcher.Search(typeName), projectTargetFrameworks, allowHigherVersions:false)
                                                    .Take(MaxPackageSuggestions);

                    foreach (var typeInfo in packagesWithGivenType)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var namespaceName = typeInfo.FullName.Contains(".") ? Path.GetFileNameWithoutExtension(typeInfo.FullName) : null;
                        if (!string.IsNullOrEmpty(namespaceName))
                        {
                            var action = new AddPackageCodeAction(_packageInstaller, typeInfo, ActionTitle, 
                                            (c) => AddImportAsync(node, namespaceName, document, placeSystemNamespaceFirst, cancellationToken));
                            context.RegisterCodeFix(action, diagnostic);
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

