using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class QualityAnalyzer
{
    private static readonly char[] TrimChars = ['.', '!', '?', ':', ' '];

    public static bool IsLowQuality(XElement element, ISymbol symbol, ISymbol targetSymbol, CommentSenseOptions options)
    {
        if (!TryGetContent(element, out var content))
            return false;

        return IsLowQualityCore(content, symbol, targetSymbol, options);
    }

    public static bool IsLowQuality(XmlNodeSyntax element, ISymbol symbol, ISymbol targetSymbol, CommentSenseOptions options)
    {
        if (!TryGetContent(element, out var content))
            return false;

        return IsLowQualityCore(content, symbol, targetSymbol, options);
    }

    public static bool IsLowQuality(XElement element, string symbolName, CommentSenseOptions options, string? tagName = null)
    {
        if (!TryGetContent(element, out var content))
            return false;

        return IsLowQuality(content, symbolName, options, tagName);
    }

    public static bool IsLowQuality(XmlNodeSyntax element, string symbolName, CommentSenseOptions options, string? tagName = null)
    {
        if (!TryGetContent(element, out var content))
            return false;

        return IsLowQuality(content, symbolName, options, tagName);
    }

    public static bool IsLowQuality(string? content, string symbolName, CommentSenseOptions options, string? tagName = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return true;

        var contentSpan = content.AsSpan();
        if (IsPoorlyFormatted(contentSpan, options))
            return true;

        var normalized = contentSpan.Trim().TrimEnd(TrimChars);
        return normalized.IsEmpty || CheckNameQuality(normalized, symbolName, options, tagName);
    }

    public static bool IsLowQualityForAnyFormat(XElement element, string displayName, string minimallyQualifiedName, CommentSenseOptions options, string? tagName = null)
    {
        if (!TryGetContent(element, out var content))
            return false;

        return IsLowQualityForAnyFormat(content, displayName, minimallyQualifiedName, options, tagName);
    }

    public static bool IsLowQualityForAnyFormat(XmlNodeSyntax element, string displayName, string minimallyQualifiedName, CommentSenseOptions options, string? tagName = null)
    {
        if (!TryGetContent(element, out var content))
            return false;

        return IsLowQualityForAnyFormat(content, displayName, minimallyQualifiedName, options, tagName);
    }

    public static bool IsLowQualityForAnyFormat(string content, string displayName, string minimallyQualifiedName, CommentSenseOptions options, string? tagName = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return true;

        var contentSpan = content.AsSpan();
        if (IsPoorlyFormatted(contentSpan, options))
            return true;

        var normalized = contentSpan.Trim().TrimEnd(TrimChars);
        if (normalized.IsEmpty)
            return true;

        if (CheckNameQuality(normalized, displayName, options, tagName))
            return true;

        return minimallyQualifiedName != displayName && CheckNameQuality(normalized, minimallyQualifiedName, options, tagName);
    }

    public static bool IsLowQualityForAnyFormat(string content, ISymbol symbol, CommentSenseOptions options, string? tagName = null)
    {
        var displayName = symbol.GetDisplayName();
        var minimallyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return IsLowQualityForAnyFormat(content, displayName, minimallyQualifiedName, options, tagName);
    }

    private static bool IsLowQualityCore(string content, ISymbol symbol, ISymbol targetSymbol, CommentSenseOptions options)
    {
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

        var simpleTypeName = type.Name;
        return simpleTypeName != typeName && IsLowQuality(content, simpleTypeName, options, tagName: tagName);
    }

    private static bool TryGetContent(XElement element, out string content)
    {
        content = element.Value;
        return !(element.HasElements && string.IsNullOrWhiteSpace(content));
    }

    private static bool TryGetContent(XmlNodeSyntax element, out string content)
    {
        content = element.GetInnerText();
        return !(element.HasChildElements() && string.IsNullOrWhiteSpace(content));
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

    private static bool CheckNameQuality(ReadOnlySpan<char> normalized, string symbolName, CommentSenseOptions options, string? tagName)
    {
        if (CheckBasicQuality(normalized, symbolName, options.MinSummaryLength, tagName))
            return true;

        foreach (var term in options.LowQualityTerms)
        {
            if (normalized.Equals(term.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return CheckSimilarity(normalized, symbolName, options.SimilarityThreshold);
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
