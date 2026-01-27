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
    private const string NameProperty = "Name";

    private static readonly ImmutableHashSet<string> DefaultReturnsKeywords = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, ReturnsTag, "return");
    private static readonly ImmutableHashSet<string> DefaultValueKeywords = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, ValueTag);

    public static void Analyze(SymbolAnalysisContext context, IMethodSymbol method, XElement xml, ImmutableHashSet<string> customLowQualityTerms)
    {
        Analyze(context, method, method, xml, customLowQualityTerms);
    }

    public static void Analyze(SymbolAnalysisContext context, IMethodSymbol method, ISymbol reportSymbol, XElement xml, ImmutableHashSet<string> customLowQualityTerms)
    {
        var location = reportSymbol.Locations.GetPrimaryLocation();
        var symbolName = reportSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        // Methods should not have <value> tag
        if (DocumentationExtensions.HasValueTag(xml))
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayValueDocumentationRule, location, symbolName));
        }

        bool effectivelyReturnsVoid = method.ReturnsVoid || IsTaskOrValueTask(method.ReturnType);
        bool hasReturnsTag = DocumentationExtensions.HasReturnsTag(xml);

        if (effectivelyReturnsVoid)
        {
            if (hasReturnsTag)
            {
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, symbolName));
            }
        }
        else if (!hasReturnsTag)
        {
            var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, ReturnsTag);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingReturnValueDocumentationRule, location, properties, symbolName));
        }
        else
        {
            var returnsElements = DocumentationExtensions.GetTargetElements(xml, ReturnsTag);
            foreach (var _ in returnsElements.Where(e => QualityAnalyzer.IsLowQuality(e, method.ReturnType.Name, customLowQualityTerms) ||
                                                         QualityAnalyzer.IsLowQuality(e, method.ReturnType.Name, DefaultReturnsKeywords)))
            {
                QualityAnalyzer.Report(context, reportSymbol, ReturnsTag, symbolName);
            }
        }
    }

    public static void Analyze(SymbolAnalysisContext context, IPropertySymbol property, XElement xml, ImmutableHashSet<string> customLowQualityTerms)
    {
        var location = property.Locations.GetPrimaryLocation();
        var symbolName = property.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        // Properties and indexers should not have <returns> tag
        if (DocumentationExtensions.HasReturnsTag(xml))
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, symbolName));
        }

        bool hasValueTag = DocumentationExtensions.HasValueTag(xml);

        if (property.GetMethod == null)
            return;

        if (!hasValueTag)
        {
            var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, ValueTag);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingValueDocumentationRule, location, properties, symbolName));
        }
        else
        {
            var valueElements = DocumentationExtensions.GetTargetElements(xml, ValueTag);
            foreach (var _ in valueElements.Where(e => QualityAnalyzer.IsLowQuality(e, property.Type.Name, customLowQualityTerms) ||
                                                       QualityAnalyzer.IsLowQuality(e, property.Type.Name, DefaultValueKeywords) ||
                                                       QualityAnalyzer.IsLowQuality(e, property.Name, customLowQualityTerms)))
            {
                QualityAnalyzer.Report(context, property, ValueTag, symbolName);
            }
        }
    }

    private static bool IsTaskOrValueTask(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol { Arity: 0 } namedType)
            return false;

        if (namedType.Name != "Task" && namedType.Name != "ValueTask")
            return false;

        var ns = namedType.ContainingNamespace.ToDisplayString();
        return ns == "System.Threading.Tasks";
    }
}
