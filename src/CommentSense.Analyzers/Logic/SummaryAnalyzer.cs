using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class SummaryAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options)
    {
        // Check for low-quality documentation against multiple symbol formats (e.g., friendly name and qualified name)
        foreach (var (summaryElement, location) in symbol.GetTargetElementsWithLocations(xml, DocumentationTags.Summary, topLevelOnly: true))
        {
            if (!QualityAnalyzer.IsLowQualityForAnyFormat(summaryElement, symbol, options, DocumentationTags.Summary))
                continue;

            QualityAnalyzer.Report(context, location, DocumentationTags.Summary, symbol.GetDisplayName());
        }
    }
}
