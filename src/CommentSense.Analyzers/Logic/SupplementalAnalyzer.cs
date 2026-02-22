using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class SupplementalAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options)
    {
        var displayName = symbol.GetDisplayName();
        var minimallyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        AnalyzeTag(context, symbol, xml, options, DocumentationTags.Remarks, displayName, minimallyQualifiedName);
        AnalyzeTag(context, symbol, xml, options, DocumentationTags.Example, displayName, minimallyQualifiedName);
    }

    private static void AnalyzeTag(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options, string tagName, string displayName, string minimallyQualifiedName)
    {
        foreach (var (element, location) in symbol.GetTargetElementsWithLocations(xml, tagName, topLevelOnly: true))
        {
            if (QualityAnalyzer.IsLowQualityForAnyFormat(element, displayName, minimallyQualifiedName, options, tagName))
                QualityAnalyzer.Report(context, location, tagName, displayName);
        }
    }
}
