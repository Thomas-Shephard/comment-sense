using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class QualityAnalyzer
{
    public static bool IsLowQuality(XElement element, string symbolName, string[]? lowQualityKeywords = null)
    {
        if (element.HasElements)
            return false;

        return IsLowQualityInternal(element.Value, symbolName, lowQualityKeywords);
    }

    private static bool IsLowQualityInternal(string? content, string symbolName, string[]? lowQualityKeywords = null)
    {
        if (content is null || string.IsNullOrWhiteSpace(content))
            return true;

        var normalized = content.Trim().TrimEnd('.', '!', '?', ':', ' ');

        if (string.IsNullOrWhiteSpace(normalized))
            return true;

        if (string.Equals(normalized, symbolName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (lowQualityKeywords is null)
            return false;

        return lowQualityKeywords.Any(keyword => string.Equals(normalized, keyword, StringComparison.OrdinalIgnoreCase));
    }

    public static void Report(SymbolAnalysisContext context, ISymbol symbol, string tagName, string targetName)
    {
        var location = symbol.Locations.GetPrimaryLocation();
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.LowQualityDocumentationRule, location, tagName, targetName));
    }

    public static void Report(SymbolAnalysisContext context, Location location, string tagName, string targetName)
    {
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.LowQualityDocumentationRule, location, tagName, targetName));
    }
}
