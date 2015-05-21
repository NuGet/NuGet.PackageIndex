// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using NuGet;
using Nuget.PackageIndex.Logging;
using TypeInfo = Nuget.PackageIndex.Models.TypeInfo;

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
        // we want to limit number of suggsted packages, to avoid rare cases when user had 
        // too many packages with the same type (since it would create super long list of suggestions,
        // which would be not that usable anyway).
        internal const int MaxPackageSuggestions = 5;

        private readonly IPackageSearcher _packageSearcher;
        private readonly IProjectMetadataProvider _projectMetadataProvider;
        
        public AddPackageDiagnosticAnalyzer(IEnumerable<IIdentifierFilter> identifierFilters, 
                                            IProjectMetadataProvider projectMetadataProvider)
            : this(new PackageSearcher(new LogFactory(LogLevel.Quiet)), identifierFilters, projectMetadataProvider)
        {
        }

        public AddPackageDiagnosticAnalyzer(IEnumerable<IIdentifierFilter> identifierFilters, 
                                            IProjectMetadataProvider projectMetadataProvider, 
                                            ILog logger)
            : this(new PackageSearcher(logger), identifierFilters, projectMetadataProvider)
        {
        }

        internal AddPackageDiagnosticAnalyzer(IPackageSearcher packageSearcher, 
                                              IEnumerable<IIdentifierFilter> identifierFilters, 
                                              IProjectMetadataProvider projectMetadataProvider)
            : base(identifierFilters)
        {
            _packageSearcher = packageSearcher;
            _projectMetadataProvider = projectMetadataProvider;
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
        protected override IEnumerable<string> AnalyzeNode(TIdentifierNameSyntax node)
        {
            var projects = _projectMetadataProvider.GetProjects(node.GetLocation().SourceTree.FilePath);
            if (projects == null || !projects.Any())
            {
                // project is unsupported
                return null;
            }

            var typeName = node.ToString();
            var packagesWithGivenType = _packageSearcher.Search(typeName);
            if (!packagesWithGivenType.Any())
            {
                return null;
            }

            // get distinct frameworks from all projects current file belongs to
            var distinctTargetFrameworks = TargetFrameworkHelper.GetDistinctTargetFrameworks(projects);

            // Note: allowHigherVersions=true here since we want to show diagnostic message for type if it exists in some package 
            // for discoverability (tooltip) but we would not supply a code fix if package already installed in the project with 
            // any version, user needs to upgrade on his own.
            // Note2: the problem here is that we don't know if type exist in older versions of the package or not and to store 
            // all package versions in index might slow things down. If we receive feedback that we need ot be more smart here 
            // we should consider adding all package versions to the local index.
            var supportedPackages = TargetFrameworkHelper.GetSupportedPackages(packagesWithGivenType,
                                                            distinctTargetFrameworks, allowHigherVersions: true);
            return GetFriendlyPackagesString(supportedPackages.Take(MaxPackageSuggestions));
        }

        private IEnumerable<string> GetFriendlyPackagesString(IEnumerable<TypeInfo> types)
        {
            foreach (var t in types)
            {
                var targetFrameworks = string.Join(";", t.TargetFrameworks.Select(x => GetFrameworkFriendlyName(x)));
                yield return string.Format(FriendlyMessageFormat, t.FullName, t.PackageName, t.PackageVersion, targetFrameworks);
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

        #region Abstract methods and properties

        /// <summary>
        /// Allow to overwrite it in concrete implementations to support other locales
        /// </summary>
        protected abstract string FriendlyMessageFormat { get; }

        #endregion
    }
}


