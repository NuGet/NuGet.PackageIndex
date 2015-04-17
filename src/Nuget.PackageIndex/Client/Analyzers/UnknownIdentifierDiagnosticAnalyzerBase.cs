using System;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace Nuget.PackageIndex.Client.Analyzers
{
    /// <summary>
    /// Generic language agnostic base class for diagnostics interested in finding all unknown identifiers (types)
    /// </summary>
    /// <typeparam name="TLanguageKindEnum">Specifies syntax language</typeparam>
    /// <typeparam name="TSimpleNameSyntax">Specifies simple name syntax</typeparam>
    /// <typeparam name="TQualifiedNameSyntax">Specifies qualified name syntax</typeparam>
    /// <typeparam name="TIdentifierNameSyntax">Specifies identifier name syntax</typeparam>
    public abstract class UnknownIdentifierDiagnosticAnalyzerBase<TLanguageKindEnum, TSimpleNameSyntax, TQualifiedNameSyntax, TIdentifierNameSyntax> : DiagnosticAnalyzer
        where TLanguageKindEnum : struct
        where TSimpleNameSyntax : SyntaxNode
        where TQualifiedNameSyntax : SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
    {
        private readonly IEnumerable<IIdentifierFilter> _identifierFilters;

        public UnknownIdentifierDiagnosticAnalyzerBase(IEnumerable<IIdentifierFilter> identifierFilters)
        {
            _identifierFilters = identifierFilters;
        }
        
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
            var identifierNameSyntaxNode = (TIdentifierNameSyntax)context.Node;

            Func<SyntaxNode, bool> isQualifiedOrSimpleName = (SyntaxNode n) => n is TQualifiedNameSyntax || n is TSimpleNameSyntax;

            if (!isQualifiedOrSimpleName(identifierNameSyntaxNode))
            {
                return;
            }

            var identifierSymbolInfo = context.SemanticModel.GetSymbolInfo(identifierNameSyntaxNode);
            if (identifierSymbolInfo.Symbol != null 
                || identifierSymbolInfo.CandidateSymbols.Any()
                || identifierSymbolInfo.CandidateReason == CandidateReason.LateBound)
            {
                return;
            }

            // check if unknow identifier is really unknown type or id, and not one of the corner cases
            // where identifier can not be bind to symbol, but is actually legal. For example, 
            // using IType=System.Type, here IType would be also unknown identifier for us, but we don't
            // need to suggest any fixes. 
            if (!_identifierFilters.All(x => x.ShouldDisplayDiagnostic(identifierNameSyntaxNode)))
            {
                return;
            }

            var messages = AnalyzeNode(identifierNameSyntaxNode);
            if (messages == null)
            {
                return;
            }

            foreach(var message in messages)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, identifierNameSyntaxNode.GetLocation(), message));
            }
        }

        #region Abstract methods and properties

        protected abstract DiagnosticDescriptor DiagnosticDescriptor { get; }
        protected abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }
        protected abstract IEnumerable<string> AnalyzeNode(TIdentifierNameSyntax node);

        #endregion
    }
}


