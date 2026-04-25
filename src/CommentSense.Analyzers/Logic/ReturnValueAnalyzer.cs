using System.Collections.Immutable;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class ReturnValueAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, DocumentationComment documentation, CommentSenseOptions options)
    {
        Analyze(context, symbol, symbol, documentation, options);
    }

    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, ISymbol targetSymbol, DocumentationComment documentation, CommentSenseOptions options)
    {
        if (targetSymbol is IPropertySymbol property)
        {
            AnalyzeProperty(context, property, targetSymbol, documentation, options);
        }
        else if (symbol is IMethodSymbol methodSymbol)
        {
            AnalyzeMethod(context, methodSymbol, targetSymbol, documentation, options);
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, IPropertySymbol property, ISymbol targetSymbol, DocumentationComment documentation, CommentSenseOptions options)
    {
        var hasInheritDoc = documentation.HasInheritDoc();
        var hasAutoValidTag = documentation.HasAutoValidTag();

        if (property.GetMethod is not null && !documentation.HasValueTag() && !hasInheritDoc && !hasAutoValidTag)
        {
            var location = targetSymbol.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, DocumentationTags.Value);
            var symbolName = targetSymbol.GetDisplayName();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingValueDocumentationRule, location, properties, symbolName));
        }

        foreach (var element in documentation.GetElements(DocumentationTags.Returns, recursive: true))
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, element.GetLocation(), targetSymbol.GetDisplayName()));
        }

        var seenValue = false;
        foreach (var element in documentation.GetElements(DocumentationTags.Value, recursive: true))
        {
            var location = element.GetLocation();
            bool isTopLevel = documentation.IsTopLevel(element);

            if (!isTopLevel || seenValue)
            {
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayValueDocumentationRule, location, targetSymbol.GetDisplayName()));
                continue;
            }

            seenValue = true;

            if (QualityAnalyzer.IsLowQuality(element, property, targetSymbol, options))
            {
                QualityAnalyzer.Report(context, location, DocumentationTags.Value, targetSymbol.GetDisplayName());
            }
        }
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, IMethodSymbol methodSymbol, ISymbol targetSymbol, DocumentationComment documentation, CommentSenseOptions options)
    {
        var hasInheritDoc = documentation.HasInheritDoc();
        var hasAutoValidTag = documentation.HasAutoValidTag();

        var isTask = methodSymbol.ReturnType.IsTaskType();
        var isVoid = methodSymbol.ReturnsVoid;
        var returnsRequired = !isVoid && !isTask;

        if (returnsRequired && !documentation.HasReturnsTag() && !hasInheritDoc && !hasAutoValidTag)
        {
            var location = targetSymbol.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, DocumentationTags.Returns);
            var symbolName = targetSymbol.GetDisplayName();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingReturnValueDocumentationRule, location, properties, symbolName));
        }

        var seenReturns = false;
        foreach (var element in documentation.GetElements(DocumentationTags.Returns, recursive: true))
        {
            var location = element.GetLocation();
            bool isTopLevel = documentation.IsTopLevel(element);

            if (!isTopLevel || isVoid || seenReturns)
            {
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, targetSymbol.GetDisplayName()));
                continue;
            }

            seenReturns = true;

            if (QualityAnalyzer.IsLowQuality(element, methodSymbol, targetSymbol, options))
            {
                QualityAnalyzer.Report(context, location, DocumentationTags.Returns, targetSymbol.GetDisplayName());
            }
        }

        foreach (var element in documentation.GetElements(DocumentationTags.Value, recursive: true))
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayValueDocumentationRule, element.GetLocation(), targetSymbol.GetDisplayName()));
        }
    }
}
