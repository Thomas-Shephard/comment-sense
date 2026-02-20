using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class ReturnValueAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options, DocumentationLocationCache locationCache)
    {
        Analyze(context, symbol, symbol, xml, options, locationCache);
    }

    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, ISymbol targetSymbol, XElement xml, CommentSenseOptions options, DocumentationLocationCache locationCache)
    {
        var methodSymbol = symbol as IMethodSymbol;
        var isTask = methodSymbol != null && methodSymbol.ReturnType.IsTaskType();
        var isVoid = methodSymbol is { ReturnsVoid: true } || isTask;

        if (targetSymbol is IPropertySymbol property)
            AnalyzeProperty(context, property, targetSymbol, xml, options, locationCache);
        else if (methodSymbol != null)
            AnalyzeMethod(context, methodSymbol, targetSymbol, xml, options, isVoid, locationCache);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, IPropertySymbol property, ISymbol targetSymbol, XElement xml, CommentSenseOptions options, DocumentationLocationCache locationCache)
    {
        var hasInheritDoc = DocumentationXmlExtensions.HasInheritDoc(xml);
        var hasAutoValidTag = DocumentationXmlExtensions.HasAutoValidTag(xml);

        // CSENSE014: Missing <value> documentation
        if (property.GetMethod is not null && !DocumentationXmlExtensions.HasValueTag(xml) && !hasInheritDoc && !hasAutoValidTag)
        {
            var location = targetSymbol.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, DocumentationTags.Value);
            var symbolName = targetSymbol.GetDisplayName();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingValueDocumentationRule, location, properties, symbolName));
        }

        // CSENSE013: Stray <returns> tag on property
        foreach (var (_, location) in targetSymbol.GetTargetElementsWithLocations(xml, DocumentationTags.Returns, locationCache, topLevelOnly: false))
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, targetSymbol.GetDisplayName()));
        }

        // CSENSE015: Stray <value> tag on property (and quality check)
        var seenValue = false;
        var effectiveTarget = DocumentationXmlExtensions.GetEffectiveTarget(xml);
        foreach (var (element, location) in targetSymbol.GetTargetElementsWithLocations(xml, DocumentationTags.Value, locationCache, topLevelOnly: false))
        {
            bool isTopLevel = DocumentationXmlExtensions.IsTopLevel(xml, element, effectiveTarget);

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

    private static void AnalyzeMethod(SymbolAnalysisContext context, IMethodSymbol methodSymbol, ISymbol targetSymbol, XElement xml, CommentSenseOptions options, bool isVoid, DocumentationLocationCache locationCache)
    {
        var hasInheritDoc = DocumentationXmlExtensions.HasInheritDoc(xml);
        var hasAutoValidTag = DocumentationXmlExtensions.HasAutoValidTag(xml);

        // CSENSE006: Missing <returns> documentation
        if (!isVoid && !DocumentationXmlExtensions.HasReturnsTag(xml) && !hasInheritDoc && !hasAutoValidTag)
        {
            var location = targetSymbol.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, DocumentationTags.Returns);
            var symbolName = targetSymbol.GetDisplayName();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingReturnValueDocumentationRule, location, properties, symbolName));
        }

        // CSENSE013: Stray <returns> tag (on void/Task, nested, or duplicate)
        // CSENSE016: Low quality <returns> documentation
        var seenReturns = false;
        var effectiveTarget = DocumentationXmlExtensions.GetEffectiveTarget(xml);
        foreach (var (element, location) in targetSymbol.GetTargetElementsWithLocations(xml, DocumentationTags.Returns, locationCache, topLevelOnly: false))
        {
            bool isTopLevel = DocumentationXmlExtensions.IsTopLevel(xml, element, effectiveTarget);

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

        // CSENSE015: Stray <value> tag on method
        foreach (var (_, location) in targetSymbol.GetTargetElementsWithLocations(xml, DocumentationTags.Value, locationCache, topLevelOnly: false))
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayValueDocumentationRule, location, targetSymbol.GetDisplayName()));
        }
    }
}
