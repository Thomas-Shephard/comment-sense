using System.Collections.Immutable;
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
        DocumentationComment documentation,
        CommentSenseOptions options,
        CollectionRuleSet rules) where TSymbol : ISymbol
    {
        if (symbols.IsEmpty && !documentation.GetElements(rules.TagName, recursive: true).Any())
            return;

        var actualIndexMap = new Dictionary<string, int>(symbols.Length, StringComparer.Ordinal);
        for (int i = 0; i < symbols.Length; i++)
        {
            actualIndexMap[symbols[i].Name] = i;
        }

        var documentedSet = ValidateDocumented(context, documentation, actualIndexMap, options, rules);

        if (!documentation.HasInheritDoc() && !documentation.HasAutoValidTag())
            ReportMissing(context, symbols, documentedSet, rules.MissingRule);
    }

    private static void ReportMissing<TSymbol>(
        SymbolAnalysisContext context,
        ImmutableArray<TSymbol> symbols,
        HashSet<string> documentedSet,
        DiagnosticDescriptor rule) where TSymbol : ISymbol
    {
        foreach (var symbol in symbols)
        {
            if (documentedSet.Contains(symbol.Name))
                continue;

            var location = symbol.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, symbol.Name);
            context.ReportDiagnostic(Diagnostic.Create(rule, location, properties, symbol.Name));
        }
    }

    private static HashSet<string> ValidateDocumented(
        SymbolAnalysisContext context,
        DocumentationComment documentation,
        Dictionary<string, int> actualIndexMap,
        CommentSenseOptions options,
        CollectionRuleSet rules)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var documentedSet = new HashSet<string>(StringComparer.Ordinal);
        var lastActualIndex = -1;

        foreach (var element in documentation.GetElements(rules.TagName, recursive: true))
        {
            var location = element.GetLocation();
            var name = element.GetAttributeValue(DocumentationAttributes.Name);
            if (!documentation.IsTopLevel(element))
            {
                ReportStray(context, rules.StrayRule, location, name);
                continue;
            }

            if (name is null || string.IsNullOrWhiteSpace(name))
                continue;

            documentedSet.Add(name);
            if (IsDuplicate(name, seen))
            {
                ReportNamed(context, rules.DuplicateRule, location, name);
                continue;
            }

            if (!actualIndexMap.TryGetValue(name, out var currentIndex))
            {
                ReportNamed(context, rules.StrayRule, location, name);
                continue;
            }

            if (QualityAnalyzer.IsLowQuality(element, name, options, tagName: rules.TagName))
                QualityAnalyzer.Report(context, location, rules.TagName, name);

            if (currentIndex < lastActualIndex)
                ReportNamed(context, rules.OrderMismatchRule, location, name);

            lastActualIndex = currentIndex;
        }

        return documentedSet;
    }

    private static bool IsDuplicate(string name, Dictionary<string, int> seen)
    {
        seen.TryGetValue(name, out var occurrence);
        seen[name] = occurrence + 1;
        return occurrence > 0;
    }

    private static void ReportStray(
        SymbolAnalysisContext context,
        DiagnosticDescriptor rule,
        Location location,
        string? name)
    {
        var cleanName = string.IsNullOrWhiteSpace(name) ? string.Empty : name;
        var displayName = string.IsNullOrWhiteSpace(name) ? "<unknown>" : name;
        var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, cleanName);
        context.ReportDiagnostic(Diagnostic.Create(rule, location, properties, displayName));
    }

    private static void ReportNamed(
        SymbolAnalysisContext context,
        DiagnosticDescriptor rule,
        Location location,
        string name)
    {
        var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.NameProperty, name);
        context.ReportDiagnostic(Diagnostic.Create(rule, location, properties, name));
    }
}
