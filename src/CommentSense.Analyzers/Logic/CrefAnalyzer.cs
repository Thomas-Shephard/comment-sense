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

        var associatedSymbol = crefAttribute.GetAssociatedSymbol(context.SemanticModel);
        if (associatedSymbol is null)
            return;

        var tree = context.Node.SyntaxTree;
        var options = CommentSenseOptions.GetOptions(context.Options.AnalyzerConfigOptionsProvider, tree);

        if (!associatedSymbol.IsEligibleForAnalysis(options.VisibilityLevel))
            return;

        var cref = crefAttribute.Cref;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(cref, context.CancellationToken);
        var resolvedSymbol = symbolInfo.Symbol;

        if (CheckGenericArgumentsVisibility(context, associatedSymbol, cref, resolvedSymbol))
            return;

        if (resolvedSymbol is null && symbolInfo.CandidateSymbols.IsEmpty)
        {
            HandleUnresolvedCref(context, crefAttribute, cref.ToString(), cref.GetLocation(), associatedSymbol, options);
            return;
        }

        if (resolvedSymbol is not null)
            HandleResolvedCref(context, associatedSymbol, cref, resolvedSymbol);

        if (crefAttribute.IsInExceptionTag())
            HandleExceptionTagCref(context, associatedSymbol, cref, cref.ToString(), resolvedSymbol, options);
    }

    private static bool CheckGenericArgumentsVisibility(SyntaxNodeAnalysisContext context, ISymbol associatedSymbol, CrefSyntax cref, ISymbol? resolvedSymbol)
    {
        var isGeneric = resolvedSymbol is INamedTypeSymbol { IsGenericType: true };
        var hasTypeArguments = cref.DescendantNodes().OfType<TypeArgumentListSyntax>().Any();

        if (!isGeneric && !hasTypeArguments)
            return false;

        var associatedVisibility = associatedSymbol.GetEffectiveVisibilityLevel();
        foreach (var typeArgList in cref.DescendantNodes().OfType<TypeArgumentListSyntax>())
        {
            foreach (var arg in typeArgList.Arguments)
            {
                var argSymbol = context.SemanticModel.GetSymbolInfo(arg, context.CancellationToken).Symbol;
                if (argSymbol is null)
                    continue;

                var argVisibility = argSymbol.GetEffectiveVisibilityLevel();
                if (argVisibility > associatedVisibility)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        CommentSenseRules.InaccessibleCrefRule,
                        cref.GetLocation(),
                        (resolvedSymbol ?? argSymbol).ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        associatedSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                    return true;
                }
            }
        }

        return false;
    }

    private static void HandleResolvedCref(SyntaxNodeAnalysisContext context, ISymbol associatedSymbol, CrefSyntax cref, ISymbol resolvedSymbol)
    {
        var associatedVisibility = associatedSymbol.GetEffectiveVisibilityLevel();
        var resolvedVisibility = resolvedSymbol.GetEffectiveVisibilityLevel();

        if (resolvedVisibility > associatedVisibility)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CommentSenseRules.InaccessibleCrefRule,
                cref.GetLocation(),
                resolvedSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                associatedSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
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
