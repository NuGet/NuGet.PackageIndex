// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Nuget.PackageIndex.Logging;
using System.Collections.Generic;
using Nuget.PackageIndex.Models;
using Nuget.PackageIndex.Client.Analyzers;

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
        private readonly ISyntaxHelper _syntaxHelper;

        public AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller, 
                                             IProjectMetadataProvider projectMetadataProvider,
                                             ISyntaxHelper syntaxHelper)
            : this(packageInstaller, new PackageSearcher(new LogFactory(LogLevel.Quiet)), projectMetadataProvider, syntaxHelper)
        {
        }

        public AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller, 
                                             ILog logger, 
                                             IProjectMetadataProvider projectMetadataProvider,
                                             ISyntaxHelper syntaxHelper)
            : this(packageInstaller, new PackageSearcher(logger), projectMetadataProvider, syntaxHelper)
        {
        }

        internal AddPackageCodeFixProviderBase(IPackageInstaller packageInstaller, 
                                               IPackageSearcher packageSearcher, 
                                               IProjectMetadataProvider projectMetadataProvider,
                                               ISyntaxHelper syntaxHelper)
        {
            _packageInstaller = packageInstaller;
            _packageSearcher = packageSearcher;
            _projectMetadataProvider = projectMetadataProvider;
            _syntaxHelper = syntaxHelper;
        }

        /// <summary>
        /// TODO: We might wat to try to reuse Diagnostics provided here if we can pass any parameters to codefix
        /// (create a custom descriptor etc?)
        /// </summary>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
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
                var distinctTargetFrameworks = TargetFrameworkHelper.GetDistinctTargetFrameworks(projects);
                var suggestions = new List<IPackageIndexModelInfo>(CollectNamespaceSuggestions(node, distinctTargetFrameworks));
                suggestions.AddRange(CollectExtensionSuggestions(node, distinctTargetFrameworks));
                suggestions.AddRange(CollectTypeSuggestions(node, distinctTargetFrameworks));

                foreach (var packageInfo in suggestions.Take(MaxPackageSuggestions))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var namespaceName = packageInfo.GetNamespace();
                    AddPackageCodeAction action = null;
                    if (string.IsNullOrEmpty(namespaceName))
                    {
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

        private IEnumerable<IPackageIndexModelInfo> CollectNamespaceSuggestions(SyntaxNode node, IEnumerable<TargetFrameworkMetadata> distinctTargetFrameworks)
        {
            string entityName;
            IEnumerable<IPackageIndexModelInfo> potentialSuggestions = null;
            if (_syntaxHelper.IsImport(node, out entityName))
            {
                potentialSuggestions = _packageSearcher.SearchNamespace(entityName);
            }

            if (potentialSuggestions == null || !potentialSuggestions.Any())
            {
                return new List<IPackageIndexModelInfo>();
            }

            return TargetFrameworkHelper.GetSupportedPackages(potentialSuggestions,
                                                                distinctTargetFrameworks, allowHigherVersions: true);
        }

        private IEnumerable<IPackageIndexModelInfo> CollectExtensionSuggestions(SyntaxNode node, IEnumerable<TargetFrameworkMetadata> distinctTargetFrameworks)
        {
            string entityName;
            IEnumerable<IPackageIndexModelInfo> potentialSuggestions = null;
            if (_syntaxHelper.IsExtension(node))
            {
                entityName = node.ToString().NormalizeGenericName();

                potentialSuggestions = _packageSearcher.SearchExtension(entityName);
            }

            if (potentialSuggestions == null || !potentialSuggestions.Any())
            {
                return new List<IPackageIndexModelInfo>();
            }

            return TargetFrameworkHelper.GetSupportedPackages(potentialSuggestions,
                                                                distinctTargetFrameworks, allowHigherVersions: true);
        }

        private IEnumerable<IPackageIndexModelInfo> CollectTypeSuggestions(SyntaxNode node, IEnumerable<TargetFrameworkMetadata> distinctTargetFrameworks)
        {
            string entityName;
            IEnumerable<IPackageIndexModelInfo> potentialSuggestions = null;
            if (_syntaxHelper.IsType(node))
            {
                entityName = node.ToString().NormalizeGenericName();

                potentialSuggestions = _packageSearcher.SearchType(entityName);
            }

            if (potentialSuggestions == null || !potentialSuggestions.Any())
            {
                return new List<IPackageIndexModelInfo>();
            }

            return TargetFrameworkHelper.GetSupportedPackages(potentialSuggestions,
                                                                distinctTargetFrameworks, allowHigherVersions: true);
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

