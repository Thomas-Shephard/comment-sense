using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers;

internal static class AnalyzerOptions
{
    private const string Prefix = "comment_sense.";

    private static readonly ConditionalWeakTable<AnalyzerConfigOptions, CommentSenseOptions> OptionsCache = new();

    public static CommentSenseOptions GetOptions(AnalyzerConfigOptionsProvider provider, SyntaxTree tree)
    {
        var options = provider.GetOptions(tree);
        return OptionsCache.GetValue(options, o =>
        {
            var globalOptions = provider.GlobalOptions;
            return new CommentSenseOptions(
                AnalyzeInternal: GetBoolOption(o, globalOptions, "analyze_internal"),
                AllowImplicitInheritDoc: GetBoolOption(o, globalOptions, "allow_implicit_inheritdoc", true),
                LowQualityTerms: GetSetOption(o, globalOptions, "low_quality_terms", []),
                IgnoredExceptions: GetSetOption(o, globalOptions, "ignored_exceptions", []),
                IgnoreSystemExceptions: GetBoolOption(o, globalOptions, "ignore_system_exceptions"),
                IgnoredExceptionNamespaces: GetSetOption(o, globalOptions, "ignored_exception_namespaces", []),
                MinSummaryLength: GetIntOption(o, globalOptions, "min_summary_length", 0),
                RequireEndingPunctuation: GetBoolOption(o, globalOptions, "require_ending_punctuation"),
                ExcludeConstants: GetBoolOption(o, globalOptions, "exclude_constants"),
                SimilarityThreshold: Math.Max(0.0, Math.Min(1.0, GetDoubleOption(o, globalOptions, "similarity_threshold", 0.0)))
            );
        });
    }

    private static bool GetBoolOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, bool defaultValue = false)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && bool.TryParse(value, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && bool.TryParse(value, out result))
            return result;

        return defaultValue;
    }

    private static int GetIntOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, int defaultValue)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && int.TryParse(value, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && int.TryParse(value, out result))
            return result;

        return defaultValue;
    }

    private static double GetDoubleOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, double defaultValue)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
            return result;

        return defaultValue;
    }

    private static IImmutableSet<string> GetSetOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, IImmutableSet<string> defaultValue)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            return ParseSet(value);

        if (globalOptions.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
            return ParseSet(value);

        return defaultValue;
    }

    private static ImmutableHashSet<string> ParseSet(string value)
    {
        var terms = value.Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        return terms;
    }
}

internal record CommentSenseOptions(
    bool AnalyzeInternal,
    bool AllowImplicitInheritDoc,
    IImmutableSet<string> LowQualityTerms,
    IImmutableSet<string> IgnoredExceptions,
    bool IgnoreSystemExceptions,
    IImmutableSet<string> IgnoredExceptionNamespaces,
    int MinSummaryLength,
    bool RequireEndingPunctuation,
    bool ExcludeConstants,
    double SimilarityThreshold
)
{
    public static readonly CommentSenseOptions Default = new(
        AnalyzeInternal: false,
        AllowImplicitInheritDoc: true,
        LowQualityTerms: ImmutableHashSet<string>.Empty,
        IgnoredExceptions: ImmutableHashSet<string>.Empty,
        IgnoreSystemExceptions: false,
        IgnoredExceptionNamespaces: ImmutableHashSet<string>.Empty,
        MinSummaryLength: 0,
        RequireEndingPunctuation: false,
        ExcludeConstants: false,
        SimilarityThreshold: 0.0
    );
}
