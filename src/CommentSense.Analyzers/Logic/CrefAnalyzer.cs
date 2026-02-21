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
            HandleUnresolvedCref(context, crefAttribute, cref.ToString(), cref.GetLocation(), symbol, options);
            return;
        }

        if (crefAttribute.IsInExceptionTag())
            HandleExceptionTagCref(context, symbol, cref, cref.ToString(), symbolInfo.Symbol, options);
    }

    private static void HandleUnresolvedCref(SyntaxNodeAnalysisContext context, XmlCrefAttributeSyntax crefAttribute, string crefText, Location location, ISymbol associatedSymbol, CommentSenseOptions options)
    {
        var properties = ImmutableDictionary<string, string?>.Empty;
        if (crefAttribute.IsInExceptionTag())
        {
            var suggestion = ExceptionAnalyzer.FindBestMatchingThrownException(associatedSymbol, crefText, options, context.Compilation, context.CancellationToken);
            if (suggestion != null)
            {
                properties = properties.Add(DocumentationAttributes.CrefProperty, suggestion);
            }
            else
            {
                var resolved = ExceptionAnalyzer.ResolveExceptionType(crefText, context.Compilation);
                var exceptionType = context.Compilation.GetTypeByMetadataName("System.Exception");
                if (resolved != null && exceptionType != null && resolved.InheritsFromOrEquals(exceptionType))
                    properties = properties.Add(DocumentationAttributes.CrefProperty, resolved.ToCrefString());
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.UnresolvedCrefRule, location, properties, crefText));
    }

    private static void HandleExceptionTagCref(SyntaxNodeAnalysisContext context, ISymbol associatedSymbol, CrefSyntax cref, string crefText, ISymbol? resolvedSymbol, CommentSenseOptions options)
    {
        var exceptionType = context.Compilation.GetTypeByMetadataName("System.Exception");
        if (resolvedSymbol is not ITypeSymbol typeSymbol)
        {
            if (resolvedSymbol is not null)
            {
                var properties = CreateExceptionProperties(context, associatedSymbol, crefText, options);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidExceptionTypeRule, cref.GetLocation(), properties, resolvedSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
            return;
        }

        if (exceptionType != null && !typeSymbol.InheritsFromOrEquals(exceptionType))
        {
            var properties = CreateExceptionProperties(context, associatedSymbol, crefText, options);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidExceptionTypeRule, cref.GetLocation(), properties, typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static ImmutableDictionary<string, string?> CreateExceptionProperties(SyntaxNodeAnalysisContext context, ISymbol associatedSymbol, string crefText, CommentSenseOptions options)
    {
        var properties = ImmutableDictionary<string, string?>.Empty;
        var suggestion = ExceptionAnalyzer.FindBestMatchingThrownException(associatedSymbol, crefText, options, context.Compilation, context.CancellationToken);
        if (suggestion != null)
            properties = properties.Add(DocumentationAttributes.CrefProperty, suggestion);

        return properties;
    }
}
