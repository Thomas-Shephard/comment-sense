using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using System.Collections.Immutable;

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
            HandleUnresolvedCref(context, crefAttribute, cref, symbol, options);
            return;
        }

        if (crefAttribute.IsInExceptionTag())
            HandleExceptionTagCref(context, symbol, cref, symbolInfo.Symbol, options);
    }

    private static void HandleUnresolvedCref(SyntaxNodeAnalysisContext context, XmlCrefAttributeSyntax crefAttribute, CrefSyntax cref, ISymbol associatedSymbol, CommentSenseOptions options)
    {
        var properties = ImmutableDictionary<string, string?>.Empty;
        if (crefAttribute.IsInExceptionTag())
        {
            var suggestion = ExceptionAnalyzer.FindBestMatchingThrownException(associatedSymbol, cref.ToString(), options, context.Compilation, context.CancellationToken);
            if (suggestion != null)
            {
                properties = properties.Add(DocumentationAttributes.CrefProperty, suggestion);
            }
            else
            {
                var resolved = ExceptionAnalyzer.ResolveExceptionType(cref.ToString(), context.Compilation);
                if (resolved != null)
                    properties = properties.Add(DocumentationAttributes.CrefProperty, resolved.ToCrefString());
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.UnresolvedCrefRule, cref.GetLocation(), properties, cref.ToString()));
    }

    private static void HandleExceptionTagCref(SyntaxNodeAnalysisContext context, ISymbol associatedSymbol, CrefSyntax cref, ISymbol? resolvedSymbol, CommentSenseOptions options)
    {
        if (resolvedSymbol is not ITypeSymbol typeSymbol)
        {
            if (resolvedSymbol is not null)
            {
                var properties = CreateExceptionProperties(context, associatedSymbol, cref, options);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidExceptionTypeRule, cref.GetLocation(), properties, resolvedSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
            return;
        }

        var exceptionType = context.Compilation.GetTypeByMetadataName("System.Exception");
        if (exceptionType != null && !typeSymbol.InheritsFromOrEquals(exceptionType))
        {
            var properties = CreateExceptionProperties(context, associatedSymbol, cref, options);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidExceptionTypeRule, cref.GetLocation(), properties, typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static ImmutableDictionary<string, string?> CreateExceptionProperties(SyntaxNodeAnalysisContext context, ISymbol associatedSymbol, CrefSyntax cref, CommentSenseOptions options)
    {
        var properties = ImmutableDictionary<string, string?>.Empty;
        var suggestion = ExceptionAnalyzer.FindBestMatchingThrownException(associatedSymbol, cref.ToString(), options, context.Compilation, context.CancellationToken);
        if (suggestion != null)
            properties = properties.Add(DocumentationAttributes.CrefProperty, suggestion);

        return properties;
    }
}
