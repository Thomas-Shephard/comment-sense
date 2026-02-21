using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class QualityAnalyzer
{
    private static readonly char[] TrimChars = ['.', '!', '?', ':', ' '];

    public static bool IsLowQuality(XElement element, ISymbol symbol, ISymbol targetSymbol, CommentSenseOptions options)
    {
        if (element.HasElements && string.IsNullOrWhiteSpace(element.Value))
            return false;

        var content = element.Value;
        var type = (symbol as IMethodSymbol)?.ReturnType ?? (symbol as IPropertySymbol)?.Type;

        // Properties use <value>, while methods and delegates use <returns>.
        // For delegates, symbol is the DelegateInvokeMethod (IMethodSymbol).
        var tagName = symbol is IPropertySymbol ? DocumentationTags.Value : DocumentationTags.Returns;

        if (IsLowQualityForAnyFormat(content, symbol, options, tagName))
            return true;

        if (!ReferenceEquals(symbol, targetSymbol) && IsLowQualityForAnyFormat(content, targetSymbol, options, tagName))
            return true;

        if (type is null)
            return false;

        var typeName = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        if (IsLowQuality(content, typeName, options, tagName: tagName))
            return true;

        // Also check the simple name (e.g., "List" for "List<int>")
        var simpleTypeName = type.Name;
        if (simpleTypeName != typeName && IsLowQuality(content, simpleTypeName, options, tagName: tagName))
            return true;

        return false;
    }

    public static bool IsLowQuality(XElement element, string symbolName, CommentSenseOptions options, string? tagName = null)
    {
        if (element.HasElements && string.IsNullOrWhiteSpace(element.Value))
            return false;

        return IsLowQuality(element.Value, symbolName, options, tagName);
    }

    public static bool IsLowQuality(string? content, string symbolName, CommentSenseOptions options, string? tagName = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return true;

        var contentSpan = content.AsSpan();
        if (IsPoorlyFormatted(contentSpan, options))
            return true;

        var normalized = contentSpan.Trim().TrimEnd(TrimChars);
        if (normalized.IsEmpty)
            return true;

        if (CheckBasicQuality(normalized, symbolName, options.MinSummaryLength, tagName))
            return true;

        foreach (var term in options.LowQualityTerms)
        {
            if (normalized.Equals(term.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return CheckSimilarity(normalized, symbolName, options.SimilarityThreshold);
    }

    private static bool IsPoorlyFormatted(ReadOnlySpan<char> content, CommentSenseOptions options)
    {
        if (options.RequireCapitalization && DocumentationQualityExtensions.StartsWithLowercase(content))
            return true;

        if (options.RequireEndingPunctuation && !DocumentationQualityExtensions.EndsWithPunctuation(content, trimEnd: true))
            return true;

        return false;
    }

    private static bool CheckSimilarity(ReadOnlySpan<char> normalized, string symbolName, double threshold)
    {
        if (threshold <= 0.0 || symbolName.Length == 0)
            return false;

        // Distance is at least the absolute difference in lengths.
        // Similarity = 1 - distance / maxLen.
        // Max possible similarity = 1 - abs(n - m) / maxLen = minLen / maxLen.
        double maxPossibleSimilarity = (double)Math.Min(normalized.Length, symbolName.Length) / Math.Max(normalized.Length, symbolName.Length);
        if (maxPossibleSimilarity < threshold)
            return false;

        return normalized.CalculateSimilarity(symbolName) >= threshold;
    }

    public static bool IsLowQualityForAnyFormat(XElement element, ISymbol symbol, CommentSenseOptions options, string? tagName = null)
    {
        if (element.HasElements && string.IsNullOrWhiteSpace(element.Value))
            return false;

        return IsLowQualityForAnyFormat(element.Value, symbol, options, tagName);
    }

    public static bool IsLowQualityForAnyFormat(string content, ISymbol symbol, CommentSenseOptions options, string? tagName = null)
    {
        var displayName = symbol.GetDisplayName();
        if (IsLowQuality(content, displayName, options, tagName: tagName))
            return true;

        var minimallyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return minimallyQualifiedName != displayName && IsLowQuality(content, minimallyQualifiedName, options, tagName: tagName);
    }

    private static bool CheckBasicQuality(ReadOnlySpan<char> normalized, string symbolName, int minLength, string? tagName)
    {
        // Use normalized length to ensure trailing punctuation doesn't artificially satisfy the requirement
        if (normalized.Length < minLength)
            return true;

        if (normalized.Equals(symbolName.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return true;

        if (tagName != null && normalized.Equals(tagName.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return true;

        // The word "return" is treated as low-quality only when documenting the <returns> tag
        return tagName == DocumentationTags.Returns && normalized.Equals("return".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    public static void Report(SymbolAnalysisContext context, Location location, string tagName, string targetName)
    {
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.LowQualityDocumentationRule, location, tagName, targetName));
    }
}
