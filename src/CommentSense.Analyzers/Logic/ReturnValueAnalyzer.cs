using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class ReturnValueAnalyzer
{
    private const string ReturnsTag = "returns";
    private const string ValueTag = "value";

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
            var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", ValueTag);
            var symbolName = targetSymbol.GetDisplayName();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingValueDocumentationRule, location, properties, symbolName));
        }

        // CSENSE013: Stray <returns> tag on property
        if (DocumentationExtensions.HasReturnsTag(xml))
        {
            var location = targetSymbol.GetDocumentationLocation(ReturnsTag);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, targetSymbol.GetDisplayName()));
        }

        // CSENSE016: Low quality <value> documentation
        var valueElements = DocumentationExtensions.GetTargetElements(xml, ValueTag).ToList();
        var valueLocations = targetSymbol.GetDocumentationLocations(ValueTag);
        for (var i = 0; i < valueElements.Count; i++)
        {
            var element = valueElements[i];
            if (!QualityAnalyzer.IsLowQuality(element, property, targetSymbol, options))
                continue;

            var location = valueLocations.GetLocationOrDefault(i, targetSymbol);
            QualityAnalyzer.Report(context, location, ValueTag, targetSymbol.GetDisplayName());
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
            var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", ReturnsTag);
            var symbolName = targetSymbol.GetDisplayName();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingReturnValueDocumentationRule, location, properties, symbolName));
        }

        // CSENSE013: Stray <returns> tag on void or Task method
        if (isVoid && DocumentationExtensions.HasReturnsTag(xml))
        {
            var location = targetSymbol.GetDocumentationLocation(ReturnsTag);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, targetSymbol.GetDisplayName()));
        }

        // CSENSE015: Stray <value> tag on method
        if (DocumentationExtensions.HasValueTag(xml))
        {
            var location = targetSymbol.GetDocumentationLocation(ValueTag);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayValueDocumentationRule, location, targetSymbol.GetDisplayName()));
        }

        // CSENSE016: Low quality <returns> documentation
        var returnsElements = DocumentationExtensions.GetTargetElements(xml, ReturnsTag).ToList();
        var returnsLocations = targetSymbol.GetDocumentationLocations(ReturnsTag);
        for (var i = 0; i < returnsElements.Count; i++)
        {
            var element = returnsElements[i];
            if (!QualityAnalyzer.IsLowQuality(element, methodSymbol, targetSymbol, options))
                continue;

            var location = returnsLocations.GetLocationOrDefault(i, targetSymbol);
            QualityAnalyzer.Report(context, location, ReturnsTag, targetSymbol.GetDisplayName());
        }
    }
}
