using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nuget.PackageIndex.Client.Analyzers;
using Nuget.PackageIndex.VisualStudio.Analyzers.IdentifierFilters;
using System;

namespace Nuget.PackageIndex.VisualStudio.Analyzers
{
    /// <summary>
    /// CSharp analyzer looking for all unknown identifiers and checking if they are types 
    /// that are located in some package that is not installed yet in the project. If such 
    /// package found it suggest user to install it.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpAddPackageDiagnosticAnalyzer : AddPackageDiagnosticAnalyzer<SyntaxKind, SimpleNameSyntax, QualifiedNameSyntax, IdentifierNameSyntax>
    {
        internal const string DiagnosticId = "MissingPackage";
        internal const string AddPackageDiagnosticDefaultMessageFormat = "{0}";

        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.IdentifierName);

        private readonly IProjectFilter _projectFilter;

        public CSharpAddPackageDiagnosticAnalyzer()
            : base(new [] // filters can be hardcoded here untill we need an extensibility
                          {
                              new UsingIdentifierFilter()
                          }) // TODO add logger that prints to Package manager console
        {
            _projectFilter = new ProjectKFilter();
        }

        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
        {
            get
            {  
                return s_kindsOfInterest;
            }
        }

        protected override DiagnosticDescriptor DiagnosticDescriptor
        {
            get
            {
                // TODO project imported interface should have GetMessage method 
                return GetDiagnosticDescriptor(DiagnosticId, 
                                               Resources.AddPackageDiagnosticCategory, 
                                               Resources.AddPackageDiagnosticTitle, 
                                               AddPackageDiagnosticDefaultMessageFormat);
            }
        }

        protected override string FriendlyMessageFormat
        {
            get
            {
                return Resources.AddPackageDiagnosticFriendlyMessageFormat;
            }
        }

        protected override IProjectFilter GetProjectFilter()
        {
            // this static instance of the filter is initialized in CSharpAddPackageCodeFixProvider 
            // which is initialized by MEF and receives an instance of SVsServiceProvider. Thi is a hack,
            // since DiagnosticAnalyzers are loaded using reflection and Attributes instead of MEF composition,
            // so we can not receive SVsServiceProvider for Diagnostic analyzer and just use this static instance.
            // Filters shuold be removed after RC, this hack is temporary



            return _projectFilter;
        }
    }
}
