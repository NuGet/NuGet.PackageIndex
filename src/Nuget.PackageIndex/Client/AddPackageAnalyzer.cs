// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Nuget.PackageIndex.Logging;
using Nuget.PackageIndex.Models;
using Nuget.PackageIndex.Client.Analyzers;

namespace Nuget.PackageIndex.Client
{
    /// <summary>
    /// This is a language agnostic base class that can add a missing package
    /// for given unknown type.
    /// TODO: Add ILogger here for telemetry purposes
    /// </summary>
    public class AddPackageAnalyzer : IAddPackageAnalyzer
    {
        private readonly IPackageSearcher _packageSearcher;
        private readonly ISyntaxHelper _syntaxHelper;

        public AddPackageAnalyzer(ISyntaxHelper syntaxHelper)
            : this(new PackageSearcher(new LogFactory(LogLevel.Quiet)), syntaxHelper)
        {
        }

        public AddPackageAnalyzer(ILog logger, ISyntaxHelper syntaxHelper)
            : this(new PackageSearcher(logger), syntaxHelper)
        {
        }

        internal AddPackageAnalyzer(IPackageSearcher packageSearcher, ISyntaxHelper syntaxHelper)
        {
            _packageSearcher = packageSearcher;
            _syntaxHelper = syntaxHelper;
        }

        #region IAddPackageAnalyzer

        public ISyntaxHelper SyntaxHelper
        {
            get
            {
                return _syntaxHelper;
            }
        }

        public IList<IPackageIndexModelInfo> GetSuggestions(SyntaxNode node, IEnumerable<ProjectMetadata> projects)
        {
            if (node == null || projects == null || !projects.Any())
            {
                return null;
            }

            // get distinct frameworks from all projects current file belongs to
            var distinctTargetFrameworks = TargetFrameworkHelper.GetDistinctTargetFrameworks(projects);

            var suggestions = new List<IPackageIndexModelInfo>(CollectNamespaceSuggestions(node, distinctTargetFrameworks));
            suggestions.AddRange(CollectExtensionSuggestions(node, distinctTargetFrameworks));
            suggestions.AddRange(CollectTypeSuggestions(node, distinctTargetFrameworks));

            return suggestions;
        }

        #endregion 

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
                return Enumerable.Empty<IPackageIndexModelInfo>();
            }

            return TargetFrameworkHelper.GetSupportedPackages(potentialSuggestions,
                                                              distinctTargetFrameworks, 
                                                              allowHigherVersions: true);
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
                return Enumerable.Empty<IPackageIndexModelInfo>();
            }

            return TargetFrameworkHelper.GetSupportedPackages(potentialSuggestions,
                                                              distinctTargetFrameworks, 
                                                              allowHigherVersions: true);
        }

        private IEnumerable<IPackageIndexModelInfo> CollectTypeSuggestions(SyntaxNode node, IEnumerable<TargetFrameworkMetadata> distinctTargetFrameworks)
        {
            var potentialSuggestions = new List<IPackageIndexModelInfo>();
            if (_syntaxHelper.IsType(node))
            {
                var entityNamesToSearchFor = new List<string>();
                var entityName = node.ToString().NormalizeGenericName();
                entityNamesToSearchFor.Add(entityName);

                if (_syntaxHelper.IsAttribute(node) && !entityName.EndsWith("Attribute"))
                {
                    entityNamesToSearchFor.Add(entityName + "Attribute");
                }

                foreach (var entity in entityNamesToSearchFor)
                {
                    var suggestions = _packageSearcher.SearchType(entity);
                    if (suggestions != null)
                    {
                        potentialSuggestions.AddRange(suggestions);
                    }
                }
            }

            if (potentialSuggestions == null || !potentialSuggestions.Any())
            {
                return Enumerable.Empty<IPackageIndexModelInfo>();
            }

            return TargetFrameworkHelper.GetSupportedPackages(potentialSuggestions,
                                                              distinctTargetFrameworks, 
                                                              allowHigherVersions: true);
        }       
    }
}

