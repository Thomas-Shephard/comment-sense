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
        foreach (var _ in summaryElements.Where(summaryElement =>
                     QualityAnalyzer.IsLowQualityForAnyFormat(summaryElement, symbol, options, SummaryTag)))
        {
            QualityAnalyzer.Report(context, symbol, SummaryTag, symbol.GetDisplayName());
        }
    }
}
