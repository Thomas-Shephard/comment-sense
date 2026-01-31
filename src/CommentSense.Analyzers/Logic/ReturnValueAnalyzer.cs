using System.Collections.Immutable;
using System.Xml.Linq;
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
            var symbolName = targetSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingValueDocumentationRule, location, properties, symbolName));
        }

        // CSENSE013: Stray <returns> tag on property
        if (DocumentationExtensions.HasReturnsTag(xml))
        {
            var location = targetSymbol.Locations.GetPrimaryLocation();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, targetSymbol.Name));
        }

        // CSENSE016: Low quality <value> documentation
        var valueElements = DocumentationExtensions.GetTargetElements(xml, ValueTag);
        foreach (var _ in valueElements.Where(e => QualityAnalyzer.IsLowQuality(e, property, targetSymbol, options)))
        {
            QualityAnalyzer.Report(context, targetSymbol, ValueTag, targetSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
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
            var symbolName = targetSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingReturnValueDocumentationRule, location, properties, symbolName));
        }

        // CSENSE013: Stray <returns> tag on void or Task method
        if (isVoid && DocumentationExtensions.HasReturnsTag(xml))
        {
            var location = targetSymbol.Locations.GetPrimaryLocation();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, targetSymbol.Name));
        }

        // CSENSE015: Stray <value> tag on method
        if (DocumentationExtensions.HasValueTag(xml))
        {
            var location = targetSymbol.Locations.GetPrimaryLocation();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayValueDocumentationRule, location, targetSymbol.Name));
        }

        // CSENSE016: Low quality <returns> documentation
        var returnsElements = DocumentationExtensions.GetTargetElements(xml, ReturnsTag);
        foreach (var _ in returnsElements.Where(e => QualityAnalyzer.IsLowQuality(e, methodSymbol, targetSymbol, options)))
        {
            QualityAnalyzer.Report(context, targetSymbol, ReturnsTag, targetSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        }
    }
}
