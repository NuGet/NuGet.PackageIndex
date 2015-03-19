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
        private readonly IPackageSearcher _packageSearcher;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        public AddPackageDiagnosticAnalyzer(IEnumerable<IIdentifierFilter> identifierFilters, ITargetFrameworkProvider targetFrameworkProvider)
            : this(new PackageSearcher(new LogFactory(LogLevel.Quiet)), identifierFilters, targetFrameworkProvider)
        {
        }

        public AddPackageDiagnosticAnalyzer(IEnumerable<IIdentifierFilter> identifierFilters, ITargetFrameworkProvider targetFrameworkProvider, ILog logger)
            : this(new PackageSearcher(logger), identifierFilters, targetFrameworkProvider)
        {
        }

        internal AddPackageDiagnosticAnalyzer(IPackageSearcher packageSearcher, IEnumerable<IIdentifierFilter> identifierFilters, ITargetFrameworkProvider targetFrameworkProvider)
            : base(identifierFilters)
        {
            _packageSearcher = packageSearcher;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        /// <summary>
        /// Does actual query to Packag Index to find packages where a given type is defined.
        /// Note: At diagnostic level there no way to know in which document, project or workspace 
        /// we are for a given SyntaxNode (or it's SyntaxNodeAnalysisContext) since diagnostics 
        /// work at csc.exe level (in command line for example) and there no container objects defined
        /// at that time. 
        /// Thus we will display information about all packages returned form index to suggest user where
        /// type can be located, however when user clicks Ctrl+. we would display only code fixes for packages
        /// that satisfy current project target frameworks list.
        /// </summary>
        protected override IEnumerable<string> AnalyzeNode(TIdentifierNameSyntax node)
        {
            // TODO remove this after RC
            var projectFilter = GetProjectFilter();
            if (!projectFilter.IsProjectSupported(node.GetLocation().SourceTree.FilePath))
            {
                return null;
            }

            var typeName = node.ToString();
            var packagesWithGivenType = _packageSearcher.Search(typeName);
            if (!packagesWithGivenType.Any())
            {
                return null;
            }

            // Check if project supports packages' target frameworks.
            // Note: if _targetFrameworkProvider returns null or empty list it means that project type
            // did not support discovery of target frameworks and thus we default to display all available
            // packages for discoverability purpose (this whole feature is about discoverability). In this case
            // we let user to figure out what he wants to do with unsupported packages, we at least show them.
            List<TypeInfo> supportedPackages;
            var projectTargetFrameworks = _targetFrameworkProvider.GetTargetFrameworks(node.GetLocation().SourceTree.FilePath);
            if (projectTargetFrameworks != null && projectTargetFrameworks.Any())
            {
                // if project target frameworks are provided, try to filter
                supportedPackages = new List<TypeInfo>();
                foreach (var packageInfo in packagesWithGivenType)
                {
                    if (SupportsProjectTargetFrameworks(packageInfo, projectTargetFrameworks))
                    {
                        supportedPackages.Add(packageInfo);
                    }
                }
            }
            else
            {
                // if project did not provide target frameworks to us, show all packages with requested type
                supportedPackages = new List<TypeInfo>(packagesWithGivenType);
            }

            return GetFriendlyPackagesString(supportedPackages);
        }

        private bool SupportsProjectTargetFrameworks(TypeInfo typeInfo, IEnumerable<string> projectTargetFrameworks)
        {
            // if we find at least any framework in package that current project supports,
            // we show this package to user.
            if (typeInfo.TargetFrameworks == null || !typeInfo.TargetFrameworks.Any())
            {
                // In this case package did not specify any target frameworks and we follow our default 
                // behavior and display as much as possible to the user
                return true;
            }
            else
            {
                var packageFrameworkNames = typeInfo.TargetFrameworks.Select(x => VersionUtility.ParseFrameworkName(x)).ToList();
                foreach (var projectFramework in projectTargetFrameworks)
                {
                    var projectFrameworkName = VersionUtility.ParseFrameworkName(projectFramework);
                    if (VersionUtility.IsCompatible(projectFrameworkName, packageFrameworkNames))
                    {
                        // if at least any project target framework supports package - display it
                        return true;
                    }
                }
            }

            return false;
        }

        private IEnumerable<string> GetFriendlyPackagesString(IEnumerable<TypeInfo> types)
        {
            foreach (var t in types)
            {
                var targetFrameworks = string.Join(";", t.TargetFrameworks.Select(x => GetFrameworkFriendlyName(x)));
                yield return string.Format(FriendlyMessageFormat, string.Format("{0}, {1} {2}, Supported frameworks: {3}", t.FullName, t.PackageName, t.PackageVersion, targetFrameworks));
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

        // must have 1 format placeholder {0}
        protected abstract string FriendlyMessageFormat { get; }
        protected abstract IProjectFilter GetProjectFilter();

        #endregion
    }
}


