using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers;

internal static class AnalyzerOptions
{
    private const string Prefix = "comment_sense";

    private static readonly ConditionalWeakTable<AnalyzerConfigOptions, CommentSenseOptions> OptionsCache = new();

    private static ImmutableHashSet<string> GetStringListOption(AnalyzerConfigOptions options, string optionName)
    {
        return options.TryGetValue($"{Prefix}.{optionName}", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Split([','], StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
    }

    private static bool GetBoolOption(AnalyzerConfigOptions options, string optionName, bool defaultValue)
    {
        return options.TryGetValue($"{Prefix}.{optionName}", out var value) && bool.TryParse(value, out var resultValue)
            ? resultValue
            : defaultValue;
    }

    public static CommentSenseOptions GetOptions(AnalyzerConfigOptionsProvider optionsProvider, SyntaxTree tree)
    {
        var options = optionsProvider.GetOptions(tree);
        return OptionsCache.GetValue(options, o => new CommentSenseOptions(
            AnalyzeInternal: GetBoolOption(o, "analyze_internal", false),
            AllowImplicitInheritDoc: GetBoolOption(o, "allow_implicit_inheritdoc", true),
            LowQualityTerms: GetStringListOption(o, "low_quality_terms"),
            IgnoredExceptions: GetStringListOption(o, "ignored_exceptions")
        ));
    }
}

internal record CommentSenseOptions(
    bool AnalyzeInternal,
    bool AllowImplicitInheritDoc,
    ImmutableHashSet<string> LowQualityTerms,
    ImmutableHashSet<string> IgnoredExceptions
);
