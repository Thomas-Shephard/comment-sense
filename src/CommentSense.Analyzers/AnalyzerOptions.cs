using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using CommentSense.Core;

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
            var visibilityLevel = GetEnumOption(o, globalOptions, "visibility_level", VisibilityLevel.Protected);

            // Backward compatibility for analyze_internal:
            // If the new visibility_level is not explicitly set, use analyze_internal as a fallback.
            if (!HasOption(o, globalOptions, "visibility_level") && GetBoolOption(o, globalOptions, "analyze_internal"))
                visibilityLevel = VisibilityLevel.Internal;

            return new CommentSenseOptions(
                VisibilityLevel: visibilityLevel,
                AllowImplicitInheritDoc: GetBoolOption(o, globalOptions, "allow_implicit_inheritdoc", true),
                LowQualityTerms: GetSetOption(o, globalOptions, "low_quality_terms", ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase)),
                Langwords: GetSetOption(o, globalOptions, "langwords", ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "true", "false", "null", "void")),
                IgnoredExceptions: GetSetOption(o, globalOptions, "ignored_exceptions", ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase)),
                IgnoreSystemExceptions: GetBoolOption(o, globalOptions, "ignore_system_exceptions"),
                IgnoredExceptionNamespaces: GetSetOption(o, globalOptions, "ignored_exception_namespaces", ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase)),
                MinSummaryLength: GetIntOption(o, globalOptions, "min_summary_length", 0),
                RequireEndingPunctuation: GetBoolOption(o, globalOptions, "require_ending_punctuation"),
                ExcludeConstants: GetBoolOption(o, globalOptions, "exclude_constants"),
                ExcludeEnums: GetBoolOption(o, globalOptions, "exclude_enums"),
                SimilarityThreshold: Math.Max(0.0, Math.Min(1.0, GetDoubleOption(o, globalOptions, "similarity_threshold", 0.0))),
                EnableConditionalSuppression: GetBoolOption(o, globalOptions, "enable_conditional_suppression"),
                ScanCalledMethodsForExceptions: GetBoolOption(o, globalOptions, "scan_called_methods_for_exceptions")
            );
        });
    }

    private static bool HasOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name)
    {
        var key = Prefix + name;
        return options.TryGetValue(key, out _) || globalOptions.TryGetValue(key, out _);
    }

    private static T GetEnumOption<T>(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, T defaultValue) where T : struct, Enum
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, true, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, true, out result))
            return result;

        return defaultValue;
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
        if (options.TryGetValue(key, out var value))
            return ParseSet(value);

        if (globalOptions.TryGetValue(key, out value))
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
    VisibilityLevel VisibilityLevel,
    bool AllowImplicitInheritDoc,
    IImmutableSet<string> LowQualityTerms,
    IImmutableSet<string> Langwords,
    IImmutableSet<string> IgnoredExceptions,
    bool IgnoreSystemExceptions,
    IImmutableSet<string> IgnoredExceptionNamespaces,
    int MinSummaryLength,
    bool RequireEndingPunctuation,
    bool ExcludeConstants,
    bool ExcludeEnums,
    double SimilarityThreshold,
    bool EnableConditionalSuppression,
    bool ScanCalledMethodsForExceptions
)
{
    [ExcludeFromCodeCoverage]
    public static CommentSenseOptions Default { get; } = new(
        VisibilityLevel: VisibilityLevel.Protected,
        AllowImplicitInheritDoc: true,
        LowQualityTerms: ImmutableHashSet<string>.Empty,
        Langwords: ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "true", "false", "null", "void"),
        IgnoredExceptions: ImmutableHashSet<string>.Empty,
        IgnoreSystemExceptions: false,
        IgnoredExceptionNamespaces: ImmutableHashSet<string>.Empty,
        MinSummaryLength: 0,
        RequireEndingPunctuation: false,
        ExcludeConstants: false,
        ExcludeEnums: false,
        SimilarityThreshold: 0.0,
        EnableConditionalSuppression: false,
        ScanCalledMethodsForExceptions: false
    );
}
