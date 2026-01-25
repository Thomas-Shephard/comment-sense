using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class CrefAnalyzer
{
    public static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var crefAttribute = (XmlCrefAttributeSyntax)context.Node;

        var memberDecl = crefAttribute.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberDecl is null)
            return;

        var symbol = memberDecl is BaseFieldDeclarationSyntax { Declaration.Variables.Count: > 0 } fieldDecl
            ? context.SemanticModel.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0])
            : context.SemanticModel.GetDeclaredSymbol(memberDecl);

        var tree = context.Node.SyntaxTree;
        var analyzeInternal = AnalyzerOptions.GetBoolOption(context.Options.AnalyzerConfigOptionsProvider, tree, "analyze_internal", defaultValue: false);

        if (symbol is null || !symbol.IsEligibleForAnalysis(analyzeInternal))
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
