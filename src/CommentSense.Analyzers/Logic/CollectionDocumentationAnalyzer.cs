using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class CollectionDocumentationAnalyzer
{
    public readonly record struct CollectionRuleSet(
        string TagName,
        DiagnosticDescriptor MissingRule,
        DiagnosticDescriptor StrayRule,
        DiagnosticDescriptor DuplicateRule,
        DiagnosticDescriptor OrderMismatchRule);

    public static void Analyze<TSymbol>(
        SymbolAnalysisContext context,
        ImmutableArray<TSymbol> symbols,
        ISymbol parentSymbol,
        XElement xml,
        CommentSenseOptions options,
        CollectionRuleSet rules,
        bool topLevelOnly = true) where TSymbol : ISymbol
    {
        if (symbols.IsEmpty && !xml.Descendants(rules.TagName).Any())
            return;

        var documentedNames = GetDocumentedNames(xml, rules.TagName, topLevelOnly);
        var documentedSet = new HashSet<string>(documentedNames, StringComparer.Ordinal);

        if (!DocumentationXmlExtensions.HasInheritDoc(xml) && !DocumentationXmlExtensions.HasAutoValidTag(xml))
            ReportMissing(context, symbols, documentedSet, rules.MissingRule);

        var actualIndexMap = new Dictionary<string, int>(symbols.Length, StringComparer.Ordinal);
        for (int i = 0; i < symbols.Length; i++)
        {
            actualIndexMap[symbols[i].Name] = i;
        }

        ValidateDocumented(context, parentSymbol, xml, actualIndexMap, options, rules, topLevelOnly);
    }

    private static IEnumerable<string> GetDocumentedNames(XElement xml, string tagName, bool topLevelOnly)
    {
        return DocumentationXmlExtensions.GetNames(xml, tagName, attributeName: DocumentationAttributes.Name, topLevelOnly: topLevelOnly);
    }

    private static void ReportMissing<TSymbol>(
        SymbolAnalysisContext context,
        ImmutableArray<TSymbol> symbols,
        HashSet<string> documentedSet,
        DiagnosticDescriptor rule) where TSymbol : ISymbol
    {
        foreach (var symbol in symbols.Where(s => !documentedSet.Contains(s.Name)))
        {
            var location = symbol.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, symbol.Name);
            context.ReportDiagnostic(Diagnostic.Create(rule, location, properties, symbol.Name));
        }
    }

    private static void ValidateDocumented(
        SymbolAnalysisContext context,
        ISymbol symbol,
        XElement xml,
        Dictionary<string, int> actualIndexMap,
        CommentSenseOptions options,
        CollectionRuleSet rules,
        bool topLevelOnly)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var lastActualIndex = -1;

        foreach (var (element, location) in symbol.GetTargetElementsWithLocations(xml, rules.TagName, topLevelOnly: topLevelOnly))
        {
            var name = element.Attribute(DocumentationAttributes.Name)?.Value;
            if (name == null || string.IsNullOrWhiteSpace(name))
                continue;

            if (!seen.TryGetValue(name, out var occurrence))
                occurrence = 0;

            seen[name] = occurrence + 1;

            if (occurrence > 0)
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, name);
                context.ReportDiagnostic(Diagnostic.Create(rules.DuplicateRule, location, properties, name));
                continue;
            }

            if (!actualIndexMap.TryGetValue(name, out var currentIndex))
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, name);
                context.ReportDiagnostic(Diagnostic.Create(rules.StrayRule, location, properties, name));
                continue;
            }

            if (QualityAnalyzer.IsLowQuality(element, name, options, tagName: rules.TagName))
            {
                QualityAnalyzer.Report(context, location, rules.TagName, name);
            }

            if (currentIndex < lastActualIndex)
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, name);
                context.ReportDiagnostic(Diagnostic.Create(rules.OrderMismatchRule, location, properties, name));
            }

            lastActualIndex = currentIndex;
        }
    }
}
