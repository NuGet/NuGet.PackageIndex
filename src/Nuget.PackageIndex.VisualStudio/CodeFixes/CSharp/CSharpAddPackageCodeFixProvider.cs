// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Shell;
using Nuget.PackageIndex.Client.CodeFixes;
using Nuget.PackageIndex.VisualStudio.CodeFixes.CSharp.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using Nuget.PackageIndex.Client;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes.CSharp
{
    /// <summary>
    /// CSharp code fix provider adding a missing package and using statement to the project,
    /// for given unknown type if this type information was found in the package index.
    /// </summary>
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.AddUsingOrImport)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.AddMissingReference)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.FullyQualify)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.FixIncorrectExitContinue)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.GenerateConstructor)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.GenerateEndConstruct)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.GenerateEnumMember)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.GenerateEvent)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.GenerateVariable)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.GenerateMethod)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.GenerateType)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.ImplementAbstractClass)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.ImplementInterface)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.MoveToTopOfFile)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.RemoveUnnecessaryImports)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.SimplifyNames)]
    [ExtensionOrder(After = RoslynPredefinedCodeFixProviderNames.SpellCheck)]/// 
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = ProviderName), Shared]
    public sealed class CSharpAddPackageCodeFixProvider : AddPackageCodeFixProviderBase
    {
        private const string DefaultTitleFormat = "Add package {0}";
        private const string TitleHostResourceId = "AddPackageTitleFormat";
        private const string ProviderName = "AddPackage";
        private readonly SVsServiceProvider _serviceProvider;

        [Import]
        private IAddPackageAnalyzerFactory AnalyzerFactory { get; set; }

        [Import(LocalNugetPackageIndex.RoslynHandshakeContract, AllowDefault = true)]
        private object RoslynIndex { get; set; }

        [ImportingConstructor]
        public CSharpAddPackageCodeFixProvider([Import]SVsServiceProvider serviceProvider)
            : base(new PackageInstaller(serviceProvider), ProjectMetadataProvider.Instance)
        {
            _serviceProvider = serviceProvider;
        }

        protected override bool Enabled
        {
            get
            {
                return RoslynIndex == null;
            }
        }

        protected override IAddPackageAnalyzer Analyzer
        {
            get
            {
                return AnalyzerFactory.GetAnalyzer(".cs");
            }
        }

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                if (PackageIndexActivityLevelProvider.ActivityLevel > ActivityLevel.SuggestionsOnly)
                {
                    // if index should be turned complettely off - attach to NO diagnostics 
                    return ImmutableArray.Create<string>();
                }

                return ImmutableArray.Create(Analyzer.SyntaxHelper.SupportedDiagnostics);
            }
        }

        private string _actionTitle;
        protected override string ActionTitle
        {
            get
            {
                if (string.IsNullOrEmpty(_actionTitle))
                {
                    var container = _serviceProvider.GetService<IComponentModel, SComponentModel>();
                    if (container != null)
                    {
                        var resourceProvider = container.DefaultExportProvider.GetExportedValue<IPackageIndexHostResourceProvider>();

                        if (resourceProvider != null)
                        {
                            _actionTitle = resourceProvider.GetResourceString(TitleHostResourceId);
                        }
                    }

                    if (string.IsNullOrEmpty(_actionTitle))
                    {
                        _actionTitle = DefaultTitleFormat;
                    }
                }

                return _actionTitle;
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
        /// For now keep simple check if using directive can be added. Later we might add some checks here,
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
                if (usings.Any(x => x.ToString().StartsWith(newNamespaceUsing)))
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
