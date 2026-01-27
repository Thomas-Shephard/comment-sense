using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers;

internal static class AnalyzerOptions
{
    private const string Prefix = "comment_sense";

    private static readonly ConditionalWeakTable<AnalyzerConfigOptions, ConcurrentDictionary<string, ImmutableHashSet<string>>> StringListCache = new();
    private static readonly ConditionalWeakTable<AnalyzerConfigOptions, ConcurrentDictionary<string, bool>> BoolCache = new();

    public static ImmutableHashSet<string> GetStringListOption(AnalyzerConfigOptionsProvider optionsProvider, SyntaxTree tree, string optionName)
    {
        var options = optionsProvider.GetOptions(tree);
        var optionsCache = StringListCache.GetOrCreateValue(options);

        if (optionsCache.TryGetValue(optionName, out var cached))
            return cached;

        var result = options.TryGetValue($"{Prefix}.{optionName}", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Split([','], StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)
            : [];

        return optionsCache.GetOrAdd(optionName, result);
    }

    public static bool GetBoolOption(AnalyzerConfigOptionsProvider optionsProvider, SyntaxTree tree, string optionName, bool defaultValue)
    {
        var options = optionsProvider.GetOptions(tree);
        var optionsCache = BoolCache.GetOrCreateValue(options);

        if (optionsCache.TryGetValue(optionName, out var cached))
            return cached;

        var result = options.TryGetValue($"{Prefix}.{optionName}", out var value) && bool.TryParse(value, out var resultValue)
            ? resultValue
            : defaultValue;

        return optionsCache.GetOrAdd(optionName, result);
    }
}
