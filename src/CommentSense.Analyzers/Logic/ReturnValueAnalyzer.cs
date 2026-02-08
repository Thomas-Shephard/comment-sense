using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class ReturnValueAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options)
    {
        Analyze(context, symbol, symbol, xml, options);
    }

    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, ISymbol targetSymbol, XElement xml, CommentSenseOptions options)
    {
        var methodSymbol = symbol as IMethodSymbol;
        var isTask = methodSymbol != null && methodSymbol.ReturnType.IsTaskType();
        var isVoid = methodSymbol is { ReturnsVoid: true } || isTask;

        if (targetSymbol is IPropertySymbol property)
            AnalyzeProperty(context, property, targetSymbol, xml, options);
        else if (methodSymbol != null)
            AnalyzeMethod(context, methodSymbol, targetSymbol, xml, options, isVoid);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, IPropertySymbol property, ISymbol targetSymbol, XElement xml, CommentSenseOptions options)
    {
        var hasInheritDoc = DocumentationExtensions.HasInheritDoc(xml);
        var hasAutoValidTag = DocumentationExtensions.HasAutoValidTag(xml);

        // CSENSE014: Missing <value> documentation
        if (property.GetMethod is not null && !DocumentationExtensions.HasValueTag(xml) && !hasInheritDoc && !hasAutoValidTag)
        {
            var location = targetSymbol.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, DocumentationTags.Value);
            var symbolName = targetSymbol.GetDisplayName();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingValueDocumentationRule, location, properties, symbolName));
        }

        // CSENSE013: Stray <returns> tag on property
        if (DocumentationExtensions.GetTargetElements(xml, DocumentationTags.Returns, recursive: true).Any())
        {
            var location = targetSymbol.GetDocumentationLocation(DocumentationTags.Returns, topLevelOnly: false);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, targetSymbol.GetDisplayName()));
        }

        // CSENSE016: Low quality <value> documentation
        foreach (var (element, location) in targetSymbol.GetTargetElementsWithLocations(xml, DocumentationTags.Value, topLevelOnly: false))
        {
            if (!QualityAnalyzer.IsLowQuality(element, property, targetSymbol, options))
                continue;

            QualityAnalyzer.Report(context, location, DocumentationTags.Value, targetSymbol.GetDisplayName());
        }
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, IMethodSymbol methodSymbol, ISymbol targetSymbol, XElement xml, CommentSenseOptions options, bool isVoid)
    {
        var hasInheritDoc = DocumentationExtensions.HasInheritDoc(xml);
        var hasAutoValidTag = DocumentationExtensions.HasAutoValidTag(xml);

        // CSENSE006: Missing <returns> documentation
        if (!isVoid && !DocumentationExtensions.HasReturnsTag(xml) && !hasInheritDoc && !hasAutoValidTag)
        {
            var location = targetSymbol.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, DocumentationTags.Returns);
            var symbolName = targetSymbol.GetDisplayName();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingReturnValueDocumentationRule, location, properties, symbolName));
        }

        // CSENSE013: Stray <returns> tag on void or Task method
        if (isVoid && DocumentationExtensions.GetTargetElements(xml, DocumentationTags.Returns, recursive: true).Any())
        {
            var location = targetSymbol.GetDocumentationLocation(DocumentationTags.Returns, topLevelOnly: false);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, targetSymbol.GetDisplayName()));
        }

        // CSENSE015: Stray <value> tag on method
        if (DocumentationExtensions.GetTargetElements(xml, DocumentationTags.Value, recursive: true).Any())
        {
            var location = targetSymbol.GetDocumentationLocation(DocumentationTags.Value, topLevelOnly: false);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayValueDocumentationRule, location, targetSymbol.GetDisplayName()));
        }

        // CSENSE016: Low quality <returns> documentation
        foreach (var (element, location) in targetSymbol.GetTargetElementsWithLocations(xml, DocumentationTags.Returns, topLevelOnly: false))
        {
            if (!QualityAnalyzer.IsLowQuality(element, methodSymbol, targetSymbol, options))
                continue;

            QualityAnalyzer.Report(context, location, DocumentationTags.Returns, targetSymbol.GetDisplayName());
        }
    }
}
