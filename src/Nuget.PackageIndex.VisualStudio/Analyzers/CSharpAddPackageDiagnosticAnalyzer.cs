// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Nuget.PackageIndex.Client.Analyzers;
using Nuget.PackageIndex.VisualStudio.Analyzers.IdentifierFilters;

namespace Nuget.PackageIndex.VisualStudio.Analyzers
{
    /// <summary>
    /// CSharp analyzer looking for all unknown identifiers and checking if they are types 
    /// that are located in some package that is not installed yet in the project. If such 
    /// package found it suggest user to install it.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpAddPackageDiagnosticAnalyzer : 
        AddPackageDiagnosticAnalyzer<SyntaxKind, SimpleNameSyntax, QualifiedNameSyntax, IdentifierNameSyntax>
    {
        internal const string DiagnosticId = "MissingPackage";
        internal const string AddPackageDiagnosticDefaultMessageFormat = "{0}";

        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.IdentifierName);

        public CSharpAddPackageDiagnosticAnalyzer()
            : base(new [] { new UsingIdentifierFilter() }, ProjectMetadataProvider.Instance)
        {
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
    }
}
