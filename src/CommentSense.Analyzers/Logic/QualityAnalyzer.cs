using System.Xml.Linq;
using CommentSense.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class QualityAnalyzer
{
    private static readonly char[] PunctuationChars = ['.', '!', '?'];
    private static readonly char[] TrimChars = [.. PunctuationChars, ':', ' '];

    public static bool IsLowQuality(XElement element, ISymbol symbol, ISymbol targetSymbol, CommentSenseOptions options)
    {
        var type = (symbol as IMethodSymbol)?.ReturnType ?? (symbol as IPropertySymbol)?.Type;

        // Properties use <value>, while methods and delegates use <returns>.
        // For delegates, symbol is the DelegateInvokeMethod (IMethodSymbol).
        var tagName = symbol is IPropertySymbol ? DocumentationTags.Value : DocumentationTags.Returns;

        if (IsLowQualityForAnyFormat(element, symbol, options, tagName))
            return true;

        if (!ReferenceEquals(symbol, targetSymbol) && IsLowQualityForAnyFormat(element, targetSymbol, options, tagName))
            return true;

        if (type is null)
            return false;

        var typeName = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        if (IsLowQuality(element, typeName, options, tagName: tagName))
            return true;

        // Also check the simple name (e.g., "List" for "List<int>")
        var simpleTypeName = type.Name;
        if (simpleTypeName != typeName && IsLowQuality(element, simpleTypeName, options, tagName: tagName))
            return true;

        return false;
    }

    public static bool IsLowQuality(XElement element, string symbolName, CommentSenseOptions options, string? tagName = null)
    {
        if (element.HasElements)
            return false;

        return IsLowQuality(element.Value, symbolName, options, tagName);
    }

    public static bool IsLowQuality(string? content, string symbolName, CommentSenseOptions options, string? tagName = null)
    {
        if (content is null || string.IsNullOrWhiteSpace(content))
            return true;

        var trimmed = content.Trim();
        if (options.RequireCapitalization && StartsWithLowercase(trimmed))
            return true;

        if (options.RequireEndingPunctuation && !HasEndingPunctuation(trimmed))
            return true;

        var normalized = trimmed.TrimEnd(TrimChars);
        if (string.IsNullOrEmpty(normalized))
            return true;

        if (CheckBasicQuality(normalized, symbolName, options.MinSummaryLength, tagName))
            return true;

        if (options.LowQualityTerms.Contains(normalized))
            return true;

        return options.SimilarityThreshold > 0.0 && CalculateSimilarity(normalized, symbolName) >= options.SimilarityThreshold;
    }

    public static bool IsLowQualityForAnyFormat(XElement element, ISymbol symbol, CommentSenseOptions options, string? tagName = null)
    {
        var displayName = symbol.GetDisplayName();
        if (IsLowQuality(element, displayName, options, tagName: tagName))
            return true;

        var minimallyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return minimallyQualifiedName != displayName && IsLowQuality(element, minimallyQualifiedName, options, tagName: tagName);
    }

    private static bool HasEndingPunctuation(string content)
    {
        var lastChar = content[content.Length - 1];
        return PunctuationChars.Contains(lastChar);
    }

    private static bool StartsWithLowercase(string content) =>
        content.Length > 0 && char.IsLetter(content, 0) && char.IsLower(content, 0);

    public static double CalculateSimilarity(string source, string target)
    {
        if (source.Equals(target, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        var distance = ComputeLevenshteinDistance(source.AsSpan(), target.AsSpan());
        return 1.0 - (double)distance / Math.Max(source.Length, target.Length);
    }

    private static bool CheckBasicQuality(string normalized, string symbolName, int minLength, string? tagName)
    {
        // Use normalized length to ensure trailing punctuation doesn't artificially satisfy the requirement
        if (normalized.Length < minLength)
            return true;

        if (string.Equals(normalized, symbolName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (tagName != null && string.Equals(normalized, tagName, StringComparison.OrdinalIgnoreCase))
            return true;

        // The word "return" is treated as low-quality only when documenting the <returns> tag
        return tagName == DocumentationTags.Returns && string.Equals(normalized, "return", StringComparison.OrdinalIgnoreCase);
    }

    private static int ComputeLevenshteinDistance(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        if (s.Length < t.Length)
        {
            var temp = s;
            s = t;
            t = temp;
        }

        int n = s.Length;
        int m = t.Length;

        const int maxStackLimit = 256;
        var rowSize = m + 1;
        Span<int> previousRow = rowSize <= maxStackLimit ? stackalloc int[rowSize] : new int[rowSize];
        Span<int> currentRow = rowSize <= maxStackLimit ? stackalloc int[rowSize] : new int[rowSize];

        for (var j = 0; j <= m; j++)
            previousRow[j] = j;

        for (var i = 0; i < n; i++)
        {
            currentRow[0] = i + 1;

            for (var j = 0; j < m; j++)
            {
                var cost = char.ToUpperInvariant(s[i]) == char.ToUpperInvariant(t[j]) ? 0 : 1;
                currentRow[j + 1] = Math.Min(
                    Math.Min(currentRow[j] + 1, previousRow[j + 1] + 1),
                    previousRow[j] + cost);
            }

            var tempRow = previousRow;
            previousRow = currentRow;
            currentRow = tempRow;
        }

        return previousRow[m];
    }

    public static void Report(SymbolAnalysisContext context, Location location, string tagName, string targetName)
    {
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.LowQualityDocumentationRule, location, tagName, targetName));
    }
}
