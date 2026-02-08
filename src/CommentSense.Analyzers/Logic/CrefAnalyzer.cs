using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using CommentSense.Core;
using CommentSense.Core.Utilities;

namespace CommentSense.Analyzers.Logic;

internal static class CrefAnalyzer
{
    public static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var crefAttribute = (XmlCrefAttributeSyntax)context.Node;

        var symbol = crefAttribute.GetAssociatedSymbol(context.SemanticModel);
        if (symbol is null)
            return;

        var tree = context.Node.SyntaxTree;
        var options = CommentSenseOptions.GetOptions(context.Options.AnalyzerConfigOptionsProvider, tree);

        if (!symbol.IsEligibleForAnalysis(options.VisibilityLevel))
            return;

        var cref = crefAttribute.Cref;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(cref, context.CancellationToken);

        if (symbolInfo.Symbol is null && symbolInfo.CandidateSymbols.IsEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.UnresolvedCrefRule, cref.GetLocation(), cref.ToString()));
            return;
        }

        if (!crefAttribute.IsInExceptionTag())
            return;

        if (symbolInfo.Symbol is not ITypeSymbol typeSymbol)
        {
            if (symbolInfo.Symbol is not null)
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidExceptionTypeRule, cref.GetLocation(), symbolInfo.Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        var exceptionType = context.Compilation.GetTypeByMetadataName("System.Exception");
        if (exceptionType != null && !typeSymbol.InheritsFromOrEquals(exceptionType))
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidExceptionTypeRule, cref.GetLocation(), typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}
