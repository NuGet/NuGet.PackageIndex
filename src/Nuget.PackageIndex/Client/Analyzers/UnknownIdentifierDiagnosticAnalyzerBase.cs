// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nuget.PackageIndex.Client.Analyzers
{
    /// <summary>
    /// Generic language agnostic base class for diagnostics interested in finding all unknown identifiers (types)
    /// </summary>
    /// <typeparam name="TLanguageKindEnum">Specifies syntax language</typeparam>
    /// <typeparam name="TSimpleNameSyntax">Specifies simple name syntax</typeparam>
    /// <typeparam name="TQualifiedNameSyntax">Specifies qualified name syntax</typeparam>
    /// <typeparam name="TIdentifierNameSyntax">Specifies identifier name syntax</typeparam>
    /// <typeparam name="TGenericNameSyntax">Specifies generic name syntax</typeparam>
    public abstract class UnknownIdentifierDiagnosticAnalyzerBase<TLanguageKindEnum, TSimpleNameSyntax, TQualifiedNameSyntax, TIdentifierNameSyntax, TGenericNameSyntax> : DiagnosticAnalyzer
        where TLanguageKindEnum : struct
        where TSimpleNameSyntax : SyntaxNode
        where TQualifiedNameSyntax : SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
        where TGenericNameSyntax : SyntaxNode
    {       
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(DiagnosticDescriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNodeInternal, SyntaxKindsOfInterest.ToArray());
        }

        protected DiagnosticDescriptor GetDiagnosticDescriptor(string id, string category, string title, string messageFormat)
        {
            return new DiagnosticDescriptor(
                id,
                title, 
                messageFormat,
                category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                customTags: new[] { WellKnownDiagnosticTags.Telemetry});
        }

        private void AnalyzeNodeInternal(SyntaxNodeAnalysisContext context)
        {
            Func<SyntaxNode, bool> isSupportedSyntax = (SyntaxNode n) => n is TQualifiedNameSyntax 
                                                                            || n is TSimpleNameSyntax
                                                                            || n is TIdentifierNameSyntax
                                                                            || n is TGenericNameSyntax;
             
            if (!isSupportedSyntax(context.Node))
            {
                return;
            }

            var identifierSymbolInfo = context.SemanticModel.GetSymbolInfo(context.Node);
            if (identifierSymbolInfo.Symbol != null 
                || identifierSymbolInfo.CandidateSymbols.Any()
                || identifierSymbolInfo.CandidateReason == CandidateReason.LateBound)
            {
                return;
            }

            var messages = AnalyzeNode(context);
            if (messages == null)
            {
                return;
            }

            foreach(var message in messages)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, context.Node.GetLocation(), message));
            }
        }

        #region Abstract methods and properties

        protected abstract DiagnosticDescriptor DiagnosticDescriptor { get; }
        protected abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }
        protected abstract IEnumerable<string> AnalyzeNode(SyntaxNodeAnalysisContext context);

        #endregion
    }
}


