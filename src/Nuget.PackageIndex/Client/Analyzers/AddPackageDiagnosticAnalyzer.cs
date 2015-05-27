// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using NuGet;
using Nuget.PackageIndex.Logging;
using Nuget.PackageIndex.Models;
using Microsoft.CodeAnalysis.Diagnostics;

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
    public abstract class AddPackageDiagnosticAnalyzer<TLanguageKindEnum, TSimpleNameSyntax, TQualifiedNameSyntax, TIdentifierNameSyntax, TGenericNameSyntax> 
        : UnknownIdentifierDiagnosticAnalyzerBase<TLanguageKindEnum, TSimpleNameSyntax, TQualifiedNameSyntax, TIdentifierNameSyntax, TGenericNameSyntax>
        where TLanguageKindEnum : struct
        where TSimpleNameSyntax : SyntaxNode
        where TQualifiedNameSyntax : SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
        where TGenericNameSyntax : SyntaxNode
    {
        // we want to limit number of suggested packages, to avoid rare cases when user had 
        // too many packages with the same type (since it would create super long list of suggestions,
        // which would be not that usable anyway).
        internal const int MaxPackageSuggestions = 5;
        private const string DefaultSuggestionFormat = "{0} {1} {2}"; // should have 3 placeholders name, package info, target frameworks

        private readonly IPackageSearcher _packageSearcher;
        private readonly IProjectMetadataProvider _projectMetadataProvider;
        private readonly ISyntaxHelper _syntaxHelper;

        public AddPackageDiagnosticAnalyzer(IProjectMetadataProvider projectMetadataProvider, ISyntaxHelper syntaxHelper)
            : this(new PackageSearcher(new LogFactory(LogLevel.Quiet)), projectMetadataProvider, syntaxHelper)
        {
        }

        public AddPackageDiagnosticAnalyzer(IProjectMetadataProvider projectMetadataProvider, 
                                            ILog logger,
                                            ISyntaxHelper syntaxHelper)
            : this(new PackageSearcher(logger), projectMetadataProvider, syntaxHelper)
        {
        }

        internal AddPackageDiagnosticAnalyzer(IPackageSearcher packageSearcher, 
                                              IProjectMetadataProvider projectMetadataProvider,
                                              ISyntaxHelper syntaxHelper)
        {
            _packageSearcher = packageSearcher;
            _projectMetadataProvider = projectMetadataProvider;
            _syntaxHelper = syntaxHelper;
        }

        /// <summary>
        /// Does actual query to Packag Index to find packages where a given type is defined.
        /// We will display information about all packages returned form index to suggest user where
        /// type can be located, however when user clicks Ctrl+. we would display only code fixes for packages
        /// that satisfy current project target frameworks list.
        /// 
        /// Note: At diagnostic level there no way to know in which document, project or workspace 
        /// we are for a given SyntaxNode (or it's SyntaxNodeAnalysisContext) since diagnostics 
        /// work at csc.exe level (in command line for example) and there no container objects defined
        /// at that time.
        /// </summary>
        protected override IEnumerable<string> AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var projects = _projectMetadataProvider.GetProjects(context.Node.GetLocation().SourceTree.FilePath);
            if (projects == null || !projects.Any())
            {
                // project is unsupported
                return null;
            }

            if (!_syntaxHelper.IsSupported(context.Node))
            {
                return null;
            }


            // get distinct frameworks from all projects current file belongs to
            var distinctTargetFrameworks = TargetFrameworkHelper.GetDistinctTargetFrameworks(projects);
            var suggestions = new List<string>(CollectNamespaceSuggestions(context, distinctTargetFrameworks));
            suggestions.AddRange(CollectExtensionSuggestions(context, distinctTargetFrameworks));
            suggestions.AddRange(CollectTypeSuggestions(context, distinctTargetFrameworks));

            return suggestions.Take(MaxPackageSuggestions);
        }

        private IEnumerable<string> CollectNamespaceSuggestions(SyntaxNodeAnalysisContext context, IEnumerable<TargetFrameworkMetadata> distinctTargetFrameworks)
        {
            var defaultResult = new List<string>();
            string entityName;
            IEnumerable<IPackageIndexModelInfo> potentialSuggestions = null;
            if (!_syntaxHelper.IsImport(context.Node, out entityName))
            {
                return defaultResult;
            }

            // if we are here, check if previous namespace is known, if it is
            // not we already did show suggestion, so skip it here
            var previousNode = context.Node.Parent.ChildNodes().First();
            var previousSymbol = context.SemanticModel.GetSymbolInfo(context.Node.Parent.ChildNodes().First());
            if (!previousNode.Equals(context.Node)
                && previousSymbol.Symbol == null)
            {
                return defaultResult;
            }

            potentialSuggestions = _packageSearcher.SearchNamespace(entityName);
            if (potentialSuggestions == null || !potentialSuggestions.Any())
            {
                return new List<string>();
            }

            // Note: allowHigherVersions=true here since we want to show diagnostic message for type if it exists in some package 
            // for discoverability (tooltip) but we would not supply a code fix if package already installed in the project with 
            // any version, user needs to upgrade on his own.
            // Note2: the problem here is that we don't know if type exist in older versions of the package or not and to store 
            // all package versions in index might slow things down. If we receive feedback that we need ot be more smart here 
            // we should consider adding all package versions to the local index.
            var supportedSuggestions = TargetFrameworkHelper.GetSupportedPackages(potentialSuggestions,
                                                                distinctTargetFrameworks, allowHigherVersions: true);

            return GetFriendlySuggestionsString(FriendlyNamespaceMessageFormat, supportedSuggestions);
        }

        private IEnumerable<string> CollectExtensionSuggestions(SyntaxNodeAnalysisContext context, IEnumerable<TargetFrameworkMetadata> distinctTargetFrameworks)
        {
            var defaultResult = new List<string>();
            string entityName;
            IEnumerable<ExtensionInfo> potentialSuggestions = null;
            if (!_syntaxHelper.IsExtension(context.Node))
            {
                return defaultResult;
            }

            // if we are here, node looks like possible extension method invocation,
            // now we need to check type of the invoker object in order to suggest 
            // extensions  for corresponding types only
            var parentTypeSymbol = context.SemanticModel.GetTypeInfo(context.Node.Parent.ChildNodes().First());
            if (parentTypeSymbol.Type == null 
                || parentTypeSymbol.Type.Kind == SymbolKind.ErrorType)
            {
                return defaultResult;
            }

            entityName = context.Node.ToString().NormalizeGenericName();
            var extensionFullName = parentTypeSymbol.Type.ToString().NormalizeGenericName() + "." + entityName;

            potentialSuggestions = _packageSearcher.SearchExtension(entityName);
            // select only extensions that have same parent (this) type as given symbol
            potentialSuggestions = potentialSuggestions.Where(x => x.FullName.Equals(extensionFullName));
            if (potentialSuggestions == null || !potentialSuggestions.Any())
            {
                return defaultResult;
            }

            // Note: allowHigherVersions=true here since we want to show diagnostic message for type if it exists in some package 
            // for discoverability (tooltip) but we would not supply a code fix if package already installed in the project with 
            // any version, user needs to upgrade on his own.
            // Note2: the problem here is that we don't know if type exist in older versions of the package or not and to store 
            // all package versions in index might slow things down. If we receive feedback that we need ot be more smart here 
            // we should consider adding all package versions to the local index.
            var supportedSuggestions = TargetFrameworkHelper.GetSupportedPackages(potentialSuggestions,
                                                                distinctTargetFrameworks, allowHigherVersions: true);

            return GetFriendlySuggestionsString(FriendlyExtensionMessageFormat, supportedSuggestions);
        }

        private IEnumerable<string> CollectTypeSuggestions(SyntaxNodeAnalysisContext context, IEnumerable<TargetFrameworkMetadata> distinctTargetFrameworks)
        {
            string entityName;
            IEnumerable<IPackageIndexModelInfo> potentialSuggestions = null;
            if (_syntaxHelper.IsType(context.Node))
            {
                entityName = context.Node.ToString().NormalizeGenericName();

                potentialSuggestions = _packageSearcher.SearchType(entityName);
            }

            if (potentialSuggestions == null || !potentialSuggestions.Any())
            {
                return new List<string>();
            }

            // Note: allowHigherVersions=true here since we want to show diagnostic message for type if it exists in some package 
            // for discoverability (tooltip) but we would not supply a code fix if package already installed in the project with 
            // any version, user needs to upgrade on his own.
            // Note2: the problem here is that we don't know if type exist in older versions of the package or not and to store 
            // all package versions in index might slow things down. If we receive feedback that we need ot be more smart here 
            // we should consider adding all package versions to the local index.
            var supportedSuggestions = TargetFrameworkHelper.GetSupportedPackages(potentialSuggestions,
                                                                distinctTargetFrameworks, allowHigherVersions: true);

            return GetFriendlySuggestionsString(FriendlyTypeMessageFormat, supportedSuggestions);
        }

        private IEnumerable<string> GetFriendlySuggestionsString(string format, IEnumerable<IPackageIndexModelInfo> suggestions)
        {
            foreach (var t in suggestions)
            {
                var targetFrameworks = string.Join(";", t.TargetFrameworks.Select(x => GetFrameworkFriendlyName(x)));
                yield return string.Format(format, t.GetFriendlyEntityName(), t.GetFriendlyPackageName(), targetFrameworks);
            }
        }

        private string GetFrameworkFriendlyName(string frameworkName)
        {
            var normalizedFrameworkName = VersionUtility.NormalizeFrameworkName(VersionUtility.ParseFrameworkName(frameworkName));
            string result = normalizedFrameworkName.Identifier;
            if (normalizedFrameworkName.Version != null 
                && (normalizedFrameworkName.Version.Major != 0 || normalizedFrameworkName.Version.Minor != 0))
            {
                result += " " + normalizedFrameworkName.Version;
            }

            if (string.IsNullOrEmpty(normalizedFrameworkName.Profile))
            {
                return result;
            }

            return result + "-" + normalizedFrameworkName.Profile;
        }

        #region Abstract and virtual methods and properties

        protected virtual string FriendlyNamespaceMessageFormat
        {
            get
            {
                return DefaultSuggestionFormat;
            }
        }

        protected virtual string FriendlyExtensionMessageFormat
        {
            get
            {
                return DefaultSuggestionFormat;
            }
        }

        protected virtual string FriendlyTypeMessageFormat
        {
            get
            {
                return DefaultSuggestionFormat;
            }
        }

        #endregion
    }
}


