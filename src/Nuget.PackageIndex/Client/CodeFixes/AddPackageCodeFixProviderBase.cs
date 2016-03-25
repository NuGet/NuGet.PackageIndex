// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        private readonly IProjectMetadataProvider _projectMetadataProvider;

        public AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller, 
                                             IProjectMetadataProvider projectMetadataProvider)
            : this(packageInstaller, new PackageSearcher(new LogFactory(LogLevel.Quiet)), projectMetadataProvider)
        {
        }

        public AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller, 
                                             ILog logger, 
                                             IProjectMetadataProvider projectMetadataProvider)
            : this(packageInstaller, new PackageSearcher(logger), projectMetadataProvider)
        {
        }

        internal AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller, 
                                               IPackageSearcher packageSearcher, 
                                               IProjectMetadataProvider projectMetadataProvider)
        {
            _packageInstaller = packageInstaller;
            _packageSearcher = packageSearcher;
            _projectMetadataProvider = projectMetadataProvider;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!Enabled)
            {
                return;
            }

            var document = context.Document;

            var projects = _projectMetadataProvider.GetProjects(document.FilePath);
            if (projects == null || !projects.Any())
            {
                // project is unsupported
                return;
            }

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
                // get distinct frameworks from all projects current file belongs to
                var suggestions = Analyzer.GetSuggestions(node, projects);
                foreach (var packageInfo in suggestions.Take(MaxPackageSuggestions))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var namespaceName = packageInfo.GetNamespace();
                    AddPackageCodeAction action = null;
                    if (string.IsNullOrEmpty(namespaceName))
                    {
                        // namspaces suggestions don't need to add another namespace
                        action = new AddPackageCodeAction(_packageInstaller,
                                                                packageInfo,
                                                                projects.Where(x => TargetFrameworkHelper.SupportsProjectTargetFrameworks(packageInfo, x.TargetFrameworks)).ToList(),
                                                                ActionTitle,
                                                                (c) => Task.FromResult(document));
                    }
                    else if (CanAddImport(node, cancellationToken))
                    {
                        action = new AddPackageCodeAction(_packageInstaller,
                                                                packageInfo,
                                                                projects.Where(x => TargetFrameworkHelper.SupportsProjectTargetFrameworks(packageInfo, x.TargetFrameworks)).ToList(),
                                                                ActionTitle,
                                                                (c) => AddImportAsync(node, namespaceName, document, placeSystemNamespaceFirst, cancellationToken));
                    }

                    context.RegisterCodeFix(action, diagnostic);
                }
            }
        }

        /// <summary>
        /// Allows actual implementation to be disabled by some outside setting.
        /// </summary>
        protected virtual bool Enabled
        {
            get
            {
                return true;
            }
        }

        #region Abstract methods and properties

        // note this format should have only one place holder {0}
        protected abstract string ActionTitle { get; }
        protected abstract bool IgnoreCase { get; }
        protected abstract bool CanAddImport(SyntaxNode node, CancellationToken cancellationToken);
        protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, string namespaceName, Document documemt, bool specialCaseSystem, CancellationToken cancellationToken);
        protected abstract IAddPackageAnalyzer Analyzer { get; }
        #endregion
    }
}

