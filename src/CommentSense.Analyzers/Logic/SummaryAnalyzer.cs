using System.Xml.Linq;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class SummaryAnalyzer
{
    private const string SummaryTag = "summary";
    private static readonly string[] LowQualityValues = [SummaryTag];

    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml)
    {
        var summaries = DocumentationExtensions.GetTargetElements(xml, SummaryTag);
        foreach (var _ in summaries.Where(s => QualityAnalyzer.IsLowQuality(s, symbol.Name, LowQualityValues)))
        {
            QualityAnalyzer.Report(context, symbol, SummaryTag, symbol.Name);
        }
    }
}
