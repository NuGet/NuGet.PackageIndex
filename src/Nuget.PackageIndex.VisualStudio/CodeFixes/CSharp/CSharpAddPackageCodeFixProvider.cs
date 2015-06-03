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

namespace Nuget.PackageIndex.VisualStudio.CodeFixes.CSharp
{
    /// <summary>
    /// CSharp code fix provider adding a missing package and using statement to the project,
    /// for given unknown type if this type information was found in the package index.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = ProviderName), Shared]
    public sealed class CSharpAddPackageCodeFixProvider : AddPackageCodeFixProviderBase
    {
        private const string DefaultTitleFormat = "Add package {0}";
        private const string TitleHostResourceId = "AddPackageTitleFormat";
        private const string ProviderName = "AddPackage";
        private readonly SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        public CSharpAddPackageCodeFixProvider([Import]SVsServiceProvider serviceProvider)
            : base(new PackageInstaller(serviceProvider), ProjectMetadataProvider.Instance, new CSharpSyntaxHelper())
        {
            _serviceProvider = serviceProvider;
        }

        private const string CS1061 = "CS1061"; // error CS1061: 'C' does not contain a definition for 'Foo' and no extension method 'Foo' accepting a first argument of type 'C' could be found
        private const string CS0103 = "CS0103"; // error CS0103: The name 'Foo' does not exist in the current context
        private const string CS0246 = "CS0246"; // error CS0246: The type or namespace name 'Version' could not be found
        private const string CS0234 = "CS0234"; // error CS0234: The type or namespace name 'Abc' does not exist in the namespace 'Bar' (are you missing an assembly reference?)

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(CS1061, CS0103, CS0246, CS0234);
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
