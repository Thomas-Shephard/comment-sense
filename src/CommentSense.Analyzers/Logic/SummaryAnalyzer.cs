using System.Xml.Linq;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class SummaryAnalyzer
{
    private const string SummaryTag = "summary";

    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options)
    {
        var summaryElements = DocumentationExtensions.GetTargetElements(xml, SummaryTag).ToList();
        if (summaryElements.Count == 0)
            return;

        // Check for low-quality documentation against multiple symbol formats (e.g., friendly name and qualified name)
        var summaryLocations = symbol.GetDocumentationLocations(SummaryTag);
        for (var i = 0; i < summaryElements.Count; i++)
        {
            var summaryElement = summaryElements[i];
            if (!QualityAnalyzer.IsLowQualityForAnyFormat(summaryElement, symbol, options, SummaryTag))
                continue;

            var location = summaryLocations.GetLocationOrDefault(i, symbol);
            QualityAnalyzer.Report(context, location, SummaryTag, symbol.GetDisplayName());
        }
    }
}
